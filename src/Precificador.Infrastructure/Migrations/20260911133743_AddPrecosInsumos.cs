using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPrecosInsumos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PrecosInsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EmpresaId = table.Column<int>(type: "INTEGER", nullable: false),
                    InsumoId = table.Column<int>(type: "INTEGER", nullable: false),
                    QuantidadeCompra = table.Column<decimal>(type: "TEXT", precision: 18, scale: 6, nullable: false),
                    PrecoCompra = table.Column<decimal>(type: "TEXT", precision: 18, scale: 4, nullable: false),
                    DataReferencia = table.Column<DateOnly>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrecosInsumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PrecosInsumos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PrecosInsumos_Insumos_InsumoId",
                        column: x => x.InsumoId,
                        principalTable: "Insumos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PrecosInsumos_EmpresaId_InsumoId_DataReferencia",
                table: "PrecosInsumos",
                columns: new[] { "EmpresaId", "InsumoId", "DataReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_PrecosInsumos_InsumoId",
                table: "PrecosInsumos",
                column: "InsumoId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PrecosInsumos");
        }
    }
}
