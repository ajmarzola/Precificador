using Precificador.Core.Empresas;

namespace Precificador.Tests.Integration.Web;

internal sealed class ContextoEmpresaTeste(int empresaId) : IEmpresaContext
{
    public int? EmpresaId => empresaId;
    public int EmpresaIdOuSentinela => empresaId;
    public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
}
