export type Lang = "vi" | "en" | "zh" | "ko" | "ja";

export const LANGUAGES: { code: Lang; label: string; flag: string }[] = [
  { code: "vi", label: "Tiếng Việt", flag: "🇻🇳" },
  { code: "en", label: "English",    flag: "🇬🇧" },
  { code: "zh", label: "中文",        flag: "🇨🇳" },
  { code: "ko", label: "한국어",       flag: "🇰🇷" },
  { code: "ja", label: "日本語",       flag: "🇯🇵" },
];

export interface ApiError {
  status: number;
  code: string;
  message: string;
}
