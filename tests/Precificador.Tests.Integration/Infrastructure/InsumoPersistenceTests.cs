using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class InsumoPersistenceTests
{
    [Fact]
    public async Task Migrations_criam_tabela_e_indice_unico_em_banco_vazio()
    {
        await using var context = await CriarContextoAsync(1);

        await context.Database.MigrateAsync();

        var objetos = await ObjetosDoSchemaAsync(context);
        Assert.Contains("Insumos", objetos);
        Assert.Contains("IX_Insumos_EmpresaId_NomeNormalizado_MarcaNormalizada", objetos);
    }

    [Fact]
    public async Task MEL010_Identidade_consolidada_persiste_e_nao_regride_apos_remocao_tecnica_do_item()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var insumo = Insumo.Criar(1, "Papel", CategoriaInsumo.MateriaPrima, UnidadeMedida.Metro);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO Produtos (EmpresaId, Nome, NomeNormalizado, MargemAlvo, Ativo) VALUES (1, 'Produto técnico', 'PRODUTO TÉCNICO', 0.3, 1)");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO FichasTecnicas (EmpresaId, ProdutoId, Rendimento, TempoAtivoMinutos) VALUES (1, 1, 1, 1)");
        await context.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ItensFichaTecnica (EmpresaId, FichaTecnicaId, InsumoId, Quantidade) VALUES (1, 1, {insumo.Id}, 1)");
        insumo.ConsolidarIdentidade();
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM ItensFichaTecnica WHERE InsumoId = {insumo.Id}");
        context.ChangeTracker.Clear();

        Assert.True((await context.Insumos.SingleAsync()).IdentidadeConsolidada);
    }

    [Fact]
    public async Task Insumo_valido_persiste_e_recupera_todos_os_valores()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();

        context.Insumos.Add(Insumo.Criar(1, "  Copo   200 ml ", CategoriaInsumo.Embalagem, UnidadeMedida.Unidade));
        await context.SaveChangesAsync();

        var insumo = await context.Insumos.AsNoTracking().SingleAsync();
        Assert.Equal("Copo 200 ml", insumo.Nome);
        Assert.Equal("COPO 200 ML", insumo.NomeNormalizado);
        Assert.Equal(CategoriaInsumo.Embalagem, insumo.Categoria);
        Assert.Equal(UnidadeMedida.Unidade, insumo.UnidadeBase);
        Assert.True(insumo.Ativo);
        Assert.False(insumo.IdentidadeConsolidada);
    }

    [Fact]
    public async Task Indice_unico_rejeita_nome_normalizado_duplicado()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();

        context.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await context.SaveChangesAsync();
        context.Insumos.Add(Insumo.Criar(1, "farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Metro_persiste_como_quatro_e_e_recuperado_corretamente()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();

        context.Insumos.Add(Insumo.Criar(1, "Fita", CategoriaInsumo.MateriaPrima, UnidadeMedida.Metro));
        await context.SaveChangesAsync();

        var unidadePersistida = await context.Database.SqlQueryRaw<int>("SELECT UnidadeBase AS Value FROM Insumos").SingleAsync();
        var insumo = await context.Insumos.AsNoTracking().SingleAsync();
        Assert.Equal(4, unidadePersistida);
        Assert.Equal(UnidadeMedida.Metro, insumo.UnidadeBase);
    }

    [Fact]
    public async Task CA04_Edicao_valida_e_persistida_sem_alterar_empresa()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var insumo = Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata", "Original");
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();

        insumo.AtualizarDados("A\u00e7\u00facar", CategoriaInsumo.Embalagem, UnidadeMedida.Unidade, "Uni\u00e3o", "Atualizada");
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var persistido = await context.Insumos.SingleAsync();
        Assert.Equal(1, persistido.EmpresaId);
        Assert.True(persistido.Ativo);
        Assert.Equal("A\u00e7\u00facar", persistido.Nome);
        Assert.Equal("UNI\u00c3O", persistido.MarcaNormalizada);
        Assert.Equal(CategoriaInsumo.Embalagem, persistido.Categoria);
        Assert.Equal(UnidadeMedida.Unidade, persistido.UnidadeBase);
        Assert.Equal("Atualizada", persistido.Observacao);
    }

    [Fact]
    public async Task CA07_Indice_unico_rejeita_edicao_para_nome_marca_duplicados_na_mesma_empresa()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        context.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata"));
        var editavel = Insumo.Criar(1, "A\u00e7\u00facar", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Uni\u00e3o");
        context.Insumos.Add(editavel);
        await context.SaveChangesAsync();

        editavel.AtualizarDados(" farinha ", CategoriaInsumo.Embalagem, UnidadeMedida.Unidade, " RENATA ");

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA03_CA04_Alteracoes_de_status_persistem()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var insumo = Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();

        insumo.Desativar();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        Assert.False((await context.Insumos.SingleAsync()).Ativo);

        var reativado = await context.Insumos.SingleAsync();
        reativado.Reativar();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.True((await context.Insumos.SingleAsync()).Ativo);
    }

    [Fact]
    public async Task CA10_Insumo_inativo_continua_participando_do_indice_unico()
    {
        await using var context = await CriarContextoAsync(1);
        await context.Database.MigrateAsync();
        var insumo = Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata");
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        insumo.Desativar();
        await context.SaveChangesAsync();

        context.Insumos.Add(Insumo.Criar(1, " farinha ", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, " RENATA "));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static async Task<List<string>> ObjetosDoSchemaAsync(PrecificadorDbContext context)
    {
        var tabelas = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables").ToListAsync();
        var indices = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.indexes WHERE name IS NOT NULL").ToListAsync();
        return [.. tabelas, .. indices];
    }

    private static async Task<PrecificadorDbContext> CriarContextoAsync(int empresaId)
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Insumo");
        return new PrecificadorDbContext(
            new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options,
            new EmpresaContextoTeste());
    }

    private sealed class EmpresaContextoTeste : IEmpresaContext
    {
        public int? EmpresaId => 1;
        public int EmpresaIdOuSentinela => 1;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}

