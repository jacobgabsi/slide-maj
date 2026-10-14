Option Strict On
Option Explicit On

Imports System.IO
Imports Newtonsoft.Json.Linq

''' <summary>
''' Plusieurs minuteries enregistrees (demande de Jacob, 28/10/2026) : on enregistre la minuterie actuelle (toutes ses lignes ou seulement les lignes choisies) sous un nom, puis on charge a la main
''' celle que l'on veut (toutes ses lignes ou seulement les lignes choisies, en REMPLACANT la minuterie actuelle ou en les AJOUTANT).
'''
''' Une minuterie = un fichier « Nom.minuterie » (JSON) dans data\settings\Minuteries : propre au poste (jamais copie par les mises a jour). Les lignes ont le format de l'export des parametres
''' (Actif, Jour, Heure, Type, Action, Play, Durée, Volume, tri, Argument). Avant chaque chargement, la minuterie actuelle est enregistree sous « Avant chargement de ... ».
''' </summary>
Friend Module MinuteriesEnregistrees

    Friend Const Extension As String = ".minuterie"

    Friend Function Dossier() As String
        Return Path.Combine(DossierReglages, "Minuteries")
    End Function

    ' Minuteries COLLECTIVES (29/10/2026, Jacob) : data\minuteries collectives, un dossier public (sans rien de confidentiel) que les mises a jour distribuent a tous les postes. On y enregistre des minuteries « type »
    ' et on les charge sur ce PC pour les modifier. Memes fichiers « Nom.minuterie » que les minuteries de ce PC.
    Friend Function DossierCollectif() As String
        Return Path.Combine(AppPath, "data", "minuteries collectives")
    End Function

    Friend Class Info
        Friend Nom As String = ""
        Friend Chemin As String = ""
        Friend Enregistree As Date
        Friend NombreDeLignes As Integer
    End Class

    Private Function CheminDe(nom As String, Optional dossierVoulu As String = Nothing) As String
        Dim propre As String = nom
        For Each c As Char In Path.GetInvalidFileNameChars()
            propre = propre.Replace(c, "_"c)
        Next
        Return Path.Combine(If(dossierVoulu, Dossier()), propre.Trim() & Extension)
    End Function

    ''' <summary>Les minuteries enregistrees, la plus recente d'abord.</summary>
    Friend Function Lister(Optional dossierVoulu As String = Nothing) As List(Of Info)
        Dim liste As New List(Of Info)
        Dim dossierLu As String = If(dossierVoulu, Dossier())
        Try
            Directory.CreateDirectory(dossierLu)
            For Each f As String In Directory.GetFiles(dossierLu, "*" & Extension)
                Try
                    Dim j As JObject = JObject.Parse(File.ReadAllText(f, Text.Encoding.UTF8))
                    Dim quand As Date = File.GetLastWriteTime(f)
                    Date.TryParse(CStr(j("enregistree")), Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.RoundtripKind, quand)
                    liste.Add(New Info With {.Nom = If(CStr(j("nom")), Path.GetFileNameWithoutExtension(f)), .Chemin = f, .Enregistree = quand,
                                             .NombreDeLignes = If(TryCast(j("lignes"), JArray) Is Nothing, 0, CType(j("lignes"), JArray).Count)})
                Catch ex As Exception
                    DiagLog("Minuteries enregistrees : " & Path.GetFileName(f) & " illisible (" & ex.Message & ")")
                End Try
            Next
        Catch ex As Exception
            DiagLog("Minuteries enregistrees : liste impossible (" & ex.Message & ")")
        End Try
        Return liste.OrderByDescending(Function(i) i.Enregistree).ToList()
    End Function

    Private Function UneLigne(ligne As DataGridViewRow, suffixe As String, actif As Boolean) As JObject
        Dim v As Func(Of String, String) = Function(colonne As String) Convert.ToString(ligne.Cells(colonne & suffixe).FormattedValue)
        Dim heure As String = v("Heure")
        Return New JObject From {
            {"Actif", actif}, {"Jour", v("Jour")}, {"Heure", If(heure.Length > 5, heure.Substring(0, 5), heure)}, {"Type", v("Type")}, {"Action", v("Action")},
            {"Play", v("Play")}, {"Durée", v("Durée")}, {"Volume", v("Volume")}, {"tri", v("tri")}, {"Argument", Convert.ToString(ligne.Cells("Argument" & suffixe).Value)}}
    End Function

    ''' <summary>Les lignes de la minuterie telle qu'elle est a l'ecran (actives, puis inactives), meme celles pas encore enregistrees par OK.</summary>
    Friend Function LignesActuelles() As JArray
        Dim lignes As New JArray()
        For Each grille As DataGridView In {Form4.DataGridView1, Form4.DataGridViewTuya}          ' (tableau principal, puis minuterie optionnelle : ses lignes portent sa marque dans leurs reglages)
            For Each ligne As DataGridViewRow In grille.Rows
                If ligne.IsNewRow Then Continue For
                Dim une As JObject = UneLigne(ligne, "", True)
                une("Argument") = TableauTuya.ArgumentAvecMarque(Convert.ToString(une("Argument")), ReferenceEquals(grille, Form4.DataGridViewTuya), String.Equals(Convert.ToString(une("Type")).Trim(), TuyaSlide.TypeMinuterie, StringComparison.OrdinalIgnoreCase))
                lignes.Add(une)
            Next
        Next
        For Each ligne As DataGridViewRow In Form4.DataGridView2.Rows
            If ligne.IsNewRow Then Continue For
            lignes.Add(UneLigne(ligne, "2", False))
        Next
        Return lignes
    End Function

    ''' <summary>Une ligne en clair pour les listes de choix : « Vendredi 17:17 · Film · C:\...\film.mp4 » (+ « (inactive) »).</summary>
    Friend Function Descriptif(ligne As JToken) As String
        Dim o As JObject = TryCast(ligne, JObject)
        If o Is Nothing Then Return ""
        Dim t As Func(Of String, String) = Function(c As String) If(o(c) Is Nothing, "", CStr(o(c)))
        Dim actif As Boolean = (o("Actif") Is Nothing OrElse CBool(o("Actif")))
        Return t("Jour") & " " & t("Heure") & "  ·  " & t("Type") & "  ·  " & t("Action") & If(actif, "", "  (inactive)")
    End Function

    ''' <summary>Les lignes SANS identifiant ni mot de passe : une adresse avec un compte (« http://utilisateur:mot-de-passe@... »), un « password= », « token= », « key= », « secret= » (dans Action ou Argument) font
    ''' ecarter la ligne. Sert aux minuteries collectives, qui sont publiees sur un depot public.</summary>
    Friend Function SansSecrets(lignes As JArray) As JArray
        Dim sures As New JArray()
        Dim motif As New System.Text.RegularExpressions.Regex("://[^/\s!]*:[^/\s!@]*@|(password|passwd|pwd|token|apikey|api_key|secret|mdp|motdepasse)\s*[=:]", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
        For Each l As JToken In lignes
            Dim texte As String = If(l("Action") Is Nothing, "", CStr(l("Action"))) & " " & If(l("Argument") Is Nothing, "", CStr(l("Argument")))
            If motif.IsMatch(texte) Then
                DiagLog("Minuteries collectives : une ligne (" & Descriptif(l).Split("·"c)(0).Trim() & ") ecartee : elle contient un identifiant ou un mot de passe")
            Else
                sures.Add(l)
            End If
        Next
        Return sures
    End Function

    ''' <summary>Enregistre ces lignes sous ce nom (le fichier du meme nom est remplace).</summary>
    Friend Function Enregistrer(nom As String, lignes As JArray, Optional dossierVoulu As String = Nothing) As Boolean
        Try
            Dim dossierEcrit As String = If(dossierVoulu, Dossier())
            Directory.CreateDirectory(dossierEcrit)
            Dim racine As New JObject From {{"nom", nom}, {"enregistree", Date.Now.ToString("o", Globalization.CultureInfo.InvariantCulture)}, {"lignes", lignes}}
            Dim chemin As String = CheminDe(nom, dossierEcrit)
            File.WriteAllText(chemin & ".tmp", racine.ToString(Newtonsoft.Json.Formatting.Indented), New Text.UTF8Encoding(False))
            File.Move(chemin & ".tmp", chemin, True)
            DiagLog("Minuteries enregistrees : « " & nom & " » enregistree (" & lignes.Count & " ligne(s))")
            Return True
        Catch ex As Exception
            DiagLog("Minuteries enregistrees : « " & nom & " » impossible (" & ex.Message & ")")
            MessageBox.Show("Enregistrement impossible : " & ex.Message, "Minuteries", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End Try
    End Function

    ''' <summary>Toutes les minuteries enregistrees, pour l'export des parametres : « nom », « enregistree », « lignes ».</summary>
    Friend Function Exporter() As JArray
        Dim tableau As New JArray()
        For Each i As Info In Lister()
            Try
                Dim j As JObject = JObject.Parse(File.ReadAllText(i.Chemin, Text.Encoding.UTF8))
                tableau.Add(New JObject From {{"nom", i.Nom}, {"enregistree", If(j("enregistree") Is Nothing, "", CStr(j("enregistree")))}, {"lignes", If(TryCast(j("lignes"), JArray), New JArray())}})
            Catch ex As Exception
                DiagLog("Export des parametres : minuterie enregistree « " & i.Nom & " » illisible (" & ex.Message & ")")
            End Try
        Next
        Return tableau
    End Function

    ''' <summary>Les lignes d'une minuterie enregistree de ce PC, ou Nothing si elle n'existe pas (pour comparer avec celle d'un fichier d'import).</summary>
    Friend Function LignesDe(nom As String) As JArray
        Try
            If Not Existe(nom) Then Return Nothing
            Dim j As JObject = JObject.Parse(File.ReadAllText(CheminDe(nom), Text.Encoding.UTF8))
            Return If(TryCast(j("lignes"), JArray), New JArray())
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    ''' <summary>Importe une minuterie d'un fichier d'export : sans doublon de nom, elle est enregistree telle quelle ; si une minuterie du meme nom existe deja sur ce PC (differente),
    ''' celle du fichier est enregistree sous « Nom (importée jj-MM-aaaa) » : rien n'est ecrase. Rend le nom sous lequel elle est enregistree.</summary>
    Friend Function Importer(nom As String, lignes As JArray) As String
        Dim voulu As String = nom
        Dim existantes As JArray = LignesDe(nom)
        If existantes IsNot Nothing Then
            If JToken.DeepEquals(existantes, lignes) Then Return nom          ' (deja la, identique)
            voulu = nom & " (importée " & Date.Now.ToString("dd-MM-yyyy") & ")"
            Dim n As Integer = 2
            While Existe(voulu)
                voulu = nom & " (importée " & Date.Now.ToString("dd-MM-yyyy") & " " & n & ")"
                n += 1
            End While
        End If
        Enregistrer(voulu, lignes)
        Return voulu
    End Function

    Friend Function Existe(nom As String, Optional dossierVoulu As String = Nothing) As Boolean
        Return File.Exists(CheminDe(nom, dossierVoulu))
    End Function

    Friend Function Lignes(info As Info) As JArray
        Dim j As JObject = JObject.Parse(File.ReadAllText(info.Chemin, Text.Encoding.UTF8))
        Return If(TryCast(j("lignes"), JArray), New JArray())
    End Function

    Friend Sub Supprimer(info As Info)
        File.Delete(info.Chemin)
        DiagLog("Minuteries enregistrees : « " & info.Nom & " » supprimee")
    End Sub

    Friend Function Renommer(info As Info, nouveauNom As String) As Boolean
        Try
            If Existe(nouveauNom) Then Return False
            Dim lignes As JArray = MinuteriesEnregistrees.Lignes(info)
            If Not Enregistrer(nouveauNom, lignes) Then Return False
            File.Delete(info.Chemin)
            Return True
        Catch ex As Exception
            DiagLog("Minuteries enregistrees : renommage impossible (" & ex.Message & ")")
            Return False
        End Try
    End Function

    Private Function MemeLigne(a As JToken, b As JToken) As Boolean
        For Each c As String In {"Jour", "Heure", "Type", "Action", "Argument"}
            If Not String.Equals(If(a(c) Is Nothing, "", CStr(a(c))), If(b(c) Is Nothing, "", CStr(b(c))), StringComparison.OrdinalIgnoreCase) Then Return False
        Next
        Return True
    End Function

    ''' <summary>Charge ces lignes : <paramref name="remplacer"/> = elles remplacent la minuterie actuelle ; sinon elles s'ajoutent (celles deja presentes ne sont pas doublees).

    ' « Annuler » dans la fenetre Option doit aussi annuler le chargement d'une minuterie : elle est appliquee tout de suite (base et minuterie en cours), la minuterie d'avant l'ouverture est donc gardee ici.
    Private _avantOuverture As JArray
    Friend ChargeeDepuisLOuverture As Boolean

    ''' <summary>La minuterie telle qu'elle est a l'ouverture de la fenetre Option.</summary>
    Friend Sub MemoriserLOuverture()
        Try
            _avantOuverture = LignesActuelles()
            ChargeeDepuisLOuverture = False
        Catch ex As Exception
            _avantOuverture = Nothing
        End Try
    End Sub

    ''' <summary>Fenetre Option annulee ou fermee sans OK : si une minuterie a ete chargee depuis son ouverture, celle d'avant est remise (tableaux, base et minuterie en cours).</summary>
    Friend Sub RestaurerLOuverture()
        If Not ChargeeDepuisLOuverture OrElse _avantOuverture Is Nothing Then Exit Sub
        ChargeeDepuisLOuverture = False
        Try
            TransfertParametres.EcrireLaMinuterie(_avantOuverture, True)
            ChargeDataMinuterieActive()
            UpdateData()
            DiagLog("Minuteries enregistrees : annulation, la minuterie d'avant l'ouverture des reglages est remise (" & _avantOuverture.Count & " ligne(s))")
        Catch ex As Exception
            DiagLog("Minuteries enregistrees : annulation impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>OK dans la fenetre Option : ce qui a ete charge est gardé.</summary>
    Friend Sub GarderLaMinuterie()
        ChargeeDepuisLOuverture = False
    End Sub
    ''' La minuterie actuelle est d'abord enregistree sous « Avant chargement de ... ». La minuterie est appliquee tout de suite (tableau, minuterie en cours, base).</summary>
    Friend Const CategoriePrincipale As String = "principale"
    Friend Const CategorieOptionnelle As String = "optionnelle"
    Friend Const CategorieInactive As String = "inactive"

    ''' <summary>De quelle minuterie est cette ligne : la principale, l'optionnelle (marque « !/Groupe:Tuya ») ou les lignes inactives ?</summary>
    Friend Function CategorieDeLaLigne(l As JToken) As String
        If l("Actif") IsNot Nothing AndAlso Not CBool(l("Actif")) Then Return CategorieInactive
        If TableauTuya.EstDuTableau(If(l("Type") Is Nothing, "", CStr(l("Type"))), If(l("Argument") Is Nothing, "", CStr(l("Argument")))) Then Return CategorieOptionnelle
        Return CategoriePrincipale
    End Function

    ''' <summary>Les minuteries (principale, optionnelle, inactive) dont ces lignes font partie, dans cet ordre.</summary>
    Friend Function CategoriesDe(lignes As JArray) As List(Of String)
        Dim presentes As New HashSet(Of String)
        For Each l As JToken In lignes
            presentes.Add(CategorieDeLaLigne(l))
        Next
        Return New List(Of String) From {CategoriePrincipale, CategorieOptionnelle, CategorieInactive}.Where(Function(c) presentes.Contains(c)).ToList()
    End Function

    ''' <summary>« la minuterie principale, la minuterie optionnelle » pour le message qui annonce ce qui est remplace.</summary>
    Friend Function NomsDesMinuteries(categories As List(Of String)) As String
        Dim noms As New List(Of String)
        For Each c As String In categories
            Select Case c
                Case CategoriePrincipale : noms.Add("la minuterie principale")
                Case CategorieOptionnelle : noms.Add("la minuterie optionnelle")
                Case CategorieInactive : noms.Add("les lignes inactives")
            End Select
        Next
        Return If(noms.Count = 0, "aucune", String.Join(", ", noms))
    End Function

    Friend DernierAjout As Integer          ' (en ajout : lignes vraiment ajoutees / deja presentes, pour le message)
    Friend DejaPresentes As Integer

    Friend Function Charger(lignes As JArray, remplacer As Boolean, nomSource As String) As Boolean
        DernierAjout = lignes.Count
        DejaPresentes = 0
        Try
            Dim actuelles As JArray = LignesActuelles()
            If actuelles.Count > 0 Then Enregistrer("Avant chargement de " & nomSource & " (" & Date.Now.ToString("dd-MM-yyyy HH-mm") & ")", actuelles)
            Dim finale As JArray
            If remplacer Then
                ' Seules les minuteries presentes dans les lignes chargees sont remplacees : des lignes de la minuterie optionnelle ne touchent pas
                ' la minuterie principale ni les lignes inactives, et inversement.
                Dim concernees As List(Of String) = CategoriesDe(lignes)
                finale = New JArray(actuelles.Where(Function(a) Not concernees.Contains(CategorieDeLaLigne(a))))
                For Each l As JToken In lignes
                    finale.Add(l)
                Next
            Else
                finale = New JArray(actuelles)
                For Each l As JToken In lignes
                    If Not actuelles.Any(Function(a) MemeLigne(a, l)) Then finale.Add(l)
                Next
                DernierAjout = finale.Count - actuelles.Count
                DejaPresentes = lignes.Count - DernierAjout
            End If
            ' Les appareils Tuya des lignes chargees sont coches directement (sinon « commande inconnue » a chaque passage) ; message seulement si un nom n'existe pas.
            Dim introuvablesTuya As List(Of String) = TuyaSlide.CocherLesAppareilsDesLignes(lignes.OfType(Of JObject)().
                Where(Function(l) String.Equals(Convert.ToString(l("Type")).Trim(), TuyaSlide.TypeMinuterie, StringComparison.OrdinalIgnoreCase)).
                Select(Function(l) New KeyValuePair(Of String, String)(Convert.ToString(l("Action")), Convert.ToString(l("Argument")))))
            TransfertParametres.EcrireLaMinuterie(finale, True)
            ChargeDataMinuterieActive()          ' (les tableaux relisent la base)
            UpdateData()                         ' (la minuterie en cours et les copies des reglages suivent)
            ChargeeDepuisLOuverture = True
            DiagLog("Minuteries enregistrees : « " & nomSource & " » chargee (" & lignes.Count & " ligne(s), " & If(remplacer, "en remplacement", "en ajout") & ")")
            If introuvablesTuya.Count > 0 Then
                DiagLog("Minuteries enregistrees : commandes Tuya inexistantes sur ce poste : " & String.Join(" ; ", introuvablesTuya))
                MessageBox.Show("Ces commandes Tuya de la minuterie chargée n'existent pas sur ce poste (aucun appareil, scène ou groupe de ce nom) :" & vbCrLf & vbCrLf &
                                String.Join(vbCrLf, introuvablesTuya.Select(Function(n) "   • " & n)) & vbCrLf & vbCrLf &
                                "Elles ne partiront pas. Actualisez la liste des appareils dans Option > Sources > Tuya.", "Minuteries", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            End If
            Return True
        Catch ex As Exception
            DiagLog("Minuteries enregistrees : chargement impossible (" & ex.Message & ")")
            MessageBox.Show("Chargement impossible : " & ex.Message, "Minuteries", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End Try
    End Function

End Module
