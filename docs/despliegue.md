# Despliegue con Docker

`docker-compose.yml` levanta todo el sistema en un servidor: base de datos, API e interfaz web.

```
Internet ──► [ web: nginx ] ──/api──► [ api: .NET ] ──► [ db: PostgreSQL ]
                 :8080                  (solo dentro            (solo dentro
                                         de Docker)              de Docker)
```

Solo la interfaz web publica un puerto. La API y la base de datos no son alcanzables desde fuera:
todo pasa por `/api`, que nginx reenvía a la API. Como el navegador siempre habla con un único
origen, no hace falta configurar CORS.

## Primera vez

Requisitos: Docker con Compose v2.

```bash
cp .env.example .env
```

Llena el `.env` (cada variable está explicada dentro del archivo):

| Variable | Qué es | Cómo generarla |
|---|---|---|
| `POSTGRES_PASSWORD` | Clave de la base de datos | `openssl rand -hex 24` |
| `JWT_SECRET_KEY` | Clave con la que se firman las sesiones | `openssl rand -base64 48` |
| `SEED_PASSWORD_INICIAL` | Contraseña de los usuarios iniciales | una clave fuerte con mayúscula, minúscula, número y símbolo |

Después:

```bash
docker compose up -d --build
```

La primera vez la API crea las tablas, los tres roles, los usuarios iniciales y los grupos.
La interfaz queda en `http://IP-DEL-SERVIDOR:8080`.

> **Cambia las contraseñas iniciales.** Todos los usuarios sembrados comparten
> `SEED_PASSWORD_INICIAL`. Entra con cada cuenta y cámbiala desde "Perfil", o desactiva las que
> no uses desde "Usuarios".

## HTTPS (obligatorio fuera de una red de confianza)

Sin HTTPS, las contraseñas y los datos de los niños viajan sin cifrar. Pon un proxy con
certificado delante de la interfaz web. Con [Caddy](https://caddyserver.com), que consigue y
renueva el certificado solo, basta este archivo (`Caddyfile`) en el mismo servidor:

```
sistema.tu-dominio.org {
    reverse_proxy localhost:8080
}
```

El nginx de la interfaz confía en la cabecera `X-Forwarded-For` únicamente cuando llega desde una
red privada (el proxy en el mismo servidor o en la misma red de Docker). Un visitante de internet
no puede falsificarla. De esa dirección depende el límite de 10 intentos de login por minuto.

## Operación diaria

```bash
docker compose ps                      # estado
docker compose logs -f api             # registros de la API
docker compose restart api             # reiniciar solo la API
curl http://localhost:8080/api/salud   # {"estado":"ok"} si la API alcanza la base
```

### Actualizar a una versión nueva

```bash
git pull
docker compose up -d --build
```

Las migraciones se aplican solas al arrancar la API. **Haz un respaldo antes de actualizar.**

### Copias de seguridad

Los datos viven en el volumen `datos-db`. Para respaldar:

```bash
docker compose exec -T db pg_dump -U danielito -Fc sistema_danielito > respaldo-$(date +%F).dump
```

Para restaurar en una base nueva y revisarla (nunca sobre la que está en uso):

```bash
docker compose exec -T db createdb -U danielito prueba_restauracion
docker compose exec -T db pg_restore -U danielito -d prueba_restauracion --no-owner < respaldo-2026-11-03.dump
```

Programa el primer comando con `cron` y guarda los archivos **fuera de este servidor**. Ver también
[respaldos.md](respaldos.md) para la instalación sin Docker.

## Ajustes avanzados

La API lee su configuración de variables de entorno (en `docker-compose.yml`, sección `api`).
El doble guion bajo reemplaza a los dos puntos: `Seguridad__IntentosDeLoginPorMinuto` es
`Seguridad:IntentosDeLoginPorMinuto`.

| Ajuste | Por omisión | Para qué |
|---|---|---|
| `Seguridad__IntentosDeLoginPorMinuto` | 10 | Intentos de login por IP antes de responder 429 |
| `Seguridad__RedirigirAHttps` | false | Que la propia API redirija a HTTPS (solo si ella atiende TLS) |
| `ForwardedHeaders__Habilitado` | false (true en el Compose) | Confiar en `X-Forwarded-*`; solo si la API no es accesible desde fuera |
| `BaseDeDatos__MigrarAlIniciar` | false (true en el Compose) | Aplicar migraciones al arrancar |
| `Cors__OrigenesPermitidos__0` | ninguno | Orígenes permitidos, si la interfaz se sirve desde otro dominio |

El proveedor de mapas se cambia al construir la interfaz con los argumentos `VITE_TILES_URL` y
`VITE_TILES_ATTRIBUTION` (ver `frontend/.env.example`). Hoy usa OpenStreetMap, que es gratuito pero
pide un uso moderado.

## Qué no cubre esta guía

- **Varias copias de la API a la vez.** Las migraciones al arrancar asumen una sola. Con varias
  hay que aplicarlas aparte antes de levantarlas.
- **Fuentes de Google.** La interfaz carga la tipografía desde Google Fonts, así que cada visita
  se la comunica a Google. Si eso importa, se puede alojar la fuente en el propio servidor.
