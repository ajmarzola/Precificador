using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ProdutoPersistenceTests
{
    private const string MigrationAnteriorProdutos = "20260912215219_AddEmpresaTimeZone";

    [Fact]
    public async Task CA18_Migration_cria_produtos_e_preserva_dados_existentes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync(MigrationAnteriorProdutos);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Insumos (EmpresaId, Nome, NomeNormalizado, MarcaNormalizada, Categoria, UnidadeBase, Ativo) VALUES (1, 'Farinha', 'FARINHA', '', 1, 1, 1)");

        await context.Database.MigrateAsync();

        var tabelas = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToListAsync();
        var indices = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'index'").ToListAsync();
        Assert.Contains("Produtos", tabelas);
        Assert.Contains("IX_Produtos_EmpresaId_NomeNormalizado", indices);
        Assert.Equal("Farinha", (await context.Insumos.SingleAsync()).Nome);
    }

    [Fact]
    public async Task CA03_Produto_valido_persiste_e_e_recuperado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();

        context.Produtos.Add(Produto.Criar(1, "  Agenda   2027 ", 0.30m, "  Planners  "));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var produto = await context.Produtos.SingleAsync();
        Assert.Equal("Agenda 2027", produto.Nome);
        Assert.Equal("AGENDA 2027", produto.NomeNormalizado);
        Assert.Equal("Planners", produto.Categoria);
        Assert.Equal(0.30m, produto.MargemAlvo);
        Assert.True(produto.Ativo);
    }

    [Fact]
    public async Task CA10_Indice_unico_rejeita_nome_normalizado_duplicado_na_mesma_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();

        context.Produtos.Add(Produto.Criar(1, "Agenda", 0.30m));
        await context.SaveChangesAsync();
        context.Produtos.Add(Produto.Criar(1, " agenda ", 0.25m, "Outra categoria"));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA09_Mesmo_nome_normalizado_e_permitido_em_empresas_diferentes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var context = CriarContexto(connection, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            context.Produtos.Add(Produto.Criar(1, "Agenda", 0.30m));
            await context.SaveChangesAsync();
        }

        await using var contextoEmpresaDois = CriarContexto(connection, 2);
        contextoEmpresaDois.Produtos.Add(Produto.Criar(2, " agenda ", 0.25m));

        await contextoEmpresaDois.SaveChangesAsync();
        Assert.Single(await contextoEmpresaDois.Produtos.ToListAsync());
    }

    [Fact]
    public async Task CA12_Query_filter_isola_produtos_por_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var context = CriarContexto(connection, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            context.Produtos.Add(Produto.Criar(1, "Agenda A", 0.30m));
            await context.SaveChangesAsync();
        }
        await using (var context = CriarContexto(connection, 2))
        {
            context.Produtos.Add(Produto.Criar(2, "Agenda B", 0.30m));
            await context.SaveChangesAsync();
        }

        await using var leituraEmpresaUm = CriarContexto(connection, 1);

        var produto = await leituraEmpresaUm.Produtos.SingleAsync();
        Assert.Equal("Agenda A", produto.Nome);
    }

    [Fact]
    public async Task CA13_Guard_rejeita_escrita_de_produto_para_outra_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();

        context.Produtos.Add(Produto.Criar(2, "Agenda", 0.30m));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA18_Fk_rejeita_empresa_inexistente()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 999);
        await context.Database.MigrateAsync();

        context.Produtos.Add(Produto.Criar(999, "Agenda", 0.30m));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var sqliteException = Assert.IsType<SqliteException>(exception.InnerException);
        Assert.Equal(19, sqliteException.SqliteErrorCode);
        Assert.Equal(787, sqliteException.SqliteExtendedErrorCode);
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
