using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection connection = new("Data Source=:memory:");
    private readonly DateOnly? dataOperacionalFixa;

    public CustomWebApplicationFactory()
    {
    }

    internal CustomWebApplicationFactory(DateOnly dataOperacionalFixa)
    {
        this.dataOperacionalFixa = dataOperacionalFixa;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureLogging(logging => logging.ClearProviders());
        builder.ConfigureServices(services =>
        {
            services.AddDataProtection().UseEphemeralDataProtectionProvider();
            services.RemoveAll<DbContextOptions<PrecificadorDbContext>>();
            if (dataOperacionalFixa.HasValue)
            {
                services.RemoveAll<IDataOperacionalEmpresa>();
                services.AddScoped<IDataOperacionalEmpresa>(_ => new DataOperacionalEmpresaFixa(dataOperacionalFixa.Value));
            }

            connection.Open();
            services.AddDbContext<PrecificadorDbContext>(options => options.UseSqlite(connection));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>().Database.Migrate();
        return host;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            connection.Dispose();
        }
    }

    private sealed class DataOperacionalEmpresaFixa(DateOnly hoje) : IDataOperacionalEmpresa
    {
        public DateOnly Hoje => hoje;
    }
}
