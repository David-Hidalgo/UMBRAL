# Umbral Product Specification (Especificación de Producto)

## 1. Descripción General del Proyecto
**UMBRAL** es una plataforma web y móvil diseñada para la operación en tiempo real de misiones de investigación inmersiva (misterio y resolución de casos). Reemplaza sistemas manuales (hojas de cálculo, grupos de mensajería) con una solución centralizada que permite modelar misiones, controlar sesiones en vivo, coordinar equipos, validar evidencias, penalizar y visualizar el avance global en un tablero interactivo.

## 2. Actores y Plataformas
* **Administrador (Plataforma Web - React):** Configura misiones, gestiona usuarios y catálogos base.
* **Operador (Plataforma Web - React):** Inicia sesiones, controla estados, libera pistas, aplica penalizaciones y supervisa el ranking y el historial.
* **Equipo Participante (Plataforma Móvil - React Native):** Accede a su sesión, visualiza pistas, temporizador, puntaje y registra evidencias o respuestas desde el campo de juego.

## 3. Requerimientos Funcionales (RF)
* **RF-01:** Crear, editar, consultar y desactivar misiones.
* **RF-02:** Registrar en cada misión etapas/nodos, pistas y tiempo máximo de ejecución.
* **RF-03:** Crear sesiones en vivo a partir de misiones activas.
* **RF-04:** Manejar estados de sesión: Programada, En Preparación, Activa, Pausada, Finalizada y Cancelada.
* **RF-05:** Registrar equipos participantes y asociarlos a una sesión.
* **RF-06:** Visualizar en la app móvil del equipo el temporizador, puntaje y pistas habilitadas.
* **RF-07:** Permitir al operador liberar pistas manual o condicionalmente.
* **RF-08:** Enviar respuestas o evidencias asociadas a una etapa de la misión (desde la app móvil).
* **RF-09:** Registrar cada envío con fecha, equipo, sesión y estado de validación.
* **RF-10:** Recalcular automáticamente el puntaje del equipo al validar o penalizar una evidencia.
* **RF-11:** Permitir al operador aplicar penalizaciones justificadas a un equipo.
* **RF-12:** Actualizar el ranking de la sesión en tiempo real.
* **RF-13:** Reflejar eventos relevantes y cambios de estado en el panel del operador en tiempo real.
* **RF-14:** Publicar eventos de dominio en el bus de mensajes al registrar evidencias o cambios significativos.
* **RF-15:** Mantener un historial de eventos con trazabilidad mínima para auditoría.
* **RF-16:** Diferenciar funcionalidades e interfaces estrictamente por rol.
* **RF-17:** Separar consultas de lecturas de los comandos de escritura (CQRS).
* **RF-18:** Disponer de validaciones de negocio antes de transiciones o envíos de evidencias.

## 4. Reglas de Negocio (RB)
* **RB-01:** Una misión solo puede usarse para crear sesiones si está activa.
* **RB-02:** Una sesión no puede iniciar sin al menos un equipo registrado.
* **RB-03:** No se aceptan evidencias si la sesión está pausada, finalizada o cancelada.
* **RB-04:** Una pista no puede liberarse dos veces al mismo equipo para la misma etapa.
* **RB-05:** Cada evidencia se asocia exactamente a un equipo, una sesión y una etapa.
* **RB-06:** Toda penalización debe registrar de forma obligatoria el motivo y momento exacto de aplicación.
* **RB-07:** El puntaje acumulado no puede quedar sin trazabilidad de origen.
* **RB-08:** El ranking se ordena de mayor a menor puntaje; el tiempo de resolución se usa como criterio de desempate.
* **RB-09:** Los cambios de estado de la sesión deben respetar una máquina de estados válida.
* **RB-10:** El operador solo administra las sesiones asignadas o visibles según las políticas del equipo.
