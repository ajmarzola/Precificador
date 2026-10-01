using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddColecaoProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ColecoesProdutos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    DataLancamento = table.Column<DateOnly>(type: "date", nullable: false),
                    DataFinalizacao = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColecoesProdutos", x => x.Id);
                    table.CheckConstraint("CK_ColecoesProdutos_Periodo", "[DataFinalizacao] IS NULL OR [DataFinalizacao] >= [DataLancamento]");
                    table.ForeignKey(
                        name: "FK_ColecoesProdutos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ColecoesProdutosCategorias",
                columns: table => new
                {
                    ColecaoProdutoId = table.Column<int>(type: "int", nullable: false),
                    CategoriaProdutoId = table.Column<int>(type: "int", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ColecoesProdutosCategorias", x => new { x.ColecaoProdutoId, x.CategoriaProdutoId });
                    table.ForeignKey(
                        name: "FK_ColecoesProdutosCategorias_CategoriasProdutos_CategoriaProdutoId",
                        column: x => x.CategoriaProdutoId,
                        principalTable: "CategoriasProdutos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ColecoesProdutosCategorias_ColecoesProdutos_ColecaoProdutoId",
                        column: x => x.ColecaoProdutoId,
                        principalTable: "ColecoesProdutos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ColecoesProdutosCategorias_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ColecoesProdutos_EmpresaId_DataLancamento",
                table: "ColecoesProdutos",
                columns: new[] { "EmpresaId", "DataLancamento" });

            migrationBuilder.CreateIndex(
                name: "IX_ColecoesProdutos_EmpresaId_NomeNormalizado_DataLancamento",
                table: "ColecoesProdutos",
                columns: new[] { "EmpresaId", "NomeNormalizado", "DataLancamento" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ColecoesProdutosCategorias_CategoriaProdutoId",
                table: "ColecoesProdutosCategorias",
                column: "CategoriaProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ColecoesProdutosCategorias_EmpresaId_CategoriaProdutoId",
                table: "ColecoesProdutosCategorias",
                columns: new[] { "EmpresaId", "CategoriaProdutoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ColecoesProdutosCategorias");

            migrationBuilder.DropTable(
                name: "ColecoesProdutos");
        }
    }
}
