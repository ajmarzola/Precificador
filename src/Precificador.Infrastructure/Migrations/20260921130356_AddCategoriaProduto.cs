using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddCategoriaProduto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CategoriasProdutos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CategoriasProdutos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CategoriasProdutos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.AddColumn<int>(
                name: "CategoriaProdutoId",
                table: "Produtos",
                type: "int",
                nullable: true);

            // Backfill: uma Categoria distinta por Empresa + texto normalizado (case-insensitive); o
            // texto de exibição é preservado a partir do Produto de menor Id dentro de cada grupo.
            migrationBuilder.Sql(
                """
                INSERT INTO CategoriasProdutos (EmpresaId, Nome, NomeNormalizado, Ativo)
                SELECT rep.EmpresaId, LTRIM(RTRIM(rep.Categoria)), UPPER(LTRIM(RTRIM(rep.Categoria))), 1
                FROM Produtos rep
                INNER JOIN (
                    SELECT EmpresaId, UPPER(LTRIM(RTRIM(Categoria))) AS CategoriaNormalizada, MIN(Id) AS MenorProdutoId
                    FROM Produtos
                    WHERE Categoria IS NOT NULL
                    GROUP BY EmpresaId, UPPER(LTRIM(RTRIM(Categoria)))
                ) grupos ON grupos.MenorProdutoId = rep.Id
                """);

            migrationBuilder.Sql(
                """
                UPDATE p
                SET p.CategoriaProdutoId = c.Id
                FROM Produtos p
                INNER JOIN CategoriasProdutos c
                    ON c.EmpresaId = p.EmpresaId
                    AND c.NomeNormalizado = UPPER(LTRIM(RTRIM(p.Categoria)))
                WHERE p.Categoria IS NOT NULL
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_CategoriaProdutoId",
                table: "Produtos",
                column: "CategoriaProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_CategoriasProdutos_EmpresaId_NomeNormalizado",
                table: "CategoriasProdutos",
                columns: new[] { "EmpresaId", "NomeNormalizado" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Produtos_CategoriasProdutos_CategoriaProdutoId",
                table: "Produtos",
                column: "CategoriaProdutoId",
                principalTable: "CategoriasProdutos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.DropColumn(
                name: "Categoria",
                table: "Produtos");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Irreversível semanticamente: o texto livre original não é recuperado a partir da Categoria estruturada.
            migrationBuilder.AddColumn<string>(
                name: "Categoria",
                table: "Produtos",
                type: "nvarchar(80)",
                maxLength: 80,
                nullable: true);

            migrationBuilder.DropForeignKey(
                name: "FK_Produtos_CategoriasProdutos_CategoriaProdutoId",
                table: "Produtos");

            migrationBuilder.DropTable(
                name: "CategoriasProdutos");

            migrationBuilder.DropIndex(
                name: "IX_Produtos_CategoriaProdutoId",
                table: "Produtos");

            migrationBuilder.DropColumn(
                name: "CategoriaProdutoId",
                table: "Produtos");
        }
    }
}

