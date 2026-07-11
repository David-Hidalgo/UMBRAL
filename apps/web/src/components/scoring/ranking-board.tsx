"use client";

import { Button } from "@umbral/ui/components/button";
import { Minus, Trophy } from "lucide-react";

import type { RankingEntry } from "@/lib/api/scoring";

interface RankingBoardProps {
	ranking: RankingEntry[];
	onPenalize: (teamId: string, teamCode: string) => void;
}

const RANK_ACCENT: Record<number, string> = {
	1: "bg-amber-500/15 text-amber-400 ring-amber-500/30",
	2: "bg-zinc-400/15 text-zinc-300 ring-zinc-400/30",
	3: "bg-orange-600/15 text-orange-400 ring-orange-600/30",
};

export function RankingBoard({ ranking, onPenalize }: RankingBoardProps) {
	if (ranking.length === 0) {
		return (
			<p className="py-6 text-center font-mono text-muted-foreground text-xs uppercase tracking-wide">
				Sin equipos registrados en el marcador.
			</p>
		);
	}

	return (
		<ul className="space-y-2">
			{ranking.map((entry) => (
				<li
					className="flex items-center justify-between rounded-md border border-border/60 bg-background/40 px-3 py-2"
					key={entry.teamId}
				>
					<div className="flex items-center gap-3">
						<span
							className={`flex size-7 items-center justify-center rounded-full font-mono text-xs ring-1 ${
								RANK_ACCENT[entry.rank] ??
								"bg-muted/40 text-muted-foreground ring-border"
							}`}
						>
							{entry.rank}
						</span>
						<span className="font-medium text-sm">{entry.teamCode}</span>
					</div>

					<div className="flex items-center gap-3">
						<span className="flex items-center gap-1 font-mono text-sm tabular-nums">
							<Trophy className="size-3.5 text-amber-400" />
							{entry.totalPoints} pts
						</span>
						<Button
							onClick={() => onPenalize(entry.teamId, entry.teamCode)}
							size="sm"
							variant="outline"
						>
							<Minus className="size-3.5" />
							Penalizar
						</Button>
					</div>
				</li>
			))}
		</ul>
	);
}
