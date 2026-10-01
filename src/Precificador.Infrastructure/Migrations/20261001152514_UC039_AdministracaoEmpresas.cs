using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class UC039_AdministracaoEmpresas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "DataDecisaoUtc",
                table: "SolicitacoesAcessoEmpresas",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DecididaPorUsuarioId",
                table: "SolicitacoesAcessoEmpresas",
                type: "nvarchar(450)",
                maxLength: 450,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EmpresaId",
                table: "SolicitacoesAcessoEmpresas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MotivoRecusa",
                table: "SolicitacoesAcessoEmpresas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EhTecnica",
                table: "Empresas",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "EncerradaEmUtc",
                table: "Empresas",
                type: "datetimeoffset",
                nullable: true);

            // Id 1 pode ter sido renomeada para um cliente real antes da FT003.
            migrationBuilder.Sql("UPDATE [Empresas] SET [EhTecnica] = 1 WHERE [Id] = 1 AND [NomeNormalizado] = N'EMPRESA INICIAL';");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAcessoEmpresas_DecididaPorUsuarioId",
                table: "SolicitacoesAcessoEmpresas",
                column: "DecididaPorUsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_SolicitacoesAcessoEmpresas_EmpresaId",
                table: "SolicitacoesAcessoEmpresas",
                column: "EmpresaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_SolicitacoesAcessoEmpresas_Decisao",
                table: "SolicitacoesAcessoEmpresas",
                sql: "([Situacao] = 1 AND [EmpresaId] IS NULL AND [DataDecisaoUtc] IS NULL AND [DecididaPorUsuarioId] IS NULL AND [MotivoRecusa] IS NULL) OR ([Situacao] = 2 AND [EmpresaId] IS NOT NULL AND [DataDecisaoUtc] IS NOT NULL AND [DecididaPorUsuarioId] IS NOT NULL AND [MotivoRecusa] IS NULL) OR ([Situacao] = 3 AND [EmpresaId] IS NULL AND [DataDecisaoUtc] IS NOT NULL AND [DecididaPorUsuarioId] IS NOT NULL)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Empresas_Encerramento",
                table: "Empresas",
                sql: "[Ativo] = 0 OR [EncerradaEmUtc] IS NULL");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail",
                unique: true,
                filter: "[NormalizedEmail] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitacoesAcessoEmpresas_AspNetUsers_DecididaPorUsuarioId",
                table: "SolicitacoesAcessoEmpresas",
                column: "DecididaPorUsuarioId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SolicitacoesAcessoEmpresas_Empresas_EmpresaId",
                table: "SolicitacoesAcessoEmpresas",
                column: "EmpresaId",
                principalTable: "Empresas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SolicitacoesAcessoEmpresas_AspNetUsers_DecididaPorUsuarioId",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropForeignKey(
                name: "FK_SolicitacoesAcessoEmpresas_Empresas_EmpresaId",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropIndex(
                name: "IX_SolicitacoesAcessoEmpresas_DecididaPorUsuarioId",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropIndex(
                name: "IX_SolicitacoesAcessoEmpresas_EmpresaId",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_SolicitacoesAcessoEmpresas_Decisao",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Empresas_Encerramento",
                table: "Empresas");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "DataDecisaoUtc",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropColumn(
                name: "DecididaPorUsuarioId",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropColumn(
                name: "EmpresaId",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropColumn(
                name: "MotivoRecusa",
                table: "SolicitacoesAcessoEmpresas");

            migrationBuilder.DropColumn(
                name: "EhTecnica",
                table: "Empresas");

            migrationBuilder.DropColumn(
                name: "EncerradaEmUtc",
                table: "Empresas");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");
        }
    }
}
