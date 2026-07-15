"use client";

import { Button } from "@umbral/ui/components/button";
import { Lightbulb } from "lucide-react";

import type { HintRequest } from "@/lib/api/hints";

interface HintRequestsBoardProps {
	requests: HintRequest[];
	getRetoTitle: (subStageId: string) => string;
	onGrant: (request: HintRequest) => void;
}

export function HintRequestsBoard({
	requests,
	getRetoTitle,
	onGrant,
}: HintRequestsBoardProps) {
	if (requests.length === 0) {
		return (
			<p className="py-6 text-center font-mono text-muted-foreground text-xs uppercase tracking-wide">
				Sin solicitudes de pista pendientes.
			</p>
		);
	}

	return (
		<ul className="space-y-2">
			{requests.map((request) => (
				<li
					className="flex items-center justify-between rounded-md border border-border/60 bg-background/40 px-3 py-2"
					key={request.id}
				>
					<div className="flex items-center gap-3">
						<Lightbulb className="size-4 text-amber-400" />
						<div className="flex flex-col">
							<span className="font-medium text-sm">{request.teamName}</span>
							<span className="font-mono text-muted-foreground text-xs">
								{getRetoTitle(request.subStageId)}
							</span>
						</div>
					</div>

					<Button onClick={() => onGrant(request)} size="sm" variant="outline">
						<Lightbulb className="size-3.5" />
						Otorgar
					</Button>
				</li>
			))}
		</ul>
	);
}
