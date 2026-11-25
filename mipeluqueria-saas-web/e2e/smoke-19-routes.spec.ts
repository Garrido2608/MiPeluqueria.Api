// filepath: e2e/smoke-19-routes.spec.ts
import path from 'node:path';
import { expect, Page, test } from '@playwright/test';

const SUPERADMIN_STORAGE_STATE = path.join(
  __dirname,
  '.auth',
  'superadmin.json'
);

interface RouteExpectation {
  id: string;
  path: string;
  headingPattern: RegExp;
}

const ADMIN_ROUTES_TO_CRAWL: readonly RouteExpectation[] = [
  // Landing Raíz
  {
    id: '00-home',
    path: '/',
    headingPattern: /Agenda en tiempo real|Mi Peluquería/i,
  },
  // 4.1 Grupo Autenticación (1..3)
  {
    id: '01-login',
    path: '/login',
    headingPattern: /Iniciar Sesión/i,
  },
  {
    id: '02-registro',
    path: '/registro',
    headingPattern: /Crear Cuenta/i,
  },
  {
    id: '03-recuperar-clave',
    path: '/recuperar-clave',
    headingPattern: /Restablecer Contraseña/i,
  },
  // 4.2 Grupo Portal Público y Autogestión Cliente (4..8)
  {
    id: '04-reservar',
    path: '/reservar',
    headingPattern: /Elegí los servicios para tu visita/i,
  },
  {
    id: '05-mi-cuenta-turnos',
    path: '/mi-cuenta/turnos',
    headingPattern: /Mis Turnos/i,
  },
  {
    id: '06-mi-cuenta-perfil',
    path: '/mi-cuenta/perfil',
    headingPattern: /Mi Perfil y Seguridad/i,
  },
  {
    id: '07-legal-terminos',
    path: '/legal/terminos',
    headingPattern: /Términos y Condiciones del Servicio/i,
  },
  {
    id: '08-legal-privacidad',
    path: '/legal/privacidad',
    headingPattern: /Política de Privacidad y Protección de Datos/i,
  },
  // 4.3 Grupo Operativo de Recepción y Caja (9..12)
  {
    id: '09-recepcion-calendario',
    path: '/recepcion/calendario',
    headingPattern: /^Calendario$/i,
  },
  {
    id: '10-recepcion-pos',
    path: '/recepcion/pos',
    headingPattern: /^Punto de Venta$/i,
  },
  {
    id: '11-recepcion-caja',
    path: '/recepcion/caja',
    headingPattern: /^Caja Diaria$/i,
  },
  {
    id: '12-recepcion-clientes',
    path: '/recepcion/clientes',
    headingPattern: /^Clientes$/i,
  },
  // 4.4 Grupo Profesional (13)
  {
    id: '13-peluquero',
    path: '/peluquero',
    headingPattern: /^Mi Puesto/i,
  },
  // 4.5 Grupo Administración del Salón (14..18)
  {
    id: '14-admin-dashboard',
    path: '/admin/dashboard',
    headingPattern: /^Dashboard Ejecutivo$/i,
  },
  {
    id: '15-admin-inventario',
    path: '/admin/inventario',
    headingPattern: /^Inventario y Compras$/i,
  },
  {
    id: '16-admin-servicios-recetas',
    path: '/admin/servicios-recetas',
    headingPattern: /^Servicios y Recetas de Gabinete$/i,
  },
  {
    id: '17-admin-staff-liquidaciones',
    path: '/admin/staff-liquidaciones',
    headingPattern: /^Staff, Comisiones y Liquidaciones$/i,
  },
  {
    id: '18-admin-configuracion-seguridad',
    path: '/admin/configuracion-seguridad',
    headingPattern: /^Configuración y Seguridad$/i,
  },
];

const SUPERADMIN_PLATFORM_ROUTES: readonly RouteExpectation[] = [
  {
    id: '19a-admin-diagnostico-api',
    path: '/admin/diagnostico-api',
    headingPattern: /^Diagnóstico API en Vivo$/i,
  },
  {
    id: '19b-saas-admin',
    path: '/saas-admin',
    headingPattern: /^Consola SaaS$/i,
  },
];

function isIgnorableConsoleError(text: string): boolean {
  const normalized = text.toLowerCase();
  if (normalized.includes('favicon.ico')) {
    return true;
  }
  if (normalized.includes('download the react devtools')) {
    return true;
  }
  return false;
}

function attachStrictSensors(page: Page) {
  const pageErrors: string[] = [];
  const consoleErrors: string[] = [];
  const serverErrors: string[] = [];
  const networkTimeouts: string[] = [];

  page.on('pageerror', (err) => {
    pageErrors.push(`[pageerror @ ${page.url()}] ${err.message}`);
  });

  page.on('console', (msg) => {
    if (msg.type() !== 'error') {
      return;
    }
    const text = msg.text();
    const locUrl = msg.location()?.url || '';
    if (isIgnorableConsoleError(text) || isIgnorableConsoleError(locUrl)) {
      return;
    }
    consoleErrors.push(
      `[console.error @ ${locUrl || page.url()}] ${text}`
    );
  });

  page.on('response', (res) => {
    const status = res.status();
    if (status >= 500) {
      serverErrors.push(
        `[HTTP ${status}] ${res.request().method()} ${res.url()}`
      );
    }
  });

  page.on('requestfailed', (req) => {
    const failureText = req.failure()?.errorText || '';
    if (
      failureText.includes('ERR_ABORTED') ||
      failureText.includes('NS_BINDING_ABORTED') ||
      req.url().includes('favicon.ico')
    ) {
      return;
    }
    networkTimeouts.push(
      `[NETWORK FAIL] ${req.method()} ${req.url()} -> ${failureText}`
    );
  });

  return {
    assertClean(stepLabel: string) {
      expect(
        pageErrors,
        `Excepción runtime detectada en ${stepLabel}`
      ).toEqual([]);
      expect(
        consoleErrors,
        `Error de consola/hidratación detectado en ${stepLabel}`
      ).toEqual([]);
      expect(
        serverErrors,
        `Respuesta HTTP >= 500 detectada en ${stepLabel}`
      ).toEqual([]);
      expect(
        networkTimeouts,
        `Timeout o fallo de red detectado en ${stepLabel}`
      ).toEqual([]);
    },
  };
}

test.describe('Smoke Crawler E2E — Pantallas del Salón (Administrador)', () => {
  test('Con storageState de admin@tesis.com recorre las 18 rutas del salón con 0 errores y valida bloqueo RBAC en plataforma', async ({
    page,
  }) => {
    const sensors = attachStrictSensors(page);

    for (const route of ADMIN_ROUTES_TO_CRAWL) {
      await page.goto(route.path, { waitUntil: 'load' });
      await expect(
        page.getByRole('heading', { name: route.headingPattern }).first()
      ).toBeVisible();

      await expect(
        page.getByText(
          /No se pudo completar la consulta|Ocurrió un problema inesperado/i
        )
      ).toHaveCount(0);

      sensors.assertClean(`${route.id} (${route.path})`);
    }

    // Verificar que Administrador sigue siendo redirigido fuera de rutas exclusivas de SuperAdminSaaS
    for (const restrictedPath of ['/saas-admin', '/admin/diagnostico-api']) {
      await page.goto(restrictedPath, { waitUntil: 'load' });
      await expect(page).toHaveURL(/\/admin\/dashboard$/);
      sensors.assertClean(`RBAC redirect (${restrictedPath})`);
    }
  });
});

test.describe('Smoke Crawler E2E — Consola SaaS y Diagnóstico 22 Controladores (SuperAdminSaaS)', () => {
  test.use({ storageState: SUPERADMIN_STORAGE_STATE });

  test('Con storageState de superadmin@mipeluqueria.com recorre /saas-admin y /admin/diagnostico-api (22/22 200 OK)', async ({
    page,
  }) => {
    const sensors = attachStrictSensors(page);

    for (const route of SUPERADMIN_PLATFORM_ROUTES) {
      await page.goto(route.path, { waitUntil: 'load' });
      await expect(
        page.getByRole('heading', { name: route.headingPattern }).first()
      ).toBeVisible();

      if (route.path === '/admin/diagnostico-api') {
        await expect(page.getByText('200 HEALTHY').first()).toBeVisible();
        await expect(page.getByText('200 HEALTHY').nth(1)).toBeVisible();

        await page
          .getByRole('button', {
            name: /Probar los 22 controladores/i,
          })
          .click();

        await expect(page.getByText('OK: 22/22')).toBeVisible({
          timeout: 90_000,
        });
        await expect(page.getByText('ERROR: 0')).toBeVisible();
      }

      if (route.path === '/saas-admin') {
        await expect(page.getByText(/AuditLogs JSON Diff/i)).toHaveCount(0);
      }

      await expect(
        page.getByText(
          /No se pudo completar la consulta|Ocurrió un problema inesperado/i
        )
      ).toHaveCount(0);

      sensors.assertClean(`${route.id} (${route.path})`);
    }
  });
});
