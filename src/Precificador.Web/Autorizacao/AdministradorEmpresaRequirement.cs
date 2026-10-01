using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Empresas;
using System.Security.Claims;

namespace Precificador.Web.Autorizacao;

public sealed class AdministradorEmpresaRequirement : IAuthorizationRequirement;

public sealed class AdministradorEmpresaHandler(PrecificadorDbContext dbContext, EmpresaContext empresaContext)
    : AuthorizationHandler<AdministradorEmpresaRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, AdministradorEmpresaRequirement requirement)
    {
        var usuarioId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var empresaId = empresaContext.EmpresaId;
        if (usuarioId is null || !empresaId.HasValue || string.IsNullOrWhiteSpace(empresaContext.TimeZoneId))
            return;

        if (!await dbContext.Empresas.AnyAsync(empresa => empresa.Id == empresaId.Value && empresa.Ativo))
        {
            empresaContext.Limpar();
            return;
        }

        var vinculo = await dbContext.UsuariosEmpresas.SingleOrDefaultAsync(v =>
            v.UsuarioId == usuarioId && v.EmpresaId == empresaId.Value);
        if (vinculo is null || !vinculo.Ativo)
        {
            empresaContext.Limpar();
            return;
        }

        if (vinculo.Perfil == PerfilUsuarioEmpresa.Administrador)
            context.Succeed(requirement);
    }
}
