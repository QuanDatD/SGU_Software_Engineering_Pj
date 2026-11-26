import { expect, test, type BrowserContext, type Page } from "@playwright/test";
import { createDraft, loginAdmin, openMapWithoutGps, runRevisionAction } from "./fixtures";

type Coordinates = { lat: number; lng: number };
declare global {
  interface Window {
    emitGps: (position: Coordinates, accuracy: number, ageMs: number) => void;
    gpsStarts: number;
  }
}
const evaluationPath = "**/api/visit/evaluate";
const gps = (page: Page) => page.locator(".live-map__gps");
const player = (page: Page) => page.locator(".audio-player");
const autoplay = (page: Page) => page.getByRole("checkbox", { name: "Play automatically on entry" });
const evaluation = (url: string) => new URL(url).pathname === "/api/visit/evaluate";

async function publishPlaces(context: BrowserContext, ...positions: Coordinates[]) {
  const admin = await context.newPage();
  await loginAdmin(admin);
  try {
    const places = [];
    for (const position of positions) {
      const draft = await createDraft(admin, "Visitor regression narration", position);
      await expect(await runRevisionAction(admin, draft.revisionId, "generate")).toBeOK();
      await expect(await runRevisionAction(admin, draft.revisionId, "publish")).toBeOK();
      places.push(draft);
    }
    return places;
  } finally {
    await admin.close();
  }
}

async function openVisit(page: Page) {
  await page.clock.install();
  await page.addInitScript(() => {
    let watch: PositionCallback | undefined;
    let refresh: number | undefined;
    window.gpsStarts = 0;
    navigator.geolocation.watchPosition = success => {
      window.gpsStarts++;
      watch = success;
      return window.gpsStarts;
    };
    navigator.geolocation.clearWatch = () => {
      watch = undefined;
      window.clearInterval(refresh);
    };
    window.emitGps = (position, accuracy, ageMs) => {
      window.clearInterval(refresh);
      const send = () => watch?.({
        timestamp: Date.now() - ageMs,
        coords: {
          latitude: position.lat, longitude: position.lng, accuracy,
          altitude: null, altitudeAccuracy: null, heading: null, speed: null,
          toJSON: () => ({}),
        },
        toJSON: () => ({}),
      });
      send();
      refresh = window.setInterval(send, 1000);
    };
  });
  await openMapWithoutGps(page);
}

async function emit(page: Page, position: Coordinates, accuracy = 5, ageMs = 0) {
  await page.evaluate(([value, error, age]) => window.emitGps(value, error, age),
    [position, accuracy, ageMs] as const);
  await expect(gps(page).getByText(`${position.lat.toFixed(6)}, ${position.lng.toFixed(6)}`, { exact: false })).toBeVisible();
}

async function advanceEvaluation(page: Page, expectedPlace: string | null) {
  const pending = page.waitForResponse(response => evaluation(response.url()) && response.request().method() === "POST");
  await page.clock.runFor(5100);
  const response = await pending;
  expect(response.ok()).toBe(true);
  expect(await response.json()).toMatchObject({ accepted: true, placeId: expectedPlace });
  await afterPaint(page);
}

async function afterPaint(page: Page) {
  await page.evaluate(() => new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve()))));
}

async function selectPlace(page: Page, title: string) {
  await page.locator(".spot-marker").getByText(title, { exact: false }).click();
}

test("TC08/TC11 waits for two matching evaluations, prevents interruption and does not repeat", async ({ page, context }) => {
  test.setTimeout(60_000);
  const first = { lat: 10.787, lng: 106.704 };
  const next = { lat: 10.787, lng: 106.707 };
  const [a, b] = await publishPlaces(context, first, next);
  await openVisit(page);
  await autoplay(page).check();
  await emit(page, first);

  await advanceEvaluation(page, a.placeId);
  await expect(gps(page)).toContainText("Visited: 0");
  await expect(player(page)).toHaveCount(0);
  await advanceEvaluation(page, a.placeId);
  await expect(player(page).getByRole("button", { name: "Pause", exact: true })).toBeEnabled();
  await expect(player(page)).toContainText(a.title);

  await emit(page, next);
  await advanceEvaluation(page, b.placeId);
  await advanceEvaluation(page, b.placeId);
  await expect(gps(page)).toContainText("Visited: 2");
  await expect(player(page)).toContainText(a.title);
  await player(page).getByRole("button", { name: "Pause", exact: true }).click();
  await advanceEvaluation(page, b.placeId);
  await expect(player(page)).toContainText(a.title);
  await expect(player(page).getByRole("button", { name: "Play", exact: true })).toBeVisible();

  await player(page).getByRole("button", { name: "Dừng" }).click();
  await advanceEvaluation(page, b.placeId);
  await expect(player(page)).toContainText(b.title);
  await expect(gps(page)).toContainText("Listened: 2");
  await player(page).getByRole("button", { name: "Dừng" }).click();
  await advanceEvaluation(page, b.placeId);
  await expect(player(page)).toHaveCount(0);
  await expect(gps(page)).toContainText("Listened: 2");
});

test("TC11 a delayed audio download cannot start after leaving its zone", async ({ page, context }) => {
  const position = { lat: 10.790, lng: 106.703 };
  const [place] = await publishPlaces(context, position);
  await openVisit(page);
  const audioPath = `**/api/places/${place.placeId}/audio/en?*`;
  const gate: { release?: () => void } = {};
  await page.route(audioPath, async route => {
    const response = await route.fetch();
    await new Promise<void>(resolve => { gate.release = resolve; });
    await route.fulfill({ response });
  });
  await autoplay(page).check();
  await emit(page, position);
  await advanceEvaluation(page, place.placeId);
  await advanceEvaluation(page, place.placeId);
  await expect.poll(() => Boolean(gate.release)).toBe(true);

  await emit(page, { lat: 10.795, lng: 106.695 });
  const downloaded = page.waitForResponse(response => response.url().includes(`/api/places/${place.placeId}/audio/en`));
  gate.release?.();
  expect((await downloaded).ok()).toBe(true);
  await expect(player(page)).toHaveCount(0);
  await expect(gps(page)).toContainText("Listened: 0");
});

test("TC09 rejects stale and inaccurate browser fixes before evaluating a zone", async ({ page, context }) => {
  const position = { lat: 10.789, lng: 106.703 };
  const [place] = await publishPlaces(context, position);
  await openVisit(page);
  const evaluations: string[] = [];
  page.on("request", request => { if (evaluation(request.url())) evaluations.push(request.url()); });
  await autoplay(page).check();

  await emit(page, position, 5, 16_000);
  await page.clock.runFor(5100);
  expect(evaluations).toHaveLength(0);
  await emit(page, position, 26);
  await page.clock.runFor(5100);
  expect(evaluations).toHaveLength(0);
  await expect(player(page)).toHaveCount(0);

  await emit(page, position, 25);
  await advanceEvaluation(page, place.placeId);
  await advanceEvaluation(page, place.placeId);
  await expect(player(page)).toContainText(place.title);
});

test("TC12 missing selected language disables manual playback and prevents automatic fallback", async ({ page, context }) => {
  const position = { lat: 10.789, lng: 106.708 };
  const [place] = await publishPlaces(context, position);
  const audioRequests: string[] = [];
  page.on("request", request => { if (request.url().includes("/audio/")) audioRequests.push(request.url()); });
  await page.route("**/api/places?lang=en", async route => {
    const response = await route.fetch();
    const places = await response.json() as { id: string; audioUrl: Record<string, string | null> }[];
    const target = places.find(item => item.id === place.placeId);
    expect(target?.audioUrl.vi).toBeTruthy();
    if (target) target.audioUrl.en = null;
    await route.fulfill({ response, json: places });
  });

  await openVisit(page);
  await autoplay(page).check();
  await emit(page, position);
  await advanceEvaluation(page, place.placeId);
  await advanceEvaluation(page, place.placeId);
  await expect(gps(page)).toContainText("Visited: 1");
  await expect(player(page)).toHaveCount(0);
  await selectPlace(page, place.title);
  await expect(page.getByRole("button", { name: "Audio unavailable in this language" })).toBeDisabled();
  expect(audioRequests).toHaveLength(0);
});

test("TC13/TC14 manual listening pauses, resumes at the same time and can replay from the start", async ({ page, context }) => {
  const [place] = await publishPlaces(context, { lat: 10.788, lng: 106.702 });
  await openVisit(page);
  await selectPlace(page, place.title);
  await page.getByRole("button", { name: /Listen$/ }).click();
  const controls = player(page);
  const slider = controls.getByRole("slider", { name: "Seek" });
  const time = async () => Number(await slider.inputValue());
  await expect.poll(time).toBeGreaterThan(0.5);

  await controls.getByRole("button", { name: "Pause", exact: true }).click();
  await expect(controls.getByRole("button", { name: "Play", exact: true })).toBeVisible();
  const pausedAt = await time();
  await afterPaint(page);
  expect(await time()).toBe(pausedAt);
  await controls.getByRole("button", { name: "Play", exact: true }).click();
  await expect(controls.getByRole("button", { name: "Pause", exact: true })).toBeVisible();
  expect(await time()).toBeGreaterThanOrEqual(pausedAt);
  await expect.poll(time).toBeGreaterThan(pausedAt);

  await controls.getByRole("button", { name: "Replay from start" }).click();
  await expect.poll(time).toBeLessThan(pausedAt);
  await expect(controls.getByRole("button", { name: "Pause", exact: true })).toBeVisible();
  await expect(gps(page)).toContainText("Listened: 1");
});

test("TC15 changing language stops the old audio and keeps position, selection and history", async ({ page, context }) => {
  const position = { lat: 10.790, lng: 106.705 };
  const [place] = await publishPlaces(context, position);
  await openVisit(page);
  await autoplay(page).check();
  await emit(page, position);
  await advanceEvaluation(page, place.placeId);
  await advanceEvaluation(page, place.placeId);
  await expect(gps(page)).toContainText("Visited: 1");
  await expect(gps(page)).toContainText("Listened: 1");
  await expect(player(page)).toContainText(place.title);
  await page.evaluate(() => { document.documentElement.dataset.ciVisit = "same-document"; });

  await page.getByRole("button", { name: /English/ }).click();
  await page.getByRole("option", { name: /Tiếng Việt/ }).click();
  await expect(player(page)).toHaveCount(0);
  await expect(gps(page)).toContainText("Đã tới: 1");
  await expect(gps(page)).toContainText("Đã nghe: 1");
  await expect(gps(page)).toContainText("10.790000, 106.705000");
  await expect(page.locator(".live-map__detail").getByRole("heading", { name: place.title })).toBeVisible();
  expect(await page.evaluate(() => document.documentElement.dataset.ciVisit)).toBe("same-document");

  const vietnameseAudio = page.waitForResponse(response => response.url().includes(`/audio/vi?revisionId=${place.revisionId}`));
  await page.getByRole("button", { name: /Nghe thuyết minh/ }).click();
  expect((await vietnameseAudio).ok()).toBe(true);
});

test("TC34 Restart stops audio, resets only demo history and returns to the chosen start", async ({ page, context }) => {
  test.setTimeout(75_000);
  const real = { lat: 10.7868, lng: 106.7054 };
  const demo = { lat: 10.7892, lng: 106.7070 };
  const [realPlace, demoPlace] = await publishPlaces(context, real, demo);
  await openVisit(page);
  await autoplay(page).check();
  await emit(page, real);
  await advanceEvaluation(page, realPlace.placeId);
  await advanceEvaluation(page, realPlace.placeId);
  await expect(gps(page)).toContainText("Visited: 1");
  await expect(gps(page)).toContainText("Listened: 1");

  await page.getByText("GPS simulation", { exact: false }).click();
  await page.getByRole("combobox", { name: /^Start/ }).selectOption(demoPlace.placeId);
  await page.getByRole("combobox", { name: /^Destination/ }).selectOption(realPlace.placeId);
  await page.getByRole("combobox", { name: /^Speed/ }).selectOption("1");
  const firstDemoEvaluation = page.waitForResponse(response => evaluation(response.url()));
  await page.getByRole("button", { name: "Start simulation" }).click();
  expect(await (await firstDemoEvaluation).json()).toMatchObject({ placeId: demoPlace.placeId });
  await expect(gps(page)).toContainText("Visited: 0");
  await expect(gps(page)).toContainText("Listened: 0");
  await autoplay(page).check();
  await advanceEvaluation(page, demoPlace.placeId);
  await advanceEvaluation(page, demoPlace.placeId);
  await expect(page.getByRole("progressbar")).not.toHaveJSProperty("value", 0);
  await expect(gps(page)).toContainText("Visited: 1", { timeout: 12_000 });
  await expect(gps(page)).toContainText("Listened: 1");
  await expect(player(page)).toContainText(demoPlace.title);

  await page.getByRole("button", { name: "Pause movement" }).click();
  await expect(player(page).getByRole("button", { name: "Pause", exact: true })).toBeVisible();
  await page.getByRole("button", { name: "Restart simulation" }).click();
  await expect(page.getByRole("progressbar")).toHaveJSProperty("value", 0);
  await expect(gps(page)).toContainText(`${demo.lat.toFixed(6)}, ${demo.lng.toFixed(6)}`);
  await expect(player(page)).toHaveCount(0);
  await expect(gps(page)).toContainText("Visited: 0");
  await expect(gps(page)).toContainText("Listened: 0");
  await expect(autoplay(page)).toBeChecked();

  await page.getByRole("button", { name: "Stop simulation" }).click();
  await expect(gps(page)).toContainText("Visited: 1");
  await expect(gps(page)).toContainText("Listened: 1");
  expect(await page.evaluate(() => window.gpsStarts)).toBe(1);
});

test("TC34 ignores a delayed zone reply across Restart and exit", async ({ page, context }) => {
  test.setTimeout(60_000);
  const start = { lat: 10.788, lng: 106.709 };
  const end = { lat: 10.78807, lng: 106.709 };
  const [from, to] = await publishPlaces(context, start, end);
  await openVisit(page);
  await page.getByText("GPS simulation", { exact: false }).click();
  await page.getByRole("combobox", { name: /^Start/ }).selectOption(from.placeId);
  await page.getByRole("combobox", { name: /^Destination/ }).selectOption(to.placeId);
  await page.getByRole("combobox", { name: /^Speed/ }).selectOption("1");

  let hold = false;
  const responseGate: { release?: () => void } = {};
  const releaseResponse = () => responseGate.release?.();
  await page.route(evaluationPath, async route => {
    if (!hold) return route.continue();
    hold = false;
    const response = await route.fetch();
    await new Promise<void>(resolve => { responseGate.release = resolve; });
    await route.fulfill({ response, headers: { ...response.headers(), "x-ci-delayed": "1" } });
  });

  const first = page.waitForResponse(response => evaluation(response.url()));
  await page.getByRole("button", { name: "Start simulation" }).click();
  expect((await first).ok()).toBe(true);
  // Freeze the route while it is still in the starting place.
  await page.getByRole("button", { name: "Pause movement" }).click();
  await autoplay(page).check();
  hold = true;
  await expect.poll(async () => {
    await page.clock.runFor(1000);
    return Boolean(responseGate.release);
  }).toBe(true);

  // A fresh reply for the same place must be only the first confirmation in the new run.
  const fresh = page.waitForResponse(response => evaluation(response.url()));
  await page.getByRole("button", { name: "Restart simulation" }).click();
  expect(await (await fresh).json()).toMatchObject({ placeId: from.placeId });
  await page.getByRole("button", { name: "Pause movement" }).click();
  const delayed = page.waitForResponse(response => response.headers()["x-ci-delayed"] === "1");
  releaseResponse();
  expect(await (await delayed).json()).toMatchObject({ placeId: from.placeId });
  await afterPaint(page);
  await expect(player(page)).toHaveCount(0);
  await expect(gps(page)).toContainText("Visited: 0");

  await advanceEvaluation(page, from.placeId);
  await expect(player(page)).toContainText(from.title);
  await expect(gps(page)).toContainText("Listened: 1");
  await afterPaint(page);

  hold = true;
  delete responseGate.release;
  await expect.poll(async () => {
    await page.clock.runFor(1000);
    return Boolean(responseGate.release);
  }).toBe(true);
  await page.getByRole("button", { name: "Stop simulation" }).click();
  const afterExit = page.waitForResponse(response => response.headers()["x-ci-delayed"] === "1");
  releaseResponse();
  expect((await afterExit).ok()).toBe(true);
  await afterPaint(page);
  await expect(player(page)).toHaveCount(0);
  await expect(gps(page)).toContainText("Visited: 0");
  expect(await page.evaluate(() => window.gpsStarts)).toBe(0);
});
