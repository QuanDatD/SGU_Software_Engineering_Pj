import { useEffect, useState } from "react";
import "../../styles/MapPage.css";
import { placeApi } from "../../services/placeApi";
import type { Place, PlaceDetail } from "../../types/place";
import { useLang } from "../../contexts/LanguageContext";
import { useGeolocation } from "../../hooks/useGeolocation";
import { useVisitSession } from "../../hooks/useVisitSession";
import { useAudio } from "../../contexts/AudioContext";
import IllustratedMap from "../../maps/IllustratedMap";
import GpsStatus from "../../components/GpsStatus";
import AudioPlayer from "../../components/AudioPlayer";
import SpotPanel from "../../components/SpotPanel";

export default function MapPage() {
  const { lang } = useLang();
  const { gps, start: startGps, stop: stopGps } = useGeolocation();
  const { track } = useAudio();

  const [places,      setPlaces]      = useState<Place[]>([]);
  const [placeDetail, setPlaceDetail] = useState<PlaceDetail | null>(null);
  const [activeId,    setActiveId]    = useState<number | null>(null);
  const [loading,     setLoading]     = useState(true);
  const [error,       setError]       = useState<string | null>(null);

  // Load danh sách khu
  useEffect(() => {
    placeApi.list()
      .then(setPlaces)
      .catch(() => setError(lang === "vi" ? "Không tải được dữ liệu." : "Failed to load data."))
      .finally(() => setLoading(false));
  }, [lang]);

  // Load chi tiết khu khi chọn
  useEffect(() => {
    if (activeId === null) { setPlaceDetail(null); return; }
    placeApi.get(activeId)
      .then(setPlaceDetail)
      .catch(() => setPlaceDetail(null));
  }, [activeId]);

  // Tự phát audio theo GPS
  const gpsPosition = gps.status === "active" ? gps.position : null;
  useVisitSession({
    position: gpsPosition,
    places: places as PlaceDetail[], // cast tạm, audioUrl sẽ load khi cần
    lang,
    enabled: gps.status === "active",
  });

  // Khi player đang phát, highlight khu tương ứng
  useEffect(() => {
    if (track) setActiveId(track.placeId);
  }, [track]);

  function handleSelect(place: Place) {
    setActiveId(place.id);
  }

  const LABELS = {
    title:     { vi: "Bản đồ tham quan",              en: "Visit Map",                 zh: "参观地图",         ko: "방문 지도",            ja: "訪問マップ" },
    eyebrow:   { vi: "HÀNH TRÌNH CỦA BẠN",             en: "YOUR JOURNEY",              zh: "您的旅程",         ko: "당신의 여정",          ja: "あなたの旅" },
    subtitle:  { vi: "Khám phá không gian xanh.",      en: "Explore the green space.",  zh: "探索绿色空间。",    ko: "녹색 공간을 탐험하세요.", ja: "緑の空間を探検しましょう。" },
    help:      { vi: "Nhấn vào điểm trên bản đồ để xem thông tin.", en: "Tap a point on the map to view details.", zh: "点击地图上的点查看详情。", ko: "지도의 포인트를 탭하여 세부 정보를 보세요.", ja: "マップのポイントをタップして詳細を表示します。" },
    animalLeg: { vi: "Động vật", en: "Animals", zh: "动物", ko: "동물", ja: "動物" },
    plantLeg:  { vi: "Thực vật", en: "Plants",  zh: "植物", ko: "식물", ja: "植物" },
    facLeg:    { vi: "Tiện ích", en: "Facilities", zh: "设施", ko: "시설", ja: "施設" },
  };
  const t = (key: keyof typeof LABELS) => LABELS[key]?.[lang] ?? LABELS[key]?.["en"] ?? "";

  return (
    <>
      <section className="section map-page">
        <div className="section__heading">
          <div>
            <span className="eyebrow eyebrow--green">{t("eyebrow")}</span>
            <h1>{t("title")}</h1>
          </div>
          <p>{t("subtitle")}</p>
        </div>

        {/* GPS status bar */}
        <GpsStatus gps={gps} onStart={startGps} onStop={stopGps} lang={lang} />

        {/* Bản đồ */}
        <div className="zoo-map-frame">
          {loading ? (
            <div className="map-loading">⏳</div>
          ) : error ? (
            <div className="map-error">{error}</div>
          ) : (
            <IllustratedMap
              places={places}
              activePlaceId={activeId}
              lang={lang}
              onSelect={handleSelect}
            />
          )}
        </div>

        {/* Chú thích */}
        <div className="map-legend">
          <span style={{ borderLeft: "3px solid #206a45", paddingLeft: 8 }}>🐘 {t("animalLeg")}</span>
          <span style={{ borderLeft: "3px solid #2d8a2d", paddingLeft: 8 }}>🌳 {t("plantLeg")}</span>
          <span style={{ borderLeft: "3px solid #5d3d85", paddingLeft: 8 }}>🏛️ {t("facLeg")}</span>
        </div>

        <p className="map-help">{t("help")}</p>
      </section>

      {/* Audio player cố định ở dưới màn hình */}
      <div className="audio-player-bar">
        <AudioPlayer lang={lang} />
      </div>

      {/* Panel chi tiết khu */}
      <SpotPanel
        place={placeDetail}
        lang={lang}
        onClose={() => setActiveId(null)}
      />
    </>
  );
}
