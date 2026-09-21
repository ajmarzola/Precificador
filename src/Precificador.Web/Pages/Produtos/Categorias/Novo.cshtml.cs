using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Categorias;

public sealed class NovoModel(PrecificadorDbContext context, IEmpresaContext empresaContext) : PageModel
{
    [BindProperty]
    public CategoriaProdutoInputModel Input { get; set; } = new();

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        CategoriaProduto categoria;
        try
        {
            categoria = CategoriaProduto.Criar(empresaContext.EmpresaId!.Value, Input.Nome!);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError("Input.Nome", exception.Message);
            return Page();
        }

        if (await context.CategoriasProdutos.AnyAsync(item => item.NomeNormalizado == categoria.NomeNormalizado))
        {
            ModelState.AddModelError("Input.Nome", CategoriaProdutoFormulario.MensagemDuplicidade);
            return Page();
        }

        context.CategoriasProdutos.Add(categoria);
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Categoria de produto cadastrada com sucesso.";
        return RedirectToPage("/Produtos/Categorias/Index");
    }
}
