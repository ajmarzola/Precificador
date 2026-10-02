using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class IdentidadeVisualEmpresaPersistenceTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Clean_e_upgrade_preservam_dados_sem_backfill(bool upgrade)
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("IdentidadeMigration");
        await using var db = Criar(cs, 1);
        if (upgrade)
        {
            await db.GetService<IMigrator>().MigrateAsync("20261002024501_UC034_ProdutosColecoes");
            db.Produtos.Add(Produto.Criar(1, "Produto preservado", .3m));
            await db.SaveChangesAsync();
        }
        await db.Database.MigrateAsync();
        Assert.Empty(await db.IdentidadesVisuaisEmpresas.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.False(db.Database.HasPendingModelChanges());
        Assert.Equal("Empresa inicial", (await db.Empresas.SingleAsync()).Nome);
        if (upgrade) Assert.Equal(.3m, (await db.Produtos.SingleAsync()).MargemAlvo);
        var identidade = IdentidadeVisualEmpresa.Criar(1, "#aabbcc");
        identidade.DefinirLogo(Convert.FromHexString("FFD8FF"), "image/jpeg");
        db.IdentidadesVisuaisEmpresas.Add(identidade); await db.SaveChangesAsync(); db.ChangeTracker.Clear();
        var relida = await db.IdentidadesVisuaisEmpresas.SingleAsync();
        Assert.Equal("#AABBCC", relida.CorPrimaria); Assert.Equal("image/jpeg", relida.LogoContentType);
        Assert.Equal(Convert.FromHexString("FFD8FF"), relida.LogoConteudo);
    }

    [Fact]
    public async Task GQF_sem_tenant_e_guard_de_insercao_alteracao_exclusao()
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("IdentidadeIsolamento");
        await using var db = Criar(cs, 1); await db.Database.MigrateAsync();
        var outra = Empresa.Criar("Outra"); db.Empresas.Add(outra); await db.SaveChangesAsync();
        db.IdentidadesVisuaisEmpresas.Add(IdentidadeVisualEmpresa.Criar(1, "#123456")); await db.SaveChangesAsync();
        await using (var b = Criar(cs, outra.Id))
        { b.IdentidadesVisuaisEmpresas.Add(IdentidadeVisualEmpresa.Criar(outra.Id, "#ABCDEF")); await b.SaveChangesAsync(); }
        Assert.Equal(1, (await db.IdentidadesVisuaisEmpresas.SingleAsync()).EmpresaId);
        await using (var sem = Criar(cs, null))
        {
            Assert.Empty(await sem.IdentidadesVisuaisEmpresas.ToListAsync());
            sem.IdentidadesVisuaisEmpresas.Add(IdentidadeVisualEmpresa.Criar(1, "#123456"));
            await Assert.ThrowsAsync<InvalidOperationException>(() => sem.SaveChangesAsync());
        }
        foreach (var operacao in new[] { "adicionar", "alterar", "excluir" })
        {
            await using var a = Criar(cs, 1);
            var externa = await a.IdentidadesVisuaisEmpresas.IgnoreQueryFilters().SingleAsync(x => x.EmpresaId == outra.Id);
            if (operacao == "adicionar") { a.ChangeTracker.Clear(); a.Add(IdentidadeVisualEmpresa.Criar(outra.Id, "#000000")); }
            if (operacao == "alterar") externa.AtualizarCor("#000000");
            if (operacao == "excluir") a.Remove(externa);
            await Assert.ThrowsAsync<InvalidOperationException>(() => a.SaveChangesAsync());
        }
        await using var consulta = Criar(cs, outra.Id);
        Assert.Equal("#ABCDEF", (await consulta.IdentidadesVisuaisEmpresas.SingleAsync()).CorPrimaria);
    }

    [Theory]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria) VALUES (9999, '#123456')")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria) VALUES (1, '#123456'); INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria) VALUES (1, '#ABCDEF')")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria) VALUES (1, '#GGGGGG')")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria) VALUES (1, '#aabbcc')")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria) VALUES (1, '#12345 ')")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria, LogoContentType) VALUES (1, '#123456', 'image/png')")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria, LogoConteudo) VALUES (1, '#123456', 0xFFD8FF)")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria, LogoConteudo, LogoContentType) VALUES (1, '#123456', 0xFFD8FF, 'image/svg+xml')")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria, LogoConteudo, LogoContentType) VALUES (1, '#123456', 0x, 'image/png')")]
    [InlineData("INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria, LogoConteudo, LogoContentType) VALUES (1, '#123456', CONVERT(varbinary(max), REPLICATE(CAST('x' AS varchar(max)), 524289)), 'image/png')")]
    public async Task Banco_rejeita_FK_PK_cor_logo_e_tamanho_invalidos(string sql)
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("IdentidadeChecks");
        await using var db = Criar(cs, 1); await db.Database.MigrateAsync();
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlRawAsync(sql));
    }

    [Fact]
    public async Task FK_restrict_e_limite_inclusivo()
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("IdentidadeFK");
        await using var db = Criar(cs, 1); await db.Database.MigrateAsync();
        var e = Empresa.Criar("Empresa com identidade"); db.Add(e); await db.SaveChangesAsync();
        await db.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO IdentidadesVisuaisEmpresas (EmpresaId, CorPrimaria, LogoConteudo, LogoContentType) VALUES ({e.Id}, '#123456', CONVERT(varbinary(max), REPLICATE(CAST('x' AS varchar(max)), 524288)), 'image/png')");
        await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM Empresas WHERE Id = {e.Id}"));
        Assert.True(await db.Empresas.AnyAsync(x => x.Id == e.Id));
    }

    private static PrecificadorDbContext Criar(string cs, int? empresaId) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(cs).Options, new Contexto(empresaId));
    private sealed class Contexto(int? id) : IEmpresaContext
    { public int? EmpresaId => id; public int EmpresaIdOuSentinela => id ?? -1; public string? TimeZoneId => Empresa.TimeZoneIdPadrao; }
}
