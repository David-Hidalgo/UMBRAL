import { Ionicons } from "@expo/vector-icons";
import { CameraView, useCameraPermissions } from "expo-camera";
import { Spinner } from "heroui-native";
import { useEffect, useState } from "react";
import { Modal, Pressable, Text, View } from "react-native";

// Límite del backend (SubmitEvidenceCommandValidator: máximo 500 caracteres).
const MAX_EVIDENCE_LENGTH = 500;

interface QrEvidenceScannerProps {
	visible: boolean;
	/** true mientras la evidencia se está enviando al servidor. */
	submitting: boolean;
	onClose: () => void;
	/** Recibe el texto del QR ya normalizado (trim). El componente bloquea nuevas
	 * lecturas mientras la promesa se resuelve; si rechaza, se puede reescanear. */
	onRead: (text: string) => Promise<void> | void;
}

export function QrEvidenceScanner({
	visible,
	submitting,
	onClose,
	onRead,
}: QrEvidenceScannerProps) {
	const [permission, requestPermission] = useCameraPermissions();
	// Bloquea lecturas adicionales tras el primer QR (evita solicitudes duplicadas).
	const [scanLocked, setScanLocked] = useState(false);
	const [scanError, setScanError] = useState<string | null>(null);

	// Reinicia el estado de lectura cada vez que se vuelve a abrir el modal.
	useEffect(() => {
		if (visible) {
			setScanLocked(false);
			setScanError(null);
		}
	}, [visible]);

	const handleBarcodeScanned = ({ data }: { data: string }) => {
		if (scanLocked || submitting) {
			return;
		}
		setScanLocked(true);
		setScanError(null);

		// El contenido del QR es entrada no confiable: se trata solo como texto plano.
		const text = data.trim();
		if (text.length === 0) {
			setScanError("El código QR está vacío.");
			setScanLocked(false);
			return;
		}
		if (text.length > MAX_EVIDENCE_LENGTH) {
			setScanError(
				`La evidencia no debe exceder los ${MAX_EVIDENCE_LENGTH} caracteres.`,
			);
			setScanLocked(false);
			return;
		}

		void (async () => {
			try {
				await onRead(text);
			} finally {
				// Si el envío falló, el modal sigue abierto y se permite reescanear.
				setScanLocked(false);
			}
		})();
	};

	const renderBody = () => {
		if (!permission) {
			return (
				<View className="flex-1 items-center justify-center gap-3">
					<Spinner />
					<Text className="font-mono text-muted text-xs uppercase">
						Verificando permiso de cámara...
					</Text>
				</View>
			);
		}

		if (!permission.granted) {
			return (
				<View className="flex-1 items-center justify-center gap-4 px-8">
					<Ionicons color="#71717a" name="camera-outline" size={40} />
					<Text className="text-center font-mono text-foreground text-sm">
						{permission.canAskAgain
							? "UMBRAL necesita acceso a la cámara para escanear evidencias QR."
							: "El permiso de cámara fue denegado. Actívalo desde los ajustes del dispositivo para escanear evidencias QR."}
					</Text>
					{permission.canAskAgain && (
						<Pressable
							accessibilityHint="Solicita el permiso de cámara del sistema"
							accessibilityLabel="Permitir cámara"
							accessibilityRole="button"
							className="items-center rounded-lg border border-accent/40 bg-accent/15 px-6 py-3"
							onPress={() => void requestPermission()}
						>
							<Text className="font-bold font-mono text-accent text-xs uppercase tracking-widest">
								Permitir cámara
							</Text>
						</Pressable>
					)}
				</View>
			);
		}

		return (
			<View className="flex-1">
				<CameraView
					barcodeScannerSettings={{ barcodeTypes: ["qr"] }}
					facing="back"
					onBarcodeScanned={
						scanLocked || submitting ? undefined : handleBarcodeScanned
					}
					style={{ flex: 1 }}
				/>
				<View className="absolute inset-x-0 bottom-0 items-center gap-2 bg-black/60 px-4 py-5">
					{submitting ? (
						<View className="flex-row items-center gap-2">
							<Spinner />
							<Text className="font-mono text-white text-xs uppercase tracking-widest">
								Enviando evidencia...
							</Text>
						</View>
					) : (
						<Text className="text-center font-mono text-white text-xs uppercase tracking-widest">
							Apunta la cámara al código QR
						</Text>
					)}
					{scanError && (
						<Text className="text-center font-mono text-danger text-xs">
							{scanError}
						</Text>
					)}
				</View>
			</View>
		);
	};

	return (
		<Modal
			animationType="slide"
			onRequestClose={onClose}
			transparent={false}
			visible={visible}
		>
			<View className="flex-1 bg-background">
				<View className="flex-row items-center justify-between border-border/15 border-b px-4 py-3">
					<Text className="font-bold font-mono text-foreground text-xs uppercase tracking-widest">
						Escanear evidencia QR
					</Text>
					<Pressable
						accessibilityHint="Cierra el escáner sin enviar evidencia"
						accessibilityLabel="Cerrar escáner"
						accessibilityRole="button"
						disabled={submitting}
						onPress={onClose}
					>
						<Ionicons color="#71717a" name="close" size={22} />
					</Pressable>
				</View>
				{renderBody()}
			</View>
		</Modal>
	);
}
