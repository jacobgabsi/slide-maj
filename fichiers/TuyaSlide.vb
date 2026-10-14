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
        ''' <summary>La maison Smart Life de l'appareil (« Soisy », « Cannes », « Bureau »...) ; vide si Tuya ne la donne pas.</summary>
        Friend MaisonNom As String = ""
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
        ''' <summary>La maison de l'appareil (ou du groupe) ; "" si inconnue : la minuterie choisit la maison d'une ligne Tuya (« !/Maison:Nom »).</summary>
        Friend MaisonNom As String = ""
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
        ''' <summary>La maison Smart Life de l'appareil (au dernier chargement) ; vide si inconnue.</summary>
        Friend MaisonNom As String = ""
    End Class

    Private Function Nettoye(texte As String) As String
        Return If(texte, "").Replace("|", " ").Replace(vbCr, " ").Replace(vbLf, " ").Trim()
    End Function

    ''' <summary>Une ligne par appareil : "Nom|id|code|1 (propose) ou 0".</summary>
    Friend Function SerialiserLesAppareils(appareils As IEnumerable(Of AppareilChoisi)) As String
        Dim lignes As New List(Of String)
        For Each a As AppareilChoisi In appareils
            If Nettoye(a.Id) = "" Then Continue For
            lignes.Add(String.Join("|", Nettoye(a.Nom), Nettoye(a.Id), Nettoye(a.Code), If(a.Coche, "1", "0"), Nettoye(a.CompteNom), Nettoye(a.Etat), If(a.EnLigne, "1", "0"), Nettoye(a.Mesure), Nettoye(a.MaisonNom)))
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
                                           .Etat = If(p.Length > 5, p(5).Trim(), ""), .EnLigne = Not (p.Length > 6 AndAlso p(6).Trim() = "0"), .Mesure = If(p.Length > 7, p(7).Trim(), ""),
                                           .MaisonNom = If(p.Length > 8, ReparerLesAccents(p(8).Trim()), "")})
        Next
        Return liste
    End Function

    ''' <summary>Un groupe d'appareils : "Groupe ON" / "Groupe OFF" dans la minuterie agissent sur tous ses appareils.</summary>
    Friend Class GroupeTuya
        Friend Nom As String = ""
        Friend Ids As New List(Of String)
        ''' <summary>Le compte Tuya auquel appartient le groupe (28/10/2026 : « les groupes doivent appartenir a chaque compte ») : ses appareils sont ceux de ce compte, et deux comptes
        ''' peuvent avoir un groupe du meme nom. Vide : groupe d'avant les comptes, valable pour tous.</summary>
        Friend CompteNom As String = ""
        ''' <summary>La maison (de ce compte) a laquelle appartient le groupe (28/10/2026 : « les groupes soient attaches a chaque maison ») : ses appareils sont ceux de cette maison. Vide : groupe d'avant les
        ''' maisons, ou maisons pas encore connues.</summary>
        Friend MaisonNom As String = ""
    End Class

    ''' <summary>Une ligne par groupe : "Nom|id1,id2,...|compte|maison".</summary>
    Friend Function SerialiserLesGroupes(groupes As IEnumerable(Of GroupeTuya)) As String
        Dim lignes As New List(Of String)
        For Each g As GroupeTuya In groupes
            If Nettoye(g.Nom) = "" Then Continue For
            lignes.Add(Nettoye(g.Nom) & "|" & String.Join(",", g.Ids.Select(Function(i) Nettoye(i)).Where(Function(i) i <> "")) & "|" & Nettoye(g.CompteNom) & "|" & Nettoye(g.MaisonNom))
        Next
        Return String.Join(vbLf, lignes)
    End Function

    ''' <summary>Les groupes enregistres. Un groupe d'avant les comptes (sans compte) est rattache au compte de ses appareils ; s'il en reunit de plusieurs comptes, il est coupe en un groupe
    ''' du meme nom par compte. Sans appareil connu, il reste sans compte (valable pour tous).</summary>
    Friend Function GroupesEnregistres() As List(Of GroupeTuya)
        Dim liste As New List(Of GroupeTuya)
        Dim appareils As List(Of AppareilChoisi) = Nothing
        For Each ligne As String In If(My.Settings.Tuya_Groupes, "").Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries)
            Dim p As String() = ligne.Split("|"c)
            If p.Length < 1 OrElse p(0).Trim() = "" Then Continue For
            Dim g As New GroupeTuya With {.Nom = p(0).Trim(), .CompteNom = If(p.Length > 2, p(2).Trim(), ""), .MaisonNom = If(p.Length > 3, ReparerLesAccents(p(3).Trim()), "")}
            If p.Length > 1 Then g.Ids.AddRange(p(1).Split(","c).Select(Function(i) i.Trim()).Where(Function(i) i <> ""))
            If g.CompteNom = "" AndAlso g.Ids.Count > 0 Then
                If appareils Is Nothing Then appareils = AppareilsEnregistres()
                Dim proprietaires As List(Of String) = appareils.Where(Function(a) g.Ids.Contains(a.Id) AndAlso a.CompteNom <> "").Select(Function(a) a.CompteNom).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
                If proprietaires.Count = 1 Then
                    g.CompteNom = proprietaires(0)
                ElseIf proprietaires.Count > 1 Then
                    For Each compte As String In proprietaires
                        Dim copie As New GroupeTuya With {.Nom = g.Nom, .CompteNom = compte}
                        copie.Ids.AddRange(g.Ids.Where(Function(id) appareils.Any(Function(a) a.Id = id AndAlso String.Equals(a.CompteNom, compte, StringComparison.OrdinalIgnoreCase))))
                        liste.Add(copie)
                    Next
                    Continue For
                End If
            End If
            ' Groupe sans maison dont les appareils sont connus avec leur maison : rattache a la maison de ses appareils, ou coupe en un groupe du meme nom par maison.
            If g.MaisonNom = "" AndAlso g.Ids.Count > 0 Then
                If appareils Is Nothing Then appareils = AppareilsEnregistres()
                Dim membres As List(Of AppareilChoisi) = appareils.Where(Function(a) g.Ids.Contains(a.Id) AndAlso a.MaisonNom <> "" AndAlso (g.CompteNom = "" OrElse String.Equals(a.CompteNom, g.CompteNom, StringComparison.OrdinalIgnoreCase))).ToList()
                Dim maisons As List(Of String) = membres.Select(Function(a) a.MaisonNom).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList()
                If maisons.Count = 1 Then
                    g.MaisonNom = maisons(0)
                ElseIf maisons.Count > 1 Then
                    For Each maison As String In maisons
                        Dim copie As New GroupeTuya With {.Nom = g.Nom, .CompteNom = g.CompteNom, .MaisonNom = maison}
                        copie.Ids.AddRange(membres.Where(Function(a) String.Equals(a.MaisonNom, maison, StringComparison.CurrentCultureIgnoreCase)).Select(Function(a) a.Id))
                        liste.Add(copie)
                    Next
                    Continue For
                End If
            End If
            liste.Add(g)
        Next
        Return liste
    End Function

    ''' <summary>Apres un chargement des appareils (qui apprend leurs maisons) : les groupes sans maison sont rattaches a la maison de leurs appareils, ou coupes en un groupe par maison (meme regle que
    ''' GroupesEnregistres, sur la liste en cours de saisie).</summary>
    Friend Sub RattacherLesGroupesAuxMaisons(groupes As List(Of GroupeTuya), appareils As List(Of AppareilChoisi))
        For Each g As GroupeTuya In groupes.ToList()
            If g.MaisonNom <> "" OrElse g.Ids.Count = 0 Then Continue For
            Dim membres As List(Of AppareilChoisi) = appareils.Where(Function(a) g.Ids.Contains(a.Id) AndAlso a.MaisonNom <> "" AndAlso (g.CompteNom = "" OrElse String.Equals(a.CompteNom, g.CompteNom, StringComparison.OrdinalIgnoreCase))).ToList()
            Dim maisons As List(Of String) = membres.Select(Function(a) a.MaisonNom).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList()
            If maisons.Count = 1 Then
                g.MaisonNom = maisons(0)
            ElseIf maisons.Count > 1 Then
                groupes.Remove(g)
                For Each maison As String In maisons
                    Dim copie As New GroupeTuya With {.Nom = g.Nom, .CompteNom = g.CompteNom, .MaisonNom = maison}
                    copie.Ids.AddRange(membres.Where(Function(a) String.Equals(a.MaisonNom, maison, StringComparison.CurrentCultureIgnoreCase)).Select(Function(a) a.Id))
                    groupes.Add(copie)
                Next
            End If
        Next
    End Sub

    ''' <summary>"Nom ON" (code = vrai) et "Nom OFF" (code = faux) pour chaque appareil coche qui a un interrupteur ; idem pour chaque groupe (tous ses appareils qui ont un interrupteur).</summary>
    ''' <param name="prefixer">Plusieurs comptes : chaque nom d'appareil commence par celui de son compte ("Maison : Salon ON").</param>
    Friend Function CommandesDe(appareils As IEnumerable(Of AppareilChoisi), Optional groupes As IEnumerable(Of GroupeTuya) = Nothing, Optional prefixer As Boolean = False) As List(Of Commande)
        Dim liste As New List(Of Commande)
        For Each a As AppareilChoisi In appareils
            If Not a.Coche OrElse a.Code = "" OrElse a.Id = "" Then Continue For
            Dim prefixe As String = If(prefixer AndAlso a.CompteNom <> "", a.CompteNom & " : ", "")
            If EstUneScene(a.Code) Then
                ' Un scenario manuel : une seule commande (« Scene Nom »), sans ON ni OFF.
                liste.Add(New Commande With {.Nom = prefixe & "Scène " & a.Nom, .NomCourt = "Scène " & a.Nom, .AppareilId = a.Id, .AppareilNom = a.Nom, .CompteNom = a.CompteNom, .MaisonNom = a.MaisonNom, .Code = a.Code, .Valeur = ""})
                Continue For
            End If
            liste.Add(New Commande With {.Nom = prefixe & a.Nom & " ON", .NomCourt = a.Nom & " ON", .AppareilId = a.Id, .AppareilNom = a.Nom, .CompteNom = a.CompteNom, .MaisonNom = a.MaisonNom, .Code = a.Code, .Valeur = "true"})
            liste.Add(New Commande With {.Nom = prefixe & a.Nom & " OFF", .NomCourt = a.Nom & " OFF", .AppareilId = a.Id, .AppareilNom = a.Nom, .CompteNom = a.CompteNom, .MaisonNom = a.MaisonNom, .Code = a.Code, .Valeur = "false"})
        Next
        If groupes IsNot Nothing Then
            Dim listeDesGroupes As List(Of GroupeTuya) = groupes.ToList()
            For Each g As GroupeTuya In listeDesGroupes
                ' Un groupe appartient a un compte et a une maison : seuls les appareils de ce compte et de cette maison en font partie (sans compte ni maison : groupe d'avant).
                Dim membres As List(Of AppareilChoisi) = appareils.Where(Function(a) a.Code <> "" AndAlso Not EstUneScene(a.Code) AndAlso g.Ids.Contains(a.Id) AndAlso
                                                                         (g.CompteNom = "" OrElse String.Equals(a.CompteNom, g.CompteNom, StringComparison.OrdinalIgnoreCase)) AndAlso
                                                                         (g.MaisonNom = "" OrElse String.Equals(a.MaisonNom, g.MaisonNom, StringComparison.CurrentCultureIgnoreCase))).ToList()
                If membres.Count = 0 Then Continue For
                ' Deux maisons d'un meme compte avec un groupe du meme nom (« Salon ») : la maison s'ajoute au nom pour les distinguer dans la minuterie.
                Dim homonyme As Boolean = g.MaisonNom <> "" AndAlso listeDesGroupes.Any(Function(x) x IsNot g AndAlso String.Equals(x.Nom, g.Nom, StringComparison.CurrentCultureIgnoreCase) AndAlso String.Equals(x.CompteNom, g.CompteNom, StringComparison.OrdinalIgnoreCase))
                Dim nomDuGroupe As String = g.Nom & If(homonyme, " (" & g.MaisonNom & ")", "")
                For Each valeur As String In {"true", "false"}
                    Dim c As New Commande With {.Nom = "Groupe " & nomDuGroupe & If(valeur = "true", " ON", " OFF"), .Valeur = valeur, .Membres = New List(Of Commande), .CompteNom = g.CompteNom, .MaisonNom = g.MaisonNom}
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
    ''' <param name="maisonNom">Avec un compte : seulement les commandes de cette maison (et celles dont la maison n'est pas connue) ; "" : toutes les maisons du compte.</param>
    Friend Function NomsDesCommandes(Optional compteNom As String = Nothing, Optional maisonNom As String = Nothing) As List(Of String)
        Dim toutes As IEnumerable(Of Commande) = Commandes()
        If Not String.IsNullOrWhiteSpace(compteNom) Then toutes = toutes.Where(Function(c) String.Equals(c.CompteNom, compteNom, StringComparison.OrdinalIgnoreCase) OrElse (c.Membres IsNot Nothing AndAlso c.CompteNom = ""))
        If Not String.IsNullOrWhiteSpace(maisonNom) Then toutes = toutes.Where(Function(c) c.MaisonNom = "" OrElse String.Equals(c.MaisonNom, maisonNom, StringComparison.CurrentCultureIgnoreCase))
        Return toutes.Select(Function(c) c.Nom).Distinct(StringComparer.OrdinalIgnoreCase).ToList()
    End Function

    ''' <summary>Les maisons connues pour la minuterie : une entree (compte, maison) par maison qui a des commandes ; une entree (compte, "") pour les commandes d'un compte dont la maison est inconnue.
    ''' Triees par maison.</summary>
    Friend Function MaisonsDesCommandes() As List(Of KeyValuePair(Of String, String))
        ' Toutes les maisons des appareils connus (coches ou non), pas seulement celles qui ont deja des commandes : sinon une maison sans appareil coche n'etait jamais proposee
        ' (28/10/2026 : « pas de fenetre pour choisir la maison »). Une entree (compte, "") seulement pour un compte dont aucune maison n'est connue.
        Dim appareils As List(Of AppareilChoisi) = AppareilsActuels().Where(Function(a) a.CompteNom <> "").ToList()
        Dim paires As New List(Of KeyValuePair(Of String, String))
        For Each compte As String In appareils.Select(Function(a) a.CompteNom).Distinct(StringComparer.OrdinalIgnoreCase)
            Dim maisons As List(Of String) = appareils.Where(Function(a) String.Equals(a.CompteNom, compte, StringComparison.OrdinalIgnoreCase) AndAlso a.MaisonNom <> "").
                                             Select(Function(a) a.MaisonNom).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList()
            If maisons.Count = 0 Then
                paires.Add(New KeyValuePair(Of String, String)(compte, ""))
            Else
                For Each m As String In maisons
                    paires.Add(New KeyValuePair(Of String, String)(compte, m))
                Next
            End If
        Next
        Return paires.OrderBy(Function(p) p.Key, StringComparer.CurrentCultureIgnoreCase).ThenBy(Function(p) p.Value, StringComparer.CurrentCultureIgnoreCase).ToList()
    End Function

    ''' <summary>Les appareils en cours de saisie dans les reglages (s'ils sont ouverts), sinon les appareils enregistres.</summary>
    Friend FournisseurDAppareils As Func(Of List(Of AppareilChoisi))

    Friend Function AppareilsActuels() As List(Of AppareilChoisi)
        Dim f As Func(Of List(Of AppareilChoisi)) = FournisseurDAppareils
        Return If(f Is Nothing, AppareilsEnregistres(), f())
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
        Await RattacherLesMaisonsAsync(c, liste).ConfigureAwait(False)
        Return liste
    End Function

    ''' <summary>Les maisons Smart Life du compte (« Soisy », « Cannes », « Bureau »...) : pour chacune, ses appareils sont rattaches a son nom. Sans cela (service non active pour le projet cloud,
    ''' droit refuse...), les appareils restent sans maison, sans erreur : le chargement continue.</summary>
    Private Async Function RattacherLesMaisonsAsync(c As Compte, liste As List(Of Appareil)) As Task
        Try
            Dim maisons As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/users/" & c.Uid.Trim() & "/homes", "").ConfigureAwait(False)
            If maisons.ValueKind <> JsonValueKind.Array Then Exit Function
            Dim rattaches As Integer = 0
            For Each maison As JsonElement In maisons.EnumerateArray()
                Dim idMaison As String = ""
                Dim v As JsonElement
                If maison.TryGetProperty("home_id", v) Then idMaison = If(v.ValueKind = JsonValueKind.Number, v.GetInt64().ToString(), If(v.ValueKind = JsonValueKind.String, v.GetString(), ""))
                Dim nomMaison As String = Texte(maison, "name")
                If idMaison = "" OrElse nomMaison = "" Then Continue For
                Try
                    Dim appareils As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/homes/" & idMaison & "/devices", "").ConfigureAwait(False)
                    Dim tableau As JsonElement = appareils
                    If appareils.ValueKind = JsonValueKind.Object AndAlso appareils.TryGetProperty("devices", v) Then tableau = v
                    If tableau.ValueKind <> JsonValueKind.Array Then Continue For
                    For Each d As JsonElement In tableau.EnumerateArray()
                        Dim id As String = Texte(d, "id")
                        Dim a As Appareil = liste.FirstOrDefault(Function(x) x.Id = id)
                        If a IsNot Nothing Then a.MaisonNom = nomMaison : rattaches += 1
                    Next
                Catch ex As Exception
                    DiagLog("Tuya : appareils de la maison « " & nomMaison & " » (compte « " & c.Nom & " ») : " & ex.Message)
                End Try
            Next
            DiagLog("Tuya : compte « " & c.Nom & " » : " & maisons.GetArrayLength() & " maison(s), " & rattaches & " appareil(s) rattache(s)")
        Catch ex As Exception
            DiagLog("Tuya : maisons du compte « " & c.Nom & " » indisponibles : " & ex.Message)
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Scenarios manuels (« Lancer en un clic » de Smart Life), 30/10/2026
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Un scenario manuel est range comme un appareil : son Code est « @scene:(numero de la maison) », son Id le numero du scenario.</summary>
    Friend Const PrefixeScene As String = "@scene:"

    Friend Function EstUneScene(code As String) As Boolean
        Return If(code, "").StartsWith(PrefixeScene, StringComparison.Ordinal)
    End Function

    ''' <summary>Les scenarios manuels de chaque maison du compte (« Soisy », « Cannes »...) : un Appareil par scenario (Categorie = « @scene:numero de la maison »). Sans droit ou service Tuya
    ''' (« Smart Home Scene Linkage » du projet cloud), la liste reste vide sans bloquer le chargement des appareils ; la raison est dans le journal.</summary>
    Friend Async Function ListerLesScenesAsync(c As Compte) As Task(Of List(Of Appareil))
        Dim liste As New List(Of Appareil)
        If Not c.Renseigne OrElse c.Uid.Trim() = "" Then Return liste
        Try
            Dim maisons As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/users/" & c.Uid.Trim() & "/homes", "").ConfigureAwait(False)
            If maisons.ValueKind <> JsonValueKind.Array Then Return liste
            For Each maison As JsonElement In maisons.EnumerateArray()
                Dim idMaison As String = TexteOuNombre(maison, "home_id")
                Dim nomMaison As String = Texte(maison, "name")
                If idMaison = "" Then Continue For
                Try
                    ' Le menu « Scenario manuel » de l'application : la liste NOUVELLE de Tuya (/v2.0/cloud/scene/rule, type=scene) ne rend que les scenarios manuels ; (les parametres sont dans l'ordre alphabetique,
                    ' comme la signature l'exige). Si elle est refusee par le projet cloud, l'ancienne liste (/v1.1), qui melange scenarios et automatisations, est filtree par le NOM (finit par ON ou OFF).
                    Dim tableau As JsonElement = Nothing
                    Dim v As JsonElement
                    Dim viaV2 As Boolean = False
                    Try
                        Dim r2 As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v2.0/cloud/scene/rule?page_no=1&page_size=100&space_id=" & idMaison & "&type=scene", "").ConfigureAwait(False)
                        If r2.ValueKind = JsonValueKind.Array Then
                            tableau = r2 : viaV2 = True
                        ElseIf r2.ValueKind = JsonValueKind.Object Then
                            For Each nom As String In {"list", "scenes", "data"}
                                If r2.TryGetProperty(nom, v) AndAlso v.ValueKind = JsonValueKind.Array Then tableau = v : viaV2 = True : Exit For
                            Next
                        End If
                        If Not viaV2 Then DiagLog("Tuya : liste v2.0 des scenarios de « " & nomMaison & " » : reponse inattendue " & r2.GetRawText().Substring(0, Math.Min(300, r2.GetRawText().Length)))
                    Catch ex As Exception
                        DiagLog("Tuya : liste v2.0 des scenarios de « " & nomMaison & " » refusee (" & ex.Message & ") : ancienne liste")
                    End Try
                    If Not viaV2 Then
                        Dim scenes As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.1/homes/" & idMaison & "/scenes", "").ConfigureAwait(False)
                        tableau = scenes
                        If scenes.ValueKind = JsonValueKind.Object Then
                            For Each nom As String In {"list", "scenes", "data"}
                                If scenes.TryGetProperty(nom, v) AndAlso v.ValueKind = JsonValueKind.Array Then tableau = v : Exit For
                            Next
                        End If
                    End If
                    If tableau.ValueKind <> JsonValueKind.Array Then
                        DiagLog("Tuya : scenarios de la maison « " & nomMaison & " » : reponse inattendue")
                        Continue For
                    End If
                    Dim n As Integer = 0, ecartes As Integer = 0
                    For Each s As JsonElement In tableau.EnumerateArray()
                        Dim id As String = TexteOuNombre(s, "scene_id")
                        If id = "" Then id = TexteOuNombre(s, "id")
                        Dim nom As String = Texte(s, "name")
                        If id = "" OrElse nom = "" Then Continue For
                        ' Seuls les scenarios MANUELS (« lancer en un clic ») : une automatisation (declenchee par une condition : heure, capteur, meteo...) est ecartee.
                        ' Tuya (ici) ne distingue pas un scenario manuel d'une automatisation dans cette liste : la regle de Jacob (30/10/2026) est le NOM, comme « Chauffages Sous Sol ON » :
                        ' seuls les scenarios dont le nom finit par ON ou OFF sont gardes.
                        Dim nomSansEspace As String = nom.Trim()
                        If n + ecartes < 2 Then DiagLog("Tuya : exemple de scenario recu (" & If(viaV2, "v2.0", "v1.1") & ") : " & s.GetRawText().Substring(0, Math.Min(400, s.GetRawText().Length)))
                        If Not viaV2 AndAlso Not (nomSansEspace.EndsWith(" ON", StringComparison.OrdinalIgnoreCase) OrElse nomSansEspace.EndsWith(" OFF", StringComparison.OrdinalIgnoreCase)) Then
                            ecartes += 1
                            Continue For
                        End If
                        liste.Add(New Appareil With {.Id = id, .Nom = nom, .Categorie = PrefixeScene & idMaison, .EnLigne = True, .MaisonNom = nomMaison})
                        n += 1
                    Next
                    DiagLog("Tuya : compte « " & c.Nom & " », maison « " & nomMaison & " » : " & n & " scenario(s) garde(s) (nom finissant par ON ou OFF), " & ecartes & " ecarte(s)")
                Catch ex As Exception
                    DiagLog("Tuya : scenarios de la maison « " & nomMaison & " » (compte « " & c.Nom & " ») illisibles : " & ex.Message)
                End Try
            Next
        Catch ex As Exception
            DiagLog("Tuya : scenarios du compte « " & c.Nom & " » indisponibles : " & ex.Message)
        End Try
        Return liste
    End Function

    ''' <summary>"" pour un scenario manuel ; sinon pourquoi c'est une automatisation : des conditions de declenchement, ou un type « automatisation » donne par Tuya.</summary>
    Private Function RaisonDEcarterUneAutomatisation(s As JsonElement) As String
        Dim v As JsonElement
        For Each nom As String In {"conditions", "preconditions"}
            If s.TryGetProperty(nom, v) AndAlso v.ValueKind = JsonValueKind.Array AndAlso v.GetArrayLength() > 0 Then Return "des « " & nom & " »"
        Next
        For Each nom As String In {"scene_type", "type", "rule_type", "category", "kind"}
            If s.TryGetProperty(nom, v) Then
                Dim t As String = If(v.ValueKind = JsonValueKind.String, v.GetString(), v.GetRawText()).ToLowerInvariant()
                If t.Contains("auto") OrElse t = "2" OrElse t.Contains("condition") Then Return nom & " = " & t
            End If
        Next
        For Each nom As String In {"automation", "is_automation", "is_auto"}
            If s.TryGetProperty(nom, v) AndAlso v.ValueKind = JsonValueKind.True Then Return nom & " = vrai"
        Next
        Return ""
    End Function

    Private Function TexteOuNombre(e As JsonElement, nom As String) As String
        Dim v As JsonElement
        If Not e.TryGetProperty(nom, v) Then Return ""
        If v.ValueKind = JsonValueKind.String Then Return If(v.GetString(), "")
        If v.ValueKind = JsonValueKind.Number Then Return v.GetRawText()
        Return ""
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

        ' Aucun code "switch*" (rubans de LED, lampes, prises d'autres marques : "led_switch", "power", "light", "lamp"...) : tout code booleen d'allumage connu est pris
        ' (26/10/2026 : "des modules Tuya que je n'arrive pas a allumer car il n'y a pas de ON OFF, par exemple Led sous sol").
        Dim noms As New List(Of String)
        If resultat.ValueKind = JsonValueKind.Array Then
            For Each e As JsonElement In resultat.EnumerateArray()
                Dim code As String = Texte(e, "code")
                Dim v As JsonElement
                noms.Add(code & If(e.TryGetProperty("value", v), "=" & v.ToString(), ""))
                If e.TryGetProperty("value", v) AndAlso (v.ValueKind = JsonValueKind.True OrElse v.ValueKind = JsonValueKind.False) AndAlso EstUnCodeDAllumage(code) Then
                    Return New KeyValuePair(Of String, String)(code, If(v.ValueKind = JsonValueKind.True, "ON", "OFF"))
                End If
            Next
        End If
        ' Dernier recours : les fonctions de l'appareil (ce qu'on peut lui commander), une booleenne d'allumage ; son etat n'est alors pas connu ("OFF" par defaut).
        Try
            Dim fonctions As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/devices/" & appareilId & "/functions", "").ConfigureAwait(False)
            Dim liste As JsonElement
            If fonctions.TryGetProperty("functions", liste) AndAlso liste.ValueKind = JsonValueKind.Array Then
                For Each f As JsonElement In liste.EnumerateArray()
                    Dim code As String = Texte(f, "code")
                    If Texte(f, "type").Equals("Boolean", StringComparison.OrdinalIgnoreCase) AndAlso EstUnCodeDAllumage(code) Then
                        DiagLog("Tuya : appareil " & appareilId & " : interrupteur trouve dans ses fonctions (" & code & ")")
                        Return New KeyValuePair(Of String, String)(code, "OFF")
                    End If
                Next
            End If
        Catch ex As Exception
        End Try
        DiagLog("Tuya : appareil " & appareilId & " : aucun interrupteur reconnu ; etat lu : " & If(noms.Count = 0, "(vide)", String.Join(", ", noms)))
        Return New KeyValuePair(Of String, String)("", "")
    End Function

    ''' <summary>Un code Tuya booleen qui allume ou eteint la charge : "switch..." deja vu plus haut, et "led_switch", "light_switch", "power", "light", "lamp", "onoff"... (jamais le voyant de l'interrupteur).</summary>
    Private Function EstUnCodeDAllumage(code As String) As Boolean
        If String.IsNullOrEmpty(code) OrElse code.Contains("backlight", StringComparison.OrdinalIgnoreCase) Then Return False
        Dim c As String = code.ToLowerInvariant()
        Return c.Contains("switch") OrElse c = "power" OrElse c.StartsWith("power_") OrElse c = "light" OrElse c = "lamp" OrElse c = "led" OrElse c = "onoff" OrElse c = "on_off" OrElse c = "start"
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Options d'un appareil (Jacob, 26/10/2026 : "remplace la colonne interrupteur par les options possibles, par exemple changer l'intensite d'une lumiere avec un curseur") :
    ' ce que l'appareil sait faire en plus d'ON / OFF, lu dans ses fonctions Tuya (/functions) avec sa valeur actuelle (/status).
    ' ------------------------------------------------------------------------------------------

    Friend Class OptionTuya
        Friend Code As String = ""
        ''' <summary>Nom en francais ("Luminosité"), ou le code si inconnu.</summary>
        Friend Nom As String = ""
        ''' <summary>"Boolean", "Integer" ou "Enum".</summary>
        Friend Type As String = ""
        Friend Min As Double
        Friend Max As Double = 100
        ''' <summary>Pas des valeurs entieres (1 le plus souvent).</summary>
        Friend Pas As Double = 1
        ''' <summary>Valeurs possibles d'une option "Enum" ("white", "colour", "scene"...).</summary>
        Friend Valeurs As New List(Of String)
        ''' <summary>Valeur actuelle lue sur l'appareil, "" si inconnue.</summary>
        Friend Actuelle As String = ""
        ''' <summary>Option « Color » (colour_data_v2) : maximum de la teinte (h), de la saturation (s) et de la valeur (v) de l'appareil.</summary>
        Friend MaxH As Double = 360
        Friend MaxS As Double = 1000
        Friend MaxV As Double = 1000
    End Class

    Private Function NomFrancaisDeLOption(code As String) As String
        Select Case code.ToLowerInvariant()
            Case "bright_value", "bright_value_v2", "bright_value_1", "bright_value_2" : Return "Luminosité"
            Case "temp_value", "temp_value_v2" : Return "Température de couleur (chaud ↔ froid)"
            Case "work_mode" : Return "Mode"
            Case "scene_data", "scene_data_v2" : Return "Scène"
            Case "countdown_1", "countdown" : Return "Minuterie"
            Case "speed", "fan_speed", "fan_speed_percent" : Return "Vitesse"
            Case "mode" : Return "Mode"
            Case "light" : Return "Lumière"
            Case "colour_data", "colour_data_v2" : Return "Couleur"
            Case "child_lock" : Return "Sécurité enfant"
            Case "temp_set", "temp_set_f" : Return "Consigne de température"
            Case "mute" : Return "Muet"
            Case Else : Return code
        End Select
    End Function

    ''' <summary>Les options reglables d'un appareil : curseur (entier), choix (liste) ou case (booleen), sans les interrupteurs ON / OFF (les boutons du tableau les font deja) ni les codes
    ''' complexes (couleur RVB, scenes). Rend une liste vide si l'appareil n'a rien de plus.</summary>
    Friend Async Function OptionsAsync(c As Compte, appareilId As String, Optional codePrincipal As String = "") As Task(Of List(Of OptionTuya))
        Dim options As New List(Of OptionTuya)
        Dim ignorees As New List(Of String)          ' (journal : ce que l'appareil propose et que la fenetre ne sait pas regler)
        Dim fonctions As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/devices/" & appareilId & "/functions", "").ConfigureAwait(False)
        Dim listeVide As JsonElement
        ' Certains appareils (passerelles Zigbee...) ont une liste « functions » vide ou courte : les specifications completes (functions + status) sont lues aussi.
        If Not fonctions.TryGetProperty("functions", listeVide) OrElse listeVide.ValueKind <> JsonValueKind.Array OrElse listeVide.GetArrayLength() = 0 Then
            Try
                Dim specs As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/devices/" & appareilId & "/specifications", "").ConfigureAwait(False)
                If specs.TryGetProperty("functions", listeVide) AndAlso listeVide.ValueKind = JsonValueKind.Array AndAlso listeVide.GetArrayLength() > 0 Then fonctions = specs
            Catch ex As Exception
                DiagLog("Tuya : specifications de l'appareil " & appareilId & " illisibles (" & ex.Message & ")")
            End Try
        End If
        Dim actuelles As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        Try
            Dim statut As JsonElement = Await AppelAsync(c, HttpMethod.Get, "/v1.0/devices/" & appareilId & "/status", "").ConfigureAwait(False)
            If statut.ValueKind = JsonValueKind.Array Then
                For Each e As JsonElement In statut.EnumerateArray()
                    Dim v As JsonElement
                    If e.TryGetProperty("value", v) Then actuelles(Texte(e, "code")) = v.ToString()
                Next
            End If
        Catch ex As Exception
        End Try
        Dim liste As JsonElement
        If Not fonctions.TryGetProperty("functions", liste) OrElse liste.ValueKind <> JsonValueKind.Array Then Return options
        For Each f As JsonElement In liste.EnumerateArray()
            Dim code As String = Texte(f, "code")
            Dim type As String = Texte(f, "type")
            If code = "" Then Continue For
            ' Seul l'interrupteur principal est omis (les boutons ON / OFF du tableau le font deja) ; les autres (switch_led, switch_backlight, switch_2...) sont des options.
            If String.Equals(code, codePrincipal, StringComparison.OrdinalIgnoreCase) Then Continue For
            Dim o As New OptionTuya With {.Code = code, .Nom = NomFrancaisDeLOption(code), .Type = type}
            actuelles.TryGetValue(code, o.Actuelle)
            If o.Actuelle Is Nothing Then o.Actuelle = ""
            If type.Equals("Boolean", StringComparison.OrdinalIgnoreCase) Then
                o.Type = "Boolean"
            ElseIf type.Equals("Integer", StringComparison.OrdinalIgnoreCase) OrElse type.Equals("Enum", StringComparison.OrdinalIgnoreCase) Then
                Dim valeurs As String = Texte(f, "values")
                Try
                    Using doc As JsonDocument = JsonDocument.Parse(If(valeurs = "", "{}", valeurs))
                        Dim r As JsonElement = doc.RootElement
                        Dim x As JsonElement
                        If o.Type.Equals("Integer", StringComparison.OrdinalIgnoreCase) Then
                            o.Type = "Integer"
                            If r.TryGetProperty("min", x) AndAlso x.ValueKind = JsonValueKind.Number Then o.Min = x.GetDouble()
                            If r.TryGetProperty("max", x) AndAlso x.ValueKind = JsonValueKind.Number Then o.Max = x.GetDouble()
                            If r.TryGetProperty("step", x) AndAlso x.ValueKind = JsonValueKind.Number AndAlso x.GetDouble() > 0 Then o.Pas = x.GetDouble()
                            If o.Max <= o.Min Then ignorees.Add(code & " (Integer, plage vide)") : Continue For
                        Else
                            o.Type = "Enum"
                            If r.TryGetProperty("range", x) AndAlso x.ValueKind = JsonValueKind.Array Then
                                For Each valeur As JsonElement In x.EnumerateArray()
                                    o.Valeurs.Add(valeur.ToString())
                                Next
                            End If
                            If o.Valeurs.Count = 0 Then ignorees.Add(code & " (Enum, aucun choix)") : Continue For
                        End If
                    End Using
                Catch ex As Exception
                    ignorees.Add(code & " (" & type & ", valeurs illisibles)")
                    Continue For
                End Try
            ElseIf type.Equals("Json", StringComparison.OrdinalIgnoreCase) AndAlso code.StartsWith("colour_data", StringComparison.OrdinalIgnoreCase) Then
                ' Couleur : teinte / saturation / valeur ; les maximums sont lus dans les valeurs de l'appareil.
                o.Type = "Color"
                Try
                    Using doc As JsonDocument = JsonDocument.Parse(If(Texte(f, "values") = "", "{}", Texte(f, "values")))
                        Dim r As JsonElement = doc.RootElement
                        Dim axe As JsonElement, x As JsonElement
                        If r.ValueKind = JsonValueKind.Object Then
                            If r.TryGetProperty("h", axe) AndAlso axe.ValueKind = JsonValueKind.Object AndAlso axe.TryGetProperty("max", x) AndAlso x.ValueKind = JsonValueKind.Number Then o.MaxH = x.GetDouble()
                            If r.TryGetProperty("s", axe) AndAlso axe.ValueKind = JsonValueKind.Object AndAlso axe.TryGetProperty("max", x) AndAlso x.ValueKind = JsonValueKind.Number Then o.MaxS = x.GetDouble()
                            If r.TryGetProperty("v", axe) AndAlso axe.ValueKind = JsonValueKind.Object AndAlso axe.TryGetProperty("max", x) AndAlso x.ValueKind = JsonValueKind.Number Then o.MaxV = x.GetDouble()
                        End If
                    End Using
                Catch ex As Exception
                    ' (maximums par defaut : 360, 1000, 1000)
                End Try
            Else
                ignorees.Add(code & " (" & type & ")")          ' (scenes, chaines libres, commandes de controle : pas reglables ici)
                Continue For
            End If
            options.Add(o)
        Next
        DiagLog("Tuya : options de l'appareil " & appareilId & " : " & options.Count & " reglable(s) [" & String.Join(", ", options.Select(Function(x) x.Code & ":" & x.Type)) & "] ; non reglables ici [" & String.Join(", ", ignorees) & "]")
        Return options
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
        Dim c As Compte = Nothing
        Dim echec As TuyaException = Nothing
        Try
            c = comptes.FirstOrDefault(Function(x) String.Equals(x.Nom, commande.CompteNom, StringComparison.OrdinalIgnoreCase))
            If c Is Nothing Then c = comptes.FirstOrDefault()
            If c Is Nothing OrElse Not c.Renseigne Then Return "compte Tuya non renseigné (onglet Sources)"
            If commande.AppareilId = "" Then Return "aucun appareil pour « " & commande.Nom & " »"
            If EstUneScene(commande.Code) Then
                ' Scenario manuel : on le declenche (service « Smart Home Scene Linkage » du projet cloud) ; l'adresse plus recente sert de secours.
                Dim maisonId As String = commande.Code.Substring(PrefixeScene.Length)
                Dim premier As TuyaException = Nothing
                Try
                    Await AppelAsync(c, HttpMethod.Post, "/v1.0/homes/" & maisonId & "/scenes/" & commande.AppareilId & "/trigger", "").ConfigureAwait(False)
                    Return ""
                Catch ex As TuyaException
                    premier = ex
                End Try
                Try
                    Await AppelAsync(c, HttpMethod.Post, "/v2.0/cloud/scene/rule/" & commande.AppareilId & "/actions/trigger", "").ConfigureAwait(False)
                    Return ""
                Catch ex2 As TuyaException
                    Return Traduire(premier)
                End Try
            End If
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
            echec = ex
        Catch ex As Exception
            Return ex.Message
        End Try
        ' « Permission refusee » (1106) ou appareil inconnu (1109) : le plus souvent l'identifiant memorise est perime (appareil supprime puis rajoute dans Smart Life). La liste des appareils du
        ' compte est relue UNE fois (27/10/2026, Jacob : « il fallait cliquer sur Charger/Actualiser appareils avant ») et la commande repart avec le nouvel identifiant.
        If c IsNot Nothing AndAlso (echec.Numero = "1106" OrElse echec.Numero = "1109") AndAlso commande.AppareilNom <> "" Then
            Try
                Dim nouvelId As String = Await ActualiserLIdentifiantAsync(c, commande.AppareilNom, commande.AppareilId).ConfigureAwait(False)
                If nouvelId <> "" AndAlso nouvelId <> commande.AppareilId Then
                    DiagLog("Tuya : « " & commande.Nom & " » : identifiant perime, appareils relus, nouvel essai")
                    Dim copie As New Commande With {.Nom = commande.Nom, .NomCourt = commande.NomCourt, .AppareilId = nouvelId, .AppareilNom = commande.AppareilNom, .CompteNom = commande.CompteNom, .Code = commande.Code, .Valeur = commande.Valeur}
                    Return Await EnvoyerLaCommandeAsync(comptes, copie).ConfigureAwait(False)
                End If
            Catch ex2 As Exception
                DiagLog("Tuya : relecture des appareils impossible (" & ex2.Message & ")")
            End Try
        End If
        Return Traduire(echec)
    End Function

    ''' <summary>Au plus une relecture par compte et par 10 minutes (une vraie permission refusee ne doit pas relire la liste a chaque commande).</summary>
    Private ReadOnly _relectures As New Dictionary(Of String, Date)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>Relit les appareils de Tuya pour ce compte, remet a jour les appareils enregistres dont l'identifiant a change (retrouves par leur nom ; le code de leur interrupteur est relu) et rend
    ''' le nouvel identifiant de <paramref name="appareilNom"/> ("" : introuvable, inchange, ou relecture deja faite il y a moins de 10 minutes).</summary>
    Private Async Function ActualiserLIdentifiantAsync(c As Compte, appareilNom As String, ancienId As String) As Task(Of String)
        SyncLock _relectures
            Dim derniere As Date
            If _relectures.TryGetValue(c.Nom, derniere) AndAlso (Date.Now - derniere).TotalMinutes < 10 Then Return ""
            _relectures(c.Nom) = Date.Now
        End SyncLock
        Dim trouves As List(Of Appareil) = Await ListerLesAppareilsAsync(c).ConfigureAwait(False)
        Dim enregistres As List(Of AppareilChoisi) = AppareilsEnregistres()
        Dim change As Boolean = False
        For Each a As AppareilChoisi In enregistres
            If Not String.Equals(a.CompteNom, c.Nom, StringComparison.OrdinalIgnoreCase) OrElse EstUneScene(a.Code) Then Continue For
            If trouves.Any(Function(t) t.Id = a.Id) Then Continue For
            Dim nouveau As Appareil = trouves.FirstOrDefault(Function(t) String.Equals(t.Nom, a.Nom, StringComparison.OrdinalIgnoreCase))
            If nouveau Is Nothing Then Continue For
            a.Id = nouveau.Id
            a.EnLigne = nouveau.EnLigne
            Try
                Dim lecture As KeyValuePair(Of String, String) = Await InterrupteurAsync(c, nouveau.Id).ConfigureAwait(False)
                If lecture.Key <> "" Then a.Code = lecture.Key
            Catch ex As Exception
                ' (code de l'interrupteur inchange)
            End Try
            change = True
            DiagLog("Tuya : appareil « " & a.Nom & " » : nouvel identifiant " & nouveau.Id)
        Next
        If change Then
            My.Settings.Tuya_Appareils = SerialiserLesAppareils(enregistres)
            My.Settings.Save()
        End If
        Dim cible As Appareil = trouves.FirstOrDefault(Function(t) String.Equals(t.Nom, appareilNom, StringComparison.OrdinalIgnoreCase))
        Return If(cible Is Nothing, "", cible.Id)
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
        ElseIf v.StartsWith("{") AndAlso v.EndsWith("}") Then
            ' Valeur structuree (couleur : {"h":120,"s":1000,"v":1000}) : envoyee comme objet JSON.
            Try
                Using doc As JsonDocument = JsonDocument.Parse(v)
                    w.WritePropertyName("value")
                    doc.RootElement.WriteTo(w)
                End Using
            Catch ex As JsonException
                w.WriteString("value", v)
            End Try
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
        Dim maisonNom As String = Convert.ToString(Arg("Maison", If(argument, ""))).Trim()
        For Each morceau As String In If(nomDeLaCommande, "").Split(";"c)
            If morceau.Trim() <> "" Then EnvoyerUne(morceau, compteNom, maisonNom)
        Next
    End Sub

    ''' <summary>La commande de ce nom : celle des appareils du compte demande, de la maison demandee d'abord (les groupes valent pour tous les comptes) ; sans compte, la premiere.</summary>
    Private Function TrouverLaCommande(toutes As List(Of Commande), nom As String, compteNom As String, Optional maisonNom As String = "") As Commande
        Dim memeNom As List(Of Commande) = toutes.Where(Function(x) String.Equals(x.Nom, nom, StringComparison.OrdinalIgnoreCase)).ToList()
        If compteNom <> "" AndAlso maisonNom <> "" Then
            Dim deLaMaison As Commande = memeNom.FirstOrDefault(Function(x) String.Equals(x.MaisonNom, maisonNom, StringComparison.CurrentCultureIgnoreCase) AndAlso
                                                                (String.Equals(x.CompteNom, compteNom, StringComparison.OrdinalIgnoreCase) OrElse x.CompteNom = ""))
            If deLaMaison IsNot Nothing Then Return deLaMaison
        End If
        If compteNom <> "" Then
            Dim duCompte As Commande = memeNom.FirstOrDefault(Function(x) String.Equals(x.CompteNom, compteNom, StringComparison.OrdinalIgnoreCase) OrElse (x.Membres IsNot Nothing AndAlso x.CompteNom = ""))
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
    Private Function ChercherLaCommande(toutes As List(Of Commande), nom As String, compteNom As String, Optional maisonNom As String = "") As Commande
        Dim commande As Commande = TrouverLaCommande(toutes, nom, compteNom, maisonNom)
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
        Dim maisonNom As String = Convert.ToString(Arg("Maison", If(argument, ""))).Trim()
        Dim toutes As List(Of Commande) = Commandes()
        Dim comptes As List(Of Compte) = ComptesActuels()
        Dim lignes As New List(Of String)
        For Each morceau As String In If(nomDeLaCommande, "").Split(";"c)
            Dim nom As String = morceau.Trim()
            If nom = "" Then Continue For
            Dim commande As Commande = ChercherLaCommande(toutes, nom, compteNom, maisonNom)
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

    Private Sub EnvoyerUne(nomDeLaCommande As String, compteNom As String, Optional maisonNom As String = "")
        Dim nom As String = If(nomDeLaCommande, "").Trim()
        If Not Actif() Then
            DiagLog("Tuya : desactive (case 'Utiliser Tuya' decochee), commande « " & nom & " » non envoyee")
            Exit Sub
        End If
        Dim toutes As List(Of Commande) = Commandes()
        Dim commande As Commande = TrouverLaCommande(toutes, nom, compteNom, maisonNom)
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
