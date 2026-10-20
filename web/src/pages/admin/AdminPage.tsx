import { useState, useCallback } from "react";
import {
  getSpots, saveSpots, resetSpots,
  getContent, saveContent, resetContent,
} from "../../data/store";
import type { Spot } from "../../data/spots";
import type { ContentStore } from "../../data/store";
import SpotsAdmin   from "./SpotsAdmin";
import ContentAdmin from "./ContentAdmin";
import MediaAdmin   from "./MediaAdmin";
import "../../styles/AdminPage.css";

type Tab = "spots" | "content" | "media";

const TABS: { id: Tab; label: string; icon: string }[] = [
  { id: "spots",   label: "Điểm tham quan", icon: "📍" },
  { id: "content", label: "Nội dung",        icon: "✏️" },
  { id: "media",   label: "Media",           icon: "🖼️" },
];

export default function AdminPage() {
  const [tab,     setTab]     = useState<Tab>("spots");
  const [spots,   setSpots]   = useState<Spot[]>(getSpots);
  const [content, setContent] = useState<ContentStore>(getContent);
  const [saved,   setSaved]   = useState(false);

  const handleSpotsChange = useCallback((s: Spot[]) => setSpots(s), []);
  const handleContentChange = useCallback((c: ContentStore) => setContent(c), []);

  function handleSaveAll() {
    saveSpots(spots);
    saveContent(content);
    setSaved(true);
    setTimeout(() => setSaved(false), 2500);
  }

  function handleResetAll() {
    if (!confirm("Đặt lại toàn bộ dữ liệu về mặc định?")) return;
    resetSpots();
    resetContent();
    setSpots(getSpots());
    setContent(getContent());
  }

  return (
    <div className="admin-layout">
      {/* Sidebar */}
      <aside className="admin-sidebar">
        <div className="admin-sidebar__brand">
          <span>🌿</span>
          <div>
            <strong>THẢO CẦM VIÊN</strong>
            <small>Admin Panel</small>
          </div>
        </div>

        <nav className="admin-sidebar__nav">
          {TABS.map((t) => (
            <button
              key={t.id}
              className={`admin-nav-btn${tab === t.id ? " is-active" : ""}`}
              onClick={() => setTab(t.id)}
            >
              <span>{t.icon}</span>
              {t.label}
            </button>
          ))}
        </nav>

        <div className="admin-sidebar__footer">
          <button className="admin-btn admin-btn--primary admin-btn--full" onClick={handleSaveAll}>
            💾 Lưu tất cả
          </button>
          <button className="admin-btn admin-btn--full admin-btn--ghost" onClick={handleResetAll}>
            ↺ Đặt lại mặc định
          </button>
          <a className="admin-btn admin-btn--full admin-btn--ghost" href="/">
            ← Về trang người dùng
          </a>
        </div>
      </aside>

      {/* Main content */}
      <main className="admin-main">
        {/* Topbar */}
        <div className="admin-topbar">
          <h1>{TABS.find((t) => t.id === tab)?.label}</h1>
          {saved && (
            <div className="admin-toast">✅ Đã lưu thành công!</div>
          )}
        </div>

        {/* Tab content */}
        {tab === "spots"   && <SpotsAdmin   spots={spots}     onChange={handleSpotsChange}   />}
        {tab === "content" && <ContentAdmin content={content} onChange={handleContentChange} />}
        {tab === "media"   && <MediaAdmin   spots={spots} />}
      </main>
    </div>
  );
}
