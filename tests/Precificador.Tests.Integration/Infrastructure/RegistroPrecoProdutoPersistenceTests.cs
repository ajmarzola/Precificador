using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class RegistroPrecoProdutoPersistenceTests
{
    [Fact]
    public async Task P1_P2_P4_P8_Round_trip_preserva_historico_append_only()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("RegistroPrecoProduto");
        await using var context = Criar(connectionString, 1);
        await context.Database.MigrateAsync();
        var produto = Produto.Criar(1, "Produto", .3m); context.Produtos.Add(produto); await context.SaveChangesAsync();
        Assert.Empty(await context.RegistrosPrecosProdutos.ToListAsync());
        context.RegistrosPrecosProdutos.AddRange(Criar(1, produto.Id, 12.345678m), Criar(1, produto.Id, 9.876543m));
        await context.SaveChangesAsync(); context.ChangeTracker.Clear();
        var registros = await context.RegistrosPrecosProdutos.OrderBy(r => r.Id).ToListAsync();
        Assert.Equal(2, registros.Count); Assert.Equal(12.345678m, registros[0].CustoReferencia); Assert.Equal(.3m, registros[0].MargemReferencia); Assert.Equal(20m, registros[0].PrecoSugerido); Assert.Equal(10m, registros[0].PrecoPrateleira); Assert.Equal(.1m, registros[0].ReservaComercialReferencia); Assert.Equal(9.876543m, registros[1].CustoReferencia);
    }

    [Fact]
    public async Task P3_P5_P6_P7_Fks_gqf_e_guards_isolam_empresa_e_produto()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("RegistroPrecoProduto");
        await using var empresaUm = Criar(connectionString, 1); await empresaUm.Database.MigrateAsync();
        empresaUm.Empresas.Add(Empresa.Criar("Dois")); var produtoUm = Produto.Criar(1, "Um", .3m); empresaUm.Produtos.Add(produtoUm); await empresaUm.SaveChangesAsync();
        await using var empresaDois = Criar(connectionString, 2); var produtoDois = Produto.Criar(2, "Dois", .3m); empresaDois.Produtos.Add(produtoDois); await empresaDois.SaveChangesAsync();
        empresaUm.RegistrosPrecosProdutos.Add(Criar(1, produtoUm.Id, 1m)); await empresaUm.SaveChangesAsync();
        empresaDois.RegistrosPrecosProdutos.Add(Criar(2, produtoDois.Id, 2m)); await empresaDois.SaveChangesAsync();
        Assert.Single(await empresaUm.RegistrosPrecosProdutos.ToListAsync()); Assert.Single(await empresaDois.RegistrosPrecosProdutos.ToListAsync());
        empresaUm.RegistrosPrecosProdutos.Add(Criar(1, produtoDois.Id, 3m));
        await Assert.ThrowsAsync<InvalidOperationException>(() => empresaUm.SaveChangesAsync());
        await Assert.ThrowsAsync<SqlException>(() => empresaUm.Database.ExecuteSqlAsync($"DELETE FROM Produtos WHERE Id = {produtoUm.Id}"));
        await Assert.ThrowsAsync<SqlException>(() => empresaUm.Database.ExecuteSqlAsync($"DELETE FROM Empresas WHERE Id = 1"));
        empresaUm.ChangeTracker.Clear();
        empresaUm.RegistrosPrecosProdutos.Add(Criar(2, produtoDois.Id, 4m));
        await Assert.ThrowsAsync<InvalidOperationException>(() => empresaUm.SaveChangesAsync());
    }

    private static RegistroPrecoProduto Criar(int empresa, int produto, decimal custo) => RegistroPrecoProduto.Criar(empresa, produto, new DateOnly(2026, 9, 15), custo, .3m, 20m, 10m, .1m);
    private static PrecificadorDbContext Criar(string connectionString, int empresa) => new(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options, new Contexto(empresa));
    private sealed class Contexto(int id) : IEmpresaContext { public int? EmpresaId => id; public int EmpresaIdOuSentinela => id; public string? TimeZoneId => Empresa.TimeZoneIdPadrao; }
}

