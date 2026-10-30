import { useState } from "react";
import type { PlaceDetail } from "../types/place";
import type { useGpsSimulator } from "../hooks/useGpsSimulator";
import { useLang } from "../contexts/LanguageContext";
import { useSiteTexts } from "../hooks/useSiteTexts";

interface Coordinate {
    lat: number;
    lng: number;
}

interface Props {
    places: PlaceDetail[];
    simulator: ReturnType<typeof useGpsSimulator>;
    onStart: (
        from: Coordinate,
        to: Coordinate,
        speed: number,
    ) => void;
    onStop: () => void;
}

export default function GpsSimulationPanel({
    places,
    simulator,
    onStart,
    onStop,
}: Props) {
    const { lang } = useLang();
    const texts = useSiteTexts(lang);
    const t = (key: string) => texts[key] ?? key;

    const [fromId, setFromId] = useState("outside");
    const [targetId, setTargetId] = useState("");
    const [speed, setSpeed] = useState(2);
    const [error, setError] = useState("");

    const target =
        places.find((place) => place.id === targetId) ??
        places[0];

    function handleStart() {
        setError("");

        if (!target) {
            setError("Chưa có điểm tham quan để giả lập.");
            return;
        }

        let from: Coordinate;

        if (fromId === "outside") {
            // Bắt đầu ở phía bắc, ngoài vòng kích hoạt của đích.
            const offsetMeters =
                Math.max(target.radiusMeters, 25) + 30;

            from = {
                lat: target.lat + offsetMeters / 111_320,
                lng: target.lng,
            };
        } else {
            const departure = places.find(
                (place) => place.id === fromId,
            );

            if (!departure) {
                setError("Điểm xuất phát không còn trong danh sách.");
                return;
            }

            from = {
                lat: departure.lat,
                lng: departure.lng,
            };
        }

        try {
            onStart(
                from,
                { lat: target.lat, lng: target.lng },
                speed,
            );
        } catch (cause) {
            setError(
                cause instanceof Error
                    ? cause.message
                    : "Không bắt đầu được giả lập.",
            );
        }
    }

    const percent = Math.round(simulator.progress * 100);

    return (
        <details className="live-map__simulation">
            	<summary>🧪 {t("sim.title")}</summary>

            <p>
                	{t("sim.note")}
            </p>

            <label>
                	{t("sim.from")}
                <select
                    value={fromId}
                    disabled={simulator.enabled}
                    onChange={(event) => setFromId(event.target.value)}
                >
                    <option value="outside">
                        	{t("sim.outside")}
                    </option>

                    {places.map((place) => (
                        <option key={place.id} value={place.id}>
                            {place.name[lang] || place.name.vi || place.slug}
                        </option>
                    ))}
                </select>
            </label>

            <label>
                	{t("sim.to")}
                <select
                    value={target?.id ?? ""}
                    disabled={simulator.enabled || places.length === 0}
                    onChange={(event) => setTargetId(event.target.value)}
                >
                    {places.length === 0 && (
                        <option value="">	{t("sim.empty")}</option>
                    )}

                    {places.map((place) => (
                        <option key={place.id} value={place.id}>
                            {place.name.vi || place.slug}
                        </option>
                    ))}
                </select>
            </label>

            <label>
                	{t("sim.speed")}
                <select
                    value={speed}
                    disabled={simulator.enabled}
                    onChange={(event) =>
                        setSpeed(Number(event.target.value))
                    }
                >
                    <option value={1}>1 m/s — {t("sim.slow")}</option>
                    <option value={2}>2 m/s — {t("sim.medium")}</option>
                    <option value={4}>4 m/s — {t("sim.fast")}</option>
                </select>
            </label>

            {error && <p role="alert">{error}</p>}

            {!simulator.enabled ? (
                <button
                    type="button"
                    disabled={places.length === 0}
                    onClick={handleStart}
                >
                    	{t("sim.start")}
                </button>
            ) : (
                <>
                    <p role="status">
                        {simulator.progress >= 1
                            ? t("sim.arrived")
                            : simulator.moving
                              ? t("sim.moving")
                              : t("sim.paused")}
                        {" · "}{percent}%
                    </p>

                    <progress
                        value={simulator.progress}
                        max={1}
                        aria-label="Tiến độ di chuyển giả lập"
                    />

                    <div>
                        {simulator.progress < 1 && (
                            <button
                                type="button"
                                onClick={
                                    simulator.moving
                                        ? simulator.pause
                                        : simulator.resume
                                }
                            >
                                {simulator.moving
                                     ? t("sim.pause")
                                    : t("sim.resume")}
                            </button>
                        )}

                        <button type="button" onClick={onStop}>
                            	{t("sim.stop")}
                        </button>
                    </div>
                </>
            )}
        </details>
    );
}