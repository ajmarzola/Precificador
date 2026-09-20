using System.Net;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Precificador.Core.Empresas;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Produtos;
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
        Assert.Equal(3, Regex.Matches(conteudo, "Não configurado").Count);
        Assert.Contains("Mão de obra sobre os insumos (%)", conteudo);
        Assert.Contains("Tarifa de energia (R$/kWh)", conteudo);
        Assert.Contains("Margem padrão para novos produtos (%)", conteudo);
        Assert.Contains("Incremento comercial de arredondamento", conteudo);
        Assert.Contains("Reserva comercial para desconto (%)", conteudo);
        Assert.Contains("10%", conteudo);
        AssertAjudasPresentes(conteudo);
        Assert.Contains("Configurações", conteudo);
        Assert.Contains("href=\"/Configuracoes/Precificacao\"", conteudo);
        Assert.Contains("href=\"/Configuracoes/Precificacao/Editar\"", conteudo);
        Assert.Contains("Alterar configurações", conteudo);
    }

    [Fact]
    public async Task MEL017_W4_W5_Edicao_exibe_ajudas_e_as_associa_aos_inputs()
    {
        using var client = await web.CriarClienteAutenticadoAsync(1);

        var response = await client.GetAsync("/Configuracoes/Precificacao/Editar");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        AssertAjudasPresentes(conteudo);
        AssertInputDescreveAjuda(conteudo, "Input.PercentualMaoDeObra", "ajuda-percentual-mao-de-obra");
        AssertInputDescreveAjuda(conteudo, "Input.TarifaEnergiaKwh", "ajuda-tarifa-energia");
        AssertInputDescreveAjuda(conteudo, "Input.MargemPadraoPercentual", "ajuda-margem-padrao");
        AssertInputDescreveAjuda(conteudo, "Input.IncrementoComercial", "ajuda-incremento-comercial");
        AssertInputDescreveAjuda(conteudo, "Input.ReservaComercialDescontoPercentual", "ajuda-reserva-comercial");
    }

    [Fact]
    public async Task UC027_W2_W3_Get_edicao_carrega_valores_atuais_nulls_vazios_e_percentuais_convertidos()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa edicao get");
        await AtualizarConfiguracaoAsync(empresa, 0.1234m, null, 0.075m, 0.500001m, 0.125m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await client.GetAsync("/Configuracoes/Precificacao/Editar");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        response.EnsureSuccessStatusCode();
        Assert.Equal("12,34", ValorDoInput(conteudo, "Input.PercentualMaoDeObra"));
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
        Assert.Equal(0.1234m, configuracao.PercentualMaoDeObra);
        Assert.Equal(0.98m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.30m, configuracao.MargemPadrao);
        Assert.Equal(0.50m, configuracao.IncrementoComercial);
        Assert.Equal(0.15m, configuracao.ReservaComercialDesconto);

        var paginaConsulta = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(response.Headers.Location!));
        Assert.Contains("Configurações de precificação atualizadas com sucesso.", paginaConsulta);
        Assert.Contains("12,34%", paginaConsulta);
        Assert.Contains("30%", paginaConsulta);
        Assert.Contains("15%", paginaConsulta);
    }

    [Fact]
    public async Task MEL023_W16_W17_W18_W19_W23_ReturnUrlLocalRetornaParaFichaERecalculaEnergia()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa return local");
        var produto = await CriarProdutoAsync(empresa);
        var ficha = await CriarFichaAsync(empresa, produto);
        await CriarUsoAsync(empresa, ficha, "Forno", 1m, 60);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);
        var returnUrl = $"/Produtos/FichaTecnica/{produto}";
        var editarUrl = "/Configuracoes/Precificacao/Editar?returnUrl=" + Uri.EscapeDataString(returnUrl);

        var get = await client.GetAsync(editarUrl);
        var conteudoGet = await WebTestHtml.LerHtmlDecodificadoAsync(get);

        get.EnsureSuccessStatusCode();
        Assert.Equal(returnUrl, ValorDoInput(conteudoGet, "ReturnUrl"));
        Assert.Contains($"href=\"{returnUrl}\"", conteudoGet);

        var invalido = await EnviarFormularioEdicaoAsync(client, "10", "abc", "30", "0,50", "10", caminhoEdicao: editarUrl);
        var conteudoInvalido = await WebTestHtml.LerHtmlDecodificadoAsync(invalido);

        Assert.Equal(HttpStatusCode.OK, invalido.StatusCode);
        Assert.Equal("abc", ValorDoInput(conteudoInvalido, "Input.TarifaEnergiaKwh"));
        Assert.Equal(returnUrl, ValorDoInput(conteudoInvalido, "ReturnUrl"));
        Assert.Null((await ObterConfiguracaoAsync(empresa)).TarifaEnergiaKwh);

        var post = await EnviarFormularioEdicaoAsync(client, "10", "2", "30", "0,50", "10", caminhoEdicao: editarUrl);

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal(returnUrl, post.Headers.Location!.ToString());
        Assert.Equal(2m, (await ObterConfiguracaoAsync(empresa)).TarifaEnergiaKwh);

        var fichaAtualizada = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync(post.Headers.Location));
        Assert.Contains("Custo de energia do lote:</strong> R$ 2,00", fichaAtualizada);
        Assert.DoesNotContain("Tarifa de energia não configurada.", fichaAtualizada);
    }

    [Theory]
    [InlineData("https://exemplo.invalid/retorno")]
    [InlineData("//exemplo.invalid/retorno")]
    public async Task MEL023_W21_W22_ReturnUrlExternaNaoEhSeguida(string returnUrl)
    {
        var empresa = await web.CriarEmpresaAsync($"Empresa return externo {Guid.NewGuid():N}");
        using var client = await web.CriarClienteAutenticadoAsync(empresa);
        var editarUrl = "/Configuracoes/Precificacao/Editar?returnUrl=" + Uri.EscapeDataString(returnUrl);

        var get = await client.GetAsync(editarUrl);
        var conteudoGet = await WebTestHtml.LerHtmlDecodificadoAsync(get);

        get.EnsureSuccessStatusCode();
        Assert.Equal(string.Empty, ValorDoInput(conteudoGet, "ReturnUrl"));
        Assert.Contains("href=\"/Configuracoes/Precificacao\"", conteudoGet);

        var post = await EnviarFormularioEdicaoAsync(
            client,
            "10",
            "1",
            "30",
            "0,50",
            "10",
            new Dictionary<string, string> { ["ReturnUrl"] = returnUrl });

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        Assert.Equal("/Configuracoes/Precificacao", post.Headers.Location!.ToString());
    }

    [Fact]
    public async Task MEL022_Percentual_vazio_e_invalido_e_preserva_configuracao()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa limpa nulls");
        await AtualizarConfiguracaoAsync(empresa, 0.1234m, 0.98m, 0.30m, 0.50m, 0.15m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "", "  ", "", "", "10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);
        Assert.Contains("O percentual de mão de obra é obrigatório.", conteudo);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0.1234m, configuracao.PercentualMaoDeObra);
        Assert.Equal(0.98m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.30m, configuracao.MargemPadrao);
        Assert.Equal(0.50m, configuracao.IncrementoComercial);
        Assert.Equal(0.15m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public async Task UC027_W6_Zero_valido_e_preservado()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa zeros");
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "0", "0", "0", "", "0");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0m, configuracao.PercentualMaoDeObra);
        Assert.Equal(0m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0m, configuracao.MargemPadrao);
        Assert.Null(configuracao.IncrementoComercial);
        Assert.Equal(0m, configuracao.ReservaComercialDesconto);
        var consulta = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));
        Assert.Contains("0%", consulta);
    }

    [Fact]
    public async Task MEL022_Percentual_negativo_e_rejeitado_sem_mutar_configuracao()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa percentual negativo");
        await AtualizarConfiguracaoAsync(empresa, 0.10m, 1m, 0.10m, 0.50m, 0.10m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "-1", "1", "10", "0,5", "10");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("O percentual de mão de obra não pode ser negativo.", conteudo);
        Assert.Equal(0.10m, (await ObterConfiguracaoAsync(empresa)).PercentualMaoDeObra);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    public async Task UC027_W7_Incremento_zero_ou_negativo_e_rejeitado(string incremento)
    {
        var empresa = await web.CriarEmpresaAsync($"Empresa incremento invalido {Guid.NewGuid():N}");
        await AtualizarConfiguracaoAsync(empresa, 0.01m, 1m, 0.10m, 0.50m, 0.10m);
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
        await AtualizarConfiguracaoAsync(empresa, 0.01m, 1m, 0.10m, 0.50m, 0.10m);
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
        await AtualizarConfiguracaoAsync(empresa, 0.01m, 1m, 0.10m, 0.50m, 0.10m);
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
    public async Task MEL022_Percentual_aceita_virgula_ponto_e_valor_acima_de_cem_porcento()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa virgula");
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "150,5", "0,75", "10,5", "0,25", "7,5");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(1.505m, configuracao.PercentualMaoDeObra);
        Assert.Equal(0.75m, configuracao.TarifaEnergiaKwh);
        Assert.Equal(0.105m, configuracao.MargemPadrao);
        Assert.Equal(0.25m, configuracao.IncrementoComercial);
        Assert.Equal(0.075m, configuracao.ReservaComercialDesconto);
    }

    [Fact]
    public async Task MEL015_C3_Round_trip_do_incremento_comercial_preserva_alta_precisao()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa round trip incremento");
        await AtualizarConfiguracaoAsync(empresa, 0.1234m, 0.98m, 0.075m, 0.500001m, 0.125m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var get = await client.GetAsync("/Configuracoes/Precificacao/Editar");
        var html = await WebTestHtml.LerHtmlDecodificadoAsync(get);

        get.EnsureSuccessStatusCode();
        var percentualMaoDeObra = ValorDoInput(html, "Input.PercentualMaoDeObra");
        var tarifa = ValorDoInput(html, "Input.TarifaEnergiaKwh");
        var margem = ValorDoInput(html, "Input.MargemPadraoPercentual");
        var incremento = ValorDoInput(html, "Input.IncrementoComercial");
        var reserva = ValorDoInput(html, "Input.ReservaComercialDescontoPercentual");
        Assert.Equal("0,500001", incremento);

        var post = await EnviarFormularioEdicaoAsync(client, percentualMaoDeObra, tarifa, margem, incremento, reserva);

        Assert.Equal(HttpStatusCode.Redirect, post.StatusCode);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0.500001m, configuracao.IncrementoComercial);
    }

    [Fact]
    public async Task UC027_W11_Post_invalido_preserva_inputs_e_banco_inalterado()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa invalido preserva");
        await AtualizarConfiguracaoAsync(empresa, 0.01m, 1m, 0.10m, 0.50m, 0.10m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await EnviarFormularioEdicaoAsync(client, "22", "abc", "33", "0,75", "12");
        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(response);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("22", ValorDoInput(conteudo, "Input.PercentualMaoDeObra"));
        Assert.Equal("abc", ValorDoInput(conteudo, "Input.TarifaEnergiaKwh"));
        Assert.Equal("33", ValorDoInput(conteudo, "Input.MargemPadraoPercentual"));
        Assert.Equal("0,75", ValorDoInput(conteudo, "Input.IncrementoComercial"));
        Assert.Equal("12", ValorDoInput(conteudo, "Input.ReservaComercialDescontoPercentual"));
        AssertAjudasPresentes(conteudo);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0.01m, configuracao.PercentualMaoDeObra);
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
        await AtualizarConfiguracaoAsync(empresaDois, 0.77m, 7m, 0.70m, 7m, 0.17m);
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
        Assert.Equal(0.11m, configuracaoUm.PercentualMaoDeObra);
        Assert.Equal(0.77m, configuracaoDois.PercentualMaoDeObra);
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

        Assert.Equal(0.10m, (await ObterConfiguracaoAsync(empresaUm)).PercentualMaoDeObra);
        Assert.Equal(0.20m, (await ObterConfiguracaoAsync(empresaDois)).PercentualMaoDeObra);
    }

    [Fact]
    public async Task UC027_W15_Post_sem_antiforgery_retorna_400_sem_mutacao()
    {
        var empresa = await web.CriarEmpresaAsync("Empresa sem antiforgery");
        await AtualizarConfiguracaoAsync(empresa, 0.01m, 1m, 0.10m, 0.50m, 0.10m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var response = await client.PostAsync("/Configuracoes/Precificacao/Editar", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.PercentualMaoDeObra"] = "99",
            ["Input.TarifaEnergiaKwh"] = "9",
            ["Input.MargemPadraoPercentual"] = "90",
            ["Input.IncrementoComercial"] = "9",
            ["Input.ReservaComercialDescontoPercentual"] = "9"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var configuracao = await ObterConfiguracaoAsync(empresa);
        Assert.Equal(0.01m, configuracao.PercentualMaoDeObra);
        Assert.Equal(0.10m, configuracao.MargemPadrao);
    }

    [Fact]
    public async Task W5_Troca_de_empresa_ativa_muda_configuracao_visivel()
    {
        var empresaUm = await web.CriarEmpresaAsync("Empresa config um");
        var empresaDois = await web.CriarEmpresaAsync("Empresa config dois");
        await AtualizarConfiguracaoAsync(empresaUm, 0.1234m, null, 0.075m, 0.50m, 0.10m);
        await AtualizarConfiguracaoAsync(empresaDois, 0.9876m, null, 0.125m, 1.25m, 0.20m);
        var usuario = await web.CriarUsuarioAsync(empresaUm, empresaDois);
        using var client = web.CriarCliente();
        var login = await web.LoginAsync(client, usuario.Email, usuario.Senha);
        Assert.Equal(HttpStatusCode.Redirect, login.StatusCode);

        await SelecionarEmpresaAsync(client, empresaUm);
        var empresaUmHtml = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));
        await SelecionarEmpresaAsync(client, empresaDois);
        var empresaDoisHtml = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));

        Assert.Contains("12,34%", empresaUmHtml);
        Assert.Contains("7,5%", empresaUmHtml);
        Assert.DoesNotContain("98,76%", empresaUmHtml);
        Assert.Contains("98,76%", empresaDoisHtml);
        Assert.Contains("12,5%", empresaDoisHtml);
        Assert.DoesNotContain("12,34%", empresaDoisHtml);
    }

    [Fact]
    public async Task W6_Empresa_ativa_nao_exibe_valores_preparados_para_outra_empresa()
    {
        var empresaDois = await web.CriarEmpresaAsync("Empresa config isolada");
        await AtualizarConfiguracaoAsync(empresaDois, 0.7777m, 66.66m, 0.333m, 5.55m, 0.44m);
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
        await AtualizarConfiguracaoAsync(empresa, 0.123456m, 0.987654m, 0.075m, 0.500001m, 0.125m);
        using var client = await web.CriarClienteAutenticadoAsync(empresa);

        var conteudo = await WebTestHtml.LerHtmlDecodificadoAsync(await client.GetAsync("/Configuracoes/Precificacao"));

        Assert.Contains("12,3456%", conteudo);
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
        string percentualMaoDeObra,
        string tarifaEnergiaKwh,
        string margemPadraoPercentual,
        string incrementoComercial,
        string reservaComercialDescontoPercentual)
    {
        var response = await EnviarFormularioEdicaoAsync(
            client,
            percentualMaoDeObra,
            tarifaEnergiaKwh,
            margemPadraoPercentual,
            incrementoComercial,
            reservaComercialDescontoPercentual);
        return response.StatusCode;
    }

    private static async Task<HttpResponseMessage> EnviarFormularioEdicaoAsync(
        HttpClient client,
        string percentualMaoDeObra,
        string tarifaEnergiaKwh,
        string margemPadraoPercentual,
        string incrementoComercial,
        string reservaComercialDescontoPercentual,
        Dictionary<string, string>? camposExtras = null,
        string caminhoEdicao = "/Configuracoes/Precificacao/Editar")
    {
        var token = await WebTestHtml.ObterTokenAntiforgeryAsync(client, caminhoEdicao);
        var dados = new Dictionary<string, string>
        {
            ["__RequestVerificationToken"] = token,
            ["Input.PercentualMaoDeObra"] = percentualMaoDeObra,
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

        return await client.PostAsync(caminhoEdicao, new FormUrlEncodedContent(dados));
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

    private static void AssertAjudasPresentes(string html)
    {
        Assert.Contains("custo base dos insumos", html);
        Assert.Contains("Valor pago por 1 kWh de energia.", html);
        Assert.Contains("somente para pré-preencher a Margem-alvo ao cadastrar um novo Produto", html);
        Assert.Contains("Produtos já cadastrados não são modificados", html);
        Assert.Contains("múltiplo monetário usado para arredondar o Preço teórico para cima", html);
        Assert.Contains("R$ 0,50", html);
        Assert.Contains("R$ 12,13", html);
        Assert.Contains("R$ 12,50", html);
        Assert.Contains("Não é desconto automático", html);
        Assert.Contains("reserva de 10%", html);
        Assert.Contains("pelo menos 11% acima do sugerido", html);
        Assert.Contains("não reinterpretam o histórico existente", html);
    }

    private static void AssertInputDescreveAjuda(string html, string nome, string idAjuda)
    {
        var match = Regex.Match(html, "<input[^>]+name=\"" + Regex.Escape(nome) + "\"[^>]*>");

        Assert.True(match.Success, $"Input '{nome}' não encontrado.");
        Assert.Contains("aria-describedby=\"" + idAjuda + "\"", match.Value);
        Assert.Contains("id=\"" + idAjuda + "\"", html);
    }

    private async Task AtualizarConfiguracaoAsync(
        int empresaId,
        decimal percentualMaoDeObra,
        decimal? tarifaEnergiaKwh,
        decimal? margemPadrao,
        decimal? incrementoComercial,
        decimal reservaComercialDesconto)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.SingleAsync(c => c.EmpresaId == empresaId);
        configuracao.Atualizar(percentualMaoDeObra, tarifaEnergiaKwh, margemPadrao, incrementoComercial, reservaComercialDesconto);
        await context.SaveChangesAsync();
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

    private async Task<int> CriarProdutoAsync(int empresaId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var produto = Produto.Criar(empresaId, $"Produto {Guid.NewGuid():N}", 0.30m);
        context.Produtos.Add(produto);
        await context.SaveChangesAsync();
        return produto.Id;
    }

    private async Task<int> CriarFichaAsync(int empresaId, int produtoId)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        var ficha = FichaTecnica.Criar(empresaId, produtoId, 1m);
        context.FichasTecnicas.Add(ficha);
        await context.SaveChangesAsync();
        return ficha.Id;
    }

    private async Task CriarUsoAsync(int empresaId, int fichaId, string nome, decimal potenciaKw, int tempoUsoMinutos)
    {
        using var scope = factory.Services.CreateScope();
        var options = scope.ServiceProvider.GetRequiredService<DbContextOptions<PrecificadorDbContext>>();
        await using var context = new PrecificadorDbContext(options, new ContextoEmpresaTeste(empresaId));
        context.UsosEquipamentosFicha.Add(UsoEquipamentoFicha.Criar(empresaId, fichaId, nome, potenciaKw, tempoUsoMinutos));
        await context.SaveChangesAsync();
    }
}
