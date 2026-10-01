using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autorizacao;
using Precificador.Web.Empresas;

namespace Precificador.Tests.Integration.Web;

public sealed class AdministradorEmpresaAuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Policy_exige_administrador_no_tenant_ativo_e_nega_sem_limpar_contexto_valido()
    {
        var web = new WebTestContext(factory);
        var empresaDois = await web.CriarEmpresaAsync();
        var administrador = await web.CriarUsuarioAsync(PerfilUsuarioEmpresa.Administrador, 1);
        var operacional = await web.CriarUsuarioAsync(PerfilUsuarioEmpresa.Operacional, 1);
        var administradorEmpresaDois = await web.CriarUsuarioAsync(PerfilUsuarioEmpresa.Administrador, empresaDois);
        var systemAdmin = await web.CriarSystemAdminAsync();

        var permitido = await AutorizarAsync(administrador.Id, 1);
        Assert.True(permitido.Autorizado);
        Assert.Equal(1, permitido.EmpresaAtiva);

        var porPerfil = await AutorizarAsync(operacional.Id, 1);
        Assert.False(porPerfil.Autorizado);
        Assert.Equal(1, porPerfil.EmpresaAtiva);

        var crossTenant = await AutorizarAsync(administrador.Id, empresaDois);
        Assert.False(crossTenant.Autorizado);
        Assert.Null(crossTenant.EmpresaAtiva);

        var semEmpresa = await AutorizarAsync(administrador.Id, null);
        Assert.False(semEmpresa.Autorizado);

        var globalSemVinculo = await AutorizarAsync(systemAdmin.Id, null);
        Assert.False(globalSemVinculo.Autorizado);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var vinculo = await db.UsuariosEmpresas.SingleAsync(item => item.UsuarioId == administradorEmpresaDois.Id && item.EmpresaId == empresaDois);
            vinculo.Ativo = false;
            await db.SaveChangesAsync();
        }
        var vinculoInativo = await AutorizarAsync(administradorEmpresaDois.Id, empresaDois);
        Assert.False(vinculoInativo.Autorizado);
        Assert.Null(vinculoInativo.EmpresaAtiva);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            await db.Database.ExecuteSqlRawAsync("UPDATE Empresas SET Ativo = 0 WHERE Id = 1");
        }
        var empresaInativa = await AutorizarAsync(administrador.Id, 1);
        Assert.False(empresaInativa.Autorizado);
        Assert.Null(empresaInativa.EmpresaAtiva);
    }

    private async Task<ResultadoAutorizacao> AutorizarAsync(string usuarioId, int? empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var session = new SessaoEmMemoria();
        var httpContext = new DefaultHttpContext { Session = session };
        var empresaContext = new EmpresaContext(new HttpContextAccessor { HttpContext = httpContext });
        if (empresaId.HasValue) empresaContext.Definir(empresaId.Value, "Empresa de teste", "America/Sao_Paulo");
        var principal = new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier, usuarioId)], "Teste"));
        var authorizationContext = new AuthorizationHandlerContext([new AdministradorEmpresaRequirement()], principal, null);
        var handler = new AdministradorEmpresaHandler(db, empresaContext);
        await handler.HandleAsync(authorizationContext);
        return new ResultadoAutorizacao(authorizationContext.HasSucceeded, empresaContext.EmpresaId);
    }

    private sealed record ResultadoAutorizacao(bool Autorizado, int? EmpresaAtiva);

    private sealed class SessaoEmMemoria : ISession
    {
        private readonly Dictionary<string, byte[]> valores = [];
        public bool IsAvailable => true;
        public string Id => "sessao-teste";
        public IEnumerable<string> Keys => valores.Keys;
        public Task LoadAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public void Clear() => valores.Clear();
        public void Remove(string key) => valores.Remove(key);
        public void Set(string key, byte[] value) => valores[key] = value;
        public bool TryGetValue(string key, out byte[] value) => valores.TryGetValue(key, out value!);
    }
}
