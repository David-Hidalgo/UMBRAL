import { createElement } from "react";
import { View } from "react-native";

import { buildTreasureMapHtml } from "./treasure-map-html";

interface TreasureMapProps {
	latitude: number;
	longitude: number;
	radius: number;
}

// En web (react-native-web) `react-native-webview` no funciona, así que el mapa
// se muestra en un <iframe>. Se usa createElement porque `iframe` no es un
// componente de React Native.
const Iframe = "iframe" as unknown as React.ComponentType<
	Record<string, unknown>
>;

export function TreasureMap({ latitude, longitude, radius }: TreasureMapProps) {
	return (
		<View className="h-56 overflow-hidden rounded-lg border border-border/20">
			<Iframe
				srcDoc={buildTreasureMapHtml(latitude, longitude, radius)}
				style={{ width: "100%", height: "100%", border: "none" }}
				title="Mapa del tesoro"
			/>
		</View>
	);
}
