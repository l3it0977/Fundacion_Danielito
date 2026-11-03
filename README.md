# Sistema Danielito

Sistema de seguimiento de niños, grupos, actividades y asistencia de la Fundación Danielito
(Warnes, Santa Cruz).

## Estructura

| Carpeta | Contenido |
|---|---|
| `backend/` | API REST en ASP.NET Core 10, con Entity Framework Core, Identity y JWT |
| `frontend/` | Interfaz web en React 19 con TypeScript y Vite |
| `backend.Tests/` | Pruebas automatizadas del backend (xUnit + SQLite en memoria) |

## Requisitos

- .NET SDK 10
- Node.js 20 o superior
- PostgreSQL 15 o superior

## Puesta en marcha

### Backend

La cadena de conexión y las claves no se guardan en el repositorio: van en user-secrets.

```bash
cd backend

dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=5432;Database=sistema_danielito;Username=postgres;Password=TU_CONTRASEÑA"

# Clave con la que se firman los tokens. Debe tener al menos 32 caracteres.
dotnet user-secrets set "Jwt:SecretKey" "UNA_CLAVE_LARGA_Y_ALEATORIA"

# Contraseña inicial de los usuarios que crea el seeder al arrancar.
# Mínimo 8 caracteres, con mayúscula, minúscula, número y símbolo.
dotnet user-secrets set "Seed:PasswordInicial" "UnaClave.Seguraa1"

# Crea las tablas. Cada contexto se migra por separado.
dotnet ef database update --context AppIdentityContext
dotnet ef database update --context AppProyectoContext

dotnet run
```

La API queda en `http://localhost:5002`.

Al arrancar por primera vez se crean los tres roles (Directora, Administrador, Maestro),
los usuarios iniciales y los grupos con su maestro a cargo.

### Frontend

```bash
cd frontend
npm install
npm run dev
```

La interfaz queda en `http://localhost:5173`.

La dirección de la API se configura en `frontend/.env` mediante `VITE_API_URL` (ver
`frontend/.env.example`). Ese mismo origen debe estar autorizado en `Cors:OrigenesPermitidos`;
para desarrollo ya viene en `backend/appsettings.Development.json`. En producción no hay
orígenes permitidos por omisión: se definen con la variable `Cors__OrigenesPermitidos__0`, o no
hacen falta si la interfaz y la API salen por el mismo origen (como en el despliegue con Docker).

## Despliegue

`docker-compose.yml` levanta base de datos, API e interfaz web con un solo comando. Ver la guía
completa, con HTTPS y copias de seguridad, en [docs/despliegue.md](docs/despliegue.md).

```bash
cp .env.example .env     # llenar las tres claves
docker compose up -d --build
```

## Pruebas

```bash
# Backend: permisos de los maestros, asistencia, reportes, baja de niños, validación de cuentas,
# bitácora y pruebas de integración de la aplicación completa (cabeceras, 401/403, límite de login).
# Si el servidor está corriendo, deténlo antes: Windows bloquea los archivos que está usando.
dotnet test backend.Tests

# Frontend: lectura de las coordenadas del mapa.
cd frontend
npm test
```

Las pruebas del backend usan SQLite en memoria, así que no tocan tu base de datos ni necesitan
PostgreSQL. La integración continua (`.github/workflows/ci.yml`) las corre, junto con el lint, las
pruebas y la compilación del frontend, en cada cambio.

## Copias de seguridad

`scripts/respaldos/` trae los scripts para respaldar, programar y restaurar la base. Ver
[docs/respaldos.md](docs/respaldos.md).

## Migrar a Supabase

Supabase expone PostgreSQL estándar, así que basta con cambiar la cadena de conexión:

```bash
cd backend
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=HOST.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.REFERENCIA;Password=TU_CONTRASEÑA;SSL Mode=Require;Trust Server Certificate=true"
```

Después se aplican las migraciones contra la nueva base. No hay que tocar código.

## Arquitectura del backend

Está organizado en capas: el controlador recibe la petición y delega en un servicio, que
concentra la lógica y el acceso a datos a través de Entity Framework.

```
Controllers/   Reciben y responden, sin lógica de negocio
Interfaces/    Un contrato por servicio
Services/      Lógica y acceso a datos
Models/        Entidades de la base
DTOS/          Lo que entra y sale por la API; las entidades nunca se exponen
Data/          AppProyectoContext (dominio) y AppIdentityContext (seguridad)
Mapper/        Conversión entre entidades y DTOs con AutoMapper
Middleware/    Traduce las excepciones al código HTTP correspondiente
```

Seguridad y dominio usan contextos separados, cada uno con su propia tabla de historial
de migraciones, sobre la misma base de datos.

## Roles

- **Directora** y **Administrador**: acceso completo, incluida la gestión de usuarios.
- **Maestro**: ve solo los niños y actividades de los grupos que tiene a cargo.

Los usuarios no se eliminan, se desactivan, para conservar el registro de quién anotó
cada asistencia. Al desactivar a alguien pierde el acceso de inmediato: el servidor
comprueba en cada petición que la cuenta siga activa (con un respaldo en memoria de un
minuto), así que no hay que esperar a que expire el token de 2 horas.

Toda alta, cambio y baja de niños, grupos, actividades, visitas y cuentas queda en la
**Bitácora** (menú, solo dirección), con quién lo hizo y el antes y después de cada campo.

Los niños siguen la misma lógica: se desactivan en lugar de borrarse, porque su historial
de asistencias y visitas alimenta los reportes. Solo se puede eliminar una ficha creada por
error, que aún no tenga ningún registro.
