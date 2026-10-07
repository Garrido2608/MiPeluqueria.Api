# MiPeluquería API - Backend SaaS Empresarial ✂️💈

API RESTful de grado comercial diseñada para la gestión integral de salones de belleza y barberías. Construida bajo los principios de **Clean Layered Architecture**, aplicando patrones de diseño empresariales para asegurar consistencia transaccional, escalabilidad y alta concurrencia.

## 🚀 Tecnologías y Stack
*   **.NET 8.0 LTS** (C# 12)
*   **Entity Framework Core 8** (Code-First Approach)
*   **SQL Server** (Base de datos relacional)
*   **Autenticación JWT Bearer** (con Hashing BCrypt)
*   **FluentValidation** (Validaciones desacopladas)
*   **AutoMapper** (Mapeo de DTOs)
*   **Swagger / OpenAPI** (Documentación de endpoints)

## 🏗️ Arquitectura y Patrones de Diseño Aplicados
El proyecto se aleja de los típicos "CRUDs académicos" para implementar soluciones robustas de la industria:
*   **Unit of Work & Repository Pattern:** Centralización de transacciones (`BeginTransactionAsync`, `Commit`, `Rollback`) para operaciones críticas como cobros mixtos y descuento de stock, garantizando cumplimiento **ACID**.
*   **Soft Delete Automático:** Intercepción en el `ApplicationDbContext` (ChangeTracker) y Global Query Filters para evitar la eliminación física de registros y mantener la trazabilidad contable.
*   **Paginación a Nivel de Motor SQL:** Implementación del wrapper `PagedResult<T>` para delegar el particionamiento de datos al servidor de base de datos (`Skip/Take`), previniendo excepciones OOM.
*   **Middleware Global de Excepciones:** Captura y normalización de errores para evitar la exposición de trazas de pila (Stack Trace) al frontend, retornando un `ApiResponse<T>` estandarizado.

## 📦 Módulos de Negocio (SaaS)
La plataforma aborda el 100% de la operatoria del salón:
1.  **Seguridad y Accesos:** Roles (Admin, Peluquero, Recepción, Cliente) y protección JWT.
2.  **Gestión de Clientes:** Fichas técnicas, fórmulas de colorimetría y control morfológico (Tipo Cabello/Rostro).
3.  **Catálogo y Profesionales:** Gestión de staff, horarios laborales y especialidades. Precios históricos congelados en servicios.
4.  **Motor Transaccional de Turnos:** Algoritmo matemático para validación de disponibilidad en tiempo real, control de solapamientos y bloqueos de agenda (vacaciones/médico).
5.  **Flujo de Caja y Puntos de Venta (PDV):** Arqueo ciego de caja diaria, soporte para cobros fraccionados/mixtos (Efectivo + QR) y descuento atómico de inventario.
6.  **Stock e Insumos:** Trazabilidad de ingresos, ventas de mostrador y consumo interno (bacha).
7.  **Liquidaciones y KPIs:** Automatización de cálculo de honorarios/comisiones y endpoints de Dashboard analítico (Facturación, Ticket Promedio).

## 🛠️ Instalación y Despliegue Local

1.  **Clonar el repositorio:**
    ```bash
    git clone [https://github.com/tu-usuario/MiPeluqueria.Api.git](https://github.com/tu-usuario/MiPeluqueria.Api.git)
    cd MiPeluqueria.Api
    ```
2.  **Configurar Cadena de Conexión:**
    Asegúrate de que tu instancia local de SQL Server esté corriendo. Verifica el archivo `appsettings.Development.json`:
    ```json
    "ConnectionStrings": {
      "DefaultConnection": "Server=.\\SQLEXPRESS;Database=MiPeluqueriaDB;Trusted_Connection=True;TrustServerCertificate=True;"
    }
    ```
3.  **Ejecutar Migraciones:**
    Abre la Consola del Administrador de Paquetes en Visual Studio y ejecuta:
    ```powershell
    Update-Database
    ```
4.  **Ejecutar el proyecto:**
    Presiona `F5` en Visual Studio. Swagger UI se abrirá automáticamente en `https://localhost:<puerto>/swagger`.

---
*Desarrollado como proyecto de Arquitectura de Sistemas / Programación IV - UTN.*