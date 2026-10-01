using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Precificador.Core.Acessos;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autenticacao;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class TokensContaPersistenceTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Migration_limpa_upgrade_preserva_dados_e_keys_globais(bool upgrade)
    {
        var connection = await SqlServerTestDatabase.CriarConnectionStringAsync("KeysMigration");
        await using var db = new PrecificadorDbContext(new DbContextOptionsBuilder<PrecificadorDbContext>().UseSqlServer(connection).Options, new SemEmpresa());
        if (upgrade)
        {
            await db.GetService<IMigrator>().MigrateAsync("20261001125004_AddSolicitacaoAcessoEmpresa");
            db.Users.Add(new UsuarioAplicacao { Id = "legado", UserName = "legado@teste.local", PasswordHash = "hash-preservado" });
            db.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = "legado", EmpresaId = 1, Ativo = true, Perfil = PerfilUsuarioEmpresa.Operacional });
            db.SolicitacoesAcessoEmpresas.Add(SolicitacaoAcessoEmpresa.Criar("Empresa", "Responsável", "pedido@teste.local", "preservada", DateTimeOffset.UtcNow));
            await db.SaveChangesAsync();
        }
        await db.Database.MigrateAsync();
        Assert.Empty(await db.Database.GetPendingMigrationsAsync());
        Assert.Single(await db.Empresas.ToListAsync());
        Assert.Equal(upgrade ? 1 : 0, await db.Users.CountAsync());
        Assert.Equal(upgrade ? 1 : 0, await db.UsuariosEmpresas.CountAsync());
        Assert.Equal(upgrade ? 1 : 0, await db.SolicitacoesAcessoEmpresas.CountAsync());
        if (upgrade)
        {
            Assert.Equal("hash-preservado", (await db.Users.SingleAsync()).PasswordHash);
            Assert.Equal("preservada", (await db.SolicitacoesAcessoEmpresas.SingleAsync()).Observacao);
        }
        Assert.Empty(await db.DataProtectionKeys.ToListAsync());
        var tipo = db.Model.FindEntityType(typeof(DataProtectionKey))!;
        Assert.Null(tipo.FindProperty("EmpresaId"));
        Assert.Empty(tipo.GetDeclaredQueryFilters());
        Assert.Equal("DataProtectionKeys", tipo.GetTableName());
    }

    [Fact]
    public async Task Nova_instancia_mesmo_banco_valida_tokens_banco_distinto_nao_valida()
    {
        var connection = await SqlServerTestDatabase.CriarConnectionStringAsync("TokensRestart");
        string id, ativacao, reset;
        using (var provider = Servicos(connection))
        {
            using var scope = provider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            await db.Database.MigrateAsync();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var user = new UsuarioAplicacao { Email = "token@teste.local", UserName = "token@teste.local" };
            Assert.True((await manager.CreateAsync(user)).Succeeded);
            id = user.Id;
            ativacao = await manager.GenerateUserTokenAsync(user, TokensConta.Ativacao, TokensConta.PurposeAtivacao);
            reset = await manager.GeneratePasswordResetTokenAsync(user);
            Assert.NotEmpty(await db.DataProtectionKeys.ToListAsync());
            Assert.Empty(await db.UserTokens.ToListAsync());
            Assert.Equal(TimeSpan.FromHours(48), scope.ServiceProvider.GetRequiredService<IOptions<OpcoesTokenAtivacao>>().Value.TokenLifespan);
            Assert.Equal(TimeSpan.FromHours(1), scope.ServiceProvider.GetRequiredService<IOptions<OpcoesTokenRecuperacao>>().Value.TokenLifespan);
            Assert.Equal(TokensConta.Recuperacao, manager.Options.Tokens.PasswordResetTokenProvider);
            Assert.Equal("Precificador", scope.ServiceProvider.GetRequiredService<IOptions<DataProtectionOptions>>().Value.ApplicationDiscriminator);
        }
        using (var provider = Servicos(connection))
        {
            using var scope = provider.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var user = (await manager.FindByIdAsync(id))!;
            Assert.True(await manager.VerifyUserTokenAsync(user, TokensConta.Ativacao, TokensConta.PurposeAtivacao, ativacao));
            Assert.True(await manager.VerifyUserTokenAsync(user, TokensConta.Recuperacao, "ResetPassword", reset));
            Assert.False(await manager.VerifyUserTokenAsync(user, TokensConta.Ativacao, "outro-purpose", ativacao));
            Assert.False(await manager.VerifyUserTokenAsync(user, TokensConta.Ativacao, TokensConta.PurposeAtivacao, reset));
            Assert.False(await manager.VerifyUserTokenAsync(user, TokensConta.Recuperacao, "ResetPassword", ativacao));
            // Mesmo usuário/stamp, outro key ring: a rejeição decorre das chaves.
            var outro = await SqlServerTestDatabase.CriarConnectionStringAsync("OutroKeyRing");
            using var providerOutro = Servicos(outro);
            using var scopeOutro = providerOutro.CreateScope();
            await scopeOutro.ServiceProvider.GetRequiredService<PrecificadorDbContext>().Database.MigrateAsync();
            var managerOutro = scopeOutro.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            Assert.False(await managerOutro.VerifyUserTokenAsync(user, TokensConta.Ativacao, TokensConta.PurposeAtivacao, ativacao));
        }
    }

    private static ServiceProvider Servicos(string connection)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IEmpresaContext, SemEmpresa>();
        services.AddDbContext<PrecificadorDbContext>(options => options.UseSqlServer(connection));
        services.AddIdentityCore<UsuarioAplicacao>(options => options.Tokens.PasswordResetTokenProvider = TokensConta.Recuperacao)
            .AddEntityFrameworkStores<PrecificadorDbContext>()
            .AddTokenProvider<TokenAtivacaoProvider>(TokensConta.Ativacao)
            .AddTokenProvider<TokenRecuperacaoProvider>(TokensConta.Recuperacao);
        services.AddDataProtection().SetApplicationName("Precificador").PersistKeysToDbContext<PrecificadorDbContext>();
        return services.BuildServiceProvider();
    }
    private sealed class SemEmpresa : IEmpresaContext
    {
        public int? EmpresaId => null;
        public int EmpresaIdOuSentinela => -1;
        public string? TimeZoneId => null;
    }
}
