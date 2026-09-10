using Microsoft.AspNetCore.Authorization;
using Precificador.Core.Empresas;

namespace Precificador.Web.Autorizacao;

public sealed class EmpresaAtivaRequirement : IAuthorizationRequirement;

public sealed class EmpresaAtivaHandler(IEmpresaContext empresaContext) : AuthorizationHandler<EmpresaAtivaRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, EmpresaAtivaRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true && empresaContext.EmpresaId.HasValue)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
