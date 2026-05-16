---
name: websocket-signalr
description: La operación en vivo de UMBRAL exige supervisión y actualizaciones en tiempo real utilizando SignalR (WebSockets) entre el backend .NET y los clientes móviles en React Native.
---

# Habilidad: WebSockets con SignalR

Cuando implementes la comunicación en tiempo real para cumplir con los requisitos operativos (RF-12, RF-13):

## 1. Casos de Uso Restringidos
- Solo usa SignalR para notificar a los clientes sobre cambios de estado instantáneos (ej. actualización del ranking, liberación manual de una pista por el operador, o cambio de estado de la sesión).

## 2. Seguridad en la Conexión
- Los hubs de SignalR deben estar protegidos con el atributo `[Authorize]`. El cliente (React/React Native) debe enviar su JWT en la conexión inicial.
- Agrupa a los usuarios por "Grupos" en SignalR (ej. Grupo "Sesion-123", Grupo "Operadores") para no enviar notificaciones a jugadores de otras misiones.