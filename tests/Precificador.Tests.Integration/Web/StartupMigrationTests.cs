using System.Net;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class StartupMigrationTests
{
    [Fact]
    public async Task Development_aplica_migrations_em_banco_sqlite_novo_antes_do_setup()
    {
        var databasePath = Path.Combine(Path.GetTempPath(), $"precificador-startup-{Guid.NewGuid():N}.db");

        try
        {
            using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment(Environments.Development);
                builder.ConfigureAppConfiguration((_, configuration) =>
                {
                    configuration.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["ConnectionStrings:Precificador"] = $"Data Source={databasePath}"
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
        }
        finally
        {
            DeleteIfExists(databasePath);
            DeleteIfExists(databasePath + "-shm");
            DeleteIfExists(databasePath + "-wal");
        }
    }

    private static void DeleteIfExists(string path)
    {
        if (!File.Exists(path))
        {
            return;
        }

        for (var tentativa = 0; tentativa < 5; tentativa++)
        {
            try
            {
                File.Delete(path);
                return;
            }
            catch (IOException) when (tentativa < 4)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Yield();
            }
            catch (UnauthorizedAccessException) when (tentativa < 4)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Yield();
            }
            catch (IOException)
            {
                return;
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }
        }
    }
}
