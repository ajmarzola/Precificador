using Microsoft.EntityFrameworkCore.Migrations;

using Precificador.Core.Empresas;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpresaTimeZone : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TimeZoneId",
                table: "Empresas",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: Empresa.TimeZoneIdPadrao);

            migrationBuilder.UpdateData(
                table: "Empresas",
                keyColumn: "Id",
                keyValue: 1,
                column: "TimeZoneId",
                value: Empresa.TimeZoneIdPadrao);
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
