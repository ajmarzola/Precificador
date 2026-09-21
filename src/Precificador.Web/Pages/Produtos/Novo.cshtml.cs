using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Pages.Produtos;

[Authorize]
public sealed class NovoModel(PrecificadorDbContext context, IEmpresaContext empresaContext, ILogger<NovoModel> logger) : PageModel
{
    [BindProperty]
    public ProdutoInputModel Input { get; set; } = new();

    public IReadOnlyList<SelectListItem> CategoriasDisponiveis { get; private set; } = [];

    public async Task<IActionResult> OnGetAsync()
    {
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas
            .AsNoTracking()
            .SingleOrDefaultAsync();

        if (configuracao is null)
        {
            return NotFound();
        }

        if (configuracao.MargemPadrao.HasValue)
        {
            Input.MargemAlvoPercentual = ProdutoFormulario.FormatarMargemAlvoPercentual(configuracao.MargemPadrao.Value);
        }

        CategoriasDisponiveis = await CategoriaProdutoSelecao.ListarAsync(context, null);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var margemInformada = ProdutoFormulario.TentarObterMargemAlvo(ModelState, Input, out var margemAlvo);
        var categoriaValida = await CategoriaProdutoSelecao.ValidarAsync(context, ModelState, Input.CategoriaProdutoId, null);
        if (!margemInformada || !categoriaValida || !ModelState.IsValid)
        {
            CategoriasDisponiveis = await CategoriaProdutoSelecao.ListarAsync(context, null);
            return Page();
        }

        Produto produto;
        try
        {
            produto = Produto.Criar(
                empresaContext.EmpresaId!.Value,
                Input.Nome!,
                margemAlvo,
                Input.CategoriaProdutoId);
        }
        catch (ArgumentException exception)
        {
            ProdutoFormulario.AdicionarErroDominio(ModelState, exception);
            CategoriasDisponiveis = await CategoriaProdutoSelecao.ListarAsync(context, null);
            return Page();
        }

        if (await context.Produtos.AnyAsync(item => item.NomeNormalizado == produto.NomeNormalizado))
        {
            ModelState.AddModelError("Input.Nome", ProdutoFormulario.MensagemDuplicidade);
            CategoriasDisponiveis = await CategoriaProdutoSelecao.ListarAsync(context, null);
            return Page();
        }

        context.Produtos.Add(produto);
        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException exception)
        {
            logger.LogError(exception, "Falha inesperada ao cadastrar o produto {NomeNormalizado}.", produto.NomeNormalizado);
            throw;
        }

        TempData["MensagemSucesso"] = "Produto cadastrado com sucesso.";
        return RedirectToPage("/Produtos/Detalhes", new { id = produto.Id });
    }
}

