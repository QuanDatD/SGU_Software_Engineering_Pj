import { useState } from "react";
import HomePage from "./pages/user/HomePage";
import MapPage  from "./pages/user/MapPage";
import QrPage from "./pages/user/QrPage";
import { useLang } from "./contexts/LanguageContext";
import LanguageSelector from "./components/LanguageSelector";
import { useQrVisit } from "./hooks/useQrVisit";
import "./styles/global.css";

type Page = "home" | "map" | "qr";

export default function App() {
  // UC01.3 — ghi nhận lượt truy cập qua QR ngay khi app mount
  useQrVisit();
  const [page,     setPage]     = useState<Page>("home");
  const [menuOpen, setMenuOpen] = useState(false);
  const { lang } = useLang();

  function changePage(next: Page) {
    setPage(next);
    setMenuOpen(false);
    window.scrollTo({ top: 0 });
  }

  const NAV = {
    explore: { vi: "CÙNG KHÁM PHÁ", en: "EXPLORE", zh: "一起探索", ko: "함께 탐험", ja: "一緒に探検" },
    home:    { vi: "Trang chủ",     en: "Home",    zh: "主页",    ko: "홈",      ja: "ホーム" },
    map:     { vi: "Bản đồ",        en: "Map",     zh: "地图",    ko: "지도",    ja: "マップ" },
    note:    { vi: "Một hành trình nhỏ,\nnhiều điều thú vị.", en: "A small journey,\nmany great discoveries.", zh: "小小旅程，\n大大发现。", ko: "작은 여정,\n큰 발견.", ja: "小さな旅、\n大きな発見。" },
    tagline: { vi: "Khám phá thiên nhiên", en: "Explore nature", zh: "探索自然", ko: "자연 탐험", ja: "自然を探検" },
    footer:  { vi: "Khám phá · Lắng nghe · Yêu thiên nhiên", en: "Explore · Listen · Love Nature", zh: "探索 · 倾听 · 热爱自然", ko: "탐험 · 듣기 · 자연 사랑", ja: "探検 · 聞く · 自然愛" },
  };
  const t = (k: keyof typeof NAV) => NAV[k]?.[lang] ?? NAV[k]?.["en"] ?? "";

  return (
    <div className="app">
      <header className="header">
        <div className="header__left">
          <button
            className="menu-button"
            type="button"
            aria-label={menuOpen ? "Đóng menu" : "Mở menu"}
            aria-expanded={menuOpen}
            aria-controls="navigation"
            onClick={() => setMenuOpen(!menuOpen)}
          >
            {menuOpen ? "✕" : "☰"}
          </button>

          <button className="brand" type="button" onClick={() => changePage("home")}>
            <span className="brand__icon" aria-hidden="true">🌿</span>
            <span>
              <strong>THẢO CẦM VIÊN</strong>
              <small>{t("tagline")}</small>
            </span>
          </button>
        </div>

        <LanguageSelector />
      </header>

      {menuOpen && (
        <>
          <button
            className="menu-overlay"
            type="button"
            aria-label="Đóng menu"
            onClick={() => setMenuOpen(false)}
          />
          <nav
            id="navigation"
            className="navigation"
            aria-label="Điều hướng chính"
            onKeyDown={(e) => { if (e.key === "Escape") setMenuOpen(false); }}
          >
            <p className="navigation__title">{t("explore")}</p>

            <button
              type="button"
              className={page === "home" ? "is-active" : ""}
              aria-current={page === "home" ? "page" : undefined}
              onClick={() => changePage("home")}
            >
              <span aria-hidden="true">⌂</span> {t("home")}
            </button>

            <button
              type="button"
              className={page === "map" ? "is-active" : ""}
              aria-current={page === "map" ? "page" : undefined}
              onClick={() => changePage("map")}
            >
              <span aria-hidden="true">🗺️</span> {t("map")}
            </button>

            <div className="navigation__note">
              {t("note").split("\n").map((line, i) => (
                <span key={i}>{line}{i === 0 && <br />}</span>
              ))}
            </div>
          </nav>
        </>
      )}
      <button
     type="button"
      className={page === "qr" ? "is-active" : ""}
      aria-current={page === "qr" ? "page" : undefined}
      onClick={() => changePage("qr")}
    >
  <span aria-hidden="true">📱</span> Mã QR truy cập
</button>
      <main>
         {page === "home" && (
          <HomePage onOpenMap={() => changePage("map")} />
          )}

      {page === "map" && <MapPage />}

          {page === "qr" && <QrPage />}
    </main>

      <footer className="footer">
        <strong>🌿 Thảo Cầm Viên</strong>
        <span>{t("footer")}</span>
        <a
          href="/admin/login"
          style={{ fontSize: 12, color: "rgba(255,255,255,0.35)", textDecoration: "none" }}
          aria-label="Admin"
        >
          ⚙️
        </a>
      </footer>
    </div>
  );
}
