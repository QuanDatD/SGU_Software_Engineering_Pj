import { api, uploadForm, mediaUrl } from "./api";
import type { Place, PlaceDetail } from "../types/place";

function toPlace(item: PlaceDetail): PlaceDetail {
  return { ...item,
    imageUrl: item.imageUrl ? mediaUrl(item.imageUrl) : null,
    audioUrl: Object.fromEntries(Object.entries(item.audioUrl ?? {}).map(([lang, url]) => [lang, url ? mediaUrl(url) : null])),
  };
}
export const placeApi = {
  async list(lang = "vi"): Promise<PlaceDetail[]> {
    return (await api.get<PlaceDetail[]>(`/api/places?lang=${encodeURIComponent(lang)}`)).map(toPlace);
  },
  async get(id: string, lang = "vi"): Promise<PlaceDetail> {
    return toPlace(await api.get<PlaceDetail>(`/api/places/${id}?lang=${encodeURIComponent(lang)}`));
  },
  async adminList(): Promise<PlaceDetail[]> {
    return (await api.get<PlaceDetail[]>("/api/admin/places")).map(toPlace);
  },
  hide: (id: string) => api.patch<void>(`/api/admin/places/${id}/hide`, {}),
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