using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.FichasTecnicas;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Apresentacao;

namespace Precificador.Web.Pages.Produtos.FichaTecnica.Itens;

public sealed class RemoverModel(PrecificadorDbContext context) : PageModel
{
    private ItemFichaTecnica? itemRastreado;

    public ProdutoResumo? Produto { get; private set; }

    public ItemResumo? Item { get; private set; }

    public InsumoResumo? Insumo { get; private set; }

    public async Task<IActionResult> OnGetAsync(int produtoId, int itemId)
    {
        return await CarregarCadeiaAsync(produtoId, itemId, rastrearItem: false)
            ? Page()
            : NotFound();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var produtoId = IdDaRota("produtoId");
        var itemId = IdDaRota("itemId");
        if (!await CarregarCadeiaAsync(produtoId, itemId, rastrearItem: true))
        {
            return NotFound();
        }

        context.ItensFichaTecnica.Remove(itemRastreado!);
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Item removido da ficha técnica com sucesso.";
        return RedirectToPage("/Produtos/FichaTecnica", new { id = produtoId });
    }

    private int IdDaRota(string nome) =>
        Convert.ToInt32(RouteData.Values[nome], CultureInfo.InvariantCulture);

    private async Task<bool> CarregarCadeiaAsync(int produtoId, int itemId, bool rastrearItem)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == produtoId)
            .Select(produto => new ProdutoResumo(produto.Id, produto.Nome, produto.Ativo))
            .SingleOrDefaultAsync();
        if (Produto is null)
        {
            return false;
        }

        var ficha = await context.FichasTecnicas.AsNoTracking()
            .Where(item => item.ProdutoId == produtoId)
            .Select(item => new FichaResumo(item.Id))
            .SingleOrDefaultAsync();
        if (ficha is null)
        {
            return false;
        }

        var consultaItem = context.ItensFichaTecnica.Where(item =>
            item.Id == itemId && item.FichaTecnicaId == ficha.Id);
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
        Item = new ItemResumo(item.Quantidade, item.Observacao, item.InsumoId);
        Insumo = await context.Insumos.AsNoTracking()
            .Where(insumo => insumo.Id == item.InsumoId)
            .Select(insumo => new InsumoResumo(insumo.Nome, insumo.Marca, insumo.UnidadeBase, insumo.Ativo))
            .SingleOrDefaultAsync();

        return Insumo is not null;
    }

    public sealed record ProdutoResumo(int Id, string Nome, bool Ativo)
    {
        public string Situacao => Ativo ? "Ativo" : "Inativo";
    }

    private sealed record FichaResumo(int Id);

    public sealed record ItemResumo(decimal Quantidade, string? Observacao, int InsumoId)
    {
        public string QuantidadeFormatada => ItemFichaTecnicaFormulario.FormatarQuantidade(Quantidade);
    }

    public sealed record InsumoResumo(string Nome, string? Marca, UnidadeMedida UnidadeBase, bool Ativo)
    {
        public string UnidadeFormatada => InsumoRotulos.Unidade(UnidadeBase);

        public string Situacao => Ativo ? "Ativo" : "Inativo";
    }
}
