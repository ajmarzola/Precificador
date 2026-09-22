using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class CategoriaProdutoPersistenceTests
{
    private const string MigracaoAnterior = "20260920172653_ReplaceHourlyLaborWithPercentage";
    private const string MigracaoUc032 = "20260921130356_AddCategoriaProduto";
    private const string MigracaoUc036 = "20260922130351_AddDesgasteEquipamentosCategoria";

    [Fact]
    public async Task P1_Migration_agrupa_categorias_por_empresa_e_texto_normalizado_preservando_nome_do_menor_id()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoAnterior);

        await InserirProdutoLegadoAsync(contexto, 1, "Papelaria escolar");
        await InserirProdutoLegadoAsync(contexto, 1, "  papelaria   ESCOLAR  ");
        await InserirProdutoLegadoAsync(contexto, 1, "Brindes");
        await InserirProdutoLegadoAsync(contexto, 1, null);

        Assert.Contains(MigracaoUc032, await contexto.Database.GetPendingMigrationsAsync());
        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoUc032);

        var categorias = await contexto.Database
            .SqlQueryRaw<string>("SELECT Nome AS Value FROM CategoriasProdutos WHERE EmpresaId = 1 ORDER BY Nome")
            .ToListAsync();
        Assert.Equal(["Brindes", "Papelaria escolar"], categorias);
        Assert.DoesNotContain(MigracaoUc032, await contexto.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task P2_Migration_isola_categorias_por_empresa_mesmo_com_mesmo_texto()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoAnterior);

        await contexto.Database.ExecuteSqlRawAsync(
            "INSERT INTO Empresas (Ativo, Nome, NomeNormalizado, TimeZoneId) VALUES (1, 'Empresa dois', 'EMPRESA DOIS', 'America/Sao_Paulo')");
        var empresaDoisId = await contexto.Database.SqlQueryRaw<int>(
            "SELECT Id AS Value FROM Empresas WHERE Nome = 'Empresa dois'").SingleAsync();

        await InserirProdutoLegadoAsync(contexto, 1, "Papelaria");
        await InserirProdutoLegadoAsync(contexto, empresaDoisId, "Papelaria");

        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoUc032);

        var categoriasEmpresaUm = await contexto.Database
            .SqlQueryRaw<int>("SELECT Id AS Value FROM CategoriasProdutos WHERE EmpresaId = 1").ToListAsync();
        var categoriasEmpresaDois = await contexto.Database
            .SqlQuery<int>($"SELECT Id AS Value FROM CategoriasProdutos WHERE EmpresaId = {empresaDoisId}").ToListAsync();
        Assert.Single(categoriasEmpresaUm);
        Assert.Single(categoriasEmpresaDois);
        Assert.NotEqual(categoriasEmpresaUm.Single(), categoriasEmpresaDois.Single());
    }

    [Fact]
    public async Task P3_Migration_liga_produtos_a_categoria_correspondente_e_mantem_produtos_sem_categoria_nulos()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoAnterior);

        var idComCategoria = await InserirProdutoLegadoAsync(contexto, 1, "Papelaria");
        var idSemCategoria = await InserirProdutoLegadoAsync(contexto, 1, null);

        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoUc032);

        var categoriaProdutoId = await contexto.Database.SqlQuery<int?>(
            $"SELECT CategoriaProdutoId AS Value FROM Produtos WHERE Id = {idComCategoria}").SingleAsync();
        var categoriaProdutoIdNulo = await contexto.Database.SqlQuery<int?>(
            $"SELECT CategoriaProdutoId AS Value FROM Produtos WHERE Id = {idSemCategoria}").SingleAsync();
        Assert.NotNull(categoriaProdutoId);
        Assert.Null(categoriaProdutoIdNulo);
    }

    [Fact]
    public async Task P4_Migration_remove_a_coluna_legada_Categoria_de_Produtos()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoAnterior);
        await InserirProdutoLegadoAsync(contexto, 1, "Papelaria");

        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoUc032);

        var colunasAntigas = await contexto.Database.SqlQueryRaw<string>(
            "SELECT COLUMN_NAME AS Value FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'Produtos' AND COLUMN_NAME = 'Categoria'").ToListAsync();
        Assert.Empty(colunasAntigas);
    }

    [Fact]
    public async Task P5_Banco_vazio_migra_sem_criar_categorias()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        await using var contexto = CriarContexto(connectionString, 1);

        await contexto.Database.MigrateAsync();

        var tabelas = await contexto.Database
            .SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables")
            .ToListAsync();
        Assert.Contains("CategoriasProdutos", tabelas);
        Assert.Empty(await contexto.CategoriasProdutos.IgnoreQueryFilters().ToListAsync());
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task P13_Uc036_backfill_preserva_categoria_ativa_inativa_produto_e_tenant()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("DesgasteCategoria");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoAnterior);
        var produtoId = await InserirProdutoLegadoAsync(contexto, 1, "Papelaria");
        await contexto.Database.ExecuteSqlRawAsync("INSERT INTO Empresas (Ativo, Nome, NomeNormalizado, TimeZoneId) VALUES (1, 'Empresa dois', 'EMPRESA DOIS', 'America/Sao_Paulo')");
        var empresaDoisId = await contexto.Database.SqlQueryRaw<int>("SELECT Id AS Value FROM Empresas WHERE Nome = 'Empresa dois'").SingleAsync();
        await InserirProdutoLegadoAsync(contexto, empresaDoisId, "Brindes");
        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoUc032);
        await contexto.Database.ExecuteSqlRawAsync("UPDATE CategoriasProdutos SET Ativo = 0 WHERE EmpresaId = {0}", empresaDoisId);

        await contexto.Database.GetService<IMigrator>().MigrateAsync(MigracaoUc036);

        var categorias = await contexto.CategoriasProdutos.IgnoreQueryFilters().OrderBy(c => c.EmpresaId).ToListAsync();
        Assert.Equal(2, categorias.Count);
        Assert.All(categorias, categoria =>
        {
            Assert.Equal(FormaCalculoDesgasteEquipamento.ValorFixoPorLote, categoria.FormaCalculoDesgasteEquipamento);
            Assert.Equal(0m, categoria.ValorDesgasteEquipamento);
        });
        Assert.False(categorias.Single(c => c.EmpresaId == empresaDoisId).Ativo);
        Assert.NotNull(await contexto.Produtos.IgnoreQueryFilters().Where(p => p.Id == produtoId).Select(p => p.CategoriaProdutoId).SingleAsync());
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task P14_Uc036_persiste_precisao_e_checks_rejeitam_forma_e_valor_invalidos()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("DesgasteChecks");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        contexto.CategoriasProdutos.Add(CategoriaProduto.Criar(1, "Fixo", FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 1.123456m));
        contexto.CategoriasProdutos.Add(CategoriaProduto.Criar(1, "Percentual", FormaCalculoDesgasteEquipamento.PercentualSobreInsumos, .125m));
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        Assert.Equal(1.123456m, (await contexto.CategoriasProdutos.SingleAsync(c => c.Nome == "Fixo")).ValorDesgasteEquipamento);
        Assert.Equal(.125m, (await contexto.CategoriasProdutos.SingleAsync(c => c.Nome == "Percentual")).ValorDesgasteEquipamento);
        await Assert.ThrowsAsync<SqlException>(() => contexto.Database.ExecuteSqlRawAsync("UPDATE CategoriasProdutos SET FormaCalculoDesgasteEquipamento = 99 WHERE Nome = 'Fixo'"));
        await Assert.ThrowsAsync<SqlException>(() => contexto.Database.ExecuteSqlRawAsync("UPDATE CategoriasProdutos SET ValorDesgasteEquipamento = -1 WHERE Nome = 'Fixo'"));
    }

    [Fact]
    public async Task P6_GQF_nao_retorna_categoria_de_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        int empresaDoisId;
        await using (var contexto = CriarContexto(connectionString, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            empresaDoisId = empresaDois.Id;

            contexto.CategoriasProdutos.Add(CriarCategoria(1, "Papelaria"));
            await contexto.SaveChangesAsync();

            await using var contextoEmpresaDois = CriarContexto(connectionString, empresaDoisId);
            contextoEmpresaDois.CategoriasProdutos.Add(CriarCategoria(empresaDoisId, "Brindes"));
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using var empresaUm = CriarContexto(connectionString, 1);
        await using var empresaDoisConsulta = CriarContexto(connectionString, empresaDoisId);

        Assert.Equal("Papelaria", (await empresaUm.CategoriasProdutos.SingleAsync()).Nome);
        Assert.Equal("Brindes", (await empresaDoisConsulta.CategoriasProdutos.SingleAsync()).Nome);
    }

    [Fact]
    public async Task P7_Indice_unico_impede_categorias_duplicadas_na_mesma_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();
        contexto.CategoriasProdutos.Add(CriarCategoria(1, "Papelaria"));
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        contexto.CategoriasProdutos.Add(CriarCategoria(1, "  PAPELARIA  "));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task P8_Guard_central_rejeita_produto_referenciando_categoria_de_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        int empresaDoisId;
        await using (var contexto = CriarContexto(connectionString, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            empresaDoisId = empresaDois.Id;
        }

        int categoriaEmpresaDoisId;
        await using (var contextoEmpresaDois = CriarContexto(connectionString, empresaDoisId))
        {
            var categoriaEmpresaDois = CriarCategoria(empresaDoisId, "Brindes");
            contextoEmpresaDois.CategoriasProdutos.Add(categoriaEmpresaDois);
            await contextoEmpresaDois.SaveChangesAsync();
            categoriaEmpresaDoisId = categoriaEmpresaDois.Id;
        }

        await using var contextoEmpresaUm = CriarContexto(connectionString, 1);
        var produto = Produto.Criar(1, "Agenda", 0.30m, categoriaEmpresaDoisId);
        contextoEmpresaUm.Produtos.Add(produto);

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task P10_Guard_central_rejeita_alteracao_tecnica_de_categoria_de_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        int empresaDoisId;
        int categoriaEmpresaDoisId;
        await using (var contexto = CriarContexto(connectionString, 1))
        {
            await contexto.Database.MigrateAsync();
            var empresaDois = Empresa.Criar("Empresa dois");
            contexto.Empresas.Add(empresaDois);
            await contexto.SaveChangesAsync();
            empresaDoisId = empresaDois.Id;
        }

        await using (var contextoEmpresaDois = CriarContexto(connectionString, empresaDoisId))
        {
            var categoriaEmpresaDois = CriarCategoria(empresaDoisId, "Brindes");
            contextoEmpresaDois.CategoriasProdutos.Add(categoriaEmpresaDois);
            await contextoEmpresaDois.SaveChangesAsync();
            categoriaEmpresaDoisId = categoriaEmpresaDois.Id;
        }

        await using var contextoEmpresaUm = CriarContexto(connectionString, 1);
        var categoriaDeOutraEmpresa = await contextoEmpresaUm.CategoriasProdutos.IgnoreQueryFilters()
            .SingleAsync(item => item.Id == categoriaEmpresaDoisId);
        categoriaDeOutraEmpresa.Renomear("Brindes alterados");

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task P12_FK_Restrict_impede_exclusao_fisica_de_categoria_referenciada_por_produto()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        int categoriaId;
        int produtoId;
        await using (var contexto = CriarContexto(connectionString, 1))
        {
            await contexto.Database.MigrateAsync();

            var categoria = CriarCategoria(1, "Papelaria");
            contexto.CategoriasProdutos.Add(categoria);
            await contexto.SaveChangesAsync();
            categoriaId = categoria.Id;

            var produto = Produto.Criar(1, "Agenda", 0.30m, categoriaId);
            contexto.Produtos.Add(produto);
            await contexto.SaveChangesAsync();
            produtoId = produto.Id;
        }

        await using (var contextoExclusao = CriarContexto(connectionString, 1))
        {
            var categoria = await contextoExclusao.CategoriasProdutos.SingleAsync(item => item.Id == categoriaId);
            contextoExclusao.CategoriasProdutos.Remove(categoria);

            var exception = await Assert.ThrowsAsync<DbUpdateException>(() => contextoExclusao.SaveChangesAsync());
            var sqlException = Assert.IsType<SqlException>(exception.InnerException);
            Assert.Equal(547, sqlException.Number);
        }

        await using var contextoVerificacao = CriarContexto(connectionString, 1);
        Assert.NotNull(await contextoVerificacao.CategoriasProdutos.SingleOrDefaultAsync(item => item.Id == categoriaId));
        Assert.Equal(categoriaId, (await contextoVerificacao.Produtos.SingleAsync(item => item.Id == produtoId)).CategoriaProdutoId);
    }

    [Fact]
    public async Task P9_Desativar_categoria_nao_desvincula_produtos_existentes()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();

        var categoria = CriarCategoria(1, "Papelaria");
        contexto.CategoriasProdutos.Add(categoria);
        await contexto.SaveChangesAsync();
        var produto = Produto.Criar(1, "Agenda", 0.30m, categoria.Id);
        contexto.Produtos.Add(produto);
        await contexto.SaveChangesAsync();
        var produtoId = produto.Id;

        categoria.Desativar();
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        var produtoAtualizado = await contexto.Produtos.SingleAsync(item => item.Id == produtoId);
        Assert.Equal(categoria.Id, produtoAtualizado.CategoriaProdutoId);
    }

    private static async Task<int> InserirProdutoLegadoAsync(PrecificadorDbContext contexto, int empresaId, string? categoria)
    {
        var nome = $"Produto {Guid.NewGuid():N}";
        if (categoria is null)
        {
            await contexto.Database.ExecuteSqlRawAsync(
                "INSERT INTO Produtos (EmpresaId, Nome, NomeNormalizado, MargemAlvo, Ativo, Categoria) VALUES ({0}, {1}, {2}, 0.2, 1, NULL)",
                empresaId, nome, nome.ToUpperInvariant());
        }
        else
        {
            await contexto.Database.ExecuteSqlRawAsync(
                "INSERT INTO Produtos (EmpresaId, Nome, NomeNormalizado, MargemAlvo, Ativo, Categoria) VALUES ({0}, {1}, {2}, 0.2, 1, {3})",
                empresaId, nome, nome.ToUpperInvariant(), categoria);
        }

        return await contexto.Database.SqlQuery<int>(
            $"SELECT Id AS Value FROM Produtos WHERE Nome = {nome}").SingleAsync();
    }

    private static PrecificadorDbContext CriarContexto(string connectionString, int empresaId) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options,
        new ContextoEmpresa(empresaId));

    private static CategoriaProduto CriarCategoria(int empresaId, string nome) =>
        CategoriaProduto.Criar(empresaId, nome, FormaCalculoDesgasteEquipamento.ValorFixoPorLote, 0m);

    private sealed class ContextoEmpresa(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
