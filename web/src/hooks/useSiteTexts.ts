import { useEffect, useState } from "react";
import type { Lang } from "../types/common";
import { getMapTexts } from "../data/mapTexts";

const BASE = (import.meta.env.VITE_API_BASE ?? "").replace(/\/$/, "");

const FALLBACK: Record<Lang, Record<string, string>> = {
    vi: {
        "app.title": "Thảo Cầm Viên Sài Gòn",
        "nav.map": "Bản đồ",
        "nav.home": "Trang chủ",
    },
    en: {
        "app.title": "Saigon Zoo Audio Guide",
        "nav.map": "Map",
        "nav.home": "Home",
    },
    zh: {
        "app.title": "西贡动植物园语音导览",
        "nav.map": "地图",
        "nav.home": "主页",
    },
    ko: {
        "app.title": "사이공 동물원 오디오 가이드",
        "nav.map": "지도",
        "nav.home": "홈",
    },
    ja: {
        "app.title": "サイゴン動植物園オーディオガイド",
        "nav.map": "マップ",
        "nav.home": "ホーム",
    },
};

const cache: Partial<Record<Lang, Record<string, string>>> = {};

function fallbackFor(lang: Lang): Record<string, string> {
    return {
        ...FALLBACK[lang],
        ...getMapTexts(lang),
    };
}

export function useSiteTexts(lang: Lang): Record<string, string> {
    const [loaded, setLoaded] = useState<{
        lang: Lang;
        texts: Record<string, string>;
    }>(() => ({
        lang,
        texts: cache[lang] ?? fallbackFor(lang),
    }));

    useEffect(() => {
        const cached = cache[lang];

        if (cached) {
            setLoaded({ lang, texts: cached });
            return;
        }

        const controller = new AbortController();
        let cancelled = false;

        async function loadTexts() {
            try {
                const response = await fetch(
                    `${BASE}/api/site-texts?lang=${encodeURIComponent(lang)}`,
                    {
                        credentials: "include",
                        signal: controller.signal,
                    },
                );

                if (!response.ok) {
                    throw new Error(`SiteTexts HTTP ${response.status}`);
                }

                const data =
                    (await response.json()) as Record<string, string>;

                if (cancelled) return;

                const merged = {
                    ...fallbackFor(lang),
                    ...data,
                };

                cache[lang] = merged;
                setLoaded({ lang, texts: merged });
            } catch {
                if (cancelled) return;

                setLoaded({
                    lang,
                    texts: fallbackFor(lang),
                });
            }
        }

        void loadTexts();

        return () => {
            cancelled = true;
            controller.abort();
        };
    }, [lang]);

    // Đổi ngôn ngữ thì dùng ngay nhãn tương ứng,
    // không hiển thị nhãn của ngôn ngữ cũ trong lúc chờ API.
    return loaded.lang === lang
        ? loaded.texts
        : cache[lang] ?? fallbackFor(lang);
}