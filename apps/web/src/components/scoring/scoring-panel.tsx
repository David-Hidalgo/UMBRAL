"use client";

import * as signalR from "@microsoft/signalr";
import { env } from "@umbral/env/web";
import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { Skeleton } from "@umbral/ui/components/skeleton";
import { History, RefreshCw, Trophy } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import {
	applyPenalty,
	getRanking,
	getSessionEvents,
	getStageProgress,
	type RankingEntry,
	ScoringApiError,
	type SessionEvent,
	type StageProgress,
} from "@/lib/api/scoring";
import { PenaltyDialog } from "./penalty-dialog";
import { RankingBoard } from "./ranking-board";
import { StageProgressBoard } from "./stage-progress-board";

interface ScoringPanelProps {
	sessionId: string;
	getAccessToken: (minValidity?: number) => Promise<string | null>;
}

interface PenaltyTarget {
	teamId: string;
	teamCode: string;
}

export function ScoringPanel({ sessionId, getAccessToken }: ScoringPanelProps) {
	const [ranking, setRanking] = useState<RankingEntry[]>([]);
	const [events, setEvents] = useState<SessionEvent[]>([]);
	const [stageProgress, setStageProgress] = useState<StageProgress | null>(
		null,
	);
	const [loading, setLoading] = useState(true);
	const [penaltyTarget, setPenaltyTarget] = useState<PenaltyTarget | null>(
		null,
	);
	const [applyingPenalty, setApplyingPenalty] = useState(false);
	const hubRef = useRef<signalR.HubConnection | null>(null);

	const refresh = useCallback(async () => {
		try {
			const token = await getAccessToken(60);
			if (!token) return;
			const [nextRanking, nextEvents, nextProgress] = await Promise.all([
				getRanking(sessionId, token),
				getSessionEvents(sessionId, token),
				getStageProgress(sessionId, token),
			]);
			setRanking(nextRanking);
			setEvents(nextEvents);
			setStageProgress(nextProgress);
		} catch (err) {
			// 404 = la sesión aún no llegó al servicio de puntuación (monitor no creado).
			if (err instanceof ScoringApiError && err.code === "not_found") {
				setRanking([]);
				setEvents([]);
				setStageProgress(null);
				return;
			}
			const message = err instanceof Error ? err.message : String(err);
			toast.error(`Error al cargar puntuación: ${message}`);
		}
	}, [sessionId, getAccessToken]);

	// El progreso de etapa cambia también sin recalcular puntaje (p. ej. una trivia fallida
	// cierra la subetapa) y al avanzar de etapa. Se refresca en silencio con un sondeo ligero.
	const loadStageProgress = useCallback(async () => {
		try {
			const token = await getAccessToken(60);
			if (!token) return;
			setStageProgress(await getStageProgress(sessionId, token));
		} catch {
			// Silencioso: 404 mientras la sesión aún no está activa en el scoring.
		}
	}, [sessionId, getAccessToken]);

	useEffect(() => {
		const interval = setInterval(() => {
			void loadStageProgress();
		}, 5000);
		return () => clearInterval(interval);
	}, [loadStageProgress]);

	// Carga inicial al seleccionar una sesión.
	useEffect(() => {
		let active = true;
		setLoading(true);
		refresh().finally(() => {
			if (active) setLoading(false);
		});
		return () => {
			active = false;
		};
	}, [refresh]);

	// Conexión en tiempo real al hub de puntuación.
	useEffect(() => {
		let active = true;
		const connection = new signalR.HubConnectionBuilder()
			.withUrl(`${env.NEXT_PUBLIC_SERVER_URL}/hubs/scoring`, {
				accessTokenFactory: async () => (await getAccessToken(60)) ?? "",
			})
			.withAutomaticReconnect()
			.build();

		connection.on(
			"RankingUpdated",
			(payload: { sessionId: string; ranking: RankingEntry[] }) => {
				if (!active) return;
				if (payload.ranking) setRanking(payload.ranking);
				// Refresca el historial de auditoría tras cada cambio.
				void refresh();
			},
		);

		// Resiliencia (frontend-spec §4): al reconectar, re-sincroniza por HTTP.
		connection.onreconnected(() => {
			void refresh();
		});

		const start = async () => {
			try {
				await connection.start();
				if (!active) return;
				await connection.invoke("JoinSessionGroup", sessionId);
				hubRef.current = connection;
			} catch {
				// Si el hub falla, el ranking sigue disponible por HTTP (refresh).
			}
		};
		void start();

		return () => {
			active = false;
			const conn = hubRef.current;
			if (conn) {
				conn
					.invoke("LeaveSessionGroup", sessionId)
					.catch(() => undefined)
					.finally(() => {
						conn.stop().catch(() => undefined);
					});
				hubRef.current = null;
			} else {
				connection.stop().catch(() => undefined);
			}
		};
	}, [sessionId, getAccessToken, refresh]);

	const handlePenalty = async (points: number, reason: string) => {
		if (!penaltyTarget) return;
		setApplyingPenalty(true);
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");
			await applyPenalty(
				sessionId,
				penaltyTarget.teamId,
				{ points, reason },
				token,
			);
			toast.success(
				`Penalización de -${points} aplicada a ${penaltyTarget.teamCode}.`,
			);
			setPenaltyTarget(null);
			await refresh();
		} catch (err) {
			const message = err instanceof Error ? err.message : String(err);
			toast.error(`Error al penalizar: ${message}`);
		} finally {
			setApplyingPenalty(false);
		}
	};

	return (
		<Card>
			<CardHeader className="flex-row items-center justify-between space-y-0">
				<CardTitle className="flex items-center gap-2 font-mono text-sm uppercase tracking-wide">
					<Trophy className="size-4 text-amber-400" />
					Ranking en vivo
				</CardTitle>
				<Button
					onClick={() => refresh()}
					size="sm"
					title="Refrescar"
					variant="ghost"
				>
					<RefreshCw className="size-4" />
				</Button>
			</CardHeader>
			<CardContent className="space-y-6">
				{loading ? (
					<div className="space-y-2">
						<Skeleton className="h-10 w-full" />
						<Skeleton className="h-10 w-full" />
					</div>
				) : (
					<RankingBoard
						onPenalize={(teamId, teamCode) =>
							setPenaltyTarget({ teamId, teamCode })
						}
						ranking={ranking}
					/>
				)}

				{stageProgress && <StageProgressBoard progress={stageProgress} />}

				<div>
					<h4 className="mb-2 flex items-center gap-2 font-mono text-muted-foreground text-xs uppercase tracking-wide">
						<History className="size-3.5" />
						Historial de eventos
					</h4>
					{events.length === 0 ? (
						<p className="font-mono text-muted-foreground text-xs">
							Sin eventos registrados.
						</p>
					) : (
						<ul className="max-h-56 space-y-1 overflow-y-auto font-mono text-xs">
							{events.map((event) => (
								<li
									className="flex gap-2 border-border/40 border-b py-1"
									key={event.id}
								>
									<span className="shrink-0 text-accent-foreground/70">
										[{event.type}]
									</span>
									<span className="text-muted-foreground">
										{event.description}
									</span>
								</li>
							))}
						</ul>
					)}
				</div>
			</CardContent>

			{penaltyTarget && (
				<PenaltyDialog
					onClose={() => setPenaltyTarget(null)}
					onSubmit={handlePenalty}
					submitting={applyingPenalty}
					teamCode={penaltyTarget.teamCode}
				/>
			)}
		</Card>
	);
}
