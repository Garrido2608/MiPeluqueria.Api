# 12 - PLAN MAESTRO DE REDISEÑO UX/UI PRO, DESIGN SYSTEM DUAL Y FIXES INTEGRALES (ETAPA 8 — CAPA 12 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Suite Vinculada (Rutas Monorepo):** `AGENTS.md`, `docs/01_SYSTEM_CONSTRAINTS.md` a `docs/11_QA_HARDENING_OBSERVABILITY_AND_REUSABILITY_PLAN.md`
**Base Aprobada de Partida:** Release Candidate `v1.0.0` (35/35 xUnit PASS, 5/5 Playwright E2E PASS, 22/22 API `200 OK`, CI/CD activo)
**Objetivo de Versión:** Release Comercial **`v1.1.0`** — Producto Comercial SaaS Listo para Salones Reales
**Estado:** PLAN ACTIVO — ETAPA 8 (10 MICRO-TAREAS ATÓMICAS + 3 HARD GATES `6A`, `6B`, `6C`)

---

## 1. LEYES INQUEBRANTABLES DE EJECUCIÓN PARA LA ETAPA 8

1. **PROTOCOLO SDD + MEMORIA EN DISCO (`docs/07`):**
   * Al iniciar **cada micro-tarea**, el agente DEBE leer primero `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md` (Secciones 1, 2 y últimos bloques de Sección 5) y la sección exacta de este archivo `docs/12`.
   * Al cerrar **cada micro-tarea**, el agente DEBE actualizar `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md` con archivos tocados, salida real de sensores y avance del puntero.
2. **REGLA DE CERO REGRESIÓN EN BACKEND (NO ROMPER CONTRATOS):**
   * Queda **TERMINANTEMENTE PROHIBIDO** renombrar propiedades existentes de DTOs, cambiar rutas HTTP vigentes, modificar nombres de variables/claims JWT o romper alguno de los **35 tests xUnit** existentes.
   * Todo cambio autorizado en `MiPeluqueria.Api/` se ejecuta **de forma vertical, aislada y acompañada de tests xUnit cercados** que demuestren el fix antes de avanzar a la siguiente tarea.
3. **ESTÁNDAR DE PANTALLA BASE (NOTEBOOK COMPACTA SIN TECLADO NUMÉRICO — 13" / 14"):**
   * Toda la interfaz de escritorio se diseña **primero** para resoluciones estándar de notebook compacta: **`1280x720`** y **`1366x768`**.
   * **Ley de Cero Scroll Horizontal:** En ninguna de las 19 rutas puede existir desborde horizontal (`document.documentElement.scrollWidth <= window.innerWidth`) a `1280x720` ni `1366x768`.
   * En monitores grandes (`>= 1536px` / `1920px`), el espacio se distribuye limpiamente mediante contenedores centrados o grillas equilibradas (`max-w-[1440px]`) sin deformar formularios ni dejar vacíos desproporcionados.
   * **Requisito Móvil Prioritario:** La pestaña **Fichas Técnicas Capilares** dentro de `/peluquero` (y su navegación base) DEBE ser 100% operable en resolución móvil **`390x844`** (uso en silla desde el celular del estilista).
4. **REGLA DE CERCADO DE TESTS (FENCING RULE) Y SENSORES OBLIGATORIOS:**
   * Ninguna tarea se cierra solo con compilación. Cada cambio de Backend corre `dotnet build --warnaserror` + `dotnet test`; cada cambio de Frontend corre `npm run typecheck`, auditoría `0 any`, auditoría `0` imports de dominio en `src/components/ui/` y `npm run build`.
   * Al actualizar copywriting o estructura visual en Frontend, los tests Playwright se actualizan **únicamente en selectores** manteniendo intactas el 100% de las aserciones de negocio, `pageerror === 0`, `console.error === 0` y `HTTP >= 500 === 0`.
5. **FRONTERA AGNÓSTICA DE REUTILIZACIÓN (`src/components/ui/`):**
   * Todo nuevo componente base (`NumericInput`, `BrandLogo`, `SearchCombobox`, `ThemeToggle`, `Badge`, `Button`) DEBE residir en `mipeluqueria-saas-web/src/components/ui/`, ser 100% agnóstico de dominio (`0` imports de `services/`, `useDomainQueries`, `*.types`) y llevar JSDoc (`@param`, `@example`).

---

## 2. DESIGN SYSTEM OFICIAL `v1.1.0` (DECISIONES APROBADAS POR EL ARQUITECTO)

Basado en estándares vigentes 2025–2026 (**W3C Design Tokens 3 capas**, **Vercel Geist**, **Material Design 3 Expressive**, **Nielsen Norman Group** y **WCAG 2.2 AA**):

### 2.1 Colorimetría Dual: *Studio Clean* (Light-First por Defecto) + *Obsidian & Indigo* (Dark Mode)

Se implementa mediante **CSS Custom Properties** en `src/app/globals.css` (`:root` para Light por defecto, `.dark` para Dark Mode) mapeadas a tokens semánticos en `tailwind.config.ts` (`darkMode: 'class'`), con persistencia en `localStorage` (`mipeluqueria_theme`).

| Token Semántico | Modo Claro — *Studio Clean* (Default) | Modo Oscuro — *Obsidian & Indigo* | Uso Exacto |
| :--- | :--- | :--- | :--- |
| `--bg-canvas` | `#F8FAFC` (Blanco Humo) | `#0B1120` (Carbono Azul) | Fondo general de la aplicación |
| `--bg-surface` | `#FFFFFF` (Blanco Puro) | `#111827` (Slate Elevado) | Cards, tablas, modales |
| `--bg-elevated` | `#F1F5F9` (Slate Suave) | `#1E293B` (Slate Medio) | Hover de filas, headers de tabla, inputs secundarios |
| `--bg-sidebar` | `#0F172A` (Slate Profundo) | `#090E1A` (Noche Profundo) | Sidebar izquierdo con alto contraste |
| `--border-subtle` | `#E2E8F0` | `#1F2937` | Bordes de cards y separadores |
| `--border-strong` | `#CBD5E1` | `#334155` | Bordes de inputs y botones secundarios |
| `--text-primary` | `#0F172A` | `#F8FAFC` | Títulos, valores y texto principal (`> 12:1`) |
| `--text-secondary` | `#475569` | `#94A3B8` | Subtítulos, metadatos, placeholders (`> 5:1`) |
| `--brand-primary` | `#4F46E5` (hover `#4338CA`) | `#6366F1` (hover `#818CF8`) | Único CTA primario (`texto #FFFFFF`, WCAG AA) |
| `--brand-accent` | `#0EA5E9` | `#38BDF8` | Focus rings, indicadores activos, links |

### 2.2 Estrategia Obligatoria para el Logo Negro del Salón (`<BrandLogo />`)
* El logotipo oficial del salón es de **trazo negro**.
* Por lo tanto, el componente agnóstico `src/components/ui/BrandLogo.tsx` renderizará la imagen desde `/logo.png` (con fallback elegante tipográfico + ícono mientras no exista el archivo físico en `public/logo.png`) siempre sobre un **contenedor claro dedicado** (`bg-white` con `rounded-xl p-1.5 shadow-sm ring-1 ring-slate-200/80`) cuando se monte dentro del Sidebar oscuro o en Modo Oscuro, asegurando visibilidad y contraste impecables en ambos temas y en comprobantes imprimibles.

### 2.3 Mapa de Colores Semánticos Globales (Inmutable en Toda la App)
Todos los módulos (**Caja**, **POS**, **Dashboard BI — torta y barras**, **Inventario**, **Agenda**, **Diagnóstico API**) DEBEN importar estos tokens compartidos desde `src/lib/semanticColors.ts`:
* **Efectivo Físico / Éxito / Caja Abierta / API 200 OK:** Esmeralda (`#059669` Light / `#10B981` Dark)
* **Digital / Mercado Pago QR / Transferencia / Tarjetas / Info:** Azul Cielo (`#0284C7` Light / `#38BDF8` Dark)
* **Advertencia / Pendiente / Seña / Stock Bajo:** Ámbar (`#D97706` Light / `#F59E0B` Dark)
* **Destructivo / Error / Anular / Baja / Alergias / Stock 0:** Carmesí (`#DC2626` Light / `#F43F5E` Dark)
* **Canje / Cortesía / Neutro:** Violeta (`#7C3AED` / `#8B5CF6`) o Slate (`#64748B`)

### 2.4 Sistema Tipográfico (2 Fuentes Máximo vía `next/font/google`)
* **UI y Títulos (`--font-sans`):** **`Plus Jakarta Sans`** (pesos `400`, `500`, `600`, `700`).
* **Finanzas, Horarios, Stock y Tickets (`--font-mono`):** **`JetBrains Mono`** (pesos `400`, `500`, `600`, `700`) aplicada junto a `tabular-nums` en montos (`$ 28.000,00`), horarios (`09:00`), porcentajes y números de comprobante.
* **Escala Única (5 niveles):** `text-2xl sm:text-3xl font-bold tabular-nums` (KPIs) · `text-xl sm:text-2xl font-semibold tracking-tight` (`h1`) · `text-base sm:text-lg font-semibold` (`h2`) · `text-sm` (cuerpo/celdas/inputs) · `text-xs font-medium` (labels/badges; prohibido abusar de `UPPERCASE` en oraciones completas).

### 2.5 Jerarquía Única de Botones (4 Variantes en `src/components/ui/DesignSystem.tsx`)
* **`primary`:** Sólido `--brand-primary`, texto blanco. **Regla estricta: máximo 1 botón `primary` por pantalla o modal.**
* **`secondary`:** Superficie sutil + borde `--border-strong` + hover visible (`Editar`, `Imprimir`, `Horarios`, `Cancelar`, `Ver Detalle`).
* **`destructive` (`soft` para filas de tabla / `solid` exclusivo para confirmaciones de borrado o egresos):** Elimina los bloques rojos agresivos repetidos en tablas.
* **`ghost`:** Transparente hasta hover (`X` de cierre, limpiar filtros, salir).
* Altura uniforme (`h-9` en tablas/toolbars, `h-10` en CTAs/modales), ícono `lucide-react` (`16px`) y spinner automático en `isLoading`.

---

## 3. MATRIZ DE DECISIONES APROBADAS POR PANTALLA

| Módulo / Pantalla | Decisión Aprobada | Alcance Clave |
| :--- | :--- | :--- |
| **0. Transversal (19 pantallas)** | **Opción 1 + Opción 2 (Combinadas)** | Sidebar compacto (`224px`) agrupado con íconos sin números `1..10`, sin `TENANT #1` (con badge de Rol), `BrandLogo` para logo negro, switch Claro/Oscuro, Topbar fino con fecha del salón, indicador `Caja #N • DD/MM`, copywriting humano sin rutas/fórmulas, `NumericInput` sin `0` pegajoso, `<PaginationBar/>` sin scroll atrapado. |
| **1. Mi Puesto (`/peluquero`)** | **Opción 1 + Móvil en Fichas** | Agenda con `Hoy \| Mañana \| Esta semana` + timeline vertical `09:00–20:00` + vista semanal; Fichas Técnicas Master-Detail 100% usable en celular (`390x844`); fix porcentaje comisión (`50%/10%` real). |
| **2. Calendario Master (`/recepcion/calendario`)** | **Opción 1** | Grilla multi-columna por profesional estilo Fresha (`09:00–20:00`, encabezados con color del estilista, descansos/bloqueos sombreados, vista Día/Semana, clic en hueco libre para agendar), Combobox buscable de clientes, mantener Registro Express y Bloqueo Horario. |
| **3. Punto de Venta (`/recepcion/pos`)** | **Opción 1** | Layout 60/40 optimizado a `1280px`, badge `Caja del DD/MM/YYYY (#N)` sin jerga de idempotencia, `NumericInput`, atajos de cobro con color semántico, comprobante rediseñado con `@media print` estricto en **1 sola hoja** sin modal ni `X`. |
| **4. Caja Diaria (`/recepcion/caja`)** | **Opción 1 + Fix Backend** | Quitar fórmula/controller del header, 2 tarjetas KPI semánticas (Verde/Azul), `NumericInput`, fecha en tabla historial (`Sesión #N — DD/MM/YYYY`), `<PaginationBar/>` y fix de cálculo de sesión abierta en `CajaService`. |
| **5. Clientes CRM (`/recepcion/clientes`)** | **Opción 1** | Header limpio sin nombres de controladores, acciones suaves en fila, modal Fichas Técnicas con miniatura de foto e indicador claro de estado Cloudinary/URL manual, alertas de alergia destacadas. |
| **6. Dashboard BI (`/admin/dashboard`)** | **Opción 1** | Filtros rápidos (`Hoy \| Esta semana \| Este mes`), 6 KPIs sin prefijos numéricos, Donut Chart + barras inferiores con el **mismo mapa semántico** que Caja, Ranking Estilistas con `colorAgendaHex`. |
| **7. Inventario, Proveedores y OC (`/admin/inventario`)** | **Opción 2** | Buscadores en Productos y Proveedores, filtros categoría/stock crítico, `"Kardex"` $\rightarrow$ `"Historial de Stock"` y badge entendible en edición, `"N artículos"` clicable en Proveedores (modal de productos vinculados), OC multi-ítem con PDF imprimible + **`"Generar OC con faltantes"`** automático por proveedor + **`"Enviar por WhatsApp"`** (`wa.me`). |
| **8. Servicios y Recetas (`/admin/servicios-recetas`)** | **Opción 1** | Headers claros en editor de Recetas + porcentaje de margen neto, mover imputación manual a acordeón colapsado con **desplegable de turnos recientes** (sin `ID DEL TURNO` crudo ni rutas `POST /api/...`). |
| **9. Staff y Liquidaciones (`/admin/staff-liquidaciones`)** | **Opción 1 + Fix Backend** | Chips colapsables de especialidades (`+N más` popover), nombre real del profesional en Comisiones, fix liquidaciones duplicadas, fix `Recibo PDF` (fechas/ms) y botón `Pagar` con confirmación de imputación a Caja, copy humano. |
| **10. Configuración y Seguridad (`/admin/configuracion-seguridad`)** | **Opción 1** | Quitar `(CRUD Completo)/(POST)/(PUT)`, resaltar fila editada en Catálogos (`Editando`), fix autofill Chrome (`new-password`) + desplegable de peluqueros en Alta Staff, quitar scroll atrapado en Auditoría Forense (`<PaginationBar/>` 10 filas + parser humano de User-Agent), pestaña `"Mis Sucursales y Mi Plan"` para `Administrador`. |
| **11. Diagnóstico API y Consola SaaS** | **Opción 2 + Rol `SuperAdminSaaS`** | Crear rol real `SuperAdminSaaS` (`RolId = 6`) + usuario semilla en Backend; restringir `/saas-admin` y `/admin/diagnostico-api` solo a `SuperAdminSaaS`; quitar pestaña mock `AuditLogs JSON Diff`; mostrar `Simulador Webhooks` solo en `development`. |

---

## FASE 8A: CIMIENTOS DEL DESIGN SYSTEM DUAL Y FIXES CRÍTICOS DE BACKEND

### TAREA 8.1 — SPEC-SYNC + Tokens *Studio Clean* (Claro/Oscuro) + Tipografía + Kit UI Base
* **Modelo Recomendado:** `Argon Mid` / `Neon Mid`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`, `docs/12` (Secciones 1 y 2).
* **[ARCHIVOS PERMITIDOS]:**
  * `docs/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (SPEC-SYNC: tokens duales, `Plus Jakarta Sans` + `JetBrains Mono`, viewport base `1280x720`/`1366x768`)
  * `mipeluqueria-saas-web/tailwind.config.ts`
  * `mipeluqueria-saas-web/src/app/globals.css`
  * `mipeluqueria-saas-web/src/app/layout.tsx`
  * `mipeluqueria-saas-web/src/context/ThemeProvider.tsx` (nuevo)
  * `mipeluqueria-saas-web/src/context/AppProviders.tsx`
  * `mipeluqueria-saas-web/src/lib/semanticColors.ts` (nuevo)
  * `mipeluqueria-saas-web/src/components/ui/DesignSystem.tsx`
  * `mipeluqueria-saas-web/src/components/ui/NumericInput.tsx` (nuevo)
  * `mipeluqueria-saas-web/src/components/ui/BrandLogo.tsx` (nuevo)
  * `mipeluqueria-saas-web/src/components/ui/SearchCombobox.tsx` (nuevo)
  * `mipeluqueria-saas-web/src/components/ui/ThemeToggle.tsx` (nuevo)
  * `mipeluqueria-saas-web/src/components/ui/index.ts`
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. Configurar `next/font/google` en `src/app/layout.tsx` con **`Plus_Jakarta_Sans`** (`--font-sans`) y **`JetBrains_Mono`** (`--font-mono`), sin parpadeo FOUC al cargar tema guardado en `localStorage` (default `'light'`, *Studio Clean*).
  2. Definir en `globals.css` y `tailwind.config.ts` (`darkMode: 'class'`) las 3 capas de tokens de la Sección 2.1 + estilos globales `@media print` limpios (1 hoja sin fondos oscuros ni barras laterales).
  3. Crear `src/lib/semanticColors.ts` con la paleta semántica única (Efectivo/Digital/Alerta/Error/Neutro) para gráficos Recharts, badges y tarjetas.
  4. Actualizar `DesignSystem.tsx` con las 4 variantes estrictas de botón (`primary`, `secondary`, `destructive`, `ghost`), alturas uniformes `h-9`/`h-10`, soporte `isLoading` y contraste WCAG 2.2 AA en Claro y Oscuro.
  5. Crear los componentes agnósticos (0 imports de dominio, JSDoc completo):
     * `NumericInput.tsx`: resuelve el `0` pegajoso permitiendo campo vacío controlado con placeholder `0`, selección automática al enfocar, decimales/enteros opcionales y bloqueo de signos negativos cuando `min >= 0`.
     * `BrandLogo.tsx`: contenedor claro dedicado para el **logo negro** del salón (`/logo.png` con fallback limpio si el archivo todavía no está en `public/`).
     * `SearchCombobox.tsx`: selector filtrable por texto (para clientes en Calendario y turnos recientes en Recetas).
     * `ThemeToggle.tsx`: botón compacto Claro/Oscuro conectado a `ThemeProvider`.
* **[SENSORES DETERMINÍSTICOS]:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` $\rightarrow$ **`0` errores**.
  2. Auditoría `\bany\b` en `mipeluqueria-saas-web/src/**` $\rightarrow$ **`0` coincidencias**.
  3. Auditoría de frontera UI (`src/components/ui/*.tsx`) buscando `services/|useDomainQueries|\.types` $\rightarrow$ **`0` coincidencias**.
  4. `npm --prefix mipeluqueria-saas-web run build` $\rightarrow$ **`0` errores**.

---

### TAREA 8.2 — Fixes Críticos Backend #1 (Cálculo de `HistorialCajas` Abierta + Rango Semanal de Turnos) + Tests xUnit
* **Modelo Recomendado:** `Argon High` / `Carbon`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/05_BUSINESS_RULES.md` (Secciones 2 y 4), `docs/12` (Sección 1 y 3).
* **[ARCHIVOS PERMITIDOS]:**
  * `MiPeluqueria.Api/Repositories/Interfaces/ISesionCajaRepository.cs` / `Repositories/Implementations/SesionCajaRepository.cs` (si requiere lectura agregada sin over-fetching)
  * `MiPeluqueria.Api/Services/Implementations/CajaService.cs`
  * `MiPeluqueria.Api/Repositories/Implementations/TurnoRepository.cs`
  * `MiPeluqueria.Api/Services/Implementations/TurnoService.cs` (solo si el rango `fechaDesde`/`fechaHasta` no se propaga)
  * `MiPeluqueria.Api.Tests/Etapa8BackendFixesTests.cs` (nuevo)
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. **Fix Bug Caja Abierta en Historial:** Auditar y corregir `CajaService.ObtenerHistorialCajasAsync` (y el cálculo en vivo de sesiones con `EstadoAbierta == true`) para que `TotalVentasEfectivo`, `TotalVentasDigital`, `TotalIngresosManuales`, `TotalEgresosManuales` y `MontoFinalEsperado` coincidan **al centavo** con `GET /api/caja/estado-actual` y `GET /api/caja/{id}` (erradicando la discrepancia `$65.500` vs `$28.000` y `$47.500` vs `$37.500` detectada en capturas). **Cero cambio de nombres de propiedades DTO.**
  2. **Soporte de Agenda Semanal (`Esta semana`):** Verificar y habilitar que `GET /api/turnos?fechaDesde=...&fechaHasta=...` filtre correctamente por rango de fechas en `TurnoRepository.GetPagedAsync` cuando no se envía `fecha` puntual, conservando 100% el comportamiento previo cuando se envía `fecha`.
  3. **Tests xUnit Cercados (`Etapa8BackendFixesTests.cs`):**
     * Test: Abrir caja `$10.000`, cobrar venta efectivo `$37.500`, venta digital `$28.000` y registrar egreso `$10.000` $\rightarrow$ verificar que `GET /api/caja/estado-actual`, `GET /api/caja/{id}` y la fila de la sesión abierta en `GET /api/caja/historial` retornen **idénticamente** `montoFinalEsperado = 37500.00` y `totalVentasDigital = 28000.00`.
     * Test: Crear turnos en días distintos de la misma semana y validar que `GET /api/turnos?fechaDesde=...&fechaHasta=...` retorne únicamente los turnos dentro del rango.
* **[SENSORES DETERMINÍSTICOS]:**
  1. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --warnaserror` $\rightarrow$ **`0 Errores / 0 Advertencias`**.
  2. `dotnet test MiPeluqueria.Api.Tests --no-build --verbosity normal` $\rightarrow$ **100% PASS (35 previos + nuevos tests verdes, 0 FAIL, 0 SKIP)**.

---

### TAREA 8.3 — Fixes Críticos Backend #2 (Liquidaciones Duplicadas, `Pagar`, `PeluqueroNombre` en Comisiones) + Rol Real `SuperAdminSaaS` (`RolId = 6`)
* **Modelo Recomendado:** `Argon High` / `Carbon`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/03_DATA_MODEL.md`, `docs/04_API_CONTRACTS.md`, `docs/05_BUSINESS_RULES.md` (Sección 4.4), `docs/09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (Sección 4.1 — reglas `HasData` con fecha estática UTC y hash BCrypt fijo).
* **[ARCHIVOS PERMITIDOS]:**
  * `MiPeluqueria.Api/Services/Implementations/LiquidacionService.cs`
  * `MiPeluqueria.Api/Repositories/Implementations/LiquidacionRepository.cs`
  * `MiPeluqueria.Api/Controllers/LiquidacionesController.cs`
  * `MiPeluqueria.Api/DTOs/Ventas/ComisionConfigDto.cs` (solo agregar propiedad aditiva `PeluqueroNombre` / `ServicioNombre` si faltara, sin renombrar existentes)
  * `MiPeluqueria.Api/Services/Implementations/ComisionConfigService.cs`
  * `MiPeluqueria.Api/Data/Context/ApplicationDbContext.cs` (Seeding de `RolId = 6 "SuperAdminSaaS"` + usuario semilla `superadmin@mipeluqueria.com` con `RolId = 6`, hash BCrypt estático de `"SuperAdmin123!"` y fecha fija `2026-01-01T00:00:00Z`)
  * `MiPeluqueria.Api/Migrations/**` (nueva migración aditiva `AddSuperAdminSaaSRole`)
  * `MiPeluqueria.Api/Controllers/SuscripcionesController.cs` (restringir `POST cambiar-plan` a `[Authorize(Roles = "SuperAdminSaaS")]`, manteniendo `GET mi-suscripcion` para `Administrador,SuperAdminSaaS` y `GET planes` público)
  * `MiPeluqueria.Api.Tests/Etapa8BackendFixesTests.cs` (y `CustomWebApplicationFactory.cs` si requiere preservar el usuario semilla `SuperAdminSaaS` en limpieza)
  * `docs/03_DATA_MODEL.md`, `docs/04_API_CONTRACTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md` (SPEC-SYNC de `RolId = 6`)
* **[ALCANCE]:**
  1. **Fix Liquidaciones Duplicadas:** Auditar `LiquidacionRepository.GetDetallesVentaPendientesLiquidacionAsync` y `LiquidacionService.LiquidarPeriodoAsync` para garantizar que ningún `DetalleVenta` ya incluido en una liquidación activa (`Pendiente` o `Pagada`) vuelva a liquidarse, y que una segunda llamada en el mismo período sin nuevos renglones retorne `400 Bad Request` claro (*"No existen ventas pendientes de liquidar para este profesional en el período seleccionado."*).
  2. **Fix Botón `Pagar` Liquidación:** Auditar firma, parámetros y flujo transaccional de `POST /api/liquidaciones/{id}/pagar` (`LiquidacionesController` + `LiquidacionService.RegistrarPagoLiquidacionAsync`), garantizando que acepte pago con o sin imputación en caja abierta y actualice `Estado = Pagada` + `FechaPago = DateTime.UtcNow`.
  3. **Fix `ComisionConfigDto`:** Asegurar que `GET /api/comisiones` y `GET /api/comisiones?peluqueroId={id}` incluyan el `PeluqueroNombre` completo y `ServicioNombre` (y auditar por qué `/peluquero` mostraba `55% / 0%` en lugar de `50% / 10%`).
  4. **Rol Real `SuperAdminSaaS` (`RolId = 6`):** Agregar `Rol { Id = 6, Nombre = "SuperAdminSaaS" }` y el usuario semilla `superadmin@mipeluqueria.com` (`SuperAdmin123!`, `RolId = 6`) en `HasData`, generar migración aditiva y aplicarla con `dotnet ef database update`. Restringir cambio de plan en `SuscripcionesController` exclusivamente a `SuperAdminSaaS` (evitando que un `Administrador` de peluquería se auto-asigne Enterprise gratis).
  5. **Tests xUnit Cercados (`Etapa8BackendFixesTests.cs`):**
     * Test: Emitir liquidación en período $\rightarrow$ reintentar misma liquidación retorna `400 Bad Request` (cero duplicados) $\rightarrow$ `POST /api/liquidaciones/{id}/pagar` retorna `200 OK` y marca `Estado = Pagada`.
     * Test: `GET /api/comisiones` retorna `PeluqueroNombre == "Carlos Gómez"`.
     * Test: `Administrador` (`admin@tesis.com`) intentando `POST /api/suscripciones/cambiar-plan` recibe `403 Forbidden`, mientras `SuperAdminSaaS` (`superadmin@mipeluqueria.com`) recibe `200 OK`.
* **[SENSORES DETERMINÍSTICOS]:**
  1. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --warnaserror` $\rightarrow$ **`0 Errores / 0 Advertencias`**.
  2. `dotnet ef database update --project MiPeluqueria.Api` aplicado limpio en `MiPeluqueriaDB`.
  3. `dotnet test MiPeluqueria.Api.Tests --no-build --verbosity normal` $\rightarrow$ **100% PASS (todos los 35 previos + toda la suite Etapa 8 verdes, 0 FAIL, 0 SKIP)**.
* **🛑 [HARD GATE 6A]:** Revisión humana de los fixes críticos de Backend (`Caja`, `Liquidaciones`, `Comisiones`, `RolId = 6 SuperAdminSaaS`) y de los cimientos del Design System dual antes de rediseñar las pantallas.

---

## FASE 8B: REDISEÑO OPERATIVO (SHELL `1280px`, AGENDA, POS, CAJA Y CLIENTES)

### TAREA 8.4 — App Shell Compacto (`1280x720` / `1366x768`): Sidebar Agrupado, Topbar, Switch Claro/Oscuro y Caja con Fecha
* **Modelo Recomendado:** `Argon Mid`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/12` (Secciones 1, 2 y 3 — Transversal).
* **[ARCHIVOS PERMITIDOS]:**
  * `mipeluqueria-saas-web/src/app/(dashboard)/layout.tsx`
  * `mipeluqueria-saas-web/src/components/ui/DesignSystem.tsx` (solo ajustes menores de soporte al layout)
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. **Sidebar Compacto (`w-56` / `224px` en desktop + drawer colapsable en móvil/pantallas chicas):**
     * **Eliminar** todos los prefijos numéricos (`1.`, `2.`, ..., `10.`) y eliminar el texto `TENANT #1`.
     * Integrar `<BrandLogo />` (con contenedor claro para logo negro), nombre comercial del salón (`Mi Peluquería Central`) y **badge discreto del Rol** (`Administrador`, `Peluquero`, `Recepcionista`, `SuperAdminSaaS`).
     * Agrupar ítems con íconos `lucide-react` (`16px`) en bloques claros:
       * *Mi Espacio:* `Mi Puesto`
       * *Operación:* `Calendario`, `Punto de Venta`, `Caja Diaria`, `Clientes`
       * *Gestión:* `Dashboard BI`, `Inventario y Compras`, `Servicios y Recetas`, `Staff y Liquidaciones`, `Configuración`
       * *Plataforma (visible SOLO si `rol === 'SuperAdminSaaS'`):* `Consola SaaS`, `Diagnóstico API`
     * **Footer del Sidebar:** Indicador de caja con fecha real de apertura (`Caja #N • DD/MM` en esmeralda o `Caja Cerrada` en rojo suave), botón `Cambiar Clave` y `Salir`.
  2. **Topbar Fino (`h-14`):** Muestra breadcrumb/título de sección actual, **fecha del día del salón** formateada en español (`Mar, 24 Nov 2026`), selector compacto de Sucursal (cuando corresponda) y `<ThemeToggle />` (Claro / Oscuro).
  3. **Garantía de Espacio en Notebook Estándar:** Área de contenido principal con `min-w-0 flex-1 overflow-x-hidden` y padding proporcionado (`px-4 py-4 lg:px-6 lg:py-5`) para que a `1280x720` el contenido tenga `~1056px` libres y `0px` de desborde horizontal.
* **[SENSORES DETERMINÍSTICOS]:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`0`).
  2. Auditoría `\bany\b` (`0`) y `0` imports de dominio en `src/components/ui/`.
  3. `npm --prefix mipeluqueria-saas-web run build` (`0`).

---

### TAREA 8.5 — Mi Puesto Profesional (`/peluquero`) + Calendario Master (`/recepcion/calendario`)
* **Modelo Recomendado:** `Carbon` / `Argon High`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sec. 4.3 y 4.4), `docs/12` (Sección 3 — Pantallas 1 y 2).
* **[ARCHIVOS PERMITIDOS]:**
  * `mipeluqueria-saas-web/src/app/(dashboard)/peluquero/page.tsx`
  * `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/calendario/page.tsx`
  * `mipeluqueria-saas-web/src/services/turnos.service.ts` (solo si falta pasar `fechaDesde`/`fechaHasta` en `getAllPaged`)
  * `mipeluqueria-saas-web/src/hooks/useDomainQueries.ts` (solo soporte query semanal)
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. **`/peluquero` (Mi Puesto Profesional — Opción 1 + Móvil en Fichas Técnicas):**
     * *1. Mi Agenda:* Segmented control **`Hoy` | `Mañana` | `Esta semana`** + datepicker. En vistas diarias renderizar **línea horaria vertical real** (`09:00` a `20:00`, con franja sombreada del descanso del profesional y línea de hora actual); en `Esta semana` renderizar columnas de lunes a sábado con turnos apilados. Quitar textos técnicos del header.
     * *2. Fichas Técnicas Capilares (Desktop + Mobile-First `390x844`):* Reemplazar la tira horizontal apretada de botones por layout **Master-Detail**: en desktop (`>= md`) lista vertical izquierda (30%) con buscador instantáneo, avatar con iniciales, Nombre y Apellido y subtítulo limpio + panel de historial derecho (70%); en móvil (`< md`, celular en silla) alternancia limpia lista $\leftrightarrow$ detalle con botón `"← Volver a clientes"`, banner rojo destacado de alergias y `+ Nueva Ficha Técnica` siempre accesible.
     * *3. Mis Comisiones y Recibos:* Corregir lectura de la regla vigente (mostrar los porcentajes reales `50%` servicios / `10%` productos de `ComisionesConfig`, no valores erróneos `55% / 0%`), unificar botones con el Design System y limpiar encabezados.
     * *4. Mis Horarios y Ausencias:* Adaptar al Design System dual (Claro/Oscuro) y contraste WCAG.
  2. **`/recepcion/calendario` (Calendario Master — Opción 1 estilo Fresha):**
     * Switch **`Día` | `Semana`**, botones rápidos `Hoy`, `<` / `>`, datepicker y filtro de estado.
     * **Vista Día:** Grilla horaria vertical (`09:00` a `20:00`, intervalos de 30 min) con columnas por profesional (encabezado con avatar/iniciales y color `colorAgendaHex`), descansos y bloqueos sombreados, bloques de turno proporcionales a su duración pintados con acento `colorAgendaHex` y menú rápido al clic (*Confirmar*, *En proceso*, *Cobrar en POS*, *Reprogramar*, *Cancelar*). Clic en celda horaria libre abre `"Nuevo Turno"` preseleccionando ese profesional y horario.
     * **Vista Semana:** Grilla Lunes a Sábado con selector de profesional (todos o individual).
     * **Modal "Nuevo Turno":** Reemplazar `<select>` saturado de clientes por `<SearchCombobox />` (`Nombre Apellido` + teléfono discreto), mantener **Registro Express** (Nombre, Apellido, Teléfono) y **Bloqueos / Ausencias** desde el header.
* **[SENSORES DETERMINÍSTICOS]:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`0`).
  2. Auditoría `\bany\b` (`0`).
  3. `npm --prefix mipeluqueria-saas-web run build` (`0`).

---

### TAREA 8.6 — Punto de Venta POS (`/recepcion/pos`), Caja Diaria (`/recepcion/caja`) y Clientes CRM (`/recepcion/clientes`)
* **Modelo Recomendado:** `Argon High` / `Carbon`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sec. 4.3), `docs/12` (Sección 3 — Pantallas 3, 4 y 5).
* **[ARCHIVOS PERMITIDOS]:**
  * `mipeluqueria-saas-web/src/components/ui/PrintReceiptModal.tsx`
  * `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/pos/page.tsx`
  * `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/caja/page.tsx`
  * `mipeluqueria-saas-web/src/app/(dashboard)/recepcion/clientes/page.tsx`
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. **Rediseño de `PrintReceiptModal.tsx` (Fix Impresión 1 Sola Hoja):**
     * Nuevo diseño de comprobante limpio y estructurado (encabezado con `<BrandLogo />`, nombre comercial, CUIT, fecha/hora, detalle tabular con `JetBrains Mono` + `tabular-nums`, desglose de seña y medios de pago).
     * CSS `@media print` estricto usando portal/contenedor exclusivo de impresión que **oculta** el fondo del modal, el título `"Vista Previa de Comprobante"`, la `"X"` de cierre y el resto del DOM, garantizando **1 sola hoja** limpia al imprimir o guardar PDF.
  2. **`/recepcion/pos` (Opción 1):**
     * Header limpio: eliminar `"MOSTRADOR & FACTURACIÓN MULTIPAGO • HEADER X-IDEMPOTENCY-KEY"` y `"Clave de Idempotencia Activa: ..."`; mostrar badge semántico verde **`Caja del DD/MM/YYYY (#N)`** (manteniendo la lógica invisible de `idempotencyKey` 100% intacta bajo el capó).
     * Reemplazar input de importe por `<NumericInput />` (sin `0` pegajoso) y aplicar colores semánticos a atajos (`100% Efectivo` esmeralda, `100% Digital/QR` azul cielo).
     * Optimizar grilla 60/40 para `1280x720` sin cortes ni scroll horizontal.
  3. **`/recepcion/caja` (Opción 1):**
     * Eliminar `"TESORERÍA & ARQUEO DE BILLETES • CAJACONTROLLER"` y la fórmula matemática del subtítulo.
     * Dos tarjetas KPI claras con colores semánticos oficiales (**Efectivo en Cajón** en Esmeralda; **Recaudación Digital** en Azul Cielo).
     * `<NumericInput />` en apertura, ingresos/egresos manuales y monto contado al cierre.
     * Tabla de historial mostrando **`Sesión #N — DD/MM/YYYY`** con `<PaginationBar />` real (sin scroll interno atrapado) y datos consistentes tras el fix de la Tarea 8.2.
  4. **`/recepcion/clientes` (Opción 1):**
     * Eliminar `"CRM 360° & DIAGNÓSTICO CAPILAR • CLIENTESCONTROLLER + FICHASTECNICASCONTROLLER"` y subtítulo redundante; mostrar título limpio `"Clientes"` + badge con total + único botón `primary` `+ Nuevo Cliente`.
     * Filas con avatar de iniciales, badges suaves de Cabello/Rostro y botones jerarquizados (`Fichas Técnicas` secundario + `Editar` secundario + `Baja` `destructive soft`).
     * En modal de Fichas Técnicas: vista previa en miniatura de la foto cargada e indicador claro cuando Cloudinary está en modo demo local (ofreciendo pegar URL manual sin confusión).
* **[SENSORES DETERMINÍSTICOS]:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`0`).
  2. Auditoría `\bany\b` (`0`) y `0` imports de dominio en `src/components/ui/`.
  3. `npm --prefix mipeluqueria-saas-web run build` (`0`).
* **🛑 [HARD GATE 6B]:** Revisión humana en vivo (a `1280x720` / `1366x768`, Modo Claro y Modo Oscuro) del Shell, Agenda, POS (incluyendo vista previa de impresión en 1 hoja), Caja y Clientes.

---

## FASE 8C: REDISEÑO GERENCIAL, COMPRAS PRO, SEGURIDAD Y CERTIFICACIÓN E2E `v1.1.0`

### TAREA 8.7 — Dashboard Ejecutivo BI (`/admin/dashboard`) + Inventario, Proveedores y OC Pro (`/admin/inventario`)
* **Modelo Recomendado:** `Argon High`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sec. 4.5), `docs/12` (Secciones 2.3 y 3 — Pantallas 6 y 7).
* **[ARCHIVOS PERMITIDOS]:**
  * `mipeluqueria-saas-web/src/app/(dashboard)/admin/dashboard/page.tsx`
  * `mipeluqueria-saas-web/src/app/(dashboard)/admin/inventario/page.tsx`
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. **`/admin/dashboard` (Opción 1):**
     * Botones rápidos de período **`Hoy` | `Esta semana` | `Este mes`** + datepickers compactos.
     * 6 tarjetas KPI sin números `1.`–`6.`, con íconos semánticos y cifras en `JetBrains Mono tabular-nums`.
     * **Coherencia Cromática Absoluta:** Donut Chart de *Ingresos por Medio de Pago* y sus barras horizontales inferiores consumiendo **exactamente el mismo color** desde `src/lib/semanticColors.ts` (Efectivo = Esmeralda, Mercado Pago QR = Azul Cielo, Transferencia = Índigo, Tarjeta = Violeta), coincidiendo 100% con Caja y POS.
     * Gráfico de barras *Ranking por Estilista* estilizado con `colorAgendaHex` o `--brand-primary`, tooltip legible en ambos temas y alerta de stock crítico estilizada con acceso directo a Inventario.
  2. **`/admin/inventario` (Opción 2 — Completa con Auto-OC + PDF + WhatsApp):**
     * **Pestaña Productos:** Buscador instantáneo por nombre/código + filtro por categoría + toggle `"Solo stock crítico"`. Renombrar botón `Kardex` a **`Historial`** (tooltip *"Movimientos auditados de stock"*). En modal Editar Producto reemplazar `"CAMPO PROTEGIDO POR KARDEX"` por badge amigable **`"Se ajusta desde Movimientos"`** con botón directo a abrir Movimiento. Usar `<NumericInput />` en precios y cantidades.
     * **Pestaña Proveedores:** Buscador por razón social/CUIT/contacto. Hacer clicable el badge **`N artículo(s)`** para abrir un modal limpio listando los productos vinculados de ese proveedor (`GET /api/productos?proveedorId={id}`, mostrando nombre, SKU, costo y stock actual).
     * **Pestaña Órdenes de Compra (OC):**
       * Modal multi-ítem con encabezados claros (`Producto`, `Cantidad`, `Costo Unit.`, `Subtotal`), botón `+ Agregar renglón` y total calculado en vivo.
       * Botón **`"Generar OC con faltantes"`**: consulta `GET /api/productos/stock-critico`, agrupa los faltantes por proveedor y precarga automáticamente el pedido sugerido para emitir en 1 clic.
       * En cada OC emitida: acción **`Ver / Imprimir OC (PDF)`** (usando `PrintReceiptModal` en formato documento comercial limpio de 1 hoja) y acción **`Enviar por WhatsApp`** (`https://wa.me/{telefono}?text=...` con el listado formateado de productos y cantidades solicitadas).
* **[SENSORES DETERMINÍSTICOS]:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`0`).
  2. Auditoría `\bany\b` (`0`).
  3. `npm --prefix mipeluqueria-saas-web run build` (`0`).

---

### TAREA 8.8 — Servicios/Recetas (`/admin/servicios-recetas`) + Staff, Comisiones y Liquidaciones (`/admin/staff-liquidaciones`)
* **Modelo Recomendado:** `Argon High`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sec. 4.5), `docs/12` (Sección 3 — Pantallas 8 y 9).
* **[ARCHIVOS PERMITIDOS]:**
  * `mipeluqueria-saas-web/src/app/(dashboard)/admin/servicios-recetas/page.tsx`
  * `mipeluqueria-saas-web/src/app/(dashboard)/admin/staff-liquidaciones/page.tsx`
  * `mipeluqueria-saas-web/src/services/ventas.service.ts` (solo si el payload de `liquidacionesService.pagar` requiere alinearse al fix de Tarea 8.3)
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. **`/admin/servicios-recetas` (Opción 1):**
     * Limpiar subtítulos técnicos en Catálogo y Recetas; usar `<NumericInput />` en precios/duraciones/cantidades.
     * En editor de Receta Técnica: añadir etiquetas explícitas de columna (`Insumo`, `Unid. a descontar`, `Medida técnica`, `Unidad`), mostrando el **Margen Neto Real** en esmeralda junto al **porcentaje de rentabilidad**.
     * Colapsar la imputación manual en un acordeón discreto `"Ajuste manual de consumo por turno"` (eliminando el texto `Ejecuta POST /api/recetas-servicio/...` y el input numérico crudo `ID DEL TURNO: 1`), reemplazándolo por `<SearchCombobox />` cargado con los turnos recientes (`#id • Cliente • Fecha • Servicio`).
  2. **`/admin/staff-liquidaciones` (Opción 1 + Fix `Recibo PDF` y `Pagar`):**
     * **Profesionales:** Renderizar hasta 2 chips de especialidad en la fila + botón/badge clicable **`+N más`** que despliega un popover ordenado con todas las especialidades (evitando desbordes a `1280px`).
     * **Esquemas de Comisión:** Mostrar el nombre real **`Carlos Gómez`** (desde `PeluqueroNombre` de Tarea 8.3) en vez de `Peluquero #1`, y `<NumericInput />` en porcentajes.
     * **Liquidación de Haberes:**
       * Deshabilitar botón `"Calcular y Emitir Liquidación"` durante mutación (`isPending`) para impedir doble emisión accidental y limpiar mensaje técnico (*"Liquidación emitida correctamente."* en vez de mencionar `DetallesLiquidacion`).
       * **Fix `Recibo PDF`:** Corregir el parseo de fechas que arrojaba error de tiempo/milisegundos (normalizar strings ISO con o sin fracción de milisegundos de SQL Server antes de formatear) y abrir el comprobante limpio de 1 hoja en `PrintReceiptModal`.
       * **Fix `Pagar`:** Conectar botón `Pagar` a un `<ConfirmModal />` que permita elegir si imputar el egreso en la Caja Abierta actual (cuando exista) o marcarlo como pagado fuera de caja, invocando `liquidacionesService.pagar(...)` con el contrato validado en Tarea 8.3 e invalidando `liquidaciones` + `caja`.
* **[SENSORES DETERMINÍSTICOS]:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`0`).
  2. Auditoría `\bany\b` (`0`).
  3. `npm --prefix mipeluqueria-saas-web run build` (`0`).

---

### TAREA 8.9 — Configuración/Seguridad (`/admin/configuracion-seguridad`) + Separación `SuperAdminSaaS` (`/saas-admin`, `/admin/diagnostico-api`)
* **Modelo Recomendado:** `Argon Mid`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sec. 4.5), `docs/12` (Sección 3 — Pantallas 10 y 11).
* **[ARCHIVOS PERMITIDOS]:**
  * `mipeluqueria-saas-web/src/middleware.ts`
  * `mipeluqueria-saas-web/src/lib/formatters.ts` (helper puro `formatUserAgentSummary` para leer navegador/SO humano)
  * `mipeluqueria-saas-web/src/app/(dashboard)/admin/configuracion-seguridad/page.tsx`
  * `mipeluqueria-saas-web/src/app/(dashboard)/saas-admin/page.tsx`
  * `mipeluqueria-saas-web/src/app/(dashboard)/admin/diagnostico-api/page.tsx`
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. **`/admin/configuracion-seguridad` (Opción 1):**
     * **Ajustes del Salón:** Adaptar al Design System dual con `<NumericInput />` en intervalos, tolerancia, horas de cancelación y `% seña`.
     * **Catálogos Maestros:** Eliminar textos técnicos `(CRUD Completo)`, `(POST)`, `(PUT)`. Al hacer clic en `Editar`, resaltar claramente la fila activa en el panel derecho con borde `--brand-primary`, fondo tintado y badge **`Editando`**, haciendo foco automático en el input del formulario.
     * **Usuarios y Sesiones:**
       * Modal `"Crear Cuenta de Personal"`: bloquear autofill invasivo de Chrome (`autoComplete="off"` en form y `autoComplete="new-password"` en contraseña, con nombres de campo no ambiguos) y reemplazar el campo numérico crudo `ID LEGAJO PELUQUERO` por un **desplegable de profesionales existentes** (`peluqueros`).
       * Tabla `"Auditoría de Accesos"`: eliminar el contenedor `max-h` con scroll interno atrapado, mostrar **10 eventos por página** con `<PaginationBar />` real conectado a la API y formatear el `Dispositivo` de forma legible (`Chrome · Windows`, `Safari · iOS`, `Cliente Node`, dejando el User-Agent completo en tooltip `title`).
     * **Nueva pestaña `4. Mis Sucursales y Plan` (para `Administrador`):** Permite al dueño del salón administrar sus sucursales físicas y visualizar en modo lectura su plan vigente (sin poder auto-asignarse planes pagos).
  2. **Separación Real `SuperAdminSaaS` (`/saas-admin` y `/admin/diagnostico-api` — Opción 2):**
     * Actualizar `src/middleware.ts` para que `/saas-admin` y `/admin/diagnostico-api` permitan exclusivamente el rol **`SuperAdminSaaS`** (redirigiendo a `/admin/dashboard` si entra un `Administrador` común).
     * En `/saas-admin/page.tsx`: eliminar la pestaña con datos mockeados `3. AuditLogs JSON Diff`, mostrar `Simulador Webhooks Pago` únicamente cuando `process.env.NODE_ENV === 'development'`, y adaptar toda la vista al Design System dual.
     * En `/admin/diagnostico-api/page.tsx`: conservar intacta toda su funcionalidad (lotes x4 + Health Checks), adaptando sus colores al mapa semántico dual Claro/Oscuro.
* **[SENSORES DETERMINÍSTICOS]:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`0`).
  2. Auditoría `\bany\b` (`0`).
  3. `npm --prefix mipeluqueria-saas-web run build` (`0`).

---

### TAREA 8.10 — Limpieza de Datos E2E, Suite Playwright Responsive (`1280x720`, `1366x768`, `390x844`), Certificación Integral y Cierre `v1.1.0`
* **Modelo Recomendado:** `Carbon` / `Argon High`
* **[LECTURA OBLIGATORIA PREVIA]:** `AGENTS.md`, `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `docs/12` (Sección 1).
* **[ARCHIVOS PERMITIDOS]:**
  * `scripts/clean-e2e-data.sql` (nuevo — borra exclusivamente registros sintéticos `*E2E*` / `*ReservaPublica*` de `MiPeluqueriaDB` sin tocar el Data Seeding)
  * `mipeluqueria-saas-web/playwright.config.ts`
  * `mipeluqueria-saas-web/e2e/auth.setup.ts`
  * `mipeluqueria-saas-web/e2e/smoke-19-routes.spec.ts`
  * `mipeluqueria-saas-web/e2e/critical-journeys.spec.ts`
  * `mipeluqueria-saas-web/e2e/layout-responsive.spec.ts` (nuevo)
  * Fixes quirúrgicos en `mipeluqueria-saas-web/src/**` si algún sensor visual/responsive detecta un desborde o fallo real
  * `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`
* **[ALCANCE]:**
  1. **Higiene de Base de Datos Local:** Crear `scripts/clean-e2e-data.sql` y agregar limpieza automática al cierre de `critical-journeys.spec.ts` para no dejar clientes/proveedores sintéticos (`Camila ReservaPublica...`, `Valentina ApellidoE2E...`) contaminando el entorno de desarrollo.
  2. **Actualización Dual de Auth en Playwright (`auth.setup.ts`):**
     * Generar `e2e/.auth/admin.json` (`admin@tesis.com` / `Admin123!`) para las pantallas del salón y `e2e/.auth/superadmin.json` (`superadmin@mipeluqueria.com` / `SuperAdmin123!`) para `/saas-admin` y `/admin/diagnostico-api`.
     * Verificar explícitamente que `admin@tesis.com` al intentar navegar a `/saas-admin` sea redirigido (protección RBAC efectiva).
  3. **Nuevo Sensor Determinístico de Layout y Temas (`e2e/layout-responsive.spec.ts`):**
     * Recorrer las rutas principales en **`1280x720`** y **`1366x768`** (tanto en **Modo Claro** como en **Modo Oscuro**), verificando con aserción estricta:
       * `scrollWidth <= innerWidth` (`0px` de desborde horizontal en notebook estándar).
       * Cero `pageerror`, cero `console.error`, cero `HTTP >= 500`.
     * Verificar **`/peluquero` (pestaña Fichas Técnicas)** en viewport móvil **`390x844`**: `scrollWidth <= innerWidth`, lista de clientes operable y apertura de detalle sin cortes.
  4. **Ampliación de Flujos Críticos (`critical-journeys.spec.ts`):**
     * Mantener intactos **Journey 1**, **Journey 2** y **Journey 3** (actualizando selectores si cambió algún texto de botón/etiqueta, nunca relajando aserciones).
     * Sumar **Journey 4 (Liquidación + Recibo PDF + Pago + OC Faltantes)**: emitir liquidación, abrir `Recibo PDF` sin error de milisegundos, ejecutar `Pagar`, y verificar en Inventario la generación de OC desde stock crítico y apertura de vista PDF.
  5. **Cierre Documental `v1.1.0`:** Asentar en `docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md` (Sección 1 y bloque `TAREA 8.10`) la tabla completa de métricas reales de cierre de la Etapa 8.
* **[SENSORES DETERMINÍSTICOS FINALES (TODO EN VERDE OBLIGATORIO)]:**
  1. `dotnet build MiPeluqueria.Api/MiPeluqueria.Api.sln --warnaserror` $\rightarrow$ **`0 Errores / 0 Advertencias`**.
  2. `dotnet test MiPeluqueria.Api.Tests --no-build --verbosity normal` $\rightarrow$ **100% PASS (0 FAIL, 0 SKIP)**.
  3. `node scripts/probe-api-22.mjs` $\rightarrow$ **22/22 `200 OK`** (`< 500 ms` en caliente).
  4. `npm --prefix mipeluqueria-saas-web run typecheck` (`0`) + `\bany\b` (`0`) + frontera UI (`0` imports de dominio) + `npm --prefix mipeluqueria-saas-web run build` (`0`).
  5. `npm --prefix mipeluqueria-saas-web run test:e2e` $\rightarrow$ **100% PASS** (19 rutas + 4 Journeys + `0px` overflow en `1280x720`, `1366x768` y móvil `390x844` + Claro/Oscuro + `0` errores `[ERR]` en Serilog).
  6. Limpieza final: `OPEN_APP_PORTS=0` (`7132`/`3000` libres) y `0` archivos temporales sueltos.
* **🛑 [HARD GATE 6C — RELEASE `v1.1.0`]:** Aprobación visual humana final y etiquetado Git `v1.1.0`.