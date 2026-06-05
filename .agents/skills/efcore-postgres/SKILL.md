---
name: efcore-postgres
description: La persistencia de datos en los microservicios de UMBRAL se gestiona con PostgreSQL utilizando Entity Framework Core.
---

# Habilidad: Persistencia con EF Core y PostgreSQL

Cuando trabajes en la capa de Infraestructura relacionada con la base de datos para UMBRAL:

## 1. Mapeo mediante Fluent API
- PROHIBIDO usar Data Annotations en las entidades. 
- Toda configuración de base de datos (claves primarias, relaciones, índices, restricciones de longitud) debe hacerse usando `IEntityTypeConfiguration<T>` en clases separadas dentro de la capa de Infraestructura.

## 2. Implementación de Repositorios
- Los Repositorios deben implementar estrictamente las interfaces definidas en la capa de Dominio.
- Los Repositorios NO deben contener reglas de negocio; su única responsabilidad es orquestar las consultas y comandos hacia `DbContext`.
- Usa operaciones asíncronas (`ToListAsync`, `FirstOrDefaultAsync`) de EF Core por defecto.

## 3. Manejo de Transacciones
- La llamada a `SaveChanges()` o `SaveChangesAsync()` NO debe hacerse en el Repositorio, sino en el Handler de la capa de Aplicación (o mediante un patrón Unit of Work) para garantizar la atomicidad de las operaciones CQRS.
