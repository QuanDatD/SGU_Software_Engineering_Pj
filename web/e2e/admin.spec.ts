import { expect, test } from "@playwright/test";

test("TC16/TC35 protected admin redirects and valid seed login opens the dashboard", async ({ page }) => {
  await page.goto("/admin");
  await expect(page).toHaveURL(/\/admin\/login$/);
  await page.getByLabel("Tên đăng nhập").fill("admin");
  await page.getByLabel("Mật khẩu").fill("incorrect");
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await expect(page.getByText("Sai tên đăng nhập hoặc mật khẩu.")).toBeVisible();
  await page.getByLabel("Mật khẩu").fill("Admin@123");
  await page.getByRole("button", { name: "Đăng nhập" }).click();
  await expect(page).toHaveURL(/\/admin$/);
  const session = await page.request.get("/api/auth/me");
  expect(session.status()).toBe(200);
  expect(await session.json()).toMatchObject({ username: "admin", role: "Admin" });
});
