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

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var produtos = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Ativo)
            .OrderBy(produto => produto.NomeNormalizado)
            .Select(produto => new ProdutoDashboard(produto.Id, produto.Nome, produto.MargemAlvo, null, null, null, null, SituacaoMargemProduto.Incompleto))
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
                SituacaoMargem = resumo.SituacaoMargem
            }
            : produto).ToList();

        ProdutosAtivos = Produtos.Count;
        AbaixoDaMargem = Produtos.Count(produto => produto.SituacaoMargem == SituacaoMargemProduto.AbaixoDaMargem);
        DentroDaMargem = Produtos.Count(produto => produto.SituacaoMargem == SituacaoMargemProduto.DentroDaMargem);
        MargemIndisponivel = Produtos.Count(produto => produto.SituacaoMargem == SituacaoMargemProduto.Incompleto);
    }

    public static string SituacaoMargemRotulo(SituacaoMargemProduto situacao) => situacao switch
    {
        SituacaoMargemProduto.AbaixoDaMargem => "Abaixo da margem",
        SituacaoMargemProduto.DentroDaMargem => "Dentro da margem",
        _ => "Margem indisponível"
    };

    public sealed record ProdutoDashboard(int Id, string Nome, decimal MargemAlvo, decimal? CustoUnitarioProduto,
        decimal? PrecoPrateleiraAtual, decimal? PrecoSugerido, decimal? MargemAtual, SituacaoMargemProduto SituacaoMargem);
}
