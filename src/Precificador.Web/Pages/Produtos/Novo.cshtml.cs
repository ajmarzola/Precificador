using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos;

[Authorize]
public sealed class NovoModel(PrecificadorDbContext context, IEmpresaContext empresaContext, ILogger<NovoModel> logger) : PageModel
{
    [BindProperty]
    public ProdutoInputModel Input { get; set; } = new();

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var margemInformada = ProdutoFormulario.TentarObterMargemAlvo(ModelState, Input, out var margemAlvo);
        if (!margemInformada || !ModelState.IsValid)
        {
            return Page();
        }

        Produto produto;
        try
        {
            produto = Produto.Criar(
                empresaContext.EmpresaId!.Value,
                Input.Nome!,
                margemAlvo,
                Input.Categoria);
        }
        catch (ArgumentException exception)
        {
            ProdutoFormulario.AdicionarErroDominio(ModelState, exception);
            return Page();
        }

        if (await context.Produtos.AnyAsync(item => item.NomeNormalizado == produto.NomeNormalizado))
        {
            ModelState.AddModelError("Input.Nome", ProdutoFormulario.MensagemDuplicidade);
            return Page();
        }

        context.Produtos.Add(produto);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Falha inesperada ao cadastrar o produto {NomeNormalizado}.", produto.NomeNormalizado);
            throw;
        }

        TempData["MensagemSucesso"] = "Produto cadastrado com sucesso.";
        return RedirectToPage();
    }
}
