import { Ionicons } from "@expo/vector-icons";
import * as signalR from "@microsoft/signalr";
import { env } from "@umbral/env/native";
import { Card, Chip, Spinner, useThemeColor } from "heroui-native";
import { useCallback, useEffect, useRef, useState } from "react";
import {
	Alert,
	Animated,
	Easing,
	Pressable,
	ScrollView,
	Text,
	TextInput,
	View,
} from "react-native";
import { Container } from "@/components/container";
import { GamePlayPanel } from "@/components/game-play-panel";
import { PlayerHeader } from "@/components/player-header";
import { SessionTimer } from "@/components/session-timer";
import { useAuth } from "@/contexts/auth-context";
import {
	getAllSessions,
	getSessionById,
	joinTeam,
	type LiveSessionDetail,
	type LiveSessionSummary,
} from "@/lib/api/sessions";
import { useSessionCountdown } from "@/lib/use-session-countdown";

const STATUS_LABELS = {
	Programada: "Programada",
	Preparacion: "En Preparación",
	Activa: "En Curso / Activa",
	Pausada: "Pausada",
	Finalizada: "Finalizada",
	Cancelada: "Cancelada",
} as const;

// ─── Signal Dot ───────────────────────────────────────────────────────────────
// Cross-platform animated status indicator. Uses Animated.View for iOS/Android
// (animate-ping is a web-only CSS animation). Works identically on Expo Web.

type SignalStatus = "disconnected" | "connecting" | "connected" | "failed";

function SignalDot({ status }: { status: SignalStatus }) {
	const pingAnim = useRef(new Animated.Value(1)).current;

	useEffect(() => {
		if (status !== "connected") {
			pingAnim.stopAnimation();
			pingAnim.setValue(1);
			return;
		}

		const loop = Animated.loop(
			Animated.sequence([
				Animated.timing(pingAnim, {
					toValue: 2,
					duration: 800,
					easing: Easing.out(Easing.ease),
					useNativeDriver: true,
				}),
				Animated.timing(pingAnim, {
					toValue: 1,
					duration: 0,
					useNativeDriver: true,
				}),
			]),
		);
		loop.start();
		return () => loop.stop();
	}, [status, pingAnim]);

	const dotColor =
		status === "connected"
			? "#22c55e" // success green
			: status === "connecting"
				? "#fbbf24" // amber
				: "#ef4444"; // danger red

	return (
		<View style={{ width: 8, height: 8 }}>
			{/* Pulsing ring — only shown when connected */}
			{status === "connected" && (
				<Animated.View
					style={{
						position: "absolute",
						width: 8,
						height: 8,
						borderRadius: 4,
						backgroundColor: dotColor,
						opacity: pingAnim.interpolate({
							inputRange: [1, 2],
							outputRange: [0.6, 0],
						}),
						transform: [{ scale: pingAnim }],
					}}
				/>
			)}
			{/* Solid center dot */}
			<View
				style={{
					position: "absolute",
					width: 8,
					height: 8,
					borderRadius: 4,
					backgroundColor: dotColor,
				}}
			/>
		</View>
	);
}

export default function Home() {
	const {
		status,
		session,
		beginLogin,
		signOut,
		hasRealmRole,
		getAccessToken,
		getSubjectId,
	} = useAuth();

	const mutedColor = useThemeColor("muted");
	const dangerColor = useThemeColor("danger");
	const foregroundColor = useThemeColor("foreground");
	const accentColor = useThemeColor("accent");

	// Live Session Gameplay state
	const [activeSessions, setActiveSessions] = useState<LiveSessionSummary[]>(
		[],
	);
	const [loadingSessions, setLoadingSessions] = useState(false);
	const [manualSessionId, setManualSessionId] = useState("");
	const [selectedSessionId, setSelectedSessionId] = useState<string | null>(
		null,
	);
	const [sessionDetail, setSessionDetail] = useState<LiveSessionDetail | null>(
		null,
	);
	const [loadingDetail, setLoadingDetail] = useState(false);
	const [joiningTeamId, setJoiningTeamId] = useState<string | null>(null);

	// Cuenta regresiva de la sesión (RF-06): única fuente para mostrar el reloj
	// y para bloquear el envío de respuestas cuando el tiempo se agota.
	const countdown = useSessionCountdown(
		sessionDetail?.status ?? "",
		sessionDetail?.endsAt ?? null,
		sessionDetail?.remainingSecondsOnPause ?? null,
	);

	// SignalR status
	const [signalrStatus, setSignalrStatus] = useState<
		"disconnected" | "connecting" | "connected" | "failed"
	>("disconnected");
	const hubConnectionRef = useRef<signalR.HubConnection | null>(null);

	// Fetch available joinable sessions
	const fetchAvailableSessions = useCallback(async () => {
		if (!hasRealmRole("player")) return;
		setLoadingSessions(true);
		try {
			const token = await getAccessToken(60);
			if (!token) return;

			// Fetch Preparing/Active sessions
			const prepSessions = await getAllSessions(token, {
				status: "Preparacion",
			});
			const actSessions = await getAllSessions(token, { status: "Activa" });

			// Merge and unique
			const merged = [...prepSessions, ...actSessions];
			const unique = merged.filter(
				(s, idx, self) => self.findIndex((x) => x.id === s.id) === idx,
			);

			setActiveSessions(unique);
		} catch (err) {
			console.warn("Failed to load available sessions", err);
		} finally {
			setLoadingSessions(false);
		}
	}, [getAccessToken, hasRealmRole]);

	// Refresh sessions list periodically when not in session
	useEffect(() => {
		if (
			status === "authenticated" &&
			hasRealmRole("player") &&
			!selectedSessionId
		) {
			fetchAvailableSessions();
			const interval = setInterval(fetchAvailableSessions, 10000);
			return () => clearInterval(interval);
		}
	}, [status, hasRealmRole, selectedSessionId, fetchAvailableSessions]);

	// Fetch details for a chosen session
	const fetchSessionDetails = useCallback(
		async (id: string) => {
			setLoadingDetail(true);
			try {
				const token = await getAccessToken(60);
				if (!token) return;

				const details = await getSessionById(id, token);
				setSessionDetail(details);
			} catch (err) {
				const errorMsg = err instanceof Error ? err.message : String(err);
				Alert.alert(
					"Error de Enlace",
					errorMsg || "No se pudo conectar con la sesión.",
				);
				setSelectedSessionId(null);
				setSessionDetail(null);
			} finally {
				setLoadingDetail(false);
			}
		},
		[getAccessToken],
	);

	// Watch selected session details
	useEffect(() => {
		if (selectedSessionId) {
			fetchSessionDetails(selectedSessionId);
		} else {
			setSessionDetail(null);
		}
	}, [selectedSessionId, fetchSessionDetails]);

	// Determine if user is in a team
	const keycloakUserId = getSubjectId();
	const myTeam = sessionDetail?.teams.find((t) =>
		t.players.some((p) => p.keycloakUserId === keycloakUserId),
	);

	// WebSocket handler
	useEffect(() => {
		if (!selectedSessionId || !myTeam) {
			// Disconnect SignalR if session or team is deselected
			if (hubConnectionRef.current) {
				hubConnectionRef.current.stop();
				hubConnectionRef.current = null;
				setSignalrStatus("disconnected");
			}
			return;
		}

		let active = true;
		setSignalrStatus("connecting");

		const connection = new signalR.HubConnectionBuilder()
			.withUrl(`${env.EXPO_PUBLIC_SERVER_URL}/hubs/sessions`, {
				accessTokenFactory: async () => {
					const token = await getAccessToken(60);
					return token ?? "";
				},
			})
			.withAutomaticReconnect()
			.build();

		connection.on(
			"PlayerJoined",
			(_event: { sessionId: string; teamId: string; displayName: string }) => {
				if (!active) return;
				// Refresh session detail to see newly joined player
				fetchSessionDetails(selectedSessionId);
			},
		);

		connection.on(
			"StateChanged",
			(event: { sessionId: string; status: string }) => {
				if (!active) return;
				if (event.status === "Finalizada" || event.status === "Cancelada") {
					Alert.alert(
						"Operación Finalizada",
						`La partida ha sido ${event.status === "Finalizada" ? "completada" : "cancelada"} por el operador táctico.`,
					);
					setSelectedSessionId(null);
					setSessionDetail(null);
				} else {
					fetchSessionDetails(selectedSessionId);
				}
			},
		);

		connection.on(
			"StageAdvanced",
			(_event: { sessionId: string; currentStageIndex: number }) => {
				if (!active) return;
				fetchSessionDetails(selectedSessionId);
			},
		);

		const startConnection = async () => {
			try {
				await connection.start();
				if (!active) return;

				// Join session group
				await connection.invoke("JoinSessionGroup", selectedSessionId);
				setSignalrStatus("connected");
				hubConnectionRef.current = connection;
			} catch (err) {
				console.warn("SignalR client connection failed", err);
				if (active) setSignalrStatus("failed");
			}
		};

		void startConnection();

		return () => {
			active = false;
			if (hubConnectionRef.current) {
				const conn = hubConnectionRef.current;
				conn
					.invoke("LeaveSessionGroup", selectedSessionId)
					.catch((e) =>
						console.warn("Failed to leave group on mobile unmount", e),
					)
					.finally(() => conn.stop());
				hubConnectionRef.current = null;
			}
		};
	}, [selectedSessionId, myTeam, getAccessToken, fetchSessionDetails]);

	// Action: Join Team
	const handleJoinTeam = async (teamId: string) => {
		if (!selectedSessionId) return;
		setJoiningTeamId(teamId);
		try {
			const token = await getAccessToken(60);
			if (!token) return;

			await joinTeam(selectedSessionId, teamId, token);
			await fetchSessionDetails(selectedSessionId);
		} catch (err) {
			const errorMsg = err instanceof Error ? err.message : String(err);
			Alert.alert(
				"Error de Registro",
				errorMsg || "No se pudo unir al equipo.",
			);
		} finally {
			setJoiningTeamId(null);
		}
	};

	// Auth loader screen
	if (status === "loading") {
		return (
			<Container className="items-center justify-center bg-background p-6">
				<View className="items-center gap-4">
					<Spinner size="lg" color="default" />
					<Text className="animate-pulse font-mono text-muted text-sm tracking-widest">
						SINCRO_UMBRAL::INICIALIZANDO...
					</Text>
				</View>
			</Container>
		);
	}

	// Unauthenticated screen
	if (!session?.user) {
		return (
			<Container className="bg-background p-6">
				<View className="mb-8 border-border/20 border-b py-6">
					<Text className="font-black font-mono text-5xl text-foreground tracking-tight">
						UMBRAL
					</Text>
					<Text className="mt-1 font-mono text-muted text-xs tracking-widest">
						SISTEMA DE SUPERVISIÓN & OPERACIONES
					</Text>
				</View>

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
			</Container>
		);
	}

	// Operator / Admin views (non-player roles)
	if (!hasRealmRole("player")) {
		let roleTitle = "Operador Táctico";
		let roleColor = "accent";
		if (hasRealmRole("admin")) {
			roleTitle = "Administrador del Sistema";
			roleColor = "danger";
		}

		return (
			<Container className="bg-background p-6">
				<View className="mb-8 border-border/20 border-b py-6">
					<Text className="font-black font-mono text-5xl text-foreground tracking-tight">
						UMBRAL
					</Text>
					<Text className="mt-1 font-mono text-muted text-xs tracking-widest">
						SISTEMA DE SUPERVISIÓN & OPERACIONES
					</Text>
				</View>

				<Card
					variant="secondary"
					className="border border-border/10 bg-card/50 p-5"
				>
					<View className="mb-4 flex-row items-center justify-between">
						<View>
							<Text className="font-mono text-muted text-xs uppercase tracking-wider">
								ESTADO DE ENLACE
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
						<Text className="mb-2 font-mono text-muted text-xs uppercase">
							INFORMACIÓN DE TERMINAL OPERATIVA
						</Text>
						<Text className="font-mono text-foreground text-xs leading-relaxed">
							Esta aplicación móvil está diseñada exclusivamente para Agentes de
							Campo (Jugadores). Como operador táctico o administrador, debes
							gestionar el Centro de Mando desde la consola web.
						</Text>
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
			</Container>
		);
	}

	// ─── PLAYER FLOWS ─────────────────────────────────────────────────────────

	// Step 1: Browse/Join Sessions list
	if (!selectedSessionId) {
		return (
			<Container className="bg-background p-6">
				<ScrollView className="flex-1" showsVerticalScrollIndicator={false}>
					<PlayerHeader
						name={
							session?.user?.name ??
							session?.user?.preferred_username ??
							"Agente"
						}
						onSignOut={() => {
							void signOut();
						}}
						roleLabel="Agente de Campo"
						subtitle={session?.user?.email ?? session?.user?.preferred_username}
					/>
					{/* Header */}
					<View className="mb-6 flex-row items-center justify-between border-border/20 border-b py-4">
						<View>
							<Text className="font-black font-mono text-3xl text-foreground tracking-tight">
								OPERACIONES
							</Text>
							<Text className="mt-0.5 font-mono text-[9px] text-muted uppercase tracking-widest">
								Buscador de Enlaces Tácticos
							</Text>
						</View>
					</View>

					{/* Manual Entry card */}
					<Card
						variant="secondary"
						className="mb-6 border border-border/10 bg-card/30 p-4"
					>
						<Text className="mb-2 font-bold font-mono text-[10px] text-muted uppercase tracking-wider">
							INGRESAR CÓDIGO DE OPERACIÓN
						</Text>
						<View className="flex-row gap-2">
							<TextInput
								placeholder="Ej: f31765-8b..."
								placeholderTextColor={`${mutedColor}cc`}
								value={manualSessionId}
								onChangeText={setManualSessionId}
								className="flex-1 rounded-lg border border-border/20 bg-muted/10 px-3 py-2.5 font-mono text-foreground text-xs"
							/>
							<Pressable
								onPress={() => {
									if (manualSessionId.trim()) {
										setSelectedSessionId(manualSessionId.trim());
									} else {
										Alert.alert(
											"Código Vacío",
											"Por favor ingresa un código de partida.",
										);
									}
								}}
								className="flex-row items-center justify-center rounded-lg bg-accent px-4 active:opacity-85"
							>
								<Ionicons name="arrow-forward-outline" size={16} color="#000" />
							</Pressable>
						</View>
					</Card>

					{/* List section */}
					<View className="mb-4 flex-row items-center justify-between">
						<Text className="font-bold font-mono text-muted text-xs uppercase tracking-widest">
							OPERACIONES EN CURSO
						</Text>
						<Pressable
							onPress={fetchAvailableSessions}
							disabled={loadingSessions}
						>
							<Ionicons
								name="refresh-outline"
								size={16}
								color={accentColor}
								className={loadingSessions ? "rotate-180" : ""}
							/>
						</Pressable>
					</View>

					{loadingSessions && activeSessions.length === 0 ? (
						<View className="items-center py-12">
							<Spinner size="md" color="default" />
							<Text className="mt-2 font-mono text-muted text-xs">
								Buscando señales...
							</Text>
						</View>
					) : activeSessions.length === 0 ? (
						<View className="items-center justify-center rounded-xl border border-border/20 border-dashed bg-card/5 px-4 py-12 text-center">
							<Ionicons name="radio-outline" size={24} color={mutedColor} />
							<Text className="mt-2 font-mono text-muted text-xs uppercase">
								Sin transmisiones activas
							</Text>
							<Text className="mt-1 text-center font-mono text-[10px] text-muted/60">
								Espera a que el operador táctico despliegue la operación o
								introduce un código manual.
							</Text>
						</View>
					) : (
						<View className="mb-6 gap-3">
							{activeSessions.map((item) => (
								<Pressable
									key={item.id}
									onPress={() => setSelectedSessionId(item.id)}
									className="flex-row items-center justify-between rounded-xl border border-border/10 bg-card/20 p-4 hover:bg-card/40"
								>
									<View className="flex-1 pr-4">
										<View className="mb-1 flex-row items-center gap-2">
											<Text className="flex-1 truncate font-bold font-mono text-foreground text-sm">
												{item.name}
											</Text>
											<Chip
												color={item.status === "Activa" ? "success" : "accent"}
												variant="soft"
												className="rounded-full px-1.5 py-0.5"
											>
												<Text className="font-bold font-mono text-[8px] uppercase">
													{item.status === "Activa"
														? "En Juego"
														: "Preparación"}
												</Text>
											</Chip>
										</View>
										<Text className="truncate font-mono text-[10px] text-muted">
											Misión: {item.missionName}
										</Text>
										<Text className="mt-1 font-mono text-[9px] text-muted/80">
											Equipos: {item.teamCount} | Agentes: {item.playerCount}
										</Text>
									</View>
									<Ionicons
										name="chevron-forward-outline"
										size={18}
										color={accentColor}
									/>
								</Pressable>
							))}
						</View>
					)}
				</ScrollView>
			</Container>
		);
	}

	// Loading detail screen
	if (loadingDetail && !sessionDetail) {
		return (
			<Container className="items-center justify-center bg-background p-6">
				<View className="items-center gap-3">
					<Spinner size="lg" color="default" />
					<Text className="animate-pulse font-mono text-muted text-xs uppercase tracking-widest">
						Estableciendo enlace...
					</Text>
				</View>
			</Container>
		);
	}

	// Session is selected but detail fetch failed
	if (!sessionDetail) {
		return (
			<Container className="items-center justify-center bg-background p-6">
				<Ionicons name="alert-circle-outline" size={32} color={dangerColor} />
				<Text className="mt-2 font-bold font-mono text-foreground text-xs uppercase">
					Error de Telemetría
				</Text>
				<Pressable
					onPress={() => setSelectedSessionId(null)}
					className="mt-4 rounded-lg border border-border/20 bg-muted/10 px-4 py-2"
				>
					<Text className="font-mono text-foreground text-xs uppercase">
						Regresar
					</Text>
				</Pressable>
			</Container>
		);
	}

	// Step 2: Selection of Squad (Team) if not yet in a team
	if (!myTeam) {
		return (
			<Container className="bg-background p-6">
				<ScrollView className="flex-1" showsVerticalScrollIndicator={false}>
					<PlayerHeader
						name={
							session?.user?.name ??
							session?.user?.preferred_username ??
							"Agente"
						}
						onSignOut={() => {
							void signOut();
						}}
						roleLabel="Agente de Campo"
						subtitle={session?.user?.email ?? session?.user?.preferred_username}
					/>
					{/* Header */}
					<View className="mb-6 flex-row items-center justify-between border-border/20 border-b py-4">
						<View className="flex-1 pr-4">
							<Text className="font-black font-mono text-2xl text-foreground tracking-tight">
								REGISTRO
							</Text>
							<Text className="truncate font-mono text-[9px] text-muted uppercase tracking-widest">
								Op: {sessionDetail.name}
							</Text>
						</View>
						<Pressable
							onPress={() => setSelectedSessionId(null)}
							className="flex-row items-center gap-1 rounded-lg border border-border/20 p-2 active:bg-muted/10"
						>
							<Ionicons
								name="arrow-back-outline"
								size={14}
								color={foregroundColor}
							/>
							<Text className="font-mono text-[10px] text-foreground uppercase">
								Salir
							</Text>
						</Pressable>
					</View>

					<Card
						variant="secondary"
						className="mb-6 border border-border/10 bg-card/30 p-4"
					>
						<Text className="mb-1 font-bold font-mono text-muted-foreground text-xs uppercase">
							SELECCIÓN DE ESCUADRÓN
						</Text>
						<Text className="font-mono text-[10px] text-muted leading-relaxed">
							Para ingresar en la red táctica, debes registrarte en uno de los
							escuadrones activos de la operación.
						</Text>
					</Card>

					<Text className="mb-3 font-bold font-mono text-muted text-xs uppercase tracking-widest">
						ESCUADRONES DISPONIBLES
					</Text>

					{sessionDetail.teams.length === 0 ? (
						<View className="items-center justify-center rounded-xl border border-border/20 border-dashed bg-card/5 px-4 py-12">
							<Ionicons name="warning-outline" size={20} color={mutedColor} />
							<Text className="mt-2 font-mono text-muted text-xs uppercase">
								Sin escuadrones creados
							</Text>
							<Text className="mt-1 text-center font-mono text-[10px] text-muted/60">
								Espera a que el operador táctico registre los equipos
								correspondientes en la consola.
							</Text>
						</View>
					) : (
						<View className="mb-6 gap-3">
							{sessionDetail.teams.map((team) => {
								const isJoining = joiningTeamId === team.id;
								return (
									<View
										key={team.id}
										className="flex-row items-center justify-between rounded-xl border border-border/10 bg-card/20 p-4"
									>
										<View className="flex-1 pr-4">
											<Text className="font-bold font-mono text-foreground text-sm">
												{team.name}
											</Text>
											<Text className="mt-1 font-mono text-[10px] text-muted">
												Agentes registrados: {team.players.length}
											</Text>
										</View>
										<Pressable
											disabled={joiningTeamId !== null}
											onPress={() => handleJoinTeam(team.id)}
											className="flex-row items-center justify-center rounded-lg bg-accent px-4 py-2 active:opacity-85 disabled:opacity-50"
										>
											{isJoining ? (
												<Spinner size="sm" color="default" />
											) : (
												<>
													<Ionicons
														name="enter-outline"
														size={14}
														color="#000"
														className="mr-1"
													/>
													<Text className="font-bold font-mono text-[11px] text-background uppercase">
														Unirse
													</Text>
												</>
											)}
										</Pressable>
									</View>
								);
							})}
						</View>
					)}
				</ScrollView>
			</Container>
		);
	}

	// Step 3: Main Active Gameplay View
	return (
		<Container className="bg-background p-6">
			<ScrollView className="flex-1" showsVerticalScrollIndicator={false}>
				<PlayerHeader
					name={
						session?.user?.name ?? session?.user?.preferred_username ?? "Agente"
					}
					onSignOut={() => {
						void signOut();
					}}
					roleLabel="Agente de Campo"
					subtitle={session?.user?.email ?? session?.user?.preferred_username}
				/>
				{/* Top telemetria bar */}
				<View className="mb-4 flex-row items-center justify-between rounded-lg border border-border/10 bg-card/20 p-2 px-3">
					<View className="flex-row items-center gap-1.5">
						{/* Signal dot indicator — cross-platform (iOS/Android/Web) */}
						<SignalDot status={signalrStatus} />
						<Text className="font-mono text-[9px] text-muted-foreground uppercase">
							{signalrStatus === "connected"
								? "ENLACE ACTIVO"
								: signalrStatus === "connecting"
									? "NEGOCIANDO SEÑAL..."
									: "CONEXIÓN CAÍDA"}
						</Text>
					</View>

					<Text className="font-bold font-mono text-[9px] text-accent uppercase">
						SQUAD: {myTeam.name}
					</Text>
				</View>

				{/* Title and details */}
				<View className="mb-6 border-border/20 border-b pb-4">
					<View className="mb-1.5 flex-row flex-wrap items-center gap-2">
						<Chip color="default" variant="soft" className="rounded">
							<Text className="font-bold font-mono text-[8px] text-muted uppercase">
								OP: {sessionDetail.id.substring(0, 6)}
							</Text>
						</Chip>
						<Chip
							color={
								sessionDetail.status === "Activa"
									? "success"
									: sessionDetail.status === "Pausada"
										? "warning"
										: "accent"
							}
							variant="soft"
							className="rounded"
						>
							<Text className="font-bold font-mono text-[8px] uppercase">
								{STATUS_LABELS[
									sessionDetail.status as keyof typeof STATUS_LABELS
								] || sessionDetail.status}
							</Text>
						</Chip>
					</View>

					<Text className="font-black font-mono text-3xl text-foreground uppercase tracking-tight">
						{sessionDetail.name}
					</Text>
					<Text className="mt-0.5 font-mono text-muted text-xs uppercase">
						Misión:{" "}
						<Text className="text-foreground">{sessionDetail.missionName}</Text>
					</Text>
				</View>

				{/* Context status card */}
				<Card
					variant="secondary"
					className="mb-6 border border-border/10 bg-card/30 p-4"
				>
					{sessionDetail.status === "Preparacion" && (
						<View className="items-center py-2">
							<Spinner size="md" color="default" className="mb-2" />
							<Text className="text-center font-bold font-mono text-foreground text-xs uppercase">
								Registro Completado
							</Text>
							<Text className="mt-1 text-center font-mono text-[10px] text-muted uppercase">
								Esperando señal de inicio del centro de operaciones...
							</Text>
						</View>
					)}

					{sessionDetail.status === "Pausada" && (
						<View className="items-center gap-2 py-2">
							<Ionicons
								name="pause-circle-outline"
								size={24}
								color={dangerColor}
							/>
							<Text className="text-center font-bold font-mono text-danger text-xs uppercase">
								OPERACIÓN SUSPENDIDA
							</Text>
							<Text className="text-center font-mono text-[10px] text-muted uppercase">
								El operador táctico ha pausado temporalmente el progreso de la
								misión.
							</Text>
						</View>
					)}

					{sessionDetail.status === "Activa" && (
						<View className="py-2">
							<View className="mb-2 flex-row items-center justify-between">
								<Text className="font-bold font-mono text-foreground text-xs">
									FASE OPERATIVA ACTIVA
								</Text>
								<Text className="font-bold font-mono text-accent text-xs">
									ETAPA #{sessionDetail.currentStageIndex + 1}
								</Text>
							</View>
							<View className="h-2 w-full overflow-hidden rounded-full bg-muted/20">
								<View
									className="h-full bg-accent"
									style={{
										width: `${((sessionDetail.currentStageIndex + 1) / 5) * 100}%`, // Dummy total 5 stages if not available
									}}
								/>
							</View>
							<Text className="mt-3 text-center font-mono text-[10px] text-muted uppercase">
								Mantente a la escucha del canal de voz de tu escuadrón.
							</Text>
						</View>
					)}
				</Card>

				{/* Temporizador de la sesión (RF-06) */}
				{(sessionDetail.status === "Activa" ||
					sessionDetail.status === "Pausada") && (
					<SessionTimer
						isExpired={countdown.isExpired}
						isPaused={countdown.isPaused}
						remainingSeconds={countdown.remainingSeconds}
					/>
				)}

				{/* Juego: reto de la etapa actual + puntaje en vivo (RF-06/08) */}
				{sessionDetail.status === "Activa" && (
					<GamePlayPanel
						currentStageIndex={sessionDetail.currentStageIndex}
						getAccessToken={getAccessToken}
						isTimeUp={countdown.isExpired}
						sessionId={sessionDetail.id}
						teamId={myTeam.id}
					/>
				)}

				{/* Squad Teammates section */}
				<Text className="mb-3 font-bold font-mono text-muted text-xs uppercase tracking-widest">
					MIEMBROS DE ESCUADRÓN
				</Text>
				<View className="mb-6 gap-2.5">
					{myTeam.players.map((p) => {
						const isMe = p.keycloakUserId === keycloakUserId;
						return (
							<View
								key={p.id}
								className={`flex-row items-center justify-between rounded-lg border p-3 ${
									isMe
										? "border-accent/30 bg-accent/10"
										: "border-border/10 bg-card/15"
								}`}
							>
								<View className="flex-row items-center gap-2">
									<Ionicons
										name="person-outline"
										size={14}
										color={isMe ? accentColor : foregroundColor}
									/>
									<Text
										className={`font-bold font-mono text-xs ${isMe ? "text-accent" : "text-foreground"}`}
									>
										{p.displayName} {isMe ? "(Tú)" : ""}
									</Text>
								</View>
								<Text className="font-mono text-[9px] text-muted-foreground">
									Ingreso:{" "}
									{new Date(p.joinedAt).toLocaleTimeString([], {
										hour: "2-digit",
										minute: "2-digit",
									})}
								</Text>
							</View>
						);
					})}
				</View>

				{/* Disconnect button */}
				<Pressable
					onPress={() => {
						Alert.alert(
							"Salir de Sesión",
							"¿Estás seguro que deseas desconectarte de la operación actual?",
							[
								{ text: "Cancelar", style: "cancel" },
								{
									text: "Desconectar",
									style: "destructive",
									onPress: () => {
										setSelectedSessionId(null);
										setSessionDetail(null);
									},
								},
							],
						);
					}}
					className="mt-4 mb-8 flex-row items-center justify-center gap-2 rounded-lg border border-danger/25 bg-danger/10 px-4 py-3.5 active:opacity-75"
				>
					<Ionicons name="exit-outline" size={16} color={dangerColor} />
					<Text className="font-bold font-mono text-danger text-xs uppercase tracking-wider">
						Abandonar Canal de Operación
					</Text>
				</Pressable>
			</ScrollView>
		</Container>
	);
}
