# Dit si Form14.vb / Module1.vb / Module2.vb / Form4.vb ont change depuis le dernier report dans IPTV (copies adaptees, voir IPTV\LISEZMOI_IPTV.md).
# -Valider : memorise l'etat actuel comme reference (apres avoir reporte les changements dans IPTV).
param([switch]$Valider)
$dir = $PSScriptRoot
$ref = Join-Path $dir '..\IPTV\reference_slide.json'
$fichiers = 'Form14.vb','Form14.Designer.vb','Module1.vb','Module2.vb','Form4.vb','MusiqueDeezer.vb'
$actuel = @{}
foreach ($f in $fichiers) { $actuel[$f] = (Get-FileHash (Join-Path $dir $f) -Algorithm SHA256).Hash }
if ($Valider -or -not (Test-Path $ref)) {
  $actuel | ConvertTo-Json | Set-Content $ref -Encoding UTF8
  Write-Host 'Reference enregistree.'; exit 0
}
$ancien = Get-Content $ref -Raw | ConvertFrom-Json
$change = @($fichiers | Where-Object { $ancien.$_ -ne $actuel[$_] })
if ($change.Count -eq 0) { Write-Host 'Rien a reporter dans IPTV.' }
else {
  Write-Host ('A verifier / reporter dans IPTV : ' + ($change -join ', '))
  Write-Host 'Puis : ReportIptv.ps1 -Valider'
}
