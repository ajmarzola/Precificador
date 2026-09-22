using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Insumos;
using Precificador.Core.Precificacao;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;
using Precificador.Web.Apresentacao;
using Precificador.Web.Pages.Produtos.FichaTecnica.Itens;
using Precificador.Web.Pages.Produtos.FichaTecnica.Equipamentos;
using Precificador.Web.Pages.Produtos.Categorias;
using Precificador.Web.Precificacao;
using FichaTecnicaDominio = Precificador.Core.FichasTecnicas.FichaTecnica;

namespace Precificador.Web.Pages.Produtos;

public sealed class FichaTecnicaModel(PrecificadorDbContext context, PrecificacaoProdutoAtual precificacaoAtual) : PageModel
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

    public decimal? PercentualMaoDeObra { get; private set; }

    public string PercentualMaoDeObraFormatado => PercentualMaoDeObra.HasValue
        ? ProdutoFormatacao.MargemAlvo(PercentualMaoDeObra.Value)
        : "indisponível";

    public IReadOnlyList<UsoEquipamentoResumo> UsosEquipamentos { get; private set; } = [];
    public decimal? CustoEnergiaLote { get; private set; }
    public bool CustoEnergiaCompleto { get; private set; }
    public FormaCalculoDesgasteEquipamento? FormaCalculoDesgasteEquipamento { get; private set; }
    public decimal? ValorDesgasteEquipamento { get; private set; }
    public decimal? CustoDesgasteEquipamentosLote { get; private set; }
    public bool CustoDesgasteCompleto { get; private set; }
    public bool DeveConfigurarTarifaEnergia => PossuiFicha && UsosEquipamentos.Count > 0 && !CustoEnergiaCompleto;

    public decimal? CustoLote { get; private set; }

    public decimal? CustoUnitarioProduto { get; private set; }

    public bool CustoProdutoCompleto { get; private set; }

    public decimal? PrecoTeorico { get; private set; }

    public decimal? PrecoSugerido { get; private set; }

    public bool PrecoProdutoCompleto { get; private set; }

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
    public string? CustoDesgasteEquipamentosLoteFormatado => CustoDesgasteEquipamentosLote is null ? null : PrecoInsumoFormatacao.CustoCalculado(CustoDesgasteEquipamentosLote.Value);
    public string ConfiguracaoDesgasteFormatada => FormaCalculoDesgasteEquipamento is null ? "Produto sem categoria: nenhum desgaste por categoria aplicado." : CategoriaProdutoFormulario.Resumir(FormaCalculoDesgasteEquipamento.Value, ValorDesgasteEquipamento!.Value);

    public string? CustoLoteFormatado => CustoLote is null ? null : PrecoInsumoFormatacao.CustoCalculado(CustoLote.Value);

    public string? CustoUnitarioProdutoFormatado => CustoUnitarioProduto is null ? null : PrecoInsumoFormatacao.CustoCalculado(CustoUnitarioProduto.Value);

    public string? PrecoTeoricoFormatado => PrecoTeorico is null ? null : PrecoInsumoFormatacao.CustoCalculado(PrecoTeorico.Value);

    public string? PrecoSugeridoFormatado => PrecoSugerido is null ? null : PrecoInsumoFormatacao.CustoCalculado(PrecoSugerido.Value);

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
        if (ficha is null)
        {
            Input.Rendimento = FichaTecnicaFormulario.FormatarRendimento(1m);
        }
        else
        {
            Input = new FichaTecnicaInputModel
            {
                Rendimento = FichaTecnicaFormulario.FormatarRendimento(ficha.Rendimento)
            };
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        return await SalvarFichaAsync(id =>
        {
            TempData["MensagemSucesso"] = "Ficha técnica salva com sucesso.";
            return RedirectToPage(new { id });
        });
    }

    public async Task<IActionResult> OnPostConfigurarTarifaAsync()
    {
        return await SalvarFichaAsync(id =>
        {
            var returnUrl = Url.Page("/Produtos/FichaTecnica", new { id });
            return RedirectToPage("/Configuracoes/Precificacao/Editar", new { returnUrl });
        });
    }

    private async Task<IActionResult> SalvarFichaAsync(Func<int, IActionResult> aoSalvar)
    {
        var id = ProdutoIdDaRota();
        if (!await CarregarProdutoAsync(id))
        {
            return NotFound();
        }

        var rendimentoInformado = FichaTecnicaFormulario.TentarObterRendimento(ModelState, Input, out var rendimento);
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
            ficha = FichaTecnicaDominio.Criar(Produto!.EmpresaId, Produto.Id, rendimento);
            context.FichasTecnicas.Add(ficha);
        }
        else
        {
            ficha.AtualizarBase(rendimento);
        }

        await context.SaveChangesAsync();
        return aoSalvar(id);
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
                produto.CategoriaProdutoId == null
                    ? null
                    : context.CategoriasProdutos.Where(categoria => categoria.Id == produto.CategoriaProdutoId).Select(categoria => categoria.Nome).FirstOrDefault(),
                produto.Ativo,
                produto.MargemAlvo))
            .SingleOrDefaultAsync();

        return Produto is not null;
    }

    private async Task<FichaResumo?> CarregarEstadoFichaAsync(int produtoId)
    {
        var ficha = await context.FichasTecnicas.AsNoTracking()
            .Where(item => item.ProdutoId == produtoId)
            .Select(item => new FichaResumo(item.Id, item.Rendimento))
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
            PercentualMaoDeObra = null;
            UsosEquipamentos = [];
            CustoEnergiaLote = null;
            CustoEnergiaCompleto = false;
            FormaCalculoDesgasteEquipamento = null;
            ValorDesgasteEquipamento = null;
            CustoDesgasteEquipamentosLote = null;
            CustoDesgasteCompleto = false;
            CustoLote = null;
            CustoUnitarioProduto = null;
            CustoProdutoCompleto = false;
            PrecoTeorico = null;
            PrecoSugerido = null;
            PrecoProdutoCompleto = false;
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

        Itens = itens.Select(item => new ItemFichaResumo(item.Id, item.InsumoId, item.Nome, item.Marca, item.Quantidade, item.PercentualPerda, item.Unidade, item.Observacao, item.InsumoAtivo, null, null, null)).ToList();

        var usos = await context.UsosEquipamentosFicha.AsNoTracking().Where(uso => uso.FichaTecnicaId == ficha.Id)
            .OrderBy(uso => uso.NomeEquipamentoNormalizado)
            .Select(uso => new UsoEquipamentoCarregado(uso.Id, uso.NomeEquipamento, uso.PotenciaKw, uso.TempoUsoMinutos)).ToListAsync();
        UsosEquipamentos = usos.Select(uso => new UsoEquipamentoResumo(uso.Id, uso.NomeEquipamento, uso.PotenciaKw, uso.TempoUsoMinutos, 0m, null)).ToList();

        return ficha;
    }

    private async Task<bool> CarregarCustosConfiguracaoAsync(FichaResumo ficha)
    {
        if (!await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking().AnyAsync())
        {
            return false;
        }
        var atual = await precificacaoAtual.CalcularAsync(Produto!.Id);
        if (atual is null) return false;
        CustoBaseItens = atual.CustoBaseItens;
        CustoBaseItensCompleto = atual.CustoBaseItens is not null;
        CustoPerdasLote = atual.CustoPerdasLote;
        CustoPerdasCompleto = atual.CustoPerdasLote is not null;
        CustoMaoDeObraLote = atual.CustoMaoDeObraLote;
        CustoMaoDeObraCompleto = atual.CustoMaoDeObraLote is not null;
        PercentualMaoDeObra = atual.PercentualMaoDeObra;
        CustoEnergiaLote = atual.CustoEnergiaLote;
        CustoEnergiaCompleto = atual.CustoEnergiaLote is not null;
        FormaCalculoDesgasteEquipamento = atual.FormaCalculoDesgasteEquipamento;
        ValorDesgasteEquipamento = atual.ValorDesgasteEquipamento;
        CustoDesgasteEquipamentosLote = atual.CustoDesgasteEquipamentosLote;
        CustoDesgasteCompleto = atual.CustoDesgasteEquipamentosLote is not null;
        Itens = Itens.Select(item => atual.Itens.TryGetValue(item.Id, out var custo) ? item with { CustoUnitario = custo.CustoUnitario, CustoItem = custo.CustoItem, CustoPerdaItem = custo.CustoPerdaItem } : item).ToList();
        UsosEquipamentos = UsosEquipamentos.Select(uso => atual.Usos.TryGetValue(uso.Id, out var custo) ? uso with { ConsumoKwh = custo.ConsumoKwh, CustoEnergiaUso = custo.CustoEnergiaUso } : uso).ToList();
        CustoLote = atual.CustoLote;
        CustoUnitarioProduto = atual.CustoUnitarioProduto;
        CustoProdutoCompleto = atual.CustoProdutoCompleto;
        PrecoTeorico = atual.PrecoTeorico;
        PrecoSugerido = atual.PrecoSugerido;
        PrecoProdutoCompleto = atual.PrecoProdutoCompleto;
        return true;
    }

    public sealed class FichaTecnicaInputModel
    {
        public string? Rendimento { get; set; }
    }

    public sealed record ProdutoResumo(int Id, int EmpresaId, string Nome, string? Categoria, bool Ativo, decimal MargemAlvo);

    public sealed record FichaResumo(int Id, decimal Rendimento);

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
