# 11 - PLAN MAESTRO DE BLINDAJE QA, OBSERVABILIDAD, TESTING AUTOMATIZADO Y REUTILIZACIÓN (ETAPA 7 — CAPA 11 SDD)
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Suite Vinculada:** `AGENTS.md`, `01_SYSTEM_CONSTRAINTS.md` a `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`
**Paradigma Operativo:** Harness Engineering (Guías + Sensores Determinísticos) + TDD Fencing
**Estado:** PLAN ACTIVO — ETAPA 7 (9 MICRO-TAREAS ATÓMICAS + 3 HARD GATES)

---

## 1. LEYES DE EJECUCIÓN PARA LA ETAPA 7 (HARNESS ENGINEERING)

1. **CERO CIERRES SOLO POR COMPILACIÓN:** Ninguna tarea de la Etapa 7 se da por cerrada únicamente con `dotnet build` o `next build`. Cada tarea exige ejecutar su **Sensor Determinístico** y adjuntar la salida real de consola.
2. **CERCADO DE TESTS (FENCING RULE):** Cuando un test automatizado (Sonda, xUnit BDD o Playwright) falle contra el código de producción, **queda terminantemente prohibido debilitar, comentar o relajar una aserción del test**. El agente debe localizar la causa raíz en producción y corregirla hasta que el test pase tal como fue especificado en `08` y `10`.
3. **TRIAJE EN 5 PASOS:** Todo defecto real se resuelve siguiendo: `Reproducir (sensor rojo) -> Localizar -> Reducir -> Corregir -> Blindar (regresión verde)`.
4. **POLÍTICA ESTRICTA DE `try-catch`:**
   - **Backend (.NET 8):** Prohibido poner `try-catch` en Controladores o para control de flujo esperable. Solo permitido en: a) integraciones externas no bloqueantes (`SmtpEmailService`, `WhatsAppService`, pasarelas), b) aislamiento por sucursal en `RecordatorioTurnosBackgroundService`, c) rollback en `UnitOfWork.ExecuteInTransactionAsync` (siempre relanzando con `throw;`), y d) `GlobalExceptionMiddleware`.
   - **Frontend (Next.js 14):** Errores esperados (400/404/409) se manejan como estado con TanStack Query + Zod + `sonner`. Errores inesperados de render caen en Error Boundaries (`error.tsx`, `(dashboard)/error.tsx`, `global-error.tsx`). Event handlers asíncronos fuera de `useApiMutation` usan `try-catch` que reporta a `errorLogger.ts`.
5. **POLÍTICA DE OBSERVABILIDAD Y PRIVACIDAD EN LOGS:**
   - Todo error de API y Frontend converge en `MiPeluqueria.Api/Logs/errors-YYYYMMDD.json` (`CompactJsonFormatter`) correlacionado por `X-Correlation-Id`, `TenantId`, `SucursalId` y `UserId`.
   - **Prohibido loguear PII o secretos:** jamás registrar `Password`, `PasswordHash`, JWT, headers `Authorization`/`Cookie`, connection strings, emails, teléfonos ni cuerpos completos de request.
6. **FRONTERA DE REUTILIZACIÓN:**
   - Todo componente en `mipeluqueria-saas-web/src/components/ui/` debe ser **100% agnóstico de dominio** (`0` imports de `src/services/*`, `useDomainQueries` o tipos de negocio), composable (`children`, `className`) y documentado con JSDoc (`@param`, `@example`).

---

## FASE 7A: GOBERNANZA SPEC-SYNC, SONDA RUNTIME Y OBSERVABILIDAD BACKEND

### TAREA 7.1 — SPEC-SYNC Documental + Mapa `AGENTS.md`
* **Modelo Recomendado:** `Hydrogen` / `Argon Low`
* **[LECTURA OBLIGATORIA PREVIA]:** `07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md`, `04_API_CONTRACTS.md`, `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md`, `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`, `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`.
* **[ARCHIVOS PERMITIDOS]:** `AGENTS.md` (nuevo en raíz del repo), `01`, `02`, `04`, `07`, `08`, `09`, `10` (solo bloques SPEC-SYNC), `.gitignore`.
* **[ALCANCE]:**
  1. Registrar en `07` (Sección 2) y `01` (Secciones 2.1, 2.3, 4.4 y 5): autorización del proyecto satélite `MiPeluqueria.Api.Tests.csproj` (sin dividir el monolito de producción), paquetes NuGet (`Serilog.AspNetCore`, `Serilog.Formatting.Compact`, `Serilog.Sinks.File`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`, `xunit`, `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`), `@playwright/test`, unificación de ruta `/api/configuracion` (manteniendo compatibilidad) y regla de componentes reutilizables `src/components/ui/`.
  2. Actualizar `02` (orden de pipeline con `CorrelationIdMiddleware`, `UseSerilogRequestLogging` y `MapHealthChecks`), `04` (contratos de `POST /api/telemetry/client-errors` y `/health/live`, `/health/ready`), `08` y `10` (ruta `/api/configuracion` + Error Boundaries + frontera UI), `09` (sección `Serilog` en JSON).
  3. Crear `AGENTS.md` en la raíz (< 80 líneas) como mapa operativo para agentes.
  4. Agregar `MiPeluqueria.Api/Logs/` y `mipeluqueria-saas-web/playwright-report/` + `test-results/` a `.gitignore`.
* **[SENSOR DETERMINÍSTICO]:** Verificar con `Select-String` que no queden referencias rotas a `/api/configuracion-salon` en `08` y `10` (o que el alias quede explícitamente documentado), y validar sintaxis JSON de ejemplos en `09`.

---

### TAREA 7.2 — Sonda Runtime de 22 Controladores + Erradicación de los 5 Timeouts
* **Modelo Recomendado:** `Carbon` / `Argon High`
* **[LECTURA OBLIGATORIA PREVIA]:** `07_EXECUTION_LEDGER_AND_CHANGELOG.md`, `04_API_CONTRACTS.md`.
* **[ARCHIVOS PERMITIDOS]:** `scripts/probe-api-22.mjs` (nuevo), `Controllers/` y `Services/` y `Repositories/` estrictamente involucrados en los 5 endpoints lentos (`Clientes`, `FichasTecnicas`, `RecetasServicio`, `Productos`, `Proveedores`), `Controllers/ConfiguracionSalonController.cs` (si se agrega alias de ruta), `07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[ALCANCE]:**
  1. Crear `scripts/probe-api-22.mjs` que autentique con `admin@tesis.com` / `Admin123!`, invoque **secuencialmente** los 22 endpoints GET de diagnóstico e imprima tabla: Endpoint, Status HTTP, Latencia (ms), Forma del Payload (`PagedResult` / `Array` / `Object`). Exit code `1` si alguno da `!= 200` o supera `2000 ms` (objetivo en caliente `< 500 ms`).
  2. Diagnosticar y corregir la causa raíz de los timeouts > `20000 ms` detectados en `GET /clientes?pageNumber=1&pageSize=5`, `GET /fichastecnicas/cliente/1`, `GET /recetas-servicio/servicio/1`, `GET /productos` y `GET /proveedores` (revisando consultas EF Core, `AsNoTracking()`, `Include`, `AsSplitQuery()`, paginación, o ciclos de serialización).
* **[SENSOR DETERMINÍSTICO]:** `dotnet build` (`0 errores / 0 advertencias`) + ejecución real de `node scripts/probe-api-22.mjs` con **22/22 `200 OK`** y latencias bajo umbral.

---

### TAREA 7.3 — Observabilidad Backend (Serilog JSON + CorrelationId + Health Checks + Telemetría + Auditoría `try-catch`)
* **Modelo Recomendado:** `Argon High` / `Neon High`
* **[LECTURA OBLIGATORIA PREVIA]:** `07`, `02_ARCHITECTURE.md` (Sec. 3), `04_API_CONTRACTS.md`, `09_ODS_ENVIRONMENT_AND_CONFIG_SPEC.md`, `11` (Sec. 1).
* **[ARCHIVOS PERMITIDOS]:** `MiPeluqueria.Api.csproj`, `Program.cs`, `appsettings.json`, `appsettings.Development.json`, `Middlewares/CorrelationIdMiddleware.cs` (nuevo), `Middlewares/GlobalExceptionMiddleware.cs`, `Middlewares/TenantResolutionMiddleware.cs`, `DTOs/Common/ClientErrorReportDto.cs` (nuevo), `Controllers/TelemetryController.cs` (nuevo), y cualquier Controller/Service donde la auditoría detecte `try-catch` indebido, `07`.
* **[ALCANCE]:**
  1. Instalar `Serilog.AspNetCore`, `Serilog.Formatting.Compact`, `Serilog.Sinks.File`, `Microsoft.Extensions.Diagnostics.HealthChecks.EntityFrameworkCore`.
  2. Configurar Bootstrap Logger en `Program.cs` (`try { ... } catch (Exception ex) { Log.Fatal(...); throw; } finally { Log.CloseAndFlush(); }`) y sinks en `appsettings.json`: Consola + `Logs/app-.log` (`Information`, 14 días) + `Logs/errors-.json` (`Warning+`, `CompactJsonFormatter`, 30 días).
  3. Crear `CorrelationIdMiddleware` (lee/genera `X-Correlation-Id`, lo inyecta en `LogContext`, `IDiagnosticContext` y header de respuesta; en `TenantResolutionMiddleware` sumar `TenantId`, `SucursalId`, `UserId` a `LogContext` e `IDiagnosticContext`).
  4. Mapear `GET /health/live` (sin DB) y `GET /health/ready` (con `DbContextCheck<ApplicationDbContext>`), ambos `[AllowAnonymous]` y sin pasar por bloqueo multi-tenant.
  5. Crear `POST /api/telemetry/client-errors` (`TelemetryController`) protegido con Rate Limiting, payload acotado (`<= 4 KB`, sin PII), que escriba el crash del navegador en Serilog con `Source = "Frontend"`.
  6. Exponer `public partial class Program { }` al final de `Program.cs` para habilitar `WebApplicationFactory<Program>`.
  7. Auditar `Controllers/*.cs` y `Services/Implementations/*.cs` eliminando cualquier `try-catch` vacío o de flujo normal.
* **[SENSOR DETERMINÍSTICO]:** `dotnet build` (`0/0`) + llamadas HTTP reales a `GET /health/live` (`200 Healthy`), `GET /health/ready` (`200 Healthy`), `POST /api/telemetry/client-errors` (`202 Accepted`) + verificación física de nueva línea JSON válida con `CorrelationId` en `MiPeluqueria.Api/Logs/errors-*.json` + `node scripts/probe-api-22.mjs` (`22/22`).
* **🛑 [HARD GATE 5A]:** Revisión humana de logs JSON, health checks y 22/22 en verde.

---

## FASE 7B: SUITE DE INTEGRACIÓN BDD CERCADA (`MiPeluqueria.Api.Tests`)

### TAREA 7.4 — Proyecto `MiPeluqueria.Api.Tests` + Tests BDD 01 al 05 (Cercados)
* **Modelo Recomendado:** `Argon High` / `Carbon`
* **[LECTURA OBLIGATORIA PREVIA]:** `07`, `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md` (Tests 01–05), `09`.
* **[ARCHIVOS PERMITIDOS]:** `MiPeluqueria.Api.sln` (agregar proyecto de tests), `MiPeluqueria.Api.Tests/**` (nuevo proyecto), y correcciones estrictas en `MiPeluqueria.Api/` solo si un test cercado detecta bug real, `07`.
* **[ALCANCE]:**
  1. Crear `MiPeluqueria.Api.Tests.csproj` (xUnit, `Microsoft.AspNetCore.Mvc.Testing`, `FluentAssertions`, `Microsoft.NET.Test.Sdk`) con `CustomWebApplicationFactory` apuntando a una BD aislada `MiPeluqueriaDB_Tests` (creada con migraciones reales y reiniciable limpiamente sin tocar `MiPeluqueriaDB`).
  2. Implementar y cercar **Tests 01 al 05 de `08`** (`MultiTenant 403 + JWT prioridad`, `RateLimiting 429 + RolId=4`, `Zero-Reflection Turnos/Descansos/Fichas`, `ConfiguracionSalon SQL`, `Serializable + RowVersion 409`) + Contract Tests de los primeros 11 controladores.
* **[SENSOR DETERMINÍSTICO]:** `dotnet test MiPeluqueria.Api.Tests` → **100% PASS / 0 SKIP**.

---

### TAREA 7.5 — Tests BDD 06 al 10 + Contract Tests 12–22 + Observabilidad Tests (Cercados)
* **Modelo Recomendado:** `Carbon` / `Argon High`
* **[LECTURA OBLIGATORIA PREVIA]:** `07`, `08_TEST_PLANS_AND_ACCEPTANCE_CRITERIA.md` (Tests 06–10).
* **[ARCHIVOS PERMITIDOS]:** `MiPeluqueria.Api.Tests/**`, y correcciones estrictas en `MiPeluqueria.Api/` solo si un test cercado detecta bug real, `07`.
* **[ALCANCE]:**
  1. Implementar y cercar **Tests 06 al 10 de `08`** (`Webhooks HMAC-SHA256 + Cruce de la Muerte`, `POS Idempotente + Seña + Arqueo Físico/Digital`, `Consumo Gabinete no bloqueante + Kardex`, `Jerarquía Comisiones + Protección Anulación Ventas/Liquidaciones`, `Bloqueos Warnings + SMTP Fail-Safe`).
  2. Sumar Contract Tests de los controladores 12 al 22 + tests automáticos de `GET /health/live`, `GET /health/ready`, header `X-Correlation-Id` y `POST /api/telemetry/client-errors`.
* **[SENSOR DETERMINÍSTICO]:** `dotnet test` (suite completa) → **100% PASS / 0 FAIL / 0 SKIP**.
* **🛑 [HARD GATE 5B]:** Aprobación humana del Backend 100% blindado (`dotnet test` verde completo).

---

## FASE 7C: KIT REUTILIZABLE, RESILIENCIA FRONTEND, PLAYWRIGHT E2E Y CI/CD

### TAREA 7.6 — Kit UI Reutilizable Agnóstico + Error Boundaries + Telemetría Frontend
* **Modelo Recomendado:** `Argon Mid` / `Neon Mid`
* **[LECTURA OBLIGATORIA PREVIA]:** `07`, `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`, `11` (Sec. 1).
* **[ARCHIVOS PERMITIDOS]:** `mipeluqueria-saas-web/src/lib/errorLogger.ts` (nuevo), `src/lib/axios.ts` (propagar `X-Correlation-Id`), `src/context/AppProviders.tsx` (listener global `error`/`unhandledrejection`), `src/app/error.tsx` (nuevo), `src/app/(dashboard)/error.tsx` (nuevo), `src/app/global-error.tsx` (nuevo), `src/components/ui/AsyncStateGuard.tsx` (nuevo), `src/components/ui/ErrorFallbackCard.tsx` (nuevo), `src/components/ui/DataTable.tsx` (nuevo), `src/components/ui/PaginationBar.tsx` (nuevo), `src/components/ui/ConfirmModal.tsx` (nuevo), `src/components/ui/index.ts` (Barrel), `07`.
* **[ALCANCE]:**
  1. Construir `errorLogger.ts` con `reportClientError` usando `sendBeacon`/`fetch` + cortocircuito anti-bucle hacia `POST /api/telemetry/client-errors`.
  2. Implementar los 3 Error Boundaries oficiales de Next.js 14 App Router (`error.tsx`, `(dashboard)/error.tsx`, `global-error.tsx`).
  3. Crear los 5 componentes base reutilizables en `src/components/ui/` siguiendo SOLID/Composición (`children`, `className`, genéricos `<T>`, JSDoc completo, **0 imports de dominio**).
* **[SENSOR DETERMINÍSTICO]:**
  1. `npm --prefix mipeluqueria-saas-web run typecheck` (`0 errores`).
  2. `Select-String -Path "mipeluqueria-saas-web\src\components\ui\*.tsx" -Pattern "services/|useDomainQueries|clientes\.types|turnos\.types|ventas\.types"` → **0 coincidencias** (garantía de agnosticismo total).
  3. Auditoría `\bany\b` → **0 coincidencias**.

---

### TAREA 7.7 — Barrido Global Frontend (`PagedResult<T>`, Null-Safety y Diagnóstico en Lotes)
* **Modelo Recomendado:** `Argon Mid`
* **[LECTURA OBLIGATORIA PREVIA]:** `07`, `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md`.
* **[ARCHIVOS PERMITIDOS]:** `mipeluqueria-saas-web/src/lib/apiHelpers.ts` (nuevo), `src/services/*.service.ts`, `src/hooks/useDomainQueries.ts`, las 19 pantallas en `src/app/**`, `07`.
* **[ALCANCE]:**
  1. Crear `extractPagedItems<T>(payload)` y `extractPaginationMeta(payload)` en `src/lib/apiHelpers.ts` (soporta `PagedResult<T>` y arrays planos retornando siempre un array seguro `T[]`, jamás `undefined`).
  2. Auditar los 8 servicios y las 19 pantallas eliminando todo crash potencial tipo `clientes.length` en `recepcion/clientes/page.tsx:374` y asegurando renderizado inmediato en Proveedores, Productos, Turnos, Caja y Liquidaciones.
  3. Reemplazar bloques ad-hoc de loading/error por `<AsyncStateGuard />` reutilizable.
  4. Actualizar `src/app/(dashboard)/admin/diagnostico-api/page.tsx` para ejecutar los 22 controladores en lotes de 4 requests paralelos + tarjeta de `/health/live` y `/health/ready`.
* **[SENSOR DETERMINÍSTICO]:** `npm --prefix mipeluqueria-saas-web run typecheck` (`0`), `\bany\b` (`0`), `npm --prefix mipeluqueria-saas-web run build` (`0 errores`).

---

### TAREA 7.8 — Harness E2E con Playwright (Crawler 19 Rutas + 3 Flujos Críticos de Dinero)
* **Modelo Recomendado:** `Carbon` / `Argon High`
* **[LECTURA OBLIGATORIA PREVIA]:** `07`, `10_FRONTEND_UI_UX_AND_STATE_CONTRACT.md` (Sec. 4).
* **[ARCHIVOS PERMITIDOS]:** `mipeluqueria-saas-web/package.json`, `playwright.config.ts` (nuevo), `e2e/smoke-19-routes.spec.ts` (nuevo), `e2e/critical-journeys.spec.ts` (nuevo), y fixes quirúrgicos en `mipeluqueria-saas-web/src/**` o `MiPeluqueria.Api/**` si Playwright revela un bug real, `07`.
* **[ALCANCE]:**
  1. Instalar `@playwright/test` + Chromium (`npx playwright install chromium`) y scripts `test:e2e` en `package.json`.
  2. **`e2e/smoke-19-routes.spec.ts`:** Login con `admin@tesis.com` / `Admin123!` y navegación automática por las 19 rutas de `10`. Falla inmediatamente ante cualquier `pageerror`, `console.error` o respuesta HTTP `>= 500` / timeout.
  3. **`e2e/critical-journeys.spec.ts`:**
     - **Journey 1 (Proveedores + CRM 360°):** Alta de Proveedor -> reload -> visible en tabla. Alta de Cliente -> Ficha Técnica -> persistencia verificada.
     - **Journey 2 (Portal Público `/reservar`):** Flujo completo sin sesión consultando disponibilidad y reservando.
     - **Journey 3 (Flujo del Dinero):** Abrir Caja (`$10.000`) -> Agendar Turno -> Cobrar en `/recepcion/pos?turnoId={id}` con medio digital -> Verificar en `/recepcion/caja` que `montoFinalEsperado === $10.000` y `totalVentasDigital > 0` -> Verificar `/admin/dashboard`.
  4. Aplicar **Regla de Cercado**: iterar corrigiendo la app hasta que toda la suite pase en verde.
* **[SENSOR DETERMINÍSTICO]:** `npm --prefix mipeluqueria-saas-web run test:e2e` → **100% PASS (19/19 rutas + 3/3 Journeys)**.
* **🛑 [HARD GATE 5C]:** Revisión humana del reporte de Playwright 100% verde.

---

### TAREA 7.9 — Pipeline CI/CD (GitHub Actions) y Cierre Definitivo `v1.0.0`
* **Modelo Recomendado:** `Hydrogen` / `Argon Low`
* **[LECTURA OBLIGATORIA PREVIA]:** `07`, `11`.
* **[ARCHIVOS PERMITIDOS]:** `.github/workflows/ci.yml` (nuevo), `MiPeluqueria.Api/07_EXECUTION_LEDGER_AND_CHANGELOG.md`.
* **[ALCANCE]:**
  1. Crear `.github/workflows/ci.yml` disparado en `push` y `pull_request` sobre `develop` y `main` con 2 jobs en paralelo:
     - `backend-ci`: .NET 8 + service container SQL Server 2022 -> `dotnet build --warnaserror` -> `dotnet test`.
     - `frontend-ci`: Node 20 -> `npm ci` -> `npm run typecheck` -> verificación `0 any` -> `npm run build`.
  2. Asentar cierre definitivo de la Etapa 7 en `07_EXECUTION_LEDGER_AND_CHANGELOG.md` con tabla de métricas (latencias, cantidad de tests xUnit y Playwright, cobertura de logs).
* **[SENSOR DETERMINÍSTICO]:** Validación sintáctica YAML de `ci.yml` + corrida final combinada (`dotnet test` + `npm run typecheck` + `npm run build`).