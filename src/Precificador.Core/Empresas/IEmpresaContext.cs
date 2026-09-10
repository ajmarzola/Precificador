namespace Precificador.Core.Empresas;

public interface IEmpresaContext
{
    int? EmpresaId { get; }
    int EmpresaIdOuSentinela { get; }
}
