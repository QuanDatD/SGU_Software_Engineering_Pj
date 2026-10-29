/* ======================================================
   AudioContext — một player duy nhất toàn app
   Đảm bảo không phát chồng, không lặp liên tục.
   ====================================================== */
import {
  createContext, useContext, useRef, useState, useEffect,
  useCallback, type ReactNode,
} from "react";

import { mediaUrl } from "../services/api";

export interface TrackInfo {
  placeId: string;
  lang: string;
  url: string;
  title: string;
}

export type PlayerState = "idle" | "loading" | "playing" | "paused" | "ended" | "error";

interface AudioCtx {
  track: TrackInfo | null;
  state: PlayerState;
  currentTime: number;
  duration: number;
  /** Phát bài mới (dừng bài cũ nếu đang phát) */
  play: (
    track: TrackInfo,
    canStart?: () => boolean,
) => Promise<boolean>;
  /** Tiếp tục bài đang tạm dừng */
  resume: () => void;
  pause: () => void;
  stop: () => void;
  seek: (seconds: number) => void;
}

const Ctx = createContext<AudioCtx | null>(null);

export function AudioProvider({ children }: { children: ReactNode }) {
  const audioRef  = useRef<HTMLAudioElement | null>(null);
  const blobUrlRef = useRef<string | null>(null);
  const downloadRef = useRef<AbortController | null>(null);
  const requestIdRef = useRef(0);
  const [track,   setTrack]   = useState<TrackInfo | null>(null);
  const [state,   setState]   = useState<PlayerState>("idle");
  const [currentTime, setCurrentTime] = useState(0);
  const [duration,    setDuration]    = useState(0);

  /* Đảm bảo chỉ có một HTMLAudioElement */
  function getAudio(): HTMLAudioElement {
    if (!audioRef.current) {
      const el = new Audio();
      el.preload = "auto";

      
      el.addEventListener("playing",       () => setState("playing"));
      el.addEventListener("pause",      () => setState((s) => s !== "ended" ? "paused" : s));
      el.addEventListener("ended",      () => { setState("ended"); });
      el.addEventListener("error",      () => setState("error"));
      el.addEventListener("timeupdate", () => setCurrentTime(el.currentTime));
      el.addEventListener("durationchange", () => setDuration(el.duration || 0));

      audioRef.current = el;
    }
    return audioRef.current;
  }

    const play = useCallback(async (
    next: TrackInfo,
    canStart?: () => boolean,
): Promise<boolean> => {
	if (canStart && !canStart()) {
    return false;
}
    const el = getAudio();
    const fullAudioUrl = mediaUrl(next.url);

    if (
        track?.url === fullAudioUrl &&
        (state === "playing" || state === "loading")
    ) {
        return true;
    }

    const requestId = ++requestIdRef.current;

    downloadRef.current?.abort();

    const controller = new AbortController();
    downloadRef.current = controller;

    el.pause();
    el.removeAttribute("src");
    el.load();

    if (blobUrlRef.current) {
        URL.revokeObjectURL(blobUrlRef.current);
        blobUrlRef.current = null;
    }

    // Track vẫn giữ URL API để nhận diện bài và phiên bản.
    setTrack({
        ...next,
        url: fullAudioUrl,
    });

    setCurrentTime(0);
    setDuration(0);
    setState("loading");

    try {
        const response = await fetch(fullAudioUrl, {
            credentials: "include",
            cache: "no-store",
            signal: controller.signal,
        });

        if (!response.ok) {
            throw new Error(`Không tải được audio: HTTP ${response.status}`);
        }

        const blob = await response.blob();

        if (blob.size === 0) {
            throw new Error("File audio rỗng.");
        }

        if (requestId !== requestIdRef.current) {
            return false;
        }

        // File đã tải trọn; trình phát sử dụng bản trong bộ nhớ.
        const blobUrl = URL.createObjectURL(blob);
        blobUrlRef.current = blobUrl;

        el.src = blobUrl;
        el.load();

        await el.play();

        if (requestId !== requestIdRef.current) {
            return false;
        }

        setState("playing");
        return true;
    } catch {
        // Không để lỗi của yêu cầu cũ làm đổi trạng thái bài mới.
        if (requestId === requestIdRef.current) {
            setState("error");
        }

        return false;
    } finally {
        if (requestId === requestIdRef.current) {
            downloadRef.current = null;
        }
    }
}, [track, state]);

  const resume = useCallback(() => {
    audioRef.current?.play().catch(() => setState("error"));
  }, []);

  const pause = useCallback(() => {
    audioRef.current?.pause();
  }, []);

    const stop = useCallback(() => {
        // Làm mất hiệu lực kết quả tải đang chờ.
        requestIdRef.current++;

        downloadRef.current?.abort();
        downloadRef.current = null;

        const el = audioRef.current;

        if (el) {
            el.pause();
            el.removeAttribute("src");
            el.load();
        }

        if (blobUrlRef.current) {
            URL.revokeObjectURL(blobUrlRef.current);
            blobUrlRef.current = null;
        }

        setTrack(null);
        setState("idle");
        setCurrentTime(0);
        setDuration(0);
    }, []);

  const seek = useCallback((seconds: number) => {
    const el = audioRef.current;
    if (!el) return;
    el.currentTime = Math.max(0, Math.min(seconds, el.duration || 0));
  }, []);

 useEffect(() => {
    return () => {
        requestIdRef.current++;

        downloadRef.current?.abort();

        const el = audioRef.current;

        if (el) {
            el.pause();
            el.removeAttribute("src");
            el.load();
        }

        if (blobUrlRef.current) {
            URL.revokeObjectURL(blobUrlRef.current);
            blobUrlRef.current = null;
        }
    };
}, []);

  return (
    <Ctx.Provider value={{ track, state, currentTime, duration, play, resume, pause, stop, seek }}>
      {children}
    </Ctx.Provider>
  );
}

export function useAudio() {
  const ctx = useContext(Ctx);
  if (!ctx) throw new Error("useAudio must be used inside AudioProvider");
  return ctx;
}
