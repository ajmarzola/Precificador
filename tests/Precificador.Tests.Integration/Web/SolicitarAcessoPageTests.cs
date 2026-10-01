using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Precificador.Core.Acessos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class SolicitarAcessoPageTests
{
    private static readonly DateTimeOffset Agora = DateTimeOffset.Parse("2026-10-01T14:30:00Z");

    [Fact]
    public async Task Home_anonima_exibe_campos_login_privacidade_sem_senha_e_sem_persistir()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = new WebTestContext(factory).CriarCliente();
        var response = await client.GetAsync("/");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);
        Assert.Contains("href=\"/Conta/Login\"", html);
        Assert.Contains("Solicitar acesso", html);
        foreach (var campo in new[] { "NomeEmpresa", "NomeResponsavel", "EmailResponsavel", "Observacao" })
            Assert.Contains($"name=\"Input.{campo}\"", html);
        Assert.DoesNotContain("type=\"password\"", html);
        Assert.Contains("Usaremos os dados informados", html);
        Assert.Empty(await Pedidos(factory));
    }

    [Fact]
    public async Task Post_valido_normaliza_usa_relogio_PRG_e_nao_altera_empresa_identity()
    {
        using var original = new CustomWebApplicationFactory();
        const string emailExistente = "existente@teste.local";
        using var factory = original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(new RelogioFixo());
        }));
        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            db.Users.Add(new Precificador.Infrastructure.Autenticacao.UsuarioAplicacao
            { Id = "existente", UserName = emailExistente, Email = emailExistente, PasswordHash = "hash-preservado" });
            await db.SaveChangesAsync();
        }
        var antes = await Contagens(factory);
        using var client = Cliente(factory);
        var response = await Enviar(client, "  Minha\t Empresa  ", "  " + emailExistente + "  ", "  primeira\nsegunda  ");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/", response.Headers.Location?.OriginalString);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/"));
        Assert.Contains("Recebemos sua solicitação de acesso.", html);
        var pedido = Assert.Single(await Pedidos(factory));
        Assert.Equal("Minha Empresa", pedido.NomeEmpresa);
        Assert.Equal("MINHA EMPRESA", pedido.NomeEmpresaNormalizado);
        Assert.Equal("João Silva", pedido.NomeResponsavel);
        Assert.Equal(emailExistente, pedido.EmailResponsavel);
        Assert.Equal(emailExistente.ToUpperInvariant(), pedido.EmailResponsavelNormalizado);
        Assert.Equal("primeira\nsegunda", pedido.Observacao);
        Assert.Equal(Agora, pedido.DataSolicitacaoUtc);
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Pendente, pedido.Situacao);
        Assert.Equal(antes, await Contagens(factory));
        using var leitura = factory.Services.CreateScope();
        Assert.Equal("hash-preservado", (await leitura.ServiceProvider.GetRequiredService<PrecificadorDbContext>().Users.SingleAsync()).PasswordHash);
    }

    [Theory]
    [InlineData("Input.NomeEmpresa", " ")]
    [InlineData("Input.NomeResponsavel", " ")]
    [InlineData("Input.EmailResponsavel", "")]
    [InlineData("Input.EmailResponsavel", "invalido")]
    [InlineData("Input.NomeEmpresa", "longo")]
    [InlineData("Input.NomeResponsavel", "longo")]
    [InlineData("Input.EmailResponsavel", "longo")]
    [InlineData("Input.Observacao", "longo")]
    public async Task Post_invalido_reexibe_erros_e_valores_sem_persistir(string campo, string valor)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Cliente(factory);
        var formulario = Formulario();
        formulario[campo] = valor == "longo" ? new string('a', campo == "Input.Observacao" ? 1001 : campo == "Input.EmailResponsavel" ? 257 : 121) : valor;
        formulario["__RequestVerificationToken"] = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/");
        var response = await client.PostAsync("/?handler=SolicitarAcesso", new FormUrlEncodedContent(formulario));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);
        Assert.Contains("field-validation-error", html);
        Assert.Contains("value=\"" + WebUtility.HtmlDecode(formulario["Input.NomeEmpresa"]) + "\"", html);
        Assert.Empty(await Pedidos(factory));
    }

    [Fact]
    public async Task Post_sem_antiforgery_e_rejeitado()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Cliente(factory);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsync("/?handler=SolicitarAcesso", new FormUrlEncodedContent(Formulario()))).StatusCode);
        Assert.Empty(await Pedidos(factory));
    }

    [Theory]
    [InlineData(1, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    public async Task Reenvio_respeita_estado_preserva_original_e_retorna_mesmo_sucesso(int situacao, int quantidade)
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Cliente(factory);
        await Enviar(client, observacao: "original");
        var original = Assert.Single(await Pedidos(factory));
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>().Database.ExecuteSqlAsync($"UPDATE SolicitacoesAcessoEmpresas SET Situacao = {situacao}");
        var response = await Enviar(client, "  EMPRESA   teste ", "JOAO@TESTE.LOCAL", "alterada");
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("Recebemos sua solicitação de acesso.", await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/")));
        var pedidos = await Pedidos(factory);
        Assert.Equal(quantidade, pedidos.Length);
        var preservado = pedidos.Single(x => x.Id == original.Id);
        Assert.Equal(original.DataSolicitacaoUtc, preservado.DataSolicitacaoUtc);
        Assert.Equal("original", preservado.Observacao);
        Assert.Equal(original.NomeResponsavel, preservado.NomeResponsavel);
        Assert.Equal(original.NomeEmpresa, preservado.NomeEmpresa);
        Assert.Equal(original.EmailResponsavel, preservado.EmailResponsavel);
        if (quantidade == 2) Assert.Single(pedidos, x => x.Situacao == SituacaoSolicitacaoAcessoEmpresa.Pendente);
    }

    [Fact]
    public async Task Pares_distintos_permitidos_e_observacao_vazia_null()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Cliente(factory);
        await Enviar(client);
        await Enviar(client, "Outra Empresa");
        await Enviar(client, email: "outro@teste.local");
        var pedidos = await Pedidos(factory);
        Assert.Equal(3, pedidos.Length);
        Assert.All(pedidos, x => Assert.Null(x.Observacao));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, true)]
    [InlineData(false, false)]
    public async Task Post_autenticado_preserva_destino_canonico_sem_persistir(bool admin, bool comEmpresa)
    {
        using var factory = new CustomWebApplicationFactory();
        var context = new WebTestContext(factory);
        var usuario = admin ? await context.CriarSystemAdminAsync() : await context.CriarUsuarioAsync(comEmpresa ? [1] : []);
        using var client = context.CriarCliente();
        await context.LoginAsync(client, usuario.Email, usuario.Senha);
        var destino = admin ? "/Admin" : comEmpresa ? "/Dashboard" : "/Empresas/Selecionar";
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, destino);
        var form = Formulario();
        form["__RequestVerificationToken"] = token;
        var response = await client.PostAsync("/?handler=SolicitarAcesso", new FormUrlEncodedContent(form));
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(destino, response.Headers.Location?.OriginalString);
        Assert.Empty(await Pedidos(factory));
    }

    [Fact]
    public async Task Corrida_real_apos_consulta_resulta_em_uma_pendente_e_dois_sucessos()
    {
        using var original = new CustomWebApplicationFactory();
        var barreira = new BarreiraInsercao();
        using var factory = original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            services.AddDbContext<PrecificadorDbContext>(options => options.AddInterceptors(barreira))));
        using var primeiro = Cliente(factory);
        using var segundo = Cliente(factory);
        // Inicializar ambos os cookies antes de disparar a corrida.
        var token1 = await WebTestHtml.ObterTokenAntiforgeryAsync(primeiro, "/");
        var token2 = await WebTestHtml.ObterTokenAntiforgeryAsync(segundo, "/");
        var a = Formulario(); a["__RequestVerificationToken"] = token1;
        var b = Formulario(); b["__RequestVerificationToken"] = token2;
        var responses = await Task.WhenAll(
            primeiro.PostAsync("/?handler=SolicitarAcesso", new FormUrlEncodedContent(a)),
            segundo.PostAsync("/?handler=SolicitarAcesso", new FormUrlEncodedContent(b)));
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.Redirect, response.StatusCode));
        Assert.Equal(2, barreira.Chegadas);
        Assert.Single(await Pedidos(factory));
    }

    [Fact]
    public async Task Erro_de_banco_nao_relacionado_nao_vira_sucesso_publico()
    {
        using var factory = new CustomWebApplicationFactory();
        using var client = Cliente(factory);
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>().Database.ExecuteSqlRawAsync(
                "ALTER TABLE SolicitacoesAcessoEmpresas ADD CONSTRAINT CK_Teste_Rejeitar CHECK (NomeEmpresa <> N'Empresa teste')");
        var response = await Enviar(client);
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Empty(await Pedidos(factory));
    }

    private static HttpClient Cliente(WebApplicationFactory<Program> factory) => factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    private static Dictionary<string, string> Formulario(string nome = "Empresa teste", string email = "joao@teste.local", string observacao = " ") => new()
    {
        ["Input.NomeEmpresa"] = nome, ["Input.NomeResponsavel"] = "  João  Silva  ",
        ["Input.EmailResponsavel"] = email, ["Input.Observacao"] = observacao,
        ["Input.Situacao"] = "2", ["Input.DataSolicitacaoUtc"] = "2000-01-01T00:00:00Z", ["Input.EmpresaId"] = "1", ["Input.Id"] = "999"
    };
    private static async Task<HttpResponseMessage> Enviar(HttpClient client, string nome = "Empresa teste", string email = "joao@teste.local", string observacao = " ")
    {
        var form = Formulario(nome, email, observacao);
        form["__RequestVerificationToken"] = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/");
        return await client.PostAsync("/?handler=SolicitarAcesso", new FormUrlEncodedContent(form));
    }
    private static async Task<SolicitacaoAcessoEmpresa[]> Pedidos(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>().SolicitacoesAcessoEmpresas.AsNoTracking().ToArrayAsync();
    }
    private static async Task<int[]> Contagens(WebApplicationFactory<Program> factory)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        return [await db.Empresas.CountAsync(), await db.Users.CountAsync(), await db.Roles.CountAsync(), await db.UserRoles.CountAsync(), await db.UsuariosEmpresas.CountAsync()];
    }
    private sealed class RelogioFixo : TimeProvider { public override DateTimeOffset GetUtcNow() => Agora; }
    private sealed class BarreiraInsercao : SaveChangesInterceptor
    {
        private readonly TaskCompletionSource liberar = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private int chegadas;
        public int Chegadas => chegadas;
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<SolicitacaoAcessoEmpresa>().Any(x => x.State == EntityState.Added))
            {
                if (Interlocked.Increment(ref chegadas) == 2) liberar.TrySetResult();
                await liberar.Task.WaitAsync(TimeSpan.FromSeconds(30), cancellationToken);
            }
            return result;
        }
    }
}
