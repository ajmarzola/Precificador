using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Tests.Integration.Infrastructure;

namespace Precificador.Tests.Integration.Web;

public sealed class IdentidadeVisualEmpresaPageTests
{
    private const string Url = "/Configuracoes/IdentidadeVisual";
    private static readonly byte[] Png = Convert.FromHexString("89504E470D0A1A0A000000");
    private static readonly byte[] Jpeg = Convert.FromHexString("FFD8FFE00000");

    [Fact]
    public async Task GET_default_sem_linha_e_ciclo_salvar_preservar_substituir_remover_restaurar()
    {
        using var h = new Cenario(); await h.Preparar(); using var admin = await h.Cliente(PerfilUsuarioEmpresa.Administrador);
        var html = await Html(admin, Url);
        Assert.Contains("Padrão do Precificador", html); Assert.Contains("#0D6EFD", html);
        Assert.DoesNotContain("src=\"/Empresa/Logo\"", html); Assert.Null(await h.Ler());
        Assert.Equal(HttpStatusCode.NotFound, (await admin.GetAsync("/Empresa/Logo")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "#aabbcc", Png, nome: "arquivo.svg", tipo: "text/html")).StatusCode);
        var identidade = (await h.Ler())!;
        Assert.Equal("#AABBCC", identidade.CorPrimaria); Assert.Equal(Png, identidade.LogoConteudo); Assert.Equal("image/png", identidade.LogoContentType);
        await Logo(admin, Png, "image/png");
        Assert.Contains("Identidade visual atualizada com sucesso.", await Html(admin, Url));
        await Post(admin, "#112233"); Assert.Equal(Png, (await h.Ler())!.LogoConteudo);
        await Post(admin, "#445566", Jpeg, nome: "logo.png", tipo: "image/png");
        await Logo(admin, Jpeg, "image/jpeg");
        await Post(admin, "#778899", remover: true);
        identidade = (await h.Ler())!; Assert.Equal("#778899", identidade.CorPrimaria); Assert.Null(identidade.LogoConteudo); Assert.Null(identidade.LogoContentType);
        await Post(admin, "#123456", Png);
        for (var i = 0; i < 2; i++)
        {
            var token = await WebTestHtml.ObterTokenAntiforgeryAsync(admin, Url);
            Assert.Equal(HttpStatusCode.Redirect, (await admin.PostAsync(Url + "?handler=RestaurarPadrao", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = token }))).StatusCode);
            Assert.Null(await h.Ler());
        }
        Assert.Contains("Padrão do Precificador", await Html(admin, Url));
    }

    [Theory]
    [InlineData("cor")] [InlineData("cor-vazia")] [InlineData("css")] [InlineData("vazio")] [InlineData("svg")]
    [InlineData("grande")] [InlineData("simultaneo")]
    public async Task Input_invalido_preserva_cor_e_logo(string caso)
    {
        using var h = new Cenario(); await h.Preparar(); using var admin = await h.Cliente(PerfilUsuarioEmpresa.Administrador);
        await Post(admin, "#112233", Png);
        var bytes = caso switch { "vazio" => Array.Empty<byte>(), "svg" => "<svg/>"u8.ToArray(), "grande" => new byte[524289], "simultaneo" => Jpeg, _ => null };
        var cor = caso switch { "cor" => "#FFF", "cor-vazia" => "", "css" => "red; color:red", _ => "#ABCDEF" };
        var resposta = await Post(admin, cor, bytes, caso == "simultaneo");
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        var html = WebUtility.HtmlDecode(await resposta.Content.ReadAsStringAsync());
        Assert.Contains(caso is "cor" or "cor-vazia" or "css" ? "Selecione uma cor válida" : caso == "grande" ? "no máximo 512 KiB" : caso == "simultaneo" ? "não as duas opções" : "PNG ou JPEG válido", html);
        var identidade = (await h.Ler())!; Assert.Equal("#112233", identidade.CorPrimaria); Assert.Equal(Png, identidade.LogoConteudo);
    }

    [Fact]
    public async Task Upload_no_limite_e_antiforgery_mass_assignment()
    {
        using var h = new Cenario(); await h.Preparar(); using var admin = await h.Cliente(PerfilUsuarioEmpresa.Administrador);
        var bytes = new byte[524288]; Png.CopyTo(bytes, 0);
        Assert.Equal(HttpStatusCode.Redirect, (await Post(admin, "#abcdef", bytes, empresaManipulada: h.OutraId)).StatusCode);
        Assert.Equal(bytes, (await h.Ler())!.LogoConteudo); Assert.Null(await h.Ler(h.OutraId));
        foreach (var suffix in new[] { "", "?handler=RestaurarPadrao" })
            Assert.Equal(HttpStatusCode.BadRequest, (await admin.PostAsync(Url + suffix, new FormUrlEncodedContent(new Dictionary<string,string> { ["Input.CorPrimaria"] = "#112233" }))).StatusCode);
        Assert.Equal("#ABCDEF", (await h.Ler())!.CorPrimaria);
    }

    [Theory]
    [InlineData("anonimo")] [InlineData("operacional")] [InlineData("systemadmin")]
    [InlineData("inativo")] [InlineData("suspensa")] [InlineData("sem-empresa")]
    public async Task Configuracao_e_handlers_exigem_Admin_ativo(string caso)
    {
        using var h = new Cenario(); await h.Preparar();
        using var client = h.Web.CriarCliente();
        if (caso == "anonimo") await h.Web.CriarUsuarioAsync(PerfilUsuarioEmpresa.Administrador, h.EmpresaId);
        UsuarioTeste? usuario = null;
        if (caso != "anonimo")
        {
            usuario = caso == "systemadmin" ? await h.Web.CriarSystemAdminAsync() :
                await h.Web.CriarUsuarioAsync(caso == "operacional" ? PerfilUsuarioEmpresa.Operacional : PerfilUsuarioEmpresa.Administrador,
                    caso == "sem-empresa" ? new[] { h.EmpresaId, h.OutraId } : new[] { h.EmpresaId });
            await h.Web.LoginAsync(client, usuario.Email, usuario.Senha);
        }
        if (caso is "inativo" or "suspensa")
        {
            using var scope = h.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            if (caso == "inativo") (await db.UsuariosEmpresas.SingleAsync(x => x.UsuarioId == usuario!.Id && x.EmpresaId == h.EmpresaId)).Ativo = false;
            else (await db.Empresas.SingleAsync(x => x.Id == h.EmpresaId)).Suspender();
            await db.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync(Url)).StatusCode);
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, caso == "systemadmin" ? "/Admin" : caso == "anonimo" ? "/Conta/Login" : "/Empresas/Selecionar");
        foreach (var suffix in new[] { "", "?handler=RestaurarPadrao" })
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync(Url + suffix, new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = token, ["Input.CorPrimaria"] = "#112233" }))).StatusCode);
        Assert.Null(await h.Ler());
    }

    [Fact]
    public async Task Admin_e_operacional_consomem_identidade_e_dashboard_preserva_cards()
    {
        using var h = new Cenario(); await h.Preparar(); using var admin = await h.Cliente(PerfilUsuarioEmpresa.Administrador);
        await Post(admin, "#112233", Png);
        using var operacional = await h.Cliente(PerfilUsuarioEmpresa.Operacional);
        foreach (var client in new[] { admin, operacional })
        {
            var html = await Html(client, "/Dashboard");
            Assert.Contains("--empresa-cor-primaria: #112233", html); Assert.Contains("empresa-navbar", html);
            Assert.Contains("src=\"/Empresa/Logo\"", html); Assert.Contains("Precificador", html); Assert.Contains("<h1>Dashboard</h1>", html);
            foreach (var card in new[] { "Produtos ativos", "Insumos ativos", "Abaixo da margem", "Dentro da margem", "Margem indisponível" }) Assert.Contains(card, html);
            await Logo(client, Png, "image/png");
        }
        Assert.Contains("href=\"/Configuracoes/IdentidadeVisual\"", await Html(admin, "/Dashboard"));
        Assert.DoesNotContain("href=\"/Configuracoes/IdentidadeVisual\"", await Html(operacional, "/Dashboard"));
        foreach (var filtro in new[] { "abaixo-da-margem", "precificacao-incompleta" })
            Assert.Contains("aria-current=\"page\"", await Html(operacional, "/Dashboard?filtro=" + filtro));
        // A consulta leve gera CASE/IS NOT NULL, sem retornar os bytes como coluna.
        var consultas = h.Sql.Comandos.Where(x => x.Contains("IdentidadesVisuaisEmpresas") && x.Contains("SELECT")).ToArray();
        Assert.Contains(consultas, x => x.Contains("CASE") && x.Contains("IS NOT NULL"));
    }

    [Fact]
    public async Task Troca_multiempresa_altera_cor_logo_e_endpoint_ignora_id_externo()
    {
        using var h = new Cenario(); await h.Preparar(); using var admin = await h.Cliente(PerfilUsuarioEmpresa.Administrador);
        await Post(admin, "#112233", Png);
        var usuario = await h.Web.CriarUsuarioAsync(PerfilUsuarioEmpresa.Administrador, h.EmpresaId, h.OutraId);
        using var multi = h.Web.CriarCliente(); await h.Web.LoginAsync(multi, usuario.Email, usuario.Senha);
        h.Sql.Comandos.Clear(); await Html(multi, "/Empresas/Selecionar");
        Assert.DoesNotContain(h.Sql.Comandos, x => x.Contains("IdentidadesVisuaisEmpresas"));
        await Selecionar(multi, h.EmpresaId); Assert.Contains("#112233", await Html(multi, "/Dashboard"));
        await Selecionar(multi, h.OutraId); Assert.Contains("#0D6EFD", await Html(multi, "/Dashboard"));
        Assert.Equal(HttpStatusCode.NotFound, (await multi.GetAsync("/Empresa/Logo?EmpresaId=" + h.EmpresaId)).StatusCode);
        await Post(multi, "#ABCDEF", Jpeg); await Logo(multi, Jpeg, "image/jpeg");
        await Selecionar(multi, h.EmpresaId); Assert.Contains("#112233", await Html(multi, "/Dashboard")); await Logo(multi, Png, "image/png");
    }

    [Fact]
    public async Task Primeira_criacao_concorrente_nao_retorna_500_e_deixa_uma_linha()
    {
        using var h = new Cenario(); await h.Preparar();
        using var a = await h.Cliente(PerfilUsuarioEmpresa.Administrador); using var b = await h.Cliente(PerfilUsuarioEmpresa.Administrador);
        var ta = await WebTestHtml.ObterTokenAntiforgeryAsync(a, Url); var tb = await WebTestHtml.ObterTokenAntiforgeryAsync(b, Url);
        var respostas = await Task.WhenAll(Post(a, "#112233", Png, token: ta), Post(b, "#ABCDEF", Jpeg, token: tb));
        Assert.All(respostas, r => Assert.Equal(HttpStatusCode.Redirect, r.StatusCode));
        var final = (await h.Ler())!; Assert.Contains(final.CorPrimaria, new[] { "#112233", "#ABCDEF" });
        using var scope = h.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(1, await db.IdentidadesVisuaisEmpresas.IgnoreQueryFilters().CountAsync());
    }

    [Fact]
    public async Task Home_Admin_e_erro_neutros_nao_consultam_branding()
    {
        using var h = new Cenario(); await h.Preparar(); using var admin = await h.Cliente(PerfilUsuarioEmpresa.Administrador); await Post(admin, "#112233", Png);
        using var anonimo = h.Web.CriarCliente(); using var global = h.Web.CriarCliente();
        var usuario = await h.Web.CriarSystemAdminAsync(); await h.Web.LoginAsync(global, usuario.Email, usuario.Senha);
        foreach (var (client, url) in new[] { (anonimo, "/"), (anonimo, "/Conta/Login"), (global, "/Admin"), (admin, "/Erro/404") })
        {
            h.Sql.Comandos.Clear(); var html = await Html(client, url);
            Assert.DoesNotContain("--empresa-cor-primaria:", html); Assert.DoesNotContain("src=\"/Empresa/Logo\"", html);
            Assert.DoesNotContain(h.Sql.Comandos, x => x.Contains("IdentidadesVisuaisEmpresas"));
        }
        Assert.Equal(HttpStatusCode.Redirect, (await anonimo.GetAsync("/Empresa/Logo")).StatusCode);
    }

    private static async Task<string> Html(HttpClient client, string url) => WebUtility.HtmlDecode(await (await client.GetAsync(url)).Content.ReadAsStringAsync());
    private static async Task Logo(HttpClient client, byte[] bytes, string tipo)
    {
        var r = await client.GetAsync("/Empresa/Logo"); Assert.Equal(HttpStatusCode.OK, r.StatusCode);
        Assert.Equal(bytes, await r.Content.ReadAsByteArrayAsync()); Assert.Equal(tipo, r.Content.Headers.ContentType!.MediaType);
        Assert.Equal("nosniff", r.Headers.GetValues("X-Content-Type-Options").Single()); Assert.True(r.Headers.CacheControl!.NoStore);
    }
    private static async Task Selecionar(HttpClient client, int id)
    {
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Empresas/Selecionar");
        Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Empresas/Selecionar", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = token, ["EmpresaId"] = id.ToString() }))).StatusCode);
    }
    private static async Task<HttpResponseMessage> Post(HttpClient client, string cor, byte[]? logo = null, bool remover = false, string nome = "logo.png", string tipo = "image/png", int? empresaManipulada = null, string? token = null)
    {
        token ??= await WebTestHtml.ObterTokenAntiforgeryAsync(client, Url);
        using var form = new MultipartFormDataContent();
        form.Add(new StringContent(token), "__RequestVerificationToken"); form.Add(new StringContent(cor), "Input.CorPrimaria");
        form.Add(new StringContent(remover.ToString()), "Input.RemoverLogo");
        if (empresaManipulada.HasValue) form.Add(new StringContent(empresaManipulada.Value.ToString()), "Input.EmpresaId");
        if (logo is not null)
        { var arquivo = new ByteArrayContent(logo); arquivo.Headers.ContentType = new(tipo); form.Add(arquivo, "Input.Logo", nome); }
        return await client.PostAsync(Url, form);
    }
    private sealed class Cenario : IDisposable
    {
        public readonly RegistroSql Sql = new();
        public WebApplicationFactory<Program> Factory { get; }
        public WebTestContext Web { get; }
        public int EmpresaId { get; private set; } public int OutraId { get; private set; }
        public Cenario()
        {
            Factory = new CustomWebApplicationFactory().WithWebHostBuilder(b => b.ConfigureServices(s =>
            {
                var cs = SqlServerTestDatabase.CriarConnectionStringAsync("IdentidadeWeb").GetAwaiter().GetResult();
                s.RemoveAll<DbContextOptions<PrecificadorDbContext>>();
                s.AddDbContext<PrecificadorDbContext>(o => o.UseSqlServer(cs).AddInterceptors(Sql));
            }));
            Web = new(Factory);
        }
        public async Task Preparar() { EmpresaId = await Web.CriarEmpresaAsync(); OutraId = await Web.CriarEmpresaAsync(); }
        public async Task<HttpClient> Cliente(PerfilUsuarioEmpresa perfil)
        { var u = await Web.CriarUsuarioAsync(perfil, EmpresaId); var c = Web.CriarCliente(); await Web.LoginAsync(c, u.Email, u.Senha); return c; }
        public async Task<IdentidadeVisualEmpresa?> Ler(int? id = null)
        {
            using var scope = Factory.Services.CreateScope(); var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            await using var db = new PrecificadorDbContext(options, new ContextoEmpresaTeste(id ?? EmpresaId));
            return await db.IdentidadesVisuaisEmpresas.SingleOrDefaultAsync();
        }
        public void Dispose() => Factory.Dispose();
    }
    private sealed class RegistroSql : DbCommandInterceptor
    {
        public ConcurrentQueue<string> Comandos { get; } = new();
        public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result, CancellationToken cancellationToken = default)
        { Comandos.Enqueue(command.CommandText); return ValueTask.FromResult(result); }
    }
}
