using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UC037_IdentidadeVisualEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IdentidadesVisuaisEmpresas",
                columns: table => new
                {
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    CorPrimaria = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    LogoConteudo = table.Column<byte[]>(type: "varbinary(max)", nullable: true),
                    LogoContentType = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentidadesVisuaisEmpresas", x => x.EmpresaId);
                    table.CheckConstraint("CK_IdentidadeVisual_Cor", "DATALENGTH([CorPrimaria]) = 14 AND [CorPrimaria] COLLATE Latin1_General_100_BIN2 LIKE '#[0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F][0-9A-F]'");
                    table.CheckConstraint("CK_IdentidadeVisual_Logo", "([LogoConteudo] IS NULL AND [LogoContentType] IS NULL) OR ([LogoConteudo] IS NOT NULL AND [LogoContentType] IS NOT NULL AND [LogoContentType] COLLATE Latin1_General_100_BIN2 IN ('image/png', 'image/jpeg'))");
                    table.CheckConstraint("CK_IdentidadeVisual_Tamanho", "[LogoConteudo] IS NULL OR DATALENGTH([LogoConteudo]) BETWEEN 1 AND 524288");
                    table.ForeignKey(
                        name: "FK_IdentidadesVisuaisEmpresas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IdentidadesVisuaisEmpresas");
        }
    }
}
