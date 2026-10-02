using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autorizacao;

namespace Precificador.Web.Empresas;

public sealed record IdentidadeVisualEfetiva(string CorPrimaria, bool TemLogo, bool Padrao);

public sealed class IdentidadeVisualEmpresaAtual(PrecificadorDbContext db, EmpresaContext empresa, IHttpContextAccessor http)
{
    private Task<IdentidadeVisualEfetiva?>? consulta;
    public Task<IdentidadeVisualEfetiva?> ObterAsync() => consulta ??= ConsultarAsync();
    private async Task<IdentidadeVisualEfetiva?> ConsultarAsync()
    {
        var usuario = http.HttpContext?.User;
        if (usuario?.Identity?.IsAuthenticated != true || usuario.IsInRole(NomesAutorizacao.SystemAdmin) ||
            empresa.EmpresaId is null || http.HttpContext!.Request.Path.StartsWithSegments("/Admin")) return null;
        return await db.IdentidadesVisuaisEmpresas.AsNoTracking()
            .Select(x => new IdentidadeVisualEfetiva(x.CorPrimaria, x.LogoConteudo != null, false))
            .SingleOrDefaultAsync() ?? new(IdentidadeVisualEmpresa.CorPadrao, false, true);
    }
}
