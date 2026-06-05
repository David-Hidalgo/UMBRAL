export interface KeycloakConfig {
	baseUrl: string;
	realm: string;
	clientId: string;
}

export type AppRole = "admin" | "operator" | "player";
