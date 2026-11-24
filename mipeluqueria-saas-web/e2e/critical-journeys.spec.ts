// filepath: e2e/critical-journeys.spec.ts
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

function inputByLabel(scope: Page | Locator, labelSubstring: string): Locator {
  return scope
    .locator('label', { hasText: labelSubstring })
    .first()
    .locator('xpath=following-sibling::input[1]');
}

function parseFormattedMoney(raw: string): number {
  // Convierte "$ 10.000,00" o "$ 15.000" o "$10,000.00" a número decimal exacto
  const cleaned = raw.replace(/[^\d.,-]/g, '').trim();
  if (!cleaned) return 0;

  const lastComma = cleaned.lastIndexOf(',');
  const lastDot = cleaned.lastIndexOf('.');

  if (lastComma > lastDot) {
    // Formato es-AR: puntos de miles, coma decimal ("10.000,00")
    const normalized = cleaned.replace(/\./g, '').replace(',', '.');
    return Number(normalized) || 0;
  }

  if (lastDot > lastComma && lastComma !== -1) {
    // Formato en-US: comas de miles, punto decimal ("10,000.00")
    const normalized = cleaned.replace(/,/g, '');
    return Number(normalized) || 0;
  }

  if (lastDot !== -1) {
    // Si tiene punto con 3 dígitos después ("10.000"), es separador de miles es-AR
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
    // Evitar domingos (0) para garantizar jornada laboral activa (Lunes=1..Sábado=6)
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

function attachJourneyGuard(page: Page) {
  const unexpectedErrors: string[] = [];

  page.on('pageerror', (err) => {
    unexpectedErrors.push(`[pageerror] ${err.message}`);
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
    const alergiaUnica = `Sensibilidad leve a PPD verificada en test ${uniqueSeed}`;

    // 1. Navegar a /admin/inventario -> esperar carga inicial -> Pestaña Proveedores -> Crear Proveedor -> Reload -> Visible
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
        name: /Inventario, Kardex, Proveedores y Órdenes de Compra/i,
      })
    ).toBeVisible();

    await page.getByRole('button', { name: /2\. Proveedores/i }).click();
    const nuevoProveedorBtn = page.getByRole('button', {
      name: /\+ Nuevo Proveedor/i,
    });
    await expect(nuevoProveedorBtn).toBeVisible();
    await nuevoProveedorBtn.click();

    const modalProv = page
      .locator('div.fixed.inset-0')
      .filter({ hasText: 'Nuevo Proveedor' });
    await expect(modalProv).toBeVisible();

    await inputByLabel(modalProv, 'Razón Social Única').fill(razonSocialUnica);
    await inputByLabel(modalProv, 'CUIT Fiscal').fill(cuitUnico);
    await inputByLabel(modalProv, 'Persona de Contacto').fill('Laura Gómez');
    await inputByLabel(modalProv, 'Teléfono').fill(`351-${uniqueSeed}`);
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

    await expect(page.getByText(razonSocialUnica)).toBeVisible();
    await expect(page.getByText(`CUIT: ${cuitUnico}`)).toBeVisible();

    // Recargar la página y verificar que el nuevo Proveedor aparece de inmediato desde SQL Server
    const reloadProveedoresPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/proveedores') &&
        res.request().method() === 'GET'
    );
    await page.reload({ waitUntil: 'load' });
    const reloadProvRes = await reloadProveedoresPromise;
    expect(reloadProvRes.status()).toBe(200);

    await page.getByRole('button', { name: /2\. Proveedores/i }).click();
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

    await page.getByRole('button', { name: /\+ Nuevo Cliente/i }).click();

    const modalCli = page
      .locator('div.fixed.inset-0')
      .filter({ hasText: 'Registrar Nuevo Cliente' });
    await expect(modalCli).toBeVisible();

    await inputByLabel(modalCli, 'Nombre *').fill(clienteNombre);
    await inputByLabel(modalCli, 'Apellido *').fill(clienteApellido);
    await inputByLabel(modalCli, 'WhatsApp / Teléfono *').fill(clienteTelefono);
    await inputByLabel(modalCli, 'Correo Electrónico').fill(clienteEmail);

    const createCliPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/clientes') &&
        res.request().method() === 'POST'
    );
    await modalCli.getByRole('button', { name: /Guardar Cliente/i }).click();

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

    const modalFichas = page
      .locator('div.fixed.inset-0')
      .filter({ hasText: 'Historial de Fichas Técnicas' });
    await expect(modalFichas).toBeVisible();

    await modalFichas.getByPlaceholder(/Ej\. Pomo 7\.1/i).fill(formulaUnica);
    await modalFichas
      .getByPlaceholder(/Ej\. Sensibilidad a decolorantes/i)
      .fill(alergiaUnica);
    await inputByLabel(modalFichas, 'Productos de Gabinete Utilizados').fill(
      'Ampolla Nutrición + Protector Térmico'
    );
    await inputByLabel(modalFichas, 'Observaciones Generales').fill(
      'Aplicación uniforme en raíces y medios.'
    );

    const createFichaPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/fichastecnicas') &&
        res.request().method() === 'POST'
    );
    await modalFichas
      .getByRole('button', { name: /Registrar Ficha Técnica/i })
      .click();

    const createFichaRes = await createFichaPromise;
    expect([200, 201]).toContain(createFichaRes.status());

    await expect(modalFichas.getByText(formulaUnica)).toBeVisible();
    await expect(modalFichas.getByText(alergiaUnica)).toBeVisible();

    // Cerrar modal, recargar página y reabrir para confirmar persistencia en SQL Server
    await modalFichas.getByRole('button', { name: 'X', exact: true }).click();
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
    await expect(page.getByText(alergiaUnica)).toBeVisible();

    guard.assertZeroServerOrPageErrors();
  });

  test('Journey 3: Flujo Completo del Dinero — Caja ($10.000) -> Turno -> POS Idempotente Digital -> Arqueo Físico Intacto -> Dashboard BI', async ({
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
      page.getByRole('heading', {
        name: /Control de Caja Diaria y Arqueo Ciego/i,
      })
    ).toBeVisible();

    const botonCerrarExistente = page.getByRole('button', {
      name: /Ejecutar Arqueo y Cerrar Caja/i,
    });
    if (await botonCerrarExistente.isVisible()) {
      // Cerramos limpiamente cualquier caja residual previa para iniciar con base exacta de $10.000
      await inputByLabel(
        page,
        'Efectivo Físico Contado en Cajón (MontoFinalReal)'
      ).fill('10000');
      await inputByLabel(page, 'Observaciones de Cierre').fill(
        'Cierre previo para iniciar Journey 3 con $10.000 exactos'
      );
      const cierrePrevioPromise = page.waitForResponse(
        (res) =>
          res.url().includes('/api/caja/cerrar') &&
          res.request().method() === 'POST'
      );
      await botonCerrarExistente.click();
      const cierreRes = await cierrePrevioPromise;
      expect(cierreRes.status()).toBe(200);

      const modalActa = page
        .locator('div.fixed.inset-0')
        .filter({ hasText: 'Vista Previa de Comprobante' });
      if (await modalActa.isVisible()) {
        await modalActa.getByRole('button', { name: /Cerrar/i }).click();
      }
    }

    await expect(page.getByText('CAJA CERRADA', { exact: true })).toBeVisible();
    await inputByLabel(page, 'Fondo Fijo Inicial en Efectivo *').fill('10000');

    const abrirCajaPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/caja/abrir') &&
        res.request().method() === 'POST'
    );
    await page.getByRole('button', { name: /Abrir Sesión de Caja/i }).click();
    const abrirCajaRes = await abrirCajaPromise;
    expect([200, 201]).toContain(abrirCajaRes.status());

    await expect(page.getByText(/CAJA ABIERTA #\d+/)).toBeVisible();

    const cardEfectivoInicial = page
      .locator('div.bg-white')
      .filter({ hasText: '1. Efectivo Físico Esperado en Cajón' })
      .first();
    const efectivoInicialTexto = await cardEfectivoInicial
      .locator('p.text-2xl')
      .innerText();
    expect(parseFormattedMoney(efectivoInicialTexto)).toBe(10000);

    // -------------------------------------------------------------------------
    // b) Agendar un Turno en /recepcion/calendario para cliente y peluquero activos
    // -------------------------------------------------------------------------
    await page.goto('/recepcion/calendario', { waitUntil: 'load' });
    await expect(
      page.getByRole('heading', {
        name: /Calendario Master Multi-Columna/i,
      })
    ).toBeVisible();

    const candidateDates = getFutureWorkingWeekdays(6);
    const headerDateInput = page.locator('header input[type="date"]').first();

    let turnoCreadoId = 0;

    for (const targetDate of candidateDates) {
      const turnosReloadPromise = page.waitForResponse(
        (res) =>
          res.url().includes('/api/turnos') &&
          res.request().method() === 'GET'
      );
      await headerDateInput.fill(targetDate);
      await turnosReloadPromise;

      await page
        .getByRole('button', { name: /\+ Nuevo Turno \/ Walk-In/i })
        .click();

      const modalTurno = page
        .locator('div.fixed.inset-0')
        .filter({ hasText: 'Otorgar Nuevo Turno en Recepción' });
      await expect(modalTurno).toBeVisible();

      const dispTurnoPromise = page.waitForResponse(
        (res) =>
          res.url().includes('/api/turnos/disponibilidad') &&
          res.request().method() === 'GET'
      );
      await modalTurno.locator('input[type="checkbox"]').first().check();
      await dispTurnoPromise;

      const slotsSelect = modalTurno
        .locator('div.flex.flex-col')
        .filter({ hasText: 'Slots Libres Verificados' })
        .locator('select');

      if ((await slotsSelect.count()) > 0) {
        const crearTurnoPromise = page.waitForResponse(
          (res) =>
            res.url().endsWith('/api/turnos') &&
            res.request().method() === 'POST'
        );
        await modalTurno
          .getByRole('button', { name: /Confirmar Turno en Agenda/i })
          .click();
        const crearTurnoRes = await crearTurnoPromise;
        expect([200, 201]).toContain(crearTurnoRes.status());

        const turnoJson = (await crearTurnoRes.json()) as ApiTurnosCreateResponse;
        expect(turnoJson.success).toBe(true);
        turnoCreadoId = turnoJson.data?.id ?? 0;
        break;
      }

      await modalTurno.getByRole('button', { name: /Cancelar/i }).click();
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
      page.getByText(new RegExp(`Vinculado al Turno #${turnoCreadoId}`))
    ).toBeVisible();
    await expect(page.getByText('SERV').first()).toBeVisible();

    // Seleccionar explícitamente un medio de pago DIGITAL (EsEfectivoFisico = false)
    const seccionMultipago = page
      .locator('div.border-t')
      .filter({
        has: page.getByRole('heading', { name: 'Desglose de Medios de Pago' }),
      })
      .first();

    // Quitar el medio preseleccionado por defecto (Efectivo) para cargar 100% digital con cupón
    const quitarPagoDefaultBtn = seccionMultipago
      .locator('div.bg-slate-100 button')
      .first();
    if (await quitarPagoDefaultBtn.isVisible()) {
      await quitarPagoDefaultBtn.click();
    }

    const selectMedioPago = seccionMultipago.locator('select').first();
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

    await inputByLabel(
      page,
      'Nro. Cupón / Referencia Operación Digital'
    ).fill(`MP-POS-E2E-${turnoCreadoId}`);

    await seccionMultipago.getByRole('button', { name: /\+ Sumar/i }).click();

    const registrarVentaPromise = page.waitForResponse(
      (res) =>
        res.url().endsWith('/api/ventas') && res.request().method() === 'POST'
    );
    await page
      .getByRole('button', { name: /Confirmar y Emitir Ticket/i })
      .click();

    const ventaRes = await registrarVentaPromise;
    expect([200, 201]).toContain(ventaRes.status());

    // Verificar que la petición envió header X-Idempotency-Key
    const sentHeaders = ventaRes.request().headers();
    expect(sentHeaders['x-idempotency-key']).toBeTruthy();

    const ventaJson = (await ventaRes.json()) as ApiVentaCreateResponse;
    expect(ventaJson.success).toBe(true);
    const totalCobradoDigital = ventaJson.data?.total ?? 0;
    expect(totalCobradoDigital).toBeGreaterThan(0);

    // Verificar generación del comprobante imprimible en pantalla
    const modalTicket = page
      .locator('div.fixed.inset-0')
      .filter({ hasText: 'Vista Previa de Comprobante' });
    await expect(modalTicket).toBeVisible();
    await expect(modalTicket.getByText(/TICKET DE VENTA POS/i)).toBeVisible();
    await expect(modalTicket.getByText(/Idempotency-Key:/i)).toBeVisible();
    await modalTicket.getByRole('button', { name: /Cerrar/i }).click();

    // -------------------------------------------------------------------------
    // d) Verificar en /recepcion/caja que montoFinalEsperado === $10.000 (sin contaminar) y totalVentasDigital > 0
    // -------------------------------------------------------------------------
    const refreshCajaPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/caja/estado-actual') &&
        res.request().method() === 'GET'
    );
    await page.goto('/recepcion/caja', { waitUntil: 'load' });
    await refreshCajaPromise;

    const cardEfectivoFinal = page
      .locator('div.bg-white')
      .filter({ hasText: '1. Efectivo Físico Esperado en Cajón' })
      .first();
    const cardDigitalFinal = page
      .locator('div.bg-white')
      .filter({ hasText: '2. Recaudación Digital Acumulada' })
      .first();

    const montoEfectivoEsperadoPostVenta = parseFormattedMoney(
      await cardEfectivoFinal.locator('p.text-2xl').innerText()
    );
    const montoDigitalAcumuladoPostVenta = parseFormattedMoney(
      await cardDigitalFinal.locator('p.text-2xl').innerText()
    );

    expect(montoEfectivoEsperadoPostVenta).toBe(10000);
    expect(montoDigitalAcumuladoPostVenta).toBeGreaterThanOrEqual(
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

    await expect(page.getByText('1. Facturación Total')).toBeVisible();
    await expect(page.getByText('2. Ticket Promedio')).toBeVisible();
    await expect(page.getByText('3. Turnos Atendidos')).toBeVisible();
    await expect(
      page.getByText('Ingresos Desglosados por Medio de Pago')
    ).toBeVisible();
    await expect(
      page.getByText('Ranking Comparativo por Estilista')
    ).toBeVisible();

    const facturacionCard = page
      .locator('div.bg-white')
      .filter({ hasText: '1. Facturación Total' })
      .first();
    const facturacionMonto = parseFormattedMoney(
      await facturacionCard.locator('p.text-2xl').innerText()
    );
    expect(facturacionMonto).toBeGreaterThanOrEqual(totalCobradoDigital);

    await expect(page.locator('.recharts-wrapper').first()).toBeVisible();

    guard.assertZeroServerOrPageErrors();
  });
});

test.describe('Critical Business Journeys E2E — Portal Público Sin Sesión (Cercado)', () => {
  test.use({ storageState: { cookies: [], origins: [] } });

  test('Journey 2: Portal Público (/reservar) sin sesión — selección de servicio, profesional, disponibilidad y reserva confirmada', async ({
    page,
  }) => {
    const guard = attachJourneyGuard(page);

    // Si el salón tuviese porcentajeSenaReserva > 0 y redirigiera al checkout externo,
    // interceptamos únicamente dominios externos de pasarela para no salir a Internet real.
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

    // Confirmar explícitamente que el contexto no posee sesión autenticada
    const hasToken = await page.evaluate(() =>
      Boolean(window.localStorage.getItem('mipeluqueria_jwt_token'))
    );
    expect(hasToken).toBe(false);

    await expect(
      page.getByRole('heading', {
        name: /Elegí los servicios para tu visita/i,
      })
    ).toBeVisible();

    // Paso 1: Seleccionar el primer servicio activo del catálogo público
    const primerServicioCard = page
      .locator('div.cursor-pointer')
      .filter({ hasText: '+ Agregar' })
      .first();
    await expect(primerServicioCard).toBeVisible();
    await primerServicioCard.click();
    await expect(page.getByText('✓ Seleccionado').first()).toBeVisible();

    await page.getByRole('button', { name: /Elegir Profesional/i }).click();

    // Paso 2: Seleccionar un profesional activo (o unificado) y pasar a disponibilidad
    await expect(
      page.getByRole('heading', {
        name: /Seleccioná tu estilista o barbero/i,
      })
    ).toBeVisible();

    // Elegimos el primer estilista específico disponible (después de la tarjeta "Cualquier profesional disponible")
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

    // Paso 3: Consultar disponibilidad real probando días hábiles futuros
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
        // Seleccionamos el último slot libre del día para minimizar colisiones con Journey 3
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

    // Paso 4: Completar datos del cliente sin sesión y confirmar reserva online
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
