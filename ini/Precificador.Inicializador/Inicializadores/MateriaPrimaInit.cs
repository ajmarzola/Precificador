using Precificador.Application.Model;
using Precificador.Domain.Filters;
using Precificador.Inicializador.Base;
using Precificador.Inicializador.Services;
using RestSharp;
using System.Text.Json;

namespace Precificador.Inicializador.Inicializadores
{
    public class MateriaPrimaInit : BaseInit<MateriaPrima, NomeFilter>
    {
        protected override string Endpoint => "MateriaPrima";

        protected override IEnumerable<MateriaPrima> Items => MateriasPrimas;

        private static IEnumerable<MateriaPrima> MateriasPrimas =>
        [
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fios"), Nome = "Barroco MultiColor 200g", QtdPacote = 200, VlrPacote = 18.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.09m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Bolinha 6,5", QtdPacote = 100, VlrPacote = 104.49m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.04m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Cordão Dourado", QtdPacote = 50, VlrPacote = 7.49m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.15m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fios"), Nome = "Cordão Encerado Settanyl", QtdPacote = 100, VlrPacote = 15.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.16m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Elastico", QtdPacote = 10, VlrPacote = 6.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.69m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fios"), Nome = "Fio Clea 1000 160g", QtdPacote = 160, VlrPacote = 16.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.11m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fitas"), Nome = "Fita 7mm", QtdPacote = 100, VlrPacote = 9.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.10m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fitas"), Nome = "Fita cetim", QtdPacote = 100, VlrPacote = 12.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.13m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Ilhos", QtdPacote = 100, VlrPacote = 4.89m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.05m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Pano de Prato Pé de Galinha", QtdPacote = 1, VlrPacote = 4.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 4.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Pano de Prato Atoalhado", QtdPacote = 3, VlrPacote = 21.09m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 7.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Desgaste"), Nome = "Desgaste Agenda", QtdPacote = 1, VlrPacote = 1.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Desgaste"), Nome = "Desgaste Bloquinho/Outros", QtdPacote = 1, VlrPacote = 0.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Desgaste"), Nome = "Desgaste Caderno A5", QtdPacote = 1, VlrPacote = 2.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 2.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Desgaste"), Nome = "Desgaste Planner/Album/Caderno Universitario", QtdPacote = 1, VlrPacote = 3.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 3.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Embalagem"), Nome = "Embalagem Padrão", QtdPacote = 1, VlrPacote = 1.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Espiral"), Nome = "Espiral 17mm", QtdPacote = 50, VlrPacote = 25.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.52m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Espiral"), Nome = "Espiral 29mm", QtdPacote = 35, VlrPacote = 63.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.83m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Espiral"), Nome = "Espiral 33mm", QtdPacote = 30, VlrPacote = 119.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 4.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Bolso A4", QtdPacote = 1, VlrPacote = 1.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Capa A4", QtdPacote = 1, VlrPacote = 0.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Capa A5", QtdPacote = 1, VlrPacote = 0.25m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.25m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Capa A6", QtdPacote = 1, VlrPacote = 0.13m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.13m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Marcador Frente Verso 1/4 A4", QtdPacote = 1, VlrPacote = 0.25m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.25m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Miolo Frente A5", QtdPacote = 1, VlrPacote = 0.03m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Miolo Frente A6", QtdPacote = 1, VlrPacote = 0.02m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Miolo Frente Verso A4", QtdPacote = 1, VlrPacote = 0.12m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.12m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Miolo Frente Verso A5", QtdPacote = 1, VlrPacote = 0.06m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.06m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Miolo Frente Verso A6", QtdPacote = 1, VlrPacote = 0.03m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Laminação e Plastificação"), Nome = "bopp", QtdPacote = 100, VlrPacote = 44.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.45m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Laminação e Plastificação"), Nome = "Polaseal A4 Divisórias", QtdPacote = 20, VlrPacote = 1.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.09m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Laminação e Plastificação"), Nome = "Polaseal marcador 1/4 A4", QtdPacote = 80, VlrPacote = 1.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha 1/4 A4"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "Bolsooffset 180gr", QtdPacote = 100, VlrPacote = 31.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.32m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "divisórias offset 180gr", QtdPacote = 100, VlrPacote = 31.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.32m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "miolo 180g A5", QtdPacote = 200, VlrPacote = 31.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.16m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "miolo 180gr A4", QtdPacote = 100, VlrPacote = 31.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.32m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 240g"), Nome = "Papel 240g A4", QtdPacote = 100, VlrPacote = 42.20m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.42m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "miolo 75gr A4", QtdPacote = 500, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.06m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "miolo 75gr A5", QtdPacote = 1000, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "Sulfite 75g A4", QtdPacote = 500, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.06m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "Sulfite 75g A5", QtdPacote = 1000, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 90g"), Nome = "Marcador 90g A6", QtdPacote = 2000, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A6"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 90g"), Nome = "miolo 90gr A4 dobrado no meio", QtdPacote = 500, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.10m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 90g"), Nome = "miolo 90gr A5", QtdPacote = 1000, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.05m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 90g"), Nome = "miolo 90gr A5 14x18", QtdPacote = 1000, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.05m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda 14,5x18 offset adesivo", QtdPacote = 40, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda 15,5x21,5 offset adesivo", QtdPacote = 40, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda 16,5x21,5 offset adesivo", QtdPacote = 40, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda 20x20 offset adesivo", QtdPacote = 40, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel offset adesivo + guarda", QtdPacote = 40, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel offset adesivo A4", QtdPacote = 40, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Fotográfico"), Nome = "Papel fotografico A4", QtdPacote = 20, VlrPacote = 15.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.80m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Kraft"), Nome = "Papel Kraft A4", QtdPacote = 30, VlrPacote = 16.10m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.54m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Kraft"), Nome = "Papel Kraft A5", QtdPacote = 60, VlrPacote = 16.10m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.27m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 15,5x21,5+2,5x21,5", QtdPacote = 20, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 15x18,5", QtdPacote = 20, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 16x22", QtdPacote = 20, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 17x22", QtdPacote = 20, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 7,7x8,5+7,7x2,5", QtdPacote = 40, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 9,5x10,5+9,5x2", QtdPacote = 20, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Frete", QtdPacote = 1, VlrPacote = 0.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Miolo Frente A4", QtdPacote = 1, VlrPacote = 0.06m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.06m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Divisória A4", QtdPacote = 1, VlrPacote = 1.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão A4", QtdPacote = 20, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda offset adesivo A4", QtdPacote = 40, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 10,5x7,5", QtdPacote = 80, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.37m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "miolo 75gr A7", QtdPacote = 4000, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.01m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Espiral"), Nome = "Espiral 20mm", QtdPacote = 50, VlrPacote = 32.20m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.64m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Ima 3x15 (1mx0,62m)", QtdPacote = 1, VlrPacote = 32.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 32.90m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "caneta quadro branco", QtdPacote = 1, VlrPacote = 1.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.70m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 19x27", QtdPacote = 20, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda 18,5x26,5 offset adesivo", QtdPacote = 40, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "miolo 75gr 18x26", QtdPacote = 500, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.06m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Plástico Cristal 1mx1,4m", QtdPacote = 1, VlrPacote = 14.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 14.90m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Tricoline"), Nome = "Tecido Tricoline 1mx1,4m trevo", QtdPacote = 1, VlrPacote = 33.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 33.80m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Botão de Pressão", QtdPacote = 200, VlrPacote = 14.15m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Caixa"), VlrUnitario = 0.07m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Manta R2 1mx1,4m", QtdPacote = 1, VlrPacote = 27.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 27.80m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Vies 20m", QtdPacote = 20, VlrPacote = 6.35m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.32m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Linha Dinner", QtdPacote = 3600, VlrPacote = 6.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.10m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Etiqueta Cetim 1,5 10m", QtdPacote = 10, VlrPacote = 2.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.25m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Transfer"), Nome = "Papel Transfer", QtdPacote = 10, VlrPacote = 54.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 5.49m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressão A4", QtdPacote = 1, VlrPacote = 0.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "Papel 180gr A4", QtdPacote = 100, VlrPacote = 31.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.32m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Alça - Fio Nautico circulo branco", QtdPacote = 208, VlrPacote = 28.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.14m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "Papel 180gr A4 Reciclado", QtdPacote = 100, VlrPacote = 25.10m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.25m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Alça - Cordão de Algodão", QtdPacote = 50, VlrPacote = 25.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.52m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Linha Circulo Maxi Mouline", QtdPacote = 8, VlrPacote = 1.40m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.18m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Caderno"), Nome = "Caderno Brochura Foroni A5", QtdPacote = 1, VlrPacote = 4.10m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 4.10m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Contact"), Nome = "Contact 200x45", QtdPacote = 1, VlrPacote = 13.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 15.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("EVA"), Nome = "EVA 40x47", QtdPacote = 10, VlrPacote = 11.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.19m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 12x12+12x12+12x1", QtdPacote = 20, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 90g"), Nome = "Sulfite 90gr 11,5x11,5", QtdPacote = 1000, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.05m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Cola Branca 1kg", QtdPacote = 1000, VlrPacote = 35.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.04m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Linha Urso", QtdPacote = 183, VlrPacote = 42.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.23m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 11x15,5", QtdPacote = 40, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda 11x14,5 offset adesivo", QtdPacote = 80, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.37m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "Sulfite 75g A6", QtdPacote = 2000, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A6"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Color Plus"), Nome = "Color Plus Guarda A4", QtdPacote = 9, VlrPacote = 5.59m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.62m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "Papel 180gr A6", QtdPacote = 400, VlrPacote = 31.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.08m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Máscara Facial de Limpeza", QtdPacote = 1, VlrPacote = 1.02m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Doces"), Nome = "Chocolate Talento 25g", QtdPacote = 15, VlrPacote = 26.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Caixa"), VlrUnitario = 1.77m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Doces"), Nome = "Goma de Mascar Trident 168g", QtdPacote = 21, VlrPacote = 30.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Caixa"), VlrUnitario = 1.48m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Barroco Natural 8 700g", QtdPacote = 700, VlrPacote = 18.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Fio Anne 500 147g", QtdPacote = 147, VlrPacote = 18.06m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.12m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "Papel 180gr Capa 6x6", QtdPacote = 400, VlrPacote = 31.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.08m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Embalagem"), Nome = "Embalagem Atacado", QtdPacote = 1, VlrPacote = 0.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Pró-labore"), Nome = "Pró-Labore Priscilla por Hora", QtdPacote = 1, VlrPacote = 6.67m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 6.67m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Custos Fixos"), Nome = "Custos Fixos por Hora", QtdPacote = 1, VlrPacote = 3.61m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 3.61m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Doces"), Nome = "Chocolate Baton 16g", QtdPacote = 30, VlrPacote = 27.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Caixa"), VlrUnitario = 0.93m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 180g"), Nome = "Card OffSet 180gr", QtdPacote = 600, VlrPacote = 31.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.05m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Card", QtdPacote = 6, VlrPacote = 0.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.08m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Doces"), Nome = "Bon Bon Arcor 15g", QtdPacote = 50, VlrPacote = 31.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Pacote"), VlrUnitario = 0.64m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Doces"), Nome = "Sonho de Valsa 15g", QtdPacote = 48, VlrPacote = 55.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Pacote"), VlrUnitario = 1.17m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Doces"), Nome = "Moranguete 13g", QtdPacote = 160, VlrPacote = 61.65m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Pacote"), VlrUnitario = 0.39m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Envelopes"), Nome = "Envelope Canguru 15x13", QtdPacote = 10, VlrPacote = 13.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Pacote"), VlrUnitario = 1.39m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Envelopes"), Nome = "Envelope Canguru 15x21", QtdPacote = 10, VlrPacote = 19.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Pacote"), VlrUnitario = 1.97m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Offset 90g"), Nome = "Sulfite 90gr", QtdPacote = 500, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.10m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Plástico Cristal"), Nome = "Plástico Alclear Light Translucido", QtdPacote = 1, VlrPacote = 26.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 26.90m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Espiral"), Nome = "Espiral 33mm plastico", QtdPacote = 25, VlrPacote = 61.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 2.48m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fitas"), Nome = "Fita cetim", QtdPacote = 100, VlrPacote = 12.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.13m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "miolo 75gr A6", QtdPacote = 2000, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A6"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 15,5x11", QtdPacote = 40, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A6"), VlrUnitario = 0.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda 11x15 offset adesivo", QtdPacote = 200, VlrPacote = 69.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.35m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Manta R1 1mx1,4m", QtdPacote = 1, VlrPacote = 21.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 21.90m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Tricoline"), Nome = "Tecido Tricoline 1mx1,4m", QtdPacote = 1, VlrPacote = 31.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 31.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Sabonete Natura com 5", QtdPacote = 5, VlrPacote = 27.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Caixa"), VlrUnitario = 5.58m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 7,5 cm x 3 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "miolo 75gr 3cm x 5 cm", QtdPacote = 5000, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha"), VlrUnitario = 0.01m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Cordão cetim", QtdPacote = 50, VlrPacote = 10.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.21m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Tira couro fechamento", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Argola para Chaveiro", QtdPacote = 50, VlrPacote = 5.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.10m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Kit Bolachas (1 Torrada, 1 Maisena, 1 Creem Cracker, 1 Chocolate, 1 Leite, 1 Gotas, 1 Banana com Canela)", QtdPacote = 10, VlrPacote = 44.09m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 4.41m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Suco Caixinha Chefa 200ml", QtdPacote = 1, VlrPacote = 1.19m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.19m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Açucar União Sache 5g", QtdPacote = 80, VlrPacote = 25.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.32m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Café Pelé Sache", QtdPacote = 100, VlrPacote = 42.51m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.43m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Capuccino 3 Corações Sache", QtdPacote = 10, VlrPacote = 19.89m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.99m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Geleia Blister 15g", QtdPacote = 6, VlrPacote = 3.95m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.66m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Envelopes"), Nome = "Envelope 80g visita 115x80 linho branco 5452 Romitec", QtdPacote = 100, VlrPacote = 27.40m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Pacote"), VlrUnitario = 0.27m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 120g"), Nome = "Papel Offset Sulfite 120g A4", QtdPacote = 125, VlrPacote = 13.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.11m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 120g"), Nome = "Papel Offset Sulfite 120g A5", QtdPacote = 250, VlrPacote = 13.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.05m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 120g"), Nome = "Papel Offset Sulfite 120g A6", QtdPacote = 500, VlrPacote = 13.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A6"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 90g"), Nome = "miolo 90gr A6", QtdPacote = 2000, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A6"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Kraft"), Nome = "Papel Kraft A6", QtdPacote = 120, VlrPacote = 16.10m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.13m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Lixa de Unha", QtdPacote = 100, VlrPacote = 6.49m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.06m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Kraft"), Nome = "Papel Kraft 20x5", QtdPacote = 180, VlrPacote = 16.10m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.09m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Fralda Cremer 70x70", QtdPacote = 5, VlrPacote = 35.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 7.18m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Toalha Cremer 120x70", QtdPacote = 3, VlrPacote = 34.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 11.63m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Couro 11 cm x 15 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Miolo Frente Verso A7", QtdPacote = 1, VlrPacote = 0.02m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Kraft"), Nome = "Papel Kraft A7", QtdPacote = 240, VlrPacote = 16.10m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.07m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "Sulfite 75g A8", QtdPacote = 8000, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A6"), VlrUnitario = 0.01m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Chá Twinings Frutas Silvestres", QtdPacote = 10, VlrPacote = 16.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.69m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Color Plus"), Nome = "Color Plus 20x5", QtdPacote = 54, VlrPacote = 5.59m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.10m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Tassel", QtdPacote = 100, VlrPacote = 37.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.38m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelaria"), Nome = "Lápis de Cor 12 cores meio lápis redondo HT 11.1702 Happy-time", QtdPacote = 1, VlrPacote = 3.30m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Caixa"), VlrUnitario = 3.30m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Elastico roliço", QtdPacote = 10, VlrPacote = 4.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.40m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Caneta decorada", QtdPacote = 12, VlrPacote = 21.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.75m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Lapis infinito", QtdPacote = 24, VlrPacote = 70.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 2.95m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Miçangas e Pedrarias"), Nome = "Balão de acrilico", QtdPacote = 25, VlrPacote = 4.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.16m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Miçangas e Pedrarias"), Nome = "Miçanga Jablonex", QtdPacote = 25, VlrPacote = 11.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.47m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Miçangas e Pedrarias"), Nome = "Cristal Plastico", QtdPacote = 25, VlrPacote = 4.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.16m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Miçangas e Pedrarias"), Nome = "Conta Plastica Florzinha", QtdPacote = 25, VlrPacote = 4.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.19m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Medalhas e Pingentes"), Nome = "Entermeio Nossa Senhora Aparecida 1,9x1,5", QtdPacote = 5, VlrPacote = 8.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.74m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Medalhas e Pingentes"), Nome = "Entermeio Nossa Senhora Aparecida 1,5x1,1", QtdPacote = 10, VlrPacote = 9.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.97m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Medalhas e Pingentes"), Nome = "Entermeio Medalha de São Bento 1,5x1,5", QtdPacote = 6, VlrPacote = 5.70m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.95m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Medalhas e Pingentes"), Nome = "Cruz e Entremeio emborrachado", QtdPacote = 1, VlrPacote = 5.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 5.90m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Medalhas e Pingentes"), Nome = "Cruxifixo Metal Niquel", QtdPacote = 10, VlrPacote = 8.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.85m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Medalhas e Pingentes"), Nome = "Cruz Face de Cristo 3,3x2", QtdPacote = 5, VlrPacote = 6.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.20m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Metais"), Nome = "Contrapino", QtdPacote = 226, VlrPacote = 3.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Metais"), Nome = "Triangulo", QtdPacote = 25, VlrPacote = 5.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.23m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Metais"), Nome = "Argola", QtdPacote = 25, VlrPacote = 3.60m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.14m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Acrilico"), Nome = "Caixa Acrilica 5x5", QtdPacote = 10, VlrPacote = 10.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 1.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Feltro"), Nome = "Feltro 10x5", QtdPacote = 140, VlrPacote = 17.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.13m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("EVA"), Nome = "Pétala EVA 10cm", QtdPacote = 160, VlrPacote = 11.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.07m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("EVA"), Nome = "Folha EVA 8cm", QtdPacote = 420, VlrPacote = 11.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("EVA"), Nome = "Pétala EVA 9cm", QtdPacote = 240, VlrPacote = 11.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.05m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Suporte Balão", QtdPacote = 10, VlrPacote = 3.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.35m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fitas"), Nome = "Fita Floral 12mm", QtdPacote = 30, VlrPacote = 4.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.15m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Saquinho Flor", QtdPacote = 100, VlrPacote = 23.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.24m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fitas"), Nome = "Fitilho Plástico 05mm", QtdPacote = 50, VlrPacote = 2.75m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.06m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Vidro 100 ml", QtdPacote = 1, VlrPacote = 3.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 3.90m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Vareta 18cm", QtdPacote = 100, VlrPacote = 7.40m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.07m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Vareta com Flor", QtdPacote = 1, VlrPacote = 0.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.99m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Tampa Plastica", QtdPacote = 10, VlrPacote = 9.20m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.92m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Alcool de Cereais", QtdPacote = 1, VlrPacote = 9.16m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Litro"), VlrUnitario = 9.16m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Essencia", QtdPacote = 100, VlrPacote = 16.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("mililitro"), VlrUnitario = 0.17m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Agua Mineral", QtdPacote = 2, VlrPacote = 2.59m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Litro"), VlrUnitario = 1.73m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Sal Grosso", QtdPacote = 2000, VlrPacote = 4.34m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.00m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Caixa com visor", QtdPacote = 10, VlrPacote = 28.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 2.85m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Post It"), Nome = "Post It 40x50 100 folhas", QtdPacote = 12, VlrPacote = 32.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 2.71m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 4,5x5,5 + 4,5x5,5 + 1x5,5", QtdPacote = 200, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A6"), VlrUnitario = 0.15m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Laminação e Plastificação"), Nome = "Vinil 1x0,22m", QtdPacote = 5, VlrPacote = 25.13m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 5.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Laminação e Plastificação"), Nome = "Vinil 15x11cm", QtdPacote = 60, VlrPacote = 25.13m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.42m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Corrente Bolinha N2 Niquel", QtdPacote = 1, VlrPacote = 5.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 5.50m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Kraft"), Nome = "Papel Kraft 10,5x9,5", QtdPacote = 180, VlrPacote = 16.10m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.09m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Offset 90g"), Nome = "Sulfite 90gr 9x9", QtdPacote = 1500, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Miolo Frente Verso 9x9", QtdPacote = 1, VlrPacote = 0.02m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Espiral"), Nome = "Espiral 14mm", QtdPacote = 50, VlrPacote = 21.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.42m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fios"), Nome = "Lã Balloon 100g", QtdPacote = 100, VlrPacote = 16.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.17m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Bico de Pato Jacaré com Garra", QtdPacote = 50, VlrPacote = 38.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.78m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fios"), Nome = "Fio de Malha Euroroma 140m", QtdPacote = 1000, VlrPacote = 16.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fios"), Nome = "Barbante Bandeirantes nº 8", QtdPacote = 2000, VlrPacote = 57.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.03m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Pólen"), Nome = "Pólen Bold - 90 g/m2 - A4 (21x29,7cm)", QtdPacote = 100, VlrPacote = 14.83m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.15m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Pólen"), Nome = "Pólen Bold - 90 g/m2 - A5", QtdPacote = 200, VlrPacote = 14.83m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.07m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 16 cm x 11 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 9 cm x 2 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 15 cm x 28 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 1,5 cm x 30 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Puxador De Metal Carrapeta 7mmx5mm", QtdPacote = 10, VlrPacote = 32.99m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 3.30m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 15 cm x 32 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 15 cm x 3,5 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 15 cm x 9 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Ímã Neodímio Ø 20x1 mm N35", QtdPacote = 20, VlrPacote = 57.37m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 2.87m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Color plus Aspen 120g"), Nome = "Color plus metalico Aspen 120g A4", QtdPacote = 100, VlrPacote = 69.95m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.70m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 90g"), Nome = "Sulfite 90gr A4", QtdPacote = 500, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.10m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 90g"), Nome = "miolo 90gr A7", QtdPacote = 4000, VlrPacote = 50.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.01m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 22 cm x 8 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Couro"), Nome = "Couro 6 cm x 8 cm", QtdPacote = 1000, VlrPacote = 18.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Marca texto", QtdPacote = 6, VlrPacote = 19.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 3.17m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Armarinhos"), Nome = "Fecho canoa Niquel", QtdPacote = 20, VlrPacote = 5.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.28m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Miolo Calendário 5x5", QtdPacote = 150, VlrPacote = 31.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.21m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Ima"), Nome = "Manta Imantada", QtdPacote = 1, VlrPacote = 22.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 22.90m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Ima"), Nome = "Manta Imantada A6", QtdPacote = 40, VlrPacote = 22.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.57m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Fotográfico"), Nome = "Papel fotografico A6", QtdPacote = 80, VlrPacote = 15.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.20m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Fotográfico"), Nome = "Papel fotografico 10X5", QtdPacote = 800, VlrPacote = 15.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Ima"), Nome = "Manta Imantada 10X5", QtdPacote = 3200, VlrPacote = 22.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.01m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Impressão"), Nome = "Impressao Bolso 10x5", QtdPacote = 20, VlrPacote = 1.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.05m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Color plus"), Nome = "Color plus A4", QtdPacote = 27, VlrPacote = 17.97m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.67m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papelão Cinza"), Nome = "Papelão 5,5x7,5", QtdPacote = 160, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A5"), VlrUnitario = 0.19m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel Fotográfico"), Nome = "Papel fotografico 8,5x10,5", QtdPacote = 80, VlrPacote = 15.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.20m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet Adesivado"), Nome = "Papel guarda 5x7 offset adesivo", QtdPacote = 160, VlrPacote = 29.80m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.19m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("OffSet 75g"), Nome = "miolo 75gr 5x7", QtdPacote = 5000, VlrPacote = 32.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.01m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Embalagem"), Nome = "Saco Adesivo Transparente 6x12cm", QtdPacote = 100, VlrPacote = 6.89m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.07m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Lenço de Papel", QtdPacote = 10, VlrPacote = 2.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.20m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Doces"), Nome = "Pirulito Florestal Coração Vermelho", QtdPacote = 50, VlrPacote = 8.79m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Unidade"), VlrUnitario = 0.18m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Papel vegetal"), Nome = "Papel vegetal A4 75g", QtdPacote = 50, VlrPacote = 19.20m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.38m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Tricoline"), Nome = "Tecido Tricoline 32cmx20cm", QtdPacote = 21, VlrPacote = 31.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 1.48m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Tricoline"), Nome = "Tecido Tricoline 11cmx6cm", QtdPacote = 212, VlrPacote = 31.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.15m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Fios"), Nome = "Barbante Bandeirantes nº 4", QtdPacote = 1000, VlrPacote = 21.89m, UnidadeMedidaId = ConsultarIdUnidadeMedida("metros"), VlrUnitario = 0.02m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Outros"), Nome = "Painço 500g", QtdPacote = 500, VlrPacote = 5.00m, UnidadeMedidaId = ConsultarIdUnidadeMedida("gramas"), VlrUnitario = 0.01m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Plastico para encadernação"), Nome = "Plastico para encadernação preto A4", QtdPacote = 50, VlrPacote = 29.90m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 0.60m },
            new MateriaPrima { GrupoId = ConsultarIdGrupo("Acetato"), Nome = "Acetato 18 micras", QtdPacote = 15, VlrPacote = 24.50m, UnidadeMedidaId = ConsultarIdUnidadeMedida("Folha A4"), VlrUnitario = 1.63m }
        ];

        private static Guid ConsultarIdUnidadeMedida(string unidadeMedida)
        {
            RestResponse response = PrecificadorApiService.GetByFilter($"/api/UnidadeMedida/ByFilter", JsonSerializer.Serialize(new NomeFilter { Nome = unidadeMedida }));

            if (response.IsSuccessStatusCode)
            {
                string? content = response.Content ?? throw new Exception("Response content is null.");
                List<UnidadeMedida> retorno = JsonSerializer.Deserialize<List<UnidadeMedida>>(content) ?? [];

                if (retorno.Count != 0)
                {
                    return retorno.First().Id;
                }
                else
                {
                    throw new Exception($"Unidade de Medida '{unidadeMedida}' não encontrado.");
                }
            }
            else
            {
                throw new Exception($"Erro ao consultar Unidade de Medida: {response.ErrorMessage}");
            }
        }

        private static Guid ConsultarIdGrupo(string grupo)
        {
            RestResponse response = PrecificadorApiService.GetByFilter($"/api/Grupo/ByFilter", JsonSerializer.Serialize(new NomeFilter { Nome = grupo }));

            if (response.IsSuccessStatusCode)
            {
                string? content = response.Content ?? throw new Exception("Response content is null.");
                List<Grupo> retorno = JsonSerializer.Deserialize<List<Grupo>>(content) ?? [];

                if (retorno.Count != 0)
                {
                    return retorno.First().Id;
                }
                else
                {
                    throw new Exception($"Grupo '{grupo}' não encontrado.");
                }
            }
            else
            {
                throw new Exception($"Erro ao consultar grupo: {response.ErrorMessage}");
            }
        }

        protected override NomeFilter GetFilter(MateriaPrima item) => new() { Nome = item.Nome };
    }
}