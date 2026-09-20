namespace Precificador.Core.Empresas;

public sealed class ConfiguracaoPrecificacaoEmpresa : IEntidadeEmpresa
{
    public const decimal PercentualMaoDeObraPadrao = 0.10m;
    public const decimal ReservaComercialDescontoPadrao = 0.10m;

    private ConfiguracaoPrecificacaoEmpresa()
    {
    }

    private ConfiguracaoPrecificacaoEmpresa(int empresaId)
    {
        DefinirEmpresa(empresaId);
        PercentualMaoDeObra = PercentualMaoDeObraPadrao;
        ReservaComercialDesconto = ReservaComercialDescontoPadrao;
        Validar();
    }

    public int EmpresaId { get; private set; }

    public decimal PercentualMaoDeObra { get; private set; }

    public decimal? TarifaEnergiaKwh { get; private set; }

    public decimal? MargemPadrao { get; private set; }

    public decimal? IncrementoComercial { get; private set; }

    public decimal ReservaComercialDesconto { get; private set; }

    public static ConfiguracaoPrecificacaoEmpresa CriarPadrao(int empresaId) => new(empresaId);

    public void Atualizar(
        decimal percentualMaoDeObra,
        decimal? tarifaEnergiaKwh,
        decimal? margemPadrao,
        decimal? incrementoComercial,
        decimal reservaComercialDesconto)
    {
        ValidarPercentualMaoDeObra(percentualMaoDeObra);
        ValidarValorNaoNegativo(tarifaEnergiaKwh, nameof(TarifaEnergiaKwh));
        ValidarFracaoMenorQueUm(margemPadrao, nameof(MargemPadrao));
        ValidarIncremento(incrementoComercial);
        ValidarReservaComercialDesconto(reservaComercialDesconto);

        PercentualMaoDeObra = percentualMaoDeObra;
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
        ValidarPercentualMaoDeObra(PercentualMaoDeObra);
        ValidarValorNaoNegativo(TarifaEnergiaKwh, nameof(TarifaEnergiaKwh));
        ValidarFracaoMenorQueUm(MargemPadrao, nameof(MargemPadrao));
        ValidarIncremento(IncrementoComercial);
        ValidarReservaComercialDesconto(ReservaComercialDesconto);
    }

    private static void ValidarValorNaoNegativo(decimal? valor, string nomeParametro)
    {
        if (valor < 0)
        {
            throw new ArgumentOutOfRangeException(nomeParametro, "A tarifa de energia não pode ser negativa.");
        }
    }

    private static void ValidarPercentualMaoDeObra(decimal percentualMaoDeObra)
    {
        if (percentualMaoDeObra < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(PercentualMaoDeObra), "O percentual de mão de obra não pode ser negativo.");
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
