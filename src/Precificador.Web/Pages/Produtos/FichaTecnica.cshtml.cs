using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Apresentacao;
using Precificador.Web.Pages.Produtos.FichaTecnica.Itens;
using FichaTecnicaDominio = Precificador.Core.FichasTecnicas.FichaTecnica;

namespace Precificador.Web.Pages.Produtos;

public sealed class FichaTecnicaModel(PrecificadorDbContext context) : PageModel
{
    [BindProperty]
    public FichaTecnicaInputModel Input { get; set; } = new();

    public ProdutoResumo? Produto { get; private set; }

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public string? MensagemAviso => TempData["MensagemAviso"] as string;

    public bool PossuiFicha { get; private set; }

    public IReadOnlyList<ItemFichaResumo> Itens { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await CarregarProdutoAsync(id))
        {
            return NotFound();
        }

        var ficha = await CarregarEstadoFichaAsync(id);
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
        await CarregarEstadoFichaAsync(id);
        if (!rendimentoInformado || !ModelState.IsValid)
        {
            return Page();
        }

        var ficha = await context.FichasTecnicas.SingleOrDefaultAsync(item => item.ProdutoId == id);
        if (ficha is null)
        {
            ficha = FichaTecnicaDominio.Criar(Produto!.EmpresaId, Produto.Id, rendimento, Input.TempoAtivoMinutos!.Value);
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

    private async Task<FichaResumo?> CarregarEstadoFichaAsync(int produtoId)
    {
        var ficha = await context.FichasTecnicas.AsNoTracking()
            .Where(item => item.ProdutoId == produtoId)
            .Select(item => new FichaResumo(item.Id, item.Rendimento, item.TempoAtivoMinutos))
            .SingleOrDefaultAsync();

        if (ficha is null)
        {
            PossuiFicha = false;
            Itens = [];
            return null;
        }

        PossuiFicha = true;
        Itens = await (
            from item in context.ItensFichaTecnica.AsNoTracking()
            join insumo in context.Insumos.AsNoTracking()
                on item.InsumoId equals insumo.Id
            where item.FichaTecnicaId == ficha.Id
            orderby insumo.NomeNormalizado, insumo.MarcaNormalizada
            select new ItemFichaResumo(
                item.Id,
                RotuloInsumo(insumo.Nome, insumo.Marca),
                ItemFichaTecnicaFormulario.FormatarQuantidade(item.Quantidade),
                insumo.UnidadeBase,
                insumo.Ativo))
            .ToListAsync();

        return ficha;
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

    private static string RotuloInsumo(string nome, string? marca) =>
        string.IsNullOrWhiteSpace(marca) ? nome : $"{nome} — {marca}";

    public sealed class FichaTecnicaInputModel
    {
        public string? Rendimento { get; set; }

        public int? TempoAtivoMinutos { get; set; }
    }

    public sealed record ProdutoResumo(int Id, int EmpresaId, string Nome, string? Categoria, bool Ativo);

    public sealed record FichaResumo(int Id, decimal Rendimento, int TempoAtivoMinutos);

    public sealed record ItemFichaResumo(
        int Id,
        string Insumo,
        string Quantidade,
        UnidadeMedida Unidade,
        bool InsumoAtivo)
    {
        public string UnidadeFormatada => InsumoRotulos.Unidade(Unidade);

        public string Situacao => InsumoAtivo ? "Ativo" : "Inativo";
    }
}
