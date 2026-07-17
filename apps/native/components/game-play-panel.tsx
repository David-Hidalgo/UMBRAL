import { Ionicons } from "@expo/vector-icons";
import * as signalR from "@microsoft/signalr";
import { env } from "@umbral/env/native";
import { Spinner } from "heroui-native";
import { useCallback, useEffect, useRef, useState } from "react";
import { Alert, Pressable, Text, View } from "react-native";

import { getTeamHints, type ReleasedHint, requestHint } from "@/lib/api/hints";
import {
	getCurrentChallenge,
	getTeamScore,
	type PlayerSubStage,
	type SubmitEvidenceResult,
	submitEvidence,
} from "@/lib/api/scoring";
import { ChallengeCard } from "./challenge-card";
import { HintList } from "./hint-list";
import { ScoreBadge } from "./score-badge";

// Payload del evento SignalR "HintReleased" (hub de sesiones, grupo team-{teamId}).
interface HintReleasedEvent {
	sessionId: string;
	teamId: string;
	releasedHintId: string;
	subStageId: string;
	clueId: string;
	text: string;
	order: number;
	releasedAt: string;
}

interface GamePlayPanelProps {
	sessionId: string;
	teamId: string;
	currentStageIndex: number;
	getAccessToken: (minValidity?: number) => Promise<string | null>;
	/** true cuando el temporizador de la sesión llegó a 0: se bloquea el envío (RF-06). */
	isTimeUp: boolean;
}

export function GamePlayPanel({
	sessionId,
	teamId,
	currentStageIndex,
	getAccessToken,
	isTimeUp,
}: GamePlayPanelProps) {
	const [subStages, setSubStages] = useState<PlayerSubStage[]>([]);
	const [loadingChallenge, setLoadingChallenge] = useState(true);
	const [score, setScore] = useState<number | null>(null);
	const [submittingSubId, setSubmittingSubId] = useState<string | null>(null);
	const [results, setResults] = useState<Record<string, SubmitEvidenceResult>>(
		{},
	);
	const [hints, setHints] = useState<ReleasedHint[]>([]);
	const [requestingHint, setRequestingHint] = useState(false);
	const [pendingHintSubIds, setPendingHintSubIds] = useState<Set<string>>(
		new Set(),
	);
	const hubRef = useRef<signalR.HubConnection | null>(null);
	const sessionHubRef = useRef<signalR.HubConnection | null>(null);

	const refreshScore = useCallback(async () => {
		try {
			const token = await getAccessToken(60);
			if (!token) return;
			const teamScore = await getTeamScore(sessionId, teamId, token);
			setScore(teamScore.totalPoints);
		} catch {
			// El equipo puede no tener puntaje aún; se ignora silenciosamente.
		}
	}, [sessionId, teamId, getAccessToken]);

	// Carga el reto actual (vista segura, sin respuestas). Se recarga al avanzar de etapa.
	useEffect(() => {
		let active = true;
		setLoadingChallenge(true);
		const load = async () => {
			try {
				const token = await getAccessToken(60);
				if (!token) return;
				const challenge = await getCurrentChallenge(sessionId, teamId, token);
				if (active) setSubStages(challenge.subStages);
			} catch (err) {
				if (active) {
					const message = err instanceof Error ? err.message : String(err);
					Alert.alert("Error", `No se pudo cargar el reto: ${message}`);
				}
			} finally {
				if (active) setLoadingChallenge(false);
			}
		};
		void load();
		void refreshScore();
		return () => {
			active = false;
		};
	}, [sessionId, teamId, currentStageIndex, getAccessToken, refreshScore]);

	// Conexión en tiempo real al hub de puntuación (grupo del equipo).
	useEffect(() => {
		let active = true;
		const connection = new signalR.HubConnectionBuilder()
			.withUrl(`${env.EXPO_PUBLIC_SERVER_URL}/hubs/scoring`, {
				accessTokenFactory: async () => (await getAccessToken(60)) ?? "",
			})
			.withAutomaticReconnect()
			.build();

		connection.on(
			"ScoreUpdated",
			(payload: { sessionId: string; teamId: string; totalPoints: number }) => {
				if (active && payload.teamId === teamId) {
					setScore(payload.totalPoints);
				}
			},
		);

		// Resiliencia (frontend-spec §4): re-sincroniza el puntaje al reconectar.
		connection.onreconnected(() => {
			void refreshScore();
		});

		const start = async () => {
			try {
				await connection.start();
				if (!active) return;
				await connection.invoke("JoinTeamGroup", sessionId, teamId);
				hubRef.current = connection;
			} catch {
				// Si el hub falla, el puntaje sigue disponible por HTTP.
			}
		};
		void start();

		return () => {
			active = false;
			const conn = hubRef.current;
			if (conn) {
				conn
					.invoke("LeaveTeamGroup", sessionId, teamId)
					.catch(() => undefined)
					.finally(() => conn.stop());
				hubRef.current = null;
			} else {
				connection.stop().catch(() => undefined);
			}
		};
	}, [sessionId, teamId, getAccessToken, refreshScore]);

	// Pistas: carga inicial + tiempo real. El evento HintReleased vive en el hub de
	// SESIONES (grupo team-{teamId}); OJO: aquí JoinTeamGroup toma UN solo argumento,
	// a diferencia del hub de scoring que toma (sessionId, teamId).
	useEffect(() => {
		let active = true;

		const loadHints = async () => {
			try {
				const token = await getAccessToken(60);
				if (!token) return;
				const released = await getTeamHints(sessionId, teamId, token);
				if (active) setHints(released);
			} catch {
				// Silencioso: aún sin pistas o sesión no disponible.
			}
		};
		void loadHints();

		// Sondeo de respaldo: si el evento HintReleased no se entrega (reconexión, o
		// esta segunda conexión al mismo hub que la de index.tsx), las pistas otorgadas
		// aparecen igual al re-sincronizar por HTTP cada pocos segundos.
		const hintPoll = setInterval(() => {
			void loadHints();
		}, 4000);

		const connection = new signalR.HubConnectionBuilder()
			.withUrl(`${env.EXPO_PUBLIC_SERVER_URL}/hubs/sessions`, {
				accessTokenFactory: async () => (await getAccessToken(60)) ?? "",
			})
			.withAutomaticReconnect()
			.build();

		connection.on("HintReleased", (payload: HintReleasedEvent) => {
			if (!active || payload.teamId !== teamId) return;
			setHints((prev) =>
				prev.some((h) => h.clueId === payload.clueId)
					? prev
					: [
							...prev,
							{
								id: payload.releasedHintId,
								teamId: payload.teamId,
								subStageId: payload.subStageId,
								clueId: payload.clueId,
								text: payload.text,
								order: payload.order,
								releasedAt: payload.releasedAt,
							},
						],
			);
			setPendingHintSubIds((prev) => {
				const next = new Set(prev);
				next.delete(payload.subStageId);
				return next;
			});
			Alert.alert("Nueva pista", payload.text);
		});

		connection.onreconnected(() => {
			void loadHints();
		});

		const start = async () => {
			try {
				await connection.start();
				if (!active) return;
				await connection.invoke("JoinTeamGroup", teamId);
				sessionHubRef.current = connection;
			} catch {
				// Si el hub falla, las pistas siguen disponibles por HTTP (loadHints).
			}
		};
		void start();

		return () => {
			active = false;
			clearInterval(hintPoll);
			const conn = sessionHubRef.current;
			if (conn) {
				conn
					.invoke("LeaveTeamGroup", teamId)
					.catch(() => undefined)
					.finally(() => conn.stop());
				sessionHubRef.current = null;
			} else {
				connection.stop().catch(() => undefined);
			}
		};
	}, [sessionId, teamId, getAccessToken]);

	const handleRequestHint = async (subStageId: string) => {
		setRequestingHint(true);
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");
			await requestHint(sessionId, teamId, subStageId, token);
			setPendingHintSubIds((prev) => new Set(prev).add(subStageId));
			Alert.alert(
				"Solicitud enviada",
				"Espera a que el operador te otorgue la pista.",
			);
		} catch (err) {
			const message = err instanceof Error ? err.message : String(err);
			Alert.alert("No se pudo pedir la pista", message);
		} finally {
			setRequestingHint(false);
		}
	};

	// Devuelve true si el servidor aceptó la solicitud (lo usa el flujo QR para
	// cerrar el escáner; un envío fallido lo deja abierto para reintentar).
	const handleSubmit = async (
		subStageId: string,
		payload: string,
	): Promise<boolean> => {
		// Guard: si el tiempo se agotó no se envía (además de ocultarse el botón).
		if (isTimeUp) return false;
		const subStage = subStages.find((s) => s.id === subStageId);
		if (!subStage) return false;

		setSubmittingSubId(subStageId);
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");
			const res = await submitEvidence(
				sessionId,
				teamId,
				{ type: subStage.type, subStageId, payload },
				token,
			);
			setResults((prev) => ({ ...prev, [subStageId]: res }));
			setScore(res.teamTotalPoints);
			// Trivia: un solo intento. Si falla, se avisa que perdió su oportunidad.
			if (subStage.type === "Trivia" && res.status === "Incorrect") {
				Alert.alert(
					"Respuesta incorrecta",
					"Perdiste tu único intento en esta pregunta.",
				);
			}
			return true;
		} catch (err) {
			const message = err instanceof Error ? err.message : String(err);
			Alert.alert("Error al enviar", message);
			return false;
		} finally {
			setSubmittingSubId(null);
		}
	};

	// Una subetapa a la vez: se muestra la primera que el equipo no haya cerrado.
	// Trivia se cierra con cualquier intento (1 solo intento); la búsqueda, al acertar.
	const isDone = (s: PlayerSubStage) => {
		if (s.closed) return true;
		const r = results[s.id];
		if (!r) return false;
		return s.type === "Trivia" ? true : r.status === "Correct";
	};
	const currentIndex = subStages.findIndex((s) => !isDone(s));
	const current = currentIndex >= 0 ? subStages[currentIndex] : null;
	const allDone = subStages.length > 0 && currentIndex === -1;

	return (
		<View className="mb-6 gap-3">
			<ScoreBadge points={score} />

			{loadingChallenge ? (
				<View className="items-center py-6">
					<Spinner />
				</View>
			) : subStages.length === 0 ? (
				<Text className="py-4 text-center font-mono text-[10px] text-muted uppercase">
					Esperando el reto de esta etapa...
				</Text>
			) : current ? (
				<View className="gap-2">
					{subStages.length > 1 && (
						<Text className="text-center font-mono text-[10px] text-muted uppercase tracking-widest">
							Reto {currentIndex + 1} de {subStages.length}
						</Text>
					)}
					<ChallengeCard
						isTimeUp={isTimeUp}
						key={current.id}
						onSubmit={(payload) => handleSubmit(current.id, payload)}
						result={results[current.id]}
						subStage={current}
						submitting={submittingSubId === current.id}
					/>

					<HintList hints={hints.filter((h) => h.subStageId === current.id)} />

					{!isTimeUp &&
						(pendingHintSubIds.has(current.id) ? (
							<View className="items-center rounded-lg border border-border/20 bg-muted/10 py-3">
								<Text className="font-mono text-[10px] text-muted uppercase tracking-widest">
									Solicitud enviada · espera al operador
								</Text>
							</View>
						) : (
							<Pressable
								className="flex-row items-center justify-center gap-2 rounded-lg border border-accent/40 bg-accent/10 py-3"
								disabled={requestingHint}
								onPress={() => handleRequestHint(current.id)}
							>
								<Ionicons color="#fbbf24" name="bulb-outline" size={16} />
								<Text className="font-bold font-mono text-accent text-xs uppercase tracking-widest">
									{requestingHint ? "Enviando..." : "Pedir pista"}
								</Text>
							</Pressable>
						))}
				</View>
			) : null}

			{allDone && (
				<View className="items-center rounded-lg border border-success/30 bg-success/10 p-3">
					<Text className="text-center font-bold font-mono text-[10px] text-success uppercase tracking-widest">
						Retos completados
					</Text>
					<Text className="mt-1 text-center font-mono text-[10px] text-muted uppercase">
						Esperando a que el operador avance a la siguiente etapa...
					</Text>
				</View>
			)}
		</View>
	);
}
