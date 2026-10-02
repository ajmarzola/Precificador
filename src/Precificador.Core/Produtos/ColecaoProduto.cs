using System.Text.RegularExpressions;
using Precificador.Core.Empresas;

namespace Precificador.Core.Produtos;

public sealed class ColecaoProduto : IEntidadeEmpresa
{
    private ColecaoProduto() { Nome = null!; NomeNormalizado = null!; }

    public int Id { get; private set; }
    public int EmpresaId { get; private set; }
    public string Nome { get; private set; }
    public string NomeNormalizado { get; private set; }
    public DateOnly DataLancamento { get; private set; }
    public DateOnly? DataFinalizacao { get; private set; }

    public static ColecaoProduto Criar(int empresaId, string nome, DateOnly dataLancamento, DateOnly? dataFinalizacao)
    {
        var colecao = new ColecaoProduto();
        colecao.DefinirEmpresa(empresaId);
        colecao.AtualizarDados(nome, dataLancamento, dataFinalizacao);
        return colecao;
    }

    public void AtualizarDados(string nome, DateOnly dataLancamento, DateOnly? dataFinalizacao)
    {
        var normalizado = Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");
        if (normalizado.Length == 0) throw new ArgumentException("O nome da coleção é obrigatório.", nameof(nome));
        if (normalizado.Length > 120) throw new ArgumentException("O nome da coleção deve possuir no máximo 120 caracteres.", nameof(nome));
        if (dataLancamento == default) throw new ArgumentException("A data de lançamento é obrigatória.", nameof(dataLancamento));
        if (dataFinalizacao < dataLancamento) throw new ArgumentException("A data de finalização não pode anteceder o lançamento.", nameof(dataFinalizacao));
        Nome = normalizado;
        NomeNormalizado = normalizado.ToUpperInvariant();
        DataLancamento = dataLancamento;
        DataFinalizacao = dataFinalizacao;
    }

    public SituacaoColecaoProduto ObterSituacao(DateOnly hoje) =>
        hoje < DataLancamento ? SituacaoColecaoProduto.Planejada :
        DataFinalizacao is DateOnly fim && hoje > fim ? SituacaoColecaoProduto.Finalizada :
        SituacaoColecaoProduto.EmAndamento;

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (EmpresaId != 0 && EmpresaId != empresaId) throw new InvalidOperationException("A coleção já pertence a outra empresa.");
        EmpresaId = empresaId;
    }
}

public enum SituacaoColecaoProduto { Planejada = 1, EmAndamento = 2, Finalizada = 3 }
