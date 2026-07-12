import { env } from "@umbral/env/web";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface RankingEntry {
	rank: number;
	teamId: string;
	teamCode: string;
	totalPoints: number;
	lastScoredAt: string | null;
}

export interface TeamScore {
	sessionId: string;
	teamId: string;
	teamCode: string;
	totalPoints: number;
}

export interface SessionEvent {
	id: string;
	sessionId: string;
	type: string;
	description: string;
	actor: string | null;
	occurredAt: string;
}

export interface SubmitEvidenceResult {
	submissionId: string;
	status: "Pending" | "Correct" | "Incorrect";
	awardedPoints: number;
	teamTotalPoints: number;
}

export interface TeamStageProgress {
	teamId: string;
	teamCode: string;
	completed: number;
}

export interface StageProgress {
	subStageCount: number;
	teams: TeamStageProgress[];
	allComplete: boolean;
}

// ─── Payload Types for Writing ────────────────────────────────────────────────

export interface ApplyPenaltyRequest {
	points: number;
	reason: string;
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

export async function getRanking(
	sessionId: string,
	token: string,
): Promise<RankingEntry[]> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/ranking`,
		{
			method: "GET",
			credentials: "omit",
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

export async function getStageProgress(
	sessionId: string,
	token: string,
): Promise<StageProgress> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/stage-progress`,
		{
			method: "GET",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<StageProgress>;
}

export async function getSessionEvents(
	sessionId: string,
	token: string,
): Promise<SessionEvent[]> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/events`,
		{
			method: "GET",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<SessionEvent[]>;
}

export async function applyPenalty(
	sessionId: string,
	teamId: string,
	data: ApplyPenaltyRequest,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/teams/${teamId}/penalties`,
		{
			method: "POST",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify(data),
		},
	);

	await handleResponse(res);
}

export async function validateEvidence(
	sessionId: string,
	submissionId: string,
	isCorrect: boolean,
	token: string,
): Promise<SubmitEvidenceResult> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/scoring/sessions/${sessionId}/submissions/${submissionId}/validate`,
		{
			method: "PATCH",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify({ isCorrect }),
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<SubmitEvidenceResult>;
}
