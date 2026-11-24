// filepath: e2e/smoke-19-routes.spec.ts
import { expect, Page, test } from '@playwright/test';

interface RouteExpectation {
  id: string;
  path: string;
  headingPattern: RegExp;
}

const ROUTES_TO_CRAWL: readonly RouteExpectation[] = [
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
    headingPattern: /Calendario Master Multi-Columna/i,
  },
  {
    id: '10-recepcion-pos',
    path: '/recepcion/pos',
    headingPattern: /Punto de Venta \(POS\) y Facturador Idempotente/i,
  },
  {
    id: '11-recepcion-caja',
    path: '/recepcion/caja',
    headingPattern: /Control de Caja Diaria y Arqueo Ciego/i,
  },
  {
    id: '12-recepcion-clientes',
    path: '/recepcion/clientes',
    headingPattern: /Padrón de Clientes y Fichas Técnicas Capilares/i,
  },
  // 4.4 Grupo Profesional (13)
  {
    id: '13-peluquero',
    path: '/peluquero',
    headingPattern: /Puesto de Trabajo/i,
  },
  // 4.5 Grupo Administración, Gerencia y SaaS (14..19)
  {
    id: '14-admin-dashboard',
    path: '/admin/dashboard',
    headingPattern: /Dashboard Ejecutivo \(Business Intelligence\)/i,
  },
  {
    id: '15-admin-inventario',
    path: '/admin/inventario',
    headingPattern: /Inventario, Kardex, Proveedores y Órdenes de Compra/i,
  },
  {
    id: '16-admin-servicios-recetas',
    path: '/admin/servicios-recetas',
    headingPattern: /Catálogo de Servicios y Recetas Técnicas de Gabinete/i,
  },
  {
    id: '17-admin-staff-liquidaciones',
    path: '/admin/staff-liquidaciones',
    headingPattern: /Staff Profesional, Esquemas de Comisión y Liquidación/i,
  },
  {
    id: '18-admin-configuracion-seguridad',
    path: '/admin/configuracion-seguridad',
    headingPattern: /Configuración del Salón, Catálogos Maestros y Seguridad/i,
  },
  {
    id: '19a-admin-diagnostico-api',
    path: '/admin/diagnostico-api',
    headingPattern: /Diagnóstico en Vivo de Conectividad con MiPeluqueria\.Api/i,
  },
  {
    id: '19b-saas-admin',
    path: '/saas-admin',
    headingPattern: /Consola de Comando Global SaaS/i,
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
    if (isIgnorableConsoleError(text)) {
      return;
    }
    consoleErrors.push(`[console.error @ ${page.url()}] ${text}`);
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
    // Ignorar cancelaciones normales de navegación SPA / prefetch de Next.js
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

test.describe('Smoke Crawler E2E — 19 Pantallas SDD + Diagnóstico 22 Controladores', () => {
  test('Con storageState de admin@tesis.com recorre las 19 pantallas con 0 errores runtime/hidratación/500', async ({
    page,
  }) => {
    const sensors = attachStrictSensors(page);

    for (const route of ROUTES_TO_CRAWL) {
      await page.goto(route.path, { waitUntil: 'load' });
      await expect(
        page.getByRole('heading', { name: route.headingPattern }).first()
      ).toBeVisible();

      // Verificación especial en /admin/diagnostico-api: Health Checks + 22 Controladores en lotes de 4
      if (route.path === '/admin/diagnostico-api') {
        await expect(page.getByText('200 HEALTHY').first()).toBeVisible();
        await expect(page.getByText('200 HEALTHY').nth(1)).toBeVisible();

        await page
          .getByRole('button', {
            name: /Probar los 22 Controladores/i,
          })
          .click();

        await expect(page.getByText('OK: 22/22')).toBeVisible({
          timeout: 45_000,
        });
        await expect(page.getByText('ERROR: 0')).toBeVisible();
      }

      // Verificar ausencia total de ErrorFallbackCard en cada ruta
      await expect(
        page.getByText(
          /No se pudo completar la consulta|Ocurrió un problema inesperado/i
        )
      ).toHaveCount(0);

      sensors.assertClean(`${route.id} (${route.path})`);
    }
  });
});
