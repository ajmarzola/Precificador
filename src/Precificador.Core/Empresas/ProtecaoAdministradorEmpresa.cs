namespace Precificador.Core.Empresas;

public static class ProtecaoAdministradorEmpresa
{
    public const string Mensagem = "Não é possível remover o último Administrador ativo da Empresa.";

    public static bool PodeRemover(bool administradorAtivo, bool existeOutroAdministradorAtivo)
        => !administradorAtivo || existeOutroAdministradorAtivo;
}
