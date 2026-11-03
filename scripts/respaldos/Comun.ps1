# Funciones compartidas por los scripts de respaldo. No se ejecuta directamente: los demás
# scripts lo cargan con ". .\Comun.ps1" y usan sus parámetros ($PgBin, $CadenaConexion y
# $ProyectoBackend).

function Buscar-PgBin {
    if ($PgBin) { return $PgBin }

    $enPath = Get-Command pg_dump.exe -ErrorAction SilentlyContinue
    if ($enPath) { return Split-Path $enPath.Source }

    $candidatas = @()
    if ($env:POSTGRES_BIN) { $candidatas += $env:POSTGRES_BIN }
    $candidatas += Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin' -ErrorAction SilentlyContinue |
        Sort-Object { [int]($_.Parent.Parent.Name -replace '\D', '0') } -Descending |
        ForEach-Object FullName
    $candidatas += 'D:\PostgreSQL\bin'

    foreach ($carpeta in $candidatas) {
        if (Test-Path (Join-Path $carpeta 'pg_dump.exe')) { return $carpeta }
    }

    throw 'No se encontró pg_dump.exe. Indica la carpeta con -PgBin "D:\PostgreSQL\bin".'
}

function Leer-CadenaConexion {
    if ($CadenaConexion) { return $CadenaConexion }

    if ($env:ConnectionStrings__DefaultConnection) {
        return $env:ConnectionStrings__DefaultConnection
    }

    $secretos = & dotnet user-secrets list --project $ProyectoBackend 2>$null
    foreach ($linea in $secretos) {
        if ($linea -match '^ConnectionStrings:DefaultConnection\s*=\s*(.+)$') {
            return $Matches[1].Trim()
        }
    }

    throw 'No se encontró la cadena de conexión (parámetro, variable de entorno ni user-secrets).'
}

function Convertir-Conexion([string]$cadena) {
    $partes = @{}
    foreach ($par in $cadena.Split(';')) {
        if ($par -match '^\s*([^=]+?)\s*=\s*(.*?)\s*$') {
            $partes[$Matches[1].ToLowerInvariant()] = $Matches[2]
        }
    }

    function Primero($nombres) {
        foreach ($n in $nombres) { if ($partes.ContainsKey($n)) { return $partes[$n] } }
        return $null
    }

    $conexion = @{
        Host     = Primero @('host', 'server')
        Puerto   = Primero @('port')
        Base     = Primero @('database', 'db')
        Usuario  = Primero @('username', 'user id', 'user', 'uid')
        Password = Primero @('password', 'pwd')
    }

    if (-not $conexion.Host -or -not $conexion.Base -or -not $conexion.Usuario) {
        throw 'La cadena de conexión debe incluir Host, Database y Username.'
    }
    if (-not $conexion.Puerto) { $conexion.Puerto = '5432' }

    return $conexion
}
