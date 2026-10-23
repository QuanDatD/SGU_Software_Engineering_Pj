import { useCallback, useEffect, useState } from "react";
import { placeApi } from "../../services/placeApi";
import { narrationApi } from "../../services/narrationApi";
import { mediaUrl } from "../../services/api";
import type { PlaceDetail } from "../../types/place";
import type { NarrationRevision } from "../../types/narration";

const LANGS = ["vi", "en", "zh", "ko", "ja"];
const STATUS = { draft: "Bản nháp", published: "Đã xuất bản", archived: "Lưu trữ" };
interface MediaPlace { place: PlaceDetail; revisions: NarrationRevision[]; error?: string }

export default function MediaAdmin() {
  const [items, setItems] = useState<MediaPlace[]>([]);
  const [selected, setSelected] = useState<Record<string, string>>({});
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const load = useCallback(async () => {
    setLoading(true); setError(null);
    try {
      const places = await placeApi.adminList();
      const rows = await Promise.all(places.map(async place => {
        try { return { place, revisions: await narrationApi.listRevisions(place.id) }; }
        catch (e) { return { place, revisions: [], error: e instanceof Error ? e.message : "Không tải được phiên bản." }; }
      }));
      setItems(rows);
    } catch (e) { setError(e instanceof Error ? e.message : "Không tải được dữ liệu."); }
    finally { setLoading(false); }
  }, []);
  useEffect(() => { void load(); }, [load]);

  return <div className="admin-section">
    <div className="admin-section__header">
      <h2>Media — Ảnh & Audio</h2>
      <button className="admin-btn" disabled={loading} onClick={() => void load()}>Tải lại</button>
    </div>
    <div className="admin-notice"><span>ℹ️</span><div>
      <strong>Ảnh và thuyết minh</strong>
      <p>Chọn ảnh tại tab Điểm tham quan. Tạo âm thanh tại tab Thuyết minh, rồi chọn phiên bản bên dưới để nghe kiểm tra.</p>
    </div></div>
    {loading && <p>⏳ Đang tải…</p>}
    {error && <p role="alert">{error}</p>}
    {!loading && !error && items.length === 0 && <p>Chưa có khu tham quan. Hãy tạo khu ở tab Điểm tham quan.</p>}
    {!loading && items.map(({ place, revisions, error: rowError }) => {
      const revision = revisions.find(r => r.id === selected[place.id]) ?? revisions[0];
      return <div className="media-place" key={place.id}>
        <div className="media-place__header"><span>{place.icon}</span><h3>{place.name?.vi || place.slug}{!place.isActive && " (Đã ẩn)"}</h3></div>
        <div className="media-place__img">{place.imageUrl
          ? <img src={place.imageUrl} alt={place.name?.vi || place.slug} />
          : <div className="media-card__placeholder">📷 Chưa có ảnh</div>}
        </div>
        {rowError ? <p role="alert">{rowError}</p> : !revision ? <p>Chưa có phiên bản thuyết minh.</p> : <>
          <label className="admin-form__field"><span>Phiên bản để nghe thử</span>
            <select aria-label={`Phiên bản ${place.slug}`} value={revision.id} onChange={e => setSelected(prev => ({ ...prev, [place.id]: e.target.value }))}>
              {revisions.map(r => <option key={r.id} value={r.id}>v{r.version} — {STATUS[r.status]}</option>)}
            </select>
          </label>
          <div className="media-place__audios">{LANGS.map(lang => {
            const job = revision.jobs.find(j => j.lang === lang);
            const translation = revision.translations.find(t => t.lang === lang);
            const url = translation?.isApproved && job?.status === "succeeded" ? job.audioUrl : null;
            return <div key={`${revision.id}:${lang}`} className={`audio-file-row${url ? " audio-file-row--ok" : ""}`}>
              <span>{lang.toUpperCase()}</span>
              {url ? <><span className="audio-file-row__ok">✅ Có sẵn</span><audio controls preload="none" src={mediaUrl(url)} className="audio-file-row__player" /></>
                : <span className="audio-file-row__missing">{job?.status === "failed" ? `❌ ${job.errorMessage || "Tạo âm thanh thất bại."}` : job?.status === "processing" ? "⏳ Đang xử lý" : "⏳ Chưa có âm thanh hợp lệ"}</span>}
            </div>;
          })}</div>
        </>}
      </div>;
    })}
  </div>;
}
