using System.ComponentModel.DataAnnotations;

namespace Precificador.Web.Pages.Configuracoes.Precificacao;

public sealed class ConfiguracaoPrecificacaoInputModel
{
    [Display(Name = "Mão de obra sobre os insumos (%)")]
    public string? PercentualMaoDeObra { get; set; }

    [Display(Name = "Tarifa de energia (R$/kWh)")]
    public string? TarifaEnergiaKwh { get; set; }

    [Display(Name = "Margem padrão para novos produtos (%)")]
    public string? MargemPadraoPercentual { get; set; }

    [Display(Name = "Incremento comercial de arredondamento")]
    public string? IncrementoComercial { get; set; }

    [Display(Name = "Reserva comercial para desconto (%)")]
    public string? ReservaComercialDescontoPercentual { get; set; }
}
