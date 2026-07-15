"use client";

import * as signalR from "@microsoft/signalr";
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
	Calendar,
	CheckCircle2,
	ChevronRight,
	Gamepad2,
	History,
	Pause,
	Play,
	Plus,
	RefreshCw,
	Search,
	User,
	UserPlus,
	Users,
	XCircle,
} from "lucide-react";
import { useCallback, useEffect, useRef, useState } from "react";
import { toast } from "sonner";

import { useAuth } from "@/components/auth-provider";
import { HintsPanel } from "@/components/hints/hints-panel";
import { RoleGuard } from "@/components/role-guard";
import { ScoringPanel } from "@/components/scoring/scoring-panel";
import { SessionTimer } from "@/components/session-timer";
import { getMissionById, type MissionDetail } from "@/lib/api/missions";
import {
	advanceStage,
	createSession,
	createTeam,
	getAllSessions,
	getSessionById,
	type LiveSessionDetail,
	type LiveSessionSummary,
	transitionSessionStatus,
} from "@/lib/api/sessions";

// ─── Interfaces & Constants ──────────────────────────────────────────────────

interface ActiveMissionOption {
	id: string;
	name: string;
	stagesCount: number;
}

interface LogEntry {
	id: string;
	timestamp: string;
	type: "info" | "success" | "warning" | "error";
	message: string;
}

const STATUS_LABELS = {
	Programada: "Programada",
	Preparacion: "En Preparación",
	Activa: "En Curso / Activa",
	Pausada: "Pausada",
	Finalizada: "Finalizada",
	Cancelada: "Cancelada",
};

const STATUS_CLASSES = {
	Programada: "bg-blue-500/10 text-blue-400 ring-blue-500/20",
	Preparacion: "bg-yellow-500/10 text-yellow-400 ring-yellow-500/20",
	Activa: "bg-emerald-500/10 text-emerald-400 ring-emerald-500/20",
	Pausada: "bg-amber-500/10 text-amber-400 ring-amber-500/20",
	Finalizada: "bg-zinc-500/10 text-zinc-400 ring-zinc-500/20",
	Cancelada: "bg-rose-500/10 text-rose-400 ring-rose-500/20",
};

// ─── Component ────────────────────────────────────────────────────────────────

export default function PartidasPage() {
	return (
		<RoleGuard requiredRoles={["admin", "operator"]}>
			<PartidasPanelInner />
		</RoleGuard>
	);
}

function PartidasPanelInner() {
	const { getAccessToken } = useAuth();

	// List & Selection State
	const [sessions, setSessions] = useState<LiveSessionSummary[]>([]);
	const [activeSessionsCount, setActiveSessionsCount] = useState(0);
	const [selectedSessionId, setSelectedSessionId] = useState<string | null>(
		null,
	);
	const [sessionDetail, setSessionDetail] = useState<LiveSessionDetail | null>(
		null,
	);
	const [missionDetail, setMissionDetail] = useState<MissionDetail | null>(
		null,
	);
	const [loadingList, setLoadingList] = useState(true);
	const [loadingDetail, setLoadingDetail] = useState(false);
	const [searchTerm, setSearchTerm] = useState("");
	const [statusFilter, setStatusFilter] = useState("");

	// Creation Modal / Form State
	const [showCreateForm, setShowCreateForm] = useState(false);
	const [activeMissions, setActiveMissions] = useState<ActiveMissionOption[]>(
		[],
	);
	const [loadingMissions, setLoadingMissions] = useState(false);
	const [newSessionName, setNewSessionName] = useState("");
	const [newSessionMissionId, setNewSessionMissionId] = useState("");
	const [newSessionScheduledAt, setNewSessionScheduledAt] = useState("");
	const [creatingSession, setCreatingSession] = useState(false);

	// Team Creation Form State
	const [newTeamName, setNewTeamName] = useState("");
	const [creatingTeam, setCreatingTeam] = useState(false);

	// Terminal Console logs state
	const [logs, setLogs] = useState<LogEntry[]>([]);
	const logsEndRef = useRef<HTMLDivElement | null>(null);

	// SignalR Connection Ref
	const hubConnectionRef = useRef<signalR.HubConnection | null>(null);

	// Add log entry helper
	const addLog = useCallback(
		(message: string, type: LogEntry["type"] = "info") => {
			const timestamp = new Date().toLocaleTimeString();
			const id = `${Date.now()}-${Math.random()}`;
			setLogs((prev) =>
				[{ id, timestamp, type, message }, ...prev].slice(0, 50),
			);
		},
		[],
	);

	// Scroll logs to bottom when updated
	// biome-ignore lint/correctness/useExhaustiveDependencies: scroll when logs update
	useEffect(() => {
		logsEndRef.current?.scrollIntoView({ behavior: "smooth" });
	}, [logs]);

	// Fetch sessions list
	const fetchSessions = useCallback(async () => {
		setLoadingList(true);
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");

			const data = await getAllSessions(token, {
				status: statusFilter || undefined,
			});
			setSessions(data);

			// Count active/preparing ones
			const activeCount = data.filter(
				(s) => s.status === "Activa" || s.status === "Preparacion",
			).length;
			setActiveSessionsCount(activeCount);
		} catch (err) {
			const errorMsg = err instanceof Error ? err.message : String(err);
			toast.error("Error al cargar partidas: " + errorMsg);
		} finally {
			setLoadingList(false);
		}
	}, [getAccessToken, statusFilter]);

	// Initialize sessions list
	useEffect(() => {
		fetchSessions();
	}, [fetchSessions]);

	// Fetch active missions for creation selection
	const fetchActiveMissions = useCallback(async () => {
		setLoadingMissions(true);
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");

			const res = await fetch(
				`${env.NEXT_PUBLIC_SERVER_URL}/api/missions?status=Active`,
				{
					method: "GET",
					headers: {
						Authorization: `Bearer ${token}`,
					},
				},
			);
			if (!res.ok) throw new Error("Error del servidor al obtener misiones.");
			const data = (await res.json()) as Array<{
				id: string;
				name: string;
				stageCount?: number;
			}>;

			const formatted = data.map((m) => ({
				id: m.id,
				name: m.name,
				stagesCount: m.stageCount || 0,
			}));
			setActiveMissions(formatted);
		} catch (err) {
			const errorMsg = err instanceof Error ? err.message : String(err);
			toast.error("No se pudieron cargar las misiones activas: " + errorMsg);
		} finally {
			setLoadingMissions(false);
		}
	}, [getAccessToken]);

	// Trigger mission fetch when opening creation form
	useEffect(() => {
		if (showCreateForm) {
			fetchActiveMissions();
		}
	}, [showCreateForm, fetchActiveMissions]);

	// Fetch session details and its mission stages
	const loadSessionDetail = useCallback(
		async (id: string) => {
			setLoadingDetail(true);
			try {
				const token = await getAccessToken(60);
				if (!token) throw new Error("No autenticado.");

				const detail = await getSessionById(id, token);
				setSessionDetail(detail);

				// Fetch mission details to get the stage structure
				try {
					const mission = await getMissionById(detail.missionId, token);
					setMissionDetail(mission);
				} catch (mErr) {
					console.warn(
						"Could not load mission details for session stages",
						mErr,
					);
					setMissionDetail(null);
				}
			} catch (err) {
				const errorMsg = err instanceof Error ? err.message : String(err);
				toast.error("Error al cargar detalles: " + errorMsg);
				setSelectedSessionId(null);
			} finally {
				setLoadingDetail(false);
			}
		},
		[getAccessToken],
	);

	// Handle selection change
	useEffect(() => {
		if (selectedSessionId) {
			loadSessionDetail(selectedSessionId);
		} else {
			setSessionDetail(null);
			setMissionDetail(null);
		}
	}, [selectedSessionId, loadSessionDetail]);

	// Setup SignalR connection when session detail is selected
	useEffect(() => {
		if (!selectedSessionId) {
			if (hubConnectionRef.current) {
				hubConnectionRef.current.stop();
				hubConnectionRef.current = null;
			}
			return;
		}

		let active = true;
		addLog(
			`Iniciando enlace táctico para sesión: ${selectedSessionId.substring(0, 8)}...`,
			"info",
		);

		const connection = new signalR.HubConnectionBuilder()
			.withUrl(`${env.NEXT_PUBLIC_SERVER_URL}/hubs/sessions`, {
				accessTokenFactory: async () => {
					const token = await getAccessToken(60);
					return token ?? "";
				},
			})
			.withAutomaticReconnect()
			.build();

		connection.on(
			"PlayerJoined",
			(event: { sessionId: string; teamId: string; displayName: string }) => {
				if (!active) return;
				addLog(
					`JUGADOR_UNIDO:: Agente "${event.displayName}" se unió al equipo.`,
					"success",
				);
				// Reload details to update players list
				loadSessionDetail(selectedSessionId);
			},
		);

		connection.on(
			"StateChanged",
			(event: { sessionId: string; status: string }) => {
				if (!active) return;
				addLog(
					`ESTADO_MODIFICADO:: Nuevo estado de partida: "${STATUS_LABELS[event.status as keyof typeof STATUS_LABELS] || event.status}"`,
					"warning",
				);
				loadSessionDetail(selectedSessionId);
				fetchSessions();
			},
		);

		connection.on(
			"StageAdvanced",
			(event: { sessionId: string; currentStageIndex: number }) => {
				if (!active) return;
				addLog(
					`ETAPA_AVANZADA:: Operación avanzó a la etapa índice: ${event.currentStageIndex + 1}`,
					"success",
				);
				loadSessionDetail(selectedSessionId);
			},
		);

		const startConnection = async () => {
			try {
				await connection.start();
				if (!active) return;
				addLog("Conexión de telemetría establecida con éxito.", "success");

				// Join session group on hub
				await connection.invoke("JoinSessionGroup", selectedSessionId);
				addLog(
					`Suscrito a eventos en tiempo real de partida: ${selectedSessionId}`,
					"info",
				);
				hubConnectionRef.current = connection;
			} catch (err) {
				if (!active) return;
				const errorMsg = err instanceof Error ? err.message : String(err);
				addLog(`Fallo al establecer telemetría: ${errorMsg}`, "error");
				toast.error("Fallo de conexión en tiempo real.");
			}
		};

		void startConnection();

		return () => {
			active = false;
			if (hubConnectionRef.current) {
				const conn = hubConnectionRef.current;
				conn
					.invoke("LeaveSessionGroup", selectedSessionId)
					.catch((e) => console.warn("Failed to leave group on unmount", e))
					.finally(() => {
						conn.stop();
					});
				hubConnectionRef.current = null;
				addLog("Canal de telemetría cerrado.", "info");
			}
		};
	}, [
		selectedSessionId,
		getAccessToken,
		loadSessionDetail,
		fetchSessions,
		addLog,
	]);

	// Actions
	const handleCreateSession = async (e: React.FormEvent) => {
		e.preventDefault();
		if (!newSessionName.trim() || !newSessionMissionId) {
			toast.error("Por favor completa los campos obligatorios.");
			return;
		}

		setCreatingSession(true);
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");

			const scheduled = newSessionScheduledAt
				? new Date(newSessionScheduledAt).toISOString()
				: null;
			const id = await createSession(
				{
					missionId: newSessionMissionId,
					name: newSessionName.trim(),
					scheduledAt: scheduled,
				},
				token,
			);

			toast.success("Partida creada con éxito.");
			setNewSessionName("");
			setNewSessionScheduledAt("");
			setShowCreateForm(false);
			await fetchSessions();
			setSelectedSessionId(id);
		} catch (err) {
			const errorMsg = err instanceof Error ? err.message : String(err);
			toast.error("Error al crear partida: " + errorMsg);
		} finally {
			setCreatingSession(false);
		}
	};

	const handleAddTeam = async (e: React.FormEvent) => {
		e.preventDefault();
		if (!selectedSessionId || !newTeamName.trim()) return;

		setCreatingTeam(true);
		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");

			await createTeam(selectedSessionId, newTeamName.trim(), token);
			toast.success(`Equipo "${newTeamName}" creado.`);
			setNewTeamName("");
			await loadSessionDetail(selectedSessionId);
		} catch (err) {
			const errorMsg = err instanceof Error ? err.message : String(err);
			toast.error("Error al crear equipo: " + errorMsg);
		} finally {
			setCreatingTeam(false);
		}
	};

	const handleTransition = async (targetStatus: string) => {
		if (!selectedSessionId) return;

		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");

			await transitionSessionStatus(selectedSessionId, targetStatus, token);
			toast.success(
				`Transición a "${STATUS_LABELS[targetStatus as keyof typeof STATUS_LABELS]}" solicitada.`,
			);
			await loadSessionDetail(selectedSessionId);
			await fetchSessions();
		} catch (err) {
			const errorMsg = err instanceof Error ? err.message : String(err);
			toast.error("Error en transición de estado: " + errorMsg);
		}
	};

	const handleAdvanceStage = async () => {
		if (!selectedSessionId || !sessionDetail) return;
		const nextIndex = sessionDetail.currentStageIndex + 1;

		try {
			const token = await getAccessToken(60);
			if (!token) throw new Error("No autenticado.");

			await advanceStage(selectedSessionId, nextIndex, token);
			toast.success(`Avanzando partida a etapa ${nextIndex + 1}`);
			await loadSessionDetail(selectedSessionId);
		} catch (err) {
			const errorMsg = err instanceof Error ? err.message : String(err);
			toast.error(`Error al avanzar etapa: ${errorMsg}`);
		}
	};

	// Filters
	const filteredSessions = sessions.filter((s) => {
		const matchesSearch =
			s.name.toLowerCase().includes(searchTerm.toLowerCase()) ||
			s.missionName.toLowerCase().includes(searchTerm.toLowerCase());
		return matchesSearch;
	});

	return (
		<div className="flex h-[calc(100vh-7rem)] flex-col gap-6 text-foreground">
			{/* Page Header */}
			<div className="flex flex-col gap-4 border-border/10 border-b pb-6 sm:flex-row sm:items-center sm:justify-between">
				<div>
					<h1 className="font-black font-mono text-4xl text-foreground uppercase tracking-tight">
						CENTRO DE OPERACIONES TÁCTICAS
					</h1>
					<p className="mt-1 font-mono text-muted-foreground text-xs uppercase tracking-wider">
						Supervisión en vivo y despacho de partidas de agentes de campo
					</p>
				</div>
				<div className="flex items-center gap-3">
					<div className="flex items-center gap-2 rounded-lg border border-border/20 bg-muted/30 px-3 py-1.5 font-mono text-xs">
						<span className="relative flex h-2 w-2">
							<span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-emerald-400 opacity-75" />
							<span className="relative inline-flex h-2 w-2 rounded-full bg-emerald-500" />
						</span>
						<span className="text-muted-foreground">PARTIDAS ACTIVAS:</span>
						<span className="font-bold text-foreground">
							{activeSessionsCount}
						</span>
					</div>
					<Button
						onClick={() => setShowCreateForm(true)}
						className="flex items-center gap-2 border border-accent bg-accent/10 font-mono text-accent text-xs uppercase hover:bg-accent/20"
					>
						<Plus className="size-4" /> Desplegar Operación
					</Button>
				</div>
			</div>

			<div className="grid flex-1 grid-cols-1 gap-6 overflow-hidden lg:grid-cols-12">
				{/* Column 1: Sidebar Sessions List */}
				<div className="flex flex-col gap-4 overflow-hidden rounded-xl border border-border/10 bg-card/10 p-4 lg:col-span-4">
					{/* Search & Filter */}
					<div className="flex gap-2">
						<div className="relative flex-1">
							<Search className="absolute top-2.5 left-3 size-4 text-muted-foreground" />
							<Input
								placeholder="Buscar operación..."
								value={searchTerm}
								onChange={(e) => setSearchTerm(e.target.value)}
								className="border-border/20 bg-muted/20 pl-9 font-mono text-foreground text-xs placeholder:text-muted-foreground/60 focus:border-accent/40 focus:ring-accent/10"
							/>
						</div>
						<Button
							variant="outline"
							size="icon"
							onClick={fetchSessions}
							disabled={loadingList}
							className="shrink-0 border-border/20 hover:bg-muted/30"
						>
							<RefreshCw
								className={`size-4 text-muted-foreground ${loadingList ? "animate-spin" : ""}`}
							/>
						</Button>
					</div>

					<div className="flex gap-1.5 overflow-x-auto border-border/10 border-b pb-2">
						<button
							type="button"
							onClick={() => setStatusFilter("")}
							className={`shrink-0 rounded-md border px-3 py-1 font-mono text-[10px] uppercase transition-colors ${
								statusFilter === ""
									? "border-accent/40 bg-accent/20 text-accent"
									: "border-border/20 bg-transparent text-muted-foreground hover:bg-muted/10 hover:text-foreground"
							}`}
						>
							Todos
						</button>
						{Object.entries(STATUS_LABELS).map(([key, val]) => (
							<button
								key={key}
								type="button"
								onClick={() => setStatusFilter(key)}
								className={`shrink-0 rounded-md border px-3 py-1 font-mono text-[10px] uppercase transition-colors ${
									statusFilter === key
										? "border-accent/40 bg-accent/20 text-accent"
										: "border-border/20 bg-transparent text-muted-foreground hover:bg-muted/10 hover:text-foreground"
								}`}
							>
								{val}
							</button>
						))}
					</div>

					{/* List container */}
					<div className="custom-scrollbar flex-1 space-y-2 overflow-y-auto pr-1">
						{loadingList ? (
							["sk-1", "sk-2", "sk-3", "sk-4"].map((id) => (
								<div
									key={id}
									className="h-20 animate-pulse rounded-lg border border-border/10 bg-muted/10"
								/>
							))
						) : filteredSessions.length === 0 ? (
							<div className="flex flex-col items-center justify-center py-12 text-center">
								<Gamepad2 className="mb-2 size-8 text-muted-foreground/50" />
								<p className="font-mono text-muted-foreground text-xs uppercase">
									No se hallaron operaciones registradas.
								</p>
							</div>
						) : (
							filteredSessions.map((session) => {
								const isSelected = selectedSessionId === session.id;
								return (
									<button
										key={session.id}
										type="button"
										onClick={() => setSelectedSessionId(session.id)}
										className={`group flex w-full cursor-pointer flex-col rounded-lg border p-3 text-left transition-all ${
											isSelected
												? "border-accent/40 bg-accent/10 shadow-accent/5 shadow-md"
												: "border-border/10 bg-muted/10 hover:border-border/20 hover:bg-muted/20"
										}`}
									>
										<div className="mb-1.5 flex w-full items-center justify-between">
											<span className="max-w-[70%] truncate font-bold font-mono text-foreground text-sm">
												{session.name}
											</span>
											<span
												className={`inline-flex items-center rounded-full px-2 py-0.5 font-mono font-semibold text-[9px] uppercase tracking-wider ring-1 ${STATUS_CLASSES[session.status as keyof typeof STATUS_CLASSES] || "bg-zinc-500/10"}`}
											>
												{STATUS_LABELS[
													session.status as keyof typeof STATUS_LABELS
												] || session.status}
											</span>
										</div>

										<div className="flex w-full items-center justify-between font-mono text-[10px] text-muted-foreground">
											<span className="max-w-[60%] truncate">
												Misión: {session.missionName}
											</span>
											<span className="flex shrink-0 items-center gap-1.5 text-foreground/80">
												<Users className="size-3" /> {session.teamCount} Eq /{" "}
												{session.playerCount} Ag
											</span>
										</div>

										{session.scheduledAt && (
											<div className="mt-2 flex w-full items-center gap-1 border-border/5 border-t pt-2 font-mono text-[9px] text-muted-foreground/80">
												<Calendar className="size-2.5" />
												<span>
													Programada:{" "}
													{new Date(session.scheduledAt).toLocaleString(
														"es-ES",
													)}
												</span>
											</div>
										)}
									</button>
								);
							})
						)}
					</div>
				</div>

				{/* Column 2: Dashboard Console Area */}
				<div className="flex flex-col gap-6 overflow-hidden lg:col-span-8">
					{/* Selected detail panel or Placeholder */}
					{!selectedSessionId ? (
						<div className="flex flex-1 flex-col items-center justify-center rounded-xl border border-border/20 border-dashed bg-card/5 p-8 text-center">
							<div className="mb-3 flex size-14 animate-pulse items-center justify-center rounded-xl border border-border/20 bg-muted/10">
								<Gamepad2 className="size-7 text-muted-foreground/60" />
							</div>
							<h3 className="font-bold font-mono text-foreground text-sm uppercase tracking-widest">
								TERMINAL DE SELECCIÓN TÁCTICA
							</h3>
							<p className="mt-1 max-w-sm font-mono text-muted-foreground text-xs uppercase">
								Selecciona una operación de la lista lateral para enlazar la
								telemetría en tiempo real o inicia un nuevo despliegue.
							</p>
						</div>
					) : loadingDetail && !sessionDetail ? (
						<div className="flex flex-1 flex-col items-center justify-center gap-3">
							<RefreshCw className="size-8 animate-spin text-accent" />
							<span className="animate-pulse font-mono text-muted-foreground text-xs uppercase tracking-widest">
								Enlazando telemetría...
							</span>
						</div>
					) : sessionDetail ? (
						<div className="flex flex-1 flex-col gap-6 overflow-hidden">
							{/* Console Header Card */}
							<div className="relative shrink-0 overflow-hidden rounded-xl border border-border/10 bg-card/25 p-5">
								<div className="absolute top-0 right-0 h-[2px] w-1/3 bg-gradient-to-r from-transparent to-accent" />
								<div className="flex flex-col gap-4 sm:flex-row sm:items-start sm:justify-between">
									<div>
										<div className="mb-1.5 flex flex-wrap items-center gap-2">
											<span className="font-mono font-semibold text-muted-foreground text-xs tracking-wider">
												ID: {sessionDetail.id.substring(0, 8)}...
											</span>
											<span
												className={`inline-flex items-center rounded-full px-2.5 py-0.5 font-bold font-mono text-[10px] uppercase tracking-widest ring-1 ${STATUS_CLASSES[sessionDetail.status as keyof typeof STATUS_CLASSES] || "bg-zinc-500/10"}`}
											>
												{STATUS_LABELS[
													sessionDetail.status as keyof typeof STATUS_LABELS
												] || sessionDetail.status}
											</span>
										</div>
										<h2 className="font-bold font-mono text-2xl text-foreground tracking-tight">
											{sessionDetail.name}
										</h2>
										<p className="mt-1 font-mono text-muted-foreground text-xs uppercase">
											Misión:{" "}
											<span className="text-foreground">
												{sessionDetail.missionName}
											</span>
										</p>
										{(sessionDetail.status === "Activa" ||
											sessionDetail.status === "Pausada") && (
											<div className="mt-3">
												<SessionTimer
													endsAt={sessionDetail.endsAt}
													remainingSecondsOnPause={
														sessionDetail.remainingSecondsOnPause
													}
													status={sessionDetail.status}
												/>
											</div>
										)}
									</div>

									<div className="flex flex-wrap gap-2 sm:self-center">
										{/* Contextual Action buttons based on State Pattern */}
										{sessionDetail.status === "Programada" && (
											<Button
												onClick={() => handleTransition("Preparacion")}
												className="bg-blue-500 font-mono text-white text-xs uppercase hover:bg-blue-600"
											>
												<Plus className="mr-1 size-3.5" /> Abrir Registro
											</Button>
										)}

										{sessionDetail.status === "Preparacion" && (
											<Button
												onClick={() => {
													if (sessionDetail.teams.length === 0) {
														toast.error(
															"Debe existir al menos un equipo registrado antes de iniciar.",
														);
														return;
													}
													handleTransition("Activa");
												}}
												className="bg-emerald-500 font-black font-mono text-black text-xs uppercase hover:bg-emerald-600"
											>
												<Play className="mr-1 size-3.5 fill-black" /> Iniciar
												Misión
											</Button>
										)}

										{sessionDetail.status === "Activa" && (
											<>
												<Button
													onClick={handleAdvanceStage}
													disabled={
														missionDetail
															? sessionDetail.currentStageIndex >=
																missionDetail.stages.length - 1
															: false
													}
													className="bg-accent font-black font-mono text-background text-xs uppercase hover:bg-accent/80"
												>
													Avanzar Etapa{" "}
													<ChevronRight className="ml-0.5 size-4" />
												</Button>
												<Button
													onClick={() => handleTransition("Pausada")}
													className="border border-amber-500/30 bg-amber-500/15 font-mono text-amber-400 text-xs uppercase hover:bg-amber-500/20"
												>
													<Pause className="mr-1 size-3.5" /> Pausar
												</Button>
												<Button
													onClick={() => handleTransition("Finalizada")}
													className="bg-zinc-500 font-bold font-mono text-black text-xs uppercase hover:bg-zinc-600"
												>
													<CheckCircle2 className="mr-1 size-3.5" /> Finalizar
												</Button>
											</>
										)}

										{sessionDetail.status === "Pausada" && (
											<>
												<Button
													onClick={() => handleTransition("Activa")}
													className="bg-emerald-500 font-bold font-mono text-black text-xs uppercase hover:bg-emerald-600"
												>
													<Play className="mr-1 size-3.5 fill-black" /> Reanudar
												</Button>
												<Button
													onClick={() => handleTransition("Finalizada")}
													className="bg-zinc-500 font-bold font-mono text-black text-xs uppercase hover:bg-zinc-600"
												>
													<CheckCircle2 className="mr-1 size-3.5" /> Finalizar
												</Button>
											</>
										)}

										{/* Cancel button: enabled for any active operations */}
										{[
											"Programada",
											"Preparacion",
											"Activa",
											"Pausada",
										].includes(sessionDetail.status) && (
											<Button
												variant="destructive"
												onClick={() => handleTransition("Cancelada")}
												className="font-mono text-xs uppercase"
											>
												<XCircle className="mr-1 size-3.5" /> Cancelar
											</Button>
										)}
									</div>
								</div>
							</div>

							<div className="grid flex-1 grid-cols-1 gap-6 overflow-hidden md:grid-cols-12">
								{/* Grid Col 1: Stages and Teams status */}
								<div className="flex flex-col gap-6 overflow-hidden md:col-span-8">
									{/* Operations progress */}
									{missionDetail && (
										<div className="shrink-0 rounded-xl border border-border/10 bg-card/10 p-4">
											<h3 className="mb-3 flex items-center gap-1.5 font-bold font-mono text-muted-foreground text-xs uppercase tracking-widest">
												<History className="size-3.5 text-accent" /> AVANCE
												OPERATIVO DE ETAPAS
											</h3>
											<div className="flex items-center gap-2">
												{missionDetail.stages
													.sort((a, b) => a.order - b.order)
													.map((stage, idx) => {
														const isCurrent =
															idx === sessionDetail.currentStageIndex;
														const isCompleted =
															idx < sessionDetail.currentStageIndex;
														return (
															<div
																key={stage.id}
																className="flex flex-1 flex-col gap-1"
															>
																<div
																	className={`h-2 rounded-full transition-all ${
																		isCurrent
																			? "bg-accent shadow-accent/20 shadow-sm"
																			: isCompleted
																				? "bg-emerald-500"
																				: "bg-muted/30"
																	}`}
																/>
																<span
																	className={`mt-1 truncate text-center font-mono text-[9px] uppercase tracking-wider ${
																		isCurrent
																			? "font-bold text-accent"
																			: isCompleted
																				? "text-emerald-400"
																				: "text-muted-foreground/60"
																	}`}
																>
																	E{idx + 1}: {stage.name}
																</span>
															</div>
														);
													})}
											</div>
										</div>
									)}

									{/* Scoring (ranking) + Pistas (solicitudes): comparten un contenedor
									    con scroll propio para que no crezcan sin límite (RF-07/11/12/15). */}
									{selectedSessionId && (
										<div className="max-h-[32rem] shrink-0 space-y-6 overflow-y-auto pr-1">
											<ScoringPanel
												getAccessToken={getAccessToken}
												sessionId={selectedSessionId}
											/>
											<HintsPanel
												getAccessToken={getAccessToken}
												missionDetail={missionDetail}
												sessionId={selectedSessionId}
											/>
										</div>
									)}

									{/* Teams / Players Console */}
									<div className="flex flex-1 flex-col overflow-hidden rounded-xl border border-border/10 bg-card/10 p-4">
										<div className="mb-4 flex shrink-0 items-center justify-between border-border/10 border-b pb-3">
											<h3 className="flex items-center gap-1.5 font-bold font-mono text-muted-foreground text-xs uppercase tracking-widest">
												<Users className="size-3.5 text-accent" /> EQUIPOS Y
												AGENTES DE CAMPO
											</h3>

											{sessionDetail.status === "Preparacion" && (
												<form onSubmit={handleAddTeam} className="flex gap-2">
													<Input
														placeholder="Nombre de equipo..."
														value={newTeamName}
														onChange={(e) => setNewTeamName(e.target.value)}
														className="h-8 w-44 border-border/20 bg-muted/20 font-mono text-xs focus:border-accent/40"
													/>
													<Button
														type="submit"
														disabled={creatingTeam || !newTeamName.trim()}
														className="h-8 border border-accent/35 bg-accent/15 px-3 font-mono text-accent text-xs hover:bg-accent/20"
													>
														{creatingTeam ? "Añadiendo..." : "Añadir"}
													</Button>
												</form>
											)}
										</div>

										<div className="custom-scrollbar flex-1 space-y-4 overflow-y-auto pr-1">
											{sessionDetail.teams.length === 0 ? (
												<div className="flex flex-col items-center justify-center py-12 text-center text-muted-foreground">
													<UserPlus className="mb-2 size-8 opacity-50" />
													<p className="font-mono text-xs uppercase">
														No hay equipos registrados en esta sesión.
													</p>
													{sessionDetail.status === "Preparacion" && (
														<p className="mt-1 font-mono text-[10px] text-accent/80 uppercase">
															Usa la consola de arriba para registrar el primer
															equipo táctico.
														</p>
													)}
												</div>
											) : (
												<div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
													{sessionDetail.teams.map((team) => (
														<div
															key={team.id}
															className="flex flex-col rounded-lg border border-border/10 bg-muted/5 p-3"
														>
															<div className="mb-2 flex items-center justify-between border-border/5 border-b pb-2">
																<span className="flex items-center gap-1.5 font-bold font-mono text-foreground text-sm">
																	<span className="h-1.5 w-1.5 rounded-full bg-accent" />{" "}
																	{team.name}
																</span>
																<span className="rounded bg-muted/10 px-2 py-0.5 font-mono text-[10px] text-muted-foreground">
																	{team.players.length} Agentes
																</span>
															</div>

															<div className="flex-1 space-y-1.5">
																{team.players.length === 0 ? (
																	<span className="block py-2 font-mono text-[10px] text-muted-foreground/60 italic">
																		Esperando agentes de campo...
																	</span>
																) : (
																	team.players.map((player) => (
																		<div
																			key={player.id}
																			className="flex items-center justify-between rounded border border-border/5 bg-muted/20 px-2.5 py-1.5 font-mono text-xs"
																		>
																			<span className="flex items-center gap-1 font-bold text-foreground/90">
																				<User className="size-3 text-accent/75" />
																				{player.displayName}
																			</span>
																			<span className="text-[9px] text-muted-foreground/60">
																				{new Date(
																					player.joinedAt,
																				).toLocaleTimeString([], {
																					hour: "2-digit",
																					minute: "2-digit",
																				})}
																			</span>
																		</div>
																	))
																)}
															</div>
														</div>
													))}
												</div>
											)}
										</div>
									</div>
								</div>

								{/* Grid Col 2: Telemetry Logger Console */}
								<div className="flex flex-col overflow-hidden rounded-xl border border-border/10 bg-black/40 p-4 md:col-span-4">
									<h3 className="mb-3 flex shrink-0 items-center gap-1.5 border-border/10 border-b pb-3 font-bold font-mono text-muted-foreground text-xs uppercase tracking-widest">
										<span className="relative flex h-2 w-2">
											<span className="absolute inline-flex h-full w-full animate-ping rounded-full bg-accent opacity-75" />
											<span className="relative inline-flex h-2 w-2 rounded-full bg-accent" />
										</span>
										TICKER DE TELEMETRÍA
									</h3>

									<div className="custom-scrollbar flex flex-1 flex-col-reverse space-y-2.5 overflow-y-auto pr-1 font-mono text-[10px] leading-relaxed">
										<div ref={logsEndRef} />
										{logs.length === 0 ? (
											<span className="block py-10 text-center text-muted-foreground/60 italic">
												Esperando telemetría...
											</span>
										) : (
											logs.map((log) => {
												let color = "text-muted-foreground";
												if (log.type === "success") color = "text-emerald-400";
												if (log.type === "warning") color = "text-amber-400";
												if (log.type === "error") color = "text-rose-400";

												return (
													<div
														key={log.id}
														className="flex items-start gap-2 rounded px-1 py-1 hover:bg-muted/5"
													>
														<span className="shrink-0 font-bold text-muted-foreground/50">
															[{log.timestamp}]
														</span>
														<span className={`${color} break-all`}>
															{log.message}
														</span>
													</div>
												);
											})
										)}
									</div>
								</div>
							</div>
						</div>
					) : null}
				</div>
			</div>

			{/* Creation Overlay Modal Form */}
			{showCreateForm && (
				<div className="fixed inset-0 z-50 flex items-center justify-center bg-black/60 p-4 backdrop-blur-sm">
					<Card className="w-full max-w-md border-border/20 bg-background/95 shadow-2xl">
						<CardHeader className="border-border/10 border-b font-mono">
							<CardTitle className="font-bold text-foreground text-xl uppercase tracking-wide">
								Desplegar Nueva Operación
							</CardTitle>
							<CardDescription className="text-muted-foreground text-xs uppercase">
								Programar o iniciar una sesión de misión táctica
							</CardDescription>
						</CardHeader>
						<form onSubmit={handleCreateSession}>
							<CardContent className="space-y-4 pt-4 font-mono">
								<div className="space-y-1.5">
									<label
										htmlFor="newSessionName"
										className="block font-bold text-muted-foreground text-xs uppercase"
									>
										Nombre de Operación *
									</label>
									<Input
										id="newSessionName"
										required
										placeholder="Ej: Operación Eclipse"
										value={newSessionName}
										onChange={(e) => setNewSessionName(e.target.value)}
										className="border-border/20 bg-muted/10 font-mono text-xs focus:border-accent"
									/>
								</div>

								<div className="space-y-1.5">
									<label
										htmlFor="newSessionMissionId"
										className="block font-bold text-muted-foreground text-xs uppercase"
									>
										Seleccionar Misión *
									</label>
									{loadingMissions ? (
										<div className="flex h-9 items-center justify-center rounded border border-border/10 bg-muted/20">
											<RefreshCw className="size-4 animate-spin text-muted-foreground" />
										</div>
									) : (
										<select
											id="newSessionMissionId"
											required
											value={newSessionMissionId}
											onChange={(e) => setNewSessionMissionId(e.target.value)}
											className="h-9 w-full rounded border border-border/20 bg-muted/20 px-3 py-1.5 font-mono text-foreground text-xs outline-none ring-offset-background focus:border-accent"
										>
											<option value="" disabled className="bg-background">
												-- Seleccionar misión activa --
											</option>
											{activeMissions.length === 0 ? (
												<option value="" disabled className="bg-background">
													No hay misiones activas disponibles
												</option>
											) : (
												activeMissions.map((m) => (
													<option
														key={m.id}
														value={m.id}
														className="bg-background text-foreground"
													>
														{m.name} ({m.stagesCount} Etapas)
													</option>
												))
											)}
										</select>
									)}
								</div>

								<div className="space-y-1.5">
									<label
										htmlFor="newSessionScheduledAt"
										className="block font-bold text-muted-foreground text-xs uppercase"
									>
										Fecha y Hora Programación (Opcional)
									</label>
									<Input
										id="newSessionScheduledAt"
										type="datetime-local"
										value={newSessionScheduledAt}
										onChange={(e) => setNewSessionScheduledAt(e.target.value)}
										className="block border-border/20 bg-muted/10 font-mono text-xs focus:border-accent"
									/>
									<span className="block text-[10px] text-muted-foreground leading-normal">
										Si se deja en blanco, la partida se considerará lista para
										inicializar de inmediato.
									</span>
								</div>
							</CardContent>
							<CardFooter className="flex justify-end gap-3 border-border/10 border-t pt-4 font-mono">
								<Button
									type="button"
									variant="outline"
									onClick={() => setShowCreateForm(false)}
									className="border-border/20 font-mono text-xs uppercase hover:bg-muted/30"
								>
									Cancelar
								</Button>
								<Button
									type="submit"
									disabled={creatingSession || loadingMissions}
									className="bg-accent font-black font-mono text-background text-xs uppercase hover:bg-accent/80"
								>
									{creatingSession
										? "Iniciando Despliegue..."
										: "Confirmar Despliegue"}
								</Button>
							</CardFooter>
						</form>
					</Card>
				</div>
			)}
		</div>
	);
}
