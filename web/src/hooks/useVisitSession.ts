import {
    useEffect,
    useLayoutEffect,
    useRef,
    useCallback,
} from "react";
import type { GpsPosition } from "../types/visit";
import type { PlaceDetail } from "../types/place";
import { visitApi } from "../services/visitApi";
import { useAudio } from "../contexts/AudioContext";
import type { Lang } from "../types/common";
import type { useVisitHistory } from "./useVisitHistory";
import { findCurrentZone } from "../utils/geofence";
interface Options {
  position: GpsPosition | null; places: PlaceDetail[]; lang: Lang; enabled: boolean;
  onEnter?: (id: string) => void;
  history: ReturnType<typeof useVisitHistory>;
}
export function useVisitSession(options: Options) {
  const audio = useAudio();

	const latest = useRef({ ...options, audio });

	// Mỗi lần vị trí, ngôn ngữ hoặc chế độ tự phát thay đổi,
	// các kết quả kiểm tra trước đó sẽ mất hiệu lực.
	const generation = useRef(0);

	useLayoutEffect(() => {
		const previous = latest.current;

		const positionAvailabilityChanged =
			(previous.position === null) !==
			(options.position === null);

		if (
			positionAvailabilityChanged ||
			previous.lang !== options.lang ||
			previous.enabled !== options.enabled
		) {
			generation.current++;
		}
		latest.current = { ...options, audio };
	});
  
  const { heard, blocked } = options.history;

  const stable = useRef<{ id: string; count: number } | null>(null);
  useEffect(() => {
    let disposed = false;
    let timer: ReturnType<typeof setTimeout>;
    async function poll() {
      const { position, lang } = latest.current;
	  const requestGeneration = generation.current;
      try {
        if (
			!position ||
			position.accuracy > 25 ||
			Date.now() - position.timestamp > 15000
		) {
			stable.current = null;
			return;
		}
        const result = await visitApi.evaluate(position.lat, position.lng, position.accuracy);
        if (
			disposed ||
			requestGeneration !== generation.current ||
			latest.current.position !== position ||
			latest.current.lang !== lang ||
			Date.now() - position.timestamp > 15000
		) {
			return;
		}
        if (!result.accepted || !result.placeId) { stable.current = null; return; }
        const id = result.placeId;
        stable.current = stable.current?.id === id ? { id, count: stable.current.count + 1 } : { id, count: 1 };
		// Giữ quy tắc hiện có: xác định cùng vùng qua hai lần kiểm tra.
		if (stable.current.count >= 2) {
			latest.current.history.markVisited(id);
		}

		// Tắt tự phát vẫn ghi nhận điểm đã tới,
		// nhưng không chạy phần phát audio phía dưới.
		if (!latest.current.enabled) return;

        const place = latest.current.places.find(p => p.id === id);
        const url = place?.audioUrl[lang];
        const key = `${id}:${lang}:${url}`;
        if (
			stable.current.count >= 2 &&
			url &&
			!heard.current.has(key) &&
			!blocked.current.has(key)
		) {
			const currentAudio = latest.current.audio;

			// Đang phát, tải hoặc tạm dừng một bài thì không
			// tự thay bài đó bằng bài khác.
			if (
				currentAudio.state === "playing" ||
				currentAudio.state === "loading" ||
				currentAudio.state === "paused"
			) {
				return;
			}

			latest.current.onEnter?.(id);

			const canStart = () => {
			const current = latest.current;

			const currentZone = findCurrentZone(
				current.position,
				current.places,
			);

			return (
				!disposed &&
				requestGeneration === generation.current &&
				current.enabled &&
				current.lang === lang &&
				currentZone?.id === id &&
				currentZone.audioUrl[lang] === url
			);
		};

		const played = await currentAudio.play(
			{
				placeId: id,
				lang,
				url,
				title: place?.name[lang] || place?.name.vi || "",
			},
			canStart,
		);

		// Yêu cầu bị hủy do điều kiện thay đổi không được
		// ghi thành lỗi trình duyệt chặn tự phát.
		if (!canStart()) {
			return;
		}

			// Nếu trạng thái đã thay đổi trong lúc tải/phát,
			// không ghi nhận lỗi tự phát cho yêu cầu cũ.
			if (
				disposed ||
				requestGeneration !== generation.current
			) {
				return;
			}

			if (played) {
				latest.current.history.markHeard(id, lang, url);
			} else {
				blocked.current.add(key);
			}
		}
        }
       catch { stable.current = null; }
      finally { if (!disposed) timer = setTimeout(poll, 5000); }
    }
    void poll();
    return () => { disposed = true; clearTimeout(timer); };
  }, []);
	const resetSession = useCallback(() => {
		generation.current++;
		options.history.reset();
		stable.current = null;
	}, [options.history.reset]);

	return { resetSession };
}