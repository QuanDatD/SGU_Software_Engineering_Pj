import { expect, test, type Dialog } from "@playwright/test";
import { createDraft, loginAdmin, openNarrationDraft, runRevisionAction } from "./fixtures";

test("UC06.5 Admin can save and reopen a manually edited translation", async ({ page }) => {
  await loginAdmin(page);
  const draft = await createDraft(page);
  const card = await openNarrationDraft(page, draft);
  await card.getByRole("button", { name: /\bEN\b/ }).click();
  await page.locator(".admin-modal").getByRole("textbox").fill("Manually edited English narration");
  await page.getByRole("button", { name: "Lưu", exact: true }).click();
  await expect(page.getByRole("heading", { name: /Sửa bản dịch/ })).toHaveCount(0);

  await page.reload();
  const persisted = await openNarrationDraft(page, draft);
  await persisted.getByRole("button", { name: /\bEN\b/ }).click();
  await expect(page.locator(".admin-modal").getByRole("textbox")).toHaveValue("Manually edited English narration");
});

test("UC06.4/UC06.6 Admin sees job results, previews audio and retries failed English", async ({ page }) => {
  await loginAdmin(page);
  const draft = await createDraft(page, "CI retry fixture");
  const card = await openNarrationDraft(page, draft);
  await card.getByRole("button", { name: /Generate audio/ }).click();

  const failed = card.locator(".job-item").filter({ hasText: /\bEN\b/ });
  const ready = card.locator(".job-item").filter({ hasText: /\bVI\b/ });
  await expect(failed.getByText(/Lỗi/)).toBeVisible();
  await expect(ready.getByText(/Xong/)).toBeVisible();
  const preview = ready.locator("audio[controls]");
  await expect(preview).toHaveAttribute("src", new RegExp(`${draft.revisionId}/audio/vi`));
  await preview.evaluate(async (audio: HTMLAudioElement) => {
    await audio.play();
  });
  await expect.poll(() => preview.evaluate((audio: HTMLAudioElement) => audio.currentTime)).toBeGreaterThan(0);

  const list = await page.request.get(`/api/admin/revisions/${draft.revisionId}/jobs`);
  await expect(list).toBeOK();
  const jobs = await list.json() as { lang: string; id: string }[];
  const englishJob = jobs.find(job => job.lang === "en");
  expect(englishJob).toBeDefined();
  const retry = page.waitForResponse(response =>
    response.request().method() === "POST" && new URL(response.url()).pathname === `/api/admin/jobs/${englishJob?.id}/retry`);
  await failed.getByRole("button", { name: "Thử lại" }).click();
  expect((await retry).ok()).toBe(true);
  await expect(failed.getByText(/Xong/)).toBeVisible();
  await expect(ready.getByText(/Xong/)).toBeVisible();
});

test("UC06.7 cancelling publication keeps the draft and confirmation publishes", async ({ page }) => {
  await loginAdmin(page);
  const draft = await createDraft(page);
  await expect(await runRevisionAction(page, draft.revisionId, "generate")).toBeOK();
  const card = await openNarrationDraft(page, draft);
  const publish = card.getByRole("button", { name: /Xuất bản/ });
  const confirmDialog = page.getByRole("dialog").filter({ hasText: /công bố|xuất bản/i });
  const publicPlace = `/api/places/${draft.placeId}`;
  const publishPath = `/api/admin/revisions/${draft.revisionId}/publish`;
  const confirmation: { nativeDialog?: Dialog } = {};
  const currentNativeDialog = () => confirmation.nativeDialog;
  let publishRequests = 0;
  page.on("dialog", dialog => { confirmation.nativeDialog = dialog; });
  page.on("request", request => {
    if (request.method() === "POST" && new URL(request.url()).pathname === publishPath) publishRequests++;
  });
  const waitForDecision = () => expect.poll(async () =>
    Boolean(currentNativeDialog()) || await confirmDialog.isVisible() || publishRequests > 0,
  ).toBe(true);

  const openConfirmation = publish.click({ noWaitAfter: true });
  await waitForDecision();
  expect(publishRequests, "Publish must wait for confirmation").toBe(0);
  const cancellationDialog = currentNativeDialog();
  if (cancellationDialog) {
    await cancellationDialog.dismiss();
    confirmation.nativeDialog = undefined;
  } else {
    await confirmDialog.getByRole("button", { name: /Hủy|Huỷ/ }).click();
  }
  await openConfirmation;
  expect((await page.request.get(publicPlace)).status()).toBe(404);
  await expect(card.getByText(/Bản nháp/)).toBeVisible();

  const reopenConfirmation = publish.click({ noWaitAfter: true });
  await waitForDecision();
  const acceptanceDialog = currentNativeDialog();
  if (acceptanceDialog) {
    await acceptanceDialog.accept();
  } else {
    await confirmDialog.getByRole("button", { name: /Xác nhận|Công bố|Xuất bản/ }).click();
  }
  await reopenConfirmation;
  await expect(card.getByText(/Đã xuất bản/)).toBeVisible();
  const publicRevision = await page.request.get(publicPlace);
  await expect(publicRevision).toBeOK();
  expect(await publicRevision.json()).toMatchObject({ revisionId: draft.revisionId });
});
