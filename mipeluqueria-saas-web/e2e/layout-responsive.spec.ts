// filepath: e2e/layout-responsive.spec.ts
import path from 'node:path';
import { expect, Page, test } from '@playwright/test';

const SUPERADMIN_STORAGE_STATE = path.join(
  __dirname,
  '.auth',
  'superadmin.json'
);

interface ResponsiveRouteTarget {
  path: string;
  headingPattern: RegExp;
}

const DESKTOP_VIEWPORTS = [
  { label: '1280x720', width: 1280, height: 720 },
  { label: '1366x768', width: 1366, height: 768 },
] as const;

const SALON_ROUTES: readonly ResponsiveRouteTarget[] = [
  { path: '/', headingPattern: /Agenda en tiempo real|Mi Peluquería/i },
  { path: '/login', headingPattern: /Iniciar Sesión/i },
  { path: '/registro', headingPattern: /Crear Cuenta/i },
  { path: '/recuperar-clave', headingPattern: /Restablecer Contraseña/i },
  { path: '/reservar', headingPattern: /Elegí los servicios para tu visita/i },
  { path: '/mi-cuenta/turnos', headingPattern: /Mis Turnos/i },
  { path: '/mi-cuenta/perfil', headingPattern: /Mi Perfil y Seguridad/i },
  {
    path: '/legal/terminos',
    headingPattern: /Términos y Condiciones del Servicio/i,
  },
  {
    path: '/legal/privacidad',
    headingPattern: /Política de Privacidad y Protección de Datos/i,
  },
  { path: '/peluquero', headingPattern: /^Mi Puesto/i },
  { path: '/recepcion/calendario', headingPattern: /^Calendario$/i },
  { path: '/recepcion/pos', headingPattern: /^Punto de Venta$/i },
  { path: '/recepcion/caja', headingPattern: /^Caja Diaria$/i },
  { path: '/recepcion/clientes', headingPattern: /^Clientes$/i },
  { path: '/admin/dashboard', headingPattern: /^Dashboard Ejecutivo$/i },
  { path: '/admin/inventario', headingPattern: /^Inventario y Compras$/i },
  {
    path: '/admin/servicios-recetas',
    headingPattern: /^Servicios y Recetas de Gabinete$/i,
  },
  {
    path: '/admin/staff-liquidaciones',
    headingPattern: /^Staff, Comisiones y Liquidaciones$/i,
  },
  {
    path: '/admin/configuracion-seguridad',
    headingPattern: /^Configuración y Seguridad$/i,
  },
];

const PLATFORM_ROUTES: readonly ResponsiveRouteTarget[] = [
  { path: '/saas-admin', headingPattern: /^Consola SaaS$/i },
  {
    path: '/admin/diagnostico-api',
    headingPattern: /^Diagnóstico API en Vivo$/i,
  },
];

function isIgnorableConsoleError(text: string): boolean {
  const normalized = text.toLowerCase();
  return (
    normalized.includes('favicon.ico') ||
    normalized.includes('download the react devtools')
  );
}

function attachLayoutSensors(page: Page) {
  const pageErrors: string[] = [];
  const consoleErrors: string[] = [];
  const serverErrors: string[] = [];

  page.on('pageerror', (err) => {
    pageErrors.push(`[pageerror @ ${page.url()}] ${err.message}`);
  });

  page.on('console', (msg) => {
    if (msg.type() !== 'error') return;
    const text = msg.text();
    const locUrl = msg.location()?.url || '';
    if (isIgnorableConsoleError(text) || isIgnorableConsoleError(locUrl)) return;
    consoleErrors.push(`[console.error @ ${locUrl || page.url()}] ${text}`);
  });

  page.on('response', (res) => {
    if (res.status() >= 500) {
      serverErrors.push(
        `[HTTP ${res.status()}] ${res.request().method()} ${res.url()}`
      );
    }
  });

  return {
    assertZeroErrors(contextLabel: string) {
      expect(pageErrors, `pageerror en ${contextLabel}`).toEqual([]);
      expect(consoleErrors, `console.error en ${contextLabel}`).toEqual([]);
      expect(serverErrors, `HTTP >= 500 en ${contextLabel}`).toEqual([]);
    },
  };
}

async function assertZeroHorizontalOverflow(
  page: Page,
  contextLabel: string
): Promise<void> {
  const metrics = await page.evaluate(() => {
    const docEl = document.documentElement;
    return {
      scrollWidth: docEl.scrollWidth,
      clientWidth: docEl.clientWidth,
      innerWidth: window.innerWidth,
      isDark: docEl.classList.contains('dark'),
    };
  });

  expect(
    metrics.scrollWidth,
    `Desborde horizontal detectado en ${contextLabel}: scrollWidth=${metrics.scrollWidth}px > clientWidth=${metrics.clientWidth}px (innerWidth=${metrics.innerWidth}px)`
  ).toBeLessThanOrEqual(metrics.clientWidth);

  expect(
    metrics.scrollWidth,
    `Desborde horizontal sobre viewport en ${contextLabel}: scrollWidth=${metrics.scrollWidth}px > innerWidth=${metrics.innerWidth}px`
  ).toBeLessThanOrEqual(metrics.innerWidth);
}

test.describe('Sensor Determinístico Responsive y Temas (1280x720 + 1366x768) — Salón', () => {
  for (const vp of DESKTOP_VIEWPORTS) {
    test(`0px de desborde horizontal y conmutación Claro/Oscuro en las rutas del salón a ${vp.label}`, async ({
      page,
    }) => {
      const sensors = attachLayoutSensors(page);
      await page.setViewportSize({ width: vp.width, height: vp.height });

      // 1. Verificar arranque en Modo Claro (Studio Clean) y conmutación mediante ThemeToggle
      await page.goto('/admin/dashboard', { waitUntil: 'load' });
      await expect(
        page.getByRole('heading', { name: /^Dashboard Ejecutivo$/i })
      ).toBeVisible();

      const startsDark = await page.evaluate(() =>
        document.documentElement.classList.contains('dark')
      );
      if (startsDark) {
        await page
          .getByRole('button', { name: /Cambiar a modo claro/i })
          .click();
      }

      // Recorrido en Modo Claro
      for (const route of SALON_ROUTES) {
        await page.goto(route.path, { waitUntil: 'load' });
        await expect(
          page.getByRole('heading', { name: route.headingPattern }).first()
        ).toBeVisible();
        await assertZeroHorizontalOverflow(
          page,
          `${vp.label} [Light] ${route.path}`
        );
        sensors.assertZeroErrors(`${vp.label} [Light] ${route.path}`);
      }

      // 2. Activar Modo Oscuro (Obsidian & Indigo) desde el Topbar y verificar persistencia
      await page.goto('/admin/dashboard', { waitUntil: 'load' });
      await page
        .getByRole('button', { name: /Cambiar a modo oscuro/i })
        .click();

      const darkActive = await page.evaluate(() => ({
        hasClass: document.documentElement.classList.contains('dark'),
        stored: window.localStorage.getItem('mipeluqueria_theme'),
      }));
      expect(darkActive.hasClass).toBe(true);
      expect(darkActive.stored).toBe('dark');

      // Recorrido de todas las pantallas del dashboard en Modo Oscuro
      const dashboardRoutes = SALON_ROUTES.filter(
        (r) =>
          r.path.startsWith('/peluquero') ||
          r.path.startsWith('/recepcion') ||
          r.path.startsWith('/admin')
      );

      for (const route of dashboardRoutes) {
        await page.goto(route.path, { waitUntil: 'load' });
        await expect(
          page.getByRole('heading', { name: route.headingPattern }).first()
        ).toBeVisible();
        const stillDark = await page.evaluate(() =>
          document.documentElement.classList.contains('dark')
        );
        expect(stillDark).toBe(true);
        await assertZeroHorizontalOverflow(
          page,
          `${vp.label} [Dark] ${route.path}`
        );
        sensors.assertZeroErrors(`${vp.label} [Dark] ${route.path}`);
      }

      // Restaurar Modo Claro
      await page.getByRole('button', { name: /Cambiar a modo claro/i }).click();
      const restoredLight = await page.evaluate(() => ({
        hasClass: document.documentElement.classList.contains('dark'),
        stored: window.localStorage.getItem('mipeluqueria_theme'),
      }));
      expect(restoredLight.hasClass).toBe(false);
      expect(restoredLight.stored).toBe('light');
    });
  }
});

test.describe('Sensor Determinístico Responsive y Temas (1280x720 + 1366x768) — Plataforma SuperAdminSaaS', () => {
  test.use({ storageState: SUPERADMIN_STORAGE_STATE });

  for (const vp of DESKTOP_VIEWPORTS) {
    test(`0px de desborde horizontal en /saas-admin y /admin/diagnostico-api (Claro y Oscuro) a ${vp.label}`, async ({
      page,
    }) => {
      const sensors = attachLayoutSensors(page);
      await page.setViewportSize({ width: vp.width, height: vp.height });

      for (const themeMode of ['light', 'dark'] as const) {
        await page.goto('/saas-admin', { waitUntil: 'load' });
        const currentlyDark = await page.evaluate(() =>
          document.documentElement.classList.contains('dark')
        );

        if (themeMode === 'dark' && !currentlyDark) {
          await page
            .getByRole('button', { name: /Cambiar a modo oscuro/i })
            .click();
        } else if (themeMode === 'light' && currentlyDark) {
          await page
            .getByRole('button', { name: /Cambiar a modo claro/i })
            .click();
        }

        for (const route of PLATFORM_ROUTES) {
          await page.goto(route.path, { waitUntil: 'load' });
          await expect(
            page.getByRole('heading', { name: route.headingPattern }).first()
          ).toBeVisible();
          await assertZeroHorizontalOverflow(
            page,
            `${vp.label} [${themeMode}] ${route.path}`
          );
          sensors.assertZeroErrors(`${vp.label} [${themeMode}] ${route.path}`);
        }
      }
    });
  }
});

test.describe('Sensor Determinístico Mobile-First (390x844) — Fichas Técnicas en /peluquero', () => {
  test('En celular (390x844) opera navegación móvil, lista de clientes y detalle de Fichas Técnicas con 0px de desborde', async ({
    page,
  }) => {
    const sensors = attachLayoutSensors(page);
    await page.setViewportSize({ width: 390, height: 844 });

    await page.goto('/peluquero', { waitUntil: 'load' });
    await expect(
      page.getByRole('heading', { name: /^Mi Puesto/i }).first()
    ).toBeVisible();
    await assertZeroHorizontalOverflow(page, '390x844 /peluquero (Mi Agenda)');

    // Verificar drawer móvil del Sidebar
    const menuBtn = page.getByRole('button', {
      name: /Abrir menú de navegación/i,
    });
    await expect(menuBtn).toBeVisible();
    await menuBtn.click();
    const sidebarAside = page.getByRole('complementary', {
      name: /Navegación principal/i,
    });
    await expect(sidebarAside).toBeVisible();
    await expect(
      sidebarAside.getByRole('button', { name: /^Cerrar menú$/i })
    ).toBeVisible();
    await page.keyboard.press('Escape');

    // Abrir pestaña Fichas Técnicas y validar Master -> Detail móvil
    const clientesFetchPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/clientes') && res.request().method() === 'GET'
    );
    await page.getByRole('tab', { name: /Fichas Técnicas/i }).click();
    const clientesRes = await clientesFetchPromise;
    expect(clientesRes.status()).toBe(200);

    await expect(page.getByPlaceholder(/Buscar por nombre o teléfono/i)).toBeVisible();
    await assertZeroHorizontalOverflow(
      page,
      '390x844 /peluquero (Lista Clientes Fichas)'
    );

    // Seleccionar cliente semilla (Juan Pérez o primer cliente disponible)
    const clienteBoton = page
      .locator('section button')
      .filter({ hasText: /Juan Pérez|\+54/i })
      .first();
    await expect(clienteBoton).toBeVisible();
    await clienteBoton.click();

    // En vista móvil debe aparecer "Volver a clientes" y "Nueva Ficha Técnica" sin recortes
    const volverBtn = page.getByRole('button', { name: /Volver a clientes/i });
    const nuevaFichaBtn = page.getByRole('button', {
      name: /Nueva Ficha Técnica/i,
    });
    await expect(volverBtn).toBeVisible();
    await expect(nuevaFichaBtn).toBeVisible();

    const btnBox = await nuevaFichaBtn.boundingBox();
    expect(btnBox).not.toBeNull();
    expect(btnBox!.x).toBeGreaterThanOrEqual(0);
    expect(btnBox!.x + btnBox!.width).toBeLessThanOrEqual(390);

    await assertZeroHorizontalOverflow(
      page,
      '390x844 /peluquero (Detalle Ficha Técnica Light)'
    );

    // Validar también en Modo Oscuro a 390x844
    await page.getByRole('button', { name: /Cambiar a modo oscuro/i }).click();
    await assertZeroHorizontalOverflow(
      page,
      '390x844 /peluquero (Detalle Ficha Técnica Dark)'
    );

    // Abrir modal de Nueva Ficha Técnica y verificar encaje en 390px
    await nuevaFichaBtn.click();
    const modalNuevaFicha = page.getByRole('dialog');
    await expect(modalNuevaFicha).toBeVisible();
    await assertZeroHorizontalOverflow(
      page,
      '390x844 /peluquero (Modal Nueva Ficha)'
    );
    await modalNuevaFicha
      .getByRole('button', { name: /Cerrar ventana/i })
      .click();

    // Regresar a la lista de clientes móvil
    await volverBtn.click();
    await expect(
      page.getByPlaceholder(/Buscar por nombre o teléfono/i)
    ).toBeVisible();
    await assertZeroHorizontalOverflow(
      page,
      '390x844 /peluquero (Retorno a Lista)'
    );

    // Restaurar Modo Claro
    await page.getByRole('button', { name: /Cambiar a modo claro/i }).click();
    sensors.assertZeroErrors('390x844 /peluquero Fichas Técnicas');
  });
});
