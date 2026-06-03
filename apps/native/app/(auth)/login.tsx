import { useAuth } from "@/contexts/auth-context";
import { Button, Spinner, Surface } from "heroui-native";
import { useState } from "react";
import { Text, View } from "react-native";

export default function LoginScreen() {
	const { login } = useAuth();
	const [isLoading, setIsLoading] = useState(false);

	async function handleLogin() {
		setIsLoading(true);
		try {
			await login();
		} finally {
			setIsLoading(false);
		}
	}

	return (
		<View className="flex-1 items-center justify-center bg-background px-6">
			<View className="mb-10 items-center gap-2">
				<Text className="font-bold text-4xl text-foreground tracking-tight">
					UMBRAL
				</Text>
				<Text className="text-muted text-sm">
					Plataforma de experiencias narrativas
				</Text>
			</View>

			<Surface variant="secondary" className="w-full max-w-sm rounded-2xl p-6">
				<Text className="mb-1 font-semibold text-foreground text-lg">
					Iniciar sesión
				</Text>
				<Text className="mb-6 text-muted text-sm">
					Usá tu cuenta institucional para acceder.
				</Text>

				<Button
					onPress={() => void handleLogin()}
					isDisabled={isLoading}
					className="w-full"
				>
					{isLoading ? (
						<Spinner size="sm" />
					) : (
						<Button.Label>Continuar con Keycloak</Button.Label>
					)}
				</Button>
			</Surface>
		</View>
	);
}
