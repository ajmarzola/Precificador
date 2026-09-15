using System.Text.RegularExpressions;
using Precificador.Core.Empresas;

namespace Precificador.Core.FichasTecnicas;

public sealed class UsoEquipamentoFicha : IEntidadeEmpresa
{
    private const int TamanhoMaximoNome = 120;

    private UsoEquipamentoFicha() { }

    private UsoEquipamentoFicha(int empresaId, int fichaTecnicaId, string nomeEquipamento, decimal potenciaKw, int tempoUsoMinutos)
    {
        ValidarIds(empresaId, fichaTecnicaId);
        var nomeNormalizado = NormalizarNome(nomeEquipamento);
        Validar(nomeNormalizado, potenciaKw, tempoUsoMinutos);
        EmpresaId = empresaId;
        FichaTecnicaId = fichaTecnicaId;
        NomeEquipamento = nomeNormalizado;
        NomeEquipamentoNormalizado = nomeNormalizado.ToUpperInvariant();
        PotenciaKw = potenciaKw;
        TempoUsoMinutos = tempoUsoMinutos;
    }

    public int Id { get; private set; }
    public int EmpresaId { get; private set; }
    public int FichaTecnicaId { get; private set; }
    public string NomeEquipamento { get; private set; } = null!;
    public string NomeEquipamentoNormalizado { get; private set; } = null!;
    public decimal PotenciaKw { get; private set; }
    public int TempoUsoMinutos { get; private set; }

    public static UsoEquipamentoFicha Criar(int empresaId, int fichaTecnicaId, string nomeEquipamento, decimal potenciaKw, int tempoUsoMinutos) =>
        new(empresaId, fichaTecnicaId, nomeEquipamento, potenciaKw, tempoUsoMinutos);

    public void AtualizarDados(string nomeEquipamento, decimal potenciaKw, int tempoUsoMinutos)
    {
        var nomeNormalizado = NormalizarNome(nomeEquipamento);
        Validar(nomeNormalizado, potenciaKw, tempoUsoMinutos);
        NomeEquipamento = nomeNormalizado;
        NomeEquipamentoNormalizado = nomeNormalizado.ToUpperInvariant();
        PotenciaKw = potenciaKw;
        TempoUsoMinutos = tempoUsoMinutos;
    }

    public void DefinirEmpresa(int empresaId)
    {
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (EmpresaId != 0 && EmpresaId != empresaId)
            throw new InvalidOperationException("O uso de equipamento já pertence a outra empresa.");
        EmpresaId = empresaId;
    }

    private static void ValidarIds(int empresaId, int fichaTecnicaId)
    {
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        if (fichaTecnicaId <= 0) throw new ArgumentOutOfRangeException(nameof(fichaTecnicaId));
    }

    private static string NormalizarNome(string nomeEquipamento) => Regex.Replace(nomeEquipamento?.Trim() ?? string.Empty, @"\s+", " ");

    private static void Validar(string nomeEquipamento, decimal potenciaKw, int tempoUsoMinutos)
    {
        if (string.IsNullOrEmpty(nomeEquipamento)) throw new ArgumentException("O nome do equipamento é obrigatório.", nameof(nomeEquipamento));
        if (nomeEquipamento.Length > TamanhoMaximoNome) throw new ArgumentException("O nome do equipamento deve possuir no máximo 120 caracteres.", nameof(nomeEquipamento));
        if (potenciaKw <= 0) throw new ArgumentOutOfRangeException(nameof(potenciaKw), "A potência deve ser maior que zero.");
        if (tempoUsoMinutos <= 0) throw new ArgumentOutOfRangeException(nameof(tempoUsoMinutos), "O tempo de uso deve ser maior que zero.");
    }
}
