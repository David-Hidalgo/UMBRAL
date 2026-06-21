import { env } from "@umbral/env/web";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface PlayerDetail {
	id: string;
	keycloakUserId: string;
	displayName: string;
	joinedAt: string;
}

export interface TeamDetail {
	id: string;
	name: string;
	players: PlayerDetail[];
}

export interface LiveSessionDetail {
	id: string;
	missionId: string;
	missionName: string;
	name: string;
	scheduledAt: string | null;
	currentStageIndex: number;
	status:
		| "Programada"
		| "Preparacion"
		| "Activa"
		| "Pausada"
		| "Finalizada"
		| "Cancelada";
	teams: TeamDetail[];
}

export interface LiveSessionSummary {
	id: string;
	missionId: string;
	missionName: string;
	name: string;
	scheduledAt: string | null;
	status:
		| "Programada"
		| "Preparacion"
		| "Activa"
		| "Pausada"
		| "Finalizada"
		| "Cancelada";
	teamCount: number;
	playerCount: number;
}

// ─── Payload Types for Writing ────────────────────────────────────────────────

export interface CreateLiveSessionRequest {
	missionId: string;
	name: string;
	scheduledAt?: string | null;
}

export interface TransitionStatusRequest {
	targetStatus: string;
}

export interface CreateTeamRequest {
	teamName: string;
}

export interface AdvanceStageRequest {
	nextStageIndex: number;
}

// ─── Error ────────────────────────────────────────────────────────────────────

export class SessionApiError extends Error {
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
		this.name = "SessionApiError";
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
		throw new SessionApiError(
			"Sesión expirada. Por favor, inicia sesión de nuevo.",
			"unauthorized",
		);
	}
	if (res.status === 404) {
		throw new SessionApiError("No encontrado.", "not_found");
	}
	if (res.status === 400) {
		throw new SessionApiError(errorMsg, "validation");
	}
	if (res.status === 409) {
		throw new SessionApiError(errorMsg, "conflict");
	}
	if (res.status === 422) {
		throw new SessionApiError(errorMsg, "unprocessable");
	}
	throw new SessionApiError(errorMsg, "server");
}

export async function createSession(
	data: CreateLiveSessionRequest,
	token: string,
): Promise<string> {
	const res = await fetch(`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions`, {
		method: "POST",
		credentials: "omit",
		headers: {
			"Content-Type": "application/json",
			Authorization: `Bearer ${token}`,
		},
		body: JSON.stringify({
			missionId: data.missionId,
			name: data.name,
			scheduledAt: data.scheduledAt || null,
		}),
	});

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<string>;
}

export async function getAllSessions(
	token: string,
	params: { missionId?: string; status?: string } = {},
): Promise<LiveSessionSummary[]> {
	const query = new URLSearchParams();
	if (params.missionId) query.set("missionId", params.missionId);
	if (params.status) query.set("status", params.status);

	const qs = query.toString() ? `?${query.toString()}` : "";
	const res = await fetch(`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions${qs}`, {
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

	return res.json() as Promise<LiveSessionSummary[]>;
}

export async function getSessionById(
	id: string,
	token: string,
): Promise<LiveSessionDetail> {
	const res = await fetch(`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions/${id}`, {
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

	return res.json() as Promise<LiveSessionDetail>;
}

export async function transitionSessionStatus(
	id: string,
	targetStatus: string,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions/${id}/status`,
		{
			method: "PATCH",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify({ targetStatus }),
		},
	);

	await handleResponse(res);
}

export async function createTeam(
	id: string,
	teamName: string,
	token: string,
): Promise<string> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions/${id}/teams`,
		{
			method: "POST",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify({ teamName }),
		},
	);

	if (!res.ok) {
		await handleResponse(res);
	}

	return res.json() as Promise<string>;
}

export async function joinTeam(
	id: string,
	teamId: string,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions/${id}/teams/${teamId}/join`,
		{
			method: "POST",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
		},
	);

	await handleResponse(res);
}

export async function advanceStage(
	id: string,
	nextStageIndex: number,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions/${id}/advance`,
		{
			method: "POST",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify({ nextStageIndex }),
		},
	);

	await handleResponse(res);
}
