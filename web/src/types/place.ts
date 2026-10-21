export interface Place {
  id: number;
  slug: string;           // "elephant", "giraffe" ...
  icon: string;           // emoji
  category: "animal" | "plant" | "facility";
  lat: number;
  lng: number;
  radiusMeters: number;   // bán kính vùng geofence
  mapX: number;           // % trên ảnh minh hoạ (0–100)
  mapY: number;           // % trên ảnh minh hoạ (0–100)
  imageUrl: string | null;
  name: Record<string, string>;   // { vi, en, zh, ko, ja }
  desc: Record<string, string>;
}

export interface PlaceDetail extends Place {
  audioUrl: Record<string, string | null>; // { vi: "/audio/...", en: null, ... }
}
