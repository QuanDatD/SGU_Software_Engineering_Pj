import { api } from "./api";

export interface HourBucket {
  hour: number;
  count: number;
}

export interface ReportingData {
  totalVisits: number;
  hourBuckets: HourBucket[];
  peakHours: number[];
}

export const reportingApi = {
  /**
   * GET /api/admin/reporting?from=YYYY-MM-DD&to=YYYY-MM-DD&includeDemo=false
   * UC07.1–07.3: Lấy tổng lượt, 24 khung giờ (VN) và giờ cao điểm.
   */
  get: (from: string, to: string, includeDemo = false): Promise<ReportingData> =>
    api.get<ReportingData>(
      `/api/admin/reporting?from=${encodeURIComponent(from)}&to=${encodeURIComponent(to)}&includeDemo=${includeDemo}`,
    ),
};
