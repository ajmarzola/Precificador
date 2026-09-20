using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Precificador.Core.Empresas;
using Precificador.Web.Apresentacao;

namespace Precificador.Web.Pages.Configuracoes.Precificacao;

public static class ConfiguracaoPrecificacaoFormulario
{
    private static readonly CultureInfo CulturaBrasileira = CultureInfo.GetCultureInfo("pt-BR");

    public static bool TentarObterValores(
        ModelStateDictionary modelState,
        ConfiguracaoPrecificacaoInputModel input,
        out ValoresConfiguracaoPrecificacao valores)
    {
        valores = default;
        var valido = true;

        valido &= TentarObterPercentualObrigatorio(modelState, "Input.PercentualMaoDeObra", input.PercentualMaoDeObra, "O percentual de mão de obra é obrigatório.", "O percentual de mão de obra deve ser um número válido.", out var percentualMaoDeObra);
        valido &= TentarObterOpcional(modelState, "Input.TarifaEnergiaKwh", input.TarifaEnergiaKwh, "A tarifa de energia deve ser um número válido.", out var tarifaEnergiaKwh);
        valido &= TentarObterPercentualOpcional(modelState, "Input.MargemPadraoPercentual", input.MargemPadraoPercentual, "A margem padrão deve ser um número válido.", out var margemPadrao);
        valido &= TentarObterOpcional(modelState, "Input.IncrementoComercial", input.IncrementoComercial, "O incremento comercial deve ser um número válido.", out var incrementoComercial);
        valido &= TentarObterPercentualObrigatorio(
            modelState,
            "Input.ReservaComercialDescontoPercentual",
            input.ReservaComercialDescontoPercentual,
            "A reserva comercial para desconto é obrigatória.",
            "A reserva comercial para desconto deve ser um número válido.",
            out var reservaComercialDesconto);

        if (!valido)
        {
            return false;
        }

        valores = new ValoresConfiguracaoPrecificacao(
            percentualMaoDeObra,
            tarifaEnergiaKwh,
            margemPadrao,
            incrementoComercial,
            reservaComercialDesconto);
        return true;
    }

    public static ConfiguracaoPrecificacaoInputModel CriarInput(ConfiguracaoPrecificacaoEmpresa configuracao) => new()
    {
        PercentualMaoDeObra = FormatarPercentual(configuracao.PercentualMaoDeObra),
        TarifaEnergiaKwh = FormatarDecimal(configuracao.TarifaEnergiaKwh),
        MargemPadraoPercentual = FormatarPercentual(configuracao.MargemPadrao),
        IncrementoComercial = FormatarDecimal(configuracao.IncrementoComercial),
        ReservaComercialDescontoPercentual = FormatarPercentual(configuracao.ReservaComercialDesconto)
    };

    public static void AdicionarErroDominio(ModelStateDictionary modelState, ArgumentException exception) =>
        modelState.AddModelError(CampoPara(exception.ParamName), MensagemPara(exception.ParamName));

    private static bool TentarObterPercentualOpcional(
        ModelStateDictionary modelState,
        string campo,
        string? texto,
        string mensagemInvalida,
        out decimal? fracao)
    {
        if (!TentarObterOpcional(modelState, campo, texto, mensagemInvalida, out var percentual))
        {
            fracao = null;
            return false;
        }

        fracao = percentual / 100m;
        return true;
    }

    private static bool TentarObterPercentualObrigatorio(
        ModelStateDictionary modelState,
        string campo,
        string? texto,
        string mensagemObrigatoria,
        string mensagemInvalida,
        out decimal fracao)
    {
        fracao = 0;
        var valorInformado = texto?.Trim();
        if (string.IsNullOrEmpty(valorInformado))
        {
            modelState.AddModelError(campo, mensagemObrigatoria);
            return false;
        }

        if (!TentarParseDecimal(valorInformado, out var percentual))
        {
            modelState.AddModelError(campo, mensagemInvalida);
            return false;
        }

        fracao = percentual / 100m;
        return true;
    }

    private static bool TentarObterOpcional(
        ModelStateDictionary modelState,
        string campo,
        string? texto,
        string mensagemInvalida,
        out decimal? valor)
    {
        valor = null;
        var valorInformado = texto?.Trim();
        if (string.IsNullOrEmpty(valorInformado))
        {
            return true;
        }

        if (TentarParseDecimal(valorInformado, out var decimalInformado))
        {
            valor = decimalInformado;
            return true;
        }

        modelState.AddModelError(campo, mensagemInvalida);
        return false;
    }

    private static bool TentarParseDecimal(string texto, out decimal valor) =>
        DecimalInputParser.TentarParse(texto, out valor);

    private static string FormatarDecimal(decimal? valor) =>
        valor?.ToString("0.######", CulturaBrasileira) ?? string.Empty;

    private static string FormatarPercentual(decimal? valor) =>
        valor.HasValue ? FormatarPercentual(valor.Value) : string.Empty;

    private static string FormatarPercentual(decimal valor) =>
        (valor * 100m).ToString("0.######", CulturaBrasileira);

    private static string CampoPara(string? nomeParametro) => nomeParametro switch
    {
        nameof(ConfiguracaoPrecificacaoEmpresa.PercentualMaoDeObra) => "Input.PercentualMaoDeObra",
        nameof(ConfiguracaoPrecificacaoEmpresa.TarifaEnergiaKwh) => "Input.TarifaEnergiaKwh",
        nameof(ConfiguracaoPrecificacaoEmpresa.MargemPadrao) => "Input.MargemPadraoPercentual",
        nameof(ConfiguracaoPrecificacaoEmpresa.IncrementoComercial) => "Input.IncrementoComercial",
        nameof(ConfiguracaoPrecificacaoEmpresa.ReservaComercialDesconto) => "Input.ReservaComercialDescontoPercentual",
        _ => string.Empty
    };

    private static string MensagemPara(string? nomeParametro) => nomeParametro switch
    {
        nameof(ConfiguracaoPrecificacaoEmpresa.PercentualMaoDeObra) => "O percentual de mão de obra não pode ser negativo.",
        nameof(ConfiguracaoPrecificacaoEmpresa.TarifaEnergiaKwh) => "A tarifa de energia não pode ser negativa.",
        nameof(ConfiguracaoPrecificacaoEmpresa.MargemPadrao) => "A margem padrão deve ser maior ou igual a 0% e menor que 100%.",
        nameof(ConfiguracaoPrecificacaoEmpresa.IncrementoComercial) => "O incremento comercial deve ser maior que zero.",
        nameof(ConfiguracaoPrecificacaoEmpresa.ReservaComercialDesconto) => "A reserva comercial para desconto deve ser maior ou igual a 0% e menor que 100%.",
        _ => "As configurações de precificação informadas são inválidas."
    };

    public readonly record struct ValoresConfiguracaoPrecificacao(
        decimal PercentualMaoDeObra,
        decimal? TarifaEnergiaKwh,
        decimal? MargemPadrao,
        decimal? IncrementoComercial,
        decimal ReservaComercialDesconto);
}
