import "@/global.css";
import { AppThemeProvider } from "@/contexts/app-theme-context";
import { AuthProvider, useAuth } from "@/contexts/auth-context";
import { Stack } from "expo-router";
import { HeroUINativeProvider, Spinner } from "heroui-native";
import { View } from "react-native";
import { GestureHandlerRootView } from "react-native-gesture-handler";
import { KeyboardProvider } from "react-native-keyboard-controller";

export const unstable_settings = {
	initialRouteName: "(auth)",
};

function StackLayout() {
	const { isLoading } = useAuth();

	if (isLoading) {
		return (
			<View className="flex-1 items-center justify-center bg-background">
				<Spinner size="lg" />
			</View>
		);
	}

	return (
		<Stack screenOptions={{ headerShown: false }}>
			<Stack.Screen name="(auth)" />
			<Stack.Screen name="(drawer)" />
			<Stack.Screen
				name="modal"
				options={{ title: "Modal", presentation: "modal" }}
			/>
		</Stack>
	);
}

export default function Layout() {
	return (
		<GestureHandlerRootView style={{ flex: 1 }}>
			<KeyboardProvider>
				<AppThemeProvider>
					<HeroUINativeProvider>
						<AuthProvider>
							<StackLayout />
						</AuthProvider>
					</HeroUINativeProvider>
				</AppThemeProvider>
			</KeyboardProvider>
		</GestureHandlerRootView>
	);
}
