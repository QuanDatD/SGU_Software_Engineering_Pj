import { useAudio } from "../contexts/AudioContext";

function fmt(s: number): string {
  if (!isFinite(s) || isNaN(s)) return "0:00";
  const m = Math.floor(s / 60);
  const sec = Math.floor(s % 60);
  return `${m}:${sec.toString().padStart(2, "0")}`;
}

interface Props {
  lang?: string;
}

export default function AudioPlayer({ lang = "vi" }: Props) {
  const { track, state, currentTime, duration, resume, pause, stop, seek } = useAudio();

  if (!track) return null;

  const isPlaying = state === "playing" || state === "loading";
  const progress  = duration > 0 ? (currentTime / duration) * 100 : 0;

  function handleSeek(e: React.ChangeEvent<HTMLInputElement>) {
    seek(parseFloat(e.target.value));
  }

  return (
    <div className="audio-player" role="region" aria-label={lang === "vi" ? "Trình phát âm thanh" : "Audio Player"}>
      {/* Tên bài */}
      <div className="audio-player__track">
        <span className="audio-player__icon">🎧</span>
        <span className="audio-player__title">{track.title}</span>
        {state === "loading" && (
          <span className="audio-player__loading">⏳</span>
        )}
        {state === "error" && (
          <span className="audio-player__error">⚠️</span>
        )}
      </div>

      {/* Progress bar */}
      <div className="audio-player__bar">
        <span className="audio-player__time">{fmt(currentTime)}</span>
        <input
          type="range"
          className="audio-player__range"
          min={0}
          max={duration || 0}
          step={0.5}
          value={currentTime}
          onChange={handleSeek}
          aria-label="Seek"
        />
        <span className="audio-player__time">{fmt(duration)}</span>
      </div>

      {/* Controls */}
      <div className="audio-player__controls">
        {/* Nghe lại từ đầu (UC03.5) */}
        <button
          className="audio-player__ctrl"
          type="button"
          aria-label="Nghe lại từ đầu"
          onClick={() => { seek(0); if (!isPlaying) resume(); }}
          title="Nghe lại từ đầu"
        >
          ↩
        </button>

        {/* Tua lùi 10s */}
        <button
          className="audio-player__ctrl"
          type="button"
          aria-label="Tua lùi 10 giây"
          onClick={() => seek(currentTime - 10)}
        >
          ⏮ 10s
        </button>

        {/* Play / Pause */}
        <button
          className="audio-player__ctrl audio-player__ctrl--main"
          type="button"
          aria-label={isPlaying ? "Tạm dừng" : "Phát"}
          disabled={state === "loading"}
          onClick={() => isPlaying ? pause() : resume()}
        >
          {isPlaying ? "⏸" : "▶"}
        </button>

        {/* Tua tới 10s */}
        <button
          className="audio-player__ctrl"
          type="button"
          aria-label="Tua tới 10 giây"
          onClick={() => seek(currentTime + 10)}
        >
          10s ⏭
        </button>

        {/* Dừng */}
        <button
          className="audio-player__ctrl audio-player__ctrl--stop"
          type="button"
          aria-label="Dừng"
          onClick={stop}
        >
          ⏹
        </button>
      </div>

      {/* Progress bar trực quan */}
      <div className="audio-player__progress">
        <div
          className="audio-player__fill"
          style={{ width: `${progress}%` }}
        />
      </div>
    </div>
  );
}
