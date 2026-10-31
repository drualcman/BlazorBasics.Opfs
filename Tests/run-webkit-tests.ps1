# Runs the browser tests in Linux WebKit through Docker. Playwright's WebKit for Windows accepts writes and keeps nothing.
param([string]$Image = 'mcr.microsoft.com/dotnet/sdk:10.0')

$repository = Split-Path -Parent $PSScriptRoot
$commands = @(
    'mkdir /work && tar -C /repository --exclude=.vs --exclude=.git --exclude=bin --exclude=obj -cf - . | tar -C /work -xf - && cd /work'
    'dotnet build Tests/BlazorBasics.Opfs.BrowserTests -v q --nologo'
    'pwsh Tests/BlazorBasics.Opfs.BrowserTests/bin/Debug/net10.0/playwright.ps1 install --with-deps webkit'
    'OPFS_BROWSERS=webkit dotnet test Tests/BlazorBasics.Opfs.BrowserTests --no-build --nologo'
) -join ' && '
docker run --rm --dns 1.1.1.1 --dns 8.8.8.8 -v "${repository}:/repository:ro" $Image bash -c $commands
exit $LASTEXITCODE
