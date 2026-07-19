using Microsoft.Data.SqlClient;
using System.Data;

namespace Precificador.Infrastructure.Persistence;

public sealed class DbConnectionFactory
{
    private readonly string _connectionString;

    public DbConnectionFactory(string connectionString)
    {
        _connectionString = string.IsNullOrWhiteSpace(connectionString)
            ? throw new ArgumentNullException(nameof(connectionString))
            : connectionString;
    }

    public IDbConnection Create()
        => new SqlConnection(_connectionString);
}
