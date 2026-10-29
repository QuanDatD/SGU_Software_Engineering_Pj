import { useEffect, useRef, useState } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import "../../styles/MapPage.css";
import { useLang } from "../../contexts/LanguageContext";
import { useAudio } from "../../contexts/AudioContext";
import { placeApi } from "../../services/placeApi";
import type { useGeolocation } from "../../hooks/useGeolocation";
import { useVisitSession } from "../../hooks/useVisitSession";
import type { PlaceDetail } from "../../types/place";
import AudioPlayer from "../../components/AudioPlayer";
import type { useVisitHistory } from "../../hooks/useVisitHistory";
import GpsSimulationPanel from "../../components/GpsSimulationPanel";
import type { useGpsSimulator } from "../../hooks/useGpsSimulator";
interface MapPageProps {
    geolocation: ReturnType<typeof useGeolocation>;
    selectedId: string | null;
    onSelectPlace: (id: string | null) => void;
    autoPlay: boolean;
    onAutoPlayChange: (enabled: boolean) => void;
    visitHistory: ReturnType<typeof useVisitHistory>;
    simulator: ReturnType<typeof useGpsSimulator>;

    onStartSimulation: (
        from: { lat: number; lng: number },
        to: { lat: number; lng: number },
        speed: number,
    ) => void;
}

export default function MapPage({
    geolocation,
    selectedId,
    onSelectPlace: setSelectedId,
    autoPlay,
    onAutoPlayChange: setAutoPlay,
    visitHistory,
    simulator,
    onStartSimulation,
}: MapPageProps) {
  const { lang } = useLang();
  const audio = useAudio();
  const { gps, start, stop } = geolocation;
  const position = gps.status === "active" ? gps.position : null;
  const container = useRef<HTMLDivElement>(null);
  const map = useRef<L.Map | null>(null);
  const [places, setPlaces] = useState<PlaceDetail[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState("");
  const selected = places.find(p => p.id === selectedId) ?? null;
  useVisitSession({
    position,
    places,
    lang,
    enabled: autoPlay && !error,
    onEnter: setSelectedId,
    history: visitHistory,
});

  // One sequential request at a time. Refresh after publication from any other browser.
  useEffect(() => {
    let disposed = false;
    let pending = false;
    async function refresh() {
      if (pending) return;
      pending = true;
      try {
        const result = await placeApi.list(lang);
        if (!disposed) { setPlaces(result); setError(""); }
      } catch (e) {
        if (!disposed) setError(e instanceof Error ? e.message : "Không kết nối được API.");
      } finally { pending = false; if (!disposed) setLoading(false); }
    }
    void refresh();
    const timer = window.setInterval(refresh, 10000);
    window.addEventListener("focus", refresh);
    return () => { disposed = true; clearInterval(timer); window.removeEventListener("focus", refresh); };
  }, [lang]);

  // Stop stale audio on language changes, hide, or a newly published revision.
  // Đổi ngôn ngữ hoặc điểm không còn công khai thì dừng.
// Xuất bản phiên bản mới không tự ngắt bài đang nghe.
useEffect(() => {
    const track = audio.track;

    if (!track) return;

    if (track.lang !== lang) {
        audio.stop();
        return;
    }

    // Chưa tải xong hoặc đang lỗi API thì chưa đủ căn cứ
    // kết luận điểm đang nghe đã bị ẩn.
    if (loading || error) return;

    const current = places.find(
        (place) => place.id === track.placeId,
    );

    if (!current) {
        audio.stop();
    }
}, [
    places,
    lang,
    loading,
    error,
    audio.track,
    audio.stop,
]);

  useEffect(() => {
    if (!container.current) return;
    const instance = L.map(container.current, { center: [10.7882, 106.7055], zoom: 17, zoomControl: false });
    L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
      attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>', maxZoom: 19,
    }).addTo(instance);
    L.control.zoom({ position: "bottomright" }).addTo(instance);
    map.current = instance;
    return () => { instance.remove(); map.current = null; };
  }, []);

  useEffect(() => {
    const instance = map.current;
    if (!instance) return;
    const layer = L.layerGroup().addTo(instance);
    for (const place of places) {
      if (!Number.isFinite(place.lat) || !Number.isFinite(place.lng)) continue;
      L.circle([place.lat, place.lng], { radius: place.radiusMeters, color: "#168652", fillOpacity: .15, weight: 1.5 }).addTo(layer);
      // DOM textContent avoids executing markup in Admin-supplied names.
      const label = document.createElement("span");
      label.textContent = `${place.icon} ${place.name[lang] || place.name.vi || place.slug}`;
      label.style.cssText = "background:#168652;color:white;border:2px solid white;border-radius:14px;padding:5px 9px;white-space:nowrap";
      L.marker([place.lat, place.lng], { icon: L.divIcon({ html: label, className: "spot-marker", iconSize: [110, 30] }) })
        .on("click", () => { setSelectedId(place.id); instance.flyTo([place.lat, place.lng], 18); }).addTo(layer);
    }
    return () => { layer.remove(); };
  }, [places, lang]);

  useEffect(() => {
    if (!map.current || !position) return;
    const layer = L.layerGroup().addTo(map.current);
    L.circleMarker([position.lat, position.lng], { radius: 8, fillColor: "#2563eb", fillOpacity: 1, color: "white", weight: 3 }).addTo(layer);
    L.circle([position.lat, position.lng], { radius: position.accuracy, color: "#2563eb", weight: 1, fillOpacity: .08 }).addTo(layer);
    return () => { layer.remove(); };
  }, [position]);

  const url = selected?.audioUrl[lang];
  const sameTrack = !!url && audio.track?.url === url && audio.track.lang === lang;
  const playing = sameTrack && (audio.state === "playing" || audio.state === "loading");
  return (
    <div className="live-map">
      <div ref={container} className="live-map__canvas" />
      <GpsSimulationPanel
            places={places}
            simulator={simulator}
            onStart={onStartSimulation}
            onStop={stop}
        />
      <section className="live-map__gps">
        <strong>
            {simulator.enabled ? "🧪 GPS giả lập" : "🛰️ GPS"}
            {" · "}{places.length} khu đã xuất bản
        </strong>
        <small>
            Đã tới: {visitHistory.visitedPlaceIds.length} điểm
            {" · "}
            Đã nghe: {visitHistory.heardPlaceIds.length} điểm
        </small>
        <div>
          <button onClick={() => { if (position) map.current?.flyTo([position.lat, position.lng], 18); else start(); }}>📍 Vị trí của tôi</button>
          <button onClick={() => { stop(); setAutoPlay(false); }}>Tắt GPS</button>
        </div>
        {position ? <small>{position.lat.toFixed(6)}, {position.lng.toFixed(6)} · ±{Math.round(position.accuracy)} m</small>
          : <small>{gps.status === "denied" ? "Bạn chưa cho phép truy cập vị trí." : gps.status === "unavailable" ? "Không lấy được GPS. Kiểm tra HTTPS và quyền vị trí." : gps.status === "requesting" ? "Đang tìm GPS…" : "Bật GPS để xác định khu đang đứng."}</small>}
        <label><input type="checkbox" checked={autoPlay} onChange={e => { setAutoPlay(e.target.checked); if (e.target.checked && !position) start(); }} /> Tự phát khi vào vùng</label>
        {position && position.accuracy > 25 && <small>GPS chưa đủ chính xác để tự phát (cần ≤25 m).</small>}
        {loading && <small>Đang lấy dữ liệu từ API…</small>}
        {error && <small role="alert">Không cập nhật được dữ liệu: {error}. Đang thử lại; tự phát tạm dừng.</small>}
        {!loading && !error && places.length === 0 && <small>Chưa có khu đang bật với thuyết minh đã xuất bản.</small>}
      </section>
      {selected && <section className="live-map__detail">
        <button className="live-map__close" onClick={() => setSelectedId(null)} aria-label="Đóng">✕</button>
        {selected.imageUrl && <img src={selected.imageUrl} alt={selected.name[lang] || selected.name.vi} />}
        <h3>{selected.name[lang] || selected.name.vi}</h3>
        <p>{selected.desc[lang] || selected.desc.vi}</p>
        <small>Vùng GPS: {selected.radiusMeters} m · Phiên bản {selected.version}</small>
        <details><summary>Nội dung thuyết minh ({lang.toUpperCase()})</summary><p>{selected.script?.[lang] || "Chưa có bản dịch."}</p></details>
        <button disabled={!url} onClick={() => {
          if (playing) audio.pause();
          else if (sameTrack && audio.state === "paused") audio.resume();
          else if (url) void audio.play({ placeId: selected.id, lang, url, title: selected.name[lang] || selected.name.vi });
        }}>{playing ? "⏸ Tạm dừng" : url ? "▶ Nghe thuyết minh" : "Chưa có âm thanh ngôn ngữ này"}</button>
        {sameTrack && audio.state === "error" && <p role="alert">Không phát được âm thanh. Nhấn Nghe để thử lại hoặc cho phép trình duyệt phát âm thanh.</p>}
      </section>}
      <AudioPlayer lang={lang} />
    </div>
  );
}
