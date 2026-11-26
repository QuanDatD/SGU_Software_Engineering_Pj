import { useEffect } from "react";
import { visitApi } from "../services/visitApi";

const SESSION_KEY = "tcv_session_id";
const SESSION_TIME = "tcv_session_time";
const SESSION_TTL = 30 * 60 * 1000; // 30 phút — BR-22

function getOrCreateSessionId(): string {
    const now = Date.now();
    const lastTime = Number(localStorage.getItem(SESSION_TIME) ?? 0);
    const existingId = localStorage.getItem(SESSION_KEY);

    // Dùng lại SessionId nếu còn trong TTL 30 phút (kể cả khi mở tab mới từ QR)
    if (existingId && now - lastTime <= SESSION_TTL) {
        localStorage.setItem(SESSION_TIME, String(now));
        return existingId;
    }

    const newId = crypto.randomUUID();
    localStorage.setItem(SESSION_KEY, newId);
    localStorage.setItem(SESSION_TIME, String(now));
    return newId;
}

export function useQrVisit() {
    useEffect(() => {
        const params = new URLSearchParams(window.location.search);
        const source = params.get("source")?.trim().toLowerCase();
        if (source !== "qr") return; // Thống nhất dấu nguồn QR

        const sessionId = getOrCreateSessionId();
        const isDemo = params.get("demo")?.trim().toLowerCase() === "true";

        visitApi.recordQrVisit(sessionId, isDemo).catch((error: unknown) => {
            console.error("Không ghi nhận được lượt truy cập QR:", error);
        });
    }, []);
}