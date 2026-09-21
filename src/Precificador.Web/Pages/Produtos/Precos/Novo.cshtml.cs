using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Apresentacao;
using Precificador.Web.Precificacao;

namespace Precificador.Web.Pages.Produtos.Precos;

public sealed class NovoModel(PrecificadorDbContext context, IDataOperacionalEmpresa dataOperacionalEmpresa, PrecificacaoProdutoAtual precificacaoAtual) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public ProdutoResumo? Produto { get; private set; }
    public ResultadoPrecificacaoProdutoAtual? Precificacao { get; private set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await CarregarAsync(id)) return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        if (!await CarregarAsync(id)) return NotFound();

        decimal precoPrateleira = 0m;
        if (!DecimalInputParser.TentarParse(Input.PrecoPrateleira, out precoPrateleira))
        {
            ModelState.AddModelError("Input.PrecoPrateleira", "O preço de prateleira deve ser um número válido.");
        }
        else if (precoPrateleira <= 0)
        {
            ModelState.AddModelError("Input.PrecoPrateleira", "O preço de prateleira deve ser maior que zero.");
        }

        if (Precificacao is null || !Precificacao.PrecoProdutoCompleto || Precificacao.CustoUnitarioProduto is null || Precificacao.PrecoSugerido is null || Precificacao.ReservaComercialDesconto is null)
            ModelState.AddModelError(string.Empty, "Precificação incompleta. Não é possível registrar o preço de prateleira.");
        if (!ModelState.IsValid) return Page();

        context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(Precificacao!.EmpresaId, Produto!.Id, dataOperacionalEmpresa.Hoje, Precificacao.CustoUnitarioProduto!.Value, Precificacao.MargemAlvo, Precificacao.PrecoSugerido!.Value, precoPrateleira, Precificacao.ReservaComercialDesconto!.Value));
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Preço de prateleira registrado com sucesso.";
        return RedirectToPage("/Produtos/Detalhes", new { id });
    }

    private async Task<bool> CarregarAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking().Where(p => p.Id == id)
            .Select(p => new ProdutoResumo(
                p.Id,
                p.EmpresaId,
                p.Nome,
                p.CategoriaProdutoId == null
                    ? null
                    : context.CategoriasProdutos.Where(categoria => categoria.Id == p.CategoriaProdutoId).Select(categoria => categoria.Nome).FirstOrDefault(),
                p.Ativo,
                p.MargemAlvo))
            .SingleOrDefaultAsync();
        if (Produto is null) return false;
        Precificacao = await precificacaoAtual.CalcularAsync(id);
        return Precificacao is not null;
    }

    public sealed class InputModel { public string? PrecoPrateleira { get; set; } }
    public sealed record ProdutoResumo(int Id, int EmpresaId, string Nome, string? Categoria, bool Ativo, decimal MargemAlvo);
}
