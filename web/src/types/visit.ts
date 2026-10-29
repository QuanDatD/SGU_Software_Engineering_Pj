export interface GpsPosition {
  lat: number;
  lng: number;
  accuracy: number; // mét
  timestamp: number;
}

/** Kết quả từ POST /api/visit/evaluate */
export interface EvaluateResult {
  placeId: string | null;  // Guid hoặc null
  accepted: boolean;
  reasonCode: string;
  distanceMeters: number | null; // mét tới tâm vùng
}

export type GpsState =
  | { status: "idle" }
  | { status: "requesting" }
  | { status: "denied" }
  | { status: "unavailable" }
  | { status: "active"; position: GpsPosition };
