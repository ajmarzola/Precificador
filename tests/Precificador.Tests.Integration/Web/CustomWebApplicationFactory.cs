using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Persistence;
using Precificador.Tests.Integration.Infrastructure;

namespace Precificador.Tests.Integration.Web;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
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
        var connectionString = SqlServerTestDatabase.CriarConnectionStringAsync("Web").GetAwaiter().GetResult();

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

            services.AddDbContext<PrecificadorDbContext>(options => options.UseSqlServer(connectionString));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>().Database.Migrate();
        return host;
    }

    private sealed class DataOperacionalEmpresaFixa(DateOnly hoje) : IDataOperacionalEmpresa
    {
        public DateOnly Hoje => hoje;
    }
}

