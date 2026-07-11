"use client";

import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardFooter,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { Input } from "@umbral/ui/components/input";
import { Label } from "@umbral/ui/components/label";
import { useState } from "react";

interface PenaltyDialogProps {
	teamCode: string;
	submitting: boolean;
	onSubmit: (points: number, reason: string) => void;
	onClose: () => void;
}

export function PenaltyDialog({
	teamCode,
	submitting,
	onSubmit,
	onClose,
}: PenaltyDialogProps) {
	const [points, setPoints] = useState("10");
	const [reason, setReason] = useState("");

	const pointsNum = Number.parseInt(points, 10);
	const reasonProvided = reason.trim().length > 0;
	const canSubmit =
		!submitting &&
		reasonProvided &&
		Number.isFinite(pointsNum) &&
		pointsNum > 0;

	return (
		<div className="fixed inset-0 z-50 flex items-center justify-center bg-black/70 p-4">
			<Card className="w-full max-w-md">
				<CardHeader>
					<CardTitle>Penalizar a {teamCode}</CardTitle>
				</CardHeader>
				<CardContent className="space-y-4">
					<div className="space-y-1.5">
						<Label htmlFor="penalty-points">Puntos a restar</Label>
						<Input
							id="penalty-points"
							min={1}
							onChange={(e) => setPoints(e.target.value)}
							type="number"
							value={points}
						/>
					</div>
					<div className="space-y-1.5">
						<Label htmlFor="penalty-reason">Motivo (obligatorio)</Label>
						<Input
							id="penalty-reason"
							onChange={(e) => setReason(e.target.value)}
							placeholder="Ej: uso de pista externa"
							value={reason}
						/>
						{!reasonProvided && (
							<p className="text-rose-400 text-xs">
								El motivo de la penalización es obligatorio.
							</p>
						)}
					</div>
				</CardContent>
				<CardFooter className="justify-end gap-2">
					<Button disabled={submitting} onClick={onClose} variant="ghost">
						Cancelar
					</Button>
					<Button
						disabled={!canSubmit}
						onClick={() => onSubmit(pointsNum, reason.trim())}
					>
						{submitting ? "Aplicando..." : "Aplicar penalización"}
					</Button>
				</CardFooter>
			</Card>
		</div>
	);
}
