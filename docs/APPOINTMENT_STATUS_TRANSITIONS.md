# Documentación Técnica — Transiciones de Estado de Turnos (`ATTENDED` y `NO_SHOW`)

## 1. Contexto y Objetivo

En el modelo de dominio actual (`Dsw2026Tpi.Domain`), la entidad `Date` (tabla `Appointments`) y el enum `DateState` ya contemplan cuatro estados:

| Valor numérico | Estado (`DateState`) | Etiqueta en Frontend (`appointmentStatus.js`) | Descripción |
| :---: | :--- | :--- | :--- |
| `0` | `BOOKED` | Confirmado | Turno reservado y pendiente de atención. |
| `1` | `CANCELLED` | Cancelado | Turno cancelado; el bloque horario (`Turn`) vuelve a `AVAILABLE`. |
| `2` | `ATTENDED` | Completado | El paciente asistió y fue atendido. |
| `3` | `NO_SHOW` | Ausente | El paciente no se presentó al turno reservado. |

Aunque el dominio ya definía los métodos `Date.Complete()` y `Date.NoShow()` y el frontend ya posee las etiquetas visuales para `ATTENDED` (*Completado*) y `NO_SHOW` (*Ausente*), el backend no exponía ninguna operación para realizar dichas transiciones.

Este cambio incorpora en la API la capacidad de actualizar el estado de una cita confirmada a `ATTENDED` o `NO_SHOW` por parte de un **Administrador**, además de habilitar el filtrado opcional por estado en la búsqueda avanzada de citas.

---

## 2. Máquina de Estados y Reglas de Negocio

```text
                    ┌──────────────────┐
         POST       │                  │   DELETE /api/appointments/{id}
  /api/appointments │      BOOKED      ├───────────────────────────────► CANCELLED
 ──────────────────►│  (Confirmado)    │   (libera Turn → AVAILABLE)     (Final)
                    └──┬────────────┬──┘
                       │            │
      PATCH .../status │            │ PATCH .../status
      {status:ATTENDED}│            │ {status:NO_SHOW}
                       ▼            ▼
                  ATTENDED       NO_SHOW
                (Completado)    (Ausente)
                   (Final)       (Final)
```

### Reglas aplicadas

1. **Completado automático al vencer el horario de finalización:**
   - Cualquier cita en estado `BOOKED` cuya fecha y hora de fin (`Turn.ScheduledDate.Date + Turn.EndTime`) sea menor o igual a `DateTime.Now` pasa automáticamente a **`ATTENDED` (Completado)** mediante `IAppointmentService.CompleteExpiredAppointments()`.
   - Se ejecuta tanto **bajo demanda** antes de `Search`, `GetByPatient`, `Cancel` y `DashboardService.GetStats()` (garantizando consistencia inmediata al consultar) como **en segundo plano** al iniciar la API y cada 1 minuto mediante `AppointmentCompletionHostedService`.
2. **Control de acceso por rol (RN05):** Solo usuarios autenticados con rol **`ADMINISTRADOR`** (`Policies.AdminPolicy`) pueden marcar manualmente un turno como `ATTENDED` o `NO_SHOW` mediante `PATCH /api/appointments/{id}/status`.
3. **Estado origen obligatorio:** Únicamente una cita en estado **`BOOKED`** puede cambiar a `ATTENDED` o `NO_SHOW`. Si la cita ya se encuentra en `CANCELLED`, `ATTENDED` o `NO_SHOW`, se rechaza con HTTP `409 Conflict` (`APPOINTMENT_STATUS_CHANGE_NOT_ALLOWED`).
4. **Estados destino permitidos:** El endpoint `PATCH /api/appointments/{id}/status` acepta exclusivamente `ATTENDED` o `NO_SHOW`. Intentar enviar `BOOKED` o `CANCELLED` se rechaza con HTTP `400 Bad Request` (`APPOINTMENT_INVALID_STATUS_TRANSITION`), ya que la cancelación posee su propio endpoint (`DELETE /api/appointments/{id}`) que además libera el bloque horario.
5. **Conservación del bloque horario (`Turn`):** Al pasar a `ATTENDED` o `NO_SHOW`, la relación `Date.Turn` y el estado del slot (`TurnState.BOOKED`) **no se liberan**, porque el horario fue consumido (atendido o perdido por inasistencia) y además permite conservar fecha, hora, médico y especialidad visibles en `GET /api/appointments/search`.
6. **Sin cambios de esquema en base de datos:** La columna `Status` en `Appointments` ya persiste `DateState` como `varchar(20)` (`HasConversion<string>()`), por lo que no se requieren migraciones de EF Core.

---

## 3. Contrato de la API

### 3.1 Actualizar estado de una cita

- **Método y ruta:** `PATCH /api/appointments/{id}/status`
- **Autorización:** `Bearer <JWT>` con rol `ADMINISTRADOR` (`Policies.AdminPolicy`).
- **Parámetro de ruta:**
  - `id` (`Guid`, obligatorio): Identificador de la cita (`Appointments.Id`).
- **Request Body (`application/json`):**

```json
{
  "status": "ATTENDED"
}
```

Valores admitidos para `status`: `"ATTENDED"` (o `2`) y `"NO_SHOW"` (o `3`).

- **Respuestas:**

| Código HTTP | Escenario | ErrorCode / Cuerpo |
| :--- | :--- | :--- |
| `200 OK` | Estado actualizado correctamente. | `AppointmentModel.Response` con el nuevo `status`. |
| `400 Bad Request` | `status` inválido (ej. `BOOKED`, `CANCELLED` o valor inexistente). | `APPOINTMENT_INVALID_STATUS_TRANSITION` |
| `401 Unauthorized` | Sin token JWT o token expirado/inválido. | — |
| `403 Forbidden` | Usuario autenticado con rol `PACIENTE`. | — |
| `404 Not Found` | No existe una cita con el `id` indicado. | `ENTITY_NOTFOUND` |
| `409 Conflict` | La cita no está en estado `BOOKED` (ya fue cancelada, completada o marcada ausente). | `APPOINTMENT_STATUS_CHANGE_NOT_ALLOWED` |

- **Ejemplo de respuesta `200 OK`:**

```json
{
  "id": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "appointmentDate": "2026-11-25",
  "status": "ATTENDED",
  "cancellationDate": null,
  "reason": "Control cardiológico anual",
  "turnId": "8a1b2c3d-4e5f-6789-abcd-ef0123456789",
  "patientId": "1a2b3c4d-5e6f-7890-abcd-ef0123456789",
  "patientDni": "35123456",
  "startTime": "09:00:00",
  "endTime": "09:30:00",
  "doctor": {
    "doctorId": "9f8e7d6c-5b4a-3210-fedc-ba9876543210",
    "name": "Dr. Alejandro Martínez",
    "speciality": {
      "specialityId": "0a1b2c3d-4e5f-6789-0123-456789abcdef",
      "name": "Cardiología"
    }
  }
}
```

### 3.2 Filtro opcional por estado en Búsqueda Avanzada (Retrocompatible)

- **Método y ruta:** `GET /api/appointments/search?pageSize=10&pageIndex=1&dni=&doctorId=&SpecialityId=&date=&status=`
- **Nuevo query param opcional:** `status` (`DateState?`: `BOOKED`, `CANCELLED`, `ATTENDED`, `NO_SHOW`).
- Si no se envía `status`, mantiene exactamente el comportamiento previo (devuelve citas de todos los estados).

---

## 4. Impacto en el Resto del Sistema

| Componente | Comportamiento al marcar `ATTENDED` o `NO_SHOW` |
| :--- | :--- |
| `GET /api/appointments/patient?dni=` | Filtra por `d.Status == DateState.BOOKED`, por lo que los turnos atendidos o ausentes dejan de listarse como turnos activos del paciente (conforme a especificación TPI). |
| `DELETE /api/appointments/{id}` | Rechaza con `409 Conflict` (`APPOINTMENT_ALREADY_CANCELLED`) cualquier intento de cancelar una cita ya `ATTENDED` o `NO_SHOW`. |
| `GET /api/appointments/search` | Devuelve `status: "ATTENDED"` o `"NO_SHOW"` junto con fecha, horario, médico, especialidad y paciente. |
| `GET /api/dashboard/stats` | `todayConfirmedAppointments` cuenta únicamente citas en `BOOKED` del día actual. |
| Frontend (`appointmentStatus.js`) | Ya mapea `ATTENDED` → badge *"Completado"* y `NO_SHOW` → badge *"Ausente"* tanto en string como en código numérico. |

---

## 5. Arquitectura y Archivos Involucrados

Siguiendo la arquitectura en capas del backend:

1. **CrossCutting (`Dsw2026Tpi.CrossCutting`):**
   - `Resources/ErrorCodes.resx` y `Resources/ErrorCodes.Designer.cs`: nuevos códigos `APPOINTMENT_INVALID_STATUS_TRANSITION` y `APPOINTMENT_STATUS_CHANGE_NOT_ALLOWED`.
2. **Application (`Dsw2026Tpi.Application`):**
   - `Dtos/AppointmentModel.cs`: nuevo record `UpdateStatusRequest(DateState Status)`.
   - `Interfaces/IAppointmentService.cs`: firma `UpdateStatus(Guid id, AppointmentModel.UpdateStatusRequest request)` y parámetro opcional `DateState? status = null` en `Search`.
   - `Services/AppointmentService.cs`: implementación de `UpdateStatus` llamando a `appointment.Complete()` / `appointment.NoShow()` en el dominio, validaciones, logging con Serilog y filtro opcional en `Search`.
3. **Api (`Dsw2026Tpi.Api`):**
   - `Controllers/AppointmentController.cs`: endpoint `[HttpPatch("{id}/status")]` protegido con `[Authorize(Policy = Policies.AdminPolicy)]` y query param opcional `status` en `Search`.
4. **Documentación:**
   - `docs/APPOINTMENT_STATUS_TRANSITIONS.md` (este documento).
   - `README.md`: actualización de la tabla de endpoints de `/api/appointments`.

---

## 6. Verificación Manual (Swagger / cURL)

1. Iniciar sesión como administrador: `POST /api/auth/admin/login` (`admin@system.com` / `Admin123!`) y autorizar en Swagger con `Bearer <token>`.
2. Consultar un turno existente en estado `BOOKED` vía `GET /api/appointments/search` y copiar su `id`.
3. Ejecutar `PATCH /api/appointments/{id}/status` con body `{ "status": "ATTENDED" }` (o `{ "status": "NO_SHOW" }`).
4. Comprobar:
   - Devuelve `200 OK` con `"status": "ATTENDED"` (o `"NO_SHOW"`).
   - Al consultar `GET /api/appointments/search` (o abrir **Búsqueda de Pacientes** en el Frontend), la cita muestra la etiqueta **Completado** o **Ausente**.
   - Al reintentar `PATCH /api/appointments/{id}/status` sobre la misma cita, devuelve `409 Conflict`.
   - Al consultar `GET /api/appointments/patient?dni=<dni>`, la cita ya no aparece entre los turnos activos.
