import { Ionicons } from "@expo/vector-icons";
import { Card, Spinner, useThemeColor } from "heroui-native";
import { useCallback, useEffect, useRef, useState } from "react";
import { Pressable, ScrollView, Text, View } from "react-native";
import { Container } from "@/components/container";
import { getRanking, type RankingEntry } from "@/lib/api/scoring";

// Colores de medalla para los tres primeros puestos (paridad con la web).
const MEDAL_COLORS: Record<number, string> = {
	1: "#f59e0b", // oro
	2: "#a1a1aa", // plata
	3: "#ea580c", // bronce
};

interface GameResultsProps {
	getAccessToken: (minValiditySeconds?: number) => Promise<string | null>;
	myTeamId?: string;
	onExit: () => void;
	sessionId: string;
	sessionName: string;
}

// Pantalla de fin de juego (RF-12): informe final con el puntaje del escuadrón
// del jugador y la clasificación completa de la operación.
export function GameResults({
	getAccessToken,
	myTeamId,
	onExit,
	sessionId,
	sessionName,
}: GameResultsProps) {
	const accentColor = useThemeColor("accent");
	const dangerColor = useThemeColor("danger");
	const mutedColor = useThemeColor("muted");

	const [ranking, setRanking] = useState<RankingEntry[] | null>(null);
	const [loading, setLoading] = useState(true);
	const [error, setError] = useState<string | null>(null);
	const activeRef = useRef(true);

	// Carga el ranking final. Reintenta una vez tras 1s: al finalizar la sesión,
	// el evento de RabbitMQ hacia scoring puede seguir en vuelo cuando el aviso
	// de SignalR ya llegó al móvil.
	const fetchRanking = useCallback(async () => {
		setLoading(true);
		setError(null);

		const attempt = async (): Promise<RankingEntry[]> => {
			const token = await getAccessToken(60);
			if (!token) {
				throw new Error("Sesión expirada. Por favor, inicia sesión de nuevo.");
			}
			return getRanking(sessionId, token);
		};

		try {
			let entries: RankingEntry[];
			try {
				entries = await attempt();
			} catch {
				await new Promise((resolve) => setTimeout(resolve, 1000));
				entries = await attempt();
			}
			if (activeRef.current) {
				setRanking(entries);
			}
		} catch (err) {
			if (activeRef.current) {
				setError(
					err instanceof Error
						? err.message
						: "No se pudo obtener el informe final.",
				);
			}
		} finally {
			if (activeRef.current) {
				setLoading(false);
			}
		}
	}, [getAccessToken, sessionId]);

	useEffect(() => {
		activeRef.current = true;
		void fetchRanking();
		return () => {
			activeRef.current = false;
		};
	}, [fetchRanking]);

	const myEntry = ranking?.find((entry) => entry.teamId === myTeamId) ?? null;

	return (
		<Container className="bg-background p-6">
			<ScrollView className="flex-1" showsVerticalScrollIndicator={false}>
				{/* Encabezado de cierre de operación */}
				<View className="mb-6 items-center border-border/20 border-b py-6">
					<View className="mb-3 rounded-full border border-accent/20 bg-accent/10 p-4">
						<Ionicons color={accentColor} name="trophy-outline" size={32} />
					</View>
					<Text className="text-center font-black font-mono text-3xl text-foreground tracking-tight">
						¡PARTIDA FINALIZADA!
					</Text>
					<Text className="mt-1 text-center font-mono text-[10px] text-muted uppercase tracking-widest">
						Op: {sessionName}
					</Text>
				</View>

				{/* Resultado del escuadrón del jugador */}
				{myEntry && (
					<Card
						className="mb-6 border border-accent/30 bg-accent/10 p-5"
						variant="secondary"
					>
						<Text className="mb-1 font-bold font-mono text-[10px] text-muted uppercase tracking-wider">
							RESULTADO DE TU ESCUADRÓN
						</Text>
						<View className="flex-row items-end justify-between">
							<View className="flex-1 pr-4">
								<Text className="font-black font-mono text-2xl text-accent">
									{myEntry.teamCode}
								</Text>
								<Text className="mt-0.5 font-mono text-muted text-xs uppercase">
									Puesto #{myEntry.rank} de {ranking?.length ?? 0}
								</Text>
							</View>
							<Text className="font-black font-mono text-4xl text-foreground tabular-nums">
								{myEntry.totalPoints}
								<Text className="text-muted text-sm"> pts</Text>
							</Text>
						</View>
					</Card>
				)}

				{/* Clasificación completa */}
				<Text className="mb-3 font-bold font-mono text-muted text-xs uppercase tracking-widest">
					CLASIFICACIÓN FINAL
				</Text>

				{loading && (
					<View className="items-center py-12">
						<Spinner color="default" size="md" />
						<Text className="mt-2 font-mono text-muted text-xs uppercase">
							Compilando informe final...
						</Text>
					</View>
				)}

				{!loading && error && (
					<View className="items-center justify-center rounded-xl border border-danger/25 bg-danger/10 px-4 py-8">
						<Ionicons
							color={dangerColor}
							name="alert-circle-outline"
							size={24}
						/>
						<Text className="mt-2 text-center font-mono text-danger text-xs">
							{error}
						</Text>
						<Pressable
							className="mt-4 rounded-lg border border-border/20 bg-muted/10 px-4 py-2 active:opacity-75"
							onPress={() => void fetchRanking()}
						>
							<Text className="font-mono text-foreground text-xs uppercase">
								Reintentar
							</Text>
						</Pressable>
					</View>
				)}

				{!(loading || error) && ranking && ranking.length === 0 && (
					<View className="items-center justify-center rounded-xl border border-border/20 border-dashed bg-card/5 px-4 py-12">
						<Ionicons color={mutedColor} name="podium-outline" size={24} />
						<Text className="mt-2 font-mono text-muted text-xs uppercase">
							Sin equipos en el marcador
						</Text>
					</View>
				)}

				{!(loading || error) && ranking && ranking.length > 0 && (
					<View className="gap-3">
						{ranking.map((entry) => {
							const isMine = entry.teamId === myTeamId;
							const medalColor = MEDAL_COLORS[entry.rank];
							return (
								<View
									className={`flex-row items-center justify-between rounded-xl border p-4 ${
										isMine
											? "border-accent/30 bg-accent/10"
											: "border-border/10 bg-card/20"
									}`}
									key={entry.teamId}
								>
									<View className="flex-1 flex-row items-center gap-3 pr-4">
										<View className="h-8 w-8 items-center justify-center rounded-full border border-border/20 bg-muted/10">
											{medalColor ? (
												<Ionicons
													color={medalColor}
													name="medal-outline"
													size={16}
												/>
											) : (
												<Text className="font-bold font-mono text-muted text-xs">
													{entry.rank}
												</Text>
											)}
										</View>
										<View className="flex-1">
											<Text
												className={`font-bold font-mono text-sm ${
													isMine ? "text-accent" : "text-foreground"
												}`}
											>
												{entry.teamCode} {isMine ? "(Tú)" : ""}
											</Text>
											<Text className="font-mono text-[9px] text-muted uppercase">
												Puesto #{entry.rank}
											</Text>
										</View>
									</View>
									<Text className="font-bold font-mono text-foreground text-sm tabular-nums">
										{entry.totalPoints} pts
									</Text>
								</View>
							);
						})}
					</View>
				)}

				{/* Volver al buscador de operaciones */}
				<Pressable
					className="mt-6 mb-8 flex-row items-center justify-center gap-2 rounded-lg bg-accent px-4 py-3.5 active:opacity-85"
					onPress={onExit}
				>
					<Ionicons color="#000" name="exit-outline" size={16} />
					<Text className="font-black font-mono text-background text-sm uppercase tracking-wider">
						Volver a Operaciones
					</Text>
				</Pressable>
			</ScrollView>
		</Container>
	);
}
