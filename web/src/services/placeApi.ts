import { api } from "./api";
import type { Place, PlaceDetail } from "../types/place";

export const placeApi = {
  /* Công khai — khu đang active, có bài đã xuất bản */
  list: (lang = "vi") =>
    api.get<Place[]>(`/api/places?lang=${lang}`),

  get: (id: string, lang = "vi") =>
    api.get<PlaceDetail>(`/api/places/${id}?lang=${lang}`),

  /* Admin */
  adminList: () =>
    api.get<Place[]>("/api/admin/places"),

  create: (body: Partial<Place>) =>
    api.post<Place>("/api/admin/places", body),

  update: (id: string, body: Partial<Place>) =>
    api.put<Place>(`/api/admin/places/${id}`, body),

  /** Ẩn khu (isActive = false), không xoá cứng */
  hide: (id: string) =>
    api.put<Place>(`/api/admin/places/${id}`, { isActive: false }),
};
