"use client";

import { useRouter } from "next/navigation";
import { useEffect } from "react";

import { useAuth } from "@/components/auth-provider";

interface RoleGuardProps {
	/** At least one of these roles must be present in the token */
	requiredRoles: string[];
	children: React.ReactNode;
}

export function RoleGuard({ requiredRoles, children }: RoleGuardProps) {
	const { status, hasRealmRole } = useAuth();
	const router = useRouter();

	const hasAccess =
		status === "authenticated" &&
		requiredRoles.some((role) => hasRealmRole(role));

	useEffect(() => {
		if (status === "loading") return;

		if (status === "unauthenticated") {
			router.replace("/login?returnTo=/dashboard");
			return;
		}

		if (!hasAccess) {
			router.replace("/unauthorized");
		}
	}, [status, hasAccess, router]);

	if (status === "loading") {
		return (
			<div className="flex h-full items-center justify-center">
				<div className="flex flex-col items-center gap-3">
					<div className="h-8 w-8 animate-spin rounded-full border-2 border-foreground/20 border-t-foreground/80" />
					<p className="text-muted-foreground text-sm">Verificando acceso…</p>
				</div>
			</div>
		);
	}

	if (!hasAccess) {
		return null;
	}

	return <>{children}</>;
}
