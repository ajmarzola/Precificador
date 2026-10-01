using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autenticacao;
using Precificador.Web.Email;

namespace Precificador.Tests.Integration.Web;

public sealed class ContaAcessoPageTests
{
    [Fact]
    public async Task Login_exibe_link_e_UC038_nao_envia_email()
    {
        using var original = new CustomWebApplicationFactory();
        var sender = new SenderFake();
        using var factory = Configurar(original, sender);
        using (var scope = factory.Services.CreateScope())
        {
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await roles.CreateAsync(new IdentityRole("SystemAdmin"))).Succeeded);
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var user = new UsuarioAplicacao { Email = "admin@teste.local", UserName = "admin@teste.local" };
            Assert.True((await manager.CreateAsync(user, "SenhaTeste1")).Succeeded);
            Assert.True((await manager.AddToRoleAsync(user, "SystemAdmin")).Succeeded);
        }
        using var client = Cliente(factory);
        Assert.Contains("href=\"/Conta/EsqueciSenha\"", await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Conta/Login")));
        var response = await client.PostAsync("/?handler=SolicitarAcesso", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.NomeEmpresa"] = "Empresa", ["Input.NomeResponsavel"] = "Responsável", ["Input.EmailResponsavel"] = "solicitante@teste.local",
            ["__RequestVerificationToken"] = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/")
        }));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Empty(sender.Mensagens);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Estado_de_password_impede_usar_rota_errada(bool comSenha)
    {
        using var original = new CustomWebApplicationFactory();
        using var factory = Configurar(original, new SenderFake());
        var usuario = await CriarUsuario(factory, comSenha);
        string token;
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            Assert.Equal(TokensConta.Recuperacao, manager.Options.Tokens.PasswordResetTokenProvider);
            Assert.Equal(TimeSpan.FromHours(48), scope.ServiceProvider.GetRequiredService<IOptions<OpcoesTokenAtivacao>>().Value.TokenLifespan);
            Assert.Equal(TimeSpan.FromHours(1), scope.ServiceProvider.GetRequiredService<IOptions<OpcoesTokenRecuperacao>>().Value.TokenLifespan);
            var user = (await manager.FindByIdAsync(usuario.Id))!;
            token = comSenha ? await manager.GenerateUserTokenAsync(user, TokensConta.Ativacao, TokensConta.PurposeAtivacao)
                : await manager.GeneratePasswordResetTokenAsync(user);
        }
        using var client = Cliente(factory);
        var rota = comSenha ? "Ativar" : "RedefinirSenha";
        var code = TokensConta.Codificar(token);
        var url = $"/Conta/{rota}?userId={usuario.Id}&code={code}";
        Assert.Contains("inválido ou expirou", await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(url)));
        Assert.Contains("inválido ou expirou", await WebTestHtml.LerHtmlDecodificadoAsync(await EnviarSenha(client, url, usuario.Id, code)));
    }

    [Theory]
    [InlineData("smtp")]
    [InlineData("url")]
    public void Startup_rejeita_configuracao_habilitada_invalida(string erro)
    {
        using var original = new CustomWebApplicationFactory();
        using var factory = original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.Configure<OpcoesSmtp>(options =>
            {
                options.Enabled = true; options.Host = erro == "smtp" ? "" : "smtp.test";
                options.Port = 587; options.FromAddress = "sistema@teste.local";
            });
            services.Configure<OpcoesAplicacao>(options => options.UrlPublica = erro == "url" ? "relativa" : "https://publico.test");
        }));
        var exception = Record.Exception(() => factory.CreateClient());
        Assert.NotNull(exception);
        var erros = exception is AggregateException aggregate ? aggregate.Flatten().InnerExceptions : [exception];
        Assert.All(erros, item => Assert.IsType<OptionsValidationException>(item));
    }
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Fluxo_completo_muda_credencial_stamp_invalida_reuso_sem_autologin(bool ativacao)
    {
        using var original = new CustomWebApplicationFactory();
        var sender = new SenderFake();
        using var factory = Configurar(original, sender);
        var usuario = await CriarUsuario(factory, !ativacao);
        string token;
        string stamp;
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var user = (await manager.FindByIdAsync(usuario.Id))!;
            stamp = user.SecurityStamp!;
            token = ativacao ? await manager.GenerateUserTokenAsync(user, TokensConta.Ativacao, TokensConta.PurposeAtivacao)
                : await manager.GeneratePasswordResetTokenAsync(user);
        }
        var rota = ativacao ? "Ativar" : "RedefinirSenha";
        var url = $"/Conta/{rota}?userId={usuario.Id}&code={TokensConta.Codificar(token)}";
        using var client = Cliente(factory);
        Assert.Contains("Nova senha", await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(url)));
        var response = await EnviarSenha(client, url, usuario.Id, TokensConta.Codificar(token));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Conta/Login", response.Headers.Location?.OriginalString);
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var user = (await manager.FindByIdAsync(usuario.Id))!;
            Assert.True(await manager.CheckPasswordAsync(user, "NovaSenha123"));
            Assert.False(await manager.CheckPasswordAsync(user, "SenhaTeste1"));
            Assert.NotEqual(stamp, user.SecurityStamp);
            if (ativacao) Assert.True(user.EmailConfirmed);
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            Assert.Single(await db.Empresas.ToListAsync());
            Assert.Empty(await db.UsuariosEmpresas.ToListAsync());
        }
        Assert.Contains("inválido ou expirou", await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(url)));
        Assert.Contains("inválido ou expirou", await WebTestHtml.LerHtmlDecodificadoAsync(await EnviarSenha(client, url, usuario.Id, TokensConta.Codificar(token))));
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync("/Admin")).StatusCode);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public async Task Recuperacao_publica_neutra_preserva_usuario_e_envia_tipo_correto(int estado)
    {
        using var original = new CustomWebApplicationFactory();
        var sender = new SenderFake { Falhar = estado == 3 };
        using var factory = Configurar(original, sender);
        var usuario = estado == 0 ? null : await CriarUsuario(factory, estado != 2);
        string? hash = null;
        using (var scope = factory.Services.CreateScope())
            if (usuario is not null) hash = (await scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>().FindByIdAsync(usuario.Id))!.PasswordHash;
        using var client = Cliente(factory);
        var form = new Dictionary<string, string> { ["Input.Email"] = usuario?.Email ?? "ausente@teste.local",
            ["__RequestVerificationToken"] = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Conta/EsqueciSenha") };
        var response = await client.PostAsync("/Conta/EsqueciSenha", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Conta/EsqueciSenhaConfirmacao", response.Headers.Location?.OriginalString);
        Assert.Contains("Se existir uma conta para esse e-mail, enviaremos as instruções de acesso.",
            await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location)));
        if (estado == 0) Assert.Empty(sender.Mensagens);
        else
        {
            var mensagem = Assert.Single(sender.Mensagens);
            Assert.Equal(usuario!.Email, mensagem.Destinatario);
            Assert.Contains(estado == 2 ? "/Conta/Ativar?" : "/Conta/RedefinirSenha?", mensagem.Texto);
            Assert.Contains(estado == 2 ? "48 horas" : "1 hora", mensagem.Texto);
            Assert.DoesNotContain("SenhaTeste1", mensagem.Texto);
            Assert.Contains("https://publico.test/base/Conta/", mensagem.Texto);
            Assert.Contains("&amp;code=", mensagem.Html);
            var link = new Uri(mensagem.Texto.Split('\n')[1]);
            var code = QueryHelpers.ParseQuery(link.Query)["code"].ToString();
            Assert.NotNull(TokensConta.Decodificar(code));
            Assert.DoesNotContain("+", code);
            Assert.DoesNotContain("/", code);
            if (estado == 3)
            {
                Assert.Single(sender.Logs);
                Assert.DoesNotContain(usuario.Email!, sender.Logs[0]);
                Assert.DoesNotContain(code, sender.Logs[0]);
                Assert.DoesNotContain("https://", sender.Logs[0]);
            }
            using var scope = factory.Services.CreateScope();
            Assert.Equal(hash, (await scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>().FindByIdAsync(usuario.Id))!.PasswordHash);
        }
    }

    [Theory]
    [InlineData("Ativar")]
    [InlineData("RedefinirSenha")]
    public async Task Links_invalidos_providers_cruzados_senha_policy_e_confirmacao(string rota)
    {
        using var original = new CustomWebApplicationFactory();
        using var factory = Configurar(original, new SenderFake());
        var usuario = await CriarUsuario(factory, rota != "Ativar");
        using var client = Cliente(factory);
        foreach (var query in new[] { "", "?userId=" + usuario.Id, "?userId=" + usuario.Id + "&code=%%%", "?userId=inexistente&code=YWJj" })
            Assert.Contains("inválido ou expirou", await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Conta/" + rota + query)));
        string certo, errado;
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var user = (await manager.FindByIdAsync(usuario.Id))!;
            var ativar = await manager.GenerateUserTokenAsync(user, TokensConta.Ativacao, TokensConta.PurposeAtivacao);
            var reset = await manager.GeneratePasswordResetTokenAsync(user);
            certo = TokensConta.Codificar(rota == "Ativar" ? ativar : reset);
            errado = TokensConta.Codificar(rota == "Ativar" ? reset : ativar);
        }
        var urlErrada = $"/Conta/{rota}?userId={usuario.Id}&code={errado}";
        Assert.Contains("inválido ou expirou", await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(urlErrada)));
        Assert.Contains("inválido ou expirou", await WebTestHtml.LerHtmlDecodificadoAsync(await EnviarSenha(client, urlErrada, usuario.Id, errado)));
        var url = $"/Conta/{rota}?userId={usuario.Id}&code={certo}";
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>()))).StatusCode);
        Assert.Contains("validation-summary-errors", await WebTestHtml.LerHtmlDecodificadoAsync(await EnviarSenha(client, url, usuario.Id, certo, "fraca", "fraca")));
        Assert.Contains("As senhas não coincidem", await WebTestHtml.LerHtmlDecodificadoAsync(await EnviarSenha(client, url, usuario.Id, certo, "NovaSenha123", "OutraSenha123")));
        using var leitura = factory.Services.CreateScope();
        var users = leitura.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
        Assert.Equal(rota != "Ativar", await users.HasPasswordAsync((await users.FindByIdAsync(usuario.Id))!));
    }

    [Fact]
    public async Task Servico_ativacao_nao_necessario_indisponivel_sem_email_e_falha_nao_mutam()
    {
        using var original = new CustomWebApplicationFactory();
        var sender = new SenderFake();
        using var factory = Configurar(original, sender);
        var comSenha = await CriarUsuario(factory, true);
        var semSenha = await CriarUsuario(factory, false);
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
        var conta = scope.ServiceProvider.GetRequiredService<ServicoConta>();
        Assert.Equal(ResultadoEnvioConta.NaoNecessario, await conta.EnviarAtivacaoAsync((await manager.FindByIdAsync(comSenha.Id))!));
        Assert.Empty(sender.Mensagens);
        var user = (await manager.FindByIdAsync(semSenha.Id))!;
        var stamp = user.SecurityStamp;
        sender.Disponivel = false;
        Assert.Equal(ResultadoEnvioConta.Indisponivel, await conta.EnviarAtivacaoAsync(user));
        sender.Disponivel = true;
        sender.Falhar = true;
        Assert.Equal(ResultadoEnvioConta.Falhou, await conta.EnviarAtivacaoAsync(user));
        Assert.Null(user.PasswordHash);
        Assert.Equal(stamp, user.SecurityStamp);
        user.Email = null;
        Assert.Equal(ResultadoEnvioConta.Indisponivel, await conta.EnviarAtivacaoAsync(user));
    }

    [Fact]
    public async Task Smtp_disabled_503_antes_da_busca_email_invalido_e_antiforgery()
    {
        using var original = new CustomWebApplicationFactory();
        var interceptor = new ImpedirBuscaUsuario();
        using var factory = original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<PrecificadorDbContext>(options => options.AddInterceptors(interceptor))));
        using var client = Cliente(factory);
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Conta/EsqueciSenha");
        interceptor.Bloquear = true;
        foreach (var email in new[] { "ausente@teste.local", "existente@teste.local" })
            Assert.Equal(HttpStatusCode.ServiceUnavailable, (await client.PostAsync("/Conta/EsqueciSenha", new FormUrlEncodedContent(
                new Dictionary<string, string> { ["Input.Email"] = email, ["__RequestVerificationToken"] = token }))).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync("/Conta/EsqueciSenha", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Input.Email"] = "invalido", ["__RequestVerificationToken"] = token }))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/Conta/EsqueciSenha", new FormUrlEncodedContent(
            new Dictionary<string, string> { ["Input.Email"] = "ausente@teste.local" }))).StatusCode);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Autenticado_redireciona_todas_paginas_GET_POST_sem_email(bool admin, bool empresa)
    {
        using var original = new CustomWebApplicationFactory();
        var sender = new SenderFake();
        using var factory = Configurar(original, sender);
        var usuario = await CriarUsuario(factory, true);
        using (var scope = factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            await roles.CreateAsync(new IdentityRole("SystemAdmin"));
            var global = new UsuarioAplicacao { UserName = "admin@teste.local", Email = "admin@teste.local" };
            Assert.True((await manager.CreateAsync(global, "SenhaTeste1")).Succeeded);
            await manager.AddToRoleAsync(global, "SystemAdmin");
            if (admin) await manager.AddToRoleAsync((await manager.FindByIdAsync(usuario.Id))!, "SystemAdmin");
            if (empresa && !admin)
            {
                var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
                db.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = 1, Ativo = true, Perfil = PerfilUsuarioEmpresa.Operacional });
                await db.SaveChangesAsync();
            }
        }
        using var client = Cliente(factory);
        var loginToken = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Conta/Login");
        var login = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["Input.Email"] = usuario.Email!, ["Input.Senha"] = "SenhaTeste1", ["__RequestVerificationToken"] = loginToken }));
        var destino = admin ? "/Admin" : empresa ? "/Dashboard" : "/Empresas/Selecionar";
        Assert.Equal(destino, login.Headers.Location?.OriginalString);
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, destino);
        foreach (var rota in new[] { "Ativar", "RedefinirSenha", "EsqueciSenha", "EsqueciSenhaConfirmacao" })
        {
            Assert.Equal(destino, (await client.GetAsync("/Conta/" + rota)).Headers.Location?.OriginalString);
            if (rota == "EsqueciSenhaConfirmacao") continue;
            Assert.Equal(destino, (await client.PostAsync("/Conta/" + rota, new FormUrlEncodedContent(new Dictionary<string, string>
                { ["__RequestVerificationToken"] = token, ["Input.Email"] = usuario.Email! }))).Headers.Location?.OriginalString);
        }
        Assert.Empty(sender.Mensagens);
    }

    private static WebApplicationFactory<Program> Configurar(CustomWebApplicationFactory original, SenderFake sender) => original.WithWebHostBuilder(builder =>
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IEmailSenderAplicacao>();
            services.AddSingleton<IEmailSenderAplicacao>(sender);
            services.AddSingleton<ILogger<ServicoConta>>(new LoggerConta(sender.Logs));
            services.Configure<OpcoesAplicacao>(options => options.UrlPublica = "https://publico.test/base/");
        }));
    private static HttpClient Cliente(WebApplicationFactory<Program> factory) => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private static async Task<UsuarioAplicacao> CriarUsuario(WebApplicationFactory<Program> factory, bool senha)
    {
        using var scope = factory.Services.CreateScope();
        var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        var user = new UsuarioAplicacao { UserName = email, Email = email };
        Assert.True((senha ? await manager.CreateAsync(user, "SenhaTeste1") : await manager.CreateAsync(user)).Succeeded);
        return user;
    }
    private static async Task<HttpResponseMessage> EnviarSenha(HttpClient client, string url, string id, string code, string senha = "NovaSenha123", string confirmacao = "NovaSenha123")
    {
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Conta/EsqueciSenha");
        return await client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string, string>
        { ["UserId"] = id, ["Code"] = code, ["Input.Senha"] = senha, ["Input.Confirmacao"] = confirmacao, ["__RequestVerificationToken"] = token }));
    }
    internal sealed class SenderFake : IEmailSenderAplicacao
    {
        public bool Disponivel { get; set; } = true;
        public bool Falhar { get; set; }
        public List<MensagemEmail> Mensagens { get; } = [];
        public List<string> Logs { get; } = [];
        public Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default)
        {
            Mensagens.Add(mensagem);
            if (Falhar) throw new InvalidOperationException("falha simulada");
            return Task.CompletedTask;
        }
    }
    private sealed class LoggerConta(List<string> logs) : ILogger<ServicoConta>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => logs.Add(formatter(state, exception) + (exception?.ToString() ?? ""));
    }
    private sealed class ImpedirBuscaUsuario : Microsoft.EntityFrameworkCore.Diagnostics.DbCommandInterceptor
    {
        public bool Bloquear { get; set; }
        public override ValueTask<Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader>> ReaderExecutingAsync(
            System.Data.Common.DbCommand command, Microsoft.EntityFrameworkCore.Diagnostics.CommandEventData eventData,
            Microsoft.EntityFrameworkCore.Diagnostics.InterceptionResult<System.Data.Common.DbDataReader> result, CancellationToken cancellationToken = default)
        {
            if (Bloquear && command.CommandText.Contains("AspNetUsers")) throw new InvalidOperationException("Busca de usuário proibida neste cenário.");
            return ValueTask.FromResult(result);
        }
    }
}
