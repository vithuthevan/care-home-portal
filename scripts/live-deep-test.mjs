/**
 * Deeper API / consistency checks for live deployment.
 */
import crypto from 'node:crypto';

const baseUrl = (process.argv[2] || '').replace(/\/$/, '');
const email = process.argv[3] || process.env.CAREHOME_LIVE_EMAIL;
const password = process.argv[4] || process.env.CAREHOME_LIVE_PASSWORD;
const bugs = [];

function bug(sev, area, title, detail) {
  bugs.push({ sev, area, title, detail });
}

async function login() {
  const key = await (await fetch(`${baseUrl}/api/auth/login-key`)).json();
  const keyObject = crypto.createPublicKey({
    key: { kty: 'RSA', n: key.n, e: key.e, alg: 'RSA-OAEP-256' },
    format: 'jwk',
  });
  const passwordCipher =
    'enc:' +
    crypto
      .publicEncrypt(
        {
          key: keyObject,
          padding: crypto.constants.RSA_PKCS1_OAEP_PADDING,
          oaepHash: 'sha256',
        },
        Buffer.from(password),
      )
      .toString('base64');
  const res = await fetch(`${baseUrl}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, passwordCipher }),
  });
  const body = await res.json();
  if (!res.ok) throw new Error('login failed');
  return body.token;
}

async function api(token, method, path, body) {
  const res = await fetch(`${baseUrl}${path}`, {
    method,
    headers: {
      Authorization: `Bearer ${token}`,
      Accept: 'application/json',
      ...(body ? { 'Content-Type': 'application/json' } : {}),
    },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await res.text();
  let json = null;
  try {
    json = JSON.parse(text);
  } catch {
    json = text;
  }
  return { status: res.status, json, text, headers: res.headers };
}

async function main() {
  if (!baseUrl || !email || !password) {
    console.error(
      'Usage: node scripts/live-deep-test.mjs <baseUrl> <email> <password>\n' +
        'Or set CAREHOME_LIVE_EMAIL and CAREHOME_LIVE_PASSWORD.',
    );
    process.exit(1);
  }

  const token = await login();
  const h = { token };

  // Unauthenticated access
  const noAuth = await api(null, 'GET', '/api/clients');
  if (noAuth.status !== 401) {
    bug('critical', 'Security', 'Unauthenticated /api/clients not 401', String(noAuth.status));
  }

  // Wrong path returns HTML (SPA fallback) — deployment/nginx config smell
  const wrongApi = await api(token, 'GET', '/api/organisation-settings');
  if (typeof wrongApi.json === 'string' || wrongApi.text.startsWith('<!')) {
    bug(
      'low',
      'Infrastructure',
      'Unknown /api/* paths fall through to SPA index.html',
      '/api/organisation-settings returns HTML with 200 — breaks API clients that typo the URL',
    );
  }

  const settings = await api(token, 'GET', '/api/settings/organisation');
  if (settings.status !== 200 || !settings.json?.name) {
    bug('high', 'Admin', 'Organisation settings unavailable', `${settings.status}`);
  }

  const dash = await api(token, 'GET', '/api/dashboard');
  if (dash.status !== 200) {
    bug('high', 'Dashboard', 'Dashboard API failed', String(dash.status));
  } else {
    const d = dash.json;
    if (d.outstandingInvoiceCount > 0 && (!d.outstandingInvoiceTotal || d.outstandingInvoiceTotal === 0)) {
      bug('medium', 'Dashboard', 'Outstanding count but zero total', JSON.stringify(d));
    }
    if (d.careHomeCount > 0 && d.currentClientCount === 0) {
      // informational — may be valid empty home
    }
  }

  const reportPaths = [
    '/api/reports/outstanding-invoices',
    '/api/reports/client-census',
    '/api/reports/current-rates',
    '/api/reports/invoices-by-client',
    '/api/reports/aged-debt',
    '/api/reports/occupancy',
  ];
  for (const p of reportPaths) {
    const r = await api(token, 'GET', p);
    if (r.status >= 500) {
      bug('high', 'Reports', `${p} server error`, `${r.status} ${r.text.slice(0, 200)}`);
    } else if (r.status === 404) {
      bug('medium', 'Reports', `${p} not found`, '');
    }
  }

  // Validation: empty company create should not 500
  const badCompany = await api(token, 'POST', '/api/companies', { name: '' });
  if (badCompany.status >= 500) {
    bug('high', 'Companies', 'Empty create causes 500', badCompany.text.slice(0, 300));
  }

  const companies = await api(token, 'GET', '/api/companies');
  const clients = await api(token, 'GET', '/api/clients?page=1&pageSize=50');
  const invoices = await api(token, 'GET', '/api/invoices?page=1&pageSize=50');

  if (clients.json?.totalCount > 0 && invoices.json?.totalCount === 0) {
    bug(
      'low',
      'Data',
      'Clients exist but no invoices',
      'May be fresh tenant — verify demo data seeded for UAT',
    );
  }

  // Billing preview without params
  const billingPreview = await api(token, 'GET', '/api/billing/preview');
  if (billingPreview.status >= 500) {
    bug('high', 'Billing', 'Billing preview 500 without params', billingPreview.text.slice(0, 300));
  }

  const periods = await api(token, 'GET', '/api/billing/periods');
  if (periods.status !== 200) {
    bug('medium', 'Billing', 'Billing periods failed', String(periods.status));
  }

  // Rate limit headers on login-key
  const keyRes = await fetch(`${baseUrl}/api/auth/login-key`);
  const csp = keyRes.headers.get('content-security-policy');
  const xcto = keyRes.headers.get('x-content-type-options');

  // Check security headers on API response
  if (!xcto) {
    bug('low', 'Security', 'API missing X-Content-Type-Options', 'login-key response');
  }

  // CORS - should not allow arbitrary origin
  const corsProbe = await fetch(`${baseUrl}/api/auth/login-key`, {
    headers: { Origin: 'https://evil.example' },
  });
  const acao = corsProbe.headers.get('access-control-allow-origin');
  if (acao === 'https://evil.example' || acao === '*') {
    bug('high', 'Security', 'Permissive CORS on API', acao);
  }

  // Login with wrong password — generic message
  const key = await (await fetch(`${baseUrl}/api/auth/login-key`)).json();
  const keyObject = crypto.createPublicKey({
    key: { kty: 'RSA', n: key.n, e: key.e, alg: 'RSA-OAEP-256' },
    format: 'jwk',
  });
  const badCipher =
    'enc:' +
    crypto
      .publicEncrypt(
        { key: keyObject, padding: crypto.constants.RSA_PKCS1_OAEP_PADDING, oaepHash: 'sha256' },
        Buffer.from('wrong-password-xyz'),
      )
      .toString('base64');
  const badLogin = await fetch(`${baseUrl}/api/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ email, passwordCipher: badCipher }),
  });
  const badBody = await badLogin.json();
  if (badLogin.status !== 401) {
    bug('medium', 'Auth', 'Wrong password not 401', String(badLogin.status));
  }
  if (badBody.message && /user not found|does not exist/i.test(badBody.message)) {
    bug('medium', 'Security', 'Login reveals user enumeration', badBody.message);
  }

  // Users list — no password fields leaked
  const users = await api(token, 'GET', '/api/users');
  if (users.status === 200) {
    const str = JSON.stringify(users.json);
    if (/passwordHash|PasswordHash/i.test(str)) {
      bug('critical', 'Security', 'User API leaks password hash', '');
    }
  }

  console.log('BUGS:', bugs.length);
  for (const b of bugs) {
    console.log(`\n[${b.sev}] ${b.area}: ${b.title}\n   ${b.detail}`);
  }
  if (!bugs.length) console.log('No issues in deep API pass.');
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
