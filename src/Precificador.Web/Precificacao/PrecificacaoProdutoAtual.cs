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
        if (ficha is null) return new ResultadoPrecificacaoProdutoAtual(produto.EmpresaId, null, null, null, null, null, null, null, null, null, false, false);

        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking()
            .Select(c => new ConfiguracaoCarregada(c.ValorHoraTrabalho, c.TarifaEnergiaKwh, c.IncrementoComercial, c.ReservaComercialDesconto))
            .SingleOrDefaultAsync();
        if (configuracao is null) return new ResultadoPrecificacaoProdutoAtual(produto.EmpresaId, null, null, null, null, null, null, null, null, null, false, false);

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
        return new ResultadoPrecificacaoProdutoAtual(produto.EmpresaId, itensCalculados.CustoBaseItens, perdas.CustoPerdasLote, maoDeObra.CustoMaoDeObraLote, energia.CustoEnergiaLote, custoProduto.CustoLote, custoProduto.CustoUnitarioProduto, precoProduto.PrecoTeorico, precoProduto.PrecoSugerido, configuracao.ReservaComercialDesconto, custoProduto.Completo, precoProduto.Completo);
    }

    private sealed record ProdutoCarregado(int Id, int EmpresaId, decimal MargemAlvo);
    private sealed record FichaCarregada(int Id, decimal Rendimento, int TempoAtivoMinutos);
    private sealed record ItemCarregado(int Id, int InsumoId, decimal Quantidade, decimal PercentualPerda);
    private sealed record ConfiguracaoCarregada(decimal? ValorHoraTrabalho, decimal? TarifaEnergiaKwh, decimal? IncrementoComercial, decimal ReservaComercialDesconto);
}

public sealed record ResultadoPrecificacaoProdutoAtual(int EmpresaId, decimal? CustoBaseItens, decimal? CustoPerdasLote, decimal? CustoMaoDeObraLote, decimal? CustoEnergiaLote, decimal? CustoLote, decimal? CustoUnitarioProduto, decimal? PrecoTeorico, decimal? PrecoSugerido, decimal? ReservaComercialDesconto, bool CustoProdutoCompleto, bool PrecoProdutoCompleto);
