// filepath: e2e/auth.setup.ts
import fs from 'node:fs';
import path from 'node:path';
import { expect, test as setup } from '@playwright/test';

const AUTH_FILE = path.join(__dirname, '.auth', 'admin.json');

setup('Autenticar sesión única de Administrador y persistir storageState', async ({
  page,
}) => {
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

  // Esperar que AppProviders hidrate React en cliente (dispara GET /api/configuracion)
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
    page.getByRole('heading', {
      name: /Dashboard Ejecutivo \(Business Intelligence\)/i,
    })
  ).toBeVisible();

  const tokenPresent = await page.evaluate(() => {
    return Boolean(
      window.localStorage.getItem('mipeluqueria_jwt_token') &&
        window.localStorage.getItem('mipeluqueria_user_session')
    );
  });
  expect(tokenPresent).toBe(true);

  fs.mkdirSync(path.dirname(AUTH_FILE), { recursive: true });
  await page.context().storageState({ path: AUTH_FILE });

  expect(unexpectedErrors).toEqual([]);
});
