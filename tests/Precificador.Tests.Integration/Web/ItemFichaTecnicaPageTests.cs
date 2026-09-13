using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Pages.Produtos.FichaTecnica.Itens;

namespace Precificador.Tests.Integration.Web;

public sealed class ItemFichaTecnicaPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task W1_Adicionar_insumo_exige_autenticacao_e_empresa_ativa()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto protegido"), ativo: true);
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await anonimo.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Login", response.Headers.Location!.ToString());

        using var autenticadoSemEmpresaAtiva = await CriarClienteAutenticadoSemEmpresaAtivaAsync();
        var acessoSemEmpresa = await autenticadoSemEmpresaAtiva.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo");
        Assert.Equal(HttpStatusCode.Redirect, acessoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoSemEmpresa.Headers.Location!.ToString());
    }

    [Fact]
    public async Task W2_Produto_sem_ficha_redireciona_sem_criar_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto sem ficha"), ativo: true);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo sem ficha"), "Marca", UnidadeMedida.Grama, ativo: true);
        var produtoComFicha = await CriarProdutoAsync(1, Nome("Produto token item"), ativo: true);
        await CriarFichaAsync(1, produtoComFicha);
        using var client = await CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo");
        var post = await EnviarFormularioAsync(client, produtoId, insumoId, "1", null, tokenProdutoId: produtoComFicha);

        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{produtoId}", get.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{produtoId}", post.Headers.Location!.ToString());
        Assert.Empty(await ListarItensAsync(produtoId: produtoId));

        var ficha = await LerHtmlDecodificadoAsync(await client.GetAsync(get.Headers.Location));
        Assert.Contains("Defina a base da ficha técnica antes de adicionar insumos.", ficha);
    }

    [Fact]
    public async Task W3_Get_lista_apenas_insumos_ativos_do_tenant_e_ficha_mostra_acao_quando_existe()
    {
        var empresaDois = await CriarEmpresaAsync();
        var produtoSemFicha = await CriarProdutoAsync(1, Nome("Produto sem acao item"), ativo: true);
        var produtoId = await CriarProdutoAsync(1, Nome("Produto com acao item"), ativo: true);
        await CriarFichaAsync(1, produtoId, 2.5m, 45);
        var ativoComMarca = await CriarInsumoAsync(1, Nome("Papel ativo"), "Marca metro", UnidadeMedida.Metro, ativo: true);
        var ativoSemMarca = await CriarInsumoAsync(1, Nome("Cola ativa"), null, UnidadeMedida.Unidade, ativo: true);
        var inativo = await CriarInsumoAsync(1, Nome("Papel inativo"), "Fora", UnidadeMedida.Grama, ativo: false);
        var outroTenant = await CriarInsumoAsync(empresaDois, Nome("Papel externo"), "Segredo", UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var fichaSemRegistro = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoSemFicha}"));
        var fichaComRegistro = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));
        var pagina = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo"));

        Assert.DoesNotContain("Adicionar insumo", fichaSemRegistro);
        Assert.Contains("Adicionar insumo", fichaComRegistro);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo", fichaComRegistro);
        Assert.Contains(await ObterRotuloInsumoAsync(ativoComMarca, 1), pagina);
        Assert.Contains(await ObterRotuloInsumoAsync(ativoSemMarca, 1), pagina);
        Assert.DoesNotContain(await ObterRotuloInsumoAsync(inativo, 1), pagina);
        Assert.DoesNotContain(await ObterRotuloInsumoAsync(outroTenant, empresaDois), pagina);
        Assert.DoesNotContain("EmpresaId", pagina);
        Assert.DoesNotContain("FichaTecnicaId", pagina);
        Assert.DoesNotContain("ItemId", pagina);
    }

    [Fact]
    public async Task W4_W19_Post_valido_com_quantidade_decimal_cria_item_e_faz_PRG()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto cria item"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Farinha item"), "Renata", UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "1,25", "  camada 1\r\ncamada 2  ");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{produtoId}", response.Headers.Location!.ToString());
        var item = Assert.Single(await ListarItensAsync(produtoId: produtoId));
        Assert.Equal(1, item.EmpresaId);
        Assert.Equal(fichaId, item.FichaTecnicaId);
        Assert.Equal(insumoId, item.InsumoId);
        Assert.Equal(1.25m, item.Quantidade);
        Assert.Equal("camada 1\r\ncamada 2", item.Observacao);

        var paginaAposRedirect = await LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location));
        Assert.Contains("Insumo adicionado à ficha técnica com sucesso.", paginaAposRedirect);
    }

    [Fact]
    public void W19_Parsing_de_quantidade_com_virgula_independe_da_cultura_atual()
    {
        var culturaOriginal = CultureInfo.CurrentCulture;
        var uiOriginal = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var modelState = new ModelStateDictionary();
        var input = new NovoModel.ItemFichaTecnicaInputModel { Quantidade = "1,25" };

        try
        {
            var valido = ItemFichaTecnicaFormulario.TentarObterQuantidade(modelState, input, out var quantidade);

            Assert.True(valido);
            Assert.True(modelState.IsValid);
            Assert.Equal(1.25m, quantidade);
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaOriginal;
            CultureInfo.CurrentUICulture = uiOriginal;
        }
    }

    [Theory]
    [InlineData(null, null, "A quantidade é obrigatória.")]
    [InlineData("texto", null, "A quantidade deve ser um número válido.")]
    [InlineData("0", null, "A quantidade deve ser maior que zero.")]
    [InlineData("1", "LONGA", null)]
    public async Task W5_Post_invalido_nao_cria_item(string? quantidade, string? observacao, string? mensagem)
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto item invalido"), ativo: true);
        await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo item invalido"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var obs = observacao == "LONGA" ? new string('a', 1001) : observacao;
        var response = await EnviarFormularioAsync(client, produtoId, insumoId, quantidade, obs);
        var conteudo = await LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Empty(await ListarItensAsync(produtoId: produtoId));
        Assert.Contains(mensagem ?? "A observação deve possuir no máximo 1000 caracteres.", conteudo);
    }

    [Fact]
    public async Task W6_Duplicidade_exibe_mensagem_e_nao_cria_segundo_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto duplicado item"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo duplicado item"), null, UnidadeMedida.Grama, ativo: true);
        await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "2", null);
        var conteudo = await LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Este insumo já foi adicionado à ficha técnica.", conteudo);
        Assert.Single(await ListarItensAsync(produtoId: produtoId));
    }

    [Fact]
    public async Task W7_W10_Insumo_inativo_ou_cross_tenant_e_rejeitado_sem_vazamento()
    {
        var empresaDois = await CriarEmpresaAsync();
        var produtoId = await CriarProdutoAsync(1, Nome("Produto insumo indisponivel"), ativo: true);
        await CriarFichaAsync(1, produtoId);
        var inativo = await CriarInsumoAsync(1, Nome("Insumo inativo ficha"), null, UnidadeMedida.Grama, ativo: false);
        var outroTenant = await CriarInsumoAsync(empresaDois, Nome("Insumo outro tenant ficha"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var postInativo = await EnviarFormularioAsync(client, produtoId, inativo, "1", null);
        var paginaInativo = await LerHtmlDecodificadoAsync(postInativo);
        var postOutroTenant = await EnviarFormularioAsync(client, produtoId, outroTenant, "1", null);
        var paginaOutroTenant = await LerHtmlDecodificadoAsync(postOutroTenant);

        Assert.Equal(HttpStatusCode.OK, postInativo.StatusCode);
        Assert.Equal(HttpStatusCode.OK, postOutroTenant.StatusCode);
        Assert.Contains("O insumo selecionado não está disponível para inclusão na ficha técnica.", paginaInativo);
        Assert.Contains("O insumo selecionado não está disponível para inclusão na ficha técnica.", paginaOutroTenant);
        Assert.Empty(await ListarItensAsync(produtoId: produtoId));
    }

    [Fact]
    public async Task W8_Produto_inativo_aceita_item_sem_reativacao()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto inativo com item"), ativo: false);
        await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo produto inativo"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "1", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Single(await ListarItensAsync(produtoId: produtoId));
        Assert.False((await ObterProdutoAsync(produtoId, 1)).Ativo);
    }

    [Fact]
    public async Task W9_Produto_ou_ficha_cross_tenant_retorna_404_sem_criar_item()
    {
        var empresaDois = await CriarEmpresaAsync();
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto outro tenant item"), ativo: true);
        await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var produtoToken = await CriarProdutoAsync(1, Nome("Produto token cross item"), ativo: true);
        await CriarFichaAsync(1, produtoToken);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo cross item"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync($"/Produtos/FichaTecnica/{produtoOutroTenant}/Itens/Novo");
        var post = await EnviarFormularioAsync(client, produtoOutroTenant, insumoId, "1", null, tokenProdutoId: produtoToken);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        Assert.Empty(await ListarItensAsync(produtoId: produtoOutroTenant));
    }

    [Fact]
    public async Task W11_Request_nao_controla_ids_ou_ownership()
    {
        var empresaDois = await CriarEmpresaAsync();
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto manipulado item externo"), ativo: true);
        var fichaOutroTenant = await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var produtoId = await CriarProdutoAsync(1, Nome("Produto manipulado item"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo manipulado item"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "1", null, new Dictionary<string, string>
        {
            ["Id"] = "999",
            ["ItemId"] = "999",
            ["EmpresaId"] = empresaDois.ToString(CultureInfo.InvariantCulture),
            ["FichaTecnicaId"] = fichaOutroTenant.ToString(CultureInfo.InvariantCulture),
            ["ProdutoId"] = produtoOutroTenant.ToString(CultureInfo.InvariantCulture),
            ["Input.Id"] = "999",
            ["Input.EmpresaId"] = empresaDois.ToString(CultureInfo.InvariantCulture),
            ["Input.FichaTecnicaId"] = fichaOutroTenant.ToString(CultureInfo.InvariantCulture)
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var item = Assert.Single(await ListarItensAsync(produtoId: produtoId));
        Assert.Equal(1, item.EmpresaId);
        Assert.Equal(fichaId, item.FichaTecnicaId);
        Assert.Equal(insumoId, item.InsumoId);
    }

    [Fact]
    public async Task W12_W13_W15_W16_Insumo_referenciado_sem_preco_protege_identidade_e_mantem_categoria_observacao_editaveis()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto RN048"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var nomeOriginal = Nome("Insumo RN048");
        var insumoId = await CriarInsumoAsync(1, nomeOriginal, "Marca original", UnidadeMedida.Grama, ativo: true, observacao: "Global original");
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, "Contextual original");
        using var client = await CriarClienteAutenticadoAsync(1);

        var pagina = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Insumos/Editar/{insumoId}"));
        Assert.Contains("Nome, marca e unidade base não podem ser alterados porque este insumo está sendo usado em ficha técnica.", pagina);
        Assert.Contains("readonly", ObterTag(pagina, "input", "Input.Nome"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("readonly", ObterTag(pagina, "input", "Input.Marca"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disabled", ObterTag(pagina, "select", "Input.UnidadeBase"), StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("disabled", ObterTag(pagina, "select", "Input.Categoria"), StringComparison.OrdinalIgnoreCase);

        var post = await EnviarEdicaoInsumoAsync(
            client,
            insumoId,
            "Nome manipulado",
            "Marca manipulada",
            "Embalagem",
            "Metro",
            "Global alterada");

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        var insumo = await ObterInsumoAsync(insumoId, 1);
        Assert.Equal(nomeOriginal, insumo.Nome);
        Assert.Equal("Marca original", insumo.Marca);
        Assert.Equal(UnidadeMedida.Grama, insumo.UnidadeBase);
        Assert.Equal(CategoriaInsumo.Embalagem, insumo.Categoria);
        Assert.Equal("Global alterada", insumo.Observacao);
        Assert.Equal("Contextual original", (await ObterItemAsync(itemId, 1)).Observacao);
    }

    [Fact]
    public async Task W14_RN040_continua_priorizando_mensagem_de_historico()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto RN040"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo RN040"), "Marca", UnidadeMedida.Grama, ativo: true);
        await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        await CriarPrecoAsync(1, insumoId);
        using var client = await CriarClienteAutenticadoAsync(1);

        var pagina = await LerHtmlDecodificadoAsync(await client.GetAsync($"/Insumos/Editar/{insumoId}"));

        Assert.Contains("Nome, marca e unidade base não podem ser alterados porque este insumo já possui histórico de preços.", pagina);
        Assert.DoesNotContain("está sendo usado em ficha técnica.", pagina);
    }

    [Fact]
    public async Task W17_Get_inclusao_nao_cria_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto get nao muta"), ativo: true);
        await CriarFichaAsync(1, produtoId);
        await CriarInsumoAsync(1, Nome("Insumo get nao muta"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo");

        response.EnsureSuccessStatusCode();
        Assert.Empty(await ListarItensAsync(produtoId: produtoId));
    }

    [Fact]
    public async Task W18_Post_sem_antiforgery_nao_cria_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto antiforgery item"), ativo: true);
        await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo antiforgery item"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await CriarClienteAutenticadoAsync(1);

        var response = await client.PostAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.InsumoId"] = insumoId.ToString(CultureInfo.InvariantCulture),
            ["Input.Quantidade"] = "1"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListarItensAsync(produtoId: produtoId));
    }

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient client,
        int produtoId,
        int insumoId,
        string? quantidade,
        string? observacao,
        Dictionary<string, string>? camposExtras = null,
        int? tokenProdutoId = null)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/FichaTecnica/{tokenProdutoId ?? produtoId}/Itens/Novo");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(pagina),
            ["Input.InsumoId"] = insumoId.ToString(CultureInfo.InvariantCulture),
            ["Input.Observacao"] = observacao ?? string.Empty
        };

        if (quantidade is not null)
        {
            dados["Input.Quantidade"] = quantidade;
        }

        if (camposExtras is not null)
        {
            foreach (var campo in camposExtras)
            {
                dados[campo.Key] = campo.Value;
            }
        }

        return await client.PostAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo", new FormUrlEncodedContent(dados));
    }

    private async Task<HttpResponseMessage> EnviarEdicaoInsumoAsync(
        HttpClient client,
        int insumoId,
        string nome,
        string marca,
        string categoria,
        string unidade,
        string observacao)
    {
        var pagina = await client.GetStringAsync($"/Insumos/Editar/{insumoId}");
        return await client.PostAsync($"/Insumos/Editar/{insumoId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = Token(pagina),
            ["Input.Nome"] = nome,
            ["Input.Marca"] = marca,
            ["Input.Categoria"] = categoria,
            ["Input.UnidadeBase"] = unidade,
            ["Input.Observacao"] = observacao
        }));
    }

    private async Task<int> CriarProdutoAsync(int empresaId, string nome, bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = Produto.Criar(empresaId, nome, 0.30m);
        if (!ativo)
        {
            produto.Desativar();
        }

        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
    }

    private async Task<int> CriarFichaAsync(int empresaId, int produtoId, decimal rendimento = 2m, int tempoAtivoMinutos = 30)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var ficha = Precificador.Core.FichasTecnicas.FichaTecnica.Criar(empresaId, produtoId, rendimento, tempoAtivoMinutos);
        context.FichasTecnicas.Add(ficha);
        await context.SaveChangesAsync();
        return ficha.Id;
    }

    private async Task<int> CriarInsumoAsync(
        int empresaId,
        string nome,
        string? marca,
        UnidadeMedida unidade,
        bool ativo,
        CategoriaInsumo categoria = CategoriaInsumo.MateriaPrima,
        string? observacao = null)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var insumo = Insumo.Criar(empresaId, nome, categoria, unidade, marca, observacao);
        if (!ativo)
        {
            insumo.Desativar();
        }

        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        return insumo.Id;
    }

    private async Task<int> CriarItemAsync(int empresaId, int fichaId, int insumoId, decimal quantidade, string? observacao)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var item = ItemFichaTecnica.Criar(empresaId, fichaId, insumoId, quantidade, observacao);
        context.ItensFichaTecnica.Add(item);
        await context.SaveChangesAsync();
        return item.Id;
    }

    private async Task CriarPrecoAsync(int empresaId, int insumoId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumoId, 1m, 10m, new DateOnly(2026, 9, 13)));
        await context.SaveChangesAsync();
    }

    private async Task<List<ItemFichaTecnica>> ListarItensAsync(int? produtoId = null)
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var query =
            from item in context.ItensFichaTecnica.IgnoreQueryFilters().AsNoTracking()
            join ficha in context.FichasTecnicas.IgnoreQueryFilters().AsNoTracking()
                on item.FichaTecnicaId equals ficha.Id
            select new { Item = item, ficha.ProdutoId };

        if (produtoId.HasValue)
        {
            query = query.Where(registro => registro.ProdutoId == produtoId.Value);
        }

        return await query.Select(registro => registro.Item).ToListAsync();
    }

    private async Task<ItemFichaTecnica> ObterItemAsync(int itemId, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.ItensFichaTecnica.AsNoTracking().SingleAsync(item => item.Id == itemId);
    }

    private async Task<Produto> ObterProdutoAsync(int id, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.Produtos.AsNoTracking().SingleAsync(produto => produto.Id == id);
    }

    private async Task<Insumo> ObterInsumoAsync(int id, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.Insumos.AsNoTracking().SingleAsync(insumo => insumo.Id == id);
    }

    private async Task<string> ObterRotuloInsumoAsync(int id, int empresaId)
    {
        var insumo = await ObterInsumoAsync(id, empresaId);
        return string.IsNullOrWhiteSpace(insumo.Marca)
            ? $"{insumo.Nome} ({Precificador.Web.Apresentacao.InsumoRotulos.Unidade(insumo.UnidadeBase)})"
            : $"{insumo.Nome} — {insumo.Marca} ({Precificador.Web.Apresentacao.InsumoRotulos.Unidade(insumo.UnidadeBase)})";
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

    private static string ObterTag(string html, string tag, string nomeCampo) =>
        Regex.Match(html, $"<{tag}[^>]*name=\"{Regex.Escape(nomeCampo)}\"[^>]*>", RegexOptions.IgnoreCase).Value;

    private static string Token(string pagina) =>
        WebUtility.HtmlDecode(Regex.Match(pagina, "name=\"__RequestVerificationToken\" type=\"hidden\" value=\"([^\"]+)\"").Groups[1].Value);

    private static string Nome(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";

    private sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
