using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Apresentacao;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Itens;

public sealed class EditarModel(PrecificadorDbContext context) : PageModel
{
    private ItemFichaTecnica? itemRastreado;

    [BindProperty]
    public ItemFichaTecnicaInputModel Input { get; set; } = new();

    public ProdutoResumo? Produto { get; private set; }

    public FichaResumo? Ficha { get; private set; }

    public ItemResumo? Item { get; private set; }

    public InsumoResumo? Insumo { get; private set; }

    public async Task<IActionResult> OnGetAsync(int produtoId, int itemId)
    {
        if (!await CarregarCadeiaAsync(produtoId, itemId, rastrearItem: false))
        {
            return NotFound();
        }

        Input = new ItemFichaTecnicaInputModel
        {
            Quantidade = ItemFichaTecnicaFormulario.FormatarQuantidade(Item!.Quantidade),
            Observacao = Item.Observacao
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var produtoId = IdDaRota("produtoId");
        var itemId = IdDaRota("itemId");
        if (!await CarregarCadeiaAsync(produtoId, itemId, rastrearItem: true))
        {
            return NotFound();
        }

        var quantidadeInformada = ItemFichaTecnicaFormulario.TentarObterQuantidade(ModelState, Input.Quantidade, out var quantidade);
        if (!quantidadeInformada || !ModelState.IsValid)
        {
            return Page();
        }

        try
        {
            itemRastreado!.AtualizarDados(quantidade, Input.Observacao);
        }
        catch (ArgumentException exception)
        {
            ModelState.AddModelError(CampoPara(exception.ParamName), exception.Message);
            return Page();
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Item da ficha técnica atualizado com sucesso.";
        return RedirectToPage("/Produtos/FichaTecnica", new { id = produtoId });
    }

    private int IdDaRota(string nome) =>
        Convert.ToInt32(RouteData.Values[nome], CultureInfo.InvariantCulture);

    private async Task<bool> CarregarCadeiaAsync(int produtoId, int itemId, bool rastrearItem)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == produtoId)
            .Select(produto => new ProdutoResumo(produto.Id, produto.EmpresaId, produto.Nome, produto.Ativo))
            .SingleOrDefaultAsync();

        if (Produto is null)
        {
            return false;
        }

        Ficha = await context.FichasTecnicas.AsNoTracking()
            .Where(ficha => ficha.ProdutoId == produtoId)
            .Select(ficha => new FichaResumo(ficha.Id, ficha.EmpresaId))
            .SingleOrDefaultAsync();

        if (Ficha is null)
        {
            return false;
        }

        var consultaItem = context.ItensFichaTecnica.Where(item =>
            item.Id == itemId &&
            item.FichaTecnicaId == Ficha.Id);

        if (!rastrearItem)
        {
            consultaItem = consultaItem.AsNoTracking();
        }

        var item = await consultaItem.SingleOrDefaultAsync();
        if (item is null)
        {
            return false;
        }

        itemRastreado = rastrearItem ? item : null;
        Item = new ItemResumo(item.Id, item.Quantidade, item.Observacao, item.InsumoId);

        Insumo = await context.Insumos.AsNoTracking()
            .Where(insumo => insumo.Id == item.InsumoId)
            .Select(insumo => new InsumoResumo(insumo.Id, insumo.Nome, insumo.Marca, insumo.UnidadeBase, insumo.Ativo))
            .SingleOrDefaultAsync();

        return Insumo is not null;
    }

    private static string CampoPara(string? nomeParametro) => nomeParametro switch
    {
        "observacao" => "Input.Observacao",
        _ => "Input.Quantidade"
    };

    public sealed class ItemFichaTecnicaInputModel
    {
        public string? Quantidade { get; set; }

        public string? Observacao { get; set; }
    }

    public sealed record ProdutoResumo(int Id, int EmpresaId, string Nome, bool Ativo)
    {
        public string Situacao => Ativo ? "Ativo" : "Inativo";
    }

    public sealed record FichaResumo(int Id, int EmpresaId);

    public sealed record ItemResumo(int Id, decimal Quantidade, string? Observacao, int InsumoId);

    public sealed record InsumoResumo(int Id, string Nome, string? Marca, UnidadeMedida UnidadeBase, bool Ativo)
    {
        public string UnidadeFormatada => InsumoRotulos.Unidade(UnidadeBase);

        public string Situacao => Ativo ? "Ativo" : "Inativo";
    }
}
