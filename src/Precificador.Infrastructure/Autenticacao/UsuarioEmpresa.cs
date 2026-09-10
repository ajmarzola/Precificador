namespace Precificador.Infrastructure.Autenticacao;

public sealed class UsuarioEmpresa
{
    public string UsuarioId { get; set; } = null!;
    public int EmpresaId { get; set; }
    public bool Ativo { get; set; }
}
