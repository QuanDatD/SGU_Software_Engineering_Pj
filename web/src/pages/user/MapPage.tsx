import { useEffect, useState } from "react";
import "../../styles/MapPage.css";
import { placeApi } from "../../services/placeApi";
import type { Place, PlaceDetail } from "../../types/place";
import { staticPlaces } from "../../data/staticPlaces";
import { useLang } from "../../contexts/LanguageContext";
import { useGeolocation } from "../../hooks/useGeolocation";
import { useVisitSession } from "../../hooks/useVisitSession";
import { useAudio } from "../../contexts/AudioContext";
import IllustratedMap from "../../maps/IllustratedMap";
import GpsStatus from "../../components/GpsStatus";
import AudioPlayer from "../../components/AudioPlayer";
import SpotPanel from "../../components/SpotPanel";

const LABELS = {
  title:    { vi: "Bản đồ tham quan",               en: "Visit Map",                  zh: "参观地图",          ko: "방문 지도",            ja: "訪問マップ"        },
  eyebrow:  { vi: "HÀNH TRÌNH CỦA BẠN",              en: "YOUR JOURNEY",               zh: "您的旅程",          ko: "당신의 여정",          ja: "あなたの旅"        },
  subtitle: { vi: "Nhấn vào điểm để xem thông tin.", en: "Tap a marker to view info.", zh: "点击标记查看信息。", ko: "마커를 탭하여 정보 보기.", ja: "マーカーをタップして情報を表示。" },
  animal:   { vi: "Động vật",  en: "Animals",    zh: "动物", ko: "동물", ja: "動物" },
  plant:    { vi: "Thực vật",  en: "Plants",     zh: "植物", ko: "식물", ja: "植物" },
  facility: { vi: "Tiện ích",  en: "Facilities", zh: "设施", ko: "시설", ja: "施設" },
  offlineNote: { vi: "Đang hiển thị dữ liệu mẫu (chưa kết nối server).", en: "Showing sample data (server not connected).", zh: "显示示例数据（未连接服务器）。", ko: "샘플 데이터 표시 중 (서버 미연결).", ja: "サンプルデータを表示中（サーバー未接続）。" },
};
const t = (key: keyof typeof LABELS, lang: string): string =>
  (LABELS[key] as Record<string, string>)?.[lang] ?? (LABELS[key] as Record<string, string>)?.["en"] ?? "";

export default function MapPage() {
  const { lang } = useLang();
  const { gps, start: startGps, stop: stopGps } = useGeolocation();
  const { track } = useAudio();

  const [places,      setPlaces]      = useState<PlaceDetail[]>(staticPlaces);
  const [placeDetail, setPlaceDetail] = useState<PlaceDetail | null>(null);
  const [activeId,    setActiveId]    = useState<number | null>(null);
  const [isOffline,   setIsOffline]   = useState(false); // true = dùng dữ liệu tĩnh

  // Thử load từ API, fallback sang dữ liệu tĩnh nếu lỗi
  useEffect(() => {
    placeApi.list()
      .then((data) => {
        setPlaces(data as PlaceDetail[]);
        setIsOffline(false);
      })
      .catch(() => {
        // Giữ staticPlaces, chỉ báo đang offline
        setIsOffline(true);
      });
  // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Load chi tiết khi click marker
  useEffect(() => {
    if (activeId === null) { setPlaceDetail(null); return; }

    // Nếu offline, dùng dữ liệu tĩnh ngay
    if (isOffline) {
      const found = staticPlaces.find((p) => p.id === activeId) ?? null;
      setPlaceDetail(found);
      return;
    }

    placeApi.get(activeId)
      .then(setPlaceDetail)
      .catch(() => {
        // Fallback sang dữ liệu tĩnh nếu API lỗi
        const found = staticPlaces.find((p) => p.id === activeId) ?? null;
        setPlaceDetail(found);
      });
  }, [activeId, isOffline]);

  // Tự phát audio theo GPS (chỉ khi GPS đang active)
  const gpsPosition = gps.status === "active" ? gps.position : null;
  useVisitSession({
    position: gpsPosition,
    places,
    lang,
    enabled: gps.status === "active",
  });

  // Highlight khu đang phát
  useEffect(() => {
    if (track) setActiveId(track.placeId);
  }, [track]);

  return (
    <>
      <section className="section map-page">
        <div className="section__heading">
          <div>
            <span className="eyebrow eyebrow--green">{t("eyebrow", lang)}</span>
            <h1>{t("title", lang)}</h1>
          </div>
          <p>{t("subtitle", lang)}</p>
        </div>

        {/* Thông báo offline nhẹ — không chặn bản đồ */}
        {isOffline && (
          <div className="map-offline-note">
            ℹ️ {t("offlineNote", lang)}
          </div>
        )}

        {/* GPS — không tự bật, user chủ động bấm */}
        <GpsStatus gps={gps} onStart={startGps} onStop={stopGps} lang={lang} />

        {/* Bản đồ — luôn hiện */}
        <div className="zoo-map-frame">
          <IllustratedMap
            places={places as Place[]}
            activePlaceId={activeId}
            lang={lang}
            onSelect={(p) => setActiveId(p.id)}
          />
        </div>

        {/* Chú thích */}
        <div className="map-legend">
          <span style={{ borderLeft: "3px solid #206a45", paddingLeft: 8 }}>🐘 {t("animal", lang)}</span>
          <span style={{ borderLeft: "3px solid #2d8a2d", paddingLeft: 8 }}>🌳 {t("plant", lang)}</span>
          <span style={{ borderLeft: "3px solid #5d3d85", paddingLeft: 8 }}>🏛️ {t("facility", lang)}</span>
        </div>
      </section>

      {/* Audio player cố định dưới màn hình */}
      <div className="audio-player-bar">
        <AudioPlayer lang={lang} />
      </div>

      {/* Panel chi tiết */}
      <SpotPanel
        place={placeDetail}
        lang={lang}
        onClose={() => setActiveId(null)}
      />
    </>
  );
}
