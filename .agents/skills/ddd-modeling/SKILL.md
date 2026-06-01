---
name: ddd-modeling
description: UMBRAL sigue los principios de Domain-Driven Design (DDD) y Arquitectura Hexagonal distribuidos en múltiples microservicios.
---

# Habilidad: Domain-Driven Design (DDD)

-Cuando se te pida crear o modificar elementos en la capa de Dominio de UMBRAL, debes aplicar ESTRICTAMENTE estas reglas:

## 1. Protección de Invariantes (Robustez)
- Las Entidades deben tener constructores privados o protegidos. La creación debe hacerse mediante "Factory Methods" (métodos estáticos como `Create(...)`) que validen las reglas de negocio antes de instanciar el objeto.
- Todas las propiedades deben tener `private set`. El estado solo se modifica a través de métodos de comportamiento (ej. `public void AplicarPenalizacion(...)`).

## 2. Value Objects
- Usa objetos de valor (inmutables, con `record` en C# 10+) para conceptos que miden o describen (ej. `TeamCode`, `ScoreValue`, `Difficulty`).

## 3. Aislamiento Total (Regla RNF-07)
- PROHIBIDO usar anotaciones de Entity Framework (Data Annotations como `[Key]`, `[Table]`, `[Required]`) en el Dominio.
- PROHIBIDO inyectar repositorios o servicios de infraestructura directamente en las entidades.

## 4. Eventos de Dominio
- Cuando una entidad cambie de estado de manera significativa (ej. `EvidenciaValidada`), debe registrar un `DomainEvent` en una lista interna para que sea publicado posteriormente.