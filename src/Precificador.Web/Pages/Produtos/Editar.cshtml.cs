using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos;

[Authorize]
public sealed class EditarModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty]
    public ProdutoInputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var produto = await context.Produtos.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new ProdutoInputModel
            {
                Nome = item.Nome,
                Categoria = item.Categoria,
                MargemAlvoPercentual = ProdutoFormulario.FormatarMargemAlvoPercentual(item.MargemAlvo)
            })
            .SingleOrDefaultAsync();

        if (produto is null)
        {
            return NotFound();
        }

        Input = produto;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var produto = await context.Produtos.SingleOrDefaultAsync(item => item.Id == id);
        if (produto is null)
        {
            return NotFound();
        }

        var margemInformada = ProdutoFormulario.TentarObterMargemAlvo(ModelState, Input, out var margemAlvo);
        if (!margemInformada || !ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            produto.AtualizarDados(Input.Nome!, margemAlvo, Input.Categoria);
        }
        catch (ArgumentException exception)
        {
            ProdutoFormulario.AdicionarErroDominio(ModelState, exception);
            return Page();
        }

        if (await context.Produtos.AnyAsync(item =>
                item.Id != id &&
                item.NomeNormalizado == produto.NomeNormalizado))
        {
            ModelState.AddModelError("Input.Nome", ProdutoFormulario.MensagemDuplicidade);
            return Page();
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Produto atualizado com sucesso.";
        return RedirectToPage("/Produtos/Detalhes", new { id });
    }
}
