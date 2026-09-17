namespace Precificador.Web.Apresentacao;

public static class ConfiguracaoPrecificacaoAjuda
{
    public const string ValorHoraTrabalho = "Valor atribuído a uma hora de trabalho ativo. O sistema usa o tempo ativo da Ficha Técnica, convertido em horas, para calcular o custo de mão de obra do lote. Se houver tempo ativo e este valor não estiver configurado, o custo de mão de obra fica indisponível.";

    public const string TarifaEnergiaKwh = "Valor pago por 1 kWh de energia. O sistema aplica essa tarifa ao consumo calculado a partir da potência e do tempo de uso dos equipamentos da Ficha Técnica. Se houver uso de equipamento e a tarifa não estiver configurada, o custo de energia fica indisponível.";

    public const string MargemPadrao = "Valor usado somente para pré-preencher a Margem-alvo ao cadastrar um novo Produto. O usuário pode alterar a margem antes de salvar, e Produtos já cadastrados não são modificados quando esta configuração muda.";

    public const string IncrementoComercial = "Define o múltiplo monetário usado para arredondar o Preço teórico para cima e formar o Preço sugerido. Ex.: com incremento de R$ 0,50, um Preço teórico de R$ 12,13 gera Preço sugerido de R$ 12,50. Se o Preço teórico já for um múltiplo exato, ele é mantido. Sem esta configuração, o Preço sugerido fica indisponível.";

    public const string ReservaComercial = "Não é desconto automático: representa o percentual do valor acima do Preço sugerido que fica reservado antes de calcular o Desconto de referência. Ex.: com reserva de 10%, o desconto só passa a ser aplicável quando o Preço de prateleira estiver pelo menos 11% acima do sugerido; a parcela que excede os 10% forma o Desconto de referência. Alterações valem para novos registros comerciais e não reinterpretam o histórico existente.";
}