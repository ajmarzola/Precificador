using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ProdutoColecaoPersistenceTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Migration_clean_e_upgrade_preservam_dados_e_schema(bool upgrade)
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("ProdutoColecaoMigration");
        await using var db = CriarContexto(cs, 1);
        if (upgrade)
        {
            await db.Database.MigrateAsync("20261001232108_AddColecaoProduto");
            var categoria = CategoriaProduto.Criar(1, "Legada", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 12.34m);
            db.Add(categoria); await db.SaveChangesAsync();
            var produto = Produto.Criar(1, "Legado", .25m, categoria.Id);
            db.Produtos.Add(produto);
            db.ColecoesProdutos.Add(ColecaoProduto.Criar(1, "Legada", new(2020, 1, 1), new(2020, 12, 31)));
            await db.SaveChangesAsync();
            db.Add(RegistroPrecoProduto.Criar(1, produto.Id, new(2020, 1, 1), 12.34m, .25m, 17m, 18m, .1m));
            await db.SaveChangesAsync();
        }
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Empty(await db.ProdutosColecoes.ToListAsync());
        if (upgrade)
        {
            Assert.Equal("Legada", (await db.CategoriasProdutos.SingleAsync()).Nome);
            Assert.Equal(18m, (await db.RegistrosPrecosProdutos.SingleAsync()).PrecoPrateleira);
            Assert.Equal(.25m, (await db.Produtos.SingleAsync()).MargemAlvo);
            Assert.Equal(new DateOnly(2020, 12, 31), (await db.ColecoesProdutos.SingleAsync()).DataFinalizacao);
        }
        var campos = await db.Database.SqlQueryRaw<string>("SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ProdutosColecoes'").ToListAsync();
        Assert.Equal(new[] { "ColecaoProdutoId", "Destaque", "EmpresaId", "ProdutoId" }, campos.Order().ToArray());
        Assert.Equal(1, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ProdutosColecoes' AND COLUMN_NAME = 'Destaque' AND DATA_TYPE = 'bit' AND IS_NULLABLE = 'NO'").SingleAsync());
        Assert.Equal(3, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID('ProdutosColecoes') AND delete_referential_action = 0").SingleAsync());
        var indices = await db.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.indexes WHERE object_id = OBJECT_ID('ProdutosColecoes')").ToListAsync();
        Assert.Contains("PK_ProdutosColecoes", indices);
        Assert.Contains("IX_ProdutosColecoes_EmpresaId_ColecaoProdutoId_Destaque", indices);
        Assert.Contains("IX_ProdutosColecoes_EmpresaId_ProdutoId", indices);
        var chave = await db.Database.SqlQueryRaw<string>("SELECT c.name AS Value FROM sys.indexes i JOIN sys.index_columns ic ON ic.object_id=i.object_id AND ic.index_id=i.index_id JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE i.object_id=OBJECT_ID('ProdutosColecoes') AND i.is_primary_key=1 ORDER BY ic.key_ordinal").ToListAsync();
        Assert.Equal(new[] { "ProdutoId", "ColecaoProdutoId" }, chave);
        Assert.Equal(0, await db.Database.SqlQueryRaw<int>("SELECT COUNT(*) AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME='Produtos' AND COLUMN_NAME='ColecaoProdutoId'").SingleAsync());
    }

    [Fact]
    public async Task NN_unicidade_restrict_e_guard_real_sync_async()
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("ProdutoColecaoGuard");
        await using var db = CriarContexto(cs, 1);
        await db.Database.MigrateAsync();
        var empresa = Empresa.Criar("Outra");
        db.Empresas.Add(empresa);
        await db.SaveChangesAsync();
        var p1 = Produto.Criar(1, "Primeiro", .2m);
        var p2 = Produto.Criar(1, "Segundo", .3m); p2.Desativar();
        var c1 = ColecaoProduto.Criar(1, "Passada", new(2020, 1, 1), new(2020, 1, 2));
        var c2 = ColecaoProduto.Criar(1, "Aberta", new(2020, 1, 1), null);
        db.AddRange(p1, p2, c1, c2); await db.SaveChangesAsync();
        db.AddRange(ProdutoColecao.Criar(1, p1.Id, c1.Id, true), ProdutoColecao.Criar(1, p1.Id, c2.Id, true), ProdutoColecao.Criar(1, p2.Id, c1.Id, true));
        await db.SaveChangesAsync();
        Assert.Equal(3, await db.ProdutosColecoes.CountAsync());
        await using var outro = CriarContexto(cs, empresa.Id);
        var alheio = Produto.Criar(empresa.Id, "Alheio", .2m);
        var alheia = ColecaoProduto.Criar(empresa.Id, "Alheia", new(2020, 1, 1), null);
        outro.AddRange(alheio, alheia); await outro.SaveChangesAsync();
        Assert.Empty(await outro.ProdutosColecoes.ToListAsync());
        await using var semEmpresa = CriarContexto(cs, null);
        Assert.Empty(await semEmpresa.ProdutosColecoes.ToListAsync());
        semEmpresa.Add(ProdutoColecao.Criar(1, p2.Id, c2.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => semEmpresa.SaveChangesAsync());
        foreach (var sincrono in new[] { true, false })
        foreach (var estado in new[] { EntityState.Added, EntityState.Modified, EntityState.Deleted })
        foreach (var ids in new[] { (alheio.Id, c1.Id), (p1.Id, alheia.Id), (999999, c1.Id), (p1.Id, 999999) })
        {
            db.ChangeTracker.Clear();
            var falso = ProdutoColecao.Criar(1, ids.Item1, ids.Item2);
            db.Entry(falso).State = estado;
            if (sincrono) Assert.Throws<InvalidOperationException>(() => db.SaveChanges());
            else await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        }
        db.ChangeTracker.Clear();
        db.Add(ProdutoColecao.Criar(empresa.Id, alheio.Id, alheia.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.Add(ProdutoColecao.Criar(1, p1.Id, c1.Id));
        var duplicado = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.Equal(2627, Assert.IsType<SqlException>(duplicado.InnerException).Number);
        db.ChangeTracker.Clear();
        await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Produtos WHERE Id={p1.Id}"));
        await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ColecoesProdutos WHERE Id={c1.Id}"));
        var vinculo = await db.ProdutosColecoes.SingleAsync(x => x.ProdutoId == p2.Id);
        db.Remove(vinculo); await db.SaveChangesAsync();
        Assert.Equal(2, await db.Produtos.CountAsync());
        Assert.Equal(2, await db.ColecoesProdutos.CountAsync());
    }

    private static PrecificadorDbContext CriarContexto(string cs, int? empresa) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(cs).Options, new Contexto(empresa));
    private sealed class Contexto(int? empresa) : IEmpresaContext
    {
        public int? EmpresaId => empresa;
        public int EmpresaIdOuSentinela => empresa ?? -1;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
