using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.FichasTecnicas;
using Precificador.Web.Apresentacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Itens;

public sealed class NovoModel(PrecificadorDbContext context) : PageModel
{
    private const string MensagemFichaAusente = "Defina a base da ficha técnica antes de adicionar insumos.";
    private const string MensagemInsumoIndisponivel = "O insumo selecionado não está disponível para inclusão na ficha técnica.";

    [BindProperty]
    public ItemFichaTecnicaInputModel Input { get; set; } = new();

    public ProdutoResumo? Produto { get; private set; }

    public FichaResumo? Ficha { get; private set; }

    public IReadOnlyList<SelectListItem> InsumosDisponiveis { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int produtoId)
    {
        if (!await CarregarProdutoAsync(produtoId))
        {
            return NotFound();
        }

        if (!await CarregarFichaAsync(produtoId))
        {
            return RedirecionarParaFicha(produtoId);
        }

        await CarregarInsumosAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var produtoId = ProdutoIdDaRota();
        if (!await CarregarProdutoAsync(produtoId))
        {
            return NotFound();
        }

        if (!await CarregarFichaAsync(produtoId))
        {
            return RedirecionarParaFicha(produtoId);
        }

        var quantidadeInformada = ItemFichaTecnicaFormulario.TentarObterQuantidade(ModelState, Input, out var quantidade);
        var percentualPerdaInformado = ItemFichaTecnicaFormulario.TentarObterPercentualPerda(ModelState, Input.PercentualPerda, out var percentualPerda);
        if (!Input.InsumoId.HasValue)
        {
            ModelState.AddModelError("Input.InsumoId", MensagemInsumoIndisponivel);
        }

        var insumo = Input.InsumoId.HasValue
            ? await context.Insumos.SingleOrDefaultAsync(item => item.Id == Input.InsumoId.Value && item.Ativo)
            : null;

        if (Input.InsumoId.HasValue && insumo is null)
        {
            ModelState.AddModelError("Input.InsumoId", MensagemInsumoIndisponivel);
        }

        if (!quantidadeInformada || !percentualPerdaInformado || !ModelState.IsValid)
        {
            await CarregarInsumosAsync();
            return Page();
        }

        if (await context.ItensFichaTecnica.AnyAsync(item =>
                item.FichaTecnicaId == Ficha!.Id &&
                item.InsumoId == insumo!.Id))
        {
            ModelState.AddModelError("Input.InsumoId", "Este insumo já foi adicionado à ficha técnica.");
            await CarregarInsumosAsync();
            return Page();
        }

        try
        {
            context.ItensFichaTecnica.Add(ItemFichaTecnica.Criar(
                Ficha!.EmpresaId,
                Ficha.Id,
                insumo!.Id,
                quantidade,
                Input.Observacao,
                percentualPerda));
            insumo!.ConsolidarIdentidade();
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(CampoPara(exception.ParamName), exception.Message);
            await CarregarInsumosAsync();
            return Page();
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Insumo adicionado à ficha técnica com sucesso.";
        return RedirectToPage("/Produtos/FichaTecnica", new { id = produtoId });
    }

    private int ProdutoIdDaRota() =>
        Convert.ToInt32(RouteData.Values["produtoId"], CultureInfo.InvariantCulture);

    private async Task<bool> CarregarProdutoAsync(int produtoId)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == produtoId)
            .Select(produto => new ProdutoResumo(
                produto.Id,
                produto.EmpresaId,
                produto.Nome,
                produto.Ativo))
            .SingleOrDefaultAsync();

        return Produto is not null;
    }

    private async Task<bool> CarregarFichaAsync(int produtoId)
    {
        Ficha = await context.FichasTecnicas.AsNoTracking()
            .Where(ficha => ficha.ProdutoId == produtoId)
            .Select(ficha => new FichaResumo(
                ficha.Id,
                ficha.EmpresaId,
                ficha.Rendimento))
            .SingleOrDefaultAsync();

        return Ficha is not null;
    }

    private async Task CarregarInsumosAsync()
    {
        InsumosDisponiveis = await context.Insumos.AsNoTracking()
            .Where(insumo => insumo.Ativo)
            .OrderBy(insumo => insumo.NomeNormalizado)
            .ThenBy(insumo => insumo.MarcaNormalizada)
            .Select(insumo => new SelectListItem
            {
                Value = insumo.Id.ToString(CultureInfo.InvariantCulture),
                Text = RotuloInsumo(insumo.Nome, insumo.Marca, insumo.UnidadeBase)
            })
            .ToListAsync();
    }

    private IActionResult RedirecionarParaFicha(int produtoId)
    {
        TempData["MensagemAviso"] = MensagemFichaAusente;
        return RedirectToPage("/Produtos/FichaTecnica", new { id = produtoId });
    }

    private static string RotuloInsumo(string nome, string? marca, Precificador.Core.Insumos.UnidadeMedida unidade) =>
        string.IsNullOrWhiteSpace(marca)
            ? $"{nome} ({InsumoRotulos.Unidade(unidade)})"
            : $"{nome} — {marca} ({InsumoRotulos.Unidade(unidade)})";

    private static string CampoPara(string? nomeParametro) => nomeParametro switch
    {
        "observacao" => "Input.Observacao",
        "percentualPerda" => "Input.PercentualPerda",
        _ => "Input.Quantidade"
    };

    public sealed class ItemFichaTecnicaInputModel
    {
        public int? InsumoId { get; set; }

        public string? Quantidade { get; set; }

        public string? Observacao { get; set; }

        public string? PercentualPerda { get; set; }
    }

    public sealed record ProdutoResumo(int Id, int EmpresaId, string Nome, bool Ativo);

    public sealed record FichaResumo(int Id, int EmpresaId, decimal Rendimento);
}
