using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Categorias;

public sealed class IndexModel(PrecificadorDbContext context) : PageModel
{
    public IReadOnlyList<CategoriaListagem> Categorias { get; private set; } = [];

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public async Task OnGetAsync()
    {
        Categorias = await context.CategoriasProdutos.AsNoTracking()
            .OrderBy(categoria => categoria.NomeNormalizado)
            .Select(categoria => new CategoriaListagem(categoria.Id, categoria.Nome, categoria.Ativo))
            .ToListAsync();
    }

    public async Task<IActionResult> OnPostDesativarAsync(int id)
    {
        var categoria = await context.CategoriasProdutos.SingleOrDefaultAsync(item => item.Id == id);
        if (categoria is null)
        {
            return NotFound();
        }

        categoria.Desativar();
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Categoria de produto desativada com sucesso.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostReativarAsync(int id)
    {
        var categoria = await context.CategoriasProdutos.SingleOrDefaultAsync(item => item.Id == id);
        if (categoria is null)
        {
            return NotFound();
        }

        categoria.Reativar();
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Categoria de produto reativada com sucesso.";
        return RedirectToPage();
    }

    public sealed record CategoriaListagem(int Id, string Nome, bool Ativo)
    {
        public string Situacao => Ativo ? "Ativo" : "Inativo";
    }
}
