import { useEffect, useState } from "react";
import { placeApi } from "../../services/placeApi";
import { narrationApi } from "../../services/narrationApi";
import type { Place } from "../../types/place";
import type { NarrationRevision, GenerationJob } from "../../types/narration";

const JOB_STATUS_LABEL: Record<string, string> = {
  queued: "⏳ Đang chờ", processing: "⚙️ Đang tạo", done: "✅ Xong", failed: "❌ Lỗi",
};
const LANG_FLAG: Record<string, string> = { vi: "🇻🇳", en: "🇬🇧", zh: "🇨🇳", ko: "🇰🇷", ja: "🇯🇵" };

export default function NarrationAdmin() {
  const [places,     setPlaces]     = useState<Place[]>([]);
  const [activeId,   setActiveId]   = useState<number | null>(null);
  const [revisions,  setRevisions]  = useState<NarrationRevision[]>([]);
  const [sourceText, setSourceText] = useState("");
  const [loading,    setLoading]    = useState(true);
  const [creating,   setCreating]   = useState(false);
  const [error,      setError]      = useState<string | null>(null);

  useEffect(() => {
    placeApi.list()
      .then((data) => { setPlaces(data); if (data.length) setActiveId(data[0].id); })
      .catch(() => setError("Không tải được danh sách khu."))
      .finally(() => setLoading(false));
  }, []);

  useEffect(() => {
    if (!activeId) return;
    narrationApi.listRevisions(activeId)
      .then(setRevisions)
      .catch(() => setRevisions([]));
  }, [activeId]);

  async function handleCreate() {
    if (!activeId || !sourceText.trim()) return;
    setCreating(true);
    try {
      const rev = await narrationApi.createRevision(activeId, sourceText.trim());
      setRevisions((prev) => [rev, ...prev]);
      setSourceText("");
    } catch {
      alert("Tạo thất bại. Kiểm tra kết nối backend.");
    } finally {
      setCreating(false);
    }
  }

  async function handlePublish(revId: number) {
    try {
      const updated = await narrationApi.publish(revId);
      setRevisions((prev) => prev.map((r) => r.id === updated.id ? updated : r));
    } catch {
      alert("Xuất bản thất bại.");
    }
  }

  async function handleRetry(job: GenerationJob) {
    try {
      await narrationApi.retryJob(job.id);
      // Refresh
      if (activeId) {
        const revs = await narrationApi.listRevisions(activeId);
        setRevisions(revs);
      }
    } catch {
      alert("Retry thất bại.");
    }
  }

  if (loading) return <div className="admin-section"><p>⏳ Đang tải…</p></div>;

  if (error) return (
    <div className="admin-section">
      <div className="admin-notice admin-notice--warning">
        <span>⚠️</span><div><strong>Chưa kết nối backend</strong><p>{error}</p></div>
      </div>
    </div>
  );

  const activePlace = places.find((p) => p.id === activeId);

  return (
    <div className="admin-section">
      {/* Chọn khu */}
      <div className="admin-section__header">
        <h2>Thuyết minh</h2>
      </div>

      <label className="admin-form__field" style={{ maxWidth: 320, marginBottom: 24 }}>
        <span>Chọn khu tham quan</span>
        <select value={activeId ?? ""} onChange={(e) => setActiveId(Number(e.target.value))}>
          {places.map((p) => (
            <option key={p.id} value={p.id}>{p.icon} {p.name?.vi}</option>
          ))}
        </select>
      </label>

      {activePlace && (
        <>
          {/* Tạo bài mới */}
          <div className="narration-create">
            <h3>Tạo bài thuyết minh mới — {activePlace.icon} {activePlace.name?.vi}</h3>
            <p className="admin-hint">
              Nhập bài tiếng Việt. Hệ thống sẽ tự dịch sang 4 ngôn ngữ còn lại và tạo file audio.
            </p>
            <label className="admin-form__field">
              <span>Nội dung tiếng Việt</span>
              <textarea
                rows={6}
                value={sourceText}
                onChange={(e) => setSourceText(e.target.value)}
                placeholder="Nhập nội dung thuyết minh tiếng Việt tại đây…"
              />
            </label>
            <button
              className="admin-btn admin-btn--primary"
              onClick={handleCreate}
              disabled={creating || !sourceText.trim()}
            >
              {creating ? "⏳ Đang tạo…" : "🎙️ Tạo thuyết minh"}
            </button>
          </div>

          {/* Danh sách phiên bản */}
          <h3 className="admin-subtitle">Lịch sử phiên bản</h3>

          {revisions.length === 0 ? (
            <p className="admin-hint">Chưa có bài thuyết minh nào.</p>
          ) : (
            revisions.map((rev) => (
              <div className={`revision-card revision-card--${rev.status}`} key={rev.id}>
                <div className="revision-card__header">
                  <span className="revision-card__version">v{rev.version}</span>
                  <span className={`revision-card__status revision-status--${rev.status}`}>
                    {rev.status === "published" ? "✅ Đã xuất bản" : rev.status === "draft" ? "📝 Bản nháp" : "📦 Đã lưu trữ"}
                  </span>
                  <span className="revision-card__date">{new Date(rev.createdAt).toLocaleDateString("vi")}</span>
                  {rev.status === "draft" && (
                    <button className="admin-btn admin-btn--sm admin-btn--primary" onClick={() => handlePublish(rev.id)}>
                      Xuất bản
                    </button>
                  )}
                </div>

                <p className="revision-card__text">{rev.sourceText}</p>

                {/* Jobs */}
                <div className="revision-card__jobs">
                  {rev.jobs.map((job) => (
                    <div key={job.id} className={`job-item job-item--${job.status}`}>
                      <span>{LANG_FLAG[job.lang] ?? "🌐"} {job.lang.toUpperCase()}</span>
                      <span>{JOB_STATUS_LABEL[job.status] ?? job.status}</span>
                      {job.status === "done" && job.audioUrl && (
                        <audio controls src={job.audioUrl} className="job-item__audio" />
                      )}
                      {job.status === "failed" && (
                        <button className="admin-btn admin-btn--sm" onClick={() => handleRetry(job)}>
                          Thử lại
                        </button>
                      )}
                    </div>
                  ))}
                </div>
              </div>
            ))
          )}
        </>
      )}
    </div>
  );
}
