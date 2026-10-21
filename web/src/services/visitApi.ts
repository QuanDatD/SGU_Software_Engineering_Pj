import { api } from "./api";
import type { EvaluateResult } from "../types/visit";

export const visitApi = {
  /**
   * POST /api/visit/evaluate
   * Gửi toạ độ GPS, nhận placeId đang ở trong vùng
   */
  evaluate: (lat: number, lng: number, accuracy: number) =>
    api.post<EvaluateResult>("/api/visit/evaluate", { lat, lng, accuracy }),
};
