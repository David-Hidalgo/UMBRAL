"use client";

import { env } from "@umbral/env/web";
import { Button } from "@umbral/ui/components/button";
import { Input } from "@umbral/ui/components/input";
import { Label } from "@umbral/ui/components/label";
import { Copy, Loader2, Pencil, RefreshCw, Save, Trash2, X } from "lucide-react";
import { useEffect, useRef, useState } from "react";
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
	/** Si el usuario autenticado puede editar este perfil. */
	canEdit: boolean;
	/** Si el usuario autenticado es admin (puede cambiar el rol). */
	isAdmin: boolean;
	onClose: () => void;
	onDeleteSuccess: () => void;
	getAccessToken: (minValidity?: number) => Promise<string | null>;
	onSaveEdit: (
		keycloakUserId: string,
		name: string,
		role: AppRole,
	) => Promise<void>;
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

export function UserDetailSheet({
	user,
	canEdit,
	isAdmin,
	onClose,
	onDeleteSuccess,
	getAccessToken,
	onSaveEdit,
}: UserDetailSheetProps) {
	const panelRef = useRef<HTMLDivElement>(null);
	const isOpen = user !== null;
	const [isDeleting, setIsDeleting] = useState(false);
	const [showConfirm, setShowConfirm] = useState(false);

	async function handleDelete() {
		if (!user) return;
		setIsDeleting(true);
		try {
			const token = await getAccessToken(60);
			if (!token) {
				toast.error("No hay sesión activa.");
				return;
			}
			const response = await fetch(
				`${env.NEXT_PUBLIC_SERVER_URL}/api/users/${user.keycloakUserId}`,
				{
					method: "DELETE",
					credentials: "omit",
					headers: { Authorization: `Bearer ${token}` },
				},
			);
			if (response.status === 204) {
				toast.success(`${user.name} ha sido desactivado.`);
				onDeleteSuccess();
			} else if (response.status === 404) {
				toast.error("El usuario no fue encontrado.");
			} else if (response.status === 409) {
				toast.error("El usuario ya estaba inactivo.");
			} else {
				toast.error(`Error inesperado (${response.status}).`);
			}
		} catch {
			toast.error("No se pudo conectar con el servidor.");
		} finally {
			setIsDeleting(false);
			setShowConfirm(false);
		}
	}

	useEffect(() => {
		setShowConfirm(false);
	}, [user?.keycloakUserId]);

	const [editMode, setEditMode] = useState(false);
	const [editName, setEditName] = useState("");
	const [editRole, setEditRole] = useState<AppRole>("player");
	const [isSubmitting, setIsSubmitting] = useState(false);

	// Sincroniza el formulario cada vez que se abre un usuario distinto
	useEffect(() => {
		if (user) {
			setEditName(user.name);
			setEditRole(user.role);
			setEditMode(false);
		}
	}, [user]);

	// Cierra el modo edición al cerrar el panel
	useEffect(() => {
		if (!isOpen) setEditMode(false);
	}, [isOpen]);

	// Cierra con Escape (sale del modo edición primero si estaba activo)
	useEffect(() => {
		if (!isOpen) return;
		const handler = (e: KeyboardEvent) => {
			if (e.key === "Escape") {
				if (editMode) {
					setEditMode(false);
				} else {
					onClose();
				}
			}
		};
		document.addEventListener("keydown", handler);
		return () => document.removeEventListener("keydown", handler);
	}, [isOpen, editMode, onClose]);

	useEffect(() => {
		if (isOpen && panelRef.current) panelRef.current.focus();
	}, [isOpen]);

	const handleSave = async () => {
		if (!user) return;

		const trimmedName = editName.trim();
		if (!trimmedName) {
			toast.error("El nombre no puede estar vacío.");
			return;
		}
		if (trimmedName.length > 200) {
			toast.error("El nombre no puede superar los 200 caracteres.");
			return;
		}

		setIsSubmitting(true);
		try {
			await onSaveEdit(user.keycloakUserId, trimmedName, editRole);
			// El padre cierra el sheet y refresca la lista al resolver.
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "No se pudo guardar los cambios.",
			);
		} finally {
			setIsSubmitting(false);
		}
	};

	return (
		<>
			{/* Backdrop */}
			<div
				aria-hidden="true"
				className={[
					"fixed inset-0 z-40 bg-black/40 backdrop-blur-sm transition-opacity duration-300",
					isOpen ? "opacity-100" : "pointer-events-none opacity-0",
				].join(" ")}
				onClick={editMode ? undefined : onClose}
			/>

			{/* Slide-over panel */}
			<div
				ref={panelRef}
				role="dialog"
				aria-modal="true"
				aria-label={editMode ? "Editar usuario" : "Detalle del usuario"}
				tabIndex={-1}
				className={[
					"fixed inset-y-0 right-0 z-50 flex w-full max-w-md flex-col bg-sidebar shadow-2xl ring-1 ring-foreground/10 transition-transform duration-300 focus:outline-none",
					isOpen ? "translate-x-0" : "translate-x-full",
				].join(" ")}
			>
				{/* Header */}
				<div className="flex items-center justify-between border-foreground/8 border-b px-6 py-4">
					<h2 className="font-semibold text-base text-foreground">
						{editMode ? "Editar usuario" : "Detalle del usuario"}
					</h2>
					<button
						type="button"
						onClick={editMode ? () => setEditMode(false) : onClose}
						disabled={isSubmitting}
						className="flex size-7 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-foreground/8 hover:text-foreground disabled:pointer-events-none disabled:opacity-50"
						aria-label={editMode ? "Cancelar edición" : "Cerrar panel"}
					>
						<X className="size-4" />
					</button>
				</div>

				{user && (
					<div className="flex flex-1 flex-col gap-6 overflow-y-auto px-6 py-6">
						{/* ── MODO VISTA ─────────────────────────────────────────────── */}
						{!editMode && (
							<>
								{/* Avatar + name + email */}
								<div className="flex flex-col items-center gap-3 text-center">
									<div className="flex size-16 items-center justify-center rounded-full bg-primary/15 font-bold text-2xl text-primary ring-2 ring-primary/25">
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

								<div className="h-px bg-foreground/8" />

								{/* Detalles */}
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
													void copyToClipboard(
														user.keycloakUserId,
														"ID de Keycloak",
													)
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

								<div className="h-px bg-foreground/8" />

								{/* Acciones */}
								<div className="space-y-2">
									<p className="font-medium text-muted-foreground text-xs uppercase tracking-wide">
										Acciones
									</p>
									{showConfirm ? (
										<div className="space-y-3 rounded-lg border border-destructive/20 bg-destructive/5 px-4 py-3">
											<p className="text-foreground text-sm">
												¿Desactivar a{" "}
												<span className="font-medium">{user.name}</span>? El usuario
												no podrá iniciar sesión.
											</p>
											<div className="flex gap-2">
												<Button
													variant="outline"
													size="sm"
													className="flex-1"
													disabled={isDeleting}
													onClick={() => setShowConfirm(false)}
												>
													Cancelar
												</Button>
												<Button
													variant="destructive"
													size="sm"
													className="flex-1 gap-2"
													disabled={isDeleting}
													onClick={() => void handleDelete()}
												>
													{isDeleting ? (
														<RefreshCw className="size-3.5 animate-spin" />
													) : (
														<Trash2 className="size-3.5" />
													)}
													Confirmar eliminación
												</Button>
											</div>
										</div>
									) : (
										<div className="flex gap-2">
											<Button
												variant="outline"
												size="sm"
												className="flex-1 gap-2"
												disabled={!canEdit}
												title={canEdit ? "Editar usuario" : "Sin permisos"}
												onClick={() => setEditMode(true)}
											>
												<Pencil className="size-3.5" />
												Editar
											</Button>
											<Button
												variant="outline"
												size="sm"
												className="flex-1 gap-2 text-destructive hover:text-destructive"
												onClick={() => setShowConfirm(true)}
											>
												<Trash2 className="size-3.5" />
												Eliminar
											</Button>
										</div>
									)}
									{!canEdit && !showConfirm && (
										<p className="text-center text-[11px] text-muted-foreground/60">
											Solo puedes editar tu propio perfil.
										</p>
									)}
								</div>
							</>
						)}

						{/* ── MODO EDICIÓN ───────────────────────────────────────────── */}
						{editMode && (
							<div className="space-y-5">
								<p className="text-muted-foreground text-sm">
									Modifica los datos del usuario. El correo electrónico no puede
									editarse.
								</p>

								{/* Nombre */}
								<div className="space-y-1.5">
									<Label htmlFor="edit-name">Nombre completo</Label>
									<Input
										id="edit-name"
										value={editName}
										onChange={(e) => setEditName(e.target.value)}
										placeholder="Nombre del usuario"
										maxLength={200}
										disabled={isSubmitting}
										autoComplete="off"
										autoFocus
									/>
									<p className="text-right text-[11px] text-muted-foreground">
										{editName.length}/200
									</p>
								</div>

								{/* Correo (solo lectura) */}
								<div className="space-y-1.5">
									<Label className="text-muted-foreground">
										Correo electrónico
										<span className="ml-2 rounded bg-foreground/8 px-1.5 py-0.5 font-normal text-[10px]">
											No editable
										</span>
									</Label>
									<Input
										value={user.email}
										disabled
										className="cursor-not-allowed"
									/>
								</div>

								{/* Rol — solo visible para admins */}
								{isAdmin && (
									<div className="space-y-1.5">
										<Label htmlFor="edit-role">Rol</Label>
										<select
											id="edit-role"
											value={editRole}
											onChange={(e) => setEditRole(e.target.value as AppRole)}
											disabled={isSubmitting}
											className="flex h-9 w-full appearance-none rounded-md border border-input bg-background px-3 py-1 text-foreground text-sm shadow-sm ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
										>
											<option value="admin">Administrador</option>
											<option value="operator">Operador</option>
											<option value="player">Jugador</option>
										</select>
									</div>
								)}

								{/* Botones */}
								<div className="flex gap-3 pt-2">
									<Button
										type="button"
										variant="outline"
										className="flex-1"
										disabled={isSubmitting}
										onClick={() => setEditMode(false)}
									>
										Cancelar
									</Button>
									<Button
										type="button"
										className="flex-1 gap-2"
										disabled={isSubmitting}
										onClick={() => void handleSave()}
									>
										{isSubmitting ? (
											<>
												<Loader2 className="size-4 animate-spin" />
												Guardando…
											</>
										) : (
											<>
												<Save className="size-3.5" />
												Guardar cambios
											</>
										)}
									</Button>
								</div>
							</div>
						)}
					</div>
				)}
			</div>
		</>
	);
}
