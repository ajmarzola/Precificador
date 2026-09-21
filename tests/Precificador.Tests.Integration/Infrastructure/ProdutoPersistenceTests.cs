using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ProdutoPersistenceTests
{
    [Fact]
    public async Task CA18_Migration_cria_tabela_produtos_e_indice_unico_em_banco_vazio()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 1);

        await context.Database.MigrateAsync();

        var tabelas = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.tables").ToListAsync();
        var indices = await context.Database.SqlQueryRaw<string>("SELECT name AS Value FROM sys.indexes WHERE name IS NOT NULL").ToListAsync();
        Assert.Contains("Produtos", tabelas);
        Assert.Contains("IX_Produtos_EmpresaId_NomeNormalizado", indices);
    }

    [Fact]
    public async Task CA03_Produto_valido_persiste_e_e_recuperado()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();

        var categoria = CategoriaProduto.Criar(1, "Planners");
        context.CategoriasProdutos.Add(categoria);
        await context.SaveChangesAsync();

        context.Produtos.Add(Produto.Criar(1, "  Agenda   2027 ", 0.30m, categoria.Id));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var produto = await context.Produtos.SingleAsync();
        Assert.Equal("Agenda 2027", produto.Nome);
        Assert.Equal("AGENDA 2027", produto.NomeNormalizado);
        Assert.Equal(categoria.Id, produto.CategoriaProdutoId);
        Assert.Equal(0.30m, produto.MargemAlvo);
        Assert.True(produto.Ativo);
    }

    [Fact]
    public async Task CA10_Indice_unico_rejeita_nome_normalizado_duplicado_na_mesma_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();

        context.Produtos.Add(Produto.Criar(1, "Agenda", 0.30m));
        await context.SaveChangesAsync();
        context.Produtos.Add(Produto.Criar(1, " agenda ", 0.25m));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA09_Mesmo_nome_normalizado_e_permitido_em_empresas_diferentes()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using (var context = CriarContexto(connectionString, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            context.Produtos.Add(Produto.Criar(1, "Agenda", 0.30m));
            await context.SaveChangesAsync();
        }

        await using var contextoEmpresaDois = CriarContexto(connectionString, 2);
        contextoEmpresaDois.Produtos.Add(Produto.Criar(2, " agenda ", 0.25m));

        await contextoEmpresaDois.SaveChangesAsync();
        Assert.Single(await contextoEmpresaDois.Produtos.ToListAsync());
    }

    [Fact]
    public async Task CA12_Query_filter_isola_produtos_por_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using (var context = CriarContexto(connectionString, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            context.Produtos.Add(Produto.Criar(1, "Agenda A", 0.30m));
            await context.SaveChangesAsync();
        }
        await using (var context = CriarContexto(connectionString, 2))
        {
            context.Produtos.Add(Produto.Criar(2, "Agenda B", 0.30m));
            await context.SaveChangesAsync();
        }

        await using var leituraEmpresaUm = CriarContexto(connectionString, 1);

        var produto = await leituraEmpresaUm.Produtos.SingleAsync();
        Assert.Equal("Agenda A", produto.Nome);
    }

    [Fact]
    public async Task CA13_Guard_rejeita_escrita_de_produto_para_outra_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();

        context.Produtos.Add(Produto.Criar(2, "Agenda", 0.30m));

        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA18_Fk_rejeita_empresa_inexistente()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 999);
        await context.Database.MigrateAsync();

        context.Produtos.Add(Produto.Criar(999, "Agenda", 0.30m));

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
        var sqlException = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Equal(547, sqlException.Number);
    }

    [Fact]
    public async Task CA04_Atualizacao_valida_persiste_campos_e_preserva_id_empresa_status()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();

        var categoriaOriginal = CategoriaProduto.Criar(1, "Planners");
        var categoriaNova = CategoriaProduto.Criar(1, "Datas");
        context.CategoriasProdutos.AddRange(categoriaOriginal, categoriaNova);
        await context.SaveChangesAsync();

        var produto = Produto.Criar(1, "Agenda", 0.30m, categoriaOriginal.Id);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        var id = produto.Id;

        produto.AtualizarDados("  Calendário   2027  ", 0.255m, categoriaNova.Id);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var atualizado = await context.Produtos.SingleAsync();
        Assert.Equal(id, atualizado.Id);
        Assert.Equal(1, atualizado.EmpresaId);
        Assert.Equal("Calendário 2027", atualizado.Nome);
        Assert.Equal("CALENDÁRIO 2027", atualizado.NomeNormalizado);
        Assert.Equal(categoriaNova.Id, atualizado.CategoriaProdutoId);
        Assert.Equal(0.255m, atualizado.MargemAlvo);
        Assert.True(atualizado.Ativo);
    }

    [Fact]
    public async Task CA10_Indice_unico_rejeita_renomeacao_para_nome_de_outro_produto_da_mesma_empresa()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();

        var produto = Produto.Criar(1, "Agenda", 0.30m);
        context.Produtos.AddRange(produto, Produto.Criar(1, "Calendário", 0.25m));
        await context.SaveChangesAsync();

        produto.AtualizarDados("  calendário  ", 0.30m);

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
    }

    [Fact]
    public async Task CA11_Mesmo_nome_normalizado_permanece_permitido_em_empresas_diferentes()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using (var context = CriarContexto(connectionString, 1))
        {
            await context.Database.MigrateAsync();
            context.Empresas.Add(Empresa.Criar("Empresa dois"));
            context.Produtos.Add(Produto.Criar(1, "Agenda", 0.30m));
            await context.SaveChangesAsync();
        }

        await using (var context = CriarContexto(connectionString, 2))
        {
            context.Produtos.Add(Produto.Criar(2, "Calendário", 0.25m));
            await context.SaveChangesAsync();
        }

        await using var contextoEmpresaDois = CriarContexto(connectionString, 2);
        var produtoEmpresaDois = await contextoEmpresaDois.Produtos.SingleAsync();
        produtoEmpresaDois.AtualizarDados(" agenda ", 0.20m);

        await contextoEmpresaDois.SaveChangesAsync();
        Assert.Equal("AGENDA", produtoEmpresaDois.NomeNormalizado);
    }

    [Fact]
    public async Task CA03_CA04_Alteracoes_de_status_persistem()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();

        var produto = Produto.Criar(1, "Agenda", 0.30m);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();

        produto.Desativar();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var inativo = await context.Produtos.SingleAsync();
        Assert.False(inativo.Ativo);

        inativo.Reativar();
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        Assert.True((await context.Produtos.SingleAsync()).Ativo);
    }

    [Fact]
    public async Task CA11_Produto_inativo_continua_participando_do_indice_unico()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("Produto");
        await using var context = CriarContexto(connectionString, 1);
        await context.Database.MigrateAsync();

        var produto = Produto.Criar(1, "Agenda", 0.30m);
        produto.Desativar();
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();

        context.Produtos.Add(Produto.Criar(1, " agenda ", 0.25m));

        await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync());
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
