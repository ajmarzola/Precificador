using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Acessos;
using Precificador.Infrastructure.Persistence.Configurations;

namespace Precificador.Infrastructure.Persistence;

public static class ViolacaoUnicidadeSolicitacaoAcesso
{
    public static bool EhPendenciaDuplicada(DbUpdateException exception) =>
        exception.Entries.Count == 1 && exception.Entries[0].Entity is SolicitacaoAcessoEmpresa &&
        exception.InnerException is SqlException sql &&
        sql.Errors.Cast<SqlError>().Any(error => error.Number is 2601 or 2627 &&
            error.Message.Contains("'" + SolicitacaoAcessoEmpresaConfiguration.IndicePendente + "'", StringComparison.Ordinal));
}
