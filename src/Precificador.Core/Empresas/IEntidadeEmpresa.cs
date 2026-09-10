namespace Precificador.Core.Empresas;

public interface IEntidadeEmpresa
{
    int EmpresaId { get; }
    void DefinirEmpresa(int empresaId);
}
