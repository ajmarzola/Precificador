using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Core.Precificacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Precificacao;

namespace Precificador.Web.Pages.Produtos;

public sealed class PrecificacaoModel(PrecificadorDbContext context, PrecificacaoProdutoAtual precificacaoAtual) : PageModel
{
    public ProdutoResumo? Produto { get; private set; }
    public ResultadoPrecificacaoProdutoAtual? Resultado { get; private set; }
    public bool PossuiFicha { get; private set; }
    public IReadOnlyList<ItemResumo> Itens { get; private set; } = [];
    public IReadOnlyList<UsoResumo> Usos { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking().Where(produto => produto.Id == id)
            .Select(produto => new ProdutoResumo(produto.Id, produto.Nome, produto.Categoria, produto.Ativo))
            .SingleOrDefaultAsync();
        if (Produto is null) return NotFound();

        Resultado = await precificacaoAtual.CalcularAsync(id);
        if (Resultado is null) return NotFound();

        var ficha = await context.FichasTecnicas.AsNoTracking().Where(item => item.ProdutoId == id)
            .Select(item => new { item.Id }).SingleOrDefaultAsync();
        if (ficha is null) return Page();

        PossuiFicha = true;
        var itens = await (
            from item in context.ItensFichaTecnica.AsNoTracking()
            join insumo in context.Insumos.AsNoTracking() on item.InsumoId equals insumo.Id
            where item.FichaTecnicaId == ficha.Id
            orderby insumo.NomeNormalizado, insumo.MarcaNormalizada
            select new ItemEstrutural(item.Id, insumo.Nome, insumo.Marca, insumo.Ativo, item.Quantidade, insumo.UnidadeBase, item.PercentualPerda))
            .ToListAsync();
        Itens = itens.Select(item => Resultado.Itens.TryGetValue(item.Id, out var calculado)
            ? new ItemResumo(item.Nome, item.Marca, item.Ativo, item.Quantidade, item.Unidade, item.PercentualPerda, calculado.CustoUnitario, calculado.CustoItem, calculado.CustoPerdaItem)
            : new ItemResumo(item.Nome, item.Marca, item.Ativo, item.Quantidade, item.Unidade, item.PercentualPerda, null, null, null)).ToList();

        var usos = await context.UsosEquipamentosFicha.AsNoTracking().Where(uso => uso.FichaTecnicaId == ficha.Id)
            .OrderBy(uso => uso.NomeEquipamentoNormalizado)
            .Select(uso => new UsoEstrutural(uso.Id, uso.NomeEquipamento, uso.PotenciaKw, uso.TempoUsoMinutos)).ToListAsync();
        Usos = usos.Select(uso => Resultado.Usos.TryGetValue(uso.Id, out var calculado)
            ? new UsoResumo(uso.NomeEquipamento, uso.PotenciaKw, uso.TempoUsoMinutos, calculado.ConsumoKwh, calculado.CustoEnergiaUso)
            : new UsoResumo(uso.NomeEquipamento, uso.PotenciaKw, uso.TempoUsoMinutos, null, null)).ToList();

        return Page();
    }

    public sealed record ProdutoResumo(int Id, string Nome, string? Categoria, bool Ativo);
    private sealed record ItemEstrutural(int Id, string Nome, string? Marca, bool Ativo, decimal Quantidade, UnidadeMedida Unidade, decimal PercentualPerda);
    private sealed record UsoEstrutural(int Id, string NomeEquipamento, decimal PotenciaKw, int TempoUsoMinutos);
    public sealed record ItemResumo(string Nome, string? Marca, bool Ativo, decimal Quantidade, UnidadeMedida Unidade, decimal PercentualPerda, decimal? CustoUnitario, decimal? CustoItem, decimal? CustoPerdaItem);
    public sealed record UsoResumo(string NomeEquipamento, decimal PotenciaKw, int TempoUsoMinutos, decimal? ConsumoKwh, decimal? CustoEnergiaUso);

    public static string SituacaoRotulo(SituacaoMargemProduto situacao) => DetalhesModel.SituacaoMargemRotulo(situacao);
}