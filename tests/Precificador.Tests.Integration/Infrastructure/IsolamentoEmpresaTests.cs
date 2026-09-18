using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class IsolamentoEmpresaTests
{
    [Fact]
    public async Task Leitura_e_escrita_respeitam_empresa_ativa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("IsolamentoEmpresa");
        var empresa1 = new ContextoEmpresa(1);
        await using (var contexto = CriarContexto(connectionString, empresa1))
        {
            await contexto.Database.MigrateAsync();
            contexto.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
            await contexto.SaveChangesAsync();
            contexto.Empresas.Add(Empresa.Criar("Empresa dois"));
            await contexto.SaveChangesAsync();
        }
        var empresa2 = new ContextoEmpresa(2);
        await using (var contexto = CriarContexto(connectionString, empresa2))
        {
            Assert.Empty(await contexto.Insumos.ToListAsync());
            contexto.Insumos.Add(Insumo.Criar(1, "Tentativa", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
            await Assert.ThrowsAsync<InvalidOperationException>(() => contexto.SaveChangesAsync());
        }
        await using var semEmpresa = CriarContexto(connectionString, new ContextoEmpresa(null));
        Assert.Empty(await semEmpresa.Insumos.ToListAsync());
    }

    private static PrecificadorDbContext CriarContexto(string connectionString, IEmpresaContext empresaContext) =>
        new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options, empresaContext);

    private sealed class ContextoEmpresa(int? empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId ?? -1;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}

