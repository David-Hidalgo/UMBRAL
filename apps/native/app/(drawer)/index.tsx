import { Ionicons } from "@expo/vector-icons";
import { Card, Chip, Spinner, useThemeColor } from "heroui-native";
import { Pressable, Text, View } from "react-native";

import { Container } from "@/components/container";
import { useAuth } from "@/contexts/auth-context";

export default function Home() {
	const { status, session, beginLogin, signOut, hasRealmRole } = useAuth();

	const mutedColor = useThemeColor("muted");
	const dangerColor = useThemeColor("danger");
	const foregroundColor = useThemeColor("foreground");
	const accentColor = useThemeColor("accent");

	if (status === "loading") {
		return (
			<Container className="items-center justify-center p-6">
				<View className="items-center gap-4">
					<Spinner size="lg" color="default" />
					<Text className="animate-pulse font-mono text-muted text-sm tracking-widest">
						SINCRO_UMBRAL::INICIALIZANDO...
					</Text>
				</View>
			</Container>
		);
	}

	// Determine role classification
	let roleTitle = "Operador";
	let roleColor = "default";
	if (hasRealmRole("admin")) {
		roleTitle = "Administrador del Sistema";
		roleColor = "danger";
	} else if (hasRealmRole("operator")) {
		roleTitle = "Operador Táctico";
		roleColor = "accent";
	} else if (hasRealmRole("player")) {
		roleTitle = "Agente de Campo";
		roleColor = "success";
	}

	return (
		<Container className="p-6">
			<View className="mb-8 border-border/20 border-b py-6">
				<Text className="font-black font-mono text-5xl text-foreground tracking-tight">
					UMBRAL
				</Text>
				<Text className="mt-1 font-mono text-muted text-xs tracking-widest">
					SISTEMA DE SUPERVISIÓN & OPERACIONES
				</Text>
			</View>

			{session?.user ? (
				<Card
					variant="secondary"
					className="mb-6 border border-border/10 bg-card/50 p-5"
				>
					<View className="mb-4 flex-row items-center justify-between">
						<View>
							<Text className="font-mono text-muted text-xs uppercase tracking-wider">
								ESTADO DE AGENTE
							</Text>
							<Text className="mt-1 font-bold font-mono text-2xl text-foreground">
								{session.user.name || "Agente Anónimo"}
							</Text>
						</View>
						<Chip
							color={roleColor as "default" | "accent" | "success" | "danger"}
							variant="soft"
						>
							<Text className="font-mono font-semibold text-xs">
								{roleTitle}
							</Text>
						</Chip>
					</View>

					<View className="mb-6 space-y-2 border-border/15 border-t pt-4">
						<View className="mb-2 flex-row justify-between">
							<Text className="font-mono text-muted text-sm">CANAL:</Text>
							<Text className="font-mono text-foreground text-sm">
								{session.user.email || "N/A"}
							</Text>
						</View>
						<View className="mb-2 flex-row justify-between">
							<Text className="font-mono text-muted text-sm">
								IDENTIFICACIÓN:
							</Text>
							<Text className="font-mono text-foreground text-sm">
								{session.user.preferred_username || "N/A"}
							</Text>
						</View>
						<View className="mb-2 flex-row justify-between">
							<Text className="font-mono text-muted text-sm">CONEXIÓN:</Text>
							<Text className="font-bold font-mono text-sm text-success">
								ACTIVA
							</Text>
						</View>
					</View>

					<Pressable
						className="flex-row items-center justify-center gap-2 rounded-lg border border-danger/40 bg-danger/20 px-4 py-3.5 active:opacity-75"
						onPress={() => {
							void signOut();
						}}
					>
						<Ionicons name="log-out-outline" size={18} color={dangerColor} />
						<Text className="font-bold font-mono text-danger text-sm uppercase tracking-wider">
							Desconectar Terminal
						</Text>
					</Pressable>
				</Card>
			) : (
				<View className="flex-1 justify-center gap-8 py-10">
					<Card
						variant="secondary"
						className="border border-border/10 bg-card/30 p-6"
					>
						<View className="mb-6 items-center">
							<View className="mb-3 rounded-full border border-accent/20 bg-accent/10 p-4">
								<Ionicons
									name="shield-checkmark-outline"
									size={32}
									color={accentColor}
								/>
							</View>
							<Text className="text-center font-bold font-mono text-foreground text-lg">
								Terminal no autorizada
							</Text>
							<Text className="mt-2 px-4 text-center font-mono text-muted text-xs leading-relaxed">
								Por favor, inicie sesión con sus credenciales del Umbral para
								acceder a las transmisiones en tiempo real.
							</Text>
						</View>

						<View className="gap-3">
							<Pressable
								className="flex-row items-center justify-center gap-2 rounded-lg bg-accent px-4 py-4 shadow-accent/25 shadow-lg active:opacity-85"
								onPress={() => {
									void beginLogin();
								}}
							>
								<Ionicons name="key-outline" size={18} color="#000" />
								<Text className="font-black font-mono text-background text-sm uppercase tracking-wider">
									Acceder al Sistema
								</Text>
							</Pressable>

							<Pressable
								className="flex-row items-center justify-center gap-2 rounded-lg border border-border/30 bg-transparent px-4 py-4 active:bg-foreground/5"
								onPress={() => {
									void beginLogin({ action: "register" });
								}}
							>
								<Ionicons
									name="person-add-outline"
									size={18}
									color={foregroundColor}
								/>
								<Text className="font-bold font-mono text-foreground text-sm uppercase tracking-wider">
									Registrar Jugador
								</Text>
							</Pressable>
						</View>
					</Card>

					<View className="flex-row items-center justify-center gap-2">
						<Ionicons name="lock-closed-outline" size={12} color={mutedColor} />
						<Text className="font-mono text-[10px] text-muted uppercase tracking-widest">
							Sesión encriptada vía Keycloak (PKCE)
						</Text>
					</View>
				</View>
			)}
		</Container>
	);
}
