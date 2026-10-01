using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class PerfilUsuarioEmpresaMigrationTests
{
    private const string MigracaoAnterior = "20260922130351_AddDesgasteEquipamentosCategoria";
    private const string MigracaoFt003 = "20261001110539_AddPerfilUsuarioEmpresa";

    [Fact]
    public async Task Upgrade_preserva_usuario_e_vinculo_legado_como_operacional_sem_criar_system_admin()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PerfilUsuarioEmpresa");
        await using var context = CriarContexto(connectionString);
        await context.Database.GetService<IMigrator>().MigrateAsync(MigracaoAnterior);
        var usuario = new UsuarioAplicacao { Id = Guid.NewGuid().ToString(), UserName = "legado@teste.local", NormalizedUserName = "LEGADO@TESTE.LOCAL" };
        context.Users.Add(usuario);
        await context.SaveChangesAsync();
        await context.Database.ExecuteSqlRawAsync(
            "INSERT INTO UsuariosEmpresas (UsuarioId, EmpresaId, Ativo) VALUES ({0}, 1, 1)", usuario.Id);

        await context.Database.GetService<IMigrator>().MigrateAsync(MigracaoFt003);

        Assert.Equal((int)PerfilUsuarioEmpresa.Operacional, await context.Database.SqlQueryRaw<int>(
            "SELECT Perfil AS Value FROM UsuariosEmpresas WHERE UsuarioId = {0}", usuario.Id).SingleAsync());
        Assert.Single(await context.Users.Where(item => item.Id == usuario.Id).ToListAsync());
        Assert.Empty(await context.UserRoles.ToListAsync());
    }

    [Fact]
    public async Task Banco_vazio_aplica_migration_sem_default_administrativo()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("PerfilUsuarioEmpresaVazio");
        await using var context = CriarContexto(connectionString);
        await context.Database.MigrateAsync();

        var defaultPerfil = await context.Database.SqlQueryRaw<string>("""
            SELECT COALESCE(dc.definition, '') AS Value
            FROM sys.columns c
            LEFT JOIN sys.default_constraints dc ON c.default_object_id = dc.object_id
            WHERE c.object_id = OBJECT_ID('UsuariosEmpresas') AND c.name = 'Perfil'
            """).SingleAsync();
        Assert.Equal(string.Empty, defaultPerfil);
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    private static PrecificadorDbContext CriarContexto(string connectionString) => new(
        new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connectionString).Options,
        new ContextoEmpresaTeste());

    private sealed class ContextoEmpresaTeste : IEmpresaContext
    {
        public int? EmpresaId => null;
        public int EmpresaIdOuSentinela => -1;
        public string? TimeZoneId => null;
    }
}
