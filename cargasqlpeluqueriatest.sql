USE [MiPeluqueriaDB];
GO

PRINT '>>> Iniciando carga de datos reales para MiPeluqueriaDB...';

-- ========================================================
-- 1. ROLES DEL SISTEMA
-- ========================================================
SET IDENTITY_INSERT Roles ON;
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 1) INSERT INTO Roles (Id, Nombre, CreatedAt, IsDeleted) VALUES (1, 'Administrador', GETUTCDATE(), 0);
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 2) INSERT INTO Roles (Id, Nombre, CreatedAt, IsDeleted) VALUES (2, 'Recepcionista', GETUTCDATE(), 0);
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 3) INSERT INTO Roles (Id, Nombre, CreatedAt, IsDeleted) VALUES (3, 'Peluquero', GETUTCDATE(), 0);
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 4) INSERT INTO Roles (Id, Nombre, CreatedAt, IsDeleted) VALUES (4, 'Cliente', GETUTCDATE(), 0);
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 5) INSERT INTO Roles (Id, Nombre, CreatedAt, IsDeleted) VALUES (5, 'Cajero', GETUTCDATE(), 0);
IF NOT EXISTS (SELECT 1 FROM Roles WHERE Id = 6) INSERT INTO Roles (Id, Nombre, CreatedAt, IsDeleted) VALUES (6, 'SuperAdminSaaS', GETUTCDATE(), 0);
SET IDENTITY_INSERT Roles OFF;

-- ========================================================
-- 2. CATÁLOGOS BASE
-- ========================================================
-- MediosPago: Sin columna Activo[cite: 17]
IF OBJECT_ID('MediosPago', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT MediosPago ON;
    IF NOT EXISTS (SELECT 1 FROM MediosPago WHERE Id = 1) INSERT INTO MediosPago (Id, Nombre, CreatedAt, IsDeleted) VALUES (1, 'Efectivo', GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM MediosPago WHERE Id = 2) INSERT INTO MediosPago (Id, Nombre, CreatedAt, IsDeleted) VALUES (2, 'Mercado Pago / Transferencia', GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM MediosPago WHERE Id = 3) INSERT INTO MediosPago (Id, Nombre, CreatedAt, IsDeleted) VALUES (3, 'Tarjeta de Débito', GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM MediosPago WHERE Id = 4) INSERT INTO MediosPago (Id, Nombre, CreatedAt, IsDeleted) VALUES (4, 'Tarjeta de Crédito', GETUTCDATE(), 0);
    SET IDENTITY_INSERT MediosPago OFF;
END

IF OBJECT_ID('TiposCabello', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT TiposCabello ON;
    IF NOT EXISTS (SELECT 1 FROM TiposCabello WHERE Id = 1) INSERT INTO TiposCabello (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (1, 'Lacio / Fino', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM TiposCabello WHERE Id = 2) INSERT INTO TiposCabello (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (2, 'Ondulado / Grueso', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM TiposCabello WHERE Id = 3) INSERT INTO TiposCabello (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (3, 'Rizado / Con rulos', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM TiposCabello WHERE Id = 4) INSERT INTO TiposCabello (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (4, 'Afro / Muy rizado', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT TiposCabello OFF;
END

IF OBJECT_ID('TiposRostro', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT TiposRostro ON;
    IF NOT EXISTS (SELECT 1 FROM TiposRostro WHERE Id = 1) INSERT INTO TiposRostro (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (1, 'Ovalado', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM TiposRostro WHERE Id = 2) INSERT INTO TiposRostro (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (2, 'Cuadrado', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM TiposRostro WHERE Id = 3) INSERT INTO TiposRostro (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (3, 'Redondo', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM TiposRostro WHERE Id = 4) INSERT INTO TiposRostro (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (4, 'Alargado', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT TiposRostro OFF;
END

IF OBJECT_ID('EstadosTurno', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT EstadosTurno ON;
    IF NOT EXISTS (SELECT 1 FROM EstadosTurno WHERE Id = 1) INSERT INTO EstadosTurno (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (1, 'Pendiente', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM EstadosTurno WHERE Id = 2) INSERT INTO EstadosTurno (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (2, 'Confirmado', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM EstadosTurno WHERE Id = 3) INSERT INTO EstadosTurno (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (3, 'En Atención', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM EstadosTurno WHERE Id = 4) INSERT INTO EstadosTurno (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (4, 'Finalizado', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM EstadosTurno WHERE Id = 5) INSERT INTO EstadosTurno (Id, Nombre, Activo, CreatedAt, IsDeleted) VALUES (5, 'Cancelado', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT EstadosTurno OFF;
END

IF OBJECT_ID('MotivosCancelacion', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT MotivosCancelacion ON;
    IF NOT EXISTS (SELECT 1 FROM MotivosCancelacion WHERE Id = 1) INSERT INTO MotivosCancelacion (Id, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (1, 'Aviso previo del cliente', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM MotivosCancelacion WHERE Id = 2) INSERT INTO MotivosCancelacion (Id, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (2, 'Reprogramación solicitada', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM MotivosCancelacion WHERE Id = 3) INSERT INTO MotivosCancelacion (Id, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (3, 'Inconveniente del profesional', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM MotivosCancelacion WHERE Id = 4) INSERT INTO MotivosCancelacion (Id, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (4, 'No se presentó (Ausente)', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT MotivosCancelacion OFF;
END

-- ========================================================
-- 3. CONFIGURACIÓN DEL SALÓN
-- ========================================================
IF OBJECT_ID('ConfiguracionSalon', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM ConfiguracionSalon WHERE Id = 1)
    BEGIN
        SET IDENTITY_INSERT ConfiguracionSalon ON;
        INSERT INTO ConfiguracionSalon (Id, NombreSalon, Telefono, Email, Direccion, HorarioApertura, HorarioCierre, IntervaloTurnosMinutos, Activo, CreatedAt, IsDeleted)
        VALUES (1, 'Mi Peluquería & Barbería Saas', '351-4567890', 'contacto@mipeluqueria.com', 'Nueva Córdoba, Córdoba', '09:00:00', '20:00:00', 30, 1, GETUTCDATE(), 0);
        SET IDENTITY_INSERT ConfiguracionSalon OFF;
    END
END

-- ========================================================
-- 4. PROVEEDORES Y CATEGORÍAS DE PRODUCTOS
-- ========================================================
IF OBJECT_ID('Proveedores', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT Proveedores ON;
    IF NOT EXISTS (SELECT 1 FROM Proveedores WHERE Id = 1) INSERT INTO Proveedores (Id, Nombre, Telefono, Email, Direccion, Activo, CreatedAt, IsDeleted) VALUES (1, 'Distribuidora Capilar Centro', '351-4112233', 'ventas@capilarcentro.com.ar', 'San Jerónimo 340, Córdoba', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Proveedores WHERE Id = 2) INSERT INTO Proveedores (Id, Nombre, Telefono, Email, Direccion, Activo, CreatedAt, IsDeleted) VALUES (2, 'Barber Supply Mayorista', '11-45556677', 'pedidos@barbersupply.com', 'Av. Corrientes 1420, CABA', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Proveedores WHERE Id = 3) INSERT INTO Proveedores (Id, Nombre, Telefono, Email, Direccion, Activo, CreatedAt, IsDeleted) VALUES (3, 'Laboratorios Silkey / Primont', '11-48889900', 'pro@silkey.com.ar', 'Parque Industrial, Buenos Aires', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT Proveedores OFF;
END

IF OBJECT_ID('CategoriasProducto', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT CategoriasProducto ON;
    IF NOT EXISTS (SELECT 1 FROM CategoriasProducto WHERE Id = 1) INSERT INTO CategoriasProducto (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (1, 'Pomadas y Fijación', 'Ceras, geles, pastas y polvos voluminizadores', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM CategoriasProducto WHERE Id = 2) INSERT INTO CategoriasProducto (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (2, 'Cuidado de Barba', 'Óleos hidratantes, bálsamos y shampoos de barba', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM CategoriasProducto WHERE Id = 3) INSERT INTO CategoriasProducto (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (3, 'Shampoos y Máscaras', 'Cuidado diario, neutros y nutrición capilar', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM CategoriasProducto WHERE Id = 4) INSERT INTO CategoriasProducto (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (4, 'Color y Tinturas (Insumos)', 'Oxidantes, tintes y polvos decolorantes de salón', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT CategoriasProducto OFF;
END

-- ========================================================
-- 5. PRODUCTOS DE STOCK
-- ========================================================
IF OBJECT_ID('Productos', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT Productos ON;
    IF NOT EXISTS (SELECT 1 FROM Productos WHERE Id = 1) INSERT INTO Productos (Id, CategoriaId, ProveedorId, CodigoBarras, Nombre, PrecioCosto, PrecioVenta, StockActual, StockMinimoAlerta, EsParaVenta, CreatedAt, IsDeleted) VALUES (1, 1, 2, '779123456001', 'Pomada Fijación Mate 100g', 3500.00, 7800.00, 20, 5, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Productos WHERE Id = 2) INSERT INTO Productos (Id, CategoriaId, ProveedorId, CodigoBarras, Nombre, PrecioCosto, PrecioVenta, StockActual, StockMinimoAlerta, EsParaVenta, CreatedAt, IsDeleted) VALUES (2, 2, 2, '779123456002', 'Óleo Hidratante de Barba 30ml', 2800.00, 6200.00, 15, 4, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Productos WHERE Id = 3) INSERT INTO Productos (Id, CategoriaId, ProveedorId, CodigoBarras, Nombre, PrecioCosto, PrecioVenta, StockActual, StockMinimoAlerta, EsParaVenta, CreatedAt, IsDeleted) VALUES (3, 1, 2, '779123456003', 'Polvo Texturizador Voluminizador 10g', 3200.00, 7200.00, 18, 5, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Productos WHERE Id = 4) INSERT INTO Productos (Id, CategoriaId, ProveedorId, CodigoBarras, Nombre, PrecioCosto, PrecioVenta, StockActual, StockMinimoAlerta, EsParaVenta, CreatedAt, IsDeleted) VALUES (4, 3, 1, '779123456004', 'Shampoo Neutro Purificante 1000ml', 4200.00, 9500.00, 12, 3, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Productos WHERE Id = 5) INSERT INTO Productos (Id, CategoriaId, ProveedorId, CodigoBarras, Nombre, PrecioCosto, PrecioVenta, StockActual, StockMinimoAlerta, EsParaVenta, CreatedAt, IsDeleted) VALUES (5, 4, 3, '779123456005', 'Oxidante Cremoso 20 Vol. 900ml', 2500.00, 0.00, 8, 2, 0, GETUTCDATE(), 0);
    SET IDENTITY_INSERT Productos OFF;
END

-- ========================================================
-- 6. CATEGORÍAS DE SERVICIO Y ESPECIALIDADES
-- ========================================================
IF OBJECT_ID('CategoriasServicio', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT CategoriasServicio ON;
    IF NOT EXISTS (SELECT 1 FROM CategoriasServicio WHERE Id = 1) INSERT INTO CategoriasServicio (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (1, 'Barbería & Barba', 'Cortes masculinos clásicos, fade y ritual de barba', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM CategoriasServicio WHERE Id = 2) INSERT INTO CategoriasServicio (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (2, 'Cortes & Peinados', 'Cortes unisex, diseño, perfilado y brushing', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM CategoriasServicio WHERE Id = 3) INSERT INTO CategoriasServicio (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (3, 'Colorimetría', 'Tinturas globales, mechas, balayage y decoloraciones', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM CategoriasServicio WHERE Id = 4) INSERT INTO CategoriasServicio (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (4, 'Tratamientos Capilares', 'Baños de crema, nutrición profunda y alisados', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT CategoriasServicio OFF;
END

IF OBJECT_ID('Especialidades', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT Especialidades ON;
    IF NOT EXISTS (SELECT 1 FROM Especialidades WHERE Id = 1) INSERT INTO Especialidades (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (1, 'Barbero Tradicional & Fade', 'Especialista en degradé a navaja y toalla caliente', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Especialidades WHERE Id = 2) INSERT INTO Especialidades (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (2, 'Estilista / Diseñador Capilar', 'Cortes texturizados, tijera y peinados de evento', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Especialidades WHERE Id = 3) INSERT INTO Especialidades (Id, Nombre, Descripcion, Activo, CreatedAt, IsDeleted) VALUES (3, 'Colorista Master', 'Especialista en mechas, balayage y neutralizaciones', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT Especialidades OFF;
END

-- ========================================================
-- 7. SERVICIOS DE LA PELUQUERÍA
-- ========================================================
IF OBJECT_ID('Servicios', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT Servicios ON;
    IF NOT EXISTS (SELECT 1 FROM Servicios WHERE Id = 1) INSERT INTO Servicios (Id, CategoriaServicioId, Nombre, Descripcion, Precio, DuracionMinutos, Activo, CreatedAt, IsDeleted) VALUES (1, 1, 'Corte Fade / Clásico', 'Corte con máquina, tijera y terminación a navaja', 8000.00, 40, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Servicios WHERE Id = 2) INSERT INTO Servicios (Id, CategoriaServicioId, Nombre, Descripcion, Precio, DuracionMinutos, Activo, CreatedAt, IsDeleted) VALUES (2, 1, 'Perfilado de Barba & Toalla Caliente', 'Rebaje, delineado a navaja y masaje con óleo', 5500.00, 30, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Servicios WHERE Id = 3) INSERT INTO Servicios (Id, CategoriaServicioId, Nombre, Descripcion, Precio, DuracionMinutos, Activo, CreatedAt, IsDeleted) VALUES (3, 1, 'Combo Corte + Barba Ritual', 'Servicio integral de corte con arreglo de barba completo', 12500.00, 60, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Servicios WHERE Id = 4) INSERT INTO Servicios (Id, CategoriaServicioId, Nombre, Descripcion, Precio, DuracionMinutos, Activo, CreatedAt, IsDeleted) VALUES (4, 2, 'Corte de Puntas & Brushing', 'Lavado reparador, corte de forma y secado modelado', 11000.00, 50, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Servicios WHERE Id = 5) INSERT INTO Servicios (Id, CategoriaServicioId, Nombre, Descripcion, Precio, DuracionMinutos, Activo, CreatedAt, IsDeleted) VALUES (5, 3, 'Coloración Global / Tinte', 'Aplicación de tintura profesional con lavado nutritivo', 22000.00, 90, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Servicios WHERE Id = 6) INSERT INTO Servicios (Id, CategoriaServicioId, Nombre, Descripcion, Precio, DuracionMinutos, Activo, CreatedAt, IsDeleted) VALUES (6, 4, 'Nutrición Profunda & Ampolla', 'Tratamiento restaurador intensivo para cabello seco', 7500.00, 40, 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT Servicios OFF;
END

-- ========================================================
-- 8. STAFF Y HORARIOS
-- ========================================================
-- Peluqueros: Activo es obligatorio
IF OBJECT_ID('Peluqueros', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT Peluqueros ON;
    IF NOT EXISTS (SELECT 1 FROM Peluqueros WHERE Id = 1) INSERT INTO Peluqueros (Id, Nombre, Apellido, Telefono, Email, Activo, CreatedAt, IsDeleted) VALUES (1, 'Marcos', 'Paz', '351-5123456', 'marcos.barbero@mipeluqueria.com', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Peluqueros WHERE Id = 2) INSERT INTO Peluqueros (Id, Nombre, Apellido, Telefono, Email, Activo, CreatedAt, IsDeleted) VALUES (2, 'Lucas', 'Benítez', '351-5234567', 'lucas.estilista@mipeluqueria.com', 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Peluqueros WHERE Id = 3) INSERT INTO Peluqueros (Id, Nombre, Apellido, Telefono, Email, Activo, CreatedAt, IsDeleted) VALUES (3, 'Valentina', 'Rossi', '351-5345678', 'valentina.color@mipeluqueria.com', 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT Peluqueros OFF;
END

IF OBJECT_ID('PeluqueroEspecialidad', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM PeluqueroEspecialidad WHERE PeluqueroId = 1 AND EspecialidadId = 1) INSERT INTO PeluqueroEspecialidad (PeluqueroId, EspecialidadId) VALUES (1, 1);
    IF NOT EXISTS (SELECT 1 FROM PeluqueroEspecialidad WHERE PeluqueroId = 2 AND EspecialidadId = 2) INSERT INTO PeluqueroEspecialidad (PeluqueroId, EspecialidadId) VALUES (2, 2);
    IF NOT EXISTS (SELECT 1 FROM PeluqueroEspecialidad WHERE PeluqueroId = 3 AND EspecialidadId = 3) INSERT INTO PeluqueroEspecialidad (PeluqueroId, EspecialidadId) VALUES (3, 3);
END

-- Horarios Laborales: Sin columna Activo[cite: 17]
IF OBJECT_ID('HorariosLaborales', 'U') IS NOT NULL
BEGIN
    DECLARE @D INT = 1;
    WHILE @D <= 6
    BEGIN
        IF NOT EXISTS (SELECT 1 FROM HorariosLaborales WHERE PeluqueroId = 1 AND DiaSemana = @D) INSERT INTO HorariosLaborales (PeluqueroId, DiaSemana, HoraInicio, HoraFin, CreatedAt, IsDeleted) VALUES (1, @D, '09:00:00', '19:00:00', GETUTCDATE(), 0);
        IF NOT EXISTS (SELECT 1 FROM HorariosLaborales WHERE PeluqueroId = 2 AND DiaSemana = @D) INSERT INTO HorariosLaborales (PeluqueroId, DiaSemana, HoraInicio, HoraFin, CreatedAt, IsDeleted) VALUES (2, @D, '10:00:00', '20:00:00', GETUTCDATE(), 0);
        IF NOT EXISTS (SELECT 1 FROM HorariosLaborales WHERE PeluqueroId = 3 AND DiaSemana = @D) INSERT INTO HorariosLaborales (PeluqueroId, DiaSemana, HoraInicio, HoraFin, CreatedAt, IsDeleted) VALUES (3, @D, '09:30:00', '18:30:00', GETUTCDATE(), 0);
        SET @D = @D + 1;
    END
END

-- ========================================================
-- 9. CLIENTES DE PRUEBA
-- ========================================================
IF OBJECT_ID('Clientes', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT Clientes ON;
    IF NOT EXISTS (SELECT 1 FROM Clientes WHERE Id = 1) INSERT INTO Clientes (Id, Nombre, Apellido, Telefono, Email, TipoCabelloId, TipoRostroId, Activo, CreatedAt, IsDeleted) VALUES (1, 'Agustín', 'Pereyra', '351-6112233', 'agustin.p@gmail.com', 1, 2, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Clientes WHERE Id = 2) INSERT INTO Clientes (Id, Nombre, Apellido, Telefono, Email, TipoCabelloId, TipoRostroId, Activo, CreatedAt, IsDeleted) VALUES (2, 'Martín', 'Bossio', '351-6223344', 'martin.b@gmail.com', 2, 1, 1, GETUTCDATE(), 0);
    IF NOT EXISTS (SELECT 1 FROM Clientes WHERE Id = 3) INSERT INTO Clientes (Id, Nombre, Apellido, Telefono, Email, TipoCabelloId, TipoRostroId, Activo, CreatedAt, IsDeleted) VALUES (3, 'Sofía', 'Martínez', '351-6334455', 'sofi.m@gmail.com', 3, 3, 1, GETUTCDATE(), 0);
    SET IDENTITY_INSERT Clientes OFF;
END

-- ========================================================
-- 10. TURNOS INICIALES
-- ========================================================
-- Turnos: Sin Activo, usa MontoTotalEstimado, Observaciones y EstadoId[cite: 17]
IF OBJECT_ID('Turnos', 'U') IS NOT NULL
BEGIN
    SET IDENTITY_INSERT Turnos ON;
    IF NOT EXISTS (SELECT 1 FROM Turnos WHERE Id = 1) 
        INSERT INTO Turnos (Id, ClienteId, PeluqueroId, EstadoTurnoId, EstadoId, FechaHoraInicio, FechaHoraFin, MontoTotalEstimado, Observaciones, CreatedAt, IsDeleted) 
        VALUES (1, 1, 1, 2, 2, DATEADD(hour, 10, CAST(CAST(GETDATE() AS DATE) AS DATETIME2)), DATEADD(minute, 40, DATEADD(hour, 10, CAST(CAST(GETDATE() AS DATE) AS DATETIME2))), 8000.00, 'Cliente prefiere degradé alto', GETUTCDATE(), 0);
        
    IF NOT EXISTS (SELECT 1 FROM Turnos WHERE Id = 2) 
        INSERT INTO Turnos (Id, ClienteId, PeluqueroId, EstadoTurnoId, EstadoId, FechaHoraInicio, FechaHoraFin, MontoTotalEstimado, Observaciones, CreatedAt, IsDeleted) 
        VALUES (2, 2, 2, 1, 1, DATEADD(minute, 90, DATEADD(hour, 10, CAST(CAST(GETDATE() AS DATE) AS DATETIME2))), DATEADD(minute, 150, DATEADD(hour, 10, CAST(CAST(GETDATE() AS DATE) AS DATETIME2))), 12500.00, 'Combo corte y arreglo de barba', GETUTCDATE(), 0);
    SET IDENTITY_INSERT Turnos OFF;
END

-- TurnoServicios[cite: 17]
IF OBJECT_ID('TurnoServicios', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM TurnoServicios WHERE TurnoId = 1 AND ServicioId = 1) INSERT INTO TurnoServicios (TurnoId, ServicioId, Precio) VALUES (1, 1, 8000.00);
    IF NOT EXISTS (SELECT 1 FROM TurnoServicios WHERE TurnoId = 2 AND ServicioId = 3) INSERT INTO TurnoServicios (TurnoId, ServicioId, Precio) VALUES (2, 3, 12500.00);
END

-- ========================================================
-- 11. SESIÓN DE CAJA INICIAL
-- ========================================================
-- SesionesCaja[cite: 17]
IF OBJECT_ID('SesionesCaja', 'U') IS NOT NULL
BEGIN
    IF NOT EXISTS (SELECT 1 FROM SesionesCaja WHERE FechaCierre IS NULL AND IsDeleted = 0)
    BEGIN
        INSERT INTO SesionesCaja (UsuarioId, FechaApertura, MontoInicial, Estado, Observaciones, CreatedAt, IsDeleted) 
        VALUES (3, GETUTCDATE(), 15000.00, 'Abierta', 'Apertura de turno de caja inicial', GETUTCDATE(), 0);
    END
END

PRINT '>>> ¡Carga de base de datos completada con éxito!';
GO