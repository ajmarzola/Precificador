using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceHourlyLaborWithPercentage : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "PercentualMaoDeObra",
                table: "ConfiguracoesPrecificacaoEmpresas",
                type: "decimal(9,6)",
                precision: 9,
                scale: 6,
                nullable: false,
                defaultValue: 0.10m);

            migrationBuilder.Sql(
                "UPDATE ConfiguracoesPrecificacaoEmpresas SET PercentualMaoDeObra = 0.10");

            migrationBuilder.UpdateData(
                table: "ConfiguracoesPrecificacaoEmpresas",
                keyColumn: "EmpresaId",
                keyValue: 1,
                column: "PercentualMaoDeObra",
                value: 0.10m);

            migrationBuilder.DropColumn(
                name: "ValorHoraTrabalho",
                table: "ConfiguracoesPrecificacaoEmpresas");

            migrationBuilder.DropColumn(
                name: "TempoAtivoMinutos",
                table: "FichasTecnicas");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversível semanticamente: os valores antigos de hora/tempo descartados no Up não podem ser recuperados.
            migrationBuilder.DropColumn(
                name: "PercentualMaoDeObra",
                table: "ConfiguracoesPrecificacaoEmpresas");

            migrationBuilder.AddColumn<int>(
                name: "TempoAtivoMinutos",
                table: "FichasTecnicas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "ValorHoraTrabalho",
                table: "ConfiguracoesPrecificacaoEmpresas",
                type: "decimal(18,6)",
                precision: 18,
                scale: 6,
                nullable: true);

            migrationBuilder.UpdateData(
                table: "ConfiguracoesPrecificacaoEmpresas",
                keyColumn: "EmpresaId",
                keyValue: 1,
                column: "ValorHoraTrabalho",
                value: null);
        }
    }
}
