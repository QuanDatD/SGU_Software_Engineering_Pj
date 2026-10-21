import { useState } from "react";
import { useAuth } from "../../contexts/AuthContext";
import ProtectedRoute from "../../components/ProtectedRoute";
import SpotsAdmin    from "./SpotsAdmin";
import NarrationAdmin from "./NarrationAdmin";
import MediaAdmin    from "./MediaAdmin";
import "../../styles/AdminPage.css";

type Tab = "spots" | "narration" | "media";

const TABS: { id: Tab; label: string; icon: string }[] = [
  { id: "spots",      label: "Điểm tham quan", icon: "📍" },
  { id: "narration",  label: "Thuyết minh",    icon: "🎙️" },
  { id: "media",      label: "Media",          icon: "🖼️"  },
];

export default function AdminPage() {
  return (
    <ProtectedRoute>
      <AdminLayout />
    </ProtectedRoute>
  );
}

function AdminLayout() {
  const { user, logout } = useAuth();
  const [tab, setTab] = useState<Tab>("spots");

  async function handleLogout() {
    await logout();
    window.location.replace("/admin/login");
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
              <span>{t.icon}</span>{t.label}
            </button>
          ))}
        </nav>

        <div className="admin-sidebar__footer">
          <div className="admin-user">
            <span>👤</span>
            <span>{user?.username}</span>
          </div>
          <button className="admin-btn admin-btn--full admin-btn--ghost" onClick={handleLogout}>
            ↩ Đăng xuất
          </button>
          <a className="admin-btn admin-btn--full admin-btn--ghost" href="/">
            ← Về trang người dùng
          </a>
        </div>
      </aside>

      {/* Main */}
      <main className="admin-main">
        <div className="admin-topbar">
          <h1>{TABS.find((t) => t.id === tab)?.label}</h1>
        </div>

        {tab === "spots"     && <SpotsAdmin />}
        {tab === "narration" && <NarrationAdmin />}
        {tab === "media"     && <MediaAdmin />}
      </main>
    </div>
  );
}
