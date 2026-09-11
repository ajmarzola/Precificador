using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Insumos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Insumos;

public sealed class IndexModel(PrecificadorDbContext context) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    public IReadOnlyList<InsumoListagem> Insumos { get; private set; } = [];
    public string? Consulta { get; private set; }
    public bool TemPesquisa => Consulta is not null;

    public async Task OnGetAsync(string? q)
    {
        Consulta = NormalizarConsulta(q);
        var consulta = context.Insumos.AsNoTracking();

        if (Consulta is not null)
        {
            consulta = consulta.Where(insumo =>
                insumo.NomeNormalizado.Contains(Consulta) || insumo.MarcaNormalizada.Contains(Consulta));
        }

        Insumos = await consulta
            .OrderBy(insumo => insumo.NomeNormalizado)
            .ThenBy(insumo => insumo.MarcaNormalizada)
            .Select(insumo => new InsumoListagem(insumo.Id, insumo.Nome, insumo.Marca, insumo.Categoria, insumo.UnidadeBase, insumo.Ativo))
            .ToListAsync();
    }

    public static string? NormalizarConsulta(string? consulta)
    {
        var normalizada = Regex.Replace(consulta?.Trim() ?? string.Empty, @"\s+", " ");
        return string.IsNullOrEmpty(normalizada) ? null : normalizada.ToUpperInvariant();
    }

    public sealed record InsumoListagem(int Id, string Nome, string? Marca, CategoriaInsumo Categoria, UnidadeMedida UnidadeBase, bool Ativo);
}
