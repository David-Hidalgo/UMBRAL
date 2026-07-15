import { Ionicons } from "@expo/vector-icons";
import { Text, View } from "react-native";

interface SessionTimerProps {
	remainingSeconds: number | null;
	isExpired: boolean;
	isPaused: boolean;
}

function formatMMSS(totalSeconds: number): string {
	const s = Math.max(0, Math.floor(totalSeconds));
	const minutes = Math.floor(s / 60);
	const seconds = s % 60;
	return `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;
}

/**
 * Muestra la cuenta regresiva de la sesión (RF-06). El conteo lo provee
 * useSessionCountdown; este componente solo pinta. Al llegar a 0 muestra
 * "Tiempo agotado" (sin acción automática).
 */
export function SessionTimer({
	remainingSeconds,
	isExpired,
	isPaused,
}: SessionTimerProps) {
	if (remainingSeconds === null) {
		return null; // La sesión no tiene temporizador configurado.
	}

	const containerClass = isExpired
		? "border-danger/40 bg-danger/10"
		: isPaused
			? "border-border/30 bg-muted/10"
			: "border-accent/30 bg-accent/10";
	const valueClass = isExpired
		? "text-danger"
		: isPaused
			? "text-muted"
			: "text-accent";

	return (
		<View
			className={`mb-4 flex-row items-center justify-between rounded-lg border px-4 py-3 ${containerClass}`}
		>
			<View className="flex-row items-center gap-2">
				<Ionicons
					name={isPaused ? "pause-outline" : "time-outline"}
					size={16}
					color={isExpired ? "#ef4444" : isPaused ? "#9ca3af" : "#fbbf24"}
				/>
				<Text className="font-bold font-mono text-foreground text-xs uppercase tracking-widest">
					{isPaused ? "Tiempo (en pausa)" : "Tiempo restante"}
				</Text>
			</View>
			<Text
				className={`font-black font-mono text-lg tabular-nums ${valueClass}`}
			>
				{isExpired ? "TIEMPO AGOTADO" : formatMMSS(remainingSeconds)}
			</Text>
		</View>
	);
}
