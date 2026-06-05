"use client";

import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardDescription,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { useRouter, useSearchParams } from "next/navigation";
import { useState } from "react";
import { useAuth } from "@/components/auth-provider";

export default function LoginClient() {
	const router = useRouter();
	const searchParams = useSearchParams();
	const { beginLogin, session, status, signOut } = useAuth();
	const returnTo = searchParams.get("returnTo") ?? "/";
	const [error, setError] = useState<string | null>(null);

	if (status === "loading") {
		return (
			<div className="flex h-full items-center justify-center px-4 py-8">
				Cargando...
			</div>
		);
	}

	return (
		<div className="flex h-full items-center justify-center px-4 py-8">
			<Card className="w-full max-w-xl border border-foreground/10 bg-background/90 shadow-2xl shadow-black/5">
				<CardHeader>
					<CardTitle>Acceso con Keycloak</CardTitle>
					<CardDescription>
						Usa este flujo para iniciar sesión desde el web y regresar al home
						con la sesión ya guardada.
					</CardDescription>
				</CardHeader>
				<CardContent className="space-y-4 pb-6">
					{error ? (
						<div className="rounded-lg border border-destructive/20 bg-destructive/10 p-3 text-destructive text-sm">
							{error}
						</div>
					) : null}

					{status === "authenticated" ? (
						<>
							<div className="rounded-lg border border-emerald-500/20 bg-emerald-500/10 p-3 text-sm">
								<p className="font-medium text-emerald-700 dark:text-emerald-300">
									Ya tienes una sesión activa
								</p>
								<p className="text-muted-foreground">
									{session?.user.name ??
										session?.user.preferred_username ??
										"Usuario autenticado"}
									{session?.user.email ? ` · ${session.user.email}` : ""}
								</p>
							</div>
							<div className="flex flex-col gap-3 sm:flex-row">
								<Button onClick={() => router.push("/")}>Ir al home</Button>
								<Button variant="outline" onClick={() => void signOut()}>
									Cerrar sesión
								</Button>
							</div>
						</>
					) : (
						<>
							<p className="text-muted-foreground text-sm">
								Al continuar, Keycloak te autenticará y te devolverá a la ruta
								solicitada con la sesión activa.
							</p>
							<div className="flex flex-col gap-3 sm:flex-row">
								<Button
									onClick={async () => {
										try {
											await beginLogin({ returnTo });
										} catch (loginError) {
											setError(
												loginError instanceof Error
													? loginError.message
													: "No se pudo iniciar el login.",
											);
										}
									}}
								>
									Continuar con Keycloak
								</Button>
								<Button variant="outline" onClick={() => router.push("/")}>
									Volver al home
								</Button>
							</div>
						</>
					)}
				</CardContent>
			</Card>
		</div>
	);
}
