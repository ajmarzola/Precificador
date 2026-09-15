using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Precificacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Precificacao;

// Orquestra o estado atual UC018-UC024. As formulas continuam exclusivamente nas calculadoras do Core.
public sealed class PrecificacaoProdutoAtual(PrecificadorDbContext context, IDataOperacionalEmpresa dataOperacionalEmpresa)
{
    public async Task<ResultadoPrecificacaoProdutoAtual?> CalcularAsync(int produtoId)
    {
        var produto = await context.Produtos.AsNoTracking().Where(p => p.Id == produtoId)
            .Select(p => new ProdutoCarregado(p.Id, p.EmpresaId, p.MargemAlvo)).SingleOrDefaultAsync();
        if (produto is null) return null;

        var registroAtual = await context.RegistrosPrecosProdutos.AsNoTracking()
            .SelecionarAtualAsync(produtoId);
        var precoPrateleiraAtual = registroAtual?.PrecoPrateleira;
        var dataReferenciaPrecoAtual = registroAtual?.DataReferencia;

        var ficha = await context.FichasTecnicas.AsNoTracking().Where(f => f.ProdutoId == produtoId)
            .Select(f => new FichaCarregada(f.Id, f.Rendimento, f.TempoAtivoMinutos)).SingleOrDefaultAsync();
        if (ficha is null)
        {
            return ResultadoIncompleto(
                produto,
                dataOperacionalEmpresa.Hoje,
                precoPrateleiraAtual,
                dataReferenciaPrecoAtual,
                ["A ficha técnica não foi cadastrada."],
                ["A ficha técnica não foi cadastrada."],
                null,
                null);
        }

        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking()
            .Select(c => new ConfiguracaoCarregada(c.ValorHoraTrabalho, c.TarifaEnergiaKwh, c.IncrementoComercial, c.ReservaComercialDesconto))
            .SingleOrDefaultAsync();
        if (configuracao is null)
        {
            return ResultadoIncompleto(
                produto,
                dataOperacionalEmpresa.Hoje,
                precoPrateleiraAtual,
                dataReferenciaPrecoAtual,
                ["As configurações de precificação não foram encontradas."],
                ["As configurações de precificação não foram encontradas."],
                ficha.Rendimento,
                ficha.TempoAtivoMinutos);
        }

        var itens = await context.ItensFichaTecnica.AsNoTracking().Where(i => i.FichaTecnicaId == ficha.Id)
            .Select(i => new ItemCarregado(i.Id, i.InsumoId, i.Quantidade, i.PercentualPerda)).ToListAsync();
        var vigentes = await context.PrecosInsumos.AsNoTracking().SelecionarVigentesAsync(itens.Select(i => i.InsumoId).Distinct().ToArray(), dataOperacionalEmpresa.Hoje);
        var itensCalculados = CalculadoraCustoItens.Calcular(itens.Select(i => new ItemCustoEntrada(i.Id, i.Quantidade, vigentes.GetValueOrDefault(i.InsumoId)?.CustoUnitario)));
        var custos = itensCalculados.Itens.ToDictionary(i => i.ItemId);
        var perdas = CalculadoraCustoPerdas.Calcular(itens.Select(i => new ItemCustoPerdaEntrada(i.Id, i.PercentualPerda, custos[i.Id].CustoItem)));
        var maoDeObra = CalculadoraCustoMaoDeObra.Calcular(ficha.TempoAtivoMinutos, configuracao.ValorHoraTrabalho);
        var usos = await context.UsosEquipamentosFicha.AsNoTracking().Where(u => u.FichaTecnicaId == ficha.Id)
            .Select(u => new UsoEquipamentoCustoEntrada(u.Id, u.PotenciaKw, u.TempoUsoMinutos)).ToListAsync();
        var energia = CalculadoraCustoEnergia.Calcular(configuracao.TarifaEnergiaKwh, usos);
        var custoProduto = CalculadoraCustoProduto.Calcular(itensCalculados.CustoBaseItens, perdas.CustoPerdasLote, maoDeObra.CustoMaoDeObraLote, energia.CustoEnergiaLote, ficha.Rendimento);
        var precoProduto = CalculadoraPrecoProduto.Calcular(custoProduto.CustoUnitarioProduto, produto.MargemAlvo, configuracao.IncrementoComercial);
        var margemAtual = CalculadoraMargemAtual.Calcular(custoProduto.CustoUnitarioProduto, precoPrateleiraAtual, produto.MargemAlvo);

        var impedimentos = new List<string>();
        var impedimentosCusto = new List<string>();
        if (itens.Count == 0)
        {
            impedimentos.Add("A ficha não possui itens.");
            impedimentosCusto.Add("A ficha não possui itens.");
        }
        else if (!itensCalculados.Completo)
        {
            impedimentos.Add("Há item(ns) sem preço vigente.");
            impedimentosCusto.Add("Há item(ns) sem preço vigente.");
        }

        if (!maoDeObra.Completo)
        {
            impedimentos.Add("Valor da hora de trabalho não configurado.");
            impedimentosCusto.Add("Valor da hora de trabalho não configurado.");
        }

        if (!energia.Completo)
        {
            impedimentos.Add("Tarifa de energia não configurada.");
            impedimentosCusto.Add("Tarifa de energia não configurada.");
        }

        if (configuracao.IncrementoComercial is null) impedimentos.Add("Incremento comercial não configurado.");

        return new ResultadoPrecificacaoProdutoAtual(
            produto.EmpresaId,
            itensCalculados.CustoBaseItens,
            perdas.CustoPerdasLote,
            maoDeObra.CustoMaoDeObraLote,
            energia.CustoEnergiaLote,
            custoProduto.CustoLote,
            custoProduto.CustoUnitarioProduto,
            precoProduto.PrecoTeorico,
            precoProduto.PrecoSugerido,
            configuracao.ReservaComercialDesconto,
            custoProduto.Completo,
            precoProduto.Completo,
            produto.MargemAlvo,
            precoPrateleiraAtual,
            dataReferenciaPrecoAtual,
            margemAtual.MargemAtual,
            margemAtual.Situacao,
            impedimentos,
            ImpedimentosMargem(precoPrateleiraAtual, impedimentosCusto))
        {
            DataOperacional = dataOperacionalEmpresa.Hoje,
            Rendimento = ficha.Rendimento,
            TempoAtivoMinutos = ficha.TempoAtivoMinutos,
            ValorHoraTrabalho = configuracao.ValorHoraTrabalho,
            TarifaEnergiaKwh = configuracao.TarifaEnergiaKwh,
            IncrementoComercial = configuracao.IncrementoComercial,
            Itens = itensCalculados.Itens.ToDictionary(i => i.ItemId, i => new ItemPrecificacaoAtual(i.CustoUnitario, i.CustoItem, perdas.Itens.Single(p => p.ItemId == i.ItemId).CustoPerdaItem)),
            Usos = energia.Usos.ToDictionary(u => u.UsoId, u => new UsoPrecificacaoAtual(u.ConsumoKwh, u.CustoEnergiaUso))
        };
    }

    private static ResultadoPrecificacaoProdutoAtual ResultadoIncompleto(
        ProdutoCarregado produto,
        DateOnly dataOperacional,
        decimal? precoPrateleiraAtual,
        DateOnly? dataReferenciaPrecoAtual,
        IReadOnlyList<string> impedimentos,
        IReadOnlyList<string> impedimentosCusto,
        decimal? rendimento,
        int? tempoAtivoMinutos)
    {
        var margemAtual = CalculadoraMargemAtual.Calcular(null, precoPrateleiraAtual, produto.MargemAlvo);
        return new ResultadoPrecificacaoProdutoAtual(
            produto.EmpresaId,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            false,
            false,
            produto.MargemAlvo,
            precoPrateleiraAtual,
            dataReferenciaPrecoAtual,
            margemAtual.MargemAtual,
            margemAtual.Situacao,
            impedimentos,
            ImpedimentosMargem(precoPrateleiraAtual, impedimentosCusto))
        {
            DataOperacional = dataOperacional,
            Rendimento = rendimento,
            TempoAtivoMinutos = tempoAtivoMinutos
        };
    }

    private static IReadOnlyList<string> ImpedimentosMargem(decimal? precoPrateleiraAtual, IReadOnlyList<string> impedimentosCusto)
    {
        var impedimentos = new List<string>();
        if (precoPrateleiraAtual is null) impedimentos.Add("Preço de prateleira não definido.");
        impedimentos.AddRange(impedimentosCusto);
        return impedimentos;
    }

    private sealed record ProdutoCarregado(int Id, int EmpresaId, decimal MargemAlvo);
    private sealed record FichaCarregada(int Id, decimal Rendimento, int TempoAtivoMinutos);
    private sealed record ItemCarregado(int Id, int InsumoId, decimal Quantidade, decimal PercentualPerda);
    private sealed record ConfiguracaoCarregada(decimal? ValorHoraTrabalho, decimal? TarifaEnergiaKwh, decimal? IncrementoComercial, decimal ReservaComercialDesconto);
}

public sealed record ResultadoPrecificacaoProdutoAtual(
    int EmpresaId,
    decimal? CustoBaseItens,
    decimal? CustoPerdasLote,
    decimal? CustoMaoDeObraLote,
    decimal? CustoEnergiaLote,
    decimal? CustoLote,
    decimal? CustoUnitarioProduto,
    decimal? PrecoTeorico,
    decimal? PrecoSugerido,
    decimal? ReservaComercialDesconto,
    bool CustoProdutoCompleto,
    bool PrecoProdutoCompleto,
    decimal MargemAlvo,
    decimal? PrecoPrateleiraAtual,
    DateOnly? DataReferenciaPrecoAtual,
    decimal? MargemAtual,
    SituacaoMargemProduto SituacaoMargem,
    IReadOnlyList<string> Impedimentos,
    IReadOnlyList<string> ImpedimentosMargemAtual)
{
    public DateOnly DataOperacional { get; init; }
    public decimal? Rendimento { get; init; }
    public int? TempoAtivoMinutos { get; init; }
    public decimal? ValorHoraTrabalho { get; init; }
    public decimal? TarifaEnergiaKwh { get; init; }
    public decimal? IncrementoComercial { get; init; }
    public IReadOnlyDictionary<int, ItemPrecificacaoAtual> Itens { get; init; } = new Dictionary<int, ItemPrecificacaoAtual>();
    public IReadOnlyDictionary<int, UsoPrecificacaoAtual> Usos { get; init; } = new Dictionary<int, UsoPrecificacaoAtual>();
}
public sealed record ItemPrecificacaoAtual(decimal? CustoUnitario, decimal? CustoItem, decimal? CustoPerdaItem);
public sealed record UsoPrecificacaoAtual(decimal ConsumoKwh, decimal? CustoEnergiaUso);
