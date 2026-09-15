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
    private readonly WebTestContext web = new(factory);
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
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo");
        var post = await EnviarFormularioAsync(client, produtoId, insumoId, "1", null, tokenProdutoId: produtoComFicha);

        Assert.Equal(HttpStatusCode.Redirect, get.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{produtoId}", get.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{produtoId}", post.Headers.Location!.ToString());
        Assert.Empty(await ListarItensAsync(produtoId: produtoId));

        var ficha = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(get.Headers.Location));
        Assert.Contains("Defina a base da ficha técnica antes de adicionar insumos.", ficha);
    }

    [Fact]
    public async Task W3_Get_lista_apenas_insumos_ativos_do_tenant_e_ficha_mostra_acao_quando_existe()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoSemFicha = await CriarProdutoAsync(1, Nome("Produto sem acao item"), ativo: true);
        var produtoId = await CriarProdutoAsync(1, Nome("Produto com acao item"), ativo: true);
        await CriarFichaAsync(1, produtoId, 2.5m, 45);
        var ativoComMarca = await CriarInsumoAsync(1, Nome("Papel ativo"), "Marca metro", UnidadeMedida.Metro, ativo: true);
        var ativoSemMarca = await CriarInsumoAsync(1, Nome("Cola ativa"), null, UnidadeMedida.Unidade, ativo: true);
        var inativo = await CriarInsumoAsync(1, Nome("Papel inativo"), "Fora", UnidadeMedida.Grama, ativo: false);
        var outroTenant = await CriarInsumoAsync(empresaDois, Nome("Papel externo"), "Segredo", UnidadeMedida.Grama, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var fichaSemRegistro = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoSemFicha}"));
        var fichaComRegistro = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));
        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo"));

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
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "1,25", "  camada 1\r\ncamada 2  ");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{produtoId}", response.Headers.Location!.ToString());
        var item = Assert.Single(await ListarItensAsync(produtoId: produtoId));
        Assert.Equal(1, item.EmpresaId);
        Assert.Equal(fichaId, item.FichaTecnicaId);
        Assert.Equal(insumoId, item.InsumoId);
        Assert.Equal(1.25m, item.Quantidade);
        Assert.Equal("camada 1\r\ncamada 2", item.Observacao);
        Assert.True((await ObterInsumoAsync(insumoId, 1)).IdentidadeConsolidada);

        var paginaAposRedirect = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location));
        Assert.Contains("Insumo adicionado à ficha técnica com sucesso.", paginaAposRedirect);
    }

    [Fact]
    public async Task W8_Post_valido_adiciona_insumo_ja_consolidado_a_ficha()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto insumo consolidado"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo consolidado"), "Marca", UnidadeMedida.Grama, ativo: true);
        await CriarPrecoAsync(1, insumoId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        Assert.True((await ObterInsumoAsync(insumoId, 1)).IdentidadeConsolidada);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "2", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var item = Assert.Single(await ListarItensAsync(produtoId: produtoId));
        Assert.Equal(fichaId, item.FichaTecnicaId);
        Assert.Equal(insumoId, item.InsumoId);
        Assert.Equal(2m, item.Quantidade);
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
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var obs = observacao == "LONGA" ? new string('a', 1001) : observacao;
        var response = await EnviarFormularioAsync(client, produtoId, insumoId, quantidade, obs);
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

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
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "2", null);
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Este insumo já foi adicionado à ficha técnica.", conteudo);
        Assert.Single(await ListarItensAsync(produtoId: produtoId));
    }

    [Fact]
    public async Task W7_W10_Insumo_inativo_ou_cross_tenant_e_rejeitado_sem_vazamento()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoId = await CriarProdutoAsync(1, Nome("Produto insumo indisponivel"), ativo: true);
        await CriarFichaAsync(1, produtoId);
        var inativo = await CriarInsumoAsync(1, Nome("Insumo inativo ficha"), null, UnidadeMedida.Grama, ativo: false);
        var outroTenant = await CriarInsumoAsync(empresaDois, Nome("Insumo outro tenant ficha"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var postInativo = await EnviarFormularioAsync(client, produtoId, inativo, "1", null);
        var paginaInativo = await WebTestHtml.LerHtmlDecodificadoAsync(postInativo);
        var postOutroTenant = await EnviarFormularioAsync(client, produtoId, outroTenant, "1", null);
        var paginaOutroTenant = await WebTestHtml.LerHtmlDecodificadoAsync(postOutroTenant);

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
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "1", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Single(await ListarItensAsync(produtoId: produtoId));
        Assert.False((await ObterProdutoAsync(produtoId, 1)).Ativo);
    }

    [Fact]
    public async Task W9_Produto_ou_ficha_cross_tenant_retorna_404_sem_criar_item()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto outro tenant item"), ativo: true);
        await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var produtoToken = await CriarProdutoAsync(1, Nome("Produto token cross item"), ativo: true);
        await CriarFichaAsync(1, produtoToken);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo cross item"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync($"/Produtos/FichaTecnica/{produtoOutroTenant}/Itens/Novo");
        var post = await EnviarFormularioAsync(client, produtoOutroTenant, insumoId, "1", null, tokenProdutoId: produtoToken);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        Assert.Empty(await ListarItensAsync(produtoId: produtoOutroTenant));
    }

    [Fact]
    public async Task W11_Request_nao_controla_ids_ou_ownership()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto manipulado item externo"), ativo: true);
        var fichaOutroTenant = await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var produtoId = await CriarProdutoAsync(1, Nome("Produto manipulado item"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo manipulado item"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

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
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Insumos/Editar/{insumoId}"));
        Assert.Contains("Nome, marca e unidade base não podem ser alterados porque a identidade deste insumo já foi consolidada.", pagina);
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

        Assert.Equal(HttpStatusCode.OK, post.StatusCode);
        var insumo = await ObterInsumoAsync(insumoId, 1);
        Assert.Equal(nomeOriginal, insumo.Nome);
        Assert.Equal("Marca original", insumo.Marca);
        Assert.Equal(UnidadeMedida.Grama, insumo.UnidadeBase);
        Assert.Equal(CategoriaInsumo.MateriaPrima, insumo.Categoria);
        Assert.Equal("Global original", insumo.Observacao);
        Assert.Equal("Contextual original", (await ObterItemAsync(itemId, 1)).Observacao);
    }

    [Fact]
    public async Task W5_Post_com_identidade_consolidada_inalterada_atualiza_categoria_e_observacao()
    {
        var nome = Nome("Insumo editável consolidado");
        var insumoId = await CriarInsumoAsync(1, nome, "Marca original", UnidadeMedida.Grama, ativo: true, observacao: "Original");
        await CriarPrecoAsync(1, insumoId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarEdicaoInsumoAsync(
            client,
            insumoId,
            nome,
            "Marca original",
            "Embalagem",
            "Grama",
            "Observação atualizada");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Insumos/Detalhes/{insumoId}", response.Headers.Location!.OriginalString);
        var insumo = await ObterInsumoAsync(insumoId, 1);
        Assert.Equal(nome, insumo.Nome);
        Assert.Equal("Marca original", insumo.Marca);
        Assert.Equal(UnidadeMedida.Grama, insumo.UnidadeBase);
        Assert.Equal(CategoriaInsumo.Embalagem, insumo.Categoria);
        Assert.Equal("Observação atualizada", insumo.Observacao);
    }

    [Fact]
    public async Task W14_RN040_continua_priorizando_mensagem_de_historico()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto RN040"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo RN040"), "Marca", UnidadeMedida.Grama, ativo: true);
        await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        await CriarPrecoAsync(1, insumoId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Insumos/Editar/{insumoId}"));

        Assert.Contains("Nome, marca e unidade base não podem ser alterados porque a identidade deste insumo já foi consolidada.", pagina);
    }

    [Fact]
    public async Task W17_Get_inclusao_nao_cria_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto get nao muta"), ativo: true);
        await CriarFichaAsync(1, produtoId);
        await CriarInsumoAsync(1, Nome("Insumo get nao muta"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

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
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.PostAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Novo", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.InsumoId"] = insumoId.ToString(CultureInfo.InvariantCulture),
            ["Input.Quantidade"] = "1"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await ListarItensAsync(produtoId: produtoId));
    }

    [Fact]
    public async Task UC015_W1_Edicao_exige_autenticacao_e_empresa_ativa()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto edicao protegida"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo edicao protegida"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await anonimo.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Login", response.Headers.Location!.ToString());

        using var autenticadoSemEmpresaAtiva = await CriarClienteAutenticadoSemEmpresaAtivaAsync();
        var acessoSemEmpresa = await autenticadoSemEmpresaAtiva.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}");
        Assert.Equal(HttpStatusCode.Redirect, acessoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoSemEmpresa.Headers.Location!.ToString());
    }

    [Fact]
    public async Task UC015_W2_Get_carrega_item_e_nao_oferece_insumo_editavel()
    {
        var produtoNome = Nome("Produto get edicao");
        var insumoNome = Nome("Insumo get edicao");
        var produtoId = await CriarProdutoAsync(1, produtoNome, ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, insumoNome, "Marca atual", UnidadeMedida.Metro, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1.25m, "observacao atual");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}"));

        Assert.Contains(produtoNome, pagina);
        Assert.Contains(insumoNome, pagina);
        Assert.Contains("Marca atual", pagina);
        Assert.Contains("m", pagina);
        Assert.Contains("Ativo", pagina);
        Assert.Equal("1,25", ValorDoInput(pagina, "Input.Quantidade"));
        Assert.Contains("observacao atual", pagina);
        Assert.DoesNotContain("Input.InsumoId", pagina);
        Assert.DoesNotContain("EmpresaId", pagina);
        Assert.DoesNotContain("FichaTecnicaId", pagina);
        Assert.DoesNotContain("<select", pagina, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UC015_W3_W23_Post_valido_com_virgula_atualiza_mesmo_item_e_faz_PRG()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto edita item"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo edita item"), "Marca", UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, "original");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarEdicaoItemAsync(client, produtoId, itemId, "1,25", "  ajustado\r\ncom quebra  ");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{produtoId}", response.Headers.Location!.ToString());
        var item = await ObterItemAsync(itemId, 1);
        Assert.Equal(1, item.EmpresaId);
        Assert.Equal(fichaId, item.FichaTecnicaId);
        Assert.Equal(insumoId, item.InsumoId);
        Assert.Equal(1.25m, item.Quantidade);
        Assert.Equal("ajustado\r\ncom quebra", item.Observacao);

        var paginaAposRedirect = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location));
        Assert.Contains("Item da ficha técnica atualizado com sucesso.", paginaAposRedirect);
        Assert.Contains("1,25", paginaAposRedirect);
    }

    [Theory]
    [InlineData(null, null, "A quantidade é obrigatória.")]
    [InlineData("texto", null, "A quantidade deve ser um número válido.")]
    [InlineData("0", null, "A quantidade deve ser maior que zero.")]
    [InlineData("-1", null, "A quantidade deve ser maior que zero.")]
    [InlineData("1", "LONGA", "A observação deve possuir no máximo 1000 caracteres.")]
    public async Task UC015_W4_Post_invalido_nao_persiste_alteracoes(string? quantidade, string? observacao, string mensagem)
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto edicao invalida"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo edicao invalida"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 2m, "original");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var obs = observacao == "LONGA" ? new string('a', 1001) : observacao;
        var response = await EnviarEdicaoItemAsync(client, produtoId, itemId, quantidade, obs);
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(mensagem, conteudo);
        var item = await ObterItemAsync(itemId, 1);
        Assert.Equal(2m, item.Quantidade);
        Assert.Equal("original", item.Observacao);
        Assert.Equal(fichaId, item.FichaTecnicaId);
        Assert.Equal(insumoId, item.InsumoId);
    }

    [Fact]
    public async Task UC015_W5_Request_manipulado_nao_altera_vinculos()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto item manipulado externo"), ativo: true);
        var fichaOutroTenant = await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var insumoOutroTenant = await CriarInsumoAsync(empresaDois, Nome("Insumo item manipulado externo"), null, UnidadeMedida.Metro, ativo: true);
        var produtoId = await CriarProdutoAsync(1, Nome("Produto item manipulado"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo item manipulado"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarEdicaoItemAsync(client, produtoId, itemId, "3", "alterado", new Dictionary<string, string>
        {
            ["Id"] = "999",
            ["ItemId"] = "999",
            ["EmpresaId"] = empresaDois.ToString(CultureInfo.InvariantCulture),
            ["FichaTecnicaId"] = fichaOutroTenant.ToString(CultureInfo.InvariantCulture),
            ["InsumoId"] = insumoOutroTenant.ToString(CultureInfo.InvariantCulture),
            ["Input.Id"] = "999",
            ["Input.EmpresaId"] = empresaDois.ToString(CultureInfo.InvariantCulture),
            ["Input.FichaTecnicaId"] = fichaOutroTenant.ToString(CultureInfo.InvariantCulture),
            ["Input.InsumoId"] = insumoOutroTenant.ToString(CultureInfo.InvariantCulture)
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var item = await ObterItemAsync(itemId, 1);
        Assert.Equal(1, item.EmpresaId);
        Assert.Equal(fichaId, item.FichaTecnicaId);
        Assert.Equal(insumoId, item.InsumoId);
        Assert.Equal(3m, item.Quantidade);
        Assert.Equal("alterado", item.Observacao);
    }

    [Fact]
    public async Task UC015_W6_W15_Insumo_inativo_permanece_visivel_e_editavel_sem_reativacao()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto insumo inativo editavel"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo inativo editavel"), "Marca", UnidadeMedida.Unidade, ativo: false);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var ficha = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));
        var get = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}"));
        var post = await EnviarEdicaoItemAsync(client, produtoId, itemId, "2", "inativo mantido");

        Assert.Contains("Itens da ficha", ficha);
        Assert.Contains("Inativo", ficha);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}", ficha);
        Assert.Contains("Situação do insumo", get);
        Assert.Contains("Inativo", get);
        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal(2m, (await ObterItemAsync(itemId, 1)).Quantidade);
        Assert.False((await ObterInsumoAsync(insumoId, 1)).Ativo);
    }

    [Fact]
    public async Task UC015_W7_Produto_inativo_permanece_editavel_sem_reativacao()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto inativo item editavel"), ativo: false);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo produto inativo editavel"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarEdicaoItemAsync(client, produtoId, itemId, "2", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(2m, (await ObterItemAsync(itemId, 1)).Quantidade);
        Assert.False((await ObterProdutoAsync(produtoId, 1)).Ativo);
    }

    [Fact]
    public async Task UC015_W8_Produto_inexistente_ou_cross_tenant_retorna_404()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto outro tenant edicao item"), ativo: true);
        var fichaOutroTenant = await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var insumoOutroTenant = await CriarInsumoAsync(empresaDois, Nome("Insumo outro tenant edicao item"), null, UnidadeMedida.Grama, ativo: true);
        var itemOutroTenant = await CriarItemAsync(empresaDois, fichaOutroTenant, insumoOutroTenant, 1m, "externo");
        var produtoToken = await CriarProdutoAsync(1, Nome("Produto token edicao item"), ativo: true);
        var fichaToken = await CriarFichaAsync(1, produtoToken);
        var insumoToken = await CriarInsumoAsync(1, Nome("Insumo token edicao item"), null, UnidadeMedida.Grama, ativo: true);
        var itemToken = await CriarItemAsync(1, fichaToken, insumoToken, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var getInexistente = await client.GetAsync($"/Produtos/FichaTecnica/999999/Itens/Editar/{itemToken}");
        var postInexistente = await EnviarEdicaoItemAsync(client, 999999, itemToken, "2", null, tokenProdutoId: produtoToken, tokenItemId: itemToken);
        var getOutroTenant = await client.GetAsync($"/Produtos/FichaTecnica/{produtoOutroTenant}/Itens/Editar/{itemOutroTenant}");
        var postOutroTenant = await EnviarEdicaoItemAsync(client, produtoOutroTenant, itemOutroTenant, "2", null, tokenProdutoId: produtoToken, tokenItemId: itemToken);

        Assert.Equal(HttpStatusCode.NotFound, getInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, postInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getOutroTenant.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, postOutroTenant.StatusCode);
        Assert.Equal("externo", (await ObterItemAsync(itemOutroTenant, empresaDois)).Observacao);
    }

    [Fact]
    public async Task UC015_W9_Item_inexistente_ou_cross_tenant_retorna_404()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoId = await CriarProdutoAsync(1, Nome("Produto item inexistente"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo item inexistente"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, "original");
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto item cross tenant"), ativo: true);
        var fichaOutroTenant = await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var insumoOutroTenant = await CriarInsumoAsync(empresaDois, Nome("Insumo item cross tenant"), null, UnidadeMedida.Grama, ativo: true);
        var itemOutroTenant = await CriarItemAsync(empresaDois, fichaOutroTenant, insumoOutroTenant, 1m, "externo");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var getInexistente = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/999999");
        var postInexistente = await EnviarEdicaoItemAsync(client, produtoId, 999999, "2", null, tokenItemId: itemId);
        var getCrossTenant = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemOutroTenant}");
        var postCrossTenant = await EnviarEdicaoItemAsync(client, produtoId, itemOutroTenant, "2", null, tokenItemId: itemId);

        Assert.Equal(HttpStatusCode.NotFound, getInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, postInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, getCrossTenant.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, postCrossTenant.StatusCode);
        Assert.Equal("original", (await ObterItemAsync(itemId, 1)).Observacao);
        Assert.Equal("externo", (await ObterItemAsync(itemOutroTenant, empresaDois)).Observacao);
    }

    [Fact]
    public async Task UC015_W10_Item_de_outra_ficha_retorna_404()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto ficha correta"), ativo: true);
        await CriarFichaAsync(1, produtoId);
        var outroProdutoId = await CriarProdutoAsync(1, Nome("Produto outra ficha"), ativo: true);
        var outraFichaId = await CriarFichaAsync(1, outroProdutoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo outra ficha"), null, UnidadeMedida.Grama, ativo: true);
        var itemOutraFicha = await CriarItemAsync(1, outraFichaId, insumoId, 1m, "outra ficha");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var get = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemOutraFicha}");
        var post = await EnviarEdicaoItemAsync(client, produtoId, itemOutraFicha, "2", null, tokenProdutoId: outroProdutoId);

        Assert.Equal(HttpStatusCode.NotFound, get.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, post.StatusCode);
        Assert.Equal("outra ficha", (await ObterItemAsync(itemOutraFicha, 1)).Observacao);
    }

    [Fact]
    public async Task UC015_W11_Get_edicao_nao_muta_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto get edicao nao muta"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo get edicao nao muta"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, "original");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}");

        response.EnsureSuccessStatusCode();
        var item = await ObterItemAsync(itemId, 1);
        Assert.Equal(1m, item.Quantidade);
        Assert.Equal("original", item.Observacao);
    }

    [Fact]
    public async Task UC015_W12_Post_sem_antiforgery_nao_altera_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto antiforgery edicao item"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo antiforgery edicao item"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, "original");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.PostAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Quantidade"] = "2",
            ["Input.Observacao"] = "alterada"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var item = await ObterItemAsync(itemId, 1);
        Assert.Equal(1m, item.Quantidade);
        Assert.Equal("original", item.Observacao);
    }

    [Fact]
    public async Task UC015_W13_Editar_item_mantem_RN048_ativa_no_insumo()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto RN048 edicao item"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo RN048 edicao item"), "Marca", UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var post = await EnviarEdicaoItemAsync(client, produtoId, itemId, "2", "referencia mantida");
        var paginaInsumo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Insumos/Editar/{insumoId}"));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Contains("Nome, marca e unidade base não podem ser alterados porque a identidade deste insumo já foi consolidada.", paginaInsumo);
        Assert.Contains("readonly", ObterTag(paginaInsumo, "input", "Input.Nome"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("readonly", ObterTag(paginaInsumo, "input", "Input.Marca"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disabled", ObterTag(paginaInsumo, "select", "Input.UnidadeBase"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UC015_W14_Ficha_lista_somente_itens_da_propria_ficha_com_link_editar()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoId = await CriarProdutoAsync(1, Nome("Produto lista itens"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoNome = Nome("Insumo lista proprio");
        var insumoId = await CriarInsumoAsync(1, insumoNome, "Marca propria", UnidadeMedida.Metro, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1.25m, null);
        var outroProdutoId = await CriarProdutoAsync(1, Nome("Produto lista outro"), ativo: true);
        var outraFichaId = await CriarFichaAsync(1, outroProdutoId);
        var outroInsumoNome = Nome("Insumo lista outra ficha");
        var outroInsumoId = await CriarInsumoAsync(1, outroInsumoNome, null, UnidadeMedida.Unidade, ativo: true);
        await CriarItemAsync(1, outraFichaId, outroInsumoId, 2m, null);
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto lista tenant"), ativo: true);
        var fichaOutroTenant = await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var insumoOutroTenantNome = Nome("Insumo lista tenant");
        var insumoOutroTenant = await CriarInsumoAsync(empresaDois, insumoOutroTenantNome, null, UnidadeMedida.Grama, ativo: true);
        await CriarItemAsync(empresaDois, fichaOutroTenant, insumoOutroTenant, 3m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));

        Assert.Contains("Itens da ficha", pagina);
        Assert.Contains("<th scope=\"col\">Insumo</th>", pagina);
        Assert.Contains("<th scope=\"col\">Marca</th>", pagina);
        Assert.Contains(insumoNome, pagina);
        Assert.Contains("Marca propria", pagina);
        Assert.Contains("1,25", pagina);
        Assert.Contains("m", pagina);
        Assert.Contains("Ativo", pagina);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}", pagina);
        Assert.DoesNotContain(outroInsumoNome, pagina);
        Assert.DoesNotContain(insumoOutroTenantNome, pagina);
        Assert.Contains("—", pagina);
        Assert.Contains("Custo unitário", pagina);
        Assert.Contains("Custo do item", pagina);
    }

    [Fact]
    public async Task UC017_W5_W6_W7_W8_W9_W11_W13_W16_Consulta_exibe_composicao_completa_ordenada_e_isolada()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoId = await CriarProdutoAsync(1, Nome("Produto UC017"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var nomeAlfa = Nome("Alfa UC017");
        var nomeZeta = Nome("Zeta UC017");
        var insumoAlfaZ = await CriarInsumoAsync(1, nomeAlfa, "Z marca", UnidadeMedida.Grama, ativo: true, observacao: "global nao contextual");
        var insumoAlfaA = await CriarInsumoAsync(1, nomeAlfa, "A marca", UnidadeMedida.Metro, ativo: true);
        var insumoZeta = await CriarInsumoAsync(1, nomeZeta, null, UnidadeMedida.Unidade, ativo: false);
        var itemAlfaZ = await CriarItemAsync(1, fichaId, insumoAlfaZ, 1.25m, "contextual linha 1\r\ncontextual linha 2");
        await CriarItemAsync(1, fichaId, insumoAlfaA, 2m, null);
        var itemZeta = await CriarItemAsync(1, fichaId, insumoZeta, 3m, null);
        var outroProduto = await CriarProdutoAsync(1, Nome("Produto outra ficha UC017"), ativo: true);
        var outraFicha = await CriarFichaAsync(1, outroProduto);
        var nomeOutraFicha = Nome("Insumo outra ficha UC017");
        await CriarItemAsync(1, outraFicha, await CriarInsumoAsync(1, nomeOutraFicha, null, UnidadeMedida.Grama, true), 1m, null);
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto outro tenant UC017"), ativo: true);
        var fichaOutroTenant = await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var nomeOutroTenant = Nome("Insumo outro tenant UC017");
        await CriarItemAsync(empresaDois, fichaOutroTenant, await CriarInsumoAsync(empresaDois, nomeOutroTenant, null, UnidadeMedida.Grama, true), 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));

        Assert.Contains("<th scope=\"col\">Marca</th>", pagina);
        Assert.Contains("Observação contextual", pagina);
        Assert.Contains("contextual linha 1", pagina);
        Assert.Contains("contextual linha 2", pagina);
        Assert.Contains("white-space: pre-wrap", pagina);
        Assert.DoesNotContain("global nao contextual", pagina);
        Assert.Matches(
            $"(?s)<td>{Regex.Escape(nomeAlfa)}</td>\\s*<td>A marca</td>\\s*<td>.*?</td>\\s*<td>.*?</td>\\s*<td[^>]*>—</td>",
            pagina);
        Assert.Contains("Inativo", pagina);
        Assert.Contains("1,25", pagina);
        Assert.Contains("m", pagina);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemAlfaZ}", pagina);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{itemZeta}", pagina);
        Assert.True(pagina.IndexOf("A marca", StringComparison.Ordinal) < pagina.IndexOf("Z marca", StringComparison.Ordinal));
        Assert.True(pagina.IndexOf("Z marca", StringComparison.Ordinal) < pagina.IndexOf(nomeZeta, StringComparison.Ordinal));
        Assert.DoesNotContain(nomeOutraFicha, pagina);
        Assert.DoesNotContain(nomeOutroTenant, pagina);
        Assert.Contains("Custo unitário", pagina);
        Assert.Contains("Custo do item", pagina);
        Assert.DoesNotContain("Margem", pagina);
    }

    [Fact]
    public async Task UC017_W14_Get_da_ficha_nao_muta_produto_ficha_item_ou_insumo()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto GET imutavel UC017"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId, 2.5m, 45);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo GET imutavel UC017"), "Marca original", UnidadeMedida.Metro, true, observacao: "Global original");
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1.25m, "Contextual original");
        var produtoAntes = await ObterProdutoAsync(produtoId, 1);
        var fichaAntes = await ObterFichaAsync(fichaId, 1);
        var itemAntes = await ObterItemAsync(itemId, 1);
        var insumoAntes = await ObterInsumoAsync(insumoId, 1);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}");

        response.EnsureSuccessStatusCode();
        var produtoDepois = await ObterProdutoAsync(produtoId, 1);
        var fichaDepois = await ObterFichaAsync(fichaId, 1);
        var itemDepois = await ObterItemAsync(itemId, 1);
        var insumoDepois = await ObterInsumoAsync(insumoId, 1);
        Assert.Equal(produtoAntes.Id, produtoDepois.Id);
        Assert.Equal(produtoAntes.EmpresaId, produtoDepois.EmpresaId);
        Assert.Equal(produtoAntes.Nome, produtoDepois.Nome);
        Assert.Equal(produtoAntes.Categoria, produtoDepois.Categoria);
        Assert.Equal(produtoAntes.MargemAlvo, produtoDepois.MargemAlvo);
        Assert.Equal(produtoAntes.Ativo, produtoDepois.Ativo);
        Assert.Equal(fichaAntes.Id, fichaDepois.Id);
        Assert.Equal(fichaAntes.EmpresaId, fichaDepois.EmpresaId);
        Assert.Equal(fichaAntes.ProdutoId, fichaDepois.ProdutoId);
        Assert.Equal(fichaAntes.Rendimento, fichaDepois.Rendimento);
        Assert.Equal(fichaAntes.TempoAtivoMinutos, fichaDepois.TempoAtivoMinutos);
        Assert.Equal(itemAntes.Id, itemDepois.Id);
        Assert.Equal(itemAntes.EmpresaId, itemDepois.EmpresaId);
        Assert.Equal(itemAntes.FichaTecnicaId, itemDepois.FichaTecnicaId);
        Assert.Equal(itemAntes.InsumoId, itemDepois.InsumoId);
        Assert.Equal(itemAntes.Quantidade, itemDepois.Quantidade);
        Assert.Equal(itemAntes.Observacao, itemDepois.Observacao);
        Assert.Equal(insumoAntes.Id, insumoDepois.Id);
        Assert.Equal(insumoAntes.EmpresaId, insumoDepois.EmpresaId);
        Assert.Equal(insumoAntes.Nome, insumoDepois.Nome);
        Assert.Equal(insumoAntes.Marca, insumoDepois.Marca);
        Assert.Equal(insumoAntes.Categoria, insumoDepois.Categoria);
        Assert.Equal(insumoAntes.UnidadeBase, insumoDepois.UnidadeBase);
        Assert.Equal(insumoAntes.Observacao, insumoDepois.Observacao);
        Assert.Equal(insumoAntes.Ativo, insumoDepois.Ativo);
        Assert.Equal(insumoAntes.IdentidadeConsolidada, insumoDepois.IdentidadeConsolidada);
    }

    [Fact]
    public async Task UC017_W15_Post_invalido_da_base_preserva_composicao_completa()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto base invalida UC017"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId, 2m, 30);
        var itemId = await CriarItemAsync(1, fichaId, await CriarInsumoAsync(1, Nome("Insumo base UC017"), "Marca preservada", UnidadeMedida.Grama, true), 1m, "Observação preservada");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarFichaBaseAsync(client, produtoId, "0", "30");
        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Marca preservada", pagina);
        Assert.Contains("Observação preservada", pagina);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}", pagina);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{itemId}", pagina);
        var ficha = await ObterFichaAsync(fichaId, 1);
        Assert.Equal(2m, ficha.Rendimento);
        Assert.Equal(30, ficha.TempoAtivoMinutos);
    }

    [Fact]
    public async Task UC015_W16_Post_invalido_da_base_preserva_navegacao_de_itens()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto base invalida itens"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId, 2m, 30);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo base invalida itens"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarFichaBaseAsync(client, produtoId, "0", "30");
        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("O rendimento deve ser maior que zero.", pagina);
        Assert.Contains("Adicionar insumo", pagina);
        Assert.Contains("Itens da ficha", pagina);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}", pagina);
        var ficha = await ObterFichaAsync(fichaId, 1);
        Assert.Equal(2m, ficha.Rendimento);
        Assert.Equal(30, ficha.TempoAtivoMinutos);
    }

    [Fact]
    public void UC015_W17_Parser_compartilhado_e_formatador_sao_deterministicos_sob_cultura_invariant()
    {
        var culturaOriginal = CultureInfo.CurrentCulture;
        var uiOriginal = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
        CultureInfo.CurrentUICulture = CultureInfo.InvariantCulture;
        var modelState = new ModelStateDictionary();

        try
        {
            var valido = ItemFichaTecnicaFormulario.TentarObterQuantidade(modelState, "1,25", out var quantidade);

            Assert.True(valido);
            Assert.True(modelState.IsValid);
            Assert.Equal(1.25m, quantidade);
            Assert.Equal("1,25", ItemFichaTecnicaFormulario.FormatarQuantidade(1.25m));
            Assert.Equal("1,234567", ItemFichaTecnicaFormulario.FormatarQuantidade(1.234567m));
        }
        finally
        {
            CultureInfo.CurrentCulture = culturaOriginal;
            CultureInfo.CurrentUICulture = uiOriginal;
        }
    }

    [Fact]
    public async Task UC015_W18_Fluxo_Novo_continua_aceitando_quantidade_apos_refatoracao()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto novo pos refatoracao"), ativo: true);
        await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo novo pos refatoracao"), null, UnidadeMedida.Grama, ativo: true);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await EnviarFormularioAsync(client, produtoId, insumoId, "2,5", null);

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(2.5m, Assert.Single(await ListarItensAsync(produtoId: produtoId)).Quantidade);
    }

    [Fact]
    public async Task UC016_W1_Remocao_exige_autenticacao_e_empresa_ativa()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto remocao protegida"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo remocao protegida"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await anonimo.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{itemId}");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("/Conta/Login", response.Headers.Location!.ToString());
        using var semEmpresa = await CriarClienteAutenticadoSemEmpresaAtivaAsync();
        var acessoSemEmpresa = await semEmpresa.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{itemId}");
        Assert.Equal(HttpStatusCode.Redirect, acessoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoSemEmpresa.Headers.Location!.ToString());
    }

    [Fact]
    public async Task UC016_W2_Get_exibe_cadeia_e_nao_remove_item()
    {
        var produtoNome = Nome("Produto confirmacao remocao");
        var insumoNome = Nome("Insumo confirmacao remocao");
        var produtoId = await CriarProdutoAsync(1, produtoNome, ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, insumoNome, "Marca", UnidadeMedida.Metro, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1.25m, "contexto");
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{itemId}"));

        Assert.Contains(produtoNome, pagina);
        Assert.Contains(insumoNome, pagina);
        Assert.Contains("Marca", pagina);
        Assert.Contains("m", pagina);
        Assert.Contains("1,25", pagina);
        Assert.Contains("contexto", pagina);
        Assert.Contains("Tem certeza que deseja remover este item da ficha técnica?", pagina);
        Assert.Equal(itemId, (await ObterItemAsync(itemId, 1)).Id);
    }

    [Fact]
    public async Task UC016_W3_Ficha_exibe_link_remover_correto()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto link remocao"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo link remocao"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));

        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{itemId}", pagina);
    }

    [Fact]
    public async Task UC016_W4_W5_Post_remove_item_selecionado_com_prg_e_preserva_outro()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto remove um"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoUm = await CriarInsumoAsync(1, Nome("Insumo remove um"), null, UnidadeMedida.Grama, ativo: true);
        var insumoDois = await CriarInsumoAsync(1, Nome("Insumo preservado"), null, UnidadeMedida.Grama, ativo: true);
        var itemRemovido = await CriarItemAsync(1, fichaId, insumoUm, 1m, null);
        var itemPreservado = await CriarItemAsync(1, fichaId, insumoDois, 2m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var post = await EnviarRemocaoAsync(client, produtoId, itemRemovido);
        var ficha = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal($"/Produtos/FichaTecnica/{produtoId}", post.Headers.Location!.ToString());
        Assert.Equal(itemPreservado, Assert.Single(await ListarItensAsync(produtoId)).Id);
        Assert.Contains("Item removido da ficha técnica com sucesso.", ficha);
    }

    [Fact]
    public async Task UC016_W6_Remover_ultimo_item_mantem_ficha_vazia()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto ultimo item"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId, 3m, 45);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo ultimo item"), null, UnidadeMedida.Grama, ativo: true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        await EnviarRemocaoAsync(client, produtoId, itemId);
        var pagina = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}"));
        var ficha = await ObterFichaAsync(fichaId, 1);

        Assert.Empty(await ListarItensAsync(produtoId));
        Assert.Equal(3m, ficha.Rendimento);
        Assert.Equal(45, ficha.TempoAtivoMinutos);
        Assert.Contains("Nenhum insumo adicionado.", pagina);
    }

    [Theory]
    [InlineData(true, false)]
    [InlineData(false, true)]
    public async Task UC016_W7_W8_Produto_ou_insumo_inativo_permite_remocao_sem_reativacao(bool produtoAtivo, bool insumoAtivo)
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto inativo remocao"), produtoAtivo);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo inativo remocao"), null, UnidadeMedida.Grama, insumoAtivo);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var post = await EnviarRemocaoAsync(client, produtoId, itemId);

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal(produtoAtivo, (await ObterProdutoAsync(produtoId, 1)).Ativo);
        Assert.Equal(insumoAtivo, (await ObterInsumoAsync(insumoId, 1)).Ativo);
    }

    [Fact]
    public async Task UC016_W9_W10_W11_Ids_invalidos_ou_de_outra_ficha_ou_tenant_retorna_404_sem_mutacao()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var produtoId = await CriarProdutoAsync(1, Nome("Produto ownership remocao"), ativo: true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var itemProprio = await CriarItemAsync(1, fichaId, await CriarInsumoAsync(1, Nome("Insumo proprio ownership remocao"), null, UnidadeMedida.Grama, true), 1m, null);
        var produtoSemFicha = await CriarProdutoAsync(1, Nome("Produto sem ficha remocao"), ativo: true);
        var outroProdutoId = await CriarProdutoAsync(1, Nome("Produto outra ficha remocao"), ativo: true);
        var outraFichaId = await CriarFichaAsync(1, outroProdutoId);
        var itemOutraFicha = await CriarItemAsync(1, outraFichaId, await CriarInsumoAsync(1, Nome("Insumo outra ficha remocao"), null, UnidadeMedida.Grama, true), 1m, null);
        var produtoOutroTenant = await CriarProdutoAsync(empresaDois, Nome("Produto outro tenant remocao"), true);
        var fichaOutroTenant = await CriarFichaAsync(empresaDois, produtoOutroTenant);
        var itemOutroTenant = await CriarItemAsync(empresaDois, fichaOutroTenant, await CriarInsumoAsync(empresaDois, Nome("Insumo outro tenant remocao"), null, UnidadeMedida.Grama, true), 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var inexistente = await client.GetAsync($"/Produtos/FichaTecnica/999999/Itens/Remover/999999");
        var semFicha = await client.GetAsync($"/Produtos/FichaTecnica/{produtoSemFicha}/Itens/Remover/999999");
        var itemInexistente = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/999999");
        var outraFicha = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{itemOutraFicha}");
        var outroTenant = await client.GetAsync($"/Produtos/FichaTecnica/{produtoOutroTenant}/Itens/Remover/{itemOutroTenant}");
        var postInexistente = await EnviarRemocaoAsync(client, produtoId, 999999, tokenProdutoId: produtoId, tokenItemId: itemProprio);
        var postOutraFicha = await EnviarRemocaoAsync(client, produtoId, itemOutraFicha, tokenProdutoId: produtoId, tokenItemId: itemProprio);
        var postOutroTenant = await EnviarRemocaoAsync(client, produtoOutroTenant, itemOutroTenant, tokenProdutoId: produtoId, tokenItemId: itemProprio);

        Assert.Equal(HttpStatusCode.NotFound, inexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, semFicha.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, itemInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, outraFicha.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, outroTenant.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, postInexistente.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, postOutraFicha.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, postOutroTenant.StatusCode);
        Assert.Equal(itemProprio, (await ObterItemAsync(itemProprio, 1)).Id);
        Assert.Equal(itemOutraFicha, (await ObterItemAsync(itemOutraFicha, 1)).Id);
        Assert.Equal(itemOutroTenant, (await ObterItemAsync(itemOutroTenant, empresaDois)).Id);
    }

    [Fact]
    public async Task UC016_W12_W13_Request_manipulado_ou_sem_antiforgery_nao_remove_outro_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto request remocao"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var itemId = await CriarItemAsync(1, fichaId, await CriarInsumoAsync(1, Nome("Insumo request alvo"), null, UnidadeMedida.Grama, true), 1m, null);
        var outroItemId = await CriarItemAsync(1, fichaId, await CriarInsumoAsync(1, Nome("Insumo request outro"), null, UnidadeMedida.Grama, true), 2m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var manipulado = await EnviarRemocaoAsync(client, produtoId, itemId, new Dictionary<string, string>
        {
            ["EmpresaId"] = "999", ["FichaTecnicaId"] = "999", ["InsumoId"] = "999", ["ItemId"] = outroItemId.ToString(CultureInfo.InvariantCulture),
            ["Input.EmpresaId"] = "999", ["Input.FichaTecnicaId"] = "999", ["Input.InsumoId"] = "999", ["Input.ItemId"] = outroItemId.ToString(CultureInfo.InvariantCulture)
        });
        var semToken = await client.PostAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{outroItemId}", new FormUrlEncodedContent([]));

        Assert.Equal(HttpStatusCode.Redirect, manipulado.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, semToken.StatusCode);
        Assert.Equal(outroItemId, Assert.Single(await ListarItensAsync(produtoId)).Id);
    }

    [Fact]
    public async Task UC016_W14_W15_Remocao_preserva_identidade_consolidada_e_bloqueio_da_ultima_referencia_sem_preco()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto identidade apos remocao"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo identidade apos remocao"), "Marca", UnidadeMedida.Grama, true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        await EnviarRemocaoAsync(client, produtoId, itemId);
        var insumo = await ObterInsumoAsync(insumoId, 1);
        var paginaEdicao = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Insumos/Editar/{insumoId}"));

        Assert.True(insumo.IdentidadeConsolidada);
        Assert.Contains("Nome, marca e unidade base não podem ser alterados porque a identidade deste insumo já foi consolidada.", paginaEdicao);
        Assert.Contains("readonly", ObterTag(paginaEdicao, "input", "Input.Nome"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("readonly", ObterTag(paginaEdicao, "input", "Input.Marca"), StringComparison.OrdinalIgnoreCase);
        Assert.Contains("disabled", ObterTag(paginaEdicao, "select", "Input.UnidadeBase"), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UC019_W1_W2_Novo_converte_percentual_e_vazio_para_fracao_ou_zero()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto perdas novo"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var primeiroInsumo = await CriarInsumoAsync(1, Nome("Insumo perda pt"), null, UnidadeMedida.Grama, true);
        var segundoInsumo = await CriarInsumoAsync(1, Nome("Insumo perda zero"), null, UnidadeMedida.Grama, true);
        var client = await web.CriarClienteAutenticadoAsync();

        var ptBr = await EnviarFormularioAsync(client, produtoId, primeiroInsumo, "1", null, percentualPerda: "12,3456");
        var vazio = await EnviarFormularioAsync(client, produtoId, segundoInsumo, "1", null);

        Assert.Equal(HttpStatusCode.Redirect, ptBr.StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, vazio.StatusCode);
        var itens = await ListarItensAsync(produtoId);
        Assert.Equal(0.123456m, itens.Single(item => item.FichaTecnicaId == fichaId && item.InsumoId == primeiroInsumo).PercentualPerda);
        Assert.Equal(0m, itens.Single(item => item.FichaTecnicaId == fichaId && item.InsumoId == segundoInsumo).PercentualPerda);

        var pontoInvariant = await CriarInsumoAsync(1, Nome("Insumo perda invariant"), null, UnidadeMedida.Grama, true);
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarFormularioAsync(client, produtoId, pontoInvariant, "1", null, percentualPerda: "12.5")).StatusCode);
        Assert.Equal(0.125m, (await ListarItensAsync(produtoId)).Single(item => item.InsumoId == pontoInvariant).PercentualPerda);
    }

    [Fact]
    public async Task UC019_W3_W4_Editar_preserva_precision_e_rejeita_percentual_invalido_sem_mutar()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto perdas editar"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo perdas editar"), null, UnidadeMedida.Grama, true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1m, "original");
        var client = await web.CriarClienteAutenticadoAsync();

        var valido = await EnviarEdicaoItemAsync(client, produtoId, itemId, "2", "alterada", percentualPerda: "12.3456");
        Assert.Equal(HttpStatusCode.Redirect, valido.StatusCode);
        var pagina = await client.GetStringAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}");
        Assert.Equal("12,3456", ValorDoInput(pagina, "Input.PercentualPerda"));

        var invalido = await EnviarEdicaoItemAsync(client, produtoId, itemId, "3", "nao deve persistir", percentualPerda: "100");
        var conteudo = await invalido.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, invalido.StatusCode);
        Assert.Contains("100", ValorDoInput(conteudo, "Input.PercentualPerda"));
        var persistido = await ObterItemAsync(itemId, 1);
        Assert.Equal((2m, "alterada", 0.123456m), (persistido.Quantidade, persistido.Observacao, persistido.PercentualPerda));
    }

    [Fact]
    public async Task UC019_W5_W7_W8_W9_W10_Ficha_exibe_perdas_e_nunca_total_parcial()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto perdas ficha"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var conhecido = await CriarInsumoAsync(1, Nome("Insumo perda conhecido"), null, UnidadeMedida.Grama, true);
        var semPreco = await CriarInsumoAsync(1, Nome("Insumo perda sem preco"), null, UnidadeMedida.Grama, true);
        var itemConhecido = await CriarItemAsync(1, fichaId, conhecido, 2m, null);
        var itemSemPreco = await CriarItemAsync(1, fichaId, semPreco, 1m, null);
        await CriarPrecoAsync(1, conhecido);
        var client = await web.CriarClienteAutenticadoAsync();

        Assert.Equal(HttpStatusCode.Redirect, (await EnviarEdicaoItemAsync(client, produtoId, itemConhecido, "2", null, percentualPerda: "10")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await EnviarEdicaoItemAsync(client, produtoId, itemSemPreco, "1", null, percentualPerda: "10")).StatusCode);
        var pagina = await client.GetStringAsync($"/Produtos/FichaTecnica/{produtoId}");

        Assert.Contains("Perda esperada (%)", pagina);
        Assert.Contains("Custo da perda", pagina);
        Assert.Contains("Custo de perdas do lote:", pagina);
        Assert.Contains("indisponível", pagina);
        Assert.Contains("Há perda(s) sem custo base determinável.", pagina);
    }

    [Fact]
    public async Task UC019_W5_W6_Ficha_exibe_custos_individuais_e_total_exato_de_perdas_conhecidas()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto perdas completas"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var primeiro = await CriarInsumoAsync(1, Nome("Insumo perda primeiro"), null, UnidadeMedida.Grama, true);
        var segundo = await CriarInsumoAsync(1, Nome("Insumo perda segundo"), null, UnidadeMedida.Grama, true);
        var itemPrimeiro = await CriarItemAsync(1, fichaId, primeiro, 2m, null);
        var itemSegundo = await CriarItemAsync(1, fichaId, segundo, 3m, null);
        await CriarPrecoAsync(1, primeiro);
        await CriarPrecoAsync(1, segundo);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        await EnviarEdicaoItemAsync(client, produtoId, itemPrimeiro, "2", null, percentualPerda: "10");
        await EnviarEdicaoItemAsync(client, produtoId, itemSegundo, "3", null, percentualPerda: "20");
        var pagina = await client.GetStringAsync($"/Produtos/FichaTecnica/{produtoId}");

        Assert.Matches(new Regex($"{Regex.Escape((await ObterInsumoAsync(primeiro, 1)).Nome)}.*?<td>10%</td>.*?<td>2</td>", RegexOptions.Singleline), pagina);
        Assert.Matches(new Regex($"{Regex.Escape((await ObterInsumoAsync(segundo, 1)).Nome)}.*?<td>20%</td>.*?<td>6</td>", RegexOptions.Singleline), pagina);
        Assert.Matches(new Regex("Custo de perdas do lote:</strong>\\s*8", RegexOptions.Singleline), pagina);
    }

    [Fact]
    public async Task UC019_W7_W8_W9_Perda_zero_sem_preco_e_perda_positiva_indisponivel_preservam_custo_conhecido()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto perdas incompletas"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var conhecido = await CriarInsumoAsync(1, Nome("Insumo conhecido"), null, UnidadeMedida.Grama, true);
        var perdaZero = await CriarInsumoAsync(1, Nome("Insumo perda zero"), null, UnidadeMedida.Grama, true);
        var perdaSemPreco = await CriarInsumoAsync(1, Nome("Insumo perda sem preco"), null, UnidadeMedida.Grama, true);
        var itemConhecido = await CriarItemAsync(1, fichaId, conhecido, 2m, null);
        var itemZero = await CriarItemAsync(1, fichaId, perdaZero, 1m, null);
        var itemIndisponivel = await CriarItemAsync(1, fichaId, perdaSemPreco, 1m, null);
        await CriarPrecoAsync(1, conhecido);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        await EnviarEdicaoItemAsync(client, produtoId, itemConhecido, "2", null, percentualPerda: "10");
        await EnviarEdicaoItemAsync(client, produtoId, itemZero, "1", null, percentualPerda: "0");
        await EnviarEdicaoItemAsync(client, produtoId, itemIndisponivel, "1", null, percentualPerda: "10");
        var pagina = await client.GetStringAsync($"/Produtos/FichaTecnica/{produtoId}");

        Assert.Matches(new Regex($"{Regex.Escape((await ObterInsumoAsync(conhecido, 1)).Nome)}.*?<td>2</td>", RegexOptions.Singleline), pagina);
        Assert.Matches(new Regex($"{Regex.Escape((await ObterInsumoAsync(perdaZero, 1)).Nome)}.*?<td>0%</td>.*?<td>0</td>", RegexOptions.Singleline), pagina);
        Assert.Matches(new Regex($"{Regex.Escape((await ObterInsumoAsync(perdaSemPreco, 1)).Nome)}.*?<td>10%</td>.*?<td>(?:—|&#x2014;)</td>", RegexOptions.Singleline), pagina);
        Assert.Contains("Há perda(s) sem custo base determinável.", pagina);
    }

    [Fact]
    public async Task UC019_W10_Ficha_vazia_mantem_custo_base_indisponivel_e_perdas_zero()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto ficha vazia"), true);
        await CriarFichaAsync(1, produtoId);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var pagina = await client.GetStringAsync($"/Produtos/FichaTecnica/{produtoId}");

        Assert.Matches(new Regex("Custo base dos itens:</strong>\\s*indisponível", RegexOptions.Singleline), pagina);
        Assert.Matches(new Regex("Custo de perdas do lote:</strong>\\s*0", RegexOptions.Singleline), pagina);
    }

    [Fact]
    public async Task UC019_W11_W12_Categoria_e_situacao_inativa_nao_bloqueiam_perda_de_item_existente()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto inativo perdas"), false);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo embalagem inativo"), null, UnidadeMedida.Unidade, false, CategoriaInsumo.Embalagem);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 2m, null);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var resposta = await EnviarEdicaoItemAsync(client, produtoId, itemId, "2", null, percentualPerda: "15");

        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Equal(0.15m, (await ObterItemAsync(itemId, 1)).PercentualPerda);
        Assert.Contains("15%", await client.GetStringAsync($"/Produtos/FichaTecnica/{produtoId}"));
    }

    [Fact]
    public async Task UC019_W14_Post_invalido_da_base_reusa_perda_persistida_sem_mutar_item()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto base invalida perdas"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo base invalida perdas"), null, UnidadeMedida.Grama, true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 2m, "persistido");
        await CriarPrecoAsync(1, insumoId);
        using var client = await web.CriarClienteAutenticadoAsync(1);
        await EnviarEdicaoItemAsync(client, produtoId, itemId, "2", "persistido", percentualPerda: "10");

        var resposta = await EnviarFichaBaseAsync(client, produtoId, "0", "30");
        var pagina = await resposta.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Matches(new Regex("Custo de perdas do lote:</strong>\\s*2", RegexOptions.Singleline), pagina);
        var item = await ObterItemAsync(itemId, 1);
        Assert.Equal((2m, "persistido", 0.10m), (item.Quantidade, item.Observacao, item.PercentualPerda));
    }

    [Fact]
    public async Task UC019_W15_Get_nao_muta_percentual_ou_persiste_custos()
    {
        var produtoId = await CriarProdutoAsync(1, Nome("Produto get perdas"), true);
        var fichaId = await CriarFichaAsync(1, produtoId);
        var insumoId = await CriarInsumoAsync(1, Nome("Insumo get perdas"), null, UnidadeMedida.Grama, true);
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 2m, "original");
        using var client = await web.CriarClienteAutenticadoAsync(1);
        await EnviarEdicaoItemAsync(client, produtoId, itemId, "2", "original", percentualPerda: "12.3456");
        var antes = await ObterItemAsync(itemId, 1);

        var resposta = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}");
        var depois = await ObterItemAsync(itemId, 1);

        Assert.True(resposta.IsSuccessStatusCode);
        Assert.Equal((antes.Quantidade, antes.Observacao, antes.PercentualPerda), (depois.Quantidade, depois.Observacao, depois.PercentualPerda));
    }

    private static async Task<HttpResponseMessage> EnviarRemocaoAsync(
        HttpClient client,
        int produtoId,
        int itemId,
        Dictionary<string, string>? camposExtras = null,
        int? tokenProdutoId = null,
        int? tokenItemId = null)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/FichaTecnica/{tokenProdutoId ?? produtoId}/Itens/Remover/{tokenItemId ?? itemId}");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina)
        };
        if (camposExtras is not null)
        {
            foreach (var campo in camposExtras)
            {
                dados[campo.Key] = campo.Value;
            }
        }

        return await client.PostAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Remover/{itemId}", new FormUrlEncodedContent(dados));
    }

    private static async Task<HttpResponseMessage> EnviarFormularioAsync(
        HttpClient client,
        int produtoId,
        int insumoId,
        string? quantidade,
        string? observacao,
        Dictionary<string, string>? camposExtras = null,
        int? tokenProdutoId = null,
        string? percentualPerda = null)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/FichaTecnica/{tokenProdutoId ?? produtoId}/Itens/Novo");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina),
            ["Input.InsumoId"] = insumoId.ToString(CultureInfo.InvariantCulture),
            ["Input.Observacao"] = observacao ?? string.Empty
        };

        if (quantidade is not null)
        {
            dados["Input.Quantidade"] = quantidade;
        }
        if (percentualPerda is not null)
        {
            dados["Input.PercentualPerda"] = percentualPerda;
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

    private static async Task<HttpResponseMessage> EnviarEdicaoItemAsync(
        HttpClient client,
        int produtoId,
        int itemId,
        string? quantidade,
        string? observacao,
        Dictionary<string, string>? camposExtras = null,
        int? tokenProdutoId = null,
        int? tokenItemId = null,
        string? percentualPerda = null)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/FichaTecnica/{tokenProdutoId ?? produtoId}/Itens/Editar/{tokenItemId ?? itemId}");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina),
            ["Input.Observacao"] = observacao ?? string.Empty
        };

        if (quantidade is not null)
        {
            dados["Input.Quantidade"] = quantidade;
        }
        if (percentualPerda is not null)
        {
            dados["Input.PercentualPerda"] = percentualPerda;
        }

        if (camposExtras is not null)
        {
            foreach (var campo in camposExtras)
            {
                dados[campo.Key] = campo.Value;
            }
        }

        return await client.PostAsync($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}", new FormUrlEncodedContent(dados));
    }

    private static async Task<HttpResponseMessage> EnviarFichaBaseAsync(
        HttpClient client,
        int produtoId,
        string? rendimento,
        string? tempoAtivo)
    {
        var respostaPagina = await client.GetAsync($"/Produtos/FichaTecnica/{produtoId}");
        var pagina = await respostaPagina.Content.ReadAsStringAsync();
        Assert.True(respostaPagina.IsSuccessStatusCode, pagina);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina)
        };

        if (rendimento is not null)
        {
            dados["Input.Rendimento"] = rendimento;
        }

        if (tempoAtivo is not null)
        {
            dados["Input.TempoAtivoMinutos"] = tempoAtivo;
        }

        return await client.PostAsync($"/Produtos/FichaTecnica/{produtoId}", new FormUrlEncodedContent(dados));
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
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina),
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
        (await context.Insumos.SingleAsync(insumo => insumo.Id == insumoId)).ConsolidarIdentidade();
        await context.SaveChangesAsync();
        return item.Id;
    }

    private async Task CriarPrecoAsync(int empresaId, int insumoId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        context.PrecosInsumos.Add(PrecoInsumo.Criar(empresaId, insumoId, 1m, 10m, new DateOnly(2026, 9, 13)));
        (await context.Insumos.SingleAsync(insumo => insumo.Id == insumoId)).ConsolidarIdentidade();
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

    private async Task<Precificador.Core.FichasTecnicas.FichaTecnica> ObterFichaAsync(int fichaId, int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.FichasTecnicas.AsNoTracking().SingleAsync(ficha => ficha.Id == fichaId);
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
    private static string ObterTag(string html, string tag, string nomeCampo) =>
        Regex.Match(html, $"<{tag}[^>]*name=\"{Regex.Escape(nomeCampo)}\"[^>]*>", RegexOptions.IgnoreCase).Value;

    private static string ValorDoInput(string conteudo, string nome)
    {
        var input = Regex.Match(conteudo, $"<input[^>]*name=\"{Regex.Escape(nome)}\"[^>]*>").Value;
        Assert.False(string.IsNullOrEmpty(input), conteudo);
        return Regex.Match(input, "value=\"([^\"]*)\"").Groups[1].Value;
    }
    private static string Nome(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";
}
