const { chromium } = require('playwright');

(async () => {
  const browser = await chromium.launch({ headless: true });
  const ctx = await browser.newContext({ viewport: { width: 1440, height: 900 } });
  const dir = '/home/cikreddy/myworld/projects/github-personal/piperag/screenshots';
  
  // Landing page
  let page = await ctx.newPage();
  await page.goto('http://localhost:4200', { waitUntil: 'networkidle', timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(1000);
  await page.screenshot({ path: `${dir}/01-landing.png`, fullPage: true });
  console.log('✅ Landing page');

  // Login page
  await page.goto('http://localhost:4200/login', { waitUntil: 'networkidle', timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(500);
  await page.screenshot({ path: `${dir}/02-login.png` });
  console.log('✅ Login page');

  // Register page
  await page.goto('http://localhost:4200/register', { waitUntil: 'networkidle', timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(500);
  await page.screenshot({ path: `${dir}/03-register.png` });
  console.log('✅ Register page');

  // Register a user and login via API to get token
  const resp = await page.request.post('http://localhost:5000/api/auth/register', {
    data: { email: `screenshot_${Date.now()}@test.com`, password: 'Test1234!', displayName: 'Screenshot User' }
  });
  const { accessToken } = await resp.json();
  
  // Set token in localStorage and navigate to dashboard
  await page.goto('http://localhost:4200/login', { waitUntil: 'networkidle' }).catch(() => {});
  await page.evaluate((token) => {
    localStorage.setItem('access_token', token);
  }, accessToken);
  
  await page.goto('http://localhost:4200/dashboard', { waitUntil: 'networkidle', timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(1000);
  await page.screenshot({ path: `${dir}/04-dashboard.png` });
  console.log('✅ Dashboard');

  // Create a project via API
  const projResp = await page.request.post('http://localhost:5000/api/projects', {
    headers: { Authorization: `Bearer ${accessToken}` },
    data: { name: 'Screenshot Project', description: 'For screenshots' }
  });
  const proj = await projResp.json();
  
  // Pipeline builder
  await page.goto(`http://localhost:4200/projects/${proj.id}/pipelines`, { waitUntil: 'networkidle', timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(1000);
  await page.screenshot({ path: `${dir}/05-pipeline-builder.png` });
  console.log('✅ Pipeline builder');

  // Chat
  await page.goto(`http://localhost:4200/projects/${proj.id}/chat`, { waitUntil: 'networkidle', timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(1000);
  await page.screenshot({ path: `${dir}/06-chat.png` });
  console.log('✅ Chat');

  // Billing
  await page.goto('http://localhost:4200/billing', { waitUntil: 'networkidle', timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(1000);
  await page.screenshot({ path: `${dir}/07-billing.png` });
  console.log('✅ Billing');

  // Widget config
  await page.goto(`http://localhost:4200/projects/${proj.id}/widget`, { waitUntil: 'networkidle', timeout: 10000 }).catch(() => {});
  await page.waitForTimeout(1000);
  await page.screenshot({ path: `${dir}/08-widget-config.png` });
  console.log('✅ Widget config');

  await browser.close();
  console.log('\nAll screenshots saved to:', dir);
})();
