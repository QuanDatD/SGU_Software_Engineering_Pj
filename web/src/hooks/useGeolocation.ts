import { useState, useEffect, useRef, useCallback } from "react";
import type { GpsState, GpsPosition } from "../types/visit";

export function useGeolocation() {
  const [gps, setGps] = useState<GpsState>({ status: "idle" });
  const watchId  = useRef<number | null>(null);
  const lastPos  = useRef<GpsPosition | null>(null);

  const start = useCallback(() => {
    if (!navigator.geolocation) {
      setGps({ status: "unavailable" });
      return;
    }
    if (watchId.current !== null) navigator.geolocation.clearWatch(watchId.current);
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


        // Bỏ qua nếu ít di chuyển


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
