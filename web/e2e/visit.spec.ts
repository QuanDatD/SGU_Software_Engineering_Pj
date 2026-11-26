import { expect, test } from "@playwright/test";
import { openMapWithoutGps } from "./fixtures";

test.describe("Visitor menus on the map", () => {
  test.beforeEach(async ({ page }) => {
    await page.route("**/api/places?*", route => route.fulfill({ json: [] }));
    await page.route("**/api/site-texts?*", route => route.fulfill({ json: {} }));
    await openMapWithoutGps(page);
  });

  test("navigation items can be clicked over the GPS panel", async ({ page }) => {
    await page.getByRole("button", { name: "Mở menu" }).click();
    const navigation = page.getByRole("navigation");
    await navigation.getByRole("button", { name: "Home" }).click();
    await expect(navigation).toBeHidden();
    await expect(page.locator(".live-map")).toBeHidden();
  });

  test("language options can be clicked over the GPS simulator", async ({ page }) => {
    const simulation = page.locator(".live-map__simulation");
    await simulation.locator("summary").click();
    await expect(simulation).toHaveJSProperty("open", true);

    await page.getByRole("button", { name: /English/ }).click();
    await page.getByRole("option", { name: /中文/ }).click();
    await expect(page.getByRole("listbox")).toBeHidden();
    await expect(page.getByRole("button", { name: /中文/ })).toBeVisible();
    await expect(simulation).toHaveJSProperty("open", true);
  });
});

test("TC02/TC03 language selection precedes denied GPS and the map still opens", async ({ page }) => {
  await page.addInitScript(() => {
    navigator.geolocation.watchPosition = (_success, failure) => {
      queueMicrotask(() => failure?.({
        code: 1, message: "Denied by test", PERMISSION_DENIED: 1, POSITION_UNAVAILABLE: 2, TIMEOUT: 3,
      }));
      return 1;
    };
  });
  await page.goto("/");
  await expect(page.getByRole("heading", { name: /choose your language/i })).toBeVisible();
  await expect(page.getByRole("button", { name: /continue/i })).toBeDisabled();
  await expect(page.getByRole("button", { name: "Enable GPS" })).toHaveCount(0);
  await page.getByRole("radio", { name: /english/i }).check();
  await page.getByRole("button", { name: /continue/i }).click();
  await page.getByRole("button", { name: "Enable GPS" }).click();
  await expect(page.getByText("Location access was denied. You can still listen manually.")).toBeVisible();
  await page.getByRole("button", { name: "Continue without GPS" }).click();
  await expect(page.locator(".live-map")).toBeVisible();
});

test("TC04/TC05 QR entry creates a session and reopening the link reuses it", async ({ page }) => {
  const isQrResponse = (url: string) => new URL(url).pathname === "/api/visit/qr";
  const first = page.waitForResponse(async response =>
    isQrResponse(response.url()) && response.ok() && (await response.json()).recorded === true);
  await page.goto("/?source=qr&demo=true");
  const initialPayload = (await first).request().postDataJSON();
  expect(initialPayload.sessionId).toEqual(expect.any(String));
  expect(initialPayload.isDemo).toBe(true);

  const reopenedLink = "/?source=qr&demo=true&entry=repeat";
  const next = page.waitForResponse(response =>
    isQrResponse(response.url()) && response.request().headers()["referer"]?.endsWith(reopenedLink));
  await page.goto(reopenedLink);
  const repeated = await next;
  expect(repeated.status()).toBe(200);
  expect(repeated.request().postDataJSON().sessionId).toBe(initialPayload.sessionId);
  expect(await repeated.json()).toMatchObject({ recorded: false });
});
