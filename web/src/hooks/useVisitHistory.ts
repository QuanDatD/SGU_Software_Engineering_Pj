import { useCallback, useRef, useState } from "react";

export function useVisitHistory() {
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