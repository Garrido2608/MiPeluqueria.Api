// filepath: e2e/critical-journeys.spec.ts
import { execFileSync } from 'node:child_process';
import fs from 'node:fs';
import path from 'node:path';
import { expect, Locator, Page, test } from '@playwright/test';

interface ApiTurnosCreateResponse {
  success: boolean;
  message: string;
  data?: {
    id: number;
    montoTotalEstimado: number;
    saldoPendienteCobro?: number;
  } | null;
}

interface ApiReservaPublicaResponse {
  success: boolean;
  message: string;
  data?: {
    turnoId: number;
    clienteId: number;
    estadoTurno: string;
    montoTotalEstimado: number;
    requiereSena: boolean;
    montoSena?: number | null;
    checkoutPagoUrl?: string | null;
  } | null;
}

interface ApiVentaCreateResponse {
  success: boolean;
  message: string;
  data?: {
    id: number;
    total: number;
    subtotal: number;
    descuentoSenaAplicado: number;
    idempotencyKey?: string | null;
  } | null;
}

interface ApiSesionCajaResponse {
  success: boolean;
  data?: {
    id: number;
    montoFinalEsperado: number;
    totalVentasDigital: number;
  } | null;
}

interface ApiLiquidacionCreateResponse {
  success: boolean;
  message: string;
  data?: {
    id: number;
    totalPagar: number;
    estado: number;
  } | null;
}

interface ApiOrdenCompraCreateResponse {
  success: boolean;
  message: string;
  data?: {
    id: number;
    total: number;
  } | null;
}

function resolveSqlServerTarget(): { server: string; database: string } {
  const repoRoot = path.resolve(__dirname, '..', '..');
  const devSettingsPath = path.join(
    repoRoot,
    'MiPeluqueria.Api',
    'appsettings.Development.json'
  );
  const envConn = process.env.ConnectionStrings__DefaultConnection || '';

  let connString = envConn;
  if (!connString && fs.existsSync(devSettingsPath)) {
    try {
      const parsed = JSON.parse(fs.readFileSync(devSettingsPath, 'utf8')) as {
        ConnectionStrings?: { DefaultConnection?: string };
      };
      connString = parsed.ConnectionStrings?.DefaultConnection || '';
    } catch {
      connString = '';
    }
  }

  const serverMatch = connString.match(/Server=([^;]+)/i);
  const dbMatch = connString.match(/Database=([^;]+)/i);

  const rawServer = serverMatch ? serverMatch[1].trim() : 'localhost';
  const normalizedServer =
    rawServer.toLowerCase() === 'localhost' ? 'lpc:localhost' : rawServer;

  return {
    server: normalizedServer,
    database: dbMatch ? dbMatch[1].trim() : 'MiPeluqueriaDB',
  };
}

function runE2EDatabaseCleanup(): void {
  const cleanSqlPath = path.resolve(
    __dirname,
    '..',
    '..',
    'scripts',
    'clean-e2e-data.sql'
  );
  if (!fs.existsSync(cleanSqlPath)) {
    return;
  }
  const { server, database } = resolveSqlServerTarget();
  execFileSync(
    'sqlcmd',
    [
      '-S',
      server,
      '-d',
      database,
      '-E',
      '-C',
      '-I',
      '-f',
      '65001',
      '-l',
      '30',
      '-b',
      '-i',
      cleanSqlPath,
    ],
    { stdio: 'pipe' }
  );
}

function inputByLabel(scope: Page | Locator, labelSubstring: string): Locator {
  return scope
    .locator('label', { hasText: labelSubstring })
    .first()
    .locator('xpath=..')
    .locator('input')
    .first();
}

function parseFormattedMoney(raw: string): number {
  const cleaned = raw.replace(/[^\d.,-]/g, '').trim();
  if (!cleaned) return 0;

  const lastComma = cleaned.lastIndexOf(',');
  const lastDot = cleaned.lastIndexOf('.');

  if (lastComma > lastDot) {
    const normalized = cleaned.replace(/\./g, '').replace(',', '.');
    return Number(normalized) || 0;
  }

  if (lastDot > lastComma && lastComma !== -1) {
    const normalized = cleaned.replace(/,/g, '');
    return Number(normalized) || 0;
  }

  if (lastDot !== -1) {
    const parts = cleaned.split('.');
    if (parts.length > 2 || (parts.length === 2 && parts[1].length === 3)) {
      return Number(cleaned.replace(/\./g, '')) || 0;
    }
  }

  return Number(cleaned.replace(',', '.')) || 0;
}

function getFutureWorkingWeekdays(count = 6): string[] {
  const dates: string[] = [];
  const cursor = new Date();
  while (dates.length < count) {
    cursor.setDate(cursor.getDate() + 1);
    if (cursor.getDay() !== 0) {
      const yyyy = cursor.getFullYear();
      const mm = String(cursor.getMonth() + 1).padStart(2, '0');
      const dd = String(cursor.getDate()).padStart(2, '0');
      dates.push(`${yyyy}-${mm}-${dd}`);
    }
  }
  return dates;
}

async function dismissCookieBannerIfVisible(page: Page): Promise<void> {
  const btn = page.getByRole('button', { name: /Solo esenciales/i });
  if (await btn.isVisible()) {
    await btn.click();
  }
}

function isIgnorableConsoleError(text: string): boolean {
  const normalized = text.toLowerCase();
  return (
    normalized.includes('favicon.ico') ||
    normalized.includes('download the react devtools')
  );
}

function attachJourneyGuard(page: Page) {
  const unexpectedErrors: string[] = [];

  page.on('pageerror', (err) => {
    unexpectedErrors.push(`[pageerror] ${err.message}`);
  });

  page.on('console', (msg) => {
    if (msg.type() !== 'error') return;
    const text = msg.text();
    const locUrl = msg.location()?.url || '';
    if (isIgnorableConsoleError(text) || isIgnorableConsoleError(locUrl)) return;
    unexpectedErrors.push(`[console.error @ ${locUrl || page.url()}] ${text}`);
  });

  page.on('response', (res) => {
    if (res.status() >= 500) {
      unexpectedErrors.push(
        `[HTTP ${res.status()}] ${res.request().method()} ${res.url()}`
      );
    }
  });

  return {
    assertZeroServerOrPageErrors() {
      expect(unexpectedErrors).toEqual([]);
    },
  };
}

// Limpieza automática al finalizar todos los flujos E2E para mantener MiPeluqueriaDB impecable
test.afterAll(() => {
  runE2EDatabaseCleanup();
});

test.describe('Critical Business Journeys E2E — Sesiones Autenticadas (Cercados)', () => {
  test('Journey 1: Alta de Proveedor con persistencia post-reload + Cliente CRM 360° y Ficha Técnica Capilar', async ({
    page,
  }) => {
    const guard = attachJourneyGuard(page);

    const uniqueSeed = String(Date.now()).slice(-7);
    const razonSocialUnica = `Distribuidora Capilar E2E ${uniqueSeed}`;
    const cuitUnico = `30-${uniqueSeed}8-9`;
    const clienteApellido = `ApellidoE2E${uniqueSeed}`;
    const clienteNombre = 'Valentina';
    const clienteTelefono = `+549351${uniqueSeed}`;
    const clienteEmail = `valentina.${uniqueSeed}@cliente-e2e.test`;
    const formulaUnica = `Pomo 8.1 (45g) + Oxidante 20v — Lote E2E ${uniqueSeed}`;
    const alergiaUnica = `Sensibilidad leve a PPD verificada en test E2E ${uniqueSeed}`;

    // 1. Navegar a /admin/inventario -> Pestaña Proveedores -> Crear Proveedor -> Reload -> Visible
    const initialProveedoresPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/proveedores') &&
        res.request().method() === 'GET'
    );
    await page.goto('/admin/inventario', { waitUntil: 'load' });
    const initialProvRes = await initialProveedoresPromise;
    expect(initialProvRes.status()).toBe(200);

    await expect(
      page.getByRole('heading', {
        level: 1,
        name: /^Inventario y Compras$/i,
      })
    ).toBeVisible();

    await page.getByRole('tab', { name: /Proveedores/i }).click();
    const nuevoProveedorBtn = page.getByRole('button', {
      name: /Nuevo Proveedor/i,
    });
    await expect(nuevoProveedorBtn).toBeVisible();
    await nuevoProveedorBtn.click();

    const modalProv = page.getByRole('dialog', { name: /^Nuevo Proveedor$/i });
    await expect(modalProv).toBeVisible();

    await inputByLabel(modalProv, 'Razón Social *').fill(razonSocialUnica);
    await inputByLabel(modalProv, 'CUIT').fill(cuitUnico);
    await inputByLabel(modalProv, 'Persona de contacto').fill('Laura Gómez');
    await inputByLabel(modalProv, 'Teléfono / WhatsApp').fill(
      `351-${uniqueSeed}`
    );
    await inputByLabel(modalProv, 'Email').fill(
      `ventas.${uniqueSeed}@proveedor-e2e.test`
    );

    const createProvPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/proveedores') &&
        res.request().method() === 'POST'
    );
    await modalProv.getByRole('button', { name: /Guardar Proveedor/i }).click();

    const createProvRes = await createProvPromise;
    expect([200, 201]).toContain(createProvRes.status());
    await expect(modalProv).toBeHidden();

    const buscarProvInput = page.getByPlaceholder(
      /Buscar por razón social, CUIT o contacto/i
    );
    await buscarProvInput.fill(uniqueSeed);
    await expect(page.getByText(razonSocialUnica)).toBeVisible();
    await expect(page.getByText(`CUIT: ${cuitUnico}`)).toBeVisible();

    // Recargar la página y verificar que el nuevo Proveedor persiste desde SQL Server
    const reloadProveedoresPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/proveedores') &&
        res.request().method() === 'GET'
    );
    await page.reload({ waitUntil: 'load' });
    const reloadProvRes = await reloadProveedoresPromise;
    expect(reloadProvRes.status()).toBe(200);

    await page.getByRole('tab', { name: /Proveedores/i }).click();
    await page
      .getByPlaceholder(/Buscar por razón social, CUIT o contacto/i)
      .fill(uniqueSeed);
    await expect(page.getByText(razonSocialUnica)).toBeVisible();
    await expect(page.getByText(`CUIT: ${cuitUnico}`)).toBeVisible();

    // 2. Navegar a /recepcion/clientes -> Crear Cliente -> Guardar Ficha Técnica -> Verificar Persistencia
    const initialClientesPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/clientes') && res.request().method() === 'GET'
    );
    await page.goto('/recepcion/clientes', { waitUntil: 'load' });
    const initialCliRes = await initialClientesPromise;
    expect(initialCliRes.status()).toBe(200);

    await expect(
      page.getByRole('heading', { level: 1, name: /^Clientes$/i })
    ).toBeVisible();

    await page.getByRole('button', { name: /Nuevo Cliente/i }).click();

    const modalCli = page.getByRole('dialog', {
      name: /Registrar nuevo cliente/i,
    });
    await expect(modalCli).toBeVisible();

    await inputByLabel(modalCli, 'Nombre *').fill(clienteNombre);
    await inputByLabel(modalCli, 'Apellido *').fill(clienteApellido);
    await inputByLabel(modalCli, 'WhatsApp / Teléfono *').fill(clienteTelefono);
    await inputByLabel(modalCli, 'Correo electrónico').fill(clienteEmail);

    const createCliPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/clientes') &&
        res.request().method() === 'POST'
    );
    await modalCli.getByRole('button', { name: /Guardar cliente/i }).click();

    const createCliRes = await createCliPromise;
    expect([200, 201]).toContain(createCliRes.status());
    await expect(modalCli).toBeHidden();

    const searchInput = page.getByPlaceholder(
      /Buscar por nombre, apellido, teléfono o email/i
    );
    await searchInput.fill(clienteApellido);
    await expect(
      page.getByText(`${clienteNombre} ${clienteApellido}`)
    ).toBeVisible();

    const clienteRow = page
      .locator('tbody tr')
      .filter({ hasText: clienteApellido })
      .first();
    await clienteRow
      .getByRole('button', { name: /Fichas Técnicas/i })
      .click();

    const modalFichas = page.getByRole('dialog', {
      name: /Fichas técnicas —/i,
    });
    await expect(modalFichas).toBeVisible();

    await modalFichas.locator('#ficha-formula').fill(formulaUnica);
    await modalFichas.locator('#ficha-alergias').fill(alergiaUnica);
    await inputByLabel(modalFichas, 'Productos utilizados').fill(
      'Ampolla Nutrición + Protector Térmico'
    );
    await inputByLabel(modalFichas, 'Observaciones generales').fill(
      'Aplicación uniforme en raíces y medios.'
    );

    const createFichaPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/fichastecnicas') &&
        res.request().method() === 'POST'
    );
    await modalFichas
      .getByRole('button', { name: /Registrar ficha técnica/i })
      .click();

    const createFichaRes = await createFichaPromise;
    expect([200, 201]).toContain(createFichaRes.status());

    await expect(modalFichas.getByText(formulaUnica)).toBeVisible();
    await expect(modalFichas.getByText(alergiaUnica).first()).toBeVisible();
    await expect(
      modalFichas.getByText(/Alerta de sensibilidad o alergias registradas/i)
    ).toBeVisible();

    // Cerrar modal, recargar página y reabrir para confirmar persistencia en SQL Server
    await modalFichas.getByRole('button', { name: /Cerrar ventana/i }).click();
    const reloadClientesPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/clientes') && res.request().method() === 'GET'
    );
    await page.reload({ waitUntil: 'load' });
    const reloadCliRes = await reloadClientesPromise;
    expect(reloadCliRes.status()).toBe(200);

    await searchInput.fill(clienteApellido);

    const clienteRowAfterReload = page
      .locator('tbody tr')
      .filter({ hasText: clienteApellido })
      .first();
    await expect(clienteRowAfterReload).toBeVisible();
    await clienteRowAfterReload
      .getByRole('button', { name: /Fichas Técnicas/i })
      .click();

    await expect(page.getByText(formulaUnica)).toBeVisible();
    await expect(page.getByText(alergiaUnica).first()).toBeVisible();

    guard.assertZeroServerOrPageErrors();
  });

  test('Journey 3: Flujo Completo del Dinero — Caja ($10.000) -> Turno -> POS Idempotente Digital -> Arqueo Físico Intacto + Historial Consistente -> Dashboard BI', async ({
    page,
  }) => {
    const guard = attachJourneyGuard(page);

    // -------------------------------------------------------------------------
    // a) Abrir Caja en /recepcion/caja con monto inicial exacto de $10.000
    // -------------------------------------------------------------------------
    const estadoCajaInitPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/caja/estado-actual') &&
        res.request().method() === 'GET'
    );
    await page.goto('/recepcion/caja', { waitUntil: 'load' });
    await estadoCajaInitPromise;

    await expect(
      page.getByRole('heading', { level: 1, name: /^Caja Diaria$/i })
    ).toBeVisible();

    const botonCerrarExistente = page.getByRole('button', {
      name: /Ejecutar arqueo y cerrar caja/i,
    });
    if (await botonCerrarExistente.isVisible()) {
      await page.locator('#caja-monto-contado').fill('10000');
      await inputByLabel(page, 'Observaciones de cierre').fill(
        'Cierre previo E2E para iniciar Journey 3 con $10.000 exactos'
      );
      const cierrePrevioPromise = page.waitForResponse(
        (res) =>
          res.url().includes('/api/caja/cerrar') &&
          res.request().method() === 'POST'
      );
      await botonCerrarExistente.click();
      const cierreRes = await cierrePrevioPromise;
      expect(cierreRes.status()).toBe(200);

      const modalActa = page.getByRole('dialog', {
        name: /Vista previa del comprobante/i,
      });
      if (await modalActa.isVisible()) {
        await modalActa.getByRole('button', { name: /^Cerrar$/i }).click();
      }
    }

    await expect(page.getByText('Caja cerrada', { exact: true })).toBeVisible();
    await page.locator('#caja-fondo-inicial').fill('10000');

    const abrirCajaPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/caja/abrir') &&
        res.request().method() === 'POST'
    );
    await page.getByRole('button', { name: /Abrir sesión de caja/i }).click();
    const abrirCajaRes = await abrirCajaPromise;
    expect([200, 201]).toContain(abrirCajaRes.status());

    const abrirCajaJson = (await abrirCajaRes.json()) as ApiSesionCajaResponse;
    const sesionCajaAbiertaId = abrirCajaJson.data?.id ?? 0;
    expect(sesionCajaAbiertaId).toBeGreaterThan(0);

    await expect(
      page.getByText(new RegExp(`Caja del \\d{2}/\\d{2}/\\d{4} \\(#${sesionCajaAbiertaId}\\)`))
    ).toBeVisible();

    const cardEfectivoInicial = page
      .locator('div.rounded-xl')
      .filter({ hasText: 'Efectivo en Cajón' })
      .first();
    const efectivoInicialTexto = await cardEfectivoInicial
      .locator('p.font-mono-tabular')
      .first()
      .innerText();
    expect(parseFormattedMoney(efectivoInicialTexto)).toBe(10000);

    // -------------------------------------------------------------------------
    // b) Agendar un Turno en /recepcion/calendario usando SearchCombobox de Clientes
    // -------------------------------------------------------------------------
    await page.goto('/recepcion/calendario', { waitUntil: 'load' });
    await expect(
      page.getByRole('heading', { level: 1, name: /^Calendario$/i })
    ).toBeVisible();

    await page.getByRole('button', { name: /^Nuevo Turno$/i }).click();
    const modalTurno = page.getByRole('dialog', { name: /^Nuevo Turno$/i });
    await expect(modalTurno).toBeVisible();

    // Seleccionar cliente existente mediante SearchCombobox
    const clienteCombobox = modalTurno.locator('#calendario-cliente-combobox');
    await clienteCombobox.click();
    const primeraOpcionCliente = modalTurno.getByRole('option').first();
    await expect(primeraOpcionCliente).toBeVisible();
    await primeraOpcionCliente.click();

    await modalTurno
      .locator('#calendario-nuevo-observaciones')
      .fill('Turno E2E Journey 3');

    const candidateDates = getFutureWorkingWeekdays(6);
    let turnoCreadoId = 0;

    // Marcar el primer servicio activo una sola vez
    const firstServiceCheckbox = modalTurno
      .locator('input[type="checkbox"]')
      .first();

    for (const targetDate of candidateDates) {
      const dispTurnoPromise = page.waitForResponse(
        (res) =>
          res.url().includes('/api/turnos/disponibilidad') &&
          res.request().method() === 'GET'
      );

      await modalTurno.locator('#calendario-nuevo-fecha').fill(targetDate);
      if (!(await firstServiceCheckbox.isChecked())) {
        await firstServiceCheckbox.check();
      }
      await dispTurnoPromise;

      const selectHorario = modalTurno.locator(
        'select#calendario-nuevo-horario'
      );
      if ((await selectHorario.count()) > 0) {
        const crearTurnoPromise = page.waitForResponse(
          (res) =>
            res.url().endsWith('/api/turnos') &&
            res.request().method() === 'POST'
        );
        await modalTurno
          .getByRole('button', { name: /^Confirmar Turno$/i })
          .click();
        const crearTurnoRes = await crearTurnoPromise;
        expect([200, 201]).toContain(crearTurnoRes.status());

        const turnoJson =
          (await crearTurnoRes.json()) as ApiTurnosCreateResponse;
        expect(turnoJson.success).toBe(true);
        turnoCreadoId = turnoJson.data?.id ?? 0;
        break;
      }
    }

    expect(turnoCreadoId).toBeGreaterThan(0);

    // -------------------------------------------------------------------------
    // c) Navegar a /recepcion/pos?turnoId={id}, verificar precarga y cobrar digital
    // -------------------------------------------------------------------------
    const precargaTurnoPromise = page.waitForResponse(
      (res) =>
        res.url().includes(`/api/turnos/${turnoCreadoId}`) &&
        res.request().method() === 'GET'
    );
    await page.goto(`/recepcion/pos?turnoId=${turnoCreadoId}`, {
      waitUntil: 'load',
    });
    const precargaRes = await precargaTurnoPromise;
    expect(precargaRes.status()).toBe(200);

    await expect(
      page.getByText(new RegExp(`Turno #${turnoCreadoId}`))
    ).toBeVisible();

    const selectMedioPago = page.locator('#pos-medio-pago');
    await expect(selectMedioPago).toBeVisible();
    const opcionesMedio = await selectMedioPago
      .locator('option')
      .allTextContents();
    const indiceDigital = opcionesMedio.findIndex((txt) =>
      /Mercado|Transferencia|Débito|Crédito|QR|Tarjeta/i.test(txt)
    );
    expect(
      indiceDigital,
      `Debe existir al menos un medio de pago digital en ${opcionesMedio.join(', ')}`
    ).toBeGreaterThanOrEqual(0);

    const valorDigital = await selectMedioPago
      .locator('option')
      .nth(indiceDigital)
      .getAttribute('value');
    expect(valorDigital).toBeTruthy();
    await selectMedioPago.selectOption(valorDigital!);

    await inputByLabel(page, 'Nro. de cupón o referencia').fill(
      `MP-POS-E2E-${turnoCreadoId}`
    );

    await page.getByRole('button', { name: /\+ Sumar/i }).click();

    const registrarVentaPromise = page.waitForResponse(
      (res) =>
        res.url().endsWith('/api/ventas') && res.request().method() === 'POST'
    );
    await page
      .getByRole('button', { name: /Confirmar y emitir ticket/i })
      .click();

    const ventaRes = await registrarVentaPromise;
    expect([200, 201]).toContain(ventaRes.status());

    // Verificar que la petición envió header X-Idempotency-Key bajo el capó
    const sentHeaders = ventaRes.request().headers();
    expect(sentHeaders['x-idempotency-key']).toBeTruthy();

    const ventaJson = (await ventaRes.json()) as ApiVentaCreateResponse;
    expect(ventaJson.success).toBe(true);
    const totalCobradoDigital = ventaJson.data?.total ?? 0;
    expect(totalCobradoDigital).toBeGreaterThan(0);

    // Verificar modal de comprobante en 1 sola hoja (#mipeluqueria-print-portal)
    const modalTicket = page.getByRole('dialog', {
      name: /Vista previa del comprobante/i,
    });
    await expect(modalTicket).toBeVisible();
    await expect(modalTicket.getByText(/Ticket de Venta/i)).toBeVisible();
    await expect(page.locator('#mipeluqueria-print-portal')).toHaveCount(1);
    await modalTicket.getByRole('button', { name: /^Cerrar$/i }).click();

    // -------------------------------------------------------------------------
    // d) Verificar en /recepcion/caja que montoFinalEsperado === $10.000 y Historial coincide al centavo
    // -------------------------------------------------------------------------
    const refreshCajaPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/caja/estado-actual') &&
        res.request().method() === 'GET'
    );
    await page.goto('/recepcion/caja', { waitUntil: 'load' });
    await refreshCajaPromise;

    const cardEfectivoFinal = page
      .locator('div.rounded-xl')
      .filter({ hasText: 'Efectivo en Cajón' })
      .first();
    const cardDigitalFinal = page
      .locator('div.rounded-xl')
      .filter({ hasText: 'Recaudación Digital' })
      .first();

    const montoEfectivoEsperadoPostVenta = parseFormattedMoney(
      await cardEfectivoFinal.locator('p.font-mono-tabular').first().innerText()
    );
    const montoDigitalAcumuladoPostVenta = parseFormattedMoney(
      await cardDigitalFinal.locator('p.font-mono-tabular').first().innerText()
    );

    expect(montoEfectivoEsperadoPostVenta).toBe(10000);
    expect(montoDigitalAcumuladoPostVenta).toBe(totalCobradoDigital);

    // Validar consistencia en la fila de la sesión abierta dentro del Historial (Fix Tarea 8.2)
    const filaSesionAbierta = page
      .locator('tbody tr')
      .filter({ hasText: `Sesión #${sesionCajaAbiertaId}` })
      .first();
    await expect(filaSesionAbierta).toBeVisible();
    const celdasFila = filaSesionAbierta.locator('td');
    expect(parseFormattedMoney(await celdasFila.nth(3).innerText())).toBe(
      10000
    );
    expect(parseFormattedMoney(await celdasFila.nth(5).innerText())).toBe(
      totalCobradoDigital
    );

    // -------------------------------------------------------------------------
    // e) Ir a /admin/dashboard y verificar KPIs ejecutivos y gráficos Recharts sin errores
    // -------------------------------------------------------------------------
    const dashboardKpisPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/dashboard/kpis') &&
        res.request().method() === 'GET'
    );
    await page.goto('/admin/dashboard', { waitUntil: 'load' });
    const kpisRes = await dashboardKpisPromise;
    expect(kpisRes.status()).toBe(200);

    await expect(
      page.getByRole('heading', { level: 1, name: /^Dashboard Ejecutivo$/i })
    ).toBeVisible();
    await expect(page.getByText('Facturación Total')).toBeVisible();
    await expect(page.getByText('Ticket Promedio')).toBeVisible();
    await expect(page.getByText('Turnos Atendidos')).toBeVisible();
    await expect(page.getByText('Ingresos por Medio de Pago')).toBeVisible();
    await expect(page.getByText('Ranking por Estilista')).toBeVisible();

    const facturacionCard = page
      .locator('article')
      .filter({ hasText: 'Facturación Total' })
      .first();
    const facturacionMonto = parseFormattedMoney(
      await facturacionCard.locator('p.font-mono-tabular').first().innerText()
    );
    expect(facturacionMonto).toBeGreaterThanOrEqual(totalCobradoDigital);

    await expect(page.locator('.recharts-wrapper').first()).toBeVisible();

    guard.assertZeroServerOrPageErrors();
  });

  test('Journey 4: Liquidación de Haberes + Recibo PDF (sin error de milisegundos) + Pago + Auto-OC con Faltantes y PDF', async ({
    page,
  }) => {
    const guard = attachJourneyGuard(page);
    const uniqueSeed = String(Date.now()).slice(-7);

    // -------------------------------------------------------------------------
    // a) Emitir Liquidación, abrir Recibo PDF y ejecutar Pagar en /admin/staff-liquidaciones
    // -------------------------------------------------------------------------
    await page.goto('/admin/staff-liquidaciones', { waitUntil: 'load' });
    await expect(
      page.getByRole('heading', {
        level: 1,
        name: /^Staff, Comisiones y Liquidaciones$/i,
      })
    ).toBeVisible();

    // Verificar nombre real del profesional en pestaña Comisiones (Fix Tarea 8.3 / 8.8)
    await page.getByRole('tab', { name: /Comisiones/i }).click();
    await expect(
      page.locator('tbody').getByText('Carlos Gómez').first()
    ).toBeVisible();

    // Emitir liquidación sobre la venta generada en Journey 3
    await page.getByRole('tab', { name: /Liquidaciones/i }).click();

    const generarLiqPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/liquidaciones') &&
        res.request().method() === 'POST'
    );
    await page
      .getByRole('button', { name: /Calcular y Emitir Liquidación/i })
      .click();

    const generarLiqRes = await generarLiqPromise;
    expect([200, 201]).toContain(generarLiqRes.status());
    const liqJson =
      (await generarLiqRes.json()) as ApiLiquidacionCreateResponse;
    expect(liqJson.success).toBe(true);
    const liquidacionId = liqJson.data?.id ?? 0;
    expect(liquidacionId).toBeGreaterThan(0);

    const filaLiq = page
      .locator('tbody tr')
      .filter({ hasText: `#${liquidacionId}` })
      .first();
    await expect(filaLiq).toBeVisible();
    await expect(filaLiq.getByText(/Pendiente/i)).toBeVisible();

    // Abrir Recibo PDF y validar que no falle por fracciones de milisegundos de SQL Server
    const detalleLiqPromise = page.waitForResponse(
      (res) =>
        res.url().includes(`/api/liquidaciones/${liquidacionId}`) &&
        res.request().method() === 'GET'
    );
    await filaLiq.getByRole('button', { name: /Recibo PDF/i }).click();
    const detalleLiqRes = await detalleLiqPromise;
    expect(detalleLiqRes.status()).toBe(200);

    const modalRecibo = page.getByRole('dialog', {
      name: new RegExp(`Vista previa del comprobante #LIQ-${liquidacionId}`, 'i'),
    });
    await expect(modalRecibo).toBeVisible();
    await expect(
      modalRecibo.getByText(/RECIBO DE LIQUIDACIÓN DE HABERES/i)
    ).toBeVisible();
    await expect(modalRecibo.getByText(/Invalid Date/i)).toHaveCount(0);
    await modalRecibo.getByRole('button', { name: /^Cerrar$/i }).click();

    // Ejecutar Pagar mediante ConfirmModal
    await filaLiq.getByRole('button', { name: /^Pagar$/i }).click();
    const modalPagar = page.getByRole('dialog', {
      name: new RegExp(`Confirmar pago — Liquidación #${liquidacionId}`, 'i'),
    });
    await expect(modalPagar).toBeVisible();

    const pagarLiqPromise = page.waitForResponse(
      (res) =>
        res.url().includes(`/api/liquidaciones/${liquidacionId}/pagar`) &&
        res.request().method() === 'POST'
    );
    await modalPagar.getByRole('button', { name: /Confirmar pago/i }).click();
    const pagarLiqRes = await pagarLiqPromise;
    expect(pagarLiqRes.status()).toBe(200);
    await expect(modalPagar).toBeHidden();
    await expect(filaLiq.getByText(/Pagad/i)).toBeVisible();

    // -------------------------------------------------------------------------
    // b) Generar Orden de Compra automática desde Stock Crítico y abrir PDF
    // -------------------------------------------------------------------------
    await page.goto('/admin/inventario', { waitUntil: 'load' });
    await expect(
      page.getByRole('heading', { level: 1, name: /^Inventario y Compras$/i })
    ).toBeVisible();

    // Crear un insumo sintético en stock crítico (1 <= 5) para garantizar disponibilidad determinística
    const nombreInsumoCritico = `Ampolla Capilar E2E ${uniqueSeed}`;
    await page.getByRole('button', { name: /Nuevo Producto/i }).click();
    const modalNuevoProd = page.getByRole('dialog', {
      name: /^Nuevo Producto$/i,
    });
    await expect(modalNuevoProd).toBeVisible();

    await inputByLabel(modalNuevoProd, 'Nombre del artículo *').fill(
      nombreInsumoCritico
    );
    await inputByLabel(modalNuevoProd, 'Código de barras / SKU').fill(
      `SKU-E2E-${uniqueSeed}`
    );
    await inputByLabel(modalNuevoProd, 'Precio de costo *').fill('3500');
    await inputByLabel(modalNuevoProd, 'Precio de venta *').fill('7000');
    await inputByLabel(modalNuevoProd, 'Stock inicial').fill('1');
    await inputByLabel(modalNuevoProd, 'Stock mínimo de alerta *').fill('5');

    const crearProdPromise = page.waitForResponse(
      (res) =>
        res.url().endsWith('/api/productos') &&
        res.request().method() === 'POST'
    );
    await modalNuevoProd
      .getByRole('button', { name: /Guardar Producto/i })
      .click();
    const crearProdRes = await crearProdPromise;
    expect([200, 201]).toContain(crearProdRes.status());
    await expect(modalNuevoProd).toBeHidden();

    // Ir a Órdenes de Compra -> Generar OC con faltantes
    await page.getByRole('tab', { name: /Órdenes de Compra/i }).click();
    const stockCriticoPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/productos/stock-critico') &&
        res.request().method() === 'GET'
    );
    await page
      .getByRole('button', { name: /Generar OC con faltantes/i })
      .click();
    const stockCriticoRes = await stockCriticoPromise;
    expect(stockCriticoRes.status()).toBe(200);

    // Si hay múltiples proveedores con faltantes, elegir el grupo que contiene el insumo E2E
    const modalGrupos = page.getByRole('dialog', {
      name: /Faltantes agrupados por proveedor/i,
    });
    if (await modalGrupos.isVisible()) {
      const tarjetaGrupo = modalGrupos
        .locator('div.rounded-lg')
        .filter({ hasText: nombreInsumoCritico })
        .first();
      if ((await tarjetaGrupo.count()) > 0) {
        await tarjetaGrupo
          .getByRole('button', { name: /Precargar OC/i })
          .click();
      } else {
        await modalGrupos
          .getByRole('button', { name: /Precargar OC/i })
          .first()
          .click();
      }
    }

    const modalNuevaOc = page.getByRole('dialog', {
      name: /^Nueva Orden de Compra$/i,
    });
    await expect(modalNuevaOc).toBeVisible();
    await inputByLabel(modalNuevaOc, 'N° Comprobante / Cotización').fill(
      `OC-E2E-${uniqueSeed}`
    );

    const emitirOcPromise = page.waitForResponse(
      (res) =>
        res.url().endsWith('/api/ordenes-compra') &&
        res.request().method() === 'POST'
    );
    await modalNuevaOc
      .getByRole('button', { name: /Emitir Orden de Compra/i })
      .click();
    const emitirOcRes = await emitirOcPromise;
    expect([200, 201]).toContain(emitirOcRes.status());
    const ocJson = (await emitirOcRes.json()) as ApiOrdenCompraCreateResponse;
    const ocCreadaId = ocJson.data?.id ?? 0;
    expect(ocCreadaId).toBeGreaterThan(0);
    await expect(modalNuevaOc).toBeHidden();

    // Abrir vista previa PDF de la OC recién emitida
    const filaOc = page
      .locator('tbody tr')
      .filter({ hasText: `#${ocCreadaId}` })
      .first();
    await expect(filaOc).toBeVisible();
    await filaOc.getByRole('button', { name: /^PDF$/i }).click();

    const codigoOcEsperado = `OC-${String(ocCreadaId).padStart(5, '0')}`;
    const modalOcPdf = page.getByRole('dialog', {
      name: new RegExp(`Vista previa del comprobante #${codigoOcEsperado}`, 'i'),
    });
    await expect(modalOcPdf).toBeVisible();
    await expect(modalOcPdf.getByText(/Orden de Compra/i).first()).toBeVisible();
    await modalOcPdf.getByRole('button', { name: /^Cerrar$/i }).click();

    // Cerrar la sesión de caja abierta durante E2E para permitir su limpieza limpia en afterAll
    const estadoCajaCierrePromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/caja/estado-actual') &&
        res.request().method() === 'GET'
    );
    await page.goto('/recepcion/caja', { waitUntil: 'load' });
    await estadoCajaCierrePromise;

    const botonCerrarFinal = page.getByRole('button', {
      name: /Ejecutar arqueo y cerrar caja/i,
    });
    await expect(botonCerrarFinal).toBeVisible();
    await page.locator('#caja-monto-contado').fill('10000');
    await inputByLabel(page, 'Observaciones de cierre').fill(
      'Cierre automático suite E2E'
    );
    const cierreFinalPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/caja/cerrar') &&
        res.request().method() === 'POST'
    );
    await botonCerrarFinal.click();
    const cierreFinalRes = await cierreFinalPromise;
    expect(cierreFinalRes.status()).toBe(200);

    guard.assertZeroServerOrPageErrors();
  });
});

test.describe('Critical Business Journeys E2E — Portal Público Sin Sesión (Cercado)', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('Journey 2: Portal Público (/reservar) sin sesión — selección de servicio, profesional, disponibilidad y reserva confirmada', async ({
    page,
  }) => {
    const guard = attachJourneyGuard(page);

    let externalCheckoutRedirectUrl: string | null = null;
    await page.route('https://www.mercadopago.com.ar/**', async (route) => {
      externalCheckoutRedirectUrl = route.request().url();
      await route.fulfill({
        status: 200,
        contentType: 'text/html',
        body: '<html><body><h1>Checkout Pasarela Externa Simulada</h1></body></html>',
      });
    });
    await page.route('https://checkout.stripe.com/**', async (route) => {
      externalCheckoutRedirectUrl = route.request().url();
      await route.fulfill({
        status: 200,
        contentType: 'text/html',
        body: '<html><body><h1>Checkout Stripe Simulado</h1></body></html>',
      });
    });

    const serviciosPublicosPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/public/catalogo-servicios') &&
        res.request().method() === 'GET'
    );
    await page.goto('/reservar', { waitUntil: 'load' });
    const servPubRes = await serviciosPublicosPromise;
    expect(servPubRes.status()).toBe(200);

    await dismissCookieBannerIfVisible(page);

    const hasToken = await page.evaluate(() =>
      Boolean(window.localStorage.getItem('mipeluqueria_jwt_token'))
    );
    expect(hasToken).toBe(false);

    await expect(
      page.getByRole('heading', {
        name: /Elegí los servicios para tu visita/i,
      })
    ).toBeVisible();

    const primerServicioCard = page
      .locator('div.cursor-pointer')
      .filter({ hasText: '+ Agregar' })
      .first();
    await expect(primerServicioCard).toBeVisible();
    await primerServicioCard.click();
    await expect(page.getByText('✓ Seleccionado').first()).toBeVisible();

    await page.getByRole('button', { name: /Elegir Profesional/i }).click();

    await expect(
      page.getByRole('heading', {
        name: /Seleccioná tu estilista o barbero/i,
      })
    ).toBeVisible();

    const tarjetasEstilistas = page.locator('section div.cursor-pointer');
    const cantidadTarjetas = await tarjetasEstilistas.count();
    if (cantidadTarjetas > 1) {
      await tarjetasEstilistas.nth(1).click();
    } else {
      await tarjetasEstilistas.first().click();
    }

    await page
      .getByRole('button', { name: /Ver Horarios Disponibles/i })
      .click();

    await expect(
      page.getByRole('heading', { name: /Elegí fecha y horario/i })
    ).toBeVisible();

    const dateInput = inputByLabel(page, 'Fecha del turno');
    const candidateDates = getFutureWorkingWeekdays(6);
    let slotSeleccionado = false;

    for (const candidateDate of candidateDates) {
      const dispPromise = page.waitForResponse(
        (res) =>
          res.url().includes('/api/public/disponibilidad') &&
          res.url().includes(candidateDate) &&
          res.request().method() === 'GET'
      );
      await dateInput.fill(candidateDate);
      const dispRes = await dispPromise;
      expect(dispRes.status()).toBe(200);

      const slotButtons = page.getByRole('button', {
        name: /^\d{2}:\d{2} hs$/,
      });
      const slotCount = await slotButtons.count();
      if (slotCount > 0) {
        await slotButtons.nth(slotCount - 1).click();
        slotSeleccionado = true;
        break;
      }
    }

    expect(
      slotSeleccionado,
      'Debe existir al menos un horario disponible en los próximos 6 días hábiles'
    ).toBe(true);

    await page
      .getByRole('button', { name: /Continuar a Mis Datos/i })
      .click();

    const uniqueSeed = String(Date.now()).slice(-7);
    await inputByLabel(page, 'Nombre *').fill('Camila');
    await inputByLabel(page, 'Apellido *').fill(`ReservaPublica${uniqueSeed}`);
    await inputByLabel(page, 'WhatsApp / Teléfono *').fill(
      `+5493518${uniqueSeed}`
    );
    await inputByLabel(page, 'Correo Electrónico').fill(
      `camila.${uniqueSeed}@reserva-online.test`
    );

    await page.locator('form input[type="checkbox"]').last().check();

    const reservarResPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/public/reservar') &&
        res.request().method() === 'POST'
    );
    await page.getByRole('button', { name: /Confirmar Reserva/i }).click();

    const reservarRes = await reservarResPromise;
    expect([200, 201]).toContain(reservarRes.status());

    const reservarJson =
      (await reservarRes.json()) as ApiReservaPublicaResponse;
    expect(reservarJson.success).toBe(true);
    expect(reservarJson.data?.turnoId).toBeGreaterThan(0);

    if (
      reservarJson.data?.requiereSena &&
      reservarJson.data?.checkoutPagoUrl
    ) {
      await expect
        .poll(() => externalCheckoutRedirectUrl, { timeout: 10_000 })
        .toContain(`TURNO_${reservarJson.data.turnoId}`);
    } else {
      await expect(
        page.getByText(`Turno Registrado #${reservarJson.data?.turnoId}`)
      ).toBeVisible();
    }

    guard.assertZeroServerOrPageErrors();
  });
});
