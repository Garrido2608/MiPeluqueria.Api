# AGENTS.md â€” Mapa Operativo y Gobernanza Harness Engineering
**Proyecto:** MiPeluqueria SaaS (`MiPeluqueria.Api` .NET 8 + `mipeluqueria-saas-web` Next.js 14)
**Paradigma:** Specification-Driven Development (SDD) + Harness Engineering + TDD Fencing

---

## 1. Flujo de Trabajo Obligatorio por Tarea
1. **Lectura de Memoria en Disco:** Antes de tocar cualquier archivo, leer [`docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md`](docs/07_EXECUTION_LEDGER_AND_CHANGELOG.md) (puntero, overrides en SecciÃ³n 2 y firmas consolidadas en SecciÃ³n 4) y [`docs/11_QA_HARDENING_OBSERVABILITY_AND_REUSABILITY_PLAN.md`](docs/11_QA_HARDENING_OBSERVABILITY_AND_REUSABILITY_PLAN.md).
2. **Alcance Estricto:** Modificar Ãºnicamente los archivos listados en `[ARCHIVOS PERMITIDOS]` de la tarea activa.
3. **Triaje en 5 Pasos ante Defectos:** `Reproducir (sensor rojo) -> Localizar -> Reducir -> Corregir -> Blindar (regresiÃ³n verde)`.
4. **Cierre y BitÃ¡cora:** Ejecutar el `[SENSOR DETERMINÃSTICO]` de la tarea, adjuntar su salida real, actualizar `07_EXECUTION_LEDGER_AND_CHANGELOG.md` (aplicando SPEC-SYNC si corresponde) y detenerse en cada Hard Gate.

---

## 2. Leyes Innegociables de IngenierÃ­a
* **Cercado de Tests (Fencing Rule):** Prohibido debilitar, comentar, saltar (`Skip`) o relajar aserciones en sondas (`probe-api-22.mjs`), tests xUnit (`MiPeluqueria.Api.Tests`) o Playwright (`e2e/`). Si un test falla, se corrige el cÃ³digo de producciÃ³n hasta cumplir la especificaciÃ³n de `08` y `10`.
* **Cero Reflection y Cero DbContext Directo:** Prohibido `System.Reflection` en servicios/repositorios y prohibido inyectar `ApplicationDbContext` en `Controllers/` o `Services/`. Flujo Ãºnico: `Controller -> IService -> IUnitOfWork -> IRepository -> ApplicationDbContext`.
* **TypeScript Strict (`0 any`):** Prohibido el uso de `any` en `mipeluqueria-saas-web/src/**` y `e2e/**`.
* **Frontera de ReutilizaciÃ³n UI (`src/components/ui/`):** Todo componente en `src/components/ui/` debe ser **100% agnÃ³stico de dominio** (`0` imports de `src/services/*`, `useDomainQueries` o tipos de negocio), composable (`children`, `className`, genÃ©ricos `<T>`) y documentado con JSDoc (`@param`, `@example`).

---

## 3. PolÃ­tica Estricta de `try-catch`
* **Backend (.NET 8):** Prohibido usar `try-catch` en Controladores o para flujo de negocio esperado. Solo permitido en:
  1. Integraciones externas no bloqueantes (`SmtpEmailService`, `WhatsAppService`, pasarelas).
  2. Aislamiento por sucursal en `RecordatorioTurnosBackgroundService`.
  3. Rollback en `UnitOfWork.ExecuteInTransactionAsync` (siempre relanzando con `throw;`).
  4. `GlobalExceptionMiddleware` y bootstrap logger en `Program.cs`.
* **Frontend (Next.js 14):** Errores HTTP esperados (`400/404/409`) se procesan vÃ­a TanStack Query + Zod + `sonner`. Crashes de render caen en Error Boundaries (`error.tsx`, `(dashboard)/error.tsx`, `global-error.tsx`). Handlers asÃ­ncronos fuera de `useApiMutation` reportan a `errorLogger.ts`.

---

## 4. Observabilidad y Privacidad en Logs (Zero-PII)
* Todos los errores de Backend y Frontend convergen en `MiPeluqueria.Api/Logs/errors-YYYYMMDD.json` (`CompactJsonFormatter`) correlacionados mediante `X-Correlation-Id`, `TenantId`, `SucursalId` y `UserId`.
* **ProhibiciÃ³n Absoluta de PII/Secretos:** JamÃ¡s registrar `Password`, `PasswordHash`, tokens JWT, headers `Authorization` o `Cookie`, connection strings, emails, telÃ©fonos ni cuerpos crudos completos de request.

---

## 5. Sensores DeterminÃ­sticos Obligatorios
Ninguna tarea se cierra solo por compilaciÃ³n. Sensores exigidos segÃºn la fase:
* **CompilaciÃ³n Base:** `dotnet build` (`0 Errores / 0 Advertencias`) y `npm --prefix mipeluqueria-saas-web run typecheck` + `npm --prefix mipeluqueria-saas-web run build`.
* **Sonda Runtime Backend:** `node scripts/probe-api-22.mjs` (`22/22` endpoints `200 OK`, latencia `< 2000 ms`, objetivo en caliente `< 500 ms`) + `/health/live` y `/health/ready`.
* **Suite BDD Backend:** `dotnet test MiPeluqueria.Api.Tests` (`100% PASS / 0 FAIL / 0 SKIP`).
* **Frontera UI y Tipado:** `Select-String` verificando `0` imports de dominio en `src/components/ui/*.tsx` y `0` ocurrencias de `\bany\b`.
* **Suite E2E Frontend:** `npm --prefix mipeluqueria-saas-web run test:e2e` (`19/19` rutas + `3/3` Journeys crÃ­ticos en verde).

