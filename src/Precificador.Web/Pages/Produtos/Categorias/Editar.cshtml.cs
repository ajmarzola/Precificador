using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Categorias;

public sealed class EditarModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty]
    public CategoriaProdutoInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var categoria = await context.CategoriasProdutos.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new CategoriaProdutoInputModel { Nome = item.Nome })
            .SingleOrDefaultAsync();

        if (categoria is null)
        {
            return NotFound();
        }

        Input = categoria;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var categoria = await context.CategoriasProdutos.SingleOrDefaultAsync(item => item.Id == id);
        if (categoria is null)
        {
            return NotFound();
        }

        try
        {
            categoria.Renomear(Input.Nome!);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError("Input.Nome", exception.Message);
            return Page();
        }

        if (await context.CategoriasProdutos.AnyAsync(item => item.Id != id && item.NomeNormalizado == categoria.NomeNormalizado))
        {
            ModelState.AddModelError("Input.Nome", CategoriaProdutoFormulario.MensagemDuplicidade);
            return Page();
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Categoria de produto atualizada com sucesso.";
        return RedirectToPage("/Produtos/Categorias/Index");
    }
}
