using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddPerfilUsuarioEmpresa : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Perfil",
                table: "UsuariosEmpresas",
                type: "int",
                nullable: true);

            migrationBuilder.Sql("UPDATE UsuariosEmpresas SET Perfil = 1 WHERE Perfil IS NULL");

            migrationBuilder.AlterColumn<int>(
                name: "Perfil",
                table: "UsuariosEmpresas",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_UsuariosEmpresas_Perfil",
                table: "UsuariosEmpresas",
                sql: "[Perfil] IN (1, 2)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_UsuariosEmpresas_Perfil",
                table: "UsuariosEmpresas");

            migrationBuilder.DropColumn(
                name: "Perfil",
                table: "UsuariosEmpresas");
        }
    }
}
