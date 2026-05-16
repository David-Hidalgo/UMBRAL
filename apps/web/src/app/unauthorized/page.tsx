"use client";

import { Button } from "@umbral/ui/components/button";
import { ShieldOff } from "lucide-react";
import { useRouter } from "next/navigation";

import { useAuth } from "@/components/auth-provider";

export default function UnauthorizedPage() {
	const router = useRouter();
	const { signOut, user } = useAuth();

	return (
		<div className="relative flex h-full items-center justify-center px-4">
			<div className="absolute inset-0 -z-10 bg-[radial-gradient(circle_at_center,rgba(239,68,68,0.08),transparent_55%)]" />
			<div className="flex max-w-sm flex-col items-center gap-5 text-center">
				<div className="flex size-16 items-center justify-center rounded-2xl bg-destructive/10 ring-1 ring-destructive/20">
					<ShieldOff className="size-8 text-destructive" />
				</div>
				<div className="space-y-2">
					<h1 className="font-semibold text-2xl text-foreground tracking-tight">
						Acceso denegado
					</h1>
					<p className="text-muted-foreground text-sm leading-relaxed">
						{user
							? "Tu cuenta no tiene el rol necesario para acceder a esta sección."
							: "No tienes permisos para acceder a esta página."}
					</p>
				</div>
				<div className="flex w-full flex-col gap-2 sm:flex-row">
					<Button
						variant="outline"
						className="flex-1"
						onClick={() => router.push("/")}
					>
						Ir al inicio
					</Button>
					{user && (
						<Button
							variant="destructive"
							className="flex-1"
							onClick={() => void signOut()}
						>
							Cerrar sesión
						</Button>
					)}
				</div>
			</div>
		</div>
	);
}
