-- ============================================================================
-- scripts/clean-e2e-data.sql
-- Higiene determinística de registros sintéticos E2E en MiPeluqueriaDB (v1.1.0)
-- ============================================================================
-- Reglas de seguridad:
-- 1. Preserva 100% el Data Seeding maestro (Usuarios 1..2, Cliente 1, Peluquero 1,
--    Proveedor 1, Servicios 1..3, Productos 1..2, ComisionesConfig 1 y catálogos).
-- 2. Preserva registros manuales reales que no contengan marcadores sintéticos
--    (*E2E*, *ReservaPublica*, dominios *.test).
-- 3. Elimina en orden estricto hoja -> raíz respetando claves foráneas y recalcula
--    totales de sesiones de caja sobrevivientes si tenían operaciones sintéticas.
-- ============================================================================

SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

DECLARE @E2EClientes TABLE ([Id] INT PRIMARY KEY);
DECLARE @E2EProveedores TABLE ([Id] INT PRIMARY KEY);
DECLARE @E2EProductos TABLE ([Id] INT PRIMARY KEY);
DECLARE @E2ETurnos TABLE ([Id] INT PRIMARY KEY);
DECLARE @E2EVentas TABLE ([Id] INT PRIMARY KEY);
DECLARE @E2EDetallesVenta TABLE ([Id] INT PRIMARY KEY);
DECLARE @E2ELiquidaciones TABLE ([Id] INT PRIMARY KEY);
DECLARE @E2EOrdenesCompra TABLE ([Id] INT PRIMARY KEY);
DECLARE @SesionesAfectadas TABLE ([Id] INT PRIMARY KEY);

-- 1. Clientes sintéticos E2E / Portal Público
INSERT INTO @E2EClientes ([Id])
SELECT [Id]
FROM [Clientes]
WHERE [Id] > 1
  AND (
    [Apellido] LIKE N'%E2E%'
    OR [Apellido] LIKE N'%ReservaPublica%'
    OR [Nombre] LIKE N'%E2E%'
    OR [Email] LIKE N'%.test'
    OR [Email] LIKE N'%e2e%'
  );

-- 2. Proveedores sintéticos E2E
INSERT INTO @E2EProveedores ([Id])
SELECT [Id]
FROM [Proveedores]
WHERE [Id] > 1
  AND (
    [RazonSocial] LIKE N'%E2E%'
    OR [Email] LIKE N'%.test'
    OR [Email] LIKE N'%e2e%'
  );

-- 3. Productos sintéticos E2E
INSERT INTO @E2EProductos ([Id])
SELECT [Id]
FROM [Productos]
WHERE [Id] > 2
  AND (
    [Nombre] LIKE N'%E2E%'
    OR [CodigoBarras] LIKE N'%E2E%'
    OR [ProveedorId] IN (SELECT [Id] FROM @E2EProveedores)
  );

-- 4. Turnos vinculados a clientes E2E o marcados como E2E
INSERT INTO @E2ETurnos ([Id])
SELECT [Id]
FROM [Turnos]
WHERE [ClienteId] IN (SELECT [Id] FROM @E2EClientes)
   OR [Observaciones] LIKE N'%E2E%';

-- 5. Ventas vinculadas a clientes/turnos E2E o cupones E2E
INSERT INTO @E2EVentas ([Id])
SELECT DISTINCT v.[Id]
FROM [Ventas] v
WHERE v.[ClienteId] IN (SELECT [Id] FROM @E2EClientes)
   OR v.[TurnoId] IN (SELECT [Id] FROM @E2ETurnos)
   OR EXISTS (
        SELECT 1
        FROM [Pagos] p
        WHERE p.[VentaId] = v.[Id]
          AND p.[ReferenciaOperacion] LIKE N'%E2E%'
   );

INSERT INTO @E2EDetallesVenta ([Id])
SELECT dv.[Id]
FROM [DetallesVenta] dv
WHERE dv.[VentaId] IN (SELECT [Id] FROM @E2EVentas)
   OR (dv.[TipoItem] = 2 AND dv.[ItemId] IN (SELECT [Id] FROM @E2EProductos));

-- 6. Liquidaciones contaminadas por renglones E2E o marcadas como E2E
INSERT INTO @E2ELiquidaciones ([Id])
SELECT DISTINCT l.[Id]
FROM [Liquidaciones] l
WHERE l.[Observaciones] LIKE N'%E2E%'
   OR EXISTS (
        SELECT 1
        FROM [DetallesLiquidacion] dl
        WHERE dl.[LiquidacionId] = l.[Id]
          AND dl.[DetalleVentaId] IN (SELECT [Id] FROM @E2EDetallesVenta)
   );

-- 7. Órdenes de compra sintéticas E2E
INSERT INTO @E2EOrdenesCompra ([Id])
SELECT DISTINCT oc.[Id]
FROM [OrdenesCompra] oc
WHERE oc.[ProveedorId] IN (SELECT [Id] FROM @E2EProveedores)
   OR oc.[NumeroComprobanteProveedor] LIKE N'%E2E%'
   OR oc.[Observaciones] LIKE N'%E2E%'
   OR EXISTS (
        SELECT 1
        FROM [DetallesOrdenCompra] doc
        WHERE doc.[OrdenCompraId] = oc.[Id]
          AND doc.[ProductoId] IN (SELECT [Id] FROM @E2EProductos)
   );

-- Registrar sesiones de caja tocadas antes de borrar ventas/movimientos E2E
INSERT INTO @SesionesAfectadas ([Id])
SELECT DISTINCT [SesionCajaId]
FROM [Ventas]
WHERE [Id] IN (SELECT [Id] FROM @E2EVentas)
UNION
SELECT DISTINCT mc.[SesionCajaId]
FROM [MovimientosCaja] mc
WHERE mc.[Concepto] LIKE N'%E2E%'
   OR (
        mc.[Tipo] = N'EgresoLiquidacion'
        AND (
            EXISTS (
                SELECT 1
                FROM @E2ELiquidaciones el
                WHERE mc.[Concepto] LIKE N'%#' + CAST(el.[Id] AS NVARCHAR(20)) + N' -%'
            )
            OR NOT EXISTS (
                SELECT 1
                FROM [Liquidaciones] l
                WHERE mc.[Concepto] LIKE N'%#' + CAST(l.[Id] AS NVARCHAR(20)) + N' -%'
            )
        )
   )
   OR EXISTS (
        SELECT 1
        FROM @E2EOrdenesCompra eoc
        WHERE mc.[Concepto] LIKE N'%Orden de Compra #' + CAST(eoc.[Id] AS NVARCHAR(20)) + N' -%'
   );

-- ============================================================================
-- BORRADO EN ORDEN HOJA -> RAÍZ
-- ============================================================================

-- Movimientos de caja originados por liquidaciones/OCs E2E
DELETE mc
FROM [MovimientosCaja] mc
WHERE mc.[Concepto] LIKE N'%E2E%'
   OR (
        mc.[Tipo] = N'EgresoLiquidacion'
        AND (
            EXISTS (
                SELECT 1
                FROM @E2ELiquidaciones el
                WHERE mc.[Concepto] LIKE N'%#' + CAST(el.[Id] AS NVARCHAR(20)) + N' -%'
            )
            OR NOT EXISTS (
                SELECT 1
                FROM [Liquidaciones] l
                WHERE mc.[Concepto] LIKE N'%#' + CAST(l.[Id] AS NVARCHAR(20)) + N' -%'
            )
        )
   )
   OR EXISTS (
        SELECT 1
        FROM @E2EOrdenesCompra eoc
        WHERE mc.[Concepto] LIKE N'%Orden de Compra #' + CAST(eoc.[Id] AS NVARCHAR(20)) + N' -%'
   );

-- Liquidaciones E2E
DELETE FROM [DetallesLiquidacion]
WHERE [LiquidacionId] IN (SELECT [Id] FROM @E2ELiquidaciones)
   OR [DetalleVentaId] IN (SELECT [Id] FROM @E2EDetallesVenta);

DECLARE @LiquidacionesEliminadas INT = 0;
DELETE FROM [Liquidaciones]
WHERE [Id] IN (SELECT [Id] FROM @E2ELiquidaciones);
SET @LiquidacionesEliminadas = @@ROWCOUNT;

-- Ventas y Pagos E2E
DELETE FROM [Pagos]
WHERE [VentaId] IN (SELECT [Id] FROM @E2EVentas);

DELETE FROM [DetallesVenta]
WHERE [VentaId] IN (SELECT [Id] FROM @E2EVentas)
   OR [Id] IN (SELECT [Id] FROM @E2EDetallesVenta);

DECLARE @VentasEliminadas INT = 0;
DELETE FROM [Ventas]
WHERE [Id] IN (SELECT [Id] FROM @E2EVentas);
SET @VentasEliminadas = @@ROWCOUNT;

-- Fichas técnicas E2E
DELETE FROM [FichasTecnicas]
WHERE [ClienteId] IN (SELECT [Id] FROM @E2EClientes)
   OR [TurnoId] IN (SELECT [Id] FROM @E2ETurnos)
   OR [FormulaAplicada] LIKE N'%E2E%'
   OR [NotasSensibilidadAlergias] LIKE N'%E2E%';

-- Kardex / Movimientos de stock E2E
DELETE FROM [MovimientosStock]
WHERE [ProductoId] IN (SELECT [Id] FROM @E2EProductos)
   OR [Motivo] LIKE N'%E2E%';

-- Turnos E2E
DELETE FROM [HistorialEstadosTurno]
WHERE [TurnoId] IN (SELECT [Id] FROM @E2ETurnos);

DELETE FROM [TurnoServicios]
WHERE [TurnoId] IN (SELECT [Id] FROM @E2ETurnos);

DECLARE @TurnosEliminados INT = 0;
DELETE FROM [Turnos]
WHERE [Id] IN (SELECT [Id] FROM @E2ETurnos);
SET @TurnosEliminados = @@ROWCOUNT;

-- Órdenes de Compra E2E
DELETE FROM [DetallesOrdenCompra]
WHERE [OrdenCompraId] IN (SELECT [Id] FROM @E2EOrdenesCompra);

DECLARE @OrdenesEliminadas INT = 0;
DELETE FROM [OrdenesCompra]
WHERE [Id] IN (SELECT [Id] FROM @E2EOrdenesCompra);
SET @OrdenesEliminadas = @@ROWCOUNT;

-- Productos E2E
DELETE FROM [ServiciosInsumosRecetas]
WHERE [ProductoId] IN (SELECT [Id] FROM @E2EProductos);

DELETE FROM [HistorialPreciosProducto]
WHERE [ProductoId] IN (SELECT [Id] FROM @E2EProductos);

DECLARE @ProductosEliminados INT = 0;
DELETE FROM [Productos]
WHERE [Id] IN (SELECT [Id] FROM @E2EProductos);
SET @ProductosEliminados = @@ROWCOUNT;

-- Sesiones de Caja abiertas/cerradas exclusivamente por corridas E2E (sin ventas ni movimientos reales)
DECLARE @SesionesEliminadas INT = 0;
DELETE sc
FROM [SesionesCaja] sc
WHERE (
        sc.[ObservacionesCierre] LIKE N'%E2E%'
     OR sc.[ObservacionesCierre] LIKE N'%Journey 3%'
  )
  AND NOT EXISTS (SELECT 1 FROM [Ventas] v WHERE v.[SesionCajaId] = sc.[Id])
  AND NOT EXISTS (SELECT 1 FROM [MovimientosCaja] mc WHERE mc.[SesionCajaId] = sc.[Id]);
SET @SesionesEliminadas = @@ROWCOUNT;

-- Si una sesión real preexistente fue cerrada automáticamente al iniciar una corrida E2E, normalizar su observación
UPDATE [SesionesCaja]
SET [ObservacionesCierre] = N'Cierre de arqueo previo'
WHERE [ObservacionesCierre] LIKE N'%E2E%';

-- Recalcular totales exactos en sesiones sobrevivientes que hayan tenido una operación E2E removida
UPDATE sc
SET
    [TotalVentasEfectivo] = ISNULL((
        SELECT SUM(p.[Importe])
        FROM [Ventas] v
        INNER JOIN [Pagos] p ON p.[VentaId] = v.[Id] AND p.[IsDeleted] = 0
        INNER JOIN [MediosPago] mp ON mp.[Id] = p.[MedioPagoId]
        WHERE v.[SesionCajaId] = sc.[Id]
          AND v.[IsDeleted] = 0
          AND mp.[EsEfectivoFisico] = 1
    ), 0),
    [TotalVentasDigital] = ISNULL((
        SELECT SUM(p.[Importe])
        FROM [Ventas] v
        INNER JOIN [Pagos] p ON p.[VentaId] = v.[Id] AND p.[IsDeleted] = 0
        INNER JOIN [MediosPago] mp ON mp.[Id] = p.[MedioPagoId]
        WHERE v.[SesionCajaId] = sc.[Id]
          AND v.[IsDeleted] = 0
          AND mp.[EsEfectivoFisico] = 0
    ), 0),
    [TotalIngresosManuales] = ISNULL((
        SELECT SUM(mc.[Monto])
        FROM [MovimientosCaja] mc
        WHERE mc.[SesionCajaId] = sc.[Id]
          AND mc.[IsDeleted] = 0
          AND mc.[Tipo] LIKE N'Ingreso%'
    ), 0),
    [TotalEgresosManuales] = ISNULL((
        SELECT SUM(mc.[Monto])
        FROM [MovimientosCaja] mc
        WHERE mc.[SesionCajaId] = sc.[Id]
          AND mc.[IsDeleted] = 0
          AND mc.[Tipo] NOT LIKE N'Ingreso%'
    ), 0)
FROM [SesionesCaja] sc
INNER JOIN @SesionesAfectadas sa ON sa.[Id] = sc.[Id];

UPDATE sc
SET
    [MontoFinalEsperado] = sc.[MontoInicial]
                         + sc.[TotalVentasEfectivo]
                         + sc.[TotalIngresosManuales]
                         - sc.[TotalEgresosManuales],
    [Diferencia] = CASE
        WHEN sc.[MontoFinalReal] IS NOT NULL
        THEN sc.[MontoFinalReal] - (
            sc.[MontoInicial]
            + sc.[TotalVentasEfectivo]
            + sc.[TotalIngresosManuales]
            - sc.[TotalEgresosManuales]
        )
        ELSE NULL
    END
FROM [SesionesCaja] sc
INNER JOIN @SesionesAfectadas sa ON sa.[Id] = sc.[Id];

-- Cuentas de usuario sintéticas asociadas a clientes E2E (nunca Usuarios 1 ni 2)
DELETE FROM [RefreshTokens]
WHERE [UsuarioId] IN (
    SELECT [Id] FROM [Usuarios]
    WHERE [Id] NOT IN (1, 2)
      AND (
        [ClienteId] IN (SELECT [Id] FROM @E2EClientes)
        OR [Email] LIKE N'%.test'
        OR [Email] LIKE N'%e2e%'
      )
);

DELETE FROM [HistorialSesiones]
WHERE [UsuarioId] IN (
    SELECT [Id] FROM [Usuarios]
    WHERE [Id] NOT IN (1, 2)
      AND (
        [ClienteId] IN (SELECT [Id] FROM @E2EClientes)
        OR [Email] LIKE N'%.test'
        OR [Email] LIKE N'%e2e%'
      )
);

DELETE FROM [Usuarios]
WHERE [Id] NOT IN (1, 2)
  AND (
    [ClienteId] IN (SELECT [Id] FROM @E2EClientes)
    OR [Email] LIKE N'%.test'
    OR [Email] LIKE N'%e2e%'
  );

-- Clientes y Proveedores sintéticos E2E
DECLARE @ClientesEliminados INT = 0;
DELETE FROM [Clientes]
WHERE [Id] IN (SELECT [Id] FROM @E2EClientes);
SET @ClientesEliminados = @@ROWCOUNT;

DECLARE @ProveedoresEliminados INT = 0;
DELETE FROM [Proveedores]
WHERE [Id] IN (SELECT [Id] FROM @E2EProveedores);
SET @ProveedoresEliminados = @@ROWCOUNT;

-- Limpieza de trazas sintéticas en AuditLogs
DELETE FROM [AuditLogs]
WHERE [ValoresNuevosJson] LIKE N'%E2E%'
   OR [ValoresNuevosJson] LIKE N'%ReservaPublica%'
   OR [ValoresNuevosJson] LIKE N'%.test%';

COMMIT TRANSACTION;

SELECT
    @ClientesEliminados AS [ClientesEliminados],
    @ProveedoresEliminados AS [ProveedoresEliminados],
    @ProductosEliminados AS [ProductosEliminados],
    @TurnosEliminados AS [TurnosEliminados],
    @VentasEliminadas AS [VentasEliminadas],
    @LiquidacionesEliminadas AS [LiquidacionesEliminadas],
    @OrdenesEliminadas AS [OrdenesCompraEliminadas],
    @SesionesEliminadas AS [SesionesCajaEliminadas];
