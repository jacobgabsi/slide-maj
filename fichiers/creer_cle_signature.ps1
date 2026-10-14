# Cle de signature des mises a jour de Slide (ECDSA P-256, magasin de cles Windows de ce compte, NON exportable).
# - Si la cle "SlideMajSignature" existe deja : affiche seulement sa partie publique.
# - Sinon (premiere fois, ou cle perdue apres une reinstallation de Windows) : la cree et affiche sa partie publique.
# La partie publique (une ligne en base64) se colle dans CleSignatureMaj.vb (constante ClePublique) ; il faut ensuite
# recompiler et recopier une fois le nouveau Slide sur chaque PC (ils n'acceptent que les mises a jour signees par cette cle).
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Core
$nom = 'SlideMajSignature'
if ([System.Security.Cryptography.CngKey]::Exists($nom)) {
    $k = [System.Security.Cryptography.CngKey]::Open($nom)
    Write-Host 'Cle de signature : presente. Partie publique :'
} else {
    $p = New-Object System.Security.Cryptography.CngKeyCreationParameters
    $p.ExportPolicy = [System.Security.Cryptography.CngExportPolicies]::None
    $k = [System.Security.Cryptography.CngKey]::Create([System.Security.Cryptography.CngAlgorithm]::ECDsaP256, $nom, $p)
    Write-Host 'Cle de signature : CREEE. A copier dans CleSignatureMaj.vb (ClePublique) :'
}
Write-Host ([Convert]::ToBase64String($k.Export([System.Security.Cryptography.CngKeyBlobFormat]::EccPublicBlob)))
Read-Host 'Entree pour fermer'
