import type { AppRole } from "@umbral/auth/types";
import { env } from "@umbral/env/native";
import * as AuthSession from "expo-auth-session";
import type { TokenResponse } from "expo-auth-session";
import * as SecureStore from "expo-secure-store";
import * as WebBrowser from "expo-web-browser";
import {
	createContext,
	useCallback,
	useContext,
	useEffect,
	useState,
} from "react";
import type React from "react";

WebBrowser.maybeCompleteAuthSession();

const TOKENS_KEY = "umbral_tokens";
const APP_ROLES: AppRole[] = ["admin", "operator", "player"];

const BASE = `${env.EXPO_PUBLIC_KEYCLOAK_URL}/realms/${env.EXPO_PUBLIC_KEYCLOAK_REALM}/protocol/openid-connect`;

const discovery: AuthSession.DiscoveryDocument = {
	authorizationEndpoint: `${BASE}/auth`,
	tokenEndpoint: `${BASE}/token`,
	revocationEndpoint: `${BASE}/revoke`,
	endSessionEndpoint: `${BASE}/logout`,
};

interface StoredTokens {
	accessToken: string;
	refreshToken: string;
	idToken: string;
	expiresAt: number;
}

interface AuthUser {
	id: string;
	email: string;
	username: string;
	firstName?: string;
	lastName?: string;
	roles: AppRole[];
}

interface AuthContextType {
	user: AuthUser | null;
	accessToken: string | null;
	isAuthenticated: boolean;
	isLoading: boolean;
	login: () => Promise<void>;
	logout: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | null>(null);

function decodeJwtPayload(token: string): Record<string, unknown> {
	const [, payload] = token.split(".");
	const decoded = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
	return JSON.parse(decoded) as Record<string, unknown>;
}

function parseUserFromToken(accessToken: string): AuthUser {
	const payload = decodeJwtPayload(accessToken);
	const realmAccess = payload.realm_access as { roles?: string[] } | undefined;
	const allRoles = realmAccess?.roles ?? [];
	const appRoles = APP_ROLES.filter((role) => allRoles.includes(role));
	return {
		id: payload.sub as string,
		email: (payload.email as string) ?? "",
		username: (payload.preferred_username as string) ?? "",
		firstName: payload.given_name as string | undefined,
		lastName: payload.family_name as string | undefined,
		roles: appRoles,
	};
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
	const [user, setUser] = useState<AuthUser | null>(null);
	const [accessToken, setAccessToken] = useState<string | null>(null);
	const [isLoading, setIsLoading] = useState(true);

	const redirectUri = AuthSession.makeRedirectUri({ scheme: "umbral" });

	const [request, response, promptAsync] = AuthSession.useAuthRequest(
		{
			clientId: env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID,
			scopes: ["openid", "profile", "email", "offline_access"],
			redirectUri,
		},
		discovery,
	);

	const saveTokens = useCallback(async (tokenResponse: TokenResponse) => {
		const at = tokenResponse.accessToken;
		if (!at) return;
		const tokens: StoredTokens = {
			accessToken: at,
			refreshToken: tokenResponse.refreshToken ?? "",
			idToken: tokenResponse.idToken ?? "",
			expiresAt: Date.now() + (tokenResponse.expiresIn ?? 300) * 1000,
		};
		await SecureStore.setItemAsync(TOKENS_KEY, JSON.stringify(tokens));
		setAccessToken(at);
		setUser(parseUserFromToken(at));
	}, []);

	const refreshTokens = useCallback(
		async (rt: string) => {
			try {
				const tokenResponse = await AuthSession.refreshAsync(
					{ clientId: env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID, refreshToken: rt },
					discovery,
				);
				await saveTokens(tokenResponse);
			} catch {
				await SecureStore.deleteItemAsync(TOKENS_KEY);
				setAccessToken(null);
				setUser(null);
			}
		},
		[saveTokens],
	);

	useEffect(() => {
		async function restoreSession() {
			try {
				const stored = await SecureStore.getItemAsync(TOKENS_KEY);
				if (!stored) {
					setIsLoading(false);
					return;
				}
				const tokens = JSON.parse(stored) as StoredTokens;
				if (Date.now() < tokens.expiresAt) {
					setAccessToken(tokens.accessToken);
					setUser(parseUserFromToken(tokens.accessToken));
					setIsLoading(false);
				} else if (tokens.refreshToken) {
					await refreshTokens(tokens.refreshToken).finally(() =>
						setIsLoading(false),
					);
				} else {
					await SecureStore.deleteItemAsync(TOKENS_KEY);
					setIsLoading(false);
				}
			} catch {
				await SecureStore.deleteItemAsync(TOKENS_KEY);
				setIsLoading(false);
			}
		}
		void restoreSession();
	}, [refreshTokens]);

	useEffect(() => {
		if (response?.type !== "success" || !request) return;
		void (async () => {
			try {
				const tokenResponse = await AuthSession.exchangeCodeAsync(
					{
						clientId: env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID,
						code: response.params.code,
						redirectUri,
						extraParams: { code_verifier: request.codeVerifier ?? "" },
					},
					discovery,
				);
				await saveTokens(tokenResponse);
			} catch {
				await SecureStore.deleteItemAsync(TOKENS_KEY);
			}
		})();
	}, [response, request, redirectUri, saveTokens]);

	async function login() {
		await promptAsync();
	}

	async function logout() {
		const stored = await SecureStore.getItemAsync(TOKENS_KEY);
		await SecureStore.deleteItemAsync(TOKENS_KEY);
		setAccessToken(null);
		setUser(null);

		const tokens = stored ? (JSON.parse(stored) as StoredTokens) : null;
		if (!tokens?.idToken) return;

		const params = new URLSearchParams({
			client_id: env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID,
			post_logout_redirect_uri: redirectUri,
			id_token_hint: tokens.idToken,
		});
		await WebBrowser.openBrowserAsync(
			`${discovery.endSessionEndpoint}?${params.toString()}`,
		);
	}

	return (
		<AuthContext.Provider
			value={{
				user,
				accessToken,
				isAuthenticated: !!user,
				isLoading,
				login,
				logout,
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
