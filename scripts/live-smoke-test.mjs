/**
 * Live deployment smoke / API regression script.
 * Usage: node scripts/live-smoke-test.mjs <baseUrl> <email> <password>
 */
import crypto from 'node:crypto';

const baseUrl = (process.argv[2] || '').replace(/\/$/, '');
const email = process.argv[3] || process.env.CAREHOME_LIVE_EMAIL;
const password = process.argv[4] || process.env.CAREHOME_LIVE_PASSWORD;

const bugs = [];
const passes = [];

function bug(severity, area, title, detail) {
  bugs.push({ severity, area, title, detail });
}

function pass(msg) {
  passes.push(msg);
}

async function fetchJson(path, options = {}) {
  const url = `${baseUrl}${path}`;
  const res = await fetch(url, {
    ...options,
    headers: {
      Accept: 'application/json',
      ...(options.headers || {}),
    },
  });
  const text = await res.text();
  let body = null;
  try {
    body = text ? JSON.parse(text) : null;
  } catch {
    body = text;
  }
  return { res, body, text };
}

function bytesToBase64(bytes) {
  return Buffer.from(bytes).toString('base64');
}

async function encryptLoginPassword(publicKey, plainPassword) {
  const keyObject = crypto.createPublicKey({
    key: {
      kty: 'RSA',
      n: publicKey.n,
      e: publicKey.e,
      alg: 'RSA-OAEP-256',
    },
    format: 'jwk',
  });
  const encrypted = crypto.publicEncrypt(
    {
      key: keyObject,
      padding: crypto.constants.RSA_PKCS1_OAEP_PADDING,
      oaepHash: 'sha256',
    },
    Buffer.from(plainPassword, 'utf8'),
  );
  return `enc:${bytesToBase64(encrypted)}`;
}

async function main() {
  if (!baseUrl || !email || !password) {
    console.error('Usage: node scripts/live-smoke-test.mjs <baseUrl> <email> <password>');
    process.exit(1);
  }

  // --- Static / health ---
  for (const path of ['/health/live', '/health/ready', '/api/auth/login-key']) {
    try {
      const { res, body } = await fetchJson(path);
      if (!res.ok) {
        bug('high', 'Infrastructure', `${path} returned ${res.status}`, String(body?.message || body));
      } else {
        pass(`${path} → ${res.status}`);
      }
    } catch (e) {
      bug('critical', 'Infrastructure', `Cannot reach ${path}`, e.message);
    }
  }

  // --- Login ---
  let token = null;
  let me = null;
  try {
    const { res: keyRes, body: keyBody } = await fetchJson('/api/auth/login-key');
    if (!keyRes.ok || !keyBody?.n) {
      bug('critical', 'Auth', 'login-key missing RSA key', JSON.stringify(keyBody));
    } else {
      const passwordCipher = await encryptLoginPassword(keyBody, password);
      const { res, body } = await fetchJson('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ email, passwordCipher }),
      });
      if (!res.ok) {
        bug('critical', 'Auth', 'Login failed', `${res.status}: ${body?.message || JSON.stringify(body)}`);
      } else {
        token = body.token;
        me = body;
        pass(`Login OK — roles: ${(body.roles || []).join(', ')}`);
        if (body.mustChangePassword) {
          bug('medium', 'Auth', 'User must change password', 'Redirects to change-password; full app may be blocked');
        }
      }
    }
  } catch (e) {
    bug('critical', 'Auth', 'Login exception', e.message);
  }

  if (!token) {
    printReport();
    process.exit(1);
  }

  const auth = { Authorization: `Bearer ${token}` };

  const apiChecks = [
    ['GET', '/api/auth/me'],
    ['GET', '/api/dashboard'],
    ['GET', '/api/companies'],
    ['GET', '/api/care-homes'],
    ['GET', '/api/clients?page=1&pageSize=20'],
    ['GET', '/api/funding-authorities'],
    ['GET', '/api/invoice-categories'],
    ['GET', '/api/nominal-codes'],
    ['GET', '/api/invoice-templates'],
    ['GET', '/api/invoices?page=1&pageSize=20'],
    ['GET', '/api/billing/periods'],
    ['GET', '/api/credit-notes'],
    ['GET', '/api/receivables/summary'],
    ['GET', '/api/payments?page=1&pageSize=20'],
    ['GET', '/api/banking/accounts'],
    ['GET', '/api/remittances?page=1&pageSize=20'],
    ['GET', '/api/revenue-assurance/dashboard'],
    ['GET', '/api/disputes'],
    ['GET', '/api/collections/dashboard'],
    ['GET', '/api/contract-renewals'],
    ['GET', '/api/misc-charges'],
    ['GET', '/api/reports/outstanding-invoices'],
    ['GET', '/api/sage-exports'],
    ['GET', '/api/users'],
    ['GET', '/api/audit?page=1&pageSize=20'],
    ['GET', '/api/settings/organisation'],
  ];

  for (const [method, path] of apiChecks) {
    try {
      const { res, body } = await fetchJson(path, { method, headers: auth });
      if (res.status === 404) {
        bug('medium', 'API', `${method} ${path} not found`, 'Route missing or wrong path');
      } else if (res.status === 403) {
        bug('low', 'API', `${method} ${path} forbidden`, 'May be role-gated for this user');
      } else if (res.status >= 500) {
        const correlation =
          body && typeof body === 'object' && body.correlationId
            ? ` correlationId=${body.correlationId}`
            : '';
        bug(
          'high',
          'API',
          `${method} ${path} server error`,
          `${res.status}: ${JSON.stringify(body).slice(0, 500)}${correlation}`,
        );
      } else if (!res.ok) {
        bug('medium', 'API', `${method} ${path} unexpected ${res.status}`, JSON.stringify(body).slice(0, 300));
      } else {
        pass(`${method} ${path} → ${res.status}`);
      }
    } catch (e) {
      bug('high', 'API', `${method} ${path} failed`, e.message);
    }
  }

  // Invoice PDF if any invoice exists
  try {
    const { res, body } = await fetchJson('/api/invoices?page=1&pageSize=5', { headers: auth });
    const items = body?.items ?? body?.data ?? (Array.isArray(body) ? body : []);
    const first = items[0];
    const id = first?.id ?? first?.publicId;
    if (id) {
      const pdf = await fetch(`${baseUrl}/api/invoices/${id}/pdf`, { headers: auth });
      const ct = pdf.headers.get('content-type') || '';
      if (!pdf.ok) {
        bug('high', 'Billing', 'Invoice PDF download failed', `${pdf.status} for invoice ${id}`);
      } else if (!ct.includes('pdf')) {
        bug('medium', 'Billing', 'Invoice PDF wrong content-type', ct);
      } else {
        const buf = await pdf.arrayBuffer();
        if (buf.byteLength < 100) {
          bug('high', 'Billing', 'Invoice PDF empty or tiny', `${buf.byteLength} bytes`);
        } else {
          pass(`Invoice PDF ${id} → ${buf.byteLength} bytes`);
        }
      }
    }
  } catch (e) {
    bug('medium', 'Billing', 'Invoice PDF check error', e.message);
  }

  // Security headers on HTML shell
  try {
    const res = await fetch(baseUrl + '/');
    const headers = {
      'strict-transport-security': res.headers.get('strict-transport-security'),
      'x-content-type-options': res.headers.get('x-content-type-options'),
      'x-frame-options': res.headers.get('x-frame-options'),
      'content-security-policy': res.headers.get('content-security-policy'),
    };
    if (!headers['strict-transport-security']) {
      bug('low', 'Security', 'Missing Strict-Transport-Security on SPA', JSON.stringify(headers));
    }
    pass('SPA root loaded: ' + res.status);
  } catch (e) {
    bug('medium', 'Frontend', 'SPA root failed', e.message);
  }

  printReport();
}

function printReport() {
  console.log('\n=== PASSES (' + passes.length + ') ===');
  passes.forEach((p) => console.log('  OK', p));
  console.log('\n=== BUGS / ISSUES (' + bugs.length + ') ===');
  for (const b of bugs) {
    console.log(`\n[${b.severity}] ${b.area}: ${b.title}`);
    console.log('  ', b.detail);
  }
}

main().catch((e) => {
  console.error(e);
  process.exit(1);
});
