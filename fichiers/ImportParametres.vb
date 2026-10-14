Option Strict On
Option Explicit On

Imports System.IO
Imports Newtonsoft.Json.Linq

' Import des parametres (27/10/2026, Jacob : « reecris completement l'import des parametres d'une facon tres claire, professionnelle et detaillee, pour choisir precisement en
' detail ce que l'on veut recuperer »).
'
' Principe : le fichier .slideconf est ANALYSE et compare a ce PC, puis la fenetre FormImportParametres presente tout ce qu'il contient, element par element, sous la forme
' de la fenetre des Options (onglet > cadre > reglage), avec pour chacun la valeur du fichier, la valeur actuelle de ce PC et s'ils different. Rien n'est ecrit tant que
' l'utilisateur n'a pas valide. Une sauvegarde de l'etat actuel est faite juste avant. Seuls les elements coches sont appliques.
'
'   - Reglages : un par un (My.Settings).
'   - Mots de passe et cles secretes (Deezer, IPTV, Tuya) : un par un ; le fichier les porte en clair, ils sont rechiffres pour ce poste ; leur valeur n'est jamais affichee.
'   - Minuterie : ligne par ligne ; au choix elle REMPLACE la minuterie de ce PC ou s'y AJOUTE (sans doublon).
'   - Radios : station par station ; meme choix.

Friend Enum GenreElement
    Reglage
    Secret
    LigneMinuterie
    Radio
    ''' <summary>Une minuterie enregistree (data\settings\Minuteries) avec toutes ses lignes.</summary>
    MinuterieEnregistree
    ''' <summary>Un reglage de l'affichage automatique des clips Blink (data\settings\BlinkClips.ini).</summary>
    ReglageBlink
    ''' <summary>Un petit fichier de reglages de data\settings (annonces, normalisation, cle OMDb, notes...).</summary>
    FichierReglage
End Enum

Friend Enum ModeImport
    Remplacer
    Ajouter
End Enum

''' <summary>Un element que l'on peut reprendre du fichier : un reglage, un mot de passe, une ligne de minuterie ou une radio.</summary>
Friend Class ElementImport
    Friend Genre As GenreElement
    Friend Onglet As String = ""
    Friend Cadre As String = ""
    ''' <summary>Nom technique du reglage, ou description courte de la ligne / de la radio.</summary>
    Friend Nom As String = ""
    ''' <summary>Texte de l'arbre : nom et valeur du fichier, sur une ligne.</summary>
    Friend Libelle As String = ""
    Friend ValeurFichier As String = ""
    Friend ValeurActuelle As String = ""
    Friend Differe As Boolean
    Friend Coche As Boolean
    ''' <summary>Valeur du fichier : le reglage, le secret, la ligne de minuterie (objet) ou la radio (objet).</summary>
    Friend Jeton As JToken
    Friend Propriete As Configuration.SettingsProperty
End Class

''' <summary>Ce que contient un fichier d'export, compare a ce PC.</summary>
Friend Class ContenuImport
    Friend Chemin As String = ""
    Friend Racine As JObject
    Friend Poste As String = ""
    Friend DateExport As String = ""
    Friend VersionSlide As String = ""
    Friend Elements As New List(Of ElementImport)
    ''' <summary>Reglages du fichier que cette version de Slide ne connait pas (export d'une autre version) : ils ne peuvent pas etre repris.</summary>
    Friend NonReconnus As New List(Of String)
    Friend LignesMinuterieDuFichier As Integer
    Friend LignesMinuterieActuelles As Integer
    Friend RadiosDuFichier As Integer
    Friend RadiosActuelles As Integer
End Class

Friend Class ResultatImport
    Friend Reglages As Integer
    Friend Secrets As Integer
    Friend LignesMinuterie As Integer
    Friend MinuteriesEnregistrees As Integer
    Friend Radios As Integer
    Friend Sauvegarde As String = ""
    Friend Erreurs As New List(Of String)
End Class

Friend Module ImportParametres

    Private Const MasqueSecret As String = "•••••••• (information confidentielle, non affichée)"

    ' ------------------------------------------------------------------------------------------
    ' Analyse du fichier
    ' ------------------------------------------------------------------------------------------

    Private Sub Decouper(famille As String, ByRef onglet As String, ByRef cadre As String)
        If famille = TransfertParametres.FamilleMinuterie Then
            onglet = "Minuterie" : cadre = "Lignes du tableau"
            Exit Sub
        End If
        Dim p As Integer = famille.IndexOf(" > ", StringComparison.Ordinal)
        If p < 0 Then
            onglet = famille : cadre = ""
        Else
            onglet = famille.Substring(0, p) : cadre = famille.Substring(p + 3)
        End If
    End Sub

    ''' <summary>Le texte d'une valeur JSON (booleens en Oui / Non, listes une valeur par ligne).</summary>
    Private Function Texte(j As JToken) As String
        If j Is Nothing Then Return "(non défini)"
        Select Case j.Type
            Case JTokenType.Null
                Return "(vide)"
            Case JTokenType.Boolean
                Return If(CBool(j), "Oui", "Non")
            Case JTokenType.Array
                Dim a As JArray = CType(j, JArray)
                If a.Count = 0 Then Return "(liste vide)"
                Return a.Count & " élément(s) :" & vbCrLf & String.Join(vbCrLf, a.Select(Function(x) Texte(x)))
            Case Else
                Dim v As JValue = TryCast(j, JValue)
                Dim s As String = If(v IsNot Nothing, Convert.ToString(v.Value, Globalization.CultureInfo.InvariantCulture), j.ToString())
                Return If(s = "", "(vide)", s)
        End Select
    End Function

    ''' <summary>Une ligne pour l'arbre : la premiere ligne de la valeur, coupee.</summary>
    Private Function Court(texte As String) As String
        Dim ligne As String = texte.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
        If ligne Is Nothing Then Return "(vide)"
        If texte.Contains(vbLf) AndAlso texte.Split({vbLf}, StringSplitOptions.RemoveEmptyEntries).Length > 1 Then Return ligne.TrimEnd(":"c) & " …"
        Return If(ligne.Length > 70, ligne.Substring(0, 70) & "…", ligne)
    End Function

    ''' <summary>La valeur d'un secret telle qu'on peut l'afficher : jamais en clair (pour Tuya, seuls les noms des comptes).</summary>
    Private Function SecretAffichable(nom As String, clair As String) As String
        If clair Is Nothing OrElse clair.Trim() = "" Then Return "(vide)"
        If String.Equals(nom, "Tuya_Comptes", StringComparison.OrdinalIgnoreCase) Then
            Dim noms As List(Of String) = clair.Split({vbLf, vbCr}, StringSplitOptions.RemoveEmptyEntries).Select(Function(l) l.Split("|"c)(0).Trim()).Where(Function(n) n <> "").ToList()
            Return noms.Count & " compte(s) Tuya : " & String.Join(", ", noms) & vbCrLf & "(identifiants et clés secrètes non affichés)"
        End If
        If String.Equals(nom, "Blink_Connexion", StringComparison.OrdinalIgnoreCase) Then
            Try
                Return "Compte Blink : " & CStr(JObject.Parse(clair)("email")) & vbCrLf & "(mot de passe et jetons non affichés)"
            Catch ex As Exception
            End Try
        End If
        If String.Equals(nom, "Nvr_Connexion", StringComparison.OrdinalIgnoreCase) Then
            Try
                Return "Enregistreur de caméras IP : " & CStr(JObject.Parse(clair)("hote")) & vbCrLf & "(identifiant et mot de passe non affichés)"
            Catch ex As Exception
            End Try
        End If
        Return MasqueSecret
    End Function

    ''' <summary>Le texte d'un reglage Blink pour l'arbre.</summary>
    Private Function TexteBlink(valeurs As JObject) As String
        Return String.Join(vbCrLf, valeurs.Properties().Select(Function(p) p.Name & " : " & Texte(p.Value)))
    End Function

    Private Function ChampLigne(l As JObject, colonne As String) As String
        Dim j As JToken = l(colonne)
        If j Is Nothing Then Return ""
        If j.Type = JTokenType.Boolean Then Return If(CBool(j), "Oui", "Non")
        Return Convert.ToString(CType(j, JValue).Value, Globalization.CultureInfo.InvariantCulture)
    End Function

    ''' <summary>Lit le fichier et le compare a ce PC. Lance FormatException si ce n'est pas un export de Slide.</summary>
    Friend Function Analyser(chemin As String) As ContenuImport
        Dim racine As JObject = JObject.Parse(File.ReadAllText(chemin))
        Dim fichier As JObject = TryCast(racine("parametres"), JObject)
        If fichier Is Nothing Then Throw New FormatException("pas de section « parametres »")
        Dim actuel As JObject = TransfertParametres.Construire(True)
        Dim parametresActuels As JObject = CType(actuel("parametres"), JObject)
        Dim secretsActuels As JObject = CType(actuel("secrets"), JObject)
        Dim c As New ContenuImport With {
            .Chemin = chemin, .Racine = racine,
            .VersionSlide = If(racine("slide") IsNot Nothing, racine("slide").ToString(), ""),
            .Poste = If(racine("poste") IsNot Nothing, racine("poste").ToString(), ""),
            .DateExport = If(racine("date") IsNot Nothing, racine("date").ToString(), "")}

        ' Reglages
        For Each p As JProperty In fichier.Properties()
            Dim nom As String = p.Name
            If TransfertParametres.Exclus.Contains(nom) OrElse TransfertParametres.NomsDesSecrets.Contains(nom, StringComparer.OrdinalIgnoreCase) Then Continue For
            If nom.StartsWith("Minuterie", StringComparison.OrdinalIgnoreCase) Then Continue For          ' (copies internes de la minuterie : reprises avec elle, voir Appliquer)
            Dim propriete As Configuration.SettingsProperty = My.Settings.Properties(nom)
            If propriete Is Nothing Then
                c.NonReconnus.Add(nom)
                Continue For
            End If
            Dim e As New ElementImport With {.Genre = GenreElement.Reglage, .Nom = nom, .Jeton = p.Value, .Propriete = propriete}
            Decouper(TransfertParametres.FamilleDe(nom), e.Onglet, e.Cadre)
            e.ValeurFichier = Texte(p.Value)
            e.ValeurActuelle = Texte(parametresActuels(nom))
            e.Differe = Not String.Equals(e.ValeurFichier, e.ValeurActuelle, StringComparison.Ordinal)
            e.Libelle = nom & " : " & Court(e.ValeurFichier)
            c.Elements.Add(e)
        Next

        ' Mots de passe et cles secretes
        Dim secrets As JObject = TryCast(racine("secrets"), JObject)
        If secrets IsNot Nothing Then
            For Each nom As String In TransfertParametres.NomsDesSecrets
                Dim clair As JToken = secrets(nom)
                If clair Is Nothing Then Continue For
                Dim e As New ElementImport With {.Genre = GenreElement.Secret, .Nom = nom, .Jeton = clair}
                Decouper(TransfertParametres.FamilleDe(nom), e.Onglet, e.Cadre)
                Dim texteFichier As String = CStr(clair)
                Dim texteActuel As String = If(secretsActuels(nom) IsNot Nothing, CStr(secretsActuels(nom)), "")
                e.ValeurFichier = SecretAffichable(nom, texteFichier)
                e.ValeurActuelle = SecretAffichable(nom, texteActuel)
                e.Differe = Not String.Equals(texteFichier, texteActuel, StringComparison.Ordinal)
                e.Libelle = nom & " : " & Court(e.ValeurFichier)
                c.Elements.Add(e)
            Next
        End If

        ' Lignes de la minuterie
        Dim lignes As JArray = TryCast(racine("minuterie"), JArray)
        Dim lignesActuelles As JArray = TryCast(actuel("minuterie"), JArray)
        c.LignesMinuterieActuelles = If(lignesActuelles IsNot Nothing, lignesActuelles.Count, 0)
        If lignes IsNot Nothing Then
            For Each jeton As JToken In lignes
                Dim l As JObject = TryCast(jeton, JObject)
                If l Is Nothing Then Continue For
                c.LignesMinuterieDuFichier += 1
                Dim e As New ElementImport With {.Genre = GenreElement.LigneMinuterie, .Jeton = l}
                Decouper(TransfertParametres.FamilleMinuterie, e.Onglet, e.Cadre)
                Dim actif As Boolean = (ChampLigne(l, "Actif") = "Oui")
                Dim action As String = ChampLigne(l, "Action")
                e.Nom = ChampLigne(l, "Jour") & " " & ChampLigne(l, "Heure") & " — " & ChampLigne(l, "Type") & " : " & If(action.Length > 60, action.Substring(0, 60) & "…", action)
                e.Libelle = If(actif, "", "(inactive) ") & e.Nom
                Dim detail As New Text.StringBuilder()
                For Each colonne As String In TransfertParametres.Colonnes
                    detail.AppendLine(colonne & " : " & ChampLigne(l, colonne))
                Next
                e.ValeurFichier = detail.ToString().TrimEnd()
                Dim presente As Boolean = lignesActuelles IsNot Nothing AndAlso lignesActuelles.Any(Function(x) LignesIdentiques(x, l))
                e.Differe = Not presente
                e.ValeurActuelle = If(presente, "Une ligne identique existe déjà dans la minuterie de ce PC.", "Aucune ligne identique dans la minuterie de ce PC.")
                c.Elements.Add(e)
            Next
        End If

        ' Minuteries enregistrees (data\settings\Minuteries) : une par element, avec toutes ses lignes ; une minuterie de meme nom deja presente sur ce PC n'est jamais ecrasee
        ' (la minuterie importee est alors enregistree sous « Nom (importée date) »).
        Dim minuteries As JArray = TryCast(racine("minuteries"), JArray)
        If minuteries IsNot Nothing Then
            For Each jeton As JToken In minuteries
                Dim m As JObject = TryCast(jeton, JObject)
                If m Is Nothing OrElse m("nom") Is Nothing Then Continue For
                Dim nom As String = CStr(m("nom"))
                Dim lignesDeLaMinuterie As JArray = If(TryCast(m("lignes"), JArray), New JArray())
                Dim e As New ElementImport With {.Genre = GenreElement.MinuterieEnregistree, .Jeton = m, .Nom = nom, .Onglet = "Minuterie", .Cadre = "Minuteries enregistrées"}
                e.Libelle = nom & " (" & lignesDeLaMinuterie.Count & " ligne(s))"
                Dim detail As New Text.StringBuilder()
                For Each l As JToken In lignesDeLaMinuterie.Take(40)
                    detail.AppendLine(MinuteriesEnregistrees.Descriptif(l))
                Next
                If lignesDeLaMinuterie.Count > 40 Then detail.AppendLine("… et " & (lignesDeLaMinuterie.Count - 40) & " autre(s) ligne(s)")
                e.ValeurFichier = detail.ToString().TrimEnd()
                Dim presentes As JArray = MinuteriesEnregistrees.LignesDe(nom)
                e.Differe = (presentes Is Nothing OrElse Not JToken.DeepEquals(presentes, lignesDeLaMinuterie))
                e.ValeurActuelle = If(presentes Is Nothing, "Aucune minuterie enregistrée de ce nom sur ce PC.",
                                      If(e.Differe, "Une minuterie du même nom existe sur ce PC, différente : elle est gardée, celle du fichier est enregistrée sous « " & nom & " (importée ...) ».",
                                         "Une minuterie identique existe déjà sur ce PC."))
                c.Elements.Add(e)
            Next
        End If

        ' Affichage automatique des clips Blink : un element par reglage (place du PIP et cameras regroupees).
        Dim blinkFichier As JObject = TryCast(racine("blink"), JObject)
        If blinkFichier IsNot Nothing Then
            Dim blinkActuel As JObject = BlinkClips.ExporterLesReglages()
            Dim groupes() As (Titre As String, Cles As String()) = {("Affichage automatique", {"Actif"}), ("Intervalle de vérification (secondes)", {"Intervalle"}), ("Plein écran (sinon PIP)", {"PleinEcran"}),
                                                                   ("Coin du PIP", {"Coin"}), ("Taille du PIP (%)", {"Taille"}), ("Volume", {"Volume"}), ("Place libre du PIP", {"PosX", "PosY"}),
                                                                   ("Caméras affichées", {"Cameras", "CamerasChoisies"})}
            For Each g In groupes
                Dim duFichier As New JObject(), actuelles As New JObject()
                For Each cle As String In g.Cles
                    If blinkFichier(cle) IsNot Nothing Then duFichier(cle) = blinkFichier(cle)
                    If blinkActuel(cle) IsNot Nothing Then actuelles(cle) = blinkActuel(cle)
                Next
                If Not duFichier.HasValues Then Continue For
                Dim e As New ElementImport With {.Genre = GenreElement.ReglageBlink, .Nom = g.Titre, .Jeton = duFichier}
                Decouper(TransfertParametres.FamilleBlink, e.Onglet, e.Cadre)
                e.ValeurFichier = TexteBlink(duFichier)
                e.ValeurActuelle = TexteBlink(actuelles)
                e.Differe = Not JToken.DeepEquals(duFichier, actuelles)
                e.Libelle = g.Titre & " : " & Court(e.ValeurFichier)
                c.Elements.Add(e)
            Next
        End If

        ' Petits fichiers de reglages (data\settings)
        Dim fichiersDuFichier As JArray = TryCast(racine("fichiers"), JArray)
        If fichiersDuFichier IsNot Nothing Then
            Dim fichiersActuels As JArray = TryCast(actuel("fichiers"), JArray)
            For Each jeton As JToken In fichiersDuFichier
                Dim f As JObject = TryCast(jeton, JObject)
                If f Is Nothing OrElse f("nom") Is Nothing Then Continue For
                Dim nomFichier As String = CStr(f("nom"))
                Dim role = TransfertParametres.FichiersDeReglages.FirstOrDefault(Function(x) String.Equals(x.Nom, nomFichier, StringComparison.OrdinalIgnoreCase))
                If role.Nom Is Nothing Then Continue For          ' (un fichier qui n'est pas dans la liste : jamais ecrit sur le PC)
                Dim contenuActuel As String = ""
                If fichiersActuels IsNot Nothing Then
                    Dim x As JToken = fichiersActuels.FirstOrDefault(Function(a) String.Equals(CStr(a("nom")), nomFichier, StringComparison.OrdinalIgnoreCase))
                    If x IsNot Nothing Then contenuActuel = CStr(x("contenu"))
                End If
                Dim contenuFichier As String = CStr(If(f("contenu"), ""))
                Dim e As New ElementImport With {.Genre = GenreElement.FichierReglage, .Nom = role.Nom, .Jeton = f}
                Decouper(TransfertParametres.FamilleFichiers, e.Onglet, e.Cadre)
                e.ValeurFichier = If(contenuFichier = "", "(vide)", contenuFichier)
                e.ValeurActuelle = If(contenuActuel = "", "(vide ou absent)", contenuActuel)
                e.Differe = Not String.Equals(contenuFichier, contenuActuel, StringComparison.Ordinal)
                e.Libelle = role.Role & " (" & nomFichier & ")"
                c.Elements.Add(e)
            Next
        End If

        ' Radios
        Dim radios As JArray = TryCast(racine("radios"), JArray)
        Dim radiosActuelles As JArray = TransfertParametres.LireLesRadios()
        c.RadiosActuelles = radiosActuelles.Count
        If radios IsNot Nothing Then
            For Each jeton As JToken In radios
                Dim r As JObject = TryCast(jeton, JObject)
                If r Is Nothing Then Continue For
                c.RadiosDuFichier += 1
                Dim nom As String = CStr(If(r("nom"), ""))
                Dim adresse As String = CStr(If(r("adresse"), ""))
                Dim e As New ElementImport With {.Genre = GenreElement.Radio, .Jeton = r, .Nom = nom}
                Decouper(TransfertParametres.FamilleRadios, e.Onglet, e.Cadre)
                e.Libelle = nom
                e.ValeurFichier = "Nom : " & nom & vbCrLf & "Adresse : " & adresse
                Dim presente As Boolean = radiosActuelles.Any(Function(x) String.Equals(CStr(If(x("adresse"), "")), adresse, StringComparison.OrdinalIgnoreCase))
                e.Differe = Not presente
                e.ValeurActuelle = If(presente, "Une radio de même adresse existe déjà dans la liste de ce PC.", "Cette radio n'est pas dans la liste de ce PC.")
                c.Elements.Add(e)
            Next
        End If

        ' Ordre des onglets / cadres de la fenetre des Options, puis ordre du fichier.
        Dim ordre As New List(Of String)
        For Each ligne As String In TransfertParametres.DefinitionDesFamilles
            ordre.Add(ligne.Substring(0, ligne.LastIndexOf("="c)))
        Next
        ordre.Add(TransfertParametres.FamilleAutres)
        Dim rang As Func(Of ElementImport, Integer) =
            Function(x)
                Dim famille As String = If(x.Genre = GenreElement.LigneMinuterie OrElse x.Genre = GenreElement.MinuterieEnregistree, TransfertParametres.FamilleMinuterie,
                                          If(x.Genre = GenreElement.Radio, TransfertParametres.FamilleRadios,
                                             If(x.Genre = GenreElement.ReglageBlink, TransfertParametres.FamilleBlink,
                                                If(x.Genre = GenreElement.FichierReglage, TransfertParametres.FamilleFichiers, TransfertParametres.FamilleDe(x.Nom)))))
                Dim i As Integer = ordre.IndexOf(famille)
                Return If(i < 0, Integer.MaxValue, i)
            End Function
        c.Elements = c.Elements.Select(Function(x, i) New With {.E = x, .I = i}).OrderBy(Function(x) rang(x.E)).ThenBy(Function(x) x.I).Select(Function(x) x.E).ToList()
        For Each e As ElementImport In c.Elements
            e.Coche = e.Differe          ' (par defaut : ce qui change quelque chose sur ce PC)
        Next
        Return c
    End Function

    Private Function LignesIdentiques(a As JToken, b As JObject) As Boolean
        Dim ao As JObject = TryCast(a, JObject)
        If ao Is Nothing Then Return False
        For Each colonne As String In TransfertParametres.Colonnes
            If Not String.Equals(ChampLigne(ao, colonne), ChampLigne(b, colonne), StringComparison.Ordinal) Then Return False
        Next
        Return True
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Application
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Applique les elements coches. La sauvegarde de l'etat actuel est faite d'abord (data\settings).</summary>
    Friend Function Appliquer(c As ContenuImport, modeMinuterie As ModeImport, modeRadios As ModeImport) As ResultatImport
        Dim res As New ResultatImport()
        Dim coches As List(Of ElementImport) = c.Elements.Where(Function(x) x.Coche).ToList()

        ' Sauvegarde de l'etat actuel (en cas de regret).
        Try
            Directory.CreateDirectory(DossierReglages)
            res.Sauvegarde = Path.Combine(DossierReglages, "Sauvegarde_avant_import_" & Date.Now.ToString("yyyyMMdd_HHmmss") & TransfertParametres.Extension)
            File.WriteAllText(res.Sauvegarde, TransfertParametres.Construire().ToString(Newtonsoft.Json.Formatting.Indented), New Text.UTF8Encoding(False))
        Catch ex As Exception
            res.Sauvegarde = ""
            res.Erreurs.Add("Sauvegarde de l'état actuel impossible : " & ex.Message)
            DiagLog("Import des parametres : sauvegarde impossible (" & ex.Message & ")")
        End Try

        ' Reglages et secrets, un par un.
        For Each e As ElementImport In coches.Where(Function(x) x.Genre = GenreElement.Reglage OrElse x.Genre = GenreElement.Secret)
            Try
                If e.Genre = GenreElement.Secret AndAlso String.Equals(e.Nom, "Blink_Connexion", StringComparison.OrdinalIgnoreCase) Then
                    BlinkApi.ImporterLaConnexion(CStr(e.Jeton))
                    res.Secrets += 1
                ElseIf e.Genre = GenreElement.Secret AndAlso String.Equals(e.Nom, "Nvr_Connexion", StringComparison.OrdinalIgnoreCase) Then
                    NvrIp.ImporterLaConnexion(CStr(e.Jeton))
                    res.Secrets += 1
                ElseIf e.Genre = GenreElement.Secret Then
                    My.Settings(e.Nom) = If(String.Equals(e.Nom, "Tuya_Comptes", StringComparison.OrdinalIgnoreCase), TuyaSlide.ImporterLesComptes(CStr(e.Jeton)), MusiqueDeezer.Proteger(CStr(e.Jeton)))
                    res.Secrets += 1
                ElseIf AppliquerUnReglage(e.Propriete, e.Jeton) Then
                    res.Reglages += 1
                End If
            Catch ex As Exception
                res.Erreurs.Add(e.Nom & " : " & ex.Message)
                DiagLog("Import des parametres : " & e.Nom & " ignore (" & ex.Message & ")")
            End Try
        Next

        ' Reglages Blink et petits fichiers de reglages.
        For Each e As ElementImport In coches.Where(Function(x) x.Genre = GenreElement.ReglageBlink)
            Try
                BlinkClips.ImporterLesReglages(CType(e.Jeton, JObject))
                res.Reglages += 1
            Catch ex As Exception
                res.Erreurs.Add("Blink « " & e.Nom & " » : " & ex.Message)
                DiagLog("Import des parametres : Blink " & e.Nom & " ignore (" & ex.Message & ")")
            End Try
        Next
        For Each e As ElementImport In coches.Where(Function(x) x.Genre = GenreElement.FichierReglage)
            Try
                Dim f As JObject = CType(e.Jeton, JObject)
                Directory.CreateDirectory(DossierReglages)
                File.WriteAllText(Path.Combine(DossierReglages, Path.GetFileName(CStr(f("nom")))), CStr(If(f("contenu"), "")))
                res.Reglages += 1
            Catch ex As Exception
                res.Erreurs.Add("Fichier « " & e.Nom & " » : " & ex.Message)
                DiagLog("Import des parametres : fichier " & e.Nom & " ignore (" & ex.Message & ")")
            End Try
        Next

        ' Minuteries enregistrees : chacune est enregistree dans data\settings\Minuteries (sans rien ecraser).
        For Each e As ElementImport In coches.Where(Function(x) x.Genre = GenreElement.MinuterieEnregistree)
            Try
                Dim m As JObject = CType(e.Jeton, JObject)
                Dim enregistree As String = MinuteriesEnregistrees.Importer(e.Nom, If(TryCast(m("lignes"), JArray), New JArray()))
                res.MinuteriesEnregistrees += 1
                DiagLog("Import des parametres : minuterie enregistree « " & e.Nom & " » reprise sous « " & enregistree & " »")
            Catch ex As Exception
                res.Erreurs.Add("Minuterie enregistrée « " & e.Nom & " » : " & ex.Message)
                DiagLog("Import des parametres : minuterie enregistree « " & e.Nom & " » ignoree (" & ex.Message & ")")
            End Try
        Next

        ' Lignes de la minuterie.
        Dim lignes As New JArray(coches.Where(Function(x) x.Genre = GenreElement.LigneMinuterie).Select(Function(x) x.Jeton))
        Try
            If lignes.Count > 0 Then
                If modeMinuterie = ModeImport.Remplacer Then
                    res.LignesMinuterie = TransfertParametres.EcrireLaMinuterie(lignes, True)
                    ' Toutes les lignes du fichier reprises : les copies de la minuterie gardees dans les reglages suivent.
                    If lignes.Count = c.LignesMinuterieDuFichier Then
                        For Each p As JProperty In CType(c.Racine("parametres"), JObject).Properties()
                            If Not p.Name.StartsWith("Minuterie", StringComparison.OrdinalIgnoreCase) Then Continue For
                            Dim propriete As Configuration.SettingsProperty = My.Settings.Properties(p.Name)
                            If propriete IsNot Nothing AndAlso propriete.PropertyType Is GetType(Specialized.StringCollection) Then AppliquerUnReglage(propriete, p.Value)
                        Next
                    End If
                Else
                    Dim actuelles As JArray = TransfertParametres.LireLaMinuterie()
                    Dim nouvelles As New JArray(lignes.Where(Function(l) Not actuelles.Any(Function(a) LignesIdentiques(a, CType(l, JObject)))))
                    If nouvelles.Count > 0 Then res.LignesMinuterie = TransfertParametres.EcrireLaMinuterie(nouvelles, False)
                End If
            End If
        Catch ex As Exception
            res.Erreurs.Add("Minuterie : " & ex.Message)
            DiagLog("Import des parametres : minuterie ignoree (" & ex.Message & ")")
        End Try
        Try
            My.Settings.Save()
        Catch ex As Exception
            res.Erreurs.Add("Enregistrement des réglages : " & ex.Message)
        End Try

        ' Radios.
        Dim radios As New JArray(coches.Where(Function(x) x.Genre = GenreElement.Radio).Select(Function(x) x.Jeton))
        TransfertParametres.NombreDeRadios = 0
        Try
            If radios.Count > 0 Then
                If modeRadios = ModeImport.Remplacer Then
                    res.Radios = TransfertParametres.EcrireLesRadios(radios)
                Else
                    Dim toutes As JArray = TransfertParametres.LireLesRadios()
                    Dim deja As New HashSet(Of String)(toutes.Select(Function(x) CStr(If(x("adresse"), ""))), StringComparer.OrdinalIgnoreCase)
                    For Each r As JToken In radios
                        If deja.Add(CStr(If(r("adresse"), ""))) Then
                            toutes.Add(r)
                            res.Radios += 1
                        End If
                    Next
                    If res.Radios > 0 Then TransfertParametres.EcrireLesRadios(toutes)
                End If
                TransfertParametres.NombreDeRadios = res.Radios
            End If
        Catch ex As Exception
            res.Erreurs.Add("Radios : " & ex.Message)
            DiagLog("Import des parametres : radios ignorees (" & ex.Message & ")")
        End Try

        DiagLog("Import des parametres : " & c.Chemin & " : " & res.Reglages & " reglage(s), " & res.Secrets & " secret(s), " & res.LignesMinuterie & " ligne(s) de minuterie (" & modeMinuterie.ToString() & "), " &
                res.Radios & " radio(s) (" & modeRadios.ToString() & "), " & res.Erreurs.Count & " erreur(s)")
        Return res
    End Function

    ''' <summary>Pose la valeur du fichier dans un reglage, selon son type. Rend faux pour un type que l'import ne gere pas.</summary>
    Private Function AppliquerUnReglage(p As Configuration.SettingsProperty, jeton As JToken) As Boolean
        If p.PropertyType Is GetType(Specialized.StringCollection) Then
            Dim collection As New Specialized.StringCollection()
            For Each element As JToken In CType(jeton, JArray)
                collection.Add(CStr(element))
            Next
            My.Settings(p.Name) = collection
        ElseIf p.PropertyType Is GetType(Boolean) Then
            My.Settings(p.Name) = CBool(jeton)
        ElseIf p.PropertyType Is GetType(Integer) Then
            My.Settings(p.Name) = CInt(jeton)
        ElseIf p.PropertyType Is GetType(Double) Then
            My.Settings(p.Name) = CDbl(jeton)
        ElseIf p.PropertyType Is GetType(String) Then
            My.Settings(p.Name) = CStr(jeton)
        Else
            Return False
        End If
        Return True
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Commande du menu (clic droit > Parametres > Importer)
    ' ------------------------------------------------------------------------------------------

    Friend Sub Commande(proprietaire As IWin32Window)
        Try
            Using d As New OpenFileDialog()
                d.Title = "Importer des paramètres"
                d.Filter = "Paramètres Slide (*" & TransfertParametres.Extension & ")|*" & TransfertParametres.Extension & "|Tous les fichiers|*.*"
                Try : Directory.CreateDirectory(DossierReglages) : Catch ex As Exception : End Try
                d.InitialDirectory = DossierReglages          ' (data\settings : la ou Slide range ses reglages)
                If d.ShowDialog(proprietaire) <> DialogResult.OK Then Exit Sub
                DiagLog("Import des parametres : fichier choisi : " & d.FileName)

                Dim contenu As ContenuImport
                Try
                    contenu = Analyser(d.FileName)
                Catch ex As Exception
                    DiagLog("Import des parametres : fichier non reconnu (" & ex.Message & ")")
                    MessageBox.Show(proprietaire, "Ce fichier n'est pas un export de paramètres Slide." & vbCrLf & ex.Message, "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                    Exit Sub
                End Try
                If contenu.Elements.Count = 0 Then
                    MessageBox.Show(proprietaire, "Ce fichier ne contient rien que cette version de Slide puisse reprendre.", "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Information)
                    Exit Sub
                End If
                DiagLog("Import des parametres : " & contenu.Elements.Count & " element(s) dans le fichier, " & contenu.Elements.Where(Function(x) x.Differe).Count() & " different(s) de ce PC, " & contenu.NonReconnus.Count & " non reconnu(s)")

                Dim modeMinuterie As ModeImport, modeRadios As ModeImport
                If Not FormImportParametres.Demander(proprietaire, contenu, modeMinuterie, modeRadios) Then Exit Sub

                Dim res As ResultatImport = Appliquer(contenu, modeMinuterie, modeRadios)
                Dim rapport As New Text.StringBuilder()
                rapport.AppendLine("Import terminé :")
                rapport.AppendLine("  • " & res.Reglages & " réglage(s) repris")
                If res.Secrets > 0 Then rapport.AppendLine("  • " & res.Secrets & " mot(s) de passe / clé(s) secrète(s) repris")
                If res.LignesMinuterie > 0 Then rapport.AppendLine("  • " & res.LignesMinuterie & " ligne(s) de minuterie " & If(modeMinuterie = ModeImport.Remplacer, "(minuterie de ce PC remplacée)", "ajoutée(s)"))
                If res.Radios > 0 Then rapport.AppendLine("  • " & res.Radios & " radio(s) " & If(modeRadios = ModeImport.Remplacer, "(liste de ce PC remplacée)", "ajoutée(s)"))
                If res.Sauvegarde <> "" Then rapport.AppendLine().AppendLine("Sauvegarde de l'état d'avant :").AppendLine(res.Sauvegarde)
                If res.Erreurs.Count > 0 Then rapport.AppendLine().AppendLine("Éléments non repris :").AppendLine(String.Join(vbCrLf, res.Erreurs.Select(Function(x) "  • " & x)))
                rapport.AppendLine().AppendLine("Pensez à vérifier les chemins (dossiers de photos et de films), repris tels quels.")
                rapport.AppendLine().Append("Relancer Slide maintenant pour que tout soit pris en compte ?")
                If MessageBox.Show(proprietaire, rapport.ToString(), "Importer les paramètres", MessageBoxButtons.YesNo, If(res.Erreurs.Count > 0, MessageBoxIcon.Warning, MessageBoxIcon.Information)) = DialogResult.Yes Then
                    Try
                        Dim exe As String = Path.Combine(AppPath, "slide.exe")
                        Process.Start(New ProcessStartInfo(exe) With {.UseShellExecute = True, .WorkingDirectory = AppPath})
                        End
                    Catch ex As Exception
                        DiagLog("Import des parametres : relance impossible (" & ex.Message & ")")
                    End Try
                End If
            End Using
        Catch ex As Exception
            ' Rien ne doit se passer en silence apres le choix du fichier.
            DiagLog("Import des parametres : erreur imprevue (" & ex.GetType().Name & " : " & ex.Message & ") " & ex.StackTrace)
            MessageBox.Show(proprietaire, "L'import a échoué : " & ex.Message, "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

End Module
