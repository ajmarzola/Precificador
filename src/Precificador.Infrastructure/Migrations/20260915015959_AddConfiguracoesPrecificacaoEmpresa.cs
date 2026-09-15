using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddConfiguracoesPrecificacaoEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ConfiguracoesPrecificacaoEmpresas",
                columns: table => new
                {
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ValorHoraTrabalho = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    TarifaEnergiaKwh = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    MargemPadrao = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: true),
                    IncrementoComercial = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: true),
                    ReservaComercialDesconto = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false, defaultValue: 0.10m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesPrecificacaoEmpresas", x => x.EmpresaId);
                    table.ForeignKey(
                        name: "FK_ConfiguracoesPrecificacaoEmpresas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.Sql("""
                INSERT INTO ConfiguracoesPrecificacaoEmpresas (EmpresaId, ReservaComercialDesconto)
                SELECT Id, 0.10
                FROM Empresas;
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ConfiguracoesPrecificacaoEmpresas");
        }
    }
}
