using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos;

[Authorize]
public sealed class NovoModel(PrecificadorDbContext context, IEmpresaContext empresaContext, ILogger<NovoModel> logger) : PageModel
{
    private const string MensagemDuplicidade = "Já existe um produto cadastrado com esse nome.";

    [BindProperty]
    public ProdutoInputModel Input { get; set; } = new();

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public void OnGet()
    {
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidarCamposObrigatorios();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        Produto produto;
        try
        {
            produto = Produto.Criar(
                empresaContext.EmpresaId!.Value,
                Input.Nome!,
                Input.MargemAlvoPercentual!.Value / 100m,
                Input.Categoria);
        }
        catch (ArgumentException exception)
        {
            AdicionarErroDominio(ModelState, exception);
            return Page();
        }

        if (await context.Produtos.AnyAsync(item => item.NomeNormalizado == produto.NomeNormalizado))
        {
            ModelState.AddModelError("Input.Nome", MensagemDuplicidade);
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

    private void ValidarCamposObrigatorios()
    {
        if (!Input.MargemAlvoPercentual.HasValue)
        {
            ModelState.AddModelError("Input.MargemAlvoPercentual", "A margem-alvo é obrigatória.");
        }
    }

    private static void AdicionarErroDominio(ModelStateDictionary modelState, ArgumentException exception) =>
        modelState.AddModelError(CampoPara(exception.ParamName), exception.Message);

    private static string CampoPara(string? nomeParametro) => nomeParametro switch
    {
        "categoria" => "Input.Categoria",
        "margemAlvo" => "Input.MargemAlvoPercentual",
        _ => "Input.Nome"
    };
}
