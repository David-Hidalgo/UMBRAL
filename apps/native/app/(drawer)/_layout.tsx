import { Ionicons } from "@expo/vector-icons";
import {
	DrawerContentScrollView,
	DrawerItem,
	type DrawerContentComponentProps,
} from "@react-navigation/drawer";
import type { AppRole } from "@umbral/auth/types";
import { useAuth } from "@/contexts/auth-context";
import { Redirect } from "expo-router";
import { Drawer } from "expo-router/drawer";
import { Button, Chip, useThemeColor } from "heroui-native";
import React, { useCallback } from "react";
import { Text, View } from "react-native";

import { ThemeToggle } from "@/components/theme-toggle";

function formatRole(role: AppRole): string {
	const labels: Record<AppRole, string> = {
		admin: "Admin",
		operator: "Operador",
		player: "Jugador",
	};
	return labels[role];
}

function getFullName(user: {
	firstName?: string;
	lastName?: string;
	username: string;
}): string {
	const full = [user.firstName, user.lastName].filter(Boolean).join(" ");
	return full || user.username;
}

function getInitial(user: {
	firstName?: string;
	username: string;
}): string {
	const name = user.firstName ?? user.username;
	return name.slice(0, 1).toUpperCase();
}

function CustomDrawerContent(props: DrawerContentComponentProps) {
	const { state, navigation } = props;
	const { user, logout } = useAuth();
	const themeColorAccent = useThemeColor("accent");
	const themeColorMuted = useThemeColor("muted");
	const themeColorForeground = useThemeColor("foreground");

	const isIndexActive = state.routes[state.index]?.name === "index";
	const primaryRole = user?.roles[0];

	return (
		<DrawerContentScrollView
			{...props}
			contentContainerStyle={{ flexGrow: 1 }}
			className="bg-background"
		>
			<View className="flex-row items-center gap-2.5 border-foreground/8 border-b px-5 py-4">
				<View className="size-8 items-center justify-center rounded-lg bg-primary/20 ring-1 ring-primary/30">
					<Ionicons
						name="shield-checkmark-outline"
						size={16}
						color={themeColorAccent}
					/>
				</View>
				<Text className="font-semibold text-foreground text-sm tracking-wide">
					UMBRAL
				</Text>
				{primaryRole && (
					<Chip
						variant="secondary"
						color="accent"
						size="sm"
						className="ml-auto"
					>
						<Chip.Label>{formatRole(primaryRole)}</Chip.Label>
					</Chip>
				)}
			</View>

			<View className="flex-1 p-3">
				<Text className="mb-2 px-2 font-semibold text-[10px] text-muted uppercase tracking-widest">
					Panel
				</Text>
				<View className={isIndexActive ? "rounded-lg bg-primary/15" : "rounded-lg"}>
					<DrawerItem
						label={({ focused }) => (
							<Text
								className={
									focused || isIndexActive
										? "font-medium text-primary text-sm"
										: "font-medium text-muted-foreground text-sm"
								}
							>
								Inicio
							</Text>
						)}
						icon={({ focused, size, color }) => (
							<Ionicons
								name="home-outline"
								size={size}
								color={
									focused || isIndexActive ? themeColorAccent : color
								}
							/>
						)}
						focused={isIndexActive}
						onPress={() => navigation.navigate("index")}
						activeTintColor={themeColorAccent}
						inactiveTintColor={themeColorMuted}
						activeBackgroundColor="transparent"
						inactiveBackgroundColor="transparent"
					/>
				</View>
			</View>

			<View className="border-foreground/8 border-t p-3">
				{user && (
					<View className="mb-2 flex-row items-center gap-3 rounded-lg px-3 py-2">
						<View className="size-8 shrink-0 items-center justify-center rounded-full bg-foreground/10">
							<Text className="font-semibold text-foreground text-xs">
								{getInitial(user)}
							</Text>
						</View>
						<View className="min-w-0 flex-1">
							<Text numberOfLines={1} className="font-medium text-foreground text-sm">
								{getFullName(user)}
							</Text>
							<Text numberOfLines={1} className="text-[11px] text-muted">
								{user.email}
							</Text>
						</View>
					</View>
				)}
				<Button
					variant="ghost"
					size="sm"
					className="w-full justify-start gap-2"
					onPress={() => void logout()}
				>
					<Ionicons
						name="log-out-outline"
						size={16}
						color={themeColorForeground}
					/>
					<Button.Label>Cerrar sesión</Button.Label>
				</Button>
			</View>
		</DrawerContentScrollView>
	);
}

function DrawerLayout() {
	const { isAuthenticated, isLoading } = useAuth();
	const themeColorForeground = useThemeColor("foreground");
	const themeColorBackground = useThemeColor("background");

	const renderThemeToggle = useCallback(() => <ThemeToggle />, []);

	if (!isLoading && !isAuthenticated) {
		return <Redirect href="/(auth)/login" />;
	}

	return (
		<Drawer
			drawerContent={(props) => <CustomDrawerContent {...props} />}
			screenOptions={{
				headerTintColor: themeColorForeground,
				headerStyle: { backgroundColor: themeColorBackground },
				headerTitleStyle: {
					fontWeight: "600",
					color: themeColorForeground,
				},
				headerRight: renderThemeToggle,
				drawerStyle: { backgroundColor: themeColorBackground },
			}}
		>
			<Drawer.Screen
				name="index"
				options={{
					headerTitle: "Inicio",
					drawerItemStyle: { display: "none" },
				}}
			/>
		</Drawer>
	);
}

export default DrawerLayout;
