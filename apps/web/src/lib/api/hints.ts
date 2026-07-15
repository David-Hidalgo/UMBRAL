import { env } from "@umbral/env/web";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface HintRequest {
	id: string;
	teamId: string;
	teamName: string;
	subStageId: string;
	status: "Pendiente" | "Resuelta";
	requestedAt: string;
	resolvedAt: string | null;
}

export interface ReleasedHint {
	id: string;
	teamId: string;
	subStageId: string;
	clueId: string;
	text: string;
	order: number;
	releasedAt: string;
}

// ─── Payload Types for Writing ────────────────────────────────────────────────

export interface GrantHintRequest {
	subStageId: string;
	clueId: string;
}

// ─── Error ────────────────────────────────────────────────────────────────────

export class HintApiError extends Error {
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
		this.name = "HintApiError";
	}
}

// ─── API functions ─────────────────────────────────────────────────────────────

async function handleResponse(res: Response): Promise<void> {
	if (res.ok) return;

	let errorMsg = `Error del servidor (${res.status})`;
	try {
		const json = await res.json();
		errorMsg =
			json.error ||
			json.errors?.[0]?.message ||
			json.detail ||
			json.title ||
			json.message ||
			errorMsg;
	} catch {}

	if (res.status === 401) {
		throw new HintApiError(
			"Sesión expirada. Por favor, inicia sesión de nuevo.",
			"unauthorized",
		);
	}
	if (res.status === 404) {
		throw new HintApiError("No encontrado.", "not_found");
	}
	if (res.status === 400) {
		throw new HintApiError(errorMsg, "validation");
	}
	if (res.status === 409) {
		throw new HintApiError(errorMsg, "conflict");
	}
	if (res.status === 422) {
		throw new HintApiError(errorMsg, "unprocessable");
	}
	throw new HintApiError(errorMsg, "server");
}

// Solicitudes de pista de una sesión (para el operador). Filtrable por estado.
export async function getHintRequests(
	sessionId: string,
	token: string,
	status?: "Pendiente" | "Resuelta",
): Promise<HintRequest[]> {
	const query = status ? `?status=${status}` : "";
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions/${sessionId}/hint-requests${query}`,
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

	return res.json() as Promise<HintRequest[]>;
}

// Pistas ya liberadas a un equipo (para no ofrecer repetidas — RB-04).
export async function getTeamHints(
	sessionId: string,
	teamId: string,
	token: string,
): Promise<ReleasedHint[]> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions/${sessionId}/teams/${teamId}/hints`,
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

	return res.json() as Promise<ReleasedHint[]>;
}

// El operador libera una pista concreta a un equipo.
export async function grantHint(
	sessionId: string,
	teamId: string,
	data: GrantHintRequest,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/sessions/${sessionId}/teams/${teamId}/hints`,
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
