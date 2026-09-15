namespace Precificador.Core.Empresas;

public sealed class ConfiguracaoPrecificacaoEmpresa : IEntidadeEmpresa
{
    public const decimal ReservaComercialDescontoPadrao = 0.10m;

    private ConfiguracaoPrecificacaoEmpresa()
    {
    }

    private ConfiguracaoPrecificacaoEmpresa(int empresaId)
    {
        DefinirEmpresa(empresaId);
        ReservaComercialDesconto = ReservaComercialDescontoPadrao;
        Validar();
    }

    public int EmpresaId { get; private set; }

    public decimal? ValorHoraTrabalho { get; private set; }

    public decimal? TarifaEnergiaKwh { get; private set; }

    public decimal? MargemPadrao { get; private set; }

    public decimal? IncrementoComercial { get; private set; }

    public decimal ReservaComercialDesconto { get; private set; }

    public static ConfiguracaoPrecificacaoEmpresa CriarPadrao(int empresaId) => new(empresaId);

    public void Atualizar(
        decimal? valorHoraTrabalho,
        decimal? tarifaEnergiaKwh,
        decimal? margemPadrao,
        decimal? incrementoComercial,
        decimal reservaComercialDesconto)
    {
        ValidarValorNaoNegativo(valorHoraTrabalho, nameof(ValorHoraTrabalho));
        ValidarValorNaoNegativo(tarifaEnergiaKwh, nameof(TarifaEnergiaKwh));
        ValidarFracaoMenorQueUm(margemPadrao, nameof(MargemPadrao));
        ValidarIncremento(incrementoComercial);
        ValidarReservaComercialDesconto(reservaComercialDesconto);

        ValorHoraTrabalho = valorHoraTrabalho;
        TarifaEnergiaKwh = tarifaEnergiaKwh;
        MargemPadrao = margemPadrao;
        IncrementoComercial = incrementoComercial;
        ReservaComercialDesconto = reservaComercialDesconto;
    }

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (EmpresaId != 0 && EmpresaId != empresaId)
        {
            throw new InvalidOperationException("A configuracao de precificacao ja pertence a outra empresa.");
        }

        EmpresaId = empresaId;
    }

    private void Validar()
    {
        ValidarValorNaoNegativo(ValorHoraTrabalho, nameof(ValorHoraTrabalho));
        ValidarValorNaoNegativo(TarifaEnergiaKwh, nameof(TarifaEnergiaKwh));
        ValidarFracaoMenorQueUm(MargemPadrao, nameof(MargemPadrao));
        ValidarIncremento(IncrementoComercial);
        ValidarReservaComercialDesconto(ReservaComercialDesconto);
    }

    private static void ValidarValorNaoNegativo(decimal? valor, string nomeParametro)
    {
        if (valor < 0)
        {
            var mensagem = nomeParametro == nameof(ValorHoraTrabalho)
                ? "O valor da hora de trabalho não pode ser negativo."
                : "A tarifa de energia não pode ser negativa.";
            throw new ArgumentOutOfRangeException(nomeParametro, mensagem);
        }
    }

    private static void ValidarFracaoMenorQueUm(decimal? valor, string nomeParametro)
    {
        if (valor < 0 || valor >= 1)
        {
            throw new ArgumentOutOfRangeException(
                nomeParametro,
                "A margem padrão deve ser maior ou igual a 0% e menor que 100%.");
        }
    }

    private static void ValidarIncremento(decimal? valor)
    {
        if (valor <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(IncrementoComercial),
                "O incremento comercial deve ser maior que zero.");
        }
    }

    private static void ValidarReservaComercialDesconto(decimal valor)
    {
        if (valor < 0 || valor >= 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(ReservaComercialDesconto),
                "A reserva comercial para desconto deve ser maior ou igual a 0% e menor que 100%.");
        }
    }
}
