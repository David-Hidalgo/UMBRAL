import { Ionicons } from "@expo/vector-icons";
import { useState } from "react";
import { Pressable, Text, TextInput, View } from "react-native";

import type { PlayerSubStage, SubmitEvidenceResult } from "@/lib/api/scoring";

interface ChallengeCardProps {
	subStage: PlayerSubStage;
	submitting: boolean;
	result?: SubmitEvidenceResult;
	onSubmit: (payload: string) => void;
	/** true cuando el temporizador de la sesión llegó a 0: se oculta el botón de enviar (RF-06). */
	isTimeUp: boolean;
}

export function ChallengeCard({
	subStage,
	submitting,
	result,
	onSubmit,
	isTimeUp,
}: ChallengeCardProps) {
	const [selectedAnswerId, setSelectedAnswerId] = useState<string | null>(null);
	const [code, setCode] = useState("");

	const isTrivia = subStage.question !== null;
	// Trivia: un solo intento (cualquier resultado la bloquea, no se puede corregir).
	// Búsqueda del tesoro: se puede reintentar hasta acertar.
	const locked = isTrivia ? result != null : result?.status === "Correct";
	// Al agotarse el tiempo se deshabilita toda interacción con el reto.
	const disabled = submitting || locked || isTimeUp;
	const canSubmit =
		!disabled &&
		(isTrivia ? selectedAnswerId !== null : code.trim().length > 0);

	const handleSubmit = () => {
		if (isTrivia && selectedAnswerId) {
			onSubmit(selectedAnswerId);
		} else if (!isTrivia) {
			onSubmit(code.trim());
		}
	};

	return (
		<View className="mb-3 rounded-xl border border-border/15 bg-card/20 p-4">
			<View className="mb-3 flex-row items-center justify-between">
				<Text className="font-bold font-mono text-foreground text-xs uppercase tracking-wide">
					{subStage.title}
				</Text>
				<View className="flex-row items-center gap-1">
					<Ionicons name="ribbon-outline" size={12} color="#fbbf24" />
					<Text className="font-mono text-[10px] text-muted uppercase">
						{subStage.points} pts
					</Text>
				</View>
			</View>

			{isTrivia && subStage.question ? (
				<View className="gap-2">
					<Text className="mb-1 font-mono text-foreground text-sm">
						{subStage.question.text}
					</Text>
					{subStage.question.answers.map((answer) => {
						const isSelected = selectedAnswerId === answer.id;
						return (
							<Pressable
								className={`flex-row items-center gap-2 rounded-lg border p-3 ${
									isSelected
										? "border-accent/40 bg-accent/10"
										: "border-border/10 bg-card/15"
								}`}
								disabled={disabled}
								key={answer.id}
								onPress={() => setSelectedAnswerId(answer.id)}
							>
								<Ionicons
									name={isSelected ? "radio-button-on" : "radio-button-off"}
									size={16}
									color={isSelected ? "#22c55e" : "#71717a"}
								/>
								<Text className="flex-1 font-mono text-foreground text-xs">
									{answer.text}
								</Text>
							</Pressable>
						);
					})}
				</View>
			) : (
				<View className="gap-2">
					{subStage.objective ? (
						<Text className="mb-1 font-mono text-muted text-xs">
							{subStage.objective}
						</Text>
					) : null}
					<TextInput
						autoCapitalize="characters"
						className="rounded-lg border border-border/20 bg-muted/10 px-3 py-2 font-mono text-foreground text-sm"
						editable={!disabled}
						onChangeText={setCode}
						placeholder="Introduce el código secreto..."
						placeholderTextColor="#71717a"
						value={code}
					/>
				</View>
			)}

			{result ? (
				<View
					className={`mt-3 flex-row items-center gap-2 rounded-lg p-2 ${
						result.status === "Correct" ? "bg-success/15" : "bg-danger/15"
					}`}
				>
					<Ionicons
						name={
							result.status === "Correct" ? "checkmark-circle" : "close-circle"
						}
						size={16}
						color={result.status === "Correct" ? "#22c55e" : "#ef4444"}
					/>
					<Text className="font-mono text-foreground text-xs">
						{result.status === "Correct"
							? `¡Correcto! +${result.awardedPoints} pts`
							: "Respuesta incorrecta."}
					</Text>
				</View>
			) : null}

			{!locked && !isTimeUp && (
				<Pressable
					className={`mt-3 items-center rounded-lg border py-3 ${
						canSubmit
							? "border-accent/40 bg-accent/15"
							: "border-border/10 bg-muted/10"
					}`}
					disabled={!canSubmit}
					onPress={handleSubmit}
				>
					<Text
						className={`font-bold font-mono text-xs uppercase tracking-widest ${
							canSubmit ? "text-accent" : "text-muted"
						}`}
					>
						{submitting ? "Enviando..." : "Enviar respuesta"}
					</Text>
				</Pressable>
			)}

			{!locked && isTimeUp && (
				<View className="mt-3 items-center rounded-lg border border-danger/30 bg-danger/10 py-3">
					<Text className="font-bold font-mono text-danger text-xs uppercase tracking-widest">
						Tiempo agotado
					</Text>
					<Text className="mt-1 font-mono text-[10px] text-muted uppercase">
						Ya no puedes responder este reto
					</Text>
				</View>
			)}
		</View>
	);
}
