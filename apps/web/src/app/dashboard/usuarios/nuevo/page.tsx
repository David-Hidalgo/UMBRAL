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
import { ArrowLeft, Loader2, UserPlus } from "lucide-react";
import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/components/auth-provider";
import { RoleGuard } from "@/components/role-guard";

type AppRole = "admin" | "operator" | "player";

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

function CreateUserForm() {
	const router = useRouter();
	const { getAccessToken } = useAuth();
	const [form, setForm] = useState<FormState>(EMPTY_FORM);
	const [loading, setLoading] = useState(false);

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

			toast.success(`Usuario "${form.name}" creado correctamente.`);
			router.push("/dashboard/usuarios");
		} catch (err) {
			toast.error(
				err instanceof Error ? err.message : "No se pudo crear el usuario.",
			);
		} finally {
			setLoading(false);
		}
	};

	return (
		<div className="mx-auto max-w-lg space-y-6">
			{/* Back nav */}
			<div>
				<Link
					href="/dashboard/usuarios"
					className="inline-flex items-center gap-1.5 text-muted-foreground text-sm transition-colors hover:text-foreground"
				>
					<ArrowLeft className="size-3.5" />
					Volver al listado
				</Link>
			</div>

			{/* Header */}
			<div>
				<h1 className="font-semibold text-2xl text-foreground tracking-tight">
					Nuevo usuario
				</h1>
				<p className="mt-1 text-muted-foreground text-sm">
					El usuario será provisionado en Keycloak con el rol seleccionado.
				</p>
			</div>

			<Card className="border-foreground/10">
				<CardHeader className="pb-4">
					<CardTitle className="flex items-center gap-2 text-base">
						<UserPlus className="size-4 text-primary" />
						Datos del usuario
					</CardTitle>
					<CardDescription>Todos los campos son obligatorios.</CardDescription>
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

						<div className="flex gap-3 pt-1">
							<Button
								type="button"
								variant="outline"
								className="flex-1"
								disabled={loading}
								onClick={() => router.push("/dashboard/usuarios")}
							>
								Cancelar
							</Button>
							<Button type="submit" className="flex-1" disabled={loading}>
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
						</div>
					</form>
				</CardContent>
			</Card>
		</div>
	);
}

export default function NuevoUsuarioPage() {
	return (
		<RoleGuard requiredRoles={["admin"]}>
			<CreateUserForm />
		</RoleGuard>
	);
}
