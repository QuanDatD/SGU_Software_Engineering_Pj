/* ======================================================
   IllustratedMap — bản đồ dạng ảnh tĩnh
   Marker đặt theo mapX/mapY (% trên ảnh)
   Dùng khi không cần bản đồ tương tác (tile map)
   ====================================================== */
import { useRef, useState } from "react";
import type { Place } from "../types/place";
import type { Lang } from "../types/common";

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
  const imgRef = useRef<HTMLDivElement | null>(null);
  const [imgError, setImgError] = useState(false);

  return (
    <div className="illu-map" ref={imgRef}>
      {!imgError ? (
        <img
          src="/images/map.jpg"
          alt="Sơ đồ Thảo Cầm Viên"
          className="illu-map__img"
          draggable={false}
          onError={() => setImgError(true)}
        />
      ) : (
        <div className="illu-map__fallback">🗺️</div>
      )}

      {/* Marker */}
      {places.map((p) => (
        <button
          key={p.id}
          type="button"
          className={`illu-map__marker${activePlaceId === p.id ? " is-active" : ""}`}
          style={{
            left: `${p.mapX}%`,
            top:  `${p.mapY}%`,
            background: CAT_COLOR[p.category] ?? "#206a45",
          }}
          title={p.name?.[lang] ?? p.slug}
          aria-label={p.name?.[lang] ?? p.slug}
          onClick={() => onSelect(p)}
        >
          <span>{p.icon}</span>
        </button>
      ))}
    </div>
  );
}
