import { useEffect, useRef, useState } from "react";
import * as L from "leaflet";
import "leaflet/dist/leaflet.css";
import "../../styles/MapPage.css";
import { spots, type Spot } from "../../data/spots";
import type { Translations, Lang } from "../../i18n";
import SpotPanel from "../../components/SpotPanel";

/*
 * Kích thước ảnh map.jpg: 1024 × 643 px
 * Hệ tọa độ CRS.Simple: góc trên-trái = [0,0], góc dưới-phải = [643, 1024]
 * Trục Y đi từ trên xuống dưới (ngược với lat/lng thông thường)
 */
const IMG_W = 1024;
const IMG_H = 643;

/*
 * Ánh xạ tọa độ thực (lat/lng) → pixel trên ảnh
 * Góc tây-bắc thực tế của Thảo Cầm Viên ≈ [10.7895, 106.7032]
 * Góc đông-nam thực tế                    ≈ [10.7855, 106.7075]
 * Điều chỉnh LAT_MAX, LAT_MIN, LNG_MIN, LNG_MAX nếu ảnh lệch
 */
const LAT_MAX = 10.7900; // vĩ độ phía bắc (top)
const LAT_MIN = 10.7850; // vĩ độ phía nam (bottom)
const LNG_MIN = 106.7030; // kinh độ phía tây (left)
const LNG_MAX = 106.7080; // kinh độ phía đông (right)

function latLngToPixel(lat: number, lng: number): [number, number] {
  const x = ((lng - LNG_MIN) / (LNG_MAX - LNG_MIN)) * IMG_W;
  // y tăng từ trên xuống, lat tăng từ dưới lên → đảo ngược
  const y = ((LAT_MAX - lat) / (LAT_MAX - LAT_MIN)) * IMG_H;
  return [y, x]; // Leaflet CRS.Simple dùng [y, x]
}

// Giới hạn bản đồ theo kích thước ảnh
const BOUNDS: L.LatLngBoundsExpression = [[0, 0], [IMG_H, IMG_W]];
const MAP_CENTER: L.LatLngTuple = [IMG_H / 2, IMG_W / 2];

const CATEGORY_COLOR: Record<string, string> = {
  animal:   "#206a45",
  plant:    "#2d8a2d",
  facility: "#5d3d85",
};

function makeIcon(emoji: string, color: string): L.DivIcon {
  return L.divIcon({
    className: "",
    html: `<div class="map-marker" style="background:${color}"><span>${emoji}</span></div>`,
    iconSize:    [42, 42],
    iconAnchor:  [21, 42],
    popupAnchor: [0, -44],
  });
}

type MapPageProps = {
  t: Translations;
  lang: Lang;
};

export default function MapPage({ t, lang }: MapPageProps) {
  const containerRef = useRef<HTMLDivElement | null>(null);
  const mapRef       = useRef<L.Map | null>(null);
  const [activeSpot, setActiveSpot] = useState<Spot | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    /* Tính minZoom để ảnh luôn lấp đầy khung, không thu nhỏ hơn */
    const containerW = containerRef.current.clientWidth  || IMG_W;
    const containerH = containerRef.current.clientHeight || 600;
    const scaleX = containerW / IMG_W;
    const scaleY = containerH / IMG_H;
    const fitScale = Math.max(scaleX, scaleY); // chọn cạnh lớn hơn
    // Leaflet CRS.Simple: zoom 0 = 1px/px, mỗi zoom level nhân đôi
    const minZoom = Math.log2(fitScale);

    /* Dùng CRS.Simple để load ảnh tĩnh làm bản đồ */
    const map = L.map(containerRef.current, {
      crs: L.CRS.Simple,
      minZoom,
      maxZoom: 3,
      zoomSnap: 0.25,
      maxBoundsViscosity: 1.0, // không cho kéo ra ngoài ảnh
    });

    mapRef.current = map;

    /* Đặt ảnh map.jpg phủ toàn bộ canvas */
    L.imageOverlay("/images/map.jpg", BOUNDS).addTo(map);

    /* Fit toàn bộ ảnh vào khung, chừa padding nhỏ */
    map.fitBounds(BOUNDS, { padding: [0, 0] });

    /* Giới hạn không kéo ra ngoài ảnh */
    map.setMaxBounds(BOUNDS);

    /* Thêm marker cho từng điểm tham quan */
    spots.forEach((spot) => {
      const color   = CATEGORY_COLOR[spot.category] ?? "#206a45";
      const icon    = makeIcon(spot.icon, color);
      const [y, x]  = latLngToPixel(spot.coords[0], spot.coords[1]);

      L.marker([y, x], { icon, title: spot.name[lang] })
        .addTo(map)
        .on("click", () => setActiveSpot(spot));
    });

    const observer = new ResizeObserver(() => map.invalidateSize());
    observer.observe(containerRef.current!);

    return () => {
      observer.disconnect();
      map.remove();
      mapRef.current = null;
    };
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lang]);

  function returnToCenter() {
    mapRef.current?.fitBounds(BOUNDS, { padding: [0, 0] });
  }

  return (
    <>
      <section className="section map-page">
        <div className="section__heading">
          <div>
            <span className="eyebrow eyebrow--green">{t.mapEyebrow}</span>
            <h1>{t.mapTitle}</h1>
          </div>
          <p>{t.mapDesc}</p>
        </div>

        <div className="map-toolbar">
          <span>{t.mapDrag}</span>
          <button className="map-home-button" type="button" onClick={returnToCenter}>
            {t.mapReturnBtn}
          </button>
        </div>

        <div className="zoo-map-frame">
          <div
            ref={containerRef}
            className="zoo-map"
            role="region"
            aria-label={t.mapTitle}
          />
        </div>

        {/* Chú thích màu marker */}
        <div className="map-legend">
          <span style={{ borderLeft: `3px solid ${CATEGORY_COLOR.animal}`, paddingLeft: 8 }}>
            🐘 {lang === "vi" ? "Động vật" : "Animals"}
          </span>
          <span style={{ borderLeft: `3px solid ${CATEGORY_COLOR.plant}`, paddingLeft: 8 }}>
            🌳 {lang === "vi" ? "Thực vật" : "Plants"}
          </span>
          <span style={{ borderLeft: `3px solid ${CATEGORY_COLOR.facility}`, paddingLeft: 8 }}>
            🏛️ {lang === "vi" ? "Tiện ích" : "Facilities"}
          </span>
        </div>

        <p className="map-help">{t.mapHelp}</p>
      </section>

      <SpotPanel
        spot={activeSpot}
        lang={lang}
        onClose={() => setActiveSpot(null)}
      />
    </>
  );
}
