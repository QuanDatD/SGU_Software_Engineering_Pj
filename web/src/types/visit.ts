export interface GpsPosition {
  lat: number;
  lng: number;
  accuracy: number; // mét
  timestamp: number;
}

export interface ZoneResult {
  placeId: number | null;  // null = không trong vùng nào
  distance: number | null; // mét tới tâm vùng gần nhất
}

export type GpsState =
  | { status: "idle" }
  | { status: "requesting" }
  | { status: "denied" }
  | { status: "unavailable" }
  | { status: "active"; position: GpsPosition };
