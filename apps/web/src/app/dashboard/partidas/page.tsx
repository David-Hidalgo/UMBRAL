"use client";

import { Gamepad2 } from "lucide-react";

export default function PartidasPage() {
	return (
		<div className="flex h-full flex-col items-center justify-center gap-4 text-center">
			<div className="flex size-16 items-center justify-center rounded-2xl bg-foreground/5 ring-1 ring-foreground/10">
				<Gamepad2 className="size-8 text-muted-foreground" />
			</div>
			<div className="space-y-1">
				<h1 className="font-semibold text-foreground text-xl">
					Gestión de Partidas
				</h1>
				<p className="text-muted-foreground text-sm">
					Esta sección está en construcción.
				</p>
			</div>
			<span className="rounded-full border border-foreground/20 border-dashed px-4 py-1.5 text-[11px] text-muted-foreground">
				Próximamente
			</span>
		</div>
	);
}
