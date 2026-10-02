using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UC034_ProdutosColecoes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProdutosColecoes",
                columns: table => new
                {
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    ColecaoProdutoId = table.Column<int>(type: "int", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Destaque = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProdutosColecoes", x => new { x.ProdutoId, x.ColecaoProdutoId });
                    table.ForeignKey(
                        name: "FK_ProdutosColecoes_ColecoesProdutos_ColecaoProdutoId",
                        column: x => x.ColecaoProdutoId,
                        principalTable: "ColecoesProdutos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProdutosColecoes_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ProdutosColecoes_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosColecoes_ColecaoProdutoId",
                table: "ProdutosColecoes",
                column: "ColecaoProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosColecoes_EmpresaId_ColecaoProdutoId_Destaque",
                table: "ProdutosColecoes",
                columns: new[] { "EmpresaId", "ColecaoProdutoId", "Destaque" });

            migrationBuilder.CreateIndex(
                name: "IX_ProdutosColecoes_EmpresaId_ProdutoId",
                table: "ProdutosColecoes",
                columns: new[] { "EmpresaId", "ProdutoId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProdutosColecoes");
        }
    }
}
