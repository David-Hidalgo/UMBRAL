import { env } from "@umbral/env/web";

export type AppRole = "admin" | "operator" | "player";

export interface UpdateUserPayload {
	name: string;
	role: AppRole;
}

/** Errores tipados que la UI puede discriminar para mostrar mensajes precisos. */
export class UpdateUserError extends Error {
	constructor(
		message: string,
		public readonly code:
			| "validation"
			| "forbidden"
			| "not_found"
			| "unauthorized"
			| "server",
	) {
		super(message);
		this.name = "UpdateUserError";
	}
}

/**
 * PUT /api/users/{keycloakUserId}
 * Resuelve en void (204) o lanza UpdateUserError con el código apropiado.
 */
export async function updateUser(
	keycloakUserId: string,
	payload: UpdateUserPayload,
	token: string,
): Promise<void> {
	const res = await fetch(
		`${env.NEXT_PUBLIC_SERVER_URL}/api/users/${keycloakUserId}`,
		{
			method: "PUT",
			credentials: "omit",
			headers: {
				"Content-Type": "application/json",
				Authorization: `Bearer ${token}`,
			},
			body: JSON.stringify(payload),
		},
	);

	if (res.status === 204) return;

	if (res.status === 400) {
		const text = await res.text().catch(() => "");
		let detail = "Datos de edición inválidos.";
		try {
			const json = JSON.parse(text) as {
				errors?:
					| Record<string, string[]>
					| Array<{ field: string; message: string }>;
			};
			const messages = Array.isArray(json.errors)
				? json.errors.map((e) => e.message)
				: Object.values(json.errors ?? {}).flat();
			if (messages.length > 0) detail = messages.join(" ");
		} catch {
			// no-op: usamos el texto plano
		}
		throw new UpdateUserError(detail, "validation");
	}

	if (res.status === 403)
		throw new UpdateUserError(
			"No tienes permiso para realizar esta acción.",
			"forbidden",
		);

	if (res.status === 404)
		throw new UpdateUserError("El usuario no fue encontrado.", "not_found");

	if (res.status === 401)
		throw new UpdateUserError(
			"Sesión expirada. Por favor, inicia sesión de nuevo.",
			"unauthorized",
		);

	throw new UpdateUserError(
		`Error inesperado del servidor (${res.status}).`,
		"server",
	);
}
