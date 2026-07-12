import * as signalR from "@microsoft/signalr";
import { env } from "@umbral/env/native";
import { Spinner } from "heroui-native";
import { useCallback, useEffect, useRef, useState } from "react";
import { Alert, Text, View } from "react-native";

import {
	getCurrentChallenge,
	getTeamScore,
	type PlayerSubStage,
	type SubmitEvidenceResult,
	submitEvidence,
} from "@/lib/api/scoring";
import { ChallengeCard } from "./challenge-card";
import { ScoreBadge } from "./score-badge";

interface GamePlayPanelProps {
	sessionId: string;
	teamId: string;
	currentStageIndex: number;
	getAccessToken: (minValidity?: number) => Promise<string | null>;
}

export function GamePlayPanel({
	sessionId,
	teamId,
	currentStageIndex,
	getAccessToken,
}: GamePlayPanelProps) {
	const [subStages, setSubStages] = useState<PlayerSubStage[]>([]);
	const [loadingChallenge, setLoadingChallenge] = useState(true);
	const [score, setScore] = useState<number | null>(null);
	const [submittingSubId, setSubmittingSubId] = useState<string | null>(null);
	const [results, setResults] = useState<Record<string, SubmitEvidenceResult>>(
		{},
	);
	const hubRef = useRef<signalR.HubConnection | null>(null);

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

	const handleSubmit = async (subStageId: string, payload: string) => {
		const subStage = subStages.find((s) => s.id === subStageId);
		if (!subStage) return;

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
		} catch (err) {
			const message = err instanceof Error ? err.message : String(err);
			Alert.alert("Error al enviar", message);
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
						key={current.id}
						onSubmit={(payload) => handleSubmit(current.id, payload)}
						result={results[current.id]}
						subStage={current}
						submitting={submittingSubId === current.id}
					/>
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
