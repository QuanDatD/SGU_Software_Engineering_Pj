import { randomUUID } from "node:crypto";
import { expect, type Page } from "@playwright/test";

export async function loginAdmin(page: Page) {
  await page.goto("/admin/login");
  await page.getByLabel("Tên đăng nhập").fill("admin");
  await page.getByLabel("Mật khẩu").fill("Admin@123");
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await expect(page).toHaveURL(/\/admin$/);
}

async function writeHeaders(page: Page) {
  const csrf = await page.request.get("/api/auth/csrf");
  await expect(csrf).toBeOK();
  return { "X-CSRF-TOKEN": (await csrf.json()).token as string };
}

export async function createDraft(page: Page, script = "CI narration", coordinates = { lat: 10.789, lng: 106.706 }) {
  const title = `CI place ${randomUUID()}`;
  const place = await page.request.post("/api/admin/places", {
    multipart: { NameVi: title, Latitude: String(coordinates.lat), Longitude: String(coordinates.lng) },
    headers: await writeHeaders(page),
  });
  await expect(place).toBeOK();
  const { id: placeId } = await place.json() as { id: string };
  const sourceText = `${script} ${randomUUID()}`;
  const draft = await page.request.post(`/api/admin/places/${placeId}/revisions`, {
    data: { sourceText },
    headers: await writeHeaders(page),
  });
  await expect(draft).toBeOK();
  const { id: revisionId } = await draft.json() as { id: string };
  return { placeId, revisionId, sourceText, title };
}

export async function runRevisionAction(page: Page, revisionId: string, action: "generate" | "publish") {
  const headers = await writeHeaders(page);
  const data = action === "publish" ? {
    confirmAllLanguagesReady: true,
    confirmAllAudioValid: true,
    confirmAllJobsCompleted: true,
    confirmAllTranslationsReviewed: true,
  } : undefined;
  return page.request.post(`/api/admin/revisions/${revisionId}/${action}`, { headers, data });
}

export async function openNarrationDraft(page: Page, draft: Awaited<ReturnType<typeof createDraft>>) {
  await page.getByRole("button", { name: /Thuyết minh/ }).click();
  await page.getByLabel("Khu tham quan").selectOption(draft.placeId);
  const card = page.locator(".revision-card").filter({ hasText: draft.sourceText });
  await expect(card).toBeVisible();
  return card;
}

export async function openMapWithoutGps(page: Page) {
  await page.route("https://*.tile.openstreetmap.org/**", route => route.abort());
  await page.goto("/");
  await page.getByRole("radio", { name: /english/i }).check();
  await page.getByRole("button", { name: /continue/i }).click();
  await page.getByRole("button", { name: "Continue without GPS" }).click();
  await expect(page.locator(".live-map")).toBeVisible();
}
