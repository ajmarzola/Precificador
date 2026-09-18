using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class MultiempresaMigrationTests
{
    [Fact]
    public async Task CA03_Banco_novo_cria_empresa_tecnica_com_timezone_padrao()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Multiempresa");
        await using var contexto = CriarContexto(connectionString, 1);

        await contexto.Database.MigrateAsync();

        var empresa = await contexto.Empresas.SingleAsync(empresa => empresa.Id == 1);
        Assert.Equal(Empresa.TimeZoneIdPadrao, empresa.TimeZoneId);
    }

    [Fact]
    public async Task CA01_Timezone_persiste_e_e_recuperado()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Multiempresa");
        await using var contexto = CriarContexto(connectionString, 1);
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
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Multiempresa");
        await using var empresa1 = CriarContexto(connectionString, 1);
        await empresa1.Database.MigrateAsync();
        empresa1.Empresas.Add(Empresa.Criar("Empresa dois"));
        await empresa1.SaveChangesAsync();
        empresa1.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await empresa1.SaveChangesAsync();
        await using var empresa2 = CriarContexto(connectionString, 2);
        empresa2.Insumos.Add(Insumo.Criar(2, "farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await empresa2.SaveChangesAsync();
        empresa2.Insumos.Add(Insumo.Criar(2, "FARINHA", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await Assert.ThrowsAsync<DbUpdateException>(() => empresa2.SaveChangesAsync());
    }

    private static PrecificadorDbContext CriarContexto(string connectionString, int empresaId) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options,
        new ContextoEmpresa(empresaId));

    private sealed class ContextoEmpresa(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}

