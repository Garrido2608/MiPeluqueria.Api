using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MiPeluqueria.Api.Migrations
{
    /// <inheritdoc />
    public partial class Seed_Admin_Usuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(7907));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(7909));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(7911));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(7912));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(7914));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(7915));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8038));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8039));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8041));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8042));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8044));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8045));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8048));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8075));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8077));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8079));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8080));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8081));

            migrationBuilder.InsertData(
                table: "Usuarios",
                columns: new[] { "Id", "AceptaTerminos", "Activo", "BloqueadoHasta", "ClienteId", "CodigoVerificacion", "CreatedAt", "CreatedByUserId", "DeletedAt", "Email", "EmailVerificado", "FechaAceptacionTerminos", "FechaExpiracionCodigo", "IntentosFallidos", "IsDeleted", "PasswordHash", "PeluqueroId", "RolId", "UpdatedAt", "Username" },
                values: new object[] { 1, true, true, null, null, null, new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8111), null, null, "admin@tesis.com", true, null, null, 0, false, "$2a$11$0nN01jB5P169DXZgN/.hCeeNInhV/9tYkH.fK5v4a9jR1LqFkI5C6", null, 1, null, "admin" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8662));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8664));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8666));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8667));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8669));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8670));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8904));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8906));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8908));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8909));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8911));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8912));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8914));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8940));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8942));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8943));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8944));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 4, 43, 50, 412, DateTimeKind.Utc).AddTicks(8946));
        }
    }
}
