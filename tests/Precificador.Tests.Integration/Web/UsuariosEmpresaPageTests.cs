using System.Collections.Concurrent;
using System.Data.Common;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Administracao;
using Precificador.Web.Email;
using Precificador.Web.Empresas;

namespace Precificador.Tests.Integration.Web;

public sealed class UsuariosEmpresaPageTests
{
    [Theory]
    [InlineData("anonimo")] [InlineData("operacional")] [InlineData("inativo")]
    [InlineData("suspensa")] [InlineData("encerrada")] [InlineData("outra")]
    [InlineData("systemadmin")]
    public async Task Area_e_POSTs_exigem_administrador_da_empresa_ativa(string estado)
    {
        using var h = new Cenario(); await h.Preparar();
        using var client = h.Cliente();
        if (estado != "anonimo")
            await h.Login(client, estado == "systemadmin" ? h.GlobalEmail : h.Admin.Email!);
        await h.AlterarDb(async db =>
        {
            var v = await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == h.EmpresaId && x.UsuarioId == h.Admin.Id);
            if (estado == "operacional") v.Perfil = PerfilUsuarioEmpresa.Operacional;
            if (estado == "inativo") v.Ativo = false;
            if (estado == "suspensa") (await db.Empresas.FindAsync(h.EmpresaId))!.Suspender();
            if (estado == "encerrada") (await db.Empresas.FindAsync(h.EmpresaId))!.Encerrar(TimeProvider.System);
            if (estado == "outra")
            {
                v.Perfil = PerfilUsuarioEmpresa.Operacional;
                db.UsuariosEmpresas.Add(new UsuarioEmpresa { EmpresaId = h.OutraId, UsuarioId = h.Admin.Id, Ativo = true, Perfil = PerfilUsuarioEmpresa.Administrador });
            }
        });
        foreach (var url in new[] { "/Usuarios", Cenario.Detalhe(h.Admin.Id) })
            Assert.Equal(HttpStatusCode.Redirect, (await client.GetAsync(url)).StatusCode);
        // Token legítimo obtido fora da área: a policy também bloqueia todos os handlers.
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, estado == "systemadmin" ? "/Admin" : estado == "anonimo" ? "/Conta/Login" : "/Empresas/Selecionar");
        foreach (var acao in new[] { "Adicionar", "AlterarPerfil", "Desvincular", "Reativar", "ReenviarAtivacao" })
        {
            var url = acao == "Adicionar" ? "/Usuarios" : Cenario.Detalhe(h.Admin.Id);
            var response = await h.Post(client, url, acao, new() { ["Input.Email"] = "negado@teste.local", ["Input.Perfil"] = "2" }, token);
            Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        }
        await h.Verificar(async db => Assert.False(await db.Users.AnyAsync(x => x.Email == "negado@teste.local")));
        Assert.Empty(h.Sender.Mensagens);
    }

    [Fact]
    public async Task Lista_filtra_ordena_mostra_credencial_e_nao_expoe_outra_empresa_ou_segredos()
    {
        using var h = new Cenario(); await h.Preparar();
        var z = await h.Usuario("z@teste.local", true, true, PerfilUsuarioEmpresa.Operacional);
        var a = await h.Usuario("a@teste.local", false, true, PerfilUsuarioEmpresa.Administrador);
        var inativo = await h.Usuario("inativo@teste.local", false, false, PerfilUsuarioEmpresa.Operacional);
        var externo = await h.Usuario("externo@teste.local", true, true, PerfilUsuarioEmpresa.Administrador, h.OutraId);
        await h.Login(h.Client, h.Admin.Email!);
        var ativos = await h.Html("/Usuarios");
        Assert.Contains("<td>" + a.Email + "</td>", ativos); Assert.Contains("<td>" + z.Email + "</td>", ativos);
        Assert.DoesNotContain("<td>" + inativo.Email + "</td>", ativos);
        var todos = await h.Html("/Usuarios?situacao=Todos");
        Assert.True(todos.IndexOf("<td>" + a.Email + "</td>", StringComparison.Ordinal) < todos.IndexOf("<td>" + z.Email + "</td>", StringComparison.Ordinal));
        Assert.True(todos.IndexOf("<td>" + z.Email + "</td>", StringComparison.Ordinal) < todos.IndexOf("<td>" + inativo.Email + "</td>", StringComparison.Ordinal));
        Assert.Contains("Aguardando ativação", todos); Assert.Contains("Ativado", todos);
        Assert.DoesNotContain(externo.Email!, todos); Assert.DoesNotContain(h.OutraNome, todos);
        Assert.DoesNotContain(z.PasswordHash!, todos); Assert.DoesNotContain(z.SecurityStamp!, todos);
        Assert.Contains("<td>" + inativo.Email + "</td>", await h.Html("/Usuarios?situacao=Inativos"));
        Assert.Equal(HttpStatusCode.BadRequest, (await h.Client.GetAsync("/Usuarios?situacao=qualquer")).StatusCode);
        Assert.Matches("href=\"/Usuarios/Detalhes\\?usuarioId=" + a.Id + "\"", todos);
        Assert.Matches("<a\\b[^>]*href=\"/Usuarios\"[^>]*>Usuários</a>", ativos);
        Assert.Equal(HttpStatusCode.NotFound, (await h.Client.GetAsync(Cenario.Detalhe(externo.Id))).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await h.Client.GetAsync(Cenario.Detalhe("inexistente"))).StatusCode);
        Assert.Contains("<dd>" + inativo.Email + "</dd>", await h.Html(Cenario.Detalhe(inativo.Id)));
    }

    [Theory]
    [InlineData("novo", 1)] [InlineData("novo", 2)] [InlineData("sem-senha", 1)]
    [InlineData("com-senha", 2)] [InlineData("inativo", 2)]
    public async Task Adicionar_cria_ou_reutiliza_preserva_identidade_e_comunica_pos_commit(string estado, int perfil)
    {
        using var h = new Cenario(); await h.Preparar();
        const string email = "convidado@teste.local";
        UsuarioAplicacao? antes = null;
        if (estado != "novo")
        {
            antes = await h.Usuario(email, estado == "com-senha", false, PerfilUsuarioEmpresa.Operacional, h.OutraId);
            await h.AlterarDb(async db =>
            {
                var u = (await db.Users.FindAsync(antes.Id))!; u.EmailConfirmed = true;
                if (estado == "inativo") db.UsuariosEmpresas.Add(new UsuarioEmpresa { UsuarioId = antes.Id, EmpresaId = h.EmpresaId, Ativo = false, Perfil = PerfilUsuarioEmpresa.Operacional });
            });
            using var scope = h.Factory.Services.CreateScope();
            var role = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await role.CreateAsync(new IdentityRole("PapelPreservado"))).Succeeded);
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            antes = (await manager.FindByIdAsync(antes.Id))!;
            Assert.True((await manager.AddToRoleAsync(antes, "PapelPreservado")).Succeeded);
        }
        await h.Login(h.Client, h.Admin.Email!);
        h.Sender.AoEnviar = async () =>
        {
            // Outra conexão lê o vínculo já commitado e consegue adquirir o mesmo lock.
            await h.Verificar(async db =>
            {
                var usuario = await db.Users.SingleAsync(x => x.Email == email);
                Assert.True(await db.UsuariosEmpresas.AnyAsync(x => x.EmpresaId == h.EmpresaId && x.UsuarioId == usuario.Id && x.Ativo));
                await db.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
                {
                    await using var tx = await db.Database.BeginTransactionAsync();
                    var recurso = $"Precificador.UC031.Empresa.{h.EmpresaId}";
                    await db.Database.ExecuteSqlInterpolatedAsync($"DECLARE @r int; EXEC @r = sp_getapplock @Resource={recurso}, @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=0; IF @r < 0 THROW 51099, 'SMTP sob lock', 1;");
                });
            });
        };
        var response = await h.Post(h.Client, "/Usuarios", "Adicionar", new()
        {
            ["Input.Email"] = "  " + email.ToUpperInvariant() + "  ", ["Input.Perfil"] = perfil.ToString(),
            ["EmpresaId"] = h.OutraId.ToString(), ["Input.EmpresaId"] = h.OutraId.ToString(),
            ["PasswordHash"] = "forjado", ["SecurityStamp"] = "forjado", ["EmailConfirmed"] = "false",
            ["Ativo"] = "false", ["Roles"] = "SystemAdmin"
        });
        Assert.True(response.StatusCode == HttpStatusCode.Redirect, await WebTestHtml.LerHtmlDecodificadoAsync(response));
        await h.Verificar(async db =>
        {
            var u = await db.Users.SingleAsync(x => x.NormalizedEmail == email.ToUpperInvariant());
            var v = await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == h.EmpresaId && x.UsuarioId == u.Id);
            Assert.True(v.Ativo); Assert.Equal((PerfilUsuarioEmpresa)perfil, v.Perfil);
            if (antes is not null)
            {
                Assert.Equal(antes.Id, u.Id); Assert.Equal(antes.PasswordHash, u.PasswordHash); Assert.Equal(antes.SecurityStamp, u.SecurityStamp);
                Assert.Equal(antes.Email, u.Email); Assert.Equal(antes.UserName, u.UserName); Assert.True(u.EmailConfirmed);
                Assert.Single(await db.UserRoles.Where(x => x.UserId == u.Id).ToListAsync());
                var outro = await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == h.OutraId && x.UsuarioId == u.Id);
                Assert.False(outro.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Operacional, outro.Perfil);
            }
            else { Assert.Null(u.PasswordHash); Assert.False(u.EmailConfirmed); Assert.Empty(await db.UserRoles.Where(x => x.UserId == u.Id).ToListAsync()); }
        });
        var mensagem = Assert.Single(h.Sender.Mensagens);
        Assert.Contains(estado == "com-senha" ? "/Conta/Login" : "/Conta/Ativar?", mensagem.Texto);
        Assert.DoesNotContain("RedefinirSenha", mensagem.Texto);
        if (estado == "com-senha") Assert.DoesNotContain("code=", mensagem.Texto);
    }

    [Theory]
    [InlineData("ativo")] [InlineData("systemadmin")]
    public async Task Adicionar_rejeita_vinculo_ativo_e_alvo_global_sem_efeitos(string estado)
    {
        using var h = new Cenario(); await h.Preparar(); await h.Login(h.Client, h.Admin.Email!);
        var response = await h.Post(h.Client, "/Usuarios", "Adicionar", new() { ["Input.Email"] = estado == "ativo" ? h.Admin.Email! : h.GlobalEmail, ["Input.Perfil"] = "1" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(response);
        Assert.Contains(estado == "ativo" ? "já possui vínculo ativo" : "Este e-mail não pode ser vinculado à Empresa.", html);
        Assert.DoesNotContain("Administrador do Sistema", html);
        await h.Verificar(async db =>
        {
            Assert.Single(await db.UsuariosEmpresas.Where(x => x.EmpresaId == h.EmpresaId).ToListAsync());
            Assert.Equal(PerfilUsuarioEmpresa.Administrador, (await db.UsuariosEmpresas.SingleAsync(x => x.UsuarioId == h.Admin.Id)).Perfil);
        });
        Assert.Empty(h.Sender.Mensagens);
    }

    [Theory]
    [InlineData("", "1")] [InlineData("invalido", "1")] [InlineData("longo", "1")]
    [InlineData("valido@teste.local", "0")] [InlineData("valido@teste.local", "99")]
    [InlineData("valido@teste.local", "")]
    public async Task Adicionar_rejeita_inputs_invalidos(string email, string perfil)
    {
        using var h = new Cenario(); await h.Preparar(); await h.Login(h.Client, h.Admin.Email!);
        if (email == "longo") email = new string('a', 250) + "@teste.local";
        Assert.Equal(HttpStatusCode.OK, (await h.Post(h.Client, "/Usuarios", "Adicionar", new() { ["Input.Email"] = email, ["Input.Perfil"] = perfil })).StatusCode);
        await h.Verificar(async db => { Assert.Equal(2, await db.Users.CountAsync()); Assert.Single(await db.UsuariosEmpresas.ToListAsync()); });
        Assert.Empty(h.Sender.Mensagens);
    }

    [Theory]
    [InlineData("AlterarPerfil")] [InlineData("Desvincular")]
    public async Task Ultimo_admin_bloqueia_inclusive_atuacao_sobre_si(string acao)
    {
        using var h = new Cenario(); await h.Preparar(); await h.Login(h.Client, h.Admin.Email!);
        var response = await h.Post(h.Client, Cenario.Detalhe(h.Admin.Id), acao, new() { ["Input.Perfil"] = "1" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(ProtecaoAdministradorEmpresa.Mensagem, await WebTestHtml.LerHtmlDecodificadoAsync(response));
        await h.Verificar(async db => { var v = await db.UsuariosEmpresas.SingleAsync(); Assert.True(v.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Administrador, v.Perfil); });
    }

    [Fact]
    public async Task Perfil_promove_demove_e_rejeita_inativo_e_enum_invalido()
    {
        using var h = new Cenario(); await h.Preparar(); var alvo = await h.Usuario("alvo@teste.local", true, true, PerfilUsuarioEmpresa.Operacional);
        await h.Login(h.Client, h.Admin.Email!);
        foreach (var perfil in new[] { "2", "1" })
        {
            Assert.Equal(HttpStatusCode.Redirect, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "AlterarPerfil", new() { ["Input.Perfil"] = perfil })).StatusCode);
            await h.Verificar(async db => Assert.Equal((PerfilUsuarioEmpresa)int.Parse(perfil), (await db.UsuariosEmpresas.SingleAsync(x => x.UsuarioId == alvo.Id)).Perfil));
        }
        foreach (var invalido in new[] { "0", "99", "" })
            Assert.Equal(HttpStatusCode.OK, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "AlterarPerfil", new() { ["Input.Perfil"] = invalido })).StatusCode);
        await h.AlterarDb(async db => (await db.UsuariosEmpresas.SingleAsync(x => x.UsuarioId == alvo.Id)).Ativo = false);
        Assert.Equal(HttpStatusCode.OK, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "AlterarPerfil", new() { ["Input.Perfil"] = "2" })).StatusCode);
        await h.Verificar(async db => Assert.Equal(PerfilUsuarioEmpresa.Operacional, (await db.UsuariosEmpresas.SingleAsync(x => x.UsuarioId == alvo.Id)).Perfil));
        Assert.Empty(h.Sender.Mensagens);
    }

    [Theory]
    [InlineData(1, true)] [InlineData(2, false)]
    public async Task Desvinculo_e_reativacao_preservam_perfil_identidade_e_outro_tenant(int perfil, bool senha)
    {
        using var h = new Cenario(); await h.Preparar();
        var alvo = await h.Usuario("alvo@teste.local", senha, true, (PerfilUsuarioEmpresa)perfil);
        await h.AlterarDb(db => { db.UsuariosEmpresas.Add(new UsuarioEmpresa { EmpresaId = h.OutraId, UsuarioId = alvo.Id, Ativo = true, Perfil = PerfilUsuarioEmpresa.Administrador }); return Task.CompletedTask; });
        await h.Login(h.Client, h.Admin.Email!);
        Assert.Equal(HttpStatusCode.Redirect, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "Desvincular", new())).StatusCode);
        await h.Verificar(async db =>
        {
            var v = await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == h.EmpresaId && x.UsuarioId == alvo.Id);
            Assert.False(v.Ativo); Assert.Equal((PerfilUsuarioEmpresa)perfil, v.Perfil);
            Assert.True((await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == h.OutraId && x.UsuarioId == alvo.Id)).Ativo);
            var u = (await db.Users.FindAsync(alvo.Id))!; Assert.Equal(alvo.PasswordHash, u.PasswordHash); Assert.Equal(alvo.SecurityStamp, u.SecurityStamp);
        });
        Assert.Empty(h.Sender.Mensagens);
        Assert.Equal(HttpStatusCode.OK, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "Desvincular", new())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "ReenviarAtivacao", new())).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "Reativar", new() { ["Input.Perfil"] = "99", ["EmpresaId"] = h.OutraId.ToString() })).StatusCode);
        await h.Verificar(async db =>
        {
            var v = await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == h.EmpresaId && x.UsuarioId == alvo.Id);
            Assert.True(v.Ativo); Assert.Equal((PerfilUsuarioEmpresa)perfil, v.Perfil);
            Assert.Equal(2, await db.UsuariosEmpresas.CountAsync(x => x.UsuarioId == alvo.Id));
        });
        Assert.Contains(senha ? "/Conta/Login" : "/Conta/Ativar?", Assert.Single(h.Sender.Mensagens).Texto);
        Assert.Equal(HttpStatusCode.OK, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "Reativar", new())).StatusCode);
        Assert.Single(h.Sender.Mensagens);
    }

    [Theory]
    [InlineData("Enviado")] [InlineData("NaoNecessario")] [InlineData("Indisponivel")] [InlineData("Falhou")]
    public async Task Reenvio_trata_resultados_sem_mutacao_ou_token_na_UI(string resultado)
    {
        using var h = new Cenario(); await h.Preparar();
        var alvo = await h.Usuario("alvo@teste.local", resultado == "NaoNecessario", true, PerfilUsuarioEmpresa.Operacional);
        await h.Login(h.Client, h.Admin.Email!);
        h.Sender.Habilitado = resultado != "Indisponivel"; h.Sender.Falhar = resultado == "Falhou";
        Assert.Equal(HttpStatusCode.Redirect, (await h.Post(h.Client, Cenario.Detalhe(alvo.Id), "ReenviarAtivacao", new())).StatusCode);
        var html = await h.Html(Cenario.Detalhe(alvo.Id));
        Assert.Contains(resultado switch { "Enviado" => "Ativação enviada.", "NaoNecessario" => "ativação não necessária", "Indisponivel" => "indisponível", _ => "falhou" }, html);
        Assert.DoesNotContain("code=", html);
        Assert.Equal(resultado == "Enviado" ? 1 : 0, h.Sender.Mensagens.Count);
        await h.Verificar(async db =>
        {
            var u = (await db.Users.FindAsync(alvo.Id))!; Assert.Equal(alvo.PasswordHash, u.PasswordHash); Assert.Equal(alvo.SecurityStamp, u.SecurityStamp);
            Assert.Equal(3, await db.Users.CountAsync()); Assert.Equal(2, await db.UsuariosEmpresas.CountAsync());
        });
    }

    [Theory]
    [InlineData("Adicionar")] [InlineData("Reativar")]
    public async Task SMTP_falha_nao_reverte_vinculo(string acao)
    {
        using var h = new Cenario(); await h.Preparar();
        var alvo = await h.Usuario("alvo@teste.local", false, false, PerfilUsuarioEmpresa.Operacional);
        await h.Login(h.Client, h.Admin.Email!); h.Sender.Falhar = true;
        var response = await h.Post(h.Client, acao == "Adicionar" ? "/Usuarios" : Cenario.Detalhe(alvo.Id), acao,
            new() { ["Input.Email"] = alvo.Email!, ["Input.Perfil"] = "2" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Contains("comunicação não pôde ser enviada", await h.Html(response.Headers.Location!.OriginalString));
        await h.Verificar(async db => Assert.True((await db.UsuariosEmpresas.SingleAsync(x => x.UsuarioId == alvo.Id)).Ativo));
    }

    [Theory]
    [InlineData("AlterarPerfil")] [InlineData("Desvincular")]
    public async Task Auto_administracao_redireciona_e_atualiza_autoridade_e_contexto(string acao)
    {
        using var h = new Cenario(); await h.Preparar();
        await h.Usuario("outro-admin@teste.local", true, true, PerfilUsuarioEmpresa.Administrador);
        await h.Login(h.Client, h.Admin.Email!);
        var response = await h.Post(h.Client, Cenario.Detalhe(h.Admin.Id), acao, new() { ["Input.Perfil"] = "1" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal(acao == "AlterarPerfil" ? "/Dashboard" : "/Empresas/Selecionar", response.Headers.Location!.OriginalString);
        // Inspeção direta da sessão antes de uma policy posterior poder limpá-la.
        var contexto = await h.Html("/TesteUc031Contexto");
        Assert.Equal(acao == "AlterarPerfil" ? h.EmpresaId.ToString() : "ausente", contexto);
        Assert.Equal(HttpStatusCode.Redirect, (await h.Client.GetAsync("/Usuarios")).StatusCode);
        if (acao == "AlterarPerfil")
        {
            var dashboard = await h.Html("/Dashboard");
            Assert.DoesNotMatch("<a\\b[^>]*href=\"/Usuarios\"", dashboard);
            Assert.Contains("Empresa ativa: " + h.Nome, dashboard);
            Assert.Equal(HttpStatusCode.OK, (await h.Client.GetAsync("/Produtos")).StatusCode);
        }
        else Assert.Equal(HttpStatusCode.Redirect, (await h.Client.GetAsync("/Dashboard")).StatusCode);
    }

    [Fact]
    public async Task Navegacao_global_nao_exibe_Usuarios()
    {
        using var h = new Cenario(); await h.Preparar(); await h.Login(h.Client, h.GlobalEmail);
        Assert.DoesNotMatch("<a\\b[^>]*href=\"/Usuarios\"", await h.Html("/Admin"));
    }

    [Theory]
    [InlineData("AlterarPerfil")] [InlineData("Desvincular")]
    [InlineData("Reativar")] [InlineData("ReenviarAtivacao")]
    public async Task Alvo_cross_tenant_retorna_404_e_mass_assignment_nao_move_vinculo(string acao)
    {
        using var h = new Cenario(); await h.Preparar();
        var alvo = await h.Usuario("externo@teste.local", false, acao != "Reativar", PerfilUsuarioEmpresa.Administrador, h.OutraId);
        await h.Login(h.Client, h.Admin.Email!);
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(h.Client, "/Usuarios");
        var response = await h.Post(h.Client, Cenario.Detalhe(alvo.Id), acao, new()
        {
            ["Input.Perfil"] = "1", ["EmpresaId"] = h.OutraId.ToString(), ["Input.EmpresaId"] = h.OutraId.ToString(),
            ["PasswordHash"] = "alterado", ["SecurityStamp"] = "alterado", ["EmailConfirmed"] = "true", ["Email"] = "alterado@teste.local", ["Roles"] = "SystemAdmin"
        }, token);
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        await h.Verificar(async db =>
        {
            var v = await db.UsuariosEmpresas.SingleAsync(x => x.UsuarioId == alvo.Id);
            Assert.Equal(h.OutraId, v.EmpresaId); Assert.Equal(acao != "Reativar", v.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Administrador, v.Perfil);
            var u = (await db.Users.FindAsync(alvo.Id))!; Assert.Equal(alvo.Email, u.Email); Assert.Equal(alvo.SecurityStamp, u.SecurityStamp); Assert.Null(u.PasswordHash); Assert.False(u.EmailConfirmed);
        });
        Assert.Empty(h.Sender.Mensagens);
    }

    [Theory]
    [InlineData("Adicionar")] [InlineData("AlterarPerfil")] [InlineData("Desvincular")]
    [InlineData("Reativar")] [InlineData("ReenviarAtivacao")]
    public async Task Todos_POSTs_exigem_antiforgery(string acao)
    {
        using var h = new Cenario(); await h.Preparar(); await h.Login(h.Client, h.Admin.Email!);
        var url = acao == "Adicionar" ? "/Usuarios?handler=Adicionar" : Cenario.Detalhe(h.Admin.Id) + "&handler=" + acao;
        Assert.Equal(HttpStatusCode.BadRequest, (await h.Client.PostAsync(url, new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Ultimos_admins_concorrentes_preservam_admin_em_SQL_Server(bool misto)
    {
        using var h = new Cenario(); await h.Preparar();
        var b = await h.Usuario("admin-b@teste.local", true, true, PerfilUsuarioEmpresa.Administrador);
        using var clientB = h.Cliente(); await h.Login(h.Client, h.Admin.Email!); await h.Login(clientB, b.Email!);
        var tokenA = await WebTestHtml.ObterTokenAntiforgeryAsync(h.Client, Cenario.Detalhe(b.Id));
        var tokenB = await WebTestHtml.ObterTokenAntiforgeryAsync(clientB, Cenario.Detalhe(h.Admin.Id));
        var respostas = await Task.WhenAll(
            h.Post(h.Client, Cenario.Detalhe(b.Id), misto ? "AlterarPerfil" : "Desvincular", new() { ["Input.Perfil"] = "1" }, tokenA),
            h.Post(clientB, Cenario.Detalhe(h.Admin.Id), "Desvincular", new(), tokenB));
        Assert.All(respostas, x => Assert.Contains(x.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Redirect }));
        Assert.Single(respostas, x => x.StatusCode == HttpStatusCode.Redirect);
        await h.Verificar(async db => Assert.Equal(1, await db.UsuariosEmpresas.CountAsync(x => x.EmpresaId == h.EmpresaId && x.Ativo && x.Perfil == PerfilUsuarioEmpresa.Administrador)));
    }

    [Fact]
    public async Task Convites_simultaneos_cross_tenant_convergem_para_uma_identidade()
    {
        var corrida = new CorridaEmail("simultaneo@teste.local");
        using var h = new Cenario(corrida: corrida); await h.Preparar();
        var b = await h.Usuario("admin-b@teste.local", true, true, PerfilUsuarioEmpresa.Administrador, h.OutraId);
        using var clientB = h.Cliente(); await h.Login(h.Client, h.Admin.Email!); await h.Login(clientB, b.Email!);
        var tokenA = await WebTestHtml.ObterTokenAntiforgeryAsync(h.Client, "/Usuarios");
        var tokenB = await WebTestHtml.ObterTokenAntiforgeryAsync(clientB, "/Usuarios");
        corrida.Ativa = true;
        var respostas = await Task.WhenAll(h.Post(h.Client, "/Usuarios", "Adicionar", new() { ["Input.Email"] = corrida.Email, ["Input.Perfil"] = "1" }, tokenA),
            h.Post(clientB, "/Usuarios", "Adicionar", new() { ["Input.Email"] = corrida.Email, ["Input.Perfil"] = "2" }, tokenB));
        Assert.All(respostas, x => Assert.Equal(HttpStatusCode.Redirect, x.StatusCode));
        Assert.Equal(2, corrida.Chegadas);
        await h.Verificar(async db =>
        {
            var u = await db.Users.SingleAsync(x => x.NormalizedEmail == corrida.Email.ToUpperInvariant());
            var vinculos = await db.UsuariosEmpresas.Where(x => x.UsuarioId == u.Id).ToListAsync();
            Assert.Equal(2, vinculos.Count); Assert.All(vinculos, x => Assert.True(x.Ativo));
            Assert.Equal(PerfilUsuarioEmpresa.Operacional, vinculos.Single(x => x.EmpresaId == h.EmpresaId).Perfil);
            Assert.Equal(PerfilUsuarioEmpresa.Administrador, vinculos.Single(x => x.EmpresaId == h.OutraId).Perfil);
        });
        Assert.Equal(2, h.Sender.Mensagens.Count);
    }

    [Fact]
    public async Task Falha_posterior_a_Identity_reverte_usuario_vinculo_e_nao_envia_email()
    {
        var falha = new FalhaVinculo(); using var h = new Cenario(falha); await h.Preparar();
        await h.Login(h.Client, h.Admin.Email!);
        falha.Ativa = true;
        Assert.Equal(HttpStatusCode.InternalServerError, (await h.Post(h.Client, "/Usuarios", "Adicionar",
            new() { ["Input.Email"] = "rollback@teste.local", ["Input.Perfil"] = "1" })).StatusCode);
        Assert.True(falha.IdentityPersistidaNaTransacao);
        await h.Verificar(async db =>
        {
            Assert.False(await db.Users.AnyAsync(x => x.Email == "rollback@teste.local"));
            Assert.Single(await db.UsuariosEmpresas.ToListAsync());
        });
        Assert.Empty(h.Sender.Mensagens);
    }

    [Theory]
    [InlineData("Reativar")] [InlineData("AlterarPerfil")]
    public async Task Dado_legado_SystemAdmin_nao_pode_ser_reativado_ou_promovido(string acao)
    {
        using var h = new Cenario(); await h.Preparar();
        string globalId = "";
        await h.AlterarDb(async db =>
        {
            globalId = (await db.Users.SingleAsync(x => x.Email == h.GlobalEmail)).Id;
            db.UsuariosEmpresas.Add(new UsuarioEmpresa { EmpresaId = h.EmpresaId, UsuarioId = globalId, Ativo = acao == "AlterarPerfil", Perfil = PerfilUsuarioEmpresa.Operacional });
        });
        await h.Login(h.Client, h.Admin.Email!);
        var response = await h.Post(h.Client, Cenario.Detalhe(globalId), acao, new() { ["Input.Perfil"] = "2" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Este e-mail não pode ser vinculado à Empresa.", await WebTestHtml.LerHtmlDecodificadoAsync(response));
        await h.Verificar(async db =>
        {
            var v = await db.UsuariosEmpresas.SingleAsync(x => x.UsuarioId == globalId);
            Assert.Equal(acao == "AlterarPerfil", v.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Operacional, v.Perfil);
        });
    }

    private sealed class FalhaVinculo : SaveChangesInterceptor
    {
        public bool Ativa { get; set; }
        public bool IdentityPersistidaNaTransacao { get; private set; }
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            var db = (PrecificadorDbContext)eventData.Context!;
            if (Ativa && db.ChangeTracker.Entries<UsuarioEmpresa>().Any(x => x.State == EntityState.Added))
            {
                IdentityPersistidaNaTransacao = await db.Users.AsNoTracking().AnyAsync(x => x.Email == "rollback@teste.local", cancellationToken);
                throw new DbUpdateException("Falha de escrita após criação Identity.");
            }
            return result;
        }
    }

    private sealed class CorridaEmail(string email) : DbCommandInterceptor
    {
        public string Email => email;
        public bool Ativa { get; set; }
        public int Chegadas => contextos.Count;
        private readonly ConcurrentDictionary<Guid, byte> contextos = new();
        private readonly TaskCompletionSource liberar = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public override async ValueTask<DbDataReader> ReaderExecutedAsync(DbCommand command, CommandExecutedEventData eventData, DbDataReader result, CancellationToken cancellationToken = default)
        {
            if (Ativa && command.Transaction is not null && command.CommandText.Contains("FROM [AspNetUsers]", StringComparison.Ordinal)
                && command.Parameters.Cast<DbParameter>().Any(x => string.Equals(x.Value?.ToString(), email, StringComparison.OrdinalIgnoreCase))
                && contextos.TryAdd(eventData.Context!.ContextId.InstanceId, 0))
            {
                if (contextos.Count == 2) liberar.TrySetResult();
                await liberar.Task.WaitAsync(TimeSpan.FromSeconds(20), cancellationToken);
            }
            return result;
        }
    }

    private sealed class Sender : IEmailSenderAplicacao
    {
        public bool Habilitado { get; set; } = true;
        public bool Disponivel => Habilitado;
        public bool Falhar { get; set; }
        public ConcurrentQueue<MensagemEmail> Mensagens { get; } = new();
        public Func<Task>? AoEnviar { get; set; }
        public async Task EnviarAsync(MensagemEmail mensagem, CancellationToken cancellationToken = default)
        {
            if (AoEnviar is not null) await AoEnviar();
            if (Falhar) throw new InvalidOperationException("Falha SMTP simulada");
            Mensagens.Enqueue(mensagem);
        }
    }

    private sealed class Cenario : IDisposable
    {
        private readonly CustomWebApplicationFactory original = new();
        public WebApplicationFactory<Program> Factory { get; }
        public HttpClient Client { get; }
        public Sender Sender { get; } = new();
        public string Nome { get; } = "Empresa " + Guid.NewGuid().ToString("N");
        public string OutraNome => "Outra " + Nome;
        public string GlobalEmail { get; } = "global@teste.local";
        public int EmpresaId { get; private set; }
        public int OutraId { get; private set; }
        public UsuarioAplicacao Admin { get; private set; } = null!;
        public Cenario(FalhaVinculo? falha = null, CorridaEmail? corrida = null)
        {
            Factory = original.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<IEmailSenderAplicacao>(); services.AddSingleton<IEmailSenderAplicacao>(Sender);
                    services.Configure<OpcoesAplicacao>(x => x.UrlPublica = "https://publico.test");
                    services.AddDbContext<PrecificadorDbContext>(options =>
                    {
                        options.UseSqlServer(sql => sql.EnableRetryOnFailure());
                        if (falha is not null) options.AddInterceptors(falha);
                        if (corrida is not null) options.AddInterceptors(corrida);
                    });
                    services.AddSingleton<Microsoft.AspNetCore.Hosting.IStartupFilter, EndpointContexto>();
                });
            });
            Client = Cliente();
        }
        public HttpClient Cliente() => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        public async Task Preparar()
        {
            using var scope = Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var a = Empresa.Criar(Nome); var b = Empresa.Criar(OutraNome); db.Empresas.AddRange(a, b); await db.SaveChangesAsync();
            EmpresaId = a.Id; OutraId = b.Id;
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await roles.CreateAsync(new IdentityRole("SystemAdmin"))).Succeeded);
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var global = new UsuarioAplicacao { Email = GlobalEmail, UserName = GlobalEmail };
            Assert.True((await usuarios.CreateAsync(global, "SenhaTeste1")).Succeeded);
            Assert.True((await usuarios.AddToRoleAsync(global, "SystemAdmin")).Succeeded);
            Admin = await Usuario("admin@teste.local", true, true, PerfilUsuarioEmpresa.Administrador);
            // Configuração necessária para regressão da navegação operacional.
            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            await using var tenant = new PrecificadorDbContext(options, new ContextoEmpresaTeste(EmpresaId));
            tenant.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(EmpresaId)); await tenant.SaveChangesAsync();
        }
        public async Task<UsuarioAplicacao> Usuario(string email, bool senha, bool ativo, PerfilUsuarioEmpresa perfil, int? empresaId = null)
        {
            using var scope = Factory.Services.CreateScope();
            var usuarios = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var usuario = new UsuarioAplicacao { Email = email, UserName = email };
            Assert.True((senha ? await usuarios.CreateAsync(usuario, "SenhaTeste1") : await usuarios.CreateAsync(usuario)).Succeeded);
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            db.UsuariosEmpresas.Add(new UsuarioEmpresa { EmpresaId = empresaId ?? EmpresaId, UsuarioId = usuario.Id, Ativo = ativo, Perfil = perfil });
            await db.SaveChangesAsync(); return usuario;
        }
        public async Task Login(HttpClient client, string email)
        {
            var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Conta/Login");
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string,string>
                { ["Input.Email"] = email, ["Input.Senha"] = "SenhaTeste1", ["__RequestVerificationToken"] = token }))).StatusCode);
        }
        public async Task<HttpResponseMessage> Post(HttpClient client, string url, string acao, Dictionary<string,string> campos, string? token = null)
        {
            campos["__RequestVerificationToken"] = token ?? await WebTestHtml.ObterTokenAntiforgeryAsync(client, url);
            return await client.PostAsync(url + (url.Contains('?') ? "&" : "?") + "handler=" + acao, new FormUrlEncodedContent(campos));
        }
        public async Task<string> Html(string url) => await WebTestHtml.LerHtmlDecodificadoAsync(await Client.GetAsync(url));
        public async Task AlterarDb(Func<PrecificadorDbContext, Task> acao)
        {
            using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            await acao(db); await db.SaveChangesAsync();
        }
        public async Task Verificar(Func<PrecificadorDbContext, Task> acao)
        {
            using var scope = Factory.Services.CreateScope(); await acao(scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>());
        }
        public static string Detalhe(string id) => "/Usuarios/Detalhes?usuarioId=" + id;
        public void Dispose() { Client.Dispose(); Factory.Dispose(); original.Dispose(); }
    }

    private sealed class EndpointContexto : Microsoft.AspNetCore.Hosting.IStartupFilter
    {
        public Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> Configure(Action<Microsoft.AspNetCore.Builder.IApplicationBuilder> next)
            => app => { next(app); app.UseEndpoints(endpoints => endpoints.MapGet("/TesteUc031Contexto", (EmpresaContext empresa) => empresa.EmpresaId?.ToString() ?? "ausente")); };
    }

}
