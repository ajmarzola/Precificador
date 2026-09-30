using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Precificacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Precificacao;

namespace Precificador.Web.Pages.Dashboard;

public sealed class IndexModel(PrecificadorDbContext context, ResumoPrecificacaoProdutosAtual resumoPrecificacao) : PageModel
{
    public IReadOnlyList<ProdutoDashboard> Produtos { get; private set; } = [];
    public int ProdutosAtivos { get; private set; }
    public int InsumosAtivos { get; private set; }
    public int AbaixoDaMargem { get; private set; }
    public int DentroDaMargem { get; private set; }
    public int MargemIndisponivel { get; private set; }
    public bool FiltroAbaixoDaMargem { get; private set; }
    public bool FiltroPrecificacaoIncompleta { get; private set; }
    public bool FiltroInvalido { get; private set; }

    public async Task OnGetAsync(string? filtro, CancellationToken cancellationToken)
    {
        var filtroNormalizado = NormalizarFiltro(filtro);
        FiltroAbaixoDaMargem = filtroNormalizado == FiltroMargem.AbaixoDaMargem;
        FiltroPrecificacaoIncompleta = filtroNormalizado == FiltroMargem.PrecificacaoIncompleta;
        FiltroInvalido = filtroNormalizado == FiltroMargem.Invalido;
        var produtos = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Ativo)
            .OrderBy(produto => produto.NomeNormalizado)
            .Select(produto => new ProdutoDashboard(produto.Id, produto.Nome, produto.MargemAlvo, null, null, null, null, SituacaoMargemProduto.Incompleto, false, Array.Empty<MotivoPrecificacaoIncompleta>()))
            .ToListAsync(cancellationToken);
        var resumos = await resumoPrecificacao.CalcularAsync(produtos.Select(produto => produto.Id).ToArray(), cancellationToken);
        InsumosAtivos = await context.Insumos.AsNoTracking().CountAsync(insumo => insumo.Ativo, cancellationToken);
        Produtos = produtos.Select(produto => resumos.TryGetValue(produto.Id, out var resumo)
            ? produto with
            {
                CustoUnitarioProduto = resumo.CustoUnitarioProduto,
                PrecoPrateleiraAtual = resumo.PrecoPrateleiraAtual,
                PrecoSugerido = resumo.PrecoSugerido,
                MargemAtual = resumo.MargemAtual,
                SituacaoMargem = resumo.SituacaoMargem,
                PrecificacaoCompleta = resumo.PrecificacaoCompleta,
                MotivosPrecificacaoIncompleta = resumo.MotivosPrecificacaoIncompleta
            }
            : produto).ToList();

        ProdutosAtivos = Produtos.Count;
        AbaixoDaMargem = Produtos.Count(produto => produto.SituacaoMargem == SituacaoMargemProduto.AbaixoDaMargem);
        DentroDaMargem = Produtos.Count(produto => produto.SituacaoMargem == SituacaoMargemProduto.DentroDaMargem);
        MargemIndisponivel = Produtos.Count(produto => produto.SituacaoMargem == SituacaoMargemProduto.Incompleto);
        if (FiltroAbaixoDaMargem)
        {
            Produtos = Produtos.Where(produto => produto.SituacaoMargem == SituacaoMargemProduto.AbaixoDaMargem).ToList();
        }
        else if (FiltroPrecificacaoIncompleta)
        {
            Produtos = Produtos.Where(produto => !produto.PrecificacaoCompleta).ToList();
        }
        else if (FiltroInvalido)
        {
            Produtos = [];
        }
    }

    private static FiltroMargem NormalizarFiltro(string? filtro)
    {
        if (string.IsNullOrWhiteSpace(filtro)) return FiltroMargem.Todos;
        return string.Equals(filtro.Trim(), "abaixo-da-margem", StringComparison.OrdinalIgnoreCase)
            ? FiltroMargem.AbaixoDaMargem
            : string.Equals(filtro.Trim(), "precificacao-incompleta", StringComparison.OrdinalIgnoreCase)
                ? FiltroMargem.PrecificacaoIncompleta
                : FiltroMargem.Invalido;
    }

    public static string SituacaoMargemRotulo(SituacaoMargemProduto situacao) => situacao switch
    {
        SituacaoMargemProduto.AbaixoDaMargem => "Abaixo da margem",
        SituacaoMargemProduto.DentroDaMargem => "Dentro da margem",
        _ => "Margem indisponível"
    };

    public sealed record ProdutoDashboard(int Id, string Nome, decimal MargemAlvo, decimal? CustoUnitarioProduto,
        decimal? PrecoPrateleiraAtual, decimal? PrecoSugerido, decimal? MargemAtual, SituacaoMargemProduto SituacaoMargem,
        bool PrecificacaoCompleta, IReadOnlyList<MotivoPrecificacaoIncompleta> MotivosPrecificacaoIncompleta);

    private enum FiltroMargem { Todos, AbaixoDaMargem, PrecificacaoIncompleta, Invalido }
}
