using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Pages.Insumos;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class ConsultaInsumosTests
{
    [Fact]
    public async Task Listagem_isola_empresa_ordena_pesquisa_e_nao_rastreia_entidades()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var opcoes = new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlite(connection).Options;
        await using (var contexto = new PrecificadorDbContext(opcoes, new ContextoEmpresa(1)))
        {
            await contexto.Database.MigrateAsync();
            contexto.Empresas.Add(Empresa.Criar("Empresa dois"));
            await contexto.SaveChangesAsync();
            contexto.Insumos.AddRange(
                Insumo.Criar(1, "Zíper", CategoriaInsumo.Embalagem, UnidadeMedida.Metro, "Beta"),
                Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata"),
                Insumo.Criar(1, "Farinha", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Caputo"));
            await contexto.SaveChangesAsync();
            await contexto.Database.ExecuteSqlRawAsync("UPDATE Insumos SET Ativo = 0 WHERE Nome = 'Zíper'");
        }
        await using (var contextoEmpresaDois = new PrecificadorDbContext(opcoes, new ContextoEmpresa(2)))
        {
            contextoEmpresaDois.Insumos.Add(Insumo.Criar(2, "Farinha externa", CategoriaInsumo.MateriaPrima, UnidadeMedida.Grama, "Renata"));
            await contextoEmpresaDois.SaveChangesAsync();
        }

        await using var leitura = new PrecificadorDbContext(opcoes, new ContextoEmpresa(1));
        var pagina = new IndexModel(leitura);
        await pagina.OnGetAsync("  farinha  ");

        Assert.Equal(["Caputo", "Renata"], pagina.Insumos.Select(insumo => insumo.Marca));
        Assert.Empty(leitura.ChangeTracker.Entries<Insumo>());

        await pagina.OnGetAsync("RENATA");
        Assert.Single(pagina.Insumos);
        await pagina.OnGetAsync("zíper");
        Assert.Single(pagina.Insumos);
        Assert.False(pagina.Insumos.Single().Ativo);
        await pagina.OnGetAsync("ziper");
        Assert.Empty(pagina.Insumos);
        await pagina.OnGetAsync("   ");
        Assert.Equal(3, pagina.Insumos.Count);

        await using var semEmpresa = new PrecificadorDbContext(opcoes, new ContextoEmpresa(null));
        var paginaSemEmpresa = new IndexModel(semEmpresa);
        await paginaSemEmpresa.OnGetAsync(null);
        Assert.Empty(paginaSemEmpresa.Insumos);
    }

    private sealed class ContextoEmpresa(int? empresaId) : IEmpresaContext
    {
        public int? EmpresaId => empresaId;
        public int EmpresaIdOuSentinela => empresaId ?? -1;
    }
}
