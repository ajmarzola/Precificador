using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class FichaTecnicaPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CA01_Ficha_tecnica_exige_autenticacao_e_empresa_ativa()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto protegido"), null, 0.30m, ativo: true);
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await anonimo.GetAsync($"/Produtos/FichaTecnica/{id}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Login", response.Headers.Location!.ToString());

        using var autenticadoSemEmpresaAtiva = await CriarClienteAutenticadoSemEmpresaAtivaAsync();
        var acessoSemEmpresa = await autenticadoSemEmpresaAtiva.GetAsync($"/Produtos/FichaTecnica/{id}");
        Assert.Equal(HttpStatusCode.Redirect, acessoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoSemEmpresa.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CA02_CA20_Get_sem_ficha_exibe_formulario_sem_criar_registro()
    {
        var nome = NomeUnico("Produto sem ficha");
        var id = await CriarProdutoAsync(1, nome, "Catálogo", 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync($"/Produtos/FichaTecnica/{id}");
        var conteudo = await LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Contains(nome, conteudo);
        Assert.Contains("Catálogo", conteudo);
        Assert.Contains("Ativo", conteudo);
        Assert.Contains("Rendimento do lote (unidades de venda)", conteudo);
        Assert.Contains("Tempo ativo de trabalho (minutos)", conteudo);
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("ProdutoId", conteudo);
        Assert.DoesNotContain("TempoForno", conteudo);
        Assert.DoesNotContain("PotenciaForno", conteudo);
        Assert.Empty(await ListarFichasAsync(produtoId: id));
    }

    [Fact]
    public async Task CA03_CA04_CA18_Post_valido_cria_ficha_e_faz_PRG_com_sucesso()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto cria ficha"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, "2,5", "45");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{id}", response.Headers.Location!.ToString());
        var ficha = Assert.Single(await ListarFichasAsync(produtoId: id));
        Assert.Equal(1, ficha.EmpresaId);
        Assert.Equal(id, ficha.ProdutoId);
        Assert.Equal(2.5m, ficha.Rendimento);
        Assert.Equal(45, ficha.TempoAtivoMinutos);

        var paginaAposRedirect = await LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location!));
        Assert.Contains("Ficha técnica salva com sucesso.", paginaAposRedirect);
    }

    [Fact]
    public async Task CA07_Get_com_ficha_carrega_valores_atuais()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto ficha existente"), null, 0.30m, ativo: true);
        await CriarFichaAsync(1, id, 3.5m, 75);
        using var client = await CriarClienteAutenticadoAsync(1);

        var conteudo = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{id}"));

        Assert.Equal(3.5m, DecimalInformado(ValorDoInput(conteudo, "Input.Rendimento")));
        Assert.Equal("75", ValorDoInput(conteudo, "Input.TempoAtivoMinutos"));
    }

    [Fact]
    public async Task CA08_Post_em_ficha_existente_atualiza_mesmo_registro()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto atualiza ficha"), null, 0.30m, ativo: true);
        var fichaId = await CriarFichaAsync(1, id, 3m, 60);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, "4,25", "90");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var ficha = Assert.Single(await ListarFichasAsync(produtoId: id));
        Assert.Equal(fichaId, ficha.Id);
        Assert.Equal(1, ficha.EmpresaId);
        Assert.Equal(id, ficha.ProdutoId);
        Assert.Equal(4.25m, ficha.Rendimento);
        Assert.Equal(90, ficha.TempoAtivoMinutos);
    }

    [Theory]
    [InlineData(null, "45", "O rendimento é obrigatório.")]
    [InlineData("0", "45", "O rendimento deve ser maior que zero.")]
    [InlineData("-1", "45", "O rendimento deve ser maior que zero.")]
    [InlineData("2", null, "O tempo ativo é obrigatório.")]
    [InlineData("2", "-1", "O tempo ativo não pode ser negativo.")]
    public async Task CA05_CA06_Post_invalido_nao_cria_nem_altera_ficha(
        string? rendimento,
        string? tempoAtivo,
        string mensagem)
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto ficha invalida"), null, 0.30m, ativo: true);
        var fichaId = await CriarFichaAsync(1, id, 2m, 30);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, rendimento, tempoAtivo);
        var conteudo = await LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(mensagem, conteudo);
        var ficha = Assert.Single(await ListarFichasAsync(produtoId: id));
        Assert.Equal(fichaId, ficha.Id);
        Assert.Equal(2m, ficha.Rendimento);
        Assert.Equal(30, ficha.TempoAtivoMinutos);
    }

    [Fact]
    public async Task CA13_Get_e_post_de_produto_inexistente_retornam_404()
    {
        var idExistente = await CriarProdutoAsync(1, NomeUnico("Produto token ficha"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync("/Produtos/FichaTecnica/999999");
        var post = await EnviarFormularioAsync(client, 999999, "2", "30", tokenProdutoId: idExistente);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task CA14_Get_e_post_cross_tenant_retornam_404_sem_alteracao()
    {
        var empresaDois = await CriarEmpresaAsync();
        var idOutroTenant = await CriarProdutoAsync(empresaDois, NomeUnico("Produto ficha cross tenant"), null, 0.30m, ativo: true);
        var fichaId = await CriarFichaAsync(empresaDois, idOutroTenant, 2m, 30);
        var idEmpresaUm = await CriarProdutoAsync(1, NomeUnico("Produto token empresa um"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync($"/Produtos/FichaTecnica/{idOutroTenant}");
        var post = await EnviarFormularioAsync(client, idOutroTenant, "9", "99", tokenProdutoId: idEmpresaUm);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        var ficha = Assert.Single(await ListarFichasAsync(empresaDois, idOutroTenant));
        Assert.Equal(fichaId, ficha.Id);
        Assert.Equal(2m, ficha.Rendimento);
        Assert.Equal(30, ficha.TempoAtivoMinutos);
    }

    [Fact]
    public async Task CA15_Request_nao_controla_empresa_produto_ou_id_da_ficha()
    {
        var empresaDois = await CriarEmpresaAsync();
        var idProdutoEmpresaDois = await CriarProdutoAsync(empresaDois, NomeUnico("Produto manipulado outro tenant"), null, 0.30m, ativo: true);
        var id = await CriarProdutoAsync(1, NomeUnico("Produto request manipulado"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, "2", "30", new Dictionary<string, string>
        {
            ["Id"] = "999",
            ["EmpresaId"] = empresaDois.ToString(CultureInfo.InvariantCulture),
            ["ProdutoId"] = idProdutoEmpresaDois.ToString(CultureInfo.InvariantCulture),
            ["Input.Id"] = "999",
            ["Input.EmpresaId"] = empresaDois.ToString(CultureInfo.InvariantCulture),
            ["Input.ProdutoId"] = idProdutoEmpresaDois.ToString(CultureInfo.InvariantCulture)
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var ficha = Assert.Single(await ListarFichasAsync(produtoId: id));
        Assert.Equal(1, ficha.EmpresaId);
        Assert.Equal(id, ficha.ProdutoId);
    }

    [Fact]
    public async Task CA16_Produto_inativo_pode_salvar_ficha_sem_reativacao()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto inativo ficha"), null, 0.30m, ativo: false);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, "2", "0");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var ficha = Assert.Single(await ListarFichasAsync(produtoId: id));
        Assert.Equal(0, ficha.TempoAtivoMinutos);
        Assert.False((await ObterProdutoAsync(id, 1)).Ativo);
    }

    [Fact]
    public async Task CA17_Detalhes_exibe_ficha_tecnica_para_ativo_e_inativo()
    {
        var idAtivo = await CriarProdutoAsync(1, NomeUnico("Produto ativo nav ficha"), null, 0.30m, ativo: true);
        var idInativo = await CriarProdutoAsync(1, NomeUnico("Produto inativo nav ficha"), null, 0.30m, ativo: false);
        using var client = await CriarClienteAutenticadoAsync(1);

        var ativo = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{idAtivo}"));
        var inativo = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{idInativo}"));
        var ficha = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{idAtivo}"));

        Assert.Contains("Ficha técnica", ativo);
        Assert.Contains($"href=\"/Produtos/FichaTecnica/{idAtivo}\"", ativo);
        Assert.Contains("Ficha técnica", inativo);
        Assert.Contains($"href=\"/Produtos/FichaTecnica/{idInativo}\"", inativo);
        Assert.Contains("Voltar para o produto", ficha);
        Assert.Contains($"href=\"/Produtos/Detalhes/{idAtivo}\"", ficha);
    }

    [Fact]
    public async Task CA19_Post_sem_antiforgery_e_rejeitado_sem_alterar_ficha()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto ficha antiforgery"), null, 0.30m, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await client.PostAsync($"/Produtos/FichaTecnica/{id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Rendimento"] = "2",
            ["Input.TempoAtivoMinutos"] = "30"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListarFichasAsync(produtoId: id));
    }

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient client,
        int id,
        string? rendimento,
        string? tempoAtivo,
        Dictionary<string, string>? camposExtras = null,
        int? tokenProdutoId = null)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/FichaTecnica/{tokenProdutoId ?? id}");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(pagina)
        };

        if (rendimento is not null)
        {
            dados["Input.Rendimento"] = rendimento;
        }

        if (tempoAtivo is not null)
        {
            dados["Input.TempoAtivoMinutos"] = tempoAtivo;
        }

        if (camposExtras is not null)
        {
            foreach (var campo in camposExtras)
            {
                dados[campo.Key] = campo.Value;
            }
        }

        return await client.PostAsync($"/Produtos/FichaTecnica/{id}", new FormUrlEncodedContent(dados));
    }

    private async Task<int> CriarProdutoAsync(int empresaId, string nome, string? categoria, decimal margemAlvo, bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = Produto.Criar(empresaId, nome, margemAlvo, categoria);
        if (!ativo)
        {
            produto.Desativar();
        }

        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
    }

    private async Task<int> CriarFichaAsync(int empresaId, int produtoId, decimal rendimento, int tempoAtivoMinutos)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var ficha = FichaTecnica.Criar(empresaId, produtoId, rendimento, tempoAtivoMinutos);
        context.FichasTecnicas.Add(ficha);
        await context.SaveChangesAsync();
        return ficha.Id;
    }

    private async Task<List<FichaTecnica>> ListarFichasAsync(int? empresaId = null, int? produtoId = null)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var query = context.FichasTecnicas.IgnoreQueryFilters().AsNoTracking();
        if (empresaId.HasValue)
        {
            query = query.Where(ficha => ficha.EmpresaId == empresaId.Value);
        }

        if (produtoId.HasValue)
        {
            query = query.Where(ficha => ficha.ProdutoId == produtoId.Value);
        }

        return await query.ToListAsync();
    }

    private async Task<Produto> ObterProdutoAsync(int id, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.Produtos.AsNoTracking().SingleAsync(produto => produto.Id == id);
    }

    private async Task<int> CriarEmpresaAsync()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var empresa = Empresa.Criar($"Empresa {Guid.NewGuid():N}");
        context.Empresas.Add(empresa);
        await context.SaveChangesAsync();
        return empresa.Id;
    }

    private async Task<HttpClient> CriarClienteAutenticadoAsync(int empresaId)
    {
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var usuario = new UsuarioAplicacao { UserName = email, Email = email };
            Assert.True((await users.CreateAsync(usuario, senha)).Succeeded);
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresaId, Ativo = true });
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var login = await client.GetAsync("/Conta/Login");
        var resposta = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(await login.Content.ReadAsStringAsync()),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        return client;
    }

    private async Task<HttpClient> CriarClienteAutenticadoSemEmpresaAtivaAsync()
    {
        var empresaDois = await CriarEmpresaAsync();
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";
        using (var scope = factory.Services.CreateScope())
        {
            var users = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var usuario = new UsuarioAplicacao { UserName = email, Email = email };
            Assert.True((await users.CreateAsync(usuario, senha)).Succeeded);
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = 1, Ativo = true });
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresaDois, Ativo = true });
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var login = await client.GetAsync("/Conta/Login");
        var resposta = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(await login.Content.ReadAsStringAsync()),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Empresas/Selecionar", resposta.Headers.Location!.ToString());
        return client;
    }

    private static async Task<string> LerHtmlDecodificadoAsync(HttpResponseMessage response)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return WebUtility.HtmlDecode(Encoding.UTF8.GetString(bytes));
    }

    private static string ValorDoInput(string conteudo, string nome)
    {
        var input = Regex.Match(conteudo, $"<input[^>]*name=\"{Regex.Escape(nome)}\"[^>]*>").Value;
        Assert.False(string.IsNullOrEmpty(input), conteudo);
        return Regex.Match(input, "value=\"([^\"]*)\"").Groups[1].Value;
    }

    private static decimal DecimalInformado(string valor) =>
        decimal.Parse(valor.Replace(',', '.'), CultureInfo.InvariantCulture);

    private static string NomeUnico(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";

    private static string Token(string pagina) =>
        WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

    private sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
