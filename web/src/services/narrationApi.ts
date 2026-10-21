import { api } from "./api";
import type { NarrationRevision } from "../types/narration";

export const narrationApi = {
  listRevisions: (placeId: number) =>
    api.get<NarrationRevision[]>(`/api/admin/places/${placeId}/narrations`),

  createRevision: (placeId: number, sourceText: string) =>
    api.post<NarrationRevision>(`/api/admin/places/${placeId}/narrations`, { sourceText }),

  publish: (revisionId: number) =>
    api.post<NarrationRevision>(`/api/admin/narrations/${revisionId}/publish`, {}),

  retryJob: (jobId: number) =>
    api.post<void>(`/api/admin/jobs/${jobId}/retry`, {}),
};
