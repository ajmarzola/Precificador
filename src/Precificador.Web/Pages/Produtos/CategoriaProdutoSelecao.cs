using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos;

/// <summary>Opções e validação do seletor de Categoria de produto usado em Produtos/Novo e Produtos/Editar.</summary>
public static class CategoriaProdutoSelecao
{
    public const string MensagemInvalida = "Selecione uma categoria de produto válida.";

    public static async Task<List<SelectListItem>> ListarAsync(PrecificadorDbContext context, int? categoriaAtualId)
    {
        var categorias = await context.CategoriasProdutos.AsNoTracking()
            .Where(categoria => categoria.Ativo || categoria.Id == categoriaAtualId)
            .OrderBy(categoria => categoria.NomeNormalizado)
            .Select(categoria => new { categoria.Id, categoria.Nome })
            .ToListAsync();

        var opcoes = new List<SelectListItem> { new("Sem categoria", string.Empty) };
        opcoes.AddRange(categorias.Select(categoria =>
            new SelectListItem(categoria.Nome, categoria.Id.ToString(CultureInfo.InvariantCulture))));
        return opcoes;
    }

    /// <summary>Rejeita categoria inexistente/cross-tenant e categoria inativa diferente da já vinculada ao produto.</summary>
    public static async Task<bool> ValidarAsync(PrecificadorDbContext context, ModelStateDictionary modelState, int? categoriaProdutoId, int? categoriaAtualId)
    {
        if (categoriaProdutoId is null)
        {
            return true;
        }

        var categoria = await context.CategoriasProdutos.AsNoTracking()
            .Where(categoria => categoria.Id == categoriaProdutoId)
            .Select(categoria => new { categoria.Ativo })
            .SingleOrDefaultAsync();

        if (categoria is null || (!categoria.Ativo && categoriaProdutoId != categoriaAtualId))
        {
            modelState.AddModelError("Input.CategoriaProdutoId", MensagemInvalida);
            return false;
        }

        return true;
    }
}
