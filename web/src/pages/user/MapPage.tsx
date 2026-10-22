import React, { useEffect, useRef, useState } from "react";
import L from "leaflet";
import "leaflet/dist/leaflet.css";
import { type Lang } from "../../i18n";

// Sửa lỗi hiển thị icon mặc định của Leaflet trên Vite
delete (L.Icon.Default.prototype as any)._getIconUrl;
L.Icon.Default.mergeOptions({
    iconRetinaUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon-2x.png",
    iconUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-icon.png",
    shadowUrl: "https://unpkg.com/leaflet@1.9.4/dist/images/marker-shadow.png",
});

// Icon vị trí người dùng (chấm xanh radar)
const userLocationIcon = L.divIcon({
    className: "user-gps-marker",
    html: `
    <div style="position: relative; width: 24px; height: 24px;">
      <div style="position: absolute; width: 24px; height: 24px; border-radius: 50%; background: rgba(37, 99, 235, 0.3); animation: pulse 2s infinite;"></div>
      <div style="position: absolute; top: 4px; left: 4px; width: 16px; height: 16px; border-radius: 50%; background: #2563eb; border: 3px solid #ffffff; box-shadow: 0 0 6px rgba(0,0,0,0.4);"></div>
    </div>
  `,
    iconSize: [24, 24],
    iconAnchor: [12, 12],
});

// Icon điểm tham quan
const spotIcon = (name: string) =>
    L.divIcon({
        className: "spot-marker",
        html: `
      <div style="display: flex; flex-direction: column; align-items: center; cursor: pointer;">
        <div style="background: #10b981; color: white; padding: 4px 8px; border-radius: 12px; font-weight: bold; font-size: 11px; white-space: nowrap; box-shadow: 0 2px 6px rgba(0,0,0,0.3); border: 1px solid white;">
          📍 ${name}
        </div>
      </div>
    `,
        iconSize: [100, 30],
        iconAnchor: [50, 15],
    });

// Dữ liệu 7 địa điểm Thảo Cầm Viên
const ZOO_SPOTS = [
    { id: "1", name: "Cổng chính & Đền Hùng", lat: 10.78755, lng: 106.70529, radius: 25, desc: "Cổng vào lịch sử và khu tưởng niệm Đền Vua Hùng." },
    { id: "2", name: "Khu Voi Châu Á", lat: 10.78842, lng: 106.70588, radius: 30, desc: "Nơi sinh sống của đàn voi Châu Á quý hiếm." },
    { id: "3", name: "Khu Hươu Cao Cổ", lat: 10.78915, lng: 106.70645, radius: 25, desc: "Khu nuôi dưỡng đàn hươu cao cổ thân thiện." },
    { id: "4", name: "Khu Hổ Đông Dương", lat: 10.78985, lng: 106.70512, radius: 30, desc: "Khu bảo tồn loài hổ Đông Dương dũng mãnh." },
    { id: "5", name: "Vườn Chim & Bò Sát", lat: 10.78892, lng: 106.70425, radius: 25, desc: "Hơn 50 loài chim nhiệt đới và bò sát quý hiếm." },
    { id: "6", name: "Vườn Bách Thảo cổ thụ", lat: 10.78795, lng: 106.70695, radius: 35, desc: "Bộ sưu tập cây cổ thụ hàng trăm năm tuổi." },
    { id: "7", name: "Bảo tàng Lịch sử TP.HCM", lat: 10.78712, lng: 106.70485, radius: 30, desc: "Nơi lưu giữ hàng ngàn hiện vật lịch sử văn hóa." },
];

// Tính khoảng cách giữa 2 tọa độ GPS (theo mét)
function getDistanceMeters(lat1: number, lon1: number, lat2: number, lon2: number) {
    const R = 6371e3;
    const dLat = ((lat2 - lat1) * Math.PI) / 180;
    const dLon = ((lon2 - lon1) * Math.PI) / 180;
    const a =
        Math.sin(dLat / 2) * Math.sin(dLat / 2) +
        Math.cos((lat1 * Math.PI) / 180) *
        Math.cos((lat2 * Math.PI) / 180) *
        Math.sin(dLon / 2) *
        Math.sin(dLon / 2);
    const c = 2 * Math.atan2(Math.sqrt(a), Math.sqrt(1 - a));
    return Math.round(R * c);
}

interface MapPageProps {
    t: any;
    lang: Lang;
}

export default function MapPage({ t, lang }: MapPageProps) {
    const mapContainerRef = useRef<HTMLDivElement>(null);
    const mapInstanceRef = useRef<L.Map | null>(null);
    const userMarkerRef = useRef<L.Marker | null>(null);
    const userAccuracyCircleRef = useRef<L.Circle | null>(null);

    const [userCoords, setUserCoords] = useState<{ lat: number; lng: number; accuracy: number } | null>(null);
    const [gpsStatus, setGpsStatus] = useState<"idle" | "requesting" | "active" | "error">("idle");
    const [activeSpot, setActiveSpot] = useState<typeof ZOO_SPOTS[0] | null>(null);
    const [watchId, setWatchId] = useState<number | null>(null);

    // 1. Khởi tạo bản đồ Leaflet
    useEffect(() => {
        if (!mapContainerRef.current || mapInstanceRef.current) return;

        // Tọa độ trung tâm Thảo Cầm Viên
        const zooCenter: [number, number] = [10.7882, 106.7055];

        const map = L.map(mapContainerRef.current, {
            center: zooCenter,
            zoom: 17,
            maxZoom: 19,
            minZoom: 15,
            zoomControl: false,
        });

        // Lớp bản đồ nền OpenStreetMap
        L.tileLayer("https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png", {
            attribution: '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
        }).addTo(map);

        // Chuyển nút zoom xuống góc dưới phải
        L.control.zoom({ position: "bottomright" }).addTo(map);

        // Vẽ 7 khu vực (Marker + Vòng tròn bán kính Geofence)
        ZOO_SPOTS.forEach((spot) => {
            // Vòng tròn Geofence
            L.circle([spot.lat, spot.lng], {
                color: "#10b981",
                fillColor: "#34d399",
                fillOpacity: 0.15,
                radius: spot.radius,
                weight: 1.5,
            }).addTo(map);

            // Marker có nhãn tên
            const marker = L.marker([spot.lat, spot.lng], {
                icon: spotIcon(spot.name),
            }).addTo(map);

            marker.on("click", () => {
                setActiveSpot(spot);
                map.flyTo([spot.lat, spot.lng], 18, { animate: true });
            });
        });

        mapInstanceRef.current = map;

        return () => {
            map.remove();
            mapInstanceRef.current = null;
        };
    }, []);

    // 2. Kích hoạt lấy vị trí GPS thời gian thực
    const startGpsTracking = () => {
        if (!navigator.geolocation) {
            alert("Thiết bị hoặc trình duyệt không hỗ trợ Geolocation!");
            return;
        }

        setGpsStatus("requesting");

        const id = navigator.geolocation.watchPosition(
            (position) => {
                const { latitude, longitude, accuracy } = position.coords;
                setUserCoords({ lat: latitude, lng: longitude, accuracy });
                setGpsStatus("active");

                const map = mapInstanceRef.current;
                if (!map) return;

                // Cập nhật hoặc tạo Marker người dùng
                if (!userMarkerRef.current) {
                    userMarkerRef.current = L.marker([latitude, longitude], { icon: userLocationIcon }).addTo(map);
                    userAccuracyCircleRef.current = L.circle([latitude, longitude], {
                        radius: accuracy,
                        color: "#3b82f6",
                        fillColor: "#60a5fa",
                        fillOpacity: 0.2,
                        weight: 1,
                    }).addTo(map);

                    // Lần đầu tiên bắt được GPS -> bay ngay đến vị trí người dùng
                    map.flyTo([latitude, longitude], 18, { animate: true });
                } else {
                    userMarkerRef.current.setLatLng([latitude, longitude]);
                    userAccuracyCircleRef.current?.setLatLng([latitude, longitude]);
                    userAccuracyCircleRef.current?.setRadius(accuracy);
                }

                // Tự động kiểm tra xem người dùng có bước vào khu vực nào không
                ZOO_SPOTS.forEach((spot) => {
                    const dist = getDistanceMeters(latitude, longitude, spot.lat, spot.lng);
                    if (dist <= spot.radius) {
                        setActiveSpot(spot);
                    }
                });
            },
            (error) => {
                console.error("Lỗi GPS:", error);
                setGpsStatus("error");
            },
            {
                enableHighAccuracy: true,
                maximumAge: 1000,
                timeout: 10000,
            }
        );

        setWatchId(id);
    };

    // 3. Nút bay về vị trí người dùng
    const focusOnUser = () => {
        if (userCoords && mapInstanceRef.current) {
            mapInstanceRef.current.flyTo([userCoords.lat, userCoords.lng], 18, { animate: true });
        } else {
            startGpsTracking();
        }
    };

    // Dọn dẹp watchPosition khi thoát trang
    useEffect(() => {
        return () => {
            if (watchId !== null) navigator.geolocation.clearWatch(watchId);
        };
    }, [watchId]);

    return (
        <div style= {{ position: "relative", width: "100%", height: "calc(100vh - 65px)", overflow: "hidden" }
}>
{/* Khung bản đồ Leaflet */ }
    < div ref = { mapContainerRef } style = {{ width: "100%", height: "100%", zIndex: 1 }} />

{/* 4. BẢNG HIỂN THỊ TỌA ĐỘ GPS (Góc trên bên trái) */ }
<div
        style={
    {
        position: "absolute",
            top: "16px",
                left: "16px",
                    zIndex: 1000,
                        background: "rgba(255, 255, 255, 0.95)",
                            padding: "12px 16px",
                                borderRadius: "12px",
                                    boxShadow: "0 4px 15px rgba(0,0,0,0.15)",
                                        backdropFilter: "blur(6px)",
                                            maxWidth: "310px",
                                                fontSize: "13px",
                                                    border: "1px solid #e2e8f0",
        }
}
      >
    <div style={ { display: "flex", alignItems: "center", justifyContent: "space-between", marginBottom: "8px" } }>
        <strong style={ { color: "#0f172a", fontSize: "14px" } }>🛰️ Tọa độ GPS của bạn </strong>
            < span
style = {{
    padding: "2px 8px",
        borderRadius: "10px",
            fontSize: "11px",
                fontWeight: 600,
                    backgroundColor:
    gpsStatus === "active" ? "#dcfce7" : gpsStatus === "error" ? "#fee2e2" : "#f1f5f9",
        color: gpsStatus === "active" ? "#166534" : gpsStatus === "error" ? "#991b1b" : "#475569",
            }}
          >
{ gpsStatus === "active"
    ? "Đang định vị"
    : gpsStatus === "requesting"
        ? "Đang tìm GPS..."
        : gpsStatus === "error"
            ? "Chưa cấp quyền"
            : "Chưa bật"}
</span>
    </div>

{
    userCoords ? (
        <div>
        <div style= {{ display: "grid", gridTemplateColumns: "1fr 1fr", gap: "6px", fontFamily: "monospace" }
}>
    <div>
    <span style={ { color: "#64748b" } }> Lat: </span> <b>{userCoords.lat.toFixed(6)}</b >
        </div>
        < div >
        <span style={ { color: "#64748b" } }> Lng: </span> <b>{userCoords.lng.toFixed(6)}</b >
            </div>
            </div>
            < div style = {{ marginTop: "6px", color: "#64748b", fontSize: "12px" }}>
                Độ chính xác: <b style={ { color: "#0f172a" } }>±{ Math.round(userCoords.accuracy) } m </b>
                    </div>
                    </div>
        ) : (
    <p style= {{ margin: "6px 0 0", color: "#64748b", fontSize: "12px" }}>
        Nhấn nút định vị bên dưới để quét vị trí bước chân của bạn trong Thảo Cầm Viên.
          </p>
        )}
</div>

{/* 5. NÚT ĐỊNH VỊ NHANH VỀ VỊ TRÍ TÔI (Góc phải dưới) */ }
<button
        onClick={ focusOnUser }
title = "Vị trí của tôi"
style = {{
    position: "absolute",
        bottom: "95px",
            right: "16px",
                zIndex: 1000,
                    width: "48px",
                        height: "48px",
                            borderRadius: "50%",
                                backgroundColor: "#ffffff",
                                    border: "2px solid #2563eb",
                                        boxShadow: "0 4px 12px rgba(0,0,0,0.2)",
                                            cursor: "pointer",
                                                display: "flex",
                                                    alignItems: "center",
                                                        justifyContent: "center",
                                                            fontSize: "20px",
        }}
      >
        🎯
</button>

{/* 6. MODAL / THẺ THÔNG TIN ĐIỂM THAM QUAN KHI ĐẾN GẦN HOẶC CLICK */ }
{
    activeSpot && (
        <div
          style={
        {
            position: "absolute",
                bottom: "20px",
                    left: "50%",
                        transform: "translateX(-50%)",
                            zIndex: 1000,
                                background: "#ffffff",
                                    width: "calc(100% - 32px)",
                                        maxWidth: "420px",
                                            borderRadius: "16px",
                                                padding: "16px",
                                                    boxShadow: "0 10px 25px rgba(0,0,0,0.2)",
                                                        border: "2px solid #10b981",
          }
    }
        >
        <div style={ { display: "flex", justifyContent: "space-between", alignItems: "flex-start" } }>
            <div>
            <span style={ { fontSize: "12px", color: "#059669", fontWeight: 700 } }>
                🌿 KHU VỰC THAM QUAN
        </span>
        < h3 style = {{ margin: "4px 0", color: "#0f172a", fontSize: "17px" }
}> { activeSpot.name } </h3>
    </div>
    < button
onClick = {() => setActiveSpot(null)}
style = {{ background: "none", border: "none", fontSize: "18px", cursor: "pointer", color: "#94a3b8" }}
            >
              ✕
</button>
    </div>

    < p style = {{ margin: "8px 0 12px", color: "#475569", fontSize: "13px", lineHeight: "1.4" }}>
    { activeSpot.desc }
        </p>

        < div style = {{ display: "flex", justifyContent: "space-between", alignItems: "center" }}>
            <span style={ { fontSize: "12px", color: "#64748b" } }>
                {
                    userCoords
                    ? `Cách bạn: ${getDistanceMeters(userCoords.lat, userCoords.lng, activeSpot.lat, activeSpot.lng)}m`
                : `Bán kính nhận diện: ${activeSpot.radius}m`}
</span>
    < button
onClick = {() => alert(`Đang kích hoạt thuyết minh âm thanh: ${activeSpot.name}`)}
style = {{
    backgroundColor: "#10b981",
        color: "white",
            border: "none",
                padding: "8px 16px",
                    borderRadius: "8px",
                        fontWeight: 600,
                            cursor: "pointer",
                                display: "flex",
                                    alignItems: "center",
                                        gap: "6px",
              }}
            >
              🔊 Nghe thuyết minh
    </button>
    </div>
    </div>
      )}

{/* Hiệu ứng nhấp nháy cho Chấm GPS */ }
<style>{`
        @keyframes pulse {
          0% { transform: scale(1); opacity: 0.8; }
          70% { transform: scale(2.4); opacity: 0; }
          100% { transform: scale(1); opacity: 0; }
        }
      `}</style>
    </div>
  );
}