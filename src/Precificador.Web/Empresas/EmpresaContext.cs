using Precificador.Core.Empresas;

namespace Precificador.Web.Empresas;

public sealed class EmpresaContext(IHttpContextAccessor httpContextAccessor) : IEmpresaContext
{
    public const string ChaveSession = "EmpresaAtivaId";
    public int? EmpresaId => httpContextAccessor.HttpContext?.Session.GetInt32(ChaveSession);
    public void Definir(int empresaId) => httpContextAccessor.HttpContext!.Session.SetInt32(ChaveSession, empresaId);
    public void Limpar() => httpContextAccessor.HttpContext?.Session.Remove(ChaveSession);
}
