import { Ionicons } from "@expo/vector-icons";
import { Text, View } from "react-native";

import type { ReleasedHint } from "@/lib/api/hints";

interface HintListProps {
	hints: ReleasedHint[];
}

export function HintList({ hints }: HintListProps) {
	if (hints.length === 0) return null;

	const ordered = [...hints].sort((a, b) => a.order - b.order);

	return (
		<View className="gap-2 rounded-lg border border-accent/30 bg-accent/10 p-3">
			<View className="flex-row items-center gap-2">
				<Ionicons color="#fbbf24" name="bulb-outline" size={16} />
				<Text className="font-bold font-mono text-foreground text-xs uppercase tracking-widest">
					Pistas recibidas
				</Text>
			</View>
			{ordered.map((hint) => (
				<View className="flex-row gap-2" key={hint.id}>
					<Text className="font-mono text-[10px] text-muted uppercase">
						{hint.order}.
					</Text>
					<Text className="flex-1 font-mono text-foreground text-xs">
						{hint.text}
					</Text>
				</View>
			))}
		</View>
	);
}
