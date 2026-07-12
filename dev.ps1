# Sobe o ambiente de desenvolvimento completo do Goal:
#   Postgres (Docker) + API em 0.0.0.0:5080 + tunel adb para o celular/emulador.
# Uso:  .\dev.ps1        (na raiz do projeto)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot

# 1. Postgres
Write-Host "[1/3] Subindo Postgres (Docker)..." -ForegroundColor Cyan
docker compose -f (Join-Path $root "docker-compose.yml") up -d | Out-Null

# 2. Tunel adb (celular fisico ou emulador -> localhost:5080 do PC)
Write-Host "[2/3] Criando tunel adb reverse tcp:5080..." -ForegroundColor Cyan
$adb = Join-Path $env:LOCALAPPDATA "Android\Sdk\platform-tools\adb.exe"
if (-not (Test-Path $adb)) { $adb = "adb" }
try {
    & $adb reverse tcp:5080 tcp:5080 2>$null
    Write-Host "      Tunel OK (refaca este script se desplugar o cabo)." -ForegroundColor Green
} catch {
    Write-Host "      Nenhum dispositivo adb conectado - tunel pulado." -ForegroundColor Yellow
}

# 3. API (fica no terminal; Ctrl+C para parar)
Write-Host "[3/3] Iniciando a API em http://0.0.0.0:5080 ..." -ForegroundColor Cyan
$env:ASPNETCORE_ENVIRONMENT = "Development"
$env:ASPNETCORE_URLS = "http://0.0.0.0:5080"
dotnet run --project (Join-Path $root "src\Goal.Api") --no-launch-profile
