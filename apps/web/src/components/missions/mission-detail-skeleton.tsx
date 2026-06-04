"use client";

import { Card, CardContent, CardHeader } from "@umbral/ui/components/card";
import { Skeleton } from "@umbral/ui/components/skeleton";

const STAGE_IDS = ["sk-stage-1", "sk-stage-2"] as const;

export function MissionDetailSkeleton() {
	return (
		<div className="space-y-8">
			{/* Header */}
			<div className="space-y-3">
				<Skeleton className="h-4 w-32 rounded" />
				<div className="flex items-start justify-between gap-4">
					<Skeleton className="h-8 w-64 max-w-full rounded" />
					<Skeleton className="h-5 w-20 shrink-0 rounded-full" />
				</div>
				<Skeleton className="h-4 w-full max-w-2xl rounded" />
				<Skeleton className="h-4 w-4/5 max-w-xl rounded" />
			</div>

			{/* Stages */}
			{STAGE_IDS.map((stageId) => (
				<div key={stageId} className="space-y-4">
					<div className="space-y-1.5 border-foreground/8 border-b pb-2">
						<Skeleton className="h-4 w-48 rounded" />
						<Skeleton className="h-3.5 w-full max-w-md rounded" />
					</div>
					<div className="space-y-3">
						<Card className="border-foreground/10 bg-card">
							<CardHeader className="pb-2">
								<div className="flex items-center justify-between gap-2">
									<Skeleton className="h-4 w-36 rounded" />
									<Skeleton className="h-4 w-12 rounded" />
								</div>
							</CardHeader>
							<CardContent className="space-y-3 pt-0">
								<Skeleton className="h-4 w-full rounded" />
								<div className="flex flex-wrap gap-2">
									<Skeleton className="h-6 w-24 rounded-full" />
									<Skeleton className="h-6 w-28 rounded-full" />
									<Skeleton className="h-6 w-20 rounded-full" />
								</div>
							</CardContent>
						</Card>
						<Card className="border-foreground/10 bg-card">
							<CardHeader className="pb-2">
								<div className="flex items-center justify-between gap-2">
									<Skeleton className="h-4 w-40 rounded" />
									<Skeleton className="h-4 w-12 rounded" />
								</div>
							</CardHeader>
							<CardContent className="space-y-2 pt-0">
								<Skeleton className="h-3.5 w-3/4 rounded" />
								<Skeleton className="h-6 w-32 rounded" />
								<Skeleton className="h-3.5 w-full rounded" />
								<Skeleton className="h-3.5 w-5/6 rounded" />
							</CardContent>
						</Card>
					</div>
				</div>
			))}
		</div>
	);
}
