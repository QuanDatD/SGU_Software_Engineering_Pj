import { useState } from "react";
import HomePage from "./pages/user/HomePage";
import MapPage from "./pages/user/MapPage";
import { type Lang, translations } from "./i18n";
import "./styles/global.css";

type Page = "home" | "map";

export default function App() {
  const [page, setPage] = useState<Page>("home");
  const [menuOpen, setMenuOpen] = useState(false);
  const [lang, setLang] = useState<Lang>("vi");
  const [langOpen, setLangOpen] = useState(false);

  const t = translations[lang];

  function changePage(nextPage: Page) {
    setPage(nextPage);
    setMenuOpen(false);
    window.scrollTo({ top: 0 });
  }

  function chooseLang(l: Lang) {
    setLang(l);
    setLangOpen(false);
  }

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

          <button
            className="brand"
            type="button"
            onClick={() => changePage("home")}
          >
            <span className="brand__icon" aria-hidden="true">🌿</span>
            <span>
              <strong>THẢO CẦM VIÊN</strong>
              <small>{t.brandTagline}</small>
            </span>
          </button>
        </div>

        {/* Nút chọn ngôn ngữ */}
        <div className="lang-picker">
          <button
            className="lang-picker__btn"
            type="button"
            aria-haspopup="listbox"
            aria-expanded={langOpen}
            onClick={() => setLangOpen(!langOpen)}
          >
            {t.langLabel}
            <span className="lang-picker__arrow" aria-hidden="true">
              {langOpen ? "▲" : "▼"}
            </span>
          </button>

          {langOpen && (
            <>
              <button
                className="lang-picker__overlay"
                type="button"
                aria-label="Đóng menu ngôn ngữ"
                onClick={() => setLangOpen(false)}
              />
              <ul className="lang-picker__menu" role="listbox">
                <li>
                  <button
                    type="button"
                    role="option"
                    aria-selected={lang === "vi"}
                    className={lang === "vi" ? "is-active" : ""}
                    onClick={() => chooseLang("vi")}
                  >
                    🇻🇳 Tiếng Việt
                  </button>
                </li>
                <li>
                  <button
                    type="button"
                    role="option"
                    aria-selected={lang === "en"}
                    className={lang === "en" ? "is-active" : ""}
                    onClick={() => chooseLang("en")}
                  >
                    🇬🇧 English
                  </button>
                </li>
              </ul>
            </>
          )}
        </div>
      </header>

      {menuOpen && (
        <>
          <button
            className="menu-overlay"
            type="button"
            aria-label="Đóng menu điều hướng"
            onClick={() => setMenuOpen(false)}
          />

          <nav
            id="navigation"
            className="navigation"
            aria-label="Điều hướng chính"
            onKeyDown={(event) => {
              if (event.key === "Escape") setMenuOpen(false);
            }}
          >
            <p className="navigation__title">{t.navExplore}</p>

            <button
              type="button"
              className={page === "home" ? "is-active" : ""}
              aria-current={page === "home" ? "page" : undefined}
              onClick={() => changePage("home")}
            >
              <span aria-hidden="true">⌂</span> {t.navHome}
            </button>

            <button
              type="button"
              className={page === "map" ? "is-active" : ""}
              aria-current={page === "map" ? "page" : undefined}
              onClick={() => changePage("map")}
            >
              <span aria-hidden="true">🗺️</span> {t.navMap}
            </button>

            <div className="navigation__note">
              {t.navNote.split("\n").map((line, i) => (
                <span key={i}>{line}{i === 0 && <br />}</span>
              ))}
            </div>
          </nav>
        </>
      )}

      <main>
        {page === "home" ? (
          <HomePage onOpenMap={() => changePage("map")} t={t} />
        ) : (
          <MapPage t={t} lang={lang} />
        )}
      </main>

      <footer className="footer">
        <strong>🌿 Thảo Cầm Viên</strong>
        <span>{t.footerTagline}</span>
      </footer>
    </div>
  );
}
