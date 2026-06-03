import { useEffect, useState } from "react";
import { Ionicons } from "@expo/vector-icons";
import type { AppRole } from "@umbral/auth/types";
import {
	Chip,
	Spinner,
	Surface,
	useThemeColor,
} from "heroui-native";
import { Text, View } from "react-native";
import * as Network from "expo-network";

import { Container } from "@/components/container";
import { useAuth } from "@/contexts/auth-context";
import { env } from "@umbral/env/native";

function formatRole(role: AppRole): string {
	const labels: Record<AppRole, string> = {
		admin: "Admin",
		operator: "Operador",
		player: "Jugador",
	};
	return labels[role];
}

function getFullName(user: {
	firstName?: string;
	lastName?: string;
	username: string;
}): string {
	const full = [user.firstName, user.lastName].filter(Boolean).join(" ");
	return full || user.username;
}

function getInitial(user: {
	firstName?: string;
	username: string;
}): string {
	const name = user.firstName ?? user.username;
	return name.slice(0, 1).toUpperCase();
}

export default function Home() {
	const { user } = useAuth();
	const successColor = useThemeColor("success");
	const dangerColor = useThemeColor("danger");
	const mutedColor = useThemeColor("muted");

	const [isConnected, setIsConnected] = useState<boolean | null>(null);
	const [isLoading, setIsLoading] = useState(true);

	useEffect(() => {
		let isMounted = true;
		async function checkConnection() {
			try {
				const networkState = await Network.getNetworkStateAsync();
				if (!networkState.isConnected) {
					if (isMounted) {
						setIsConnected(false);
						setIsLoading(false);
					}
					return;
				}

				const controller = new AbortController();
				const timeoutId = setTimeout(() => controller.abort(), 5000);

				const response = await fetch(`${env.EXPO_PUBLIC_SERVER_URL}/health`, {
					signal: controller.signal,
				});
				clearTimeout(timeoutId);

				if (isMounted) {
					setIsConnected(response.ok);
				}
			} catch {
				if (isMounted) {
					setIsConnected(false);
				}
			} finally {
				if (isMounted) {
					setIsLoading(false);
				}
			}
		}

		checkConnection();
		const interval = setInterval(checkConnection, 10000);

		return () => {
			isMounted = false;
			clearInterval(interval);
		};
	}, []);

	if (!user) {
		return null;
	}

	const primaryRole = user.roles[0];
	const greetingName = user.firstName ?? user.username;

	return (
		<Container className="px-4 pb-4">
			<View className="mb-5 py-6">
				<Text className="font-bold text-3xl text-foreground tracking-tight">
					Hola, {greetingName}
				</Text>
				<Text className="mt-1 text-muted text-sm">Bienvenido a UMBRAL</Text>
			</View>

			<Surface variant="secondary" className="mt-6 rounded-xl p-4">
				<View className="flex-row items-center gap-3">
					<View className="size-10 items-center justify-center rounded-full bg-foreground/10">
						<Text className="font-semibold text-foreground text-sm">
							{getInitial(user)}
						</Text>
					</View>
					<View className="min-w-0 flex-1">
						<Text className="font-medium text-foreground text-sm">
							{getFullName(user)}
						</Text>
						<Text className="mt-0.5 text-muted text-xs">{user.email}</Text>
					</View>
					{primaryRole && (
						<Chip variant="secondary" color="accent" size="sm">
							<Chip.Label>{formatRole(primaryRole)}</Chip.Label>
						</Chip>
					)}
				</View>
			</Surface>

			<Surface
				variant="secondary"
				className="mt-4 items-center rounded-xl p-6"
			>
				<Ionicons
					name="game-controller-outline"
					size={40}
					color={mutedColor}
				/>
				<Text className="mt-4 font-semibold text-foreground text-lg">
					Sin sesión activa
				</Text>
				<Text className="mt-1 text-center text-muted text-sm">
					El operador debe iniciar una sesión para que puedas participar.
				</Text>
				<View
					className="mt-4 rounded-full border border-foreground/20 px-4 py-1.5"
					style={{ borderStyle: "dashed" }}
				>
					<Text className="text-[11px] text-muted">Próximamente</Text>
				</View>
			</Surface>

			<Surface variant="secondary" className="mt-4 rounded-xl p-4">
				<View className="mb-3 flex-row items-center justify-between">
					<Text className="font-medium text-foreground">
						Conexión con el servidor
					</Text>
					<Chip
						variant="secondary"
						color={isConnected ? "success" : "danger"}
						size="sm"
					>
						<Chip.Label>{isConnected ? "LIVE" : "OFFLINE"}</Chip.Label>
					</Chip>
				</View>

				<Surface variant="tertiary" className="rounded-lg p-3">
					<View className="flex-row items-center">
						<View
							className={`mr-3 h-2 w-2 rounded-full ${isConnected ? "bg-success" : "bg-muted"}`}
						/>
						<View className="flex-1">
							<Text className="mt-0.5 text-muted text-xs">
								{isLoading
									? "Comprobando conexión..."
									: isConnected
										? "Conectado al servidor"
										: "Sin conexión al servidor"}
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
