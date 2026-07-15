import { env } from "@umbral/env/web";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface MissionDetail {
	id: string;
	name: string;
	description: string;
	state: "Active" | "Inactive";
	activeSessionCount: number;
	difficultyLevel: "Beginner" | "Intermediate" | "Advanced";
	maxTimeMinutes: number;
	stages: StageDetail[];
}

export interface StageDetail {
	id: string;
	name: string;
	description: string;
	order: number;
	activeSessionCount: number;
	subStages: SubStageDetail[];
}

export interface SubStageDetail {
	id: string;
	title: string;
	order: number;
	points: number;
	activeSessionCount: number;
	question: QuestionDetail | null;
	objective: string | null;
	secretCode: string | null;
	clues: ClueDetail[] | null;
	latitude: number | null;
	longitude: number | null;
	searchRadiusMeters: number | null;
}

export interface QuestionDetail {
	id: string;
	text: string;
	answers: AnswerDetail[];
}

export interface AnswerDetail {
	id: string;
	text: string;
	isCorrect: boolean;
}

export interface ClueDetail {
	id: string;
	text: string;
	order: number;
}

// ─── Payload Types for Writing ────────────────────────────────────────────────

export interface UpdateMissionRequest {
	name?: string;
	description?: string;
	state?: "Active" | "Inactive";
	difficultyLevel?: "Beginner" | "Intermediate" | "Advanced";
	maxTimeMinutes?: number;
}

export interface CreateStageRequest {
	name: string;
	description: string;
	order: number;
	subStages: CreateSubStageRequest[];
}

export interface CreateSubStageRequest {
	id?: string;
	title: string;
	order: number;
	type: "Trivia" | "TreasureHunt";
	points: number;
	question?: CreateQuestionRequest | null;
	objective?: string | null;
	secretCode?: string | null;
	clues?: CreateClueRequest[] | null;
	latitude?: number | null;
	longitude?: number | null;
	searchRadiusMeters?: number | null;
}

export interface CreateQuestionRequest {
	id?: string;
	text: string;
	answers: CreateAnswerRequest[];
}

export interface CreateAnswerRequest {
	id?: string;
	text: string;
	isCorrect: boolean;
}

export interface CreateClueRequest {
	id?: string;
	text: string;
	order: number;
}

// ─── Error ────────────────────────────────────────────────────────────────────

export class MissionApiError extends Error {
	constructor(
		message: string,
		public readonly code:
			| "not_found"
			| "unauthorized"
			| "validation"
			| "conflict"
			| "unprocessable"
			| "server",
	) {
		super(message);
		this.name = "MissionApiError";
	}
}

// ─── API functions ─────────────────────────────────────────────────────────────

async function handleResponse(res: Response): Promise<void> {
	if (res.ok) return;

	let errorMsg = `Error del servidor (${res.status})`;
	try {
		const json = await res.json();
		errorMsg =
			json.error || json.errors?.[0]?.message || json.message || errorMsg;
	} catch {}

	if (res.status === 401) {
		throw new MissionApiError(
			"Sesión expirada. Por favor, inicia sesión de nuevo.",
			"unauthorized",
		);
	}
	if (res.status === 404) {
		throw new MissionApiError("No encontrado.", "not_found");
	}
	if (res.status === 400) {
		throw new MissionApiError(errorMsg, "validation");
	}
	if (res.status === 409) {
		throw new MissionApiError(errorMsg, "conflict");
	}
	if (res.status === 422) {
		throw new MissionApiError(errorMsg, "unprocessable");
	}
	throw new MissionApiError(errorMsg, "server");
}

export async function getMissionById(
	id: string,
	token: string,
): Promise<MissionDetail> {
	const res = await fetch(`${env.NEXT_PUBLIC_SERVER_URL}/api/missions/${id}`, {
		method: "GET",
		credentials: "omit",
		headers: {
			"Content-Type": "application/json",
			Authorization: `Bearer ${token}`,
		},
	});

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<MissionDetail>;
}

export async function updateMission(
	id: string,
	data: UpdateMissionRequest,
	token: string,
): Promise<void> {
	const res = await fetch(`${env.NEXT_PUBLIC_SERVER_URL}/api/missions/${id}`, {
		method: "PATCH",
		credentials: "omit",
		headers: {
			"Content-Type": "application/json",
			Authorization: `Bearer ${token}`,
		},
		body: JSON.stringify(data),
	});

	await handleResponse(res);
}

export async function deleteMission(id: string, token: string): Promise<void> {
	const res = await fetch(`${env.NEXT_PUBLIC_SERVER_URL}/api/missions/${id}`, {
		method: "DELETE",
		credentials: "omit",
		headers: {
			"Content-Type": "application/json",
			Authorization: `Bearer ${token}`,
		},
	});

	await handleResponse(res);
}

export async function addStage(
	missionId: string,
	stage: CreateStageRequest,
	token: string,
): Promise<{ id: string }> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/missions/${missionId}/stages`,
		{
			method: "POST",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify(stage),
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<{ id: string }>;
}

export async function updateStage(
	missionId: string,
	stageId: string,
	stage: CreateStageRequest,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/missions/${missionId}/stages/${stageId}`,
		{
			method: "PUT",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify(stage),
		},
	);

	await handleResponse(res);
}

export async function deleteStage(
	missionId: string,
	stageId: string,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/missions/${missionId}/stages/${stageId}`,
		{
			method: "DELETE",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
		},
	);

	await handleResponse(res);
}

export async function reorderStages(
	missionId: string,
	stageIds: string[],
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/missions/${missionId}/stages/reorder`,
		{
			method: "PUT",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify(stageIds),
		},
	);

	await handleResponse(res);
}
