using Precificador.Core.Empresas;

namespace Precificador.Core.FichasTecnicas;

public sealed class ItemFichaTecnica : IEntidadeEmpresa
{
    private const int TamanhoMaximoObservacao = 1000;

    private ItemFichaTecnica()
    {
    }

    private ItemFichaTecnica(int empresaId, int fichaTecnicaId, int insumoId, decimal quantidade, string? observacao)
    {
        ValidarIds(empresaId, fichaTecnicaId, insumoId);
        ValidarQuantidade(quantidade);

        var observacaoNormalizada = NormalizarObservacao(observacao);
        ValidarObservacao(observacaoNormalizada);

        EmpresaId = empresaId;
        FichaTecnicaId = fichaTecnicaId;
        InsumoId = insumoId;
        Quantidade = quantidade;
        Observacao = observacaoNormalizada;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public int FichaTecnicaId { get; private set; }

    public int InsumoId { get; private set; }

    public decimal Quantidade { get; private set; }

    public string? Observacao { get; private set; }

    public static ItemFichaTecnica Criar(
        int empresaId,
        int fichaTecnicaId,
        int insumoId,
        decimal quantidade,
        string? observacao = null) =>
        new(empresaId, fichaTecnicaId, insumoId, quantidade, observacao);

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (EmpresaId != 0 && EmpresaId != empresaId)
        {
            throw new InvalidOperationException("O item da ficha técnica já pertence a outra empresa.");
        }

        EmpresaId = empresaId;
    }

    private static void ValidarIds(int empresaId, int fichaTecnicaId, int insumoId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (fichaTecnicaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fichaTecnicaId));
        }

        if (insumoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(insumoId));
        }
    }

    private static void ValidarQuantidade(decimal quantidade)
    {
        if (quantidade <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantidade), "A quantidade deve ser maior que zero.");
        }
    }

    private static string? NormalizarObservacao(string? observacao)
    {
        var observacaoNormalizada = observacao?.Trim();
        return string.IsNullOrEmpty(observacaoNormalizada) ? null : observacaoNormalizada;
    }

    private static void ValidarObservacao(string? observacao)
    {
        if (observacao?.Length > TamanhoMaximoObservacao)
        {
            throw new ArgumentException("A observação deve possuir no máximo 1000 caracteres.", nameof(observacao));
        }
    }
}
