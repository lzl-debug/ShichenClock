// One-shot visual and interaction check. Uses an existing Playwright runtime and installed Edge.
const { chromium } = require(process.env.SHICHEN_PLAYWRIGHT || 'playwright-core');
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { pathToFileURL } = require('node:url');
const root = path.resolve(__dirname, '..');
const output = path.join(require('node:os').homedir(), '.claude', 'e2e-artifacts', 'ShichenClock');
fs.mkdirSync(output, { recursive: true });
(async () => {
  const browser = await chromium.launch({ channel: 'msedge', headless: true });
  const page = await browser.newPage({ viewport: { width: 1280, height: 1000 }, deviceScaleFactor: 1 });
  const errors = [];
  page.on('pageerror', error => errors.push(error.message));
  try {
    console.log('[step] Gallery, all 16 images, modal');
    await page.goto(pathToFileURL(path.join(root, '风格预览.html')).href);
    assert.equal(await page.locator('.card').count(), 16);
    await page.locator('.art img').evaluateAll(async images => {
      for (const img of images) img.loading = 'eager';
      await Promise.all(images.map(img => img.decode()));
    });
    assert.equal(await page.locator('.art img').evaluateAll(images => images.filter(i => i.naturalWidth === 720).length), 16);
    await page.locator('.art').last().click();
    assert.equal(await page.locator('#detail-title').textContent(), '玄枢');
    assert.equal(await page.locator('#detail').evaluate(el => el.open), true);
    await page.keyboard.press('Escape');
    assert.equal(await page.locator('#detail').evaluate(el => el.open), false);
    await page.screenshot({ path: path.join(output, 'gallery.png'), fullPage: true });

    console.log('[step] Interactive dials, boundaries, play/pause');
    await page.goto(pathToFileURL(path.join(root, '动态表盘预览.html')).href);
    assert.equal(await page.locator('#readout').textContent(), '17:38 酉时');
    const expectedBits = [1,3,7,15,31,63,62,60,56,48,32,0];
    for (let index = 0; index < 12; index++) {
      const time = (index * 120 + 1380) % 1440;
      await page.locator('#time').evaluate((el, value) => { el.value = value; el.dispatchEvent(new Event('input', { bubbles: true })); }, time);
      for (const id of ['hands', 'cycle']) {
        assert.equal(await page.locator(`#${id} .hexagram`).count(), 12);
        assert.equal(await page.locator(`#${id} .hexagram[data-current=true]`).count(), 1);
        assert.equal(await page.locator(`#${id} .hexagram[data-current=true]`).getAttribute('data-index'), String(index));
        assert.equal(await page.locator(`#${id} .hexagram[data-current=true]`).getAttribute('data-bits'), String(expectedBits[index]));
        assert.equal(await page.locator(`#${id} .hexagram[data-current=true] [data-yang=true]`).count(), index < 6 ? index + 1 : 11 - index);
        assert.equal(await page.locator(`#${id} .hexagram`).nth(index).locator('[data-line]').first().getAttribute('data-line'), '5');
      }
    }
    for (const [time, label] of [[1379, '22:59 亥时'], [1380, '23:00 子时'], [0, '00:00 子时'], [60, '01:00 丑时']]) {
      await page.locator('#time').evaluate((el, value) => { el.value = value; el.dispatchEvent(new Event('input', { bubbles: true })); }, time);
      assert.equal(await page.locator('#readout').textContent(), label);
      if (time === 1380 || time === 60) assert.match(await page.locator('#cycle').textContent(), /0%/);
    }
    await page.locator('#play').click();
    await page.waitForTimeout(280);
    assert.notEqual(await page.locator('#readout').textContent(), '01:00 丑时');
    await page.locator('#play').click();
    const stopped = await page.locator('#readout').textContent();
    await page.waitForTimeout(100);
    assert.equal(await page.locator('#readout').textContent(), stopped);
    await page.locator('#time').evaluate(el => { el.value = 1058; el.dispatchEvent(new Event('input', { bubbles: true })); });
    await page.screenshot({ path: path.join(output, 'dynamic-dials.png'), fullPage: true });

    console.log('[step] Mobile layout and current-time control');
    await page.setViewportSize({ width: 390, height: 844 });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= window.innerWidth), true);
    await page.locator('#now').click();
    assert.equal(await page.locator('#readout').evaluate(el => /^\d\d:\d\d .时$/.test(el.textContent)), true);
    await page.screenshot({ path: path.join(output, 'dynamic-mobile.png'), fullPage: true });
    console.log('[step] Twelve message hexagrams: every boundary, line order, playback, mobile');
    await page.setViewportSize({ width: 1280, height: 1000 });
    await page.goto(pathToFileURL(path.join(root, '易象预览.html')).href);
    assert.equal(await page.locator('#name').textContent(), '观');
    const hexagrams = ['复','临','泰','大壮','夬','乾','姤','遁','否','观','剥','坤'];
    for (let index = 0; index < 12; index++) {
      await page.locator('#cycle button').nth(index).click();
      assert.equal(await page.locator('#name').textContent(), hexagrams[index]);
      assert.equal(await page.locator('#hex .yao').count(), 6);
      assert.equal(await page.locator('#hex .yang').count(), index < 6 ? index + 1 : 11 - index);
      assert.equal(await page.locator('#cycle button[aria-pressed=true]').count(), 1);
      if (index === 0) assert.match(await page.locator('#hex .yao').last().getAttribute('class'), /yang/);
      if (index === 10) assert.match(await page.locator('#hex .yao').first().getAttribute('class'), /yang/);
      const before = (index * 120 + 1380 + 1439) % 1440;
      await page.locator('#time').evaluate((el, value) => { el.value = value; el.dispatchEvent(new Event('input', { bubbles: true })); }, before);
      assert.equal(await page.locator('#name').textContent(), hexagrams[(index + 11) % 12]);
    }
    await page.locator('#play').click();
    const first = await page.locator('#readout').textContent();
    await page.waitForTimeout(280);
    assert.notEqual(await page.locator('#readout').textContent(), first);
    await page.locator('#play').click();
    const paused = await page.locator('#readout').textContent();
    await page.waitForTimeout(100);
    assert.equal(await page.locator('#readout').textContent(), paused);
    await page.locator('#time').evaluate(el => { el.value = 1058; el.dispatchEvent(new Event('input', { bubbles: true })); });
    await page.screenshot({ path: path.join(output, 'yijing.png'), fullPage: true });
    await page.setViewportSize({ width: 390, height: 844 });
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
    await page.locator('summary').click();
    assert.equal(await page.locator('#table tr').count(), 12);
    assert.equal(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth), true);
    await page.locator('#now').click();
    assert.match(await page.locator('#readout').textContent(), /^\d\d:\d\d .时$/);
    await page.screenshot({ path: path.join(output, 'yijing-mobile.png'), fullPage: true });
    assert.deepEqual(errors, []);
    fs.writeFileSync(path.join(output, 'preview-results.txt'), 'PASS: 16 gallery images, modal, original dials, all 12 message hexagram boundaries and line order, accelerated play/pause, current time, mobile overflow including explanation table, no browser errors.\n');
    console.log('PASS: preview gallery and interactive dials.');
  } finally {
    await browser.close();
  }
})().catch(error => { console.error(error); process.exitCode = 1; });
