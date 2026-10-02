using System.Data;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autorizacao;

namespace Precificador.Web.Empresas;

public sealed class ServicoIdentidadeVisualEmpresa(PrecificadorDbContext db, EmpresaContext empresa, IHttpContextAccessor http)
{
    public Task<bool> SalvarAsync(string cor, byte[]? logo, bool remover)
    {
        cor = IdentidadeVisualEmpresa.NormalizarCor(cor);
        if (logo is not null && remover) throw new ArgumentException("Envie um novo logo ou escolha remover o logo atual, não as duas opções.");
        var tipo = logo is null ? null : IdentidadeVisualEmpresa.DetectarContentType(logo);
        return TransacionarAsync(async () =>
        {
            var identidade = await db.IdentidadesVisuaisEmpresas.SingleOrDefaultAsync();
            if (identidade is null)
            {
                identidade = IdentidadeVisualEmpresa.Criar(empresa.EmpresaId!.Value, cor);
                db.IdentidadesVisuaisEmpresas.Add(identidade);
            }
            else identidade.AtualizarCor(cor);
            if (logo is not null) identidade.DefinirLogo(logo, tipo!);
            else if (remover) identidade.RemoverLogo();
        });
    }

    public Task<bool> RestaurarAsync() => TransacionarAsync(async () =>
    {
        var identidade = await db.IdentidadesVisuaisEmpresas.SingleOrDefaultAsync();
        if (identidade is not null) db.IdentidadesVisuaisEmpresas.Remove(identidade);
    });

    private async Task<bool> TransacionarAsync(Func<Task> alterar)
    {
        var empresaId = empresa.EmpresaId;
        var usuario = http.HttpContext?.User;
        var atorId = usuario?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (empresaId is null || atorId is null || usuario!.IsInRole(NomesAutorizacao.SystemAdmin)) return false;
        try
        {
            return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
            {
                db.ChangeTracker.Clear();
                await using var transacao = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                var recurso = $"Precificador.UC037.Empresa.{empresaId}";
                await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sp_getapplock @Resource = {recurso}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000; IF @r < 0 THROW 51037, 'Identidade visual temporariamente indisponível.', 1;");
                if (!await db.Empresas.AnyAsync(x => x.Id == empresaId && x.Ativo) ||
                    !await db.UsuariosEmpresas.AnyAsync(x => x.EmpresaId == empresaId && x.UsuarioId == atorId && x.Ativo && x.Perfil == PerfilUsuarioEmpresa.Administrador)) return false;
                await alterar();
                await db.SaveChangesAsync();
                await transacao.CommitAsync();
                return true;
            });
        }
        catch (SqlException ex) when (ex.Number == 51037)
        {
            throw new InvalidOperationException("Identidade visual temporariamente indisponível. Tente novamente.", ex);
        }
    }
}
