using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Precificacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Precificacao;

namespace Precificador.Web.Pages.Produtos;

public sealed class DetalhesModel(PrecificadorDbContext context, PrecificacaoProdutoAtual precificacaoAtual) : PageModel
{
    public ProdutoDetalhes? Produto { get; private set; }

    public ResultadoPrecificacaoProdutoAtual? Precificacao { get; private set; }

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == id)
            .Select(produto => new ProdutoDetalhes(produto.Id, produto.Nome, produto.Categoria, produto.MargemAlvo, produto.Ativo))
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

    public sealed record ProdutoDetalhes(int Id, string Nome, string? Categoria, decimal MargemAlvo, bool Ativo);

    public static string SituacaoMargemRotulo(SituacaoMargemProduto situacao) =>
        situacao switch
        {
            SituacaoMargemProduto.AbaixoDaMargem => "Abaixo da margem",
            SituacaoMargemProduto.DentroDaMargem => "Dentro da margem",
            _ => "Incompleto"
        };
}
