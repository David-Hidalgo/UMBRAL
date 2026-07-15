import { env } from "@umbral/env/native";

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
	maxTimeMinutes: number;
	endsAt: string | null;
	remainingSecondsOnPause: number | null;
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

export async function getSessionById(
	id: string,
	token: string,
): Promise<LiveSessionDetail> {
	const res = await fetch(`${env.EXPO_PUBLIC_SERVER_URL}/api/sessions/${id}`, {
		method: "GET",
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

export async function joinTeam(
	id: string,
	teamId: string,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.EXPO_PUBLIC_SERVER_URL}/api/sessions/${id}/teams/${teamId}/join`,
		{
			method: "POST",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
		},
	);

	await handleResponse(res);
}

export async function getAllSessions(
	token: string,
	params: { status?: string } = {},
): Promise<LiveSessionSummary[]> {
	const query = new URLSearchParams();
	if (params.status) query.set("status", params.status);

	const qs = query.toString() ? `?${query.toString()}` : "";
	const res = await fetch(`${env.EXPO_PUBLIC_SERVER_URL}/api/sessions${qs}`, {
		method: "GET",
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
