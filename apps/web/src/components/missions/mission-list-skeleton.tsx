"use client";

import { Card, CardContent, CardHeader } from "@umbral/ui/components/card";
import { Skeleton } from "@umbral/ui/components/skeleton";

const SKELETON_IDS = ["sk-m1", "sk-m2", "sk-m3", "sk-m4"] as const;

export function MissionListSkeleton() {
	return (
		<div className="grid gap-4 sm:grid-cols-2">
			{SKELETON_IDS.map((id) => (
				<Card
					key={id}
					className="flex flex-col justify-between border-foreground/10 bg-card"
				>
					<CardHeader className="space-y-3">
						<div className="flex items-start justify-between gap-2">
							<Skeleton className="h-5 w-40 rounded" />
							<Skeleton className="h-4.5 w-16 rounded-full" />
						</div>
						<div className="space-y-1.5">
							<Skeleton className="h-3.5 w-full rounded" />
							<Skeleton className="h-3.5 w-5/6 rounded" />
						</div>
					</CardHeader>
					<CardContent className="grid grid-cols-2 gap-2 border-foreground/5 border-t pt-4">
						<Skeleton className="h-9 rounded-lg" />
						<Skeleton className="h-9 rounded-lg" />
					</CardContent>
				</Card>
			))}
		</div>
	);
}
