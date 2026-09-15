using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Tests.Integration.Web;

public sealed class ConfiguracaoPrecificacaoPageTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    private readonly WebTestContext web = new(factory);

    [Fact]
    public async Task W1_Pagina_exige_autenticacao_e_empresa_ativa()
    {
        using var anonimo = factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var acessoAnonimo = await anonimo.GetAsync("/Configuracoes/Precificacao");

        Assert.Equal(HttpStatusCode.Redirect, acessoAnonimo.StatusCode);
        Assert.Contains("/Conta/Login", acessoAnonimo.Headers.Location!.ToString());

        using var autenticadoSemEmpresaAtiva = await CriarClienteAutenticadoSemEmpresaAtivaAsync();
        var acessoSemEmpresa = await autenticadoSemEmpresaAtiva.GetAsync("/Configuracoes/Precificacao");

        Assert.Equal(HttpStatusCode.Redirect, acessoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoSemEmpresa.Headers.Location!.ToString());
    }

    [Fact]
    public async Task W2_W3_W4_W8_W9_Empresa_padrao_exibe_campos_ajuda_navegacao_e_sem_edicao()
    {
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync("/Configuracoes/Precificacao");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Equal(4, Regex.Matches(conteudo, "Não configurado").Count);
        Assert.Contains("Valor da hora de trabalho", conteudo);
        Assert.Contains("Tarifa de energia (R$/kWh)", conteudo);
        Assert.Contains("Margem padrão para novos produtos (%)", conteudo);
        Assert.Contains("Incremento comercial de arredondamento", conteudo);
        Assert.Contains("Reserva comercial para desconto (%)", conteudo);
        Assert.Contains("10%", conteudo);
        Assert.Contains("Percentual reservado acima do preço sugerido antes de formar o desconto de referência.", conteudo);
        Assert.Contains("Configurações", conteudo);
        Assert.Contains("href=\"/Configuracoes/Precificacao\"", conteudo);
        Assert.DoesNotContain("/Configuracoes/Precificacao/Editar", conteudo);
        Assert.DoesNotContain("Alterar", conteudo);
        Assert.DoesNotContain("Editar", conteudo);
    }

    [Fact]
    public async Task W5_Troca_de_empresa_ativa_muda_configuracao_visivel()
    {
        var empresaUm = await web.CriarEmpresaAsync("Empresa config um");
        var empresaDois = await web.CriarEmpresaAsync("Empresa config dois");
        await AtualizarConfiguracaoAsync(empresaUm, 12.34m, null, 0.075m, 0.50m, 0.10m);
        await AtualizarConfiguracaoAsync(empresaDois, 98.76m, null, 0.125m, 1.25m, 0.20m);
        var usuario = await web.CriarUsuarioAsync(empresaUm, empresaDois);
        using var client = web.CriarCliente();
        var login = await web.LoginAsync(client, usuario.Email, usuario.Senha);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        await SelecionarEmpresaAsync(client, empresaUm);
        var empresaUmHtml = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));
        await SelecionarEmpresaAsync(client, empresaDois);
        var empresaDoisHtml = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));

        Assert.Contains("R$ 12,34", empresaUmHtml);
        Assert.Contains("7,5%", empresaUmHtml);
        Assert.DoesNotContain("R$ 98,76", empresaUmHtml);
        Assert.Contains("R$ 98,76", empresaDoisHtml);
        Assert.Contains("12,5%", empresaDoisHtml);
        Assert.DoesNotContain("R$ 12,34", empresaDoisHtml);
    }

    [Fact]
    public async Task W6_Empresa_ativa_nao_exibe_valores_preparados_para_outra_empresa()
    {
        var empresaDois = await web.CriarEmpresaAsync("Empresa config isolada");
        await AtualizarConfiguracaoAsync(empresaDois, 77.77m, 66.66m, 0.333m, 5.55m, 0.44m);
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));

        Assert.DoesNotContain("77,77", conteudo);
        Assert.DoesNotContain("66,66", conteudo);
        Assert.DoesNotContain("33,3%", conteudo);
        Assert.DoesNotContain("44%", conteudo);
    }

    [Fact]
    public async Task W7_Get_nao_cria_configuracao_ausente()
    {
        var empresaSemConfiguracao = await web.CriarEmpresaAsync("Empresa sem config");
        await RemoverConfiguracaoAsync(empresaSemConfiguracao);
        using var client = await web.CriarClienteAutenticadoAsync(empresaSemConfiguracao);

        var response = await client.GetAsync("/Configuracoes/Precificacao");
        var quantidade = await ContarConfiguracoesAsync(empresaSemConfiguracao);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, quantidade);
    }

    [Fact]
    public async Task W10_Percentuais_e_numeros_usam_pt_BR_deterministico()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa formatacao");
        await AtualizarConfiguracaoAsync(empresa, 12.345678m, 0.987654m, 0.075m, 0.500001m, 0.125m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));

        Assert.Contains("R$ 12,345678", conteudo);
        Assert.Contains("R$ 0,987654", conteudo);
        Assert.Contains("7,5%", conteudo);
        Assert.Contains("R$ 0,500001", conteudo);
        Assert.Contains("12,5%", conteudo);
    }

    private async Task<HttpClient> CriarClienteAutenticadoSemEmpresaAtivaAsync()
    {
        var empresaDois = await web.CriarEmpresaAsync("Empresa selecao config");
        var usuario = await web.CriarUsuarioAsync(1, empresaDois);
        var client = web.CriarCliente();
        var resposta = await web.LoginAsync(client, usuario.Email, usuario.Senha);
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
        Assert.Contains("/Empresas/Selecionar", resposta.Headers.Location!.ToString());
        return client;
    }

    private static async Task SelecionarEmpresaAsync(HttpClient client, int empresaId)
    {
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Empresas/Selecionar");
        var resposta = await client.PostAsync("/Empresas/Selecionar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["EmpresaId"] = empresaId.ToString()
        }));
        Assert.Equal(HttpStatusCode.Redirect, resposta.StatusCode);
    }

    private async Task AtualizarConfiguracaoAsync(
        int empresaId,
        decimal? valorHoraTrabalho,
        decimal? tarifaEnergiaKwh,
        decimal? margemPadrao,
        decimal? incrementoComercial,
        decimal reservaComercialDesconto)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            UPDATE ConfiguracoesPrecificacaoEmpresas
            SET ValorHoraTrabalho = {valorHoraTrabalho},
                TarifaEnergiaKwh = {tarifaEnergiaKwh},
                MargemPadrao = {margemPadrao},
                IncrementoComercial = {incrementoComercial},
                ReservaComercialDesconto = {reservaComercialDesconto}
            WHERE EmpresaId = {empresaId}
            """);
    }

    private async Task RemoverConfiguracaoAsync(int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            DELETE FROM ConfiguracoesPrecificacaoEmpresas
            WHERE EmpresaId = {empresaId}
            """);
    }

    private async Task<int> ContarConfiguracoesAsync(int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.ConfiguracoesPrecificacaoEmpresas.CountAsync();
    }
}
