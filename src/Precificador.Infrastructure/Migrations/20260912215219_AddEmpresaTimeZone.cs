using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaTimeZone : Migration
    {
        private const string TimeZoneIdPadraoHistorico = "America/Sao_Paulo";

        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Empresas",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: TimeZoneIdPadraoHistorico);

            migrationBuilder.UpdateData(
                table: "Empresas",
                keyColumn: "Id",
                keyValue: 1,
                column: "TimeZoneId",
                value: TimeZoneIdPadraoHistorico);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TimeZoneId",
                table: "Empresas");
        }
    }
}
