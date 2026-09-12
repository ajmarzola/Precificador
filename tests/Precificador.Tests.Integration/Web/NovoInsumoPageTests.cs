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
    [Fact]
    public async Task Get_novo_insumo_retorna_sucesso_e_exibe_campos()
    {
        using var client = await CriarClienteAutenticadoAsync();

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
        using var client = await CriarClienteAutenticadoAsync();
        var nome = $"Farinha {Guid.NewGuid():N}";

        var response = await EnviarFormularioAsync(client, nome, "MateriaPrima", "Grama");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using (var scope = factory.Services.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            Assert.True(await context.Insumos.IgnoreQueryFilters().AnyAsync(insumo => insumo.Nome == nome));
        }

        var paginaAposRedirect = await client.GetAsync(response.Headers.Location!);
        var conteudo = await paginaAposRedirect.Content.ReadAsStringAsync();
        Assert.Contains("Insumo cadastrado com sucesso.", conteudo);
    }

    [Fact]
    public async Task Post_valido_com_marca_e_observacao_persiste_na_empresa_ativa()
    {
        using var client = await CriarClienteAutenticadoAsync();
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
        using var client = await CriarClienteAutenticadoAsync();
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
        using var client = await CriarClienteAutenticadoAsync();
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
        using var client = await CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarInsumosAsync();

        var response = await EnviarFormularioAsync(client, "   ", "MateriaPrima", "Grama");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task MEL005_Novo_com_categoria_invalida_exibe_erro_e_nao_persiste()
    {
        using var client = await CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarInsumosAsync();

        var response = await EnviarFormularioAsync(client, $"Categoria invalida {Guid.NewGuid():N}", "0", "Grama");
        var conteudo = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("A categoria é obrigatória.", conteudo);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task MEL005_Novo_com_unidade_invalida_exibe_erro_e_nao_persiste()
    {
        using var client = await CriarClienteAutenticadoAsync();
        var quantidadeAntes = await ContarInsumosAsync();

        var response = await EnviarFormularioAsync(client, $"Unidade invalida {Guid.NewGuid():N}", "MateriaPrima", "0");
        var conteudo = WebUtility.HtmlDecode(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("A unidade base é obrigatória.", conteudo);
        Assert.Equal(quantidadeAntes, await ContarInsumosAsync());
    }

    [Fact]
    public async Task Post_com_nome_duplicado_nao_persiste_e_exibe_mensagem_funcional()
    {
        var nome = $"Açúcar {Guid.NewGuid():N}";
        using var client = await CriarClienteAutenticadoAsync();
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
        using var client = await CriarClienteAutenticadoAsync();
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
        var empresaDois = await CriarEmpresaAsync();
        var nome = $"Farinha {Guid.NewGuid():N}";
        using var clienteEmpresaUm = await CriarClienteAutenticadoAsync();
        using var clienteEmpresaDois = await CriarClienteAutenticadoAsync(empresaDois);

        var cadastroEmpresaUm = await EnviarFormularioAsync(clienteEmpresaUm, nome, "MateriaPrima", "Grama", "Renata");
        var cadastroEmpresaDois = await EnviarFormularioAsync(clienteEmpresaDois, nome, "MateriaPrima", "Grama", "Renata");

        Assert.Equal(HttpStatusCode.Redirect, cadastroEmpresaUm.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, cadastroEmpresaDois.StatusCode);
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var contextoEmpresaUm = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
        await using var contextoEmpresaDois = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaDois));
        Assert.Single(await contextoEmpresaUm.Insumos.Where(insumo => insumo.Nome == nome).ToListAsync());
        Assert.Single(await contextoEmpresaDois.Insumos.Where(insumo => insumo.Nome == nome).ToListAsync());
    }

    [Fact]
    public async Task Get_novo_insumo_exibe_materia_prima_e_metro_sem_ingrediente()
    {
        using var client = await CriarClienteAutenticadoAsync();

        var conteudo = WebUtility.HtmlDecode(await (await client.GetAsync("/Insumos/Novo")).Content.ReadAsStringAsync());

        Assert.Contains("Matéria-prima", conteudo);
        Assert.Contains(">m</option>", conteudo);
        Assert.DoesNotContain(">Ingrediente</option>", conteudo);
    }

    [Fact]
    public async Task Post_valido_com_metro_persiste_insumo()
    {
        using var client = await CriarClienteAutenticadoAsync();
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
        var token = Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
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
        var login = await client.GetAsync("/Conta/Login");
        var pagina = await login.Content.ReadAsStringAsync();
        var token = Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value;
        var resposta = await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebUtility.HtmlDecode(token),
            ["Input.Email"] = email,
            ["Input.Senha"] = senha
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
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

    private sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
    }
}
