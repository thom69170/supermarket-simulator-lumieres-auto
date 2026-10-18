# Fabrique les zips dans dist\. Prerequis : "dotnet build -c Release" deja fait.
# BepInEx (IL2CPP, win x64) est telecharge depuis builds.bepinex.dev s'il n'est pas deja dans dist\bepinex.
param([string]$Version = "1.0.0")
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.IO.Compression, System.IO.Compression.FileSystem
$root = $PSScriptRoot
$dist = Join-Path $root "dist"
$dll  = Join-Path $root "bin\Release\net10.0\AutoLights.dll"
if (-not (Test-Path $dll)) { throw "AutoLights.dll introuvable : lance 'dotnet build -c Release' d'abord." }

$bx = Join-Path $dist "bepinex"
if (-not (Test-Path (Join-Path $bx "BepInEx"))) {
    New-Item -ItemType Directory -Force $bx | Out-Null
    $zip = Join-Path $dist "bepinex.zip"
    Invoke-WebRequest "https://builds.bepinex.dev/projects/bepinex_be/789/BepInEx-Unity.IL2CPP-win-x64-6.0.0-be.789%2B6cea286.zip" -OutFile $zip -UseBasicParsing
    Expand-Archive $zip $bx -Force
}

function New-Zip($srcDir, $zipPath) {
    if (Test-Path $zipPath) { Remove-Item $zipPath -Force }
    $z = [System.IO.Compression.ZipFile]::Open($zipPath, 'Create')
    try {
        $base = (Resolve-Path $srcDir).Path.TrimEnd('\')
        Get-ChildItem $srcDir -Recurse -File -Force | ForEach-Object {
            $rel = $_.FullName.Substring($base.Length + 1).Replace('\', '/')
            [void][System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($z, $_.FullName, $rel, 'Optimal')
        }
    } finally { $z.Dispose() }
}

foreach ($kind in "complet", "lite") {
    $stage = Join-Path $dist "stage-$kind"
    if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
    $files = Join-Path $stage "fichiers"
    New-Item -ItemType Directory -Force (Join-Path $files "BepInEx\plugins") | Out-Null
    if ($kind -eq "complet") { Copy-Item (Join-Path $bx "*") $files -Recurse -Force }
    Copy-Item $dll (Join-Path $files "BepInEx\plugins\AutoLights.dll") -Force
    Copy-Item (Join-Path $root "installer\*") $stage -Force
    New-Zip $stage (Join-Path $dist "LumieresAuto-v$Version-$kind.zip")
    Remove-Item $stage -Recurse -Force
}
Get-ChildItem $dist -Filter *.zip | Select-Object Name, Length
