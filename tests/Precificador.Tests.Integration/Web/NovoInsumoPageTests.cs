using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;
using Precificador.Infrastructure.Autenticacao;
using Microsoft.AspNetCore.Identity;

namespace Precificador.Tests.Integration.Web;

public sealed class NovoInsumoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);
    [Fact]
    public async Task Get_novo_insumo_retorna_sucesso_e_exibe_campos()
    {
        using var client = await web.CriarClienteAutenticadoAsync();

        var response = await client.GetAsync("/Insumos/Novo");
        var conteudo = await response.Content.ReadAsStringAsync();
        var conteudoDecodificado = WebUtility.HtmlDecode(conteudo);

        response.EnsureSuccessStatusCode();
        Assert.Contains("Nome", conteudoDecodificado);
        Assert.Contains("Marca", conteudoDecodificado);
        Assert.Contains("Categoria", conteudoDecodificado);
        Assert.Contains("Unidade base", conteudoDecodificado);
        Assert.Contains("Observação", conteudoDecodificado);
        Assert.DoesNotContain("EmpresaId", conteudoDecodificado);
    }

    [Fact]
    public async Task Post_valido_persiste_redireciona_e_exibe_mensagem_de_sucesso()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Farinha {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, "MateriaPrima", "Grama");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        int insumoId;
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var insumo = await context.Insumos.IgnoreQueryFilters().SingleAsync(item => item.Nome == nome);
            insumoId = insumo.Id;
        }

        Assert.Equal($"/Insumos/Detalhes/{insumoId}", response.Headers.Location!.ToString());

        var paginaAposRedirect = await client.GetAsync(response.Headers.Location!);
        Assert.Equal(HttpStatusCode.OK, paginaAposRedirect.StatusCode);
        var conteudo = await paginaAposRedirect.Content.ReadAsStringAsync();
        Assert.Contains("Insumo cadastrado com sucesso.", conteudo);
        Assert.Contains(nome, conteudo);
        Assert.Contains("Registrar preço", conteudo);
    }

    [Fact]
    public async Task Post_valido_com_marca_e_observacao_persiste_na_empresa_ativa()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Farinha {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, "MateriaPrima", "Grama", "  Renata   Premium ", "  W 300\nProteína 13,5%  ");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var insumo = await context.Insumos.IgnoreQueryFilters().SingleAsync(item => item.Nome == nome);
        Assert.Equal(1, insumo.EmpresaId);
        Assert.Equal("Renata Premium", insumo.Marca);
        Assert.Equal("RENATA PREMIUM", insumo.MarcaNormalizada);
        Assert.Equal("W 300\nProteína 13,5%", insumo.Observacao);
    }

    [Fact]
    public async Task Post_com_mesmo_nome_e_marcas_distintas_e_aceito_na_empresa_ativa()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Farinha {Guid.NewGuid():N}";

        var renata = await EnviarFormularioAsync(client, nome, "MateriaPrima", "Grama", "Renata");
        var caputo = await EnviarFormularioAsync(client, nome, "MateriaPrima", "Grama", "Caputo");

        Assert.Equal(HttpStatusCode.Redirect, renata.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, caputo.StatusCode);
        Assert.Equal(2, await ContarInsumosAsync(nome));
    }

    [Fact]
    public async Task Post_com_marca_ou_observacao_acima_do_limite_nao_persiste()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarInsumosAsync();

        var marcaInvalida = await EnviarFormularioAsync(client, $"Marca {Guid.NewGuid():N}", "MateriaPrima", "Grama", new string('a', 81));
        var observacaoInvalida = await EnviarFormularioAsync(client, $"Observacao {Guid.NewGuid():N}", "MateriaPrima", "Grama", observacao: new string('a', 1001));

        Assert.Equal(HttpStatusCode.OK, marcaInvalida.StatusCode);
        Assert.Equal(HttpStatusCode.OK, observacaoInvalida.StatusCode);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task Post_invalido_nao_persiste()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarInsumosAsync();

        var response = await EnviarFormularioAsync(client, "   ", "MateriaPrima", "Grama");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task MEL005_Novo_com_categoria_invalida_exibe_erro_e_nao_persiste()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarInsumosAsync();

        var response = await EnviarFormularioAsync(client, $"Categoria invalida {Guid.NewGuid():N}", string.Empty, "Grama");
        var conteudo = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("A categoria é obrigatória.", conteudo);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task MEL005_Novo_com_unidade_invalida_exibe_erro_e_nao_persiste()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarInsumosAsync();

        var response = await EnviarFormularioAsync(client, $"Unidade invalida {Guid.NewGuid():N}", "MateriaPrima", string.Empty);
        var conteudo = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("A unidade base é obrigatória.", conteudo);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task Post_com_nome_duplicado_nao_persiste_e_exibe_mensagem_funcional()
    {
        var nome = $"Açúcar {Guid.NewGuid():N}";
        using var client = await web.CriarClienteAutenticadoAsync();
        var primeiroCadastro = await EnviarFormularioAsync(client, nome, "MateriaPrima", "Grama");
        Assert.Equal(HttpStatusCode.Redirect, primeiroCadastro.StatusCode);
        var response = await EnviarFormularioAsync(client, $"  {nome.ToUpperInvariant()}  ", "MateriaPrima", "Grama");
        var conteudo = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Já existe um insumo cadastrado com esse nome e marca.", WebUtility.HtmlDecode(conteudo));
        Assert.Equal(1, await ContarInsumosAsync(nome));
    }

    [Fact]
    public async Task Post_com_nome_e_marca_duplicados_nao_persiste_e_exibe_mensagem_funcional()
    {
        var nome = $"Farinha {Guid.NewGuid():N}";
        using var client = await web.CriarClienteAutenticadoAsync();
        var primeiroCadastro = await EnviarFormularioAsync(client, nome, "MateriaPrima", "Grama", "Renata");
        Assert.Equal(HttpStatusCode.Redirect, primeiroCadastro.StatusCode);

        var response = await EnviarFormularioAsync(client, $"  {nome.ToUpperInvariant()}  ", "MateriaPrima", "Grama", "  RENATA  ");
        var conteudo = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Já existe um insumo cadastrado com esse nome e marca.", WebUtility.HtmlDecode(conteudo));
        Assert.Equal(1, await ContarInsumosAsync(nome));
    }

    [Fact]
    public async Task Post_com_mesma_combinacao_marcada_em_empresas_diferentes_e_permitido_e_isolado()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var nome = $"Farinha {Guid.NewGuid():N}";
        using var clienteEmpresaUm = await web.CriarClienteAutenticadoAsync();
        using var clienteEmpresaDois = await web.CriarClienteAutenticadoAsync(empresaDois);

        var cadastroEmpresaUm = await EnviarFormularioAsync(clienteEmpresaUm, nome, "MateriaPrima", "Grama", "Renata");
        var cadastroEmpresaDois = await EnviarFormularioAsync(clienteEmpresaDois, nome, "MateriaPrima", "Grama", "Renata");

        Assert.Equal(HttpStatusCode.Redirect, cadastroEmpresaUm.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, cadastroEmpresaDois.StatusCode);
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var contextoEmpresaUm = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
        await using var contextoEmpresaDois = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaDois));
        var insumoEmpresaUm = await contextoEmpresaUm.Insumos.SingleAsync(insumo => insumo.Nome == nome);
        var insumoEmpresaDois = await contextoEmpresaDois.Insumos.SingleAsync(insumo => insumo.Nome == nome);
        Assert.Equal($"/Insumos/Detalhes/{insumoEmpresaUm.Id}", cadastroEmpresaUm.Headers.Location!.ToString());
        Assert.Equal($"/Insumos/Detalhes/{insumoEmpresaDois.Id}", cadastroEmpresaDois.Headers.Location!.ToString());
    }

    [Fact]
    public async Task Get_novo_insumo_exibe_materia_prima_e_metro_sem_ingrediente()
    {
        using var client = await web.CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await (await client.GetAsync("/Insumos/Novo")).Content.ReadAsStringAsync());

        Assert.Contains("Matéria-prima", conteudo);
        Assert.Contains(">m</option>", conteudo);
        Assert.DoesNotContain(">Ingrediente</option>", conteudo);
    }

    [Fact]
    public async Task Post_valido_com_metro_persiste_insumo()
    {
        using var client = await web.CriarClienteAutenticadoAsync();
        var nome = $"Fita {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, "MateriaPrima", "Metro");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var insumo = await context.Insumos.IgnoreQueryFilters().SingleAsync(item => item.Nome == nome);
        Assert.Equal(CategoriaInsumo.MateriaPrima, insumo.Categoria);
        Assert.Equal(UnidadeMedida.Metro, insumo.UnidadeBase);
    }

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(HttpClient client, string nome, string categoria, string unidadeBase, string? marca = null, string? observacao = null)
    {
        var respostaPagina = await client.GetAsync("/Insumos/Novo");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var token = WebTestHtml.ExtrairTokenAntiforgery(pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.Nome"] = nome,
            ["Input.Categoria"] = categoria,
            ["Input.UnidadeBase"] = unidadeBase,
            ["Input.Marca"] = marca ?? string.Empty,
            ["Input.Observacao"] = observacao ?? string.Empty
        };

        return await client.PostAsync("/Insumos/Novo", new FormUrlEncodedContent(dados));
    }

    private async Task<int> ContarInsumosAsync(string? nome = null)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        return nome is null
            ? await context.Insumos.IgnoreQueryFilters().CountAsync()
            : await context.Insumos.IgnoreQueryFilters().CountAsync(insumo => insumo.Nome == nome);
    }
}
