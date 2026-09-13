using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class FichaTecnicaPersistenceTests
{
    private const string MigrationAnteriorFichas = "20260912230859_AddProdutos";

    [Fact]
    public async Task CA21_Migration_cria_fichas_e_preserva_produtos_existentes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync(MigrationAnteriorFichas);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Produtos (EmpresaId, Nome, NomeNormalizado, MargemAlvo, Ativo) VALUES (1, 'Agenda', 'AGENDA', '0.30', 1)");

        await context.Database.MigrateAsync();

        var tabelas = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToListAsync();
        var indices = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'index'").ToListAsync();
        Assert.Contains("FichasTecnicas", tabelas);
        Assert.Contains("IX_FichasTecnicas_EmpresaId_ProdutoId", indices);
        Assert.Equal("Agenda", (await context.Produtos.SingleAsync()).Nome);
        Assert.Empty(await context.FichasTecnicas.ToListAsync());
    }

    [Fact]
    public async Task CA10_Indice_unico_rejeita_segunda_ficha_do_mesmo_produto()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();
        var produto = Produto.Criar(1, "Agenda", 0.30m);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();

        context.FichasTecnicas.Add(FichaTecnica.Criar(1, produto.Id, 2m, 30));
        await context.SaveChangesAsync();
        context.FichasTecnicas.Add(FichaTecnica.Criar(1, produto.Id, 3m, 40));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA12_Query_filter_isola_fichas_por_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        int produtoEmpresaUm;
        await using (var context = CriarContexto(connection, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            var produto = Produto.Criar(1, "Agenda A", 0.30m);
            context.Produtos.Add(produto);
            await context.SaveChangesAsync();
            produtoEmpresaUm = produto.Id;
            context.FichasTecnicas.Add(FichaTecnica.Criar(1, produto.Id, 2m, 30));
            await context.SaveChangesAsync();
        }

        await using (var context = CriarContexto(connection, 2))
        {
            var produto = Produto.Criar(2, "Agenda B", 0.30m);
            context.Produtos.Add(produto);
            await context.SaveChangesAsync();
            context.FichasTecnicas.Add(FichaTecnica.Criar(2, produto.Id, 4m, 60));
            await context.SaveChangesAsync();
        }

        await using var leituraEmpresaUm = CriarContexto(connection, 1);

        var ficha = await leituraEmpresaUm.FichasTecnicas.SingleAsync();
        Assert.Equal(produtoEmpresaUm, ficha.ProdutoId);
        Assert.Equal(1, ficha.EmpresaId);
    }

    [Fact]
    public async Task CA11_Guard_rejeita_ficha_referenciando_produto_de_outra_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        int produtoEmpresaDois;
        await using (var context = CriarContexto(connection, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            await context.SaveChangesAsync();
        }

        await using (var context = CriarContexto(connection, 2))
        {
            var produto = Produto.Criar(2, "Agenda externa", 0.30m);
            context.Produtos.Add(produto);
            await context.SaveChangesAsync();
            produtoEmpresaDois = produto.Id;
        }

        await using var contextoEmpresaUm = CriarContexto(connection, 1);
        contextoEmpresaUm.FichasTecnicas.Add(FichaTecnica.Criar(1, produtoEmpresaDois, 2m, 30));

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task CA21_Fks_restrict_preservam_coerencia_de_empresa_e_produto()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();
        var produto = Produto.Criar(1, "Agenda", 0.30m);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        context.FichasTecnicas.Add(FichaTecnica.Criar(1, produto.Id, 2m, 30));
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var produtoPersistido = await context.Produtos.SingleAsync();
        context.Produtos.Remove(produtoPersistido);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA08_Atualizacao_round_trip_preserva_a_mesma_ficha()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();
        var produto = Produto.Criar(1, "Agenda", 0.30m);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        var ficha = FichaTecnica.Criar(1, produto.Id, 2m, 30);
        context.FichasTecnicas.Add(ficha);
        await context.SaveChangesAsync();
        var fichaId = ficha.Id;
        context.ChangeTracker.Clear();

        var existente = await context.FichasTecnicas.SingleAsync();
        existente.AtualizarBase(2.5m, 45);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var atualizada = await context.FichasTecnicas.SingleAsync();
        Assert.Equal(fichaId, atualizada.Id);
        Assert.Equal(1, atualizada.EmpresaId);
        Assert.Equal(produto.Id, atualizada.ProdutoId);
        Assert.Equal(2.5m, atualizada.Rendimento);
        Assert.Equal(45, atualizada.TempoAtivoMinutos);
    }

    private static PrecificadorDbContext CriarContexto(SqliteConnection connection, int empresaId) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite(connection).Options,
        new ContextoEmpresa(empresaId));

    private sealed class ContextoEmpresa(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
