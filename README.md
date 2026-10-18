# Lumieres Auto - addon pour Supermarket Simulator

Allume automatiquement les lumieres du magasin, a l'ouverture ou a une heure choisie.
Reglages dans le menu pause (Echap) > **Lumieres auto**.

- Jeu : Supermarket Simulator (Steam), teste sur la v1.7.2 (Unity 6, IL2CPP)
- Chargeur de mods : BepInEx 6.0.0-be.789 (IL2CPP, win x64)

## Installer
Telecharger `LumieresAuto-vX.Y.Z-complet.zip` depuis les Releases, tout extraire, double-cliquer `INSTALLER.bat`.
Details dans `installer/LISEZ-MOI.txt`.

## Compiler
Le projet se compile sans telechargement, en s'appuyant sur une copie du jeu ou BepInEx a deja ete lance une fois
(les fichiers d'interface sont generes au premier lancement). Le chemin est dans `AutoLights.csproj` (`TestGame`).

    dotnet build -c Release
    ./package.ps1      # fabrique dist/*.zip

## Code
Tout est dans `src/Plugin.cs` : bouton injecte dans `EscapeMenuManager`, panneau IMGUI, logique d'allumage via
`StoreLightManager`, `DayCycleManager` et `StoreStatus`.
