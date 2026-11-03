<#
.SYNOPSIS
    Programa el respaldo diario de la base de datos en el Programador de tareas de Windows.

.DESCRIPTION
    Crea (o reemplaza) la tarea "Respaldo Sistema Danielito", que ejecuta
    Respaldar-BaseDeDatos.ps1 todos los días a la hora indicada. Corre con tu usuario, que es
    el que tiene los user-secrets con la conexión a la base.

    Si el equipo está apagado a esa hora, la tarea se ejecuta al encenderlo.

.EXAMPLE
    .\Registrar-TareaDeRespaldo.ps1 -WhatIf          # solo muestra lo que haría
    .\Registrar-TareaDeRespaldo.ps1 -Hora 23:30 -Destino E:\Respaldos
#>
[CmdletBinding(SupportsShouldProcess)]
param(
    [string]$Hora = '02:00',
    [string]$Destino = (Join-Path $env:USERPROFILE 'Respaldos-Danielito'),
    [int]$DiasDeRetencion = 30,
    [string]$NombreDeLaTarea = 'Respaldo Sistema Danielito'
)

$ErrorActionPreference = 'Stop'

$script = Join-Path $PSScriptRoot 'Respaldar-BaseDeDatos.ps1'
$argumentos = "-NoProfile -ExecutionPolicy Bypass -File `"$script`" -Destino `"$Destino`" -DiasDeRetencion $DiasDeRetencion"

$accion = New-ScheduledTaskAction -Execute 'powershell.exe' -Argument $argumentos
$disparador = New-ScheduledTaskTrigger -Daily -At $Hora
$ajustes = New-ScheduledTaskSettingsSet -StartWhenAvailable -ExecutionTimeLimit (New-TimeSpan -Hours 1)

if ($PSCmdlet.ShouldProcess($NombreDeLaTarea, "Programar respaldo diario a las $Hora en $Destino")) {
    Register-ScheduledTask -TaskName $NombreDeLaTarea -Action $accion -Trigger $disparador `
        -Settings $ajustes -Description 'Copia de seguridad diaria de la base del Sistema Danielito' `
        -Force | Out-Null

    Write-Host "Tarea '$NombreDeLaTarea' registrada: todos los días a las $Hora, guarda en $Destino."
    Write-Host "Pruébala ahora con: Start-ScheduledTask -TaskName '$NombreDeLaTarea'"
}
