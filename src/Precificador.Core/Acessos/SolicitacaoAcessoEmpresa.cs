using System.ComponentModel.DataAnnotations;
using Precificador.Core.Empresas;

namespace Precificador.Core.Acessos;

public sealed class SolicitacaoAcessoEmpresa
{
    private SolicitacaoAcessoEmpresa() { }

    public int Id { get; private set; }
    public string NomeEmpresa { get; private set; } = null!;
    public string NomeEmpresaNormalizado { get; private set; } = null!;
    public string NomeResponsavel { get; private set; } = null!;
    public string EmailResponsavel { get; private set; } = null!;
    public string EmailResponsavelNormalizado { get; private set; } = null!;
    public string? Observacao { get; private set; }
    public DateTimeOffset DataSolicitacaoUtc { get; private set; }
    public SituacaoSolicitacaoAcessoEmpresa Situacao { get; private set; }
    public int? EmpresaId { get; private set; }
    public DateTimeOffset? DataDecisaoUtc { get; private set; }
    public string? DecididaPorUsuarioId { get; private set; }
    public string? MotivoRecusa { get; private set; }

    public void Aprovar(int empresaId, string decididaPorUsuarioId, DateTimeOffset dataUtc)
    {
        ValidarDecisao(decididaPorUsuarioId);
        if (empresaId <= 0) throw new ArgumentOutOfRangeException(nameof(empresaId));
        EmpresaId = empresaId;
        DecididaPorUsuarioId = decididaPorUsuarioId;
        DataDecisaoUtc = dataUtc.ToUniversalTime();
        Situacao = SituacaoSolicitacaoAcessoEmpresa.Aprovada;
    }

    public void Recusar(string decididaPorUsuarioId, DateTimeOffset dataUtc, string? motivo)
    {
        ValidarDecisao(decididaPorUsuarioId);
        var normalizado = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim();
        if (normalizado?.Length > 500) throw new ArgumentException("O motivo deve possuir no máximo 500 caracteres.", nameof(motivo));
        MotivoRecusa = normalizado;
        DecididaPorUsuarioId = decididaPorUsuarioId;
        DataDecisaoUtc = dataUtc.ToUniversalTime();
        Situacao = SituacaoSolicitacaoAcessoEmpresa.Recusada;
    }

    private void ValidarDecisao(string usuarioId)
    {
        if (Situacao != SituacaoSolicitacaoAcessoEmpresa.Pendente) throw new InvalidOperationException("A solicitação já foi decidida.");
        if (string.IsNullOrWhiteSpace(usuarioId) || usuarioId.Length > 450) throw new ArgumentException("O usuário da decisão é obrigatório.", nameof(usuarioId));
    }

    public static SolicitacaoAcessoEmpresa Criar(string? nomeEmpresa, string? nomeResponsavel,
        string? emailResponsavel, string? observacao, DateTimeOffset dataSolicitacaoUtc)
    {
        var nome = Validar(NormalizadorNome.Normalizar(nomeEmpresa), 120, nameof(nomeEmpresa), "Nome da Empresa");
        var responsavel = Validar(NormalizadorNome.Normalizar(nomeResponsavel), 120, nameof(nomeResponsavel), "Nome do responsável");
        var email = Validar(emailResponsavel?.Trim() ?? "", 256, nameof(emailResponsavel), "E-mail");
        if (!new EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("Informe um e-mail válido.", nameof(emailResponsavel));
        var nota = string.IsNullOrWhiteSpace(observacao) ? null : observacao.Trim();
        if (nota?.Length > 1000)
            throw new ArgumentException("A Observação deve possuir no máximo 1000 caracteres.", nameof(observacao));
        return new SolicitacaoAcessoEmpresa
        {
            NomeEmpresa = nome, NomeEmpresaNormalizado = nome.ToUpperInvariant(),
            NomeResponsavel = responsavel, EmailResponsavel = email,
            EmailResponsavelNormalizado = email.ToUpperInvariant(), Observacao = nota,
            DataSolicitacaoUtc = dataSolicitacaoUtc.ToUniversalTime(), Situacao = SituacaoSolicitacaoAcessoEmpresa.Pendente
        };
    }

    private static string Validar(string valor, int limite, string parametro, string rotulo)
    {
        if (string.IsNullOrWhiteSpace(valor)) throw new ArgumentException($"{rotulo} é obrigatório.", parametro);
        if (valor.Length > limite) throw new ArgumentException($"{rotulo} deve possuir no máximo {limite} caracteres.", parametro);
        return valor;
    }
}
