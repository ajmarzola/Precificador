using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class MultiempresaMigrationTests
{
    private const string MigrationAddPrecosInsumos = "20260911133743_AddPrecosInsumos";

    [Fact]
    public async Task Upgrade_do_banco_uc001_preserva_insumo_e_o_associa_a_empresa_tecnica()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync("20260909200644_CreateInsumos");
        await contexto.Database.ExecuteSqlRawAsync("INSERT INTO Insumos (Nome, NomeNormalizado, Categoria, UnidadeBase, Ativo) VALUES ('Farinha', 'FARINHA', 1, 1, 1)");

        await contexto.Database.MigrateAsync();

        var objetos = await contexto.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type = 'table'").ToListAsync();
        Assert.Contains("AspNetUsers", objetos);
        Assert.Contains("Empresas", objetos);
        Assert.Contains("UsuariosEmpresas", objetos);
        var insumo = await contexto.Insumos.SingleAsync();
        Assert.Equal(1, insumo.EmpresaId);
        Assert.Equal(CategoriaInsumo.MateriaPrima, insumo.Categoria);
        Assert.Equal("Empresa inicial", (await contexto.Empresas.SingleAsync(empresa => empresa.Id == 1)).Nome);
    }

    [Fact]
    public async Task CA02_Migration_adiciona_timezone_e_preserva_empresa_existente()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync(MigrationAddPrecosInsumos);
        await contexto.Database.ExecuteSqlRawAsync("INSERT INTO Empresas (Nome, NomeNormalizado, Ativo) VALUES ('Empresa antiga', 'EMPRESA ANTIGA', 1)");

        await contexto.Database.MigrateAsync();

        var timezones = await contexto.Database
            .SqlQueryRaw<string>("SELECT TimeZoneId AS Value FROM Empresas ORDER BY Id")
            .ToListAsync();
        Assert.All(timezones, timeZoneId => Assert.Equal(Empresa.TimeZoneIdPadrao, timeZoneId));
    }

    [Fact]
    public async Task CA03_Banco_novo_cria_empresa_tecnica_com_timezone_padrao()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);

        await contexto.Database.MigrateAsync();

        var empresa = await contexto.Empresas.SingleAsync(empresa => empresa.Id == 1);
        Assert.Equal(Empresa.TimeZoneIdPadrao, empresa.TimeZoneId);
    }

    [Fact]
    public async Task CA01_Timezone_persiste_e_e_recuperado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var contexto = CriarContexto(connection, 1);
        await contexto.Database.MigrateAsync();
        contexto.Empresas.Add(Empresa.Criar("Empresa UTC", "UTC"));
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var empresa = await contexto.Empresas.SingleAsync(empresa => empresa.Nome == "Empresa UTC");

        Assert.Equal("UTC", empresa.TimeZoneId);
    }

    [Fact]
    public async Task Mesmo_nome_normalizado_e_permitido_em_empresas_distintas_e_rejeitado_na_mesma()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var empresa1 = CriarContexto(connection, 1);
        await empresa1.Database.MigrateAsync();
        empresa1.Empresas.Add(Empresa.Criar("Empresa dois"));
        await empresa1.SaveChangesAsync();
        empresa1.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await empresa1.SaveChangesAsync();
        await using var empresa2 = CriarContexto(connection, 2);
        empresa2.Insumos.Add(Insumo.Criar(2, "farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await empresa2.SaveChangesAsync();
        empresa2.Insumos.Add(Insumo.Criar(2, "FARINHA", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await Assert.ThrowsAsync<DbUpdateException>(() => empresa2.SaveChangesAsync());
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
