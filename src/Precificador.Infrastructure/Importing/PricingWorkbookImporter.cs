using Dapper;
using ExcelDataReader;
using Precificador.Application.Abstractions;
using Precificador.Application.Models;
using Precificador.Infrastructure.Persistence;
using System.Data;
using System.Text;

namespace Precificador.Infrastructure.Importing;

public sealed class PricingWorkbookImporter : IWorkbookImporter
{
    private readonly DbConnectionFactory _connectionFactory;

    private static readonly HashSet<string> IgnoredSheets = new(StringComparer.OrdinalIgnoreCase)
    {
        "Referências",
        "Custos Fixos",
        "Custos Variáveis",
        "Pró-Labore"
    };

    public PricingWorkbookImporter(DbConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public async Task ImportAsync(string workbookPath, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(workbookPath);

        if (!File.Exists(workbookPath))
            throw new FileNotFoundException("Arquivo não encontrado.", workbookPath);

        var dataSet = ReadWorkbook(workbookPath);

        using var connection = _connectionFactory.Create();
        connection.Open();

        using var transaction = connection.BeginTransaction();

        await SeedCostComponentTypesAsync(connection, transaction);
        await ImportReferencesAsync(dataSet, connection, transaction);
        await ImportGlobalCostsAsync(dataSet, connection, transaction);
        await ImportCollectionsAsync(dataSet, connection, transaction);

        transaction.Commit();
    }

    private static DataSet ReadWorkbook(string workbookPath)
    {
        using var stream = File.Open(workbookPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        using var reader = ExcelReaderFactory.CreateReader(stream);

        return reader.AsDataSet(new ExcelDataSetConfiguration
        {
            ConfigureDataTable = _ => new ExcelDataTableConfiguration
            {
                UseHeaderRow = false
            }
        });
    }

    private static async Task SeedCostComponentTypesAsync(IDbConnection connection, IDbTransaction transaction)
    {
        var componentTypes = new[]
        {
            new { Name = "Packaging", IsPercentage = false },
            new { Name = "EquipmentWear", IsPercentage = false },
            new { Name = "CardFee", IsPercentage = true },
            new { Name = "InstallmentFee", IsPercentage = true },
            new { Name = "FreightAdjustment", IsPercentage = false },
            new { Name = "FixedOverhead", IsPercentage = false },
            new { Name = "VariableOverhead", IsPercentage = false },
            new { Name = "Labor", IsPercentage = false }
        };

        foreach (var item in componentTypes)
        {
            await connection.ExecuteAsync(
                """
                IF NOT EXISTS (SELECT 1 FROM pricing.CostComponentType WHERE Name = @Name)
                BEGIN
                    INSERT INTO pricing.CostComponentType (Name, IsPercentage)
                    VALUES (@Name, @IsPercentage);
                END
                """,
                item,
                transaction);
        }

        foreach (var group in new[] { "Custos Fixos", "Custos Variáveis", "Pró-Labore" })
        {
            await connection.ExecuteAsync(
                """
                IF NOT EXISTS (SELECT 1 FROM pricing.GlobalCostGroup WHERE Name = @Name)
                BEGIN
                    INSERT INTO pricing.GlobalCostGroup (Name)
                    VALUES (@Name);
                END
                """,
                new { Name = group },
                transaction);
        }
    }

    private static async Task ImportReferencesAsync(DataSet dataSet, IDbConnection connection, IDbTransaction transaction)
    {
        var table = dataSet.Tables.Cast<DataTable>()
            .FirstOrDefault(t => string.Equals(t.TableName, "Referências", StringComparison.OrdinalIgnoreCase));

        if (table is null)
            throw new InvalidOperationException("Aba 'Referências' não encontrada.");

        for (var i = 0; i < table.Rows.Count; i++)
        {
            var row = table.Rows[i];

            var groupName = SpreadsheetValueParser.AsText(row[0]);
            var name = SpreadsheetValueParser.AsText(row[1]);

            if (string.IsNullOrWhiteSpace(groupName) || string.IsNullOrWhiteSpace(name))
                continue;

            if (string.Equals(groupName, "Grupo", StringComparison.OrdinalIgnoreCase) &&
                string.Equals(name, "Produto", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var packageQuantity = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsDecimal(row[2]) : null;
            var packagePrice = row.ItemArray.Length > 3 ? SpreadsheetValueParser.AsDecimal(row[3]) : null;
            var freightAmount = row.ItemArray.Length > 4 ? SpreadsheetValueParser.AsDecimal(row[4]) : null;
            var unitOfMeasure = row.ItemArray.Length > 5 ? SpreadsheetValueParser.AsText(row[5]) : null;

            decimal? unitCost = null;
            decimal? freightUnitCost = null;
            decimal? totalUnitCost = null;

            if (packageQuantity is > 0 && packagePrice.HasValue)
                unitCost = Math.Round(packagePrice.Value / packageQuantity.Value, 6);

            if (packageQuantity is > 0 && freightAmount.HasValue)
                freightUnitCost = Math.Round(freightAmount.Value / packageQuantity.Value, 6);

            totalUnitCost = (unitCost ?? 0m) + (freightUnitCost ?? 0m);

            await connection.ExecuteAsync(
                """
                IF EXISTS (SELECT 1 FROM pricing.RawMaterial WHERE Name = @Name)
                BEGIN
                    UPDATE pricing.RawMaterial
                       SET GroupName = @GroupName,
                           PackageQuantity = @PackageQuantity,
                           PackagePrice = @PackagePrice,
                           FreightAmount = @FreightAmount,
                           UnitOfMeasure = @UnitOfMeasure,
                           UnitCost = @UnitCost,
                           FreightUnitCost = @FreightUnitCost,
                           TotalUnitCost = @TotalUnitCost,
                           SourceSheetName = @SourceSheetName,
                           SourceRowNumber = @SourceRowNumber,
                           UpdatedAt = SYSUTCDATETIME()
                     WHERE Name = @Name;
                END
                ELSE
                BEGIN
                    INSERT INTO pricing.RawMaterial
                    (
                        GroupName, Name, PackageQuantity, PackagePrice, FreightAmount,
                        UnitOfMeasure, UnitCost, FreightUnitCost, TotalUnitCost,
                        SourceSheetName, SourceRowNumber
                    )
                    VALUES
                    (
                        @GroupName, @Name, @PackageQuantity, @PackagePrice, @FreightAmount,
                        @UnitOfMeasure, @UnitCost, @FreightUnitCost, @TotalUnitCost,
                        @SourceSheetName, @SourceRowNumber
                    );
                END
                """,
                new
                {
                    GroupName = groupName,
                    Name = name,
                    PackageQuantity = packageQuantity,
                    PackagePrice = packagePrice,
                    FreightAmount = freightAmount,
                    UnitOfMeasure = unitOfMeasure,
                    UnitCost = unitCost,
                    FreightUnitCost = freightUnitCost,
                    TotalUnitCost = totalUnitCost,
                    SourceSheetName = table.TableName,
                    SourceRowNumber = i + 1
                },
                transaction);
        }
    }

    private static async Task ImportGlobalCostsAsync(DataSet dataSet, IDbConnection connection, IDbTransaction transaction)
    {
        var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Custos Fixos"] = "Custos Fixos",
            ["Custos Variáveis"] = "Custos Variáveis",
            ["Pró-Labore"] = "Pró-Labore"
        };

        foreach (var mapping in mappings)
        {
            var table = dataSet.Tables.Cast<DataTable>()
                .FirstOrDefault(t => string.Equals(t.TableName, mapping.Key, StringComparison.OrdinalIgnoreCase));

            if (table is null)
                continue;

            var groupId = await connection.ExecuteScalarAsync<int>(
                "SELECT GlobalCostGroupId FROM pricing.GlobalCostGroup WHERE Name = @Name;",
                new { Name = mapping.Value },
                transaction);

            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                var description = SpreadsheetValueParser.AsText(row[0]);
                var amount = row.ItemArray.Length > 1 ? SpreadsheetValueParser.AsDecimal(row[1]) : null;
                var notes = row.ItemArray.Length > 2 ? SpreadsheetValueParser.AsText(row[2]) : null;

                if (string.IsNullOrWhiteSpace(description))
                    continue;

                if (IsGlobalCostMetaRow(description))
                    continue;

                await connection.ExecuteAsync(
                    """
                    INSERT INTO pricing.GlobalCostEntry
                    (
                        GlobalCostGroupId, Description, MonthlyAmount, Notes, SourceSheetName, SourceRowNumber
                    )
                    VALUES
                    (
                        @GlobalCostGroupId, @Description, @MonthlyAmount, @Notes, @SourceSheetName, @SourceRowNumber
                    );
                    """,
                    new
                    {
                        GlobalCostGroupId = groupId,
                        Description = description,
                        MonthlyAmount = amount,
                        Notes = notes,
                        SourceSheetName = table.TableName,
                        SourceRowNumber = i + 1
                    },
                    transaction);
            }
        }
    }

    private static bool IsGlobalCostMetaRow(string description)
    {
        var normalized = SpreadsheetValueParser.Normalize(description);

        return normalized is
            "custos fixos" or
            "custos variaveis" or
            "pro-labore priscilla" or
            "valor mensal" or
            "dias trabalhados" or
            "valor dia" or
            "horas por dia" or
            "valor hora";
    }

    private static async Task ImportCollectionsAsync(DataSet dataSet, IDbConnection connection, IDbTransaction transaction)
    {
        var tables = dataSet.Tables.Cast<DataTable>()
            .Where(t => !IgnoredSheets.Contains(t.TableName));

        foreach (var table in tables)
        {
            var collectionId = await UpsertCollectionAsync(table.TableName, connection, transaction);
            var parser = new CollectionSheetParser(table);
            var blocks = parser.Parse();

            foreach (var block in blocks)
            {
                var productId = await InsertProductAsync(collectionId, block, connection, transaction);
                await InsertBomItemsAsync(productId, block, connection, transaction);
                await InsertMarginAsync(productId, block, connection, transaction);
                await InsertFinancialComponentsAsync(productId, block, connection, transaction);
                await InsertMarketResearchAsync(productId, block, connection, transaction);
            }
        }
    }

    private static async Task<int> UpsertCollectionAsync(string name, IDbConnection connection, IDbTransaction transaction)
    {
        return await connection.ExecuteScalarAsync<int>(
            """
            IF EXISTS (SELECT 1 FROM pricing.Collection WHERE Name = @Name)
            BEGIN
                UPDATE pricing.Collection
                   SET SourceSheetName = @Name,
                       UpdatedAt = SYSUTCDATETIME()
                 WHERE Name = @Name;

                SELECT CollectionId
                  FROM pricing.Collection
                 WHERE Name = @Name;
            END
            ELSE
            BEGIN
                INSERT INTO pricing.Collection (Name, SourceSheetName)
                VALUES (@Name, @Name);

                SELECT CAST(SCOPE_IDENTITY() AS INT);
            END
            """,
            new { Name = name },
            transaction);
    }

    private static async Task<int> InsertProductAsync(int collectionId, ProductBlockModel block, IDbConnection connection, IDbTransaction transaction)
    {
        return await connection.ExecuteScalarAsync<int>(
            """
            INSERT INTO pricing.Product
            (
                CollectionId, Name, SuggestedPrice, MaterialCostTotal, FinalComputedCost,
                SourceSheetName, SourceRowStart, SourceRowEnd
            )
            VALUES
            (
                @CollectionId, @Name, @SuggestedPrice, @MaterialCostTotal, @FinalComputedCost,
                @SourceSheetName, @SourceRowStart, @SourceRowEnd
            );

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """,
            new
            {
                CollectionId = collectionId,
                Name = block.ProductName,
                SuggestedPrice = block.SuggestedPrice,
                MaterialCostTotal = block.MaterialCost,
                FinalComputedCost = block.FinalComputedCost,
                SourceSheetName = block.SourceSheetName,
                SourceRowStart = block.StartRow,
                SourceRowEnd = block.EndRow
            },
            transaction);
    }

    private static async Task InsertBomItemsAsync(int productId, ProductBlockModel block, IDbConnection connection, IDbTransaction transaction)
    {
        for (var i = 0; i < block.Items.Count; i++)
        {
            var item = block.Items[i];

            var rawMaterialId = await connection.ExecuteScalarAsync<int?>(
                "SELECT RawMaterialId FROM pricing.RawMaterial WHERE Name = @Name;",
                new { Name = item.MaterialName },
                transaction);

            if (!rawMaterialId.HasValue)
                throw new InvalidOperationException($"Matéria-prima não encontrada em Referências: '{item.MaterialName}'.");

            await connection.ExecuteAsync(
                """
                INSERT INTO pricing.ProductMaterial
                (
                    ProductId, RawMaterialId, QuantityUsed, UnitCostSnapshot, TotalCostSnapshot, SortOrder, SourceRowNumber
                )
                VALUES
                (
                    @ProductId, @RawMaterialId, @QuantityUsed, @UnitCostSnapshot, @TotalCostSnapshot, @SortOrder, @SourceRowNumber
                );
                """,
                new
                {
                    ProductId = productId,
                    RawMaterialId = rawMaterialId.Value,
                    QuantityUsed = item.QuantityUsed,
                    UnitCostSnapshot = item.UnitCost,
                    TotalCostSnapshot = item.TotalCost,
                    SortOrder = i + 1,
                    SourceRowNumber = item.SourceRowNumber
                },
                transaction);
        }
    }

    private static async Task InsertMarginAsync(int productId, ProductBlockModel block, IDbConnection connection, IDbTransaction transaction)
    {
        if (block.MarginPercent is null && block.MarginAmount is null && block.PriceWithMargin is null)
            return;

        await connection.ExecuteAsync(
            """
            INSERT INTO pricing.ProductMargin
            (
                ProductId, MarginPercent, MarginAmount, PriceWithMargin, Notes
            )
            VALUES
            (
                @ProductId, @MarginPercent, @MarginAmount, @PriceWithMargin, @Notes
            );
            """,
            new
            {
                ProductId = productId,
                MarginPercent = block.MarginPercent,
                MarginAmount = block.MarginAmount,
                PriceWithMargin = block.PriceWithMargin,
                Notes = block.MarginNotes
            },
            transaction);
    }

    private static async Task InsertFinancialComponentsAsync(int productId, ProductBlockModel block, IDbConnection connection, IDbTransaction transaction)
    {
        foreach (var component in block.CostComponents)
        {
            var typeId = await connection.ExecuteScalarAsync<int>(
                "SELECT CostComponentTypeId FROM pricing.CostComponentType WHERE Name = @Name;",
                new { Name = component.TypeName },
                transaction);

            await connection.ExecuteAsync(
                """
                INSERT INTO pricing.ProductCostComponent
                (
                    ProductId, CostComponentTypeId, Amount, Percentage, Notes, SortOrder
                )
                VALUES
                (
                    @ProductId, @CostComponentTypeId, @Amount, @Percentage, @Notes, @SortOrder
                );
                """,
                new
                {
                    ProductId = productId,
                    CostComponentTypeId = typeId,
                    Amount = component.Amount,
                    Percentage = component.Percentage,
                    Notes = component.Notes,
                    SortOrder = component.SortOrder
                },
                transaction);
        }
    }

    private static async Task InsertMarketResearchAsync(int productId, ProductBlockModel block, IDbConnection connection, IDbTransaction transaction)
    {
        for (var i = 0; i < block.MarketResearch.Count; i++)
        {
            var item = block.MarketResearch[i];

            await connection.ExecuteAsync(
                """
                INSERT INTO pricing.ProductMarketResearch
                (
                    ProductId, Label, ObservedPrice, Notes, SortOrder
                )
                VALUES
                (
                    @ProductId, @Label, @ObservedPrice, @Notes, @SortOrder
                );
                """,
                new
                {
                    ProductId = productId,
                    Label = item.Label,
                    ObservedPrice = item.ObservedPrice,
                    Notes = item.Notes,
                    SortOrder = i + 1
                },
                transaction);
        }
    }
}
