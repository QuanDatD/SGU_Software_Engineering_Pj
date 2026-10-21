/* ======================================================
   useVisitSession
   - Gửi GPS lên server mỗi POLL_MS
   - Nhận placeId đang ở trong vùng
   - Tự phát audio khi vào vùng mới (chống phát chồng & lặp)
   - Không phát lại khu đã nghe trong session
   ====================================================== */
import { useEffect, useRef, useCallback } from "react";
import type { GpsPosition } from "../types/visit";
import type { PlaceDetail } from "../types/place";
import { visitApi } from "../services/visitApi";
import { useAudio } from "../contexts/AudioContext";
import type { Lang } from "../types/common";

const POLL_MS         = 5_000;  // gửi GPS mỗi 5 giây
const STABLE_COUNT    = 2;      // phải ở trong vùng N lần liên tiếp trước khi phát
const MAX_ACCURACY_M  = 25;     // không xử lý nếu kém hơn

interface Options {
  position: GpsPosition | null;
  places: PlaceDetail[];
  lang: Lang;
  enabled: boolean;             // false khi GPS bị từ chối
}

export function useVisitSession({ position, places, lang, enabled }: Options) {
  const { play, track, state } = useAudio();

  const stableRef   = useRef<{ placeId: number; count: number } | null>(null);
  const heardSet    = useRef<Set<number>>(new Set()); // khu đã nghe trong session
  const lastPollRef = useRef<number>(0);
  const timerRef    = useRef<ReturnType<typeof setTimeout> | null>(null);

  const poll = useCallback(async () => {
    if (!position || !enabled) return;
    if (position.accuracy > MAX_ACCURACY_M) return;

    const now = Date.now();
    if (now - lastPollRef.current < POLL_MS) return;
    lastPollRef.current = now;

    try {
      const result = await visitApi.checkZone(position.lat, position.lng, position.accuracy);

      if (result.placeId === null) {
        stableRef.current = null;
        return;
      }

      const pid = result.placeId;

      // Tích lũy stable count
      if (stableRef.current?.placeId === pid) {
        stableRef.current.count += 1;
      } else {
        stableRef.current = { placeId: pid, count: 1 };
      }

      // Đủ ổn định + chưa nghe + không đang phát cùng khu
      if (
        stableRef.current.count >= STABLE_COUNT &&
        !heardSet.current.has(pid) &&
        !(state === "playing" && track?.placeId === pid)
      ) {
        const placeDetail = places.find((p) => p.id === pid);
        const audioUrl = placeDetail?.audioUrl?.[lang];

        if (audioUrl) {
          heardSet.current.add(pid);
          play({
            placeId: pid,
            lang,
            url: audioUrl,
            title: placeDetail?.name?.[lang] ?? String(pid),
          });
        }
      }
    } catch { /* lỗi mạng — bỏ qua */ }
  }, [position, enabled, places, lang, play, track, state]);

  // Chạy poll theo interval
  useEffect(() => {
    if (!enabled || !position) return;

    timerRef.current = setTimeout(poll, POLL_MS);
    return () => {
      if (timerRef.current) clearTimeout(timerRef.current);
    };
  }, [position, enabled, poll]);

  /** Reset khu đã nghe (dùng khi khách bắt đầu tour lại) */
  const resetSession = useCallback(() => {
    heardSet.current.clear();
    stableRef.current = null;
  }, []);

  return { resetSession };
}
