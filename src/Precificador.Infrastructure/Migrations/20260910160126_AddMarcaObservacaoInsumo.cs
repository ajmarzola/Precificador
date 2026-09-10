using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddMarcaObservacaoInsumo : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Insumos_EmpresaId_NomeNormalizado",
                table: "Insumos");

            migrationBuilder.AddColumn<string>(
                name: "Marca",
                table: "Insumos",
                type: "TEXT",
                maxLength: 80,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MarcaNormalizada",
                table: "Insumos",
                type: "TEXT",
                maxLength: 80,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Observacao",
                table: "Insumos",
                type: "TEXT",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_EmpresaId_NomeNormalizado_MarcaNormalizada",
                table: "Insumos",
                columns: new[] { "EmpresaId", "NomeNormalizado", "MarcaNormalizada" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Insumos_EmpresaId_NomeNormalizado_MarcaNormalizada",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "Marca",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "MarcaNormalizada",
                table: "Insumos");

            migrationBuilder.DropColumn(
                name: "Observacao",
                table: "Insumos");

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_EmpresaId_NomeNormalizado",
                table: "Insumos",
                columns: new[] { "EmpresaId", "NomeNormalizado" },
                unique: true);
        }
    }
}
