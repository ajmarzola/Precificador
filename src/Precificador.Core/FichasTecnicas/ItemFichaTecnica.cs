using Precificador.Core.Empresas;

namespace Precificador.Core.FichasTecnicas;

public sealed class ItemFichaTecnica : IEntidadeEmpresa
{
    private const int TamanhoMaximoObservacao = 1000;

    private ItemFichaTecnica()
    {
    }

    private ItemFichaTecnica(int empresaId, int fichaTecnicaId, int insumoId, decimal quantidade, string? observacao, decimal percentualPerda)
    {
        ValidarIds(empresaId, fichaTecnicaId, insumoId);
        ValidarQuantidade(quantidade);

        var observacaoNormalizada = NormalizarObservacao(observacao);
        ValidarObservacao(observacaoNormalizada);
        ValidarPercentualPerda(percentualPerda);

        EmpresaId = empresaId;
        FichaTecnicaId = fichaTecnicaId;
        InsumoId = insumoId;
        Quantidade = quantidade;
        Observacao = observacaoNormalizada;
        PercentualPerda = percentualPerda;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public int FichaTecnicaId { get; private set; }

    public int InsumoId { get; private set; }

    public decimal Quantidade { get; private set; }

    public string? Observacao { get; private set; }

    public decimal PercentualPerda { get; private set; }

    public static ItemFichaTecnica Criar(
        int empresaId,
        int fichaTecnicaId,
        int insumoId,
        decimal quantidade,
        string? observacao = null,
        decimal percentualPerda = 0m) =>
        new(empresaId, fichaTecnicaId, insumoId, quantidade, observacao, percentualPerda);

    public void AtualizarDados(decimal quantidade, string? observacao, decimal percentualPerda)
    {
        var observacaoNormalizada = NormalizarObservacao(observacao);
        ValidarQuantidade(quantidade);
        ValidarObservacao(observacaoNormalizada);
        ValidarPercentualPerda(percentualPerda);

        Quantidade = quantidade;
        Observacao = observacaoNormalizada;
        PercentualPerda = percentualPerda;
    }

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

    private static void ValidarPercentualPerda(decimal percentualPerda)
    {
        if (percentualPerda < 0 || percentualPerda >= 1)
        {
            throw new ArgumentOutOfRangeException(nameof(percentualPerda), "O percentual de perda deve ser maior ou igual a zero e menor que um.");
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
