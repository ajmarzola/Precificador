using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos.Colecoes;

public sealed class NovoModel(PrecificadorDbContext context, IEmpresaContext empresaContext) : PageModel
{
    [BindProperty] public ColecaoProdutoInputModel Input { get; set; } = new();
    public IReadOnlyList<CategoriaOpcao> Categorias { get; private set; } = [];

    public async Task OnGetAsync() => await CarregarCategoriasAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        await CarregarCategoriasAsync();
        var dadosValidos = ColecaoProdutoFormulario.TentarObterDados(ModelState, Input, empresaContext.EmpresaId!.Value, out var colecao);
        var categoriasValidas = await ColecaoProdutoFormulario.CategoriasValidasAsync(context, ModelState, Input.CategoriaProdutoIds, []);
        if (!dadosValidos || !categoriasValidas || !ModelState.IsValid) return Page();
        if (await context.ColecoesProdutos.AnyAsync(x => x.NomeNormalizado == colecao!.NomeNormalizado && x.DataLancamento == colecao.DataLancamento))
        {
            ModelState.AddModelError("Input.Nome", ColecaoProdutoFormulario.MensagemDuplicidade);
            return Page();
        }
        context.ColecoesProdutos.Add(colecao!);
        foreach (var id in Input.CategoriaProdutoIds.Distinct())
            context.ColecoesProdutosCategorias.Add(new ColecaoProdutoCategoria(colecao!, id));
        try { await context.SaveChangesAsync(); }
        catch (DbUpdateException erro) when (ColecaoProdutoFormulario.Duplicidade(erro))
        {
            ModelState.AddModelError("Input.Nome", ColecaoProdutoFormulario.MensagemDuplicidade);
            return Page();
        }
        TempData["MensagemSucesso"] = "Coleção cadastrada com sucesso.";
        return RedirectToPage("/Produtos/Colecoes/Index");
    }

    private async Task CarregarCategoriasAsync() => Categorias = await context.CategoriasProdutos.AsNoTracking()
        .Where(x => x.Ativo).OrderBy(x => x.NomeNormalizado)
        .Select(x => new CategoriaOpcao(x.Id, x.Nome, x.Ativo)).ToListAsync();
}

public sealed record CategoriaOpcao(int Id, string Nome, bool Ativo);
