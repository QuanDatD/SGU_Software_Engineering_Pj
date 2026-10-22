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
  play: (track: TrackInfo) => Promise<boolean>;
  /** Tiếp tục bài đang tạm dừng */
  resume: () => void;
  pause: () => void;
  stop: () => void;
  seek: (seconds: number) => void;
}

const Ctx = createContext<AudioCtx | null>(null);

export function AudioProvider({ children }: { children: ReactNode }) {
  const audioRef  = useRef<HTMLAudioElement | null>(null);
  const [track,   setTrack]   = useState<TrackInfo | null>(null);
  const [state,   setState]   = useState<PlayerState>("idle");
  const [currentTime, setCurrentTime] = useState(0);
  const [duration,    setDuration]    = useState(0);

  /* Đảm bảo chỉ có một HTMLAudioElement */
  function getAudio(): HTMLAudioElement {
    if (!audioRef.current) {
      const el = new Audio();
      el.preload = "auto";

      el.addEventListener("loadstart",  () => setState("loading"));
      el.addEventListener("play",       () => setState("playing"));
      el.addEventListener("pause",      () => setState((s) => s !== "ended" ? "paused" : s));
      el.addEventListener("ended",      () => { setState("ended"); });
      el.addEventListener("error",      () => setState("error"));
      el.addEventListener("timeupdate", () => setCurrentTime(el.currentTime));
      el.addEventListener("durationchange", () => setDuration(el.duration || 0));

      audioRef.current = el;
    }
    return audioRef.current;
  }

    const play = useCallback(async (next: TrackInfo): Promise<boolean> => {
        const el = getAudio();

        // Không phát lại bài đang phát
        if (track?.url === mediaUrl(next.url) && (state === "playing" || state === "loading")) return true;

        el.pause();

        // Xử lý nối domain Backend nếu là đường dẫn tương đối
        const fullAudioUrl = mediaUrl(next.url);

        const currentTrack = { ...next, url: fullAudioUrl };

        setTrack(currentTrack);
        setCurrentTime(0);
        setDuration(0);
        setState("loading");

        el.src = fullAudioUrl;
        el.currentTime = 0;
        el.load();
        try { await el.play(); return true; } catch { if (el.src === new URL(fullAudioUrl, location.href).href) setState("error"); return false; }
    }, [track, state]);

  const resume = useCallback(() => {
    audioRef.current?.play().catch(() => setState("error"));
  }, []);

  const pause = useCallback(() => {
    audioRef.current?.pause();
  }, []);

  const stop = useCallback(() => {
    const el = audioRef.current;
    if (!el) return;
    el.pause();
    el.removeAttribute("src");
    el.load();
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

  useEffect(() => () => { audioRef.current?.pause(); }, []);

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
