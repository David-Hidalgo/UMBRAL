"use client";

import * as signalR from "@microsoft/signalr";
import { env } from "@umbral/env/web";
import {
	Card,
	CardContent,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { Skeleton } from "@umbral/ui/components/skeleton";
import { Lightbulb } from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import {
	getHintRequests,
	getTeamHints,
	grantHint,
	HintApiError,
	type HintRequest,
} from "@/lib/api/hints";
import type { ClueDetail, MissionDetail } from "@/lib/api/missions";
import { GrantHintDialog } from "./grant-hint-dialog";
import { HintRequestsBoard } from "./hint-requests-board";

interface HintsPanelProps {
	sessionId: string;
	missionDetail: MissionDetail | null;
	getAccessToken: (minValidity?: number) => Promise<string | null>;
}

interface GrantTarget {
	request: HintRequest;
	clues: ClueDetail[];
	releasedClueIds: string[];
}

export function HintsPanel({
	sessionId,
	missionDetail,
	getAccessToken,
}: HintsPanelProps) {
	const [requests, setRequests] = useState<HintRequest[]>([]);
	const [loading, setLoading] = useState(true);
	const [grantTarget, setGrantTarget] = useState<GrantTarget | null>(null);
	const [granting, setGranting] = useState(false);
	const hubRef = useRef<signalR.HubConnection | null>(null);

	// Busca una subetapa por id en toda la misión (independiente de la etapa).
	const findSubStage = useCallback(
		(subStageId: string) =>
			missionDetail?.stages
				.flatMap((s) => s.subStages)
				.find((ss) => ss.id === subStageId) ?? null,
		[missionDetail],
	);

	const getRetoTitle = useCallback(
		(subStageId: string) => findSubStage(subStageId)?.title ?? "Reto",
		[findSubStage],
	);

	const refresh = useCallback(async () => {
		try {
			const token = await getAccessToken(60);
			if (!token) return;
			setRequests(await getHintRequests(sessionId, token, "Pendiente"));
		} catch (err) {
			// 404 = la sesión aún no existe/está activa; se muestra vacío en silencio.
			if (err instanceof HintApiError && err.code === "not_found") {
				setRequests([]);
				return;
			}
			const message = err instanceof Error ? err.message : String(err);
			toast.error(`Error al cargar solicitudes de pista: ${message}`);
		}
	}, [sessionId, getAccessToken]);

	// Carga inicial.
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

	// Sondeo de respaldo: una solicitud puede llegar sin que el evento SignalR se
	// entregue (reconexión, segunda conexión al mismo hub, etc.). Se re-sincroniza
	// en silencio cada pocos segundos para que el panel nunca se quede desactualizado.
	useEffect(() => {
		const interval = setInterval(() => {
			void refresh();
		}, 4000);
		return () => clearInterval(interval);
	}, [refresh]);

	// Conexión en tiempo real al hub de sesiones (solicitudes de pista).
	useEffect(() => {
		let active = true;
		const connection = new signalR.HubConnectionBuilder()
			.withUrl(`${env.NEXT_PUBLIC_SERVER_URL}/hubs/sessions`, {
				accessTokenFactory: async () => (await getAccessToken(60)) ?? "",
			})
			.withAutomaticReconnect()
			.build();

		// Ante cualquier cambio (nueva solicitud o resuelta), re-sincroniza por HTTP.
		connection.on("HintRequested", () => {
			if (active) void refresh();
		});
		connection.on("HintRequestResolved", () => {
			if (active) void refresh();
		});
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
				// Si el hub falla, las solicitudes siguen visibles por HTTP (refresh).
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

	const openGrantDialog = async (request: HintRequest) => {
		const subStage = findSubStage(request.subStageId);
		const clues = subStage?.clues ?? [];
		let releasedClueIds: string[] = [];
		try {
			const token = await getAccessToken(60);
			if (token) {
				const released = await getTeamHints(sessionId, request.teamId, token);
				releasedClueIds = released.map((h) => h.clueId);
			}
		} catch {
			// No es crítico: si falla, se muestran todas; el backend igual valida RB-04.
		}
		setGrantTarget({ request, clues, releasedClueIds });
	};

	const handleGrant = async (clueId: string) => {
		if (!grantTarget) return;
		setGranting(true);
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");
			await grantHint(
				sessionId,
				grantTarget.request.teamId,
				{ subStageId: grantTarget.request.subStageId, clueId },
				token,
			);
			toast.success(`Pista otorgada a ${grantTarget.request.teamName}.`);
			setGrantTarget(null);
			await refresh();
		} catch (err) {
			const message = err instanceof Error ? err.message : String(err);
			toast.error(`Error al otorgar pista: ${message}`);
		} finally {
			setGranting(false);
		}
	};

	return (
		<Card>
			<CardHeader>
				<CardTitle className="flex items-center gap-2 font-mono text-sm uppercase tracking-wide">
					<Lightbulb className="size-4 text-amber-400" />
					Solicitudes de pista
				</CardTitle>
			</CardHeader>
			<CardContent>
				{loading ? (
					<div className="space-y-2">
						<Skeleton className="h-12 w-full" />
						<Skeleton className="h-12 w-full" />
					</div>
				) : (
					<HintRequestsBoard
						getRetoTitle={getRetoTitle}
						onGrant={openGrantDialog}
						requests={requests}
					/>
				)}
			</CardContent>

			{grantTarget && (
				<GrantHintDialog
					clues={grantTarget.clues}
					onClose={() => setGrantTarget(null)}
					onSubmit={handleGrant}
					releasedClueIds={grantTarget.releasedClueIds}
					retoTitle={getRetoTitle(grantTarget.request.subStageId)}
					submitting={granting}
					teamName={grantTarget.request.teamName}
				/>
			)}
		</Card>
	);
}
