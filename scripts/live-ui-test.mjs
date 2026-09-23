/**
 * Playwright UI smoke — requires: npx playwright install chromium (once)
 * Usage: node scripts/live-ui-test.mjs <baseUrl> <email> <password>
 */
import { chromium } from 'playwright';
import crypto from 'node:crypto';

const baseUrl = (process.argv[2] || '').replace(/\/$/, '');
const email = process.argv[3];
const password = process.argv[4];

const bugs = [];
const routes = [
  '/dashboard',
  '/companies',
  '/care-homes',
  '/clients',
  '/funding-authorities',
  '/invoice-categories',
  '/nominal-codes',
  '/invoice-templates',
  '/billing',
  '/invoices',
  '/credit-notes',
  '/receivables',
  '/payments',
  '/banking',
  '/remittances',
  '/revenue-assurance',
  '/disputes',
  '/collections',
  '/contract-renewals',
  '/misc-charges',
  '/reports',
  '/sage-exports',
  '/users',
  '/audit',
  '/settings/organisation',
];

function bug(severity, area, title, detail) {
  bugs.push({ severity, area, title, detail });
}

async function encryptPassword(publicKey, plain) {
  const keyObject = crypto.createPublicKey({
    key: { kty: 'RSA', n: publicKey.n, e: publicKey.e, alg: 'RSA-OAEP-256' },
    format: 'jwk',
  });
  const encrypted = crypto.publicEncrypt(
    {
      key: keyObject,
      padding: crypto.constants.RSA_PKCS1_OAEP_PADDING,
      oaepHash: 'sha256',
    },
    Buffer.from(plain, 'utf8'),
  );
  return `enc:${encrypted.toString('base64')}`;
}

async function loginViaApi(request) {
  const keyRes = await request.get(`${baseUrl}/api/auth/login-key`);
  const keyBody = await keyRes.json();
  const passwordCipher = await encryptPassword(keyBody, password);
  const loginRes = await request.post(`${baseUrl}/api/auth/login`, {
    data: { email, passwordCipher },
  });
  if (!loginRes.ok()) {
    throw new Error(`Login failed: ${loginRes.status()} ${await loginRes.text()}`);
  }
  return loginRes.json();
}

async function main() {
  if (!baseUrl || !email || !password) {
    console.error('Usage: node scripts/live-ui-test.mjs <baseUrl> <email> <password>');
    process.exit(1);
  }

  let browser;
  const launchOpts = { headless: true, channel: 'msedge' };
  try {
    browser = await chromium.launch(launchOpts);
  } catch {
    try {
      browser = await chromium.launch({
        headless: true,
        executablePath: 'C:/Program Files/Google/Chrome/Application/chrome.exe',
      });
    } catch (e) {
      console.error('Could not launch Edge/Chrome for Playwright:', e.message);
      process.exit(2);
    }
  }

  const context = await browser.newContext({ baseURL: baseUrl });
  const page = await context.newPage();
  const apiErrors = [];
  const consoleErrors = [];

  page.on('console', (msg) => {
    if (msg.type() === 'error') {
      const text = msg.text();
      if (!text.includes('favicon')) consoleErrors.push(text);
    }
  });

  page.on('response', (res) => {
    const url = res.url();
    if (url.includes('/api/') && res.status() >= 400) {
      apiErrors.push({ status: res.status(), url });
    }
  });

  const auth = await loginViaApi(context.request);
  await page.addInitScript((user) => {
    localStorage.setItem('carehome.auth', JSON.stringify(user));
  }, auth);

  // Login page
  await page.goto('/login');
  if (await page.locator('text=Care Home Back Office').count() === 0) {
    bug('medium', 'UI', 'Login page missing branding', await page.title());
  }

  for (const route of routes) {
    apiErrors.length = 0;
    const routeConsoleStart = consoleErrors.length;
    await page.goto(route, { waitUntil: 'networkidle', timeout: 60000 }).catch((e) => {
      bug('high', 'UI', `Navigation timeout: ${route}`, e.message);
    });

    const url = page.url();
    if (url.includes('/login')) {
      bug('high', 'UI', `Redirected to login from ${route}`, url);
      continue;
    }
    if (url.includes('/forbidden')) {
      bug('low', 'UI', `Forbidden for ${route}`, 'Role may block route');
      continue;
    }

    const bodyText = await page.locator('body').innerText().catch(() => '');
    if (/something went wrong|unexpected error|error loading/i.test(bodyText)) {
      bug('high', 'UI', `Error message on ${route}`, bodyText.slice(0, 200));
    }

    for (const err of apiErrors) {
      if (err.status === 403) continue;
      bug('high', 'UI/API', `HTTP ${err.status} on ${route}`, err.url);
    }

    for (let i = routeConsoleStart; i < consoleErrors.length; i++) {
      const c = consoleErrors[i];
      if (/chunk|Loading chunk|NG0/.test(c)) {
        bug('high', 'UI', `Console error on ${route}`, c);
      }
    }
  }

  // Interactive: open first client if list has rows
  await page.goto('/clients', { waitUntil: 'networkidle' });
  const clientLink = page.locator('a[href^="/clients/"]').first();
  if (await clientLink.count()) {
    await clientLink.click();
    await page.waitForLoadState('networkidle');
    const tabs = ['Profile', 'Funding', 'Invoices', 'Documents'];
    for (const tab of tabs) {
      const tabBtn = page.getByRole('tab', { name: new RegExp(tab, 'i') }).or(page.getByRole('button', { name: new RegExp(tab, 'i') }));
      if (await tabBtn.count()) {
        await tabBtn.first().click().catch(() => {});
        await page.waitForTimeout(800);
        const txt = await page.locator('body').innerText();
        if (/error|failed to load/i.test(txt) && /profile|funding/i.test(tab.toLowerCase())) {
          // weak signal — only flag if tab panel empty
        }
      }
    }
  }

  await browser.close();

  console.log('\n=== UI BUGS (' + bugs.length + ') ===');
  for (const b of bugs) {
    console.log(`\n[${b.severity}] ${b.area}: ${b.title}`);
    console.log('  ', b.detail);
  }
  if (bugs.length === 0) console.log('None detected in automated UI pass.');
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
