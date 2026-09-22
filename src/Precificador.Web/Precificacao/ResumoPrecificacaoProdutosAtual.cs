using Microsoft.EntityFrameworkCore;
using Precificador.Core.Empresas;
using Precificador.Core.Precificacao;
using Precificador.Core.Produtos;
using Precificador.Infrastructure.Persistence;

namespace Precificador.Web.Precificacao;

// Carrega os conjuntos necessários para a listagem sem orquestrar um Produto por vez.
public sealed class ResumoPrecificacaoProdutosAtual(PrecificadorDbContext context, IDataOperacionalEmpresa dataOperacionalEmpresa)
{
    public async Task<IReadOnlyDictionary<int, ResumoPrecificacaoProdutoAtual>> CalcularAsync(
        IReadOnlyCollection<int> produtoIds,
        CancellationToken cancellationToken = default)
    {
        var ids = produtoIds.Distinct().ToArray();
        if (ids.Length == 0) return new Dictionary<int, ResumoPrecificacaoProdutoAtual>();

        var produtos = await context.Produtos.AsNoTracking()
            .Where(produto => ids.Contains(produto.Id))
            .Select(produto => new ProdutoCarregado(produto.Id, produto.MargemAlvo, produto.CategoriaProdutoId))
            .ToListAsync(cancellationToken);
        if (produtos.Count == 0) return new Dictionary<int, ResumoPrecificacaoProdutoAtual>();

        var categoriaIds = produtos.Where(produto => produto.CategoriaProdutoId.HasValue)
            .Select(produto => produto.CategoriaProdutoId!.Value).Distinct().ToArray();
        var categorias = await context.CategoriasProdutos.AsNoTracking()
            .Where(categoria => categoriaIds.Contains(categoria.Id))
            .Select(categoria => new CategoriaCarregada(categoria.Id, categoria.FormaCalculoDesgasteEquipamento, categoria.ValorDesgasteEquipamento))
            .ToDictionaryAsync(categoria => categoria.Id, cancellationToken);
        var fichas = await context.FichasTecnicas.AsNoTracking()
            .Where(ficha => ids.Contains(ficha.ProdutoId))
            .Select(ficha => new FichaCarregada(ficha.Id, ficha.ProdutoId, ficha.Rendimento))
            .ToListAsync(cancellationToken);
        var fichasPorProduto = fichas.ToDictionary(ficha => ficha.ProdutoId);
        var fichaIds = fichas.Select(ficha => ficha.Id).ToArray();
        var itens = await context.ItensFichaTecnica.AsNoTracking()
            .Where(item => fichaIds.Contains(item.FichaTecnicaId))
            .Select(item => new ItemCarregado(item.Id, item.FichaTecnicaId, item.InsumoId, item.Quantidade, item.PercentualPerda))
            .ToListAsync(cancellationToken);
        var usos = await context.UsosEquipamentosFicha.AsNoTracking()
            .Where(uso => fichaIds.Contains(uso.FichaTecnicaId))
            .Select(uso => new UsoCarregado(uso.FichaTecnicaId, uso.Id, uso.PotenciaKw, uso.TempoUsoMinutos))
            .ToListAsync(cancellationToken);
        var vigentes = await context.PrecosInsumos.AsNoTracking().SelecionarVigentesAsync(
            itens.Select(item => item.InsumoId).Distinct().ToArray(), dataOperacionalEmpresa.Hoje, cancellationToken);
        var registrosAtuais = await context.RegistrosPrecosProdutos.AsNoTracking().SelecionarAtuaisAsync(ids, cancellationToken);
        var configuracao = await context.ConfiguracoesPrecificacaoEmpresas.AsNoTracking()
            .Select(item => new ConfiguracaoCarregada(item.PercentualMaoDeObra, item.TarifaEnergiaKwh))
            .SingleOrDefaultAsync(cancellationToken);

        var itensPorFicha = itens.GroupBy(item => item.FichaTecnicaId).ToDictionary(grupo => grupo.Key, grupo => grupo.ToArray());
        var usosPorFicha = usos.GroupBy(uso => uso.FichaTecnicaId).ToDictionary(grupo => grupo.Key, grupo => grupo.ToArray());
        var resultado = new Dictionary<int, ResumoPrecificacaoProdutoAtual>();

        foreach (var produto in produtos)
        {
            var precoPrateleira = registrosAtuais.GetValueOrDefault(produto.Id)?.PrecoPrateleira;
            if (configuracao is null || !fichasPorProduto.TryGetValue(produto.Id, out var ficha))
            {
                resultado.Add(produto.Id, Incompleto(produto.Id, produto.MargemAlvo, precoPrateleira));
                continue;
            }

            var itensDaFicha = itensPorFicha.GetValueOrDefault(ficha.Id) ?? [];
            var itensCalculados = CalculadoraCustoItens.Calcular(itensDaFicha.Select(item =>
                new ItemCustoEntrada(item.Id, item.Quantidade, vigentes.GetValueOrDefault(item.InsumoId)?.CustoUnitario)));
            var custosItens = itensCalculados.Itens.ToDictionary(item => item.ItemId);
            var perdas = CalculadoraCustoPerdas.Calcular(itensDaFicha.Select(item =>
                new ItemCustoPerdaEntrada(item.Id, item.PercentualPerda, custosItens[item.Id].CustoItem)));
            var maoDeObra = CalculadoraCustoMaoDeObra.Calcular(itensCalculados.CustoBaseItens, configuracao.PercentualMaoDeObra);
            var energia = CalculadoraCustoEnergia.Calcular(configuracao.TarifaEnergiaKwh,
                (usosPorFicha.GetValueOrDefault(ficha.Id) ?? []).Select(uso => new UsoEquipamentoCustoEntrada(uso.Id, uso.PotenciaKw, uso.TempoUsoMinutos)));
            categorias.TryGetValue(produto.CategoriaProdutoId ?? 0, out var categoria);
            var desgaste = CalculadoraCustoDesgasteEquipamentos.Calcular(
                categoria?.FormaCalculoDesgasteEquipamento,
                categoria?.ValorDesgasteEquipamento,
                itensCalculados.CustoBaseItens);
            var custo = CalculadoraCustoProduto.Calcular(itensCalculados.CustoBaseItens, perdas.CustoPerdasLote,
                maoDeObra.CustoMaoDeObraLote, energia.CustoEnergiaLote, desgaste.CustoDesgasteEquipamentosLote, ficha.Rendimento);
            var margem = CalculadoraMargemAtual.Calcular(custo.CustoUnitarioProduto, precoPrateleira, produto.MargemAlvo);
            resultado.Add(produto.Id, new ResumoPrecificacaoProdutoAtual(produto.Id, custo.CustoUnitarioProduto, precoPrateleira, margem.MargemAtual, margem.Situacao));
        }

        return resultado;
    }

    private static ResumoPrecificacaoProdutoAtual Incompleto(int produtoId, decimal margemAlvo, decimal? precoPrateleira)
    {
        var margem = CalculadoraMargemAtual.Calcular(null, precoPrateleira, margemAlvo);
        return new ResumoPrecificacaoProdutoAtual(produtoId, null, precoPrateleira, margem.MargemAtual, margem.Situacao);
    }

    private sealed record ProdutoCarregado(int Id, decimal MargemAlvo, int? CategoriaProdutoId);
    private sealed record CategoriaCarregada(int Id, FormaCalculoDesgasteEquipamento FormaCalculoDesgasteEquipamento, decimal ValorDesgasteEquipamento);
    private sealed record FichaCarregada(int Id, int ProdutoId, decimal Rendimento);
    private sealed record ItemCarregado(int Id, int FichaTecnicaId, int InsumoId, decimal Quantidade, decimal PercentualPerda);
    private sealed record UsoCarregado(int FichaTecnicaId, int Id, decimal PotenciaKw, int TempoUsoMinutos);
    private sealed record ConfiguracaoCarregada(decimal PercentualMaoDeObra, decimal? TarifaEnergiaKwh);
}

public sealed record ResumoPrecificacaoProdutoAtual(
    int ProdutoId,
    decimal? CustoUnitarioProduto,
    decimal? PrecoPrateleiraAtual,
    decimal? MargemAtual,
    SituacaoMargemProduto SituacaoMargem);
