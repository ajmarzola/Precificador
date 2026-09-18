using Microsoft.Data.SqlClient;
using Testcontainers.MsSql;

namespace Precificador.Tests.Integration.Infrastructure;

/// <summary>
/// Um único container SQL Server compartilhado por toda a sessão de testes de integração.
/// Cada chamador recebe um database novo e isolado dentro do mesmo container.
/// O container é encerrado automaticamente pelo Ryuk (Testcontainers) ao final do processo de teste.
/// </summary>
internal static class SqlServerTestDatabase
{
    private static readonly Lazy<Task<MsSqlContainer>> ContainerLazy = new(IniciarContainerAsync);

    private static async Task<MsSqlContainer> IniciarContainerAsync()
    {
        var container = new MsSqlBuilder()
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .Build();
        await container.StartAsync();
        return container;
    }

    /// <summary>Gera uma connection string para um database novo e isolado dentro do container compartilhado.</summary>
    public static async Task<string> CriarConnectionStringAsync(string prefixoDatabase)
    {
        var container = await ContainerLazy.Value;
        var builder = new SqlConnectionStringBuilder(container.GetConnectionString())
        {
            InitialCatalog = $"{prefixoDatabase}_{Guid.NewGuid():N}",
            TrustServerCertificate = true
        };
        return builder.ConnectionString;
    }
}
