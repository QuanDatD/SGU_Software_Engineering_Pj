import { createContext, useContext, useState, type ReactNode } from "react";
import type { Lang } from "../types/common";

interface LanguageCtx {
  lang: Lang;
  setLang: (l: Lang) => void;
}

const Ctx = createContext<LanguageCtx | null>(null);

export function LanguageProvider({ children }: { children: ReactNode }) {
  const [lang, setLangState] = useState<Lang>(() => {
    try {
        const stored = localStorage.getItem("lang");

        if (
            stored === "vi" ||
            stored === "en" ||
            stored === "zh" ||
            stored === "ko" ||
            stored === "ja"
        ) {
            return stored;
        }
    } catch {
        // Không đọc được storage thì dùng tiếng Việt.
    }

    return "vi";
});

  function setLang(l: Lang) {
    setLangState(l);

    try {
        localStorage.setItem("lang", l);
    } catch {
        // Vẫn sử dụng ngôn ngữ trong bộ nhớ nếu trình duyệt chặn lưu.
    }
}

  return <Ctx.Provider value={{ lang, setLang }}>{children}</Ctx.Provider>;
}

export function useLang() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("useLang must be used inside LanguageProvider");
  return ctx;
}
