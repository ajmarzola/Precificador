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
        using var client = await CriarClienteAutenticadoAsync();

        var response = await client.GetAsync("/Produtos/Novo");
        var conteudo = await LerHtmlDecodificadoAsync(response);

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
    public async Task CA03_CA16_Post_valido_persiste_produto_da_empresa_ativa_e_faz_PRG()
    {
        using var client = await CriarClienteAutenticadoAsync();
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
        var conteudo = await LerHtmlDecodificadoAsync(paginaAposRedirect);
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
        using var client = await CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarProdutosAsync();

        var response = await EnviarFormularioAsync(client, nome, categoria, margemPercentual);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(quantidadeAntes, await ContarProdutosAsync());
    }

    [Fact]
    public async Task CA08_Duplicidade_na_mesma_empresa_exibe_mensagem_e_nao_cria_segundo_produto()
    {
        using var client = await CriarClienteAutenticadoAsync();
        var nome = $"Pão de Açúcar {Guid.NewGuid():N}";
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarFormularioAsync(client, nome, null, "30")).StatusCode);

        var response = await EnviarFormularioAsync(client, $"  {nome.ToUpperInvariant()}  ", "Outra", "25");
        var conteudo = await LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Já existe um produto cadastrado com esse nome.", conteudo);
        Assert.Equal(1, await ContarProdutosAsync(nome));
    }

    [Fact]
    public async Task CA09_Mesmo_nome_em_outra_empresa_nao_bloqueia_cadastro()
    {
        var empresaDois = await CriarEmpresaAsync();
        var nome = $"Agenda {Guid.NewGuid():N}";
        using var clienteEmpresaDois = await CriarClienteAutenticadoAsync(empresaDois);
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarFormularioAsync(clienteEmpresaDois, nome, null, "30")).StatusCode);
        using var clienteEmpresaUm = await CriarClienteAutenticadoAsync();

        var response = await EnviarFormularioAsync(clienteEmpresaUm, $"  {nome.ToUpperInvariant()}  ", null, "25");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(2, await ContarProdutosNormalizadosAsync(nome.ToUpperInvariant()));
    }

    [Fact]
    public async Task CA11_Request_nao_controla_empresa_ou_status_inicial()
    {
        var empresaDois = await CriarEmpresaAsync();
        using var client = await CriarClienteAutenticadoAsync();
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
        var conteudo = await LerHtmlDecodificadoAsync(response);

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
        var token = Token(pagina);
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

    private async Task<HttpClient> CriarClienteAutenticadoAsync(int empresaId = 1)
    {
        var email = $"usuario-{Guid.NewGuid():N}@teste.local";
        const string senha = "SenhaTeste1";
        using (var scope = factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var usuario = new UsuarioAplicacao { UserName = email, Email = email };
            Assert.True((await userManager.CreateAsync(usuario, senha)).Succeeded);
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresaId, Ativo = true });
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var pagina = await (await client.GetAsync("/Conta/Login")).Content.ReadAsStringAsync();
        var resposta = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(pagina),
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
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var usuario = new UsuarioAplicacao { UserName = email, Email = email };
            Assert.True((await userManager.CreateAsync(usuario, senha)).Succeeded);
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = 1, Ativo = true });
            context.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = usuario.Id, EmpresaId = empresaDois, Ativo = true });
            await context.SaveChangesAsync();
        }

        var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false, HandleCookies = true });
        var pagina = await (await client.GetAsync("/Conta/Login")).Content.ReadAsStringAsync();
        var resposta = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(pagina),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Empresas/Selecionar", resposta.Headers.Location!.ToString());
        return client;
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

    private static async Task<string> LerHtmlDecodificadoAsync(HttpResponseMessage response)
    {
        var bytes = await response.Content.ReadAsByteArrayAsync();
        return WebUtility.HtmlDecode(Encoding.UTF8.GetString(bytes));
    }

    private static string Token(string pagina) =>
        WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);
}
