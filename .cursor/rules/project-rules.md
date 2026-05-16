# Project Rules

## Arquitectura y Límites
* El sistema se rige bajo Arquitectura Hexagonal y Limpia, dividida en microservicios independientes (aunque basta con que se evidencia la separacion conceptual) .
* La capa de Dominio es el núcleo absoluto: **no debe tener ninguna dependencia** hacia Entity Framework, RabbitMQ, o frameworks web.
* La comunicación entre el backend y la aplicación móvil en React Native debe realizarse exclusivamente a través de la API HTTP (adaptadores primarios) y WebSockets.
* Implementar mecanismos de logging estructurado y seguridad básica por roles (Administrador, Operador, Equipo Participante) en todos los flujos de entrada.

## Infraestructura y Contenedores
* Todo el entorno de desarrollo local (API, PostgreSQL, RabbitMQ) debe levantarse y ejecutarse mediante contenedores configurados en `docker-compose.yml`.
* Los archivos de configuración del entorno (como `.env`) no deben ser versionados; proporcionar un `.env.example` en el repositorio.
* La persistencia base debe resolverse utilizando Entity Framework Core.

## Integración Continua y Calidad (CI/CD)
* El proyecto debe incluir un pipeline de integración continua que ejecute automáticamente tareas de restauración, compilación y pruebas en cada PR.
* Como meta académica estricta, el backend debe alcanzar y mantener un mínimo de **90% de cobertura de pruebas**.
* El pipeline debe fallar si la compilación se rompe o si la cobertura cae por debajo del límite establecido.

## Flujo Operativo y de Dominio
* Respetar las reglas de negocio del dominio en todo momento, por ejemplo: una sesión no puede iniciar sin equipos, y el ranking debe actualizarse en tiempo real resolviendo empates por tiempo de resolución.
* Desacoplar las acciones de auditoría, recálculo de puntos y notificaciones mediante mensajería en RabbitMQ.
