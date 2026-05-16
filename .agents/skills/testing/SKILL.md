---
name: testing
description: El proyecto exige un alto estándar de calidad con una meta académica de al menos 90% de cobertura en el backend.
---

## Reglas de Pruebas
* Escribir pruebas unitarias enfocadas en los Handlers de MediatR, Servicios de Dominio y comportamiento de Entidades.
* Utilizar Mocks (ej. Moq o NSubstitute) exclusivamente para simular dependencias externas y repositorios.
* Estructurar las pruebas bajo el patrón AAA (Arrange, Act, Assert).
* Implementar pruebas de integración para verificar la correcta comunicación con PostgreSQL y RabbitMQ utilizando Testcontainers.
* Nombrar los métodos de prueba de forma descriptiva indicando el escenario y el resultado esperado.
* Asegurar que la lógica de cálculo de puntajes y validación de reglas de negocio esté cubierta exhaustivamente.