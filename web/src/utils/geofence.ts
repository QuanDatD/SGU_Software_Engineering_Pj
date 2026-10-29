import type { PlaceDetail } from "../types/place";
import type { GpsPosition } from "../types/visit";

function distanceMeters(
    position: GpsPosition,
    place: PlaceDetail,
): number {
    const radians = (degrees: number) =>
        degrees * Math.PI / 180;

    const deltaLat = radians(place.lat - position.lat);
    const deltaLng = radians(place.lng - position.lng);

    const value =
        Math.sin(deltaLat / 2) ** 2 +
        Math.cos(radians(position.lat)) *
        Math.cos(radians(place.lat)) *
        Math.sin(deltaLng / 2) ** 2;

    const a = Math.max(0, Math.min(1, value));

    return 6_371_000 * 2 * Math.atan2(
        Math.sqrt(a),
        Math.sqrt(1 - a),
    );
}

// Dùng để kiểm tra lại vị trí ngay trước khi phát audio.
// API vẫn là nơi đánh giá vùng chính trong luồng hiện tại.
export function findCurrentZone(
    position: GpsPosition | null,
    places: PlaceDetail[],
): PlaceDetail | null {
    if (
        !position ||
        !Number.isFinite(position.lat) ||
        !Number.isFinite(position.lng) ||
        !Number.isFinite(position.accuracy) ||
        !Number.isFinite(position.timestamp) ||
        position.lat < -90 ||
        position.lat > 90 ||
        position.lng < -180 ||
        position.lng > 180 ||
        position.accuracy < 0 ||
        position.accuracy > 25 ||
        Date.now() - position.timestamp > 15000
    ) {
        return null;
    }

    const candidates = places
        .filter((place) =>
            place.isActive &&
            Number.isFinite(place.lat) &&
            Number.isFinite(place.lng) &&
            Number.isFinite(place.radiusMeters) &&
            place.radiusMeters > 0
        )
        .map((place) => ({
            place,
            distance: distanceMeters(position, place),
        }))
        .filter((item) =>
            item.distance <= item.place.radiusMeters
        );

    candidates.sort((a, b) => {
        const priorityDifference =
            b.place.priority - a.place.priority;

        if (priorityDifference !== 0) {
            return priorityDifference;
        }

        const distanceDifference = a.distance - b.distance;

        if (distanceDifference !== 0) {
            return distanceDifference;
        }

        const firstId = a.place.id.toLowerCase();
        const secondId = b.place.id.toLowerCase();

        return firstId < secondId ? -1 : firstId > secondId ? 1 : 0;
    });

    return candidates[0]?.place ?? null;
}