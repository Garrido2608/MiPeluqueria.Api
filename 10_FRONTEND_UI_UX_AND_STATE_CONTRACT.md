# 10 - ARQUITECTURA DE ESTADO FRONTEND, INTERCEPTORES RESILIENTES Y MAPEO DE LAS 19 PANTALLAS (CAPA 10 SDD)
**Proyecto:** `mipeluqueria-saas-web` (Next.js 14 App Router + TypeScript Strict + TanStack Query v5 + RHF/Zod + Sonner)
**Documentos Padres Vinculados:** `01_SYSTEM_CONSTRAINTS.md`, `02_ARCHITECTURE.md`, `04_API_CONTRACTS.md`, `05_BUSINESS_RULES.md`
**Estándar de Ingeniería:** TanStack Query v5 Deterministic Cache Matrix & OWASP ASVS Frontend Security
**Estado:** INMUTABLE - CONTRATO DE INTEGRACIÓN UI <-> API PARA LA ETAPA 6

---

## 1. ARQUITECTURA DEL CLIENTE HTTP (`src/lib/axios.ts`) Y PREVENCIÓN DE "REFRESH TOKEN STORM"

Para evitar cierres de sesión accidentales cuando una pantalla lanza múltiples peticiones concurrentes con un access token recién expirado, `src/lib/axios.ts` debe implementar:

1. **Inyección Automática de Contexto en Request Interceptor:**
   * `Authorization: Bearer ${token}` (si existe sesión activa).
   * `X-Tenant-Id: ${user?.tenantId ?? process.env.NEXT_PUBLIC_DEFAULT_TENANT_ID ?? '1'}`.
   * `X-Sucursal-Id: ${user?.sucursalId ?? process.env.NEXT_PUBLIC_DEFAULT_SUCURSAL_ID ?? '1'}`.
2. **Cola de Concurrencia (`isRefreshing` + `failedQueue`) en Response Interceptor (`401 Unauthorized`):**
   * Si llega un `401` desde un endpoint que **no** sea `/auth/login` ni `/auth/refresh-token`:
     * Si `isRefreshing === false`, marca `isRefreshing = true` e invoca una única vez `POST /api/auth/refresh-token` enviando `{ token, refreshToken }`.
     * Toda otra petición que reciba `401` mientras `isRefreshing === true` se encola en `failedQueue` esperando una Promesa.
     * Cuando `/api/auth/refresh-token` responde `200 OK`, se actualizan `token` y `refreshToken` en el almacenamiento de sesión, se resuelven todas las promesas de `failedQueue` reintentando las peticiones originales con el nuevo JWT y se libera `isRefreshing = false`.
     * Si `/api/auth/refresh-token` falla, se rechaza `failedQueue`, se limpia la sesión y se redirige a `/login`.

---

## 2. MATRIZ DE CACHÉ E INVALIDACIÓN DETERMINISTA EN TANSTACK QUERY (`QUERY KEYS`)

Queda prohibido usar strings sueltos en `useQuery`. Toda clave en `src/hooks/` debe incluir el contexto `(tenantId, sucursalId)` y respetar esta jerarquía:

| Dominio | Query Key Estándar | Endpoint Vinculado | `staleTime` | Eventos que Disparan `invalidateQueries` |
| :--- | :--- | :--- | :--- | :--- |
| **Catálogos** | `['catalogos', tenantId, sucursalId]` | `GET /api/catalogos/todos` | `30 min` | `POST/PUT/DELETE /api/catalogos/{tipo}` |
| **Config Salón** | `['configuracion-salon', tenantId, sucursalId]` | `GET /api/configuracion-salon` | `30 min` | `PUT /api/configuracion-salon` |
| **Clientes** | `['clientes', tenantId, sucursalId, filter]` | `GET /api/clientes` | `1 min` | `POST/PUT/DELETE /api/clientes` |
| **Fichas Técnicas** | `['fichas-tecnicas', clienteId]` | `GET /api/fichastecnicas/cliente/{clienteId}` | `1 min` | `POST/PUT/DELETE /api/fichastecnicas` |
| **Staff / Horarios** | `['peluqueros', tenantId, sucursalId]` | `GET /api/peluqueros` y `/horarios` | `5 min` | `POST/PUT/DELETE /api/peluqueros`, `PUT /horarios` |
| **Servicios/Recetas**| `['servicios', tenantId, sucursalId]` | `GET /api/servicios` y `/api/recetas-servicio/servicio/{id}` | `5 min` | `POST/PUT/DELETE /api/servicios`, `PUT /api/recetas-servicio` |
| **Agenda / Turnos** | `['turnos', tenantId, sucursalId, filter]` | `GET /api/turnos` y `/disponibilidad` | `0 (siempre fresco)` | Alta/reprogramación/cambio de estado de turno, alta de bloqueo horario y cobro en POS con `turnoId`. |
| **Caja** | `['caja', tenantId, sucursalId, 'actual']` | `GET /api/caja/estado-actual` y `/historial` | `0 (siempre fresco)` | Apertura, cierre, ingreso/egreso manual, venta POS, recepción de OC con caja y pago de liquidación. |
| **Stock / Kardex** | `['productos', tenantId, sucursalId, filter]` | `GET /api/productos` y `/{id}/kardex` | `30 seg` | CRUD productos, movimiento manual, recepción OC, venta POS y consumo de gabinete. |
| **Dashboard KPIs** | `['dashboard', tenantId, sucursalId, rango]` | `GET /api/dashboard/kpis` | `2 min` | Venta POS, cierre de turno y cierre de caja. |

---

## 3. PROTOCOLO UNIFICADO DE FEEDBACK VISUAL (`SONNER` + `ApiResponse<T>` + `409 Conflict`)

Todo custom hook de mutación (`useMutation`) debe procesar el contrato `ApiResponse<T>` bajo estas 4 reglas estrictas:
1. **Éxito Limpio (`res.success === true` y `!res.warnings?.length`):** Ejecutar `toast.success(res.message)`.
2. **Éxito con Alertas No Bloqueantes (`res.success === true` y `res.warnings?.length > 0`):** Ejecutar `toast.success(res.message)` e iterar sobre `res.warnings` disparando `toast.warning(alerta, { duration: 8000 })` por cada ítem (fundamental para informar stock de gabinete agotado, turnos solapados por un bloqueo o fallo de envío de correo SMTP).
3. **Error de Validación de Campos (`HTTP 400` con `res.fieldErrors`):** Mapear cada clave del diccionario `res.fieldErrors` directamente al formulario mediante `form.setError(campo, { type: 'server', message: mensajes[0] })` de `react-hook-form` y disparar `toast.error(res.message)`.
4. **Conflicto de Concurrencia Optimista (`HTTP 409 Conflict` por `RowVersion`):** Disparar `toast.error(res.message)` e **invalidar automáticamente la query del recurso en edición** (`queryClient.invalidateQueries(...)`) para que la vista descargue los datos frescos y el nuevo `RowVersion`.

---

## 4. MAPEO EXACTO DE LAS 19 PANTALLAS EXISTENTES (`src/app/`) CONTRA ENDPOINTS Y CONTRATOS

### 4.1 Grupo Autenticación (`src/app/(auth)/`)
1. **`/login/page.tsx`:** Consume `POST /api/auth/login` (`LoginRequestDto`). Persiste `token`, `refreshToken`, `usuarioId`, `rol`, `tenantId`, `sucursalId` y redirige según rol (`Administrador` / `SuperAdminSaaS` $\rightarrow$ `/admin/dashboard`, `Recepcionista` / `Cajero` $\rightarrow$ `/recepcion/calendario`, `Peluquero` $\rightarrow$ `/peluquero`, `Cliente` $\rightarrow$ `/mi-cuenta/turnos`).
2. **`/registro/page.tsx`:** Consume `POST /api/auth/registro` (`RegistroUsuarioDto` con `rolId: 4` fijo) y modal OTP contra `POST /api/auth/verificar-email`.
3. **`/recuperar-clave/page.tsx`:** Consume `POST /api/auth/olvide-password` y `POST /api/auth/restablecer-password`.

### 4.2 Grupo Portal Público y Autogestión Cliente (`src/app/(public)/`)
4. **`/reservar/page.tsx`:** Wizard público sin autenticación:
   * Paso 1: `GET /api/public/catalogo-servicios` y `GET /api/public/peluqueros`.
   * Paso 2: `GET /api/public/disponibilidad` (consume `horariosDisponibles` y `slots` de `DisponibilidadHorarioResponseDto`).
   * Paso 3: `POST /api/public/reservar` (`ReservaPublicaRequestDto`). Si `requiereSena === true` y retorna `checkoutPagoUrl`, redirige a la pasarela; si `requiereSena === false`, confirma el turno en pantalla.
5. **`/mi-cuenta/turnos/page.tsx`:** Consume `GET /api/turnos/cliente/{clienteId}` y permite cancelar vía `PATCH /api/turnos/{id}/estado` respetando la ventana `horasAnticipacionCancelacion`.
6. **`/mi-cuenta/perfil/page.tsx`:** Datos del cliente y `POST /api/auth/cambiar-password`.
7. **`/legal/terminos/page.tsx`** y **8. `/legal/privacidad/page.tsx`:** Páginas estáticas institucionales.

### 4.3 Grupo Operativo de Recepción y Caja (`src/app/(dashboard)/recepcion/`)
9. **`/recepcion/calendario/page.tsx`:**
   * Consume `GET /api/turnos` pintando cada tarjeta con `turno.colorAgendaHex`.
   * Modales conectados a `POST /api/turnos`, `PUT /api/turnos/{id}/reprogramar`, `PATCH /api/turnos/{id}/estado` y `POST /api/bloqueos` (mostrando `warnings` si pisa turnos).
   * Acción **"Facturar en POS"** navega a `/recepcion/pos?turnoId={turno.id}`.
10. **`/recepcion/pos/page.tsx` [REGLA CRÍTICA DE IDEMPOTENCIA Y SEÑA]:**
    * **Ciclo de Vida de `idempotencyKey`:** Mantiene un estado `idempotencyKey` inicializado con `crypto.randomUUID()`. Si `POST /api/ventas` falla por red o timeout (`500` / network error), el reintento envía **la misma clave**. En cuanto `POST /api/ventas` responde `200 OK` (al vaciar el carrito para una nueva venta) o si el cajero modifica ítems/montos tras un `400 Bad Request`, **se regenera obligatoriamente un nuevo `crypto.randomUUID()`**.
    * **Precarga por `?turnoId={id}`:** Obtiene `GET /api/turnos/{id}`, carga los ítems desde `turno.serviciosDetalle` respetando su `precioHistorico`, asigna `clienteId` y `peluqueroId`, y si `turno.senaPagada === true`, renderiza el descuento **`"Seña Online Abonada: -$X"`** (`turno.montoSena`) exigiendo en los medios de pago únicamente el `turno.saldoPendienteCobro`.
11. **`/recepcion/caja/page.tsx` [ALINEACIÓN ESTRICTA CON `SesionCajaResponseDto`]:**
    * Consume `GET /api/caja/estado-actual` (con su lista `movimientos`) y `GET /api/caja/historial` (paginado, donde `movimientos` viene vacío para evitar *over-fetching*, consultando el detalle puntual con `GET /api/caja/{id}`).
    * Renderiza de forma independiente:
      * **Efectivo Físico Esperado en Cajón:** lee estrictamente **`caja.montoFinalEsperado`** (calculado sobre `totalVentasEfectivo` donde `esEfectivoFisico === true`).
      * **Recaudación Digital Acumulada:** lee estrictamente **`caja.totalVentasDigital`**.
    * Conecta `POST /api/caja/abrir`, `POST /api/caja/ingreso`, `POST /api/caja/egreso` y `POST /api/caja/cerrar` (enviando `montoFinalReal` y `observacionesCierre`).
12. **`/recepcion/clientes/page.tsx`:**
    * Consume `GET /api/clientes` (`PagedResult<ClienteResponseDto>`), `POST/PUT/DELETE /api/clientes` y el Drawer de **Fichas Técnicas** (`GET /api/fichastecnicas/cliente/{id}`, `POST /api/fichastecnicas`, `PUT /api/fichastecnicas/{id}`) incluyendo `formulaAplicada`, `observaciones`, `notasSensibilidadAlergias`, `productosUtilizados` y `fotoResultadoUrl` (subida vía `src/lib/cloudinary.ts`).

### 4.4 Grupo Profesional (`src/app/(dashboard)/peluquero/`)
13. **`/peluquero/page.tsx`:** Agenda diaria del profesional, consulta/carga de Fichas Técnicas y botón de descarga de insumos conectado a `POST /api/recetas-servicio/turno/{turnoId}/consumir-gabinete` (renderizando alertas `warnings` si un insumo quedó en stock `0`).

### 4.5 Grupo Administración, Gerencia y SaaS (`src/app/(dashboard)/admin/` y `saas-admin/`)
14. **`/admin/dashboard/page.tsx`:** Consume `GET /api/dashboard/kpis` y alimenta gráficos de `recharts` (`ingresosPorMedioPago` y `rankingPeluqueros`).
15. **`/admin/inventario/page.tsx`:** Pestañas conectadas a `/api/productos` (`ActualizarProductoDto` en `PUT`), `/api/productos/{id}/kardex` (mostrando `saldoResultante`), `/api/productos/movimiento-stock`, `/api/proveedores` (`ActualizarProveedorDto` en `PUT`, incluyendo `cuit`) y `/api/ordenes-compra` (alta y recepción).
16. **`/admin/servicios-recetas/page.tsx`:** Consume `/api/servicios` (`ActualizarServicioDto` en `PUT`), `/api/catalogos/categorias-servicio` y `/api/recetas-servicio` (`GET servicio/{servicioId}` y `PUT`).
17. **`/admin/staff-liquidaciones/page.tsx`:** Consume `/api/peluqueros` (`ActualizarPeluqueroDto` en `PUT`), `/api/peluqueros/{id}/horarios` (enviando `horaInicioDescanso`, `horaFinDescanso` y `activo`), `/api/comisiones` y `/api/liquidaciones` (`generar`, `pagar` y `POST /api/liquidaciones/{id}/anular`).
18. **`/admin/configuracion-seguridad/page.tsx`:** Formulario completo conectado a `GET/PUT /api/configuracion-salon` (`ConfiguracionSalonDto`), CRUD de los 7 catálogos en `/api/catalogos/{tipo}` (`ActualizarCatalogoItemDto` con `esEfectivoFisico` en medios de pago) y `/api/usuarios` (incluyendo `GET /api/usuarios/historial-sesiones` paginado).
19. **`/admin/diagnostico-api/page.tsx`** y **`/saas-admin/page.tsx`:** Diagnóstico de salud de la API y administración Multi-Tenant conectada a `/api/sucursales` y `/api/suscripciones` (`ISaaSService`).
 