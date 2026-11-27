import { useEffect, useState } from "react";
import { visitApi } from "../services/visitApi";

const SESSION_KEY = "tcv_session_id";
const SESSION_EXPIRES = "tcv_session_expires";
const SESSION_TTL = 60 * 1000; // Giới hạn phiên 1 phút

function initQrSession(): number | null {
    const now = Date.now();
    const params = new URLSearchParams(window.location.search);
    const source = params.get("source")?.trim().toLowerCase();

    const existingId = localStorage.getItem(SESSION_KEY);
    const existingExpires = Number(localStorage.getItem(SESSION_EXPIRES) ?? 0);

    // 1. Người dùng vừa quét mã QR (?source=qr)
    if (source === "qr") {
        let sessionId = existingId;
        let expiresAt = existingExpires;

        // Nếu chưa có phiên hoặc phiên cũ đã hết hạn -> tạo phiên mới 1 phút
        if (!sessionId || now >= expiresAt) {
            sessionId = crypto.randomUUID();
            expiresAt = now + SESSION_TTL;
            localStorage.setItem(SESSION_KEY, sessionId);
            localStorage.setItem(SESSION_EXPIRES, String(expiresAt));
        }

        const isDemo = params.get("demo")?.trim().toLowerCase() === "true";
        visitApi.recordQrVisit(sessionId, isDemo).catch((error: unknown) => {
            console.error("Không ghi nhận được lượt truy cập QR:", error);
        });

        // Xóa ?source=qr khỏi thanh địa chỉ để khi F5 sau 1 phút không tự cấp lại phiên mới
        params.delete("source");
        const cleanQuery = params.toString();
        const cleanUrl = window.location.pathname + (cleanQuery ? `?${cleanQuery}` : "") + window.location.hash;
        window.history.replaceState({}, "", cleanUrl);

        return expiresAt;
    }

    // 2. Không có ?source=qr -> kiểm tra xem phiên cũ còn hạn không
    if (existingId && now < existingExpires) {
        return existingExpires;
    }

    // Hết hạn hoặc chưa quét QR -> xóa phiên cũ
    localStorage.removeItem(SESSION_KEY);
    localStorage.removeItem(SESSION_EXPIRES);
    return null;
}

export function useQrVisit(isQrPage: boolean) {
    const [expiresAt, setExpiresAt] = useState<number | null>(() =>
        isQrPage ? null : initQrSession(),
    );
    const [now, setNow] = useState(() => Date.now());

    useEffect(() => {
        if (isQrPage) return;

        const timer = window.setInterval(() => {
            const current = Date.now();
            setNow(current);

            if (expiresAt && current >= expiresAt) {
                localStorage.removeItem(SESSION_KEY);
                localStorage.removeItem(SESSION_EXPIRES);
                setExpiresAt(null);
            }
        }, 1000);

        return () => window.clearInterval(timer);
    }, [isQrPage, expiresAt]);

    const remainingSeconds =
        expiresAt && expiresAt > now ? Math.ceil((expiresAt - now) / 1000) : 0;

    // Hết hạn khi không phải trang /qr và không còn thời gian phiên
    const isExpired = !isQrPage && remainingSeconds <= 0;

    return { isExpired, remainingSeconds };
}