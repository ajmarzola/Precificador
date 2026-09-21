using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Pages.Produtos;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ConsultaProdutosTests
{
    [Fact]
    public async Task CA02_CA10_Query_filter_isola_produtos_e_ordena_por_nome_normalizado()
    {
        var opcoes = await CriarOpcoesMigradasAsync(1);
        await CriarProdutosPadraoAsync(opcoes);

        await using var leitura = new PrecificadorDbContext(opcoes, new ContextoEmpresa(1));
        var pagina = new IndexModel(leitura);
        await pagina.OnGetAsync(null);

        Assert.Equal(["Agenda", "Calendário 2027", "Planner"], pagina.Produtos.Select(produto => produto.Nome));
        Assert.Empty(leitura.ChangeTracker.Entries<Produto>());
    }

    [Fact]
    public async Task CA06_CA07_Pesquisa_parcial_por_nome_normaliza_caixa_e_whitespace()
    {
        var opcoes = await CriarOpcoesMigradasAsync(1);
        await CriarProdutosPadraoAsync(opcoes);

        await using var leitura = new PrecificadorDbContext(opcoes, new ContextoEmpresa(1));
        var pagina = new IndexModel(leitura);
        await pagina.OnGetAsync("  caLenDÁRIO  2027  ");

        var produto = Assert.Single(pagina.Produtos);
        Assert.Equal("Calendário 2027", produto.Nome);

        await pagina.OnGetAsync("calendario");
        Assert.Empty(pagina.Produtos);
    }

    [Fact]
    public async Task CA08_Pesquisa_vazia_equivale_a_listagem_completa()
    {
        var opcoes = await CriarOpcoesMigradasAsync(1);
        await CriarProdutosPadraoAsync(opcoes);

        await using var leitura = new PrecificadorDbContext(opcoes, new ContextoEmpresa(1));
        var pagina = new IndexModel(leitura);
        await pagina.OnGetAsync("   ");

        Assert.Equal(3, pagina.Produtos.Count);
        Assert.False(pagina.TemPesquisa);
    }

    [Fact]
    public async Task CA09_Pesquisa_nao_considera_categoria()
    {
        var opcoes = await CriarOpcoesMigradasAsync(1);
        await CriarProdutosPadraoAsync(opcoes);

        await using var leitura = new PrecificadorDbContext(opcoes, new ContextoEmpresa(1));
        var pagina = new IndexModel(leitura);
        await pagina.OnGetAsync("Papelaria");

        Assert.Empty(pagina.Produtos);
    }

    [Fact]
    public async Task CA13_Pesquisa_sem_resultado_retorna_colecao_vazia_sem_erro()
    {
        var opcoes = await CriarOpcoesMigradasAsync(1);
        await CriarProdutosPadraoAsync(opcoes);

        await using var leitura = new PrecificadorDbContext(opcoes, new ContextoEmpresa(1));
        var pagina = new IndexModel(leitura);
        await pagina.OnGetAsync("naoexiste");

        Assert.Empty(pagina.Produtos);
        Assert.True(pagina.TemPesquisa);
    }

    private static async Task<DbContextOptions<PrecificadorDbContext>> CriarOpcoesMigradasAsync(int empresaId)
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ConsultaProdutos");
        var opcoes = new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options;
        await using var context = new PrecificadorDbContext(opcoes, new ContextoEmpresa(empresaId));
        await context.Database.MigrateAsync();
        context.Empresas.Add(Empresa.Criar("Empresa dois"));
        await context.SaveChangesAsync();
        return opcoes;
    }

    private static async Task CriarProdutosPadraoAsync(DbContextOptions<PrecificadorDbContext> opcoes)
    {
        await using (var empresaUm = new PrecificadorDbContext(opcoes, new ContextoEmpresa(1)))
        {
            empresaUm.Produtos.AddRange(
                Produto.Criar(1, "Planner", 0.30m),
                Produto.Criar(1, "Calendário 2027", 0.255m),
                Produto.Criar(1, "Agenda", 0m));
            await empresaUm.SaveChangesAsync();
        }

        await using var empresaDois = new PrecificadorDbContext(opcoes, new ContextoEmpresa(2));
        empresaDois.Produtos.Add(Produto.Criar(2, "Bloco externo", 0.20m));
        await empresaDois.SaveChangesAsync();
    }

    private sealed class ContextoEmpresa(int? empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId ?? -1;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}

