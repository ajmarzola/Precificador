using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDesgasteEquipamentosCategoria : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FormaCalculoDesgasteEquipamento",
                table: "CategoriasProdutos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorDesgasteEquipamento",
                table: "CategoriasProdutos",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.Sql("UPDATE [CategoriasProdutos] SET [FormaCalculoDesgasteEquipamento] = 1, [ValorDesgasteEquipamento] = 0 WHERE [FormaCalculoDesgasteEquipamento] IS NULL OR [ValorDesgasteEquipamento] IS NULL;");

            migrationBuilder.AlterColumn<int>(
                name: "FormaCalculoDesgasteEquipamento",
                table: "CategoriasProdutos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "ValorDesgasteEquipamento",
                table: "CategoriasProdutos",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: false,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,6)",
                oldPrecision: 18,
                oldScale: 6,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_CategoriasProdutos_FormaCalculoDesgasteEquipamento",
                table: "CategoriasProdutos",
                sql: "[FormaCalculoDesgasteEquipamento] IN (1, 2)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_CategoriasProdutos_ValorDesgasteEquipamento",
                table: "CategoriasProdutos",
                sql: "[ValorDesgasteEquipamento] >= 0");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_CategoriasProdutos_FormaCalculoDesgasteEquipamento",
                table: "CategoriasProdutos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_CategoriasProdutos_ValorDesgasteEquipamento",
                table: "CategoriasProdutos");

            migrationBuilder.DropColumn(
                name: "FormaCalculoDesgasteEquipamento",
                table: "CategoriasProdutos");

            migrationBuilder.DropColumn(
                name: "ValorDesgasteEquipamento",
                table: "CategoriasProdutos");
        }
    }
}
