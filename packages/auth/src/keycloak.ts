import Keycloak, {
	type KeycloakInitOptions,
	type KeycloakLoginOptions,
	type KeycloakLogoutOptions,
} from "keycloak-js";

import type { KeycloakConfig } from "./types";

type KeycloakClient = InstanceType<typeof Keycloak>;

const keycloakClientCache = new Map<string, KeycloakClient>();

function toKeycloakCacheKey(config: KeycloakConfig) {
	return `${normalizeKeycloakBaseUrl(config.baseUrl)}::${config.realm}::${config.clientId}`;
}

function normalizeKeycloakBaseUrl(baseUrl: string) {
	return baseUrl.replace(/\/+$/, "");
}

export function createKeycloakClient(config: KeycloakConfig) {
	const cacheKey = toKeycloakCacheKey(config);
	const cachedClient = keycloakClientCache.get(cacheKey);

	if (cachedClient) {
		return cachedClient;
	}

	const client = new Keycloak({
		url: normalizeKeycloakBaseUrl(config.baseUrl),
		realm: config.realm,
		clientId: config.clientId,
	});

	keycloakClientCache.set(cacheKey, client);

	return client;
}

export async function initializeKeycloak(
	client: KeycloakClient,
	options: KeycloakInitOptions = {},
) {
	return client.init({
		...options,
		pkceMethod: options.pkceMethod ?? "S256",
	});
}

export async function loginWithKeycloak(
	client: KeycloakClient,
	options: KeycloakLoginOptions = {},
) {
	await client.login({ ...options });
}

export async function logoutFromKeycloak(
	client: KeycloakClient,
	options: KeycloakLogoutOptions = {},
) {
	await client.logout({ ...options });
}
