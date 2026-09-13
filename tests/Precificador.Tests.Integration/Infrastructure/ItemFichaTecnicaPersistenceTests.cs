using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ItemFichaTecnicaPersistenceTests
{
    private const string MigrationAnteriorItens = "20260913215922_AddFichasTecnicas";

    [Fact]
    public async Task P1_Migration_cria_itens_e_preserva_dados_existentes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync(MigrationAnteriorItens);
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Produtos (EmpresaId, Nome, NomeNormalizado, MargemAlvo, Ativo) VALUES (1, 'Agenda', 'AGENDA', '0.30', 1)");
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO Insumos (EmpresaId, Nome, NomeNormalizado, MarcaNormalizada, Categoria, UnidadeBase, Ativo) VALUES (1, 'Papel', 'PAPEL', '', 1, 4, 1)");

        await context.Database.MigrateAsync();

        var tabelas = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToListAsync();
        var indices = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'index'").ToListAsync();
        Assert.Contains("ItensFichaTecnica", tabelas);
        Assert.Contains("IX_ItensFichaTecnica_EmpresaId_FichaTecnicaId_InsumoId", indices);
        Assert.Equal("Agenda", (await context.Produtos.SingleAsync()).Nome);
        Assert.Equal("Papel", (await context.Insumos.SingleAsync()).Nome);
        Assert.Empty(await context.ItensFichaTecnica.ToListAsync());
    }

    [Fact]
    public async Task P2_Indice_unico_rejeita_mesmo_insumo_duas_vezes_na_mesma_ficha()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);

        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m));
        await context.SaveChangesAsync();
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 2m));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task P3_Mesmo_insumo_e_permitido_em_fichas_diferentes()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();
        var insumo = Insumo.Criar(1, "Papel", CategoriaInsumo.MateriaPrima, UnidadeMedida.Metro);
        var produtoUm = Produto.Criar(1, "Agenda A", 0.30m);
        var produtoDois = Produto.Criar(1, "Agenda B", 0.30m);
        context.Insumos.Add(insumo);
        context.Produtos.AddRange(produtoUm, produtoDois);
        await context.SaveChangesAsync();
        var fichaUm = FichaTecnica.Criar(1, produtoUm.Id, 2m, 30);
        var fichaDois = FichaTecnica.Criar(1, produtoDois.Id, 3m, 40);
        context.FichasTecnicas.AddRange(fichaUm, fichaDois);
        await context.SaveChangesAsync();

        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaUm.Id, insumo.Id, 1m));
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaDois.Id, insumo.Id, 2m));
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.ItensFichaTecnica.CountAsync());
    }

    [Fact]
    public async Task P4_Query_filter_isola_itens_por_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var context = CriarContexto(connection, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            await context.SaveChangesAsync();
            var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
            context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m));
            await context.SaveChangesAsync();
        }

        await using (var context = CriarContexto(connection, 2))
        {
            var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 2);
            context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(2, fichaId, insumoId, 2m));
            await context.SaveChangesAsync();
        }

        await using var leituraEmpresaUm = CriarContexto(connection, 1);

        var item = await leituraEmpresaUm.ItensFichaTecnica.SingleAsync();
        Assert.Equal(1, item.EmpresaId);
        Assert.Equal(1m, item.Quantidade);
    }

    [Fact]
    public async Task P5_Guard_rejeita_item_com_ficha_de_outra_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var context = CriarContexto(connection, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            await context.SaveChangesAsync();
        }

        await using var contextoEmpresaDois = CriarContexto(connection, 2);
        var (_, fichaEmpresaDois, _) = await CriarFichaEInsumoAsync(contextoEmpresaDois, 2);
        await using var contextoEmpresaUm = CriarContexto(connection, 1);
        var (_, _, insumoEmpresaUm) = await CriarFichaEInsumoAsync(contextoEmpresaUm, 1);

        contextoEmpresaUm.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaEmpresaDois, insumoEmpresaUm, 1m));

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task P6_Guard_rejeita_item_com_insumo_de_outra_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using (var context = CriarContexto(connection, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            await context.SaveChangesAsync();
        }

        await using var contextoEmpresaDois = CriarContexto(connection, 2);
        var (_, _, insumoEmpresaDois) = await CriarFichaEInsumoAsync(contextoEmpresaDois, 2);
        await using var contextoEmpresaUm = CriarContexto(connection, 1);
        var (_, fichaEmpresaUm, _) = await CriarFichaEInsumoAsync(contextoEmpresaUm, 1);

        contextoEmpresaUm.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaEmpresaUm, insumoEmpresaDois, 1m));

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task P7_Fks_restrict_preservam_referencias_de_ficha_e_insumo()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m));
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        context.Insumos.Remove(await context.Insumos.SingleAsync(insumo => insumo.Id == insumoId));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.FichasTecnicas.Remove(await context.FichasTecnicas.SingleAsync(ficha => ficha.Id == fichaId));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task<(int ProdutoId, int FichaId, int InsumoId)> CriarFichaEInsumoAsync(PrecificadorDbContext context, int empresaId)
    {
        var produto = Produto.Criar(empresaId, $"Produto {Guid.NewGuid():N}", 0.30m);
        var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        context.Produtos.Add(produto);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        var ficha = FichaTecnica.Criar(empresaId, produto.Id, 2m, 30);
        context.FichasTecnicas.Add(ficha);
        await context.SaveChangesAsync();
        return (produto.Id, ficha.Id, insumo.Id);
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
