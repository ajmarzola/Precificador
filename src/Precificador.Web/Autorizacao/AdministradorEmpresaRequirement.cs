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
        if (usuarioId is not null && empresaId.HasValue && !string.IsNullOrWhiteSpace(empresaContext.TimeZoneId) &&
            await dbContext.UsuariosEmpresas.AnyAsync(v =>
                v.UsuarioId == usuarioId && v.EmpresaId == empresaId.Value && v.Ativo &&
                v.Perfil == PerfilUsuarioEmpresa.Administrador &&
                dbContext.Empresas.Any(empresa => empresa.Id == empresaId.Value && empresa.Ativo)))
        {
            context.Succeed(requirement);
            return;
        }

        empresaContext.Limpar();
    }
}
