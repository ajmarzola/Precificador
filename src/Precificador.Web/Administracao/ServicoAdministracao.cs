using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Precificador.Core.Acessos;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Autenticacao;
using Precificador.Web.Autorizacao;

namespace Precificador.Web.Administracao;

public sealed record ResultadoAdministracao(bool Sucesso, string Mensagem, bool NaoEncontrado = false);

public sealed class ServicoAdministracao(PrecificadorDbContext db, UserManager<UsuarioAplicacao> usuarios,
    ServicoConta conta, TimeProvider tempo)
{
    private const string ResponsavelGlobal = "O responsável informado é um Administrador do Sistema. Use outro e-mail para o Administrador da Empresa.";

    public async Task<ResultadoAdministracao> AprovarAsync(int id, string nome, string decisor)
    {
        UsuarioAplicacao? destinatario = null;
        Empresa? nova = null;
        var resultado = await TransacionarAsync(async () =>
        {
            var solicitacao = await db.SolicitacoesAcessoEmpresas.SingleOrDefaultAsync(x => x.Id == id);
            if (solicitacao is null) return Ausente();
            if (solicitacao.Situacao != SituacaoSolicitacaoAcessoEmpresa.Pendente) return Erro("A solicitação já foi decidida.");
            nova = Empresa.Criar(nome);
            if (await db.Empresas.AnyAsync(x => x.NomeNormalizado == nova.NomeNormalizado))
                return Erro("Já existe uma Empresa com esse nome. A solicitação permanece Pendente.");
            destinatario = await ResolverUsuarioAsync(solicitacao.EmailResponsavel);
            db.Empresas.Add(nova);
            await db.SaveChangesAsync();

            // Contexto limitado à configuração padrão da Empresa recém-criada. Mesma conexão e
            // transação; nenhum bypass de filtro/guard ou alteração da sessão do SystemAdmin.
            var options = new DbContextOptionsBuilder<PrecificadorDbContext>()
                .UseSqlServer(db.Database.GetDbConnection()).Options;
            await using (var criacao = new PrecificadorDbContext(options, new ContextoNovaEmpresa(nova.Id)))
            {
                await criacao.Database.UseTransactionAsync(db.Database.CurrentTransaction!.GetDbTransaction());
                criacao.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(nova.Id));
                await criacao.SaveChangesAsync();
            }
            db.UsuariosEmpresas.Add(new UsuarioEmpresa { EmpresaId = nova.Id, UsuarioId = destinatario.Id,
                Ativo = true, Perfil = PerfilUsuarioEmpresa.Administrador });
            solicitacao.Aprovar(nova.Id, decisor, tempo.GetUtcNow());
            await db.SaveChangesAsync();
            return Ok("A Empresa foi criada.");
        });
        if (!resultado.Sucesso) return resultado;
        var envio = await ComunicarAcessoAsync(destinatario!, nova!.Nome);
        return envio == ResultadoEnvioConta.Enviado ? resultado
            : Ok("A Empresa foi criada, mas a comunicação ao Administrador não pôde ser enviada.");
    }

    public async Task<ResultadoAdministracao> RecusarAsync(int id, string? motivo, string decisor)
    {
        string? email = null;
        var resultado = await TransacionarAsync(async () =>
        {
            var solicitacao = await db.SolicitacoesAcessoEmpresas.SingleOrDefaultAsync(x => x.Id == id);
            if (solicitacao is null) return Ausente();
            solicitacao.Recusar(decisor, tempo.GetUtcNow(), motivo);
            email = solicitacao.EmailResponsavel;
            await db.SaveChangesAsync();
            return Ok("Solicitação recusada.");
        });
        if (!resultado.Sucesso) return resultado;
        return await conta.EnviarRecusaAsync(email!) == ResultadoEnvioConta.Enviado ? resultado
            : Ok("Solicitação recusada, mas a comunicação não pôde ser enviada.");
    }

    public async Task<ResultadoAdministracao> DefinirAdministradorAsync(int id, string email, string? administradorAtual = null)
    {
        UsuarioAplicacao? destinatario = null;
        string? nome = null;
        var resultado = await TransacionarAsync(async () =>
        {
            var empresa = await db.Empresas.SingleOrDefaultAsync(x => x.Id == id && !x.EhTecnica);
            if (empresa is null) return Ausente();
            empresa.ValidarAdministravel();
            var admins = db.UsuariosEmpresas.Where(x => x.EmpresaId == id && x.Ativo && x.Perfil == PerfilUsuarioEmpresa.Administrador);
            UsuarioEmpresa? anterior = null;
            if (administradorAtual is null)
            {
                if (await admins.AnyAsync()) return Erro("A Empresa já possui Administrador ativo.");
            }
            else
            {
                anterior = await admins.SingleOrDefaultAsync(x => x.UsuarioId == administradorAtual);
                if (anterior is null) return Erro("Selecione um Administrador ativo dessa Empresa.");
            }
            destinatario = await ResolverUsuarioAsync(email);
            if (destinatario.Id == administradorAtual) return Erro("O novo Administrador deve ser diferente do atual.");
            var vinculo = await db.UsuariosEmpresas.SingleOrDefaultAsync(x => x.EmpresaId == id && x.UsuarioId == destinatario.Id);
            if (vinculo is null)
            {
                vinculo = new UsuarioEmpresa { EmpresaId = id, UsuarioId = destinatario.Id };
                db.UsuariosEmpresas.Add(vinculo);
            }
            vinculo.Ativo = true;
            vinculo.Perfil = PerfilUsuarioEmpresa.Administrador;
            await db.SaveChangesAsync();
            if (anterior is not null) anterior.Perfil = PerfilUsuarioEmpresa.Operacional;
            await db.SaveChangesAsync();
            nome = empresa.Nome;
            return Ok("Administrador definido.");
        });
        if (!resultado.Sucesso) return resultado;
        return await ComunicarAcessoAsync(destinatario!, nome!) == ResultadoEnvioConta.Enviado ? resultado
            : Ok("Administrador definido, mas a comunicação não pôde ser enviada.");
    }

    public Task<ResultadoAdministracao> AlterarSituacaoAsync(int id, string acao, bool confirmar = false)
        => TransacionarAsync(async () =>
        {
            var empresa = await db.Empresas.SingleOrDefaultAsync(x => x.Id == id && !x.EhTecnica);
            if (empresa is null) return Ausente();
            switch (acao)
            {
                case "Suspender": empresa.Suspender(); break;
                case "Reativar":
                    empresa.Reativar(await db.UsuariosEmpresas.AnyAsync(x => x.EmpresaId == id && x.Ativo && x.Perfil == PerfilUsuarioEmpresa.Administrador)); break;
                case "Encerrar" when confirmar: empresa.Encerrar(tempo); break;
                default: return Erro("Confirme explicitamente o encerramento.");
            }
            await db.SaveChangesAsync();
            return Ok("Situação da Empresa atualizada.");
        });

    public async Task<ResultadoAdministracao> ReenviarAtivacaoAsync(int id, string usuarioId)
    {
        var empresa = await db.Empresas.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && !x.EhTecnica);
        if (empresa is null) return Ausente();
        if (empresa.EncerradaEmUtc.HasValue) return Erro("A Empresa está encerrada.");
        if (!await db.UsuariosEmpresas.AnyAsync(x => x.EmpresaId == id && x.UsuarioId == usuarioId && x.Ativo && x.Perfil == PerfilUsuarioEmpresa.Administrador))
            return Erro("O vínculo não é um Administrador ativo dessa Empresa.");
        var usuario = await usuarios.FindByIdAsync(usuarioId);
        if (usuario is null) return Ausente();
        var resultado = await conta.EnviarAtivacaoAsync(usuario);
        return Ok(resultado switch {
            ResultadoEnvioConta.Enviado => "Ativação enviada.",
            ResultadoEnvioConta.NaoNecessario => "O Administrador já possui senha; ativação não necessária.",
            ResultadoEnvioConta.Indisponivel => "O envio de ativação está indisponível.",
            _ => "O envio de ativação falhou." });
    }

    private async Task<UsuarioAplicacao> ResolverUsuarioAsync(string email)
    {
        email = email.Trim();
        if (email.Length > 256 || !new EmailAddressAttribute().IsValid(email)) throw new ArgumentException("Informe um e-mail válido.");
        var usuario = await usuarios.FindByEmailAsync(email);
        if (usuario is null)
        {
            usuario = new UsuarioAplicacao { UserName = email, Email = email };
            var criacao = await usuarios.CreateAsync(usuario);
            if (!criacao.Succeeded)
            {
                if (criacao.Errors.All(x => x.Code is "DuplicateEmail" or "DuplicateUserName")) throw new CorridaIdentidadeException();
                throw new ArgumentException("Não foi possível criar a identidade do responsável.");
            }
        }
        if (await usuarios.IsInRoleAsync(usuario, NomesAutorizacao.SystemAdmin)) throw new ArgumentException(ResponsavelGlobal);
        return usuario;
    }

    private async Task<ResultadoAdministracao> TransacionarAsync(Func<Task<ResultadoAdministracao>> operacao)
    {
        for (var tentativa = 0; tentativa < 3; tentativa++)
        {
            try
            {
                return await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    db.ChangeTracker.Clear();
                    await using var transacao = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
                    // Lock SQL compartilhado por instâncias da aplicação, com liberação no commit/rollback.
                    // Serializa somente as escritas administrativas curtas, nunca SMTP.
                    await db.Database.ExecuteSqlRawAsync("DECLARE @r int; EXEC @r = sp_getapplock @Resource = 'Precificador.UC039', @LockMode = 'Exclusive', @LockOwner = 'Transaction', @LockTimeout = 30000; IF @r < 0 THROW 51039, 'Administração temporariamente indisponível.', 1;");
                    var resultado = await operacao();
                    if (resultado.Sucesso) await transacao.CommitAsync();
                    return resultado;
                });
            }
            catch (CorridaIdentidadeException) when (tentativa < 2) { db.ChangeTracker.Clear(); }
            catch (CorridaIdentidadeException) { db.ChangeTracker.Clear(); return Erro("A identidade foi alterada concorrentemente. Tente novamente."); }
            catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sql && sql.Number is 2601 or 2627)
            {
                db.ChangeTracker.Clear();
                // Unicidade é a autoridade final. Somente uma corrida no índice de e-mail
                // ou username permite repetir a resolução da identidade.
                if (tentativa < 2 && (sql.Message.Contains("EmailIndex", StringComparison.Ordinal) || sql.Message.Contains("UserNameIndex", StringComparison.Ordinal))) continue;
                return Erro("Conflito de cadastro. A operação não foi aplicada; revise a solicitação.");
            }
            catch (ArgumentException ex) { db.ChangeTracker.Clear(); return Erro(ex.Message); }
            catch (InvalidOperationException ex) { db.ChangeTracker.Clear(); return Erro(ex.Message); }
        }
        return Erro("A identidade foi alterada concorrentemente. Tente novamente.");
    }

    private async Task<ResultadoEnvioConta> ComunicarAcessoAsync(UsuarioAplicacao usuario, string nome)
        => await usuarios.HasPasswordAsync(usuario) ? await conta.EnviarAcessoLiberadoAsync(usuario, nome) : await conta.EnviarAtivacaoAsync(usuario);
    private static ResultadoAdministracao Ok(string mensagem) => new(true, mensagem);
    private static ResultadoAdministracao Erro(string mensagem) => new(false, mensagem);
    private static ResultadoAdministracao Ausente() => new(false, "Recurso não encontrado.", true);
    private sealed class CorridaIdentidadeException : Exception;
    private sealed class ContextoNovaEmpresa(int id) : IEmpresaContext
    {
        public int? EmpresaId => id;
        public int EmpresaIdOuSentinela => id;
        public string? TimeZoneId => Empresa.TimeZoneIdPadrao;
    }
}
