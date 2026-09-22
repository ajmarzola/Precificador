using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Precificacao;

namespace Precificador.Web.Pages.Produtos;

public sealed class IndexModel(PrecificadorDbContext context, ResumoPrecificacaoProdutosAtual resumoPrecificacao) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public IReadOnlyList<ProdutoListagem> Produtos { get; private set; } = [];
    public IReadOnlyList<SelectListItem> Categorias { get; private set; } = [];
    public string? Consulta { get; private set; }
    public string? Categoria { get; private set; }
    public bool TemFiltros => Consulta is not null || Categoria is not null;
    public bool TemPesquisa => Consulta is not null;

    public async Task OnGetAsync(string? q, string? categoria = null)
    {
        Consulta = NormalizarConsulta(q);
        Categoria = NormalizarCategoria(categoria);
        var categoriasCarregadas = await context.CategoriasProdutos.AsNoTracking()
            .OrderBy(item => item.NomeNormalizado)
            .Select(item => new { item.Id, item.Nome, item.Ativo })
            .ToListAsync();
        Categorias = [
            new SelectListItem("Todas as categorias", string.Empty, Categoria is null),
            new SelectListItem("Sem categoria", "sem-categoria", Categoria == "sem-categoria"),
            .. categoriasCarregadas.Select(item => new SelectListItem(
                item.Nome + (item.Ativo ? string.Empty : " (inativa)"),
                item.Id.ToString(),
                Categoria == item.Id.ToString()))
        ];
        var consulta = context.Produtos.AsNoTracking();

        if (Consulta is not null)
        {
            consulta = consulta.Where(produto => produto.NomeNormalizado.Contains(Consulta));
        }

        if (Categoria == "sem-categoria")
        {
            consulta = consulta.Where(produto => produto.CategoriaProdutoId == null);
        }
        else if (int.TryParse(Categoria, out var categoriaId) && categoriaId > 0)
        {
            consulta = consulta.Where(produto => produto.CategoriaProdutoId == categoriaId);
        }
        else if (Categoria is not null)
        {
            consulta = consulta.Where(_ => false);
        }

        var produtos = await consulta
            .OrderBy(produto => produto.NomeNormalizado)
            .Select(produto => new ProdutoListagem(
                produto.Id,
                produto.Nome,
                produto.CategoriaProdutoId == null
                    ? null
                    : context.CategoriasProdutos.Where(categoria => categoria.Id == produto.CategoriaProdutoId).Select(categoria => categoria.Nome).FirstOrDefault(),
                produto.MargemAlvo,
                produto.Ativo,
                null,
                null,
                null))
            .ToListAsync();
        var resumos = await resumoPrecificacao.CalcularAsync(produtos.Select(produto => produto.Id).ToArray());
        Produtos = produtos.Select(produto => resumos.TryGetValue(produto.Id, out var resumo)
            ? produto with { CustoUnitarioProduto = resumo.CustoUnitarioProduto, PrecoPrateleiraAtual = resumo.PrecoPrateleiraAtual, MargemAtual = resumo.MargemAtual }
            : produto).ToList();
    }

    public static string? NormalizarConsulta(string? consulta)
    {
        var normalizada = Regex.Replace(consulta?.Trim() ?? string.Empty, @"\s+", " ");
        return string.IsNullOrEmpty(normalizada) ? null : normalizada.ToUpperInvariant();
    }

    private static string? NormalizarCategoria(string? categoria) =>
        string.IsNullOrWhiteSpace(categoria) ? null : categoria.Trim();

    public sealed record ProdutoListagem(int Id, string Nome, string? Categoria, decimal MargemAlvo, bool Ativo,
        decimal? CustoUnitarioProduto, decimal? PrecoPrateleiraAtual, decimal? MargemAtual);
}
