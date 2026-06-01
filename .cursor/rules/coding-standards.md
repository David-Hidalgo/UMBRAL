# Coding Standards

## Convenciones de Nomenclatura (C# y .NET Core)
* Utilizar `PascalCase` para clases, interfaces, registros (records), métodos y propiedades.
* Utilizar `camelCase` para variables locales y parámetros de métodos.
* Prefijar las interfaces siempre con una `I` (ej. `IMissionRepository`).
* Utilizar un guion bajo `_` seguido de `camelCase` para los campos privados y de solo lectura (`_sessionContext`).
* Nombrar los Handlers indicando su acción explícita (ej. `SubmitEvidenceCommandHandler`).

## Principios de Diseño y Limpieza
* Aplicar rigurosamente los principios SOLID en la creación de nuevas clases y servicios. 
* Cada clase debe tener una única responsabilidad (SRP).
* Evitar el uso de "Magic Strings" o "Magic Numbers"; agruparlos en constantes o enumeraciones.
* Mantener los métodos cortos y declarativos. Si un método tiene más de 20 líneas, evaluar su extracción a métodos privados auxiliares.

## Control de Versiones y Trabajo en Equipo
* Seguir la estrategia de ramificación **Gitflow** para gestionar el ciclo de vida del desarrollo.
* Nombrar las ramas de forma descriptiva: `feature/nombre-de-funcionalidad`, `bugfix/descripcion-error`, `release/vX.X.X`.
* Todos los Pull Requests deben ser revisados y aprobados por al menos un compañero de equipo, como Andrés, antes de ser fusionados a las ramas principales.
* Asegurar que el repositorio mantenga un historial limpio, utilizando nombres de commits consistentes e imperativos.
* Mantener la regla de 'Un solo commit lógico por rama de feature' utilizando git commit --amend para evitar ensuciar el historial

## Manejo de Errores y Validaciones
* Utilizar un Middleware de manejo global de excepciones para atrapar errores no controlados y formatear respuestas HTTP estandarizadas.
* Las validaciones de entrada deben ocurrir antes de ejecutar la lógica de negocio, centralizadas mediante herramientas como FluentValidation.
* El dominio debe lanzar excepciones específicas de negocio (ej. `InvalidSessionStateException`) en lugar de excepciones genéricas.
