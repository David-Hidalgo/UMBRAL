"use client";

import { Button } from "@umbral/ui/components/button";
import {
	Activity,
	ArrowRight,
	Clock,
	Shield,
	ShieldCheck,
	Users,
} from "lucide-react";
import { useRouter } from "next/navigation";

import { useAuth } from "@/components/auth-provider";

const STATS = [
	{ icon: Activity, label: "Misiones en tiempo real" },
	{ icon: Clock,    label: "Temporizador global" },
	{ icon: Users,    label: "Gestión de equipos" },
	{ icon: Shield,   label: "Panel de operaciones" },
];

export default function Home() {
	const router = useRouter();
	const { status, user } = useAuth();

	const handleCta = () => {
		if (status === "authenticated") {
			router.push("/dashboard");
		} else {
			router.push("/login");
		}
	};

	const ctaLabel =
		status === "loading"
			? "Cargando..."
			: status === "authenticated"
				? `Ir al panel${user?.name ? `, ${user.name}` : ""}`
				: "Acceder al sistema";

	return (
		<div className="relative flex h-full flex-col items-center justify-center overflow-hidden px-4">
			{/* Fondo con gradiente radial sutil */}
			<div className="pointer-events-none absolute inset-0 -z-10 bg-[radial-gradient(ellipse_80%_60%_at_50%_-10%,rgba(56,189,248,0.10),transparent),radial-gradient(ellipse_60%_50%_at_80%_110%,rgba(99,102,241,0.08),transparent)]" />

			{/* Contenido centrado */}
			<div className="flex flex-col items-center gap-6 text-center">
				{/* Icono de escudo */}
				<div className="relative">
					<div className="flex size-16 items-center justify-center rounded-2xl border border-foreground/10 bg-background/80 shadow-lg shadow-black/20 backdrop-blur">
						<ShieldCheck className="size-8 text-foreground/80" />
					</div>
					{/* Punto de acento */}
					<span className="absolute -right-1 -top-1 size-3 rounded-full border-2 border-background bg-sky-400" />
				</div>

				{/* Título */}
				<div className="space-y-2">
					<h1 className="text-5xl font-bold tracking-[0.25em] text-foreground md:text-7xl">
						UMBRAL
					</h1>
					<p className="text-xs font-medium tracking-[0.3em] text-muted-foreground/70">
						SISTEMA DE GESTIÓN DE MISIONES
					</p>
				</div>

				{/* Descripción */}
				<p className="max-w-sm text-sm text-muted-foreground md:text-base">
					{status === "authenticated"
						? "Sesión activa. Accede al panel de control para gestionar misiones y equipos."
						: "Accede con tu cuenta institucional para gestionar misiones y sesiones en tiempo real."}
				</p>

				{/* Botón CTA mejorado */}
				<Button
					size="lg"
					disabled={status === "loading"}
					onClick={handleCta}
					className="group relative h-11 gap-2 rounded-lg px-7 text-sm font-semibold shadow-md transition-all duration-200 hover:scale-[1.02] hover:shadow-lg"
				>
					{ctaLabel}
					<ArrowRight className="size-4 transition-transform duration-200 group-hover:translate-x-0.5" />
				</Button>
			</div>

			{/* Pills de características */}
			<div className="absolute bottom-8 left-0 right-0 flex flex-wrap items-center justify-center gap-3 px-4">
				{STATS.map(({ icon: Icon, label }) => (
					<div
						key={label}
						className="flex items-center gap-2 rounded-full border border-foreground/10 bg-background/60 px-4 py-1.5 text-xs text-muted-foreground backdrop-blur"
					>
						<Icon className="size-3.5 shrink-0" />
						{label}
					</div>
				))}
			</div>
		</div>
	);
}
