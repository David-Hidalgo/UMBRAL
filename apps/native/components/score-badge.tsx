import { Ionicons } from "@expo/vector-icons";
import { Text, View } from "react-native";

interface ScoreBadgeProps {
	points: number | null;
}

export function ScoreBadge({ points }: ScoreBadgeProps) {
	return (
		<View className="flex-row items-center justify-between rounded-lg border border-accent/30 bg-accent/10 px-4 py-3">
			<View className="flex-row items-center gap-2">
				<Ionicons name="trophy-outline" size={16} color="#fbbf24" />
				<Text className="font-bold font-mono text-foreground text-xs uppercase tracking-widest">
					Puntaje del escuadrón
				</Text>
			</View>
			<Text className="font-bold font-mono text-accent text-lg tabular-nums">
				{points === null ? "—" : points}
			</Text>
		</View>
	);
}
