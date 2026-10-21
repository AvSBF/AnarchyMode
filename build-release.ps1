param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$dll = Join-Path $root "bin\Release\net6.0\AnarchyMode.dll"
$pluginDir = Join-Path $root "package\BepInEx\plugins"
$zip = Join-Path $root "AnarchyMode-$Version.zip"

dotnet build (Join-Path $root "AnarchyMode.csproj") -c Release --nologo

New-Item -ItemType Directory -Force -Path $pluginDir | Out-Null
Copy-Item -LiteralPath $dll -Destination (Join-Path $pluginDir "AnarchyMode.dll") -Force

if (Test-Path -LiteralPath $zip) {
    Remove-Item -LiteralPath $zip -Force
}

Compress-Archive -Path (Join-Path $root "package\*") -DestinationPath $zip

Write-Output $zip
