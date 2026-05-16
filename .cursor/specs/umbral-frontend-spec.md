# Umbral Frontend Specification (Especificación Frontend)

## 1. Enfoque de Clientes Duales
El ecosistema frontend de UMBRAL se divide en dos aplicaciones totalmente independientes para responder a las necesidades específicas de los actores de negocio:
1.  **Panel de Administración y Operación (Web):** Desarrollado en React.
2.  **Aplicación de Equipos Participantes (Móvil):** Desarrollada en React Native.

## 2. Aplicación Web - React (Administradores y Operadores)
* **Stack:** React (TypeScript), empaquetado con Vite.
* **Propósito:** Proporcionar una consola densa en datos, orientada al monitoreo intensivo, visualización de tablas de ranking, logs de auditoría en vivo, interfaces para dar de alta misiones y herramientas de control manual para el operador (activar, pausar, penalizar).
* **Consumo en Tiempo Real:** Se suscribe al grupo global `"Admins"` en SignalR para recibir flujos constantes de notificaciones sobre envíos de evidencias y cambios de temporizadores globales.

## 3. Aplicación Móvil - React Native (Participantes)
* **Stack:** React Native (TypeScript), gestionado de forma ordenada y separada.
* **Propósito:** Interfaz inmersiva, de alto contraste y liviana optimizada para el uso en smartphones. Debe mostrar el temporizador en cuenta regresiva local (sincronizado), la pista actual habilitada y un módulo de envío rápido de respuestas o evidencias (texto/archivos).
* **Optimización de Red:** Los datos transferidos deben ser mínimos. En lugar de descargar todo el estado del juego constantemente, la app móvil reacciona a eventos de SignalR (ej. `"PistaLiberada"`) que gatillan peticiones HTTP de lectura (`Queries`) altamente específicas y ligeras.

## 4. Gestión de Estado y Conectividad Común
* **Manejo de Estado Local:** Uso de librerías ligeras como `Zustand` o la Context API para gestionar de manera óptima el estado sin penalizar el rendimiento en hilos móviles.
* **Estrategia de Resiliencia en WebSockets:** Implementación de políticas de reconexión automática en el cliente de SignalR (`withAutomaticReconnect()`). En caso de desconexión (especialmente crítico en el móvil por pérdida de señal), la app debe mostrar un indicador visual discreto e invocar un endpoint de sincronización HTTP para recuperar el estado perdido inmediatamente al reconectarse.
