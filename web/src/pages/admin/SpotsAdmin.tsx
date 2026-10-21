import { useEffect, useState } from "react";
import { placeApi } from "../../services/placeApi";
import type { Place } from "../../types/place";

const EMPTY: Omit<Place, "id"> = {
  slug: "", icon: "📍", category: "facility",
  lat: 10.7875, lng: 106.705, radiusMeters: 30,
  mapX: 50, mapY: 50, imageUrl: null,
  name: { vi: "", en: "", zh: "", ko: "", ja: "" },
  desc: { vi: "", en: "", zh: "", ko: "", ja: "" },
};

const CAT_LABEL: Record<Place["category"], string> = {
  animal: "🐘 Động vật", plant: "🌳 Thực vật", facility: "🏛️ Tiện ích",
};
const LANGS = ["vi", "en", "zh", "ko", "ja"];

export default function SpotsAdmin() {
  const [places,  setPlaces]  = useState<Place[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);
  const [editing, setEditing] = useState<Partial<Place> & { id?: number } | null>(null);
  const [saving,  setSaving]  = useState(false);
  const [confirm, setConfirm] = useState<number | null>(null);

  useEffect(() => {
    placeApi.list()
      .then(setPlaces)
      .catch(() => setError("Không tải được dữ liệu. Kiểm tra kết nối backend."))
      .finally(() => setLoading(false));
  }, []);

  async function handleSave() {
    if (!editing) return;
    setSaving(true);
    try {
      if (editing.id) {
        const updated = await placeApi.update(editing.id, editing);
        setPlaces((prev) => prev.map((p) => p.id === updated.id ? updated : p));
      } else {
        const created = await placeApi.create(editing);
        setPlaces((prev) => [...prev, created]);
      }
      setEditing(null);
    } catch {
      alert("Lưu thất bại. Vui lòng thử lại.");
    } finally {
      setSaving(false);
    }
  }

  async function handleDelete(id: number) {
    try {
      await placeApi.remove(id);
      setPlaces((prev) => prev.filter((p) => p.id !== id));
    } catch {
      alert("Xoá thất bại.");
    }
    setConfirm(null);
  }

  function set<K extends keyof Place>(key: K, value: Place[K]) {
    setEditing((prev) => prev ? { ...prev, [key]: value } : prev);
  }

  function setLang(field: "name" | "desc", lang: string, value: string) {
    setEditing((prev) =>
      prev ? { ...prev, [field]: { ...(prev[field] ?? {}), [lang]: value } } : prev
    );
  }

  if (loading) return <div className="admin-section"><p>⏳ Đang tải…</p></div>;

  if (error) return (
    <div className="admin-section">
      <div className="admin-notice admin-notice--warning">
        <span>⚠️</span><div><strong>Chưa kết nối backend</strong><p>{error}</p></div>
      </div>
      <p style={{ color: "#627568", fontSize: 14 }}>
        Giao diện admin sẽ hoạt động đầy đủ sau khi backend C# được khởi động.
      </p>
    </div>
  );

  return (
    <div className="admin-section">
      <div className="admin-section__header">
        <h2>Điểm tham quan ({places.length})</h2>
        <button className="admin-btn admin-btn--primary" onClick={() => setEditing({ ...EMPTY })}>
          + Thêm điểm mới
        </button>
      </div>

      {/* Table */}
      <div className="spots-table">
        <div className="spots-table__head">
          <span>Icon</span><span>Tên (VI)</span><span>Tên (EN)</span>
          <span>Loại</span><span>Bán kính</span><span></span>
        </div>
        {places.map((p) => (
          <div className="spots-table__row" key={p.id}>
            <span className="spots-table__icon">{p.icon}</span>
            <span>{p.name?.vi}</span>
            <span>{p.name?.en}</span>
            <span><span className={`admin-tag admin-tag--${p.category}`}>{CAT_LABEL[p.category]}</span></span>
            <span className="spots-table__coords">{p.radiusMeters}m</span>
            <span className="spots-table__actions">
              <button className="admin-btn admin-btn--sm" onClick={() => setEditing({ ...p })}>Sửa</button>
              <button className="admin-btn admin-btn--sm admin-btn--danger" onClick={() => setConfirm(p.id)}>Xoá</button>
            </span>
          </div>
        ))}
      </div>

      {/* Confirm xoá */}
      {confirm !== null && (
        <div className="admin-modal-overlay">
          <div className="admin-modal">
            <h3>Xác nhận xoá</h3>
            <p>Xoá <strong>{places.find(p => p.id === confirm)?.name?.vi}</strong>?</p>
            <div className="admin-modal__actions">
              <button className="admin-btn" onClick={() => setConfirm(null)}>Huỷ</button>
              <button className="admin-btn admin-btn--danger" onClick={() => handleDelete(confirm!)}>Xoá</button>
            </div>
          </div>
        </div>
      )}

      {/* Form */}
      {editing && (
        <div className="admin-modal-overlay">
          <div className="admin-modal admin-modal--wide">
            <h3>{editing.id ? "Sửa điểm tham quan" : "Thêm điểm tham quan"}</h3>
            <div className="admin-form">
              {/* Icon, Loại, Slug */}
              <div className="admin-form__row">
                <label className="admin-form__field">
                  <span>Icon (emoji)</span>
                  <input value={editing.icon ?? ""} onChange={(e) => set("icon", e.target.value)} maxLength={4} />
                </label>
                <label className="admin-form__field">
                  <span>Loại</span>
                  <select value={editing.category ?? "facility"} onChange={(e) => set("category", e.target.value as Place["category"])}>
                    {(["animal","plant","facility"] as Place["category"][]).map(c =>
                      <option key={c} value={c}>{CAT_LABEL[c]}</option>
                    )}
                  </select>
                </label>
                <label className="admin-form__field">
                  <span>Slug (mã khu)</span>
                  <input value={editing.slug ?? ""} onChange={(e) => set("slug", e.target.value)} placeholder="elephant" />
                </label>
              </div>

              {/* Tên theo ngôn ngữ */}
              <p className="admin-form__section-label">Tên khu</p>
              <div className="admin-form__row">
                {LANGS.map((l) => (
                  <label key={l} className="admin-form__field">
                    <span>{l.toUpperCase()}</span>
                    <input value={(editing.name as Record<string,string>)?.[l] ?? ""}
                      onChange={(e) => setLang("name", l, e.target.value)} />
                  </label>
                ))}
              </div>

              {/* Mô tả ngắn */}
              <p className="admin-form__section-label">Mô tả ngắn</p>
              <div className="admin-form__row">
                {LANGS.map((l) => (
                  <label key={l} className="admin-form__field">
                    <span>{l.toUpperCase()}</span>
                    <textarea rows={2} value={(editing.desc as Record<string,string>)?.[l] ?? ""}
                      onChange={(e) => setLang("desc", l, e.target.value)} />
                  </label>
                ))}
              </div>

              {/* Tọa độ & vùng */}
              <div className="admin-form__row">
                <label className="admin-form__field">
                  <span>Vĩ độ (lat)</span>
                  <input type="number" step="0.00001" value={editing.lat ?? 0}
                    onChange={(e) => set("lat", parseFloat(e.target.value))} />
                </label>
                <label className="admin-form__field">
                  <span>Kinh độ (lng)</span>
                  <input type="number" step="0.00001" value={editing.lng ?? 0}
                    onChange={(e) => set("lng", parseFloat(e.target.value))} />
                </label>
                <label className="admin-form__field">
                  <span>Bán kính vùng (mét)</span>
                  <input type="number" min={5} max={200} value={editing.radiusMeters ?? 30}
                    onChange={(e) => set("radiusMeters", parseInt(e.target.value))} />
                </label>
              </div>

              {/* Vị trí trên sơ đồ ảnh */}
              <div className="admin-form__row">
                <label className="admin-form__field">
                  <span>Vị trí X trên ảnh (%)</span>
                  <input type="number" min={0} max={100} value={editing.mapX ?? 50}
                    onChange={(e) => set("mapX", parseFloat(e.target.value))} />
                </label>
                <label className="admin-form__field">
                  <span>Vị trí Y trên ảnh (%)</span>
                  <input type="number" min={0} max={100} value={editing.mapY ?? 50}
                    onChange={(e) => set("mapY", parseFloat(e.target.value))} />
                </label>
                <label className="admin-form__field">
                  <span>URL ảnh</span>
                  <input value={editing.imageUrl ?? ""} onChange={(e) => set("imageUrl", e.target.value || null)}
                    placeholder="/images/spots/elephant.jpg" />
                </label>
              </div>
            </div>

            <div className="admin-modal__actions">
              <button className="admin-btn" onClick={() => setEditing(null)}>Huỷ</button>
              <button className="admin-btn admin-btn--primary" onClick={handleSave} disabled={saving}>
                {saving ? "Đang lưu…" : "Lưu"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
