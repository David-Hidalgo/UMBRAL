---
name: cqrs-mediatr
description: El sistema UMBRAL utiliza una arquitectura basada en microservicios, aplicando el patrón CQRS mediado por MediatR en el ecosistema .NET Core.
---

# Habilidad: CQRS con MediatR

Cuando trabajes en la capa de Aplicación (Application Layer), aplicarás el patrón CQRS de forma rigurosa[cite: 1, 2, 3]:

## 1. Separación Command/Query
- **Commands:** Operaciones que cambian el estado del sistema (ej. `SubmitEvidenceCommand`). Deben encapsular sus datos. Nunca devuelven entidades completas, solo IDs o `Unit`.
- **Queries:** Operaciones de solo lectura (ej. `GetTeamRankingQuery`). Deben devolver DTOs (Data Transfer Objects) o ViewModels optimizados para la UI, NUNCA entidades del dominio.

## 2. Responsabilidad de los Handlers
- Los `IRequestHandler` son orquestadores. Su única función es:
  1. Obtener la entidad de un repositorio.
  2. Ejecutar un método de dominio en la entidad (o un servicio de dominio).
  3. Guardar los cambios en el repositorio.
- PROHIBIDO meter reglas de negocio complejas (if/else anidados sobre estados) directamente en el Handler; eso pertenece a la Entidad.

## 3. Validaciones Transversales
- Todo Command debe tener su clase validadora correspondiente usando `FluentValidation` para asegurar la robustez antes de que el Handler se ejecute (vía MediatR Pipeline Behaviors).