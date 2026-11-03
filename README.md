# Sistema Danielito

Sistema de seguimiento de niños, grupos, actividades y asistencia de la Fundación Danielito
(Warnes, Santa Cruz).

## Estructura

| Carpeta | Contenido |
|---|---|
| `backend/` | API REST en ASP.NET Core 10, con Entity Framework Core, Identity y JWT |
| `frontend/` | Interfaz web en React 19 con TypeScript y Vite |

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

La dirección de la API se configura en `frontend/.env` mediante `VITE_API_URL`. Ese mismo
origen debe estar autorizado en `Cors:OrigenesPermitidos` dentro de `backend/appsettings.json`.

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
cada asistencia. Ten en cuenta que al desactivar a alguien su sesión sigue siendo válida
hasta que expire el token, que dura 2 horas.
