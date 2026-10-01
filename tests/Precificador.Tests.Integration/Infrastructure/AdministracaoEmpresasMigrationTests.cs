using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Precificador.Core.Acessos;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Persistence;
using Precificador.Tests.Integration.Web;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class AdministracaoEmpresasMigrationTests
{
    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Upgrade_UC040_preserva_legado_seed_condicional_keyring_e_dados_tenant(bool renomeada)
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("UC039Upgrade");
        await using var db = new PrecificadorDbContext(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(cs).Options, new ContextoEmpresaTeste(1));
        await db.GetService<IMigrator>().MigrateAsync("20261001140025_UC040_PersistirDataProtectionKeys");
        if (renomeada) await db.Database.ExecuteSqlRawAsync("UPDATE Empresas SET Nome = N'Cliente real', NomeNormalizado = N'CLIENTE REAL' WHERE Id = 1");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PasswordHash, SecurityStamp, ConcurrencyStamp, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES ('legado', 'legado@teste.local', 'LEGADO@TESTE.LOCAL', 'legado@teste.local', 'LEGADO@TESTE.LOCAL', 1, 'hash-preservado', 'stamp-preservado', 'concorrencia', 0, 0, 0, 0);
            INSERT INTO UsuariosEmpresas (UsuarioId, EmpresaId, Ativo, Perfil) VALUES ('legado', 1, 1, 1);
            INSERT INTO SolicitacoesAcessoEmpresas (NomeEmpresa, NomeEmpresaNormalizado, NomeResponsavel, EmailResponsavel, EmailResponsavelNormalizado, DataSolicitacaoUtc, Situacao)
            VALUES (N'Pedido legado', N'PEDIDO LEGADO', N'Ana', 'ana@teste.local', 'ANA@TESTE.LOCAL', '2026-10-01T12:00:00+00:00', 1);
            INSERT INTO DataProtectionKeys (FriendlyName, Xml) VALUES ('chave-legada', '<key>preservar</key>');
            INSERT INTO Insumos (EmpresaId, Nome, NomeNormalizado, MarcaNormalizada, Categoria, UnidadeBase, Ativo, IdentidadeConsolidada)
            VALUES (1, N'Insumo legado', N'INSUMO LEGADO', '', 1, 1, 1, 0);
            """);
        await db.Database.MigrateAsync();
        var empresa = await db.Empresas.SingleAsync();
        Assert.Equal(!renomeada, empresa.EhTecnica); Assert.True(empresa.Ativo); Assert.Null(empresa.EncerradaEmUtc);
        Assert.Equal(renomeada ? "Cliente real" : "Empresa inicial", empresa.Nome);
        var user = await db.Users.SingleAsync(); Assert.Equal("hash-preservado", user.PasswordHash); Assert.Equal("stamp-preservado", user.SecurityStamp);
        var vinculo = await db.UsuariosEmpresas.SingleAsync(); Assert.Equal(Precificador.Infrastructure.Autenticacao.PerfilUsuarioEmpresa.Operacional, vinculo.Perfil);
        Assert.Empty(await db.UserRoles.ToListAsync());
        var pedido = await db.SolicitacoesAcessoEmpresas.SingleAsync();
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Pendente, pedido.Situacao);
        Assert.Null(pedido.EmpresaId); Assert.Null(pedido.DataDecisaoUtc); Assert.Null(pedido.DecididaPorUsuarioId); Assert.Null(pedido.MotivoRecusa);
        Assert.Equal("<key>preservar</key>", (await db.DataProtectionKeys.SingleAsync()).Xml);
        Assert.Equal("Insumo legado", (await db.Insumos.SingleAsync()).Nome);
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task Banco_vazio_aplica_tudo_constraints_e_FKs_restrict_protegem_invariantes()
    {
        var cs = await SqlServerTestDatabase.CriarConnectionStringAsync("UC039Clean");
        await using var db = new PrecificadorDbContext(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(cs).Options, new ContextoEmpresaTeste(1));
        await db.Database.MigrateAsync();
        Assert.True((await db.Empresas.SingleAsync()).EhTecnica);
        var pedido = SolicitacaoAcessoEmpresa.Criar("Pedido", "Ana", "ana@teste.local", null, DateTimeOffset.UtcNow);
        db.SolicitacoesAcessoEmpresas.Add(pedido); await db.SaveChangesAsync();
        foreach (var sql in new[]
        {
            "UPDATE Empresas SET EncerradaEmUtc = SYSDATETIMEOFFSET() WHERE Id = 1",
            "UPDATE SolicitacoesAcessoEmpresas SET Situacao = 2",
            "UPDATE SolicitacoesAcessoEmpresas SET Situacao = 3",
            "UPDATE SolicitacoesAcessoEmpresas SET EmpresaId = 1",
            "UPDATE SolicitacoesAcessoEmpresas SET MotivoRecusa = 'motivo'"
        })
        {
            Assert.Equal(547, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync(sql))).Number);
        }
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES ('decisor', 'global@teste.local', 'GLOBAL@TESTE.LOCAL', 'global@teste.local', 'GLOBAL@TESTE.LOCAL', 0, 0, 0, 0, 0);
            UPDATE SolicitacoesAcessoEmpresas SET Situacao = 2, EmpresaId = 1, DataDecisaoUtc = SYSDATETIMEOFFSET(), DecididaPorUsuarioId = 'decisor';
            """);
        Assert.Equal(547, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM AspNetUsers WHERE Id = 'decisor'"))).Number);
        Assert.Equal(547, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync("DELETE FROM Empresas WHERE Id = 1"))).Number);
        Assert.Equal(2601, (await Assert.ThrowsAsync<SqlException>(() => db.Database.ExecuteSqlRawAsync("""
            INSERT INTO AspNetUsers (Id, UserName, NormalizedUserName, Email, NormalizedEmail, EmailConfirmed, PhoneNumberConfirmed, TwoFactorEnabled, LockoutEnabled, AccessFailedCount)
            VALUES ('duplicado', 'outro@teste.local', 'OUTRO@TESTE.LOCAL', 'global@teste.local', 'GLOBAL@TESTE.LOCAL', 0, 0, 0, 0, 0);
            """))).Number);
    }
}
