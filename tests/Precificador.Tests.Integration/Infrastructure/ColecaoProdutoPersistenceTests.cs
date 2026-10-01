using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ColecaoProdutoPersistenceTests
{
    [Fact]
    public async Task Migration_limpa_cria_schema_e_preserva_dados_no_upgrade()
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("ColecaoMigration");
        await using var db = CriarContexto(cs, 1);
        await db.Database.MigrateAsync("20261001152514_UC039_AdministracaoEmpresas");
        var categoria = CategoriaProduto.Criar(1, "Papelaria", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 0);
        db.CategoriasProdutos.Add(categoria);
        await db.SaveChangesAsync();
        var produto = Produto.Criar(1, "Produto preservado", .2m, categoria.Id);
        db.Produtos.Add(produto);
        await db.SaveChangesAsync();
        await db.Database.MigrateAsync();
        Assert.Equal("Papelaria", (await db.CategoriasProdutos.SingleAsync()).Nome);
        Assert.Equal("Produto preservado", (await db.Produtos.SingleAsync()).Nome);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        var datas = await db.Database.SqlQueryRaw<string>("SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ColecoesProdutos' AND DATA_TYPE = 'date'").ToListAsync();
        Assert.Contains("DataLancamento", datas);
        Assert.Contains("DataFinalizacao", datas);
        await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync("INSERT INTO ColecoesProdutos (EmpresaId, Nome, NomeNormalizado, DataLancamento, DataFinalizacao) VALUES (1, 'X', 'X', '2027-02-02', '2027-02-01')"));
    }

    [Fact]
    public async Task Unicidade_isolamento_e_guard_de_associacao()
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("ColecaoPersistencia");
        await using var db = CriarContexto(cs, 1);
        await db.Database.MigrateAsync();
        var empresa2 = Empresa.Criar("Outra empresa");
        db.Empresas.Add(empresa2);
        await db.SaveChangesAsync();
        var categoria1 = CategoriaProduto.Criar(1, "Cat A", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 0);
        db.CategoriasProdutos.Add(categoria1);
        await db.SaveChangesAsync();
        await using var outro = CriarContexto(cs, empresa2.Id);
        var categoria2 = CategoriaProduto.Criar(empresa2.Id, "Cat B", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 0);
        outro.CategoriasProdutos.Add(categoria2);
        await outro.SaveChangesAsync();

        var data = new DateOnly(2027, 1, 1);
        var colecao = ColecaoProduto.Criar(1, "Natal", data, null);
        db.ColecoesProdutos.Add(colecao);
        db.ColecoesProdutosCategorias.Add(new ColecaoProdutoCategoria(colecao, categoria1.Id));
        await db.SaveChangesAsync();
        outro.ColecoesProdutos.Add(ColecaoProduto.Criar(empresa2.Id, "Natal", data, null));
        await outro.SaveChangesAsync();
        Assert.Single(outro.ColecoesProdutos.ToList());
        Assert.Single(db.ColecoesProdutos.ToList());
        Assert.Empty(outro.ColecoesProdutosCategorias.ToList());
        Assert.Single(db.ColecoesProdutosCategorias);

        db.ColecoesProdutosCategorias.Add(new ColecaoProdutoCategoria(colecao, categoria2.Id));
        await Assert.ThrowsAsync<InvalidOperationException>(() => db.SaveChangesAsync());
        db.ChangeTracker.Clear();
        db.ColecoesProdutos.Add(ColecaoProduto.Criar(1, "Natal", data.AddYears(1), null));
        await db.SaveChangesAsync();
        db.ColecoesProdutos.Add(ColecaoProduto.Criar(1, "NATAL", data, null));
        await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
    }

    private static PrecificadorDbContext CriarContexto(string cs, int empresaId) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(cs).Options,
        new Contexto(empresaId));

    private sealed class Contexto(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
