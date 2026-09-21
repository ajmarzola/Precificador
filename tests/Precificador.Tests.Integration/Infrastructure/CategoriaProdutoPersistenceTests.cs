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

        Assert.Single(await contexto.Database.GetPendingMigrationsAsync());
        await contexto.Database.MigrateAsync();

        var categorias = await contexto.Database
            .SqlQueryRaw<string>("SELECT Nome AS Value FROM CategoriasProdutos WHERE EmpresaId = 1 ORDER BY Nome")
            .ToListAsync();
        Assert.Equal(["Brindes", "Papelaria escolar"], categorias);
        Assert.Empty(await contexto.Database.GetPendingMigrationsAsync());
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

        await contexto.Database.MigrateAsync();

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

        await contexto.Database.MigrateAsync();

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

        await contexto.Database.MigrateAsync();

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

            contexto.CategoriasProdutos.Add(CategoriaProduto.Criar(1, "Papelaria"));
            await contexto.SaveChangesAsync();

            await using var contextoEmpresaDois = CriarContexto(connectionString, empresaDoisId);
            contextoEmpresaDois.CategoriasProdutos.Add(CategoriaProduto.Criar(empresaDoisId, "Brindes"));
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
        contexto.CategoriasProdutos.Add(CategoriaProduto.Criar(1, "Papelaria"));
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();

        contexto.CategoriasProdutos.Add(CategoriaProduto.Criar(1, "  PAPELARIA  "));

        await Assert.ThrowsAsync<DbUpdateException>(() => contexto.SaveChangesAsync());
    }

    [Fact]
    public async Task P8_Guard_central_rejeita_produto_referenciando_categoria_de_outra_empresa()
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

            var categoriaEmpresaDois = CategoriaProduto.Criar(empresaDoisId, "Brindes");
            contexto.CategoriasProdutos.Add(categoriaEmpresaDois);
            await contexto.SaveChangesAsync();
            categoriaEmpresaDoisId = categoriaEmpresaDois.Id;
        }

        await using var contextoEmpresaUm = CriarContexto(connectionString, 1);
        var produto = Produto.Criar(1, "Agenda", 0.30m, categoriaEmpresaDoisId);
        contextoEmpresaUm.Produtos.Add(produto);

        await Assert.ThrowsAsync<InvalidOperationException>(() => contextoEmpresaUm.SaveChangesAsync());
    }

    [Fact]
    public async Task P9_Desativar_categoria_nao_desvincula_produtos_existentes()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("CategoriaProduto");
        await using var contexto = CriarContexto(connectionString, 1);
        await contexto.Database.MigrateAsync();

        var categoria = CategoriaProduto.Criar(1, "Papelaria");
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

    private sealed class ContextoEmpresa(int empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
