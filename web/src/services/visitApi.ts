import { api } from "./api";
import type { ZoneResult } from "../types/visit";

export const visitApi = {
  /** Gửi toạ độ GPS lên server, nhận về placeId đang ở trong vùng */
  checkZone: (lat: number, lng: number, accuracy: number) =>
    api.post<ZoneResult>("/api/visit/zone", { lat, lng, accuracy }),
};
