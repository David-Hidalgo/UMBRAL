"use client";

import { env } from "@umbral/env/web";
import { Button } from "@umbral/ui/components/button";
import { Input } from "@umbral/ui/components/input";
import {
	AlertTriangle,
	ChevronDown,
	RefreshCw,
	Search,
	UserPlus,
	Users,
} from "lucide-react";
import type { Route } from "next";
import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/components/auth-provider";
import { RoleGuard } from "@/components/role-guard";
import {
	type AppRole,
	type UserDetailData,
	UserDetailSheet,
} from "@/components/users/user-detail-sheet";
import { UserListSkeleton } from "@/components/users/user-list-skeleton";
import { updateUser } from "@/lib/api/users";

// ─── Types ───────────────────────────────────────────────────────────────────

interface ApiUser {
	id: string;
	keycloakUserId: string;
	email: string;
	name: string;
	role: string;
	createdAt: string;
}

type FetchState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "success"; users: ApiUser[] };

// ─── Constants ───────────────────────────────────────────────────────────────

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

const ALL_ROLES: { value: AppRole | ""; label: string }[] = [
	{ value: "", label: "Todos los roles" },
	{ value: "admin", label: "Administrador" },
	{ value: "operator", label: "Operador" },
	{ value: "player", label: "Jugador" },
];

// ─── Sub-components ──────────────────────────────────────────────────────────

function RoleBadge({ role }: { role: string }) {
	const r = role as AppRole;
	return (
		<span
			className={`inline-flex shrink-0 items-center rounded-full px-2.5 py-0.5 font-medium text-[11px] ${ROLE_COLORS[r] ?? "bg-foreground/10 text-foreground/60"}`}
		>
			{ROLE_LABELS[r] ?? role}
		</span>
	);
}

function formatDateShort(iso: string): string {
	try {
		return new Intl.DateTimeFormat("es-VE", {
			day: "numeric",
			month: "short",
			year: "numeric",
		}).format(new Date(iso));
	} catch {
		return "";
	}
}

function toSafeRole(role: string): AppRole {
	return role === "admin" || role === "operator" || role === "player"
		? (role as AppRole)
		: "player";
}

// ─── Main page inner ─────────────────────────────────────────────────────────

function UsersListPageInner() {
	const { getAccessToken, hasRealmRole, getSubjectId } = useAuth();
	const isAdmin = hasRealmRole("admin");
	const subjectId = getSubjectId();

	const [search, setSearch] = useState("");
	const [roleFilter, setRoleFilter] = useState<AppRole | "">("");
	const [state, setState] = useState<FetchState>({ status: "loading" });
	const [selectedUser, setSelectedUser] = useState<UserDetailData | null>(null);

	// Debounce search input
	const debounceRef = useRef<ReturnType<typeof setTimeout> | null>(null);
	const [debouncedSearch, setDebouncedSearch] = useState("");

	useEffect(() => {
		return () => {
			if (debounceRef.current) clearTimeout(debounceRef.current);
		};
	}, []);

	const handleSearchChange = (e: React.ChangeEvent<HTMLInputElement>) => {
		const value = e.target.value;
		setSearch(value);
		if (debounceRef.current) clearTimeout(debounceRef.current);
		debounceRef.current = setTimeout(() => setDebouncedSearch(value), 300);
	};

	const fetchUsers = useCallback(async () => {
		setState({ status: "loading" });
		try {
			const accessToken = await getAccessToken(60);
			if (!accessToken) {
				setState({ status: "error", message: "No hay sesión activa." });
				return;
			}

			const params = new URLSearchParams();
			if (debouncedSearch.trim()) params.set("search", debouncedSearch.trim());
			if (roleFilter) params.set("role", roleFilter);

			const query = params.toString();
			const url = `${env.NEXT_PUBLIC_SERVER_URL}/api/users${query ? `?${query}` : ""}`;

			const response = await fetch(url, {
				method: "GET",
				credentials: "omit",
				headers: {
					"Content-Type": "application/json",
					Authorization: `Bearer ${accessToken}`,
				},
			});

			if (!response.ok) {
				const errorText = await response.text().catch(() => "");
				throw new Error(
					errorText || `Error del servidor (${response.status})`,
				);
			}

			const users = (await response.json()) as ApiUser[];
			setState({ status: "success", users });
		} catch (err) {
			const message =
				err instanceof Error ? err.message : "No se pudo cargar la lista.";
			setState({ status: "error", message });
			toast.error(message);
		}
	}, [getAccessToken, debouncedSearch, roleFilter]);

	useEffect(() => {
		void fetchUsers();
	}, [fetchUsers]);

	const handleViewDetail = (user: ApiUser) => {
		setSelectedUser({
			id: user.id,
			keycloakUserId: user.keycloakUserId,
			name: user.name,
			email: user.email,
			role: toSafeRole(user.role),
			createdAt: user.createdAt,
		});
	};

	/**
	 * Orquesta la llamada HTTP PUT y, en caso de éxito:
	 * - Cierra el sheet.
	 * - Refresca la lista para mostrar los datos actualizados.
	 * Si lanza un error, el sheet lo captura y muestra el toast.
	 */
	const handleSaveEdit = async (
		keycloakUserId: string,
		name: string,
		role: AppRole,
	): Promise<void> => {
		const token = await getAccessToken(60);
		if (!token) throw new Error("No hay sesión activa.");

		await updateUser(keycloakUserId, { name, role }, token);

		toast.success("Usuario actualizado correctamente.");
		setSelectedUser(null);
		void fetchUsers();
	};

	const users = state.status === "success" ? state.users : [];

	const canEditSelected = selectedUser
		? isAdmin || selectedUser.keycloakUserId === subjectId
		: false;

	return (
		<>
			<div className="mx-auto max-w-5xl space-y-6">
				{/* Page header */}
				<div className="flex items-start justify-between gap-4">
					<div>
						<h1 className="font-semibold text-2xl text-foreground tracking-tight">
							Usuarios
						</h1>
						<p className="mt-1 text-muted-foreground text-sm">
							Gestiona los usuarios registrados en el realm de Keycloak.
						</p>
					</div>
					<Link href={"/dashboard/usuarios/nuevo" as Route}>
						<Button className="gap-2 shrink-0">
							<UserPlus className="size-4" />
							Nuevo usuario
						</Button>
					</Link>
				</div>

				{/* Filters */}
				<div className="flex flex-col gap-3 sm:flex-row">
					<div className="relative flex-1">
						<Search className="pointer-events-none absolute left-3 top-1/2 size-4 -translate-y-1/2 text-muted-foreground" />
						<Input
							id="user-search"
							placeholder="Buscar por nombre o correo…"
							value={search}
							onChange={handleSearchChange}
							className="pl-9"
							autoComplete="off"
						/>
					</div>
					<div className="relative">
						<select
							id="role-filter"
							value={roleFilter}
							onChange={(e) => setRoleFilter(e.target.value as AppRole | "")}
							className="flex h-9 w-full appearance-none rounded-md border border-input bg-background py-1 pl-3 pr-8 text-foreground text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 sm:w-44"
						>
							{ALL_ROLES.map((r) => (
								<option key={r.value} value={r.value}>
									{r.label}
								</option>
							))}
						</select>
						<ChevronDown className="pointer-events-none absolute right-2.5 top-1/2 size-3.5 -translate-y-1/2 text-muted-foreground" />
					</div>
				</div>

				{/* Users list card */}
				<div className="rounded-xl border border-foreground/10 bg-card">
					{/* Card header */}
					<div className="flex items-center justify-between border-b border-foreground/8 px-5 py-3.5">
						<div className="flex items-center gap-2">
							<Users className="size-4 text-primary" />
							<span className="font-medium text-foreground text-sm">
								Lista de usuarios
							</span>
							{state.status === "success" && users.length > 0 && (
								<span className="rounded-full bg-primary/15 px-2 py-0.5 font-medium text-[10px] text-primary ring-1 ring-primary/25">
									{users.length}
								</span>
							)}
						</div>
						{state.status !== "loading" && (
							<button
								type="button"
								onClick={() => void fetchUsers()}
								className="flex size-7 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-foreground/8 hover:text-foreground"
								aria-label="Recargar usuarios"
								title="Recargar"
							>
								<RefreshCw className="size-3.5" />
							</button>
						)}
					</div>

					<div className="px-5 py-2">
						{/* Loading */}
						{state.status === "loading" && <UserListSkeleton />}

						{/* Error */}
						{state.status === "error" && (
							<div className="flex flex-col items-center gap-3 py-14 text-center">
								<div className="flex size-12 items-center justify-center rounded-xl bg-destructive/10">
									<AlertTriangle className="size-5 text-destructive" />
								</div>
								<div>
									<p className="font-medium text-foreground text-sm">
										Error al cargar usuarios
									</p>
									<p className="mt-0.5 text-muted-foreground text-xs">
										{state.message}
									</p>
								</div>
								<Button
									size="sm"
									variant="outline"
									className="gap-2"
									onClick={() => void fetchUsers()}
								>
									<RefreshCw className="size-3.5" />
									Reintentar
								</Button>
							</div>
						)}

						{/* Empty state */}
						{state.status === "success" && users.length === 0 && (
							<div className="flex flex-col items-center gap-3 py-14 text-center">
								<div className="flex size-12 items-center justify-center rounded-xl bg-foreground/5">
									<Users className="size-5 text-muted-foreground" />
								</div>
								<div>
									<p className="font-medium text-foreground text-sm">
										{debouncedSearch || roleFilter
											? "Sin resultados"
											: "Sin usuarios"}
									</p>
									<p className="mt-0.5 text-muted-foreground text-xs">
										{debouncedSearch || roleFilter
											? "Prueba con otros filtros de búsqueda."
											: "Crea el primer usuario con el botón de arriba."}
									</p>
								</div>
								{(debouncedSearch || roleFilter) && (
									<Button
										size="sm"
										variant="outline"
										onClick={() => {
											setSearch("");
											setDebouncedSearch("");
											setRoleFilter("");
										}}
									>
										Limpiar filtros
									</Button>
								)}
							</div>
						)}

						{/* User rows */}
						{state.status === "success" && users.length > 0 && (
							<ul className="divide-y divide-foreground/6">
								{users.map((u) => {
									return (
										<li
											key={u.keycloakUserId || u.id}
											className="group flex items-center gap-3 py-3.5"
										>
											{/* Avatar */}
											<div className="flex size-9 shrink-0 items-center justify-center rounded-full bg-foreground/10 font-semibold text-foreground text-xs">
												{(u.name || u.email).slice(0, 1).toUpperCase()}
											</div>

											{/* Info */}
											<div className="min-w-0 flex-1">
												<p className="truncate font-medium text-foreground text-sm">
													{u.name || "—"}
												</p>
												<p className="truncate text-[11px] text-muted-foreground">
													{u.email}
												</p>
											</div>

											{/* Role badge */}
											<RoleBadge role={u.role} />

											{/* Date */}
											<span className="hidden shrink-0 text-[11px] text-muted-foreground sm:block">
												{formatDateShort(u.createdAt)}
											</span>

											{/* Ver detalle */}
											<Button
												size="sm"
												variant="ghost"
												className="shrink-0 text-xs opacity-0 transition-opacity group-hover:opacity-100 focus:opacity-100"
												onClick={() => handleViewDetail(u)}
											>
												Ver detalle
											</Button>
										</li>
									);
								})}
							</ul>
						)}
					</div>
				</div>
			</div>

			{/* User detail slide-over */}
			<UserDetailSheet
				user={selectedUser}
				canEdit={canEditSelected}
				isAdmin={isAdmin}
				onClose={() => setSelectedUser(null)}
				getAccessToken={getAccessToken}
				onDeleteSuccess={() => {
					setSelectedUser(null);
					void fetchUsers();
				}}
				onSaveEdit={(keycloakUserId, name, role) =>
					handleSaveEdit(keycloakUserId, name, role)
				}
			/>
		</>
	);
}

// ─── Export ──────────────────────────────────────────────────────────────────

export default function UsuariosPage() {
	return (
		<RoleGuard requiredRoles={["admin"]}>
			<UsersListPageInner />
		</RoleGuard>
	);
}
