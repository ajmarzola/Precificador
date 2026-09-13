using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class EditarProdutoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task CA01_Edicao_de_produto_exige_autenticacao_e_empresa_ativa()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto protegido"), null, 0.30m);
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await anonimo.GetAsync($"/Produtos/Editar/{id}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Login", response.Headers.Location!.ToString());

        using var autenticadoSemEmpresaAtiva = await CriarClienteAutenticadoSemEmpresaAtivaAsync();
        var acessoSemEmpresa = await autenticadoSemEmpresaAtiva.GetAsync($"/Produtos/Editar/{id}");
        Assert.Equal(HttpStatusCode.Redirect, acessoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoSemEmpresa.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CA02_CA03_Get_edicao_carrega_campos_permitidos_e_margem_percentual()
    {
        var nomeProduto = NomeUnico("Produto edicao");
        var id = await CriarProdutoAsync(1, nomeProduto, "Catálogo", 0.255m);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync($"/Produtos/Editar/{id}");
        var conteudo = await LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Contains("Nome", conteudo);
        Assert.Contains($"value=\"{nomeProduto}\"", conteudo);
        Assert.Contains("Categoria", conteudo);
        Assert.Contains("value=\"Catálogo\"", conteudo);
        Assert.Contains("Margem-alvo (%)", conteudo);
        Assert.Equal(25.5m, DecimalInformado(ValorDoInput(conteudo, "Input.MargemAlvoPercentual")));
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("NomeNormalizado", conteudo);
        Assert.DoesNotContain("Ativo", conteudo);
        Assert.DoesNotContain("Preço de venda", conteudo);
        Assert.DoesNotContain("Custo", conteudo);
        Assert.DoesNotContain("Ficha Técnica", conteudo);
    }

    [Fact]
    public async Task CA04_CA15_Post_valido_atualiza_produto_e_faz_PRG_para_detalhes()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto original"), "Planners", 0.30m);
        using var client = await CriarClienteAutenticadoAsync(1);
        var novoNome = NomeUnico("Calendário atualizado");

        var response = await EnviarFormularioAsync(client, id, $"  {novoNome}  ", "  Datas   2027 ", "25,5");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Produtos/Detalhes/{id}", response.Headers.Location!.ToString());
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var produto = await context.Produtos.IgnoreQueryFilters().SingleAsync(produto => produto.Id == id);
            Assert.Equal(1, produto.EmpresaId);
            Assert.Equal(novoNome, produto.Nome);
            Assert.Equal(novoNome.ToUpperInvariant(), produto.NomeNormalizado);
            Assert.Equal("Datas 2027", produto.Categoria);
            Assert.Equal(0.255m, produto.MargemAlvo);
            Assert.True(produto.Ativo);
        }

        var detalhes = await LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location!));
        Assert.Contains("Produto atualizado com sucesso.", detalhes);
        Assert.Contains(novoNome, detalhes);
    }

    [Theory]
    [InlineData("   ", "Categoria", "30")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Categoria", "30")]
    [InlineData("Agenda", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "30")]
    [InlineData("Agenda", "Categoria", null)]
    [InlineData("Agenda", "Categoria", "-1")]
    [InlineData("Agenda", "Categoria", "100")]
    [InlineData("Agenda", "Categoria", "101")]
    public async Task CA05_CA06_CA07_Post_invalido_nao_persiste_alteracoes(
        string nome,
        string categoria,
        string? margemPercentual)
    {
        var nomeOriginal = NomeUnico("Produto preservado");
        var id = await CriarProdutoAsync(1, nomeOriginal, "Original", 0.30m);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, nome, categoria, margemPercentual);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        await AssertProdutoAsync(id, nomeOriginal, "Original", 0.30m, 1, true);
    }

    [Fact]
    public async Task CA09_Salvar_sem_mudar_nome_normalizado_nao_detecta_o_proprio_produto_como_duplicado()
    {
        var nomeProduto = NomeUnico("Produto proprio nome");
        var id = await CriarProdutoAsync(1, nomeProduto, "Original", 0.30m);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, $"  {nomeProduto.ToUpperInvariant()}  ", "Atualizada", "20");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await AssertProdutoAsync(id, nomeProduto.ToUpperInvariant(), "Atualizada", 0.20m, 1, true);
        Assert.Equal(1, await ContarProdutosNormalizadosAsync(nomeProduto.ToUpperInvariant()));
    }

    [Fact]
    public async Task CA10_Renomear_para_outro_produto_da_mesma_empresa_exibe_duplicidade_e_nao_persiste()
    {
        var nomeOriginal = NomeUnico("Produto origem");
        var nomeExistente = NomeUnico("Produto existente");
        var id = await CriarProdutoAsync(1, nomeOriginal, "Original", 0.30m);
        await CriarProdutoAsync(1, nomeExistente, "Outra", 0.25m);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, $"  {nomeExistente.ToUpperInvariant()}  ", "Alterada", "20");
        var conteudo = await LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Já existe um produto cadastrado com esse nome.", conteudo);
        await AssertProdutoAsync(id, nomeOriginal, "Original", 0.30m, 1, true);
    }

    [Fact]
    public async Task CA11_Nome_existente_apenas_em_outra_empresa_nao_bloqueia_edicao()
    {
        var empresaDois = await CriarEmpresaAsync();
        var nomeOutroTenant = NomeUnico("Produto outro tenant");
        await CriarProdutoAsync(empresaDois, nomeOutroTenant, null, 0.30m);
        var id = await CriarProdutoAsync(1, NomeUnico("Produto empresa um"), null, 0.25m);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, id, $"  {nomeOutroTenant.ToUpperInvariant()}  ", null, "35");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await AssertProdutoAsync(id, nomeOutroTenant.ToUpperInvariant(), null, 0.35m, 1, true);
        Assert.Equal(2, await ContarProdutosNormalizadosAsync(nomeOutroTenant.ToUpperInvariant()));
    }

    [Fact]
    public async Task CA12_Request_nao_controla_empresa_ou_status()
    {
        var empresaDois = await CriarEmpresaAsync();
        var id = await CriarProdutoAsync(1, NomeUnico("Produto manipulado"), null, 0.30m);
        using var client = await CriarClienteAutenticadoAsync(1);
        var novoNome = NomeUnico("Produto atualizado manipulado");

        var response = await EnviarFormularioAsync(client, id, novoNome, null, "40", new Dictionary<string, string>
        {
            ["EmpresaId"] = empresaDois.ToString(CultureInfo.InvariantCulture),
            ["Input.EmpresaId"] = empresaDois.ToString(CultureInfo.InvariantCulture),
            ["Ativo"] = "false",
            ["Input.Ativo"] = "false"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        await AssertProdutoAsync(id, novoNome, null, 0.40m, 1, true);
    }

    [Fact]
    public async Task CA13_Get_e_post_de_id_inexistente_retornam_404()
    {
        var idExistente = await CriarProdutoAsync(1, NomeUnico("Produto token"), null, 0.30m);
        using var client = await CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync("/Produtos/Editar/999999");
        var post = await EnviarFormularioAsync(client, 999999, "Produto inexistente", null, "30", tokenProdutoId: idExistente);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
    }

    [Fact]
    public async Task CA14_Get_e_post_cross_tenant_retornam_404_sem_alterar_registro()
    {
        var empresaDois = await CriarEmpresaAsync();
        var nomeOutroTenant = NomeUnico("Produto cross tenant");
        var idOutroTenant = await CriarProdutoAsync(empresaDois, nomeOutroTenant, "Externo", 0.30m);
        var idEmpresaUm = await CriarProdutoAsync(1, NomeUnico("Produto token empresa um"), null, 0.20m);
        using var client = await CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync($"/Produtos/Editar/{idOutroTenant}");
        var post = await EnviarFormularioAsync(
            client,
            idOutroTenant,
            "Tentativa cross tenant",
            "Alterada",
            "40",
            tokenProdutoId: idEmpresaUm);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        await AssertProdutoAsync(idOutroTenant, nomeOutroTenant, "Externo", 0.30m, empresaDois, true);
    }

    [Fact]
    public async Task CA16_Detalhes_exibe_editar_e_edicao_exibe_cancelar_para_o_mesmo_produto()
    {
        var id = await CriarProdutoAsync(1, NomeUnico("Produto navegacao edicao"), null, 0.30m);
        using var client = await CriarClienteAutenticadoAsync(1);

        var detalhes = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{id}"));
        var edicao = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Editar/{id}"));

        Assert.Contains("Editar", detalhes);
        Assert.Contains($"href=\"/Produtos/Editar/{id}\"", detalhes);
        Assert.Contains("Cancelar", edicao);
        Assert.Contains($"href=\"/Produtos/Detalhes/{id}\"", edicao);
        Assert.Contains("Salvar", edicao);
    }

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient client,
        int id,
        string nome,
        string? categoria,
        string? margemPercentual,
        Dictionary<string, string>? camposExtras = null,
        int? tokenProdutoId = null)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/Editar/{tokenProdutoId ?? id}");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(pagina),
            ["Input.Nome"] = nome,
            ["Input.Categoria"] = categoria ?? string.Empty
        };

        if (margemPercentual is not null)
        {
            dados["Input.MargemAlvoPercentual"] = margemPercentual;
        }

        if (camposExtras is not null)
        {
            foreach (var campo in camposExtras)
            {
                dados[campo.Key] = campo.Value;
            }
        }

        return await client.PostAsync($"/Produtos/Editar/{id}", new FormUrlEncodedContent(dados));
    }

    private async Task<int> CriarProdutoAsync(int empresaId, string nome, string? categoria, decimal margemAlvo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = Produto.Criar(empresaId, nome, margemAlvo, categoria);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
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

    private async Task AssertProdutoAsync(
        int id,
        string nome,
        string? categoria,
        decimal margemAlvo,
        int empresaId,
        bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var produto = await context.Produtos.IgnoreQueryFilters().AsNoTracking().SingleAsync(produto => produto.Id == id);
        Assert.Equal(nome, produto.Nome);
        Assert.Equal(nome.ToUpperInvariant(), produto.NomeNormalizado);
        Assert.Equal(categoria, produto.Categoria);
        Assert.Equal(margemAlvo, produto.MargemAlvo);
        Assert.Equal(empresaId, produto.EmpresaId);
        Assert.Equal(ativo, produto.Ativo);
    }

    private async Task<int> ContarProdutosNormalizadosAsync(string nomeNormalizado)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        return await context.Produtos.IgnoreQueryFilters().CountAsync(produto => produto.NomeNormalizado == nomeNormalizado);
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
