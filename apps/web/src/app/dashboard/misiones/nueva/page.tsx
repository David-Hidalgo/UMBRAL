"use client";

import { env } from "@umbral/env/web";
import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { Checkbox } from "@umbral/ui/components/checkbox";
import { Input } from "@umbral/ui/components/input";
import { Label } from "@umbral/ui/components/label";
import {
	ArrowLeft,
	Check,
	Compass,
	HelpCircle,
	Loader2,
	Plus,
	PlusCircle,
	Save,
	ScrollText,
	Trash,
} from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";
import { useAuth } from "@/components/auth-provider";
import { RoleGuard } from "@/components/role-guard";

// ─── Types and Interfaces ───────────────────────────────────────────────────

interface ClientAnswer {
	tempId: string;
	text: string;
	isCorrect: boolean;
}

interface ClientQuestion {
	tempId: string;
	text: string;
	answers: ClientAnswer[];
}

interface ClientClue {
	tempId: string;
	text: string;
	order: number;
}

interface ClientSubStage {
	tempId: string;
	title: string;
	order: number;
	type: "Trivia" | "TreasureHunt";
	points: number;
	// Trivia: exactly 1 question
	question: ClientQuestion;
	// TreasureHunt
	objective: string;
	secretCode: string;
	clues: ClientClue[];
}

interface ClientStage {
	tempId: string;
	name: string;
	description: string;
	order: number;
	subStages: ClientSubStage[];
}

interface MissionForm {
	name: string;
	description: string;
	stages: ClientStage[];
}

// ─── Payload Types (API shape) ────────────────────────────────────────────────

type SubStagePayload =
	| {
			title: string;
			order: number;
			type: "Trivia";
			points: number;
			question: {
				text: string;
				answers: { text: string; isCorrect: boolean }[];
			};
	  }
	| {
			title: string;
			order: number;
			type: "TreasureHunt";
			points: number;
			objective: string;
			secretCode: string;
			clues: { text: string; order: number }[];
	  };

// ─── Helpers ─────────────────────────────────────────────────────────────────

function makeDefaultQuestion(): ClientQuestion {
	return {
		tempId: crypto.randomUUID(),
		text: "Nueva pregunta",
		answers: [
			{ tempId: crypto.randomUUID(), text: "Opción 1", isCorrect: true },
			{ tempId: crypto.randomUUID(), text: "Opción 2", isCorrect: false },
		],
	};
}

// ─── Main Component ──────────────────────────────────────────────────────────

function CreateMissionForm() {
	const router = useRouter();
	const { getAccessToken } = useAuth();
	const [loading, setLoading] = useState(false);

	const [mission, setMission] = useState<MissionForm>({
		name: "",
		description: "",
		stages: [],
	});

	// Helper to calculate total points across all substages
	const calculateTotalPoints = () => {
		let total = 0;
		for (const stage of mission.stages) {
			for (const sub of stage.subStages) {
				total += Number(sub.points) || 0;
			}
		}
		return total;
	};

	// ─── Handlers: Basic Fields ───────────────────────────────────────────────

	const handleMissionChange = (
		e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement>,
	) => {
		setMission((prev) => ({ ...prev, [e.target.name]: e.target.value }));
	};

	// ─── Handlers: Stages ──────────────────────────────────────────────────────

	const addStage = () => {
		const newStage: ClientStage = {
			tempId: crypto.randomUUID(),
			name: `Etapa ${mission.stages.length + 1}`,
			description: "",
			order: mission.stages.length,
			subStages: [],
		};
		setMission((prev) => ({ ...prev, stages: [...prev.stages, newStage] }));
	};

	const removeStage = (stageId: string) => {
		setMission((prev) => {
			const filtered = prev.stages.filter((s) => s.tempId !== stageId);
			const updated = filtered.map((s, index) => ({ ...s, order: index }));
			return { ...prev, stages: updated };
		});
	};

	const updateStageField = (
		stageId: string,
		field: keyof ClientStage,
		value: ClientStage[keyof ClientStage],
	) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) =>
				s.tempId === stageId ? { ...s, [field]: value } : s,
			),
		}));
	};

	// ─── Handlers: SubStages ───────────────────────────────────────────────────

	const addSubStage = (stageId: string, type: "Trivia" | "TreasureHunt") => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((stage) => {
				if (stage.tempId !== stageId) return stage;

				const newSub: ClientSubStage = {
					tempId: crypto.randomUUID(),
					title: `${type === "Trivia" ? "Trivia" : "Búsqueda"} ${stage.subStages.length + 1}`,
					order: stage.subStages.length,
					type,
					points: 10,
					question: makeDefaultQuestion(),
					objective: "",
					secretCode: "",
					clues:
						type === "TreasureHunt"
							? [
									{
										tempId: crypto.randomUUID(),
										text: "Pista 1",
										order: 0,
									},
								]
							: [],
				};

				return { ...stage, subStages: [...stage.subStages, newSub] };
			}),
		}));
	};

	const removeSubStage = (stageId: string, subId: string) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				const filtered = s.subStages.filter((sub) => sub.tempId !== subId);
				const updated = filtered.map((sub, index) => ({
					...sub,
					order: index,
				}));
				return { ...s, subStages: updated };
			}),
		}));
	};

	const updateSubStageField = (
		stageId: string,
		subId: string,
		field: keyof ClientSubStage,
		value: ClientSubStage[keyof ClientSubStage],
	) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				return {
					...s,
					subStages: s.subStages.map((sub) =>
						sub.tempId === subId ? { ...sub, [field]: value } : sub,
					),
				};
			}),
		}));
	};

	// ─── Handlers: Trivia Question (single per substage) ──────────────────────

	const updateQuestionField = (
		stageId: string,
		subId: string,
		field: keyof ClientQuestion,
		value: ClientQuestion[keyof ClientQuestion],
	) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				return {
					...s,
					subStages: s.subStages.map((sub) => {
						if (sub.tempId !== subId) return sub;
						return { ...sub, question: { ...sub.question, [field]: value } };
					}),
				};
			}),
		}));
	};

	// ─── Handlers: Trivia Answers ─────────────────────────────────────────────

	const addAnswer = (stageId: string, subId: string) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				return {
					...s,
					subStages: s.subStages.map((sub) => {
						if (sub.tempId !== subId) return sub;
						const newA: ClientAnswer = {
							tempId: crypto.randomUUID(),
							text: "Nueva opción",
							isCorrect: false,
						};
						return {
							...sub,
							question: {
								...sub.question,
								answers: [...sub.question.answers, newA],
							},
						};
					}),
				};
			}),
		}));
	};

	const removeAnswer = (stageId: string, subId: string, aId: string) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				return {
					...s,
					subStages: s.subStages.map((sub) => {
						if (sub.tempId !== subId) return sub;
						return {
							...sub,
							question: {
								...sub.question,
								answers: sub.question.answers.filter((a) => a.tempId !== aId),
							},
						};
					}),
				};
			}),
		}));
	};

	const updateAnswerField = (
		stageId: string,
		subId: string,
		aId: string,
		field: keyof ClientAnswer,
		value: ClientAnswer[keyof ClientAnswer],
	) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				return {
					...s,
					subStages: s.subStages.map((sub) => {
						if (sub.tempId !== subId) return sub;
						return {
							...sub,
							question: {
								...sub.question,
								answers: sub.question.answers.map((a) =>
									a.tempId === aId ? { ...a, [field]: value } : a,
								),
							},
						};
					}),
				};
			}),
		}));
	};

	// ─── Handlers: Treasure Hunt Clues ────────────────────────────────────────

	const addClue = (stageId: string, subId: string) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				return {
					...s,
					subStages: s.subStages.map((sub) => {
						if (sub.tempId !== subId) return sub;
						const newClue: ClientClue = {
							tempId: crypto.randomUUID(),
							text: "Nueva pista",
							order: sub.clues.length,
						};
						return { ...sub, clues: [...sub.clues, newClue] };
					}),
				};
			}),
		}));
	};

	const removeClue = (stageId: string, subId: string, clueId: string) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				return {
					...s,
					subStages: s.subStages.map((sub) => {
						if (sub.tempId !== subId) return sub;
						const filtered = sub.clues.filter((c) => c.tempId !== clueId);
						const reindexed = filtered.map((c, index) => ({
							...c,
							order: index,
						}));
						return { ...sub, clues: reindexed };
					}),
				};
			}),
		}));
	};

	const updateClueField = (
		stageId: string,
		subId: string,
		clueId: string,
		field: keyof ClientClue,
		value: ClientClue[keyof ClientClue],
	) => {
		setMission((prev) => ({
			...prev,
			stages: prev.stages.map((s) => {
				if (s.tempId !== stageId) return s;
				return {
					...s,
					subStages: s.subStages.map((sub) => {
						if (sub.tempId !== subId) return sub;
						return {
							...sub,
							clues: sub.clues.map((c) =>
								c.tempId === clueId ? { ...c, [field]: value } : c,
							),
						};
					}),
				};
			}),
		}));
	};

	// ─── Submission ────────────────────────────────────────────────────────────

	const handleSubmit = async () => {
		if (!mission.name.trim() || !mission.description.trim()) {
			toast.error("Por favor completa el nombre y descripción de la misión.");
			return;
		}

		if (mission.stages.length === 0) {
			toast.error("La misión debe tener al menos una etapa.");
			return;
		}

		for (const stage of mission.stages) {
			if (!stage.name.trim() || !stage.description.trim()) {
				toast.error(`La etapa "${stage.name}" debe tener descripción.`);
				return;
			}

			if (stage.subStages.length === 0) {
				toast.error(
					`La etapa "${stage.name}" debe tener al menos una subetapa.`,
				);
				return;
			}

			for (const sub of stage.subStages) {
				if (!sub.title.trim()) {
					toast.error("Ingresa el título para la subetapa.");
					return;
				}

				if ((Number(sub.points) || 0) < 0) {
					toast.error(`Los puntos de "${sub.title}" no pueden ser negativos.`);
					return;
				}

				if (sub.type === "Trivia") {
					if (!sub.question.text.trim()) {
						toast.error(
							`Escribe el texto de la pregunta en la trivia "${sub.title}".`,
						);
						return;
					}
					if (sub.question.answers.length === 0) {
						toast.error(
							`La pregunta de "${sub.title}" debe tener opciones de respuesta.`,
						);
						return;
					}
					const hasCorrect = sub.question.answers.some((a) => a.isCorrect);
					if (!hasCorrect) {
						toast.error(
							`La pregunta de "${sub.title}" requiere al menos una respuesta correcta marcada.`,
						);
						return;
					}
					for (const a of sub.question.answers) {
						if (!a.text.trim()) {
							toast.error("Las opciones de respuesta no pueden estar vacías.");
							return;
						}
					}
				} else if (sub.type === "TreasureHunt") {
					if (!sub.objective.trim()) {
						toast.error(
							`Especifica el objetivo de la búsqueda "${sub.title}".`,
						);
						return;
					}
					if (!sub.secretCode.trim()) {
						toast.error(
							`Especifica el código secreto de la búsqueda "${sub.title}".`,
						);
						return;
					}
					if (!/^[a-zA-Z0-9-]+$/.test(sub.secretCode.trim())) {
						toast.error(
							`El código secreto de "${sub.title}" debe ser alfanumérico y solo puede contener guiones.`,
						);
						return;
					}
					if (sub.clues.length === 0) {
						toast.error(
							`La búsqueda "${sub.title}" debe tener al menos una pista.`,
						);
						return;
					}
					for (const c of sub.clues) {
						if (!c.text.trim()) {
							toast.error("Las pistas no pueden estar vacías.");
							return;
						}
					}
				}
			}
		}

		setLoading(true);

		try {
			const accessToken = await getAccessToken(60);

			if (!accessToken) {
				toast.error("No hay sesión activa.");
				return;
			}

			// Map client state to exact server format
			const payload = {
				name: mission.name,
				description: mission.description,
				stages: mission.stages.map((s) => ({
					name: s.name,
					description: s.description,
					order: s.order,
					subStages: s.subStages.map((sub): SubStagePayload => {
						if (sub.type === "Trivia") {
							return {
								title: sub.title,
								order: sub.order,
								type: "Trivia",
								points: Number(sub.points) || 0,
								question: {
									text: sub.question.text,
									answers: sub.question.answers.map((a) => ({
										text: a.text,
										isCorrect: a.isCorrect,
									})),
								},
							};
						}
						return {
							title: sub.title,
							order: sub.order,
							type: "TreasureHunt",
							points: Number(sub.points) || 0,
							objective: sub.objective,
							secretCode: sub.secretCode.trim().toUpperCase(),
							clues: sub.clues.map((c) => ({
								text: c.text,
								order: c.order,
							})),
						};
					}),
				})),
			};

			const response = await fetch(
				`${env.NEXT_PUBLIC_SERVER_URL}/api/missions`,
				{
					method: "POST",
					credentials: "omit",
					headers: {
						"Content-Type": "application/json",
						Authorization: `Bearer ${accessToken}`,
					},
					body: JSON.stringify(payload),
				},
			);

			if (!response.ok) {
				const errorJson = await response.json().catch(() => null);
				const errorMsg =
					errorJson?.error ||
					errorJson?.errors?.[0]?.message ||
					`Error del servidor (${response.status})`;
				throw new Error(errorMsg);
			}

			toast.success(
				`Plantilla de Misión "${mission.name}" creada exitosamente en PostgreSQL.`,
			);
			router.push("/dashboard/misiones");
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "No se pudo crear la misión.",
			);
		} finally {
			setLoading(false);
		}
	};

	return (
		<div className="mx-auto max-w-5xl space-y-6">
			{/* Back button */}
			<div>
				<Link
					href="/dashboard/misiones"
					className="inline-flex items-center gap-1.5 text-muted-foreground text-sm transition-colors hover:text-foreground"
				>
					<ArrowLeft className="size-3.5" />
					Volver a Misiones
				</Link>
			</div>

			{/* Page title */}
			<div className="flex items-center justify-between gap-4">
				<div>
					<h1 className="font-semibold text-2xl text-foreground tracking-tight">
						Diseñar Nueva Misión
					</h1>
					<p className="mt-1 text-muted-foreground text-sm">
						Configura la estructura jerárquica de etapas y subetapas (Trivia o
						Búsqueda del Tesoro).
					</p>
				</div>
				<Button
					type="button"
					className="gap-2"
					onClick={handleSubmit}
					disabled={loading}
				>
					{loading ? (
						<>
							<Loader2 className="size-4 animate-spin" />
							Guardando…
						</>
					) : (
						<>
							<Save className="size-4" />
							Guardar Plantilla
						</>
					)}
				</Button>
			</div>

			{/* 2 Column Layout: Form & Stats sidebar */}
			<div className="grid items-start gap-6 md:grid-cols-3">
				{/* Main editor form (2 cols) */}
				<div className="space-y-6 md:col-span-2">
					{/* Basic Mission Card */}
					<Card className="border-foreground/10">
						<CardHeader>
							<CardTitle className="flex items-center gap-2 text-base">
								<ScrollText className="size-4 text-primary" />
								Configuración de Plantilla
							</CardTitle>
							<CardDescription>
								Establece los datos de identificación generales de la misión.
							</CardDescription>
						</CardHeader>
						<CardContent className="space-y-4">
							<div className="space-y-1.5">
								<Label htmlFor="name">Nombre de la Misión</Label>
								<Input
									id="name"
									name="name"
									placeholder="Ej. Desafío Criptográfico Principal"
									value={mission.name}
									onChange={handleMissionChange}
									disabled={loading}
									autoComplete="off"
								/>
							</div>
							<div className="space-y-1.5">
								<Label htmlFor="description">Descripción</Label>
								<textarea
									id="description"
									name="description"
									placeholder="Describe el trasfondo del juego o instrucciones para los participantes..."
									value={mission.description}
									onChange={handleMissionChange}
									disabled={loading}
									className="flex min-h-[80px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
								/>
							</div>
						</CardContent>
					</Card>

					{/* Stages Editor Section */}
					<div className="space-y-4">
						<div className="flex items-center justify-between">
							<h3 className="font-semibold text-foreground text-lg">
								Etapas de la Misión
							</h3>
							<Button
								type="button"
								variant="outline"
								size="sm"
								onClick={addStage}
								className="gap-1.5"
								disabled={loading}
							>
								<Plus className="size-3.5" /> Agregar Etapa
							</Button>
						</div>

						{mission.stages.length === 0 ? (
							<div className="rounded-xl border border-foreground/20 border-dashed p-8 text-center text-muted-foreground text-sm">
								No hay etapas agregadas. Haz clic en "Agregar Etapa" para
								comenzar.
							</div>
						) : (
							mission.stages.map((stage, sIdx) => (
								<Card
									key={stage.tempId}
									className="relative border-foreground/15 shadow-sm"
								>
									<CardHeader className="flex flex-row items-center justify-between pb-3">
										<div className="flex-1 space-y-1 pr-6">
											<div className="flex items-center gap-2">
												<span className="rounded-full bg-primary/10 px-2 py-0.5 font-bold text-primary text-xs">
													Etapa {sIdx + 1}
												</span>
												<Input
													value={stage.name}
													onChange={(e) =>
														updateStageField(
															stage.tempId,
															"name",
															e.target.value,
														)
													}
													placeholder="Nombre de la etapa"
													className="h-8 w-64 border-none bg-transparent py-0 pl-1 font-semibold text-base shadow-none hover:bg-foreground/5 focus-visible:ring-0"
													disabled={loading}
												/>
											</div>
											<Input
												value={stage.description}
												onChange={(e) =>
													updateStageField(
														stage.tempId,
														"description",
														e.target.value,
													)
												}
												placeholder="Breve descripción de esta fase del juego"
												className="h-7 border-none bg-transparent pl-1 text-muted-foreground text-xs shadow-none hover:bg-foreground/5 focus-visible:ring-0"
												disabled={loading}
											/>
										</div>
										<Button
											type="button"
											variant="ghost"
											size="icon"
											className="size-8 shrink-0 text-muted-foreground hover:text-destructive"
											onClick={() => removeStage(stage.tempId)}
											disabled={loading}
											title="Eliminar Etapa"
										>
											<Trash className="size-4" />
										</Button>
									</CardHeader>

									<CardContent className="space-y-4 pt-0">
										{/* Sub-stages list */}
										<div className="space-y-3 rounded-xl border border-foreground/5 bg-foreground/3 p-4">
											<div className="flex items-center justify-between">
												<span className="font-semibold text-muted-foreground text-xs">
													Subetapas (Actividades)
												</span>
												<div className="flex gap-2">
													<Button
														type="button"
														variant="outline"
														size="xs"
														className="h-7 gap-1 text-[10px]"
														onClick={() => addSubStage(stage.tempId, "Trivia")}
														disabled={loading}
													>
														<HelpCircle className="size-3" /> + Trivia
													</Button>
													<Button
														type="button"
														variant="outline"
														size="xs"
														className="h-7 gap-1 text-[10px]"
														onClick={() =>
															addSubStage(stage.tempId, "TreasureHunt")
														}
														disabled={loading}
													>
														<Compass className="size-3" /> + Búsqueda
													</Button>
												</div>
											</div>

											{stage.subStages.length === 0 ? (
												<div className="py-4 text-center text-muted-foreground/60 text-xs">
													Sin actividades. Agrega una Trivia o Búsqueda del
													Tesoro.
												</div>
											) : (
												stage.subStages.map((_sub, _subIdx) => (
													<div
														key={_sub.tempId}
														className="relative space-y-4 rounded-lg border border-foreground/10 bg-background p-4"
													>
														{/* SubStage Header */}
														<div className="flex items-start justify-between gap-3 border-foreground/5 border-b pb-2">
															<div className="flex items-center gap-2">
																<span className="rounded bg-foreground/10 px-2 py-0.5 font-semibold text-[10px] text-foreground tracking-wider">
																	{_sub.type === "Trivia"
																		? "TRIVIA"
																		: "BÚSQUEDA"}
																</span>
																<Input
																	value={_sub.title}
																	onChange={(e) =>
																		updateSubStageField(
																			stage.tempId,
																			_sub.tempId,
																			"title",
																			e.target.value,
																		)
																	}
																	placeholder="Título de la actividad"
																	className="h-7 w-44 border-none bg-transparent py-0 pl-1 font-semibold text-xs shadow-none hover:bg-foreground/5 focus-visible:ring-0"
																	disabled={loading}
																/>
															</div>
															<div className="flex items-center gap-2">
																{/* Points field — shared by both types */}
																<div className="flex items-center gap-1">
																	<Label className="shrink-0 text-[10px] text-muted-foreground">
																		Pts:
																	</Label>
																	<Input
																		type="number"
																		value={_sub.points}
																		onChange={(e) =>
																			updateSubStageField(
																				stage.tempId,
																				_sub.tempId,
																				"points",
																				Number.parseInt(e.target.value, 10) ||
																					0,
																			)
																		}
																		className="h-7 w-16 p-1 text-center text-xs"
																		disabled={loading}
																		min={0}
																	/>
																</div>
																<Button
																	type="button"
																	variant="ghost"
																	size="icon"
																	className="size-6 text-muted-foreground hover:text-destructive"
																	onClick={() =>
																		removeSubStage(stage.tempId, _sub.tempId)
																	}
																	disabled={loading}
																>
																	<Trash className="size-3.5" />
																</Button>
															</div>
														</div>

														{/* TRIVIA TYPE SUBSTAGE */}
														{_sub.type === "Trivia" && (
															<div className="space-y-3">
																<span className="flex items-center gap-1 font-semibold text-[11px] text-muted-foreground">
																	Pregunta
																</span>

																<div className="space-y-3 rounded-lg border border-foreground/10 border-dashed bg-foreground/2 p-3">
																	{/* Question text */}
																	<Input
																		value={_sub.question.text}
																		onChange={(e) =>
																			updateQuestionField(
																				stage.tempId,
																				_sub.tempId,
																				"text",
																				e.target.value,
																			)
																		}
																		placeholder="Escribe la pregunta..."
																		className="h-8 bg-background text-xs"
																		disabled={loading}
																	/>

																	{/* Answer options */}
																	<div className="space-y-2 border-primary/20 border-l-2 pl-4">
																		<div className="flex items-center justify-between">
																			<span className="font-semibold text-[10px] text-muted-foreground">
																				Opciones
																			</span>
																			<Button
																				type="button"
																				variant="ghost"
																				className="h-5 text-[9px] text-primary"
																				onClick={() =>
																					addAnswer(stage.tempId, _sub.tempId)
																				}
																				disabled={loading}
																			>
																				<PlusCircle className="mr-1 size-3" />
																				Agregar Opción
																			</Button>
																		</div>

																		{_sub.question.answers.map((ans) => (
																			<div
																				key={ans.tempId}
																				className="flex items-center gap-2"
																			>
																				<Checkbox
																					id={`correct-${ans.tempId}`}
																					checked={ans.isCorrect}
																					onCheckedChange={(checked) =>
																						updateAnswerField(
																							stage.tempId,
																							_sub.tempId,
																							ans.tempId,
																							"isCorrect",
																							checked === true,
																						)
																					}
																					disabled={loading}
																				/>
																				<Input
																					value={ans.text}
																					onChange={(e) =>
																						updateAnswerField(
																							stage.tempId,
																							_sub.tempId,
																							ans.tempId,
																							"text",
																							e.target.value,
																						)
																					}
																					placeholder="Texto de la respuesta"
																					className="h-7 flex-1 bg-background text-xs"
																					disabled={loading}
																				/>
																				<Button
																					type="button"
																					variant="ghost"
																					size="icon"
																					className="size-6 shrink-0 text-muted-foreground hover:text-destructive"
																					onClick={() =>
																						removeAnswer(
																							stage.tempId,
																							_sub.tempId,
																							ans.tempId,
																						)
																					}
																					disabled={loading}
																				>
																					<Trash className="size-3" />
																				</Button>
																			</div>
																		))}
																	</div>
																</div>
															</div>
														)}

														{/* TREASURE HUNT TYPE SUBSTAGE */}
														{_sub.type === "TreasureHunt" && (
															<div className="space-y-4">
																<div className="grid gap-4 sm:grid-cols-2">
																	<div className="space-y-1.5">
																		<Label className="text-muted-foreground text-xs">
																			Objetivo de la Búsqueda
																		</Label>
																		<Input
																			value={_sub.objective}
																			onChange={(e) =>
																				updateSubStageField(
																					stage.tempId,
																					_sub.tempId,
																					"objective",
																					e.target.value,
																				)
																			}
																			placeholder="¿Qué deben encontrar los jugadores?"
																			className="h-8 bg-background text-xs"
																			disabled={loading}
																		/>
																	</div>

																	<div className="space-y-1.5">
																		<Label className="text-muted-foreground text-xs">
																			Código Secreto (para validación)
																		</Label>
																		<div className="flex gap-2">
																			<Input
																				value={_sub.secretCode}
																				onChange={(e) =>
																					updateSubStageField(
																						stage.tempId,
																						_sub.tempId,
																						"secretCode",
																						e.target.value
																							.toUpperCase()
																							.replace(/[^A-Z0-9-]/g, ""),
																					)
																				}
																				placeholder="Ej. SECRETO-123"
																				className="h-8 flex-1 bg-background font-mono text-xs uppercase"
																				disabled={loading}
																			/>
																			<Button
																				type="button"
																				variant="outline"
																				size="sm"
																				className="h-8 shrink-0 text-xs"
																				onClick={() => {
																					const rand = Math.random()
																						.toString(36)
																						.substring(2, 6)
																						.toUpperCase();
																					updateSubStageField(
																						stage.tempId,
																						_sub.tempId,
																						"secretCode",
																						`TESORO-${rand}`,
																					);
																				}}
																				disabled={loading}
																			>
																				Generar
																			</Button>
																		</div>
																	</div>
																</div>

																<div className="space-y-2">
																	<div className="flex items-center justify-between">
																		<span className="flex items-center gap-1 font-semibold text-[11px] text-muted-foreground">
																			Pistas Ordenadas
																		</span>
																		<Button
																			type="button"
																			variant="outline"
																			size="xs"
																			className="h-6 gap-1 text-[10px]"
																			onClick={() =>
																				addClue(stage.tempId, _sub.tempId)
																			}
																			disabled={loading}
																		>
																			<PlusCircle className="size-3" /> Agregar
																			Pista
																		</Button>
																	</div>

																	{_sub.clues.map((clue, clueIdx) => (
																		<div
																			key={clue.tempId}
																			className="flex items-center gap-2 rounded border border-foreground/5 bg-foreground/2 p-2"
																		>
																			<span className="font-semibold text-muted-foreground text-xs">
																				P{clueIdx + 1}:
																			</span>
																			<Input
																				value={clue.text}
																				onChange={(e) =>
																					updateClueField(
																						stage.tempId,
																						_sub.tempId,
																						clue.tempId,
																						"text",
																						e.target.value,
																					)
																				}
																				placeholder="Descripción de la pista..."
																				className="h-8 flex-1 bg-background text-xs"
																				disabled={loading}
																			/>
																			<Button
																				type="button"
																				variant="ghost"
																				size="icon"
																				className="size-7 text-muted-foreground hover:text-destructive"
																				onClick={() =>
																					removeClue(
																						stage.tempId,
																						_sub.tempId,
																						clue.tempId,
																					)
																				}
																				disabled={loading}
																			>
																				<Trash className="size-3.5" />
																			</Button>
																		</div>
																	))}
																</div>
															</div>
														)}
													</div>
												))
											)}
										</div>
									</CardContent>
								</Card>
							))
						)}
					</div>
				</div>

				{/* Live Summary Sidebar (1 col) */}
				<div className="space-y-6">
					<Card className="sticky top-6 border-foreground/10 bg-card/60 backdrop-blur">
						<CardHeader>
							<CardTitle className="font-semibold text-foreground text-sm uppercase tracking-wide">
								Resumen del Diseño
							</CardTitle>
							<CardDescription>
								Estadísticas del juego en tiempo real.
							</CardDescription>
						</CardHeader>
						<CardContent className="space-y-4 text-sm">
							<div className="flex items-center justify-between border-foreground/5 border-b py-1.5">
								<span className="text-muted-foreground">Nombre:</span>
								<span
									className="max-w-[150px] truncate font-semibold text-foreground"
									title={mission.name || "Sin nombre"}
								>
									{mission.name || "Sin nombre"}
								</span>
							</div>
							<div className="flex items-center justify-between border-foreground/5 border-b py-1.5">
								<span className="text-muted-foreground">Etapas:</span>
								<span className="font-bold text-primary">
									{mission.stages.length}
								</span>
							</div>
							<div className="flex items-center justify-between border-foreground/5 border-b py-1.5">
								<span className="text-muted-foreground">
									Subetapas de Trivia:
								</span>
								<span className="font-semibold text-foreground">
									{mission.stages.reduce(
										(acc, s) =>
											acc +
											s.subStages.filter((sub) => sub.type === "Trivia").length,
										0,
									)}
								</span>
							</div>
							<div className="flex items-center justify-between border-foreground/5 border-b py-1.5">
								<span className="text-muted-foreground">
									Subetapas de Búsqueda:
								</span>
								<span className="font-semibold text-foreground">
									{mission.stages.reduce(
										(acc, s) =>
											acc +
											s.subStages.filter((sub) => sub.type === "TreasureHunt")
												.length,
										0,
									)}
								</span>
							</div>
							<div className="flex items-center justify-between border-foreground/5 border-b py-1.5 text-primary">
								<span className="font-semibold">Puntos Totales:</span>
								<span className="font-bold text-base">
									{calculateTotalPoints()} Pts
								</span>
							</div>
							<div className="space-y-1 rounded-lg bg-foreground/5 p-3 text-muted-foreground text-xs">
								<span className="flex items-center gap-1 font-semibold text-foreground">
									<Check className="size-3 text-emerald-400" />
									Reglas de Validación (Domain)
								</span>
								<p>• Estado inicial Inactiva.</p>
								<p>
									• Trivias requieren al menos una opción marcada como correcta.
								</p>
								<p>• Puntos asignados por subetapa (Trivia y Búsqueda).</p>
								<p>• Orden correlativo para etapas e índices de pistas.</p>
							</div>
						</CardContent>
					</Card>
				</div>
			</div>
		</div>
	);
}

export default function NuevaMisionPage() {
	return (
		<RoleGuard requiredRoles={["admin", "operator"]}>
			<CreateMissionForm />
		</RoleGuard>
	);
}
