/* ======================================================
   Data Store — abstraction layer
   Hiện tại đọc/ghi localStorage + in-memory.
   Sau này thay các hàm này bằng API calls (fetch/axios)
   mà không cần sửa gì ở các component.
   ====================================================== */

import { spots as defaultSpots, type Spot } from "./spots";
import { translations, type Lang, type Translations } from "../i18n";

const SPOTS_KEY   = "tcv_spots";
const CONTENT_KEY = "tcv_content";

/* ---------- Spots ---------- */

export function getSpots(): Spot[] {
  try {
    const raw = localStorage.getItem(SPOTS_KEY);
    if (raw) return JSON.parse(raw) as Spot[];
  } catch { /* ignore */ }
  return defaultSpots;
}

export function saveSpots(spots: Spot[]): void {
  localStorage.setItem(SPOTS_KEY, JSON.stringify(spots));
}

export function resetSpots(): void {
  localStorage.removeItem(SPOTS_KEY);
}

/* ---------- Translations / Content ---------- */

export type ContentStore = Record<Lang, Translations>;

export function getContent(): ContentStore {
  try {
    const raw = localStorage.getItem(CONTENT_KEY);
    if (raw) return JSON.parse(raw) as ContentStore;
  } catch { /* ignore */ }
  // Deep clone để tránh mutate object gốc
  return JSON.parse(JSON.stringify(translations)) as ContentStore;
}

export function saveContent(content: ContentStore): void {
  localStorage.setItem(CONTENT_KEY, JSON.stringify(content));
}

export function resetContent(): void {
  localStorage.removeItem(CONTENT_KEY);
}
