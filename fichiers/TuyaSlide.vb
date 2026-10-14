Option Strict On
Option Explicit On

Imports System.Net.Http
Imports System.Security.Cryptography
Imports System.Text
Imports System.Text.Json

''' <summary>
''' Tuya / Smart Life dans Slide (demande de Jacob, 21/10/2026) : piloter des appareils Tuya depuis la minuterie (type de tache "Tuya"),
''' avec les parametres de l'onglet Sources (cadre Tuya). Meme API cloud que le petit programme V:\VBstudio.Net10\Tuya (TuyaClient / TuyaDevice) :
''' jeton par signature HMAC-SHA256, puis commandes sur /v1.0/devices/{id}/commands.
'''
''' Parametres (reglages de l'utilisateur, JAMAIS dans le programme : le depot de Slide est public) :
'''   - Tuya_ClientId, Tuya_Secret (chiffre par Windows, comme le mot de passe Deezer), Tuya_Uid (compte Smart Life lie au projet cloud), Tuya_Region ;
'''   - Tuya_Appareils : les appareils du compte, une ligne "Nom|id|code d'interrupteur|1 si coche" ; chaque appareil coche donne "Nom ON" et "Nom OFF" a la minuterie.
'''     La case Action de la minuterie propose ces noms ; un interrupteur Tuya se pilote par son code (switch, switch_1...) a vrai ou faux.
''' Une commande est envoyee en tache de fond : la minuterie n'attend pas, une erreur reseau est notee dans le journal.
''' </summary>
Friend Module TuyaSlide

    Friend Const TypeMinuterie As String = "Tuya"

    Friend Class Region
        Friend Code As String
        Friend Libelle As String
        Friend Adresse As String
    End Class

    ''' <summary>Les centres de donnees du cloud Tuya (celui du projet cloud de l'utilisateur).</summary>
    Friend ReadOnly Regions As Region() = {
        New Region With {.Code = "eu", .Libelle = "Europe centrale", .Adresse = "https://openapi.tuyaeu.com"},
        New Region With {.Code = "weu", .Libelle = "Europe de l'ouest", .Adresse = "https://openapi-weaz.tuyaeu.com"},
        New Region With {.Code = "us", .Libelle = "Amérique de l'ouest", .Adresse = "https://openapi.tuyaus.com"},
        New Region With {.Code = "ueu", .Libelle = "Amérique de l'est", .Adresse = "https://openapi-ueaz.tuyaus.com"},
        New Region With {.Code = "cn", .Libelle = "Chine", .Adresse = "https://openapi.tuyacn.com"},
        New Region With {.Code = "in", .Libelle = "Inde", .Adresse = "https://openapi.tuyain.com"}}

    Friend Class Appareil
        Friend Id As String
        Friend Nom As String
        Friend Categorie As String
        Friend EnLigne As Boolean
    End Class

    ''' <summary>Une commande de la minuterie : un nom, un appareil, un code de fonction et sa valeur.</summary>
    Friend Class Commande
        Friend Nom As String = ""
        Friend AppareilId As String = ""
        Friend AppareilNom As String = ""
        Friend Code As String = ""
        Friend Valeur As String = ""
        ''' <summary>Compte (Compte.Nom) de l'appareil commande ; "" pour une commande de groupe (chaque membre a le sien).</summary>
        Friend CompteNom As String = ""
        ''' <summary>Le nom sans le prefixe du compte ("Salon ON" pour "Maison : Salon ON") : sert a retrouver une ligne de minuterie quand le nombre de comptes change.</summary>
        Friend NomCourt As String = ""
        ''' <summary>Commande de groupe : les commandes de chacun de ses appareils (Nothing : commande d'un seul appareil).</summary>
        Friend Membres As List(Of Commande)

        ''' <summary>"Nom [id]" : comme la case Appareil de la grille des reglages.</summary>
        Friend ReadOnly Property Appareil As String
            Get
                Return If(AppareilNom = "", AppareilId, AppareilNom & " [" & AppareilId & "]")
            End Get
        End Property
    End Class

    ''' <summary>Les identifiants du compte (reglages enregistres, ou saisie en cours dans les reglages).</summary>
    Friend Class Compte
        ''' <summary>Nom donne par l'utilisateur (unique) : "Maison", "Bureau"... Il repere les appareils de ce compte.</summary>
        Friend Nom As String = ""
        Friend ClientId As String = ""
        Friend Secret As String = ""
        Friend Uid As String = ""
        Friend RegionCode As String = "eu"

        Friend ReadOnly Property Adresse As String
            Get
                For Each r As Region In Regions
                    If String.Equals(r.Code, RegionCode, StringComparison.OrdinalIgnoreCase) Then Return r.Adresse
                Next
                Return Regions(0).Adresse
            End Get
        End Property

        Friend ReadOnly Property Renseigne As Boolean
            Get
                Return ClientId.Trim() <> "" AndAlso Secret.Trim() <> ""
            End Get
        End Property
    End Class

    ''' <summary>Nom du premier compte quand il n'a pas ete choisi (reglages d'avant les comptes multiples).</summary>
    Friend Const NomDuPremierCompte As String = "Compte 1"

    ''' <summary>
    ''' Les comptes enregistres (secret dechiffre). Reglage Tuya_Comptes : une ligne par compte, "Nom|ClientId|secret chiffre|Uid|region".
    ''' Les reglages d'avant (un seul compte : Tuya_ClientId, Tuya_Secret, Tuya_Uid, Tuya_Region) deviennent le compte "Compte 1" tant que la
    ''' liste est vide.
    ''' </summary>
    Friend Function ComptesEnregistres() As List(Of Compte)
        Dim liste As New List(Of Compte)
        For Each ligne As String In If(My.Settings.Tuya_Comptes, "").Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries)
            Dim p As String() = ligne.Split("|"c)
            If p.Length < 4 OrElse p(0).Trim() = "" Then Continue For
            liste.Add(New Compte With {.Nom = p(0).Trim(), .ClientId = p(1).Trim(), .Secret = MusiqueDeezer.Deproteger(p(2).Trim()), .Uid = p(3).Trim(),
                                       .RegionCode = If(p.Length > 4 AndAlso p(4).Trim() <> "", p(4).Trim(), "eu")})
        Next
        If liste.Count = 0 AndAlso If(My.Settings.Tuya_ClientId, "").Trim() <> "" Then
            liste.Add(New Compte With {
                .Nom = NomDuPremierCompte,
                .ClientId = My.Settings.Tuya_ClientId.Trim(),
                .Secret = MusiqueDeezer.Deproteger(If(My.Settings.Tuya_Secret, "")),
                .Uid = If(My.Settings.Tuya_Uid, "").Trim(),
                .RegionCode = If(String.IsNullOrWhiteSpace(My.Settings.Tuya_Region), "eu", My.Settings.Tuya_Region.Trim())})
        End If
        Return liste
    End Function

    Private Function LignesDesComptes(comptes As IEnumerable(Of Compte), chiffrer As Boolean) As String
        Dim lignes As New List(Of String)
        For Each c As Compte In comptes
            If Nettoye(c.Nom) = "" Then Continue For
            lignes.Add(String.Join("|", Nettoye(c.Nom), Nettoye(c.ClientId), If(chiffrer, MusiqueDeezer.Proteger(c.Secret.Trim()), Nettoye(c.Secret)), Nettoye(c.Uid), Nettoye(c.RegionCode)))
        Next
        Return String.Join(vbLf, lignes)
    End Function

    Friend Sub EnregistrerLesComptes(comptes As IEnumerable(Of Compte))
        My.Settings.Tuya_Comptes = LignesDesComptes(comptes, True)
    End Sub

    ''' <summary>Les comptes avec leur secret EN CLAIR : pour l'export des parametres (rechiffres a l'import, voir ImporterLesComptes).</summary>
    Friend Function ExporterLesComptes() As String
        Return LignesDesComptes(ComptesEnregistres(), False)
    End Function

    ''' <summary>Le texte exporte (secrets en clair) devient le reglage Tuya_Comptes (secrets chiffres pour ce PC).</summary>
    Friend Function ImporterLesComptes(texte As String) As String
        Dim comptes As New List(Of Compte)
        For Each ligne As String In If(texte, "").Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries)
            Dim p As String() = ligne.Split("|"c)
            If p.Length < 4 OrElse p(0).Trim() = "" Then Continue For
            comptes.Add(New Compte With {.Nom = p(0).Trim(), .ClientId = p(1).Trim(), .Secret = p(2).Trim(), .Uid = p(3).Trim(), .RegionCode = If(p.Length > 4, p(4).Trim(), "eu")})
        Next
        Return LignesDesComptes(comptes, True)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Appareils coches (reglage Tuya_Appareils) : chacun donne deux commandes a la minuterie, "Nom ON" et "Nom OFF"
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Un appareil du compte, avec le code de son interrupteur (switch, switch_1...) et la case "proposer dans la minuterie".</summary>
    Friend Class AppareilChoisi
        Friend Id As String = ""
        Friend Nom As String = ""
        Friend Code As String = ""
        Friend Coche As Boolean
        ''' <summary>Nom du compte (Compte.Nom) auquel appartient l'appareil.</summary>
        Friend CompteNom As String = ""
        ''' <summary>Etat lu sur l'appareil : "ON", "OFF" ou "" (pas encore lu ; non enregistre).</summary>
        Friend Etat As String = ""
        ''' <summary>L'appareil est en ligne (donnee par Tuya au dernier chargement).</summary>
        Friend EnLigne As Boolean = True
        ''' <summary>Mesure d'un capteur (appareil sans interrupteur) : "18,8 °C - 58 %" ; vide sinon.</summary>
        Friend Mesure As String = ""
    End Class

    Private Function Nettoye(texte As String) As String
        Return If(texte, "").Replace("|", " ").Replace(vbCr, " ").Replace(vbLf, " ").Trim()
    End Function

    ''' <summary>Une ligne par appareil : "Nom|id|code|1 (propose) ou 0".</summary>
    Friend Function SerialiserLesAppareils(appareils As IEnumerable(Of AppareilChoisi)) As String
        Dim lignes As New List(Of String)
        For Each a As AppareilChoisi In appareils
            If Nettoye(a.Id) = "" Then Continue For
            lignes.Add(String.Join("|", Nettoye(a.Nom), Nettoye(a.Id), Nettoye(a.Code), If(a.Coche, "1", "0"), Nettoye(a.CompteNom), Nettoye(a.Etat), If(a.EnLigne, "1", "0"), Nettoye(a.Mesure)))
        Next
        Return String.Join(vbLf, lignes)
    End Function

    ''' <summary>Repare un nom enregistre avec des accents abimes ("GÃ©nÃ©ral" pour "Général" : texte UTF-8 lu en Latin-1 par une version precedente) ; un nom correct reste tel quel.</summary>
    Private Function ReparerLesAccents(texte As String) As String
        Try
            If String.IsNullOrEmpty(texte) OrElse Not texte.Any(Function(c) c = ChrW(&HC3) OrElse c = ChrW(&HC2)) Then Return texte
            If texte.Any(Function(c) AscW(c) > &HFF) Then Return texte
            Dim octets As Byte() = Text.Encoding.Latin1.GetBytes(texte)
            Dim propre As String = New Text.UTF8Encoding(False, True).GetString(octets)
            Return propre
        Catch ex As Exception
            Return texte          ' (pas de l'UTF-8 valide : le nom etait correct)
        End Try
    End Function

    Friend Function AppareilsEnregistres() As List(Of AppareilChoisi)
        Dim liste As New List(Of AppareilChoisi)
        Dim premier As String = Nothing
        For Each ligne As String In If(My.Settings.Tuya_Appareils, "").Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries)
            Dim p As String() = ligne.Split("|"c)
            If p.Length < 3 OrElse p(1).Trim() = "" Then Continue For
            Dim nomDuCompte As String = If(p.Length > 4, p(4).Trim(), "")
            If nomDuCompte = "" Then
                ' Appareil enregistre avant les comptes multiples : il appartient au premier compte.
                If premier Is Nothing Then premier = If(ComptesEnregistres().FirstOrDefault()?.Nom, NomDuPremierCompte)
                nomDuCompte = premier
            End If
            liste.Add(New AppareilChoisi With {.Nom = ReparerLesAccents(p(0).Trim()), .Id = p(1).Trim(), .Code = p(2).Trim(), .Coche = (p.Length > 3 AndAlso p(3).Trim() = "1"), .CompteNom = nomDuCompte,
                                           .Etat = If(p.Length > 5, p(5).Trim(), ""), .EnLigne = Not (p.Length > 6 AndAlso p(6).Trim() = "0"), .Mesure = If(p.Length > 7, p(7).Trim(), "")})
        Next
        Return liste
    End Function

    ''' <summary>Un groupe d'appareils : "Groupe ON" / "Groupe OFF" dans la minuterie agissent sur tous ses appareils.</summary>
    Friend Class GroupeTuya
        Friend Nom As String = ""
        Friend Ids As New List(Of String)
    End Class

    ''' <summary>Une ligne par groupe : "Nom|id1,id2,...".</summary>
    Friend Function SerialiserLesGroupes(groupes As IEnumerable(Of GroupeTuya)) As String
        Dim lignes As New List(Of String)
        For Each g As GroupeTuya In groupes
            If Nettoye(g.Nom) = "" Then Continue For
            lignes.Add(Nettoye(g.Nom) & "|" & String.Join(",", g.Ids.Select(Function(i) Nettoye(i)).Where(Function(i) i <> "")))
        Next
        Return String.Join(vbLf, lignes)
    End Function

    Friend Function GroupesEnregistres() As List(Of GroupeTuya)
        Dim liste As New List(Of GroupeTuya)
        For Each ligne As String In If(My.Settings.Tuya_Groupes, "").Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries)
            Dim p As String() = ligne.Split("|"c)
            If p.Length < 1 OrElse p(0).Trim() = "" Then Continue For
            Dim g As New GroupeTuya With {.Nom = p(0).Trim()}
            If p.Length > 1 Then g.Ids.AddRange(p(1).Split(","c).Select(Function(i) i.Trim()).Where(Function(i) i <> ""))
            liste.Add(g)
        Next
        Return liste
    End Function

    ''' <summary>"Nom ON" (code = vrai) et "Nom OFF" (code = faux) pour chaque appareil coche qui a un interrupteur ; idem pour chaque groupe (tous ses appareils qui ont un interrupteur).</summary>
    ''' <param name="prefixer">Plusieurs comptes : chaque nom d'appareil commence par celui de son compte ("Maison : Salon ON").</param>
    Friend Function CommandesDe(appareils As IEnumerable(Of AppareilChoisi), Optional groupes As IEnumerable(Of GroupeTuya) = Nothing, Optional prefixer As Boolean = False) As List(Of Commande)
        Dim liste As New List(Of Commande)
        For Each a As AppareilChoisi In appareils
            If Not a.Coche OrElse a.Code = "" OrElse a.Id = "" Then Continue For
            Dim prefixe As String = If(prefixer AndAlso a.CompteNom <> "", a.CompteNom & " : ", "")
            liste.Add(New Commande With {.Nom = prefixe & a.Nom & " ON", .NomCourt = a.Nom & " ON", .AppareilId = a.Id, .AppareilNom = a.Nom, .CompteNom = a.CompteNom, .Code = a.Code, .Valeur = "true"})
            liste.Add(New Commande With {.Nom = prefixe & a.Nom & " OFF", .NomCourt = a.Nom & " OFF", .AppareilId = a.Id, .AppareilNom = a.Nom, .CompteNom = a.CompteNom, .Code = a.Code, .Valeur = "false"})
        Next
        If groupes IsNot Nothing Then
            For Each g As GroupeTuya In groupes
                Dim membres As List(Of AppareilChoisi) = appareils.Where(Function(a) a.Code <> "" AndAlso g.Ids.Contains(a.Id)).ToList()
                If membres.Count = 0 Then Continue For
                For Each valeur As String In {"true", "false"}
                    Dim c As New Commande With {.Nom = "Groupe " & g.Nom & If(valeur = "true", " ON", " OFF"), .Valeur = valeur, .Membres = New List(Of Commande)}
                    For Each a As AppareilChoisi In membres
                        c.Membres.Add(New Commande With {.Nom = a.Nom, .AppareilId = a.Id, .AppareilNom = a.Nom, .CompteNom = a.CompteNom, .Code = a.Code, .Valeur = valeur})
                    Next
                    liste.Add(c)
                Next
            Next
        End If
        Return liste
    End Function

    ''' <summary>Quand les reglages sont ouverts (Form4), les cases et les champs en cours de saisie : la minuterie voit les commandes avant le OK.</summary>
    Friend FournisseurDeCommandes As Func(Of List(Of Commande))
    Friend FournisseurDeComptes As Func(Of List(Of Compte))
    Friend FournisseurActif As Func(Of Boolean)

    ''' <summary>La case "Utiliser Tuya" (celle des reglages en cours de saisie s'ils sont ouverts, sinon l'enregistree).</summary>
    Friend Function Actif() As Boolean
        Dim f As Func(Of Boolean) = FournisseurActif
        Return If(f Is Nothing, My.Settings.Tuya_Actif, f())
    End Function

    ''' <summary>Les comptes en cours de saisie dans les reglages (s'ils sont ouverts), sinon les comptes enregistres.</summary>
    Friend Function ComptesActuels() As List(Of Compte)
        Dim f As Func(Of List(Of Compte)) = FournisseurDeComptes
        Return If(f Is Nothing, ComptesEnregistres(), f())
    End Function

    Friend Function Commandes() As List(Of Commande)
        Dim f As Func(Of List(Of Commande)) = FournisseurDeCommandes
        If f IsNot Nothing Then Return f()
        Return CommandesDe(AppareilsEnregistres(), GroupesEnregistres(), False)          ' (le compte de la ligne est dans son argument : "!/Compte:Nom")
    End Function

    ''' <summary>Les noms des commandes, pour la case Action de la minuterie (type "Tuya").</summary>
    ''' <param name="compteNom">Les commandes des appareils de ce compte, et celles des groupes (valables pour tous les comptes) ; Nothing ou "" : toutes.</param>
    Friend Function NomsDesCommandes(Optional compteNom As String = Nothing) As List(Of String)
        Dim toutes As IEnumerable(Of Commande) = Commandes()
        If Not String.IsNullOrWhiteSpace(compteNom) Then toutes = toutes.Where(Function(c) c.Membres IsNot Nothing OrElse String.Equals(c.CompteNom, compteNom, StringComparison.OrdinalIgnoreCase))
        Return toutes.Select(Function(c) c.Nom).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Appels a l'API
    ' ------------------------------------------------------------------------------------------

    Private ReadOnly _http As New HttpClient With {.Timeout = TimeSpan.FromSeconds(20)}
    ''' <summary>Un jeton d'acces par compte (cle : centre de donnees, identifiant client, empreinte du secret) et sa fin de validite.</summary>
    Private ReadOnly _jetons As New Dictionary(Of String, KeyValuePair(Of String, Date))
    Private ReadOnly _verrou As New Object()

    Private Function CleDuJeton(c As Compte) As String
        Return c.Adresse & "|" & c.ClientId & "|" & Sha256Hex(c.Secret)
    End Function

    Private Function Sha256Hex(donnees As String) As String
        Return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(donnees))).ToLowerInvariant()
    End Function

    Private Function Signer(texte As String, secret As String) As String
        Using hmac As New HMACSHA256(Encoding.UTF8.GetBytes(secret))
            Return Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(texte)))
        End Using
    End Function

    Private Function Reponse(json As String) As JsonElement
        Using doc As JsonDocument = JsonDocument.Parse(json)
            Dim racine As JsonElement = doc.RootElement
            Dim ok As JsonElement
            If Not racine.TryGetProperty("success", ok) OrElse ok.ValueKind <> JsonValueKind.True Then
                Dim msg As JsonElement
                Dim code As JsonElement
                Dim texte As String = If(racine.TryGetProperty("msg", msg), msg.ToString(), json)
                Dim numero As String = If(racine.TryGetProperty("code", code), code.ToString(), "")
                Throw New TuyaException(texte, numero)
            End If
            Dim resultat As JsonElement
            If racine.TryGetProperty("result", resultat) Then Return resultat.Clone()
            Return racine.Clone()
        End Using
    End Function

    Friend Class TuyaException
        Inherits Exception
        Friend ReadOnly Property Numero As String
        Friend Sub New(message As String, numero As String)
            MyBase.New(message)
            Me.Numero = numero
        End Sub
    End Class

    Private Async Function JetonAsync(c As Compte) As Task(Of String)
        Dim cle As String = CleDuJeton(c)
        SyncLock _verrou
            Dim connu As KeyValuePair(Of String, Date)
            If _jetons.TryGetValue(cle, connu) AndAlso Date.UtcNow < connu.Value Then Return connu.Key
        End SyncLock
        Dim t As String = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
        Dim aSigner As String = c.ClientId & t & "GET" & vbLf & Sha256Hex("") & vbLf & "" & vbLf & "/v1.0/token?grant_type=1"
        Using demande As New HttpRequestMessage(HttpMethod.Get, c.Adresse & "/v1.0/token?grant_type=1")
            demande.Headers.Add("client_id", c.ClientId)
            demande.Headers.Add("sign", Signer(aSigner, c.Secret))
            demande.Headers.Add("t", t)
            demande.Headers.Add("sign_method", "HMAC-SHA256")
            Using reponseHttp As HttpResponseMessage = Await _http.SendAsync(demande).ConfigureAwait(False)
                Dim json As String = Encoding.UTF8.GetString(Await reponseHttp.Content.ReadAsByteArrayAsync().ConfigureAwait(False))          ' (Tuya ne donne pas le jeu de caracteres : sans cela, les accents sont lus en Latin-1)
                Dim resultat As JsonElement = Reponse(json)
                Dim jeton As String = resultat.GetProperty("access_token").GetString()
                Dim duree As Integer = resultat.GetProperty("expire_time").GetInt32()
                SyncLock _verrou
                    _jetons(cle) = New KeyValuePair(Of String, Date)(jeton, Date.UtcNow.AddSeconds(Math.Max(60, duree - 60)))
                End SyncLock
                Return jeton
            End Using
        End Using
    End Function

    Private Async Function AppelAsync(c As Compte, methode As HttpMethod, chemin As String, corps As String) As Task(Of JsonElement)
        For essai As Integer = 1 To 2
            Dim jeton As String = Await JetonAsync(c).ConfigureAwait(False)
            Dim t As String = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString()
            Dim aSigner As String = c.ClientId & jeton & t & methode.Method.ToUpperInvariant() & vbLf & Sha256Hex(corps) & vbLf & "" & vbLf & chemin
            Using demande As New HttpRequestMessage(methode, c.Adresse & chemin)
                demande.Headers.Add("client_id", c.ClientId)
                demande.Headers.Add("access_token", jeton)
                demande.Headers.Add("sign", Signer(aSigner, c.Secret))
                demande.Headers.Add("t", t)
                demande.Headers.Add("sign_method", "HMAC-SHA256")
                If corps <> "" Then demande.Content = New StringContent(corps, Encoding.UTF8, "application/json")
                Using reponseHttp As HttpResponseMessage = Await _http.SendAsync(demande).ConfigureAwait(False)
                    Dim json As String = Encoding.UTF8.GetString(Await reponseHttp.Content.ReadAsByteArrayAsync().ConfigureAwait(False))          ' (Tuya ne donne pas le jeu de caracteres : sans cela, les accents sont lus en Latin-1)
                    Try
                        Return Reponse(json)
                    Catch ex As TuyaException When essai = 1 AndAlso (ex.Numero = "1010" OrElse ex.Numero = "1011")
                        SyncLock _verrou
                            _jetons.Remove(CleDuJeton(c))              ' jeton refuse : un nouveau, une seule fois
                        End SyncLock
                    End Try
                End Using
            End Using
        Next
        Throw New TuyaException("jeton refusé", "1010")
    End Function

    ''' <summary>Les appareils du compte Smart Life (identifiant du compte "Uid" requis).</summary>
    Friend Async Function ListerLesAppareilsAsync(c As Compte) As Task(Of List(Of Appareil))
        If Not c.Renseigne Then Throw New TuyaException("Renseignez l'identifiant client et la clé secrète.", "")
        If c.Uid.Trim() = "" Then Throw New TuyaException("Renseignez l'identifiant du compte (UID).", "")
        Dim resultat As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/users/" & c.Uid.Trim() & "/devices", "").ConfigureAwait(False)
        Dim liste As New List(Of Appareil)
        If resultat.ValueKind = JsonValueKind.Array Then
            For Each e As JsonElement In resultat.EnumerateArray()
                Dim a As New Appareil With {.Id = Texte(e, "id"), .Nom = Texte(e, "name"), .Categorie = Texte(e, "category")}
                Dim enLigne As JsonElement
                a.EnLigne = e.TryGetProperty("online", enLigne) AndAlso enLigne.ValueKind = JsonValueKind.True
                liste.Add(a)
            Next
        End If
        Return liste
    End Function

    Private Function Texte(e As JsonElement, nom As String) As String
        Dim v As JsonElement
        If e.TryGetProperty(nom, v) AndAlso v.ValueKind = JsonValueKind.String Then Return If(v.GetString(), "")
        Return ""
    End Function

    ''' <summary>Le code de l'interrupteur d'un appareil (switch, switch_1...), d'apres son etat ; "" s'il n'en a pas.</summary>
    Friend Async Function CodeDeLInterrupteurAsync(c As Compte, appareilId As String) As Task(Of String)
        Dim resultat As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/devices/" & appareilId & "/status", "").ConfigureAwait(False)
        If resultat.ValueKind = JsonValueKind.Array Then
            For Each e As JsonElement In resultat.EnumerateArray()
                Dim code As String = Texte(e, "code")
                Dim v As JsonElement
                If code.StartsWith("switch", StringComparison.OrdinalIgnoreCase) AndAlso Not code.Contains("led") AndAlso
                   e.TryGetProperty("value", v) AndAlso (v.ValueKind = JsonValueKind.True OrElse v.ValueKind = JsonValueKind.False) Then Return code
            Next
        End If
        Return ""
    End Function

    ''' <summary>
    ''' L'interrupteur d'un appareil : son code et son etat actuel ("ON" ou "OFF") ; code "" s'il n'en a pas (capteur, passerelle, camera, telecommande...).
    ''' Un appareil peut avoir plusieurs codes "switch*" booleens : on prend dans l'ordre switch, switch_1, switch_led (lampes, rubans), switch_led_1, puis le premier autre
    ''' sauf "switch_backlight" (le voyant de l'interrupteur, pas la charge : verifie sur un variateur 3 touches qui en avait les deux).
    ''' </summary>
    Friend Async Function InterrupteurAsync(c As Compte, appareilId As String) As Task(Of KeyValuePair(Of String, String))
        Dim resultat As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/devices/" & appareilId & "/status", "").ConfigureAwait(False)
        Dim candidats As New List(Of KeyValuePair(Of String, Boolean))
        If resultat.ValueKind = JsonValueKind.Array Then
            For Each e As JsonElement In resultat.EnumerateArray()
                Dim code As String = Texte(e, "code")
                Dim v As JsonElement
                If code.StartsWith("switch", StringComparison.OrdinalIgnoreCase) AndAlso e.TryGetProperty("value", v) AndAlso
                   (v.ValueKind = JsonValueKind.True OrElse v.ValueKind = JsonValueKind.False) Then
                    candidats.Add(New KeyValuePair(Of String, Boolean)(code, v.ValueKind = JsonValueKind.True))
                End If
            Next
        End If
        For Each prefere As String In {"switch", "switch_1", "switch_led", "switch_led_1"}
            Dim trouve As KeyValuePair(Of String, Boolean) = candidats.FirstOrDefault(Function(k) String.Equals(k.Key, prefere, StringComparison.OrdinalIgnoreCase))
            If trouve.Key IsNot Nothing Then Return New KeyValuePair(Of String, String)(trouve.Key, If(trouve.Value, "ON", "OFF"))
        Next
        Dim autre As KeyValuePair(Of String, Boolean) = candidats.FirstOrDefault(Function(k) Not k.Key.Contains("backlight", StringComparison.OrdinalIgnoreCase))
        If autre.Key IsNot Nothing Then Return New KeyValuePair(Of String, String)(autre.Key, If(autre.Value, "ON", "OFF"))
        Return New KeyValuePair(Of String, String)("", "")
    End Function

    Private ReadOnly _echelles As New Dictionary(Of String, Dictionary(Of String, Integer))

    ''' <summary>Les echelles des valeurs d'un appareil (code -> nombre de decimales : 188 avec l'echelle 1 vaut 18,8), lues dans ses specifications ; gardees en memoire.</summary>
    Private Async Function EchellesAsync(c As Compte, appareilId As String) As Task(Of Dictionary(Of String, Integer))
        SyncLock _verrou
            Dim connues As Dictionary(Of String, Integer) = Nothing
            If _echelles.TryGetValue(appareilId, connues) Then Return connues
        End SyncLock
        Dim echelles As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        Try
            Dim resultat As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/devices/" & appareilId & "/specifications", "").ConfigureAwait(False)
            Dim statut As JsonElement
            If resultat.TryGetProperty("status", statut) AndAlso statut.ValueKind = JsonValueKind.Array Then
                For Each e As JsonElement In statut.EnumerateArray()
                    Dim valeurs As String = Texte(e, "values")
                    If valeurs = "" Then Continue For
                    Using doc As JsonDocument = JsonDocument.Parse(valeurs)
                        Dim echelle As JsonElement
                        If doc.RootElement.ValueKind = JsonValueKind.Object AndAlso doc.RootElement.TryGetProperty("scale", echelle) AndAlso echelle.ValueKind = JsonValueKind.Number Then
                            echelles(Texte(e, "code")) = echelle.GetInt32()
                        End If
                    End Using
                Next
            End If
        Catch ex As Exception
            ' (specifications indisponibles : valeurs lues telles quelles)
        End Try
        SyncLock _verrou
            _echelles(appareilId) = echelles
        End SyncLock
        Return echelles
    End Function

    ''' <summary>
    ''' Pour un appareil SANS interrupteur (capteur de temperature et d'humidite...) : sa mesure en clair, par exemple "18,8 °C - 58 % - pile 30 %" ; "" s'il n'a rien de tel.
    ''' </summary>
    Friend Async Function MesureAsync(c As Compte, appareilId As String) As Task(Of String)
        Dim statut As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/devices/" & appareilId & "/status", "").ConfigureAwait(False)
        Dim valeurs As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
        If statut.ValueKind = JsonValueKind.Array Then
            For Each e As JsonElement In statut.EnumerateArray()
                Dim v As JsonElement
                If e.TryGetProperty("value", v) AndAlso v.ValueKind = JsonValueKind.Number Then valeurs(Texte(e, "code")) = v.GetDouble()
            Next
        End If
        If valeurs.Count = 0 Then Return ""
        Dim echelles As Dictionary(Of String, Integer) = Await EchellesAsync(c, appareilId).ConfigureAwait(False)
        Dim lire As Func(Of String(), Double?) =
            Function(codes As String()) As Double?
                For Each code As String In codes
                    Dim brut As Double
                    If valeurs.TryGetValue(code, brut) Then
                        Dim echelle As Integer = 0
                        echelles.TryGetValue(code, echelle)
                        Return brut / Math.Pow(10, echelle)
                    End If
                Next
                Return Nothing
            End Function
        Dim morceaux As New List(Of String)
        Dim temperature As Double? = lire({"va_temperature", "temp_current", "temperature", "temp_value", "temp"})
        Dim humidite As Double? = lire({"humidity_value", "va_humidity", "humidity"})
        Dim pile As Double? = lire({"battery_percentage"})
        If temperature.HasValue Then morceaux.Add(temperature.Value.ToString("0.0", Globalization.CultureInfo.CurrentCulture) & " °C")
        If humidite.HasValue Then morceaux.Add(humidite.Value.ToString("0.#", Globalization.CultureInfo.CurrentCulture) & " %")
        If morceaux.Count > 0 AndAlso pile.HasValue Then morceaux.Add("pile " & pile.Value.ToString("0", Globalization.CultureInfo.CurrentCulture) & " %")
        Return String.Join(" - ", morceaux)
    End Function

    ''' <summary>Envoie une commande ; rend "" si elle est acceptee, sinon la raison de l'echec.</summary>
    ''' <param name="comptes">Les comptes connus : la commande part avec celui de son appareil (Commande.CompteNom) ; a defaut, le premier.</param>
    Friend Async Function EnvoyerLaCommandeAsync(comptes As IList(Of Compte), commande As Commande) As Task(Of String)
        If commande.Membres IsNot Nothing Then
            Dim resultats As String() = Await Task.WhenAll(commande.Membres.Select(Function(m) EnvoyerLaCommandeAsync(comptes, m))).ConfigureAwait(False)
            Dim echecs As List(Of String) = resultats.Select(Function(r, i) If(r = "", "", commande.Membres(i).Nom & " : " & r)).Where(Function(r) r <> "").ToList()
            Return String.Join(" ; ", echecs)
        End If
        Try
            Dim c As Compte = comptes.FirstOrDefault(Function(x) String.Equals(x.Nom, commande.CompteNom, StringComparison.OrdinalIgnoreCase))
            If c Is Nothing Then c = comptes.FirstOrDefault()
            If c Is Nothing OrElse Not c.Renseigne Then Return "compte Tuya non renseigné (onglet Sources)"
            If commande.AppareilId = "" Then Return "aucun appareil pour « " & commande.Nom & " »"
            Dim corps As String
            Using flux As New IO.MemoryStream()
                Using w As New Utf8JsonWriter(flux)
                    w.WriteStartObject()
                    w.WriteStartArray("commands")
                    w.WriteStartObject()
                    w.WriteString("code", commande.Code)
                    EcrireLaValeur(w, commande.Valeur)
                    w.WriteEndObject()
                    w.WriteEndArray()
                    w.WriteEndObject()
                End Using
                corps = Encoding.UTF8.GetString(flux.ToArray())
            End Using
            Await AppelAsync(c, HttpMethod.Post, "/v1.0/devices/" & commande.AppareilId & "/commands", corps).ConfigureAwait(False)
            Return ""
        Catch ex As TuyaException
            Return Traduire(ex)
        Catch ex As Exception
            Return ex.Message
        End Try
    End Function

    ''' <summary>vrai / faux, nombre entier ou decimal, sinon texte.</summary>
    Private Sub EcrireLaValeur(w As Utf8JsonWriter, valeur As String)
        Dim v As String = If(valeur, "").Trim()
        Dim n As Long, d As Double
        If String.Equals(v, "true", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(v, "vrai", StringComparison.OrdinalIgnoreCase) Then
            w.WriteBoolean("value", True)
        ElseIf String.Equals(v, "false", StringComparison.OrdinalIgnoreCase) OrElse String.Equals(v, "faux", StringComparison.OrdinalIgnoreCase) Then
            w.WriteBoolean("value", False)
        ElseIf Long.TryParse(v, Globalization.NumberStyles.Integer, Globalization.CultureInfo.InvariantCulture, n) Then
            w.WriteNumber("value", n)
        ElseIf Double.TryParse(v, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, d) Then
            w.WriteNumber("value", d)
        Else
            w.WriteString("value", v)
        End If
    End Sub

    ''' <summary>
    ''' Execute les commandes de la ligne de minuterie (separees par ";" quand la ligne en a plusieurs), en tache de fond (la minuterie n'attend pas) ; l'echec est
    ''' note dans le journal. <paramref name="argument"/> : l'argument de la ligne ; "!/Compte:Nom" dit de quel compte sont ses appareils (plusieurs comptes).
    ''' </summary>
    Friend Sub Envoyer(nomDeLaCommande As String, Optional argument As String = "")
        Dim compteNom As String = Convert.ToString(Arg("Compte", If(argument, ""))).Trim()
        For Each morceau As String In If(nomDeLaCommande, "").Split(";"c)
            If morceau.Trim() <> "" Then EnvoyerUne(morceau, compteNom)
        Next
    End Sub

    ''' <summary>La commande de ce nom : celle des appareils du compte demande (les groupes valent pour tous les comptes) ; sans compte, la premiere.</summary>
    Private Function TrouverLaCommande(toutes As List(Of Commande), nom As String, compteNom As String) As Commande
        Dim memeNom As List(Of Commande) = toutes.Where(Function(x) String.Equals(x.Nom, nom, StringComparison.OrdinalIgnoreCase)).ToList()
        If compteNom <> "" Then
            Dim duCompte As Commande = memeNom.FirstOrDefault(Function(x) x.Membres IsNot Nothing OrElse String.Equals(x.CompteNom, compteNom, StringComparison.OrdinalIgnoreCase))
            If duCompte IsNot Nothing Then Return duCompte
        End If
        Return memeNom.FirstOrDefault()        ' (compte inconnu ou supprime : la premiere commande de ce nom)
    End Function

    ''' <summary>L'erreur de Tuya en clair (les plus frequentes) : "device is offline (code 2001)" devient "appareil hors ligne".</summary>
    Private Function Traduire(ex As TuyaException) As String
        Select Case ex.Numero
            Case "2001" : Return "appareil hors ligne (Tuya ne le joint pas) - code 2001"
            Case "1004" : Return "signature refusée : Client ID ou clé secrète faux - code 1004"
            Case "1106" : Return "permission refusée : compte non lié au projet cloud, ou UID faux - code 1106"
            Case "28841105" : Return "service non activé pour le projet (IoT Core...) - code 28841105"
            Case "2406" : Return "centre de données incorrect - code 2406"
        End Select
        Return ex.Message & If(ex.Numero <> "", " (code " & ex.Numero & ")", "")
    End Function

    ''' <summary>La commande de ce nom, y compris pour une ligne ecrite avec le prefixe d'une version precedente ("Maison : Salon ON").</summary>
    Private Function ChercherLaCommande(toutes As List(Of Commande), nom As String, compteNom As String) As Commande
        Dim commande As Commande = TrouverLaCommande(toutes, nom, compteNom)
        If commande Is Nothing AndAlso nom.Contains(" : ") Then
            commande = TrouverLaCommande(toutes, nom.Substring(nom.IndexOf(" : ") + 3), nom.Substring(0, nom.IndexOf(" : ")).Trim())
        End If
        Return commande
    End Function

    ''' <summary>
    ''' Comme Envoyer, mais ATTEND la reponse de Tuya et la rend en clair (une ligne par commande : « X » : envoyee, ou echec et sa raison) : sert au clic sur la case Play
    ''' de la minuterie, qui montre le resultat a l'ecran.
    ''' </summary>
    Friend Async Function EnvoyerEtRapporter(nomDeLaCommande As String, Optional argument As String = "") As Task(Of String)
        If Not Actif() Then Return "Tuya est désactivé : cochez « Utiliser Tuya » dans l'onglet Sources."
        Dim compteNom As String = Convert.ToString(Arg("Compte", If(argument, ""))).Trim()
        Dim toutes As List(Of Commande) = Commandes()
        Dim comptes As List(Of Compte) = ComptesActuels()
        Dim lignes As New List(Of String)
        For Each morceau As String In If(nomDeLaCommande, "").Split(";"c)
            Dim nom As String = morceau.Trim()
            If nom = "" Then Continue For
            Dim commande As Commande = ChercherLaCommande(toutes, nom, compteNom)
            If commande Is Nothing Then
                lignes.Add("« " & nom & " » : commande inconnue (onglet Sources, cadre Tuya)")
                DiagLog("Tuya : commande « " & nom & " » inconnue (liste des commandes, onglet Sources)")
                Continue For
            End If
            Dim erreur As String = Await EnvoyerLaCommandeAsync(comptes, commande)
            If erreur = "" Then
                lignes.Add("« " & commande.Nom & " » : envoyée")
                DiagLog("Tuya : « " & commande.Nom & " » envoyée (" & commande.Code & " = " & commande.Valeur & ")")
            Else
                lignes.Add("« " & commande.Nom & " » : ÉCHEC, " & erreur)
                DiagLog("Tuya : « " & commande.Nom & " » échec : " & erreur)
            End If
        Next
        If lignes.Count = 0 Then Return "Aucune commande Tuya dans cette ligne."
        Return String.Join(vbCrLf, lignes)
    End Function

    Private Sub EnvoyerUne(nomDeLaCommande As String, compteNom As String)
        Dim nom As String = If(nomDeLaCommande, "").Trim()
        If Not Actif() Then
            DiagLog("Tuya : desactive (case 'Utiliser Tuya' decochee), commande « " & nom & " » non envoyee")
            Exit Sub
        End If
        Dim toutes As List(Of Commande) = Commandes()
        Dim commande As Commande = TrouverLaCommande(toutes, nom, compteNom)
        If commande Is Nothing AndAlso nom.Contains(" : ") Then
            ' Ligne ecrite par une version precedente, qui faisait preceder le nom de celui du compte ("Maison : Salon ON").
            commande = TrouverLaCommande(toutes, nom.Substring(nom.IndexOf(" : ") + 3), nom.Substring(0, nom.IndexOf(" : ")).Trim())
        End If
        If commande Is Nothing Then
            DiagLog("Tuya : commande « " & nom & " » inconnue (liste des commandes, onglet Sources)")
            Exit Sub
        End If
        Dim comptes As List(Of Compte) = ComptesActuels()
        Task.Run(Async Function() As Task
                     Dim erreur As String = Await EnvoyerLaCommandeAsync(comptes, commande).ConfigureAwait(False)
                     If erreur = "" Then
                         DiagLog("Tuya : « " & commande.Nom & " » envoyée (" & commande.Code & " = " & commande.Valeur & ")")
                     Else
                         DiagLog("Tuya : « " & commande.Nom & " » échec : " & erreur)
                     End If
                 End Function)
    End Sub

End Module
