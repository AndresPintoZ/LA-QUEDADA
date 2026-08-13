# =============================================================================
# Siembra datos de demostración en un entorno local de PlanVibe.
#
#   .\scripts\sembrar-datos-de-demostracion.ps1
#
# Crea una organizadora verificada, dos planes reales de Ávila y una persona
# apuntada a uno de ellos. Sirve para ver la aplicación con contenido sin tener
# que rellenar formularios a mano.
#
# SOLO PARA DESARROLLO. Usa el proveedor de verificación simulado, que aprueba a
# cualquiera sin comprobar ninguna identidad.
# =============================================================================

[CmdletBinding()]
param(
    [string]$Api = 'http://localhost:8080'
)

$ErrorActionPreference = 'Stop'

# -----------------------------------------------------------------------------
# Los caracteres acentuados se construyen con su punto de código en lugar de
# escribirlos literalmente.
#
# El motivo: PowerShell 5.1 lee los archivos .ps1 sin BOM como ANSI, no como
# UTF-8. Un "é" escrito directamente aquí se leería como dos caracteres y llegaría
# doblemente codificado a la base de datos. Con [char] el resultado es el mismo
# en cualquier versión de PowerShell y con cualquier codificación del archivo.
# -----------------------------------------------------------------------------
$aAcentuada = [char]0x00C1   # Á
$eAcentuada = [char]0x00E9   # é
$enie       = [char]0x00F1   # ñ

$avila       = "$($aAcentuada)vila"
$valleAmbles = "Valle Ambl$($eAcentuada)s"
$penia       = "El Barraco de la Pe$($enie)a"

function Enviar {
    param($Metodo, $Ruta, $Cuerpo, $Token)

    $parametros = @{
        Uri         = ($Api + $Ruta)
        Method      = $Metodo
        ContentType = 'application/json; charset=utf-8'
    }

    if ($Token) { $parametros.Headers = @{ Authorization = ('Bearer ' + $Token) } }

    # Bytes UTF-8 explícitos: PowerShell 5.1 codifica el cuerpo en ISO-8859-1 por
    # defecto y rompería cualquier texto con tilde.
    if ($Cuerpo) {
        $parametros.Body = [Text.Encoding]::UTF8.GetBytes(($Cuerpo | ConvertTo-Json -Depth 6 -Compress))
    }

    Invoke-RestMethod @parametros
}

Write-Host 'Comprobando que la API responde...'
$salud = Invoke-WebRequest ($Api + '/salud') -UseBasicParsing
if ($salud.StatusCode -ne 200) { throw "La API no responde en $Api" }

$categorias = Invoke-RestMethod ($Api + '/api/categorias')
$idBici = ($categorias | Where-Object { $_.clave -eq 'bici-y-deporte' }).id
$idSenderismo = ($categorias | Where-Object { $_.clave -eq 'senderismo' }).id
$idMusica = ($categorias | Where-Object { $_.clave -eq 'musica' }).id

$sufijo = Get-Date -Format 'yyyyMMddHHmmss'
$claveDemo = 'una frase larga que recuerdo bien'

# --- Organizadora verificada -------------------------------------------------
$correoOrganizadora = "club.bici.$sufijo@ejemplo.es"

Write-Host 'Creando la organizadora...'
Enviar POST '/api/identidad/registro' @{
    correo                = $correoOrganizadora
    contrasena            = $claveDemo
    nombreVisible         = "Club Bici $avila"
    ciudad                = $avila
    anioDeNacimiento      = 1990
    versionNormasAceptada = '2026-08'
} | Out-Null

$sesion = Enviar POST '/api/identidad/sesion' @{ correo = $correoOrganizadora; contrasena = $claveDemo; dispositivo = 'siembra' }
$token = $sesion.tokens.tokenDeAcceso

Write-Host 'Verificando a la organizadora (proveedor simulado)...'
$verificacion = Enviar POST '/api/identidad/verificacion' $null $token
Enviar POST '/api/identidad/verificacion/completar' @{ referenciaExterna = $verificacion.referenciaExterna } $token | Out-Null

# La reclamación "puede organizar" se emite al iniciar sesión, así que hay que
# renovar el token para que el nuevo rol tenga efecto.
$sesion = Enviar POST '/api/identidad/sesion' @{ correo = $correoOrganizadora; contrasena = $claveDemo; dispositivo = 'siembra' }
$token = $sesion.tokens.tokenDeAcceso

# --- Planes ------------------------------------------------------------------
Write-Host 'Publicando planes...'

$planes = @(
    @{
        titulo                    = "Ruta en bici por el $valleAmbles"
        descripcion               = 'Pedaleamos 35 km sin prisa. Si te descuelgas, te esperamos.'
        categoriaId               = $idBici
        inicio                    = (Get-Date).AddDays(5).ToUniversalTime().ToString('o')
        duracionEnMinutos         = 210
        lugar                     = 'Puente Adaja'
        referencia                = 'Junto al quiosco'
        direccionExacta           = 'Av. de Juan Carlos I, 12'
        latitud                   = 40.6565
        longitud                  = -4.7009
        confirmaQueEsLugarPublico = $true
        capacidad                 = 15
        normas                    = @('Casco obligatorio', 'Nivel medio', '+16 a' + [char]0x00F1 + 'os')
    },
    @{
        titulo                    = 'Paseo a Los Cuatro Postes al atardecer'
        descripcion               = 'Paseo tranquilo de hora y media. Apto para todos los niveles.'
        categoriaId               = $idSenderismo
        inicio                    = (Get-Date).AddDays(2).ToUniversalTime().ToString('o')
        duracionEnMinutos         = 90
        lugar                     = 'Los Cuatro Postes'
        referencia                = 'Aparcamiento del mirador'
        direccionExacta           = 'Ctra. de Salamanca, s/n'
        latitud                   = 40.6577
        longitud                  = -4.7132
        confirmaQueEsLugarPublico = $true
        capacidad                 = 20
        normas                    = @('Calzado c' + [char]0x00F3 + 'modo', 'Agua')
    },
    @{
        titulo                    = 'Concierto en la Plaza del Mercado Chico'
        descripcion               = 'Quedamos antes para tomar algo y entramos juntos.'
        categoriaId               = $idMusica
        inicio                    = (Get-Date).AddDays(9).ToUniversalTime().ToString('o')
        duracionEnMinutos         = 180
        lugar                     = 'Plaza del Mercado Chico'
        referencia                = 'Bajo el reloj del ayuntamiento'
        direccionExacta           = 'Plaza del Mercado Chico, 1'
        latitud                   = 40.6560
        longitud                  = -4.7005
        confirmaQueEsLugarPublico = $true
        capacidad                 = 8
        normas                    = @('Puntualidad', 'Entrada por tu cuenta')
    },
    @{
        titulo                    = "Senderismo en $penia"
        descripcion               = 'Ruta de dificultad media con vistas a la sierra. Volvemos a mediodia.'
        categoriaId               = $idSenderismo
        inicio                    = (Get-Date).AddDays(12).ToUniversalTime().ToString('o')
        duracionEnMinutos         = 300
        lugar                     = $penia
        referencia                = 'Junto a la iglesia'
        direccionExacta           = 'Plaza Mayor, 1'
        latitud                   = 40.4636
        longitud                  = -4.6403
        confirmaQueEsLugarPublico = $true
        capacidad                 = 12
        normas                    = @('Nivel medio', 'Comida de picnic')
    }
)

$creados = @()
foreach ($plan in $planes) {
    $resultado = Enviar POST '/api/quedadas' $plan $token
    $creados += $resultado.id
    Write-Host ('  - ' + $plan.titulo)
}

# --- Una persona apuntada ----------------------------------------------------
Write-Host 'Creando una persona asistente y apuntandola al primer plan...'
$correoAsistente = "diego.$sufijo@ejemplo.es"
$claveAsistente = 'otra frase larga y distinta'

Enviar POST '/api/identidad/registro' @{
    correo                = $correoAsistente
    contrasena            = $claveAsistente
    nombreVisible         = 'Diego L.'
    ciudad                = $avila
    anioDeNacimiento      = 2000
    versionNormasAceptada = '2026-08'
} | Out-Null

$sesionAsistente = Enviar POST '/api/identidad/sesion' @{ correo = $correoAsistente; contrasena = $claveAsistente; dispositivo = 'siembra' }
$rutaAsistencia = '/api/quedadas/' + $creados[0] + [char]47 + 'asistencia'
Enviar POST $rutaAsistencia $null $sesionAsistente.tokens.tokenDeAcceso | Out-Null

# --- Resumen -----------------------------------------------------------------
Write-Host ''
Write-Host 'Listo. Cuentas creadas (solo para este entorno local):'
Write-Host ('  Organizadora verificada : ' + $correoOrganizadora)
Write-Host ('  Asistente               : ' + $correoAsistente)
Write-Host ('  Contrasena organizadora : ' + $claveDemo)
Write-Host ('  Contrasena asistente    : ' + $claveAsistente)
Write-Host ''
Write-Host 'Abre http://localhost:3000/explorar para verlo.'
