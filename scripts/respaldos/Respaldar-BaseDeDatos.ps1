<#
.SYNOPSIS
    Copia de seguridad de la base de datos del Sistema Danielito con pg_dump.

.DESCRIPTION
    Genera un archivo .dump comprimido (formato personalizado de PostgreSQL), comprueba que
    se pueda leer con pg_restore y borra los respaldos más viejos que -DiasDeRetencion.

    La cadena de conexión sale, en este orden, de:
      1. el parámetro -CadenaConexion,
      2. la variable de entorno ConnectionStrings__DefaultConnection,
      3. los user-secrets del proyecto backend (los mismos que usa la API).
    Así la contraseña no se escribe en ningún script ni en el repositorio.

.EXAMPLE
    .\Respaldar-BaseDeDatos.ps1
    .\Respaldar-BaseDeDatos.ps1 -Destino E:\Respaldos -DiasDeRetencion 60
#>
[CmdletBinding()]
param(
    # Carpeta donde se guardan los respaldos. Conviene que esté en OTRO disco que la base.
    [string]$Destino = (Join-Path $env:USERPROFILE 'Respaldos-Danielito'),

    # Los respaldos más antiguos que esto se borran.
    [int]$DiasDeRetencion = 30,

    [string]$CadenaConexion,

    # Carpeta bin de PostgreSQL. Si se omite, se busca sola.
    [string]$PgBin,

    [string]$ProyectoBackend = (Join-Path $PSScriptRoot '..\..\backend')
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot 'Comun.ps1')

$bin = Buscar-PgBin
$conexion = Convertir-Conexion (Leer-CadenaConexion)

New-Item -ItemType Directory -Force -Path $Destino | Out-Null

$marca = Get-Date -Format 'yyyy-MM-dd_HHmmss'
$archivo = Join-Path $Destino "$($conexion.Base)_$marca.dump"
$registro = Join-Path $Destino 'respaldos.log'

function Escribir-Registro([string]$mensaje) {
    $linea = "$(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')  $mensaje"
    Add-Content -Path $registro -Value $linea
    Write-Host $linea
}

# La contraseña viaja solo en el entorno de este proceso, nunca en la línea de comandos.
$passwordAnterior = $env:PGPASSWORD
$env:PGPASSWORD = $conexion.Password

try {
    Escribir-Registro "Iniciando respaldo de '$($conexion.Base)' en $($conexion.Host):$($conexion.Puerto)"

    & (Join-Path $bin 'pg_dump.exe') `
        --host $conexion.Host --port $conexion.Puerto --username $conexion.Usuario `
        --format custom --compress 9 --no-owner --file $archivo $conexion.Base

    if ($LASTEXITCODE -ne 0) { throw "pg_dump terminó con código $LASTEXITCODE." }

    # Un respaldo que no se puede leer no sirve: se comprueba antes de darlo por bueno.
    & (Join-Path $bin 'pg_restore.exe') --list $archivo | Out-Null
    if ($LASTEXITCODE -ne 0) { throw 'El archivo generado no se puede leer con pg_restore.' }

    $tamano = [math]::Round((Get-Item $archivo).Length / 1KB, 1)
    Escribir-Registro "Respaldo correcto: $archivo ($tamano KB)"

    $limite = (Get-Date).AddDays(-$DiasDeRetencion)
    $viejos = Get-ChildItem $Destino -Filter "$($conexion.Base)_*.dump" |
        Where-Object { $_.LastWriteTime -lt $limite }

    foreach ($viejo in $viejos) {
        Remove-Item $viejo.FullName -Force
        Escribir-Registro "Borrado por antigüedad (> $DiasDeRetencion días): $($viejo.Name)"
    }
}
catch {
    Escribir-Registro "ERROR: $($_.Exception.Message)"
    if (Test-Path $archivo) { Remove-Item $archivo -Force }
    exit 1
}
finally {
    $env:PGPASSWORD = $passwordAnterior
}
