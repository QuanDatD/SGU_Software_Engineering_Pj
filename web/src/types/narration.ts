export type JobStatus = "queued" | "processing" | "done" | "failed";

export interface GenerationJob {
  id: number;
  lang: string;
  status: JobStatus;
  stage: string | null;
  errorMessage: string | null;
  audioUrl: string | null;
  createdAt: string;
  updatedAt: string;
}

export interface NarrationRevision {
  id: number;
  placeId: number;
  version: number;
  status: "draft" | "published" | "archived";
  sourceText: string;
  jobs: GenerationJob[];
  createdAt: string;
}
