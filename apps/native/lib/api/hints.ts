import { env } from "@umbral/env/native";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface ReleasedHint {
	id: string;
	teamId: string;
	subStageId: string;
	clueId: string;
	text: string;
	order: number;
	releasedAt: string;
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

// El jugador solicita una pista para su reto actual.
export async function requestHint(
	sessionId: string,
	teamId: string,
	subStageId: string,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.EXPO_PUBLIC_SERVER_URL}/api/sessions/${sessionId}/teams/${teamId}/hint-requests`,
		{
			method: "POST",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify({ subStageId }),
		},
	);

	await handleResponse(res);
}

// Pistas ya liberadas al equipo (para sobrevivir recargas / reconexión).
export async function getTeamHints(
	sessionId: string,
	teamId: string,
	token: string,
): Promise<ReleasedHint[]> {
	const res = await fetch(
		`${env.EXPO_PUBLIC_SERVER_URL}/api/sessions/${sessionId}/teams/${teamId}/hints`,
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

	return res.json() as Promise<ReleasedHint[]>;
}
