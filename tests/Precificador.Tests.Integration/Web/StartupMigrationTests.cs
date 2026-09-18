using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Precificador.Infrastructure.Persistence;
using Precificador.Tests.Integration.Infrastructure;

namespace Precificador.Tests.Integration.Web;

public sealed class StartupMigrationTests
{
    [Fact]
    public async Task Development_aplica_migrations_em_banco_sql_server_novo_antes_do_setup()
    {
        var connectionString = await SqlServerTestDatabase.CriarConnectionStringAsync("StartupMigration");

        using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment(Environments.Development);
            builder.ConfigureAppConfiguration((_, configuration) =>
            {
                configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Precificador"] = connectionString
                });
            });
        });

        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/Setup");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();

        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(0, await context.Users.CountAsync());
        Assert.True(await context.Empresas.AnyAsync());

        // Segunda execução deve ser idempotente: nenhuma migration pendente e nenhum erro.
        await context.Database.MigrateAsync();
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
        Assert.Equal(1, await context.Empresas.CountAsync());
    }
}
