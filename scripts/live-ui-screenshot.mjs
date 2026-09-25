import { chromium } from 'playwright';
import crypto from 'node:crypto';

const baseUrl = process.argv[2].replace(/\/$/, '');
const email = process.argv[3];
const password = process.argv[4];
const route = process.argv[5] || '/billing';

async function login(request) {
  const key = await (await request.get(`${baseUrl}/api/auth/login-key`)).json();
  const keyObject = crypto.createPublicKey({
    key: { kty: 'RSA', n: key.n, e: key.e, alg: 'RSA-OAEP-256' },
    format: 'jwk',
  });
  const passwordCipher =
    'enc:' +
    crypto
      .publicEncrypt(
        { key: keyObject, padding: crypto.constants.RSA_PKCS1_OAEP_PADDING, oaepHash: 'sha256' },
        Buffer.from(password),
      )
      .toString('base64');
  const res = await request.post(`${baseUrl}/api/auth/login`, { data: { email, passwordCipher } });
  return res.json();
}

const browser = await chromium.launch({ headless: true, channel: 'msedge' });
const context = await browser.newContext({ baseURL: baseUrl });
const auth = await login(context.request);
const page = await context.newPage();
await page.addInitScript((user) => localStorage.setItem('carehome.auth', JSON.stringify(user)), auth);
await page.goto(route, { waitUntil: 'networkidle', timeout: 60000 });
const text = await page.locator('body').innerText();
console.log('URL', page.url());
console.log('BODY SNIPPET', text.slice(0, 500));
await browser.close();
