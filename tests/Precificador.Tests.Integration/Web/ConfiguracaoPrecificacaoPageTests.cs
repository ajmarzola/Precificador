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

        var acessoAnonimoEdicao = await anonimo.GetAsync("/Configuracoes/Precificacao/Editar");
        Assert.Equal(HttpStatusCode.Redirect, acessoAnonimoEdicao.StatusCode);
        Assert.Contains("/Conta/Login", acessoAnonimoEdicao.Headers.Location!.ToString());

        using var autenticadoSemEmpresaAtiva = await CriarClienteAutenticadoSemEmpresaAtivaAsync();
        var acessoSemEmpresa = await autenticadoSemEmpresaAtiva.GetAsync("/Configuracoes/Precificacao");
        Assert.Equal(HttpStatusCode.Redirect, acessoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoSemEmpresa.Headers.Location!.ToString());

        var acessoEdicaoSemEmpresa = await autenticadoSemEmpresaAtiva.GetAsync("/Configuracoes/Precificacao/Editar");
        Assert.Equal(HttpStatusCode.Redirect, acessoEdicaoSemEmpresa.StatusCode);
        Assert.Contains("/Conta/Login", acessoEdicaoSemEmpresa.Headers.Location!.ToString());
    }

    [Fact]
    public async Task W2_W3_W4_W8_W16_Empresa_padrao_exibe_campos_ajuda_navegacao_e_link_de_edicao()
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
        Assert.Contains("href=\"/Configuracoes/Precificacao/Editar\"", conteudo);
        Assert.Contains("Alterar configurações", conteudo);
    }

    [Fact]
    public async Task UC027_W2_W3_Get_edicao_carrega_valores_atuais_nulls_vazios_e_percentuais_convertidos()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa edicao get");
        await AtualizarConfiguracaoAsync(empresa, 12.34m, null, 0.075m, 0.500001m, 0.125m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await client.GetAsync("/Configuracoes/Precificacao/Editar");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Equal("12,34", ValorDoInput(conteudo, "Input.ValorHoraTrabalho"));
        Assert.Equal(string.Empty, ValorDoInput(conteudo, "Input.TarifaEnergiaKwh"));
        Assert.Equal("7,5", ValorDoInput(conteudo, "Input.MargemPadraoPercentual"));
        Assert.Equal("0,500001", ValorDoInput(conteudo, "Input.IncrementoComercial"));
        Assert.Equal("12,5", ValorDoInput(conteudo, "Input.ReservaComercialDescontoPercentual"));
        Assert.DoesNotContain("EmpresaId", conteudo);
        Assert.DoesNotContain("Input.Id", conteudo);
    }

    [Fact]
    public async Task UC027_Get_edicao_com_configuracao_ausente_retorna_404_sem_criar()
    {
        var empresaSemConfiguracao = await web.CriarEmpresaAsync("Empresa edicao sem config");
        await RemoverConfiguracaoAsync(empresaSemConfiguracao);
        using var client = await web.CriarClienteAutenticadoAsync(empresaSemConfiguracao);

        var response = await client.GetAsync("/Configuracoes/Precificacao/Editar");
        var quantidade = await ContarConfiguracoesAsync(empresaSemConfiguracao);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal(0, quantidade);
    }

    [Fact]
    public async Task UC027_W4_Post_valido_atualiza_e_redireciona_com_mensagem()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa post valido");
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "12,34", "0,98", "30", "0,50", "15");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.Equal("/Configuracoes/Precificacao", response.Headers.Location!.ToString());
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(12.34m, configuracao.ValorHoraTrabalho);
        Assert.Equal(0.98m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.30m, configuracao.MargemPadrao);
        Assert.Equal(0.50m, configuracao.IncrementoComercial);
        Assert.Equal(0.15m, configuracao.ReservaComercialDesconto);

        var paginaConsulta = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location!));
        Assert.Contains("Configurações de precificação atualizadas com sucesso.", paginaConsulta);
        Assert.Contains("R$ 12,34", paginaConsulta);
        Assert.Contains("30%", paginaConsulta);
        Assert.Contains("15%", paginaConsulta);
    }

    [Fact]
    public async Task UC027_W5_Campos_opcionais_vazios_limpam_para_null()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa limpa nulls");
        await AtualizarConfiguracaoAsync(empresa, 12.34m, 0.98m, 0.30m, 0.50m, 0.15m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "", "  ", "", "", "10");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Null(configuracao.ValorHoraTrabalho);
        Assert.Null(configuracao.TarifaEnergiaKwh);
        Assert.Null(configuracao.MargemPadrao);
        Assert.Null(configuracao.IncrementoComercial);
        Assert.Equal(0.10m, configuracao.ReservaComercialDesconto);
        var consulta = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));
        Assert.Equal(4, Regex.Matches(consulta, "Não configurado").Count);
    }

    [Fact]
    public async Task UC027_W6_Zero_valido_e_preservado()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa zeros");
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "0", "0", "0", "", "0");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0m, configuracao.ValorHoraTrabalho);
        Assert.Equal(0m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0m, configuracao.MargemPadrao);
        Assert.Null(configuracao.IncrementoComercial);
        Assert.Equal(0m, configuracao.ReservaComercialDesconto);
        var consulta = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));
        Assert.Contains("R$ 0,00", consulta);
        Assert.Contains("0%", consulta);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task UC027_W7_Incremento_zero_ou_negativo_e_rejeitado(string incremento)
    {
        var empresa = await web.CriarEmpresaAsync($"Empresa incremento invalido {Guid.NewGuid():N}");
        await AtualizarConfiguracaoAsync(empresa, 1m, 1m, 0.10m, 0.50m, 0.10m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "2", "2", "20", incremento, "15");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("O incremento comercial deve ser maior que zero.", conteudo);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0.50m, configuracao.IncrementoComercial);
    }

    [Theory]
    [InlineData("-1")]
    [InlineData("100")]
    public async Task UC027_W8_Margem_invalida_e_rejeitada(string margem)
    {
        var empresa = await web.CriarEmpresaAsync($"Empresa margem invalida {Guid.NewGuid():N}");
        await AtualizarConfiguracaoAsync(empresa, 1m, 1m, 0.10m, 0.50m, 0.10m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "2", "2", margem, "1", "15");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("A margem padrão deve ser maior ou igual a 0% e menor que 100%.", conteudo);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0.10m, configuracao.MargemPadrao);
    }

    [Theory]
    [InlineData("")]
    [InlineData("-1")]
    [InlineData("100")]
    public async Task UC027_W9_Reserva_vazia_negativa_ou_maior_igual_100_e_rejeitada(string reserva)
    {
        var empresa = await web.CriarEmpresaAsync($"Empresa reserva invalida {Guid.NewGuid():N}");
        await AtualizarConfiguracaoAsync(empresa, 1m, 1m, 0.10m, 0.50m, 0.10m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "2", "2", "20", "1", reserva);
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(
            conteudo.Contains("A reserva comercial para desconto é obrigatória.") ||
            conteudo.Contains("A reserva comercial para desconto deve ser maior ou igual a 0% e menor que 100%."));
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0.10m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_W10_Virgula_decimal_pt_BR_e_aceita()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa virgula");
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "123,45", "0,75", "10,5", "0,25", "7,5");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(123.45m, configuracao.ValorHoraTrabalho);
        Assert.Equal(0.75m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.105m, configuracao.MargemPadrao);
        Assert.Equal(0.25m, configuracao.IncrementoComercial);
        Assert.Equal(0.075m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_W11_Post_invalido_preserva_inputs_e_banco_inalterado()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa invalido preserva");
        await AtualizarConfiguracaoAsync(empresa, 1m, 1m, 0.10m, 0.50m, 0.10m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "22", "abc", "33", "0,75", "12");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("22", ValorDoInput(conteudo, "Input.ValorHoraTrabalho"));
        Assert.Equal("abc", ValorDoInput(conteudo, "Input.TarifaEnergiaKwh"));
        Assert.Equal("33", ValorDoInput(conteudo, "Input.MargemPadraoPercentual"));
        Assert.Equal("0,75", ValorDoInput(conteudo, "Input.IncrementoComercial"));
        Assert.Equal("12", ValorDoInput(conteudo, "Input.ReservaComercialDescontoPercentual"));
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(1m, configuracao.ValorHoraTrabalho);
        Assert.Equal(1m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.10m, configuracao.MargemPadrao);
        Assert.Equal(0.50m, configuracao.IncrementoComercial);
        Assert.Equal(0.10m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_W12_W14_Request_manipulado_nao_muda_alvo_nem_outro_tenant()
    {
        var empresaUm = await web.CriarEmpresaAsync("Empresa manipulada um");
        var empresaDois = await web.CriarEmpresaAsync("Empresa manipulada dois");
        await AtualizarConfiguracaoAsync(empresaDois, 77m, 7m, 0.70m, 7m, 0.17m);
        using var client = await web.CriarClienteAutenticadoAsync(empresaUm);

        var response = await EnviarFormularioEdicaoAsync(client, "11", "1", "10", "0,25", "5", new Dictionary<string, string>
        {
            ["EmpresaId"] = empresaDois.ToString(),
            ["Input.EmpresaId"] = empresaDois.ToString(),
            ["Id"] = empresaDois.ToString(),
            ["Input.Id"] = empresaDois.ToString()
        });

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var configuracaoUm = await ObterConfiguracaoAsync(empresaUm);
        var configuracaoDois = await ObterConfiguracaoAsync(empresaDois);
        Assert.Equal(11m, configuracaoUm.ValorHoraTrabalho);
        Assert.Equal(77m, configuracaoDois.ValorHoraTrabalho);
        Assert.Equal(0.70m, configuracaoDois.MargemPadrao);
    }

    [Fact]
    public async Task UC027_W13_Troca_de_empresa_altera_configuracao_editada()
    {
        var empresaUm = await web.CriarEmpresaAsync("Empresa troca um");
        var empresaDois = await web.CriarEmpresaAsync("Empresa troca dois");
        var usuario = await web.CriarUsuarioAsync(empresaUm, empresaDois);
        using var client = web.CriarCliente();
        Assert.Equal(HttpStatusCode.Redirect, (await web.LoginAsync(client, usuario.Email, usuario.Senha)).StatusCode);

        await SelecionarEmpresaAsync(client, empresaUm);
        Assert.Equal(HttpStatusCode.Redirect, await StatusDoPostEdicaoAsync(client, "10", "", "", "", "10"));
        await SelecionarEmpresaAsync(client, empresaDois);
        Assert.Equal(HttpStatusCode.Redirect, await StatusDoPostEdicaoAsync(client, "20", "", "", "", "10"));

        Assert.Equal(10m, (await ObterConfiguracaoAsync(empresaUm)).ValorHoraTrabalho);
        Assert.Equal(20m, (await ObterConfiguracaoAsync(empresaDois)).ValorHoraTrabalho);
    }

    [Fact]
    public async Task UC027_W15_Post_sem_antiforgery_retorna_400_sem_mutacao()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa sem antiforgery");
        await AtualizarConfiguracaoAsync(empresa, 1m, 1m, 0.10m, 0.50m, 0.10m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await client.PostAsync("/Configuracoes/Precificacao/Editar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.ValorHoraTrabalho"] = "99",
            ["Input.TarifaEnergiaKwh"] = "9",
            ["Input.MargemPadraoPercentual"] = "90",
            ["Input.IncrementoComercial"] = "9",
            ["Input.ReservaComercialDescontoPercentual"] = "9"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(1m, configuracao.ValorHoraTrabalho);
        Assert.Equal(0.10m, configuracao.MargemPadrao);
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

        Assert.Contains("R$ 12,35", conteudo);
        Assert.Contains("R$ 0,99", conteudo);
        Assert.Contains("7,5%", conteudo);
        Assert.Contains("R$ 0,50", conteudo);
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

    private static async Task<HttpStatusCode> StatusDoPostEdicaoAsync(
        HttpClient client,
        string valorHoraTrabalho,
        string tarifaEnergiaKwh,
        string margemPadraoPercentual,
        string incrementoComercial,
        string reservaComercialDescontoPercentual)
    {
        var response = await EnviarFormularioEdicaoAsync(
            client,
            valorHoraTrabalho,
            tarifaEnergiaKwh,
            margemPadraoPercentual,
            incrementoComercial,
            reservaComercialDescontoPercentual);
        return response.StatusCode;
    }

    private static async Task<HttpResponseMessage> EnviarFormularioEdicaoAsync(
        HttpClient client,
        string valorHoraTrabalho,
        string tarifaEnergiaKwh,
        string margemPadraoPercentual,
        string incrementoComercial,
        string reservaComercialDescontoPercentual,
        Dictionary<string, string>? camposExtras = null)
    {
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, "/Configuracoes/Precificacao/Editar");
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.ValorHoraTrabalho"] = valorHoraTrabalho,
            ["Input.TarifaEnergiaKwh"] = tarifaEnergiaKwh,
            ["Input.MargemPadraoPercentual"] = margemPadraoPercentual,
            ["Input.IncrementoComercial"] = incrementoComercial,
            ["Input.ReservaComercialDescontoPercentual"] = reservaComercialDescontoPercentual
        };

        if (camposExtras is not null)
        {
            foreach (var campo in camposExtras)
            {
                dados[campo.Key] = campo.Value;
            }
        }

        return await client.PostAsync("/Configuracoes/Precificacao/Editar", new FormUrlEncodedContent(dados));
    }

    private async Task<ConfiguracaoPrecificacaoEmpresa> ObterConfiguracaoAsync(int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        return await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking().SingleAsync();
    }

    private static string ValorDoInput(string html, string nome)
    {
        var pattern = "<input[^>]+name=\"" + Regex.Escape(nome) + "\"[^>]*value=\"([^\"]*)\"";
        var match = Regex.Match(html, pattern);
        return match.Success ? match.Groups[1].Value : string.Empty;
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
