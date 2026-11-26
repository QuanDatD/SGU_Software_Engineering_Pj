import { randomUUID } from "node:crypto";
import { expect, test } from "@playwright/test";
import { loginAdmin } from "./fixtures";

test("UC07.1 report keeps the applied dates and chart until Apply", async ({ page }) => {
  await loginAdmin(page);
  const visit = await page.request.post("/api/visit/qr", {
    data: { sessionId: randomUUID(), isDemo: false },
  });
  await expect(visit).toBeOK();

  const isReport = (url: string) => new URL(url).pathname === "/api/admin/reporting";
  await page.getByRole("button", { name: /Báo cáo/ }).click();
  const from = page.getByLabel("Từ ngày", { exact: true });
  const to = page.getByLabel(/Đến ngày/);
  const apply = page.getByRole("button", { name: "Áp dụng", exact: true });
  const originalFrom = await from.inputValue();
  const originalTo = await to.inputValue();
  const originalLabel = page.getByText(`${originalFrom} → ${originalTo}`, { exact: true });
  const emptyChart = page.getByText("Không có lượt truy cập trong khoảng thời gian đã chọn.", { exact: true });
  const chart = page.getByRole("heading", { name: "Chi tiết theo giờ" });
  await apply.click();
  await expect(originalLabel).toBeVisible();
  await expect(chart).toBeVisible();

  const draftRequests: string[] = [];
  page.on("request", request => {
    if (isReport(request.url())) draftRequests.push(request.url());
  });
  await from.fill("2000-01-01");
  await to.fill("2000-01-02");
  await page.evaluate(() => new Promise<void>(resolve => requestAnimationFrame(() => requestAnimationFrame(() => resolve()))));
  expect.soft(draftRequests, "Draft dates must not request report data").toHaveLength(0);
  await expect.soft(originalLabel).toBeVisible({ timeout: 1500 });
  await expect.soft(chart).toBeVisible({ timeout: 1500 });

  const applied = page.waitForResponse(response => {
    const url = new URL(response.url());
    return isReport(response.url()) && url.searchParams.get("from") === "2000-01-01"
      && url.searchParams.get("to") === "2000-01-03";
  });
  await apply.click();
  const updated = await applied;
  expect(updated.ok()).toBe(true);
  expect(await updated.json()).toMatchObject({ totalVisits: 0, peakHours: [] });
  await expect(page.getByText("2000-01-01 → 2000-01-02", { exact: true })).toBeVisible();
  await expect(emptyChart).toBeVisible();
});
