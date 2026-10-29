import { useEffect, useRef, useCallback } from "react";
import type { GpsPosition } from "../types/visit";
import type { PlaceDetail } from "../types/place";
import { visitApi } from "../services/visitApi";
import { useAudio } from "../contexts/AudioContext";
import type { Lang } from "../types/common";
interface Options {
  position: GpsPosition | null; places: PlaceDetail[]; lang: Lang; enabled: boolean;
  onEnter?: (id: string) => void;
}
export function useVisitSession(options: Options) {
  const audio = useAudio();
  const latest = useRef({ ...options, audio });
  useEffect(() => { latest.current = { ...options, audio }; });
  const heard = useRef(new Set<string>());
  const blocked = useRef(new Set<string>());
  useEffect(() => {
    if (audio.state === "playing" && audio.track)
      heard.current.add(`${audio.track.placeId}:${audio.track.lang}:${audio.track.url}`);
  }, [audio.state, audio.track]);
  const stable = useRef<{ id: string; count: number } | null>(null);
  useEffect(() => {
    let disposed = false;
    let timer: ReturnType<typeof setTimeout>;
    async function poll() {
      const { position, lang, enabled, onEnter, audio } = latest.current;
      try {
        if (!enabled || !position || position.accuracy > 25 || Date.now() - position.timestamp > 15000) {
          stable.current = null; return;
        }
        const result = await visitApi.evaluate(position.lat, position.lng, position.accuracy);
        if (disposed || !latest.current.enabled || latest.current.lang !== lang) return;
        if (!result.accepted || !result.placeId) { stable.current = null; return; }
        const id = result.placeId;
        stable.current = stable.current?.id === id ? { id, count: stable.current.count + 1 } : { id, count: 1 };
        const place = latest.current.places.find(p => p.id === id);
        const url = place?.audioUrl[lang];
        const key = `${id}:${lang}:${url}`;
        if (stable.current.count >= 2 && url && !heard.current.has(key) && !blocked.current.has(key)) {
          if (audio.state === "playing" || audio.state === "loading") return;
          onEnter?.(id);
          const played = await audio.play({ placeId: id, lang, url, title: place?.name[lang] ?? "" });
          // A blocked autoplay remains retryable with the manual Listen button.
          if (played) heard.current.add(key); else blocked.current.add(key);
        }
      } catch { stable.current = null; }
      finally { if (!disposed) timer = setTimeout(poll, 5000); }
    }
    void poll();
    return () => { disposed = true; clearTimeout(timer); };
  }, []);
  const resetSession = useCallback(() => { heard.current.clear(); blocked.current.clear(); stable.current = null; }, []);
  return { resetSession };
}
