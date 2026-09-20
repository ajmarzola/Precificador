using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class RegistroPrecoProdutoConsultaTests
{
    [Fact]
    public async Task P1_P2_Ordenacao_por_data_e_id_seleciona_registro_atual()
    {
        await using var context = await CriarAsync(1);
        await context.Database.MigrateAsync();
        var produto = await CriarProdutoAsync(context, 1);
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, new DateOnly(2026, 9, 1), 10m));
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, new DateOnly(2026, 9, 15), 20m));
        await context.SaveChangesAsync();
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, new DateOnly(2026, 9, 15), 30m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var historico = await context.RegistrosPrecosProdutos.AsNoTracking()
            .ListarHistoricoAsync(produto.Id);
        var atual = await context.RegistrosPrecosProdutos.AsNoTracking()
            .SelecionarAtualAsync(produto.Id);

        Assert.Equal([30m, 20m, 10m], historico.Select(registro => registro.PrecoPrateleira));
        Assert.Equal(30m, atual!.PrecoPrateleira);
    }

    [Fact]
    public async Task P3_Gqf_isola_historico_entre_empresas()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("RegistroPrecoProdutoConsulta");
        await using (var empresaUm = Criar(connectionString, 1))
        {
            await empresaUm.Database.MigrateAsync();
            empresaUm.Empresas.Add(Empresa.Criar("Empresa dois"));
            await empresaUm.SaveChangesAsync();
            var produtoUm = await CriarProdutoAsync(empresaUm, 1, "Um");
            empresaUm.RegistrosPrecosProdutos.Add(Registro(1, produtoUm.Id, new DateOnly(2026, 9, 15), 10m));
            await empresaUm.SaveChangesAsync();
        }

        await using (var empresaDois = Criar(connectionString, 2))
        {
            var produtoDois = await CriarProdutoAsync(empresaDois, 2, "Dois");
            empresaDois.RegistrosPrecosProdutos.Add(Registro(2, produtoDois.Id, new DateOnly(2026, 9, 15), 20m));
            await empresaDois.SaveChangesAsync();
        }

        await using var contextoEmpresaUm = Criar(connectionString, 1);
        await using var contextoEmpresaDois = Criar(connectionString, 2);

        Assert.Equal(10m, Assert.Single(await contextoEmpresaUm.RegistrosPrecosProdutos.AsNoTracking().ToListAsync()).PrecoPrateleira);
        Assert.Equal(20m, Assert.Single(await contextoEmpresaDois.RegistrosPrecosProdutos.AsNoTracking().ToListAsync()).PrecoPrateleira);
    }

    [Fact]
    public async Task P4_Consulta_historica_nao_altera_registros()
    {
        await using var context = await CriarAsync(1);
        await context.Database.MigrateAsync();
        var produto = await CriarProdutoAsync(context, 1);
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, new DateOnly(2026, 9, 15), 10m));
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        await context.RegistrosPrecosProdutos.AsNoTracking().ListarHistoricoAsync(produto.Id);
        await context.RegistrosPrecosProdutos.AsNoTracking().SelecionarAtualAsync(produto.Id);

        Assert.Equal(0, await context.SaveChangesAsync());
        Assert.Equal(10m, Assert.Single(await context.RegistrosPrecosProdutos.ToListAsync()).PrecoPrateleira);
    }

    [Fact]
    public async Task P5_Selecao_do_atual_independe_da_configuracao_vigente()
    {
        await using var context = await CriarAsync(1);
        await context.Database.MigrateAsync();
        var produto = await CriarProdutoAsync(context, 1);
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, new DateOnly(2026, 9, 1), 10m, reserva: .10m));
        context.RegistrosPrecosProdutos.Add(Registro(1, produto.Id, new DateOnly(2026, 9, 2), 11m, reserva: .05m));
        await context.SaveChangesAsync();

        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync();
        configuracao.Atualizar(.10m, null, null, .50m, .90m);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var atual = await context.RegistrosPrecosProdutos.AsNoTracking()
            .SelecionarAtualAsync(produto.Id);

        Assert.Equal(11m, atual!.PrecoPrateleira);
        Assert.Equal(.05m, atual.ReservaComercialReferencia);
    }

    private static async Task<Produto> CriarProdutoAsync(PrecificadorDbContext context, int empresaId, string nome = "Produto")
    {
        var produto = Produto.Criar(empresaId, $"{nome} {Guid.NewGuid():N}", .30m);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto;
    }

    private static RegistroPrecoProduto Registro(int empresaId, int produtoId, DateOnly data, decimal precoPrateleira, decimal reserva = .10m) =>
        RegistroPrecoProduto.Criar(empresaId, produtoId, data, 10m, .30m, 100m, precoPrateleira, reserva);

    private static async Task<PrecificadorDbContext> CriarAsync(int empresaId)
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("RegistroPrecoProdutoConsulta");
        return Criar(connectionString, empresaId);
    }

    private static PrecificadorDbContext Criar(string connectionString, int empresaId) =>
        new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options, new Contexto(empresaId));

    private sealed class Contexto(int id) : IEmpresaContext
    {
        public int? EmpresaId => id;
        public int EmpresaIdOuSentinela => id;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}

