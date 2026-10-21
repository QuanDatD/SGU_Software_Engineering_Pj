import { useEffect, useState } from "react";
import { placeApi } from "../../services/placeApi";
import type { PlaceDetail } from "../../types/place";

const LANGS = ["vi","en","zh","ko","ja"];
const LANG_FLAG: Record<string, string> = { vi:"🇻🇳", en:"🇬🇧", zh:"🇨🇳", ko:"🇰🇷", ja:"🇯🇵" };

export default function MediaAdmin() {
  const [places,  setPlaces]  = useState<PlaceDetail[]>([]);
  const [loading, setLoading] = useState(true);
  const [error,   setError]   = useState<string | null>(null);

  useEffect(() => {
    Promise.all(
      /* Lấy chi tiết từng khu để có audioUrl */
      [placeApi.list().then((list) =>
        Promise.all(list.map((p) => placeApi.get(p.id)))
      )]
    )
      .then(([[details]]) => setPlaces(details as unknown as PlaceDetail[]))
      .catch(() => setError("Không tải được dữ liệu."))
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div className="admin-section"><p>⏳ Đang tải…</p></div>;

  if (error) return (
    <div className="admin-section">
      <div className="admin-notice admin-notice--warning">
        <span>⚠️</span><div><strong>Chưa kết nối backend</strong><p>{error}</p></div>
      </div>
    </div>
  );

  return (
    <div className="admin-section">
      <div className="admin-section__header"><h2>Media — Ảnh & Audio</h2></div>

      <div className="admin-notice">
        <span>ℹ️</span>
        <div>
          <strong>Upload ảnh</strong>
          <p>Ảnh được upload qua tab "Điểm tham quan" → trường URL ảnh. File lưu tại <code>storage/images/</code>.</p>
        </div>
      </div>

      {places.map((p) => (
        <div className="media-place" key={p.id}>
          <div className="media-place__header">
            <span>{p.icon}</span>
            <h3>{p.name?.vi}</h3>
          </div>

          {/* Ảnh */}
          <div className="media-place__img">
            {p.imageUrl ? (
              <img src={p.imageUrl} alt={p.name?.vi} />
            ) : (
              <div className="media-card__placeholder">📷 Chưa có ảnh</div>
            )}
          </div>

          {/* Audio theo ngôn ngữ */}
          <div className="media-place__audios">
            {LANGS.map((l) => {
              const url = p.audioUrl?.[l];
              return (
                <div key={l} className={`audio-file-row${url ? " audio-file-row--ok" : ""}`}>
                  <span>{LANG_FLAG[l]} {l.toUpperCase()}</span>
                  {url ? (
                    <>
                      <span className="audio-file-row__ok">✅ Có sẵn</span>
                      <audio controls src={url} className="audio-file-row__player" />
                    </>
                  ) : (
                    <span className="audio-file-row__missing">⏳ Chưa có</span>
                  )}
                </div>
              );
            })}
          </div>
        </div>
      ))}
    </div>
  );
}
