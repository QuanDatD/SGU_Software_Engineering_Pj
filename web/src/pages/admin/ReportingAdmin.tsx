import { useCallback, useEffect, useState } from "react";
import { reportingApi, type ReportingData } from "../../services/reportingApi";

function todayStr(): string {
  // Dùng locale VN để tránh lệch ngày do UTC offset — BR-23
  const now = new Date();
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function sevenDaysAgoStr(): string {
  const now = new Date();
  now.setDate(now.getDate() - 6);
  const y = now.getFullYear();
  const m = String(now.getMonth() + 1).padStart(2, "0");
  const d = String(now.getDate()).padStart(2, "0");
  return `${y}-${m}-${d}`;
}

function fmtHour(h: number): string {
  return `${String(h).padStart(2, "0")}:00`;
}

export default function ReportingAdmin() {
  const [from, setFrom] = useState(sevenDaysAgoStr());
  const [to, setTo] = useState(todayStr());
  const [includeDemo, setIncludeDemo] = useState(false);
  const [data, setData] = useState<ReportingData | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);
  const [applied, setApplied] = useState(false);

  const handleApply = useCallback(async () => {
    if (!from || !to) {
        setError("Vui lòng chọn ngày bắt đầu và kết thúc.");
        return;
    }

    if (from > to) {
        setError("Ngày bắt đầu không được sau ngày kết thúc.");
        return;
    }

    // Ngày kết thúc trên giao diện là ngày được tính vào báo cáo.
    // API nhận mốc kết thúc không bao gồm, nên cộng thêm một ngày.
    const toExclusive = new Date(`${to}T00:00:00Z`);

    toExclusive.setUTCDate(toExclusive.getUTCDate() + 1);

    const toStr = toExclusive.toISOString().slice(0, 10);

    setLoading(true);
    setError(null);

    try {
        const result = await reportingApi.get(
            from,
            toStr,
            includeDemo,
        );

        setData(result);
        setApplied(true);
    } catch (error: unknown) {
        setData(null);

        setError(
            error instanceof Error
                ? error.message
                : "Không tải được dữ liệu báo cáo.",
        );
    } finally {
        setLoading(false);
    }
}, [from, to, includeDemo]);

useEffect(() => {
    void handleApply();
}, [handleApply]);

  const maxCount = data ? Math.max(...data.hourBuckets.map(b => b.count), 1) : 1;

  return (
    <div className="admin-section">
      <div className="admin-section__header">
        <h2>Báo cáo thống kê</h2>
      </div>

      <div className="admin-notice">
        <span>ℹ️</span>
        <div>
          <strong>Lượt truy cập qua QR</strong>
          <p>
            Đếm số lần khách mở website từ liên kết QR. Mỗi phiên trình duyệt chỉ được tính một lần.
            Thời gian hiển thị theo <strong>giờ Việt Nam</strong>.
          </p>
        </div>
      </div>

      {/* Bộ lọc */}
      <div style={{ display: "flex", flexWrap: "wrap", gap: 14, alignItems: "flex-end", marginBottom: 24, background: "#f8fbf6", border: "1px solid #e0e8da", borderRadius: 12, padding: 16 }}>
        <label className="admin-form__field" style={{ minWidth: 160 }}>
          <span>Từ ngày</span>
          <input type="date" value={from} max={to} onChange={e => setFrom(e.target.value)} />
        </label>
        <label className="admin-form__field" style={{ minWidth: 160 }}>
          <span>Đến ngày (bao gồm)</span>
          <input type="date" value={to} min={from} max={todayStr()} onChange={e => setTo(e.target.value)} />
        </label>
        <label className="admin-form__field" style={{ flexDirection: "row", alignItems: "center", gap: 8 }}>
          <input type="checkbox" checked={includeDemo} onChange={e => setIncludeDemo(e.target.checked)} />
          <span>Gồm lượt demo</span>
        </label>
        <button className="admin-btn admin-btn--primary" onClick={() => void handleApply()} disabled={loading}>
          {loading ? "⏳ Đang tải…" : "Áp dụng"}
        </button>
      </div>

      {error && <p role="alert" style={{ color: "#b42318", marginBottom: 16 }}>{error}</p>}

      {applied && data && (
        <>
          {/* Tổng lượt */}
          <div style={{ display: "flex", gap: 16, marginBottom: 28, flexWrap: "wrap" }}>
            <div style={{ background: "#fff", border: "1px solid #e0e8da", borderRadius: 12, padding: "20px 28px", minWidth: 180 }}>
              <div style={{ fontSize: 12, color: "#627568", fontWeight: 700, marginBottom: 6 }}>TỔNG LƯỢT TRUY CẬP</div>
              <div style={{ fontSize: 36, fontWeight: 800, color: "#143e2b" }}>{data.totalVisits.toLocaleString("vi-VN")}</div>
              <div style={{ fontSize: 12, color: "#8aad94", marginTop: 4 }}>
                {from} → {to}{includeDemo ? " (gồm demo)" : ""}
              </div>
            </div>

            {/* Giờ cao điểm */}
            <div style={{ background: "#fff", border: "1px solid #e0e8da", borderRadius: 12, padding: "20px 28px", minWidth: 220 }}>
              <div style={{ fontSize: 12, color: "#627568", fontWeight: 700, marginBottom: 6 }}>GIỜ CAO ĐIỂM</div>
              {data.totalVisits === 0 ? (
                <div style={{ fontSize: 14, color: "#8aad94" }}>Chưa có dữ liệu</div>
              ) : (
                <>
                  <div style={{ display: "flex", flexWrap: "wrap", gap: 8 }}>
                    {data.peakHours.map(h => (
                      <span key={h} style={{
                        background: "#e8f4ec", color: "#143e2b",
                        borderRadius: 8, padding: "6px 14px",
                        fontSize: 18, fontWeight: 800,
                      }}>
                        {fmtHour(h)}
                      </span>
                    ))}
                  </div>
                  <div style={{ fontSize: 12, color: "#8aad94", marginTop: 6 }}>
                    {data.hourBuckets.find(b => b.hour === data.peakHours[0])?.count ?? 0} lượt
                    {data.peakHours.length > 1 && " — đồng hạng"}
                  </div>
                </>
              )}
            </div>
          </div>

          {/* Biểu đồ phân bố theo 24 giờ (UC07.2) */}
          <h3 className="admin-subtitle">Phân bố lượt truy cập theo giờ trong ngày</h3>
          <p className="admin-hint">
            Tổng hợp tất cả các ngày trong khoảng lọc. Giờ Việt Nam (GMT+7).
          </p>

          {data.totalVisits === 0 ? (
            <p className="admin-hint">Không có lượt truy cập trong khoảng thời gian đã chọn.</p>
          ) : (
            <div style={{ overflowX: "auto" }}>
              <div style={{
                display: "grid",
                gridTemplateColumns: "repeat(24, minmax(36px, 1fr))",
                gap: 4,
                alignItems: "flex-end",
                minWidth: 700,
                padding: "8px 0 0",
              }}>
                {data.hourBuckets.map(bucket => {
                  const pct = Math.round((bucket.count / maxCount) * 100);
                  const isPeak = data.peakHours.includes(bucket.hour);
                  return (
                    <div key={bucket.hour} style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 4 }}>
                      <div style={{ fontSize: 11, fontWeight: 700, color: isPeak ? "#206a45" : "#627568" }}>
                        {bucket.count > 0 ? bucket.count : ""}
                      </div>
                      <div
                        style={{
                          width: "100%",
                          height: Math.max(pct * 1.2, bucket.count > 0 ? 4 : 0),
                          background: isPeak ? "#206a45" : "#a8d4b8",
                          borderRadius: "4px 4px 0 0",
                          transition: "height 0.3s ease",
                          minHeight: bucket.count > 0 ? 4 : 0,
                        }}
                        title={`${fmtHour(bucket.hour)}: ${bucket.count} lượt`}
                      />
                      <div style={{ fontSize: 10, color: "#8aad94", writingMode: "vertical-rl", transform: "rotate(180deg)", height: 28 }}>
                        {fmtHour(bucket.hour)}
                      </div>
                    </div>
                  );
                })}
              </div>
            </div>
          )}

          {/* Bảng chi tiết */}
          {data.totalVisits > 0 && (
            <>
              <h3 className="admin-subtitle" style={{ marginTop: 28 }}>Chi tiết theo giờ</h3>
              <div style={{ display: "grid", gridTemplateColumns: "repeat(auto-fill, minmax(120px, 1fr))", gap: 8 }}>
                {data.hourBuckets.filter(b => b.count > 0).map(bucket => (
                  <div key={bucket.hour} style={{
                    padding: "10px 12px",
                    background: data.peakHours.includes(bucket.hour) ? "#e8f4ec" : "#fff",
                    border: `1px solid ${data.peakHours.includes(bucket.hour) ? "#b8dfc8" : "#e0e8da"}`,
                    borderRadius: 8,
                    display: "flex",
                    justifyContent: "space-between",
                    alignItems: "center",
                    fontSize: 13,
                  }}>
                    <span style={{ fontWeight: 600 }}>{fmtHour(bucket.hour)}</span>
                    <span style={{ color: "#206a45", fontWeight: 700 }}>{bucket.count}</span>
                  </div>
                ))}
              </div>
            </>
          )}
        </>
      )}

      {!applied && !loading && (
        <p className="admin-hint">Chọn khoảng thời gian và nhấn "Áp dụng" để xem báo cáo.</p>
      )}
    </div>
  );
}
