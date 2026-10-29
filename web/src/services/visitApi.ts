import { api } from "./api";
import type { EvaluateResult } from "../types/visit";

export const visitApi = {
  /**
   * POST /api/visit/evaluate
   * Gửi toạ độ GPS, nhận placeId đang ở trong vùng
   */
  evaluate: (lat: number, lng: number, accuracy: number) =>
    api.post<EvaluateResult>("/api/visit/evaluate", { lat, lng, accuracy }),

  /**
   * POST /api/visit/qr
   * Ghi nhận lượt truy cập qua QR (UC01.3, BR-20 – BR-26)
   */
  recordQrVisit: (sessionId: string, isDemo: boolean) =>
    api.post<{ recorded: boolean; message: string }>("/api/visit/qr", { sessionId, isDemo }),
};
