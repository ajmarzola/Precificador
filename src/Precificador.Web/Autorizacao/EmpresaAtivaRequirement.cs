using Microsoft.AspNetCore.Authorization;
using Precificador.Core.Empresas;
using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Empresas;

namespace Precificador.Web.Autorizacao;

public sealed class EmpresaAtivaRequirement : IAuthorizationRequirement;

public sealed class EmpresaAtivaHandler(PrecificadorDbContext dbContext, EmpresaContext empresaContext) : AuthorizationHandler<EmpresaAtivaRequirement>
{
    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, EmpresaAtivaRequirement requirement)
    {
        var usuarioId = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        var empresaId = empresaContext.EmpresaId;
        if (usuarioId is not null && empresaId.HasValue && !string.IsNullOrWhiteSpace(empresaContext.TimeZoneId) && await dbContext.UsuariosEmpresas.AnyAsync(v =>
                v.UsuarioId == usuarioId && v.EmpresaId == empresaId.Value && v.Ativo &&
                dbContext.Empresas.Any(empresa => empresa.Id == empresaId.Value && empresa.Ativo)))
        {
            context.Succeed(requirement);
            return;
        }

        empresaContext.Limpar();
    }
}
