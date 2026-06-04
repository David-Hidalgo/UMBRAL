"use client";

import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import {
	AlertTriangle,
	ArrowLeft,
	Check,
	Compass,
	HelpCircle,
	X,
} from "lucide-react";
import type { Route } from "next";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/components/auth-provider";
import { MissionDetailSkeleton } from "@/components/missions/mission-detail-skeleton";
import { RoleGuard } from "@/components/role-guard";
import {
	GetMissionError,
	getMissionById,
	type MissionDetail,
	type SubStageDetail,
} from "@/lib/api/missions";

// ─── Types ───────────────────────────────────────────────────────────────────

type FetchState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "success"; data: MissionDetail };

// ─── Helpers ─────────────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: MissionDetail["status"] }) {
	const isActive = status === "Active";
	return (
		<span
			className={`inline-flex shrink-0 items-center rounded-full px-2.5 py-0.5 font-semibold text-[10px] ring-1 ${
				isActive
					? "bg-emerald-500/15 text-emerald-400 ring-emerald-500/25"
					: "bg-amber-500/15 text-amber-400 ring-amber-500/25"
			}`}
		>
			{isActive ? "Activa" : "Inactiva"}
		</span>
	);
}

function isTriviaSubStage(sub: SubStageDetail): boolean {
	return sub.question !== null;
}

function isTreasureHuntSubStage(sub: SubStageDetail): boolean {
	return sub.objective !== null;
}

function TriviaSubStageCard({ sub }: { sub: SubStageDetail }) {
	const question = sub.question;
	if (!question) return null;

	return (
		<Card className="border-foreground/10 bg-card">
			<CardHeader className="pb-2">
				<div className="flex items-center justify-between gap-2">
					<CardTitle className="flex items-center gap-2 font-medium text-sm">
						<HelpCircle className="size-4 text-primary" />
						{sub.title}
					</CardTitle>
					<span className="shrink-0 font-semibold text-muted-foreground text-xs">
						{sub.points} pts
					</span>
				</div>
			</CardHeader>
			<CardContent className="space-y-3 pt-0">
				<p className="text-foreground text-sm">&ldquo;{question.text}&rdquo;</p>
				<div className="flex flex-wrap gap-2">
					{question.answers.map((answer) => (
						<span
							key={answer.id}
							className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 font-medium text-xs ring-1 ${
								answer.isCorrect
									? "bg-emerald-500/15 text-emerald-400 ring-emerald-500/25"
									: "bg-foreground/8 text-muted-foreground ring-foreground/15"
							}`}
						>
							{answer.isCorrect ? (
								<Check className="size-3 shrink-0" />
							) : (
								<X className="size-3 shrink-0 opacity-60" />
							)}
							{answer.text}
						</span>
					))}
				</div>
			</CardContent>
		</Card>
	);
}

function TreasureHuntSubStageCard({ sub }: { sub: SubStageDetail }) {
	const clues = [...(sub.clues ?? [])].sort((a, b) => a.order - b.order);

	return (
		<Card className="border-foreground/10 bg-card">
			<CardHeader className="pb-2">
				<div className="flex items-center justify-between gap-2">
					<CardTitle className="flex items-center gap-2 font-medium text-sm">
						<Compass className="size-4 text-primary" />
						{sub.title}
					</CardTitle>
					<span className="shrink-0 font-semibold text-muted-foreground text-xs">
						{sub.points} pts
					</span>
				</div>
			</CardHeader>
			<CardContent className="space-y-3 pt-0 text-sm">
				{sub.objective && (
					<p>
						<span className="font-medium text-muted-foreground">
							Objetivo:{" "}
						</span>
						<span className="text-foreground">
							&ldquo;{sub.objective}&rdquo;
						</span>
					</p>
				)}
				{sub.secretCode && (
					<div>
						<span className="font-medium text-muted-foreground text-xs uppercase tracking-wide">
							Código
						</span>
						<code className="mt-1 block truncate rounded bg-foreground/6 px-2 py-1 font-mono text-[11px] text-foreground">
							{sub.secretCode}
						</code>
					</div>
				)}
				{clues.length > 0 && (
					<div>
						<span className="font-medium text-muted-foreground text-xs uppercase tracking-wide">
							Pistas
						</span>
						<ol className="mt-1.5 list-decimal space-y-1 pl-5 text-foreground">
							{clues.map((clue) => (
								<li key={clue.id}>{clue.text}</li>
							))}
						</ol>
					</div>
				)}
			</CardContent>
		</Card>
	);
}

function SubStageCard({ sub }: { sub: SubStageDetail }) {
	if (isTriviaSubStage(sub)) return <TriviaSubStageCard sub={sub} />;
	if (isTreasureHuntSubStage(sub))
		return <TreasureHuntSubStageCard sub={sub} />;
	return null;
}

// ─── Main panel ──────────────────────────────────────────────────────────────

function MissionDetailPanelInner() {
	const params = useParams<{ id: string }>();
	const router = useRouter();
	const { getAccessToken } = useAuth();
	const missionId = params.id;

	const [state, setState] = useState<FetchState>({ status: "loading" });

	const fetchMission = useCallback(async () => {
		if (!missionId) {
			setState({
				status: "error",
				message: "Identificador de misión inválido.",
			});
			return;
		}

		setState({ status: "loading" });
		try {
			const token = await getAccessToken(60);
			if (!token) {
				setState({ status: "error", message: "No hay sesión activa." });
				return;
			}

			const data = await getMissionById(missionId, token);
			setState({ status: "success", data });
		} catch (err) {
			const message =
				err instanceof GetMissionError
					? err.message
					: err instanceof Error
						? err.message
						: "No se pudo cargar el detalle de la misión.";
			setState({ status: "error", message });
			toast.error(message);
		}
	}, [getAccessToken, missionId]);

	useEffect(() => {
		void fetchMission();
	}, [fetchMission]);

	const stages =
		state.status === "success"
			? [...state.data.stages]
					.sort((a, b) => a.order - b.order)
					.map((stage) => ({
						...stage,
						subStages: [...stage.subStages].sort((a, b) => a.order - b.order),
					}))
			: [];

	return (
		<div className="mx-auto max-w-5xl space-y-6">
			<div>
				<button
					type="button"
					onClick={() => router.back()}
					className="inline-flex items-center gap-1.5 text-muted-foreground text-sm transition-colors hover:text-foreground"
				>
					<ArrowLeft className="size-3.5" />
					Volver a misiones
				</button>
			</div>

			{state.status === "loading" && <MissionDetailSkeleton />}

			{state.status === "error" && (
				<div className="flex flex-col items-center gap-3 py-14 text-center">
					<div className="flex size-12 items-center justify-center rounded-xl bg-destructive/10">
						<AlertTriangle className="size-5 text-destructive" />
					</div>
					<div>
						<p className="font-medium text-foreground text-sm">
							Error al cargar la misión
						</p>
						<p className="mt-0.5 text-muted-foreground text-xs">
							{state.message}
						</p>
					</div>
					<div className="flex gap-2">
						<Button
							size="sm"
							variant="outline"
							onClick={() => void fetchMission()}
						>
							Reintentar
						</Button>
						<Link href={"/dashboard/misiones" as Route}>
							<Button size="sm" variant="ghost">
								Volver a misiones
							</Button>
						</Link>
					</div>
				</div>
			)}

			{state.status === "success" && (
				<div className="space-y-8">
					<div className="space-y-2">
						<div className="flex flex-wrap items-start justify-between gap-3">
							<h1 className="font-semibold text-2xl text-foreground tracking-tight">
								{state.data.name}
							</h1>
							<StatusBadge status={state.data.status} />
						</div>
						<p className="text-muted-foreground text-sm">
							{state.data.description || "Sin descripción."}
						</p>
					</div>

					{stages.length === 0 ? (
						<p className="py-8 text-center text-muted-foreground text-sm">
							Esta misión no tiene etapas definidas.
						</p>
					) : (
						stages.map((stage, stageIndex) => (
							<section key={stage.id} className="space-y-4">
								<div className="border-foreground/8 border-b pb-2">
									<h2 className="font-semibold text-foreground text-sm uppercase tracking-wide">
										Etapa {stageIndex + 1} — {stage.name}
										<span className="ml-2 font-normal text-muted-foreground normal-case">
											(orden: {stage.order})
										</span>
									</h2>
									{stage.description && (
										<p className="mt-1 text-muted-foreground text-xs">
											{stage.description}
										</p>
									)}
								</div>

								{stage.subStages.length === 0 ? (
									<p className="text-muted-foreground text-xs">
										Sin subetapas en esta etapa.
									</p>
								) : (
									<div className="space-y-3">
										{stage.subStages.map((sub) => (
											<SubStageCard key={sub.id} sub={sub} />
										))}
									</div>
								)}
							</section>
						))
					)}
				</div>
			)}
		</div>
	);
}

export default function MissionDetailPage() {
	return (
		<RoleGuard requiredRoles={["admin", "operator"]}>
			<MissionDetailPanelInner />
		</RoleGuard>
	);
}
