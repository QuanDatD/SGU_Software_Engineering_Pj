import { useState } from "react";
import { useLang } from "../contexts/LanguageContext";
import type { useGeolocation } from "../hooks/useGeolocation";
import { LANGUAGES, type Lang } from "../types/common";

interface VisitSetupProps {
    geolocation: ReturnType<typeof useGeolocation>;
    onComplete: () => void;
}

// Các dòng lần lượt là:
// tiêu đề, giải thích, bật GPS, chờ GPS,
// đã có GPS, bị từ chối, không lấy được GPS,
// mở bản đồ, tiếp tục thủ công.
const GPS_TEXT = {
    vi: [
        "Cho phép sử dụng vị trí",
        "Vị trí giúp xác định khu bạn đang đứng và hỗ trợ tự phát thuyết minh khi bạn bật chế độ này. Bạn vẫn có thể chọn điểm và nghe thủ công nếu không dùng GPS.",
        "Bật GPS",
        "Đang xác định vị trí…",
        "Đã nhận được vị trí.",
        "Bạn chưa cho phép truy cập vị trí. Bạn vẫn có thể nghe thủ công.",
        "Chưa lấy được vị trí. Bạn có thể thử lại hoặc nghe thủ công.",
        "Mở bản đồ",
        "Tiếp tục không dùng GPS",
    ],
    en: [
        "Allow location access",
        "Location identifies your current zone and supports automatic narration when enabled. You can still select places and listen manually without GPS.",
        "Enable GPS",
        "Finding your location…",
        "Location received.",
        "Location access was denied. You can still listen manually.",
        "Location is unavailable. Try again or listen manually.",
        "Open map",
        "Continue without GPS",
    ],
    zh: [
        "允许访问位置",
        "位置信息用于识别您所在的区域，并在开启自动播放后提供讲解。不使用GPS也可以选择景点手动收听。",
        "启用GPS",
        "正在获取位置…",
        "已获取位置。",
        "未允许访问位置，您仍可手动收听。",
        "暂时无法获取位置，请重试或手动收听。",
        "打开地图",
        "不使用GPS继续",
    ],
    ko: [
        "위치 접근 허용",
        "위치 정보는 현재 구역을 확인하고 자동 재생을 켰을 때 해설을 제공하는 데 사용됩니다. GPS 없이도 장소를 선택하여 직접 들을 수 있습니다.",
        "GPS 켜기",
        "위치를 확인하고 있습니다…",
        "위치를 확인했습니다.",
        "위치 접근이 거부되었습니다. 직접 선택하여 들을 수 있습니다.",
        "위치를 확인할 수 없습니다. 다시 시도하거나 직접 선택해 주세요.",
        "지도 열기",
        "GPS 없이 계속",
    ],
    ja: [
        "位置情報へのアクセス",
        "位置情報で現在のエリアを確認し、自動再生を有効にすると解説を再生できます。GPSを使わずに場所を選んで聞くこともできます。",
        "GPSを有効にする",
        "位置情報を取得しています…",
        "位置情報を取得しました。",
        "位置情報が許可されていません。手動で解説を聞くことができます。",
        "位置情報を取得できません。再試行するか、手動で聞いてください。",
        "地図を開く",
        "GPSを使わずに続ける",
    ],
} as const;

export default function VisitSetup({
    geolocation,
    onComplete,
}: VisitSetupProps) {
    const { lang, setLang } = useLang();

    const [step, setStep] = useState<"language" | "gps">("language");
    const [selectedLang, setSelectedLang] = useState<Lang | null>(null);

    const { gps, start, stop } = geolocation;
    const text = GPS_TEXT[lang];

    function confirmLanguage() {
        if (!selectedLang) return;

        setLang(selectedLang);
        setStep("gps");
    }

    function continueManually() {
        stop();
        onComplete();
    }

    return (
        <main
            style={{
                minHeight: "100dvh",
                display: "grid",
                placeItems: "center",
                padding: 24,
                background: "#edf6ef",
                color: "#163e2c",
            }}
        >
            <section
                style={{
                    width: "100%",
                    maxWidth: 520,
                    boxSizing: "border-box",
                    padding: 28,
                    borderRadius: 24,
                    background: "#fff",
                }}
            >
                <p>🌿 THẢO CẦM VIÊN</p>

                {step === "language" ? (
                    <>
                        <h1>Chọn ngôn ngữ / Choose your language</h1>

                        <div
                            style={{
                                display: "grid",
                                gap: 12,
                                marginBottom: 24,
                            }}
                        >
                            {LANGUAGES.map((item) => (
                                <label
                                    key={item.code}
                                    style={{
                                        padding: 14,
                                        borderRadius: 12,
                                        border: "1px solid #b7d7bf",
                                        cursor: "pointer",
                                        background:
                                            selectedLang === item.code
                                                ? "#e4f3e8"
                                                : "#fff",
                                    }}
                                >
                                    <input
                                        type="radio"
                                        name="initial-language"
                                        value={item.code}
                                        checked={selectedLang === item.code}
                                        onChange={() =>
                                            setSelectedLang(item.code)
                                        }
                                    />
                                    {" "}{item.flag} {item.label}
                                </label>
                            ))}
                        </div>

                        <button
                            type="button"
                            disabled={selectedLang === null}
                            onClick={confirmLanguage}
                        >
                            Tiếp tục / Continue
                        </button>
                    </>
                ) : (
                    <>
                        <h1>{text[0]}</h1>
                        <p style={{ lineHeight: 1.7 }}>{text[1]}</p>

                        <p role="status" aria-live="polite">
                            {gps.status === "requesting" && text[3]}
                            {gps.status === "active" && text[4]}
                            {gps.status === "denied" && text[5]}
                            {gps.status === "unavailable" && text[6]}
                        </p>

                        <div
                            style={{
                                display: "flex",
                                gap: 12,
                                flexWrap: "wrap",
                            }}
                        >
                            {gps.status === "active" ? (
                                <button
                                    type="button"
                                    onClick={onComplete}
                                >
                                    {text[7]}
                                </button>
                            ) : (
                                <button
                                    type="button"
                                    disabled={gps.status === "requesting"}
                                    onClick={start}
                                >
                                    {text[2]}
                                </button>
                            )}

                            <button
                                type="button"
                                onClick={continueManually}
                            >
                                {text[8]}
                            </button>
                        </div>
                    </>
                )}
            </section>
        </main>
    );
}