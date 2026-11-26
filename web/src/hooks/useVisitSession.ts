import { useEffect, useLayoutEffect, useRef } from "react";
import type { GpsPosition } from "../types/visit";
import type { PlaceDetail } from "../types/place";
import { visitApi } from "../services/visitApi";
import { useAudio } from "../contexts/AudioContext";
import type { Lang } from "../types/common";
import type { useVisitHistory } from "./useVisitHistory";
import { findCurrentZone, isEligiblePosition } from "../utils/geofence";

interface Options {
  position: GpsPosition | null;
  places: PlaceDetail[];
  lang: Lang;
  enabled: boolean;
  locationSignal: AbortSignal;
  onEnter?: (id: string) => void;
  history: ReturnType<typeof useVisitHistory>;
}

export function useVisitSession(options: Options) {
  const audio = useAudio();
  const latest = useRef({ ...options, audio });
  const generation = useRef(0);
  const stable = useRef<{ id: string; count: number } | null>(null);
  const { locationSignal } = options;

  useLayoutEffect(() => {
    const previous = latest.current;
    const positionAvailabilityChanged = !!previous.position !== !!options.position;
    if (positionAvailabilityChanged) stable.current = null;
    if (positionAvailabilityChanged || previous.lang !== options.lang || previous.enabled !== options.enabled) {
      generation.current++;
    }
    latest.current = { ...options, audio };
  });

  useEffect(() => {
    let disposed = false;
    let timer: ReturnType<typeof setTimeout>;
    stable.current = null;

    async function poll() {
      const { position, lang } = latest.current;
      const requestGeneration = generation.current;
      const isCurrent = () => !disposed && !locationSignal.aborted
        && requestGeneration === generation.current;

      try {
        if (!isEligiblePosition(position)) {
          stable.current = null;
          return;
        }

        const result = await visitApi.evaluate(position.lat, position.lng, position.accuracy);
        if (!isCurrent()) return;

        const current = latest.current;
        const place = findCurrentZone(current.position, current.places);
        if (!result.accepted || !place || result.placeId !== place.id) {
          stable.current = null;
          return;
        }

        const id = place.id;
        const count = stable.current?.id === id ? stable.current.count + 1 : 1;
        stable.current = { id, count };
        if (count < 2) return;
        current.history.markVisited(id);

        const url = place.audioUrl[lang];
        if (!current.enabled || !url) return;
        const key = `${id}:${lang}:${url}`;
        const { heard, blocked, markHeard } = current.history;
        if (heard.current.has(key) || blocked.current.has(key)) return;

        const player = current.audio;
        if (player.state === "playing" || player.state === "loading" || player.state === "paused") return;

        const canStart = () => {
          if (!isCurrent() || !latest.current.enabled) return false;
          const zone = findCurrentZone(latest.current.position, latest.current.places);
          return zone?.id === id && zone.audioUrl[lang] === url;
        };
        current.onEnter?.(id);
        const played = await player.play({
          placeId: id,
          lang,
          url,
          title: place.name[lang] || place.name.vi || "",
        }, canStart);
        if (!canStart()) return;
        if (played) markHeard(id, lang, url);
        else blocked.current.add(key);
      } catch {
        if (isCurrent()) stable.current = null;
      } finally {
        if (!disposed && !locationSignal.aborted) timer = setTimeout(poll, 5000);
      }
    }

    void poll();
    return () => {
      disposed = true;
      clearTimeout(timer);
    };
  }, [locationSignal]);
}
