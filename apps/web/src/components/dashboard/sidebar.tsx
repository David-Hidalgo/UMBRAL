"use client";

import { Button } from "@umbral/ui/components/button";
import { Gamepad2, LogOut, ScrollText, ShieldCheck, Users } from "lucide-react";
import type { Route } from "next";
import Link from "next/link";
import { usePathname } from "next/navigation";

import { useAuth } from "@/components/auth-provider";

interface NavItem {
	href: Route;
	label: string;
	icon: React.ReactNode;
	requiredRole?: string;
}

const NAV_ITEMS: NavItem[] = [
	{
		href: "/dashboard/partidas",
		label: "Partidas",
		icon: <Gamepad2 className="size-4" />,
	},
	{
		href: "/dashboard/misiones",
		label: "Misiones",
		icon: <ScrollText className="size-4" />,
	},
	{
		href: "/dashboard/usuarios",
		label: "Usuarios",
		icon: <Users className="size-4" />,
		requiredRole: "admin",
	},
];

export function DashboardSidebar() {
	const { user, signOut, hasRealmRole } = useAuth();
	const pathname = usePathname();

	const visibleItems = NAV_ITEMS.filter(
		(item) => !item.requiredRole || hasRealmRole(item.requiredRole),
	);

	const displayName = user?.name ?? user?.preferred_username ?? "Usuario";
	const displayEmail = user?.email;

	return (
		<aside className="flex h-full flex-col border-white/8 border-r bg-sidebar">
			{/* Brand */}
			<div className="flex items-center gap-2.5 border-white/8 border-b px-5 py-4">
				<div className="flex size-8 items-center justify-center rounded-lg bg-primary/20 ring-1 ring-primary/30">
					<ShieldCheck className="size-4 text-primary" />
				</div>
				<span className="font-semibold text-foreground text-sm tracking-wide">
					UMBRAL
				</span>
				<span className="ml-auto rounded-full bg-primary/15 px-2 py-0.5 font-medium text-[10px] text-primary ring-1 ring-primary/25">
					{hasRealmRole("admin") ? "Admin" : "Operator"}
				</span>
			</div>

			{/* Navigation */}
			<nav className="flex-1 space-y-1 p-3">
				<p className="mb-2 px-2 font-semibold text-[10px] text-muted-foreground/60 uppercase tracking-widest">
					Panel
				</p>
				{visibleItems.map((item) => {
					const isActive =
						pathname === item.href || pathname.startsWith(`${item.href}/`);
					return (
						<Link
							key={item.href}
							href={item.href}
							className={[
								"group flex items-center gap-3 rounded-lg px-3 py-2.5 font-medium text-sm transition-all duration-150",
								isActive
									? "bg-primary/15 text-primary ring-1 ring-primary/20"
									: "text-muted-foreground hover:bg-foreground/6 hover:text-foreground",
							].join(" ")}
						>
							<span
								className={[
									"transition-colors",
									isActive
										? "text-primary"
										: "text-muted-foreground group-hover:text-foreground",
								].join(" ")}
							>
								{item.icon}
							</span>
							{item.label}
						</Link>
					);
				})}
			</nav>

			{/* User info + logout */}
			<div className="border-white/8 border-t p-3">
				<div className="mb-2 flex items-center gap-3 rounded-lg px-3 py-2">
					<div className="flex size-8 shrink-0 items-center justify-center rounded-full bg-foreground/10 font-semibold text-foreground text-xs">
						{displayName.slice(0, 1).toUpperCase()}
					</div>
					<div className="min-w-0 flex-1">
						<p className="truncate font-medium text-foreground text-sm">
							{displayName}
						</p>
						{displayEmail && (
							<p className="truncate text-[11px] text-muted-foreground">
								{displayEmail}
							</p>
						)}
					</div>
				</div>
				<Button
					size="sm"
					variant="ghost"
					className="w-full justify-start gap-2 text-muted-foreground hover:text-foreground"
					onClick={() => void signOut()}
				>
					<LogOut className="size-4" />
					Cerrar sesión
				</Button>
			</div>
		</aside>
	);
}
