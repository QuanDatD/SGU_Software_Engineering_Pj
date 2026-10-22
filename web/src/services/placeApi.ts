import { api, uploadForm } from "./api";
import type { Place, PlaceDetail } from "../types/place";

interface ApiPlace {
  id: string;
  code: string;
  category: Place["category"];
  language: string;

  name: string;
  summary: string;
  script: string;

  latitude: number;
  longitude: number;
  radiusMeters: number;
  priority: number;

  mapX: number;
  mapY: number;
  isActive: boolean;

  imageUrl: string | null;
  audioUrl: string | null;

  names: Record<string, string>;
  descriptions: Record<string, string>;
}

function toPlace(item: ApiPlace): PlaceDetail {
  const icons: Record<Place["category"], string> = {
    animal: "🐾",
    plant: "🌳",
    facility: "📍",
  };

  return {
    id: item.id,
    slug: item.code,
    icon: icons[item.category] ?? "📍",
    category: item.category,

    lat: item.latitude,
    lng: item.longitude,
    radiusMeters: item.radiusMeters,
    priority: item.priority,

    mapX: item.mapX,
    mapY: item.mapY,
    isActive: item.isActive,
    imageUrl: item.imageUrl,

    name: item.names,
    desc: item.descriptions,

    audioUrl: {
      [item.language]: item.audioUrl,
    },
  };
}

export const placeApi = {
  async list(lang = "vi"): Promise<PlaceDetail[]> {
    const result = await api.get<ApiPlace[]>(
      `/api/places?lang=${encodeURIComponent(lang)}`,
    );

    return result.map(toPlace);
  },

  async get(id: string, lang = "vi"): Promise<PlaceDetail> {
    const result = await api.get<ApiPlace>(
      `/api/places/${id}?lang=${encodeURIComponent(lang)}`,
    );

    return toPlace(result);
  },

  async adminList(): Promise<Place[]> {
    const result = await api.get<ApiPlace[]>(
      "/api/admin/places",
    );

    return result.map(toPlace);
  },

  // Các API ghi sẽ triển khai ở bước tiếp theo.
  create: (body: Partial<Place>) =>
    api.post<Place>("/api/admin/places", body),

  update: (id: string, body: Partial<Place>) =>
    api.put<Place>(`/api/admin/places/${id}`, body),

  hide: (id: string) =>
    api.put<Place>(
      `/api/admin/places/${id}`,
      { isActive: false },
    ),
};
export interface SavePlaceDraftRequest {
  nameVi: string;
  descriptionVi: string;
  category: Place["category"];

  latitude: number;
  longitude: number;
  radiusMeters: number;

  mapX: number;
  mapY: number;
  priority: number;

  image: File | null;
}

export async function savePlaceDraft(
  id: string | undefined,
  input: SavePlaceDraftRequest,
): Promise<void> {
  const form = new FormData();

  form.append("NameVi", input.nameVi);
  form.append("DescriptionVi", input.descriptionVi);
  form.append("Category", input.category);

  form.append("Latitude", String(input.latitude));
  form.append("Longitude", String(input.longitude));
  form.append("RadiusMeters", String(input.radiusMeters));

  form.append("MapX", String(input.mapX));
  form.append("MapY", String(input.mapY));
  form.append("Priority", String(input.priority));

  if (input.image) {
    form.append("Image", input.image);
  }

  const path = id
    ? `/api/admin/places/${encodeURIComponent(id)}`
    : "/api/admin/places";

  await uploadForm<unknown>(
    path,
    id ? "PUT" : "POST",
    form,
  );
}