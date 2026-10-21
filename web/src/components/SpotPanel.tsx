import { useEffect } from "react";
import type { PlaceDetail } from "../types/place";
import type { Lang } from "../types/common";
import { useAudio } from "../contexts/AudioContext";

interface Props {
  place: PlaceDetail | null;
  lang: Lang;
  onClose: () => void;
}

const CAT_LABEL: Record<string, Record<Lang, string>> = {
  animal:   { vi: "Động vật", en: "Animal",   zh: "动物", ko: "동물",    ja: "動物" },
  plant:    { vi: "Thực vật", en: "Plant",    zh: "植物", ko: "식물",    ja: "植物" },
  facility: { vi: "Tiện ích", en: "Facility", zh: "设施", ko: "시설",    ja: "施設" },
};

const CAT_COLOR: Record<string, string> = {
  animal:   "#206a45",
  plant:    "#2d8a2d",
  facility: "#5d3d85",
};

export default function SpotPanel({ place, lang, onClose }: Props) {
  const { play, track, state, pause, resume } = useAudio();

  useEffect(() => {
    function onKey(e: KeyboardEvent) { if (e.key === "Escape") onClose(); }
    window.addEventListener("keydown", onKey);
    return () => window.removeEventListener("keydown", onKey);
  }, [onClose]);

  useEffect(() => {
    document.body.style.overflow = place ? "hidden" : "";
    return () => { document.body.style.overflow = ""; };
  }, [place]);

  if (!place) return null;

  const name     = place.name?.[lang] ?? place.name?.["vi"] ?? "";
  const desc     = place.desc?.[lang] ?? place.desc?.["vi"] ?? "";
  const catLabel = CAT_LABEL[place.category]?.[lang] ?? place.category;
  const catColor = CAT_COLOR[place.category] ?? "#206a45";
  const audioUrl = place.audioUrl?.[lang] ?? null;

  const isThisPlaying =
    track?.placeId === place.id && track?.lang === lang &&
    (state === "playing" || state === "loading");

  function handlePlayPause() {
    if (isThisPlaying) {
      pause();
    } else if (track?.placeId === place!.id && track?.lang === lang && state === "paused") {
      resume();
    } else if (audioUrl) {
      play({ placeId: place!.id, lang, url: audioUrl, title: name });
    }
  }

  return (
    <>
      <button className="spot-panel__overlay" type="button" aria-label="Đóng" onClick={onClose} />

      <aside className="spot-panel" role="dialog" aria-modal="true" aria-label={name}>
        {/* Ảnh */}
        <div className="spot-panel__img-wrap">
          {place.imageUrl ? (
            <img src={place.imageUrl} alt={name} className="spot-panel__img" />
          ) : (
            <div className="spot-panel__img-placeholder" aria-hidden="true">
              <span>{place.icon}</span>
            </div>
          )}
          <button className="spot-panel__close" type="button" aria-label="Đóng" onClick={onClose}>✕</button>
        </div>

        {/* Body */}
        <div className="spot-panel__body">
          <span className="spot-panel__tag" style={{ background: catColor }}>
            {place.icon} {catLabel}
          </span>

          <h2 className="spot-panel__title">{name}</h2>
          <p className="spot-panel__desc">{desc}</p>

          <div className="spot-panel__divider" />

          {/* Audio */}
          {audioUrl ? (
            <button
              className={`spot-panel__play-btn${isThisPlaying ? " is-playing" : ""}`}
              type="button"
              onClick={handlePlayPause}
            >
              {isThisPlaying ? "⏸" : "▶"}
              {isThisPlaying
                ? (lang === "vi" ? " Tạm dừng" : " Pause")
                : (lang === "vi" ? " Nghe thuyết minh" : " Listen to audio guide")}
            </button>
          ) : (
            <div className="spot-panel__audio-placeholder">
              <span>🎧</span>
              <div>
                <strong>{lang === "vi" ? "Thuyết minh sắp ra mắt" : "Audio guide coming soon"}</strong>
                <small>{lang === "vi" ? "Nội dung đang được chuẩn bị" : "Content is being prepared"}</small>
              </div>
            </div>
          )}
        </div>
      </aside>
    </>
  );
}
