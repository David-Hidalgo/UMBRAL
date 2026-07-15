"use client";

import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardFooter,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { useState } from "react";

import type { ClueDetail } from "@/lib/api/missions";

interface GrantHintDialogProps {
	teamName: string;
	retoTitle: string;
	clues: ClueDetail[];
	// Ids de pistas ya liberadas a este equipo (para deshabilitarlas — RB-04).
	releasedClueIds: string[];
	submitting: boolean;
	onSubmit: (clueId: string) => void;
	onClose: () => void;
}

export function GrantHintDialog({
	teamName,
	retoTitle,
	clues,
	releasedClueIds,
	submitting,
	onSubmit,
	onClose,
}: GrantHintDialogProps) {
	const orderedClues = [...clues].sort((a, b) => a.order - b.order);
	const [selectedClueId, setSelectedClueId] = useState<string | null>(() => {
		const firstAvailable = orderedClues.find(
			(c) => !releasedClueIds.includes(c.id),
		);
		return firstAvailable?.id ?? null;
	});

	const hasClues = orderedClues.length > 0;
	const canSubmit = !submitting && selectedClueId !== null;

	return (
		<div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
			<Card className="w-full max-w-md">
				<CardHeader>
					<CardTitle>Otorgar pista a {teamName}</CardTitle>
					<p className="font-mono text-muted-foreground text-xs uppercase tracking-wide">
						{retoTitle}
					</p>
				</CardHeader>
				<CardContent className="space-y-3">
					{hasClues ? (
						<ul className="space-y-2">
							{orderedClues.map((clue) => {
								const alreadyReleased = releasedClueIds.includes(clue.id);
								return (
									<li key={clue.id}>
										<label
											className={`flex cursor-pointer items-start gap-2 rounded-md border px-3 py-2 text-sm ${
												alreadyReleased
													? "cursor-not-allowed border-border/40 opacity-50"
													: "border-border/60 hover:border-accent"
											}`}
										>
											<input
												checked={selectedClueId === clue.id}
												className="mt-1"
												disabled={alreadyReleased || submitting}
												name="clue"
												onChange={() => setSelectedClueId(clue.id)}
												type="radio"
												value={clue.id}
											/>
											<span className="flex flex-col">
												<span className="font-mono text-muted-foreground text-xs">
													Pista {clue.order}
													{alreadyReleased && " · ya liberada"}
												</span>
												<span>{clue.text}</span>
											</span>
										</label>
									</li>
								);
							})}
						</ul>
					) : (
						<p className="rounded-md border border-amber-500/30 bg-amber-500/10 px-3 py-2 text-amber-300 text-sm">
							Este reto no tiene pistas configuradas.
						</p>
					)}
				</CardContent>
				<CardFooter className="justify-end gap-2">
					<Button disabled={submitting} onClick={onClose} variant="ghost">
						Cancelar
					</Button>
					<Button
						disabled={!canSubmit}
						onClick={() => selectedClueId && onSubmit(selectedClueId)}
					>
						{submitting ? "Otorgando..." : "Otorgar pista"}
					</Button>
				</CardFooter>
			</Card>
		</div>
	);
}
