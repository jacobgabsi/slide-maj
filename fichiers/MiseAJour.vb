Option Strict On
Option Explicit On

Imports System.IO
Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json

''' <summary>Un fichier de la version publiee (manifeste.json).</summary>
Friend NotInheritable Class FichierMaj
    ''' <summary>Chemin relatif au dossier du projet, avec "/".</summary>
    Friend Chemin As String = ""
    Friend Taille As Long
    Friend Sha256 As String = ""
End Class

''' <summary>Ce que dit le manifeste d'une publication.</summary>
Friend NotInheritable Class InfoVersion
    Friend Version As String = ""
    Friend Publiee As String = ""
    Friend Notes As String = ""
    ''' <summary>"dossier" ou "web".</summary>
    Friend Origine As String = ""
    ''' <summary>Dossier (chemin) ou adresse (URL, terminee par "/") qui contient manifeste.json et fichiers\.</summary>
    Friend Base As String = ""
    Friend Fichiers As New List(Of FichierMaj)
    ''' <summary>Les fichiers qui different de ceux du poste (voir MiseAJour.Comparer) ; Nothing tant que ce n'est pas calcule.</summary>
    Friend AReprendre As List(Of FichierMaj)

    Friend Function OctetsAReprendre() As Long
        If AReprendre Is Nothing Then Return 0
        Return AReprendre.Sum(Function(f) f.Taille)
    End Function
End Class

''' <summary>Resultat d'une verification.</summary>
Friend Enum ResultatMaj
    ALaSource
    AJour
    Disponible
    Preparee
    Injoignable
    Echec
End Enum

''' <summary>Ce que donne une verification.</summary>
Friend NotInheritable Class BilanMaj
    Friend Resultat As ResultatMaj
    Friend Message As String = ""
    Friend Info As InfoVersion
End Class

''' <summary>
''' Mises a jour a distance de Slide (demande de Jacob, 14/10/2026 : "c'est tres long de copier a chaque fois sur les
''' autres PC les nouvelles versions de Slide... comme dans PennyTop, sauf qu'il faut prevoir que toutes les machines ne
''' sont pas toujours dans le meme reseau", puis "j'ai besoin d'une version COMPLETE dans chaque PC").
'''
''' Chaque PC garde une copie COMPLETE du dossier du projet (sources, bibliotheques, lanceur, installateurs, programme
''' compile), dont seul le dossier des donnees (bin\Debug\net10.0-windows\data) lui est propre. A chaque compilation,
''' publier_maj.ps1 publie ce dossier sous la forme d'un manifeste (manifeste.json : la version et, pour chaque fichier,
''' son chemin, sa taille et son SHA-256) et des fichiers eux-memes (fichiers\...).
'''
''' Un poste lit le manifeste dans l'un de ces deux endroits, dans cet ordre :
'''   1. un dossier (reseau local, ou dossier synchronise) : reglage "Maj_Dossier" ;
'''   2. une adresse web (depot public GitHub, signe) : "Maj_AdresseVersion" (adresse de manifeste.json).
''' Il compare ses fichiers au manifeste et ne telecharge QUE ceux qui different (premiere fois : tout ce qui manque).
'''
''' Deroulement :
'''   - au lancement (mode "me la proposer") : la version publiee est lue ; si elle est plus
'''     recente, la fenetre FormProposerMaj la propose (installee d'elle-meme apres quelques secondes sans reponse) ;
'''   - ou en tache de fond (mode "automatique") : les fichiers sont telecharges dans maj\pret, installes au prochain
'''     lancement ou par la relance de nuit ;
'''   - l'installation remplace chaque fichier en renommant l'ancien ("nom.old_xxxxxxxx", permis meme pour un programme en
'''     cours d'execution) ; une erreur defait tout.
'''
''' Le poste qui PUBLIE (celui qui compile : poste_source.txt porte son nom) ne se met jamais a jour.
''' </summary>
Friend Module MiseAJour

    Friend Const NomDuManifeste As String = "manifeste.json"

    ''' <summary>Delai de lecture d'un dossier reseau ou d'une adresse web.</summary>
    Private Const DelaiLectureMs As Integer = 8000

    ''' <summary>Disposition du programme compile par rapport au dossier du projet.</summary>
    Private Const PrefixeProgramme As String = "bin/Debug/net10.0-windows/"

    ''' <summary>Verification en cours (une seule a la fois).</summary>
    Private _enCours As Boolean

    ''' <summary>Version plus recente trouvee mais pas encore preparee ("" : aucune).</summary>
    Friend VersionDisponible As String = ""

    Private _surveillance As System.Windows.Forms.Timer
    Private _verifQuotidienne As System.Windows.Forms.Timer
    Private ReadOnly _heureLancement As Date = Date.Now          ' (verification quotidienne : 5 min apres le lancement)

    ' ------------------------------------------------------------------------------------------
    ' Outils
    ' ------------------------------------------------------------------------------------------

    ''' <summary>
    ''' Le dossier du projet : trois niveaux au-dessus du programme (bin\Debug\net10.0-windows), ou le dossier du
    ''' programme lui-meme s'il n'est pas dans cette disposition.
    ''' </summary>
    Friend Function RacineDuProjet(appPath As String) As String
        Dim p As String = Path.GetFullPath(appPath).TrimEnd("\"c)
        Dim fin As String = "\bin\Debug\net10.0-windows"
        If p.EndsWith(fin, StringComparison.OrdinalIgnoreCase) Then Return p.Substring(0, p.Length - fin.Length)
        Return p
    End Function

    ''' <summary>Chemin d'un fichier du manifeste sur ce poste. Programme sans dossier de projet (copie plate) : les
    ''' fichiers de bin\Debug\net10.0-windows vont directement dans le dossier du programme.</summary>
    Friend Function CheminRelatifLocal(racine As String, appPath As String, chemin As String) As String
        Dim rel As String = chemin.Replace("/"c, "\"c)
        If String.Equals(Path.GetFullPath(racine).TrimEnd("\"c), Path.GetFullPath(appPath).TrimEnd("\"c), StringComparison.OrdinalIgnoreCase) AndAlso
           chemin.StartsWith(PrefixeProgramme, StringComparison.OrdinalIgnoreCase) Then
            rel = chemin.Substring(PrefixeProgramme.Length).Replace("/"c, "\"c)
        End If
        Return rel
    End Function

    ''' <summary>Vrai si ce poste est celui qui PUBLIE (il a compile ce dossier : poste_source.txt porte son nom). Un dossier
    ''' copie d'un autre ordinateur porte le nom de cet autre ordinateur : le poste se met a jour normalement.</summary>
    Friend Function EstLaSource(racine As String) As Boolean
        Try
            Dim marque As String = Path.Combine(racine, "poste_source.txt")
            If Not File.Exists(marque) Then Return False
            Return String.Equals(File.ReadAllText(marque).Trim(), Environment.MachineName, StringComparison.OrdinalIgnoreCase)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Friend Function EstLaSource() As Boolean
        Return EstLaSource(RacineDuProjet(AppPath))
    End Function

    ''' <summary>Compare deux numeros de version ("8.609") : Vrai si <paramref name="candidate"/> est plus recente.</summary>
    Friend Function EstPlusRecente(candidate As String, actuelle As String) As Boolean
        Dim a As Decimal, b As Decimal
        If Decimal.TryParse(candidate, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, a) AndAlso
           Decimal.TryParse(actuelle, Globalization.NumberStyles.Number, Globalization.CultureInfo.InvariantCulture, b) Then
            Return a > b
        End If
        Return String.CompareOrdinal(candidate, actuelle) > 0
    End Function

    Private Function EmpreinteDe(chemin As String) As String
        Using f As New FileStream(chemin, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
            Using sha As SHA256 = SHA256.Create()
                Return Convert.ToHexString(sha.ComputeHash(f)).ToLowerInvariant()
            End Using
        End Using
    End Function

    ''' <summary>Un fichier (chemin relatif) ne part jamais : donnees du poste, dossiers de travail, journaux... (meme regle
    ''' que publier_maj.ps1 ; protege les donnees meme si un manifeste en portait par erreur).</summary>
    ''' <summary>Vrai si le chemin relatif est dans le dossier data\ du programme (bin\...\data\...).</summary>
    Private Function EstDansData(relatif As String) As Boolean
        Dim segments As String() = relatif.Replace("\"c, "/"c).ToLowerInvariant().Split("/"c)
        If segments.Length < 3 OrElse segments(0) <> "bin" Then Return False
        For i As Integer = 1 To segments.Length - 2
            If segments(i) = "data" Then Return True
        Next
        Return False
    End Function

    Private Function Exclu(relatif As String) As Boolean
        Dim segments As String() = relatif.Replace("\"c, "/"c).ToLowerInvariant().Split("/"c)
        Dim nom As String = segments(segments.Length - 1)
        For i As Integer = 0 To segments.Length - 2
            Dim s As String = segments(i)
            If s = "obj" OrElse s = ".vs" Then Return True
            If s = "release" AndAlso i = 1 AndAlso segments(0) = "bin" Then Return True
            If i = 0 AndAlso (s = "publication" OrElse s = "maj" OrElse s = "runtime") Then Return True
        Next
        ' Dossier data\ du programme : synchronise comme le reste, SAUF ce qui est propre a un poste ou personnel (memes
        ' regles que publier_maj.ps1) : slide.accdb et ses copies, SaveListPhoto/SaveListVideo/SaveComputerName.txt,
        ' gmail.txt, les listes personnelles. Les medias sont publies (choix de Jacob, 14/10/2026 : "il n'y a rien de confidentiel"); les fichiers de plus de 90 Mo restent hors du depot.
        Dim dansData As Boolean = False
        For i As Integer = 1 To segments.Length - 2
            If segments(i) = "data" AndAlso segments(0) = "bin" Then dansData = True
        Next
        If dansData Then
            For j As Integer = 2 To segments.Length - 2
                If segments(j) = "settings" AndAlso segments(j - 1) = "data" Then Return True   ' data\settings\ : propre au poste
            Next
            If nom = "slide.accdb" OrElse nom = "savelistphoto.txt" OrElse nom = "savelistvideo.txt" OrElse nom = "savecomputername.txt" OrElse nom = "savelistmusic.m3u" OrElse nom = "lieuxenligne.txt" Then Return True
            If nom = "niveauxsonores.txt" OrElse nom = "niveauxsonores.sauvegarde.txt" OrElse nom = "soniedesvideos.txt" OrElse nom = "videossansson.txt" OrElse nom = "artiste.png" Then Return True
            If nom.Contains(".accdb") Then Return True
            If nom.StartsWith("iptv") Then Return True                ' listes IPTV : utilisateur et mot de passe du compte dans les URLs
        End If
        If nom.EndsWith(".pdb") OrElse nom.EndsWith(".log") OrElse nom.EndsWith(".tmp") OrElse nom.EndsWith(".user") Then Return True
        If nom.Contains(".old_") OrElse nom = "thumbs.db" Then Return True
        If nom = "tailscale_cle.txt" OrElse nom = "gmail.txt" OrElse nom.StartsWith(".") Then Return True   ' secret : ne voyage jamais par les mises a jour
        If segments.Length = 1 AndAlso (nom = "poste_source.txt" OrElse nom = "publier_maj.actif" OrElse nom = "publier_maj.cache") Then Return True
        Return False
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Lecture du manifeste
    ' ------------------------------------------------------------------------------------------

    Private Function Analyser(json As String, origine As String, baseDossier As String) As InfoVersion
        Using doc As JsonDocument = JsonDocument.Parse(json)
            Dim r As JsonElement = doc.RootElement
            If r.ValueKind <> JsonValueKind.Object Then Throw New InvalidDataException("manifeste illisible")
            Dim lire As Func(Of JsonElement, String, String) =
                Function(e As JsonElement, nom As String) As String
                    Dim v As JsonElement = Nothing
                    If e.ValueKind = JsonValueKind.Object AndAlso e.TryGetProperty(nom, v) Then
                        Return If(v.ValueKind = JsonValueKind.String, v.GetString(), v.GetRawText())
                    End If
                    Return ""
                End Function
            Dim info As New InfoVersion With {.Version = lire(r, "version").Trim(), .Publiee = lire(r, "publiee"),
                                              .Notes = lire(r, "notes"), .Origine = origine, .Base = baseDossier}
            If info.Version = "" Then Throw New InvalidDataException("manifeste sans numero de version")
            Dim tableau As JsonElement = Nothing
            If r.TryGetProperty("fichiers", tableau) AndAlso tableau.ValueKind = JsonValueKind.Array Then
                For Each e As JsonElement In tableau.EnumerateArray()
                    Dim f As New FichierMaj With {.Chemin = lire(e, "chemin").Replace("\"c, "/"c).Trim("/"c), .Sha256 = lire(e, "sha256").Trim().ToLowerInvariant()}
                    Long.TryParse(lire(e, "taille"), f.Taille)
                    If f.Chemin <> "" Then info.Fichiers.Add(f)
                Next
            End If
            If info.Fichiers.Count = 0 Then Throw New InvalidDataException("manifeste sans fichiers")
            Return info
        End Using
    End Function

    ''' <summary>Vrai si "signature" (base64, ECDSA P-256 / SHA-256) est celle de "octets" avec la cle publique embarquee.</summary>
    Friend Function SignatureValide(octets As Byte(), signature As String) As Boolean
        Try
            If CleSignatureMaj.ClePublique = "" Then Return False
            Using cle As System.Security.Cryptography.CngKey = System.Security.Cryptography.CngKey.Import(
                    Convert.FromBase64String(CleSignatureMaj.ClePublique), System.Security.Cryptography.CngKeyBlobFormat.EccPublicBlob)
                Using ecdsa As New System.Security.Cryptography.ECDsaCng(cle)
                    Return ecdsa.VerifyData(octets, Convert.FromBase64String(signature.Trim()), System.Security.Cryptography.HashAlgorithmName.SHA256)
                End Using
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

    ''' <summary>La version publiee : dans le dossier d'abord, sur le web ensuite. Nothing (et la raison) si aucun des deux ne
    ''' repond. <paramref name="adresseVersion"/> : adresse de manifeste.json (une ancienne adresse de version.json est
    ''' corrigee).</summary>
    Friend Async Function LireLaVersionPubliee(dossier As String, adresseVersion As String) As Task(Of KeyValuePair(Of InfoVersion, String))
        Dim raisons As New List(Of String)
        dossier = If(dossier, "").Trim()
        adresseVersion = If(adresseVersion, "").Trim()

        If dossier <> "" Then
            Dim lecture As Task(Of InfoVersion) = Task.Run(
                Function() As InfoVersion
                    Dim f As String = Path.Combine(dossier, NomDuManifeste)
                    If Not File.Exists(f) Then Return Nothing
                    Return Analyser(File.ReadAllText(f), "dossier", dossier)
                End Function)
            Dim fini As Task = Await Task.WhenAny(lecture, Task.Delay(DelaiLectureMs))
            If fini Is lecture Then
                Try
                    Dim info As InfoVersion = lecture.GetAwaiter().GetResult()
                    If info IsNot Nothing Then Return New KeyValuePair(Of InfoVersion, String)(info, "")
                    raisons.Add("dossier : pas de " & NomDuManifeste)
                Catch ex As Exception
                    raisons.Add("dossier : " & ex.Message)
                End Try
            Else
                raisons.Add("dossier injoignable")
            End If
        End If

        If adresseVersion <> "" Then
            Try
                If adresseVersion.EndsWith("/version.json", StringComparison.OrdinalIgnoreCase) Then
                    adresseVersion = adresseVersion.Substring(0, adresseVersion.Length - "version.json".Length) & NomDuManifeste
                End If
                If adresseVersion.Contains("100.124.125.55") OrElse adresseVersion.Contains("ajc.fr/slide-maj") Then adresseVersion = "https://raw.githubusercontent.com/Jacobgabsi/slide-maj/main/manifeste.json"   ' ancienne adresse (Tailscale)
                Dim hote As New Uri(adresseVersion)
                ' Un serveur qui ne repond pas ne fait pas attendre : essai de connexion de 2,5 s d'abord.
                Using tcp As New Net.Sockets.TcpClient()
                    Dim connexion As Task = tcp.ConnectAsync(hote.Host, hote.Port)
                    If Not Await Task.WhenAny(connexion, Task.Delay(2500)) Is connexion OrElse connexion.IsFaulted Then
                        Throw New IOException("le serveur ne répond pas (" & hote.Host & ":" & hote.Port & ")")
                    End If
                End Using
                Using client As New HttpClient With {.Timeout = TimeSpan.FromMilliseconds(DelaiLectureMs)}
                    client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Slide-diaporama/" & SlideVersion)
                    ' GitHub (raw) garde ses pages en cache : une branche peut rester a l'ancienne version pendant des
                    ' minutes. On demande donc le dernier commit et on lit tous les fichiers a CE commit (adresse immuable).
                    adresseVersion = Await EpingleSurLeDernierCommit(client, adresseVersion).ConfigureAwait(False)
                    ' Le manifeste web n'est cru que s'il est signe (manifeste.sig, cle publique dans CleSignatureMaj).
                    Dim octets As Byte() = Await client.GetByteArrayAsync(adresseVersion).ConfigureAwait(False)
                    Dim adresseSignature As String = adresseVersion.Substring(0, adresseVersion.LastIndexOf("/"c) + 1) & "manifeste.sig"
                    Dim signature As String = Await client.GetStringAsync(adresseSignature).ConfigureAwait(False)
                    If Not SignatureValide(octets, signature) Then Throw New InvalidDataException("signature du manifeste invalide : mise à jour refusée")
                    Dim json As String = System.Text.Encoding.UTF8.GetString(octets).TrimStart(ChrW(&HFEFF))
                    Dim baseWeb As String = adresseVersion.Substring(0, adresseVersion.LastIndexOf("/"c) + 1)
                    Return New KeyValuePair(Of InfoVersion, String)(Analyser(json, "web", baseWeb), "")
                End Using
            Catch ex As Exception
                raisons.Add("web : " & ex.Message)
            End Try
        End If

        If raisons.Count = 0 Then raisons.Add("aucun dossier ni adresse web de mise à jour n'est indiqué")
        Return New KeyValuePair(Of InfoVersion, String)(Nothing, String.Join(" ; ", raisons))
    End Function


    ''' <summary>
    ''' Adresse raw.githubusercontent.com/proprietaire/depot/branche/... : rend la meme adresse avec le numero du dernier
    ''' commit a la place du nom de branche (jamais en cache, et tous les fichiers viennent du meme commit). Si GitHub
    ''' ne repond pas (ou autre hebergeur) : l'adresse d'origine est rendue.
    ''' </summary>
    Private Async Function EpingleSurLeDernierCommit(client As HttpClient, adresse As String) As Task(Of String)
        Try
            Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(adresse,
                "^https://raw\.githubusercontent\.com/([^/]+)/([^/]+)/([^/]+)/(.+)$", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            If Not m.Success Then Return adresse
            Dim requete As New HttpRequestMessage(HttpMethod.Get, "https://api.github.com/repos/" & m.Groups(1).Value & "/" & m.Groups(2).Value & "/commits/" & m.Groups(3).Value)
            requete.Headers.TryAddWithoutValidation("Accept", "application/vnd.github.sha")
            Using reponse As HttpResponseMessage = Await client.SendAsync(requete).ConfigureAwait(False)
                If Not reponse.IsSuccessStatusCode Then Return adresse
                Dim sha As String = (Await reponse.Content.ReadAsStringAsync().ConfigureAwait(False)).Trim()
                If sha.Length <> 40 OrElse Not sha.All(Function(c) Uri.IsHexDigit(c)) Then Return adresse
                Return "https://raw.githubusercontent.com/" & m.Groups(1).Value & "/" & m.Groups(2).Value & "/" & sha & "/" & m.Groups(4).Value
            End Using
        Catch ex As Exception
            Return adresse
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Comparaison avec les fichiers du poste
    ' ------------------------------------------------------------------------------------------

    Private Function FichierCache(racine As String) As String
        Return Path.Combine(racine, "maj", "empreintes.txt")
    End Function

    ''' <summary>Cache des empreintes du poste (chemin -> taille, date, SHA-256) : seuls les fichiers modifies sont relus.</summary>
    Private Function ChargerLeCache(racine As String) As Dictionary(Of String, String())
        Dim cache As New Dictionary(Of String, String())(StringComparer.OrdinalIgnoreCase)
        Try
            If File.Exists(FichierCache(racine)) Then
                For Each ligne As String In File.ReadAllLines(FichierCache(racine))
                    Dim p As String() = ligne.Split(ControlChars.Tab)
                    If p.Length = 4 Then cache(p(0)) = p
                Next
            End If
        Catch ex As Exception
        End Try
        Return cache
    End Function

    Private Sub GarderLeCache(racine As String, cache As Dictionary(Of String, String()))
        Try
            Directory.CreateDirectory(Path.Combine(racine, "maj"))
            File.WriteAllLines(FichierCache(racine), cache.Values.Select(Function(p) String.Join(ControlChars.Tab, p)))
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>
    ''' Les fichiers du manifeste qui manquent sur ce poste ou en different (taille ou SHA-256). Les fichiers exclus
    ''' (donnees du poste) ne sont jamais proposes.
    ''' </summary>
    ''' <summary>Les fichiers du programme (hors data\ et hors fichiers propres au poste) qui n'existent pas sur ce poste.
    ''' Rapide : une simple verification d'existence.</summary>
    Friend Function FichiersDuProgrammeManquants(info As InfoVersion, racine As String) As List(Of FichierMaj)
        Dim liste As New List(Of FichierMaj)
        Try
            For Each f As FichierMaj In info.Fichiers
                If Exclu(f.Chemin) OrElse EstDansData(f.Chemin) Then Continue For
                Dim local As String = Path.Combine(racine, CheminRelatifLocal(racine, AppPath, f.Chemin))
                If Not File.Exists(local) Then liste.Add(f)
            Next
        Catch ex As Exception
        End Try
        Return liste
    End Function

    Friend Function Comparer(info As InfoVersion, racine As String, appPath As String) As List(Of FichierMaj)
        Dim cache As Dictionary(Of String, String()) = ChargerLeCache(racine)
        Dim differents As New List(Of FichierMaj)
        For Each f As FichierMaj In info.Fichiers
            If Exclu(f.Chemin) Then Continue For
            Dim local As String = Path.Combine(racine, CheminRelatifLocal(racine, appPath, f.Chemin))
            Try
                If File.Exists(local) Then
                    Dim fi As New FileInfo(local)
                    If fi.Length = f.Taille Then
                        Dim e As String() = Nothing
                        Dim sha As String
                        If cache.TryGetValue(local, e) AndAlso e(1) = fi.Length.ToString() AndAlso e(2) = fi.LastWriteTimeUtc.Ticks.ToString() Then
                            sha = e(3)
                        Else
                            sha = EmpreinteDe(local)
                            cache(local) = {local, fi.Length.ToString(), fi.LastWriteTimeUtc.Ticks.ToString(), sha}
                        End If
                        If String.Equals(sha, f.Sha256, StringComparison.OrdinalIgnoreCase) Then Continue For
                    End If
                End If
            Catch ex As Exception
                ' fichier illisible : on le reprend
            End Try
            differents.Add(f)
        Next
        GarderLeCache(racine, cache)
        Return differents
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Preparation (telechargement des fichiers qui different) puis installation
    ' ------------------------------------------------------------------------------------------

    Friend Function DossierPret(racine As String) As String
        Return Path.Combine(racine, "maj", "pret")
    End Function

    Private Function VersionPreparee(racine As String) As String
        Try
            Dim f As String = Path.Combine(DossierPret(racine), "_version.txt")
            If File.Exists(f) Then Return File.ReadAllText(f).Trim()
        Catch ex As Exception
        End Try
        Return ""
    End Function

    ''' <summary>Une version est preparee (tous ses fichiers sont la), plus recente que celle du programme.</summary>
    Friend Function PaquetPret(racine As String, versionActuelle As String) As Boolean
        Dim v As String = VersionPreparee(racine)
        Return v <> "" AndAlso EstPlusRecente(v, versionActuelle)
    End Function

    Friend Function PaquetPret() As Boolean
        Dim racine As String = RacineDuProjet(AppPath)
        Return Not EstLaSource(racine) AndAlso PaquetPret(racine, SlideVersion)
    End Function

    ''' <summary>
    ''' Telecharge (ou copie) les fichiers qui different dans maj\pret, un par un, avec controle de la taille et du SHA-256.
    ''' Un fichier deja la et correct n'est pas retelecharge : une preparation interrompue reprend ou elle en etait.
    ''' </summary>
    Friend Async Function Preparer(info As InfoVersion, racine As String, appPath As String,
                                   progres As IProgress(Of KeyValuePair(Of Long, Long))) As Task
        If info.AReprendre Is Nothing Then info.AReprendre = Comparer(info, racine, appPath)
        Dim pret As String = DossierPret(racine)
        ' Une preparation DEJA COMPLETE d'une autre version : on repart a zero (une interrompue de la meme version reprend).
        Dim deja As String = VersionPreparee(racine)
        If deja <> "" AndAlso deja <> info.Version Then
            Try
                Directory.Delete(pret, True)
            Catch ex As Exception
            End Try
        ElseIf deja <> "" Then
            File.Delete(Path.Combine(pret, "_version.txt"))
        End If
        Directory.CreateDirectory(pret)

        Dim total As Long = info.OctetsAReprendre()
        Dim fait As Long = 0
        Using client As New HttpClient With {.Timeout = TimeSpan.FromMinutes(20)}
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "Slide-diaporama/" & SlideVersion)
            For Each f As FichierMaj In info.AReprendre
                Dim destination As String = Path.Combine(pret, CheminRelatifLocal(racine, appPath, f.Chemin))
                Directory.CreateDirectory(Path.GetDirectoryName(destination))
                Dim provisoire As String = destination & ".part"
                If File.Exists(provisoire) Then File.Delete(provisoire)
                If File.Exists(destination) AndAlso New FileInfo(destination).Length = f.Taille AndAlso
                   String.Equals(EmpreinteDe(destination), f.Sha256, StringComparison.OrdinalIgnoreCase) Then
                    fait += f.Taille
                    If progres IsNot Nothing Then progres.Report(New KeyValuePair(Of Long, Long)(fait, total))
                    Continue For
                End If

                If info.Origine = "dossier" Then
                    Dim source As String = Path.Combine(info.Base, "fichiers", f.Chemin.Replace("/"c, "\"c))
                    Using entree As New FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read)
                        Using sortie As New FileStream(provisoire, FileMode.Create, FileAccess.Write)
                            Dim tampon(1048575) As Byte
                            Do
                                Dim n As Integer = Await entree.ReadAsync(tampon, 0, tampon.Length).ConfigureAwait(False)
                                If n = 0 Then Exit Do
                                Await sortie.WriteAsync(tampon, 0, n).ConfigureAwait(False)
                                fait += n
                                If progres IsNot Nothing Then progres.Report(New KeyValuePair(Of Long, Long)(fait, total))
                            Loop
                        End Using
                    End Using
                Else
                    Dim adresse As String = info.Base & "fichiers/" & String.Join("/", f.Chemin.Split("/"c).Select(Function(s) Uri.EscapeDataString(s)))
                    Using reponse As HttpResponseMessage = Await client.GetAsync(adresse, HttpCompletionOption.ResponseHeadersRead).ConfigureAwait(False)
                        reponse.EnsureSuccessStatusCode()
                        Using flux As Stream = Await reponse.Content.ReadAsStreamAsync().ConfigureAwait(False)
                            Using sortie As New FileStream(provisoire, FileMode.Create, FileAccess.Write)
                                Dim tampon(1048575) As Byte
                                Do
                                    Dim n As Integer = Await flux.ReadAsync(tampon, 0, tampon.Length).ConfigureAwait(False)
                                    If n = 0 Then Exit Do
                                    Await sortie.WriteAsync(tampon, 0, n).ConfigureAwait(False)
                                    fait += n
                                    If progres IsNot Nothing Then progres.Report(New KeyValuePair(Of Long, Long)(fait, total))
                                Loop
                            End Using
                        End Using
                    End Using
                End If

                If New FileInfo(provisoire).Length <> f.Taille Then
                    File.Delete(provisoire)
                    Throw New InvalidDataException(f.Chemin & " : fichier incomplet (taille différente de celle annoncée)")
                End If
                If Not String.Equals(EmpreinteDe(provisoire), f.Sha256, StringComparison.OrdinalIgnoreCase) Then
                    File.Delete(provisoire)
                    Throw New InvalidDataException(f.Chemin & " : fichier corrompu (empreinte SHA-256 différente de celle annoncée)")
                End If
                File.Move(provisoire, destination, True)
            Next
        End Using
        ' La version n'est inscrite qu'a la fin : un dossier a moitie rempli n'est jamais installe.
        ' Reparation (meme version, fichiers manquants) : le paquet est marque un cran au-dessus pour que l'installation l'accepte.
        Dim versionDuPaquet As String = If(EstPlusRecente(info.Version, SlideVersion), info.Version, SlideVersion & "1")
        File.WriteAllText(Path.Combine(pret, "_version.txt"), versionDuPaquet)
    End Function

    ''' <summary>
    ''' Installe la version preparee : chaque fichier existant est renomme "nom.old_xxxxxxxx" (permis meme pour un
    ''' programme en cours d'execution), puis le nouveau prend sa place. Une erreur defait tout. Rend Vrai si installee.
    ''' </summary>
    Friend Function Installer(racine As String, versionActuelle As String, ByRef message As String) As Boolean
        message = ""
        Dim pret As String = DossierPret(racine)
        If Not Directory.Exists(pret) Then Return False
        Dim nouvelle As String = VersionPreparee(racine)
        If nouvelle = "" OrElse Not EstPlusRecente(nouvelle, versionActuelle) Then
            Try
                Directory.Delete(pret, True)                      ' paquet perime ou incomplet : jete
            Catch ex As Exception
            End Try
            Return False
        End If

        Dim remplaces As New List(Of KeyValuePair(Of String, String))    ' (fichier, ancien renomme ou "")
        Dim ignores As New List(Of String)                                ' fichiers de data verrouilles : laisses tels quels
        Try
            ' Le programme d'abord, puis data : un fichier de data verrouille (VLC, outil en cours...) ne doit pas faire
            ' annuler toute l'installation (14/10/2026 : la mise a jour ne remplacait pas slide.dll sur l'autre PC).
            Dim fichiers As List(Of String) = Directory.EnumerateFiles(pret, "*", SearchOption.AllDirectories).
                OrderBy(Function(p) If(EstDansData(Path.GetRelativePath(pret, p)), 1, 0)).ToList()
            For Each f As String In fichiers
                Dim rel As String = Path.GetRelativePath(pret, f)
                If rel.Equals("_version.txt", StringComparison.OrdinalIgnoreCase) Then Continue For
                If Exclu(rel) Then Continue For
                Dim dest As String = Path.Combine(racine, rel)
                Dim ancien As String = ""
                Dim renomme As Boolean = False
                Try
                    Directory.CreateDirectory(Path.GetDirectoryName(dest))
                    If File.Exists(dest) Then
                        ancien = dest & ".old_" & Guid.NewGuid().ToString("N").Substring(0, 8)
                        File.Move(dest, ancien)
                        renomme = True
                    End If
                    File.Move(f, dest)
                    remplaces.Add(New KeyValuePair(Of String, String)(dest, ancien))
                Catch exFichier As Exception
                    If Not EstDansData(rel) Then
                        If renomme Then remplaces.Add(New KeyValuePair(Of String, String)(dest, ancien))
                        Throw                                              ' programme : tout ou rien
                    End If
                    ' data : on remet l'ancien et on continue
                    Try
                        If renomme AndAlso Not File.Exists(dest) Then File.Move(ancien, dest)
                    Catch
                    End Try
                    ignores.Add(rel & " (" & exFichier.Message & ")")
                End Try
            Next
        Catch ex As Exception
            ' Retour en arriere : les nouveaux fichiers partent, les anciens reviennent.
            For i As Integer = remplaces.Count - 1 To 0 Step -1
                Try
                    If File.Exists(remplaces(i).Key) Then File.Delete(remplaces(i).Key)
                    If remplaces(i).Value <> "" Then File.Move(remplaces(i).Value, remplaces(i).Key)
                Catch
                End Try
            Next
            Try
                Directory.Delete(pret, True)                      ' ce qui ne s'installe pas est jete : il sera retelecharge
            Catch
            End Try
            message = "installation annulée (" & ex.Message & ")"
            Return False
        End Try

        Try
            Directory.Delete(pret, True)
        Catch ex As Exception
        End Try
        message = "Slide " & versionActuelle & " -> " & nouvelle & " : " & remplaces.Count & " fichier(s) remplacé(s)"
        If ignores.Count > 0 Then
            message &= ", " & ignores.Count & " fichier(s) de data non remplacé(s) (verrouillés) : " & String.Join(" ; ", ignores.Take(5))
        End If
        ' Marque l'installation : au redemarrage qui suit, Slide ne re-verifie pas tout de suite (15/10/2026).
        Try
            Directory.CreateDirectory(Path.Combine(racine, "maj"))
            File.WriteAllText(Path.Combine(racine, "maj", "installe_le.txt"), Date.Now.ToString("o"))
        Catch ex As Exception
        End Try
        Return True
    End Function

    ''' <summary>Efface les anciens fichiers laisses par une installation (ils etaient verrouilles tant que l'ancien
    ''' programme tournait). Les dossiers de donnees et de travail ne sont pas parcourus.</summary>
    Friend Sub NettoyerLesAnciens(racine As String)
        Try
            Dim pile As New Stack(Of String)
            pile.Push(racine)
            While pile.Count > 0
                Dim d As String = pile.Pop()
                For Each sous As String In Directory.EnumerateDirectories(d)
                    Dim nom As String = Path.GetFileName(sous).ToLowerInvariant()
                    If nom = "data" OrElse nom = "obj" OrElse nom = ".vs" OrElse nom = "publication" OrElse nom = "maj" OrElse nom = "runtime" Then Continue For
                    pile.Push(sous)
                Next
                For Each f As String In Directory.EnumerateFiles(d, "*.old_*")
                    Try
                        File.Delete(f)
                    Catch
                    End Try
                Next
            End While
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Dans le programme
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Verifie la version publiee et, si <paramref name="avecPreparation"/>, prepare les fichiers. Aucune interface.</summary>
    Friend Async Function Verifier(avecPreparation As Boolean, progres As IProgress(Of KeyValuePair(Of Long, Long))) As Task(Of BilanMaj)
        Dim bilan As New BilanMaj
        Dim racine As String = RacineDuProjet(AppPath)
        If EstLaSource(racine) Then
            bilan.Resultat = ResultatMaj.ALaSource
            bilan.Message = "Ce poste est celui qui publie Slide (il l'a compilé) : il ne se met pas à jour."
            Return bilan
        End If
        If _enCours Then
            bilan.Resultat = ResultatMaj.Injoignable
            bilan.Message = "Une vérification est déjà en cours."
            Return bilan
        End If
        _enCours = True
        Try
            If PaquetPret(racine, SlideVersion) Then
                bilan.Resultat = ResultatMaj.Preparee
                bilan.Message = "La version " & VersionPreparee(racine) & " est prête : elle sera installée au prochain lancement."
                Return bilan
            End If
            Dim lecture As KeyValuePair(Of InfoVersion, String) = Await LireLaVersionPubliee(My.Settings.Maj_Dossier, My.Settings.Maj_AdresseVersion)
            Dim info As InfoVersion = lecture.Key
            If info Is Nothing Then
                bilan.Resultat = ResultatMaj.Injoignable
                bilan.Message = "Aucune version publiée n'a pu être lue (" & lecture.Value & ")."
                Return bilan
            End If
            bilan.Info = info
            If Not EstPlusRecente(info.Version, SlideVersion) Then
                VersionDisponible = ""
                bilan.Resultat = ResultatMaj.AJour
                bilan.Message = "Slide est à jour (version " & SlideVersion & ", publiée : " & info.Version & ")."
                Return bilan
            End If
            VersionDisponible = info.Version
            info.AReprendre = Await Task.Run(Function() Comparer(info, racine, AppPath))
            If Not avecPreparation Then
                bilan.Resultat = ResultatMaj.Disponible
                bilan.Message = "La version " & info.Version & " est disponible (vous avez la " & SlideVersion & ") : " & info.AReprendre.Count & " fichier(s)."
                Return bilan
            End If
            Await Preparer(info, racine, AppPath, progres).ConfigureAwait(False)
            bilan.Resultat = ResultatMaj.Preparee
            bilan.Message = "La version " & info.Version & " est prête : elle sera installée au prochain lancement."
            DiagLog("Mise a jour : version " & info.Version & " preparee (" & info.AReprendre.Count & " fichier(s), " & info.Origine & ")")
            Return bilan
        Catch ex As Exception
            bilan.Resultat = ResultatMaj.Echec
            bilan.Message = "Mise à jour impossible : " & ex.Message
            DiagLog("Mise a jour : echec (" & ex.Message & ")")
            Return bilan
        Finally
            _enCours = False
        End Try
    End Function

    ''' <summary>
    ''' Au lancement : s'il y a une version preparee, elle est installee puis le nouveau programme est lance.
    ''' Rend Vrai si Slide doit s'arreter (le nouveau programme est parti).
    ''' </summary>
    Friend Function InstallerAuLancement() As Boolean
        Try
            Dim racine As String = RacineDuProjet(AppPath)
            NettoyerLesAnciens(racine)
            If EstLaSource(racine) Then Return False
            If Not PaquetPret(racine, SlideVersion) Then Return False
            Dim message As String = ""
            If Not Installer(racine, SlideVersion, message) Then
                DiagLog("Mise a jour : " & message)
                Return False
            End If
            DiagLog("Mise a jour : " & message & " -> relance")
            Dim exe As String = IO.Path.Combine(AppPath, "slide.exe")
            Process.Start(New ProcessStartInfo(exe) With {.UseShellExecute = True, .WorkingDirectory = AppPath})
            Return True
        Catch ex As Exception
            DiagLog("Mise a jour : " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>
    ''' Attend une tache SANS figer l'interface (les messages Windows continuent d'etre traites), au plus
    ''' <paramref name="delaiMs"/>. <paramref name="termine"/> dit si elle a fini ; sinon elle continue en tache de fond.
    ''' </summary>
    Private Function AttendreSansBloquer(Of T)(tache As Task(Of T), delaiMs As Integer, ByRef termine As Boolean) As T
        Dim fin As Long = Environment.TickCount64 + delaiMs
        Do While Not tache.IsCompleted AndAlso Environment.TickCount64 < fin
            Application.DoEvents()
            Threading.Thread.Sleep(15)
        Loop
        termine = tache.IsCompleted
        If termine Then Return tache.GetAwaiter().GetResult()
        Return Nothing
    End Function

    ''' <summary>
    ''' Au lancement, mode "me la proposer" (comme PennyTop, 14/10/2026) : la version publiee est lue ;
    ''' si elle est plus recente, la fenetre FormProposerMaj la propose. Une
    ''' reponse "maintenant" (ou aucune reponse apres quelques secondes) telecharge les fichiers qui different,
    ''' les installe et relance Slide. Rend Vrai si Slide doit s'arreter (le nouveau programme est parti).
    ''' </summary>
    Friend Function ProposerAuLancement() As Boolean
        Try
            Dim racine As String = RacineDuProjet(AppPath)
            If EstLaSource(racine) Then
                DiagLog("Mise a jour : ce poste est celui qui publie Slide (poste_source.txt) : aucune mise a jour ici")
                Return False
            End If
            Dim modeMaj As String = If(My.Settings.Maj_Mode, "demander")
            If modeMaj <> "demander" Then
                DiagLog("Mise a jour : mode '" & modeMaj & "' : rien a proposer au lancement")
                Return False
            End If
            Dim dossier As String = My.Settings.Maj_Dossier.Trim()
            Dim web As String = My.Settings.Maj_AdresseVersion.Trim()
            If dossier = "" AndAlso web = "" Then
                DiagLog("Mise a jour : aucun dossier ni adresse web de mise a jour dans les reglages")
                Return False
            End If
            If PaquetPret(racine, SlideVersion) Then Return InstallerAuLancement()


            ' La recherche d'une version publiee (reseau, jusqu'a 30 s) et la comparaison des fichiers (jusqu'a 40 s) ne
            ' bloquent PLUS le demarrage (15/10/2026 : "le lancement de Slide est plus long qu'avant, la musique marche alors
            ' que Form1 est encore affiche") : le diaporama demarre tout de suite, la verification se fait ensuite, en tache de
            ' fond, et la proposition n'apparait que si une version plus recente existe.
            ProposerEnArrierePlan(dossier, web, racine)
            Return False
        Catch ex As Exception
            DiagLog("Mise a jour : " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>Apres le demarrage du diaporama : cherche la version publiee et, si elle est plus recente, la propose.
    ''' "Oui" (ou le bouton) telecharge, installe et relance Slide ; "Plus tard" ou 30 s sans reponse : Slide continue.</summary>
    Private Async Sub ProposerEnArrierePlan(dossier As String, web As String, racine As String)
        Try
            Await Task.Delay(10000)                                  ' le diaporama et la musique d'abord
            ' Slide vient d'etre mis a jour (et relance) : inutile de re-verifier tout de suite (la surveillance reste active).
            Try
                Dim marque As String = Path.Combine(racine, "maj", "installe_le.txt")
                Dim quand As Date
                If File.Exists(marque) AndAlso Date.TryParse(File.ReadAllText(marque).Trim(), quand) AndAlso (Date.Now - quand).TotalMinutes < 30 Then
                    DiagLog("Mise a jour : installee il y a moins de 30 minutes, pas de nouvelle verification au lancement")
                    Exit Sub
                End If
            Catch ex As Exception
            End Try
            Dim chrono As Stopwatch = Stopwatch.StartNew()
            Dim lecture As KeyValuePair(Of InfoVersion, String) = Await Task.Run(Function() LireLaVersionPubliee(dossier, web))
            Dim info As InfoVersion = lecture.Key
            If info Is Nothing Then
                DiagLog("Mise a jour : aucune version publiee lue (" & lecture.Value & ")")
                Exit Sub
            End If
            DiagLog("Mise a jour : version publiee lue en " & chrono.ElapsedMilliseconds & " ms (" & info.Version & ", " & info.Fichiers.Count & " fichiers)")
            Dim manquants As List(Of FichierMaj) = Nothing
            If Not EstPlusRecente(info.Version, SlideVersion) Then
                ' Meme version, mais des fichiers du PROGRAMME manquent (nouveau PC, copie incomplete : 15/10/2026, CoreAudio.dll
                ' absente, Slide s'arretait au demarrage) : ils sont repris, sans toucher a data\.
                manquants = Await Task.Run(Function() FichiersDuProgrammeManquants(info, racine))
                If manquants.Count = 0 Then
                    DiagLog("Mise a jour : Slide est a jour (" & SlideVersion & ", publiee " & info.Version & ")")
                    Exit Sub
                End If
                DiagLog("Mise a jour : " & manquants.Count & " fichier(s) du programme manquant(s) : " & String.Join(", ", manquants.Take(6).Select(Function(x) IO.Path.GetFileName(x.Chemin))))
            End If
            VersionDisponible = info.Version
            chrono.Restart()
            If manquants IsNot Nothing Then
                info.AReprendre = manquants
            Else
                info.AReprendre = Await Task.Run(Function() Comparer(info, racine, AppPath))
            End If
            DiagLog("Mise a jour : comparaison des fichiers en " & chrono.ElapsedMilliseconds & " ms")
            If info.AReprendre.Count = 0 Then
                DiagLog("Mise a jour : version " & info.Version & " publiee mais aucun fichier ne differe : rien a faire")
                Exit Sub
            End If
            DiagLog("Mise a jour : version " & info.Version & " disponible (" & info.Origine & ", " & info.AReprendre.Count & " fichier(s), " &
                    (info.OctetsAReprendre() \ 1048576) & " Mo), proposee")
            ' NON MODALE (15/10/2026 : "sur un PC lent le clic droit prend parfois 30 s au demarrage") : la fenetre modale bloquait
            ' tout Slide, clic droit compris, pendant ses 30 s d'attente. Le diaporama reste utilisable pendant qu'elle est la.
            Dim fenetre As New FormProposerMaj(info)
            fenetre.TopMost = True
            AddHandler fenetre.FormClosed,
                Sub(expediteur As Object, ev As FormClosedEventArgs)
                    Try
                        Dim accepte As Boolean = (fenetre.DialogResult = DialogResult.Yes)
                        fenetre.Dispose()
                        If accepte AndAlso InstallerAuLancement() Then End
                    Catch ex2 As Exception
                        DiagLog("Mise a jour (installation) : " & ex2.Message)
                    End Try
                End Sub
            fenetre.Show()
        Catch ex As Exception
            DiagLog("Mise a jour (arriere-plan) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Installe la version preparee juste avant qu'un nouveau programme soit lance par la relance de nuit.
    ''' Rend Vrai si une version a ete installee.</summary>
    Friend Function InstallerAvantRelance() As Boolean
        Try
            Dim racine As String = RacineDuProjet(AppPath)
            If EstLaSource(racine) OrElse Not PaquetPret(racine, SlideVersion) Then Return False
            Dim message As String = ""
            Dim ok As Boolean = Installer(racine, SlideVersion, message)
            DiagLog("Mise a jour : " & message & If(ok, " (relance de nuit)", ""))
            Return ok
        Catch ex As Exception
            DiagLog("Mise a jour : " & ex.Message)
            Return False
        End Try
    End Function

    ''' <summary>A appeler une fois, depuis le fil de l'interface : verifie 1 minute apres le lancement, puis toutes les 6 heures
    ''' (mode "automatique" : les fichiers sont prepares ; mode "demander" : seulement signales).</summary>
    Friend Sub Surveiller()
        If _surveillance IsNot Nothing OrElse EstLaSource() Then Exit Sub
        _surveillance = New System.Windows.Forms.Timer With {.Interval = 60000}
        AddHandler _surveillance.Tick,
            Async Sub(sender As Object, e As EventArgs)
                _surveillance.Interval = 6 * 3600 * 1000
                Dim mode As String = If(My.Settings.Maj_Mode, "demander")
                If mode = "non" Then Exit Sub
                Dim bilan As BilanMaj = Await Verifier(mode = "auto", Nothing)
                DiagLog("Mise a jour : " & bilan.Message)
            End Sub
        _surveillance.Start()

        ' Verification ET installation a une heure precise, chaque jour (Form4, onglet Divers, 14/10/2026 : "verif et
        ' install auto") : a l'heure choisie (ou, si Slide est lance apres cette heure, 5 minutes apres son lancement), Slide cherche
        ' une version plus recente, la telecharge et se relance pour l'installer en gardant son etat. Une fois par jour.
        _verifQuotidienne = New System.Windows.Forms.Timer With {.Interval = 30000}
        AddHandler _verifQuotidienne.Tick,
            Async Sub(sender As Object, e As EventArgs)
                Try
                    If Not My.Settings.Maj_VerifActive Then Exit Sub
                    Dim heure As Date
                    If Not Date.TryParseExact(My.Settings.Maj_VerifHeure, "HH:mm", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, heure) Then Exit Sub
                    Dim aujourdhui As String = Date.Today.ToString("yyyy-MM-dd")
                    If My.Settings.Maj_VerifDerniere = aujourdhui Then Exit Sub
                    Dim cible As Date = Date.Today.Add(heure.TimeOfDay)
                    If Date.Now < cible Then Exit Sub
                    ' Slide vient de demarrer (PC allume apres l'heure choisie) : on laisse 5 minutes au demarrage avant de
                    ' chercher et d'installer, plutot que d'attendre.
                    If (Date.Now - _heureLancement).TotalMinutes < 5 Then Exit Sub
                    My.Settings.Maj_VerifDerniere = aujourdhui
                    My.Settings.Save()
                    DiagLog("Mise a jour : verification quotidienne de " & heure.ToString("HH:mm"))
                    Dim bilan As BilanMaj = Await Verifier(True, Nothing)
                    DiagLog("Mise a jour : " & bilan.Message)
                    If bilan.Resultat = ResultatMaj.Preparee Then
                        DiagLog("Mise a jour : installation automatique (Slide se relance)")
                        RelanceDeNuit.RelancerMaintenant()
                    End If
                Catch ex As Exception
                    DiagLog("Mise a jour : verification quotidienne impossible (" & ex.Message & ")")
                End Try
            End Sub
        _verifQuotidienne.Start()
    End Sub

End Module
