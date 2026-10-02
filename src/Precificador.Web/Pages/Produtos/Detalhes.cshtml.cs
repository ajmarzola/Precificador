using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Precificacao;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Precificacao;

namespace Precificador.Web.Pages.Produtos;

public sealed class DetalhesModel(PrecificadorDbContext context, PrecificacaoProdutoAtual precificacaoAtual, IDataOperacionalEmpresa dataOperacional) : PageModel
{
    public IReadOnlyList<ColecaoDetalhes> Colecoes { get; private set; } = [];
    public ProdutoDetalhes? Produto { get; private set; }

    public ResultadoPrecificacaoProdutoAtual? Precificacao { get; private set; }

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == id)
            .Select(produto => new ProdutoDetalhes(
                produto.Id,
                produto.Nome,
                produto.CategoriaProdutoId == null
                    ? null
                    : context.CategoriasProdutos.Where(categoria => categoria.Id == produto.CategoriaProdutoId).Select(categoria => categoria.Nome).FirstOrDefault(),
                produto.MargemAlvo,
                produto.Ativo))
            .SingleOrDefaultAsync();

        if (Produto is null)
        {
            return NotFound();
        }

        Precificacao = await precificacaoAtual.CalcularAsync(id);
        if (Precificacao is null)
        {
            return NotFound();
        }

        var vinculos = await context.ProdutosColecoes.AsNoTracking().Where(x => x.ProdutoId == id)
            .Include(x => x.ColecaoProduto).OrderByDescending(x => x.ColecaoProduto.DataLancamento)
            .ThenBy(x => x.ColecaoProduto.NomeNormalizado).ToListAsync();
        Colecoes = vinculos.Select(x => new ColecaoDetalhes(x.ColecaoProduto, x.Destaque,
            x.ColecaoProduto.ObterSituacao(dataOperacional.Hoje))).ToArray();
        return Page();
    }

    public async Task<IActionResult> OnPostDesativarAsync(int id)
    {
        var produto = await context.Produtos.SingleOrDefaultAsync(item => item.Id == id);
        if (produto is null)
        {
            return NotFound();
        }

        produto.Desativar();
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Produto desativado com sucesso.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostReativarAsync(int id)
    {
        var produto = await context.Produtos.SingleOrDefaultAsync(item => item.Id == id);
        if (produto is null)
        {
            return NotFound();
        }

        produto.Reativar();
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Produto reativado com sucesso.";
        return RedirectToPage(new { id });
    }

    public sealed record ColecaoDetalhes(ColecaoProduto Colecao, bool Destaque, SituacaoColecaoProduto Situacao)
    {
        public string SituacaoTexto => Situacao switch
        { SituacaoColecaoProduto.Planejada => "Planejada", SituacaoColecaoProduto.EmAndamento => "Em andamento", _ => "Finalizada" };
    }

    public sealed record ProdutoDetalhes(int Id, string Nome, string? Categoria, decimal MargemAlvo, bool Ativo);

    public static string SituacaoMargemRotulo(SituacaoMargemProduto situacao) =>
        situacao switch
        {
            SituacaoMargemProduto.AbaixoDaMargem => "Abaixo da margem",
            SituacaoMargemProduto.DentroDaMargem => "Dentro da margem",
            _ => "Incompleto"
        };
}
