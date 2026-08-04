# Trabajo Práctico Integrador
## Desarrollo de Software 2026
### Carabajal, Lourdes Camila - 56696 
### Roman, Iara Belen - 53431
### Saavedra, Mariana - 53441

Acceso al [documento](https://frtutneduar-my.sharepoint.com/:b:/g/personal/franciscovicente_doc_frt_utn_edu_ar/IQD-5kaAARqnT5eL7EnPMCPgAfmPoH09HkfmQQYH8pMgqdA?e=i6C3fT)

Instrucciones:
* Realizar una bifurcación por grupo
* Crear una rama de larga duración `development`
* Completar `README` con los integrantes en cada bifurcación
* Todos los integrantes deben participar con confirmaciones en el repositorio bifurcado
* Organizar el trabajo en equipo y crear ramas temporales
* Actualizar la rama de larga duración mediante **pull-requests**
* No eliminar las ramas temporales
* Tener en cuenta que ya se realizaron las migraciones de Identity, crear nuevas de ser necesario
* Para más detalles, revisar la grabación de la última clase
* El endpoint de registración de usuarios administradores está disponible para crear usuarios y poder hacer pruebas, a futuro se eliminará


## CONFIGURACION INICIAL:

#### Requisitos previos
- .NET 10 SDK
- SQL Server LocalDB (viene incluido con Visual Studio, o se instala aparte con SQL Server Express/LocalDB)
- Herramienta CLI de EF Core:

dotnet tool install --global dotnet-ef
### Pasos

### 1 Restaurar dependencias

dotnet restore Dsw2026Tpi.slnx
Aplicar las migraciones de base de datos 

dotnet ef database update --project Dsw2026Tpi.Data --startup-project Dsw2026Tpi.Api --context Dsw2026TpiDbContext
dotnet ef database update --project Dsw2026Tpi.Data --startup-project Dsw2026Tpi.Api --context AuthenticationDbContext

Esto crea la base Dsw2026Tpi en LocalDB, siembra los roles (Administrador, Paciente) y deja todas las tablas listas.

### 2 Ejecutar la API

dotnet run --project Dsw2026Tpi.Api
Al arrancar, el sistema crea automáticamente un usuario administrador con las credenciales definidas en appsettings.Development.json (por defecto: admin@system.com / Admin123!), si todavía no existe.

### 3 Abrir Swagger
La app queda escuchando en http://localhost:5278 (o https://localhost:7075). 
Entrar a: http://localhost:5278/swagger
Ahí se pueden probar todos los endpoints de forma interactiva.

### 4 Autenticarse en Swagger
Ejecutar POST /api/auth/admin/login con el usuario sembrado (o POST /api/auth/patient/login con cualquier email + DNI, que autoregistra al paciente).
Copiar el token de la respuesta.
Click en el botón Authorize (arriba a la derecha en Swagger) y pegar Bearer <token>.
A partir de ahí, todos los endpoints protegidos van a incluir el token automáticamente.

## ENDPOINTS IMPLEMENTADOS

Todas las respuestas de error siguen el formato `{ "errorCode": "...", "message": "...", "details": [...] }`. Los endpoints marcados como protegidos requieren el header `Authorization: Bearer <token>`.

### Autenticación (`/api/auth`)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/auth/admin/register` | Admin | Crea un nuevo administrador. Body: `{ email, password }` |
| POST | `/api/auth/admin/login` | Público | Login de administrador. Body: `{ email, password }` → `{ token, role }` |
| POST | `/api/auth/patient/login` | Público | Login de paciente; si el DNI/email no existe, lo autoregistra. Body: `{ email, dni }` → `{ token, role }` |

### Especialidades (`/api/specialties`)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/api/specialties?pageSize&pageIndex&name` | Cualquier usuario autenticado | Lista paginada de especialidades activas, filtrable por nombre |
| GET | `/api/specialties/{id}` | Cualquier usuario autenticado | Detalle de una especialidad |
| POST | `/api/specialties` | Admin | Crea una especialidad. Body: `{ name, description }` |
| PUT | `/api/specialties/{id}` | Admin | Actualiza una especialidad |
| DELETE | `/api/specialties/{id}` | Admin | Baja lógica |

### Médicos (`/api/doctors`)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/api/doctors?pageSize&pageIndex&name` | Cualquier usuario autenticado | Lista paginada de médicos activos |
| GET | `/api/doctors/{id}` | Cualquier usuario autenticado | Detalle de un médico |
| POST | `/api/doctors` | Admin | Crea un médico. Body: `{ name, licenseNumber, specialtyId }` |
| PUT | `/api/doctors/{id}` | Admin | Actualiza un médico |
| DELETE | `/api/doctors/{id}` | Admin | Baja lógica |
| GET | `/api/doctors/{doctorId}/availabilities?year&month` | Cualquier usuario autenticado | Franjas horarias configuradas para ese médico |

### Disponibilidades (`/api/availabilities`)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/availabilities` | Admin | Define la disponibilidad mensual de un médico y genera automáticamente los turnos de 30 minutos (excluyendo feriados). Body: `{ doctorId, year, month, days: [{ dayOfWeek, startTime, endTime }] }` |
| PUT | `/api/availabilities` | Admin | Reemplaza toda la disponibilidad del mes indicado (mismo body que el POST) |

### Turnos (`/api/appointments`)

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| POST | `/api/appointments` | Paciente | Reserva un turno. Body: `{ doctorId, availabilitySlotId, patient: { dni }, reason }` |
| GET | `/api/appointments/patient?dni` | Autenticado (un paciente solo puede consultar su propio DNI; Admin puede consultar cualquiera) | Turnos activos (reservados) de un paciente |
| DELETE | `/api/appointments/{id}` | Paciente | Cancela un turno reservado |
| GET | `/api/appointments/search?pageSize&pageIndex&patientDni&doctorId&specialtyId&date` | Admin | Búsqueda avanzada paginada, combinando filtros |
| GET | `/api/appointments?date=YYYY-MM-DD` | Admin | Turnos agendados para un día puntual |

### Salud

| Método | Ruta | Auth | Descripción |
|--------|------|------|-------------|
| GET | `/health-check` | Público | Chequeo de estado de la API |
