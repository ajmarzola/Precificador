using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Colecoes;

public sealed class EditarModel(PrecificadorDbContext context, IEmpresaContext empresaContext) : PageModel
{
    [BindProperty] public ColecaoProdutoInputModel Input { get; set; } = new();
    public IReadOnlyList<CategoriaOpcao> Categorias { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var colecao = await context.ColecoesProdutos.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id);
        if (colecao is null) return NotFound();
        Input = new ColecaoProdutoInputModel
        {
            Nome = colecao.Nome,
            DataLancamento = colecao.DataLancamento,
            DataFinalizacao = colecao.DataFinalizacao,
            CategoriaProdutoIds = await context.ColecoesProdutosCategorias.AsNoTracking()
                .Where(x => x.ColecaoProdutoId == id).Select(x => x.CategoriaProdutoId).ToListAsync()
        };
        await CarregarCategoriasAsync(Input.CategoriaProdutoIds);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var colecao = await context.ColecoesProdutos.SingleOrDefaultAsync(x => x.Id == id);
        if (colecao is null) return NotFound();
        var existentes = await context.ColecoesProdutosCategorias.Where(x => x.ColecaoProdutoId == id).ToListAsync();
        var idsExistentes = existentes.Select(x => x.CategoriaProdutoId).ToArray();
        await CarregarCategoriasAsync(idsExistentes);
        var dadosValidos = ColecaoProdutoFormulario.TentarObterDados(ModelState, Input, empresaContext.EmpresaId!.Value, out var dados);
        var categoriasValidas = await ColecaoProdutoFormulario.CategoriasValidasAsync(context, ModelState, Input.CategoriaProdutoIds, idsExistentes);
        if (!dadosValidos || !categoriasValidas || !ModelState.IsValid) return Page();
        if (await context.ColecoesProdutos.AnyAsync(x => x.Id != id && x.NomeNormalizado == dados!.NomeNormalizado && x.DataLancamento == dados.DataLancamento))
        {
            ModelState.AddModelError("Input.Nome", ColecaoProdutoFormulario.MensagemDuplicidade);
            return Page();
        }
        colecao.AtualizarDados(Input.Nome!, Input.DataLancamento!.Value, Input.DataFinalizacao);
        var selecionados = Input.CategoriaProdutoIds.Distinct().ToHashSet();
        context.ColecoesProdutosCategorias.RemoveRange(existentes.Where(x => !selecionados.Contains(x.CategoriaProdutoId)));
        foreach (var categoriaId in selecionados.Except(idsExistentes))
            context.ColecoesProdutosCategorias.Add(new ColecaoProdutoCategoria(colecao, categoriaId));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException erro) when (ColecaoProdutoFormulario.Duplicidade(erro))
        {
            ModelState.AddModelError("Input.Nome", ColecaoProdutoFormulario.MensagemDuplicidade);
            return Page();
        }
        TempData["MensagemSucesso"] = "Coleção atualizada com sucesso.";
        return RedirectToPage("/Produtos/Colecoes/Index");
    }

    private async Task CarregarCategoriasAsync(IReadOnlyCollection<int> vinculadas) => Categorias = await context.CategoriasProdutos.AsNoTracking()
        .Where(x => x.Ativo || vinculadas.Contains(x.Id)).OrderBy(x => x.NomeNormalizado)
        .Select(x => new CategoriaOpcao(x.Id, x.Nome, x.Ativo)).ToListAsync();
}
