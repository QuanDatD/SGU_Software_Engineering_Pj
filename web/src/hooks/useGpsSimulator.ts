import { useCallback, useEffect, useRef, useState } from "react";
import type { GpsPosition, GpsState } from "../types/visit";

interface Coordinate {
    lat: number;
    lng: number;
}

interface SimulationRoute {
    from: Coordinate;
    to: Coordinate;
    speedMetersPerSecond: number;
    distanceMeters: number;
}

function distanceMeters(from: Coordinate, to: Coordinate): number {
    const earthRadius = 6_371_000;
    const toRadians = (degrees: number) => degrees * Math.PI / 180;

    const lat1 = toRadians(from.lat);
    const lat2 = toRadians(to.lat);

    const deltaLat = toRadians(to.lat - from.lat);
    const deltaLng = toRadians(to.lng - from.lng);

    const value =
        Math.sin(deltaLat / 2) ** 2 +
        Math.cos(lat1) *
        Math.cos(lat2) *
        Math.sin(deltaLng / 2) ** 2;

    const a = Math.max(0, Math.min(1, value));

    return earthRadius * 2 * Math.atan2(
        Math.sqrt(a),
        Math.sqrt(1 - a),
    );
}

function isValidCoordinate(point: Coordinate): boolean {
    return (
        Number.isFinite(point.lat) &&
        Number.isFinite(point.lng) &&
        point.lat >= -90 &&
        point.lat <= 90 &&
        point.lng >= -180 &&
        point.lng <= 180
    );
}

export function useGpsSimulator() {
    const [enabled, setEnabled] = useState(false);
    const [moving, setMoving] = useState(false);
    const [progress, setProgress] = useState(0);
    const [position, setPosition] = useState<GpsPosition | null>(null);

    const routeRef = useRef<SimulationRoute | null>(null);
    const travelledRef = useRef(0);
    const lastTickRef = useRef(0);
    const movingRef = useRef(false);

    const start = useCallback((
        from: Coordinate,
        to: Coordinate,
        speedMetersPerSecond: number,
    ) => {
        if (!isValidCoordinate(from) || !isValidCoordinate(to)) {
            throw new Error("Tọa độ giả lập không hợp lệ.");
        }

        if (
            !Number.isFinite(speedMetersPerSecond) ||
            speedMetersPerSecond <= 0 ||
            speedMetersPerSecond > 10
        ) {
            throw new Error("Tốc độ giả lập phải lớn hơn 0 và không quá 10 m/s.");
        }

        const distance = distanceMeters(from, to);

        if (distance < 1) {
            throw new Error("Điểm xuất phát và đích phải cách nhau ít nhất 1 mét.");
        }

        routeRef.current = {
            from: { ...from },
            to: { ...to },
            speedMetersPerSecond,
            distanceMeters: distance,
        };

        travelledRef.current = 0;
        lastTickRef.current = performance.now();
        movingRef.current = true;

        setPosition({
            lat: from.lat,
            lng: from.lng,
            accuracy: 5,
            timestamp: Date.now(),
        });

        setProgress(0);
        setMoving(true);
        setEnabled(true);
    }, []);

    const pause = useCallback(() => {
        movingRef.current = false;
        setMoving(false);
    }, []);

    const resume = useCallback(() => {
        const route = routeRef.current;

        if (!route || travelledRef.current >= route.distanceMeters) {
            return;
        }

        lastTickRef.current = performance.now();
        movingRef.current = true;
        setMoving(true);
    }, []);

    const stop = useCallback(() => {
        movingRef.current = false;
        routeRef.current = null;
        travelledRef.current = 0;

        setEnabled(false);
        setMoving(false);
        setPosition(null);
        setProgress(0);
    }, []);

    useEffect(() => {
        if (!enabled) return;

        const timer = window.setInterval(() => {
            const route = routeRef.current;

            if (!route) return;

            const now = performance.now();

            // Giới hạn bước thời gian để tránh nhảy quá xa
            // khi tab bị trình duyệt tạm ngưng.
            const elapsedSeconds = Math.min(
                Math.max((now - lastTickRef.current) / 1000, 0),
                2,
            );

            lastTickRef.current = now;

            if (movingRef.current) {
                travelledRef.current = Math.min(
                    route.distanceMeters,
                    travelledRef.current +
                        route.speedMetersPerSecond * elapsedSeconds,
                );
            }

            const ratio =
                travelledRef.current / route.distanceMeters;

            setPosition({
                lat:
                    route.from.lat +
                    (route.to.lat - route.from.lat) * ratio,
                lng:
                    route.from.lng +
                    (route.to.lng - route.from.lng) * ratio,
                accuracy: 5,
                timestamp: Date.now(),
            });

            setProgress(ratio);

            if (ratio >= 1 && movingRef.current) {
                movingRef.current = false;
                setMoving(false);
            }
        }, 1000);

        return () => window.clearInterval(timer);
    }, [enabled]);

    const gps: GpsState = enabled && position
        ? { status: "active", position }
        : { status: "idle" };

    return {
        enabled,
        moving,
        progress,
        gps,
        start,
        pause,
        resume,
        stop,
    };
}