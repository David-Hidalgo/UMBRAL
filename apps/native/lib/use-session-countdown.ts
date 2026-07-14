import { useEffect, useState } from "react";

export interface SessionCountdown {
	/** Segundos restantes; null si la sesión no tiene temporizador configurado. */
	remainingSeconds: number | null;
	/** true cuando el temporizador llegó a 0 (solo si hay temporizador). */
	isExpired: boolean;
	isPaused: boolean;
}

/**
 * Cuenta regresiva de la sesión calculada localmente en el cliente (RF-06). El servidor solo
 * entrega el instante de expiración (endsAt); aquí se tickea cada segundo. Al pausar se usa el
 * tiempo congelado. Única fuente de verdad para mostrar el reloj y para bloquear la respuesta
 * cuando el tiempo se agota.
 */
export function useSessionCountdown(
	status: string,
	endsAt: string | null,
	remainingSecondsOnPause: number | null,
): SessionCountdown {
	const isPaused = status === "Pausada";
	const [nowMs, setNowMs] = useState(() => Date.now());

	useEffect(() => {
		// Solo tickea cuando el reloj corre (Activa con deadline).
		if (isPaused || !endsAt) {
			return;
		}
		const id = setInterval(() => setNowMs(Date.now()), 1000);
		return () => clearInterval(id);
	}, [isPaused, endsAt]);

	if (isPaused) {
		if (remainingSecondsOnPause == null) {
			return { remainingSeconds: null, isExpired: false, isPaused };
		}
		const remaining = remainingSecondsOnPause;
		return { remainingSeconds: remaining, isExpired: remaining <= 0, isPaused };
	}
	if (endsAt) {
		const remaining = (new Date(endsAt).getTime() - nowMs) / 1000;
		return { remainingSeconds: remaining, isExpired: remaining <= 0, isPaused };
	}
	return { remainingSeconds: null, isExpired: false, isPaused };
}
