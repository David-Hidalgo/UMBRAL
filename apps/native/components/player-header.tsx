import { Ionicons } from "@expo/vector-icons";
import { Chip } from "heroui-native";
import { Pressable, Text, View } from "react-native";

interface PlayerHeaderProps {
	name: string;
	roleLabel: string;
	subtitle?: string;
	onSignOut: () => void;
}

export function PlayerHeader({
	name,
	roleLabel,
	subtitle,
	onSignOut,
}: PlayerHeaderProps) {
	return (
		<View className="mb-5 flex-row items-center justify-between rounded-xl border border-border/10 bg-card/40 p-4">
			<View className="flex-1 flex-row items-center gap-3">
				<View className="size-10 items-center justify-center rounded-full border border-accent/30 bg-accent/10">
					<Ionicons name="person" size={18} color="#a1a1aa" />
				</View>
				<View className="flex-1">
					<Text
						className="font-bold font-mono text-foreground text-sm"
						numberOfLines={1}
					>
						{name}
					</Text>
					{subtitle ? (
						<Text
							className="font-mono text-[10px] text-muted"
							numberOfLines={1}
						>
							{subtitle}
						</Text>
					) : null}
				</View>
			</View>

			<View className="flex-row items-center gap-2">
				<Chip color="accent" variant="soft">
					<Text className="font-mono font-semibold text-[10px]">
						{roleLabel}
					</Text>
				</Chip>
				<Pressable
					accessibilityLabel="Cerrar sesión"
					accessibilityRole="button"
					className="size-9 items-center justify-center rounded-lg border border-danger/30 bg-danger/10 active:opacity-70"
					onPress={onSignOut}
				>
					<Ionicons name="log-out-outline" size={16} color="#ef4444" />
				</Pressable>
			</View>
		</View>
	);
}
