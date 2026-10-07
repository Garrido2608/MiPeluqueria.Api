using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiPeluqueria.Api.Migrations
{
    /// <inheritdoc />
    public partial class Seed_Catalogos_Completos_E_Indices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Ventas_VentaId",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_PeluqueroId",
                table: "Turnos");

            migrationBuilder.InsertData(
                table: "CategoriasProducto",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "IsDeleted", "Nombre", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Ceras y Pomadas de Modelado", null },
                    { 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Shampoos y Acondicionadores Profesionales", null },
                    { 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Aceites y Bálsamos para Barba", null },
                    { 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Insumos Técnicos de Salón (Tinturas y Oxidantes)", null }
                });

            migrationBuilder.InsertData(
                table: "CategoriasServicio",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "IsDeleted", "Nombre", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Peluquería", null },
                    { 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Barbería", null },
                    { 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Colorimetría", null },
                    { 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Tratamientos Capilares", null }
                });

            migrationBuilder.InsertData(
                table: "Especialidades",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "IsDeleted", "Nombre", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Corte Clásico y Tijera", null },
                    { 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Fade, Navaja y Perfilado de Barba", null },
                    { 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Colorimetría, Balayage y Decoloración", null },
                    { 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, false, "Nutrición, Keratina y Recuperación Capilar", null }
                });

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 6,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 7,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.InsertData(
                table: "MotivosCancelacion",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "Descripcion", "IsDeleted", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Cancelación con aviso previo del cliente", false, null },
                    { 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Ausencia o indisposición del profesional", false, null },
                    { 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Reprogramación solicitada por recepción", false, null },
                    { 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Fuerza mayor / Feriado / Corte de suministro", false, null }
                });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.InsertData(
                table: "TiposCabello",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "Descripcion", "IsDeleted", "Nombre", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Baja densidad, requiere volumen ligero", false, "Lacio Fino", null },
                    { 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Textura versátil con tendencia a frizz moderado", false, "Ondulado Medio", null },
                    { 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Alta necesidad de hidratación y definición", false, "Rizado / Con Rulos", null },
                    { 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Estructura densa ideal para fades de alto contraste", false, "Grueso / Afro", null }
                });

            migrationBuilder.InsertData(
                table: "TiposRostro",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "Descripcion", "IsDeleted", "Nombre", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Proporciones equilibradas, admite todo tipo de cortes", false, "Ovalado", null },
                    { 2, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Mandíbula marcada, favorece degradados laterales", false, "Cuadrado", null },
                    { 3, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Favorece volumen superior y laterales cortos", false, "Redondo", null },
                    { 4, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Favorece estructura equilibrada en laterales", false, "Alargado / Diamante", null }
                });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Agenda_Solapamiento",
                table: "Turnos",
                columns: new[] { "PeluqueroId", "FechaHoraInicio", "FechaHoraFin", "EstadoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CodigoBarras",
                table: "Productos",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_Liquidaciones",
                table: "DetallesVenta",
                columns: new[] { "PeluqueroId", "TipoItem" });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Email",
                table: "Clientes",
                column: "Email");

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Ventas_VentaId",
                table: "Pagos",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Pagos_Ventas_VentaId",
                table: "Pagos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_Agenda_Solapamiento",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CodigoBarras",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_DetallesVenta_Liquidaciones",
                table: "DetallesVenta");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Email",
                table: "Clientes");

            migrationBuilder.DeleteData(
                table: "CategoriasProducto",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CategoriasProducto",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CategoriasProducto",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CategoriasProducto",
                keyColumn: "Id",
                keyValue: 4);

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

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TiposCabello",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TiposCabello",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TiposCabello",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TiposCabello",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "TiposRostro",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TiposRostro",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TiposRostro",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "TiposRostro",
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

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_PeluqueroId",
                table: "Turnos",
                column: "PeluqueroId");

            migrationBuilder.AddForeignKey(
                name: "FK_Pagos_Ventas_VentaId",
                table: "Pagos",
                column: "VentaId",
                principalTable: "Ventas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
