import type { GpsState } from "../types/visit";

interface Props {
  gps: GpsState;
  onStart: () => void;
  onStop:  () => void;
  lang: string;
}

const LABELS: Record<string, Record<string, string>> = {
  idle:        { vi: "Bật GPS để tự động phát", en: "Enable GPS for auto-play", zh: "启用GPS自动播放", ko: "자동 재생을 위해 GPS 켜기", ja: "自動再生にGPSを有効にする" },
  requesting:  { vi: "Đang xin quyền GPS…",     en: "Requesting GPS…",          zh: "正在请求GPS权限…",  ko: "GPS 권한 요청 중…",              ja: "GPS許可を要求中…" },
  denied:      { vi: "GPS bị từ chối",           en: "GPS denied",               zh: "GPS被拒绝",        ko: "GPS 거부됨",                      ja: "GPS拒否されました" },
  unavailable: { vi: "GPS không khả dụng",       en: "GPS unavailable",          zh: "GPS不可用",        ko: "GPS 사용 불가",                   ja: "GPS利用不可" },
  active:      { vi: "GPS đang hoạt động",       en: "GPS active",               zh: "GPS已启用",        ko: "GPS 활성",                        ja: "GPS有効" },
};

export default function GpsStatus({ gps, onStart, onStop, lang }: Props) {
  const l = LANGUAGES_FALLBACK(lang);

  const label = LABELS[gps.status]?.[l] ?? LABELS[gps.status]?.["en"] ?? gps.status;

  const accuracy = gps.status === "active" ? gps.position.accuracy : null;

  const icon =
    gps.status === "active"      ? "📍" :
    gps.status === "requesting"  ? "⏳" :
    gps.status === "denied"      ? "🚫" :
    gps.status === "unavailable" ? "⚠️" : "📡";

  return (
    <div className={`gps-status gps-status--${gps.status}`}>
      <span className="gps-status__icon">{icon}</span>
      <span className="gps-status__label">{label}</span>

      {accuracy !== null && (
        <span className="gps-status__accuracy">±{Math.round(accuracy)}m</span>
      )}

      {gps.status === "idle" || gps.status === "denied" || gps.status === "unavailable" ? (
        <button className="gps-status__btn" onClick={onStart} type="button">
          {l === "vi" ? "Bật" : "Enable"}
        </button>
      ) : gps.status === "active" ? (
        <button className="gps-status__btn gps-status__btn--stop" onClick={onStop} type="button">
          {l === "vi" ? "Tắt" : "Stop"}
        </button>
      ) : null}
    </div>
  );
}

function LANGUAGES_FALLBACK(lang: string): string {
  return ["vi", "en", "zh", "ko", "ja"].includes(lang) ? lang : "en";
}
