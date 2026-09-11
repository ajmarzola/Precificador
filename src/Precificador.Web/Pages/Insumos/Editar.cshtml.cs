using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos;

[Authorize]
public sealed class EditarModel(PrecificadorDbContext context) : PageModel
{
    private const string MensagemDuplicidade = "J\u00e1 existe um insumo cadastrado com esse nome e marca.";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var insumo = await context.Insumos.AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new InputModel
            {
                Nome = item.Nome,
                Marca = item.Marca,
                Categoria = item.Categoria,
                UnidadeBase = item.UnidadeBase,
                Observacao = item.Observacao
            })
            .SingleOrDefaultAsync();

        if (insumo is null)
        {
            return NotFound();
        }

        Input = insumo;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var insumo = await context.Insumos.SingleOrDefaultAsync(item => item.Id == id);
        if (insumo is null)
        {
            return NotFound();
        }

        ValidarCamposObrigatorios();
        if (!ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            insumo.AtualizarDados(Input.Nome!, Input.Categoria!.Value, Input.UnidadeBase!.Value, Input.Marca, Input.Observacao);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(CampoPara(exception.ParamName), exception.Message);
            return Page();
        }

        if (await context.Insumos.AnyAsync(item =>
                item.Id != id &&
                item.NomeNormalizado == insumo.NomeNormalizado &&
                item.MarcaNormalizada == insumo.MarcaNormalizada))
        {
            ModelState.AddModelError("Input.Nome", MensagemDuplicidade);
            return Page();
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Insumo atualizado com sucesso.";
        return RedirectToPage("/Insumos/Detalhes", new { id });
    }

    private void ValidarCamposObrigatorios()
    {
        if (!Input.Categoria.HasValue || !Enum.IsDefined(Input.Categoria.Value))
        {
            ModelState.AddModelError("Input.Categoria", "A categoria \u00e9 obrigat\u00f3ria.");
        }

        if (!Input.UnidadeBase.HasValue || !Enum.IsDefined(Input.UnidadeBase.Value))
        {
            ModelState.AddModelError("Input.UnidadeBase", "A unidade base \u00e9 obrigat\u00f3ria.");
        }
    }

    private static string CampoPara(string? nomeParametro) => nomeParametro switch
    {
        "marca" => "Input.Marca",
        "observacao" => "Input.Observacao",
        _ => "Input.Nome"
    };

    public sealed class InputModel
    {
        [Display(Name = "Nome")]
        public string? Nome { get; set; }

        [Display(Name = "Marca")]
        public string? Marca { get; set; }

        public CategoriaInsumo? Categoria { get; set; }

        [Display(Name = "Unidade base")]
        public UnidadeMedida? UnidadeBase { get; set; }

        [Display(Name = "Observa\u00e7\u00e3o")]
        public string? Observacao { get; set; }
    }
}
