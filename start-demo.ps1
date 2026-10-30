#requires -Version 5.1
<#
Place this file beside the web and backend folders.
Run: powershell -NoProfile -ExecutionPolicy Bypass -File .\start-demo.ps1
Default: real Admin login. Optional: -DemoAdmin (development demonstration only).
Close the old API/Vite/tunnel windows before starting a new run.
#>
[CmdletBinding()]
param(
    [string]$ProjectRoot = $PSScriptRoot,
    [ValidateRange(30, 900)]
    [int]$StartupTimeoutSeconds = 180,
    [switch]$DemoAdmin,
    [switch]$NoBrowser
)

$ErrorActionPreference = 'Stop'

function Quote-PS([string]$Value) {
    return "'" + $Value.Replace("'", "''") + "'"
}

function Open-ServiceWindow([string]$Title, [string]$Body) {
    $titleLiteral = Quote-PS $Title
    $scriptText = @"
`$ErrorActionPreference = 'Stop'
`$Host.UI.RawUI.WindowTitle = $titleLiteral
try {
$Body
    if (`$LASTEXITCODE -ne 0) {
        Write-Host ('Process exited with code: ' + `$LASTEXITCODE) -ForegroundColor Red
    }
} catch {
    Write-Host (`$_.Exception.Message) -ForegroundColor Red
}
"@
    $encoded = [Convert]::ToBase64String(
        [Text.Encoding]::Unicode.GetBytes($scriptText)
    )
    $process = Start-Process -FilePath 'powershell.exe' `
        -ArgumentList @('-NoLogo', '-NoProfile', '-NoExit', '-EncodedCommand', $encoded) `
        -PassThru
    return $process.Id
}

function Wait-Http([string]$Url, [string]$Label) {
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        try {
            $response = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 3
            if ($response.StatusCode -eq 200) { return }
        } catch {
            # The corresponding service window retains its error output.
        }
        Start-Sleep -Milliseconds 750
    }
    throw "$Label did not become ready. Check its service window. URL: $Url"
}

try {
    # Neu khong nhan duoc thu muc tu file script,
# su dung thu muc hien tai cua terminal.
if ([string]::IsNullOrWhiteSpace($ProjectRoot)) {
    $ProjectRoot = (Get-Location).Path
}

$projectPath = (Resolve-Path -LiteralPath $ProjectRoot).Path

Write-Host "Project folder: $projectPath" -ForegroundColor Cyan
    $webDir = Join-Path $projectPath 'web'
    $apiProject = Join-Path $projectPath 'backend\Zoo.Api\Zoo.Api.csproj'
    $outputDir = Join-Path $projectPath 'demo-output'

    if (-not (Test-Path -LiteralPath $apiProject -PathType Leaf)) {
        throw 'Place start-demo.ps1 in the project root beside web and backend, or pass -ProjectRoot.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $webDir 'package.json') -PathType Leaf)) {
        throw 'web/package.json was not found.'
    }
    if (-not (Test-Path -LiteralPath (Join-Path $webDir 'vite.config.ts') -PathType Leaf)) {
        throw 'web/vite.config.ts was not found. This launcher targets the supplied project structure.'
    }

    $dotnetPath = (Get-Command dotnet -CommandType Application -ErrorAction Stop).Source
    $npmPath = (Get-Command npm.cmd -CommandType Application -ErrorAction Stop).Source
    $cloudflarePath = (Get-Command cloudflared -CommandType Application -ErrorAction Stop).Source

    foreach ($port in @(5230, 5173)) {
        $listeners = @(Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
        if ($listeners.Count -gt 0) {
            $owners = ($listeners.OwningProcess | Sort-Object -Unique) -join ', '
            throw "Port $port is already in use (PID: $owners). Close the old API/Vite window, then run again. This script does not kill existing processes."
        }
    }

    New-Item -ItemType Directory -Path $outputDir -Force | Out-Null
    $stamp = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
    $tunnelLog = Join-Path $outputDir "tunnel-$stamp.log"
    $runtimeConfig = Join-Path $outputDir "vite-demo-$stamp.config.mjs"

    $rootQuoted = Quote-PS $projectPath
    $webQuoted = Quote-PS $webDir
    $apiQuoted = Quote-PS $apiProject
    $dotnetQuoted = Quote-PS $dotnetPath
    $npmQuoted = Quote-PS $npmPath
    $cloudflareQuoted = Quote-PS $cloudflarePath
    $logQuoted = Quote-PS $tunnelLog
    $configQuoted = Quote-PS $runtimeConfig

    $demoArgument = ''
    if ($DemoAdmin) {
        $demoArgument = ' -- --demo-admin'
        Write-Warning 'DemoAdmin is ON: authentication tests are not valid in this mode.'
    }

    Write-Host '[1/4] Starting API on port 5230...' -ForegroundColor Cyan
    $apiWindowId = Open-ServiceWindow 'Zoo - Backend API' @"
Set-Location -LiteralPath $rootQuoted
& $dotnetQuoted run --project $apiQuoted --launch-profile http$demoArgument
"@
    Wait-Http 'http://localhost:5230/api/places' 'Backend API'

    Write-Host '[2/4] Creating Cloudflare link...' -ForegroundColor Cyan
    # cloudflared writes its own log. Do not pipe native stderr through Tee-Object.
    # The origin is started in step 3 after the generated hostname is known.
    $tunnelWindowId = Open-ServiceWindow 'Zoo - Cloudflare' @"
Set-Location -LiteralPath $rootQuoted
& $cloudflareQuoted tunnel --url http://127.0.0.1:5173 --logfile $logQuoted
"@

    $publicUrl = $null
    $deadline = (Get-Date).AddSeconds($StartupTimeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Path -LiteralPath $tunnelLog) {
            try {
                $logText = Get-Content -LiteralPath $tunnelLog -Raw -ErrorAction Stop
                $urlMatch = [regex]::Match(
                    [string]$logText,
                    'https://[a-z0-9-]+\.trycloudflare\.com',
                    [Text.RegularExpressions.RegexOptions]::IgnoreCase
                )
                if ($urlMatch.Success) {
                    $publicUrl = $urlMatch.Value.TrimEnd('/')
                    break
                }
            } catch {
                # Retry if cloudflared is writing/locking the log briefly.
            }
        }
        Start-Sleep -Milliseconds 750
    }
    if (-not $publicUrl) {
        throw "No Cloudflare URL was found. Check the Cloudflare window and log: $tunnelLog"
    }

    $tunnelHost = ([Uri]$publicUrl).DnsSafeHost
    $hostJson = ConvertTo-Json -InputObject $tunnelHost -Compress

    # Temporary wrapper: preserves plugins and the original config, while setting
    # the exact hostname and same-origin proxies for THIS run. No tracked config edit.
    $configText = @"
import originalConfig from '../web/vite.config.ts';

export default async function (environment) {
    const config = await (typeof originalConfig === 'function'
        ? originalConfig(environment)
        : originalConfig);
    return {
        ...config,
        server: {
            ...config.server,
            host: '127.0.0.1',
            port: 5173,
            strictPort: true,
            allowedHosts: [$hostJson],
            proxy: {
                ...config.server?.proxy,
                '/api': { target: 'http://localhost:5230', changeOrigin: true },
                '/storage': { target: 'http://localhost:5230', changeOrigin: true },
            },
        },
    };
}
"@
    Set-Content -LiteralPath $runtimeConfig -Value $configText -Encoding UTF8

    Write-Host '[3/4] Starting frontend on port 5173...' -ForegroundColor Cyan
    $webWindowId = Open-ServiceWindow 'Zoo - Frontend Vite' @"
Set-Location -LiteralPath $webQuoted
# '/' is kept as a real environment variable even by Windows PowerShell 5.1.
# services/api.ts removes its trailing slash, giving a same-origin API base.
`$env:VITE_API_BASE = '/'
`$env:__VITE_ADDITIONAL_SERVER_ALLOWED_HOSTS = $(Quote-PS $tunnelHost)
if (-not (Test-Path -LiteralPath 'node_modules\.bin\vite.cmd')) {
    & $npmQuoted ci --no-audit --no-fund
    if (`$LASTEXITCODE -ne 0) { throw 'npm ci failed. Check the output above.' }
}
& $npmQuoted run dev -- --config $configQuoted --host 127.0.0.1 --port 5173 --strictPort
"@
    Wait-Http 'http://127.0.0.1:5173/qr' 'Frontend'
    Wait-Http 'http://127.0.0.1:5173/api/places' 'Frontend to API proxy'

    # Test the generated hostname locally before opening the public link.
    $probe = Invoke-WebRequest -Uri 'http://127.0.0.1:5173/qr' `
        -Headers @{ Host = $tunnelHost } -UseBasicParsing -TimeoutSec 10
    if ($probe.StatusCode -ne 200) {
        throw 'Vite did not accept the generated Cloudflare hostname.'
    }

    $qrUrl = "$publicUrl/qr"
    $visitorUrl = "$publicUrl/?source=qr"
    $adminUrl = "$publicUrl/admin/login"
    $linkFile = Join-Path $outputDir 'link.txt'
    @(
        "Website: $publicUrl",
        "QR page: $qrUrl",
        "Visitor link: $visitorUrl",
        "Admin login: $adminUrl"
    ) | Set-Content -LiteralPath $linkFile -Encoding UTF8

    $runInfo = [ordered]@{
        StartedAt = (Get-Date).ToString('o')
        BackendWindowPid = $apiWindowId
        FrontendWindowPid = $webWindowId
        TunnelWindowPid = $tunnelWindowId
        Website = $publicUrl
        QrPage = $qrUrl
        RuntimeConfig = $runtimeConfig
        TunnelLog = $tunnelLog
        DemoAdmin = [bool]$DemoAdmin
    }
    $runInfo | ConvertTo-Json | Set-Content `
        -LiteralPath (Join-Path $outputDir "run-$stamp.json") -Encoding UTF8

    Write-Host '[4/4] Local services and hostname checks passed.' -ForegroundColor Green
    Write-Host "QR PAGE : $qrUrl" -ForegroundColor Green
    Write-Host "USER    : $visitorUrl"
    Write-Host "ADMIN   : $adminUrl"
    Write-Host "LINKS   : $linkFile"
    Write-Host 'Public DNS/tunnel propagation may take a moment. Refresh if needed.'
    Write-Host 'Keep the Backend, Frontend and Cloudflare windows open.'
    Write-Host 'To stop: press Ctrl+C in each of those three windows.'

    if (-not $NoBrowser) { Start-Process $qrUrl }
} catch {
    Write-Host ''
    Write-Host ('START FAILED: ' + $_.Exception.Message) -ForegroundColor Red
    Write-Host 'Service windows already opened are kept for diagnosis.'
    Write-Host 'Close them before retrying. No existing process was killed.'
    exit 1
}
