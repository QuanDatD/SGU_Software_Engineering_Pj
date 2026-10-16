import { useState } from "react";
import HomePage from "./pages/user/HomePage";
import MapPage from "./pages/user/MapPage";
import "./styles/global.css";

type Page = "home" | "map";

export default function App() {
  const [page, setPage] = useState<Page>("home");
  const [menuOpen, setMenuOpen] = useState(false);

  function changePage(nextPage: Page) {
    setPage(nextPage);
    setMenuOpen(false);
    window.scrollTo({ top: 0 });
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
              <small>Khám phá thiên nhiên</small>
            </span>
          </button>
        </div>

        <span className="language">🇻🇳 Tiếng Việt</span>
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
            <p className="navigation__title">CÙNG KHÁM PHÁ</p>

            <button
              type="button"
              className={page === "home" ? "is-active" : ""}
              aria-current={page === "home" ? "page" : undefined}
              onClick={() => changePage("home")}
            >
              <span aria-hidden="true">⌂</span> Trang chủ
            </button>

            <button
              type="button"
              className={page === "map" ? "is-active" : ""}
              aria-current={page === "map" ? "page" : undefined}
              onClick={() => changePage("map")}
            >
              <span aria-hidden="true">🗺️</span> Bản đồ tham quan
            </button>

            <div className="navigation__note">
              Một hành trình nhỏ,
              <br />
              nhiều điều thú vị.
            </div>
          </nav>
        </>
      )}

      <main>
        {page === "home" ? (
          <HomePage onOpenMap={() => changePage("map")} />
        ) : (
          <MapPage />
        )}
      </main>

      <footer className="footer">
        <strong>🌿 Thảo Cầm Viên</strong>
        <span>Khám phá · Lắng nghe · Yêu thiên nhiên</span>
      </footer>
    </div>
  );
}