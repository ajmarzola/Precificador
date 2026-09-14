using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddInsumoIdentidadeConsolidada : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IdentidadeConsolidada",
                table: "Insumos",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("""
                UPDATE Insumos
                SET IdentidadeConsolidada = 1
                WHERE EXISTS (SELECT 1 FROM PrecosInsumos WHERE PrecosInsumos.InsumoId = Insumos.Id)
                   OR EXISTS (SELECT 1 FROM ItensFichaTecnica WHERE ItensFichaTecnica.InsumoId = Insumos.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IdentidadeConsolidada",
                table: "Insumos");
        }
    }
}
