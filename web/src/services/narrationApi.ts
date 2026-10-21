import { api } from "./api";
import type { NarrationRevision, NarrationTranslation } from "../types/narration";

export const narrationApi = {
  /* Lấy danh sách phiên bản của một khu */
  listRevisions: (placeId: string) =>
    api.get<NarrationRevision[]>(`/api/admin/places/${placeId}/revisions`),

  /* Tạo phiên bản nháp mới */
  createRevision: (placeId: string, sourceText: string) =>
    api.post<NarrationRevision>(`/api/admin/places/${placeId}/revisions`, { sourceText }),

  /* Sửa nội dung bản dịch theo ngôn ngữ */
  updateTranslation: (revisionId: string, lang: string, text: string) =>
    api.put<NarrationTranslation>(
      `/api/admin/revisions/${revisionId}/translations/${lang}`,
      { text },
    ),

  /* Tạo công việc generate audio cho tất cả 5 ngôn ngữ */
  generate: (revisionId: string) =>
    api.post<NarrationRevision>(`/api/admin/revisions/${revisionId}/generate`, {}),

  /* Xem tiến độ các job */
  listJobs: (revisionId: string) =>
    api.get<NarrationRevision["jobs"]>(`/api/admin/revisions/${revisionId}/jobs`),

  /* Thử lại job lỗi */
  retryJob: (jobId: string) =>
    api.post<void>(`/api/admin/jobs/${jobId}/retry`, {}),

  /* Xuất bản phiên bản */
  publish: (revisionId: string) =>
    api.post<NarrationRevision>(`/api/admin/revisions/${revisionId}/publish`, {}),
};
