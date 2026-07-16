import { View } from "react-native";
import { WebView } from "react-native-webview";

import { buildTreasureMapHtml } from "./treasure-map-html";

interface TreasureMapProps {
	latitude: number;
	longitude: number;
	radius: number;
}

// Nativo (iOS/Android): mapa de solo lectura con el pin del tesoro y el radio,
// dentro de un WebView. En web se usa treasure-map.web.tsx (iframe).
export function TreasureMap({ latitude, longitude, radius }: TreasureMapProps) {
	return (
		<View className="h-56 overflow-hidden rounded-lg border border-border/20">
			<WebView
				// Mapa de solo lectura: solo se permite renderizar el documento embebido.
				// Cualquier navegación de nivel superior (enlaces/redirecciones a http/https)
				// se bloquea. Los recursos de Leaflet/OSM son sub-recursos y no pasan por aquí.
				originWhitelist={["about:*", "data:*"]}
				onShouldStartLoadWithRequest={(request) =>
					request.url === "about:blank" ||
					request.url.startsWith("about:") ||
					request.url.startsWith("data:")
				}
				setSupportMultipleWindows={false}
				javaScriptCanOpenWindowsAutomatically={false}
				source={{ html: buildTreasureMapHtml(latitude, longitude, radius) }}
				style={{ backgroundColor: "transparent" }}
			/>
		</View>
	);
}
