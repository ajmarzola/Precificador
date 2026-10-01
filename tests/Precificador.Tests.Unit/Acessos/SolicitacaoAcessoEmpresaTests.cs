using Precificador.Core.Acessos;
using Precificador.Core.Empresas;

namespace Precificador.Tests.Unit.Acessos;

public sealed class SolicitacaoAcessoEmpresaTests
{
    [Theory]
    [InlineData("  Minha   Empresa  ")]
    [InlineData("\tMinha\nEmpresa\u00a0")]
    [InlineData("Émpresa Ágil")]
    public void Normalizacao_equivale_a_empresa_e_preserva_dados_informativos(string nome)
    {
        var data = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.FromHours(-3));
        var pedido = SolicitacaoAcessoEmpresa.Criar(nome, "  João\t  Silva ", "  Joao@Teste.local ", "  linha 1\nlinha 2  ", data);
        var empresa = Empresa.Criar(nome);
        Assert.Equal(empresa.Nome, pedido.NomeEmpresa);
        Assert.Equal(empresa.NomeNormalizado, pedido.NomeEmpresaNormalizado);
        Assert.Equal("João Silva", pedido.NomeResponsavel);
        Assert.Equal("Joao@Teste.local", pedido.EmailResponsavel);
        Assert.Equal("JOAO@TESTE.LOCAL", pedido.EmailResponsavelNormalizado);
        Assert.Equal("linha 1\nlinha 2", pedido.Observacao);
        Assert.Equal(data.ToUniversalTime(), pedido.DataSolicitacaoUtc);
        Assert.Equal(TimeSpan.Zero, pedido.DataSolicitacaoUtc.Offset);
        Assert.Equal(SituacaoSolicitacaoAcessoEmpresa.Pendente, pedido.Situacao);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \r\n\t ")]
    public void Observacao_vazia_vira_null(string? observacao) =>
        Assert.Null(Criar(observacao: observacao).Observacao);

    [Theory]
    [InlineData("nomeEmpresa", " ")]
    [InlineData("nomeResponsavel", " ")]
    [InlineData("emailResponsavel", " ")]
    [InlineData("emailResponsavel", "invalido")]
    public void Dados_obrigatorios_invalidos_sao_rejeitados(string campo, string valor)
    {
        var exception = Assert.Throws<ArgumentException>(() => Criar(
            nome: campo == "nomeEmpresa" ? valor : "Empresa",
            responsavel: campo == "nomeResponsavel" ? valor : "João",
            email: campo == "emailResponsavel" ? valor : "joao@teste.local"));
        Assert.Equal(campo, exception.ParamName);
    }

    [Theory]
    [InlineData("nomeEmpresa", 120)]
    [InlineData("nomeResponsavel", 120)]
    [InlineData("emailResponsavel", 256)]
    [InlineData("observacao", 1000)]
    public void Limites_sao_aplicados_apos_normalizacao(string campo, int limite)
    {
        string Valor(int tamanho) => campo == "emailResponsavel" ? new string('a', tamanho - 8) + "@t.local" : new string('a', tamanho);
        SolicitacaoAcessoEmpresa Pedido(int tamanho) => Criar(
            nome: campo == "nomeEmpresa" ? "  " + Valor(tamanho) + "  " : "Empresa",
            responsavel: campo == "nomeResponsavel" ? Valor(tamanho) : "João",
            email: campo == "emailResponsavel" ? " " + Valor(tamanho) + " " : "joao@teste.local",
            observacao: campo == "observacao" ? Valor(tamanho) : null);
        Assert.NotNull(Pedido(limite));
        Assert.Equal(campo, Assert.Throws<ArgumentException>(() => Pedido(limite + 1)).ParamName);
    }

    [Fact]
    public void Nome_bruto_acima_do_limite_e_valido_se_normalizado_cabe()
    {
        var nome = "   " + new string('a', 120) + "   ";
        Assert.Equal(Empresa.Criar(nome).Nome, Criar(nome: nome).NomeEmpresa);
    }

    [Fact]
    public void Enum_tem_valores_estaveis()
    {
        Assert.Equal(1, (int)SituacaoSolicitacaoAcessoEmpresa.Pendente);
        Assert.Equal(2, (int)SituacaoSolicitacaoAcessoEmpresa.Aprovada);
        Assert.Equal(3, (int)SituacaoSolicitacaoAcessoEmpresa.Recusada);
    }

    private static SolicitacaoAcessoEmpresa Criar(string nome = "Empresa", string responsavel = "João",
        string email = "joao@teste.local", string? observacao = null) =>
        SolicitacaoAcessoEmpresa.Criar(nome, responsavel, email, observacao, DateTimeOffset.Parse("2026-10-01T12:00:00Z"));
}
