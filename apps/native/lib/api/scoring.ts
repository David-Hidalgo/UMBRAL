import { env } from "@umbral/env/native";

// ─── Types ────────────────────────────────────────────────────────────────────

export type SubmissionType = "Trivia" | "TreasureHunt";

export interface TeamScore {
	sessionId: string;
	teamId: string;
	teamCode: string;
	totalPoints: number;
}

// Fila del ranking final de la sesión (RF-12), ordenado según RB-08.
export interface RankingEntry {
	rank: number;
	teamId: string;
	teamCode: string;
	totalPoints: number;
	lastScoredAt: string | null;
}

export interface SubmitEvidenceResult {
	submissionId: string;
	status: "Pending" | "Correct" | "Incorrect";
	awardedPoints: number;
	teamTotalPoints: number;
}

export interface SubmitEvidenceRequest {
	type: SubmissionType;
	subStageId: string;
	payload: string;
}

// Vista segura del reto (sin respuestas correctas ni código secreto).
export interface PlayerAnswer {
	id: string;
	text: string;
}

export interface PlayerQuestion {
	id: string;
	text: string;
	answers: PlayerAnswer[];
}

export interface PlayerSubStage {
	id: string;
	title: string;
	points: number;
	type: SubmissionType;
	question: PlayerQuestion | null;
	objective: string | null;
	// El jugador ya no puede enviar: Trivia = ya respondió (1 intento); Búsqueda = ya resolvió.
	closed: boolean;
	// Ubicación del tesoro (solo Búsqueda del Tesoro): guía visual en el mapa.
	latitude: number | null;
	longitude: number | null;
	searchRadiusMeters: number | null;
}

export interface PlayerChallenge {
	sessionId: string;
	status: string;
	subStages: PlayerSubStage[];
}

// ─── Error ────────────────────────────────────────────────────────────────────

export class ScoringApiError extends Error {
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
		this.name = "ScoringApiError";
	}
}

// ─── API functions ─────────────────────────────────────────────────────────────

async function handleResponse(res: Response): Promise<void> {
	if (res.ok) return;

	let errorMsg = `Error del servidor (${res.status})`;
	try {
		const json = await res.json();
		// Orden: primero el mensaje específico (validación de campo), luego el
		// detalle/título de Problem Details (409/500), y por último genéricos.
		errorMsg =
			json.error ||
			json.errors?.[0]?.message ||
			json.detail ||
			json.title ||
			json.message ||
			errorMsg;
	} catch {}

	if (res.status === 401) {
		throw new ScoringApiError(
			"Sesión expirada. Por favor, inicia sesión de nuevo.",
			"unauthorized",
		);
	}
	if (res.status === 404) {
		throw new ScoringApiError("No encontrado.", "not_found");
	}
	if (res.status === 400) {
		throw new ScoringApiError(errorMsg, "validation");
	}
	if (res.status === 409) {
		throw new ScoringApiError(errorMsg, "conflict");
	}
	if (res.status === 422) {
		throw new ScoringApiError(errorMsg, "unprocessable");
	}
	throw new ScoringApiError(errorMsg, "server");
}

export async function submitEvidence(
	sessionId: string,
	teamId: string,
	data: SubmitEvidenceRequest,
	token: string,
): Promise<SubmitEvidenceResult> {
	const res = await fetch(
		`${env.EXPO_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/teams/${teamId}/submissions`,
		{
			method: "POST",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify(data),
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<SubmitEvidenceResult>;
}

export async function getTeamScore(
	sessionId: string,
	teamId: string,
	token: string,
): Promise<TeamScore> {
	const res = await fetch(
		`${env.EXPO_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/teams/${teamId}/score`,
		{
			method: "GET",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<TeamScore>;
}

export async function getRanking(
	sessionId: string,
	token: string,
): Promise<RankingEntry[]> {
	const res = await fetch(
		`${env.EXPO_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/ranking`,
		{
			method: "GET",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<RankingEntry[]>;
}

export async function getCurrentChallenge(
	sessionId: string,
	teamId: string,
	token: string,
): Promise<PlayerChallenge> {
	const res = await fetch(
		`${env.EXPO_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/current-challenge?teamId=${teamId}`,
		{
			method: "GET",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<PlayerChallenge>;
}
