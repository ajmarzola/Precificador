using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class MarcaObservacaoInsumoTests
{
    [Fact]
    public async Task Upgrade_da_ft002_preserva_insumo_e_inicializa_novos_campos()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync("20260910124716_AddMultiempresaIdentity");
        await contexto.Database.ExecuteSqlRawAsync("INSERT INTO Insumos (EmpresaId, Nome, NomeNormalizado, Categoria, UnidadeBase, Ativo) VALUES (1, 'Farinha antiga', 'FARINHA ANTIGA', 1, 1, 1)");

        await contexto.Database.MigrateAsync();

        var insumo = await contexto.Insumos.SingleAsync();
        Assert.Equal(1, insumo.EmpresaId);
        Assert.Null(insumo.Marca);
        Assert.Equal(string.Empty, insumo.MarcaNormalizada);
        Assert.Null(insumo.Observacao);
    }

    [Fact]
    public async Task Indice_composto_permite_marcas_distintas_e_rejeita_mesma_marca_na_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();

        contexto.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata"));
        contexto.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Caputo"));
        await contexto.SaveChangesAsync();

        contexto.Insumos.Add(Insumo.Criar(1, " farinha ", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "  RENATA  "));
        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task Indice_composto_rejeita_sem_marca_duplicado_e_permite_mesma_combinacao_em_outra_empresa()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var empresa1 = CriarContexto(connection, 1);
        await empresa1.Database.MigrateAsync();
        empresa1.Empresas.Add(Empresa.Criar("Empresa dois"));
        await empresa1.SaveChangesAsync();
        empresa1.Insumos.Add(Insumo.Criar(1, "Sal", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        empresa1.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata"));
        await empresa1.SaveChangesAsync();
        empresa1.Insumos.Add(Insumo.Criar(1, " sal ", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await Assert.ThrowsAsync<DbUpdateException>(() => empresa1.SaveChangesAsync());
        empresa1.ChangeTracker.Clear();

        await using var empresa2 = CriarContexto(connection, 2);
        empresa2.Insumos.Add(Insumo.Criar(2, "Sal", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        empresa2.Insumos.Add(Insumo.Criar(2, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata"));
        await empresa2.SaveChangesAsync();

        Assert.Equal(2, await empresa2.Insumos.CountAsync());
    }

    [Fact]
    public async Task Marca_e_observacao_sao_persistidas_e_recuperadas()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();
        contexto.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata", "W 300\nProteína 13,5%"));
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var insumo = await contexto.Insumos.SingleAsync();
        Assert.Equal("Renata", insumo.Marca);
        Assert.Equal("RENATA", insumo.MarcaNormalizada);
        Assert.Equal("W 300\nProteína 13,5%", insumo.Observacao);
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
