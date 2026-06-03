"use client";

import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardDescription,
	CardFooter,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { Compass, Gamepad2, Plus, ScrollText, Trophy } from "lucide-react";
import type { Route } from "next";
import Link from "next/link";
import { RoleGuard } from "@/components/role-guard";

// Mock templates represent pre-defined standard game patterns
const MOCK_TEMPLATES = [
	{
		id: "tmpl-1",
		name: "Búsqueda del Tesoro del Campus",
		description: "Misión clásica de inducción. Los jugadores deben resolver acertijos físicos en las instalaciones para hallar la contraseña final.",
		stagesCount: 3,
		substagesCount: 4,
		difficulty: "Fácil",
		points: 150,
	},
	{
		id: "tmpl-2",
		name: "Desafío de Trivia de Ciberseguridad",
		description: "Evaluación inmersiva de conceptos de seguridad informática mediante etapas secuenciales de trivias contrarreloj.",
		stagesCount: 2,
		substagesCount: 5,
		difficulty: "Medio",
		points: 300,
	},
];

function MisionesPanelInner() {
	return (
		<div className="mx-auto max-w-5xl space-y-6">
			{/* Page header */}
			<div className="flex items-start justify-between gap-4">
				<div>
					<h1 className="font-semibold text-2xl text-foreground tracking-tight">
						Diseño de Misiones
					</h1>
					<p className="mt-1 text-muted-foreground text-sm">
						Crea y diseña misiones que servirán como plantillas para las sesiones de juego de los participantes.
					</p>
				</div>
				<Link href={"/dashboard/misiones/nueva" as Route}>
					<Button className="shrink-0 gap-2">
						<Plus className="size-4" />
						Crear Misión
					</Button>
				</Link>
			</div>

			{/* Info banner */}
			<div className="rounded-xl border border-primary/20 bg-primary/5 p-4 text-primary text-sm flex gap-3 items-start">
				<ScrollText className="size-5 shrink-0 mt-0.5" />
				<div>
					<span className="font-semibold">Base de datos de Misiones conectada</span>
					<p className="text-primary/80 mt-0.5">
						Las misiones diseñadas se guardan en la base de datos PostgreSQL independiente del servicio de misiones (`missionservice_db`). Inicialmente se guardan en estado "Inactiva" y se bloquea la edición de sus etapas una vez que se activan en una sesión de juego.
					</p>
				</div>
			</div>

			{/* Grid templates */}
			<div className="space-y-4">
				<h2 className="text-base font-semibold text-foreground">
					Plantillas del Sistema (Mock)
				</h2>
				<div className="grid gap-4 sm:grid-cols-2">
					{MOCK_TEMPLATES.map((tmpl) => (
						<Card key={tmpl.id} className="border-foreground/10 flex flex-col justify-between">
							<CardHeader>
								<div className="flex items-start justify-between gap-2">
									<CardTitle className="text-base font-semibold truncate">
										{tmpl.name}
									</CardTitle>
									<span className={`text-[10px] font-semibold px-2 py-0.5 rounded-full ring-1 ${
										tmpl.difficulty === "Fácil" 
											? "bg-emerald-500/10 text-emerald-400 ring-emerald-500/20" 
											: "bg-amber-500/10 text-amber-400 ring-amber-500/20"
									}`}>
										{tmpl.difficulty}
									</span>
								</div>
								<CardDescription className="line-clamp-2 mt-1">
									{tmpl.description}
								</CardDescription>
							</CardHeader>
							<CardContent className="text-xs text-muted-foreground grid grid-cols-3 gap-2 border-t border-foreground/5 pt-4">
								<div className="flex flex-col items-center p-2 rounded-lg bg-foreground/5">
									<span className="font-semibold text-foreground text-sm">{tmpl.stagesCount}</span>
									<span>Etapas</span>
								</div>
								<div className="flex flex-col items-center p-2 rounded-lg bg-foreground/5">
									<span className="font-semibold text-foreground text-sm">{tmpl.substagesCount}</span>
									<span>Subetapas</span>
								</div>
								<div className="flex flex-col items-center p-2 rounded-lg bg-foreground/5">
									<span className="font-semibold text-foreground text-sm">{tmpl.points}</span>
									<span>Puntos Max</span>
								</div>
							</CardContent>
							<CardFooter className="border-t border-foreground/5 px-6 py-3 flex gap-2">
								<Button variant="ghost" size="sm" className="w-full text-xs" disabled>
									Ver Estructura
								</Button>
							</CardFooter>
						</Card>
					))}
				</div>
			</div>
		</div>
	);
}

export default function MisionesPage() {
	return (
		<RoleGuard requiredRoles={["admin", "operator"]}>
			<MisionesPanelInner />
		</RoleGuard>
	);
}
