export type JobStatus = "queued" | "processing" | "succeeded" | "failed";
export type JobStage  = "translate" | "synthesize" | "store";
export type RevisionStatus = "draft" | "published" | "archived";

export interface GenerationJob {
  id: string;          // Guid
  lang: string;
  status: JobStatus;
  stage: JobStage | null;
  errorMessage: string | null;
  audioUrl: string | null;   // URL công khai khi succeeded
  createdAt: string;
  updatedAt: string;
}

export interface NarrationTranslation {
  lang: string;
  text: string;
  isApproved: boolean;
}

export interface NarrationRevision {
  id: string;          // Guid
  placeId: string;
  version: number;
  status: RevisionStatus;
  sourceText: string;          // bản gốc tiếng Việt
  translations: NarrationTranslation[];
  jobs: GenerationJob[];
  createdAt: string;
}
