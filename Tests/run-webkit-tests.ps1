# Runs the browser tests in Linux through Docker, in WebKit unless told otherwise, because Playwright's WebKit for Windows
# accepts writes and keeps nothing. The container also has a small volume of its own for the tests that fill a disk.
param(
    [string]$Image = 'mcr.microsoft.com/dotnet/sdk:10.0',
    [string[]]$Browsers = @('webkit')
)

$repository = Split-Path -Parent $PSScriptRoot
$engines = $Browsers -split ','
$commands = @(
    'mkdir /work && tar -C /repository --exclude=.vs --exclude=.git --exclude=bin --exclude=obj -cf - . | tar -C /work -xf - && cd /work'
    'dotnet build Tests/BlazorBasics.Opfs.BrowserTests -v q --nologo'
    "pwsh Tests/BlazorBasics.Opfs.BrowserTests/bin/Debug/net10.0/playwright.ps1 install --with-deps $($engines -join ' ')"
    'apt-get install -y -qq xvfb dbus-x11'
    "OPFS_BROWSERS=$($engines -join ',') OPFS_DISK_FOLDER=/opfs-disk dotnet test Tests/BlazorBasics.Opfs.BrowserTests --no-build --nologo"
) -join ' && '
docker run --rm --dns 1.1.1.1 --dns 8.8.8.8 --tmpfs /opfs-disk:size=2g -v "${repository}:/repository:ro" $Image bash -c $commands
exit $LASTEXITCODE
