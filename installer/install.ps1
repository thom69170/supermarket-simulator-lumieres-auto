# Installateur de "Lumieres Auto" pour Supermarket Simulator (Steam, Windows)
param([string]$GameDir = "", [switch]$Uninstall)
$ErrorActionPreference = "Stop"
$GameName = "Supermarket Simulator"
$Here = Split-Path -Parent $MyInvocation.MyCommand.Path
$Files = Join-Path $Here "fichiers"

function Find-GameDir {
    $libs = New-Object System.Collections.Generic.List[string]
    $steam = $null
    foreach ($k in "HKCU:\Software\Valve\Steam", "HKLM:\SOFTWARE\WOW6432Node\Valve\Steam") {
        try { $p = (Get-ItemProperty $k -ErrorAction Stop).SteamPath; if (-not $p) { $p = (Get-ItemProperty $k).InstallPath }; if ($p) { $steam = $p; break } } catch {}
    }
    if ($steam) {
        $libs.Add($steam)
        $vdf = Join-Path $steam "steamapps\libraryfolders.vdf"
        if (Test-Path $vdf) {
            Select-String -Path $vdf -Pattern '"path"\s+"([^"]+)"' | ForEach-Object {
                $libs.Add(($_.Matches[0].Groups[1].Value -replace '\\', '\'))
            }
        }
    }
    foreach ($l in $libs) {
        $c = Join-Path $l "steamapps\common\$GameName"
        if (Test-Path (Join-Path $c "$GameName.exe")) { return $c }
    }
    return $null
}

Write-Host ""
Write-Host "=== Lumieres Auto - $(if ($Uninstall) {'desinstallation'} else {'installation'}) ===" -ForegroundColor Cyan

if (-not $GameDir) { $GameDir = Find-GameDir }
if (-not $GameDir) {
    Write-Host "Je n'ai pas trouve le jeu automatiquement."
    Write-Host "Dans Steam : clic droit sur le jeu > Gerer > Parcourir les fichiers locaux, puis copie le chemin du dossier."
    $GameDir = (Read-Host "Colle ici le chemin du dossier du jeu").Trim('" ')
}
if (-not (Test-Path (Join-Path $GameDir "$GameName.exe"))) {
    Write-Host "Erreur : '$GameName.exe' est introuvable dans : $GameDir" -ForegroundColor Red
    exit 1
}
Write-Host "Jeu trouve : $GameDir"

if (Get-Process -Name $GameName -ErrorAction SilentlyContinue) {
    Write-Host "Erreur : le jeu est ouvert. Ferme-le puis relance l'installateur." -ForegroundColor Red
    exit 1
}

$plugin = Join-Path $GameDir "BepInEx\plugins\AutoLights.dll"

if ($Uninstall) {
    if (Test-Path $plugin) { Remove-Item $plugin -Force; Write-Host "Addon retire." -ForegroundColor Green }
    else { Write-Host "L'addon n'etait pas installe." }
    Write-Host "BepInEx a ete laisse en place (d'autres mods peuvent l'utiliser)."
    Write-Host "Pour le retirer aussi, voir LISEZ-MOI.txt."
    exit 0
}

if (-not (Test-Path (Join-Path $Files "BepInEx\plugins\AutoLights.dll"))) {
    Write-Host "Erreur : le dossier 'fichiers' est introuvable. Decompresse tout le zip avant de lancer l'installateur." -ForegroundColor Red
    exit 1
}

$hasBepInEx = Test-Path (Join-Path $GameDir "BepInEx\core\BepInEx.Core.dll")
if ($hasBepInEx) {
    Write-Host "BepInEx est deja installe : j'ajoute seulement l'addon."
    New-Item -ItemType Directory -Force (Join-Path $GameDir "BepInEx\plugins") | Out-Null
    Copy-Item (Join-Path $Files "BepInEx\plugins\AutoLights.dll") $plugin -Force
}
else {
    Write-Host "Installation de BepInEx et de l'addon..."
    Copy-Item (Join-Path $Files "*") $GameDir -Recurse -Force
}

Write-Host ""
Write-Host "Termine !" -ForegroundColor Green
if (-not $hasBepInEx) { Write-Host "Le tout premier lancement du jeu est long (1 a 3 minutes) : c'est normal, ne le ferme pas." }
Write-Host "Dans le jeu : menu pause (Echap) > bouton 'Lumieres auto'."
