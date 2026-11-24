using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BCP.Academico.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Clientes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Codigo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    IdentificadorEnmascarado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    SelloSesion = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clientes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EventosAuditoria",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Detalle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Resultado = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Cuentas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numero = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 40, nullable: false),
                    Moneda = table.Column<string>(type: "TEXT", maxLength: 3, nullable: false),
                    SaldoCentimos = table.Column<long>(type: "INTEGER", nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: true),
                    Titular = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cuentas", x => x.Id);
                    table.CheckConstraint("CK_Cuenta_SaldoNoNegativo", "SaldoCentimos >= 0");
                    table.ForeignKey(
                        name: "FK_Cuentas_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Contactos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    Nombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    CuentaDestinoId = table.Column<int>(type: "INTEGER", nullable: false),
                    CelularYapeEnmascarado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Contactos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Contactos_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Contactos_Cuentas_CuentaDestinoId",
                        column: x => x.CuentaDestinoId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Transferencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Referencia = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ClienteId = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaOrigenId = table.Column<int>(type: "INTEGER", nullable: false),
                    CuentaDestinoId = table.Column<int>(type: "INTEGER", nullable: false),
                    DestinatarioNombre = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false),
                    DestinoEnmascarado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Canal = table.Column<string>(type: "TEXT", maxLength: 10, nullable: false),
                    ImporteCentimos = table.Column<long>(type: "INTEGER", nullable: false),
                    ComisionCentimos = table.Column<long>(type: "INTEGER", nullable: false),
                    Concepto = table.Column<string>(type: "TEXT", maxLength: 60, nullable: false),
                    Estado = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ClaveIdempotencia = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    HuellaSolicitud = table.Column<string>(type: "TEXT", maxLength: 64, nullable: false),
                    CreadaEnUtc = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ActualizadaEnUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transferencias", x => x.Id);
                    table.CheckConstraint("CK_Transferencia_ImportePositivo", "ImporteCentimos > 0");
                    table.ForeignKey(
                        name: "FK_Transferencias_Clientes_ClienteId",
                        column: x => x.ClienteId,
                        principalTable: "Clientes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transferencias_Cuentas_CuentaDestinoId",
                        column: x => x.CuentaDestinoId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Transferencias_Cuentas_CuentaOrigenId",
                        column: x => x.CuentaOrigenId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    CuentaId = table.Column<int>(type: "INTEGER", nullable: false),
                    TransferenciaId = table.Column<int>(type: "INTEGER", nullable: true),
                    Tipo = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    ImporteCentimos = table.Column<long>(type: "INTEGER", nullable: false),
                    SaldoResultanteCentimos = table.Column<long>(type: "INTEGER", nullable: false),
                    Descripcion = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    FechaUtc = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Movimientos_Cuentas_CuentaId",
                        column: x => x.CuentaId,
                        principalTable: "Cuentas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_Transferencias_TransferenciaId",
                        column: x => x.TransferenciaId,
                        principalTable: "Transferencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Codigo",
                table: "Clientes",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Contactos_ClienteId",
                table: "Contactos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Contactos_CuentaDestinoId",
                table: "Contactos",
                column: "CuentaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_ClienteId",
                table: "Cuentas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Cuentas_Numero",
                table: "Cuentas",
                column: "Numero",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EventosAuditoria_FechaUtc",
                table: "EventosAuditoria",
                column: "FechaUtc");

            migrationBuilder.CreateIndex(
                name: "IX_EventosAuditoria_Referencia",
                table: "EventosAuditoria",
                column: "Referencia");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_CuentaId_FechaUtc",
                table: "Movimientos",
                columns: new[] { "CuentaId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_TransferenciaId_CuentaId_Tipo",
                table: "Movimientos",
                columns: new[] { "TransferenciaId", "CuentaId", "Tipo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_ClienteId_ClaveIdempotencia",
                table: "Transferencias",
                columns: new[] { "ClienteId", "ClaveIdempotencia" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_CuentaDestinoId",
                table: "Transferencias",
                column: "CuentaDestinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_CuentaOrigenId",
                table: "Transferencias",
                column: "CuentaOrigenId");

            migrationBuilder.CreateIndex(
                name: "IX_Transferencias_Referencia",
                table: "Transferencias",
                column: "Referencia",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Contactos");

            migrationBuilder.DropTable(
                name: "EventosAuditoria");

            migrationBuilder.DropTable(
                name: "Movimientos");

            migrationBuilder.DropTable(
                name: "Transferencias");

            migrationBuilder.DropTable(
                name: "Cuentas");

            migrationBuilder.DropTable(
                name: "Clientes");
        }
    }
}
