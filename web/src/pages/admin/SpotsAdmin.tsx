import { useState } from "react";
import { type Spot } from "../../data/spots";

type Props = {
  spots: Spot[];
  onChange: (spots: Spot[]) => void;
};

const EMPTY_SPOT: Omit<Spot, "id"> = {
  coords:   [10.7875, 106.705],
  icon:     "📍",
  category: "facility",
  img:      "",
  name:     { vi: "", en: "" },
  desc:     { vi: "", en: "" },
  details:  { vi: "", en: "" },
};

type FormSpot = Spot;

function generateId(name: string): string {
  return name.toLowerCase().replace(/\s+/g, "-").replace(/[^a-z0-9-]/g, "") || `spot-${Date.now()}`;
}

export default function SpotsAdmin({ spots, onChange }: Props) {
  const [editing, setEditing] = useState<FormSpot | null>(null);
  const [isNew,   setIsNew]   = useState(false);
  const [confirm, setConfirm] = useState<string | null>(null); // id cần xoá

  function openNew() {
    setEditing({ id: "", ...EMPTY_SPOT });
    setIsNew(true);
  }

  function openEdit(spot: Spot) {
    setEditing({ ...spot });
    setIsNew(false);
  }

  function handleSave() {
    if (!editing) return;
    const spot = { ...editing };
    if (isNew) {
      spot.id = generateId(spot.name.vi || spot.name.en);
      onChange([...spots, spot]);
    } else {
      onChange(spots.map((s) => (s.id === spot.id ? spot : s)));
    }
    setEditing(null);
  }

  function handleDelete(id: string) {
    onChange(spots.filter((s) => s.id !== id));
    setConfirm(null);
  }

  function set<K extends keyof FormSpot>(key: K, value: FormSpot[K]) {
    setEditing((prev) => prev ? { ...prev, [key]: value } : prev);
  }

  function setNested(key: "name" | "desc" | "details", lang: "vi" | "en", value: string) {
    setEditing((prev) =>
      prev ? { ...prev, [key]: { ...prev[key], [lang]: value } } : prev
    );
  }

  const CATEGORIES: Spot["category"][] = ["animal", "plant", "facility"];
  const CAT_LABEL: Record<Spot["category"], string> = {
    animal: "🐘 Động vật", plant: "🌳 Thực vật", facility: "🏛️ Tiện ích",
  };

  return (
    <div className="admin-section">
      <div className="admin-section__header">
        <h2>Điểm tham quan</h2>
        <button className="admin-btn admin-btn--primary" onClick={openNew}>
          + Thêm điểm mới
        </button>
      </div>

      {/* Danh sách */}
      <div className="spots-table">
        <div className="spots-table__head">
          <span>Icon</span>
          <span>Tên (VI)</span>
          <span>Tên (EN)</span>
          <span>Loại</span>
          <span>Tọa độ</span>
          <span></span>
        </div>

        {spots.map((s) => (
          <div className="spots-table__row" key={s.id}>
            <span className="spots-table__icon">{s.icon}</span>
            <span>{s.name.vi}</span>
            <span>{s.name.en}</span>
            <span><span className={`admin-tag admin-tag--${s.category}`}>{CAT_LABEL[s.category]}</span></span>
            <span className="spots-table__coords">
              {s.coords[0].toFixed(4)}, {s.coords[1].toFixed(4)}
            </span>
            <span className="spots-table__actions">
              <button className="admin-btn admin-btn--sm" onClick={() => openEdit(s)}>Sửa</button>
              <button className="admin-btn admin-btn--sm admin-btn--danger" onClick={() => setConfirm(s.id)}>Xoá</button>
            </span>
          </div>
        ))}
      </div>

      {/* Confirm xoá */}
      {confirm && (
        <div className="admin-modal-overlay">
          <div className="admin-modal">
            <h3>Xác nhận xoá</h3>
            <p>Bạn chắc chắn muốn xoá điểm <strong>{spots.find(s => s.id === confirm)?.name.vi}</strong>?</p>
            <div className="admin-modal__actions">
              <button className="admin-btn" onClick={() => setConfirm(null)}>Huỷ</button>
              <button className="admin-btn admin-btn--danger" onClick={() => handleDelete(confirm)}>Xoá</button>
            </div>
          </div>
        </div>
      )}

      {/* Form thêm/sửa */}
      {editing && (
        <div className="admin-modal-overlay">
          <div className="admin-modal admin-modal--wide">
            <h3>{isNew ? "Thêm điểm tham quan" : `Sửa: ${editing.name.vi}`}</h3>

            <div className="admin-form">
              {/* Icon & Loại */}
              <div className="admin-form__row">
                <label className="admin-form__field">
                  <span>Icon (emoji)</span>
                  <input value={editing.icon} onChange={(e) => set("icon", e.target.value)} maxLength={4} />
                </label>
                <label className="admin-form__field">
                  <span>Loại</span>
                  <select value={editing.category} onChange={(e) => set("category", e.target.value as Spot["category"])}>
                    {CATEGORIES.map((c) => <option key={c} value={c}>{CAT_LABEL[c]}</option>)}
                  </select>
                </label>
              </div>

              {/* Tên */}
              <div className="admin-form__row">
                <label className="admin-form__field">
                  <span>Tên tiếng Việt</span>
                  <input value={editing.name.vi} onChange={(e) => setNested("name", "vi", e.target.value)} />
                </label>
                <label className="admin-form__field">
                  <span>Tên tiếng Anh</span>
                  <input value={editing.name.en} onChange={(e) => setNested("name", "en", e.target.value)} />
                </label>
              </div>

              {/* Ảnh */}
              <label className="admin-form__field">
                <span>Đường dẫn ảnh (ví dụ: /images/spots/elephant.jpg)</span>
                <input value={editing.img} onChange={(e) => set("img", e.target.value)} placeholder="/images/..." />
              </label>

              {/* Tọa độ */}
              <div className="admin-form__row">
                <label className="admin-form__field">
                  <span>Vĩ độ (Latitude)</span>
                  <input type="number" step="0.00001" value={editing.coords[0]}
                    onChange={(e) => set("coords", [parseFloat(e.target.value), editing.coords[1]])} />
                </label>
                <label className="admin-form__field">
                  <span>Kinh độ (Longitude)</span>
                  <input type="number" step="0.00001" value={editing.coords[1]}
                    onChange={(e) => set("coords", [editing.coords[0], parseFloat(e.target.value)])} />
                </label>
              </div>

              {/* Mô tả ngắn */}
              <div className="admin-form__row">
                <label className="admin-form__field">
                  <span>Mô tả ngắn (VI)</span>
                  <textarea rows={2} value={editing.desc.vi} onChange={(e) => setNested("desc", "vi", e.target.value)} />
                </label>
                <label className="admin-form__field">
                  <span>Mô tả ngắn (EN)</span>
                  <textarea rows={2} value={editing.desc.en} onChange={(e) => setNested("desc", "en", e.target.value)} />
                </label>
              </div>

              {/* Nội dung chi tiết */}
              <div className="admin-form__row">
                <label className="admin-form__field">
                  <span>Nội dung chi tiết (VI)</span>
                  <textarea rows={4} value={editing.details.vi} onChange={(e) => setNested("details", "vi", e.target.value)} />
                </label>
                <label className="admin-form__field">
                  <span>Nội dung chi tiết (EN)</span>
                  <textarea rows={4} value={editing.details.en} onChange={(e) => setNested("details", "en", e.target.value)} />
                </label>
              </div>
            </div>

            <div className="admin-modal__actions">
              <button className="admin-btn" onClick={() => setEditing(null)}>Huỷ</button>
              <button className="admin-btn admin-btn--primary" onClick={handleSave}>Lưu</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
