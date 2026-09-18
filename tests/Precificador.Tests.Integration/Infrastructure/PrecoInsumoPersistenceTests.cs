using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class PrecoInsumoPersistenceTests
{
    [Fact]
    public async Task CA19_Migration_cria_tabela_precos_em_banco_vazio()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PrecoInsumo");
        await using var contexto = CriarContexto(connectionString, 1);

        await contexto.Database.MigrateAsync();

        Assert.Contains("PrecosInsumos", await contexto.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables").ToListAsync());
    }

    [Fact]
    public async Task CA18_Preco_persiste_com_fks_restrict_para_empresa_e_insumo()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PrecoInsumo");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        var insumo = await CriarInsumoAsync(contexto, 1);

        contexto.PrecosInsumos.Add(PrecoInsumo.Criar(1, insumo.Id, 1000m, 5.39m, new DateOnly(2026, 1, 1)));
        await contexto.SaveChangesAsync();

        Assert.Single(await contexto.PrecosInsumos.ToListAsync());
    }

    [Fact]
    public async Task CA08_Dois_precos_na_mesma_data_sao_preservados_e_maior_id_e_o_mais_recente()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PrecoInsumo");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        var insumo = await CriarInsumoAsync(contexto, 1);
        var data = new DateOnly(2026, 1, 1);
        var primeiro = PrecoInsumo.Criar(1, insumo.Id, 1m, 2m, data);
        var segundo = PrecoInsumo.Criar(1, insumo.Id, 1m, 3m, data);

        contexto.PrecosInsumos.AddRange(primeiro, segundo);
        await contexto.SaveChangesAsync();

        var precos = await contexto.PrecosInsumos
            .Where(preco => preco.InsumoId == insumo.Id)
            .OrderBy(preco => preco.Id)
            .ToListAsync();

        Assert.Equal(2, precos.Count);
        Assert.True(precos[1].Id > precos[0].Id);
        Assert.Equal(3m, precos[1].PrecoCompra);
    }

    [Fact]
    public async Task CA10_Query_filter_isola_precos_por_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PrecoInsumo");
        await using var empresaUm = CriarContexto(connectionString, 1);
        await empresaUm.Database.MigrateAsync();
        empresaUm.Empresas.Add(Empresa.Criar("Empresa dois"));
        await empresaUm.SaveChangesAsync();

        var insumoUm = await CriarInsumoAsync(empresaUm, 1);
        empresaUm.PrecosInsumos.Add(PrecoInsumo.Criar(1, insumoUm.Id, 1m, 1m, new DateOnly(2026, 1, 1)));
        await empresaUm.SaveChangesAsync();

        await using var empresaDois = CriarContexto(connectionString, 2);
        var insumoDois = await CriarInsumoAsync(empresaDois, 2);
        empresaDois.PrecosInsumos.Add(PrecoInsumo.Criar(2, insumoDois.Id, 1m, 1m, new DateOnly(2026, 1, 1)));
        await empresaDois.SaveChangesAsync();

        Assert.Single(await empresaUm.PrecosInsumos.ToListAsync());
        Assert.Single(await empresaDois.PrecosInsumos.ToListAsync());
    }

    [Fact]
    public async Task CA10_Guard_rejeita_escrita_de_preco_para_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PrecoInsumo");
        await using var empresaUm = CriarContexto(connectionString, 1);
        await empresaUm.Database.MigrateAsync();
        empresaUm.Empresas.Add(Empresa.Criar("Empresa dois"));
        await empresaUm.SaveChangesAsync();

        await using var empresaDois = CriarContexto(connectionString, 2);
        var insumo = await CriarInsumoAsync(empresaDois, 2);
        empresaUm.PrecosInsumos.Add(PrecoInsumo.Criar(2, insumo.Id, 1m, 1m, new DateOnly(2026, 1, 1)));

        await Assert.ThrowsAsync<InvalidOperationException>(() => empresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task CA10_Guard_rejeita_preco_com_empresa_ativa_apontando_para_insumo_de_outro_tenant()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PrecoInsumo");
        await using var empresaUm = CriarContexto(connectionString, 1);
        await empresaUm.Database.MigrateAsync();
        empresaUm.Empresas.Add(Empresa.Criar("Empresa dois"));
        await empresaUm.SaveChangesAsync();

        await using var empresaDois = CriarContexto(connectionString, 2);
        var insumoOutroTenant = await CriarInsumoAsync(empresaDois, 2);

        empresaUm.PrecosInsumos.Add(PrecoInsumo.Criar(1, insumoOutroTenant.Id, 1m, 1m, new DateOnly(2026, 1, 1)));

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => empresaUm.SaveChangesAsync());
        Assert.Equal("O insumo referenciado pelo preço não pertence à mesma empresa.", exception.Message);
        Assert.Empty(await empresaUm.PrecosInsumos.ToListAsync());
    }

    [Fact]
    public async Task CA18_Exclusao_fisica_de_insumo_com_preco_e_rejeitada_pela_fk()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PrecoInsumo");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        var insumo = await CriarInsumoAsync(contexto, 1);
        contexto.PrecosInsumos.Add(PrecoInsumo.Criar(1, insumo.Id, 1m, 1m, new DateOnly(2026, 1, 1)));
        await contexto.SaveChangesAsync();

        await Assert.ThrowsAsync<SqlException>(() => contexto.Database.ExecuteSqlAsync($"DELETE FROM Insumos WHERE Id = {insumo.Id}"));
    }

    private static PrecificadorDbContext CriarContexto(string connectionString, int empresaId) =>
        new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options, new ContextoEmpresa(empresaId));

    private static async Task<Insumo> CriarInsumoAsync(PrecificadorDbContext context, int empresaId)
    {
        var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        return insumo;
    }

    private sealed class ContextoEmpresa(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}

