using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Core.Precificacao;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Apresentacao;
using Precificador.Web.Pages.Produtos.FichaTecnica.Itens;
using FichaTecnicaDominio = Precificador.Core.FichasTecnicas.FichaTecnica;

namespace Precificador.Web.Pages.Produtos;

public sealed class FichaTecnicaModel(PrecificadorDbContext context, IDataOperacionalEmpresa dataOperacionalEmpresa) : PageModel
{
    [BindProperty]
    public FichaTecnicaInputModel Input { get; set; } = new();

    public ProdutoResumo? Produto { get; private set; }

    public string? MensagemSucesso => TempData["MensagemSucesso"] as string;

    public string? MensagemAviso => TempData["MensagemAviso"] as string;

    public bool PossuiFicha { get; private set; }

    public IReadOnlyList<ItemFichaResumo> Itens { get; private set; } = [];

    public decimal? CustoBaseItens { get; private set; }

    public bool CustoBaseItensCompleto { get; private set; }

    public decimal? CustoMaoDeObraLote { get; private set; }

    public bool CustoMaoDeObraCompleto { get; private set; }

    public string? CustoBaseItensFormatado => CustoBaseItens is null
        ? null
        : PrecoInsumoFormatacao.CustoCalculado(CustoBaseItens.Value);

    public string? CustoMaoDeObraLoteFormatado => CustoMaoDeObraLote is null
        ? null
        : PrecoInsumoFormatacao.CustoCalculado(CustoMaoDeObraLote.Value);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await CarregarProdutoAsync(id))
        {
            return NotFound();
        }

        var ficha = await CarregarEstadoFichaAsync(id);
        if (PossuiFicha && !await CarregarCustoMaoDeObraAsync(ficha!))
        {
            return NotFound();
        }
        if (ficha is not null)
        {
            Input = new FichaTecnicaInputModel
            {
                Rendimento = FichaTecnicaFormulario.FormatarRendimento(ficha.Rendimento),
                TempoAtivoMinutos = ficha.TempoAtivoMinutos
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var id = ProdutoIdDaRota();
        if (!await CarregarProdutoAsync(id))
        {
            return NotFound();
        }

        var rendimentoInformado = FichaTecnicaFormulario.TentarObterRendimento(ModelState, Input, out var rendimento);
        ValidarTempoAtivo();
        var fichaPersistida = await CarregarEstadoFichaAsync(id);
        if (PossuiFicha && !await CarregarCustoMaoDeObraAsync(fichaPersistida!))
        {
            return NotFound();
        }
        if (!rendimentoInformado || !ModelState.IsValid)
        {
            return Page();
        }

        var ficha = await context.FichasTecnicas.SingleOrDefaultAsync(item => item.ProdutoId == id);
        if (ficha is null)
        {
            ficha = FichaTecnicaDominio.Criar(Produto!.EmpresaId, Produto.Id, rendimento, Input.TempoAtivoMinutos!.Value);
            context.FichasTecnicas.Add(ficha);
        }
        else
        {
            ficha.AtualizarBase(rendimento, Input.TempoAtivoMinutos!.Value);
        }

        await context.SaveChangesAsync();
        TempData["MensagemSucesso"] = "Ficha técnica salva com sucesso.";
        return RedirectToPage(new { id });
    }

    private int ProdutoIdDaRota() =>
        Convert.ToInt32(RouteData.Values["id"], CultureInfo.InvariantCulture);

    private async Task<bool> CarregarProdutoAsync(int id)
    {
        Produto = await context.Produtos.AsNoTracking()
            .Where(produto => produto.Id == id)
            .Select(produto => new ProdutoResumo(
                produto.Id,
                produto.EmpresaId,
                produto.Nome,
                produto.Categoria,
                produto.Ativo))
            .SingleOrDefaultAsync();

        return Produto is not null;
    }

    private async Task<FichaResumo?> CarregarEstadoFichaAsync(int produtoId)
    {
        var ficha = await context.FichasTecnicas.AsNoTracking()
            .Where(item => item.ProdutoId == produtoId)
            .Select(item => new FichaResumo(item.Id, item.Rendimento, item.TempoAtivoMinutos))
            .SingleOrDefaultAsync();

        if (ficha is null)
        {
            PossuiFicha = false;
            Itens = [];
            CustoBaseItens = null;
            CustoBaseItensCompleto = false;
            CustoMaoDeObraLote = null;
            CustoMaoDeObraCompleto = false;
            return null;
        }

        PossuiFicha = true;
        var itens = await (
            from item in context.ItensFichaTecnica.AsNoTracking()
            join insumo in context.Insumos.AsNoTracking()
                on item.InsumoId equals insumo.Id
            where item.FichaTecnicaId == ficha.Id
            orderby insumo.NomeNormalizado, insumo.MarcaNormalizada
            select new ItemFichaCarregado(
                item.Id,
                item.InsumoId,
                insumo.Nome,
                insumo.Marca,
                item.Quantidade,
                insumo.UnidadeBase,
                item.Observacao,
                insumo.Ativo))
            .ToListAsync();

        var precosVigentes = await context.PrecosInsumos.AsNoTracking().SelecionarVigentesAsync(
            itens.Select(item => item.InsumoId).Distinct().ToArray(),
            dataOperacionalEmpresa.Hoje);
        var calculo = CalculadoraCustoItens.Calcular(itens.Select(item =>
            new ItemCustoEntrada(
                item.Id,
                item.Quantidade,
                precosVigentes.GetValueOrDefault(item.InsumoId)?.CustoUnitario)));
        var custosPorItem = calculo.Itens.ToDictionary(item => item.ItemId);

        Itens = itens.Select(item =>
        {
            var custo = custosPorItem[item.Id];
            return new ItemFichaResumo(
                item.Id,
                item.InsumoId,
                item.Nome,
                item.Marca,
                item.Quantidade,
                item.Unidade,
                item.Observacao,
                item.InsumoAtivo,
                custo.CustoUnitario,
                custo.CustoItem);
        }).ToList();
        CustoBaseItens = calculo.CustoBaseItens;
        CustoBaseItensCompleto = calculo.Completo;

        return ficha;
    }

    private async Task<bool> CarregarCustoMaoDeObraAsync(FichaResumo ficha)
    {
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking()
            .Select(item => new ConfiguracaoPrecificacaoResumo(item.ValorHoraTrabalho))
            .SingleOrDefaultAsync();

        if (configuracao is null)
        {
            return false;
        }

        var calculo = CalculadoraCustoMaoDeObra.Calcular(ficha.TempoAtivoMinutos, configuracao.ValorHoraTrabalho);
        CustoMaoDeObraLote = calculo.CustoMaoDeObraLote;
        CustoMaoDeObraCompleto = calculo.Completo;
        return true;
    }

    private void ValidarTempoAtivo()
    {
        if (Input.TempoAtivoMinutos is null)
        {
            ModelState.AddModelError("Input.TempoAtivoMinutos", "O tempo ativo é obrigatório.");
        }
        else if (Input.TempoAtivoMinutos < 0)
        {
            ModelState.AddModelError("Input.TempoAtivoMinutos", "O tempo ativo não pode ser negativo.");
        }
    }

    public sealed class FichaTecnicaInputModel
    {
        public string? Rendimento { get; set; }

        public int? TempoAtivoMinutos { get; set; }
    }

    public sealed record ProdutoResumo(int Id, int EmpresaId, string Nome, string? Categoria, bool Ativo);

    public sealed record FichaResumo(int Id, decimal Rendimento, int TempoAtivoMinutos);

    private sealed record ConfiguracaoPrecificacaoResumo(decimal? ValorHoraTrabalho);

    public sealed record ItemFichaResumo(
        int Id,
        int InsumoId,
        string Nome,
        string? Marca,
        decimal Quantidade,
        UnidadeMedida Unidade,
        string? Observacao,
        bool InsumoAtivo,
        decimal? CustoUnitario,
        decimal? CustoItem)
    {
        public string QuantidadeFormatada => ItemFichaTecnicaFormulario.FormatarQuantidade(Quantidade);

        public string UnidadeFormatada => InsumoRotulos.Unidade(Unidade);

        public string Situacao => InsumoAtivo ? "Ativo" : "Inativo";

        public string CustoUnitarioFormatado => CustoUnitario is null
            ? "Sem preço vigente"
            : PrecoInsumoFormatacao.CustoUnitario(CustoUnitario.Value);

        public string CustoItemFormatado => CustoItem is null
            ? "—"
            : PrecoInsumoFormatacao.CustoCalculado(CustoItem.Value);
    }

    private sealed record ItemFichaCarregado(
        int Id,
        int InsumoId,
        string Nome,
        string? Marca,
        decimal Quantidade,
        UnidadeMedida Unidade,
        string? Observacao,
        bool InsumoAtivo);
}
