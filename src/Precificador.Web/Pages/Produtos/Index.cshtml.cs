using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos;

public sealed class IndexModel(PrecificadorDbContext context) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public IReadOnlyList<ProdutoListagem> Produtos { get; private set; } = [];
    public string? Consulta { get; private set; }
    public bool TemPesquisa => Consulta is not null;

    public async Task OnGetAsync(string? q)
    {
        Consulta = NormalizarConsulta(q);
        var consulta = context.Produtos.AsNoTracking();

        if (Consulta is not null)
        {
            consulta = consulta.Where(produto => produto.NomeNormalizado.Contains(Consulta));
        }

        Produtos = await consulta
            .OrderBy(produto => produto.NomeNormalizado)
            .Select(produto => new ProdutoListagem(produto.Id, produto.Nome, produto.Categoria, produto.MargemAlvo, produto.Ativo))
            .ToListAsync();
    }

    public static string? NormalizarConsulta(string? consulta)
    {
        var normalizada = Regex.Replace(consulta?.Trim() ?? string.Empty, @"\s+", " ");
        return string.IsNullOrEmpty(normalizada) ? null : normalizada.ToUpperInvariant();
    }

    public sealed record ProdutoListagem(int Id, string Nome, string? Categoria, decimal MargemAlvo, bool Ativo);
}
