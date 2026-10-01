using Precificador.Core.Empresas;

namespace Precificador.Tests.Unit.Empresas;

public sealed class ProtecaoAdministradorEmpresaTests
{
    [Theory]
    [InlineData(true, false, false)]
    [InlineData(true, true, true)]
    [InlineData(false, false, true)]
    [InlineData(false, true, true)]
    public void Remocao_preserva_ultimo_administrador_ativo(bool admin, bool outro, bool permitido)
        => Assert.Equal(permitido, ProtecaoAdministradorEmpresa.PodeRemover(admin, outro));
}
