$ErrorActionPreference = "Stop"

# Thu muc du an: noi dat file start-demo.ps1
$projectRoot = $PSScriptRoot
$webDir = Join-Path $projectRoot "web"
$apiProject = Join-Path $projectRoot "backend\Zoo.Api\Zoo.Api.csproj"
$demoDir = Join-Path $projectRoot "demo-output"

New-Item -ItemType Directory -Force -Path $demoDir | Out-Null

# Kiem tra cong cu
foreach ($command in @("dotnet", "npm.cmd", "cloudflared")) {
    if (-not (Get-Command $command -ErrorAction SilentlyContinue)) {
        throw "Khong tim thay $command. Kiem tra cai dat va mo lai PowerShell."
    }
}

if (-not (Test-Path $apiProject)) {
    throw "Khong tim thay project API: $apiProject"
}

# Tranh chay trung voi cac cua so dang mo
foreach ($port in @(5230, 5173)) {
    $listener = Get-NetTCPConnection `
        -LocalPort $port `
        -State Listen `
        -ErrorAction SilentlyContinue

    if ($listener) {
        throw "Cong $port dang duoc su dung. Tat backend/frontend cu roi chay lai."
    }
}

function Open-DemoWindow([string]$content) {
    $encoded = [Convert]::ToBase64String(
        [Text.Encoding]::Unicode.GetBytes($content)
    )

    Start-Process powershell.exe `
        -ArgumentList "-NoExit", "-EncodedCommand", $encoded | Out-Null
}

function Quote-PS([string]$value) {
    return "'" + $value.Replace("'", "''") + "'"
}

# 1. Bat backend
$rootQuoted = Quote-PS $projectRoot
$apiQuoted = Quote-PS $apiProject

Open-DemoWindow @"
Set-Location $rootQuoted
dotnet run --project $apiQuoted --launch-profile http
"@

Write-Host "Dang cho backend khoi dong..."

$backendReady = $false

for ($i = 0; $i -lt 90; $i++) {
    try {
        $null = Invoke-RestMethod `
            -Uri "http://localhost:5230/api/places" `
            -TimeoutSec 2

        $backendReady = $true
        break
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $backendReady) {
    throw "API chua san sang. Xem loi trong cua so backend."
}

# 2. Bat tunnel va ghi log rieng cho lan chay nay
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
$tunnelLog = Join-Path $demoDir "tunnel-$stamp.log"
$logQuoted = Quote-PS $tunnelLog

Open-DemoWindow @"
cloudflared tunnel --url http://localhost:5173 2>&1 |
    Tee-Object -FilePath $logQuoted
"@

Write-Host "Dang lay duong dan Cloudflare..."

$publicUrl = $null

for ($i = 0; $i -lt 90; $i++) {
    if (Test-Path $tunnelLog) {
        $logText = Get-Content $tunnelLog -Raw

        $match = [regex]::Match(
            $logText,
            "https://[a-z0-9-]+\.trycloudflare\.com"
        )

        if ($match.Success) {
            $publicUrl = $match.Value
            break
        }
    }

    Start-Sleep -Seconds 1
}

if (-not $publicUrl) {
    throw "Chua lay duoc link Cloudflare. Xem cua so tunnel."
}

# 3. Bat frontend, cho phep dung ten mien vua tao
$tunnelHost = ([Uri]$publicUrl).Host
$webQuoted = Quote-PS $webDir
$hostQuoted = Quote-PS $tunnelHost

Open-DemoWindow @"
Set-Location $webQuoted
`$env:VITE_API_BASE = ''
`$env:__VITE_ADDITIONAL_SERVER_ALLOWED_HOSTS = $hostQuoted
npm.cmd run dev -- --port 5173 --strictPort
"@

Write-Host "Dang cho frontend khoi dong..."

$frontendReady = $false

for ($i = 0; $i -lt 60; $i++) {
    try {
        $null = Invoke-WebRequest `
            -Uri "http://localhost:5173" `
            -UseBasicParsing `
            -TimeoutSec 2

        $frontendReady = $true
        break
    }
    catch {
        Start-Sleep -Seconds 1
    }
}

if (-not $frontendReady) {
    throw "Frontend chua san sang. Xem cua so Vite."
}

# 4. Luu duong dan va mo website
$linkFile = Join-Path $demoDir "link.txt"

Set-Content -Path $linkFile -Value $publicUrl -Encoding UTF8

Write-Host ""
Write-Host "LINK TRINH BAY: $publicUrl" -ForegroundColor Green
Write-Host "Link da luu tai: $linkFile"
Write-Host ""
Write-Host "Trong website: mo menu > Ma QR truy cap."
Write-Host "QR se tu lay dung dia chi Cloudflare dang mo."
Write-Host ""
Write-Host "Giu mo 3 cua so backend, frontend va Cloudflare."

Start-Process $publicUrl