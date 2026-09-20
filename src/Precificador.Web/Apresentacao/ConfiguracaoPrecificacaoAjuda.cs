namespace Precificador.Web.Apresentacao;

public static class ConfiguracaoPrecificacaoAjuda
{
    public const string PercentualMaoDeObra = "Percentual aplicado sobre o custo base dos insumos para compor o custo de mão de obra do lote. Ex.: insumos de R$ 12,00 e percentual de 10% geram R$ 1,20 de mão de obra. O percentual pode ser maior que 100% quando o trabalho artesanal tiver peso maior que os materiais.";

    public const string TarifaEnergiaKwh = "Valor pago por 1 kWh de energia. O sistema aplica essa tarifa ao consumo calculado a partir da potência e do tempo de uso dos equipamentos elétricos da Ficha Técnica. A tarifa só é necessária quando a Ficha possui equipamento elétrico; sem usos elétricos, o custo de energia é zero. Se houver uso elétrico e a tarifa não estiver configurada, o custo de energia fica indisponível.";

    public const string MargemPadrao = "Valor usado somente para pré-preencher a Margem-alvo ao cadastrar um novo Produto. O usuário pode alterar a margem antes de salvar, e Produtos já cadastrados não são modificados quando esta configuração muda.";

    public const string IncrementoComercial = "Define o múltiplo monetário usado para arredondar o Preço teórico para cima e formar o Preço sugerido. Ex.: com incremento de R$ 0,50, um Preço teórico de R$ 12,13 gera Preço sugerido de R$ 12,50. Se o Preço teórico já for um múltiplo exato, ele é mantido. Sem esta configuração, o Preço sugerido fica indisponível.";

    public const string ReservaComercial = "Não é desconto automático: representa o percentual do valor acima do Preço sugerido que fica reservado antes de calcular o Desconto de referência. Ex.: com reserva de 10%, o desconto só passa a ser aplicável quando o Preço de prateleira estiver pelo menos 11% acima do sugerido; a parcela que excede os 10% forma o Desconto de referência. Alterações valem para novos registros comerciais e não reinterpretam o histórico existente.";
}
