Option Strict On
Option Explicit On

''' <summary>
''' Cle PUBLIQUE de signature des mises a jour (courbe ECDSA P-256, bloc CNG "EccPublicBlob" en base64). Elle n'est pas
''' secrete : elle ne sert qu'a verifier que manifeste.json a bien ete signe par le poste de Jacob (la cle privee reste
''' dans le magasin de cles Windows de son compte, non exportable). Une mise a jour recue du web sans signature valide
''' est refusee, meme si le serveur ou l'adresse sont detournes.
''' Generee le 14/10/2026 (cle "SlideMajSignature" du magasin CNG). Si la cle privee est perdue : en creer une nouvelle
''' (creer_cle_signature.ps1), remplacer cette valeur et recopier une fois le nouveau Slide sur chaque PC.
''' </summary>
Friend Module CleSignatureMaj
    Friend Const ClePublique As String = "RUNTMSAAAABmlc0UuBd6zjvtIkQKZ5oO1xysKeArlXVJ9jgVq97xuaSwtdkxJtSir/03NVDGMspBPhDta4z9U74E2XbtQIrd"
End Module
