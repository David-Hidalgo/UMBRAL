# Umbral Backend Technical Specification (Especificación Técnica Backend)

## 1. Stack Tecnológico Principal
* **Framework:** .NET Core 8.0/9.0 utilizando C#.
* **Persistencia:** PostgreSQL.
* **ORM:** Entity Framework Core (Enfoque Code-First).
* **Mensajería:** RabbitMQ como Message Broker.
* **Tiempo Real:** ASP.NET Core SignalR.

## 2. Configuración de Persistencia (EF Core)
* **Mapeo de Entidades:** Implementación estricta de `IEntityTypeConfiguration<T>` aislada en la capa de infraestructura. Uso explícito de la Fluent API.
* **Mapeo de Value Objects:** Configuración mediante `OwnsOne` o el nuevo `ComplexProperty` de EF Core para asegurar que los objetos de valor se aplanen en las columnas correspondientes de la tabla en PostgreSQL.
* **Estrategia de Consultas:** Uso mandatorio de `.AsNoTracking()` en todos los query handlers de MediatR para optimizar el rendimiento y evitar que el DbContext rastree cambios innecesarios en lecturas.

## 3. Canalización de Casos de Uso (MediatR)
* **Pipeline Behaviors:** Centralización de aspectos transversales (Cross-Cutting Concerns). Se debe incluir obligatoriamente un `ValidationBehavior` que evalúe las reglas de `FluentValidation` antes de llegar al Handler.
* **Manejo Global de Excepciones:** Un middleware personalizado atrapa excepciones de dominio (ej. `InvalidSessionStateException`) y mapea códigos de estado HTTP semánticos (400 Bad Request, 404 Not Found) devolviendo un formato estandarizado (RFC 7807 Problem Details).

## 4. Arquitectura de Mensajería con RabbitMQ
* **Eventos de Integración:** Publicación de payloads ligeros que contienen identificadores clave (ej. `EvidenceSubmittedIntegrationEvent`, `SessionStateChangedIntegrationEvent`).
* **Consumidores Idempotentes:** Cada consumidor debe verificar si el identificador único del evento ya ha sido procesado mediante una tabla de control de deduplicación (patrón Outbox/Inbox) para prevenir inconsistencias en el recálculo de puntajes.

## 5. Implementación de SignalR Hubs
* **Segmentación por Grupos:** Las conexiones se organizan dinámicamente:
    * `Groups.AddToGroupAsync(Context.ConnectionId, "Admins")` para operadores y administradores web.
    * `Groups.AddToGroupAsync(Context.ConnectionId, $"Session-{sessionId}-Team-{teamId}")` para aislar los flujos que pertenecen exclusivamente a un equipo móvil.
* **Seguridad:** Hubs decorados con `[Authorize]` exigiendo claims de rol específicos para restringir la invocación de métodos de transmisión.
