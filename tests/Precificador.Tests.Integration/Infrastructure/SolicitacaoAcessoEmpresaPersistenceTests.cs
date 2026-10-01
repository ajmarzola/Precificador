using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Precificador.Core.Acessos;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class SolicitacaoAcessoEmpresaPersistenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Migration_banco_vazio_e_upgrade_FT003_preservam_empresa_e_identity(bool upgrade)
    {
        await using var db = await CriarAsync(migrar: false);
        if (upgrade)
        {
            await db.GetService<IMigrator>().MigrateAsync("20261001110539_AddPerfilUsuarioEmpresa");
            db.Users.Add(new UsuarioAplicacao { Id = "legado", UserName = "legado@teste.local" });
            db.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = "legado", EmpresaId = 1, Ativo = true, Perfil = PerfilUsuarioEmpresa.Operacional });
            await db.SaveChangesAsync();
        }
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Single(await db.Empresas.ToListAsync());
        Assert.Equal(upgrade ? 1 : 0, await db.Users.CountAsync());
        Assert.Equal(upgrade ? 1 : 0, await db.UsuariosEmpresas.CountAsync());
        Assert.Empty(await db.Roles.ToListAsync());
        Assert.Empty(await db.UserRoles.ToListAsync());
        Assert.Empty(await db.SolicitacoesAcessoEmpresas.ToListAsync());
        if (upgrade)
        {
            Assert.Equal("legado@teste.local", (await db.Users.SingleAsync()).UserName);
            Assert.Equal(PerfilUsuarioEmpresa.Operacional, (await db.UsuariosEmpresas.SingleAsync()).Perfil);
        }
        var tipo = db.Model.FindEntityType(typeof(SolicitacaoAcessoEmpresa))!;
        Assert.Empty(tipo.GetDeclaredQueryFilters());
        Assert.True(tipo.FindProperty("EmpresaId")!.IsNullable); // referência global de decisão introduzida pela UC039
        Assert.False(typeof(IEntidadeEmpresa).IsAssignableFrom(typeof(SolicitacaoAcessoEmpresa)));
        db.SolicitacoesAcessoEmpresas.Add(Pedido());
        await db.SaveChangesAsync();
        Assert.Single(await db.SolicitacoesAcessoEmpresas.ToListAsync());
    }

    [Fact]
    public async Task Indice_filtrado_rejeita_somente_pendencia_do_mesmo_par()
    {
        await using var db = await CriarAsync();
        db.Add(Pedido());
        await db.SaveChangesAsync();
        db.Add(Pedido());
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.True(ViolacaoUnicidadeSolicitacaoAcesso.EhPendenciaDuplicada(exception));
        db.ChangeTracker.Clear();
        db.Add(Pedido("Outra Empresa"));
        db.Add(Pedido(email: "outro@teste.local"));
        await db.SaveChangesAsync();
        Assert.Equal(3, await db.SolicitacoesAcessoEmpresas.CountAsync());
        db.Users.Add(new UsuarioAplicacao { Id = "decisor", UserName = "decisor@teste.local" });
        await db.SaveChangesAsync();
        foreach (var solicitacao in await db.SolicitacoesAcessoEmpresas.ToListAsync())
            solicitacao.Recusar("decisor", DateTimeOffset.UtcNow, null);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        db.Add(Pedido());
        await db.SaveChangesAsync();
        Assert.Equal(4, await db.SolicitacoesAcessoEmpresas.CountAsync());
    }

    [Theory]
    [InlineData("99")]
    [InlineData("NULL")]
    public async Task Banco_rejeita_situacao_invalida_ou_ausente(string situacao)
    {
        await using var db = await CriarAsync();
        db.Add(Pedido());
        await db.SaveChangesAsync();
        var exception = await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(
            situacao == "NULL" ? "UPDATE SolicitacoesAcessoEmpresas SET Situacao = NULL" : "UPDATE SolicitacoesAcessoEmpresas SET Situacao = 99"));
        Assert.Equal(situacao == "NULL" ? 515 : 547, exception.Number);
    }

    [Fact]
    public async Task Violacao_de_outro_indice_unico_nao_e_pendencia_duplicada()
    {
        await using var db = await CriarAsync();
        await db.Database.ExecuteSqlRawAsync("CREATE UNIQUE INDEX UX_Teste_Outro ON SolicitacoesAcessoEmpresas (NomeResponsavel)");
        db.Add(Pedido());
        await db.SaveChangesAsync();
        db.Add(Pedido("Outra"));
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => db.SaveChangesAsync());
        Assert.False(ViolacaoUnicidadeSolicitacaoAcesso.EhPendenciaDuplicada(exception));
        Assert.False(ViolacaoUnicidadeSolicitacaoAcesso.EhPendenciaDuplicada(new DbUpdateException("outro erro")));
    }

    private static SolicitacaoAcessoEmpresa Pedido(string nome = "Empresa", string email = "joao@teste.local") =>
        SolicitacaoAcessoEmpresa.Criar(nome, "João", email, null, DateTimeOffset.Parse("2026-10-01T12:00:00Z"));

    private static async Task<PrecificadorDbContext> CriarAsync(bool migrar = true)
    {
        var connection = await SqlServerTestDatabase.CriarConnectionStringAsync("SolicitacaoAcesso");
        var db = new PrecificadorDbContext(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connection).Options, new SemEmpresa());
        if (migrar) await db.Database.MigrateAsync();
        return db;
    }

    private sealed class SemEmpresa : IEmpresaContext
    {
        public int? EmpresaId => null;
        public int EmpresaIdOuSentinela => -1;
        public string? TimeZoneId => null;
    }
}
