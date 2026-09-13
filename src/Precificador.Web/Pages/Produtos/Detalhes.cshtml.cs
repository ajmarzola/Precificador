using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos;

public sealed class DetalhesModel(PrecificadorDbContext context) : PageModel
{
    public ProdutoDetalhes? Produto { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == id)
            .Select(produto => new ProdutoDetalhes(produto.Nome, produto.Categoria, produto.MargemAlvo, produto.Ativo))
            .SingleOrDefaultAsync();

        return Produto is null ? NotFound() : Page();
    }

    public sealed record ProdutoDetalhes(string Nome, string? Categoria, decimal MargemAlvo, bool Ativo);
}
