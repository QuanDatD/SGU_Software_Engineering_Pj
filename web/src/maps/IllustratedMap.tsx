/* ======================================================
   IllustratedMap — bản đồ dạng ảnh tĩnh
   mapX / mapY theo contract = pixel trên ảnh gốc (1024×643)
   Component tự tính % để đặt marker đúng vị trí.
   ====================================================== */
import { useRef, useState } from "react";
import type { Place } from "../types/place";
import type { Lang } from "../types/common";

/* Kích thước ảnh gốc map.jpg */
const IMG_W = 1024;
const IMG_H = 643;

const CAT_COLOR: Record<string, string> = {
  animal:   "#206a45",
  plant:    "#2d8a2d",
  facility: "#5d3d85",
};

interface Props {
  places: Place[];
  activePlaceId: number | null;
  lang: Lang;
  onSelect: (place: Place) => void;
}

export default function IllustratedMap({ places, activePlaceId, lang, onSelect }: Props) {
  const [imgLoaded, setImgLoaded] = useState(false);
  const [imgError,  setImgError]  = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);

  return (
    <div className="illu-map" ref={wrapRef}>
      {!imgError ? (
        <img
          src="/images/map.jpg"
          alt="Sơ đồ Thảo Cầm Viên"
          className="illu-map__img"
          draggable={false}
          onLoad={()  => setImgLoaded(true)}
          onError={() => setImgError(true)}
        />
      ) : (
        <div className="illu-map__fallback">🗺️</div>
      )}

      {/* Marker — chỉ render khi ảnh đã load hoặc fallback */}
      {(imgLoaded || imgError) && places.map((p) => {
        /*
         * mapX, mapY = pixel trên ảnh gốc (theo contract Bảng 3)
         * Chuyển sang % để marker đúng vị trí dù ảnh co giãn
         */
        const xPct = ((p.mapX ?? IMG_W / 2) / IMG_W) * 100;
        const yPct = ((p.mapY ?? IMG_H / 2) / IMG_H) * 100;

        return (
          <button
            key={p.id}
            type="button"
            className={`illu-map__marker${activePlaceId === p.id ? " is-active" : ""}`}
            style={{
              left:       `${xPct}%`,
              top:        `${yPct}%`,
              background: CAT_COLOR[p.category] ?? "#206a45",
            }}
            title={p.name?.[lang] ?? p.slug}
            aria-label={p.name?.[lang] ?? p.slug}
            onClick={() => onSelect(p)}
          >
            <span>{p.icon}</span>
          </button>
        );
      })}
    </div>
  );
}
