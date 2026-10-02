using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Colecoes;

public sealed class ProdutosModel(PrecificadorDbContext context, IEmpresaContext empresaContext, IDataOperacionalEmpresa dataOperacional) : PageModel
{
    public ColecaoProduto Colecao { get; private set; } = null!;
    public string Situacao => Colecao.ObterSituacao(dataOperacional.Hoje) switch
    { SituacaoColecaoProduto.Planejada => "Planejada", SituacaoColecaoProduto.EmAndamento => "Em andamento", _ => "Finalizada" };
    public IReadOnlyList<string> Categorias { get; private set; } = [];
    public IReadOnlyList<VinculoLinha> Vinculos { get; private set; } = [];
    public IReadOnlyList<SelectListItem> ProdutosDisponiveis { get; private set; } = [];
    [BindProperty] public VincularInput Input { get; set; } = new();
    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;
    private const string Duplicado = "Este produto já está vinculado à coleção.";

    public async Task<IActionResult> OnGetAsync(int id) => await CarregarAsync(id) ? Page() : NotFound();

    public async Task<IActionResult> OnPostVincularAsync(int id)
    {
        if (!await CarregarAsync(id)) return NotFound();
        if (!ModelState.IsValid) return Page();
        if (!await context.Produtos.AnyAsync(x => x.Id == Input.ProdutoId))
        {
            ModelState.AddModelError("Input.ProdutoId", "Selecione um produto válido.");
            return Page();
        }
        if (await context.ProdutosColecoes.AnyAsync(x => x.ColecaoProdutoId == id && x.ProdutoId == Input.ProdutoId))
        {
            ModelState.AddModelError("Input.ProdutoId", Duplicado);
            return Page();
        }
        var vinculo = ProdutoColecao.Criar(empresaContext.EmpresaId!.Value, Input.ProdutoId, id, Input.Destaque);
        context.ProdutosColecoes.Add(vinculo);
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException erro) when (erro.InnerException is SqlException sql &&
            sql.Errors.Cast<SqlError>().Any(e => e.Number is 2601 or 2627 && e.Message.Contains("PK_ProdutosColecoes", StringComparison.Ordinal)))
        {
            context.Entry(vinculo).State = EntityState.Detached;
            await CarregarAsync(id);
            ModelState.AddModelError("Input.ProdutoId", Duplicado);
            return Page();
        }
        TempData["MensagemSucesso"] = "Produto vinculado à coleção.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDefinirDestaqueAsync(int id, int produtoId, bool destaque)
    {
        var vinculo = await context.ProdutosColecoes.SingleOrDefaultAsync(x => x.ColecaoProdutoId == id && x.ProdutoId == produtoId);
        if (vinculo is null) return NotFound();
        vinculo.DefinirDestaque(destaque);
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Destaque atualizado.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDesvincularAsync(int id, int produtoId)
    {
        var vinculo = await context.ProdutosColecoes.SingleOrDefaultAsync(x => x.ColecaoProdutoId == id && x.ProdutoId == produtoId);
        if (vinculo is null) return NotFound();
        context.ProdutosColecoes.Remove(vinculo);
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Produto desvinculado da coleção.";
        return RedirectToPage(new { id });
    }

    private async Task<bool> CarregarAsync(int id)
    {
        var colecao = await context.ColecoesProdutos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (colecao is null) return false;
        Colecao = colecao;
        Categorias = await context.ColecoesProdutosCategorias.AsNoTracking().Where(x => x.ColecaoProdutoId == id)
            .OrderBy(x => x.CategoriaProduto.NomeNormalizado).Select(x => x.CategoriaProduto.Nome).ToListAsync();
        Vinculos = await context.ProdutosColecoes.AsNoTracking().Where(x => x.ColecaoProdutoId == id)
            .OrderByDescending(x => x.Destaque).ThenBy(x => x.Produto.NomeNormalizado)
            .Select(x => new VinculoLinha(x.ProdutoId, x.Produto.Nome,
                context.CategoriasProdutos.Where(c => c.Id == x.Produto.CategoriaProdutoId).Select(c => c.Nome).FirstOrDefault(),
                x.Produto.Ativo, x.Destaque)).ToListAsync();
        ProdutosDisponiveis = await context.Produtos.AsNoTracking()
            .Where(x => !context.ProdutosColecoes.Any(v => v.ProdutoId == x.Id && v.ColecaoProdutoId == id))
            .OrderByDescending(x => x.Ativo).ThenBy(x => x.NomeNormalizado)
            .Select(x => new SelectListItem(x.Nome + (x.Ativo ? "" : " (inativo)"), x.Id.ToString())).ToListAsync();
        return true;
    }

    public sealed class VincularInput
    {
        [Range(1, int.MaxValue, ErrorMessage = "Selecione um produto válido.")]
        public int ProdutoId { get; set; }
        public bool Destaque { get; set; }
    }
    public sealed record VinculoLinha(int ProdutoId, string Nome, string? Categoria, bool Ativo, bool Destaque);
}
