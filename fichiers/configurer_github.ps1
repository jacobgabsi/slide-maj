# Configuration UNIQUE de la publication des mises a jour de Slide sur GitHub (depot public Jacobgabsi/slide-maj).
# A lancer a la main sur le poste de Jacob, dans un terminal PowerShell :
#   powershell -NoProfile -ExecutionPolicy Bypass -File "V:\VBstudio.Net10\Slide\configurer_github.ps1"
# Demande le jeton d'acces GitHub (a coller : rien ne s'affiche) puis le range CHIFFRE pour ce compte Windows dans
# %APPDATA%\Slide\github.cfg - jamais dans le projet (le dossier du projet est copie sur d'autres PC).
$ErrorActionPreference = 'Stop'
$dossier = Join-Path $env:APPDATA 'Slide'
New-Item -ItemType Directory -Force -Path $dossier | Out-Null
$cfg = Join-Path $dossier 'github.cfg'

$depot = Read-Host 'Depot GitHub [Jacobgabsi/slide-maj]'
if ([string]::IsNullOrWhiteSpace($depot)) { $depot = 'Jacobgabsi/slide-maj' }
$jeton = Read-Host 'Jeton GitHub (a coller, rien ne s''affiche)' -AsSecureString
if ($jeton.Length -lt 20) { throw 'Le jeton semble vide ou incomplet : rien n''a ete enregistre.' }

[System.IO.File]::WriteAllLines($cfg, @(
    ('depot=' + $depot.Trim()),
    'branche=main',
    ('jeton=' + (ConvertFrom-SecureString $jeton))))
Write-Host "Configuration enregistree (chiffree pour ce compte Windows) : $cfg"
Read-Host 'Entree pour fermer'
