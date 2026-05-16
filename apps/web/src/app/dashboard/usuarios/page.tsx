"use client";

import { env } from "@umbral/env/web";
import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { Input } from "@umbral/ui/components/input";
import { Label } from "@umbral/ui/components/label";
import { CheckCircle2, Loader2, UserPlus, Users } from "lucide-react";
import { useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/components/auth-provider";
import { RoleGuard } from "@/components/role-guard";

type AppRole = "admin" | "operator" | "player";

interface CreatedUser {
	id: string;
	name: string;
	email: string;
	role: AppRole;
	createdAt: Date;
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

function RoleBadge({ role }: { role: AppRole }) {
	return (
		<span
			className={`inline-flex items-center rounded-full px-2.5 py-0.5 font-medium text-[11px] ${ROLE_COLORS[role]}`}
		>
			{ROLE_LABELS[role]}
		</span>
	);
}

interface FormState {
	name: string;
	email: string;
	password: string;
	role: AppRole;
}

const EMPTY_FORM: FormState = {
	name: "",
	email: "",
	password: "",
	role: "player",
};

function UsersPageInner() {
	const { getAccessToken } = useAuth();
	const [form, setForm] = useState<FormState>(EMPTY_FORM);
	const [loading, setLoading] = useState(false);
	const [createdUsers, setCreatedUsers] = useState<CreatedUser[]>([]);

	const handleChange = (
		e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
	) => {
		setForm((prev) => ({ ...prev, [e.target.name]: e.target.value }));
	};

	const handleSubmit = async (e: React.FormEvent) => {
		e.preventDefault();

		if (!form.name.trim() || !form.email.trim() || !form.password.trim()) {
			toast.error("Completa todos los campos obligatorios.");
			return;
		}

		setLoading(true);

		try {
			const accessToken = await getAccessToken(60);

			if (!accessToken) {
				toast.error("No hay sesión activa.");
				return;
			}

			const response = await fetch(`${env.NEXT_PUBLIC_SERVER_URL}/api/users`, {
				method: "POST",
				credentials: "omit",
				headers: {
					"Content-Type": "application/json",
					Authorization: `Bearer ${accessToken}`,
				},
				body: JSON.stringify({
					name: form.name,
					email: form.email,
					password: form.password,
					role: form.role,
				}),
			});

			if (!response.ok) {
				const errorText = await response.text().catch(() => "");
				throw new Error(errorText || `Error del servidor (${response.status})`);
			}

			const created = (await response.json()) as { id: string };

			setCreatedUsers((prev) => [
				{
					id: created.id ?? crypto.randomUUID(),
					name: form.name,
					email: form.email,
					role: form.role,
					createdAt: new Date(),
				},
				...prev,
			]);

			toast.success(`Usuario "${form.name}" creado correctamente.`);
			setForm(EMPTY_FORM);
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "No se pudo crear el usuario.",
			);
		} finally {
			setLoading(false);
		}
	};

	return (
		<div className="mx-auto max-w-4xl space-y-6">
			{/* Header */}
			<div>
				<h1 className="font-semibold text-2xl text-foreground tracking-tight">
					Administración de usuarios
				</h1>
				<p className="mt-1 text-muted-foreground text-sm">
					Crea nuevos usuarios y asígnales un rol en el realm de Keycloak.
				</p>
			</div>

			<div className="grid gap-6 lg:grid-cols-[1fr_1.2fr]">
				{/* Form */}
				<Card className="border-foreground/10">
					<CardHeader className="pb-4">
						<CardTitle className="flex items-center gap-2 text-base">
							<UserPlus className="size-4 text-primary" />
							Nuevo usuario
						</CardTitle>
						<CardDescription>
							El usuario será provisionado en Keycloak con el rol seleccionado.
						</CardDescription>
					</CardHeader>
					<CardContent>
						<form onSubmit={(e) => void handleSubmit(e)} className="space-y-4">
							<div className="space-y-1.5">
								<Label htmlFor="name">Nombre completo</Label>
								<Input
									id="name"
									name="name"
									placeholder="Ana García"
									value={form.name}
									onChange={handleChange}
									disabled={loading}
									autoComplete="off"
								/>
							</div>

							<div className="space-y-1.5">
								<Label htmlFor="email">Correo electrónico</Label>
								<Input
									id="email"
									name="email"
									type="email"
									placeholder="ana@ejemplo.com"
									value={form.email}
									onChange={handleChange}
									disabled={loading}
									autoComplete="off"
								/>
							</div>

							<div className="space-y-1.5">
								<Label htmlFor="password">Contraseña temporal</Label>
								<Input
									id="password"
									name="password"
									type="password"
									placeholder="••••••••"
									value={form.password}
									onChange={handleChange}
									disabled={loading}
								/>
							</div>

							<div className="space-y-1.5">
								<Label htmlFor="role">Rol</Label>
								<select
									id="role"
									name="role"
									value={form.role}
									onChange={handleChange}
									disabled={loading}
									className="flex h-9 w-full appearance-none rounded-md border border-input bg-background px-3 py-1 text-foreground text-sm shadow-sm ring-offset-background transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
								>
									<option value="admin">Administrador</option>
									<option value="operator">Operador</option>
									<option value="player">Jugador</option>
								</select>
							</div>

							<Button type="submit" className="w-full" disabled={loading}>
								{loading ? (
									<>
										<Loader2 className="size-4 animate-spin" />
										Creando…
									</>
								) : (
									<>
										<UserPlus className="size-4" />
										Crear usuario
									</>
								)}
							</Button>
						</form>
					</CardContent>
				</Card>

				{/* Table */}
				<Card className="border-foreground/10">
					<CardHeader className="pb-4">
						<CardTitle className="flex items-center gap-2 text-base">
							<Users className="size-4 text-primary" />
							Usuarios creados
							{createdUsers.length > 0 && (
								<span className="ml-auto rounded-full bg-primary/15 px-2 py-0.5 font-medium text-[10px] text-primary ring-1 ring-primary/25">
									{createdUsers.length}
								</span>
							)}
						</CardTitle>
						<CardDescription>Historial de esta sesión.</CardDescription>
					</CardHeader>
					<CardContent>
						{createdUsers.length === 0 ? (
							<div className="flex flex-col items-center gap-3 py-10 text-center">
								<div className="flex size-12 items-center justify-center rounded-xl bg-foreground/5">
									<Users className="size-5 text-muted-foreground" />
								</div>
								<p className="text-muted-foreground text-sm">
									Aún no has creado ningún usuario.
								</p>
							</div>
						) : (
							<ul className="divide-y divide-foreground/6">
								{createdUsers.map((u) => (
									<li
										key={u.id}
										className="flex items-center gap-3 py-3 first:pt-0 last:pb-0"
									>
										<div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-foreground/10 font-semibold text-foreground text-xs">
											{u.name.slice(0, 1).toUpperCase()}
										</div>
										<div className="min-w-0 flex-1">
											<p className="truncate font-medium text-foreground text-sm">
												{u.name}
											</p>
											<p className="truncate text-[11px] text-muted-foreground">
												{u.email}
											</p>
										</div>
										<RoleBadge role={u.role} />
										<CheckCircle2 className="size-4 shrink-0 text-emerald-500" />
									</li>
								))}
							</ul>
						)}
					</CardContent>
				</Card>
			</div>
		</div>
	);
}

export default function UsuariosPage() {
	return (
		<RoleGuard requiredRoles={["admin"]}>
			<UsersPageInner />
		</RoleGuard>
	);
}
