"use client";

import "leaflet/dist/leaflet.css";
import L from "leaflet";
import { useEffect } from "react";
import {
	Circle,
	MapContainer,
	Marker,
	TileLayer,
	useMap,
	useMapEvents,
} from "react-leaflet";

interface TreasureMapPickerProps {
	latitude: number | null;
	longitude: number | null;
	radius: number;
	onChange: (lat: number, lng: number) => void;
	onRadiusChange: (radius: number) => void;
	disabled?: boolean;
}

// Caracas como centro por defecto cuando aún no se marcó un punto.
const DEFAULT_CENTER: [number, number] = [10.4806, -66.9036];

// Pin dibujado con un divIcon (SVG) para no depender de las imágenes de Leaflet,
// que se rompen con los bundlers.
const pinIcon = L.divIcon({
	className: "",
	html: `<svg width="30" height="42" viewBox="0 0 24 36" xmlns="http://www.w3.org/2000/svg">
		<path d="M12 0C5.4 0 0 5.4 0 12c0 9 12 24 12 24s12-15 12-24C24 5.4 18.6 0 12 0z" fill="#3b82f6"/>
		<circle cx="12" cy="12" r="5" fill="#fff"/>
	</svg>`,
	iconSize: [30, 42],
	iconAnchor: [15, 42],
});

function ClickHandler({
	onChange,
	disabled,
}: {
	onChange: (lat: number, lng: number) => void;
	disabled?: boolean;
}) {
	useMapEvents({
		click(e) {
			if (!disabled) onChange(e.latlng.lat, e.latlng.lng);
		},
	});
	return null;
}

// Recentra el mapa cuando el punto cambia desde fuera (p. ej. al cargar una misión).
function Recenter({ lat, lng }: { lat: number | null; lng: number | null }) {
	const map = useMap();
	useEffect(() => {
		if (lat !== null && lng !== null) {
			map.setView([lat, lng], map.getZoom());
		}
	}, [lat, lng, map]);
	return null;
}

export function TreasureMapPicker({
	latitude,
	longitude,
	radius,
	onChange,
	onRadiusChange,
	disabled,
}: TreasureMapPickerProps) {
	const hasPoint = latitude !== null && longitude !== null;
	const center: [number, number] = hasPoint
		? [latitude, longitude]
		: DEFAULT_CENTER;

	return (
		<div className="space-y-2">
			<div className="h-72 overflow-hidden rounded-md border border-border/40">
				<MapContainer
					center={center}
					scrollWheelZoom
					style={{ height: "100%", width: "100%" }}
					zoom={15}
				>
					<TileLayer
						attribution='&copy; <a href="https://www.openstreetmap.org/copyright">OpenStreetMap</a>'
						url="https://tile.openstreetmap.org/{z}/{x}/{y}.png"
					/>
					<ClickHandler disabled={disabled} onChange={onChange} />
					<Recenter lat={latitude} lng={longitude} />
					{hasPoint && (
						<>
							<Marker icon={pinIcon} position={[latitude, longitude]} />
							<Circle
								center={[latitude, longitude]}
								pathOptions={{ color: "#f59e0b", fillColor: "#f59e0b" }}
								radius={radius}
							/>
						</>
					)}
				</MapContainer>
			</div>

			<div className="flex items-center justify-between gap-3 text-muted-foreground text-xs">
				<span className="font-mono">
					{hasPoint ? (
						<>
							Lat: <b className="text-foreground">{latitude.toFixed(6)}</b> ·
							Lon: <b className="text-foreground">{longitude.toFixed(6)}</b>
						</>
					) : (
						"Haz clic en el mapa para marcar la ubicación del tesoro."
					)}
				</span>
			</div>

			<div className="space-y-1">
				<div className="flex items-center justify-between text-muted-foreground text-xs">
					<span>Radio de búsqueda</span>
					<b className="text-foreground">{radius} m</b>
				</div>
				<input
					className="w-full accent-amber-500"
					disabled={disabled}
					max={300}
					min={10}
					onChange={(e) => onRadiusChange(Number(e.target.value))}
					step={5}
					type="range"
					value={radius}
				/>
			</div>
		</div>
	);
}
