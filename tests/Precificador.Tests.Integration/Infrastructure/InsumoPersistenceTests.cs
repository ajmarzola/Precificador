using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class InsumoPersistenceTests
{
    [Fact]
    public async Task Migrations_criam_tabela_e_indice_unico_em_banco_sqlite_vazio()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);

        await context.Database.MigrateAsync();

        var objetos = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sqlite_master WHERE type IN ('table', 'index')").ToListAsync();
        Assert.Contains("Insumos", objetos);
        Assert.Contains("IX_Insumos_EmpresaId_NomeNormalizado_MarcaNormalizada", objetos);
    }

    [Fact]
    public async Task Insumo_valido_persiste_e_recupera_todos_os_valores()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
        await context.Database.MigrateAsync();

        context.Insumos.Add(Insumo.Criar(1, "  Copo   200 ml ", CategoriaInsumo.Embalagem, UnidadeMedida.Unidade));
        await context.SaveChangesAsync();

        var insumo = await context.Insumos.AsNoTracking().SingleAsync();
        Assert.Equal("Copo 200 ml", insumo.Nome);
        Assert.Equal("COPO 200 ML", insumo.NomeNormalizado);
        Assert.Equal(CategoriaInsumo.Embalagem, insumo.Categoria);
        Assert.Equal(UnidadeMedida.Unidade, insumo.UnidadeBase);
        Assert.True(insumo.Ativo);
    }

    [Fact]
    public async Task Indice_unico_rejeita_nome_normalizado_duplicado()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
        await context.Database.MigrateAsync();

        context.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));
        await context.SaveChangesAsync();
        context.Insumos.Add(Insumo.Criar(1, "farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task Metro_persiste_como_quatro_e_e_recuperado_corretamente()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
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
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
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
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
        await context.Database.MigrateAsync();
        context.Insumos.Add(Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata"));
        var editavel = Insumo.Criar(1, "A\u00e7\u00facar", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Uni\u00e3o");
        context.Insumos.Add(editavel);
        await context.SaveChangesAsync();

        editavel.AtualizarDados(" farinha ", CategoriaInsumo.Embalagem, UnidadeMedida.Unidade, " RENATA ");

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA03_CA04_Alteracoes_de_status_persistem_no_sqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
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
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        await using var context = CriarContexto(connection);
        await context.Database.MigrateAsync();
        var insumo = Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata");
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        insumo.Desativar();
        await context.SaveChangesAsync();

        context.Insumos.Add(Insumo.Criar(1, " farinha ", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, " RENATA "));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    private static PrecificadorDbContext CriarContexto(SqliteConnection connection) =>
        new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite(connection).Options, new EmpresaContextoTeste());

    private sealed class EmpresaContextoTeste : IEmpresaContext
    {
        public int? EmpresaId => 1;
        public int EmpresaIdOuSentinela => 1;
    }
}
