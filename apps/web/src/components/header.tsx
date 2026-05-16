"use client";

import { Button } from "@umbral/ui/components/button";
import { LayoutDashboard, LogIn, LogOut, ShieldCheck } from "lucide-react";
import type { Route } from "next";
import Link from "next/link";
import { useRouter } from "next/navigation";

import { useAuth } from "./auth-provider";
import { ModeToggle } from "./mode-toggle";

export default function Header() {
	const router = useRouter();
	const { signOut, status, hasRealmRole } = useAuth();

	const isPanelUser =
		status === "authenticated" &&
		(hasRealmRole("admin") || hasRealmRole("operator"));

	const links: { to: Route; label: string }[] = [
		{ to: "/", label: "Inicio" },
		...(isPanelUser ? [{ to: "/dashboard" as Route, label: "Panel" }] : []),
	];

	return (
		<header className="border-b border-foreground/10 bg-background/80 backdrop-blur">
			<div className="flex flex-row items-center justify-between px-4 py-2">
				{/* Marca */}
				<div className="flex items-center gap-3">
					<Link href="/" className="flex items-center gap-2 group">
						<div className="flex size-7 items-center justify-center rounded-md border border-foreground/10 bg-foreground/5 transition-colors group-hover:bg-foreground/10">
							<ShieldCheck className="size-4 text-foreground/70" />
						</div>
						<span className="text-sm font-semibold tracking-widest text-foreground/90">
							UMBRAL
						</span>
					</Link>

					{/* Links de navegación */}
					{links.length > 0 && (
						<nav className="ml-4 flex items-center gap-1">
							{links.map(({ to, label }) => (
								<Link
									key={to}
									href={to}
									className="rounded-md px-3 py-1.5 text-sm text-muted-foreground transition-colors hover:bg-foreground/5 hover:text-foreground"
								>
									{label}
								</Link>
							))}
						</nav>
					)}
				</div>

				{/* Acciones */}
				<div className="flex items-center gap-2">
					{isPanelUser && (
						<Button
							size="sm"
							variant="ghost"
							onClick={() => router.push("/dashboard")}
							className="gap-1.5 rounded-md text-xs"
						>
							<LayoutDashboard className="size-3.5" />
							Panel
						</Button>
					)}

					{status === "authenticated" ? (
						<Button
							size="sm"
							variant="outline"
							onClick={() => void signOut()}
							className="gap-1.5 rounded-md text-xs"
						>
							<LogOut className="size-3.5" />
							Cerrar sesión
						</Button>
					) : (
						<Button
							size="sm"
							onClick={() => router.push("/login")}
							className="gap-1.5 rounded-md text-xs"
						>
							<LogIn className="size-3.5" />
							Iniciar sesión
						</Button>
					)}

					<ModeToggle />
				</div>
			</div>
		</header>
	);
}
