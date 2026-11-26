import { expect, test } from "@playwright/test";

test("BR20 only entry marked source=qr sends a QR event to the API", async ({ page }) => {
  const isQr = (url: string) => new URL(url).pathname === "/api/visit/qr";
  const events: string[] = [];
  page.on("request", request => {
    if (isQr(request.url())) events.push(request.url());
  });
  await page.goto("/?source=organic");
  await expect(page.getByRole("heading", { name: /choose your language/i })).toBeVisible();
  await page.evaluate(() => new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve()))));
  expect(events).toHaveLength(0);

  const event = page.waitForResponse(async response =>
    isQr(response.url()) && response.ok() && (await response.json()).recorded === true);
  await page.goto("/?source=qr");
  const payload = (await event).request().postDataJSON();
  expect(payload).toMatchObject({ sessionId: expect.any(String), isDemo: false });
});
