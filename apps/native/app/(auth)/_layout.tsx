import { useAuth } from "@/contexts/auth-context";
import { Redirect, Stack } from "expo-router";

export default function AuthLayout() {
	const { isAuthenticated, isLoading } = useAuth();

	if (!isLoading && isAuthenticated) {
		return <Redirect href="/(drawer)" />;
	}

	return <Stack screenOptions={{ headerShown: false }} />;
}
