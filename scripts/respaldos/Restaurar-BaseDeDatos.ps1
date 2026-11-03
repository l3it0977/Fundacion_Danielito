<#
.SYNOPSIS
    Restaura un respaldo en una base NUEVA, para comprobar que sirve o para recuperar datos.

.DESCRIPTION
    Crea la base indicada en -BaseDestino (debe NO existir) y carga en ella el .dump.
    Nunca pisa la base de producción: si quieres reemplazarla, hazlo a mano y con la API detenida
    (ver docs/respaldos.md).

    Un respaldo que nunca se probó no es un respaldo: conviene restaurar uno cada cierto tiempo.

.EXAMPLE
    .\Restaurar-BaseDeDatos.ps1 -Archivo E:\Respaldos\sistema_danielito_2026-11-03_020000.dump -BaseDestino prueba_restauracion
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)] [string]$Archivo,
    [Parameter(Mandatory)] [string]$BaseDestino,

    [string]$CadenaConexion,
    [string]$PgBin,
    [string]$ProyectoBackend = (Join-Path $PSScriptRoot '..\..\backend')
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path $Archivo)) { throw "No existe el archivo: $Archivo" }

. (Join-Path $PSScriptRoot 'Comun.ps1')

$bin = Buscar-PgBin
$conexion = Convertir-Conexion (Leer-CadenaConexion)

if ($BaseDestino -eq $conexion.Base) {
    throw "Por seguridad no se restaura sobre la base en uso ('$($conexion.Base)'). Elige otro nombre."
}

$passwordAnterior = $env:PGPASSWORD
$env:PGPASSWORD = $conexion.Password

try {
    $comunes = @('--host', $conexion.Host, '--port', $conexion.Puerto, '--username', $conexion.Usuario)

    & (Join-Path $bin 'createdb.exe') @comunes $BaseDestino
    if ($LASTEXITCODE -ne 0) { throw "No se pudo crear la base '$BaseDestino' (¿ya existe?)." }

    & (Join-Path $bin 'pg_restore.exe') @comunes --no-owner --dbname $BaseDestino $Archivo
    if ($LASTEXITCODE -ne 0) { throw "pg_restore terminó con código $LASTEXITCODE." }

    Write-Host "Restauración completa en la base '$BaseDestino'."
    Write-Host "Cuando termines de revisarla: dropdb --host $($conexion.Host) --username $($conexion.Usuario) $BaseDestino"
}
finally {
    $env:PGPASSWORD = $passwordAnterior
}
