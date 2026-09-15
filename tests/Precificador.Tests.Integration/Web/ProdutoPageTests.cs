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

public sealed class ProdutoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);
    [Fact]
    public async Task CA01_Cadastro_de_produto_exige_autenticacao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.GetAsync("/Produtos/Novo");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Login", response.Headers.Location!.ToString());

        using var autenticadoSemEmpresaAtiva = await CriarClienteAutenticadoSemEmpresaAtivaAsync();
        var acessoSemEmpresa = await autenticadoSemEmpresaAtiva.GetAsync("/Produtos/Novo");
        Assert.Equal(HttpStatusCode.Redirect, acessoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoSemEmpresa.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CA02_Get_exibe_apenas_campos_funcionais_do_cadastro()
    {
        using var client = await web.CriarClienteAutenticadoAsync();

        var response = await client.GetAsync("/Produtos/Novo");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Contains("Nome", conteudo);
        Assert.Contains("Categoria", conteudo);
        Assert.Contains("Margem-alvo (%)", conteudo);
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("NomeNormalizado", conteudo);
        Assert.DoesNotContain("Ativo", conteudo);
        Assert.DoesNotContain("Preço de venda", conteudo);
        Assert.DoesNotContain("Custo", conteudo);
        Assert.DoesNotContain("Ficha Técnica", conteudo);
    }

    [Fact]
    public async Task UC027_W17_MargemPadrao_configurada_preenche_margem_alvo()
    {
        await AtualizarMargemPadraoAsync(1, 0.255m);
        using var client = await web.CriarClienteAutenticadoAsync();

        var response = await client.GetAsync("/Produtos/Novo");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Equal("25,5", ValorDoInput(conteudo, "Input.MargemAlvoPercentual"));
    }

    [Fact]
    public async Task UC027_W17_MargemPadrao_preserva_precisao_ao_preencher_e_salvar_sem_alteracao()
    {
        await AtualizarMargemPadraoAsync(1, 0.123456m);
        using var client = await web.CriarClienteAutenticadoAsync();
        var responseGet = await client.GetAsync("/Produtos/Novo");
        var conteudoGet = await WebTestHtml.LerHtmlDecodificadoAsync(responseGet);
        var margemPreenchida = ValorDoInput(conteudoGet, "Input.MargemAlvoPercentual");
        var nome = $"Produto margem precisa {Guid.NewGuid():N}";

        var responsePost = await EnviarFormularioAsync(client, nome, null, margemPreenchida);

        responseGet.EnsureSuccessStatusCode();
        Assert.Equal("12,3456", margemPreenchida);
        Assert.Equal(HttpStatusCode.Redirect, responsePost.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var produto = await context.Produtos.IgnoreQueryFilters().SingleAsync(produto => produto.Nome == nome);
        Assert.Equal(0.123456m, produto.MargemAlvo);
    }

    [Fact]
    public async Task UC027_W18_MargemPadrao_null_nao_preenche_margem_alvo()
    {
        await AtualizarMargemPadraoAsync(1, null);
        using var client = await web.CriarClienteAutenticadoAsync();

        var response = await client.GetAsync("/Produtos/Novo");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Equal(string.Empty, ValorDoInput(conteudo, "Input.MargemAlvoPercentual"));
    }

    [Fact]
    public async Task UC027_W19_MargemPadrao_zero_preenche_zero()
    {
        await AtualizarMargemPadraoAsync(1, 0m);
        using var client = await web.CriarClienteAutenticadoAsync();

        var response = await client.GetAsync("/Produtos/Novo");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Equal("0", ValorDoInput(conteudo, "Input.MargemAlvoPercentual"));
    }

    [Fact]
    public async Task UC027_ProdutoNovo_sem_configuracao_retorna_404()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa produto sem config");
        await RemoverConfiguracaoAsync(empresa);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await client.GetAsync("/Produtos/Novo");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UC027_W20_Usuario_pode_sobrescrever_margem_preenchida()
    {
        await AtualizarMargemPadraoAsync(1, 0.40m);
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Produto margem sobrescrita {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, null, "25");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var produto = await context.Produtos.IgnoreQueryFilters().SingleAsync(produto => produto.Nome == nome);
        Assert.Equal(0.25m, produto.MargemAlvo);
    }

    [Fact]
    public async Task UC027_W21_Post_invalido_preserva_margem_digitada_sem_reaplicar_padrao()
    {
        await AtualizarMargemPadraoAsync(1, 0.40m);
        using var client = await web.CriarClienteAutenticadoAsync();

        var response = await EnviarFormularioAsync(client, " ", null, "25");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("25", ValorDoInput(conteudo, "Input.MargemAlvoPercentual"));
    }

    [Fact]
    public async Task UC027_W22_Produto_existente_nao_muda_ao_alterar_margem_padrao()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Produto existente margem {Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarFormularioAsync(client, nome, null, "30")).StatusCode);

        await AtualizarMargemPadraoAsync(1, 0.10m);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var produto = await context.Produtos.IgnoreQueryFilters().SingleAsync(produto => produto.Nome == nome);
        Assert.Equal(0.30m, produto.MargemAlvo);
    }

    [Fact]
    public async Task CA03_CA16_Post_valido_persiste_produto_da_empresa_ativa_e_faz_PRG()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Agenda {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, "  Planners   2027  ", "30");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Produtos/Novo", response.Headers.Location!.ToString());
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var produto = await context.Produtos.IgnoreQueryFilters().SingleAsync(produto => produto.Nome == nome);
            Assert.Equal(1, produto.EmpresaId);
            Assert.Equal("Planners 2027", produto.Categoria);
            Assert.Equal(0.30m, produto.MargemAlvo);
            Assert.True(produto.Ativo);
        }

        var paginaAposRedirect = await client.GetAsync(response.Headers.Location!);
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(paginaAposRedirect);
        Assert.Contains("Produto cadastrado com sucesso.", conteudo);
        Assert.DoesNotContain($"value=\"{nome}\"", conteudo);
    }

    [Theory]
    [InlineData("   ", "Categoria", "30")]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "Categoria", "30")]
    [InlineData("Agenda", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "30")]
    [InlineData("Agenda", "Categoria", "-1")]
    [InlineData("Agenda", "Categoria", "100")]
    [InlineData("Agenda", "Categoria", "101")]
    public async Task CA04_CA06_CA07_Post_invalido_nao_persiste_produto(string nome, string categoria, string margemPercentual)
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarProdutosAsync();

        var response = await EnviarFormularioAsync(client, nome, categoria, margemPercentual);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(quantidadeAntes, await ContarProdutosAsync());
    }

    [Fact]
    public async Task CA08_Duplicidade_na_mesma_empresa_exibe_mensagem_e_nao_cria_segundo_produto()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Pão de Açúcar {Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarFormularioAsync(client, nome, null, "30")).StatusCode);

        var response = await EnviarFormularioAsync(client, $"  {nome.ToUpperInvariant()}  ", "Outra", "25");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Já existe um produto cadastrado com esse nome.", conteudo);
        Assert.Equal(1, await ContarProdutosAsync(nome));
    }

    [Fact]
    public async Task CA09_Mesmo_nome_em_outra_empresa_nao_bloqueia_cadastro()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var nome = $"Agenda {Guid.NewGuid():N}";
        using var clienteEmpresaDois = await web.CriarClienteAutenticadoAsync(empresaDois);
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarFormularioAsync(clienteEmpresaDois, nome, null, "30")).StatusCode);
        using var clienteEmpresaUm = await web.CriarClienteAutenticadoAsync();

        var response = await EnviarFormularioAsync(clienteEmpresaUm, $"  {nome.ToUpperInvariant()}  ", null, "25");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(2, await ContarProdutosNormalizadosAsync(nome.ToUpperInvariant()));
    }

    [Fact]
    public async Task CA11_Request_nao_controla_empresa_ou_status_inicial()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Produto manipulado {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, null, "30", new Dictionary<string, string>
        {
            ["EmpresaId"] = empresaDois.ToString(),
            ["Input.EmpresaId"] = empresaDois.ToString(),
            ["Ativo"] = "false",
            ["Input.Ativo"] = "false"
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var produto = await context.Produtos.IgnoreQueryFilters().SingleAsync(produto => produto.Nome == nome);
        Assert.Equal(1, produto.EmpresaId);
        Assert.True(produto.Ativo);
    }

    [Fact]
    public async Task CA17_Home_exibe_link_cadastrar_produto()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Contains("Cadastrar produto", conteudo);
        Assert.Contains("href=\"/Produtos/Novo\"", conteudo);
    }

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient client,
        string nome,
        string? categoria,
        string margemPercentual,
        Dictionary<string, string>? camposExtras = null)
    {
        var respostaPagina = await client.GetAsync("/Produtos/Novo");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var token = WebTestHtml.ExtrairTokenAntiforgery(pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Nome"] = nome,
            ["Input.Categoria"] = categoria ?? string.Empty,
            ["Input.MargemAlvoPercentual"] = margemPercentual
        };

        if (camposExtras is not null)
        {
            foreach (var campo in camposExtras)
            {
                dados[campo.Key] = campo.Value;
            }
        }

        return await client.PostAsync("/Produtos/Novo", new FormUrlEncodedContent(dados));
    }

    private async Task<int> ContarProdutosAsync(string? nome = null)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        return nome is null
            ? await context.Produtos.IgnoreQueryFilters().CountAsync()
            : await context.Produtos.IgnoreQueryFilters().CountAsync(produto => produto.Nome == nome);
    }

    private async Task<int> ContarProdutosNormalizadosAsync(string nomeNormalizado)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        return await context.Produtos.IgnoreQueryFilters().CountAsync(produto => produto.NomeNormalizado == nomeNormalizado);
    }

    private async Task AtualizarMargemPadraoAsync(int empresaId, decimal? margemPadrao)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ConfiguracoesPrecificacaoEmpresas
            SET MargemPadrao = {margemPadrao}
            WHERE EmpresaId = {empresaId}
            """);
    }

    private async Task RemoverConfiguracaoAsync(int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ConfiguracoesPrecificacaoEmpresas
            WHERE EmpresaId = {empresaId}
            """);
    }

    private static string ValorDoInput(string html, string nome)
    {
        var pattern = "<input[^>]+name=\"" + Regex.Escape(nome) + "\"[^>]*value=\"([^\"]*)\"";
        var match = Regex.Match(html, pattern);
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    private async Task<HttpClient> CriarClienteAutenticadoSemEmpresaAtivaAsync()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var usuario = await web.CriarUsuarioAsync(1, empresaDois);
        var client = web.CriarCliente();
        var resposta = await web.LoginAsync(client, usuario.Email, usuario.Senha);
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Empresas/Selecionar", resposta.Headers.Location!.ToString());
        return client;
    }
}
