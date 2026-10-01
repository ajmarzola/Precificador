using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSolicitacaoAcessoEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SolicitacoesAcessoEmpresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NomeEmpresa = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeEmpresaNormalizado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeResponsavel = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    EmailResponsavel = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    EmailResponsavelNormalizado = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    DataSolicitacaoUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    Situacao = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SolicitacoesAcessoEmpresas", x => x.Id);
                    table.CheckConstraint("CK_SolicitacoesAcessoEmpresas_Situacao", "[Situacao] IN (1, 2, 3)");
                });

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAcessoEmpresas_Situacao_DataSolicitacaoUtc",
                table: "SolicitacoesAcessoEmpresas",
                columns: new[] { "Situacao", "DataSolicitacaoUtc" });

            migrationBuilder.CreateIndex(
                name: "UX_SolicitacoesAcessoEmpresas_Pendente_Nome_Email",
                table: "SolicitacoesAcessoEmpresas",
                columns: new[] { "NomeEmpresaNormalizado", "EmailResponsavelNormalizado" },
                unique: true,
                filter: "[Situacao] = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SolicitacoesAcessoEmpresas");
        }
    }
}
