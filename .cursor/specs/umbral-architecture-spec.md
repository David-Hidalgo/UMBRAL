# Umbral Architecture Specification (Especificación de Arquitectura)

## 1. Estilo Arquitectónico Global
UMBRAL se implementa mediante una **Arquitectura distribuida basada en Microservicios**, donde cada servicio sigue internamente los principios de la **Arquitectura Hexagonal (Puertos y Adaptadores)** y **Clean Architecture**. El objetivo es aislar completamente el núcleo de negocio (Dominio) de los frameworks, bases de datos y clientes externos.

## 2. Descomposición en Microservicios
Actualmente el sistema consta de **3 microservicios independientes** (con capacidad de expandirse):
1.  **Missions Service (Servicio de Misiones):** Maneja el Bounded Context de "Diseño de Misiones" (Mission, MissionNode).
2.  **Sessions Service (Servicio de Operación/Sesiones):** Maneja la ejecución en vivo, transiciones de estado, equipos y pistas (LiveSession, Team).
3.  **Scoring & Monitoring Service (Servicio de Puntuación y Monitoreo):** Centraliza el cálculo de puntajes, penalizaciones, ranking e historial de auditoría (EvidenceSubmission, ScoreEntry, SessionEvent).

## 3. Patrones Tácticos de Domain-Driven Design (DDD)
* **Capa de Dominio:** Contiene entidades de comportamiento rico, Value Objects (inmutables, ej. `TeamCode`, `ScoreValue`, `PenaltyReason`) y raíces de agregado (`Mission`, `LiveSession`). Los setters públicos están estrictamente prohibidos; toda mutación se realiza mediante métodos semánticos.
* **Puertos y Adaptadores:** El Core se comunica con el exterior mediante interfaces (Puertos). Los adaptadores primarios son controladores API HTTP y Hubs de SignalR. Los adaptadores secundarios son repositorios EF Core y clientes de mensajería RabbitMQ.

## 4. Segregación de Responsabilidades (CQRS)
Mediante la librería `MediatR` en .NET Core, se dividen los flujos:
* **Commands (Escritura):** Modifican el estado del dominio. Pasan por una cadena de validación (`FluentValidation` pipeline behavior). No retornan datos complejos, solo confirmaciones o IDs.
* **Queries (Lectura):** Optimizadas para pantallas específicas. No cargan el modelo de dominio; realizan proyecciones directas (`AsNoTracking`) hacia DTOs planos para consumo directo de React (Web) y React Native (Mobile).

## 5. Integración y Tiempo Real
* **Comunicación Asíncrona (RabbitMQ):** Desacopla subprocesos secundarios como auditoría, recálculo de puntos ante penalizaciones y almacenamiento de historial mediante Integration Events.
* **Comunicación en Tiempo Real (SignalR WebSockets):** Sincroniza de inmediato el ranking, la liberación de pistas y los temporizadores con los clientes conectados sin saturar con encuestas periódicas (polling).
