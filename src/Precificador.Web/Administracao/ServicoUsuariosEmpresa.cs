using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autenticacao;
using Precificador.Web.Autorizacao;
using Precificador.Web.Empresas;

namespace Precificador.Web.Administracao;

public sealed record UsuarioEmpresaConsulta(string UsuarioId, string? Email, PerfilUsuarioEmpresa Perfil,
    bool Ativo, bool SemSenha);

public sealed class ServicoUsuariosEmpresa(PrecificadorDbContext db, UserManager<UsuarioAplicacao> usuarios,
    ServicoConta conta, EmpresaContext empresaContext, IHttpContextAccessor http)
{
    private const string AlvoProibido = "Este e-mail não pode ser vinculado à Empresa.";
    private IQueryable<UsuarioEmpresa> Vinculos => db.UsuariosEmpresas.Where(x => x.EmpresaId == empresaContext.EmpresaIdOuSentinela);

    public Task<List<UsuarioEmpresaConsulta>> ListarAsync(string situacao)
        => Consultar(Vinculos.Where(x => situacao == "Todos" || x.Ativo == (situacao == "Ativos"))).ToListAsync();

    public Task<UsuarioEmpresaConsulta?> ConsultarAsync(string usuarioId)
        => Consultar(Vinculos.Where(x => x.UsuarioId == usuarioId)).SingleOrDefaultAsync();

    private IQueryable<UsuarioEmpresaConsulta> Consultar(IQueryable<UsuarioEmpresa> vinculos)
        => from v in vinculos.AsNoTracking()
           join u in db.Users.AsNoTracking() on v.UsuarioId equals u.Id
           orderby v.Ativo descending, u.NormalizedEmail
           select new UsuarioEmpresaConsulta(u.Id, u.Email, v.Perfil, v.Ativo, u.PasswordHash == null);

    public async Task<ResultadoAdministracao> AdicionarAsync(string email, PerfilUsuarioEmpresa perfil)
    {
        email = email.Trim();
        if (email.Length == 0 || email.Length > 256 || !new EmailAddressAttribute().IsValid(email))
            return Erro("Informe um e-mail válido.");
        if (!PerfilValido(perfil)) return Erro("Selecione um perfil válido.");
        UsuarioAplicacao? destinatario = null;
        var resultado = await TransacionarAsync(async () =>
        {
            destinatario = await usuarios.FindByEmailAsync(email);
            if (destinatario is null)
            {
                destinatario = new UsuarioAplicacao { Email = email, UserName = email };
                var criacao = await usuarios.CreateAsync(destinatario);
                if (!criacao.Succeeded)
                {
                    if (criacao.Errors.Any() && criacao.Errors.All(x => x.Code is "DuplicateEmail" or "DuplicateUserName"))
                        throw new CorridaIdentidadeException();
                    return Erro("Não foi possível criar a identidade. Revise o e-mail informado.");
                }
            }
            if (await usuarios.IsInRoleAsync(destinatario, NomesAutorizacao.SystemAdmin)) return Erro(AlvoProibido);
            var vinculo = await Vinculos.SingleOrDefaultAsync(x => x.UsuarioId == destinatario.Id);
            if (vinculo?.Ativo == true) return Erro("Este usuário já possui vínculo ativo com a Empresa.");
            if (vinculo is null)
            {
                vinculo = new UsuarioEmpresa { EmpresaId = empresaContext.EmpresaId!.Value, UsuarioId = destinatario.Id };
                db.UsuariosEmpresas.Add(vinculo);
            }
            vinculo.Ativo = true;
            vinculo.Perfil = perfil;
            await db.SaveChangesAsync();
            return Ok("Acesso liberado.");
        });
        return resultado.Sucesso ? await ComunicarAsync(destinatario!, resultado) : resultado;
    }

    public Task<ResultadoAdministracao> AlterarPerfilAsync(string usuarioId, PerfilUsuarioEmpresa perfil)
    {
        if (!PerfilValido(perfil)) return Task.FromResult(Erro("Selecione um perfil válido."));
        return TransacionarAsync(async () =>
        {
            var vinculo = await Vinculos.SingleOrDefaultAsync(x => x.UsuarioId == usuarioId);
            if (vinculo is null) return Ausente();
            if (!vinculo.Ativo) return Erro("O vínculo está inativo.");
            var usuario = await usuarios.FindByIdAsync(usuarioId);
            if (perfil == PerfilUsuarioEmpresa.Administrador && await usuarios.IsInRoleAsync(usuario!, NomesAutorizacao.SystemAdmin))
                return Erro(AlvoProibido);
            if (perfil == PerfilUsuarioEmpresa.Operacional && !await PodeRemoverAsync(vinculo))
                return Erro(ProtecaoAdministradorEmpresa.Mensagem);
            vinculo.Perfil = perfil;
            await db.SaveChangesAsync();
            return Ok("Perfil atualizado.");
        });
    }

    public Task<ResultadoAdministracao> DesvincularAsync(string usuarioId)
        => TransacionarAsync(async () =>
        {
            var vinculo = await Vinculos.SingleOrDefaultAsync(x => x.UsuarioId == usuarioId);
            if (vinculo is null) return Ausente();
            if (!vinculo.Ativo) return Erro("O vínculo já está inativo.");
            if (!await PodeRemoverAsync(vinculo)) return Erro(ProtecaoAdministradorEmpresa.Mensagem);
            vinculo.Ativo = false;
            await db.SaveChangesAsync();
            return Ok("Usuário desvinculado.");
        });

    public async Task<ResultadoAdministracao> ReativarAsync(string usuarioId)
    {
        UsuarioAplicacao? destinatario = null;
        var resultado = await TransacionarAsync(async () =>
        {
            var vinculo = await Vinculos.SingleOrDefaultAsync(x => x.UsuarioId == usuarioId);
            if (vinculo is null) return Ausente();
            if (vinculo.Ativo) return Erro("O vínculo já está ativo.");
            destinatario = await usuarios.FindByIdAsync(usuarioId);
            if (await usuarios.IsInRoleAsync(destinatario!, NomesAutorizacao.SystemAdmin)) return Erro(AlvoProibido);
            vinculo.Ativo = true;
            await db.SaveChangesAsync();
            return Ok("Usuário reativado.");
        });
        return resultado.Sucesso ? await ComunicarAsync(destinatario!, resultado) : resultado;
    }

    public async Task<ResultadoAdministracao> ReenviarAtivacaoAsync(string usuarioId)
    {
        UsuarioAplicacao? destinatario = null;
        var validacao = await TransacionarAsync(async () =>
        {
            var vinculo = await Vinculos.SingleOrDefaultAsync(x => x.UsuarioId == usuarioId);
            if (vinculo is null) return Ausente();
            if (!vinculo.Ativo) return Erro("O vínculo está inativo.");
            destinatario = await usuarios.FindByIdAsync(usuarioId);
            if (await usuarios.IsInRoleAsync(destinatario!, NomesAutorizacao.SystemAdmin)) return Erro(AlvoProibido);
            return Ok("");
        });
        if (!validacao.Sucesso) return validacao;
        return Ok(await conta.EnviarAtivacaoAsync(destinatario!) switch
        {
            ResultadoEnvioConta.Enviado => "Ativação enviada.",
            ResultadoEnvioConta.NaoNecessario => "O usuário já possui senha; ativação não necessária.",
            ResultadoEnvioConta.Indisponivel => "O envio de ativação está indisponível.",
            _ => "O envio de ativação falhou."
        });
    }

    private async Task<bool> PodeRemoverAsync(UsuarioEmpresa vinculo)
        => ProtecaoAdministradorEmpresa.PodeRemover(vinculo.Ativo && vinculo.Perfil == PerfilUsuarioEmpresa.Administrador,
            await Vinculos.AnyAsync(x => x.UsuarioId != vinculo.UsuarioId && x.Ativo && x.Perfil == PerfilUsuarioEmpresa.Administrador));

    private async Task<ResultadoAdministracao> TransacionarAsync(Func<Task<ResultadoAdministracao>> operacao)
    {
        var empresaId = empresaContext.EmpresaId;
        var atorId = http.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!empresaId.HasValue || atorId is null || http.HttpContext!.User.IsInRole(NomesAutorizacao.SystemAdmin))
            return Erro("Acesso não autorizado.");
        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            try
            {
                return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    db.ChangeTracker.Clear();
                    await using var transacao = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    var recurso = $"Precificador.UC031.Empresa.{empresaId.Value}";
                    await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sp_getapplock @Resource = {recurso}, @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000; IF @r < 0 THROW 51031, 'Administração temporariamente indisponível.', 1;");
                    // A policy pode ter sido avaliada antes de outra requisição demover/desvincular o ator.
                    if (!await db.Empresas.AnyAsync(x => x.Id == empresaId && x.Ativo)
                        || !await Vinculos.AnyAsync(x => x.UsuarioId == atorId && x.Ativo && x.Perfil == PerfilUsuarioEmpresa.Administrador))
                        return Erro("Acesso não autorizado.");
                    var resultado = await operacao();
                    if (resultado.Sucesso) await transacao.CommitAsync();
                    else { await transacao.RollbackAsync(); db.ChangeTracker.Clear(); }
                    return resultado;
                });
            }
            catch (CorridaIdentidadeException) { db.ChangeTracker.Clear(); }
            catch (DbUpdateException ex) when (ConflitoIdentidade(ex)) { db.ChangeTracker.Clear(); }
            catch (SqlException ex) when (ex.Number == 51031)
            {
                db.ChangeTracker.Clear();
                return Erro("Administração temporariamente indisponível. Tente novamente.");
            }
        }
        return Erro("A identidade foi alterada concorrentemente. Tente novamente.");
    }

    private static bool ConflitoIdentidade(DbUpdateException ex)
        => ex.InnerException is SqlException sql && sql.Number is 2601 or 2627
            && ex.Entries.Any(x => x.Entity is UsuarioAplicacao)
            && (sql.Message.Contains("EmailIndex", StringComparison.Ordinal) || sql.Message.Contains("UserNameIndex", StringComparison.Ordinal));

    private async Task<ResultadoAdministracao> ComunicarAsync(UsuarioAplicacao destinatario, ResultadoAdministracao resultado)
    {
        // A transação e seu application lock já foram liberados.
        var envio = await usuarios.HasPasswordAsync(destinatario)
            ? await conta.EnviarAcessoLiberadoAsync(destinatario, empresaContext.Nome!)
            : await conta.EnviarAtivacaoAsync(destinatario);
        return envio == ResultadoEnvioConta.Enviado ? resultado
            : Ok(resultado.Mensagem + " A comunicação não pôde ser enviada.");
    }

    private static bool PerfilValido(PerfilUsuarioEmpresa perfil)
        => perfil is PerfilUsuarioEmpresa.Administrador or PerfilUsuarioEmpresa.Operacional;
    private static ResultadoAdministracao Ok(string mensagem) => new(true, mensagem);
    private static ResultadoAdministracao Erro(string mensagem) => new(false, mensagem);
    private static ResultadoAdministracao Ausente() => new(false, "Recurso não encontrado.", true);
    private sealed class CorridaIdentidadeException : Exception;
}
