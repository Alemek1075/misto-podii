const { test, expect } = require('@playwright/test');

test('catalog, event page, and SEO endpoints', async ({ page, request }) => {
  await page.goto('/');
  await expect(page.getByRole('heading', { name: 'Знайдіть свою подію' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Майстерня міських плакатів', exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Майстерня міських плакатів', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Майстерня міських плакатів' })).toBeVisible();
  await expect(page.getByLabel('Контакт організатора')).toContainText('hello@example.org');
  await expect(page.locator('link[rel="canonical"]')).toHaveAttribute('href', /\/Events\/Details\/1$/);
  expect((await request.get('/robots.txt')).status()).toBe(200);
  expect((await request.get('/sitemap.xml')).status()).toBe(200);
});

test('map filter changes points, chart summary, and event list', async ({ page }) => {
  await page.goto('/Map');
  await expect(page.getByRole('status')).toContainText('Показано подій: 2');
  await page.getByLabel('Категорія', { exact: true }).selectOption({ label: 'Музика' });
  await expect(page.getByRole('status')).toContainText('Показано подій: 1');
  await expect(page.locator('#category-summary')).toContainText('Музика: 1');
  await expect(page.locator('#category-summary')).not.toContainText('Майстерня');
  await expect(page.locator('#map-event-list')).toContainText('Музика у вечірньому саду');
  await expect(page.locator('#map-event-list')).not.toContainText('Майстерня міських плакатів');
});

test('MVC CRUD, autocomplete, and live status update', async ({ page, browser }) => {
  const suffix = Date.now().toString(36);
  const organizer = `E2E Organizer ${suffix}`;
  const venue = `E2E Venue ${suffix}`;
  const event = `E2E Event ${suffix}`;

  await page.goto('/Organizers/Create');
  await page.getByLabel('Назва').fill(organizer);
  await page.getByLabel('Опис').fill('Temporary end-to-end organizer');
  await page.getByLabel('Контакт').fill('e2e@example.org');
  await page.getByRole('button', { name: 'Зберегти' }).click();
  await expect(page.getByRole('heading', { name: organizer })).toBeVisible();
  const organizerId = Number(new URL(page.url()).pathname.split('/').pop());

  await page.goto('/Venues/Create');
  await page.getByLabel('Назва').fill(venue);
  await page.getByLabel('Адреса').fill('Test address');
  await page.getByLabel('Широта').fill('50.45');
  await page.getByLabel('Довгота').fill('30.52');
  await page.getByLabel('Місткість').fill('25');
  await page.getByRole('button', { name: 'Зберегти' }).click();
  await expect(page.getByRole('heading', { name: venue })).toBeVisible();
  const venueId = Number(new URL(page.url()).pathname.split('/').pop());

  await page.goto('/Events/Create');
  await page.getByLabel('Назва').fill(event);
  await page.getByLabel('Опис').fill('Temporary end-to-end event');
  await page.getByLabel('Категорія').fill('Test');
  await page.getByLabel('Початок (UTC)').fill('2030-01-01T15:00');
  await page.getByLabel('Зображення (URL)').fill('/images/posters.svg');
  await page.getByLabel('Організатор').selectOption(String(organizerId));
  await page.getByRole('combobox', { name: 'Місце' }).fill(venue.slice(0, 6));
  await page.getByRole('option', { name: venue }).click();
  await page.getByRole('button', { name: 'Зберегти' }).click();
  await expect(page.getByRole('heading', { name: event })).toBeVisible();
  const eventId = Number(new URL(page.url()).pathname.split('/').pop());

  const livePage = await browser.newPage();
  await livePage.goto('/Live');
  await expect(livePage.locator('#live-connection')).toHaveText('З’єднано');
  const liveRow = livePage.locator('.live-row').filter({ hasText: event });
  await expect(liveRow.locator('.event-status')).toHaveText('Заплановано');
  await page.goto(`/Events/Edit/${eventId}`);
  await page.getByLabel('Статус').selectOption({ label: 'Перенесено' });
  await page.getByRole('button', { name: 'Зберегти' }).click();
  await expect(page.getByRole('heading', { name: event })).toBeVisible();
  await expect(liveRow.locator('.event-status')).toHaveText('Перенесено');
  await livePage.close();

  await page.goto(`/Events/Delete/${eventId}`);
  await page.getByRole('button', { name: 'Видалити' }).click();
  await expect(page.getByRole('heading', { name: 'Події' })).toBeVisible();
  await page.goto(`/Venues/Delete/${venueId}`);
  await page.getByRole('button', { name: 'Видалити' }).click();
  await page.goto(`/Organizers/Delete/${organizerId}`);
  await page.getByRole('button', { name: 'Видалити' }).click();
  await expect(page.getByRole('heading', { name: 'Організатори' })).toBeVisible();
});
