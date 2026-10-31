# Serves the test app for manual checks on real devices. Open it on this computer at the address it prints, or on a phone
# through the tunnel, because a phone only gets a secure context over HTTPS. Reports arrive in artifacts/device-reports.
param(
    [int]$Port = 5180,
    [switch]$NoTunnel
)

$ErrorActionPreference = 'Stop'
$repository = Split-Path -Parent $PSScriptRoot
$published = Join-Path $repository 'artifacts/device'
$reports = Join-Path $repository 'artifacts/device-reports'
$hostProject = Join-Path $PSScriptRoot 'BlazorBasics.Opfs.DeviceHost'
$hostOutput = Join-Path $repository 'artifacts/device-host'

dotnet publish (Join-Path $PSScriptRoot 'BlazorBasics.Opfs.TestApp') -c Release -o $published -v q --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
dotnet publish $hostProject -c Release -o $hostOutput -v q --nologo
if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

$hostArguments = "`"$(Join-Path $hostOutput 'BlazorBasics.Opfs.DeviceHost.dll')`" `"$(Join-Path $published 'wwwroot')`" `"$reports`" $Port"
$server = Start-Process dotnet -PassThru -NoNewWindow -ArgumentList $hostArguments
try {
    if (-not $NoTunnel -and (Get-Command devtunnel -ErrorAction SilentlyContinue)) {
        Write-Host "Open the 'Connect via browser' address on the device. Anyone with it can reach this page until you press Ctrl+C."
        devtunnel host -p $Port --allow-anonymous
    } else {
        if (-not $NoTunnel) {
            Write-Host "devtunnel is not installed: winget install Microsoft.devtunnel, then devtunnel user login, then run this again."
        }
        Write-Host "The page is at http://127.0.0.1:$Port on this computer. Press Ctrl+C to stop."
        Wait-Process -Id $server.Id
    }
} finally {
    Stop-Process -Id $server.Id -Force -ErrorAction SilentlyContinue
}
