using Precificador.Core.Empresas;

namespace Precificador.Core.Produtos;

public sealed class RegistroPrecoProduto : IEntidadeEmpresa
{
    private RegistroPrecoProduto() { }

    private RegistroPrecoProduto(int empresaId, int produtoId, DateOnly dataReferencia, decimal custoReferencia, decimal margemReferencia, decimal precoSugerido, decimal precoPrateleira, decimal reservaComercialReferencia)
    {
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (produtoId <= 0) throw new ArgumentOutOfRangeException(nameof(produtoId));
        if (dataReferencia == default) throw new ArgumentOutOfRangeException(nameof(dataReferencia));
        if (custoReferencia < 0) throw new ArgumentOutOfRangeException(nameof(custoReferencia));
        if (margemReferencia < 0 || margemReferencia >= 1) throw new ArgumentOutOfRangeException(nameof(margemReferencia));
        if (precoSugerido < 0) throw new ArgumentOutOfRangeException(nameof(precoSugerido));
        if (precoPrateleira <= 0) throw new ArgumentOutOfRangeException(nameof(precoPrateleira));
        if (reservaComercialReferencia < 0 || reservaComercialReferencia >= 1) throw new ArgumentOutOfRangeException(nameof(reservaComercialReferencia));

        EmpresaId = empresaId;
        ProdutoId = produtoId;
        DataReferencia = dataReferencia;
        CustoReferencia = custoReferencia;
        MargemReferencia = margemReferencia;
        PrecoSugerido = precoSugerido;
        PrecoPrateleira = precoPrateleira;
        ReservaComercialReferencia = reservaComercialReferencia;
    }

    public int Id { get; private set; }
    public int EmpresaId { get; private set; }
    public int ProdutoId { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public decimal CustoReferencia { get; private set; }
    public decimal MargemReferencia { get; private set; }
    public decimal PrecoSugerido { get; private set; }
    public decimal PrecoPrateleira { get; private set; }
    public decimal ReservaComercialReferencia { get; private set; }

    public static RegistroPrecoProduto Criar(int empresaId, int produtoId, DateOnly dataReferencia, decimal custoReferencia, decimal margemReferencia, decimal precoSugerido, decimal precoPrateleira, decimal reservaComercialReferencia) =>
        new(empresaId, produtoId, dataReferencia, custoReferencia, margemReferencia, precoSugerido, precoPrateleira, reservaComercialReferencia);

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (EmpresaId != 0 && EmpresaId != empresaId) throw new InvalidOperationException("O registro de preco ja pertence a outra empresa.");
        EmpresaId = empresaId;
    }
}
