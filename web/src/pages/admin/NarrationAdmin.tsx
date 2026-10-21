import { useEffect, useState, useCallback } from "react";
import { placeApi } from "../../services/placeApi";
import { narrationApi } from "../../services/narrationApi";
import type { Place } from "../../types/place";
import type { NarrationRevision, GenerationJob } from "../../types/narration";

const JOB_LABEL: Record<string, string> = {
  queued: "⏳ Đang chờ", processing: "⚙️ Đang xử lý",
  succeeded: "✅ Xong", failed: "❌ Lỗi",
};
const LANG_FLAG: Record<string, string> = {
  vi: "🇻🇳", en: "🇬🇧", zh: "🇨🇳", ko: "🇰🇷", ja: "🇯🇵",
};
const LANGS = ["vi", "en", "zh", "ko", "ja"];

export default function NarrationAdmin() {
  const [places,      setPlaces]      = useState<Place[]>([]);
  const [activeId,    setActiveId]    = useState<string>("");
  const [revisions,   setRevisions]   = useState<NarrationRevision[]>([]);
  const [sourceText,  setSourceText]  = useState("");
  const [editRevId,   setEditRevId]   = useState<string | null>(null);
  const [editLang,    setEditLang]    = useState<string>("vi");
  const [editText,    setEditText]    = useState("");
  const [loading,     setLoading]     = useState(true);
  const [error,       setError]       = useState<string | null>(null);
  const [busy,        setBusy]        = useState(false);

  useEffect(() => {
    placeApi.adminList()
      .then((data) => {
        setPlaces(data);
        if (data.length) setActiveId(data[0].id);
      })
      .catch(() => setError("Không tải được danh sách khu."))
      .finally(() => setLoading(false));
  }, []);

  const loadRevisions = useCallback((id: string) => {
    narrationApi.listRevisions(id)
      .then(setRevisions)
      .catch(() => setRevisions([]));
  }, []);

  useEffect(() => {
    if (activeId) loadRevisions(activeId);
  }, [activeId, loadRevisions]);

  /* Tạo phiên bản nháp mới */
  async function handleCreate() {
    if (!activeId || !sourceText.trim()) return;
    setBusy(true);
    try {
      const rev = await narrationApi.createRevision(activeId, sourceText.trim());
      setRevisions((prev) => [rev, ...prev]);
      setSourceText("");
    } catch { alert("Tạo thất bại."); }
    finally { setBusy(false); }
  }

  /* Mở form sửa bản dịch */
  function openEditTranslation(rev: NarrationRevision, lang: string) {
    const existing = rev.translations.find((t) => t.lang === lang);
    setEditRevId(rev.id);
    setEditLang(lang);
    setEditText(existing?.text ?? "");
  }

  /* Lưu bản dịch */
  async function handleSaveTranslation() {
    if (!editRevId) return;
    setBusy(true);
    try {
      await narrationApi.updateTranslation(editRevId, editLang, editText);
      loadRevisions(activeId);
      setEditRevId(null);
    } catch { alert("Lưu thất bại."); }
    finally { setBusy(false); }
  }

  /* Tạo audio (generate) */
  async function handleGenerate(revId: string) {
    setBusy(true);
    try {
      await narrationApi.generate(revId);
      loadRevisions(activeId);
    } catch { alert("Generate thất bại."); }
    finally { setBusy(false); }
  }

  /* Xuất bản */
  async function handlePublish(revId: string) {
    setBusy(true);
    try {
      await narrationApi.publish(revId);
      loadRevisions(activeId);
    } catch { alert("Xuất bản thất bại."); }
    finally { setBusy(false); }
  }

  /* Retry job lỗi */
  async function handleRetry(job: GenerationJob) {
    try {
      await narrationApi.retryJob(job.id);
      loadRevisions(activeId);
    } catch { alert("Retry thất bại."); }
  }

  if (loading) return <div className="admin-section"><p>⏳ Đang tải…</p></div>;
  if (error)   return (
    <div className="admin-section">
      <div className="admin-notice admin-notice--warning">
        <span>⚠️</span><div><strong>Chưa kết nối backend</strong><p>{error}</p></div>
      </div>
    </div>
  );

  const activePlace = places.find((p) => p.id === activeId);

  return (
    <div className="admin-section">
      <div className="admin-section__header"><h2>Thuyết minh</h2></div>

      {/* Chọn khu */}
      <label className="admin-form__field" style={{ maxWidth: 340, marginBottom: 24 }}>
        <span>Khu tham quan</span>
        <select value={activeId} onChange={(e) => setActiveId(e.target.value)}>
          {places.map((p) => (
            <option key={p.id} value={p.id}>{p.icon} {p.name?.vi}</option>
          ))}
        </select>
      </label>

      {activePlace && (
        <>
          {/* Tạo phiên bản mới */}
          <div className="narration-create">
            <h3>Tạo phiên bản mới — {activePlace.icon} {activePlace.name?.vi}</h3>
            <p className="admin-hint">
              Nhập bài tiếng Việt gốc. Sau khi tạo, bạn có thể sửa bản dịch từng ngôn ngữ, rồi bấm Generate để tạo audio.
            </p>
            <label className="admin-form__field">
              <span>Nội dung tiếng Việt (bản gốc)</span>
              <textarea rows={5} value={sourceText}
                onChange={(e) => setSourceText(e.target.value)}
                placeholder="Nhập nội dung thuyết minh tiếng Việt…" />
            </label>
            <button className="admin-btn admin-btn--primary" onClick={handleCreate}
              disabled={busy || !sourceText.trim()}>
              {busy ? "⏳ Đang tạo…" : "📝 Tạo phiên bản nháp"}
            </button>
          </div>

          {/* Danh sách phiên bản */}
          <h3 className="admin-subtitle">Lịch sử phiên bản ({revisions.length})</h3>

          {revisions.length === 0 && <p className="admin-hint">Chưa có phiên bản nào.</p>}

          {revisions.map((rev) => (
            <div className={`revision-card revision-card--${rev.status}`} key={rev.id}>
              <div className="revision-card__header">
                <span className="revision-card__version">v{rev.version}</span>
                <span className={`revision-status--${rev.status}`}>
                  {rev.status === "published" ? "✅ Đã xuất bản" :
                   rev.status === "draft"     ? "📝 Bản nháp" : "📦 Lưu trữ"}
                </span>
                <span className="revision-card__date">
                  {new Date(rev.createdAt).toLocaleDateString("vi")}
                </span>

                {/* Nút hành động */}
                <div style={{ display: "flex", gap: 6, marginLeft: "auto" }}>
                  {rev.status === "draft" && rev.jobs.length === 0 && (
                    <button className="admin-btn admin-btn--sm admin-btn--primary"
                      onClick={() => handleGenerate(rev.id)} disabled={busy}>
                      🎙️ Generate audio
                    </button>
                  )}
                  {rev.status === "draft" && rev.jobs.some(j => j.status === "succeeded") && (
                    <button className="admin-btn admin-btn--sm admin-btn--primary"
                      onClick={() => handlePublish(rev.id)} disabled={busy}>
                      📢 Xuất bản
                    </button>
                  )}
                </div>
              </div>

              {/* Bản gốc */}
              <p className="revision-card__text">{rev.sourceText}</p>

              {/* Bản dịch */}
              <div style={{ padding: "0 16px 12px" }}>
                <p style={{ fontSize: 12, fontWeight: 700, color: "#627568", margin: "8px 0 6px" }}>
                  BẢN DỊCH
                </p>
                <div style={{ display: "grid", gridTemplateColumns: "repeat(5, 1fr)", gap: 6 }}>
                  {LANGS.map((l) => {
                    const tr = rev.translations.find((t) => t.lang === l);
                    return (
                      <button key={l}
                        className={`admin-btn admin-btn--sm${tr?.text ? "" : " admin-btn--danger"}`}
                        style={{ fontSize: 11 }}
                        onClick={() => openEditTranslation(rev, l)}>
                        {LANG_FLAG[l]} {l.toUpperCase()}
                        {tr?.text ? " ✓" : " +"}
                      </button>
                    );
                  })}
                </div>
              </div>

              {/* Jobs audio */}
              {rev.jobs.length > 0 && (
                <div className="revision-card__jobs">
                  {rev.jobs.map((job) => (
                    <div key={job.id} className={`job-item job-item--${job.status}`}>
                      <span>{LANG_FLAG[job.lang] ?? "🌐"} {job.lang.toUpperCase()}</span>
                      <span>{JOB_LABEL[job.status] ?? job.status}</span>
                      {job.status === "succeeded" && job.audioUrl && (
                        <audio controls src={job.audioUrl} className="job-item__audio" />
                      )}
                      {job.status === "failed" && (
                        <>
                          <span style={{ fontSize: 11, color: "#c0392b" }}>{job.errorMessage}</span>
                          <button className="admin-btn admin-btn--sm" onClick={() => handleRetry(job)}>
                            Thử lại
                          </button>
                        </>
                      )}
                    </div>
                  ))}
                </div>
              )}
            </div>
          ))}
        </>
      )}

      {/* Modal sửa bản dịch */}
      {editRevId && (
        <div className="admin-modal-overlay">
          <div className="admin-modal">
            <h3>{LANG_FLAG[editLang]} Sửa bản dịch — {editLang.toUpperCase()}</h3>
            <label className="admin-form__field">
              <span>Nội dung</span>
              <textarea rows={6} value={editText} onChange={(e) => setEditText(e.target.value)} />
            </label>
            <div className="admin-modal__actions">
              <button className="admin-btn" onClick={() => setEditRevId(null)}>Huỷ</button>
              <button className="admin-btn admin-btn--primary" onClick={handleSaveTranslation} disabled={busy}>
                {busy ? "Đang lưu…" : "Lưu"}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
