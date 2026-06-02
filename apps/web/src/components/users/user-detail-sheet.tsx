"use client";

import { Button } from "@umbral/ui/components/button";
import { Copy, Pencil, Trash2, X } from "lucide-react";
import { useEffect, useRef } from "react";
import { toast } from "sonner";

export type AppRole = "admin" | "operator" | "player";

export interface UserDetailData {
	id: string;
	keycloakUserId: string;
	name: string;
	email: string;
	role: AppRole;
	createdAt: string; // ISO string
}

const ROLE_LABELS: Record<AppRole, string> = {
	admin: "Administrador",
	operator: "Operador",
	player: "Jugador",
};

const ROLE_COLORS: Record<AppRole, string> = {
	admin: "bg-violet-500/15 text-violet-400 ring-1 ring-violet-500/25",
	operator: "bg-cyan-500/15 text-cyan-400 ring-1 ring-cyan-500/25",
	player: "bg-emerald-500/15 text-emerald-400 ring-1 ring-emerald-500/25",
};

interface UserDetailSheetProps {
	user: UserDetailData | null;
	onClose: () => void;
}

function formatDate(iso: string): string {
	try {
		return new Intl.DateTimeFormat("es-VE", {
			day: "numeric",
			month: "long",
			year: "numeric",
			hour: "2-digit",
			minute: "2-digit",
			timeZoneName: "short",
		}).format(new Date(iso));
	} catch {
		return iso;
	}
}

async function copyToClipboard(text: string, label: string) {
	try {
		await navigator.clipboard.writeText(text);
		toast.success(`${label} copiado al portapapeles.`);
	} catch {
		toast.error("No se pudo copiar al portapapeles.");
	}
}

export function UserDetailSheet({ user, onClose }: UserDetailSheetProps) {
	const panelRef = useRef<HTMLDivElement>(null);
	const isOpen = user !== null;

	// Close on Escape key
	useEffect(() => {
		if (!isOpen) return;
		const handler = (e: KeyboardEvent) => {
			if (e.key === "Escape") onClose();
		};
		document.addEventListener("keydown", handler);
		return () => document.removeEventListener("keydown", handler);
	}, [isOpen, onClose]);

	// Focus panel container when open (improves keyboard navigation)
	useEffect(() => {
		if (isOpen && panelRef.current) {
			panelRef.current.focus();
		}
	}, [isOpen]);

	return (
		<>
			{/* Backdrop */}
			<div
				aria-hidden="true"
				className={[
					"fixed inset-0 z-40 bg-black/40 backdrop-blur-sm transition-opacity duration-300",
					isOpen ? "opacity-100" : "pointer-events-none opacity-0",
				].join(" ")}
				onClick={onClose}
			/>

			{/* Slide-over panel */}
			<div
				ref={panelRef}
				role="dialog"
				aria-modal="true"
				aria-label="Detalle del usuario"
				tabIndex={-1}
				className={[
					"fixed inset-y-0 right-0 z-50 flex w-full max-w-md flex-col bg-sidebar shadow-2xl ring-1 ring-foreground/10 transition-transform duration-300 focus:outline-none",
					isOpen ? "translate-x-0" : "translate-x-full",
				].join(" ")}
			>
				{/* Header */}
				<div className="flex items-center justify-between border-b border-foreground/8 px-6 py-4">
					<h2 className="font-semibold text-base text-foreground">
						Detalle del usuario
					</h2>
					<button
						type="button"
						onClick={onClose}
						className="flex size-7 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-foreground/8 hover:text-foreground"
						aria-label="Cerrar panel"
					>
						<X className="size-4" />
					</button>
				</div>

				{user && (
					<div className="flex flex-1 flex-col gap-6 overflow-y-auto px-6 py-6">
						{/* Avatar + name + email */}
						<div className="flex flex-col items-center gap-3 text-center">
							<div className="flex size-16 items-center justify-center rounded-full bg-primary/15 ring-2 ring-primary/25 font-bold text-2xl text-primary">
								{user.name.slice(0, 1).toUpperCase()}
							</div>
							<div>
								<p className="font-semibold text-foreground text-lg leading-tight">
									{user.name}
								</p>
								<p className="mt-0.5 text-muted-foreground text-sm">
									{user.email}
								</p>
							</div>
							<span
								className={`inline-flex items-center rounded-full px-3 py-1 font-medium text-xs ${ROLE_COLORS[user.role]}`}
							>
								{ROLE_LABELS[user.role]}
							</span>
						</div>

						{/* Divider */}
						<div className="h-px bg-foreground/8" />

						{/* Details */}
						<dl className="space-y-4">
							<div>
								<dt className="mb-1 font-medium text-muted-foreground text-xs uppercase tracking-wide">
									Correo electrónico
								</dt>
								<dd className="flex items-center gap-2 text-foreground text-sm">
									<span className="flex-1 truncate">{user.email}</span>
									<button
										type="button"
										onClick={() =>
											void copyToClipboard(user.email, "Correo electrónico")
										}
										className="flex size-6 shrink-0 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-foreground/8 hover:text-foreground"
										aria-label="Copiar correo"
									>
										<Copy className="size-3.5" />
									</button>
								</dd>
							</div>

							<div>
								<dt className="mb-1 font-medium text-muted-foreground text-xs uppercase tracking-wide">
									ID de Keycloak
								</dt>
								<dd className="flex items-center gap-2">
									<code className="flex-1 truncate rounded bg-foreground/6 px-2 py-1 font-mono text-[11px] text-foreground">
										{user.keycloakUserId}
									</code>
									<button
										type="button"
										onClick={() =>
											void copyToClipboard(user.keycloakUserId, "ID de Keycloak")
										}
										className="flex size-6 shrink-0 items-center justify-center rounded text-muted-foreground transition-colors hover:bg-foreground/8 hover:text-foreground"
										aria-label="Copiar ID de Keycloak"
									>
										<Copy className="size-3.5" />
									</button>
								</dd>
							</div>

							<div>
								<dt className="mb-1 font-medium text-muted-foreground text-xs uppercase tracking-wide">
									Fecha de creación
								</dt>
								<dd className="text-foreground text-sm">
									{formatDate(user.createdAt)}
								</dd>
							</div>
						</dl>

						{/* Divider */}
						<div className="h-px bg-foreground/8" />

						{/* Actions (disabled — future feature) */}
						<div className="space-y-2">
							<p className="font-medium text-muted-foreground text-xs uppercase tracking-wide">
								Acciones
							</p>
							<div className="flex gap-2">
								<Button
									variant="outline"
									size="sm"
									className="flex-1 gap-2 opacity-50"
									disabled
									title="Próximamente"
								>
									<Pencil className="size-3.5" />
									Editar
								</Button>
								<Button
									variant="outline"
									size="sm"
									className="flex-1 gap-2 text-destructive opacity-50 hover:text-destructive"
									disabled
									title="Próximamente"
								>
									<Trash2 className="size-3.5" />
									Eliminar
								</Button>
							</div>
							<p className="text-center text-[11px] text-muted-foreground/60">
								Estas acciones estarán disponibles próximamente.
							</p>
						</div>
					</div>
				)}
			</div>
		</>
	);
}
