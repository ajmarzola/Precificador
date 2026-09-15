using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRegistrosPrecosProdutos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RegistrosPrecosProdutos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProdutoId = table.Column<int>(type: "INTEGER", nullable: false),
                    DataReferencia = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    CustoReferencia = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    MargemReferencia = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false),
                    PrecoSugerido = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    PrecoPrateleira = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    ReservaComercialReferencia = table.Column<decimal>(type: "TEXT", precision: 9, scale: 6, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosPrecosProdutos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RegistrosPrecosProdutos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RegistrosPrecosProdutos_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPrecosProdutos_EmpresaId_ProdutoId_DataReferencia",
                table: "RegistrosPrecosProdutos",
                columns: new[] { "EmpresaId", "ProdutoId", "DataReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPrecosProdutos_ProdutoId",
                table: "RegistrosPrecosProdutos",
                column: "ProdutoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RegistrosPrecosProdutos");
        }
    }
}
