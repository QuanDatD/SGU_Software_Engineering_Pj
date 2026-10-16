import { useEffect, useRef } from "react";
import * as L from "leaflet";

import "leaflet/dist/leaflet.css";
import "../../styles/MapPage.css";

// Tọa độ để mở bản đồ tại khu vực Thảo Cầm Viên.
// Đây chưa phải tọa độ của từng khu động vật.
const ZOO_CENTER: L.LatLngTuple = [10.7875, 106.705];

export default function MapPage() {
  // Phần tử HTML chứa bản đồ.
  const containerRef = useRef<HTMLDivElement | null>(null);

  // Lưu bản đồ để nút "Về Thảo Cầm Viên" sử dụng.
  const mapRef = useRef<L.Map | null>(null);

  useEffect(() => {
    if (!containerRef.current) return;

    // Tạo bản đồ.
    const map = L.map(containerRef.current).setView(
      ZOO_CENTER,
      17
    );

    mapRef.current = map;

    // Thêm lớp hình ảnh bản đồ từ OpenStreetMap.
    L.tileLayer(
      "https://tile.openstreetmap.org/{z}/{x}/{y}.png",
      {
        maxZoom: 19,
        attribution:
          '&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a> contributors',
      }
    ).addTo(map);

    // Cập nhật kích thước khi khung hiển thị thay đổi.
    const observer = new ResizeObserver(() => {
      map.invalidateSize();
    });

    observer.observe(containerRef.current);

    // Dọn bản đồ khi chuyển về trang chủ.
    // Cũng giúp tránh lỗi tạo bản đồ hai lần trong StrictMode.
    return () => {
      observer.disconnect();
      map.remove();
      mapRef.current = null;
    };
  }, []);

  function returnToZoo() {
    mapRef.current?.setView(ZOO_CENTER, 17);
  }

  return (
    <section className="section map-page">
      <div className="section__heading">
        <div>
          <span className="eyebrow eyebrow--green">
            HÀNH TRÌNH CỦA BẠN
          </span>

          <h1>Bản đồ tham quan</h1>
        </div>

        <p>
          Khám phá không gian xanh tại Thảo Cầm Viên.
        </p>
      </div>

      <div className="map-toolbar">
        <span>Kéo bản đồ để xem các khu vực</span>

        <button
          className="map-home-button"
          type="button"
          onClick={returnToZoo}
        >
          📍 Về Thảo Cầm Viên
        </button>
      </div>

      <div className="zoo-map-frame">
        <div
          ref={containerRef}
          className="zoo-map"
          role="region"
          aria-label="Bản đồ khu vực Thảo Cầm Viên"
        />
      </div>

      <p className="map-help">
        Dùng nút + / − để phóng to hoặc thu nhỏ.
        Trên điện thoại, dùng hai ngón tay để thay đổi độ phóng đại.
      </p>
    </section>
  );
}