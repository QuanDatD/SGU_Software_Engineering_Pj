import { useEffect } from "react";
import { visitApi } from "../services/visitApi";

const SESSION_KEY  = "tcv_session_id";
const SESSION_TIME = "tcv_session_time";
const SESSION_TTL  = 30 * 60 * 1000; // 30 phút — BR-22

/**
 * Lấy hoặc tạo mới SessionId theo quy tắc BR-22.
 * SessionId tồn tại trong sessionStorage + timestamp trong localStorage.
 * Nếu hơn 30 phút không hoạt động → tạo phiên mới.
 */
function getOrCreateSessionId(): string {
  const now = Date.now();
  const lastTime = Number(localStorage.getItem(SESSION_TIME) ?? 0);

  // Tạo phiên mới nếu đã quá TTL hoặc chưa có
  if (now - lastTime > SESSION_TTL) {
    const newId = crypto.randomUUID();
    sessionStorage.setItem(SESSION_KEY, newId);
    localStorage.setItem(SESSION_TIME, String(now));
    return newId;
  }

  // Khôi phục phiên trong cùng tab
  let id = sessionStorage.getItem(SESSION_KEY);
  if (!id) {
    id = crypto.randomUUID();
    sessionStorage.setItem(SESSION_KEY, id);
  }
  localStorage.setItem(SESSION_TIME, String(now));
  return id;
}

/**
 * UC01.3 — Ghi nhận lượt truy cập qua QR.
 * Chỉ kích hoạt khi URL có tham số `source=qr` (BR-20).
 * Không chặn luồng chính của ứng dụng.
 */
export function useQrVisit() {
  useEffect(() => {
    const params = new URLSearchParams(window.location.search);
    if (params.get("source") !== "qr") return; // BR-20: chỉ ghi nhận từ QR

    const sessionId = getOrCreateSessionId();
    // BR-26: đánh dấu demo nếu URL có tham số demo=true
    const isDemo = params.get("demo") === "true";

    // Fire-and-forget — không chặn UI dù backend lỗi
    visitApi.recordQrVisit(sessionId, isDemo).catch(() => {
      // Ghi nhận thất bại không ảnh hưởng hành trình của du khách
    });
  }, []); // chỉ chạy 1 lần khi mount
}
