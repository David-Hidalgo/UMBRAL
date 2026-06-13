import { env } from "@umbral/env/native";
import * as Crypto from "expo-crypto";
import * as Linking from "expo-linking";
import * as ExpoSecureStore from "expo-secure-store";
import * as WebBrowser from "expo-web-browser";
import {
	createContext,
	useCallback,
	useContext,
	useEffect,
	useMemo,
	useState,
} from "react";
import { Platform } from "react-native";

const SecureStore = {
	setItemAsync: async (key: string, value: string) => {
		if (Platform.OS === "web") {
			sessionStorage.setItem(key, value);
		} else {
			await ExpoSecureStore.setItemAsync(key, value);
		}
	},
	getItemAsync: async (key: string): Promise<string | null> => {
		if (Platform.OS === "web") {
			return sessionStorage.getItem(key);
		}
		return await ExpoSecureStore.getItemAsync(key);
	},
	deleteItemAsync: async (key: string) => {
		if (Platform.OS === "web") {
			sessionStorage.removeItem(key);
		} else {
			await ExpoSecureStore.deleteItemAsync(key);
		}
	},
};

// Complete the auth session redirects in the browser
WebBrowser.maybeCompleteAuthSession();

export type AuthStatus = "loading" | "authenticated" | "unauthenticated";

export type StoredUser = {
	name?: string;
	preferred_username?: string;
	email?: string;
};

export type AuthSession = {
	user: StoredUser;
};

export type AuthContextValue = {
	status: AuthStatus;
	session: AuthSession | null;
	user: StoredUser | null;
	beginLogin: (options?: { action?: "login" | "register" }) => Promise<void>;
	signOut: () => Promise<void>;
	refreshSession: () => Promise<void>;
	getAccessToken: (minValidity?: number) => Promise<string | null>;
	hasRealmRole: (role: string) => boolean;
	getSubjectId: () => string | null;
};

const AuthContext = createContext<AuthContextValue | null>(null);

// Utility functions for PKCE and Base64Url encoding
function base64UrlEncode(bytes: Uint8Array): string {
	const chars =
		"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789-_";
	let result = "";
	let i = 0;
	const len = bytes.length;
	while (i < len) {
		const byte1 = bytes[i++];
		const byte2 = i < len ? bytes[i++] : Number.NaN;
		const byte3 = i < len ? bytes[i++] : Number.NaN;

		const enc1 = byte1 >> 2;
		const enc2 = ((byte1 & 3) << 4) | (Number.isNaN(byte2) ? 0 : byte2 >> 4);
		const enc3 = Number.isNaN(byte2)
			? Number.NaN
			: ((byte2 & 15) << 2) | (Number.isNaN(byte3) ? 0 : byte3 >> 6);
		const enc4 = Number.isNaN(byte3) ? Number.NaN : byte3 & 63;

		result += chars[enc1] + chars[enc2];
		if (!Number.isNaN(enc3)) {
			result += chars[enc3];
		}
		if (!Number.isNaN(enc4)) {
			result += chars[enc4];
		}
	}
	return result;
}

function generateCodeVerifier(): string {
	const bytes = Crypto.getRandomBytes(32);
	return base64UrlEncode(bytes);
}

async function generateCodeChallenge(verifier: string): Promise<string> {
	const digest = await Crypto.digestStringAsync(
		Crypto.CryptoDigestAlgorithm.SHA256,
		verifier,
		{ encoding: Crypto.CryptoEncoding.BASE64 },
	);
	return digest.replace(/\+/g, "-").replace(/\//g, "_").replace(/=+$/, "");
}

// Utility functions for decoding JWTs in React Native
function decodeBase64(input: string): string {
	const chars =
		"ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789+/";
	const str = input.replace(/=+$/, "");
	let output = "";
	let buffer = 0;
	let bits = 0;
	for (let i = 0; i < str.length; i++) {
		const char = str[i];
		const val = chars.indexOf(char);
		if (val === -1) continue;
		buffer = (buffer << 6) | val;
		bits += 6;
		if (bits >= 8) {
			bits -= 8;
			const byte = (buffer >> bits) & 0xff;
			output += String.fromCharCode(byte);
		}
	}
	return output;
}

export interface JwtPayload {
	name?: string;
	preferred_username?: string;
	given_name?: string;
	email?: string;
	sub?: string;
	exp: number;
	realm_access?: {
		roles: string[];
	};
}

function decodeJwt(token: string): JwtPayload | null {
	try {
		const parts = token.split(".");
		if (parts.length !== 3) return null;
		const payloadBase64 = parts[1].replace(/-/g, "+").replace(/_/g, "/");
		const binaryString = decodeBase64(payloadBase64);
		const jsonStr = decodeURIComponent(
			Array.from(binaryString)
				.map((c) => `%${`00${c.charCodeAt(0).toString(16)}`.slice(-2)}`)
				.join(""),
		);
		return JSON.parse(jsonStr);
	} catch (e) {
		console.error("Failed to decode JWT", e);
		return null;
	}
}

export function AuthProvider({ children }: { children: React.ReactNode }) {
	const [status, setStatus] = useState<AuthStatus>("loading");
	const [session, setSession] = useState<AuthSession | null>(null);
	const [accessToken, setAccessToken] = useState<string | null>(null);

	// Helper to extract session profile from id_token payload
	const syncSessionFromTokens = useCallback(
		(accToken: string, idToken: string) => {
			const decodedId = decodeJwt(idToken) || decodeJwt(accToken);
			if (!decodedId) {
				throw new Error("Unable to decode authentication tokens");
			}

			const userProfile: StoredUser = {
				name:
					decodedId.name ||
					decodedId.preferred_username ||
					decodedId.given_name,
				preferred_username: decodedId.preferred_username || decodedId.sub,
				email: decodedId.email,
			};

			setAccessToken(accToken);
			setSession({ user: userProfile });
			setStatus("authenticated");
		},
		[],
	);

	const clearSessionState = useCallback(() => {
		setAccessToken(null);
		setSession(null);
		setStatus("unauthenticated");
	}, []);

	const saveStoredTokens = useCallback(
		async (tokenData: {
			access_token: string;
			refresh_token?: string;
			id_token?: string;
		}) => {
			await SecureStore.setItemAsync(
				"umbral_access_token",
				tokenData.access_token,
			);
			if (tokenData.refresh_token) {
				await SecureStore.setItemAsync(
					"umbral_refresh_token",
					tokenData.refresh_token,
				);
			}
			if (tokenData.id_token) {
				await SecureStore.setItemAsync("umbral_id_token", tokenData.id_token);
			}
		},
		[],
	);

	const clearStoredTokens = useCallback(async () => {
		await SecureStore.deleteItemAsync("umbral_access_token");
		await SecureStore.deleteItemAsync("umbral_refresh_token");
		await SecureStore.deleteItemAsync("umbral_id_token");
		await SecureStore.deleteItemAsync("umbral_code_verifier");
	}, []);

	const refreshTokensRequest = useCallback(
		async (refreshToken: string) => {
			const tokenUrl = `${env.EXPO_PUBLIC_KEYCLOAK_URL}/realms/${env.EXPO_PUBLIC_KEYCLOAK_REALM}/protocol/openid-connect/token`;
			const response = await fetch(tokenUrl, {
				method: "POST",
				headers: {
					"Content-Type": "application/x-www-form-urlencoded",
				},
				body: new URLSearchParams({
					grant_type: "refresh_token",
					client_id: env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID,
					refresh_token: refreshToken,
				}).toString(),
			});

			if (!response.ok) {
				throw new Error(
					`Token refresh request failed with status: ${response.status}`,
				);
			}

			const tokenData = await response.json();
			await saveStoredTokens(tokenData);
			return tokenData;
		},
		[saveStoredTokens],
	);

	// Setup initial session from secure store on mount
	useEffect(() => {
		const bootstrap = async () => {
			try {
				const accToken = await SecureStore.getItemAsync("umbral_access_token");
				const refToken = await SecureStore.getItemAsync("umbral_refresh_token");
				const idToken = await SecureStore.getItemAsync("umbral_id_token");

				if (!accToken || !refToken || !idToken) {
					clearSessionState();
					return;
				}

				// Validate access token expiry
				const decodedAcc = decodeJwt(accToken);
				if (decodedAcc && decodedAcc.exp * 1000 > Date.now() + 10000) {
					// Access token is still valid (at least 10s left)
					syncSessionFromTokens(accToken, idToken);
				} else {
					// Access token expired, attempt to refresh
					try {
						const tokenData = await refreshTokensRequest(refToken);
						syncSessionFromTokens(
							tokenData.access_token,
							tokenData.id_token || idToken,
						);
					} catch (e) {
						console.warn("Bootstrap token refresh failed, signing out", e);
						await clearStoredTokens();
						clearSessionState();
					}
				}
			} catch (error) {
				console.error("Failed to bootstrap Keycloak session", error);
				clearSessionState();
			}
		};

		void bootstrap();
	}, [
		clearSessionState,
		clearStoredTokens,
		refreshTokensRequest,
		syncSessionFromTokens,
	]);

	const exchangeCodeForTokens = useCallback(
		async (code: string, redirectUri: string) => {
			const verifier = await SecureStore.getItemAsync("umbral_code_verifier");
			if (!verifier) {
				throw new Error("Missing PKCE code verifier in secure storage");
			}

			const tokenUrl = `${env.EXPO_PUBLIC_KEYCLOAK_URL}/realms/${env.EXPO_PUBLIC_KEYCLOAK_REALM}/protocol/openid-connect/token`;
			const response = await fetch(tokenUrl, {
				method: "POST",
				headers: {
					"Content-Type": "application/x-www-form-urlencoded",
				},
				body: new URLSearchParams({
					grant_type: "authorization_code",
					client_id: env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID,
					code,
					redirect_uri: redirectUri,
					code_verifier: verifier,
				}).toString(),
			});

			if (!response.ok) {
				const errText = await response.text();
				throw new Error(`Failed to exchange authorization code: ${errText}`);
			}

			const tokenData = await response.json();
			await saveStoredTokens(tokenData);
			syncSessionFromTokens(
				tokenData.access_token,
				tokenData.id_token || tokenData.access_token,
			);
		},
		[saveStoredTokens, syncSessionFromTokens],
	);

	const beginLogin = useCallback(
		async (options?: { action?: "login" | "register" }) => {
			try {
				setStatus("loading");
				const verifier = generateCodeVerifier();
				await SecureStore.setItemAsync("umbral_code_verifier", verifier);
				const challenge = await generateCodeChallenge(verifier);

				const redirectUri =
					Platform.OS === "web"
						? Linking.createURL("redirect")
						: "umbral://redirect";
				const isRegister = options?.action === "register";
				const endpoint = isRegister ? "registrations" : "auth";
				const authUrl =
					`${env.EXPO_PUBLIC_KEYCLOAK_URL}/realms/${env.EXPO_PUBLIC_KEYCLOAK_REALM}/protocol/openid-connect/${endpoint}?` +
					"response_type=code" +
					`&client_id=${env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID}` +
					`&redirect_uri=${encodeURIComponent(redirectUri)}` +
					`&code_challenge=${challenge}` +
					"&code_challenge_method=S256" +
					"&scope=openid%20profile%20email";

				const result = await WebBrowser.openAuthSessionAsync(
					authUrl,
					redirectUri,
				);

				if (result.type === "success" && result.url) {
					const { queryParams } = Linking.parse(result.url);
					const code = queryParams?.code;

					if (typeof code === "string") {
						await exchangeCodeForTokens(code, redirectUri);
					} else {
						throw new Error("No authorization code returned in redirect URI");
					}
				} else {
					// User cancelled login or window closed
					const accToken = await SecureStore.getItemAsync(
						"umbral_access_token",
					);
					const idToken = await SecureStore.getItemAsync("umbral_id_token");
					if (accToken && idToken) {
						syncSessionFromTokens(accToken, idToken);
					} else {
						clearSessionState();
					}
				}
			} catch (error) {
				console.error("Failed to begin Keycloak login flow", error);
				clearSessionState();
				throw error;
			}
		},
		[exchangeCodeForTokens, syncSessionFromTokens, clearSessionState],
	);

	const signOut = useCallback(async () => {
		try {
			setStatus("loading");
			const idToken = await SecureStore.getItemAsync("umbral_id_token");
			const redirectUri =
				Platform.OS === "web"
					? Linking.createURL("redirect")
					: "umbral://redirect";

			let logoutUrl =
				`${env.EXPO_PUBLIC_KEYCLOAK_URL}/realms/${env.EXPO_PUBLIC_KEYCLOAK_REALM}/protocol/openid-connect/logout?` +
				`client_id=${env.EXPO_PUBLIC_KEYCLOAK_CLIENT_ID}` +
				`&post_logout_redirect_uri=${encodeURIComponent(redirectUri)}`;

			if (idToken) {
				logoutUrl += `&id_token_hint=${idToken}`;
			}

			await clearStoredTokens();
			clearSessionState();

			// Open browser session briefly to clear Keycloak cookies
			await WebBrowser.openAuthSessionAsync(logoutUrl, redirectUri);
		} catch (error) {
			console.error("Error during Keycloak sign out", error);
			await clearStoredTokens();
			clearSessionState();
		}
	}, [clearStoredTokens, clearSessionState]);

	const refreshSession = useCallback(async () => {
		const refToken = await SecureStore.getItemAsync("umbral_refresh_token");
		if (!refToken) {
			clearSessionState();
			throw new Error("No refresh token available");
		}

		try {
			const tokenData = await refreshTokensRequest(refToken);
			syncSessionFromTokens(
				tokenData.access_token,
				tokenData.id_token || tokenData.access_token,
			);
		} catch (error) {
			console.error("Failed to refresh session", error);
			await clearStoredTokens();
			clearSessionState();
			throw error;
		}
	}, [
		refreshTokensRequest,
		syncSessionFromTokens,
		clearStoredTokens,
		clearSessionState,
	]);

	const getAccessToken = useCallback(
		async (minValidity = 60): Promise<string | null> => {
			const accToken = await SecureStore.getItemAsync("umbral_access_token");
			const refToken = await SecureStore.getItemAsync("umbral_refresh_token");

			if (!accToken || !refToken) {
				return null;
			}

			const decoded = decodeJwt(accToken);
			if (!decoded) return null;

			// Check if token is valid for at least minValidity seconds
			const isExpired = decoded.exp * 1000 < Date.now() + minValidity * 1000;
			if (!isExpired) {
				return accToken;
			}

			// Try to refresh token
			try {
				const tokenData = await refreshTokensRequest(refToken);
				setAccessToken(tokenData.access_token);
				return tokenData.access_token;
			} catch (e) {
				console.warn("Failed to refresh access token, clearing session", e);
				await clearStoredTokens();
				clearSessionState();
				return null;
			}
		},
		[refreshTokensRequest, clearStoredTokens, clearSessionState],
	);

	const hasRealmRole = useCallback(
		(role: string): boolean => {
			if (!accessToken) return false;
			const decoded = decodeJwt(accessToken);
			const roles = decoded?.realm_access?.roles || [];
			return roles.includes(role);
		},
		[accessToken],
	);

	const getSubjectId = useCallback((): string | null => {
		if (!accessToken) return null;
		const decoded = decodeJwt(accessToken);
		return decoded?.sub || null;
	}, [accessToken]);

	const contextValue = useMemo(
		() => ({
			status,
			session,
			user: session?.user ?? null,
			beginLogin,
			signOut,
			refreshSession,
			getAccessToken,
			hasRealmRole,
			getSubjectId,
		}),
		[
			status,
			session,
			beginLogin,
			signOut,
			refreshSession,
			getAccessToken,
			hasRealmRole,
			getSubjectId,
		],
	);

	return (
		<AuthContext.Provider value={contextValue}>{children}</AuthContext.Provider>
	);
}

export function useAuth() {
	const context = useContext(AuthContext);
	if (!context) {
		throw new Error("useAuth must be used within an AuthProvider");
	}
	return context;
}
