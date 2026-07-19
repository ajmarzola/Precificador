using Precificador.Application.Models;
using Precificador.Domain.Enums;
using System.Data;

namespace Precificador.Infrastructure.Importing;

internal sealed class CollectionSheetParser
{
    private static readonly HashSet<string> FinancialLabels = new(StringComparer.OrdinalIgnoreCase)
    {
        "Custo",
        "Margem",
        "Pesquisa",
        "Valor",
        "Tarifa Pagamento",
        "Tarifa Parcelamento 3x s/ Juros",
        "Frete - São Paulo"
    };

    private readonly DataTable _table;

    public CollectionSheetParser(DataTable table)
    {
        _table = table;
    }

    public IReadOnlyList<ProductBlockModel> Parse()
    {
        var result = new List<ProductBlockModel>();
        ProductBlockModel? current = null;
        ParseState state = ParseState.AwaitingProductHeader;
        List<string?>? researchLabels = null;

        for (var rowIndex = 0; rowIndex < _table.Rows.Count; rowIndex++)
        {
            var row = _table.Rows[rowIndex];

            if (IsBlankRow(row))
            {
                if (current is not null && state == ParseState.ReadingFinancials)
                {
                    current.EndRow = rowIndex + 1;
                    result.Add(current);
                    current = null;
                    state = ParseState.AwaitingProductHeader;
                    researchLabels = null;
                }

                continue;
            }

            if (IsProductHeader(row))
            {
                if (current is not null)
                {
                    current.EndRow = rowIndex;
                    result.Add(current);
                }

                current = new ProductBlockModel
                {
                    ProductName = SpreadsheetValueParser.AsText(row[0])!,
                    SuggestedPrice = row.ItemArray.Length > 3 ? SpreadsheetValueParser.AsDecimal(row[3]) : null,
                    SourceSheetName = _table.TableName,
                    StartRow = rowIndex + 1
                };

                state = ParseState.ReadingBom;
                researchLabels = null;
                continue;
            }

            if (current is null)
                continue;

            if (IsBomRow(row))
            {
                current.Items.Add(new ProductBomItemModel
                {
                    MaterialName = SpreadsheetValueParser.AsText(row[0])!,
                    QuantityUsed = row.ItemArray.Length > 1 ? SpreadsheetValueParser.AsDecimal(row[1]) : null,
                    UnitCost = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsDecimal(row[2]) : null,
                    TotalCost = row.ItemArray.Length > 3 ? SpreadsheetValueParser.AsDecimal(row[3]) : null,
                    SourceRowNumber = rowIndex + 1
                });

                state = ParseState.ReadingBom;
                continue;
            }

            var label = SpreadsheetValueParser.Normalize(SpreadsheetValueParser.AsText(row[0]));

            switch (label)
            {
                case "custo":
                    current.MaterialCost = row.ItemArray.Length > 3 ? SpreadsheetValueParser.AsDecimal(row[3]) : null;
                    current.FinalComputedCost = current.MaterialCost;
                    state = ParseState.ReadingFinancials;
                    break;

                case "margem":
                    current.MarginPercent = row.ItemArray.Length > 1 ? SpreadsheetValueParser.AsDecimal(row[1]) : null;
                    current.MarginAmount = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsDecimal(row[2]) : null;
                    current.PriceWithMargin = row.ItemArray.Length > 3 ? SpreadsheetValueParser.AsDecimal(row[3]) : null;
                    current.MarginNotes = row.ItemArray.Length > 4 ? SpreadsheetValueParser.AsText(row[4]) : null;
                    state = ParseState.ReadingFinancials;
                    break;

                case "tarifa pagamento":
                    current.CostComponents.Add(new ProductCostComponentItemModel
                    {
                        TypeName = "CardFee",
                        Percentage = row.ItemArray.Length > 1 ? SpreadsheetValueParser.AsDecimal(row[1]) : null,
                        Amount = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsDecimal(row[2]) : null,
                        Notes = "Tarifa de pagamento",
                        SortOrder = current.CostComponents.Count + 1
                    });
                    state = ParseState.ReadingFinancials;
                    break;

                case "tarifa parcelamento 3x s/ juros":
                    current.CostComponents.Add(new ProductCostComponentItemModel
                    {
                        TypeName = "InstallmentFee",
                        Percentage = row.ItemArray.Length > 1 ? SpreadsheetValueParser.AsDecimal(row[1]) : null,
                        Amount = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsDecimal(row[2]) : null,
                        Notes = "Tarifa de parcelamento",
                        SortOrder = current.CostComponents.Count + 1
                    });
                    state = ParseState.ReadingFinancials;
                    break;

                case "frete - sao paulo":
                    current.CostComponents.Add(new ProductCostComponentItemModel
                    {
                        TypeName = "FreightAdjustment",
                        Percentage = row.ItemArray.Length > 1 ? SpreadsheetValueParser.AsDecimal(row[1]) : null,
                        Amount = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsDecimal(row[2]) : null,
                        Notes = row.ItemArray.Length > 4 ? SpreadsheetValueParser.AsText(row[4]) : "Frete da praça",
                        SortOrder = current.CostComponents.Count + 1
                    });
                    state = ParseState.ReadingFinancials;
                    break;

                case "pesquisa":
                    researchLabels = row.ItemArray
                        .Skip(1)
                        .Select(SpreadsheetValueParser.AsText)
                        .ToList();
                    state = ParseState.ReadingFinancials;
                    break;

                case "valor":
                    if (row.ItemArray.Length > 3)
                    {
                        var explicitSuggestedPrice = SpreadsheetValueParser.AsDecimal(row[3]);
                        if (explicitSuggestedPrice.HasValue)
                            current.SuggestedPrice = explicitSuggestedPrice;
                    }

                    if (researchLabels is not null)
                    {
                        for (var col = 1; col < row.ItemArray.Length && col - 1 < researchLabels.Count; col++)
                        {
                            var observedPrice = SpreadsheetValueParser.AsDecimal(row[col]);
                            var researchLabel = researchLabels[col - 1];

                            if (string.IsNullOrWhiteSpace(researchLabel) && !observedPrice.HasValue)
                                continue;

                            current.MarketResearch.Add(new MarketResearchItemModel
                            {
                                Label = researchLabel ?? $"Coluna {col + 1}",
                                ObservedPrice = observedPrice
                            });
                        }
                    }

                    state = ParseState.ReadingFinancials;
                    break;
            }
        }

        if (current is not null)
        {
            current.EndRow = _table.Rows.Count;
            result.Add(current);
        }

        return result;
    }

    private static bool IsBlankRow(DataRow row)
        => row.ItemArray.All(x => string.IsNullOrWhiteSpace(SpreadsheetValueParser.AsText(x)));

    private static bool IsProductHeader(DataRow row)
    {
        var a = SpreadsheetValueParser.AsText(row[0]);
        if (string.IsNullOrWhiteSpace(a))
            return false;

        if (FinancialLabels.Contains(a))
            return false;

        var b = row.ItemArray.Length > 1 ? SpreadsheetValueParser.AsText(row[1]) : null;
        var c = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsText(row[2]) : null;
        var d = row.ItemArray.Length > 3 ? SpreadsheetValueParser.AsDecimal(row[3]) : null;

        var hasHeaderShape =
            string.IsNullOrWhiteSpace(b) &&
            string.IsNullOrWhiteSpace(c) &&
            (d.HasValue || row.ItemArray.Length <= 3 || string.IsNullOrWhiteSpace(SpreadsheetValueParser.AsText(row[3])));

        return hasHeaderShape;
    }

    private static bool IsBomRow(DataRow row)
    {
        var a = SpreadsheetValueParser.AsText(row[0]);
        if (string.IsNullOrWhiteSpace(a))
            return false;

        if (FinancialLabels.Contains(a))
            return false;

        var b = row.ItemArray.Length > 1 ? SpreadsheetValueParser.AsDecimal(row[1]) : null;
        var c = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsDecimal(row[2]) : null;
        var d = row.ItemArray.Length > 3 ? SpreadsheetValueParser.AsDecimal(row[3]) : null;

        return b.HasValue || c.HasValue || d.HasValue;
    }
}
