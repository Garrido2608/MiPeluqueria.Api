using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace MiPeluqueria.Api.Migrations
{
    /// <inheritdoc />
    public partial class Fase2_SaaS_Completa_Final : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BloqueosHorario_Peluqueros_PeluqueroId",
                table: "BloqueosHorario");

            migrationBuilder.DropForeignKey(
                name: "FK_ComisionesConfigs_Peluqueros_PeluqueroId",
                table: "ComisionesConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_ComisionesConfigs_Servicios_ServicioId",
                table: "ComisionesConfigs");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallesLiquidacion_Liquidaciones_LiquidacionId",
                table: "DetallesLiquidacion");

            migrationBuilder.DropForeignKey(
                name: "FK_FichasTecnicas_Turnos_TurnoId",
                table: "FichasTecnicas");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialEstadoTurnos_EstadosTurno_EstadoAnteriorId",
                table: "HistorialEstadoTurnos");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialEstadoTurnos_EstadosTurno_EstadoNuevoId",
                table: "HistorialEstadoTurnos");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialEstadoTurnos_Turnos_TurnoId",
                table: "HistorialEstadoTurnos");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialSesiones_Usuarios_UsuarioId",
                table: "HistorialSesiones");

            migrationBuilder.DropForeignKey(
                name: "FK_HorariosLaborales_Peluqueros_PeluqueroId",
                table: "HorariosLaborales");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Proveedores_ProveedorId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Peluqueros_PeluqueroId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Turnos_TurnoId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Ventas_SesionCajaId",
                table: "Ventas");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_Agenda_Solapamiento",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Turnos_ClienteId",
                table: "Turnos");

            migrationBuilder.DropIndex(
                name: "IX_Servicios_CategoriaId",
                table: "Servicios");

            migrationBuilder.DropIndex(
                name: "IX_Productos_CodigoBarras",
                table: "Productos");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosStock_ProductoId",
                table: "MovimientosStock");

            migrationBuilder.DropIndex(
                name: "IX_MovimientosCaja_SesionCajaId",
                table: "MovimientosCaja");

            migrationBuilder.DropIndex(
                name: "IX_Liquidaciones_PeluqueroId",
                table: "Liquidaciones");

            migrationBuilder.DropIndex(
                name: "IX_HorariosLaborales_PeluqueroId",
                table: "HorariosLaborales");

            migrationBuilder.DropIndex(
                name: "IX_HistorialSesiones_UsuarioId",
                table: "HistorialSesiones");

            migrationBuilder.DropIndex(
                name: "IX_FichasTecnicas_ClienteId",
                table: "FichasTecnicas");

            migrationBuilder.DropIndex(
                name: "IX_DetallesVenta_Liquidaciones",
                table: "DetallesVenta");

            migrationBuilder.DropIndex(
                name: "IX_Clientes_Email",
                table: "Clientes");

            migrationBuilder.DropIndex(
                name: "IX_BloqueosHorario_PeluqueroId",
                table: "BloqueosHorario");

            migrationBuilder.DropPrimaryKey(
                name: "PK_HistorialEstadoTurnos",
                table: "HistorialEstadoTurnos");

            migrationBuilder.DropIndex(
                name: "IX_HistorialEstadoTurnos_TurnoId",
                table: "HistorialEstadoTurnos");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComisionesConfigs",
                table: "ComisionesConfigs");

            migrationBuilder.DropIndex(
                name: "IX_ComisionesConfigs_PeluqueroId",
                table: "ComisionesConfigs");

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

            migrationBuilder.RenameTable(
                name: "HistorialEstadoTurnos",
                newName: "HistorialEstadosTurno");

            migrationBuilder.RenameTable(
                name: "ComisionesConfigs",
                newName: "ComisionesConfig");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialEstadoTurnos_EstadoNuevoId",
                table: "HistorialEstadosTurno",
                newName: "IX_HistorialEstadosTurno_EstadoNuevoId");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialEstadoTurnos_EstadoAnteriorId",
                table: "HistorialEstadosTurno",
                newName: "IX_HistorialEstadosTurno_EstadoAnteriorId");

            migrationBuilder.RenameIndex(
                name: "IX_ComisionesConfigs_ServicioId",
                table: "ComisionesConfig",
                newName: "IX_ComisionesConfig_ServicioId");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "RolId1",
                table: "Usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "EstadoTurnoId",
                table: "Turnos",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoriaServicioId",
                table: "Servicios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CategoriaProductoId",
                table: "Productos",
                type: "int",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "ColorAgendaHex",
                table: "Peluqueros",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                defaultValue: "#3B82F6",
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7);

            migrationBuilder.AddColumn<int>(
                name: "TipoCabelloId1",
                table: "Clientes",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TipoRostroId1",
                table: "Clientes",
                type: "int",
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistorialEstadosTurno",
                table: "HistorialEstadosTurno",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComisionesConfig",
                table: "ComisionesConfig",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: true),
                    EntidadNombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Accion = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ClavePrimaria = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    ValoresAnterioresJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ValoresNuevosJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IpAddress = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfiguracionSalon",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreSalon = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    RazonSocial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Cuit = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    EmailContacto = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    MonedaIso = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "ARS"),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "America/Argentina/Cordoba"),
                    IntervaloTurnosMinutos = table.Column<int>(type: "int", nullable: false),
                    MinutosToleranciaEspera = table.Column<int>(type: "int", nullable: false),
                    HorasAnticipacionCancelacion = table.Column<int>(type: "int", nullable: false),
                    PorcentajeSenaReserva = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    NotificacionesWhatsAppActivas = table.Column<bool>(type: "bit", nullable: false),
                    NotificacionesEmailActivas = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfiguracionSalon", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPreciosProducto",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    PrecioCostoAnterior = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioCostoNuevo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioVentaAnterior = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioVentaNuevo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPreciosProducto", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialPreciosProducto_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HistorialPreciosServicio",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServicioId = table.Column<int>(type: "int", nullable: false),
                    PrecioAnterior = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PrecioNuevo = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    FechaCambio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UsuarioModificacionId = table.Column<int>(type: "int", nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HistorialPreciosServicio", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HistorialPreciosServicio_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "OrdenesCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProveedorId = table.Column<int>(type: "int", nullable: false),
                    NumeroComprobanteProveedor = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    FechaEmision = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaRecepcion = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    Total = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Observaciones = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    UsuarioCreacionId = table.Column<int>(type: "int", nullable: false),
                    UsuarioRecepcionId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrdenesCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrdenesCompra_Proveedores_ProveedorId",
                        column: x => x.ProveedorId,
                        principalTable: "Proveedores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PlanesSuscripcion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Nombre = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PrecioMensualUsd = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxSucursales = table.Column<int>(type: "int", nullable: false),
                    MaxPeluqueros = table.Column<int>(type: "int", nullable: false),
                    IncluyeWhatsAppRecordatorios = table.Column<bool>(type: "bit", nullable: false),
                    IncluyePortalReservasOnline = table.Column<bool>(type: "bit", nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanesSuscripcion", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefreshTokens",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UsuarioId = table.Column<int>(type: "int", nullable: false),
                    Token = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RevokedByIp = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    ReplacedByToken = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ReasonRevoked = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefreshTokens", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefreshTokens_Usuarios_UsuarioId",
                        column: x => x.UsuarioId,
                        principalTable: "Usuarios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ServiciosInsumosRecetas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServicioId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    CantidadUnidadesDescontar = table.Column<int>(type: "int", nullable: false),
                    CantidadMedidaEstandar = table.Column<decimal>(type: "decimal(10,2)", nullable: false),
                    UnidadMedida = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    DescuentoAutomaticoAlCompletarTurno = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiciosInsumosRecetas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiciosInsumosRecetas_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ServiciosInsumosRecetas_Servicios_ServicioId",
                        column: x => x.ServicioId,
                        principalTable: "Servicios",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    NombreComercial = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SubdominioSlug = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    IdentificacionFiscal = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: true),
                    EmailPropietario = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PaisIso = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Activo = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DetallesOrdenCompra",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrdenCompraId = table.Column<int>(type: "int", nullable: false),
                    ProductoId = table.Column<int>(type: "int", nullable: false),
                    CantidadPedida = table.Column<int>(type: "int", nullable: false),
                    CantidadRecibida = table.Column<int>(type: "int", nullable: false),
                    PrecioUnitarioCosto = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Subtotal = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DetallesOrdenCompra", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenCompra_OrdenesCompra_OrdenCompraId",
                        column: x => x.OrdenCompraId,
                        principalTable: "OrdenesCompra",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DetallesOrdenCompra_Productos_ProductoId",
                        column: x => x.ProductoId,
                        principalTable: "Productos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Sucursales",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Direccion = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    Telefono = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    TimeZoneId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "America/Argentina/Cordoba"),
                    MonedaIso = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false, defaultValue: "ARS"),
                    EsCasaMatriz = table.Column<bool>(type: "bit", nullable: false),
                    Activa = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sucursales", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sucursales_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SuscripcionesTenant",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenantId = table.Column<int>(type: "int", nullable: false),
                    PlanSuscripcionId = table.Column<int>(type: "int", nullable: false),
                    FechaInicio = table.Column<DateTime>(type: "datetime2", nullable: false),
                    FechaProximoVencimiento = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    ExternalSubscriptionId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SuscripcionesTenant", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SuscripcionesTenant_PlanesSuscripcion_PlanSuscripcionId",
                        column: x => x.PlanSuscripcionId,
                        principalTable: "PlanesSuscripcion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SuscripcionesTenant_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "PlanesSuscripcion",
                columns: new[] { "Id", "Activo", "CreatedAt", "CreatedByUserId", "DeletedAt", "IncluyePortalReservasOnline", "IncluyeWhatsAppRecordatorios", "IsDeleted", "MaxPeluqueros", "MaxSucursales", "Nombre", "PrecioMensualUsd", "UpdatedAt" },
                values: new object[,]
                {
                    { 1, true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, true, false, 3, 1, "Starter", 19.99m, null },
                    { 2, true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, true, false, 15, 3, "Professional", 39.99m, null },
                    { 3, true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, true, true, false, 999, 99, "Enterprise", 79.99m, null }
                });

            migrationBuilder.InsertData(
                table: "Tenants",
                columns: new[] { "Id", "Activo", "CreatedAt", "CreatedByUserId", "DeletedAt", "EmailPropietario", "IdentificacionFiscal", "IsDeleted", "NombreComercial", "PaisIso", "SubdominioSlug", "UpdatedAt" },
                values: new object[] { 1, true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "admin@tesis.com", "30-71234567-9", false, "Mi Peluquería Central", "AR", "mi-peluqueria-central", null });

            migrationBuilder.UpdateData(
                table: "Usuarios",
                keyColumn: "Id",
                keyValue: 1,
                column: "RolId1",
                value: null);

            migrationBuilder.InsertData(
                table: "Sucursales",
                columns: new[] { "Id", "Activa", "CreatedAt", "CreatedByUserId", "DeletedAt", "Direccion", "EsCasaMatriz", "IsDeleted", "MonedaIso", "Nombre", "Telefono", "TenantId", "TimeZoneId", "UpdatedAt" },
                values: new object[] { 1, true, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, "Av. Colón 500, Córdoba", true, false, "ARS", "Sucursal Casa Matriz (Córdoba)", "+54 9 351 000-0000", 1, "America/Argentina/Cordoba", null });

            migrationBuilder.InsertData(
                table: "SuscripcionesTenant",
                columns: new[] { "Id", "CreatedAt", "CreatedByUserId", "DeletedAt", "Estado", "ExternalSubscriptionId", "FechaInicio", "FechaProximoVencimiento", "IsDeleted", "PlanSuscripcionId", "TenantId", "UpdatedAt" },
                values: new object[] { 1, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), null, null, 2, null, new DateTime(2026, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), new DateTime(2027, 10, 1, 0, 0, 0, 0, DateTimeKind.Utc), false, 2, 1, null });

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
                name: "IX_SesionesCaja_EstadoAbierta_FechaApertura",
                table: "SesionesCaja",
                columns: new[] { "EstadoAbierta", "FechaApertura" });

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
                name: "IX_FichasTecnicas_ClienteId_Fecha",
                table: "FichasTecnicas",
                columns: new[] { "ClienteId", "Fecha" });

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
                name: "IX_HistorialEstadosTurno_TurnoId_FechaCambio",
                table: "HistorialEstadosTurno",
                columns: new[] { "TurnoId", "FechaCambio" });

            migrationBuilder.CreateIndex(
                name: "UX_ComisionesConfig_Peluquero_Servicio",
                table: "ComisionesConfig",
                columns: new[] { "PeluqueroId", "ServicioId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entidad_Clave_Fecha",
                table: "AuditLogs",
                columns: new[] { "EntidadNombre", "ClavePrimaria", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenCompra_OrdenCompraId",
                table: "DetallesOrdenCompra",
                column: "OrdenCompraId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesOrdenCompra_ProductoId",
                table: "DetallesOrdenCompra",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPreciosProducto_ProductoId",
                table: "HistorialPreciosProducto",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialPreciosServicio_ServicioId",
                table: "HistorialPreciosServicio",
                column: "ServicioId");

            migrationBuilder.CreateIndex(
                name: "IX_OrdenesCompra_Proveedor_Estado_Fecha",
                table: "OrdenesCompra",
                columns: new[] { "ProveedorId", "Estado", "FechaEmision" });

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_UsuarioId",
                table: "RefreshTokens",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "UX_RefreshTokens_Token",
                table: "RefreshTokens",
                column: "Token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiciosInsumosRecetas_ProductoId",
                table: "ServiciosInsumosRecetas",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "UX_ServiciosInsumosRecetas_Servicio_Producto",
                table: "ServiciosInsumosRecetas",
                columns: new[] { "ServicioId", "ProductoId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Sucursales_TenantId",
                table: "Sucursales",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "IX_SuscripcionesTenant_PlanSuscripcionId",
                table: "SuscripcionesTenant",
                column: "PlanSuscripcionId");

            migrationBuilder.CreateIndex(
                name: "IX_SuscripcionesTenant_TenantId",
                table: "SuscripcionesTenant",
                column: "TenantId");

            migrationBuilder.CreateIndex(
                name: "UX_Tenants_SubdominioSlug",
                table: "Tenants",
                column: "SubdominioSlug",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.AddForeignKey(
                name: "FK_BloqueosHorario_Peluqueros_PeluqueroId",
                table: "BloqueosHorario",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

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
                name: "FK_ComisionesConfig_Peluqueros_PeluqueroId",
                table: "ComisionesConfig",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ComisionesConfig_Servicios_ServicioId",
                table: "ComisionesConfig",
                column: "ServicioId",
                principalTable: "Servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesLiquidacion_Liquidaciones_LiquidacionId",
                table: "DetallesLiquidacion",
                column: "LiquidacionId",
                principalTable: "Liquidaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_FichasTecnicas_Turnos_TurnoId",
                table: "FichasTecnicas",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialEstadosTurno_EstadosTurno_EstadoAnteriorId",
                table: "HistorialEstadosTurno",
                column: "EstadoAnteriorId",
                principalTable: "EstadosTurno",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialEstadosTurno_EstadosTurno_EstadoNuevoId",
                table: "HistorialEstadosTurno",
                column: "EstadoNuevoId",
                principalTable: "EstadosTurno",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialEstadosTurno_Turnos_TurnoId",
                table: "HistorialEstadosTurno",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialSesiones_Usuarios_UsuarioId",
                table: "HistorialSesiones",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_HorariosLaborales_Peluqueros_PeluqueroId",
                table: "HorariosLaborales",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_CategoriasProducto_CategoriaProductoId",
                table: "Productos",
                column: "CategoriaProductoId",
                principalTable: "CategoriasProducto",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Proveedores_ProveedorId",
                table: "Productos",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

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
                name: "FK_Usuarios_Clientes_ClienteId",
                table: "Usuarios",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Peluqueros_PeluqueroId",
                table: "Usuarios",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Roles_RolId1",
                table: "Usuarios",
                column: "RolId1",
                principalTable: "Roles",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Turnos_TurnoId",
                table: "Ventas",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BloqueosHorario_Peluqueros_PeluqueroId",
                table: "BloqueosHorario");

            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_TiposCabello_TipoCabelloId1",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_Clientes_TiposRostro_TipoRostroId1",
                table: "Clientes");

            migrationBuilder.DropForeignKey(
                name: "FK_ComisionesConfig_Peluqueros_PeluqueroId",
                table: "ComisionesConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_ComisionesConfig_Servicios_ServicioId",
                table: "ComisionesConfig");

            migrationBuilder.DropForeignKey(
                name: "FK_DetallesLiquidacion_Liquidaciones_LiquidacionId",
                table: "DetallesLiquidacion");

            migrationBuilder.DropForeignKey(
                name: "FK_FichasTecnicas_Turnos_TurnoId",
                table: "FichasTecnicas");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialEstadosTurno_EstadosTurno_EstadoAnteriorId",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialEstadosTurno_EstadosTurno_EstadoNuevoId",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialEstadosTurno_Turnos_TurnoId",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropForeignKey(
                name: "FK_HistorialSesiones_Usuarios_UsuarioId",
                table: "HistorialSesiones");

            migrationBuilder.DropForeignKey(
                name: "FK_HorariosLaborales_Peluqueros_PeluqueroId",
                table: "HorariosLaborales");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_CategoriasProducto_CategoriaProductoId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Productos_Proveedores_ProveedorId",
                table: "Productos");

            migrationBuilder.DropForeignKey(
                name: "FK_Servicios_CategoriasServicio_CategoriaServicioId",
                table: "Servicios");

            migrationBuilder.DropForeignKey(
                name: "FK_Turnos_EstadosTurno_EstadoTurnoId",
                table: "Turnos");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Peluqueros_PeluqueroId",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Usuarios_Roles_RolId1",
                table: "Usuarios");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas");

            migrationBuilder.DropForeignKey(
                name: "FK_Ventas_Turnos_TurnoId",
                table: "Ventas");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ConfiguracionSalon");

            migrationBuilder.DropTable(
                name: "DetallesOrdenCompra");

            migrationBuilder.DropTable(
                name: "HistorialPreciosProducto");

            migrationBuilder.DropTable(
                name: "HistorialPreciosServicio");

            migrationBuilder.DropTable(
                name: "RefreshTokens");

            migrationBuilder.DropTable(
                name: "ServiciosInsumosRecetas");

            migrationBuilder.DropTable(
                name: "Sucursales");

            migrationBuilder.DropTable(
                name: "SuscripcionesTenant");

            migrationBuilder.DropTable(
                name: "OrdenesCompra");

            migrationBuilder.DropTable(
                name: "PlanesSuscripcion");

            migrationBuilder.DropTable(
                name: "Tenants");

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
                name: "IX_SesionesCaja_EstadoAbierta_FechaApertura",
                table: "SesionesCaja");

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
                name: "IX_FichasTecnicas_ClienteId_Fecha",
                table: "FichasTecnicas");

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

            migrationBuilder.DropPrimaryKey(
                name: "PK_HistorialEstadosTurno",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropIndex(
                name: "IX_HistorialEstadosTurno_TurnoId_FechaCambio",
                table: "HistorialEstadosTurno");

            migrationBuilder.DropPrimaryKey(
                name: "PK_ComisionesConfig",
                table: "ComisionesConfig");

            migrationBuilder.DropIndex(
                name: "UX_ComisionesConfig_Peluquero_Servicio",
                table: "ComisionesConfig");

            migrationBuilder.DropColumn(
                name: "RolId1",
                table: "Usuarios");

            migrationBuilder.DropColumn(
                name: "EstadoTurnoId",
                table: "Turnos");

            migrationBuilder.DropColumn(
                name: "CategoriaServicioId",
                table: "Servicios");

            migrationBuilder.DropColumn(
                name: "CategoriaProductoId",
                table: "Productos");

            migrationBuilder.DropColumn(
                name: "TipoCabelloId1",
                table: "Clientes");

            migrationBuilder.DropColumn(
                name: "TipoRostroId1",
                table: "Clientes");

            migrationBuilder.RenameTable(
                name: "HistorialEstadosTurno",
                newName: "HistorialEstadoTurnos");

            migrationBuilder.RenameTable(
                name: "ComisionesConfig",
                newName: "ComisionesConfigs");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialEstadosTurno_EstadoNuevoId",
                table: "HistorialEstadoTurnos",
                newName: "IX_HistorialEstadoTurnos_EstadoNuevoId");

            migrationBuilder.RenameIndex(
                name: "IX_HistorialEstadosTurno_EstadoAnteriorId",
                table: "HistorialEstadoTurnos",
                newName: "IX_HistorialEstadoTurnos_EstadoAnteriorId");

            migrationBuilder.RenameIndex(
                name: "IX_ComisionesConfig_ServicioId",
                table: "ComisionesConfigs",
                newName: "IX_ComisionesConfigs_ServicioId");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "Usuarios",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(500)",
                oldMaxLength: 500);

            migrationBuilder.AlterColumn<string>(
                name: "ColorAgendaHex",
                table: "Peluqueros",
                type: "nvarchar(7)",
                maxLength: 7,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(7)",
                oldMaxLength: 7,
                oldDefaultValue: "#3B82F6");

            migrationBuilder.AddPrimaryKey(
                name: "PK_HistorialEstadoTurnos",
                table: "HistorialEstadoTurnos",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_ComisionesConfigs",
                table: "ComisionesConfigs",
                column: "Id");

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

            migrationBuilder.CreateIndex(
                name: "IX_Ventas_SesionCajaId",
                table: "Ventas",
                column: "SesionCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_Agenda_Solapamiento",
                table: "Turnos",
                columns: new[] { "PeluqueroId", "FechaHoraInicio", "FechaHoraFin", "EstadoId" });

            migrationBuilder.CreateIndex(
                name: "IX_Turnos_ClienteId",
                table: "Turnos",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_Servicios_CategoriaId",
                table: "Servicios",
                column: "CategoriaId");

            migrationBuilder.CreateIndex(
                name: "IX_Productos_CodigoBarras",
                table: "Productos",
                column: "CodigoBarras");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosStock_ProductoId",
                table: "MovimientosStock",
                column: "ProductoId");

            migrationBuilder.CreateIndex(
                name: "IX_MovimientosCaja_SesionCajaId",
                table: "MovimientosCaja",
                column: "SesionCajaId");

            migrationBuilder.CreateIndex(
                name: "IX_Liquidaciones_PeluqueroId",
                table: "Liquidaciones",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "IX_HorariosLaborales_PeluqueroId",
                table: "HorariosLaborales",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialSesiones_UsuarioId",
                table: "HistorialSesiones",
                column: "UsuarioId");

            migrationBuilder.CreateIndex(
                name: "IX_FichasTecnicas_ClienteId",
                table: "FichasTecnicas",
                column: "ClienteId");

            migrationBuilder.CreateIndex(
                name: "IX_DetallesVenta_Liquidaciones",
                table: "DetallesVenta",
                columns: new[] { "PeluqueroId", "TipoItem" });

            migrationBuilder.CreateIndex(
                name: "IX_Clientes_Email",
                table: "Clientes",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_BloqueosHorario_PeluqueroId",
                table: "BloqueosHorario",
                column: "PeluqueroId");

            migrationBuilder.CreateIndex(
                name: "IX_HistorialEstadoTurnos_TurnoId",
                table: "HistorialEstadoTurnos",
                column: "TurnoId");

            migrationBuilder.CreateIndex(
                name: "IX_ComisionesConfigs_PeluqueroId",
                table: "ComisionesConfigs",
                column: "PeluqueroId");

            migrationBuilder.AddForeignKey(
                name: "FK_BloqueosHorario_Peluqueros_PeluqueroId",
                table: "BloqueosHorario",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ComisionesConfigs_Peluqueros_PeluqueroId",
                table: "ComisionesConfigs",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ComisionesConfigs_Servicios_ServicioId",
                table: "ComisionesConfigs",
                column: "ServicioId",
                principalTable: "Servicios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_DetallesLiquidacion_Liquidaciones_LiquidacionId",
                table: "DetallesLiquidacion",
                column: "LiquidacionId",
                principalTable: "Liquidaciones",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FichasTecnicas_Turnos_TurnoId",
                table: "FichasTecnicas",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialEstadoTurnos_EstadosTurno_EstadoAnteriorId",
                table: "HistorialEstadoTurnos",
                column: "EstadoAnteriorId",
                principalTable: "EstadosTurno",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialEstadoTurnos_EstadosTurno_EstadoNuevoId",
                table: "HistorialEstadoTurnos",
                column: "EstadoNuevoId",
                principalTable: "EstadosTurno",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialEstadoTurnos_Turnos_TurnoId",
                table: "HistorialEstadoTurnos",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HistorialSesiones_Usuarios_UsuarioId",
                table: "HistorialSesiones",
                column: "UsuarioId",
                principalTable: "Usuarios",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_HorariosLaborales_Peluqueros_PeluqueroId",
                table: "HorariosLaborales",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Productos_Proveedores_ProveedorId",
                table: "Productos",
                column: "ProveedorId",
                principalTable: "Proveedores",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Clientes_ClienteId",
                table: "Usuarios",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Usuarios_Peluqueros_PeluqueroId",
                table: "Usuarios",
                column: "PeluqueroId",
                principalTable: "Peluqueros",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Clientes_ClienteId",
                table: "Ventas",
                column: "ClienteId",
                principalTable: "Clientes",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Ventas_Turnos_TurnoId",
                table: "Ventas",
                column: "TurnoId",
                principalTable: "Turnos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
