# Copias de seguridad de la base de datos

La base guarda datos sensibles de menores (vulnerabilidad, ubicación de la vivienda, asistencia) y
no se puede reconstruir. Los scripts de `scripts/respaldos/` hacen una copia diaria con `pg_dump`.

## Hacer un respaldo ahora

```powershell
cd scripts\respaldos
.\Respaldar-BaseDeDatos.ps1
```

- Guarda `sistema_danielito_AAAA-MM-DD_HHMMSS.dump` en `%USERPROFILE%\Respaldos-Danielito`.
- Comprueba que el archivo se pueda leer con `pg_restore` antes de darlo por bueno.
- Borra los respaldos de más de 30 días (`-DiasDeRetencion`).
- Registra cada paso en `respaldos.log`, dentro de la misma carpeta.
- Toma la conexión de los user-secrets del backend: la contraseña no se escribe en ningún script.

Parámetros útiles: `-Destino E:\Respaldos`, `-DiasDeRetencion 60`, `-PgBin "D:\PostgreSQL\bin"`
(si `pg_dump` no se encuentra solo).

> **Pon el destino en otro disco** (o en una carpeta sincronizada con la nube). Un respaldo en el
> mismo disco que la base no protege de un fallo del disco.

## Programarlo todos los días

```powershell
.\Registrar-TareaDeRespaldo.ps1 -WhatIf     # muestra lo que haría, sin hacerlo
.\Registrar-TareaDeRespaldo.ps1             # 02:00 todos los días
.\Registrar-TareaDeRespaldo.ps1 -Hora 23:30 -Destino E:\Respaldos
```

Crea la tarea "Respaldo Sistema Danielito" en el Programador de tareas, con tu usuario (el de los
user-secrets). Si el equipo está apagado a esa hora, se ejecuta al encenderlo.
Para probarla: `Start-ScheduledTask -TaskName "Respaldo Sistema Danielito"`.

## Comprobar que un respaldo sirve

Un respaldo que nunca se restauró no es de fiar. Una vez al mes, restaura el último en una base
nueva y revísala:

```powershell
.\Restaurar-BaseDeDatos.ps1 -Archivo "$env:USERPROFILE\Respaldos-Danielito\sistema_danielito_2026-11-03_020000.dump" -BaseDestino prueba_restauracion
```

El script se niega a restaurar sobre la base en uso. Al terminar, borra la de prueba con
`dropdb --username postgres prueba_restauracion`.

## Recuperar la base de verdad

1. Detén la API.
2. Restaura en una base nueva (comando de arriba) y comprueba que los datos son los esperados.
3. Cambia la cadena de conexión para que apunte a esa base:
   `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=...;Database=prueba_restauracion;..."`
4. Arranca la API y comprueba. Cuando estés conforme, renombra o borra la base dañada.

## Si usas Supabase

Supabase ya hace copias diarias en sus planes de pago (los gratuitos no las incluyen). Aun así,
puedes usar estos mismos scripts apuntando la cadena de conexión a Supabase.
