using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiPeluqueria.Api.Migrations
{
    /// <inheritdoc />
    public partial class MultiTenantFinal : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_TiposCabello_TipoCabelloId1",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_TiposRostro_TipoRostroId1",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_PeluqueroEspecialidades_Peluqueros_PeluqueroId",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CategoriasProducto_CategoriaProductoId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Servicios_CategoriasServicio_CategoriaServicioId",
                table: "Servicios");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_EstadosTurno_EstadoTurnoId",
                table: "Turnos");

            migrationBuilder.DropForeignKey(
                name: "FK_TurnoServicios_Turnos_TurnoId",
                table: "TurnoServicios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Roles_RolId1",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_Fecha_Dashboard",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_SesionCajaId_Fecha",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Usuarios_RolId1",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "UX_Usuarios_Email_Active",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "UX_Usuarios_Username_Active",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TurnoServicios",
                table: "TurnoServicios");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_ClienteId_FechaHoraInicio",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_Disponibilidad_Peluquero_Fecha_Estado",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_EstadoTurnoId",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Sucursales_TenantId",
                table: "Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_SesionesCaja_EstadoAbierta_FechaApertura",
                table: "SesionesCaja");

            migrationBuilder.DropIndex(
                name: "UX_ServiciosInsumosRecetas_Servicio_Producto",
                table: "ServiciosInsumosRecetas");

            migrationBuilder.DropIndex(
                name: "IX_Servicios_CategoriaId_Activo",
                table: "Servicios");

            migrationBuilder.DropIndex(
                name: "IX_Servicios_CategoriaServicioId",
                table: "Servicios");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CategoriaProductoId",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_Productos_StockCritico",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "UX_Productos_CodigoBarras_Active",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "UX_Peluqueros_Email_Active",
                table: "Peluqueros");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PeluqueroEspecialidades",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesCompra_Proveedor_Estado_Fecha",
                table: "OrdenesCompra");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_Kardex_ProductoId_Fecha",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_SesionCajaId_Fecha",
                table: "MovimientosCaja");

            migrationBuilder.DropIndex(
                name: "IX_Liquidaciones_PeluqueroId_Estado_FechaDesde",
                table: "Liquidaciones");

            migrationBuilder.DropIndex(
                name: "IX_HorariosLaborales_PeluqueroId_DiaSemana",
                table: "HorariosLaborales");

            migrationBuilder.DropIndex(
                name: "IX_HistorialSesiones_UsuarioId_FechaIntento",
                table: "HistorialSesiones");

            migrationBuilder.DropIndex(
                name: "IX_HistorialEstadosTurno_TurnoId_FechaCambio",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropIndex(
                name: "IX_FichasTecnicas_ClienteId_Fecha",
                table: "FichasTecnicas");

            migrationBuilder.DropIndex(
                name: "UX_ComisionesConfig_Peluquero_Servicio",
                table: "ComisionesConfig");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Telefono",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_TipoCabelloId1",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_TipoRostroId1",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "UX_Clientes_Email_Active",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorario_PeluqueroId_Fechas",
                table: "BloqueosHorario");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Entidad_Clave_Fecha",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "RolId1",
                table: "Usuarios",
                newName: "UpdatedByUserId");

            migrationBuilder.RenameColumn(
                name: "EstadoTurnoId",
                table: "Turnos",
                newName: "UpdatedByUserId");

            migrationBuilder.RenameColumn(
                name: "CategoriaServicioId",
                table: "Servicios",
                newName: "UpdatedByUserId");

            migrationBuilder.RenameColumn(
                name: "CategoriaProductoId",
                table: "Productos",
                newName: "UpdatedByUserId");

            migrationBuilder.RenameColumn(
                name: "Fecha",
                table: "FichasTecnicas",
                newName: "FechaRegistro");

            migrationBuilder.RenameColumn(
                name: "DetalleFormulaTintura",
                table: "FichasTecnicas",
                newName: "Observaciones");

            migrationBuilder.RenameColumn(
                name: "TipoRostroId1",
                table: "Clientes",
                newName: "UpdatedByUserId");

            migrationBuilder.RenameColumn(
                name: "TipoCabelloId1",
                table: "Clientes",
                newName: "SucursalId");

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "DescuentoSenaAplicado",
                table: "Ventas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "IdempotencyKey",
                table: "Ventas",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Ventas",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<decimal>(
                name: "Subtotal",
                table: "Ventas",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Ventas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Usuarios",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "TurnoServicios",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "TurnoServicios",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "TurnoServicios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "TurnoServicios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "TurnoServicios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "TurnoServicios",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "TurnoServicios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TurnoServicios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "TurnoServicios",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "TurnoServicios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Turnos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaPagoSena",
                table: "Turnos",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "MontoSena",
                table: "Turnos",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciaPagoExterno",
                table: "Turnos",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "RequiereSena",
                table: "Turnos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Turnos",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<bool>(
                name: "SenaPagada",
                table: "Turnos",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Turnos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Turnos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "TiposRostro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "TiposRostro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TiposRostro",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "TiposRostro",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "TiposCabello",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "TiposCabello",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "TiposCabello",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "TiposCabello",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Tenants",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "SuscripcionesTenant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "SuscripcionesTenant",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Sucursales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Sucursales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "SesionesCaja",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ObservacionesCierre",
                table: "SesionesCaja",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "SesionesCaja",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "SesionesCaja",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "SesionesCaja",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalEgresosManuales",
                table: "SesionesCaja",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalIngresosManuales",
                table: "SesionesCaja",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVentasDigital",
                table: "SesionesCaja",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalVentasEfectivo",
                table: "SesionesCaja",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "SesionesCaja",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "ServiciosInsumosRecetas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "ServiciosInsumosRecetas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ServiciosInsumosRecetas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "ServiciosInsumosRecetas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Servicios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Servicios",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Servicios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Servicios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Roles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Roles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "RefreshTokens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "RefreshTokens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "RefreshTokens",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "RefreshTokens",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Cuit",
                table: "Proveedores",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Proveedores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Proveedores",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Proveedores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Proveedores",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Proveedores",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Productos",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Productos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "PlanesSuscripcion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "PlanesSuscripcion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Peluqueros",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Peluqueros",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Peluqueros",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Peluqueros",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Peluqueros",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "PeluqueroEspecialidades",
                type: "int",
                nullable: false,
                defaultValue: 0)
                .Annotation("SqlServer:Identity", "1, 1");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAt",
                table: "PeluqueroEspecialidades",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "PeluqueroEspecialidades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DeletedAt",
                table: "PeluqueroEspecialidades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "PeluqueroEspecialidades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeleted",
                table: "PeluqueroEspecialidades",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "PeluqueroEspecialidades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "PeluqueroEspecialidades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "PeluqueroEspecialidades",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "PeluqueroEspecialidades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Pagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReferenciaOperacion",
                table: "Pagos",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Pagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Pagos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Pagos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "OrdenesCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "OrdenesCompra",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "OrdenesCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "OrdenesCompra",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "OrdenesCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "MovimientosStock",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SaldoResultante",
                table: "MovimientosStock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "MovimientosStock",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "MovimientosStock",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "MovimientosStock",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "MovimientosCaja",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "MovimientosCaja",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "MovimientosCaja",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "MovimientosCaja",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "MotivosCancelacion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "MotivosCancelacion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "MotivosCancelacion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "MotivosCancelacion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "MediosPago",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "MediosPago",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EsEfectivoFisico",
                table: "MediosPago",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "MediosPago",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "MediosPago",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "MediosPago",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Bonificaciones",
                table: "Liquidaciones",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Liquidaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Descuentos",
                table: "Liquidaciones",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<DateTime>(
                name: "FechaEmision",
                table: "Liquidaciones",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "Observaciones",
                table: "Liquidaciones",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Liquidaciones",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Liquidaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Liquidaciones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Liquidaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UsuarioLiquidacionId",
                table: "Liquidaciones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                table: "HorariosLaborales",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "HorariosLaborales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HoraFinDescanso",
                table: "HorariosLaborales",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "HoraInicioDescanso",
                table: "HorariosLaborales",
                type: "time",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "HorariosLaborales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HorariosLaborales",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "HorariosLaborales",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "HistorialSesiones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "HistorialSesiones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HistorialSesiones",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "HistorialSesiones",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "HistorialPreciosServicio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "HistorialPreciosServicio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HistorialPreciosServicio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "HistorialPreciosServicio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "HistorialPreciosProducto",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "HistorialPreciosProducto",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HistorialPreciosProducto",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "HistorialPreciosProducto",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "HistorialEstadosTurno",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "HistorialEstadosTurno",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "HistorialEstadosTurno",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "HistorialEstadosTurno",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "FichasTecnicas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FormulaAplicada",
                table: "FichasTecnicas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "FotoResultadoUrl",
                table: "FichasTecnicas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ProductosUtilizados",
                table: "FichasTecnicas",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "FichasTecnicas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "FichasTecnicas",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "FichasTecnicas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "EstadosTurno",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "EstadosTurno",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Especialidades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "Especialidades",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "Especialidades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Especialidades",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "Especialidades",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "DetallesVenta",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "DetallesVenta",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "DetallesVenta",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "DetallesVenta",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "DetallesOrdenCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "DetallesOrdenCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "DetallesOrdenCompra",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "DetallesOrdenCompra",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "DetallesLiquidacion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "DetallesLiquidacion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "DetallesLiquidacion",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "DetallesLiquidacion",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "ConfiguracionSalon",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HorarioAtencionTexto",
                table: "ConfiguracionSalon",
                type: "nvarchar(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "NombreComercial",
                table: "ConfiguracionSalon",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "ConfiguracionSalon",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ConfiguracionSalon",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "ConfiguracionSalon",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "ComisionesConfig",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "ComisionesConfig",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "ComisionesConfig",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "ComisionesConfig",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "Clientes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Clientes",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "Clientes",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "CategoriasServicio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "CategoriasServicio",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "CategoriasServicio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CategoriasServicio",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "CategoriasServicio",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "CategoriasProducto",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Descripcion",
                table: "CategoriasProducto",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "CategoriasProducto",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "CategoriasProducto",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "CategoriasProducto",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "BloqueosHorario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "BloqueosHorario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "BloqueosHorario",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "BloqueosHorario",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeletedByUserId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SucursalId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TenantId",
                table: "AuditLogs",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UpdatedByUserId",
                table: "AuditLogs",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_TurnoServicios",
                table: "TurnoServicios",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_PeluqueroEspecialidades",
                table: "PeluqueroEspecialidades",
                column: "Id");

            migrationBuilder.InsertData(
                table: "CategoriasProducto",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Descripcion", "IsDeleted", "Nombre", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Línea de lavado profesional y reventa", false, "Shampoos y Acondicionadores", 1, 1, null, null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Pomadas, ceras mate, geles y lacas", false, "Fijación, Ceras y Styling", 1, 1, null, null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Insumos técnicos de uso interno en salón", false, "Tinturas y Oxidantes (Gabinete)", 1, 1, null, null },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Máscaras nutritivas, óleos y ampollas capilares", false, "Tratamientos y Ampollas", 1, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "CategoriasServicio",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Descripcion", "IsDeleted", "Nombre", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Servicios de corte de cabello y arreglo de barba", false, "Cortes y Barbería", 1, 1, null, null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Servicios técnicos de color y decoloración", false, "Colorimetría", 1, 1, null, null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Recuperación y salud de la fibra capilar", false, "Tratamientos y Nutrición", 1, 1, null, null },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Modelado y acabado profesional", false, "Peinados y Styling", 1, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "ConfiguracionSalon",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "Cuit", "DeletedAt", "DeletedByUserId", "Direccion", "EmailContacto", "HorarioAtencionTexto", "HorasAnticipacionCancelacion", "IntervaloTurnosMinutos", "IsDeleted", "MinutosToleranciaEspera", "MonedaIso", "NombreComercial", "NombreSalon", "NotificacionesEmailActivas", "NotificacionesWhatsAppActivas", "PorcentajeSenaReserva", "RazonSocial", "SucursalId", "Telefono", "TenantId", "TimeZoneId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "30-71234567-9", null, null, "Av. Colón 500, Córdoba", "admin@tesis.com", "Lunes a Sábados de 09:00 a 20:00 hs", 24, 30, false, 15, "ARS", "Mi Peluquería Central", "Mi Peluquería Central", true, true, 0.00m, "Mi Peluquería S.A.", 1, "+54 9 351 000-0000", 1, "America/Argentina/Cordoba", null, null });

            migrationBuilder.InsertData(
                table: "Especialidades",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Descripcion", "IsDeleted", "Nombre", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Cortes clásicos, modernos y diseño personalizado", false, "Corte y Estilismo", 1, 1, null, null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Balayage, tintura global, reflejos y decoloración", false, "Colorimetría y Mechas", 1, 1, null, null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Perfilado de barba con navaja y degradados", false, "Barbería Clásica y Fade", 1, 1, null, null },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Hidratación, nutrición, cauterización y keratina", false, "Tratamientos Capilares", 1, 1, null, null },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Brushing, ondas y recogidos sociales", false, "Peinados y Eventos", 1, 1, null, null }
                });

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "EstadosTurno",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "Descripcion", "EsEfectivoFisico", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Dinero físico en cajón", true, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "DeletedByUserId", "Descripcion", "EsEfectivoFisico", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cobro electrónico por terminal débito", false, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "DeletedByUserId", "Descripcion", "EsEfectivoFisico", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Cobro electrónico por terminal crédito", false, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "DeletedByUserId", "Descripcion", "EsEfectivoFisico", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Transferencia bancaria o CVU", false, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "DeletedByUserId", "Descripcion", "EsEfectivoFisico", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Pago digital mediante QR Mercado Pago", false, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 6,
                columns: new[] { "CreatedAt", "DeletedByUserId", "Descripcion", "EsEfectivoFisico", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "MODO u otra billetera digital", false, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MediosPago",
                keyColumn: "Id",
                keyValue: 7,
                columns: new[] { "CreatedAt", "DeletedByUserId", "Descripcion", "EsEfectivoFisico", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "Atención bonificada sin ingreso en caja", false, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "DeletedByUserId", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "DeletedByUserId", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null });

            migrationBuilder.UpdateData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "DeletedByUserId", "SucursalId", "TenantId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, 1, 1, null });

            migrationBuilder.InsertData(
                table: "Peluqueros",
                columns: new[] { "Id", "Activo", "Apellido", "ColorAgendaHex", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Email", "IsDeleted", "Nombre", "SucursalId", "Telefono", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { 1, true, "Gómez", "#3B82F6", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "carlos.gomez@tesis.com", false, "Carlos", 1, "+54 9 351 555-0101", 1, null, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "IncluyeWhatsAppRecordatorios", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, false, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.InsertData(
                table: "Proveedores",
                columns: new[] { "Id", "Contacto", "CreatedAt", "CreatedByUserId", "Cuit", "DeletedAt", "DeletedByUserId", "Email", "IsDeleted", "RazonSocial", "SucursalId", "Telefono", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { 1, "Laura Martínez", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "30-70998877-1", null, null, "ventas@distribuidoracapilar.com", false, "Distribuidora Capilar Córdoba S.R.L.", 1, "+54 9 351 444-9900", 1, null, null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 2,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 3,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 4,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "Roles",
                keyColumn: "Id",
                keyValue: 5,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "Sucursales",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.UpdateData(
                table: "SuscripcionesTenant",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "FechaInicio", "FechaProximoVencimiento", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2028, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "UpdatedByUserId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null });

            migrationBuilder.InsertData(
                table: "TiposCabello",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Descripcion", "IsDeleted", "Nombre", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Cabello sin ondas ni rulos naturales", false, "Lacio", 1, 1, null, null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Cabello con ondas suaves en medios y puntas", false, "Ondulado", 1, 1, null, null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Cabello con rulos definidos", false, "Rizado", 1, 1, null, null },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Cabello con rizo muy cerrado y alta porosidad", false, "Afro / Muy Rizado", 1, 1, null, null },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Fibra capilar delgada que requiere cuidado térmico", false, "Fino / Quebradizo", 1, 1, null, null },
                    { 6, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Raíz con tendencia oleosa y puntas secas", false, "Graso / Mixto", 1, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "TiposRostro",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Descripcion", "IsDeleted", "Nombre", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Proporciones equilibradas y simétricas", false, "Ovalado", 1, 1, null, null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Pómulos anchos y contornos suaves", false, "Redondo", 1, 1, null, null },
                    { 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Mandíbula y frente marcadas del mismo ancho", false, "Cuadrado", 1, 1, null, null },
                    { 4, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Mayor longitud vertical que ancho facial", false, "Alargado / Rectangular", 1, 1, null, null },
                    { 5, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Pómulos destacados y mentón fino", false, "Corazón / Diamante", 1, 1, null, null }
                });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "DeletedByUserId", "FechaAceptacionTerminos", "PasswordHash", "SucursalId", "TenantId" },
                values: new object[] { new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), "$2a$11$r4llG6xDC0cihMfzpC0/sOKZSMe1jQTVy01fKSfCkcq70nhWJHVhK", 1, 1 });

            migrationBuilder.InsertData(
                table: "Clientes",
                columns: new[] { "Id", "Activo", "Apellido", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Email", "FechaNacimiento", "FechaRegistro", "IsDeleted", "Nombre", "SucursalId", "Telefono", "TenantId", "TipoCabelloId", "TipoRostroId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { 1, true, "Pérez", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "juan.perez@cliente.com", new DateTime(1995, 5, 15, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, "Juan", 1, "+5493516000001", 1, 1, 1, null, null });

            migrationBuilder.InsertData(
                table: "ComisionesConfig",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "IsDeleted", "PeluqueroId", "PorcentajeServicio", "PorcentajeVentaProducto", "ServicioId", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, 1, 50.00m, 10.00m, null, 1, 1, null, null });

            migrationBuilder.InsertData(
                table: "HorariosLaborales",
                columns: new[] { "Id", "Activo", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "DiaSemana", "HoraFin", "HoraFinDescanso", "HoraInicio", "HoraInicioDescanso", "IsDeleted", "PeluqueroId", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 1, new TimeSpan(0, 20, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), false, 1, 1, 1, null, null },
                    { 2, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 2, new TimeSpan(0, 20, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), false, 1, 1, 1, null, null },
                    { 3, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 3, new TimeSpan(0, 20, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), false, 1, 1, 1, null, null },
                    { 4, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 4, new TimeSpan(0, 20, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), false, 1, 1, 1, null, null },
                    { 5, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 5, new TimeSpan(0, 20, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), false, 1, 1, 1, null, null },
                    { 6, true, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 6, new TimeSpan(0, 20, 0, 0, 0), new TimeSpan(0, 14, 0, 0, 0), new TimeSpan(0, 9, 0, 0, 0), new TimeSpan(0, 13, 0, 0, 0), false, 1, 1, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "PeluqueroEspecialidades",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "EspecialidadId", "IsDeleted", "PeluqueroId", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 1, false, 1, 1, 1, null, null },
                    { 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, 3, false, 1, 1, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "Productos",
                columns: new[] { "Id", "CategoriaId", "CodigoBarras", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "EsParaVenta", "IsDeleted", "Nombre", "PrecioCosto", "PrecioVenta", "ProveedorId", "StockActual", "StockMinimoAlerta", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, 2, "7790001000011", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, "Cera Modeladora Mate 100g", 4500.00m, 9500.00m, 1, 20, 5, 1, 1, null, null },
                    { 2, 3, "7790001000028", new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, false, false, "Pomo Tintura Profesional 60g (Insumo)", 3800.00m, 0.00m, 1, 15, 5, 1, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "Servicios",
                columns: new[] { "Id", "Activo", "CategoriaId", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "Descripcion", "DuracionMinutos", "IsDeleted", "Nombre", "Precio", "SucursalId", "TenantId", "UpdatedAt", "UpdatedByUserId" },
                values: new object[,]
                {
                    { 1, true, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Corte personalizado con máquina y tijera, incluye lavado.", 30, false, "Corte Clásico / Fade", 12000.00m, 1, 1, null, null },
                    { 2, true, 2, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Aplicación de color completo con productos profesionales.", 90, false, "Coloración Global / Tintura", 28000.00m, 1, 1, null, null },
                    { 3, true, 3, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, "Baño de crema e hidratación profunda.", 45, false, "Nutrición Capilar Intensiva", 18000.00m, 1, 1, null, null }
                });

            migrationBuilder.InsertData(
                table: "ServiciosInsumosRecetas",
                columns: new[] { "Id", "CantidadMedidaEstandar", "CantidadUnidadesDescontar", "CreatedAt", "CreatedByUserId", "DeletedAt", "DeletedByUserId", "DescuentoAutomaticoAlCompletarTurno", "IsDeleted", "ProductoId", "ServicioId", "SucursalId", "TenantId", "UnidadMedida", "UpdatedAt", "UpdatedByUserId" },
                values: new object[] { 1, 60.00m, 1, new DateTime(2026, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, null, true, false, 2, 2, 1, 1, "gr", null, null });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Fecha_Dashboard",
                table: "Ventas",
                columns: new[] { "TenantId", "SucursalId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_SesionCajaId",
                table: "Ventas",
                column: "SesionCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_SesionCajaId_Fecha",
                table: "Ventas",
                columns: new[] { "TenantId", "SucursalId", "SesionCajaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "UX_Ventas_Local_IdempotencyKey",
                table: "Ventas",
                columns: new[] { "TenantId", "SucursalId", "IdempotencyKey" },
                unique: true,
                filter: "[IdempotencyKey] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Usuarios_Local_Email",
                table: "Usuarios",
                columns: new[] { "TenantId", "SucursalId", "Email" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Usuarios_Local_Username",
                table: "Usuarios",
                columns: new[] { "TenantId", "SucursalId", "Username" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TurnoServicios_TurnoId",
                table: "TurnoServicios",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "UX_TurnoServicios_Turno_Servicio",
                table: "TurnoServicios",
                columns: new[] { "TenantId", "SucursalId", "TurnoId", "ServicioId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ClienteId",
                table: "Turnos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ClienteId_FechaHoraInicio",
                table: "Turnos",
                columns: new[] { "TenantId", "SucursalId", "ClienteId", "FechaHoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Disponibilidad_Peluquero_Fecha_Estado",
                table: "Turnos",
                columns: new[] { "TenantId", "SucursalId", "PeluqueroId", "FechaHoraInicio", "EstadoId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_PeluqueroId",
                table: "Turnos",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "UX_TiposRostro_Local_Nombre",
                table: "TiposRostro",
                columns: new[] { "TenantId", "SucursalId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_TiposCabello_Local_Nombre",
                table: "TiposCabello",
                columns: new[] { "TenantId", "SucursalId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Sucursales_Tenant_Nombre",
                table: "Sucursales",
                columns: new[] { "TenantId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesCaja_EstadoAbierta_FechaApertura",
                table: "SesionesCaja",
                columns: new[] { "TenantId", "SucursalId", "EstadoAbierta", "FechaApertura" });

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosInsumosRecetas_ServicioId",
                table: "ServiciosInsumosRecetas",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiciosInsumosRecetas_Servicio_Producto",
                table: "ServiciosInsumosRecetas",
                columns: new[] { "TenantId", "SucursalId", "ServicioId", "ProductoId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_CategoriaId",
                table: "Servicios",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_CategoriaId_Activo",
                table: "Servicios",
                columns: new[] { "TenantId", "SucursalId", "CategoriaId", "Activo" });

            migrationBuilder.CreateIndex(
                name: "UX_Servicios_Local_Nombre",
                table: "Servicios",
                columns: new[] { "TenantId", "SucursalId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Roles_Nombre",
                table: "Roles",
                column: "Nombre",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Proveedores_Local_Cuit",
                table: "Proveedores",
                columns: new[] { "TenantId", "SucursalId", "Cuit" },
                unique: true,
                filter: "[Cuit] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Proveedores_Local_RazonSocial",
                table: "Proveedores",
                columns: new[] { "TenantId", "SucursalId", "RazonSocial" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_StockCritico",
                table: "Productos",
                columns: new[] { "TenantId", "SucursalId", "StockActual", "StockMinimoAlerta" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Productos_Local_CodigoBarras",
                table: "Productos",
                columns: new[] { "TenantId", "SucursalId", "CodigoBarras" },
                unique: true,
                filter: "[CodigoBarras] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Productos_Local_Nombre",
                table: "Productos",
                columns: new[] { "TenantId", "SucursalId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Peluqueros_Local_Email",
                table: "Peluqueros",
                columns: new[] { "TenantId", "SucursalId", "Email" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PeluqueroEspecialidades_PeluqueroId",
                table: "PeluqueroEspecialidades",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "UX_PeluqueroEspecialidades_Activa",
                table: "PeluqueroEspecialidades",
                columns: new[] { "TenantId", "SucursalId", "PeluqueroId", "EspecialidadId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_Proveedor_Estado_Fecha",
                table: "OrdenesCompra",
                columns: new[] { "TenantId", "SucursalId", "ProveedorId", "Estado", "FechaEmision" });

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_ProveedorId",
                table: "OrdenesCompra",
                column: "ProveedorId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_Kardex_ProductoId_Fecha",
                table: "MovimientosStock",
                columns: new[] { "TenantId", "SucursalId", "ProductoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ProductoId",
                table: "MovimientosStock",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_SesionCajaId",
                table: "MovimientosCaja",
                column: "SesionCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_SesionCajaId_Fecha",
                table: "MovimientosCaja",
                columns: new[] { "TenantId", "SucursalId", "SesionCajaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "UX_MotivosCancelacion_Local_Descripcion",
                table: "MotivosCancelacion",
                columns: new[] { "TenantId", "SucursalId", "Descripcion" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_MediosPago_Local_Nombre",
                table: "MediosPago",
                columns: new[] { "TenantId", "SucursalId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Liquidaciones_PeluqueroId",
                table: "Liquidaciones",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "IX_Liquidaciones_PeluqueroId_Estado_FechaDesde",
                table: "Liquidaciones",
                columns: new[] { "TenantId", "SucursalId", "PeluqueroId", "Estado", "FechaDesde" });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosLaborales_PeluqueroId",
                table: "HorariosLaborales",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosLaborales_PeluqueroId_DiaSemana",
                table: "HorariosLaborales",
                columns: new[] { "TenantId", "SucursalId", "PeluqueroId", "DiaSemana" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialSesiones_UsuarioId",
                table: "HistorialSesiones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialSesiones_UsuarioId_FechaIntento",
                table: "HistorialSesiones",
                columns: new[] { "TenantId", "SucursalId", "UsuarioId", "FechaIntento" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosTurno_TurnoId",
                table: "HistorialEstadosTurno",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosTurno_TurnoId_FechaCambio",
                table: "HistorialEstadosTurno",
                columns: new[] { "TenantId", "SucursalId", "TurnoId", "FechaCambio" });

            migrationBuilder.CreateIndex(
                name: "IX_FichasTecnicas_ClienteId",
                table: "FichasTecnicas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_FichasTecnicas_ClienteId_FechaRegistro",
                table: "FichasTecnicas",
                columns: new[] { "TenantId", "SucursalId", "ClienteId", "FechaRegistro" });

            migrationBuilder.CreateIndex(
                name: "UX_EstadosTurno_Nombre",
                table: "EstadosTurno",
                column: "Nombre",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Especialidades_Local_Nombre",
                table: "Especialidades",
                columns: new[] { "TenantId", "SucursalId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_ConfiguracionSalon_Tenant_Sucursal",
                table: "ConfiguracionSalon",
                columns: new[] { "TenantId", "SucursalId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ComisionesConfig_PeluqueroId",
                table: "ComisionesConfig",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "UX_ComisionesConfig_Local_Peluquero_Servicio",
                table: "ComisionesConfig",
                columns: new[] { "TenantId", "SucursalId", "PeluqueroId", "ServicioId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Clientes_Local_Email",
                table: "Clientes",
                columns: new[] { "TenantId", "SucursalId", "Email" },
                unique: true,
                filter: "[Email] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Clientes_Local_Telefono",
                table: "Clientes",
                columns: new[] { "TenantId", "SucursalId", "Telefono" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_CategoriasServicio_Local_Nombre",
                table: "CategoriasServicio",
                columns: new[] { "TenantId", "SucursalId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_CategoriasProducto_Local_Nombre",
                table: "CategoriasProducto",
                columns: new[] { "TenantId", "SucursalId", "Nombre" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorario_PeluqueroId",
                table: "BloqueosHorario",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorario_PeluqueroId_Fechas",
                table: "BloqueosHorario",
                columns: new[] { "TenantId", "SucursalId", "PeluqueroId", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entidad_Clave_Fecha",
                table: "AuditLogs",
                columns: new[] { "TenantId", "SucursalId", "EntidadNombre", "ClavePrimaria", "FechaUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_PeluqueroEspecialidades_Peluqueros_PeluqueroId",
                table: "PeluqueroEspecialidades",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_TurnoServicios_Turnos_TurnoId",
                table: "TurnoServicios",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PeluqueroEspecialidades_Peluqueros_PeluqueroId",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropForeignKey(
                name: "FK_TurnoServicios_Turnos_TurnoId",
                table: "TurnoServicios");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_Fecha_Dashboard",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_SesionCajaId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_SesionCajaId_Fecha",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "UX_Ventas_Local_IdempotencyKey",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "UX_Usuarios_Local_Email",
                table: "Usuarios");

            migrationBuilder.DropIndex(
                name: "UX_Usuarios_Local_Username",
                table: "Usuarios");

            migrationBuilder.DropPrimaryKey(
                name: "PK_TurnoServicios",
                table: "TurnoServicios");

            migrationBuilder.DropIndex(
                name: "IX_TurnoServicios_TurnoId",
                table: "TurnoServicios");

            migrationBuilder.DropIndex(
                name: "UX_TurnoServicios_Turno_Servicio",
                table: "TurnoServicios");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_ClienteId",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_ClienteId_FechaHoraInicio",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_Disponibilidad_Peluquero_Fecha_Estado",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_PeluqueroId",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "UX_TiposRostro_Local_Nombre",
                table: "TiposRostro");

            migrationBuilder.DropIndex(
                name: "UX_TiposCabello_Local_Nombre",
                table: "TiposCabello");

            migrationBuilder.DropIndex(
                name: "UX_Sucursales_Tenant_Nombre",
                table: "Sucursales");

            migrationBuilder.DropIndex(
                name: "IX_SesionesCaja_EstadoAbierta_FechaApertura",
                table: "SesionesCaja");

            migrationBuilder.DropIndex(
                name: "IX_ServiciosInsumosRecetas_ServicioId",
                table: "ServiciosInsumosRecetas");

            migrationBuilder.DropIndex(
                name: "UX_ServiciosInsumosRecetas_Servicio_Producto",
                table: "ServiciosInsumosRecetas");

            migrationBuilder.DropIndex(
                name: "IX_Servicios_CategoriaId",
                table: "Servicios");

            migrationBuilder.DropIndex(
                name: "IX_Servicios_CategoriaId_Activo",
                table: "Servicios");

            migrationBuilder.DropIndex(
                name: "UX_Servicios_Local_Nombre",
                table: "Servicios");

            migrationBuilder.DropIndex(
                name: "UX_Roles_Nombre",
                table: "Roles");

            migrationBuilder.DropIndex(
                name: "UX_Proveedores_Local_Cuit",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "UX_Proveedores_Local_RazonSocial",
                table: "Proveedores");

            migrationBuilder.DropIndex(
                name: "IX_Productos_StockCritico",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "UX_Productos_Local_CodigoBarras",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "UX_Productos_Local_Nombre",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "UX_Peluqueros_Local_Email",
                table: "Peluqueros");

            migrationBuilder.DropPrimaryKey(
                name: "PK_PeluqueroEspecialidades",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropIndex(
                name: "IX_PeluqueroEspecialidades_PeluqueroId",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropIndex(
                name: "UX_PeluqueroEspecialidades_Activa",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesCompra_Proveedor_Estado_Fecha",
                table: "OrdenesCompra");

            migrationBuilder.DropIndex(
                name: "IX_OrdenesCompra_ProveedorId",
                table: "OrdenesCompra");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_Kardex_ProductoId_Fecha",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_ProductoId",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_SesionCajaId",
                table: "MovimientosCaja");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_SesionCajaId_Fecha",
                table: "MovimientosCaja");

            migrationBuilder.DropIndex(
                name: "UX_MotivosCancelacion_Local_Descripcion",
                table: "MotivosCancelacion");

            migrationBuilder.DropIndex(
                name: "UX_MediosPago_Local_Nombre",
                table: "MediosPago");

            migrationBuilder.DropIndex(
                name: "IX_Liquidaciones_PeluqueroId",
                table: "Liquidaciones");

            migrationBuilder.DropIndex(
                name: "IX_Liquidaciones_PeluqueroId_Estado_FechaDesde",
                table: "Liquidaciones");

            migrationBuilder.DropIndex(
                name: "IX_HorariosLaborales_PeluqueroId",
                table: "HorariosLaborales");

            migrationBuilder.DropIndex(
                name: "IX_HorariosLaborales_PeluqueroId_DiaSemana",
                table: "HorariosLaborales");

            migrationBuilder.DropIndex(
                name: "IX_HistorialSesiones_UsuarioId",
                table: "HistorialSesiones");

            migrationBuilder.DropIndex(
                name: "IX_HistorialSesiones_UsuarioId_FechaIntento",
                table: "HistorialSesiones");

            migrationBuilder.DropIndex(
                name: "IX_HistorialEstadosTurno_TurnoId",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropIndex(
                name: "IX_HistorialEstadosTurno_TurnoId_FechaCambio",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropIndex(
                name: "IX_FichasTecnicas_ClienteId",
                table: "FichasTecnicas");

            migrationBuilder.DropIndex(
                name: "IX_FichasTecnicas_ClienteId_FechaRegistro",
                table: "FichasTecnicas");

            migrationBuilder.DropIndex(
                name: "UX_EstadosTurno_Nombre",
                table: "EstadosTurno");

            migrationBuilder.DropIndex(
                name: "UX_Especialidades_Local_Nombre",
                table: "Especialidades");

            migrationBuilder.DropIndex(
                name: "UX_ConfiguracionSalon_Tenant_Sucursal",
                table: "ConfiguracionSalon");

            migrationBuilder.DropIndex(
                name: "IX_ComisionesConfig_PeluqueroId",
                table: "ComisionesConfig");

            migrationBuilder.DropIndex(
                name: "UX_ComisionesConfig_Local_Peluquero_Servicio",
                table: "ComisionesConfig");

            migrationBuilder.DropIndex(
                name: "UX_Clientes_Local_Email",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "UX_Clientes_Local_Telefono",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "UX_CategoriasServicio_Local_Nombre",
                table: "CategoriasServicio");

            migrationBuilder.DropIndex(
                name: "UX_CategoriasProducto_Local_Nombre",
                table: "CategoriasProducto");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorario_PeluqueroId",
                table: "BloqueosHorario");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorario_PeluqueroId_Fechas",
                table: "BloqueosHorario");

            migrationBuilder.DropIndex(
                name: "IX_AuditLogs_Entidad_Clave_Fecha",
                table: "AuditLogs");

            migrationBuilder.DeleteData(
                table: "CategoriasProducto",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CategoriasProducto",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "CategoriasServicio",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Clientes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ComisionesConfig",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "ConfiguracionSalon",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "HorariosLaborales",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "HorariosLaborales",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "HorariosLaborales",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "HorariosLaborales",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "HorariosLaborales",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "HorariosLaborales",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "PeluqueroEspecialidades",
                keyColumn: "Id",
                keyColumnType: "int",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "PeluqueroEspecialidades",
                keyColumn: "Id",
                keyColumnType: "int",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Productos",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Servicios",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Servicios",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "ServiciosInsumosRecetas",
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
                table: "TiposCabello",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "TiposCabello",
                keyColumn: "Id",
                keyValue: 6);

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

            migrationBuilder.DeleteData(
                table: "TiposRostro",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                table: "CategoriasProducto",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "CategoriasServicio",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CategoriasServicio",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Especialidades",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Peluqueros",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Productos",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Servicios",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "TiposCabello",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "TiposRostro",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "CategoriasProducto",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "CategoriasServicio",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Proveedores",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "DescuentoSenaAplicado",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "IdempotencyKey",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "Subtotal",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Ventas");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "TurnoServicios");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "FechaPagoSena",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "MontoSena",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "ReferenciaPagoExterno",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "RequiereSena",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "SenaPagada",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "TiposRostro");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "TiposRostro");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TiposRostro");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "TiposRostro");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "TiposCabello");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "TiposCabello");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "TiposCabello");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "TiposCabello");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Tenants");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SuscripcionesTenant");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "SuscripcionesTenant");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Sucursales");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Sucursales");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "ObservacionesCierre",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "TotalEgresosManuales",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "TotalIngresosManuales",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "TotalVentasDigital",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "TotalVentasEfectivo",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "SesionesCaja");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ServiciosInsumosRecetas");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "ServiciosInsumosRecetas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ServiciosInsumosRecetas");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ServiciosInsumosRecetas");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Roles");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "Cuit",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Proveedores");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PlanesSuscripcion");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "PlanesSuscripcion");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Peluqueros");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Peluqueros");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Peluqueros");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Peluqueros");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Peluqueros");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "CreatedAt",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "DeletedAt",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "IsDeleted",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "PeluqueroEspecialidades");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "ReferenciaOperacion",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Pagos");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "OrdenesCompra");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "SaldoResultante",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MovimientosStock");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MovimientosCaja");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MotivosCancelacion");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "MotivosCancelacion");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MotivosCancelacion");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MotivosCancelacion");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "MediosPago");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "MediosPago");

            migrationBuilder.DropColumn(
                name: "EsEfectivoFisico",
                table: "MediosPago");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "MediosPago");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "MediosPago");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "MediosPago");

            migrationBuilder.DropColumn(
                name: "Bonificaciones",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "Descuentos",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "FechaEmision",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "Observaciones",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "UsuarioLiquidacionId",
                table: "Liquidaciones");

            migrationBuilder.DropColumn(
                name: "Activo",
                table: "HorariosLaborales");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HorariosLaborales");

            migrationBuilder.DropColumn(
                name: "HoraFinDescanso",
                table: "HorariosLaborales");

            migrationBuilder.DropColumn(
                name: "HoraInicioDescanso",
                table: "HorariosLaborales");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "HorariosLaborales");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HorariosLaborales");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "HorariosLaborales");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HistorialSesiones");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "HistorialSesiones");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HistorialSesiones");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "HistorialSesiones");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HistorialPreciosServicio");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "HistorialPreciosServicio");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HistorialPreciosServicio");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "HistorialPreciosServicio");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HistorialPreciosProducto");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "HistorialPreciosProducto");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HistorialPreciosProducto");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "HistorialPreciosProducto");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "FichasTecnicas");

            migrationBuilder.DropColumn(
                name: "FormulaAplicada",
                table: "FichasTecnicas");

            migrationBuilder.DropColumn(
                name: "FotoResultadoUrl",
                table: "FichasTecnicas");

            migrationBuilder.DropColumn(
                name: "ProductosUtilizados",
                table: "FichasTecnicas");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "FichasTecnicas");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "FichasTecnicas");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "FichasTecnicas");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "EstadosTurno");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "EstadosTurno");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Especialidades");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "Especialidades");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "Especialidades");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Especialidades");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "Especialidades");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "DetallesVenta");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DetallesOrdenCompra");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "DetallesOrdenCompra");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DetallesOrdenCompra");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "DetallesOrdenCompra");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "DetallesLiquidacion");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "DetallesLiquidacion");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "DetallesLiquidacion");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "DetallesLiquidacion");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ConfiguracionSalon");

            migrationBuilder.DropColumn(
                name: "HorarioAtencionTexto",
                table: "ConfiguracionSalon");

            migrationBuilder.DropColumn(
                name: "NombreComercial",
                table: "ConfiguracionSalon");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "ConfiguracionSalon");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ConfiguracionSalon");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ConfiguracionSalon");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "ComisionesConfig");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "ComisionesConfig");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "ComisionesConfig");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "ComisionesConfig");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CategoriasServicio");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "CategoriasServicio");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "CategoriasServicio");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CategoriasServicio");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "CategoriasServicio");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "CategoriasProducto");

            migrationBuilder.DropColumn(
                name: "Descripcion",
                table: "CategoriasProducto");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "CategoriasProducto");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "CategoriasProducto");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "CategoriasProducto");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "BloqueosHorario");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "BloqueosHorario");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "BloqueosHorario");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "BloqueosHorario");

            migrationBuilder.DropColumn(
                name: "DeletedByUserId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "SucursalId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "TenantId",
                table: "AuditLogs");

            migrationBuilder.DropColumn(
                name: "UpdatedByUserId",
                table: "AuditLogs");

            migrationBuilder.RenameColumn(
                name: "UpdatedByUserId",
                table: "Usuarios",
                newName: "RolId1");

            migrationBuilder.RenameColumn(
                name: "UpdatedByUserId",
                table: "Turnos",
                newName: "EstadoTurnoId");

            migrationBuilder.RenameColumn(
                name: "UpdatedByUserId",
                table: "Servicios",
                newName: "CategoriaServicioId");

            migrationBuilder.RenameColumn(
                name: "UpdatedByUserId",
                table: "Productos",
                newName: "CategoriaProductoId");

            migrationBuilder.RenameColumn(
                name: "Observaciones",
                table: "FichasTecnicas",
                newName: "DetalleFormulaTintura");

            migrationBuilder.RenameColumn(
                name: "FechaRegistro",
                table: "FichasTecnicas",
                newName: "Fecha");

            migrationBuilder.RenameColumn(
                name: "UpdatedByUserId",
                table: "Clientes",
                newName: "TipoRostroId1");

            migrationBuilder.RenameColumn(
                name: "SucursalId",
                table: "Clientes",
                newName: "TipoCabelloId1");

            migrationBuilder.AddPrimaryKey(
                name: "PK_TurnoServicios",
                table: "TurnoServicios",
                columns: new[] { "TurnoId", "ServicioId" });

            migrationBuilder.AddPrimaryKey(
                name: "PK_PeluqueroEspecialidades",
                table: "PeluqueroEspecialidades",
                columns: new[] { "PeluqueroId", "EspecialidadId" });

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

            migrationBuilder.UpdateData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "MotivosCancelacion",
                keyColumn: "Id",
                keyValue: 4,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "IncluyeWhatsAppRecordatorios" },
                values: new object[] { new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), true });

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 2,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "PlanesSuscripcion",
                keyColumn: "Id",
                keyValue: 3,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

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

            migrationBuilder.UpdateData(
                table: "Sucursales",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "SuscripcionesTenant",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "FechaInicio", "FechaProximoVencimiento" },
                values: new object[] { new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc) });

            migrationBuilder.UpdateData(
                table: "Tenants",
                keyColumn: "Id",
                keyValue: 1,
                column: "CreatedAt",
                value: new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "CreatedAt", "FechaAceptacionTerminos", "PasswordHash" },
                values: new object[] { new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, "$2a$11$0nN01jB5P169DXZgN/.hCeeNInhV/9tYkH.fK5v4a9jR1LqFkI5C6" });

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_Fecha_Dashboard",
                table: "Ventas",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_SesionCajaId_Fecha",
                table: "Ventas",
                columns: new[] { "SesionCajaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Usuarios_RolId1",
                table: "Usuarios",
                column: "RolId1");

            migrationBuilder.CreateIndex(
                name: "UX_Usuarios_Email_Active",
                table: "Usuarios",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Usuarios_Username_Active",
                table: "Usuarios",
                column: "Username",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ClienteId_FechaHoraInicio",
                table: "Turnos",
                columns: new[] { "ClienteId", "FechaHoraInicio" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Disponibilidad_Peluquero_Fecha_Estado",
                table: "Turnos",
                columns: new[] { "PeluqueroId", "FechaHoraInicio", "EstadoId" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_EstadoTurnoId",
                table: "Turnos",
                column: "EstadoTurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursales_TenantId",
                table: "Sucursales",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SesionesCaja_EstadoAbierta_FechaApertura",
                table: "SesionesCaja",
                columns: new[] { "EstadoAbierta", "FechaApertura" });

            migrationBuilder.CreateIndex(
                name: "UX_ServiciosInsumosRecetas_Servicio_Producto",
                table: "ServiciosInsumosRecetas",
                columns: new[] { "ServicioId", "ProductoId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_CategoriaId_Activo",
                table: "Servicios",
                columns: new[] { "CategoriaId", "Activo" });

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_CategoriaServicioId",
                table: "Servicios",
                column: "CategoriaServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CategoriaProductoId",
                table: "Productos",
                column: "CategoriaProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_StockCritico",
                table: "Productos",
                columns: new[] { "StockActual", "StockMinimoAlerta" },
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Productos_CodigoBarras_Active",
                table: "Productos",
                column: "CodigoBarras",
                unique: true,
                filter: "[CodigoBarras] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UX_Peluqueros_Email_Active",
                table: "Peluqueros",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_Proveedor_Estado_Fecha",
                table: "OrdenesCompra",
                columns: new[] { "ProveedorId", "Estado", "FechaEmision" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_Kardex_ProductoId_Fecha",
                table: "MovimientosStock",
                columns: new[] { "ProductoId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_SesionCajaId_Fecha",
                table: "MovimientosCaja",
                columns: new[] { "SesionCajaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Liquidaciones_PeluqueroId_Estado_FechaDesde",
                table: "Liquidaciones",
                columns: new[] { "PeluqueroId", "Estado", "FechaDesde" });

            migrationBuilder.CreateIndex(
                name: "IX_HorariosLaborales_PeluqueroId_DiaSemana",
                table: "HorariosLaborales",
                columns: new[] { "PeluqueroId", "DiaSemana" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialSesiones_UsuarioId_FechaIntento",
                table: "HistorialSesiones",
                columns: new[] { "UsuarioId", "FechaIntento" });

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadosTurno_TurnoId_FechaCambio",
                table: "HistorialEstadosTurno",
                columns: new[] { "TurnoId", "FechaCambio" });

            migrationBuilder.CreateIndex(
                name: "IX_FichasTecnicas_ClienteId_Fecha",
                table: "FichasTecnicas",
                columns: new[] { "ClienteId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "UX_ComisionesConfig_Peluquero_Servicio",
                table: "ComisionesConfig",
                columns: new[] { "PeluqueroId", "ServicioId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Telefono",
                table: "Clientes",
                column: "Telefono",
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_TipoCabelloId1",
                table: "Clientes",
                column: "TipoCabelloId1");

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_TipoRostroId1",
                table: "Clientes",
                column: "TipoRostroId1");

            migrationBuilder.CreateIndex(
                name: "UX_Clientes_Email_Active",
                table: "Clientes",
                column: "Email",
                unique: true,
                filter: "[Email] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorario_PeluqueroId_Fechas",
                table: "BloqueosHorario",
                columns: new[] { "PeluqueroId", "FechaInicio", "FechaFin" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entidad_Clave_Fecha",
                table: "AuditLogs",
                columns: new[] { "EntidadNombre", "ClavePrimaria", "FechaUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_TiposCabello_TipoCabelloId1",
                table: "Clientes",
                column: "TipoCabelloId1",
                principalTable: "TiposCabello",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Clientes_TiposRostro_TipoRostroId1",
                table: "Clientes",
                column: "TipoRostroId1",
                principalTable: "TiposRostro",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_PeluqueroEspecialidades_Peluqueros_PeluqueroId",
                table: "PeluqueroEspecialidades",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CategoriasProducto_CategoriaProductoId",
                table: "Productos",
                column: "CategoriaProductoId",
                principalTable: "CategoriasProducto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Servicios_CategoriasServicio_CategoriaServicioId",
                table: "Servicios",
                column: "CategoriaServicioId",
                principalTable: "CategoriasServicio",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Turnos_EstadosTurno_EstadoTurnoId",
                table: "Turnos",
                column: "EstadoTurnoId",
                principalTable: "EstadosTurno",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_TurnoServicios_Turnos_TurnoId",
                table: "TurnoServicios",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Roles_RolId1",
                table: "Usuarios",
                column: "RolId1",
                principalTable: "Roles",
                principalColumn: "Id");
        }
    }
}
