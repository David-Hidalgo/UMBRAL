import { createEnv } from "@t3-oss/env-core";
import { Platform } from "react-native";
import { z } from "zod";

const _env = createEnv({
	clientPrefix: "EXPO_PUBLIC_",
	client: {
		EXPO_PUBLIC_SERVER_URL: z.url(),
		// URL usada cuando se ejecuta en Expo Web (browser).
		// Si no está definida, se usa EXPO_PUBLIC_SERVER_URL como fallback.
		EXPO_PUBLIC_SERVER_URL_WEB: z.url().optional(),
		EXPO_PUBLIC_KEYCLOAK_URL: z.url(),
		EXPO_PUBLIC_KEYCLOAK_REALM: z.string().min(1),
		EXPO_PUBLIC_KEYCLOAK_CLIENT_ID: z.string().min(1),
	},
	runtimeEnv: process.env,
	emptyStringAsUndefined: true,
});

export const env = {
	..._env,
	// En web (browser), priorizar EXPO_PUBLIC_SERVER_URL_WEB para evitar
	// problemas de CORS/conectividad con la IP LAN de dispositivos físicos.
	EXPO_PUBLIC_SERVER_URL:
		Platform.OS === "web"
			? (_env.EXPO_PUBLIC_SERVER_URL_WEB ?? _env.EXPO_PUBLIC_SERVER_URL)
			: _env.EXPO_PUBLIC_SERVER_URL,
};
