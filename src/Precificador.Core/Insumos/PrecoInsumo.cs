namespace Precificador.Core.Insumos;

public sealed class PrecoInsumo : Precificador.Core.Empresas.IEntidadeEmpresa
{
    private PrecoInsumo()
    {
    }

    private PrecoInsumo(int empresaId, int insumoId, decimal quantidadeCompra, decimal precoCompra, DateOnly dataReferencia)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (insumoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(insumoId));
        }

        if (quantidadeCompra <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidadeCompra), "A quantidade de compra deve ser maior que zero.");
        }

        if (precoCompra <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(precoCompra), "O preço de compra deve ser maior que zero.");
        }

        EmpresaId = empresaId;
        InsumoId = insumoId;
        QuantidadeCompra = quantidadeCompra;
        PrecoCompra = precoCompra;
        DataReferencia = dataReferencia;
    }

    public int Id { get; private set; }
    public int EmpresaId { get; private set; }
    public int InsumoId { get; private set; }
    public decimal QuantidadeCompra { get; private set; }
    public decimal PrecoCompra { get; private set; }
    public DateOnly DataReferencia { get; private set; }
    public decimal CustoUnitario => PrecoCompra / QuantidadeCompra;

    public static PrecoInsumo Criar(int empresaId, int insumoId, decimal quantidadeCompra, decimal precoCompra, DateOnly dataReferencia) =>
        new(empresaId, insumoId, quantidadeCompra, precoCompra, dataReferencia);

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (EmpresaId != 0 && EmpresaId != empresaId)
        {
            throw new InvalidOperationException("O preço do insumo já pertence a outra empresa.");
        }

        EmpresaId = empresaId;
    }
}
