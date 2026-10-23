export interface Place {
  id: string;                        // Guid (string)
  slug: string;
  icon: string;
  category: "animal" | "plant" | "facility";
  lat: number;
  lng: number;
  radiusMeters: number;
  mapX: number;                      // pixel trên ảnh gốc
  mapY: number;
  priority: number;                  // số lớn hơn ưu tiên hơn
  isActive: boolean;
  imageUrl: string | null;
  name: Record<string, string>;      // { vi, en, zh, ko, ja }
  desc: Record<string, string>;
}

export interface PlaceDetail extends Place {
  revisionId?: string | null;
  version?: number | null;
  script?: Record<string, string>;
  /** audioUrl trả về từ API — không lộ đường dẫn vật lý */
  audioUrl: Record<string, string | null>;
}
