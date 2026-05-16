# Umbral Quality & DevOps Specification (Especificación de Calidad y DevOps)

## 1. Estándar de Pruebas Automatizadas
Para cumplir con los exigentes objetivos académicos del curso en la UCAB, el backend del proyecto debe asegurar un blindaje técnico robusto:
* **Meta de Cobertura:** Mínimo **90% de cobertura de código** en la lógica del backend (Domain, Application Handlers).
* **Pruebas Unitarias:** Enfocadas en probar las reglas de negocio de los agregados y el comportamiento de los comandos de MediatR. Uso estricto del patrón **Arrange-Act-Assert (AAA)** y aislamiento mediante dobles de prueba (Mocks con `Moq` o `NSubstitute`).
* **Pruebas de Integración:** Validación obligatoria de los adaptadores secundarios. Se deben utilizar **Testcontainers** para levantar instancias efímeras de PostgreSQL y RabbitMQ en memoria Docker durante la ejecución de la suite de pruebas.

## 2. Entorno de Desarrollo Local (Docker Compose)
Toda la solución debe poder ejecutarse localmente sin configuraciones manuales complejas. El archivo `docker-compose.yml` debe orquestar:
* Contenedor de base de datos PostgreSQL con volumen persistente para desarrollo.
* Contenedor de RabbitMQ con la interfaz de administración (Management Plugin) expuesta.
* Contenedores independientes para cada uno de los 3 microservicios .NET Core (utilizando Docker Multi-stage Builds para optimizar producción).
* Contenedor para la aplicación Web React.
* Variables de entorno administradas centralmente en un archivo `.env` local (no versionado).

## 3. Integración Continua (CI Pipeline)
Cada Pull Request dirigido a las ramas principales (`main`, `develop`) debe disparar de forma obligatoria un pipeline automatizado (ej. GitHub Actions) que ejecute las siguientes etapas de control:
1.  **Restore & Build:** Verificación de compilación limpia de la solución completa en .NET y TypeScript.
2.  **Test Execution:** Ejecución completa de la suite de pruebas unitarias y de integración.
3.  **Coverage Report:** Cálculo del porcentaje de cobertura. El pipeline fallará automáticamente si se rompe la compilación o si la cobertura final del backend es inferior al 90%.

## 4. Flujo de Trabajo en Equipo (Gitflow)
* **Estrategia:** Uso de Gitflow estricto. Ramas de desarrollo base en `develop`, características en `feature/nombre`, correcciones en `bugfix/nombre`.
* **Gobernanza:** Ningún cambio se integra directamente a `develop` sin un Pull Request previo. Los PRs requieren la validación automatizada del pipeline de CI y la revisión/aprobación manual cruzada entre los compañeros de equipo (Emmanuel y Andrés).
