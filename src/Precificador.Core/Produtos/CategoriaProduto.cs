using System.Text.RegularExpressions;
using Precificador.Core.Empresas;

namespace Precificador.Core.Produtos;

public sealed class CategoriaProduto : IEntidadeEmpresa
{
    private const int TamanhoMaximoNome = 80;

    private CategoriaProduto()
    {
        Nome = null!;
        NomeNormalizado = null!;
    }

    private CategoriaProduto(int empresaId, string nome, FormaCalculoDesgasteEquipamento formaCalculoDesgasteEquipamento, decimal valorDesgasteEquipamento)
    {
        DefinirEmpresa(empresaId);
        var nomeNormalizado = NormalizarNome(nome);
        ValidarNome(nomeNormalizado);

        Nome = nomeNormalizado;
        NomeNormalizado = nomeNormalizado.ToUpperInvariant();
        ValidarDesgaste(formaCalculoDesgasteEquipamento, valorDesgasteEquipamento);
        FormaCalculoDesgasteEquipamento = formaCalculoDesgasteEquipamento;
        ValorDesgasteEquipamento = valorDesgasteEquipamento;
        Ativo = true;
    }

    public int Id { get; private set; }

    public int EmpresaId { get; private set; }

    public string Nome { get; private set; }

    public string NomeNormalizado { get; private set; }

    public bool Ativo { get; private set; }

    public FormaCalculoDesgasteEquipamento FormaCalculoDesgasteEquipamento { get; private set; }

    public decimal ValorDesgasteEquipamento { get; private set; }

    public static CategoriaProduto Criar(int empresaId, string nome, FormaCalculoDesgasteEquipamento formaCalculoDesgasteEquipamento, decimal valorDesgasteEquipamento) => new(empresaId, nome, formaCalculoDesgasteEquipamento, valorDesgasteEquipamento);

    public void Renomear(string nome)
    {
        var nomeNormalizado = NormalizarNome(nome);
        ValidarNome(nomeNormalizado);

        Nome = nomeNormalizado;
        NomeNormalizado = nomeNormalizado.ToUpperInvariant();
    }

    public void AtualizarDados(string nome, FormaCalculoDesgasteEquipamento formaCalculoDesgasteEquipamento, decimal valorDesgasteEquipamento)
    {
        var nomeNormalizado = NormalizarNome(nome);
        ValidarNome(nomeNormalizado);
        ValidarDesgaste(formaCalculoDesgasteEquipamento, valorDesgasteEquipamento);
        Nome = nomeNormalizado;
        NomeNormalizado = nomeNormalizado.ToUpperInvariant();
        FormaCalculoDesgasteEquipamento = formaCalculoDesgasteEquipamento;
        ValorDesgasteEquipamento = valorDesgasteEquipamento;
    }

    public void Desativar() => Ativo = false;

    public void Reativar() => Ativo = true;

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(empresaId));
        }

        if (EmpresaId != 0 && EmpresaId != empresaId)
        {
            throw new InvalidOperationException("A categoria de produto já pertence a outra empresa.");
        }

        EmpresaId = empresaId;
    }

    private static string NormalizarNome(string? nome) =>
        Regex.Replace(nome?.Trim() ?? string.Empty, @"\s+", " ");

    private static void ValidarNome(string nome)
    {
        if (string.IsNullOrWhiteSpace(nome))
        {
            throw new ArgumentException("O nome da categoria é obrigatório.", nameof(nome));
        }

        if (nome.Length > TamanhoMaximoNome)
        {
            throw new ArgumentException("O nome da categoria deve possuir no máximo 80 caracteres.", nameof(nome));
        }
    }

    private static void ValidarDesgaste(FormaCalculoDesgasteEquipamento forma, decimal valor)
    {
        if (!Enum.IsDefined(forma)) throw new ArgumentOutOfRangeException(nameof(forma));
        if (valor < 0m) throw new ArgumentOutOfRangeException(nameof(valor));
    }
}
