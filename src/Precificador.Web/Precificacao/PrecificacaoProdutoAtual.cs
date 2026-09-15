using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Precificacao;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Precificacao;

// Orquestra o estado atual UC018-UC023. As formulas continuam exclusivamente nas calculadoras do Core.
public sealed class PrecificacaoProdutoAtual(PrecificadorDbContext context, IDataOperacionalEmpresa dataOperacionalEmpresa)
{
    public async Task<ResultadoPrecificacaoProdutoAtual?> CalcularAsync(int produtoId)
    {
        var produto = await context.Produtos.AsNoTracking().Where(p => p.Id == produtoId)
            .Select(p => new ProdutoCarregado(p.Id, p.EmpresaId, p.MargemAlvo)).SingleOrDefaultAsync();
        if (produto is null) return null;

        var ficha = await context.FichasTecnicas.AsNoTracking().Where(f => f.ProdutoId == produtoId)
            .Select(f => new FichaCarregada(f.Id, f.Rendimento, f.TempoAtivoMinutos)).SingleOrDefaultAsync();
        if (ficha is null) return new ResultadoPrecificacaoProdutoAtual(produto.EmpresaId, null, null, null, null, null, null, null, null, null, false, false, produto.MargemAlvo, ["A ficha técnica não foi cadastrada."]);

        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking()
            .Select(c => new ConfiguracaoCarregada(c.ValorHoraTrabalho, c.TarifaEnergiaKwh, c.IncrementoComercial, c.ReservaComercialDesconto))
            .SingleOrDefaultAsync();
        if (configuracao is null) return new ResultadoPrecificacaoProdutoAtual(produto.EmpresaId, null, null, null, null, null, null, null, null, null, false, false, produto.MargemAlvo, ["As configurações de precificação não foram encontradas."]);

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
        var impedimentos = new List<string>();
        if (itens.Count == 0) impedimentos.Add("A ficha não possui itens.");
        else if (!itensCalculados.Completo) impedimentos.Add("Há item(ns) sem preço vigente.");
        if (!maoDeObra.Completo) impedimentos.Add("Valor da hora de trabalho não configurado.");
        if (!energia.Completo) impedimentos.Add("Tarifa de energia não configurada.");
        if (configuracao.IncrementoComercial is null) impedimentos.Add("Incremento comercial não configurado.");
        return new ResultadoPrecificacaoProdutoAtual(produto.EmpresaId, itensCalculados.CustoBaseItens, perdas.CustoPerdasLote, maoDeObra.CustoMaoDeObraLote, energia.CustoEnergiaLote, custoProduto.CustoLote, custoProduto.CustoUnitarioProduto, precoProduto.PrecoTeorico, precoProduto.PrecoSugerido, configuracao.ReservaComercialDesconto, custoProduto.Completo, precoProduto.Completo, produto.MargemAlvo, impedimentos)
        {
            Itens = itensCalculados.Itens.ToDictionary(i => i.ItemId, i => new ItemPrecificacaoAtual(i.CustoUnitario, i.CustoItem, perdas.Itens.Single(p => p.ItemId == i.ItemId).CustoPerdaItem)),
            Usos = energia.Usos.ToDictionary(u => u.UsoId, u => new UsoPrecificacaoAtual(u.ConsumoKwh, u.CustoEnergiaUso))
        };
    }

    private sealed record ProdutoCarregado(int Id, int EmpresaId, decimal MargemAlvo);
    private sealed record FichaCarregada(int Id, decimal Rendimento, int TempoAtivoMinutos);
    private sealed record ItemCarregado(int Id, int InsumoId, decimal Quantidade, decimal PercentualPerda);
    private sealed record ConfiguracaoCarregada(decimal? ValorHoraTrabalho, decimal? TarifaEnergiaKwh, decimal? IncrementoComercial, decimal ReservaComercialDesconto);
}

public sealed record ResultadoPrecificacaoProdutoAtual(int EmpresaId, decimal? CustoBaseItens, decimal? CustoPerdasLote, decimal? CustoMaoDeObraLote, decimal? CustoEnergiaLote, decimal? CustoLote, decimal? CustoUnitarioProduto, decimal? PrecoTeorico, decimal? PrecoSugerido, decimal? ReservaComercialDesconto, bool CustoProdutoCompleto, bool PrecoProdutoCompleto, decimal MargemAlvo, IReadOnlyList<string> Impedimentos)
{
    public IReadOnlyDictionary<int, ItemPrecificacaoAtual> Itens { get; init; } = new Dictionary<int, ItemPrecificacaoAtual>();
    public IReadOnlyDictionary<int, UsoPrecificacaoAtual> Usos { get; init; } = new Dictionary<int, UsoPrecificacaoAtual>();
}
public sealed record ItemPrecificacaoAtual(decimal? CustoUnitario, decimal? CustoItem, decimal? CustoPerdaItem);
public sealed record UsoPrecificacaoAtual(decimal ConsumoKwh, decimal? CustoEnergiaUso);
