# Publication AUTOMATIQUE de Slide pour les mises a jour a distance (demande de Jacob, 14/10/2026 :
# "publier cette version a chaque compilation", puis "j'ai besoin d'une version COMPLETE dans chaque PC").
#
# Appele par Slide.vbproj apres chaque compilation reussie. Publie le dossier du projet COMPLET - sources, bibliotheques,
# lanceur, installateurs, programme compile (bin\Debug\net10.0-windows) - sauf :
#   (data\ est publie sauf 5 fichiers propres a chaque poste, les medias et les copies de la base ; voir Exclu), obj\, .vs\, Publication\, maj\, *.pdb, *.log, *.user, poste_source.txt...
# sous la forme :
#   manifeste.json        : la version et la liste des fichiers (chemin, taille, SHA-256)
#   fichiers\<chemin>     : chaque fichier, tel quel
# Un poste compare ses fichiers a la liste et ne telecharge QUE ceux qui different (comme PennyTop, par le web).
#
# Ne publie que si :
#   - ce poste est un poste "publicateur" : le fichier publier_maj.actif existe dans le dossier du projet (il n'est jamais
#     publie : les autres postes ne publient donc pas) ;
#   - bin a bien la derniere compilation (slide.dll de bin = celui de obj : sinon Slide tournait, exe verrouille) ;
#   - la liste des fichiers a change.
# Ne fait jamais echouer la compilation (code de sortie 0) ; le resultat est dans publier_maj.log.
param(
    [string]$ProjectDir,
    [string]$OutDir,
    [string]$ObjDir,
    [string]$Publication
)
$ErrorActionPreference = 'Stop'
$journal = Join-Path $ProjectDir 'publier_maj.log'
function Note([string]$texte) {
    $ligne = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss') + ' ; ' + $texte
    Add-Content -Path $journal -Value $ligne -Encoding UTF8
    Write-Host ('Mise a jour : ' + $texte)
}

function Empreinte([string]$chemin) {
    $f = [System.IO.File]::Open($chemin, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
    try {
        $sha = [System.Security.Cryptography.SHA256]::Create()
        try { return ([System.BitConverter]::ToString($sha.ComputeHash($f)) -replace '-', '').ToLowerInvariant() } finally { $sha.Dispose() }
    } finally { $f.Dispose() }
}

# Un chemin relatif (avec "/") fait-il partie de la publication ? (meme regle que MiseAJour.Exclu)
function Exclu([string]$rel) {
    $segments = $rel.ToLowerInvariant().Split('/')
    $nom = $segments[$segments.Length - 1]
    for ($i = 0; $i -lt $segments.Length - 1; $i++) {
        $s = $segments[$i]
        if ($s -eq 'obj' -or $s -eq '.vs') { return $true }
        if ($s -eq 'release' -and $i -eq 1 -and $segments[0] -eq 'bin') { return $true }   # copie inutile pour les autres postes
        if ($i -eq 0 -and ($s -eq 'publication' -or $s -eq 'maj' -or $s -eq 'runtime')) { return $true }
    }
    # Dossier data\ du programme (bin\...\data) : publie comme le reste (version identique sur chaque PC), SAUF ce qui est
    # propre a un poste ou personnel (Jacob, 14/10/2026 : "identique sauf slide.accdb, SaveListPhoto.txt,
    # SaveListVideo.txt, SaveComputerName.txt, gmail.txt") ; le depot GitHub est PUBLIC : aucune donnee personnelle,
    # aucun mot de passe, aucun media.
    $dansData = $false
    for ($k = 1; $k -lt $segments.Length - 1; $k++) { if ($segments[$k] -eq 'data' -and $segments[0] -eq 'bin') { $dansData = $true } }
    if ($dansData) {
        for ($j = 2; $j -lt $segments.Length - 1; $j++) { if ($segments[$j] -eq 'settings' -and $segments[$j - 1] -eq 'data') { return $true } }   # data\settings\ : propre au poste
        if ($nom -in @('slide.accdb', 'savelistphoto.txt', 'savelistvideo.txt', 'savecomputername.txt', 'savelistmusic.m3u', 'lieuxenligne.txt')) { return $true }
        if ($nom -in @('niveauxsonores.txt', 'niveauxsonores.sauvegarde.txt', 'soniedesvideos.txt', 'videossansson.txt', 'artiste.png')) { return $true }   # listes personnelles (choix de Jacob, 14/10/2026)
        if ($nom.Contains('.accdb')) { return $true }
        if ($nom.StartsWith('iptv')) { return $true }                      # listes IPTV : contiennent l'utilisateur et le mot de passe du compte                       # copies et sauvegardes de la base
    }
    if ($nom.EndsWith('.pdb') -or $nom.EndsWith('.log') -or $nom.EndsWith('.tmp') -or $nom.EndsWith('.user')) { return $true }
    if ($nom.Contains('.old_') -or $nom -eq 'thumbs.db') { return $true }
    if ($nom -eq 'tailscale_cle.txt' -or $nom -eq 'gmail.txt' -or $nom.StartsWith('.')) { return $true }   # (l'hebergement refuse les fichiers dont le nom commence par un point)
    if ($segments.Length -eq 1 -and ($nom -eq 'poste_source.txt' -or $nom -eq 'publier_maj.actif' -or $nom -eq 'publier_maj.cache')) { return $true }
    return $false
}

# Signature du manifeste : ECDSA P-256 / SHA-256 avec la cle privee "SlideMajSignature" du magasin de cles Windows de
# ce compte (non exportable). La cle publique correspondante est dans CleSignatureMaj.vb.
function Signer([string]$fichierManifeste, [string]$fichierSignature) {
    Add-Type -AssemblyName System.Core
    $nom = 'SlideMajSignature'
    if (-not [System.Security.Cryptography.CngKey]::Exists($nom)) { throw 'cle de signature absente (lancer creer_cle_signature.ps1)' }
    $k = [System.Security.Cryptography.CngKey]::Open($nom)
    $e = New-Object System.Security.Cryptography.ECDsaCng($k)
    $e.HashAlgorithm = [System.Security.Cryptography.CngAlgorithm]::Sha256
    $sig = $e.SignData([System.IO.File]::ReadAllBytes($fichierManifeste))
    [System.IO.File]::WriteAllText($fichierSignature, [Convert]::ToBase64String($sig), (New-Object System.Text.UTF8Encoding($false)))
}
# Envoi vers le web en tache de fond (voir televerser_github.ps1) : la compilation n'attend pas.
function Televerser {
    # Depot GitHub public (voir configurer_github.ps1) : seul canal de mise a jour a distance.
    $script = Join-Path $ProjectDir 'televerser_github.ps1'
    if (-not (Test-Path (Join-Path $env:APPDATA 'Slide\github.cfg'))) { Note 'envoi web non configure (configurer_github.ps1)'; return }
    if (-not (Test-Path $script)) { return }
    Start-Process -FilePath 'powershell.exe' -WindowStyle Hidden -ArgumentList @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', ('"' + $script + '"'), '-ProjectDir', ('"' + $ProjectDir + '"'), '-Publication', ('"' + $Publication + '"'))
}

try {
    # Ce dossier est le projet de CE poste : MiseAJour.EstLaSource le sait par ce fichier (une copie du dossier sur un
    # autre PC porte le nom du poste d'origine, et ce PC se met a jour normalement).
    try { [System.IO.File]::WriteAllText((Join-Path $ProjectDir 'poste_source.txt'), $env:COMPUTERNAME) } catch { }

    if (-not (Test-Path (Join-Path $ProjectDir 'publier_maj.actif'))) {
        Note 'publication non activee sur ce poste (pas de publier_maj.actif)'
        exit 0
    }
    if ([string]::IsNullOrWhiteSpace($Publication)) { $Publication = Join-Path $ProjectDir 'Publication' }
    $racine = [System.IO.Path]::GetFullPath($ProjectDir).TrimEnd('\')
    $bin = [System.IO.Path]::GetFullPath($OutDir).TrimEnd('\')
    $obj = [System.IO.Path]::GetFullPath($ObjDir).TrimEnd('\')

    $dllBin = Join-Path $bin 'slide.dll'
    $dllObj = Join-Path $obj 'slide.dll'
    if (-not (Test-Path $dllBin) -or -not (Test-Path $dllObj)) { Note 'rien publie (slide.dll introuvable)'; exit 0 }
    $shaDll = Empreinte $dllBin
    if ($shaDll -ne (Empreinte $dllObj)) {
        Note 'rien publie : bin n''a pas la derniere compilation (Slide tourne ? fermez-le et regenerez)'
        exit 0
    }
    $versionAuto = Get-Content -Path (Join-Path $ProjectDir 'VersionAuto.vb') -Raw
    if ($versionAuto -notmatch 'SlideVersionAuto As String = "([0-9.]+)"') { Note 'rien publie (version illisible)'; exit 0 }
    $version = $Matches[1]

    # Cache des empreintes (taille + date) : seuls les fichiers modifies sont relus.
    $fichierCache = Join-Path $ProjectDir 'publier_maj.cache'
    $cache = @{}
    if (Test-Path $fichierCache) {
        foreach ($l in [System.IO.File]::ReadAllLines($fichierCache)) {
            $p = $l.Split("`t")
            if ($p.Length -eq 4) { $cache[$p[0]] = $p }
        }
    }

    # Parcours du projet (les dossiers exclus ne sont pas descendus : data\ fait plusieurs Go).
    $liste = New-Object System.Collections.Generic.List[object]
    $pile = New-Object System.Collections.Generic.Stack[string]
    $pile.Push($racine)
    while ($pile.Count -gt 0) {
        $d = $pile.Pop()
        foreach ($sous in [System.IO.Directory]::EnumerateDirectories($d)) {
            $relS = $sous.Substring($racine.Length + 1).Replace('\', '/').ToLowerInvariant()
            $nomS = [System.IO.Path]::GetFileName($sous).ToLowerInvariant()
            if ($nomS -eq 'obj' -or $nomS -eq '.vs') { continue }
            if ($relS -eq 'publication' -or $relS -eq 'maj' -or $relS -eq 'runtime') { continue }
            if ($relS -eq 'bin/release') { continue }
            $pile.Push($sous)
        }
        foreach ($fic in [System.IO.Directory]::EnumerateFiles($d)) {
            $rel = $fic.Substring($racine.Length + 1).Replace('\', '/')
            if (Exclu $rel) { continue }
            $fi = New-Object System.IO.FileInfo($fic)
            if ($fi.Length -gt 90MB) { Note ('ignore (trop gros pour GitHub, > 90 Mo) : ' + $rel); continue }
            $cle = $rel.ToLowerInvariant()
            $e = $cache[$cle]
            if ($e -and $e[1] -eq [string]$fi.Length -and $e[2] -eq [string]$fi.LastWriteTimeUtc.Ticks) {
                $sha = $e[3]
            } else {
                $sha = Empreinte $fic
                $cache[$cle] = @($cle, [string]$fi.Length, [string]$fi.LastWriteTimeUtc.Ticks, $sha)
            }
            $liste.Add([pscustomobject]@{ chemin = $rel; taille = $fi.Length; sha256 = $sha })
        }
    }
    [System.IO.File]::WriteAllLines($fichierCache, @($cache.Values | ForEach-Object { $_ -join "`t" }))
    $liste = @($liste | Sort-Object chemin)

    # Rien de change depuis la derniere publication ?
    $fichierManifeste = Join-Path $Publication 'manifeste.json'
    $ancien = @{}
    if (Test-Path $fichierManifeste) {
        try {
            $m = Get-Content -Path $fichierManifeste -Raw -Encoding UTF8 | ConvertFrom-Json
            foreach ($f in $m.fichiers) { $ancien[$f.chemin] = $f.sha256 }
            if ($m.version -eq $version -and $ancien.Count -eq $liste.Count) {
                $identique = $true
                foreach ($f in $liste) { if ($ancien[$f.chemin] -ne $f.sha256) { $identique = $false; break } }
                if ($identique -and (Test-Path (Join-Path $Publication 'manifeste.sig'))) { Note ('version ' + $version + ' deja publiee (inchangee, ' + $liste.Count + ' fichiers)'); Televerser; exit 0 }
            }
        } catch { $ancien = @{} }
    }

    # Fichiers : copie de ceux qui ont change (ou manquent), retrait de ceux qui n'existent plus.
    $dossierFichiers = Join-Path $Publication 'fichiers'
    New-Item -ItemType Directory -Force -Path $dossierFichiers | Out-Null
    $copies = 0; $octets = 0
    foreach ($f in $liste) {
        $dest = Join-Path $dossierFichiers ($f.chemin.Replace('/', '\'))
        if ((Test-Path $dest) -and $ancien[$f.chemin] -eq $f.sha256 -and (Get-Item $dest).Length -eq $f.taille) { continue }
        New-Item -ItemType Directory -Force -Path ([System.IO.Path]::GetDirectoryName($dest)) | Out-Null
        $src = Join-Path $racine ($f.chemin.Replace('/', '\'))
        $tmp = $dest + '.tmp'
        $in = [System.IO.File]::Open($src, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
        try {
            $out = [System.IO.File]::Create($tmp)
            try { $in.CopyTo($out) } finally { $out.Dispose() }
        } finally { $in.Dispose() }
        Move-Item -Path $tmp -Destination $dest -Force
        $copies++; $octets += $f.taille
    }
    $voulus = New-Object System.Collections.Generic.HashSet[string]
    foreach ($f in $liste) { [void]$voulus.Add($f.chemin.ToLowerInvariant()) }
    $retires = 0
    foreach ($existant in [System.IO.Directory]::EnumerateFiles($dossierFichiers, '*', [System.IO.SearchOption]::AllDirectories)) {
        $rel = $existant.Substring($dossierFichiers.Length + 1).Replace('\', '/')
        if (-not $voulus.Contains($rel.ToLowerInvariant())) { Remove-Item $existant -Force; $retires++ }
    }

    # Le manifeste en dernier : un poste ne voit jamais une liste dont les fichiers ne sont pas tous la.
    $manifeste = [ordered]@{
        version    = $version
        publiee    = (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')
        notes      = ''
        dll_sha256 = $shaDll
        fichiers   = $liste
    } | ConvertTo-Json -Depth 4
    $provisoire = $fichierManifeste + '.tmp'
    [System.IO.File]::WriteAllText($provisoire, $manifeste, (New-Object System.Text.UTF8Encoding($false)))
    Move-Item -Path $provisoire -Destination $fichierManifeste -Force
    Signer $fichierManifeste (Join-Path $Publication 'manifeste.sig')

    Note ('version ' + $version + ' publiee : ' + $liste.Count + ' fichiers, ' + $copies + ' copie(s) (' + [math]::Round($octets / 1MB, 1) + ' Mo), ' + $retires + ' retire(s) (' + $Publication + ')')
    Televerser
} catch {
    Note ('publication impossible : ' + $_.Exception.Message)
}
exit 0
