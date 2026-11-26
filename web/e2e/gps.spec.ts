import { expect, test } from "@playwright/test";
import { createDraft, loginAdmin, openMapWithoutGps, runRevisionAction } from "./fixtures";

test("TC07 first valid location is brought into the map viewport", async ({ page, context }) => {
  await context.grantPermissions(["geolocation"]);
  await context.setGeolocation({ latitude: 10.797, longitude: 106.715, accuracy: 5 });
  await openMapWithoutGps(page);

  await page.getByRole("button", { name: /my location/i }).click();
  await expect(page.getByText("10.797000, 106.715000")).toBeVisible();
  const canvas = page.locator(".live-map__canvas");
  const marker = canvas.locator('.leaflet-overlay-pane path[fill="#2563eb"][stroke="white"]');
  await expect.poll(async () => {
    const map = await canvas.boundingBox();
    const dot = await marker.boundingBox();
    if (!map || !dot || dot.width === 0 || dot.height === 0) return false;
    const x = dot.x + dot.width / 2;
    const y = dot.y + dot.height / 2;
    return x > map.x && x < map.x + map.width && y > map.y && y < map.y + map.height;
  }, { message: "The first valid GPS marker should enter the map viewport after one click" }).toBe(true);
});

test("TC34 stopping and starting simulation resets route progress without requesting GPS", async ({ page, context }) => {
  const admin = await context.newPage();
  await loginAdmin(admin);
  const draft = await createDraft(admin);
  await expect(await runRevisionAction(admin, draft.revisionId, "generate")).toBeOK();
  await expect(await runRevisionAction(admin, draft.revisionId, "publish")).toBeOK();
  await admin.close();

  await page.addInitScript(() => {
    window.sessionStorage.setItem("ci-gps-watches", "0");
    navigator.geolocation.watchPosition = () => {
      const watches = Number(window.sessionStorage.getItem("ci-gps-watches"));
      window.sessionStorage.setItem("ci-gps-watches", String(watches + 1));
      return 1;
    };
  });
  await openMapWithoutGps(page);
  await page.getByText("GPS simulation", { exact: false }).click();
  await page.getByRole("combobox", { name: /^Destination/ }).selectOption(draft.placeId);
  const start = page.getByRole("button", { name: "Start simulation" });
  const stop = page.getByRole("button", { name: "Stop simulation" });
  const progress = page.getByRole("progressbar");
  const routeProgress = () => progress.evaluate((element: HTMLProgressElement) => element.value);
  await start.click();
  await expect.poll(routeProgress).toBeGreaterThan(0);
  await page.getByRole("button", { name: "Pause movement" }).click();
  await expect(page.getByRole("button", { name: "Resume movement" })).toBeVisible();
  await stop.click();
  await expect(progress).toHaveCount(0);

  await start.click();
  await expect(progress).toHaveJSProperty("value", 0);
  await stop.click();
  await expect(start).toBeVisible();
  expect(await page.evaluate(() => sessionStorage.getItem("ci-gps-watches"))).toBe("0");
});
