using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class ListarConsultarProdutosPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);
    [Fact]
    public async Task CA01_Area_de_produtos_exige_autenticacao()
    {
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var lista = await client.GetAsync("/Produtos");
        var detalhes = await client.GetAsync("/Produtos/Detalhes/1");

        Assert.Equal(HttpStatusCode.Redirect, lista.StatusCode);
        Assert.Contains("/Conta/Login", lista.Headers.Location!.ToString());
        Assert.Equal(HttpStatusCode.Redirect, detalhes.StatusCode);
        Assert.Contains("/Conta/Login", detalhes.Headers.Location!.ToString());
    }

    [Fact]
    public async Task CA02_CA03_Listagem_exibe_apenas_produtos_do_tenant_com_campos_funcionais()
    {
        var nomeProduto = NomeUnico("Agenda");
        var id = await CriarProdutoAsync(1, nomeProduto, "Planners", 0.30m);
        var outroTenant = NomeUnico("Produto externo");
        await CriarProdutoAsync(await web.CriarEmpresaAsync(), outroTenant, "Externa", 0.20m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos"));
        var linha = LinhaProduto(conteudo, nomeProduto);

        Assert.Contains(nomeProduto, linha);
        Assert.Contains("Planners", linha);
        Assert.Contains("30%", linha);
        Assert.Contains("Ativo", linha);
        Assert.Contains($"/Produtos/Detalhes/{id}", linha);
        Assert.DoesNotContain(outroTenant, conteudo);
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("NomeNormalizado", conteudo);
        Assert.DoesNotContain("Preço de venda", conteudo);
        Assert.Contains("Custo unitário atual", conteudo);
        Assert.Contains("Preço de prateleira vigente", conteudo);
        Assert.Contains("Margem atual", conteudo);
        Assert.DoesNotContain("Ficha Técnica", conteudo);
    }

    [Fact]
    public async Task CA04_Produto_sem_categoria_exibe_traco()
    {
        var nomeProduto = NomeUnico("Produto sem categoria");
        await CriarProdutoAsync(1, nomeProduto, null, 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos"));

        Assert.Contains("—", LinhaProduto(conteudo, nomeProduto));
    }

    [Fact]
    public async Task CA05_Margem_alvo_e_exibida_como_percentual()
    {
        var margemZero = NomeUnico("Margem zero");
        var margemFracionada = NomeUnico("Margem fracionada");
        var margemInteira = NomeUnico("Margem inteira");
        await CriarProdutoAsync(1, margemZero, null, 0m);
        await CriarProdutoAsync(1, margemFracionada, null, 0.255m);
        await CriarProdutoAsync(1, margemInteira, null, 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos"));

        Assert.Contains("0%", LinhaProduto(conteudo, margemZero));
        Assert.Contains("25,5%", LinhaProduto(conteudo, margemFracionada));
        Assert.Contains("30%", LinhaProduto(conteudo, margemInteira));
    }

    [Fact]
    public async Task CA06_CA07_Pesquisa_por_nome_filtra_e_normaliza_consulta()
    {
        var nomeCalendario = NomeUnico("Calendário 2027");
        var nomeAgenda = NomeUnico("Agenda");
        await CriarProdutoAsync(1, nomeCalendario, "Datas", 0.255m);
        await CriarProdutoAsync(1, nomeAgenda, "Planners", 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);
        var termoNormalizado = Uri.EscapeDataString(nomeCalendario.ToLowerInvariant().Replace(" ", "  "));

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos?q=%20%20{termoNormalizado}%20%20"));

        Assert.Contains(nomeCalendario, conteudo);
        Assert.DoesNotContain(nomeAgenda, conteudo);

        var semAcento = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos?q=calendario"));
        Assert.DoesNotContain(nomeCalendario, semAcento);
        Assert.Contains("Nenhum produto encontrado para os filtros informados.", semAcento);
    }

    [Fact]
    public async Task CA08_Pesquisa_vazia_mantem_listagem()
    {
        var nomeProduto = NomeUnico("Produto pesquisa vazia");
        await CriarProdutoAsync(1, nomeProduto, null, 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos?q=%20%20%20"));

        Assert.Contains(nomeProduto, conteudo);
        Assert.DoesNotContain("Nenhum produto encontrado para os filtros informados.", conteudo);
    }

    [Fact]
    public async Task CA09_Termo_presente_apenas_na_categoria_nao_filtra_produto()
    {
        var nomeProduto = NomeUnico("Produto por nome");
        await CriarProdutoAsync(1, nomeProduto, "Categoria Exclusiva Busca", 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos?q=Categoria%20Exclusiva%20Busca"));

        Assert.DoesNotContain(nomeProduto, conteudo);
        Assert.Contains("Nenhum produto encontrado para os filtros informados.", conteudo);
    }

    [Fact]
    public async Task CA12_Empresa_sem_produtos_exibe_estado_vazio_e_link_de_cadastro()
    {
        var empresaSemProdutos = await web.CriarEmpresaAsync();
        using var client = await web.CriarClienteAutenticadoAsync(empresaSemProdutos);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos"));

        Assert.Contains("Não há produtos cadastrados para a empresa ativa.", conteudo);
        Assert.Contains("Cadastrar produto", conteudo);
        Assert.Contains("href=\"/Produtos/Novo\"", conteudo);
    }

    [Fact]
    public async Task CA13_Pesquisa_sem_resultado_exibe_estado_apropriado()
    {
        await CriarProdutoAsync(1, NomeUnico("Produto existente"), null, 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos?q=naoexiste"));

        Assert.Contains("Nenhum produto encontrado para os filtros informados.", conteudo);
        Assert.Contains("Limpar filtros", conteudo);
        Assert.Contains("name=\"q\"", conteudo);
    }

    [Fact]
    public async Task MEL024_Filtro_com_resultado_preserva_selecao_e_exibe_limpar()
    {
        var nome = NomeUnico("Produto filtrado");
        await CriarProdutoAsync(1, nome, "Categoria filtrável", .30m);
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
        var categoriaId = await context.CategoriasProdutos.Where(item => item.Nome == "Categoria filtrável").Select(item => item.Id).SingleAsync();
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos?categoria={categoriaId}"));

        Assert.Contains(nome, conteudo);
        Assert.Contains("Limpar filtros", conteudo);
        Assert.Matches($"<option\\b(?=[^>]*\\bvalue=\"{categoriaId}\")(?=[^>]*\\bselected(?:=\"selected\")?)[^>]*>", conteudo);
    }

    [Fact]
    public async Task MEL024_Grid_exibe_indicadores_calculados_e_nao_persiste_get()
    {
        var completo = await CriarProdutoPrecificavelAsync("Completo", 10m, 20m, ativo: true);
        var inativo = await CriarProdutoPrecificavelAsync("Inativo", 10m, 20m, ativo: false);
        var negativo = await CriarProdutoPrecificavelAsync("Negativo", 10m, 5m, ativo: true);
        var semFicha = await CriarProdutoComPrecoSemFichaAsync("Sem ficha", 20m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos"));

        Assert.Contains("R$ 10,00", LinhaProduto(conteudo, completo.Nome));
        Assert.Contains("50%", LinhaProduto(conteudo, completo.Nome));
        Assert.Contains("R$ 10,00", LinhaProduto(conteudo, inativo.Nome));
        Assert.Contains("Inativo", LinhaProduto(conteudo, inativo.Nome));
        Assert.Contains("-100%", LinhaProduto(conteudo, negativo.Nome));
        Assert.Contains("R$ 20,00", LinhaProduto(conteudo, semFicha.Nome));
        Assert.Contains("indisponível", LinhaProduto(conteudo, semFicha.Nome));

        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var verificacao = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
        Assert.Equal(0, await verificacao.SaveChangesAsync());
    }

    [Fact]
    public async Task CA14_CA15_Detalhes_exibem_somente_campos_permitidos()
    {
        var nomeProduto = NomeUnico("Produto detalhes");
        var id = await CriarProdutoAsync(1, nomeProduto, "Catálogo", 0.255m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync($"/Produtos/Detalhes/{id}");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Contains("Nome", conteudo);
        Assert.Contains(nomeProduto, conteudo);
        Assert.Contains("Categoria", conteudo);
        Assert.Contains("Catálogo", conteudo);
        Assert.Contains("Margem-alvo", conteudo);
        Assert.Contains("25,5%", conteudo);
        Assert.Contains("Situação", conteudo);
        Assert.Contains("Ativo", conteudo);
        Assert.Contains("Voltar para produtos", conteudo);
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("NomeNormalizado", conteudo);
        Assert.DoesNotContain("Preço de venda", conteudo);
        Assert.DoesNotContain("Histórico de venda", conteudo);
        Assert.DoesNotContain("Preço teórico", conteudo);
        Assert.DoesNotContain("Preço sugerido", conteudo);
        Assert.DoesNotContain("Ficha Técnica", conteudo);
    }

    [Fact]
    public async Task CA16_Detalhes_de_id_inexistente_retorna_404()
    {
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync("/Produtos/Detalhes/999999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CA17_Detalhes_cross_tenant_retorna_404()
    {
        var empresaDois = await web.CriarEmpresaAsync();
        var idOutroTenant = await CriarProdutoAsync(empresaDois, NomeUnico("Produto outro tenant"), null, 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync($"/Produtos/Detalhes/{idOutroTenant}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CA11_Listagem_e_detalhes_apresentam_situacao()
    {
        var nomeProduto = NomeUnico("Produto ativo");
        var id = await CriarProdutoAsync(1, nomeProduto, null, 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var lista = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos"));
        var detalhes = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{id}"));

        Assert.Contains("Ativo", LinhaProduto(lista, nomeProduto));
        Assert.Contains("Situação", detalhes);
        Assert.Contains("Ativo", detalhes);
    }

    [Fact]
    public async Task Navegacao_principal_aponta_para_produtos_e_lista_navega_para_cadastro_e_detalhes()
    {
        var nomeProduto = NomeUnico("Produto navegacao");
        var id = await CriarProdutoAsync(1, nomeProduto, null, 0.30m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var lista = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Produtos"));
        var detalhes = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync($"/Produtos/Detalhes/{id}"));

        Assert.Contains("Produtos", lista);
        Assert.Contains("href=\"/Produtos\"", lista);
        Assert.Contains("Cadastrar produto", lista);
        Assert.Contains("href=\"/Produtos/Novo\"", lista);
        Assert.Contains($"/Produtos/Detalhes/{id}", LinhaProduto(lista, nomeProduto));
        Assert.Contains("Voltar para produtos", detalhes);
        Assert.Contains("href=\"/Produtos\"", detalhes);
        Assert.DoesNotContain("Editar", lista);
    }

    private async Task<int> CriarProdutoAsync(int empresaId, string nome, string? categoria, decimal margemAlvo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        int? categoriaId = categoria is null ? null : await ObterOuCriarCategoriaIdAsync(context, empresaId, categoria);

        var produto = Produto.Criar(empresaId, nome, margemAlvo, categoriaId);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
    }

    private async Task<(int Id, string Nome)> CriarProdutoPrecificavelAsync(string prefixo, decimal custo, decimal preco, bool ativo)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(0m, 0m, null, null, .10m);
        var nome = NomeUnico(prefixo);
        var produto = Produto.Criar(1, nome, .30m); if (!ativo) produto.Desativar();
        context.Produtos.Add(produto); await context.SaveChangesAsync();
        var ficha = FichaTecnica.Criar(1, produto.Id, 1m);
        var insumo = Insumo.Criar(1, NomeUnico("Insumo"), CategoriaInsumo.MateriaPrima, UnidadeMedida.Unidade);
        context.FichasTecnicas.Add(ficha); context.Insumos.Add(insumo); await context.SaveChangesAsync();
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, ficha.Id, insumo.Id, 1m));
        context.PrecosInsumos.Add(PrecoInsumo.Criar(1, insumo.Id, 1m, custo, new DateOnly(2026, 9, 15)));
        context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(1, produto.Id, new DateOnly(2026, 9, 15), custo, .30m, custo, preco, .10m));
        await context.SaveChangesAsync();
        return (produto.Id, nome);
    }

    private async Task<(int Id, string Nome)> CriarProdutoComPrecoSemFichaAsync(string prefixo, decimal preco)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(1));
        var nome = NomeUnico(prefixo); var produto = Produto.Criar(1, nome, .30m);
        context.Produtos.Add(produto); await context.SaveChangesAsync();
        context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(1, produto.Id, new DateOnly(2026, 9, 15), 10m, .30m, 10m, preco, .10m));
        await context.SaveChangesAsync();
        return (produto.Id, nome);
    }

    private static async Task<int> ObterOuCriarCategoriaIdAsync(PrecificadorDbContext context, int empresaId, string nome)
    {
        var nomeNormalizado = Regex.Replace(nome.Trim(), @"\s+", " ").ToUpperInvariant();
        var existente = await context.CategoriasProdutos.IgnoreQueryFilters()
            .FirstOrDefaultAsync(categoria => categoria.EmpresaId == empresaId && categoria.NomeNormalizado == nomeNormalizado);
        if (existente is not null)
        {
            return existente.Id;
        }

        var categoriaNova = CategoriaProduto.Criar(empresaId, nome, FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 0m);
        context.CategoriasProdutos.Add(categoriaNova);
        await context.SaveChangesAsync();
        return categoriaNova.Id;
    }
    private static string LinhaProduto(string conteudo, string nome)
    {
        var match = Regex.Match(conteudo, $"<tr>.*?{Regex.Escape(nome)}.*?</tr>", RegexOptions.Singleline);
        Assert.True(match.Success, conteudo);
        return match.Value;
    }

    private static string NomeUnico(string prefixo) => $"{prefixo} {Guid.NewGuid():N}";
}
