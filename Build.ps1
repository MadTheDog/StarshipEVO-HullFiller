param(
    [string]$GameDir = 'C:\Program Files (x86)\Steam\steamapps\common\Starship EVO',
    [string]$LoaderDir,
    [string]$DotnetPath = 'dotnet',
    [string]$RestoreSource
)
$ErrorActionPreference = 'Stop'
if (!$LoaderDir) { $LoaderDir = Join-Path $GameDir 'BepInEx\core' }
$root = $PSScriptRoot
$assembly = Join-Path $GameDir 'Starship EVO_Data\Managed\Assembly-CSharp.dll'
foreach ($required in @($assembly,(Join-Path $LoaderDir 'BepInEx.dll'),(Join-Path $LoaderDir '0Harmony.dll'),(Join-Path $LoaderDir 'Mono.Cecil.dll'))) {
    if (!(Test-Path -LiteralPath $required)) { throw "Missing build dependency: $required" }
}
$env:DOTNET_CLI_HOME = Join-Path $root '.build-home'
$env:DOTNET_CLI_TELEMETRY_OPTOUT = '1'
$env:DOTNET_GENERATE_ASPNET_CERTIFICATE = 'false'
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = '1'
$env:NUGET_PACKAGES = Join-Path $root '.packages'
$restoreArgs = @()
if ($RestoreSource) { $restoreArgs = @('--source',$RestoreSource) }
Push-Location $root
try {
    & $DotnetPath restore src/HullFiller.csproj @restoreArgs "-p:GameDir=$GameDir" "-p:LoaderDir=$LoaderDir"
    if ($LASTEXITCODE) { throw 'Plugin restore failed.' }
    & $DotnetPath restore tests/HullFiller.Tests.csproj @restoreArgs "-p:GameDir=$GameDir" "-p:LoaderDir=$LoaderDir"
    if ($LASTEXITCODE) { throw 'Test restore failed.' }
    & $DotnetPath build src/HullFiller.csproj -c Release --no-restore -p:UseSharedCompilation=false "-p:GameDir=$GameDir" "-p:LoaderDir=$LoaderDir"
    if ($LASTEXITCODE) { throw 'Plugin build failed.' }
    & $DotnetPath build tests/HullFiller.Tests.csproj -c Release --no-restore -p:UseSharedCompilation=false "-p:GameDir=$GameDir" "-p:LoaderDir=$LoaderDir"
    if ($LASTEXITCODE) { throw 'Test build failed.' }
    $dll = Join-Path $root 'src\bin\Release\netstandard2.1\HullFiller.dll'
    & $DotnetPath (Join-Path $root 'tests\bin\Release\net8.0\HullFiller.Tests.dll') $assembly $dll
    if ($LASTEXITCODE) { throw 'Geometry, symmetry or native integration checks failed.' }
    $package = Join-Path $root 'dist\HullFiller-v0.1.1'
    $plugins = Join-Path $package 'BepInEx\plugins\HullFiller'
    New-Item -ItemType Directory -Force -Path $plugins | Out-Null
    Copy-Item -LiteralPath $dll -Destination (Join-Path $plugins 'HullFiller.dll')
    Copy-Item -LiteralPath 'README.md','LICENSE' -Destination $package
    $hash = (Get-FileHash -LiteralPath (Join-Path $plugins 'HullFiller.dll')).Hash.ToLowerInvariant()
    [IO.File]::WriteAllText((Join-Path $package 'SHA256SUMS.txt'), "$hash  BepInEx/plugins/HullFiller/HullFiller.dll`n", [Text.UTF8Encoding]::new($false))
    $zip = Join-Path $root 'dist\HullFiller-v0.1.1.zip'
    Compress-Archive -Path (Join-Path $package '*') -DestinationPath $zip -Force
    Write-Output "Package: $zip"
    Write-Output "DLL SHA256: $hash"
} finally { Pop-Location }
