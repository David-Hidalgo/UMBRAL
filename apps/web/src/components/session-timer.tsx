"use client";

import { useEffect, useState } from "react";

interface SessionTimerProps {
	status: string;
	endsAt: string | null;
	remainingSecondsOnPause: number | null;
}

function formatMMSS(totalSeconds: number): string {
	const s = Math.max(0, Math.floor(totalSeconds));
	const minutes = Math.floor(s / 60);
	const seconds = s % 60;
	return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

/**
 * Cuenta regresiva de la sesión para el operador (RF-06/RF-13). El servidor solo entrega
 * el instante de expiración (endsAt); el conteo se hace localmente en el cliente. Al pausar
 * muestra el tiempo congelado; al llegar a cero, "Tiempo agotado" (sin acción automática).
 */
export function SessionTimer({
	status,
	endsAt,
	remainingSecondsOnPause,
}: SessionTimerProps) {
	const isPaused = status === "Pausada";
	const [nowMs, setNowMs] = useState(() => Date.now());

	useEffect(() => {
		if (isPaused || !endsAt) {
			return;
		}
		const id = setInterval(() => setNowMs(Date.now()), 1000);
		return () => clearInterval(id);
	}, [isPaused, endsAt]);

	let remainingSeconds: number;
	if (isPaused) {
		if (remainingSecondsOnPause == null) return null;
		remainingSeconds = remainingSecondsOnPause;
	} else if (endsAt) {
		remainingSeconds = (new Date(endsAt).getTime() - nowMs) / 1000;
	} else {
		return null;
	}

	const expired = remainingSeconds <= 0;
	const tone = expired
		? "text-red-400 ring-red-500/30 bg-red-500/10"
		: isPaused
			? "text-amber-400 ring-amber-500/30 bg-amber-500/10"
			: "text-emerald-400 ring-emerald-500/30 bg-emerald-500/10";

	return (
		<div
			className={`inline-flex items-center gap-3 rounded-lg px-4 py-2 ring-1 ${tone}`}
		>
			<span className="font-bold font-mono text-[10px] text-muted-foreground uppercase tracking-widest">
				{isPaused ? "Tiempo (en pausa)" : "Tiempo restante"}
			</span>
			<span className="font-black font-mono text-xl tabular-nums">
				{expired ? "TIEMPO AGOTADO" : formatMMSS(remainingSeconds)}
			</span>
		</div>
	);
}
