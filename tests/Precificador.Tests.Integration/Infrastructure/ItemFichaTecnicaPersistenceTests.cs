using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ItemFichaTecnicaPersistenceTests
{
    [Fact]
    public async Task P1_Migration_cria_tabela_itens_e_indice_unico_em_banco_vazio()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);

        await context.Database.MigrateAsync();

        var tabelas = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables").ToListAsync();
        var indices = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.indexes WHERE name IS NOT NULL").ToListAsync();
        Assert.Contains("ItensFichaTecnica", tabelas);
        Assert.Contains("IX_ItensFichaTecnica_EmpresaId_FichaTecnicaId_InsumoId", indices);
    }

    [Fact]
    public async Task UC019_P1_Item_novo_sem_percentual_informado_persiste_com_zero()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 1.25m, "existente"));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var item = await context.ItensFichaTecnica.SingleAsync();

        Assert.Equal((1.25m, "existente", 0m), (item.Quantidade, item.Observacao, item.PercentualPerda));
    }

    [Fact]
    public async Task UC019_P3_Percentual_perda_persiste_com_seis_casas()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m, percentualPerda: 0.123456m));
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        Assert.Equal(0.123456m, (await context.ItensFichaTecnica.SingleAsync()).PercentualPerda);
    }

    [Fact]
    public async Task P2_Indice_unico_rejeita_mesmo_insumo_duas_vezes_na_mesma_ficha()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);

        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m));
        await context.SaveChangesAsync();
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 2m));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task P3_Mesmo_insumo_e_permitido_em_fichas_diferentes()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var insumo = Insumo.Criar(1, "Papel", CategoriaInsumo.MateriaPrima, UnidadeMedida.Metro);
        var produtoUm = Produto.Criar(1, "Agenda A", 0.30m);
        var produtoDois = Produto.Criar(1, "Agenda B", 0.30m);
        context.Insumos.Add(insumo);
        context.Produtos.AddRange(produtoUm, produtoDois);
        await context.SaveChangesAsync();
        var fichaUm = FichaTecnica.Criar(1, produtoUm.Id, 2m);
        var fichaDois = FichaTecnica.Criar(1, produtoDois.Id, 3m);
        context.FichasTecnicas.AddRange(fichaUm, fichaDois);
        await context.SaveChangesAsync();

        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaUm.Id, insumo.Id, 1m));
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaDois.Id, insumo.Id, 2m));
        await context.SaveChangesAsync();

        Assert.Equal(2, await context.ItensFichaTecnica.CountAsync());
    }

    [Fact]
    public async Task P4_Query_filter_isola_itens_por_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using (var context = CriarContexto(connectionString, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            await context.SaveChangesAsync();
            var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
            context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m));
            await context.SaveChangesAsync();
        }

        await using (var context = CriarContexto(connectionString, 2))
        {
            var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 2);
            context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(2, fichaId, insumoId, 2m));
            await context.SaveChangesAsync();
        }

        await using var leituraEmpresaUm = CriarContexto(connectionString, 1);

        var item = await leituraEmpresaUm.ItensFichaTecnica.SingleAsync();
        Assert.Equal(1, item.EmpresaId);
        Assert.Equal(1m, item.Quantidade);
    }

    [Fact]
    public async Task P5_Guard_rejeita_item_com_ficha_de_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using (var context = CriarContexto(connectionString, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            await context.SaveChangesAsync();
        }

        await using var contextoEmpresaDois = CriarContexto(connectionString, 2);
        var (_, fichaEmpresaDois, _) = await CriarFichaEInsumoAsync(contextoEmpresaDois, 2);
        await using var contextoEmpresaUm = CriarContexto(connectionString, 1);
        var (_, _, insumoEmpresaUm) = await CriarFichaEInsumoAsync(contextoEmpresaUm, 1);

        contextoEmpresaUm.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaEmpresaDois, insumoEmpresaUm, 1m));

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task P6_Guard_rejeita_item_com_insumo_de_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using (var context = CriarContexto(connectionString, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            await context.SaveChangesAsync();
        }

        await using var contextoEmpresaDois = CriarContexto(connectionString, 2);
        var (_, _, insumoEmpresaDois) = await CriarFichaEInsumoAsync(contextoEmpresaDois, 2);
        await using var contextoEmpresaUm = CriarContexto(connectionString, 1);
        var (_, fichaEmpresaUm, _) = await CriarFichaEInsumoAsync(contextoEmpresaUm, 1);

        contextoEmpresaUm.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaEmpresaUm, insumoEmpresaDois, 1m));

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task P7_Fks_restrict_preservam_referencias_de_ficha_e_insumo()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m));
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        context.Insumos.Remove(await context.Insumos.SingleAsync(insumo => insumo.Id == insumoId));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());

        context.ChangeTracker.Clear();
        context.FichasTecnicas.Remove(await context.FichasTecnicas.SingleAsync(ficha => ficha.Id == fichaId));
        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task P8_UC015_Round_trip_atualiza_quantidade_e_observacao_no_mesmo_item()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        var item = ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m, "original");
        context.ItensFichaTecnica.Add(item);
        await context.SaveChangesAsync();
        var itemId = item.Id;

        context.ChangeTracker.Clear();
        var persistido = await context.ItensFichaTecnica.SingleAsync(item => item.Id == itemId);
        persistido.AtualizarDados(1.25m, "  ajustado  ", 0m);
        await context.SaveChangesAsync();

        context.ChangeTracker.Clear();
        var atualizado = await context.ItensFichaTecnica.AsNoTracking().SingleAsync(item => item.Id == itemId);
        Assert.Equal(1.25m, atualizado.Quantidade);
        Assert.Equal("ajustado", atualizado.Observacao);
    }

    [Fact]
    public async Task P9_UC015_Atualizacao_preserva_empresa_ficha_e_insumo()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        var item = ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m, null);
        context.ItensFichaTecnica.Add(item);
        await context.SaveChangesAsync();
        var itemId = item.Id;

        item.AtualizarDados(2m, "alterado", 0m);
        await context.SaveChangesAsync();

        var atualizado = await context.ItensFichaTecnica.AsNoTracking().SingleAsync(item => item.Id == itemId);
        Assert.Equal(1, atualizado.EmpresaId);
        Assert.Equal(fichaId, atualizado.FichaTecnicaId);
        Assert.Equal(insumoId, atualizado.InsumoId);
    }

    [Fact]
    public async Task P10_UC015_RN049_permanece_intacta_apos_atualizar_item()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        var item = ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m, null);
        context.ItensFichaTecnica.Add(item);
        await context.SaveChangesAsync();

        item.AtualizarDados(2m, null, 0m);
        context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(1, fichaId, insumoId, 3m));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task P11_UC015_Schema_de_item_permanece_sem_campos_novos()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();

        var colunas = await context.Database
            .SqlQueryRaw<string>("SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'ItensFichaTecnica' ORDER BY ORDINAL_POSITION")
            .ToListAsync();

        Assert.Equal(
            ["Id", "EmpresaId", "FichaTecnicaId", "InsumoId", "Quantidade", "Observacao", "PercentualPerda"],
            colunas);
    }

    [Fact]
    public async Task UC016_P1_Remover_item_proprio_persiste_exclusao()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        var item = ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m);
        context.ItensFichaTecnica.Add(item);
        await context.SaveChangesAsync();

        context.ItensFichaTecnica.Remove(item);
        await context.SaveChangesAsync();

        Assert.Empty(await context.ItensFichaTecnica.ToListAsync());
    }

    [Fact]
    public async Task UC016_P2_Remover_item_preserva_produto_ficha_e_insumo()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (produtoId, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        var item = ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m);
        context.ItensFichaTecnica.Add(item);
        await context.SaveChangesAsync();

        context.ItensFichaTecnica.Remove(item);
        await context.SaveChangesAsync();

        Assert.Equal(produtoId, (await context.Produtos.SingleAsync()).Id);
        Assert.Equal(fichaId, (await context.FichasTecnicas.SingleAsync()).Id);
        Assert.Equal(insumoId, (await context.Insumos.SingleAsync()).Id);
    }

    [Fact]
    public async Task UC016_P3_Guard_rejeita_delete_de_item_de_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using (var contextoInicial = CriarContexto(connectionString, 1))
        {
            await contextoInicial.Database.MigrateAsync();
            contextoInicial.Empresas.Add(Empresa.Criar("Empresa dois"));
            await contextoInicial.SaveChangesAsync();
        }

        ItemFichaTecnica itemEmpresaDois;
        await using (var contextoEmpresaDois = CriarContexto(connectionString, 2))
        {
            var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(contextoEmpresaDois, 2);
            itemEmpresaDois = ItemFichaTecnica.Criar(2, fichaId, insumoId, 1m);
            contextoEmpresaDois.ItensFichaTecnica.Add(itemEmpresaDois);
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using var contextoEmpresaUm = CriarContexto(connectionString, 1);
        contextoEmpresaUm.ItensFichaTecnica.Remove(itemEmpresaDois);

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task UC016_P4_Remover_item_preserva_identidade_consolidada()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("ItemFichaTecnica");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();
        var (_, fichaId, insumoId) = await CriarFichaEInsumoAsync(context, 1);
        var item = ItemFichaTecnica.Criar(1, fichaId, insumoId, 1m);
        context.ItensFichaTecnica.Add(item);
        (await context.Insumos.SingleAsync(insumo => insumo.Id == insumoId)).ConsolidarIdentidade();
        await context.SaveChangesAsync();

        context.ItensFichaTecnica.Remove(item);
        await context.SaveChangesAsync();

        Assert.True((await context.Insumos.SingleAsync(insumo => insumo.Id == insumoId)).IdentidadeConsolidada);
    }

    private static async Task<(int ProdutoId, int FichaId, int InsumoId)> CriarFichaEInsumoAsync(PrecificadorDbContext context, int empresaId)
    {
        var produto = Produto.Criar(empresaId, $"Produto {Guid.NewGuid():N}", 0.30m);
        var insumo = Insumo.Criar(empresaId, $"Insumo {Guid.NewGuid():N}", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama);
        context.Produtos.Add(produto);
        context.Insumos.Add(insumo);
        await context.SaveChangesAsync();
        var ficha = FichaTecnica.Criar(empresaId, produto.Id, 2m);
        context.FichasTecnicas.Add(ficha);
        await context.SaveChangesAsync();
        return (produto.Id, ficha.Id, insumo.Id);
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

