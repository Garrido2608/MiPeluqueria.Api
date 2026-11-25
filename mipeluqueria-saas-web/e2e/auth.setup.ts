// filepath: e2e/auth.setup.ts
import fs from 'node:fs';
import path from 'node:path';
import { expect, Page, test as setup } from '@playwright/test';

const ADMIN_AUTH_FILE = path.join(__dirname, '.auth', 'admin.json');
const SUPERADMIN_AUTH_FILE = path.join(__dirname, '.auth', 'superadmin.json');

function isIgnorableConsoleError(text: string): boolean {
  const normalized = text.toLowerCase();
  return (
    normalized.includes('favicon.ico') ||
    normalized.includes('download the react devtools')
  );
}

function attachAuthSensors(page: Page) {
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
    assertClean() {
      expect(unexpectedErrors).toEqual([]);
    },
  };
}

setup(
  '1. Autenticar Administrador (admin@tesis.com), validar bloqueo RBAC y persistir admin.json',
  async ({ page }) => {
    const sensors = attachAuthSensors(page);

    const hydrationConfigPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/configuracion') &&
        res.request().method() === 'GET'
    );
    await page.goto('/login', { waitUntil: 'load' });
    await hydrationConfigPromise;

    await expect(
      page.getByRole('heading', { name: /Iniciar Sesión/i })
    ).toBeVisible();

    const cookieEssentialBtn = page.getByRole('button', {
      name: /Solo esenciales/i,
    });
    if (await cookieEssentialBtn.isVisible()) {
      await cookieEssentialBtn.click();
    }

    const loginInputs = page.locator('form input');
    await loginInputs.nth(0).fill('admin@tesis.com');
    await loginInputs.nth(1).fill('Admin123!');
    await expect(loginInputs.nth(0)).toHaveValue('admin@tesis.com');
    await expect(loginInputs.nth(1)).toHaveValue('Admin123!');

    const loginResponsePromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/auth/login') &&
        res.request().method() === 'POST'
    );
    await page.getByRole('button', { name: /Ingresar al Sistema/i }).click();

    const loginRes = await loginResponsePromise;
    expect(loginRes.status()).toBe(200);

    await page.waitForURL('**/admin/dashboard', { timeout: 25_000 });
    await expect(
      page.getByRole('heading', { level: 1, name: /^Dashboard Ejecutivo$/i })
    ).toBeVisible();

    // Verificar que el Sidebar NO expone módulos de plataforma al Administrador común
    const sidebarAside = page.getByRole('complementary', {
      name: /Navegación principal/i,
    });
    await expect(sidebarAside).toBeVisible();
    await expect(
      sidebarAside.getByRole('link', { name: /Consola SaaS/i })
    ).toHaveCount(0);
    await expect(
      sidebarAside.getByRole('link', { name: /Diagnóstico API/i })
    ).toHaveCount(0);

    // Verificar protección RBAC real en middleware: redirección desde /saas-admin y /admin/diagnostico-api
    await page.goto('/saas-admin', { waitUntil: 'load' });
    await expect(page).toHaveURL(/\/admin\/dashboard$/);

    await page.goto('/admin/diagnostico-api', { waitUntil: 'load' });
    await expect(page).toHaveURL(/\/admin\/dashboard$/);

    const tokenPresent = await page.evaluate(() => {
      return Boolean(
        window.localStorage.getItem('mipeluqueria_jwt_token') &&
          window.localStorage.getItem('mipeluqueria_user_session')
      );
    });
    expect(tokenPresent).toBe(true);

    fs.mkdirSync(path.dirname(ADMIN_AUTH_FILE), { recursive: true });
    await page.context().storageState({ path: ADMIN_AUTH_FILE });

    sensors.assertClean();
  }
);

setup(
  '2. Autenticar SuperAdminSaaS (superadmin@mipeluqueria.com), validar acceso de plataforma y persistir superadmin.json',
  async ({ page }) => {
    const sensors = attachAuthSensors(page);

    const hydrationConfigPromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/configuracion') &&
        res.request().method() === 'GET'
    );
    await page.goto('/login', { waitUntil: 'load' });
    await hydrationConfigPromise;

    const cookieEssentialBtn = page.getByRole('button', {
      name: /Solo esenciales/i,
    });
    if (await cookieEssentialBtn.isVisible()) {
      await cookieEssentialBtn.click();
    }

    const loginInputs = page.locator('form input');
    await loginInputs.nth(0).fill('superadmin@mipeluqueria.com');
    await loginInputs.nth(1).fill('SuperAdmin123!');

    const loginResponsePromise = page.waitForResponse(
      (res) =>
        res.url().includes('/api/auth/login') &&
        res.request().method() === 'POST'
    );
    await page.getByRole('button', { name: /Ingresar al Sistema/i }).click();

    const loginRes = await loginResponsePromise;
    expect(loginRes.status()).toBe(200);

    await page.waitForURL('**/saas-admin', { timeout: 25_000 });
    await expect(
      page.getByRole('heading', { level: 1, name: /^Consola SaaS$/i })
    ).toBeVisible();
    await expect(page.getByText(/AuditLogs JSON Diff/i)).toHaveCount(0);

    await page.goto('/admin/diagnostico-api', { waitUntil: 'load' });
    await expect(page).toHaveURL(/\/admin\/diagnostico-api$/);
    await expect(
      page.getByRole('heading', {
        level: 1,
        name: /^Diagnóstico API en Vivo$/i,
      })
    ).toBeVisible();

    fs.mkdirSync(path.dirname(SUPERADMIN_AUTH_FILE), { recursive: true });
    await page.context().storageState({ path: SUPERADMIN_AUTH_FILE });

    sensors.assertClean();
  }
);
