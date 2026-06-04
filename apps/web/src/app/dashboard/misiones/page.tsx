"use client";

import { env } from "@umbral/env/web";
import { Button } from "@umbral/ui/components/button";
import {
	Card,
	CardContent,
	CardDescription,
	CardFooter,
	CardHeader,
	CardTitle,
} from "@umbral/ui/components/card";
import { Input } from "@umbral/ui/components/input";
import {
	AlertTriangle,
	ChevronDown,
	Plus,
	RefreshCw,
	ScrollText,
	Search,
} from "lucide-react";
import type { Route } from "next";
import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/components/auth-provider";
import { MissionListSkeleton } from "@/components/missions/mission-list-skeleton";
import { RoleGuard } from "@/components/role-guard";

// ─── Types ───────────────────────────────────────────────────────────────────

interface ApiMission {
	id: string;
	name: string;
	description: string;
	status: string; // "Active" or "Inactive"
	stageCount: number;
}

type FetchState =
	| { status: "loading" }
	| { status: "error"; message: string }
	| { status: "success"; missions: ApiMission[] };

// ─── Constants ───────────────────────────────────────────────────────────────

const ALL_STATUSES = [
	{ value: "", label: "Todos los estados" },
	{ value: "Active", label: "Activas" },
	{ value: "Inactive", label: "Inactivas" },
] as const;

// ─── Sub-components ──────────────────────────────────────────────────────────

function StatusBadge({ status }: { status: string }) {
	const isActive = status.toLowerCase() === "active";
	return (
		<span
			className={`inline-flex shrink-0 items-center rounded-full px-2.5 py-0.5 font-semibold text-[10px] ring-1 ${
				isActive
					? "bg-emerald-500/15 text-emerald-400 ring-emerald-500/25"
					: "bg-amber-500/15 text-amber-400 ring-amber-500/25"
			}`}
		>
			{isActive ? "Activa" : "Inactiva"}
		</span>
	);
}

// ─── Main page inner ─────────────────────────────────────────────────────────

function MisionesPanelInner() {
	const { getAccessToken } = useAuth();

	const [search, setSearch] = useState("");
	const [statusFilter, setStatusFilter] = useState("");
	const [state, setState] = useState<FetchState>({ status: "loading" });

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

	const fetchMissions = useCallback(async () => {
		setState({ status: "loading" });
		try {
			const accessToken = await getAccessToken(60);
			if (!accessToken) {
				setState({ status: "error", message: "No hay sesión activa." });
				return;
			}

			const params = new URLSearchParams();
			if (debouncedSearch.trim()) params.set("search", debouncedSearch.trim());
			if (statusFilter) params.set("status", statusFilter);

			const query = params.toString();
			const url = `${env.NEXT_PUBLIC_SERVER_URL}/api/missions${query ? `?${query}` : ""}`;

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
				throw new Error(errorText || `Error del servidor (${response.status})`);
			}

			const missions = (await response.json()) as ApiMission[];
			setState({ status: "success", missions });
		} catch (err) {
			const message =
				err instanceof Error
					? err.message
					: "No se pudo cargar la lista de misiones.";
			setState({ status: "error", message });
			toast.error(message);
		}
	}, [getAccessToken, debouncedSearch, statusFilter]);

	useEffect(() => {
		void fetchMissions();
	}, [fetchMissions]);

	const missions = state.status === "success" ? state.missions : [];

	return (
		<div className="mx-auto max-w-5xl space-y-6">
			{/* Page header */}
			<div className="flex items-start justify-between gap-4">
				<div>
					<h1 className="font-semibold text-2xl text-foreground tracking-tight">
						Diseño de Misiones
					</h1>
					<p className="mt-1 text-muted-foreground text-sm">
						Crea y gestiona las misiones que servirán como plantillas para las
						sesiones de juego de los participantes.
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
			<div className="flex items-start gap-3 rounded-xl border border-primary/20 bg-primary/5 p-4 text-primary text-sm">
				<ScrollText className="mt-0.5 size-5 shrink-0" />
				<div>
					<span className="font-semibold">
						Base de datos de Misiones conectada
					</span>
					<p className="mt-0.5 text-primary/80">
						Las misiones diseñadas se guardan en la base de datos PostgreSQL
						independiente del servicio de misiones (`missionservice_db`).
						Inicialmente se guardan en estado "Inactiva" y se bloquea la edición
						de sus etapas una vez que se activan en una sesión de juego.
					</p>
				</div>
			</div>

			{/* Filters */}
			<div className="flex flex-col gap-3 sm:flex-row">
				<div className="relative flex-1">
					<Search className="pointer-events-none absolute top-1/2 left-3 size-4 -translate-y-1/2 text-muted-foreground" />
					<Input
						id="mission-search"
						placeholder="Buscar por nombre de misión…"
						value={search}
						onChange={handleSearchChange}
						className="pl-9"
						autoComplete="off"
					/>
				</div>
				<div className="relative">
					<select
						id="status-filter"
						value={statusFilter}
						onChange={(e) => setStatusFilter(e.target.value)}
						className="flex h-9 w-full appearance-none rounded-md border border-input bg-background py-1 pr-8 pl-3 text-foreground text-sm shadow-sm transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 sm:w-44"
					>
						{ALL_STATUSES.map((s) => (
							<option key={s.value} value={s.value}>
								{s.label}
							</option>
						))}
					</select>
					<ChevronDown className="pointer-events-none absolute top-1/2 right-2.5 size-3.5 -translate-y-1/2 text-muted-foreground" />
				</div>
			</div>

			{/* Grid misiones */}
			<div className="space-y-4">
				<div className="flex items-center justify-between border-foreground/8 border-b pb-2">
					<div className="flex items-center gap-2">
						<ScrollText className="size-4 text-primary" />
						<span className="font-medium text-foreground text-sm">
							Plantillas de Misión
						</span>
						{state.status === "success" && missions.length > 0 && (
							<span className="rounded-full bg-primary/15 px-2 py-0.5 font-medium text-[10px] text-primary ring-1 ring-primary/25">
								{missions.length}
							</span>
						)}
					</div>
					{state.status !== "loading" && (
						<button
							type="button"
							onClick={() => void fetchMissions()}
							className="flex size-7 items-center justify-center rounded-md text-muted-foreground transition-colors hover:bg-foreground/8 hover:text-foreground"
							aria-label="Recargar misiones"
							title="Recargar"
						>
							<RefreshCw className="size-3.5" />
						</button>
					)}
				</div>

				{/* Loading state */}
				{state.status === "loading" && <MissionListSkeleton />}

				{/* Error state */}
				{state.status === "error" && (
					<div className="flex flex-col items-center gap-3 py-14 text-center">
						<div className="flex size-12 items-center justify-center rounded-xl bg-destructive/10">
							<AlertTriangle className="size-5 text-destructive" />
						</div>
						<div>
							<p className="font-medium text-foreground text-sm">
								Error al cargar misiones
							</p>
							<p className="mt-0.5 text-muted-foreground text-xs">
								{state.message}
							</p>
						</div>
						<Button
							size="sm"
							variant="outline"
							className="gap-2"
							onClick={() => void fetchMissions()}
						>
							<RefreshCw className="size-3.5" />
							Reintentar
						</Button>
					</div>
				)}

				{/* Empty state */}
				{state.status === "success" && missions.length === 0 && (
					<div className="flex flex-col items-center gap-3 py-14 text-center">
						<div className="flex size-12 items-center justify-center rounded-xl bg-foreground/5">
							<ScrollText className="size-5 text-muted-foreground" />
						</div>
						<div>
							<p className="font-medium text-foreground text-sm">
								{debouncedSearch || statusFilter
									? "Sin resultados"
									: "No hay misiones diseñadas"}
							</p>
							<p className="mt-0.5 text-muted-foreground text-xs">
								{debouncedSearch || statusFilter
									? "Prueba ajustando los filtros de búsqueda."
									: "Diseña tu primera misión con el botón de arriba."}
							</p>
						</div>
						{(debouncedSearch || statusFilter) && (
							<Button
								size="sm"
								variant="outline"
								onClick={() => {
									setSearch("");
									setDebouncedSearch("");
									setStatusFilter("");
								}}
							>
								Limpiar filtros
							</Button>
						)}
					</div>
				)}

				{/* Success state */}
				{state.status === "success" && missions.length > 0 && (
					<div className="grid gap-4 sm:grid-cols-2">
						{missions.map((m) => (
							<Card
								key={m.id}
								className="flex flex-col justify-between border-foreground/10 bg-card transition-all duration-300 hover:border-primary/20"
							>
								<CardHeader>
									<div className="flex items-start justify-between gap-2">
										<CardTitle className="truncate font-semibold text-base">
											{m.name}
										</CardTitle>
										<StatusBadge status={m.status} />
									</div>
									<CardDescription className="mt-1 line-clamp-2">
										{m.description || "Sin descripción."}
									</CardDescription>
								</CardHeader>
								<CardContent className="grid grid-cols-2 gap-2 border-foreground/5 border-t pt-4 text-muted-foreground text-xs">
									<div className="flex flex-col items-center rounded-lg bg-foreground/5 p-2">
										<span className="font-semibold text-foreground text-sm">
											{m.stageCount}
										</span>
										<span>Etapas</span>
									</div>
									<div className="flex flex-col items-center justify-center rounded-lg bg-foreground/5 p-2">
										<span className="text-center font-semibold text-foreground text-xs">
											Misión
										</span>
										<span>Plantilla</span>
									</div>
								</CardContent>
								<CardFooter className="flex gap-2 border-foreground/5 border-t px-6 py-3">
									<Button
										variant="ghost"
										size="sm"
										className="w-full cursor-not-allowed text-muted-foreground text-xs hover:bg-transparent"
										disabled
									>
										Estructura (Detalle próximamente)
									</Button>
								</CardFooter>
							</Card>
						))}
					</div>
				)}
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
