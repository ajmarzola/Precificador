using Precificador.Core.Precificacao;

namespace Precificador.Web.Apresentacao;

public static class PrecificacaoRotulos
{
    public static string MotivoIncompleto(MotivoPrecificacaoIncompleta motivo) => motivo switch
    {
        MotivoPrecificacaoIncompleta.FichaTecnicaAusente => "Ficha técnica não cadastrada",
        MotivoPrecificacaoIncompleta.FichaTecnicaSemItens => "Ficha técnica sem itens",
        MotivoPrecificacaoIncompleta.InsumoSemPrecoVigente => "Insumo sem preço vigente",
        MotivoPrecificacaoIncompleta.ConfiguracaoPrecificacaoAusente => "Configuração de precificação não encontrada",
        MotivoPrecificacaoIncompleta.TarifaEnergiaNaoConfigurada => "Tarifa de energia não configurada",
        MotivoPrecificacaoIncompleta.IncrementoComercialNaoConfigurado => "Incremento comercial não configurado",
        MotivoPrecificacaoIncompleta.PrecoPrateleiraNaoDefinido => "Preço de prateleira não definido",
        _ => throw new ArgumentOutOfRangeException(nameof(motivo))
    };
}
