using System.Data;
using Microsoft.Data.SqlClient;

namespace Precificador.Tests.Integration;

/// <summary>
/// Cria parâmetros SQL Server decimais com precisão/escala explícitas para arranges de teste
/// via SQL bruto, evitando que o provider infira precisão a partir do valor .NET e arredonde
/// o dado antes de chegar à coluna (a inferência padrão do SqlClient não usa o Precision/Scale
/// configurado no modelo EF).
/// </summary>
internal static class SqlDecimalParameter
{
    public static SqlParameter Criar(string nome, decimal? valor, byte precision, byte scale) => new(nome, SqlDbType.Decimal)
    {
        Precision = precision,
        Scale = scale,
        Value = (object?)valor ?? DBNull.Value
    };
}
