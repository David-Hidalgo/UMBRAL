"use client";

import {
	createKeycloakClient,
	initializeKeycloak,
	loginWithKeycloak,
	logoutFromKeycloak,
} from "@umbral/auth/keycloak";
import type { KeycloakConfig } from "@umbral/auth/types";
import { env } from "@umbral/env/web";
import { createContext, useContext, useEffect, useMemo, useState } from "react";

type AuthStatus = "loading" | "authenticated" | "unauthenticated";

type StoredUser = {
	name?: string;
	preferred_username?: string;
	email?: string;
};

type KeycloakProfile = {
	firstName?: string;
	lastName?: string;
	username?: string;
	email?: string;
};

type AuthSession = {
	user: StoredUser;
};

type AuthContextValue = {
	status: AuthStatus;
	session: AuthSession | null;
	user: StoredUser | null;
	beginLogin: (options?: { returnTo?: string }) => Promise<void>;
	signOut: () => Promise<void>;
	refreshSession: () => Promise<void>;
	getAccessToken: (minValidity?: number) => Promise<string | null>;
	/** Delegates to keycloak-js hasRealmRole – works after initialization */
	hasRealmRole: (role: string) => boolean;
	/** Returns the `sub` claim of the current token (Keycloak user ID), or null if not authenticated. */
	getSubjectId: () => string | null;
};

const AuthContext = createContext<AuthContextValue | null>(null);

function getAuthConfig(): KeycloakConfig {
	return {
		baseUrl: env.NEXT_PUBLIC_KEYCLOAK_URL,
		realm: env.NEXT_PUBLIC_KEYCLOAK_REALM,
		clientId: env.NEXT_PUBLIC_KEYCLOAK_CLIENT_ID,
	};
}

function toStoredUser(profile: KeycloakProfile): StoredUser {
	return {
		name:
			profile.firstName || profile.lastName
				? [profile.firstName, profile.lastName].filter(Boolean).join(" ")
				: profile.username,
		preferred_username: profile.username,
		email: profile.email,
	};
}

function buildSession(profile: KeycloakProfile): AuthSession {
	return {
		user: toStoredUser(profile),
	};
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
	const [status, setStatus] = useState<AuthStatus>("loading");
	const [session, setSession] = useState<AuthSession | null>(null);
	const keycloakClient = useMemo(
		() => createKeycloakClient(getAuthConfig()),
		[],
	);

	useEffect(() => {
		let cancelled = false;

		const setUnauthenticated = () => {
			if (cancelled) return;
			setSession(null);
			setStatus("unauthenticated");
		};

		const syncSessionFromClient = async () => {
			const profile = await keycloakClient.loadUserProfile();
			if (cancelled) return;
			setSession(buildSession(profile));
			setStatus("authenticated");
		};

		const handleSyncError = (error: unknown) => {
			console.error("Failed to load Keycloak profile.", error);
			setUnauthenticated();
		};

		keycloakClient.onAuthSuccess = () => {
			void syncSessionFromClient().catch(handleSyncError);
		};

		keycloakClient.onAuthRefreshSuccess = () => {
			void syncSessionFromClient().catch(handleSyncError);
		};

		keycloakClient.onAuthLogout = () => {
			setUnauthenticated();
		};

		keycloakClient.onAuthError = () => {
			console.error("Keycloak authentication error.");
			setUnauthenticated();
		};

		keycloakClient.onTokenExpired = () => {
			void keycloakClient
				.updateToken(60)
				.then((refreshed) => {
					if (refreshed) {
						return syncSessionFromClient();
					}

					if (!keycloakClient.tokenParsed) {
						setUnauthenticated();
					}

					return undefined;
				})
				.catch((error) => {
					console.error("Failed to refresh Keycloak token.", error);
					setUnauthenticated();
				});
		};

		const bootstrap = async () => {
			try {
				const authenticated = await initializeKeycloak(keycloakClient, {
					onLoad: "check-sso",
					silentCheckSsoRedirectUri: `${window.location.origin}/silent-check-sso.html`,
				});

				if (!authenticated) {
					setUnauthenticated();
					return;
				}

				await syncSessionFromClient();
			} catch (error) {
				console.error("Failed to initialize Keycloak.", error);
				setUnauthenticated();
			}
		};

		void bootstrap();

		return () => {
			cancelled = true;
		};
	}, [keycloakClient]);

	const beginLogin: AuthContextValue["beginLogin"] = async (options) => {
		const returnTo = options?.returnTo ?? "/";
		const redirectUri = `${window.location.origin}${returnTo}`;

		await loginWithKeycloak(keycloakClient, {
			redirectUri,
		});
	};

	const refreshSession: AuthContextValue["refreshSession"] = async () => {
		if (!keycloakClient.authenticated) {
			setSession(null);
			setStatus("unauthenticated");
			return;
		}

		try {
			await keycloakClient.updateToken(60);
			const profile = await keycloakClient.loadUserProfile();
			setSession(buildSession(profile));
			setStatus("authenticated");
		} catch (error) {
			console.error("Failed to refresh Keycloak session.", error);
			setSession(null);
			setStatus("unauthenticated");
			throw error;
		}
	};

	const getAccessToken: AuthContextValue["getAccessToken"] = async (
		minValidity = 60,
	) => {
		if (!keycloakClient.authenticated) {
			setSession(null);
			setStatus("unauthenticated");
			return null;
		}

		try {
			await keycloakClient.updateToken(minValidity);
		} catch (error) {
			console.error("Failed to refresh Keycloak token.", error);
			setSession(null);
			setStatus("unauthenticated");
			throw error;
		}

		return keycloakClient.token ?? null;
	};

	const signOut: AuthContextValue["signOut"] = async () => {
		setSession(null);
		setStatus("unauthenticated");

		await logoutFromKeycloak(keycloakClient, {
			redirectUri: `${window.location.origin}/`,
		});
	};

	const hasRealmRole = (role: string): boolean => {
		if (!keycloakClient.authenticated) {
			return false;
		}

		return keycloakClient.hasRealmRole(role);
	};

	const getSubjectId = (): string | null => {
		if (!keycloakClient.authenticated) return null;
		return keycloakClient.tokenParsed?.sub ?? null;
	};

	return (
		<AuthContext.Provider
			value={{
				status,
				session,
				user: session?.user ?? null,
				beginLogin,
				signOut,
				refreshSession,
				getAccessToken,
				hasRealmRole,
				getSubjectId,
			}}
		>
			{children}
		</AuthContext.Provider>
	);
}

export function useAuth() {
	const context = useContext(AuthContext);

	if (!context) {
		throw new Error("useAuth must be used within an AuthProvider.");
	}

	return context;
}
