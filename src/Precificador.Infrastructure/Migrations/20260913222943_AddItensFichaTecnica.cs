using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddItensFichaTecnica : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ItensFichaTecnica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    FichaTecnicaId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    Quantidade = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    Observacao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ItensFichaTecnica", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ItensFichaTecnica_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensFichaTecnica_FichasTecnicas_FichaTecnicaId",
                        column: x => x.FichaTecnicaId,
                        principalTable: "FichasTecnicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ItensFichaTecnica_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ItensFichaTecnica_EmpresaId_FichaTecnicaId_InsumoId",
                table: "ItensFichaTecnica",
                columns: new[] { "EmpresaId", "FichaTecnicaId", "InsumoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ItensFichaTecnica_FichaTecnicaId",
                table: "ItensFichaTecnica",
                column: "FichaTecnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_ItensFichaTecnica_InsumoId",
                table: "ItensFichaTecnica",
                column: "InsumoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ItensFichaTecnica");
        }
    }
}
