using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.FichasTecnicas;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos;

public sealed class FichaTecnicaModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty]
    public FichaTecnicaInputModel Input { get; set; } = new();

    public ProdutoResumo? Produto { get; private set; }

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await CarregarProdutoAsync(id))
        {
            return NotFound();
        }

        var ficha = await context.FichasTecnicas.AsNoTracking()
            .SingleOrDefaultAsync(item => item.ProdutoId == id);

        if (ficha is not null)
        {
            Input = new FichaTecnicaInputModel
            {
                Rendimento = FichaTecnicaFormulario.FormatarRendimento(ficha.Rendimento),
                TempoAtivoMinutos = ficha.TempoAtivoMinutos
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var id = ProdutoIdDaRota();
        if (!await CarregarProdutoAsync(id))
        {
            return NotFound();
        }

        var rendimentoInformado = FichaTecnicaFormulario.TentarObterRendimento(ModelState, Input, out var rendimento);
        ValidarTempoAtivo();
        if (!rendimentoInformado || !ModelState.IsValid)
        {
            return Page();
        }

        var ficha = await context.FichasTecnicas.SingleOrDefaultAsync(item => item.ProdutoId == id);
        if (ficha is null)
        {
            ficha = FichaTecnica.Criar(Produto!.EmpresaId, Produto.Id, rendimento, Input.TempoAtivoMinutos!.Value);
            context.FichasTecnicas.Add(ficha);
        }
        else
        {
            ficha.AtualizarBase(rendimento, Input.TempoAtivoMinutos!.Value);
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Ficha técnica salva com sucesso.";
        return RedirectToPage(new { id });
    }

    private int ProdutoIdDaRota() =>
        Convert.ToInt32(RouteData.Values["id"], CultureInfo.InvariantCulture);

    private async Task<bool> CarregarProdutoAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == id)
            .Select(produto => new ProdutoResumo(
                produto.Id,
                produto.EmpresaId,
                produto.Nome,
                produto.Categoria,
                produto.Ativo))
            .SingleOrDefaultAsync();

        return Produto is not null;
    }

    private void ValidarTempoAtivo()
    {
        if (Input.TempoAtivoMinutos is null)
        {
            ModelState.AddModelError("Input.TempoAtivoMinutos", "O tempo ativo é obrigatório.");
        }
        else if (Input.TempoAtivoMinutos < 0)
        {
            ModelState.AddModelError("Input.TempoAtivoMinutos", "O tempo ativo não pode ser negativo.");
        }
    }

    public sealed class FichaTecnicaInputModel
    {
        public string? Rendimento { get; set; }

        public int? TempoAtivoMinutos { get; set; }
    }

    public sealed record ProdutoResumo(int Id, int EmpresaId, string Nome, string? Categoria, bool Ativo);
}
