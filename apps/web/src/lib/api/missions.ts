import { env } from "@umbral/env/web";

// ─── Types ────────────────────────────────────────────────────────────────────

export interface MissionDetail {
	id: string;
	name: string;
	description: string;
	status: "Active" | "Inactive";
	stages: StageDetail[];
}

export interface StageDetail {
	id: string;
	name: string;
	description: string;
	order: number;
	subStages: SubStageDetail[];
}

export interface SubStageDetail {
	id: string;
	title: string;
	order: number;
	points: number;
	question: QuestionDetail | null;
	objective: string | null;
	secretCode: string | null;
	clues: ClueDetail[] | null;
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

// ─── Error ────────────────────────────────────────────────────────────────────

export class GetMissionError extends Error {
	constructor(
		message: string,
		public readonly code: "not_found" | "unauthorized" | "server",
	) {
		super(message);
		this.name = "GetMissionError";
	}
}

// ─── API function ─────────────────────────────────────────────────────────────

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

	if (res.status === 401)
		throw new GetMissionError(
			"Sesión expirada. Por favor, inicia sesión de nuevo.",
			"unauthorized",
		);

	if (res.status === 404)
		throw new GetMissionError("La misión no fue encontrada.", "not_found");

	if (!res.ok)
		throw new GetMissionError(
			`Error inesperado del servidor (${res.status}).`,
			"server",
		);

	return res.json() as Promise<MissionDetail>;
}
