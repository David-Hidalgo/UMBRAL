"use client";

import { DashboardSidebar } from "@/components/dashboard/sidebar";
import { RoleGuard } from "@/components/role-guard";

export default function DashboardLayout({
	children,
}: {
	children: React.ReactNode;
}) {
	return (
		<RoleGuard requiredRoles={["admin", "operator"]}>
			<div className="flex h-full overflow-hidden">
				<div className="w-60 shrink-0">
					<DashboardSidebar />
				</div>
				<main className="flex-1 overflow-y-auto bg-background/50 p-6">
					{children}
				</main>
			</div>
		</RoleGuard>
	);
}
