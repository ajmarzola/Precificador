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
        var itemId = await CriarItemAsync(1, fichaId, insumoId, 1.25m, "nao deve aparecer");
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
        Assert.Contains($"{insumoNome} — Marca propria", pagina);
        Assert.Contains("1,25", pagina);
        Assert.Contains("m", pagina);
        Assert.Contains("Ativo", pagina);
        Assert.Contains($"/Produtos/FichaTecnica/{produtoId}/Itens/Editar/{itemId}", pagina);
        Assert.DoesNotContain(outroInsumoNome, pagina);
        Assert.DoesNotContain(insumoOutroTenantNome, pagina);
        Assert.DoesNotContain("nao deve aparecer", pagina);
        Assert.DoesNotContain("Custo", pagina);
        Assert.DoesNotContain("Preço", pagina);
        Assert.DoesNotContain("Total", pagina);
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
            ["__RequestVerificationToken"] = WebTestHtml.ExtrairTokenAntiforgery(pagina),
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

    private static async Task<HttpResponseMessage> EnviarEdicaoItemAsync(
        HttpClient client,
        int produtoId,
        int itemId,
        string? quantidade,
        string? observacao,
        Dictionary<string, string>? camposExtras = null,
        int? tokenProdutoId = null,
        int? tokenItemId = null)
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
