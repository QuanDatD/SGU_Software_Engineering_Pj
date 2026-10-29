import { useEffect, useState } from "react";
import type { Lang } from "../types/common";

const BASE = (import.meta.env.VITE_API_BASE ?? "").replace(/\/$/, "");

// Fallback tối thiểu khi API không khả dụng
const FALLBACK: Record<string, Record<string, string>> = {
  vi: { "app.title": "Thảo Cầm Viên Sài Gòn", "nav.map": "Bản đồ", "nav.home": "Trang chủ" },
  en: { "app.title": "Saigon Zoo Audio Guide",  "nav.map": "Map",    "nav.home": "Home" },
  zh: { "app.title": "西贡动植物园语音导览",          "nav.map": "地图",   "nav.home": "主页" },
  ko: { "app.title": "사이공 동물원 오디오 가이드",     "nav.map": "지도",   "nav.home": "홈" },
  ja: { "app.title": "サイゴン動植物園オーディオガイド", "nav.map": "マップ", "nav.home": "ホーム" },
};

const cache: Partial<Record<Lang, Record<string, string>>> = {};

/**
 * Lấy nhãn giao diện từ GET /api/site-texts?lang=
 * BR-28: SiteTexts lưu text theo key/ngôn ngữ trong DB.
 * Trả fallback tối thiểu nếu API chưa sẵn sàng.
 */
export function useSiteTexts(lang: Lang): Record<string, string> {
  const [texts, setTexts] = useState<Record<string, string>>(
    cache[lang] ?? FALLBACK[lang] ?? {}
  );

  useEffect(() => {
    // Đã cache rồi — không fetch lại
    if (cache[lang]) { setTexts(cache[lang]!); return; }

    let cancelled = false;
    fetch(`${BASE}/api/site-texts?lang=${encodeURIComponent(lang)}`, {
      credentials: "include",
      cache: "default",
    })
      .then(r => r.ok ? r.json() as Promise<Record<string, string>> : Promise.reject())
      .then(data => {
        if (cancelled) return;
        // Merge với fallback để đảm bảo luôn có đủ key tối thiểu
        const merged = { ...(FALLBACK[lang] ?? {}), ...data };
        cache[lang] = merged;
        setTexts(merged);
      })
      .catch(() => {
        // Giữ fallback, không crash
        if (!cancelled) setTexts(FALLBACK[lang] ?? {});
      });

    return () => { cancelled = true; };
  }, [lang]);

  return texts;
}
