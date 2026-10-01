using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Colecoes;

public sealed class IndexModel(PrecificadorDbContext context, IDataOperacionalEmpresa dataOperacional) : PageModel
{
    public IReadOnlyList<ColecaoListagem> Colecoes { get; private set; } = [];
    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public async Task OnGetAsync()
    {
        var colecoes = await context.ColecoesProdutos.AsNoTracking()
            .OrderByDescending(x => x.DataLancamento).ThenBy(x => x.NomeNormalizado)
            .ToListAsync();
        var categorias = await context.ColecoesProdutosCategorias.AsNoTracking()
            .Select(x => new { x.ColecaoProdutoId, x.CategoriaProduto.Nome, x.CategoriaProduto.NomeNormalizado })
            .ToListAsync();
        Colecoes = colecoes.Select(x => new ColecaoListagem(x.Id, x.Nome, x.DataLancamento, x.DataFinalizacao,
            x.ObterSituacao(dataOperacional.Hoje),
            categorias.Where(c => c.ColecaoProdutoId == x.Id).OrderBy(c => c.NomeNormalizado).Select(c => c.Nome).ToArray())).ToArray();
    }

    public sealed record ColecaoListagem(int Id, string Nome, DateOnly DataLancamento, DateOnly? DataFinalizacao, SituacaoColecaoProduto Situacao, IReadOnlyList<string> Categorias)
    {
        public string SituacaoTexto => Situacao switch { SituacaoColecaoProduto.Planejada => "Planejada", SituacaoColecaoProduto.EmAndamento => "Em andamento", _ => "Finalizada" };
    }
}
