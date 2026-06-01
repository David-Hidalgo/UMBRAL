---
name: rabbitmq-events
description: UMBRAL requiere comunicación asíncrona entre sus microservicios para procesos que no deben bloquear la experiencia en tiempo real, operando mediante RabbitMQ.
---

# Habilidad: Eventos con RabbitMQ

Cuando implementes RabbitMQ en UMBRAL (Regla RNF-05, RF-14)[cite: 1]:

## 1. Publicación Simple
- Usa un servicio publicador básico para enviar mensajes a una cola cuando ocurran eventos importantes en el dominio (ej. `EvidenciaValidada`, `PistaLiberada`).

## 2. Consumo Desacoplado
- Los procesos secundarios (como recalcular el puntaje de un equipo o guardar en el historial) deben escuchar la cola y procesar el mensaje de fondo, sin bloquear la respuesta HTTP (experiencia del usuario principal).

## 3. Robustez Básica
- Serializa los datos del evento en formato JSON antes de enviarlos a la cola.
- En el consumidor, usa siempre un bloque `try/catch` general al recibir el mensaje. Si ocurre un error (ej. el JSON viene corrupto), captúralo en los logs para que la aplicación no se caiga repentinamente.