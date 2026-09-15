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
using Precificador.Web.Pages.Produtos.FichaTecnica.Equipamentos;
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

    public decimal? RendimentoAtual { get; private set; }

    public IReadOnlyList<ItemFichaResumo> Itens { get; private set; } = [];

    public decimal? CustoBaseItens { get; private set; }

    public bool CustoBaseItensCompleto { get; private set; }

    public decimal? CustoPerdasLote { get; private set; }

    public bool CustoPerdasCompleto { get; private set; }

    public decimal? CustoMaoDeObraLote { get; private set; }

    public bool CustoMaoDeObraCompleto { get; private set; }

    public IReadOnlyList<UsoEquipamentoResumo> UsosEquipamentos { get; private set; } = [];
    public decimal? CustoEnergiaLote { get; private set; }
    public bool CustoEnergiaCompleto { get; private set; }

    public decimal? CustoLote { get; private set; }

    public decimal? CustoUnitarioProduto { get; private set; }

    public bool CustoProdutoCompleto { get; private set; }

    public string? CustoBaseItensFormatado => CustoBaseItens is null
        ? null
        : PrecoInsumoFormatacao.CustoCalculado(CustoBaseItens.Value);

    public string? CustoMaoDeObraLoteFormatado => CustoMaoDeObraLote is null
        ? null
        : PrecoInsumoFormatacao.CustoCalculado(CustoMaoDeObraLote.Value);
    public string? CustoPerdasLoteFormatado => CustoPerdasLote is null
        ? null
        : PrecoInsumoFormatacao.CustoCalculado(CustoPerdasLote.Value);
    public string? CustoEnergiaLoteFormatado => CustoEnergiaLote is null ? null : PrecoInsumoFormatacao.CustoCalculado(CustoEnergiaLote.Value);

    public string? CustoLoteFormatado => CustoLote is null ? null : PrecoInsumoFormatacao.CustoCalculado(CustoLote.Value);

    public string? CustoUnitarioProdutoFormatado => CustoUnitarioProduto is null ? null : PrecoInsumoFormatacao.CustoCalculado(CustoUnitarioProduto.Value);

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await CarregarProdutoAsync(id))
        {
            return NotFound();
        }

        var ficha = await CarregarEstadoFichaAsync(id);
        if (PossuiFicha && !await CarregarCustosConfiguracaoAsync(ficha!))
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
        if (PossuiFicha && !await CarregarCustosConfiguracaoAsync(fichaPersistida!))
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
            RendimentoAtual = null;
            Itens = [];
            CustoBaseItens = null;
            CustoBaseItensCompleto = false;
            CustoPerdasLote = null;
            CustoPerdasCompleto = false;
            CustoMaoDeObraLote = null;
            CustoMaoDeObraCompleto = false;
            UsosEquipamentos = [];
            CustoEnergiaLote = null;
            CustoEnergiaCompleto = false;
            CustoLote = null;
            CustoUnitarioProduto = null;
            CustoProdutoCompleto = false;
            return null;
        }

        PossuiFicha = true;
        RendimentoAtual = ficha.Rendimento;
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
                item.PercentualPerda,
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
        var perdas = CalculadoraCustoPerdas.Calcular(itens.Select(item =>
            new ItemCustoPerdaEntrada(item.Id, item.PercentualPerda, custosPorItem[item.Id].CustoItem)));
        var perdasPorItem = perdas.Itens.ToDictionary(item => item.ItemId);

        Itens = itens.Select(item =>
        {
            var custo = custosPorItem[item.Id];
            return new ItemFichaResumo(
                item.Id,
                item.InsumoId,
                item.Nome,
                item.Marca,
                item.Quantidade,
                item.PercentualPerda,
                item.Unidade,
                item.Observacao,
                item.InsumoAtivo,
                custo.CustoUnitario,
                custo.CustoItem,
                perdasPorItem[item.Id].CustoPerdaItem);
        }).ToList();
        CustoBaseItens = calculo.CustoBaseItens;
        CustoBaseItensCompleto = calculo.Completo;
        CustoPerdasLote = perdas.CustoPerdasLote;
        CustoPerdasCompleto = perdas.Completo;

        var usos = await context.UsosEquipamentosFicha.AsNoTracking().Where(uso => uso.FichaTecnicaId == ficha.Id)
            .OrderBy(uso => uso.NomeEquipamentoNormalizado)
            .Select(uso => new UsoEquipamentoCarregado(uso.Id, uso.NomeEquipamento, uso.PotenciaKw, uso.TempoUsoMinutos)).ToListAsync();
        UsosEquipamentos = usos.Select(uso => new UsoEquipamentoResumo(uso.Id, uso.NomeEquipamento, uso.PotenciaKw, uso.TempoUsoMinutos, 0m, null)).ToList();

        return ficha;
    }

    private async Task<bool> CarregarCustosConfiguracaoAsync(FichaResumo ficha)
    {
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking()
            .Select(item => new ConfiguracaoPrecificacaoResumo(item.ValorHoraTrabalho, item.TarifaEnergiaKwh))
            .SingleOrDefaultAsync();

        if (configuracao is null)
        {
            return false;
        }

        var calculo = CalculadoraCustoMaoDeObra.Calcular(ficha.TempoAtivoMinutos, configuracao.ValorHoraTrabalho);
        CustoMaoDeObraLote = calculo.CustoMaoDeObraLote;
        CustoMaoDeObraCompleto = calculo.Completo;
        var energia = CalculadoraCustoEnergia.Calcular(configuracao.TarifaEnergiaKwh, UsosEquipamentos.Select(uso => new UsoEquipamentoCustoEntrada(uso.Id, uso.PotenciaKw, uso.TempoUsoMinutos)));
        var energiaPorUso = energia.Usos.ToDictionary(uso => uso.UsoId);
        UsosEquipamentos = UsosEquipamentos.Select(uso => uso with { ConsumoKwh = energiaPorUso[uso.Id].ConsumoKwh, CustoEnergiaUso = energiaPorUso[uso.Id].CustoEnergiaUso }).ToList();
        CustoEnergiaLote = energia.CustoEnergiaLote;
        CustoEnergiaCompleto = energia.Completo;
        var custoProduto = CalculadoraCustoProduto.Calcular(
            CustoBaseItens,
            CustoPerdasLote,
            CustoMaoDeObraLote,
            CustoEnergiaLote,
            ficha.Rendimento);
        CustoLote = custoProduto.CustoLote;
        CustoUnitarioProduto = custoProduto.CustoUnitarioProduto;
        CustoProdutoCompleto = custoProduto.Completo;
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

    private sealed record ConfiguracaoPrecificacaoResumo(decimal? ValorHoraTrabalho, decimal? TarifaEnergiaKwh);

    public sealed record UsoEquipamentoResumo(int Id, string NomeEquipamento, decimal PotenciaKw, int TempoUsoMinutos, decimal ConsumoKwh, decimal? CustoEnergiaUso)
    {
        public string PotenciaFormatada => UsoEquipamentoFichaFormulario.FormatarPotencia(PotenciaKw);
        public string ConsumoFormatado => UsoEquipamentoFichaFormulario.FormatarConsumo(ConsumoKwh);
        public string CustoFormatado => CustoEnergiaUso is null ? "—" : PrecoInsumoFormatacao.CustoCalculado(CustoEnergiaUso.Value);
    }

    private sealed record UsoEquipamentoCarregado(int Id, string NomeEquipamento, decimal PotenciaKw, int TempoUsoMinutos);

    public sealed record ItemFichaResumo(
        int Id,
        int InsumoId,
        string Nome,
        string? Marca,
        decimal Quantidade,
        decimal PercentualPerda,
        UnidadeMedida Unidade,
        string? Observacao,
        bool InsumoAtivo,
        decimal? CustoUnitario,
        decimal? CustoItem,
        decimal? CustoPerdaItem)
    {
        public string QuantidadeFormatada => ItemFichaTecnicaFormulario.FormatarQuantidade(Quantidade);

        public string UnidadeFormatada => InsumoRotulos.Unidade(Unidade);

        public string Situacao => InsumoAtivo ? "Ativo" : "Inativo";

        public string PercentualPerdaFormatado => ItemFichaTecnicaFormulario.FormatarPercentualPerda(PercentualPerda) + "%";

        public string CustoUnitarioFormatado => CustoUnitario is null
            ? "Sem preço vigente"
            : PrecoInsumoFormatacao.CustoUnitario(CustoUnitario.Value);

        public string CustoItemFormatado => CustoItem is null
            ? "—"
            : PrecoInsumoFormatacao.CustoCalculado(CustoItem.Value);

        public string CustoPerdaFormatado => CustoPerdaItem is null
            ? "—"
            : PrecoInsumoFormatacao.CustoCalculado(CustoPerdaItem.Value);
    }

    private sealed record ItemFichaCarregado(
        int Id,
        int InsumoId,
        string Nome,
        string? Marca,
        decimal Quantidade,
        decimal PercentualPerda,
        UnidadeMedida Unidade,
        string? Observacao,
        bool InsumoAtivo);
}
