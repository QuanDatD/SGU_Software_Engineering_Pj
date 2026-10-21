import { useState, useEffect, useRef, useCallback } from "react";
import type { GpsState, GpsPosition } from "../types/visit";

/* Chỉ cập nhật nếu di chuyển > MIN_DELTA mét hoặc độ chính xác thay đổi đáng kể */
const MIN_DISTANCE_M = 3;
const MAX_ACCURACY_M = 30; // bỏ qua nếu accuracy > 30m

function haversineM(a: GpsPosition, b: GpsPosition): number {
  const R = 6_371_000;
  const dLat = ((b.lat - a.lat) * Math.PI) / 180;
  const dLng = ((b.lng - a.lng) * Math.PI) / 180;
  const s =
    Math.sin(dLat / 2) ** 2 +
    Math.cos((a.lat * Math.PI) / 180) *
      Math.cos((b.lat * Math.PI) / 180) *
      Math.sin(dLng / 2) ** 2;
  return R * 2 * Math.atan2(Math.sqrt(s), Math.sqrt(1 - s));
}

export function useGeolocation() {
  const [gps, setGps] = useState<GpsState>({ status: "idle" });
  const watchId  = useRef<number | null>(null);
  const lastPos  = useRef<GpsPosition | null>(null);

  const start = useCallback(() => {
    if (!navigator.geolocation) {
      setGps({ status: "unavailable" });
      return;
    }
    setGps({ status: "requesting" });

    watchId.current = navigator.geolocation.watchPosition(
      (pos) => {
        const next: GpsPosition = {
          lat:       pos.coords.latitude,
          lng:       pos.coords.longitude,
          accuracy:  pos.coords.accuracy,
          timestamp: pos.timestamp,
        };

        // Bỏ qua nếu quá kém
        if (next.accuracy > MAX_ACCURACY_M) return;

        // Bỏ qua nếu ít di chuyển
        if (lastPos.current && haversineM(lastPos.current, next) < MIN_DISTANCE_M) return;

        lastPos.current = next;
        setGps({ status: "active", position: next });
      },
      (err) => {
        if (err.code === 1) setGps({ status: "denied" });
        else                setGps({ status: "unavailable" });
      },
      { enableHighAccuracy: true, maximumAge: 5_000, timeout: 15_000 },
    );
  }, []);

  const stop = useCallback(() => {
    if (watchId.current !== null) {
      navigator.geolocation.clearWatch(watchId.current);
      watchId.current = null;
    }
    setGps({ status: "idle" });
    lastPos.current = null;
  }, []);

  // Dọn dẹp khi unmount
  useEffect(() => () => {
    if (watchId.current !== null)
      navigator.geolocation.clearWatch(watchId.current);
  }, []);

  return { gps, start, stop };
}
