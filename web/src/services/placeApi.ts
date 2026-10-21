import { api } from "./api";
import type { Place, PlaceDetail } from "../types/place";

export const placeApi = {
  list:   ()                  => api.get<Place[]>("/api/places"),
  get:    (id: number)        => api.get<PlaceDetail>(`/api/places/${id}`),
  create: (body: Partial<Place>) => api.post<Place>("/api/admin/places", body),
  update: (id: number, body: Partial<Place>) => api.put<Place>(`/api/admin/places/${id}`, body),
  remove: (id: number)        => api.delete<void>(`/api/admin/places/${id}`),
};
