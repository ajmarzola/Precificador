using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Precificacao;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Precos;

public sealed class HistoricoModel(PrecificadorDbContext context) : PageModel
{
    public ProdutoResumo? Produto { get; private set; }

    public PrecoAtualResumo? PrecoAtual { get; private set; }

    public IReadOnlyList<PrecoHistoricoLinha> Historico { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == id)
            .Select(produto => new ProdutoResumo(
                produto.Id,
                produto.Nome,
                produto.CategoriaProdutoId == null
                    ? null
                    : context.CategoriasProdutos.Where(categoria => categoria.Id == produto.CategoriaProdutoId).Select(categoria => categoria.Nome).FirstOrDefault(),
                produto.Ativo))
            .SingleOrDefaultAsync();

        if (Produto is null)
        {
            return NotFound();
        }

        var registros = await context.RegistrosPrecosProdutos.AsNoTracking()
            .ListarHistoricoAsync(id);

        var registroAtual = registros.FirstOrDefault();
        PrecoAtual = registroAtual is null
            ? null
            : new PrecoAtualResumo(registroAtual.DataReferencia, registroAtual.PrecoPrateleira);

        Historico = registros
            .Select((registro, indice) => PrecoHistoricoLinha.Criar(registro, indice == 0 ? "Atual" : "Anterior"))
            .ToList();

        return Page();
    }

    public sealed record ProdutoResumo(int Id, string Nome, string? Categoria, bool Ativo);

    public sealed record PrecoAtualResumo(DateOnly DataReferencia, decimal PrecoPrateleira);

    public sealed record PrecoHistoricoLinha(
        DateOnly DataReferencia,
        string Status,
        decimal CustoReferencia,
        decimal MargemReferencia,
        decimal PrecoSugerido,
        decimal PrecoPrateleira,
        decimal ReservaComercialReferencia,
        decimal? DescontoReferencia)
    {
        public static PrecoHistoricoLinha Criar(RegistroPrecoProduto registro, string status) =>
            new(
                registro.DataReferencia,
                status,
                registro.CustoReferencia,
                registro.MargemReferencia,
                registro.PrecoSugerido,
                registro.PrecoPrateleira,
                registro.ReservaComercialReferencia,
                CalculadoraDescontoReferencia.Calcular(
                    registro.PrecoSugerido,
                    registro.PrecoPrateleira,
                    registro.ReservaComercialReferencia));
    }
}
