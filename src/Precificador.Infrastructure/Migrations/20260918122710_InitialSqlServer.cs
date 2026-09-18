using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Precificador.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialSqlServer : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Empresas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Empresas", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    RoleId = table.Column<string>(type: "nvarchar(450)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracoesPrecificacaoEmpresas",
                columns: table => new
                {
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    ValorHoraTrabalho = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    TarifaEnergiaKwh = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    MargemPadrao = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IncrementoComercial = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: true),
                    ReservaComercialDesconto = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false, defaultValue: 0.10m)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracoesPrecificacaoEmpresas", x => x.EmpresaId);
                    table.ForeignKey(
                        name: "FK_ConfiguracoesPrecificacaoEmpresas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Insumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Marca = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    MarcaNormalizada = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Categoria = table.Column<int>(type: "int", nullable: false),
                    UnidadeBase = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false),
                    IdentidadeConsolidada = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Insumos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Insumos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Produtos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Nome = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeNormalizado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Categoria = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    MargemAlvo = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Produtos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Produtos_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UsuariosEmpresas",
                columns: table => new
                {
                    UsuarioId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    Ativo = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsuariosEmpresas", x => new { x.UsuarioId, x.EmpresaId });
                    table.ForeignKey(
                        name: "FK_UsuariosEmpresas_AspNetUsers_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_UsuariosEmpresas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrecosInsumos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    InsumoId = table.Column<int>(type: "int", nullable: false),
                    QuantidadeCompra = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PrecoCompra = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DataReferencia = table.Column<DateOnly>(type: "date", nullable: false)
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

            migrationBuilder.CreateTable(
                name: "FichasTecnicas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    Rendimento = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TempoAtivoMinutos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FichasTecnicas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FichasTecnicas_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_FichasTecnicas_Produtos_ProdutoId",
                        column: x => x.ProdutoId,
                        principalTable: "Produtos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosPrecosProdutos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    ProdutoId = table.Column<int>(type: "int", nullable: false),
                    DataReferencia = table.Column<DateOnly>(type: "date", nullable: false),
                    CustoReferencia = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    MargemReferencia = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false),
                    PrecoSugerido = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    PrecoPrateleira = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    ReservaComercialReferencia = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ItensFichaTecnica",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    FichaTecnicaId = table.Column<int>(type: "int", nullable: false),
                    InsumoId = table.Column<int>(type: "int", nullable: false),
                    Quantidade = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    Observacao = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PercentualPerda = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: false, defaultValue: 0m)
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

            migrationBuilder.CreateTable(
                name: "UsosEquipamentosFicha",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmpresaId = table.Column<int>(type: "int", nullable: false),
                    FichaTecnicaId = table.Column<int>(type: "int", nullable: false),
                    NomeEquipamento = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    NomeEquipamentoNormalizado = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    PotenciaKw = table.Column<decimal>(type: "decimal(18,6)", precision: 18, scale: 6, nullable: false),
                    TempoUsoMinutos = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsosEquipamentosFicha", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UsosEquipamentosFicha_Empresas_EmpresaId",
                        column: x => x.EmpresaId,
                        principalTable: "Empresas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UsosEquipamentosFicha_FichasTecnicas_FichaTecnicaId",
                        column: x => x.FichaTecnicaId,
                        principalTable: "FichasTecnicas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "Empresas",
                columns: new[] { "Id", "Ativo", "Nome", "NomeNormalizado", "TimeZoneId" },
                values: new object[] { 1, true, "Empresa inicial", "EMPRESA INICIAL", "America/Sao_Paulo" });

            migrationBuilder.InsertData(
                table: "ConfiguracoesPrecificacaoEmpresas",
                columns: new[] { "EmpresaId", "IncrementoComercial", "MargemPadrao", "ReservaComercialDesconto", "TarifaEnergiaKwh", "ValorHoraTrabalho" },
                values: new object[] { 1, null, null, 0.10m, null, null });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Empresas_NomeNormalizado",
                table: "Empresas",
                column: "NomeNormalizado",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FichasTecnicas_EmpresaId_ProdutoId",
                table: "FichasTecnicas",
                columns: new[] { "EmpresaId", "ProdutoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FichasTecnicas_ProdutoId",
                table: "FichasTecnicas",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_Insumos_EmpresaId_NomeNormalizado_MarcaNormalizada",
                table: "Insumos",
                columns: new[] { "EmpresaId", "NomeNormalizado", "MarcaNormalizada" },
                unique: true);

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

            migrationBuilder.CreateIndex(
                name: "IX_PrecosInsumos_EmpresaId_InsumoId_DataReferencia",
                table: "PrecosInsumos",
                columns: new[] { "EmpresaId", "InsumoId", "DataReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_PrecosInsumos_InsumoId",
                table: "PrecosInsumos",
                column: "InsumoId");

            migrationBuilder.CreateIndex(
                name: "IX_Produtos_EmpresaId_NomeNormalizado",
                table: "Produtos",
                columns: new[] { "EmpresaId", "NomeNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPrecosProdutos_EmpresaId_ProdutoId_DataReferencia",
                table: "RegistrosPrecosProdutos",
                columns: new[] { "EmpresaId", "ProdutoId", "DataReferencia" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosPrecosProdutos_ProdutoId",
                table: "RegistrosPrecosProdutos",
                column: "ProdutoId");

            migrationBuilder.CreateIndex(
                name: "IX_UsosEquipamentosFicha_EmpresaId_FichaTecnicaId_NomeEquipamentoNormalizado",
                table: "UsosEquipamentosFicha",
                columns: new[] { "EmpresaId", "FichaTecnicaId", "NomeEquipamentoNormalizado" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UsosEquipamentosFicha_FichaTecnicaId",
                table: "UsosEquipamentosFicha",
                column: "FichaTecnicaId");

            migrationBuilder.CreateIndex(
                name: "IX_UsuariosEmpresas_EmpresaId",
                table: "UsuariosEmpresas",
                column: "EmpresaId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "ConfiguracoesPrecificacaoEmpresas");

            migrationBuilder.DropTable(
                name: "ItensFichaTecnica");

            migrationBuilder.DropTable(
                name: "PrecosInsumos");

            migrationBuilder.DropTable(
                name: "RegistrosPrecosProdutos");

            migrationBuilder.DropTable(
                name: "UsosEquipamentosFicha");

            migrationBuilder.DropTable(
                name: "UsuariosEmpresas");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "Insumos");

            migrationBuilder.DropTable(
                name: "FichasTecnicas");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Produtos");

            migrationBuilder.DropTable(
                name: "Empresas");
        }
    }
}
