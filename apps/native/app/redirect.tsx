import { useLocalSearchParams, useRouter, useRootNavigationState } from "expo-router";
import { Spinner } from "heroui-native";
import { useEffect, useState } from "react";
import { Text, View } from "react-native";
import { useAuth } from "@/contexts/auth-context";

export default function RedirectPage() {
	const { code: rawCode } = useLocalSearchParams<{ code?: string | string[] }>();
	const code = Array.isArray(rawCode) ? rawCode[0] : rawCode;
	const { exchangeCodeForTokens } = useAuth();
	const router = useRouter();
	const navigationState = useRootNavigationState();
	const [error, setError] = useState<string | null>(null);

	useEffect(() => {
		// Wait until navigation state is fully loaded to avoid navigation errors
		if (!navigationState?.key) return;

		let isMounted = true;
		if (code) {
			exchangeCodeForTokens(code)
				.then(() => {
					if (isMounted) {
						router.replace("/");
					}
				})
				.catch((err: unknown) => {
					if (isMounted) {
						console.error("Failed to complete login redirect", err);
						setError(err instanceof Error ? err.message : "Failed to authenticate");
					}
				});
		} else {
			router.replace("/");
		}

		return () => {
			isMounted = false;
		};
	}, [code, exchangeCodeForTokens, router, navigationState?.key]);

	if (error) {
		return (
			<View className="flex-1 items-center justify-center bg-background p-6">
				<Text className="font-mono text-danger text-sm">{error}</Text>
			</View>
		);
	}

	return (
		<View className="flex-1 items-center justify-center bg-background p-6">
			<View className="items-center gap-4">
				<Spinner size="lg" color="default" />
				<Text className="animate-pulse font-mono text-muted text-sm tracking-widest">
					SINCRO_UMBRAL::AUTENTICANDO...
				</Text>
			</View>
		</View>
	);
}
