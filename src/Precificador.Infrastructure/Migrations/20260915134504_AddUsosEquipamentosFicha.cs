using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUsosEquipamentosFicha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UsosEquipamentosFicha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    FichaTecnicaId = table.Column<int>(type: "INTEGER", nullable: false),
                    NomeEquipamento = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    NomeEquipamentoNormalizado = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    PotenciaKw = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    TempoUsoMinutos = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsosEquipamentosFicha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsosEquipamentosFicha_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsosEquipamentosFicha_FichasTecnicas_FichaTecnicaId",
                        column: x => x.FichaTecnicaId,
                        principalTable: "FichasTecnicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsosEquipamentosFicha_EmpresaId_FichaTecnicaId_NomeEquipamentoNormalizado",
                table: "UsosEquipamentosFicha",
                columns: new[] { "EmpresaId", "FichaTecnicaId", "NomeEquipamentoNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsosEquipamentosFicha_FichaTecnicaId",
                table: "UsosEquipamentosFicha",
                column: "FichaTecnicaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsosEquipamentosFicha");
        }
    }
}
