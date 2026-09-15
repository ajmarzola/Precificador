using Precificador.Core.FichasTecnicas;

namespace Precificador.Tests.Unit.FichasTecnicas;

public sealed class ItemFichaTecnicaTests
{
    [Fact]
    public void U1_Criar_item_valido_preserva_empresa_ficha_insumo_quantidade_e_observacao()
    {
        var item = ItemFichaTecnica.Criar(7, 11, 13, 1.25m, "  massa principal  ");

        Assert.Equal(7, item.EmpresaId);
        Assert.Equal(11, item.FichaTecnicaId);
        Assert.Equal(13, item.InsumoId);
        Assert.Equal(1.25m, item.Quantidade);
        Assert.Equal("massa principal", item.Observacao);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void U2_Quantidade_deve_ser_maior_que_zero(string quantidade)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ItemFichaTecnica.Criar(1, 1, 1, decimal.Parse(quantidade, System.Globalization.CultureInfo.InvariantCulture)));
    }

    [Fact]
    public void U3_Observacao_opcional_normaliza_e_valida_limite()
    {
        Assert.Null(ItemFichaTecnica.Criar(1, 1, 1, 1m).Observacao);
        Assert.Null(ItemFichaTecnica.Criar(1, 1, 1, 1m, "   ").Observacao);
        Assert.Equal("linha 1\r\nlinha 2", ItemFichaTecnica.Criar(1, 1, 1, 1m, "  linha 1\r\nlinha 2  ").Observacao);

        Assert.Throws<ArgumentException>(() => ItemFichaTecnica.Criar(1, 1, 1, 1m, new string('a', 1001)));
    }

    [Theory]
    [InlineData(0, 1, 1)]
    [InlineData(-1, 1, 1)]
    [InlineData(1, 0, 1)]
    [InlineData(1, -1, 1)]
    [InlineData(1, 1, 0)]
    [InlineData(1, 1, -1)]
    public void U4_Ids_de_empresa_ficha_e_insumo_devem_ser_positivos(int empresaId, int fichaId, int insumoId)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemFichaTecnica.Criar(empresaId, fichaId, insumoId, 1m));
    }

    [Fact]
    public void U5_Item_nao_possui_unidade_propria()
    {
        var propriedades = typeof(ItemFichaTecnica).GetProperties().Select(propriedade => propriedade.Name);

        Assert.DoesNotContain("Unidade", propriedades);
        Assert.DoesNotContain("UnidadeBase", propriedades);
        Assert.DoesNotContain("UnidadeMedida", propriedades);
    }

    [Fact]
    public void U6_AtualizarDados_valido_altera_quantidade_observacao_e_preserva_vinculos()
    {
        var item = ItemFichaTecnica.Criar(7, 11, 13, 1m, "original");

        item.AtualizarDados(2.5m, "  massa principal  ", 0m);

        Assert.Equal(7, item.EmpresaId);
        Assert.Equal(11, item.FichaTecnicaId);
        Assert.Equal(13, item.InsumoId);
        Assert.Equal(2.5m, item.Quantidade);
        Assert.Equal("massa principal", item.Observacao);
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-0.01")]
    public void U7_AtualizarDados_rejeita_quantidade_invalida(string quantidade)
    {
        var item = ItemFichaTecnica.Criar(1, 2, 3, 1m, "original");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            item.AtualizarDados(decimal.Parse(quantidade, System.Globalization.CultureInfo.InvariantCulture), "alterada", 0m));

        Assert.Equal(1m, item.Quantidade);
        Assert.Equal("original", item.Observacao);
    }

    [Fact]
    public void U8_AtualizarDados_normaliza_observacao_e_valida_limite()
    {
        var item = ItemFichaTecnica.Criar(1, 2, 3, 1m, "original");

        item.AtualizarDados(1m, "   ", 0m);
        Assert.Null(item.Observacao);

        item.AtualizarDados(1m, "  linha 1\r\nlinha 2  ", 0m);
        Assert.Equal("linha 1\r\nlinha 2", item.Observacao);

        Assert.Throws<ArgumentException>(() => item.AtualizarDados(1m, new string('a', 1001), 0m));
        Assert.Equal("linha 1\r\nlinha 2", item.Observacao);
    }

    [Fact]
    public void U9_AtualizarDados_invalido_e_atomico()
    {
        var item = ItemFichaTecnica.Criar(1, 2, 3, 1m, "original");

        Assert.Throws<ArgumentException>(() => item.AtualizarDados(2m, new string('a', 1001), 0m));

        Assert.Equal(1m, item.Quantidade);
        Assert.Equal("original", item.Observacao);
    }

    [Fact]
    public void D1_D2_D3_Criacao_defaulta_aceita_e_valida_percentual_de_perda()
    {
        Assert.Equal(0m, ItemFichaTecnica.Criar(1, 2, 3, 1m).PercentualPerda);
        Assert.Equal(0.123456m, ItemFichaTecnica.Criar(1, 2, 3, 1m, percentualPerda: 0.123456m).PercentualPerda);
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemFichaTecnica.Criar(1, 2, 3, 1m, percentualPerda: -0.01m));
        Assert.Throws<ArgumentOutOfRangeException>(() => ItemFichaTecnica.Criar(1, 2, 3, 1m, percentualPerda: 1m));
    }

    [Fact]
    public void D4_D5_Edicao_altera_perda_e_permanece_atomica()
    {
        var item = ItemFichaTecnica.Criar(7, 11, 13, 1m, "original", 0.10m);

        item.AtualizarDados(2m, "alterada", 0.25m);
        Assert.Equal((7, 11, 13, 2m, "alterada", 0.25m), (item.EmpresaId, item.FichaTecnicaId, item.InsumoId, item.Quantidade, item.Observacao, item.PercentualPerda));

        Assert.Throws<ArgumentOutOfRangeException>(() => item.AtualizarDados(3m, "invalida", 1m));
        Assert.Equal((2m, "alterada", 0.25m), (item.Quantidade, item.Observacao, item.PercentualPerda));
    }
}
