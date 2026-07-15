"use client";

import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardDescription,
	CardFooter,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { Input } from "@umbral/ui/components/input";
import { Label } from "@umbral/ui/components/label";
import {
	AlertTriangle,
	ArrowDown,
	ArrowLeft,
	ArrowUp,
	Check,
	Compass,
	Edit,
	HelpCircle,
	Loader2,
	Lock,
	Plus,
	Save,
	Trash,
	Unlock,
	X,
} from "lucide-react";
import dynamic from "next/dynamic";
import Link from "next/link";
import { useParams, useRouter } from "next/navigation";
import { useCallback, useEffect, useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/components/auth-provider";
import { MissionDetailSkeleton } from "@/components/missions/mission-detail-skeleton";
import { RoleGuard } from "@/components/role-guard";
import {
	addStage,
	type CreateStageRequest,
	deleteMission,
	deleteStage,
	getMissionById,
	type MissionDetail,
	reorderStages,
	type StageDetail,
	type SubStageDetail,
	updateMission,
	updateStage,
} from "@/lib/api/missions";

// Leaflet necesita `window`, así que el mapa se carga solo en el cliente (sin SSR).
const TreasureMapPicker = dynamic(
	() =>
		import("@/components/missions/treasure-map-picker").then(
			(m) => m.TreasureMapPicker,
		),
	{ ssr: false },
);

// ─── Client State Types for Editor ──────────────────────────────────────────

interface ClientAnswer {
	tempId: string;
	id?: string;
	text: string;
	isCorrect: boolean;
}

interface ClientQuestion {
	tempId: string;
	id?: string;
	text: string;
	answers: ClientAnswer[];
}

interface ClientClue {
	tempId: string;
	id?: string;
	text: string;
	order: number;
}

interface ClientSubStage {
	tempId: string;
	id?: string;
	title: string;
	order: number;
	type: "Trivia" | "TreasureHunt";
	points: number;
	question: ClientQuestion;
	objective: string;
	secretCode: string;
	latitude: number | null;
	longitude: number | null;
	searchRadiusMeters: number;
	clues: ClientClue[];
}

interface StageForm {
	name: string;
	description: string;
	subStages: ClientSubStage[];
}

// ─── Fetch State ─────────────────────────────────────────────────────────────

type FetchState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "success"; data: MissionDetail };

// ─── Helpers ─────────────────────────────────────────────────────────────────

function StatusBadge({ state }: { state: MissionDetail["state"] }) {
	const isActive = state === "Active";
	return (
		<span
			className={`inline-flex shrink-0 items-center rounded-full px-2.5 py-0.5 font-semibold text-[10px] ring-1 ${
				isActive
					? "bg-emerald-500/15 text-emerald-400 ring-emerald-500/25"
					: "bg-amber-500/15 text-amber-400 ring-amber-500/25"
			}`}
		>
			{isActive ? "Activa" : "Inactiva"}
		</span>
	);
}

function DifficultyBadge({
	level,
}: {
	level: MissionDetail["difficultyLevel"];
}) {
	const colors = {
		Beginner: "bg-cyan-500/15 text-cyan-400 ring-cyan-500/25",
		Intermediate: "bg-amber-500/15 text-amber-400 ring-amber-500/25",
		Advanced: "bg-rose-500/15 text-rose-400 ring-rose-500/25",
	};
	const labels = {
		Beginner: "Principiante",
		Intermediate: "Intermedio",
		Advanced: "Avanzado",
	};
	return (
		<span
			className={`inline-flex shrink-0 items-center rounded-full px-2.5 py-0.5 font-semibold text-[10px] ring-1 ${colors[level] || colors.Beginner}`}
		>
			{labels[level] || level}
		</span>
	);
}

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

function mapStageToForm(stage: StageDetail): StageForm {
	return {
		name: stage.name,
		description: stage.description,
		subStages: stage.subStages.map((sub) => {
			const type = sub.question !== null ? "Trivia" : "TreasureHunt";
			const question: ClientQuestion = sub.question
				? {
						tempId: crypto.randomUUID(),
						id: sub.question.id,
						text: sub.question.text,
						answers: sub.question.answers.map((a) => ({
							tempId: crypto.randomUUID(),
							id: a.id,
							text: a.text,
							isCorrect: a.isCorrect,
						})),
					}
				: makeDefaultQuestion();

			const clues: ClientClue[] = (sub.clues ?? []).map((c) => ({
				tempId: crypto.randomUUID(),
				id: c.id,
				text: c.text,
				order: c.order,
			}));

			return {
				tempId: crypto.randomUUID(),
				id: sub.id,
				title: sub.title,
				order: sub.order,
				type,
				points: sub.points,
				question,
				objective: sub.objective ?? "",
				secretCode: sub.secretCode ?? "",
				latitude: sub.latitude,
				longitude: sub.longitude,
				searchRadiusMeters: sub.searchRadiusMeters ?? 120,
				clues,
			};
		}),
	};
}

function mapFormToRequest(form: StageForm, order: number): CreateStageRequest {
	return {
		name: form.name,
		description: form.description,
		order,
		subStages: form.subStages.map((sub) => {
			const hasClues = sub.clues && sub.clues.length > 0;
			if (sub.type === "Trivia") {
				return {
					id: sub.id,
					title: sub.title,
					order: sub.order,
					type: "Trivia",
					points: Number(sub.points) || 0,
					question: {
						id: sub.question.id,
						text: sub.question.text,
						answers: sub.question.answers.map((a) => ({
							id: a.id,
							text: a.text,
							isCorrect: a.isCorrect,
						})),
					},
					clues: hasClues
						? sub.clues.map((c) => ({
								id: c.id,
								text: c.text,
								order: c.order,
							}))
						: null,
				};
			}
			return {
				id: sub.id,
				title: sub.title,
				order: sub.order,
				type: "TreasureHunt",
				points: Number(sub.points) || 0,
				objective: sub.objective,
				secretCode: sub.secretCode.trim().toUpperCase(),
				latitude: sub.latitude,
				longitude: sub.longitude,
				searchRadiusMeters:
					sub.latitude !== null ? sub.searchRadiusMeters : null,
				clues: hasClues
					? sub.clues.map((c) => ({
							id: c.id,
							text: c.text,
							order: c.order,
						}))
					: null,
			};
		}),
	};
}

function validateStageForm(form: StageForm): boolean {
	if (!form.name.trim() || !form.description.trim()) {
		toast.error("Por favor completa el nombre y descripción de la etapa.");
		return false;
	}

	if (form.subStages.length === 0) {
		toast.error("La etapa debe tener al menos una subetapa.");
		return false;
	}

	for (const sub of form.subStages) {
		if (!sub.title.trim()) {
			toast.error("Ingresa el título para la subetapa.");
			return false;
		}

		if ((Number(sub.points) || 0) < 0) {
			toast.error(`Los puntos de "${sub.title}" no pueden ser negativos.`);
			return false;
		}

		if (sub.type === "Trivia") {
			if (!sub.question.text.trim()) {
				toast.error(
					`Escribe el texto de la pregunta en la trivia "${sub.title}".`,
				);
				return false;
			}
			if (sub.question.answers.length === 0) {
				toast.error(
					`La pregunta de "${sub.title}" debe tener opciones de respuesta.`,
				);
				return false;
			}
			const hasCorrect = sub.question.answers.some((a) => a.isCorrect);
			if (!hasCorrect) {
				toast.error(
					`La pregunta de "${sub.title}" requiere al menos una respuesta correcta marcada.`,
				);
				return false;
			}
			for (const a of sub.question.answers) {
				if (!a.text.trim()) {
					toast.error("Las opciones de respuesta no pueden estar vacías.");
					return false;
				}
			}
		} else if (sub.type === "TreasureHunt") {
			if (!sub.objective.trim()) {
				toast.error(`Especifica el objetivo de la búsqueda "${sub.title}".`);
				return false;
			}
			if (!sub.secretCode.trim()) {
				toast.error(
					`Especifica el código secreto de la búsqueda "${sub.title}".`,
				);
				return false;
			}
			if (!/^[a-zA-Z0-9-]+$/.test(sub.secretCode.trim())) {
				toast.error(
					`El código secreto de "${sub.title}" debe ser alfanumérico y solo puede contener guiones.`,
				);
				return false;
			}
		}

		for (const c of sub.clues) {
			if (!c.text.trim()) {
				toast.error("Las pistas no pueden estar vacías.");
				return false;
			}
		}
	}

	return true;
}

// ─── SubStage Card (Dumb Visual Components) ──────────────────────────────────

function TriviaSubStageCard({ sub }: { sub: SubStageDetail }) {
	const question = sub.question;
	if (!question) return null;
	const clues = [...(sub.clues ?? [])].sort((a, b) => a.order - b.order);

	return (
		<Card className="border-foreground/10 bg-card/40">
			<CardHeader className="pb-2">
				<div className="flex items-center justify-between gap-2">
					<CardTitle className="flex items-center gap-2 font-medium text-sm">
						<HelpCircle className="size-4 text-primary" />
						{sub.title} (Trivia)
					</CardTitle>
					<span className="shrink-0 font-semibold text-muted-foreground text-xs">
						{sub.points} pts
					</span>
				</div>
			</CardHeader>
			<CardContent className="space-y-3 pt-0">
				<p className="text-foreground text-sm">&ldquo;{question.text}&rdquo;</p>
				<div className="flex flex-wrap gap-2">
					{question.answers.map((answer) => (
						<span
							key={answer.id}
							className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 font-medium text-xs ring-1 ${
								answer.isCorrect
									? "bg-emerald-500/15 text-emerald-400 ring-emerald-500/25"
									: "bg-foreground/8 text-muted-foreground ring-foreground/15"
							}`}
						>
							{answer.isCorrect ? (
								<Check className="size-3 shrink-0" />
							) : (
								<X className="size-3 shrink-0 opacity-60" />
							)}
							{answer.text}
						</span>
					))}
				</div>
				{clues.length > 0 && (
					<div className="border-foreground/5 border-t pt-2">
						<span className="font-semibold text-muted-foreground text-xs uppercase tracking-wide">
							Pistas Asociadas
						</span>
						<ol className="mt-1 list-decimal space-y-0.5 pl-5 text-foreground text-xs">
							{clues.map((clue) => (
								<li key={clue.id}>{clue.text}</li>
							))}
						</ol>
					</div>
				)}
			</CardContent>
		</Card>
	);
}

function TreasureHuntSubStageCard({ sub }: { sub: SubStageDetail }) {
	const clues = [...(sub.clues ?? [])].sort((a, b) => a.order - b.order);

	return (
		<Card className="border-foreground/10 bg-card/40">
			<CardHeader className="pb-2">
				<div className="flex items-center justify-between gap-2">
					<CardTitle className="flex items-center gap-2 font-medium text-sm">
						<Compass className="size-4 text-primary" />
						{sub.title} (Búsqueda)
					</CardTitle>
					<span className="shrink-0 font-semibold text-muted-foreground text-xs">
						{sub.points} pts
					</span>
				</div>
			</CardHeader>
			<CardContent className="space-y-3 pt-0 text-sm">
				{sub.objective && (
					<p>
						<span className="font-medium text-muted-foreground">
							Objetivo:{" "}
						</span>
						<span className="text-foreground">
							&ldquo;{sub.objective}&rdquo;
						</span>
					</p>
				)}
				{sub.secretCode && (
					<div>
						<span className="font-medium text-muted-foreground text-xs uppercase tracking-wide">
							Código de validación
						</span>
						<code className="mt-1 block max-w-max rounded bg-foreground/6 px-2 py-0.5 font-mono text-foreground text-xs">
							{sub.secretCode}
						</code>
					</div>
				)}
				{clues.length > 0 && (
					<div>
						<span className="font-semibold text-muted-foreground text-xs uppercase tracking-wide">
							Pistas Ordenadas
						</span>
						<ol className="mt-1 list-decimal space-y-0.5 pl-5 text-foreground text-xs">
							{clues.map((clue) => (
								<li key={clue.id}>{clue.text}</li>
							))}
						</ol>
					</div>
				)}
			</CardContent>
		</Card>
	);
}

function SubStageCard({ sub }: { sub: SubStageDetail }) {
	if (sub.question !== null) return <TriviaSubStageCard sub={sub} />;
	if (sub.objective !== null) return <TreasureHuntSubStageCard sub={sub} />;
	return null;
}

// ─── Main Panel Component ───────────────────────────────────────────────────

function MissionDetailPanelInner() {
	const params = useParams<{ id: string }>();
	const router = useRouter();
	const { getAccessToken } = useAuth();
	const missionId = params.id;

	const [state, setState] = useState<FetchState>({ status: "loading" });

	// Operations Loading States
	const [loadingOperations, setLoadingOperations] = useState({
		deletingMission: false,
		savingMission: false,
		savingStage: false,
		togglingState: false,
		reordering: false,
		deletingStageId: null as string | null,
	});

	// Forms Modes
	const [editMissionMode, setEditMissionMode] = useState(false);
	const [editStageId, setEditStageId] = useState<string | null>(null);
	const [isAddingStage, setIsAddingStage] = useState(false);

	// Local Form States
	const [missionForm, setMissionForm] = useState({
		name: "",
		description: "",
		difficultyLevel: "Beginner" as "Beginner" | "Intermediate" | "Advanced",
		maxTimeMinutes: 60,
	});

	const [stageForm, setStageForm] = useState<StageForm>({
		name: "",
		description: "",
		subStages: [],
	});

	// ─── Fetch Mission Data ────────────────────────────────────────────────────

	const fetchMission = useCallback(async () => {
		if (!missionId) {
			setState({
				status: "error",
				message: "Identificador de misión inválido.",
			});
			return;
		}

		setState({ status: "loading" });
		try {
			const token = await getAccessToken(60);
			if (!token) {
				setState({ status: "error", message: "No hay sesión activa." });
				return;
			}

			const data = await getMissionById(missionId, token);
			setState({ status: "success", data });

			// Sync basic form fields
			setMissionForm({
				name: data.name,
				description: data.description,
				difficultyLevel: data.difficultyLevel,
				maxTimeMinutes: data.maxTimeMinutes,
			});
		} catch (err) {
			const message =
				err instanceof Error
					? err.message
					: "No se pudo cargar el detalle de la misión.";
			setState({ status: "error", message });
			toast.error(message);
		}
	}, [getAccessToken, missionId]);

	useEffect(() => {
		void fetchMission();
	}, [fetchMission]);

	const data = state.status === "success" ? state.data : null;

	const stages = data
		? [...data.stages]
				.sort((a, b) => a.order - b.order)
				.map((stage) => ({
					...stage,
					subStages: [...stage.subStages].sort((a, b) => a.order - b.order),
				}))
		: [];

	// ─── Mission level actions ────────────────────────────────────────────────

	const handleUpdateMission = async () => {
		if (!missionForm.name.trim() || !missionForm.description.trim()) {
			toast.error("Por favor completa el nombre y descripción.");
			return;
		}
		if (missionForm.maxTimeMinutes <= 0) {
			toast.error("El tiempo máximo debe ser mayor a 0 minutos.");
			return;
		}

		setLoadingOperations((prev) => ({ ...prev, savingMission: true }));
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No hay sesión activa.");

			await updateMission(missionId, missionForm, token);
			toast.success("Misión actualizada con éxito.");
			setEditMissionMode(false);
			await fetchMission();
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "Error al actualizar misión.",
			);
		} finally {
			setLoadingOperations((prev) => ({ ...prev, savingMission: false }));
		}
	};

	const handleDeleteMission = async () => {
		if (
			!window.confirm(
				"¿Estás seguro de que deseas eliminar permanentemente esta misión? Esta acción no se puede deshacer.",
			)
		) {
			return;
		}

		setLoadingOperations((prev) => ({ ...prev, deletingMission: true }));
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No hay sesión activa.");

			await deleteMission(missionId, token);
			toast.success("Misión eliminada correctamente.");
			router.push("/dashboard/misiones");
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "No se pudo eliminar la misión.",
			);
		} finally {
			setLoadingOperations((prev) => ({ ...prev, deletingMission: false }));
		}
	};

	const handleToggleState = async () => {
		if (!data) return;
		const newState = data.state === "Active" ? "Inactive" : "Active";

		setLoadingOperations((prev) => ({ ...prev, togglingState: true }));
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No hay sesión activa.");

			await updateMission(missionId, { state: newState }, token);
			toast.success(
				`Misión cambiada a estado: ${newState === "Active" ? "Activa" : "Inactiva"}.`,
			);
			await fetchMission();
		} catch (err) {
			toast.error(
				err instanceof Error
					? err.message
					: "Error al cambiar estado de la misión.",
			);
		} finally {
			setLoadingOperations((prev) => ({ ...prev, togglingState: false }));
		}
	};

	// ─── Stage level actions ──────────────────────────────────────────────────

	const handleStartAddStage = () => {
		setStageForm({
			name: `Etapa ${stages.length + 1}`,
			description: "",
			subStages: [],
		});
		setEditStageId(null);
		setIsAddingStage(true);
	};

	const handleStartEditStage = (stage: StageDetail) => {
		setStageForm(mapStageToForm(stage));
		setIsAddingStage(false);
		setEditStageId(stage.id);
	};

	const handleSaveStage = async (stageId: string | null) => {
		if (!validateStageForm(stageForm)) return;

		setLoadingOperations((prev) => ({ ...prev, savingStage: true }));
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No hay sesión activa.");

			if (stageId) {
				// UPDATE STAGE
				const existingStage = stages.find((s) => s.id === stageId);
				const order = existingStage
					? existingStage.order
					: stages.length > 0
						? Math.max(...stages.map((s) => s.order)) + 1
						: 1;
				const request = mapFormToRequest(stageForm, order);
				await updateStage(missionId, stageId, request, token);
				toast.success("Etapa actualizada correctamente.");
				setEditStageId(null);
			} else {
				// ADD STAGE
				const order =
					stages.length > 0 ? Math.max(...stages.map((s) => s.order)) + 1 : 1;
				const request = mapFormToRequest(stageForm, order);
				await addStage(missionId, request, token);
				toast.success("Nueva etapa agregada.");
				setIsAddingStage(false);
			}

			await fetchMission();
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "Error al guardar la etapa.",
			);
		} finally {
			setLoadingOperations((prev) => ({ ...prev, savingStage: false }));
		}
	};

	const handleDeleteStage = async (stageId: string) => {
		if (
			!window.confirm(
				"¿Seguro que deseas eliminar esta etapa y todas sus subetapas?",
			)
		) {
			return;
		}

		setLoadingOperations((prev) => ({ ...prev, deletingStageId: stageId }));
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No hay sesión activa.");

			await deleteStage(missionId, stageId, token);
			toast.success("Etapa eliminada correctamente.");
			await fetchMission();
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "No se pudo eliminar la etapa.",
			);
		} finally {
			setLoadingOperations((prev) => ({ ...prev, deletingStageId: null }));
		}
	};

	const handleReorderStage = async (
		index: number,
		direction: "up" | "down",
	) => {
		if (!data) return;
		const newIndex = direction === "up" ? index - 1 : index + 1;
		if (newIndex < 0 || newIndex >= stages.length) return;

		setLoadingOperations((prev) => ({ ...prev, reordering: true }));
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No hay sesión activa.");

			const reordered = [...stages];
			const [moved] = reordered.splice(index, 1);
			reordered.splice(newIndex, 0, moved);

			const stageIds = reordered.map((s) => s.id);
			await reorderStages(missionId, stageIds, token);
			toast.success("Orden de etapas actualizado.");
			await fetchMission();
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "No se pudo reordenar las etapas.",
			);
		} finally {
			setLoadingOperations((prev) => ({ ...prev, reordering: false }));
		}
	};

	// ─── Stage form sub-handlers ──────────────────────────────────────────────

	const handleAddSubStage = (type: "Trivia" | "TreasureHunt") => {
		const newSub: ClientSubStage = {
			tempId: crypto.randomUUID(),
			title: `${type === "Trivia" ? "Trivia" : "Búsqueda"} ${stageForm.subStages.length + 1}`,
			order: stageForm.subStages.length,
			type,
			points: 10,
			question: makeDefaultQuestion(),
			objective: "",
			secretCode: "",
			latitude: null,
			longitude: null,
			searchRadiusMeters: 120,
			clues: [],
		};
		setStageForm((prev) => ({
			...prev,
			subStages: [...prev.subStages, newSub],
		}));
	};

	const handleRemoveSubStage = (subTempId: string) => {
		setStageForm((prev) => {
			const filtered = prev.subStages.filter((sub) => sub.tempId !== subTempId);
			const updated = filtered.map((sub, index) => ({ ...sub, order: index }));
			return { ...prev, subStages: updated };
		});
	};

	const handleUpdateSubStage = (
		subTempId: string,
		field: keyof ClientSubStage,
		value: unknown,
	) => {
		setStageForm((prev) => ({
			...prev,
			subStages: prev.subStages.map((sub) =>
				sub.tempId === subTempId ? { ...sub, [field]: value } : sub,
			),
		}));
	};

	// Trivia Handlers
	const handleUpdateQuestionText = (subTempId: string, text: string) => {
		setStageForm((prev) => ({
			...prev,
			subStages: prev.subStages.map((sub) => {
				if (sub.tempId !== subTempId) return sub;
				return { ...sub, question: { ...sub.question, text } };
			}),
		}));
	};

	const handleAddAnswer = (subTempId: string) => {
		setStageForm((prev) => ({
			...prev,
			subStages: prev.subStages.map((sub) => {
				if (sub.tempId !== subTempId) return sub;
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
		}));
	};

	const handleRemoveAnswer = (subTempId: string, answerTempId: string) => {
		setStageForm((prev) => ({
			...prev,
			subStages: prev.subStages.map((sub) => {
				if (sub.tempId !== subTempId) return sub;
				return {
					...sub,
					question: {
						...sub.question,
						answers: sub.question.answers.filter(
							(a) => a.tempId !== answerTempId,
						),
					},
				};
			}),
		}));
	};

	const handleUpdateAnswer = (
		subTempId: string,
		answerTempId: string,
		field: keyof ClientAnswer,
		value: unknown,
	) => {
		setStageForm((prev) => ({
			...prev,
			subStages: prev.subStages.map((sub) => {
				if (sub.tempId !== subTempId) return sub;
				return {
					...sub,
					question: {
						...sub.question,
						answers: sub.question.answers.map((a) =>
							a.tempId === answerTempId ? { ...a, [field]: value } : a,
						),
					},
				};
			}),
		}));
	};

	// Clue Handlers
	const handleAddClue = (subTempId: string) => {
		setStageForm((prev) => ({
			...prev,
			subStages: prev.subStages.map((sub) => {
				if (sub.tempId !== subTempId) return sub;
				const newC: ClientClue = {
					tempId: crypto.randomUUID(),
					text: "Nueva pista",
					order: sub.clues.length,
				};
				return { ...sub, clues: [...sub.clues, newC] };
			}),
		}));
	};

	const handleRemoveClue = (subTempId: string, clueTempId: string) => {
		setStageForm((prev) => ({
			...prev,
			subStages: prev.subStages.map((sub) => {
				if (sub.tempId !== subTempId) return sub;
				const filtered = sub.clues.filter((c) => c.tempId !== clueTempId);
				const indexed = filtered.map((c, idx) => ({ ...c, order: idx }));
				return { ...sub, clues: indexed };
			}),
		}));
	};

	const handleUpdateClueText = (
		subTempId: string,
		clueTempId: string,
		text: string,
	) => {
		setStageForm((prev) => ({
			...prev,
			subStages: prev.subStages.map((sub) => {
				if (sub.tempId !== subTempId) return sub;
				return {
					...sub,
					clues: sub.clues.map((c) =>
						c.tempId === clueTempId ? { ...c, text } : c,
					),
				};
			}),
		}));
	};

	// ─── Render Stage Edit UI Inline ──────────────────────────────────────────

	const renderStageEditor = (stageId: string | null) => {
		return (
			<Card className="border-primary/30 bg-primary/5 ring-1 ring-primary/20">
				<CardHeader>
					<CardTitle className="font-semibold text-base text-foreground">
						{stageId ? "Editar Etapa" : "Nueva Etapa para Misión"}
					</CardTitle>
					<CardDescription>
						Define el nombre, descripción y el conjunto de actividades
						(subetapas) asociadas.
					</CardDescription>
				</CardHeader>
				<CardContent className="space-y-4">
					<div className="space-y-1.5">
						<Label>Nombre de la Etapa</Label>
						<Input
							placeholder="Ej. Fase 1: Descubrimiento"
							value={stageForm.name}
							onChange={(e) =>
								setStageForm((prev) => ({ ...prev, name: e.target.value }))
							}
							disabled={loadingOperations.savingStage}
						/>
					</div>
					<div className="space-y-1.5">
						<Label>Descripción de la Etapa</Label>
						<textarea
							placeholder="¿Qué deben hacer los jugadores en esta etapa?"
							value={stageForm.description}
							onChange={(e) =>
								setStageForm((prev) => ({
									...prev,
									description: e.target.value,
								}))
							}
							disabled={loadingOperations.savingStage}
							className="flex min-h-[60px] w-full rounded-md border border-input bg-background px-3 py-2 text-sm shadow-sm placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-1 focus-visible:ring-ring disabled:cursor-not-allowed disabled:opacity-50"
						/>
					</div>

					{/* Substages section */}
					<div className="space-y-3 rounded-lg border border-foreground/10 bg-background/50 p-4">
						<div className="flex items-center justify-between">
							<span className="font-semibold text-muted-foreground text-xs uppercase tracking-wide">
								Subetapas (Actividades)
							</span>
							<div className="flex gap-2">
								<Button
									type="button"
									variant="outline"
									size="xs"
									className="h-7 text-[10px]"
									onClick={() => handleAddSubStage("Trivia")}
									disabled={loadingOperations.savingStage}
								>
									<HelpCircle className="size-3" /> + Trivia
								</Button>
								<Button
									type="button"
									variant="outline"
									size="xs"
									className="h-7 text-[10px]"
									onClick={() => handleAddSubStage("TreasureHunt")}
									disabled={loadingOperations.savingStage}
								>
									<Compass className="size-3" /> + Búsqueda
								</Button>
							</div>
						</div>

						{stageForm.subStages.length === 0 ? (
							<p className="py-6 text-center text-muted-foreground text-xs italic">
								No hay subetapas creadas. Agrega al menos una Trivia o Búsqueda
								del Tesoro.
							</p>
						) : (
							<div className="space-y-4">
								{stageForm.subStages.map((sub, _sIdx) => (
									<div
										key={sub.tempId}
										className="relative rounded-lg border border-foreground/5 bg-foreground/3 p-3.5"
									>
										<div className="absolute top-2.5 right-2.5">
											<Button
												type="button"
												variant="ghost"
												size="icon"
												className="size-7 text-muted-foreground hover:text-destructive"
												onClick={() => handleRemoveSubStage(sub.tempId)}
												disabled={loadingOperations.savingStage}
												title="Eliminar Subetapa"
											>
												<Trash className="size-3.5" />
											</Button>
										</div>

										<div className="grid gap-3 pr-6 sm:grid-cols-3">
											<div className="space-y-1 sm:col-span-2">
												<Label className="text-[11px] text-muted-foreground">
													Título de la Actividad
												</Label>
												<Input
													value={sub.title}
													onChange={(e) =>
														handleUpdateSubStage(
															sub.tempId,
															"title",
															e.target.value,
														)
													}
													placeholder="Ej. Resolver Acertijo de la Esfinge"
													className="h-8 bg-background text-xs"
													disabled={loadingOperations.savingStage}
												/>
											</div>
											<div className="space-y-1">
												<Label className="text-[11px] text-muted-foreground">
													Puntos Otorgados
												</Label>
												<Input
													type="number"
													value={sub.points}
													onChange={(e) =>
														handleUpdateSubStage(
															sub.tempId,
															"points",
															Number(e.target.value) || 0,
														)
													}
													className="h-8 bg-background text-xs"
													disabled={loadingOperations.savingStage}
												/>
											</div>
										</div>

										{/* Trivia Specific Editor */}
										{sub.type === "Trivia" && (
											<div className="mt-3 space-y-3 border-foreground/5 border-t pt-3">
												<div className="space-y-1">
													<Label className="text-[11px] text-primary/80">
														Pregunta
													</Label>
													<Input
														value={sub.question.text}
														onChange={(e) =>
															handleUpdateQuestionText(
																sub.tempId,
																e.target.value,
															)
														}
														placeholder="Escribe la pregunta de Trivia..."
														className="h-8 bg-background text-xs"
														disabled={loadingOperations.savingStage}
													/>
												</div>
												<div className="space-y-2">
													<div className="flex items-center justify-between">
														<span className="font-semibold text-[10px] text-muted-foreground uppercase">
															Respuestas (Marca las correctas)
														</span>
														<Button
															type="button"
															variant="outline"
															size="xs"
															className="h-5 text-[9px]"
															onClick={() => handleAddAnswer(sub.tempId)}
															disabled={loadingOperations.savingStage}
														>
															+ Opción
														</Button>
													</div>

													<div className="space-y-1.5">
														{sub.question.answers.map((ans) => (
															<div
																key={ans.tempId}
																className="flex items-center gap-2"
															>
																<input
																	type="checkbox"
																	checked={ans.isCorrect}
																	onChange={(e) =>
																		handleUpdateAnswer(
																			sub.tempId,
																			ans.tempId,
																			"isCorrect",
																			e.target.checked,
																		)
																	}
																	disabled={loadingOperations.savingStage}
																	className="size-3.5 rounded border-input text-primary focus:ring-primary"
																/>
																<Input
																	value={ans.text}
																	onChange={(e) =>
																		handleUpdateAnswer(
																			sub.tempId,
																			ans.tempId,
																			"text",
																			e.target.value,
																		)
																	}
																	placeholder="Opción de respuesta"
																	className="h-7 flex-1 bg-background text-xs"
																	disabled={loadingOperations.savingStage}
																/>
																<Button
																	type="button"
																	variant="ghost"
																	size="icon"
																	className="size-6 text-muted-foreground hover:text-destructive"
																	onClick={() =>
																		handleRemoveAnswer(sub.tempId, ans.tempId)
																	}
																	disabled={loadingOperations.savingStage}
																>
																	<Trash className="size-3" />
																</Button>
															</div>
														))}
													</div>
												</div>
											</div>
										)}

										{/* Treasure Hunt Specific Editor */}
										{sub.type === "TreasureHunt" && (
											<div className="mt-3 space-y-3 border-foreground/5 border-t pt-3">
												<div className="grid gap-3 sm:grid-cols-2">
													<div className="space-y-1">
														<Label className="text-[11px] text-primary/80">
															Objetivo del Tesoro
														</Label>
														<Input
															value={sub.objective}
															onChange={(e) =>
																handleUpdateSubStage(
																	sub.tempId,
																	"objective",
																	e.target.value,
																)
															}
															placeholder="¿Qué deben buscar?"
															className="h-8 bg-background text-xs"
															disabled={loadingOperations.savingStage}
														/>
													</div>
													<div className="space-y-1">
														<Label className="text-[11px] text-primary/80">
															Código de Validación
														</Label>
														<Input
															value={sub.secretCode}
															onChange={(e) =>
																handleUpdateSubStage(
																	sub.tempId,
																	"secretCode",
																	e.target.value
																		.toUpperCase()
																		.replace(/[^A-Z0-9-]/g, ""),
																)
															}
															placeholder="Ej. TESORO-123"
															className="h-8 bg-background font-mono text-xs uppercase"
															disabled={loadingOperations.savingStage}
														/>
													</div>
												</div>
												<div className="space-y-1">
													<Label className="text-[11px] text-primary/80">
														Ubicación del tesoro (opcional)
													</Label>
													<TreasureMapPicker
														disabled={loadingOperations.savingStage}
														latitude={sub.latitude}
														longitude={sub.longitude}
														onChange={(lat, lng) => {
															handleUpdateSubStage(sub.tempId, "latitude", lat);
															handleUpdateSubStage(
																sub.tempId,
																"longitude",
																lng,
															);
														}}
														onRadiusChange={(r) =>
															handleUpdateSubStage(
																sub.tempId,
																"searchRadiusMeters",
																r,
															)
														}
														radius={sub.searchRadiusMeters}
													/>
												</div>
											</div>
										)}

										{/* Common Clues editor */}
										<div className="mt-3.5 space-y-2 border-foreground/5 border-t pt-3">
											<div className="flex items-center justify-between">
												<span className="font-semibold text-[10px] text-muted-foreground uppercase">
													Pistas Opcionales
												</span>
												<Button
													type="button"
													variant="outline"
													size="xs"
													className="h-5 text-[9px]"
													onClick={() => handleAddClue(sub.tempId)}
													disabled={loadingOperations.savingStage}
												>
													+ Pista
												</Button>
											</div>

											{sub.clues.length === 0 ? (
												<p className="text-[10px] text-muted-foreground italic">
													Sin pistas adicionales.
												</p>
											) : (
												<div className="space-y-1.5">
													{sub.clues.map((clue, cIdx) => (
														<div
															key={clue.tempId}
															className="flex items-center gap-2"
														>
															<span className="font-medium text-muted-foreground text-xs">
																P{cIdx + 1}:
															</span>
															<Input
																value={clue.text}
																onChange={(e) =>
																	handleUpdateClueText(
																		sub.tempId,
																		clue.tempId,
																		e.target.value,
																	)
																}
																placeholder="Descripción de la pista"
																className="h-7 flex-1 bg-background text-xs"
																disabled={loadingOperations.savingStage}
															/>
															<Button
																type="button"
																variant="ghost"
																size="icon"
																className="size-6 text-muted-foreground hover:text-destructive"
																onClick={() =>
																	handleRemoveClue(sub.tempId, clue.tempId)
																}
																disabled={loadingOperations.savingStage}
															>
																<Trash className="size-3" />
															</Button>
														</div>
													))}
												</div>
											)}
										</div>
									</div>
								))}
							</div>
						)}
					</div>
				</CardContent>
				<CardFooter className="flex justify-end gap-2 border-foreground/5 border-t bg-card/10 px-6 py-3">
					<Button
						variant="ghost"
						size="sm"
						onClick={() => {
							setEditStageId(null);
							setIsAddingStage(false);
						}}
						disabled={loadingOperations.savingStage}
					>
						Cancelar
					</Button>
					<Button
						size="sm"
						onClick={() => void handleSaveStage(stageId)}
						disabled={loadingOperations.savingStage}
						className="gap-1.5"
					>
						{loadingOperations.savingStage ? (
							<>
								<Loader2 className="size-3.5 animate-spin" />
								Guardando…
							</>
						) : (
							<>
								<Save className="size-3.5" />
								Guardar Etapa
							</>
						)}
					</Button>
				</CardFooter>
			</Card>
		);
	};

	// ─── Main Render ─────────────────────────────────────────────────────────

	if (state.status === "loading") {
		return (
			<div className="mx-auto max-w-5xl">
				<MissionDetailSkeleton />
			</div>
		);
	}

	if (state.status === "error" || !data) {
		return (
			<div className="mx-auto max-w-5xl space-y-6">
				<div>
					<Link
						href="/dashboard/misiones"
						className="inline-flex items-center gap-1.5 text-muted-foreground text-sm transition-colors hover:text-foreground"
					>
						<ArrowLeft className="size-3.5" />
						Volver a misiones
					</Link>
				</div>
				<Card className="space-y-4 border-destructive/20 bg-destructive/5 p-8 text-center">
					<div className="mx-auto flex size-12 items-center justify-center rounded-full bg-destructive/10 text-destructive">
						<AlertTriangle className="size-6" />
					</div>
					<h2 className="font-semibold text-foreground text-lg">
						Error al cargar misión
					</h2>
					<p className="text-muted-foreground text-sm">
						{state.status === "error"
							? state.message
							: "Identificador inválido."}
					</p>
					<Button
						onClick={() => void fetchMission()}
						variant="outline"
						size="sm"
					>
						Reintentar
					</Button>
				</Card>
			</div>
		);
	}

	const isMissionActiveInSessions = data.activeSessionCount > 0;

	return (
		<div className="mx-auto max-w-5xl space-y-6">
			<div>
				<Link
					href="/dashboard/misiones"
					className="inline-flex items-center gap-1.5 text-muted-foreground text-sm transition-colors hover:text-foreground"
				>
					<ArrowLeft className="size-3.5" />
					Volver a Misiones
				</Link>
			</div>

			{/* Main Detail Header Card */}
			<Card className="border-foreground/10 bg-card">
				<CardHeader>
					<div className="flex flex-wrap items-start justify-between gap-4">
						<div className="min-w-[280px] flex-1 space-y-1.5">
							{editMissionMode ? (
								<Input
									value={missionForm.name}
									onChange={(e) =>
										setMissionForm((prev) => ({
											...prev,
											name: e.target.value,
										}))
									}
									placeholder="Nombre de la misión"
									className="max-w-md font-semibold text-lg"
									disabled={loadingOperations.savingMission}
								/>
							) : (
								<h1 className="flex items-center gap-2 font-semibold text-2xl text-foreground tracking-tight">
									{data.name}
									{isMissionActiveInSessions && (
										<span title="Misión en uso activo">
											<Lock className="size-4 text-amber-500" />
										</span>
									)}
								</h1>
							)}

							{editMissionMode ? (
								<textarea
									value={missionForm.description}
									onChange={(e) =>
										setMissionForm((prev) => ({
											...prev,
											description: e.target.value,
										}))
									}
									placeholder="Descripción de la misión"
									className="flex min-h-[60px] w-full rounded-md border border-input bg-background px-3 py-2 text-xs shadow-sm placeholder:text-muted-foreground focus-visible:outline-none"
									disabled={loadingOperations.savingMission}
								/>
							) : (
								<p className="text-muted-foreground text-sm">
									{data.description || "Sin descripción."}
								</p>
							)}
						</div>

						{/* Badges Column */}
						<div className="flex shrink-0 flex-col items-end gap-1.5">
							<div className="flex items-center gap-1.5">
								<DifficultyBadge level={data.difficultyLevel} />
								<StatusBadge state={data.state} />
							</div>
							<div className="text-[11px] text-muted-foreground">
								Sesiones Activas:{" "}
								<span className="font-semibold text-foreground">
									{data.activeSessionCount}
								</span>
							</div>
							<div className="text-[11px] text-muted-foreground">
								Tiempo Límite:{" "}
								<span className="font-semibold text-foreground">
									{data.maxTimeMinutes} min
								</span>
							</div>
						</div>
					</div>
				</CardHeader>

				{/* Editing Panel Fields */}
				{editMissionMode && (
					<CardContent className="border-foreground/5 border-t pt-4">
						<div className="grid grid-cols-2 gap-4">
							<div className="space-y-1.5">
								<Label htmlFor="difficultyLevel">Nivel de Dificultad</Label>
								<select
									id="difficultyLevel"
									name="difficultyLevel"
									value={missionForm.difficultyLevel}
									onChange={(e) =>
										setMissionForm((prev) => ({
											...prev,
											difficultyLevel: e.target
												.value as typeof missionForm.difficultyLevel,
										}))
									}
									className="flex h-9 w-full rounded-md border border-input bg-background px-3 py-1 text-sm shadow-sm focus-visible:outline-none"
									disabled={loadingOperations.savingMission}
								>
									<option value="Beginner">Principiante</option>
									<option value="Intermediate">Intermedio</option>
									<option value="Advanced">Avanzado</option>
								</select>
							</div>
							<div className="space-y-1.5">
								<Label htmlFor="maxTimeMinutes">Tiempo Máximo (minutos)</Label>
								<Input
									id="maxTimeMinutes"
									type="number"
									min="1"
									value={missionForm.maxTimeMinutes}
									onChange={(e) =>
										setMissionForm((prev) => ({
											...prev,
											maxTimeMinutes: Number(e.target.value) || 0,
										}))
									}
									className="h-9"
									disabled={loadingOperations.savingMission}
								/>
							</div>
						</div>
					</CardContent>
				)}

				<CardFooter className="flex flex-wrap items-center justify-between gap-3 border-foreground/5 border-t bg-foreground/2 px-6 py-3">
					<div className="flex gap-2">
						{/* Toggle State Button */}
						<Button
							variant="outline"
							size="sm"
							onClick={handleToggleState}
							disabled={
								loadingOperations.togglingState ||
								isMissionActiveInSessions ||
								stages.some((s) => s.activeSessionCount > 0)
							}
							className="h-8 gap-1.5 text-xs"
							title={
								isMissionActiveInSessions ||
								stages.some((s) => s.activeSessionCount > 0)
									? "No se puede cambiar el estado de una misión con sesiones activas"
									: ""
							}
						>
							{loadingOperations.togglingState ? (
								<Loader2 className="size-3.5 animate-spin" />
							) : data.state === "Active" ? (
								<>
									<Lock className="size-3.5 text-amber-500" />
									Desactivar Misión
								</>
							) : (
								<>
									<Unlock className="size-3.5 text-emerald-500" />
									Activar Misión
								</>
							)}
						</Button>

						{/* Hard Delete Button */}
						<Button
							variant="destructive"
							size="sm"
							onClick={handleDeleteMission}
							disabled={
								loadingOperations.deletingMission || isMissionActiveInSessions
							}
							className="h-8 gap-1.5 text-xs"
							title={
								isMissionActiveInSessions
									? "No se puede eliminar una misión con sesiones activas"
									: ""
							}
						>
							{loadingOperations.deletingMission ? (
								<Loader2 className="size-3.5 animate-spin" />
							) : (
								<>
									<Trash className="size-3.5" />
									Eliminar Misión
								</>
							)}
						</Button>
					</div>

					<div className="flex gap-2">
						{editMissionMode ? (
							<>
								<Button
									variant="ghost"
									size="sm"
									className="h-8 text-xs"
									onClick={() => setEditMissionMode(false)}
									disabled={loadingOperations.savingMission}
								>
									Cancelar
								</Button>
								<Button
									size="sm"
									className="h-8 gap-1.5 text-xs"
									onClick={handleUpdateMission}
									disabled={loadingOperations.savingMission}
								>
									{loadingOperations.savingMission ? (
										<Loader2 className="size-3 animate-spin" />
									) : (
										<>
											<Save className="size-3" />
											Guardar
										</>
									)}
								</Button>
							</>
						) : (
							<Button
								variant="outline"
								size="sm"
								className="h-8 gap-1.5 text-xs"
								onClick={() => setEditMissionMode(true)}
								disabled={isMissionActiveInSessions}
								title={
									isMissionActiveInSessions
										? "No se pueden editar los campos de una misión activa en uso"
										: ""
								}
							>
								<Edit className="size-3.5" />
								Editar Misión
							</Button>
						)}
					</div>
				</CardFooter>
			</Card>

			{/* Guard Warning Banner if Active Sessions exist */}
			{isMissionActiveInSessions && (
				<div className="flex items-start gap-3 rounded-lg border border-amber-500/20 bg-amber-500/5 p-4 text-amber-500 text-xs">
					<AlertTriangle className="mt-0.5 size-4 shrink-0" />
					<div>
						<span className="font-semibold">Misión en ejecución activa</span>
						<p className="mt-0.5 text-amber-500/80 leading-relaxed">
							Esta plantilla está siendo utilizada en {data.activeSessionCount}{" "}
							sesión(es) de juego activas. Se ha bloqueado la eliminación de la
							misión, la alteración del orden de las etapas, la creación de
							nuevas etapas, y la edición de campos globales de la misión. Sin
							embargo, aún puedes editar las etapas individuales que no tengan
							sesiones activas.
						</p>
					</div>
				</div>
			)}

			{/* Section: Stages Management */}
			<div className="space-y-4 pt-4">
				<div className="flex items-center justify-between border-foreground/10 border-b pb-2">
					<h2 className="font-semibold text-base text-foreground tracking-tight">
						Etapas del Desafío ({stages.length})
					</h2>
					<Button
						variant="outline"
						size="sm"
						className="h-8 gap-1.5 text-xs"
						onClick={handleStartAddStage}
						disabled={
							isMissionActiveInSessions || isAddingStage || editStageId !== null
						}
					>
						<Plus className="size-3.5" />
						Agregar Etapa
					</Button>
				</div>

				{/* Inline Add Stage Editor */}
				{isAddingStage && renderStageEditor(null)}

				{stages.length === 0 && !isAddingStage && (
					<div className="rounded-xl border border-foreground/10 border-dashed bg-card/20 p-10 text-center text-muted-foreground text-sm">
						Esta misión no tiene etapas diseñadas todavía. Haz clic en "Agregar
						Etapa" arriba para estructurar el juego.
					</div>
				)}

				<div className="space-y-6">
					{stages.map((stage, idx) => {
						const isEditingThis = editStageId === stage.id;
						const isStageActive = stage.activeSessionCount > 0;

						if (isEditingThis) {
							return <div key={stage.id}>{renderStageEditor(stage.id)}</div>;
						}

						return (
							<section
								key={stage.id}
								className="group relative space-y-4 rounded-xl border border-foreground/5 bg-card/25 p-5 transition-all duration-300 hover:border-foreground/10"
							>
								{/* Stage Header details */}
								<div className="flex flex-wrap items-start justify-between gap-4 border-foreground/5 border-b pb-3">
									<div className="space-y-1">
										<h3 className="flex flex-wrap items-center gap-2 font-semibold text-foreground text-sm uppercase tracking-wider">
											Etapa {idx + 1}: {stage.name}
											<span
												className={`inline-flex items-center gap-1 rounded-full px-2.5 py-0.5 font-medium text-[10px] normal-case ring-1 ${
													isStageActive
														? "bg-amber-500/15 text-amber-400 ring-amber-500/25"
														: "bg-foreground/5 text-muted-foreground ring-foreground/10"
												}`}
											>
												Sesiones: {stage.activeSessionCount}
											</span>
											{isStageActive && (
												<span className="inline-flex items-center gap-1 rounded-full bg-rose-500/15 px-2.5 py-0.5 font-medium text-[10px] text-rose-400 normal-case ring-1 ring-rose-500/25">
													<Lock className="size-2.5" /> Bloqueada
												</span>
											)}
										</h3>
										{stage.description && (
											<p className="text-muted-foreground text-xs">
												{stage.description}
											</p>
										)}
									</div>

									{/* Action buttons per stage */}
									<div className="flex items-center gap-2">
										{/* Reorder actions */}
										<div className="flex items-center rounded border border-foreground/10 bg-background/50">
											<Button
												variant="ghost"
												size="icon"
												className="size-7 rounded-none text-muted-foreground hover:text-foreground"
												onClick={() => void handleReorderStage(idx, "up")}
												disabled={idx === 0 || isMissionActiveInSessions}
												title="Subir etapa"
											>
												<ArrowUp className="size-3.5" />
											</Button>
											<Button
												variant="ghost"
												size="icon"
												className="size-7 rounded-none border-foreground/10 border-l text-muted-foreground hover:text-foreground"
												onClick={() => void handleReorderStage(idx, "down")}
												disabled={
													idx === stages.length - 1 || isMissionActiveInSessions
												}
												title="Bajar etapa"
											>
												<ArrowDown className="size-3.5" />
											</Button>
										</div>

										{/* Edit stage */}
										<Button
											variant="outline"
											size="xs"
											className="h-7 gap-1.5 text-xs"
											onClick={() => handleStartEditStage(stage)}
											disabled={
												isStageActive || isAddingStage || editStageId !== null
											}
											title={
												isStageActive
													? "Esta etapa tiene sesiones activas y no se puede editar"
													: ""
											}
										>
											<Edit className="size-3" />
											Editar
										</Button>

										{/* Delete stage */}
										<Button
											variant="ghost"
											size="icon"
											className="size-7 text-muted-foreground hover:bg-destructive/5 hover:text-destructive"
											onClick={() => void handleDeleteStage(stage.id)}
											disabled={
												isStageActive ||
												isAddingStage ||
												editStageId !== null ||
												loadingOperations.deletingStageId !== null
											}
											title={
												isStageActive
													? "Esta etapa tiene sesiones activas y no se puede eliminar"
													: "Eliminar etapa"
											}
										>
											{loadingOperations.deletingStageId === stage.id ? (
												<Loader2 className="size-3 animate-spin" />
											) : (
												<Trash className="size-3.5" />
											)}
										</Button>
									</div>
								</div>

								{/* Substages cards container */}
								{stage.subStages.length === 0 ? (
									<p className="py-2 text-muted-foreground text-xs italic">
										Esta etapa no contiene actividades (subetapas) configuradas.
									</p>
								) : (
									<div className="grid gap-4 sm:grid-cols-2">
										{stage.subStages.map((sub) => (
											<SubStageCard key={sub.id} sub={sub} />
										))}
									</div>
								)}
							</section>
						);
					})}
				</div>
			</div>
		</div>
	);
}

export default function MissionDetailPage() {
	return (
		<RoleGuard requiredRoles={["admin", "operator"]}>
			<MissionDetailPanelInner />
		</RoleGuard>
	);
}
