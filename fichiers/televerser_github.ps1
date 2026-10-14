# Envoi du dossier Publication vers le depot GitHub public des mises a jour de Slide (voir configurer_github.ps1).
# Lance par publier_maj.ps1 en tache de fond apres une publication.
#
# Principe : un clone local (%LOCALAPPDATA%\Slide\depot_maj) est mis en miroir sur Publication, puis UN SEUL commit est
# conserve (--amend) et pousse de force : l'historique ne grossit jamais (limite de taille du depot) et git n'envoie
# que les fichiers nouveaux ou modifies. manifeste.json / manifeste.sig sont ceux de Publication (signes).
# Le jeton vient de %APPDATA%\Slide\github.cfg (chiffre pour ce compte Windows) ; il est donne a git par variables
# d'environnement du processus : jamais sur la ligne de commande, jamais dans la configuration du depot, jamais journalise.
param([string]$ProjectDir, [string]$Publication)
$ErrorActionPreference = 'Stop'
$journal = Join-Path $ProjectDir 'publier_maj.log'
function Note([string]$t) { Add-Content -Path $journal -Value ((Get-Date).ToString('yyyy-MM-dd HH:mm:ss') + ' ; envoi GitHub : ' + $t) -Encoding UTF8 }

$mutex = New-Object System.Threading.Mutex($false, 'Local\SlideTeleverserGithub')
if (-not $mutex.WaitOne(0)) { exit 0 }     # un envoi est deja en cours : il enverra aussi la derniere version
try {
    $cfg = Join-Path $env:APPDATA 'Slide\github.cfg'
    if (-not (Test-Path $cfg)) { Note 'non configure (lancer configurer_github.ps1)'; exit 0 }
    $c = @{}
    foreach ($l in [System.IO.File]::ReadAllLines($cfg)) { $i = $l.IndexOf('='); if ($i -gt 0) { $c[$l.Substring(0, $i)] = $l.Substring($i + 1) } }
    $secure = ConvertTo-SecureString $c['jeton']
    $jeton = [System.Runtime.InteropServices.Marshal]::PtrToStringBSTR([System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($secure))
    $branche = if ($c['branche']) { $c['branche'] } else { 'main' }

    $git = $null
    $cmd = Get-Command git.exe -ErrorAction SilentlyContinue
    if ($cmd) { $git = $cmd.Source }
    foreach ($p in @("$env:ProgramFiles\Git\cmd\git.exe", "$env:ProgramFiles\Git\bin\git.exe", "${env:ProgramFiles(x86)}\Git\cmd\git.exe")) { if (-not $git -and (Test-Path $p)) { $git = $p } }
    if (-not $git) { Note 'git introuvable (installer Git pour Windows)'; exit 0 }

    $manifeste = Join-Path $Publication 'manifeste.json'
    if (-not (Test-Path $manifeste) -or -not (Test-Path (Join-Path $Publication 'manifeste.sig'))) { Note 'manifeste ou signature absent'; exit 0 }
    $m = Get-Content -Path $manifeste -Raw -Encoding UTF8 | ConvertFrom-Json

    $travail = Join-Path $env:LOCALAPPDATA 'Slide\depot_maj'
    New-Item -ItemType Directory -Force -Path $travail | Out-Null

    # git ecrit sur stderr meme quand tout va bien : on controle le code de sortie, pas les erreurs PowerShell.
    function Git([string[]]$arguments, [switch]$Tolerant) {
        $ancienne = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
        try {
            $sortie = & $git -C $travail @arguments 2>&1 | Out-String
            $code = $LASTEXITCODE
        } finally { $ErrorActionPreference = $ancienne }
        if ($code -ne 0 -and -not $Tolerant) { throw ('git ' + $arguments[0] + ' : code ' + $code + ' ' + (($sortie -replace '\s+', ' ').Trim())) }
        return [pscustomobject]@{ Code = $code; Sortie = $sortie }
    }

    if (-not (Test-Path (Join-Path $travail '.git'))) {
        [void](Git @('init', '-b', $branche))
        [void](Git @('config', 'user.name', 'Slide publication'))
        [void](Git @('config', 'user.email', 'slide-publication@users.noreply.github.com'))
        [void](Git @('config', 'core.autocrlf', 'false'))      # les octets publies = ceux des empreintes SHA-256
        [void](Git @('config', 'core.safecrlf', 'false'))
        [void](Git @('config', 'gc.auto', '0'))
    }

    # Miroir de Publication (sans .git ni les fichiers de suivi de l'envoi FTP).
    $ancienne = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
    & robocopy.exe $Publication $travail /MIR /XD .git /XF ftp_etat.txt /NFL /NDL /NJH /NJS /NP /R:2 /W:2 | Out-Null
    $rc = $LASTEXITCODE
    $ErrorActionPreference = $ancienne
    if ($rc -ge 8) { throw ('copie vers le depot local : robocopy code ' + $rc) }

    [void](Git @('add', '-A'))
    $etat = Git @('status', '--porcelain')
    $aUnCommit = (Git @('rev-parse', '--verify', 'HEAD') -Tolerant).Code -eq 0
    if ($aUnCommit -and [string]::IsNullOrWhiteSpace($etat.Sortie)) {
        # Rien de nouveau localement : on s'assure seulement que GitHub a bien ce commit (envoi precedent interrompu ?)
        $aPousser = $true
    } else {
        $message = 'Slide ' + $m.version + ' (' + $m.publiee + ')'
        if ($aUnCommit) { [void](Git @('commit', '--amend', '-m', $message)) } else { [void](Git @('commit', '-m', $message)) }
        $aPousser = $true
    }

    if ($aPousser) {
        $entete = 'AUTHORIZATION: basic ' + [Convert]::ToBase64String([System.Text.Encoding]::ASCII.GetBytes('x-access-token:' + $jeton))
        $env:GIT_CONFIG_COUNT = '1'
        $env:GIT_CONFIG_KEY_0 = 'http.https://github.com/.extraheader'
        $env:GIT_CONFIG_VALUE_0 = $entete
        $env:GIT_TERMINAL_PROMPT = '0'
        try {
            $debut = Get-Date
            $r = Git @('push', '--force', ('https://github.com/' + $c['depot'] + '.git'), ('HEAD:refs/heads/' + $branche)) -Tolerant
        } finally {
            $env:GIT_CONFIG_COUNT = $null; $env:GIT_CONFIG_KEY_0 = $null; $env:GIT_CONFIG_VALUE_0 = $null
        }
        $texte = ($r.Sortie -replace '\s+', ' ').Trim().Replace($jeton, '***').Replace($entete, '***')
        if ($r.Code -ne 0) { Note ('echec du push : code ' + $r.Code + ' ' + $texte); exit 0 }
        Note ('version ' + $m.version + ' envoyee (' + [math]::Round(((Get-Date) - $debut).TotalSeconds) + ' s) : ' + $texte)
    }
} catch {
    Note ('echec : ' + $_.Exception.Message)
} finally {
    try { $mutex.ReleaseMutex() } catch { }
}
exit 0
