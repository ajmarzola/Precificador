using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;
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
        if (Input.PrecoPrateleira <= 0) ModelState.AddModelError("Input.PrecoPrateleira", "O preço de prateleira deve ser maior que zero.");
        if (Precificacao is null || !Precificacao.PrecoProdutoCompleto || Precificacao.CustoUnitarioProduto is null || Precificacao.PrecoSugerido is null || Precificacao.ReservaComercialDesconto is null)
            ModelState.AddModelError(string.Empty, "Precificação incompleta. Não é possível registrar o preço de prateleira.");
        if (!ModelState.IsValid) return Page();

        context.RegistrosPrecosProdutos.Add(RegistroPrecoProduto.Criar(Produto!.EmpresaId, Produto.Id, dataOperacionalEmpresa.Hoje, Precificacao!.CustoUnitarioProduto!.Value, Produto.MargemAlvo, Precificacao.PrecoSugerido!.Value, Input.PrecoPrateleira, Precificacao.ReservaComercialDesconto!.Value));
        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Preço de prateleira registrado com sucesso.";
        return RedirectToPage("/Produtos/Detalhes", new { id });
    }

    private async Task<bool> CarregarAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking().Where(p => p.Id == id).Select(p => new ProdutoResumo(p.Id, p.EmpresaId, p.Nome, p.Categoria, p.Ativo, p.MargemAlvo)).SingleOrDefaultAsync();
        if (Produto is null) return false;
        Precificacao = await precificacaoAtual.CalcularAsync(id);
        return Precificacao is not null;
    }

    public sealed class InputModel { public decimal PrecoPrateleira { get; set; } }
    public sealed record ProdutoResumo(int Id, int EmpresaId, string Nome, string? Categoria, bool Ativo, decimal MargemAlvo);
}
