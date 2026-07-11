"use client";

import { CheckCircle2, ListChecks } from "lucide-react";

import type { StageProgress } from "@/lib/api/scoring";

interface StageProgressBoardProps {
	progress: StageProgress;
}

export function StageProgressBoard({ progress }: StageProgressBoardProps) {
	if (progress.subStageCount === 0) {
		return null;
	}

	return (
		<div>
			<h4 className="mb-2 flex items-center gap-2 font-mono text-muted-foreground text-xs uppercase tracking-wide">
				<ListChecks className="size-3.5" />
				Progreso de la etapa ({progress.subStageCount} retos)
			</h4>

			<ul className="space-y-1.5">
				{progress.teams.map((team) => {
					const done = team.completed >= progress.subStageCount;
					return (
						<li
							className="flex items-center justify-between rounded-md border border-border/50 bg-background/40 px-3 py-1.5 font-mono text-xs"
							key={team.teamId}
						>
							<span className="font-medium">{team.teamCode}</span>
							<span
								className={`flex items-center gap-1 tabular-nums ${
									done ? "text-emerald-400" : "text-muted-foreground"
								}`}
							>
								{done && <CheckCircle2 className="size-3.5" />}
								{team.completed}/{progress.subStageCount}
							</span>
						</li>
					);
				})}
			</ul>

			{progress.allComplete && (
				<div className="mt-2 flex items-center gap-2 rounded-md border border-emerald-500/30 bg-emerald-500/10 px-3 py-2 font-mono text-emerald-400 text-xs">
					<CheckCircle2 className="size-4 shrink-0" />
					Todos los equipos completaron la etapa — puedes avanzar.
				</div>
			)}
		</div>
	);
}
