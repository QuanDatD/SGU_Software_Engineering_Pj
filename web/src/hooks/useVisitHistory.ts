import { useCallback, useEffect, useRef, useState } from "react";
import { useAudio } from "../contexts/AudioContext";

export function useVisitHistory() {
    const audio = useAudio();

    // Khóa bài: điểm + ngôn ngữ + URL audio.
    const heard = useRef(new Set<string>());

    // Những bài từng bị trình duyệt chặn tự phát.
    const blocked = useRef(new Set<string>());

    // Danh sách điểm có ít nhất một bài đã bắt đầu phát.
    const heardPlaceSet = useRef(new Set<string>());
    const [heardPlaceIds, setHeardPlaceIds] = useState<string[]>([]);
	const visitedPlaceSet = useRef(new Set<string>());
	const [visitedPlaceIds, setVisitedPlaceIds] = useState<string[]>([]);

	const markVisited = useCallback((placeId: string) => {
    if (visitedPlaceSet.current.has(placeId)) return;

    visitedPlaceSet.current.add(placeId);
    setVisitedPlaceIds([...visitedPlaceSet.current]);
}, []);

    const markHeard = useCallback(
        (placeId: string, lang: string, url: string) => {
            heard.current.add(`${placeId}:${lang}:${url}`);

            if (!heardPlaceSet.current.has(placeId)) {
                heardPlaceSet.current.add(placeId);
                setHeardPlaceIds([...heardPlaceSet.current]);
            }
        },
        [],
    );

    // Ghi nhận cả khi khách nhấn nghe thủ công.
    useEffect(() => {
        if (audio.state !== "playing" || !audio.track) return;

        markHeard(
            audio.track.placeId,
            audio.track.lang,
            audio.track.url,
        );
    }, [audio.state, audio.track, markHeard]);

    const reset = useCallback(() => {
        heard.current.clear();
        blocked.current.clear();
        heardPlaceSet.current.clear();
        setHeardPlaceIds([]);
		visitedPlaceSet.current.clear();
		setVisitedPlaceIds([]);
    }, []);

    return {
		heard,
		blocked,
		heardPlaceIds,
		markHeard,
		visitedPlaceIds,
		markVisited,
		reset,
	};
}