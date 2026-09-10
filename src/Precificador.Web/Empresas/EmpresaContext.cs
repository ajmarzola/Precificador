using Precificador.Core.Empresas;

namespace Precificador.Web.Empresas;

public sealed class EmpresaContext(IHttpContextAccessor httpContextAccessor) : IEmpresaContext
{
    public const string ChaveSession = "EmpresaAtivaId";
    public const string ChaveNomeSession = "EmpresaAtivaNome";
    public int? EmpresaId => httpContextAccessor.HttpContext?.Session.GetInt32(ChaveSession);
    public int EmpresaIdOuSentinela => EmpresaId ?? -1;
    public string? Nome => httpContextAccessor.HttpContext?.Session.GetString(ChaveNomeSession);
    public void Definir(int empresaId, string nome)
    {
        httpContextAccessor.HttpContext!.Session.SetInt32(ChaveSession, empresaId);
        httpContextAccessor.HttpContext.Session.SetString(ChaveNomeSession, nome);
    }
    public void Limpar()
    {
        httpContextAccessor.HttpContext?.Session.Remove(ChaveSession);
        httpContextAccessor.HttpContext?.Session.Remove(ChaveNomeSession);
    }
}
