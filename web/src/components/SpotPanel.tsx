import { useEffect } from "react";
import type { Spot } from "../data/spots";
import type { Lang } from "../i18n";

type SpotPanelProps = {
  spot: Spot | null;
  lang: Lang;
  onClose: () => void;
};

const CATEGORY_LABEL: Record<string, { vi: string; en: string }> = {
  animal:   { vi: "Động vật", en: "Animal" },
  plant:    { vi: "Thực vật", en: "Plant"  },
  facility: { vi: "Tiện ích", en: "Facility" },
};

const CATEGORY_COLOR: Record<string, string> = {
  animal:   "#206a45",
  plant:    "#2d8a2d",
  facility: "#5d3d85",
};

export default function SpotPanel({ spot, lang, onClose }: SpotPanelProps) {
  /* Đóng panel khi nhấn Escape */
  useEffect(() => {
    function handleKey(e: KeyboardEvent) {
      if (e.key === "Escape") onClose();
    }
    window.addEventListener("keydown", handleKey);
    return () => window.removeEventListener("keydown", handleKey);
  }, [onClose]);

  /* Khoá scroll body khi panel mở */
  useEffect(() => {
    if (spot) {
      document.body.style.overflow = "hidden";
    } else {
      document.body.style.overflow = "";
    }
    return () => { document.body.style.overflow = ""; };
  }, [spot]);

  if (!spot) return null;

  const name     = spot.name[lang];
  const desc     = spot.desc[lang];
  const details  = spot.details[lang];
  const catLabel = CATEGORY_LABEL[spot.category]?.[lang] ?? "";
  const catColor = CATEGORY_COLOR[spot.category] ?? "#206a45";

  return (
    <>
      {/* Overlay tối phía sau panel */}
      <button
        className="spot-panel__overlay"
        type="button"
        aria-label={lang === "vi" ? "Đóng panel" : "Close panel"}
        onClick={onClose}
      />

      {/* Panel trượt từ phải */}
      <aside
        className="spot-panel"
        role="dialog"
        aria-modal="true"
        aria-label={name}
      >
        {/* Ảnh đầu panel */}
        <div className="spot-panel__img-wrap">
          <img
            src={spot.img}
            alt={name}
            className="spot-panel__img"
            onError={(e) => {
              const img = e.currentTarget as HTMLImageElement;
              img.style.display = "none";
              const ph = img.nextElementSibling as HTMLElement | null;
              if (ph) ph.style.display = "flex";
            }}
          />
          {/* Placeholder khi chưa có ảnh */}
          <div className="spot-panel__img-placeholder" style={{ display: "none" }} aria-hidden="true">
            <span>{spot.icon}</span>
          </div>

          {/* Nút đóng nằm trên ảnh */}
          <button
            className="spot-panel__close"
            type="button"
            aria-label={lang === "vi" ? "Đóng" : "Close"}
            onClick={onClose}
          >
            ✕
          </button>
        </div>

        {/* Nội dung */}
        <div className="spot-panel__body">
          {/* Tag loại */}
          <span
            className="spot-panel__tag"
            style={{ background: catColor }}
          >
            {spot.icon} {catLabel}
          </span>

          <h2 className="spot-panel__title">{name}</h2>
          <p className="spot-panel__desc">{desc}</p>

          <div className="spot-panel__divider" />

          <p className="spot-panel__details">{details}</p>

          {/*
           * Audio thuyết minh — placeholder
           * Khi có file audio, đặt vào /public/audio/<id>.mp3
           * và bỏ comment phần dưới
           */}
          <div className="spot-panel__audio-placeholder">
            <span>🎧</span>
            <div>
              <strong>
                {lang === "vi" ? "Thuyết minh âm thanh" : "Audio Guide"}
              </strong>
              <small>
                {lang === "vi"
                  ? "Sắp ra mắt — nội dung đang được chuẩn bị"
                  : "Coming soon — content is being prepared"}
              </small>
            </div>
          </div>
        </div>
      </aside>
    </>
  );
}
