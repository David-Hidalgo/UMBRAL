"use client";

import { Skeleton } from "@umbral/ui/components/skeleton";

const SKELETON_IDS = ["sk-a", "sk-b", "sk-c", "sk-d", "sk-e", "sk-f"] as const;

export function UserListSkeleton() {
	return (
		<ul className="divide-y divide-foreground/6">
			{SKELETON_IDS.map((id) => (
				<li key={id} className="flex items-center gap-3 py-3.5">
					{/* Avatar */}
					<Skeleton className="size-9 shrink-0 rounded-full" />
					{/* Name + email */}
					<div className="min-w-0 flex-1 space-y-1.5">
						<Skeleton className="h-3.5 w-32 rounded" />
						<Skeleton className="h-3 w-48 rounded" />
					</div>
					{/* Role badge */}
					<Skeleton className="h-5 w-20 rounded-full" />
					{/* Date */}
					<Skeleton className="hidden h-3 w-24 rounded sm:block" />
					{/* Action button */}
					<Skeleton className="size-7 rounded" />
				</li>
			))}
		</ul>
	);
}
