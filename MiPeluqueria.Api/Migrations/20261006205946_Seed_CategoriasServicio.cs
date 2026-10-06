using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiPeluqueria.Api.Migrations
{
    /// <inheritdoc />
    public partial class Seed_CategoriasServicio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "CategoriasServicio",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "IsDeleted", "Nombre", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5344), null, null, false, "Peluquería", null },
                    { 2, new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5346), null, null, false, "Barbería", null },
                    { 3, new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5348), null, null, false, "Colorimetría", null },
                    { 4, new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5350), null, null, false, "Tratamientos Capilares", null }
                });

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(4900));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(4902));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(4904));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(4906));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(4907));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(4909));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5248));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5250));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5252));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5253));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5255));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5256));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5258));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5295));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5297));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5299));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5301));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5303));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 6, 20, 59, 45, 779, DateTimeKind.Utc).AddTicks(5391));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "CategoriasServicio",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CategoriasServicio",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CategoriasServicio",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CategoriasServicio",
                keyColumn: "Id",
                keyValue: 4);

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

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 3, 5, 15, 6, 820, DateTimeKind.Utc).AddTicks(8111));
        }
    }
}
