using Microsoft.Extensions.DependencyInjection;
using Precificador.Infrastructure.Persistence;
using Precificador.Tests.Integration.Web;

namespace Precificador.Tests.Integration.Infrastructure;

public sealed class DatabaseConfigurationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public void Db_context_uses_sqlite_provider_in_test_environment()
    {
        using var scope = factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();

        Assert.Equal("Microsoft.EntityFrameworkCore.Sqlite", context.Database.ProviderName);
    }
}
