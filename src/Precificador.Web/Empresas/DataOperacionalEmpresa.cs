using Precificador.Core.Empresas;

namespace Precificador.Web.Empresas;

public sealed class DataOperacionalEmpresa(IEmpresaContext empresaContext, TimeProvider timeProvider) : IDataOperacionalEmpresa
{
    public DateOnly Hoje
    {
        get
        {
            var timeZoneId = empresaContext.TimeZoneId;
            if (!empresaContext.EmpresaId.HasValue || string.IsNullOrWhiteSpace(timeZoneId))
            {
                throw new InvalidOperationException("Uma empresa ativa com fuso horário é necessária para obter a data operacional.");
            }

            TimeZoneInfo timeZone;
            try
            {
                timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException exception)
            {
                throw new InvalidOperationException("O fuso horário da empresa ativa é inválido.", exception);
            }
            catch (InvalidTimeZoneException exception)
            {
                throw new InvalidOperationException("O fuso horário da empresa ativa é inválido.", exception);
            }

            var instanteEmpresa = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
            return DateOnly.FromDateTime(instanteEmpresa.DateTime);
        }
    }
}
