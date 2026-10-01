using System.Collections.Concurrent;
using System.Net;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Precificador.Core.Acessos;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Autenticacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Administracao;
using Precificador.Web.Email;

namespace Precificador.Tests.Integration.Web;

public sealed class AdministracaoEmpresasPageTests
{
    [Theory]
    [InlineData("novo")] [InlineData("sem-senha")] [InlineData("com-senha")]
    public async Task Aprovar_cria_tenant_completo_reutiliza_identidade_e_comunica_depois_do_commit(string estado)
    {
        using var h = new Cenario();
        var id = await h.Solicitar();
        string? usuarioId = null, hash = null, stamp = null;
        if (estado != "novo")
        {
            using var scope = h.Factory.Services.CreateScope();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var usuario = new UsuarioAplicacao { UserName = h.Email, Email = h.Email };
            Assert.True((estado == "com-senha" ? await manager.CreateAsync(usuario, "SenhaTeste1") : await manager.CreateAsync(usuario)).Succeeded);
            usuarioId = usuario.Id; hash = usuario.PasswordHash; stamp = usuario.SecurityStamp;
        }
        await h.Login();
        h.Sender.AoEnviar = async () =>
        {
            using var scope = h.Factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Aprovada, (await db.SolicitacoesAcessoEmpresas.FindAsync(id))!.Situacao);
            Assert.True(await db.Empresas.AnyAsync(x => x.NomeNormalizado == h.Nome.ToUpperInvariant()));
        };
        var response = await h.PostSolicitacao(id, "Aprovar", new() { ["Aprovacao.Nome"] = "  " + h.Nome + "  ",
            ["EmpresaId"] = "1", ["Situacao"] = "3", ["DecididaPorUsuarioId"] = "forjado", ["EhTecnica"] = "true", ["Ativo"] = "false", ["Perfil"] = "1" });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        using var ver = h.Factory.Services.CreateScope();
        var contexto = ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var solicitacao = (await contexto.SolicitacoesAcessoEmpresas.FindAsync(id))!;
        var empresa = (await contexto.Empresas.FindAsync(solicitacao.EmpresaId))!;
        Assert.False(empresa.EhTecnica); Assert.True(empresa.Ativo); Assert.Null(empresa.EncerradaEmUtc);
        Assert.Equal(h.AdminId, solicitacao.DecididaPorUsuarioId);
        Assert.Equal(Cenario.DataFixa, solicitacao.DataDecisaoUtc);
        var vinculo = await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == empresa.Id);
        Assert.True(vinculo.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Administrador, vinculo.Perfil);
        Assert.False(await contexto.UsuariosEmpresas.AnyAsync(x => x.UsuarioId == h.AdminId));
        // Leitura física explícita da configuração criada; não concede acesso ao Admin.
        var config = await contexto.ConfiguracoesPrecificacaoEmpresas.IgnoreQueryFilters().SingleAsync(x => x.EmpresaId == empresa.Id);
        Assert.Equal(ConfiguracaoPrecificacaoEmpresa.PercentualMaoDeObraPadrao, config.PercentualMaoDeObra);
        var user = await contexto.Users.SingleAsync(x => x.NormalizedEmail == h.Email.ToUpperInvariant());
        if (usuarioId is not null) { Assert.Equal(usuarioId, user.Id); Assert.Equal(hash, user.PasswordHash); Assert.Equal(stamp, user.SecurityStamp); }
        else Assert.Null(user.PasswordHash);
        var mensagem = Assert.Single(h.Sender.Mensagens);
        Assert.Contains(estado == "com-senha" ? "/Conta/Login" : "/Conta/Ativar?", mensagem.Texto);
        if (estado == "com-senha") Assert.DoesNotContain("code=", mensagem.Texto);
        Assert.Contains("Empresa criada", await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync($"/Admin/Solicitacoes/Detalhes/{id}")));
    }

    [Theory]
    [InlineData("systemadmin")] [InlineData("colisao")]
    public async Task Aprovar_responsavel_global_ou_nome_existente_preserva_pendente_sem_efeitos(string erro)
    {
        using var h = new Cenario(); await h.Login();
        if (erro == "systemadmin") h.Email = h.AdminEmail;
        else await h.CriarEmpresa();
        var id = await h.Solicitar();
        var resposta = await h.PostSolicitacao(id, "Aprovar", new() { ["Aprovacao.Nome"] = h.Nome });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        Assert.Contains(erro == "systemadmin" ? "Administrador do Sistema" : "Já existe", await WebTestHtml.LerHtmlDecodificadoAsync(resposta));
        using var scope = h.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Pendente, (await db.SolicitacoesAcessoEmpresas.FindAsync(id))!.Situacao);
        Assert.Empty(await db.UsuariosEmpresas.ToListAsync()); Assert.Empty(h.Sender.Mensagens);
    }

    [Theory]
    [InlineData("mesma")] [InlineData("email")] [InlineData("nome")]
    public async Task Aprovar_concorrente_controla_mesma_solicitacao_email_e_nome_em_SQL_Server(string corrida)
    {
        using var h = new Cenario(); await h.Login();
        var a = await h.Solicitar();
        var nomeA = h.Nome;
        if (corrida == "email") h.Nome += " Segunda";
        if (corrida == "nome") h.Email = "outra-" + h.Email;
        var b = corrida == "mesma" ? a : await h.Solicitar();
        var nomeB = h.Nome;
        var tokenA = await WebTestHtml.ObterTokenAntiforgeryAsync(h.Client, $"/Admin/Solicitacoes/Detalhes/{a}");
        var tokenB = await WebTestHtml.ObterTokenAntiforgeryAsync(h.Client, $"/Admin/Solicitacoes/Detalhes/{b}");
        Task<HttpResponseMessage> Post(int id, string nome, string token) => h.Client.PostAsync($"/Admin/Solicitacoes/Detalhes/{id}?handler=Aprovar",
            new FormUrlEncodedContent(new Dictionary<string,string> { ["Aprovacao.Nome"] = nome, ["__RequestVerificationToken"] = token }));
        var respostas = await Task.WhenAll(Post(a, nomeA, tokenA), Post(b, nomeB, tokenB));
        Assert.All(respostas, x => Assert.Contains(x.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Redirect }));
        Assert.Equal(corrida == "email" ? 2 : 1, respostas.Count(x => x.StatusCode == HttpStatusCode.Redirect));
        using var scope = h.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(corrida == "email" ? 2 : 1, await db.Empresas.CountAsync(x => !x.EhTecnica));
        Assert.Equal(corrida == "email" ? 2 : 1, await db.UsuariosEmpresas.CountAsync());
        Assert.Equal(2, await db.Users.CountAsync()); // SystemAdmin + uma identidade responsável
        if (corrida == "nome")
        {
            Assert.Equal(1, await db.SolicitacoesAcessoEmpresas.CountAsync(x => x.Situacao == SituacaoSolicitacaoAcessoEmpresa.Pendente));
            Assert.Equal(1, await db.SolicitacoesAcessoEmpresas.CountAsync(x => x.Situacao == SituacaoSolicitacaoAcessoEmpresa.Aprovada));
        }
    }

    [Fact]
    public async Task Falha_pre_commit_reverte_inclusive_Identity()
    {
        var falha = new FalhaDecisao();
        using var h = new Cenario(falha);
        await h.Login(); var id = await h.Solicitar();
        falha.Ativa = true;
        var resposta = await h.PostSolicitacao(id, "Aprovar", new() { ["Aprovacao.Nome"] = h.Nome });
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var scope = h.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Pendente, (await db.SolicitacoesAcessoEmpresas.FindAsync(id))!.Situacao);
        Assert.False(await db.Users.AnyAsync(x => x.Email == h.Email));
        Assert.False(await db.Empresas.AnyAsync(x => !x.EhTecnica));
        Assert.Empty(await db.UsuariosEmpresas.ToListAsync());
        Assert.False(await db.ConfiguracoesPrecificacaoEmpresas.IgnoreQueryFilters().AnyAsync(x => x.EmpresaId != 1));
        Assert.Empty(h.Sender.Mensagens);
    }

    [Fact]
    public async Task Falha_email_pos_commit_preserva_aprovacao_e_permite_reenvio()
    {
        using var h = new Cenario(); await h.Login(); var id = await h.Solicitar();
        h.Sender.Falhar = true;
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostSolicitacao(id, "Aprovar", new() { ["Aprovacao.Nome"] = h.Nome })).StatusCode);
        Assert.Contains("não pôde ser enviada", await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync($"/Admin/Solicitacoes/Detalhes/{id}")));
        int empresaId; string userId;
        using (var scope = h.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            empresaId = (await db.SolicitacoesAcessoEmpresas.FindAsync(id))!.EmpresaId!.Value;
            userId = (await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == empresaId)).UsuarioId;
        }
        h.Sender.Falhar = false;
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostEmpresa(empresaId, "Reenviar", new() { ["usuarioId"] = userId })).StatusCode);
        Assert.Single(h.Sender.Mensagens);
    }

    [Theory]
    [InlineData(false)] [InlineData(true)]
    public async Task Recusa_terminal_nao_cria_tenant_e_nao_envia_motivo_interno(bool falhar)
    {
        using var h = new Cenario(); await h.Login(); var id = await h.Solicitar(); h.Sender.Falhar = falhar;
        var resposta = await h.PostSolicitacao(id, "Recusar", new() { ["Recusa.Motivo"] = "  Segredo interno  " });
        Assert.True(resposta.StatusCode == HttpStatusCode.Redirect, await WebTestHtml.LerHtmlDecodificadoAsync(resposta));
        using var scope = h.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var item = (await db.SolicitacoesAcessoEmpresas.FindAsync(id))!;
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Recusada, item.Situacao); Assert.Equal("Segredo interno", item.MotivoRecusa);
        Assert.Equal(h.AdminId, item.DecididaPorUsuarioId); Assert.Equal(Cenario.DataFixa, item.DataDecisaoUtc);
        Assert.Null(item.EmpresaId); Assert.Single(await db.Users.ToListAsync()); Assert.Empty(await db.UsuariosEmpresas.ToListAsync());
        Assert.All(h.Sender.Mensagens, m => Assert.DoesNotContain("Segredo", m.Texto));
        Assert.Equal(HttpStatusCode.OK, (await h.PostSolicitacao(id, "Aprovar", new() { ["Aprovacao.Nome"] = h.Nome })).StatusCode);
    }

    [Theory]
    [InlineData("novo")] [InlineData("operacional")] [InlineData("inativo")] [InlineData("com-senha")] [InlineData("sem-senha")]
    public async Task Definir_admin_corrige_legado_sem_promocao_automatica(string estado)
    {
        using var h = new Cenario(); await h.Login(); var empresa = await h.CriarEmpresa();
        string? userId = null;
        if (estado != "novo")
        {
            using var scope = h.Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var u = new UsuarioAplicacao { Email = h.Email, UserName = h.Email };
            Assert.True((estado == "com-senha" ? await manager.CreateAsync(u, "SenhaTeste1") : await manager.CreateAsync(u)).Succeeded);
            userId = u.Id;
            if (estado is "operacional" or "inativo")
            {
                db.UsuariosEmpresas.Add(new UsuarioEmpresa { EmpresaId = empresa, UsuarioId = u.Id, Ativo = estado != "inativo", Perfil = PerfilUsuarioEmpresa.Operacional });
                await db.SaveChangesAsync();
            }
        }
        Assert.Contains("Sem administrador", await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync($"/Admin/Empresas/Detalhes/{empresa}")));
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostEmpresa(empresa, "Definir", new() { ["Input.Email"] = h.Email })).StatusCode);
        using var ver = h.Factory.Services.CreateScope(); var contexto = ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var v = await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == empresa);
        if (userId is not null) Assert.Equal(userId, v.UsuarioId);
        Assert.True(v.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Administrador, v.Perfil);
        Assert.Single(h.Sender.Mensagens);
    }

    [Fact]
    public async Task Substituir_admin_preserva_outros_admins_vinculos_e_credencial_em_empresa_suspensa()
    {
        using var h = new Cenario(); await h.Login(); var id = await h.CriarEmpresa();
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostEmpresa(id, "Definir", new() { ["Input.Email"] = h.Email })).StatusCode);
        string atual, outro, terceiro, hash, stamp;
        int outra;
        using (var scope = h.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            atual = (await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id)).UsuarioId;
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var u = new UsuarioAplicacao { Email = "novo-" + h.Email, UserName = "novo-" + h.Email };
            Assert.True((await manager.CreateAsync(u, "SenhaTeste1")).Succeeded);
            outro = u.Id; hash = u.PasswordHash!; stamp = u.SecurityStamp!;
            var preservado = new UsuarioAplicacao { Email = "preservado-" + h.Email, UserName = "preservado-" + h.Email };
            Assert.True((await manager.CreateAsync(preservado)).Succeeded); terceiro = preservado.Id;
            var empresa = Empresa.Criar("Outra " + h.Nome); db.Empresas.Add(empresa); await db.SaveChangesAsync(); outra = empresa.Id;
            db.UsuariosEmpresas.AddRange(new UsuarioEmpresa { EmpresaId = id, UsuarioId = outro, Ativo = false, Perfil = PerfilUsuarioEmpresa.Operacional },
                new UsuarioEmpresa { EmpresaId = id, UsuarioId = terceiro, Ativo = true, Perfil = PerfilUsuarioEmpresa.Administrador },
                new UsuarioEmpresa { EmpresaId = outra, UsuarioId = atual, Ativo = true, Perfil = PerfilUsuarioEmpresa.Administrador });
            await db.SaveChangesAsync();
        }
        await h.PostEmpresa(id, "Suspender", new());
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostEmpresa(id, "Substituir", new() { ["Input.AdministradorAtual"] = atual, ["Input.Email"] = "novo-" + h.Email })).StatusCode);
        using var ver = h.Factory.Services.CreateScope(); var contexto = ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var anterior = await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id && x.UsuarioId == atual);
        Assert.True(anterior.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Operacional, anterior.Perfil);
        Assert.Equal(PerfilUsuarioEmpresa.Administrador, (await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == outra && x.UsuarioId == atual)).Perfil);
        Assert.Equal(PerfilUsuarioEmpresa.Administrador, (await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id && x.UsuarioId == outro)).Perfil);
        var adminPreservado = await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id && x.UsuarioId == terceiro);
        Assert.True(adminPreservado.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Administrador, adminPreservado.Perfil);
        Assert.True((await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id && x.UsuarioId == outro)).Ativo);
        var usuario = (await contexto.Users.FindAsync(outro))!; Assert.Equal(hash, usuario.PasswordHash); Assert.Equal(stamp, usuario.SecurityStamp);
    }

    [Fact]
    public async Task Lifecycle_preserva_dados_bloqueia_contexto_antigo_e_encerramento_exige_confirmacao()
    {
        using var h = new Cenario(); await h.Login(); var id = await h.CriarEmpresa();
        using (var scope = h.Factory.Services.CreateScope())
        {
            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            await using var tenantDb = new PrecificadorDbContext(options, new ContextoEmpresaTeste(id));
            tenantDb.Insumos.Add(Insumo.Criar(id, "Material confidencial", (CategoriaInsumo)1, (UnidadeMedida)1));
            await tenantDb.SaveChangesAsync();
        }
        Assert.Equal(HttpStatusCode.OK, (await h.PostEmpresa(id, "Reativar", new())).StatusCode);
        await h.PostEmpresa(id, "Definir", new() { ["Input.Email"] = h.Email });
        using (var scope = h.Factory.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            Assert.True((await manager.AddPasswordAsync((await manager.FindByEmailAsync(h.Email))!, "SenhaTeste1")).Succeeded);
        }
        using var tenant = h.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        await h.LoginCliente(tenant, h.Email);
        Assert.Equal(HttpStatusCode.OK, (await tenant.GetAsync("/Dashboard")).StatusCode);
        await h.PostEmpresa(id, "Suspender", new());
        Assert.Equal(HttpStatusCode.Redirect, (await tenant.GetAsync("/Dashboard")).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostEmpresa(id, "Reativar", new())).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await h.PostEmpresa(id, "Encerrar", new())).StatusCode);
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostEmpresa(id, "Encerrar", new() { ["ConfirmarEncerramento"] = "true" })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await h.PostEmpresa(id, "Reativar", new())).StatusCode);
        using var ver = h.Factory.Services.CreateScope(); var db = ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var empresa = (await db.Empresas.FindAsync(id))!; Assert.False(empresa.Ativo); Assert.Equal(Cenario.DataFixa, empresa.EncerradaEmUtc);
        Assert.Single(await db.UsuariosEmpresas.Where(x => x.EmpresaId == id).ToListAsync());
        Assert.True(await db.ConfiguracoesPrecificacaoEmpresas.IgnoreQueryFilters().AnyAsync(x => x.EmpresaId == id));
        Assert.Equal("Material confidencial", (await db.Insumos.IgnoreQueryFilters().SingleAsync(x => x.EmpresaId == id)).Nome);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync($"/Admin/Empresas/Detalhes/{id}"));
        Assert.DoesNotContain("handler=Reativar", html); Assert.DoesNotContain("handler=Definir", html);
        Assert.DoesNotContain("Material confidencial", html);
    }

    [Fact]
    public async Task Tecnica_e_alvos_SystemAdmin_rejeitados_post_sem_antiforgery_rejeitado()
    {
        using var h = new Cenario(); await h.Login(); var id = await h.CriarEmpresa();
        Assert.Equal(HttpStatusCode.NotFound, (await h.Client.GetAsync("/Admin/Empresas/Detalhes/1")).StatusCode);
        foreach (var acao in new[] { "Suspender", "Reativar", "Encerrar", "Definir", "Substituir", "Reenviar" })
        {
            var token = await WebTestHtml.ObterTokenAntiforgeryAsync(h.Client, "/Admin");
            Assert.Equal(HttpStatusCode.NotFound, (await h.Client.PostAsync($"/Admin/Empresas/Detalhes/1?handler={acao}", new FormUrlEncodedContent(new Dictionary<string,string> { ["__RequestVerificationToken"] = token }))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await h.Client.PostAsync($"/Admin/Empresas/Detalhes/{id}?handler={acao}", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
        }
        Assert.Equal(HttpStatusCode.OK, (await h.PostEmpresa(id, "Definir", new() { ["Input.Email"] = h.AdminEmail })).StatusCode);
        using var ver = h.Factory.Services.CreateScope();
        Assert.Empty(await ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>().UsuariosEmpresas.ToListAsync());
    }

    [Fact]
    public async Task Listas_filtros_ordenacao_encoding_contadores_e_admin_sem_dados_comerciais()
    {
        using var h = new Cenario(); await h.Login(); var id = await h.Solicitar();
        Assert.Contains(h.Nome, await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Solicitacoes")));
        Assert.Equal(HttpStatusCode.BadRequest, (await h.Client.GetAsync("/Admin/Solicitacoes?situacao=invalida")).StatusCode);
        await h.PostSolicitacao(id, "Recusar", new());
        Assert.DoesNotContain(h.Nome, await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Solicitacoes")));
        Assert.Contains(h.Nome, await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Solicitacoes?situacao=Recusada")));
        var empresa = await h.CriarEmpresa();
        var lista = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Empresas"));
        Assert.Contains(h.Nome, lista); Assert.Contains("Sem administrador", lista); Assert.DoesNotContain("Empresa inicial", lista);
        Assert.Equal(HttpStatusCode.BadRequest, (await h.Client.GetAsync("/Admin/Empresas?situacao=invalida")).StatusCode);
        var detalhe = await h.Client.GetAsync($"/Admin/Solicitacoes/Detalhes/{id}");
        var html = await detalhe.Content.ReadAsStringAsync();
        Assert.Contains("&lt;script&gt;", html); Assert.DoesNotContain("<script>observacao", html);
        Assert.DoesNotContain("handler=Aprovar", html);
        var admin = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin"));
        Assert.Contains("Empresas Ativas</dt><dd>1</dd>", admin);
        Assert.DoesNotContain("href=\"/Produtos\"", admin); Assert.DoesNotContain("href=\"/Insumos\"", admin);
        await h.PostEmpresa(empresa, "Suspender", new());
        Assert.Contains(h.Nome, await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Empresas?situacao=Suspensa")));
        Assert.DoesNotContain(h.Nome, await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Empresas?situacao=Ativa")));
    }

    [Theory]
    [InlineData("Enviado")] [InlineData("NaoNecessario")] [InlineData("Indisponivel")] [InlineData("Falhou")]
    public async Task Reenvio_trata_quatro_resultados_sem_duplicar_identidade_ou_vinculo(string resultado)
    {
        using var h = new Cenario(); await h.Login(); var id = await h.CriarEmpresa();
        await h.PostEmpresa(id, "Definir", new() { ["Input.Email"] = h.Email });
        string usuarioId;
        using (var scope = h.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            usuarioId = (await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id)).UsuarioId;
            if (resultado == "NaoNecessario")
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
                Assert.True((await manager.AddPasswordAsync((await manager.FindByIdAsync(usuarioId))!, "SenhaTeste1")).Succeeded);
            }
        }
        h.Sender.Mensagens.Clear(); h.Sender.Falhar = resultado == "Falhou"; h.Sender.Habilitado = resultado != "Indisponivel";
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostEmpresa(id, "Reenviar", new() { ["usuarioId"] = usuarioId })).StatusCode);
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync($"/Admin/Empresas/Detalhes/{id}"));
        Assert.Contains(resultado switch { "Enviado" => "Ativação enviada", "NaoNecessario" => "ativação não necessária", "Indisponivel" => "indisponível", _ => "falhou" }, html);
        Assert.Equal(resultado == "Enviado" ? 1 : 0, h.Sender.Mensagens.Count);
        using var ver = h.Factory.Services.CreateScope(); var contexto = ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(2, await contexto.Users.CountAsync()); Assert.Single(await contexto.UsuariosEmpresas.ToListAsync());
    }

    [Fact]
    public async Task Reativar_sem_admin_falha_definir_em_encerrada_e_substituir_SystemAdmin_nao_mutam()
    {
        using var h = new Cenario(); await h.Login(); var id = await h.CriarEmpresa();
        await h.PostEmpresa(id, "Suspender", new());
        Assert.Equal(HttpStatusCode.OK, (await h.PostEmpresa(id, "Reativar", new())).StatusCode);
        await h.PostEmpresa(id, "Definir", new() { ["Input.Email"] = h.Email });
        string atual;
        using (var scope = h.Factory.Services.CreateScope())
            atual = (await scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>().UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id)).UsuarioId;
        Assert.Equal(HttpStatusCode.OK, (await h.PostEmpresa(id, "Substituir", new() { ["Input.Email"] = h.AdminEmail, ["Input.AdministradorAtual"] = atual })).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await h.PostEmpresa(id, "Substituir", new() { ["Input.Email"] = h.Email, ["Input.AdministradorAtual"] = atual })).StatusCode);
        await h.PostEmpresa(id, "Encerrar", new() { ["ConfirmarEncerramento"] = "true" });
        foreach (var acao in new[] { "Definir", "Substituir", "Reenviar", "Suspender", "Reativar" })
            Assert.Equal(HttpStatusCode.OK, (await h.PostEmpresa(id, acao, new() { ["Input.Email"] = "outra-" + h.Email, ["Input.AdministradorAtual"] = atual, ["usuarioId"] = atual })).StatusCode);
        using var ver = h.Factory.Services.CreateScope(); var db = ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var vinculo = await db.UsuariosEmpresas.SingleAsync(); Assert.Equal(atual, vinculo.UsuarioId); Assert.Equal(PerfilUsuarioEmpresa.Administrador, vinculo.Perfil);
        Assert.False((await db.Empresas.FindAsync(id))!.Ativo); Assert.Equal(2, await db.Users.CountAsync());
    }

    [Theory]
    [InlineData("Nome")] [InlineData("Motivo")] [InlineData("Email")]
    public async Task Inputs_invalidos_nao_persistem_efeitos(string campo)
    {
        using var h = new Cenario(); await h.Login(); var solicitacao = await h.Solicitar(); var empresa = await h.CriarEmpresa();
        var resposta = campo switch
        {
            "Nome" => await h.PostSolicitacao(solicitacao, "Aprovar", new() { ["Aprovacao.Nome"] = new string('x', 121) }),
            "Motivo" => await h.PostSolicitacao(solicitacao, "Recusar", new() { ["Recusa.Motivo"] = new string('x', 501) }),
            _ => await h.PostEmpresa(empresa, "Definir", new() { ["Input.Email"] = "invalido" })
        };
        Assert.Equal(HttpStatusCode.OK, resposta.StatusCode);
        using var ver = h.Factory.Services.CreateScope(); var db = ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Pendente, (await db.SolicitacoesAcessoEmpresas.FindAsync(solicitacao))!.Situacao);
        Assert.Single(await db.Empresas.Where(x => !x.EhTecnica).ToListAsync()); Assert.Single(await db.Users.ToListAsync()); Assert.Empty(await db.UsuariosEmpresas.ToListAsync());
    }

    [Theory]
    [InlineData("novo")] [InlineData("sem-senha")] [InlineData("com-senha")]
    public async Task Substituir_cria_ou_reutiliza_novo_admin_e_preserva_vinculo_em_outra_empresa(string estado)
    {
        using var h = new Cenario(); await h.Login(); var id = await h.CriarEmpresa();
        await h.PostEmpresa(id, "Definir", new() { ["Input.Email"] = h.Email });
        var novoEmail = "novo-" + h.Email;
        string atual; string? novoId = null, hash = null, stamp = null;
        int outraId;
        using (var scope = h.Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            atual = (await db.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id)).UsuarioId;
            var outra = Empresa.Criar("Outra " + h.Nome); db.Empresas.Add(outra); await db.SaveChangesAsync(); outraId = outra.Id;
            if (estado != "novo")
            {
                var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
                var u = new UsuarioAplicacao { Email = novoEmail, UserName = novoEmail };
                Assert.True((estado == "com-senha" ? await manager.CreateAsync(u, "SenhaTeste1") : await manager.CreateAsync(u)).Succeeded);
                novoId = u.Id; hash = u.PasswordHash; stamp = u.SecurityStamp;
                db.UsuariosEmpresas.Add(new UsuarioEmpresa { EmpresaId = outraId, UsuarioId = u.Id, Ativo = false, Perfil = PerfilUsuarioEmpresa.Operacional });
                await db.SaveChangesAsync();
            }
        }
        h.Sender.Mensagens.Clear();
        Assert.Equal(HttpStatusCode.Redirect, (await h.PostEmpresa(id, "Substituir", new() { ["Input.Email"] = novoEmail, ["Input.AdministradorAtual"] = atual })).StatusCode);
        using var ver = h.Factory.Services.CreateScope(); var contexto = ver.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
        var usuario = await contexto.Users.SingleAsync(x => x.Email == novoEmail);
        if (novoId is not null)
        {
            Assert.Equal(novoId, usuario.Id); Assert.Equal(hash, usuario.PasswordHash); Assert.Equal(stamp, usuario.SecurityStamp);
            var outro = await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == outraId && x.UsuarioId == novoId);
            Assert.False(outro.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Operacional, outro.Perfil);
        }
        var novo = await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id && x.UsuarioId == usuario.Id);
        Assert.True(novo.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Administrador, novo.Perfil);
        var anterior = await contexto.UsuariosEmpresas.SingleAsync(x => x.EmpresaId == id && x.UsuarioId == atual);
        Assert.True(anterior.Ativo); Assert.Equal(PerfilUsuarioEmpresa.Operacional, anterior.Perfil);
        Assert.Contains(estado == "com-senha" ? "/Conta/Login" : "/Conta/Ativar?", Assert.Single(h.Sender.Mensagens).Texto);
    }

    [Fact]
    public async Task Listas_ordenam_por_data_e_nome_e_dashboard_conta_todos_estados_reais()
    {
        using var h = new Cenario(); await h.Login();
        var antigo = await h.Solicitar(); var nomeAntigo = h.Nome;
        h.Nome = "Z " + nomeAntigo; var recente = await h.Solicitar(); var nomeRecente = h.Nome;
        using (var scope = h.Factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>().Database.ExecuteSqlInterpolatedAsync($"UPDATE SolicitacoesAcessoEmpresas SET DataSolicitacaoUtc = {Cenario.DataFixa.AddDays(-1)} WHERE Id = {antigo}");
        var pendentes = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Solicitacoes"));
        Assert.True(pendentes.IndexOf("<td>" + nomeAntigo + "</td>", StringComparison.Ordinal) < pendentes.IndexOf("<td>" + nomeRecente + "</td>", StringComparison.Ordinal));
        var todas = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Solicitacoes?situacao=Todas"));
        Assert.True(todas.IndexOf("<td>" + nomeRecente + "</td>", StringComparison.Ordinal) < todas.IndexOf("<td>" + nomeAntigo + "</td>", StringComparison.Ordinal));
        await h.PostSolicitacao(antigo, "Aprovar", new() { ["Aprovacao.Nome"] = nomeAntigo });
        var aprovadas = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Solicitacoes?situacao=Aprovada"));
        Assert.Contains("<td>" + nomeAntigo + "</td>", aprovadas); Assert.DoesNotContain("<td>" + nomeRecente + "</td>", aprovadas);
        var suspensa = await h.CriarEmpresa(); await h.PostEmpresa(suspensa, "Suspender", new());
        h.Nome = "A " + nomeAntigo; var encerrada = await h.CriarEmpresa();
        await h.PostEmpresa(encerrada, "Encerrar", new() { ["ConfirmarEncerramento"] = "true" });
        var lista = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Empresas"));
        Assert.True(lista.IndexOf("<td>" + h.Nome + "</td>", StringComparison.Ordinal) < lista.IndexOf("<td>" + nomeAntigo + "</td>", StringComparison.Ordinal));
        var dashboard = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin"));
        foreach (var rotulo in new[] { "Solicitações Pendentes", "Empresas Ativas", "Empresas Suspensas", "Empresas Encerradas" })
            Assert.Contains(rotulo + "</dt><dd>1</dd>", dashboard);
        var encerradas = await WebTestHtml.LerHtmlDecodificadoAsync(await h.Client.GetAsync("/Admin/Empresas?situacao=Encerrada"));
        Assert.Contains("<td>" + h.Nome + "</td>", encerradas); Assert.DoesNotContain("<td>" + nomeAntigo + "</td>", encerradas);
        foreach (var acao in new[] { "Aprovar", "Recusar" })
            Assert.Equal(HttpStatusCode.BadRequest, (await h.Client.PostAsync($"/Admin/Solicitacoes/Detalhes/{recente}?handler={acao}", new FormUrlEncodedContent(new Dictionary<string,string>()))).StatusCode);
    }

    private sealed class FalhaDecisao : SaveChangesInterceptor
    {
        public bool Ativa { get; set; }
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (Ativa && eventData.Context!.ChangeTracker.Entries<SolicitacaoAcessoEmpresa>().Any(x => x.Entity.Situacao == SituacaoSolicitacaoAcessoEmpresa.Aprovada))
                throw new InvalidOperationException("Falha de persistência provocada antes do commit.");
            return ValueTask.FromResult(result);
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
            if (Falhar) throw new InvalidOperationException("SMTP indisponível");
            Mensagens.Enqueue(mensagem);
        }
    }
    private sealed class Cenario : IDisposable
    {
        public static readonly DateTimeOffset DataFixa = new(2026, 10, 1, 15, 0, 0, TimeSpan.Zero);
        private readonly CustomWebApplicationFactory original = new();
        public WebApplicationFactory<Program> Factory { get; }
        public Sender Sender { get; } = new();
        public HttpClient Client { get; }
        public string Nome { get; set; } = "Empresa " + Guid.NewGuid().ToString("N");
        public string Email { get; set; } = "responsavel-" + Guid.NewGuid().ToString("N") + "@teste.local";
        public string AdminId { get; private set; } = "";
        public string AdminEmail { get; } = "global-" + Guid.NewGuid().ToString("N") + "@teste.local";
        public Cenario(FalhaDecisao? falha = null)
        {
            Factory = original.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
            {
                services.RemoveAll<IEmailSenderAplicacao>(); services.AddSingleton<IEmailSenderAplicacao>(Sender);
                services.RemoveAll<TimeProvider>(); services.AddSingleton<TimeProvider>(new TempoFixo());
                services.Configure<OpcoesAplicacao>(x => x.UrlPublica = "https://publico.test");
                if (falha is not null) services.AddDbContext<PrecificadorDbContext>(options => options.AddInterceptors(falha));
            }));
            Client = Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        }
        public async Task Login()
        {
            using var scope = Factory.Services.CreateScope();
            var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
            Assert.True((await roles.CreateAsync(new IdentityRole("SystemAdmin"))).Succeeded);
            var manager = scope.ServiceProvider.GetRequiredService<UserManager<UsuarioAplicacao>>();
            var u = new UsuarioAplicacao { Email = AdminEmail, UserName = AdminEmail };
            Assert.True((await manager.CreateAsync(u, "SenhaTeste1")).Succeeded);
            Assert.True((await manager.AddToRoleAsync(u, "SystemAdmin")).Succeeded);
            AdminId = u.Id;
            await LoginCliente(Client, AdminEmail);
        }
        public async Task LoginCliente(HttpClient client, string email)
        {
            var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Conta/Login");
            Assert.Equal(HttpStatusCode.Redirect, (await client.PostAsync("/Conta/Login", new FormUrlEncodedContent(new Dictionary<string,string>
            { ["__RequestVerificationToken"] = token, ["Input.Email"] = email, ["Input.Senha"] = "SenhaTeste1" }))).StatusCode);
        }
        public async Task<int> Solicitar()
        {
            using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var s = SolicitacaoAcessoEmpresa.Criar(Nome, "Ana", Email, "<script>observacao</script>", DataFixa);
            db.SolicitacoesAcessoEmpresas.Add(s); await db.SaveChangesAsync(); return s.Id;
        }
        public async Task<int> CriarEmpresa()
        {
            using var scope = Factory.Services.CreateScope(); var db = scope.ServiceProvider.GetRequiredService<PrecificadorDbContext>();
            var e = Empresa.Criar(Nome); db.Empresas.Add(e); await db.SaveChangesAsync();
            var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
            await using var tenant = new PrecificadorDbContext(options, new ContextoEmpresaTeste(e.Id));
            tenant.ConfiguracoesPrecificacaoEmpresas.Add(ConfiguracaoPrecificacaoEmpresa.CriarPadrao(e.Id));
            await tenant.SaveChangesAsync(); return e.Id;
        }
        public Task<HttpResponseMessage> PostSolicitacao(int id, string acao, Dictionary<string,string> campos) => Post($"/Admin/Solicitacoes/Detalhes/{id}", acao, campos);
        public Task<HttpResponseMessage> PostEmpresa(int id, string acao, Dictionary<string,string> campos) => Post($"/Admin/Empresas/Detalhes/{id}", acao, campos);
        private async Task<HttpResponseMessage> Post(string url, string acao, Dictionary<string,string> campos)
        {
            campos["__RequestVerificationToken"] = await WebTestHtml.ObterTokenAntiforgeryAsync(Client, url);
            return await Client.PostAsync(url + "?handler=" + acao, new FormUrlEncodedContent(campos));
        }
        public void Dispose() { Client.Dispose(); Factory.Dispose(); original.Dispose(); }
        private sealed class TempoFixo : TimeProvider { public override DateTimeOffset GetUtcNow() => DataFixa; }
    }
}
