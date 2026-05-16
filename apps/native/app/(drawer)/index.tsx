import { Ionicons } from "@expo/vector-icons";
import {
	Button,
	Chip,
	Separator,
	Spinner,
	Surface,
	useThemeColor,
} from "heroui-native";
import { Text, View } from "react-native";

import { Container } from "@/components/container";

export default function Home() {
	const successColor = useThemeColor("success");
	const dangerColor = useThemeColor("danger");

	return (
		<Container className="px-4 pb-4">
			<View className="mb-5 py-6">
				<Text className="font-semibold text-3xl text-foreground tracking-tight">
					Better T Stack
				</Text>
				<Text className="mt-1 text-muted text-sm">
					Full-stack TypeScript starter
				</Text>
			</View>

			<Surface variant="secondary" className="rounded-xl p-4">
				<View className="mb-3 flex-row items-center justify-between">
					<Text className="font-medium text-foreground">System Status</Text>
					<Chip
						variant="secondary"
						color={isConnected ? "success" : "danger"}
						size="sm"
					>
						<Chip.Label>{isConnected ? "LIVE" : "OFFLINE"}</Chip.Label>
					</Chip>
				</View>

				<Separator className="mb-3" />

				<Surface variant="tertiary" className="rounded-lg p-3">
					<View className="flex-row items-center">
						<View
							className={`mr-3 h-2 w-2 rounded-full ${isConnected ? "bg-success" : "bg-muted"}`}
						/>
						<View className="flex-1">
							<Text className="font-medium text-foreground text-sm" />
							<Text className="mt-0.5 text-muted text-xs">
								{isLoading
									? "Checking connection..."
									: isConnected
										? "Connected to API"
										: "API Disconnected"}
							</Text>
						</View>
						{isLoading && <Spinner size="sm" />}
						{!isLoading && isConnected && (
							<Ionicons
								name="checkmark-circle"
								size={18}
								color={successColor}
							/>
						)}
						{!isLoading && !isConnected && (
							<Ionicons name="close-circle" size={18} color={dangerColor} />
						)}
					</View>
				</Surface>
			</Surface>
		</Container>
	);
}
