Option Strict On
Option Explicit On

Imports System.Data.OleDb
Imports System.IO
Imports Newtonsoft.Json.Linq

''' <summary>
''' Export / import des parametres de Slide et de sa minuterie (clic droit > Parametres), pour configurer un autre PC avec les
''' memes reglages (demande de Jacob, 16/10/2026).
'''
''' Le fichier (.slideconf, du JSON) contient TOUS les reglages de Slide (My.Settings), sans exception (16/10/2026 : "exporte tous les
''' reglages et mots de passe sans exceptions"), et TOUTES les lignes de la minuterie (table MinuterieActive, lignes inactives comprises).
''' Les mots de passe (Deezer, comptes IPTV) sont chiffres pour le poste (DPAPI) : ils sont donc ECRITS EN CLAIR dans le fichier (section
''' "secrets"), puis rechiffres pour le nouveau poste a l'import. Le fichier doit etre garde en lieu sur et supprime apres usage.
''' Ne sont PAS transferes : la base slide.accdb elle-meme, les listes IPTV, les niveaux sonores, gmail.txt (data\settings). Les chemins
''' (dossiers de photos, films) sont repris tels quels.
''' L'import sauvegarde d'abord les reglages actuels dans data\settings.
''' </summary>
Friend Module TransfertParametres

    ''' <summary>Reglages jamais exportes ni importes : AUCUN (demande de Jacob).</summary>
    Private ReadOnly Exclus As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>Reglages gardes CHIFFRES pour le poste (DPAPI, voir MusiqueDeezer.Proteger) : exportes en clair, rechiffres a l'import.</summary>
    Private ReadOnly NomsDesSecrets As String() = {"DeezerMotDePasse", "IPTV_MotDePasse1", "IPTV_MotDePasse2", "IPTV_MotDePasse3"}

    Private Const Extension As String = ".slideconf"

    ' ------------------------------------------------------------------------------------------
    ' Export
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les reglages de la minuterie (copie dans les reglages) : exclus de l'export quand la minuterie n'est pas voulue.</summary>
    Private Function EstLaMinuterieDesReglages(p As Configuration.SettingsProperty) As Boolean
        Return p.Name.StartsWith("Minuterie", StringComparison.OrdinalIgnoreCase) AndAlso p.PropertyType Is GetType(Specialized.StringCollection)
    End Function

    Friend Function Construire(Optional avecMinuterie As Boolean = True) As JObject
        Dim racine As New JObject()
        racine("slide") = SlideVersion
        racine("poste") = Environment.MachineName
        racine("date") = Date.Now.ToString("yyyy-MM-dd HH:mm:ss")

        Dim parametres As New JObject()
        Dim secrets As New JObject()
        For Each p As Configuration.SettingsProperty In My.Settings.Properties
            If Exclus.Contains(p.Name) Then Continue For
            If NomsDesSecrets.Contains(p.Name, StringComparer.OrdinalIgnoreCase) Then
                secrets(p.Name) = MusiqueDeezer.Deproteger(CStr(If(My.Settings(p.Name), "")))        ' ("" si le mot de passe n'est pas lisible ici)
                Continue For
            End If
            If Not avecMinuterie AndAlso EstLaMinuterieDesReglages(p) Then Continue For
            Dim valeur As Object = My.Settings(p.Name)
            If valeur Is Nothing Then Continue For
            If TypeOf valeur Is Specialized.StringCollection Then
                Dim tableau As New JArray()
                For Each s As String In CType(valeur, Specialized.StringCollection)
                    tableau.Add(s)
                Next
                parametres(p.Name) = tableau
            ElseIf TypeOf valeur Is Boolean Then
                parametres(p.Name) = CBool(valeur)
            ElseIf TypeOf valeur Is Integer Then
                parametres(p.Name) = CInt(valeur)
            ElseIf TypeOf valeur Is String Then
                parametres(p.Name) = CStr(valeur)
            End If
        Next
        racine("parametres") = parametres
        racine("secrets") = secrets
        If avecMinuterie Then racine("minuterie") = LireLaMinuterie()
        Return racine
    End Function

    ''' <summary>Toutes les lignes de la table MinuterieActive (base), ou a defaut vide : la minuterie est aussi dans les reglages.</summary>
    Private Function LireLaMinuterie() As JArray
        Dim lignes As New JArray()
        Try
            Using connexion As New OleDbConnection(OLEDBPROVIDER & DatabasePath)
                connexion.Open()
                Using commande As New OleDbCommand("SELECT * FROM MinuterieActive", connexion)
                    Using lecteur As OleDbDataReader = commande.ExecuteReader()
                        While lecteur.Read()
                            Dim l As New JObject()
                            For Each colonne As String In Colonnes
                                Dim v As Object = lecteur(colonne)
                                If colonne = "Actif" Then
                                    l(colonne) = If(v Is DBNull.Value, False, CBool(v))
                                Else
                                    l(colonne) = If(v Is DBNull.Value, "", Convert.ToString(v))
                                End If
                            Next
                            lignes.Add(l)
                        End While
                    End Using
                End Using
            End Using
        Catch ex As Exception
            DiagLog("Export des parametres : minuterie illisible dans la base (" & ex.Message & ")")
        End Try
        Return lignes
    End Function

    Private ReadOnly Colonnes As String() = {"Actif", "Jour", "Heure", "Type", "Action", "Play", "Durée", "Volume", "tri", "Argument"}

    Friend Function NomParDefaut() As String
        Return "SlideParametres_" & Environment.MachineName & "_" & Date.Now.ToString("yyyyMMdd") & Extension
    End Function

    ''' <summary>Ecrit le fichier d'export. Rend le nombre de reglages et de lignes de minuterie ecrits.</summary>
    Friend Function Exporter(chemin As String, avecMinuterie As Boolean, ByRef reglages As Integer, ByRef lignes As Integer) As Boolean
        Try
            Dim racine As JObject = Construire(avecMinuterie)
            reglages = CType(racine("parametres"), JObject).Count
            lignes = If(avecMinuterie, CType(racine("minuterie"), JArray).Count, 0)
            File.WriteAllText(chemin, racine.ToString(Newtonsoft.Json.Formatting.Indented), New Text.UTF8Encoding(False))
            DiagLog("Export des parametres : " & chemin & " (" & reglages & " reglages, " & lignes & " lignes de minuterie)")
            Return True
        Catch ex As Exception
            DiagLog("Export des parametres : " & ex.Message)
            MessageBox.Show("L'export a échoué : " & ex.Message, "Exporter les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Import
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Applique un fichier d'export. Rend le nombre de reglages et de lignes de minuterie importes (-1 : echec).</summary>
    Friend Function Importer(chemin As String, avecMinuterie As Boolean, ByRef reglages As Integer, ByRef lignes As Integer, ByRef sauvegarde As String) As Boolean
        reglages = 0 : lignes = 0 : sauvegarde = ""
        Try
            Dim racine As JObject = JObject.Parse(File.ReadAllText(chemin))
            Dim parametres As JObject = TryCast(racine("parametres"), JObject)
            If parametres Is Nothing Then
                MessageBox.Show("Ce fichier n'est pas un export de paramètres Slide.", "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Return False
            End If

            ' Sauvegarde de l'etat actuel (en cas de regret) : data\settings.
            Try
                Dim dossier As String = DossierReglages
                Directory.CreateDirectory(dossier)
                sauvegarde = Path.Combine(dossier, "Sauvegarde_avant_import_" & Date.Now.ToString("yyyyMMdd_HHmmss") & Extension)
                File.WriteAllText(sauvegarde, Construire().ToString(Newtonsoft.Json.Formatting.Indented), New Text.UTF8Encoding(False))
            Catch ex As Exception
                sauvegarde = ""
                DiagLog("Import des parametres : sauvegarde impossible (" & ex.Message & ")")
            End Try

            ' Mots de passe : le fichier les porte en clair, ils sont rechiffres pour ce poste.
            Dim secrets As JObject = TryCast(racine("secrets"), JObject)
            If secrets IsNot Nothing Then
                For Each nom As String In NomsDesSecrets
                    Dim clair As JToken = secrets(nom)
                    If clair Is Nothing Then Continue For
                    Try
                        My.Settings(nom) = MusiqueDeezer.Proteger(CStr(clair))
                        reglages += 1
                    Catch ex As Exception
                        DiagLog("Import des parametres : " & nom & " ignore (" & ex.Message & ")")
                    End Try
                Next
            End If

            For Each p As Configuration.SettingsProperty In My.Settings.Properties
                If Exclus.Contains(p.Name) Then Continue For
                If NomsDesSecrets.Contains(p.Name, StringComparer.OrdinalIgnoreCase) Then Continue For
                If Not avecMinuterie AndAlso EstLaMinuterieDesReglages(p) Then Continue For
                Dim jeton As JToken = parametres(p.Name)
                If jeton Is Nothing Then Continue For
                Try
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
                    ElseIf p.PropertyType Is GetType(String) Then
                        My.Settings(p.Name) = CStr(jeton)
                    Else
                        Continue For
                    End If
                    reglages += 1
                Catch ex As Exception
                    DiagLog("Import des parametres : " & p.Name & " ignore (" & ex.Message & ")")
                End Try
            Next
            My.Settings.Save()

            Dim minuterie As JArray = TryCast(racine("minuterie"), JArray)
            If avecMinuterie AndAlso minuterie IsNot Nothing AndAlso minuterie.Count > 0 Then lignes = EcrireLaMinuterie(minuterie)
            DiagLog("Import des parametres : " & chemin & " (" & reglages & " reglages, " & lignes & " lignes de minuterie)")
            Return True
        Catch ex As Exception
            DiagLog("Import des parametres : " & ex.Message)
            MessageBox.Show("L'import a échoué : " & ex.Message, "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return False
        End Try
    End Function

    ''' <summary>Remplace les lignes de la table MinuterieActive par celles du fichier.</summary>
    Private Function EcrireLaMinuterie(lignes As JArray) As Integer
        Dim ecrites As Integer = 0
        BaseMinuterie.PreparerLaBase(DatabasePath)          ' (colonne Argument en texte long)
        Using connexion As New OleDbConnection(OLEDBPROVIDER & DatabasePath)
            connexion.Open()
            Using vider As New OleDbCommand("DELETE FROM MinuterieActive", connexion)
                vider.ExecuteNonQuery()
            End Using
            For Each jeton As JToken In lignes
                Dim l As JObject = TryCast(jeton, JObject)
                If l Is Nothing Then Continue For
                Using ajout As New OleDbCommand("INSERT INTO MinuterieActive ([Actif], [Jour], [Heure], [Type], [Action], [Play], [Durée], [Volume], [tri], [Argument]) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)", connexion)
                    For Each colonne As String In Colonnes
                        If colonne = "Actif" Then
                            ajout.Parameters.AddWithValue("@" & colonne, If(l(colonne) Is Nothing, False, CBool(l(colonne))))
                        Else
                            ajout.Parameters.AddWithValue("@" & colonne, If(l(colonne) Is Nothing, "", CStr(l(colonne))))
                        End If
                    Next
                    ajout.ExecuteNonQuery()
                    ecrites += 1
                End Using
            Next
        End Using
        Return ecrites
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Commandes du menu (clic droit)
    ' ------------------------------------------------------------------------------------------

    Friend Sub CommandeExporter(proprietaire As IWin32Window)
        Dim avecMinuterie As Boolean = True
        If Not FormTransfert.Demander(proprietaire, "Exporter les paramètres",
                                      "TOUS les réglages de Slide seront enregistrés dans un fichier, pour configurer un autre PC." & vbCrLf &
                                      "ATTENTION : les mots de passe (Deezer, comptes IPTV) y sont écrits EN CLAIR : gardez ce fichier en lieu sûr et supprimez-le après usage." & vbCrLf &
                                      "La base, les listes IPTV et les niveaux sonores ne sont pas exportés.",
                                      "Exporter AUSSI la minuterie (toutes ses lignes)", True, avecMinuterie) Then Exit Sub
        Using d As New SaveFileDialog()
            d.Title = "Exporter les paramètres" & If(avecMinuterie, " et la minuterie", "")
            d.Filter = "Paramètres Slide (*" & Extension & ")|*" & Extension
            d.FileName = NomParDefaut()
            d.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            If d.ShowDialog(proprietaire) <> DialogResult.OK Then Exit Sub
            Dim reglages As Integer, lignes As Integer
            If Exporter(d.FileName, avecMinuterie, reglages, lignes) Then
                MessageBox.Show(proprietaire, "Export terminé :" & vbCrLf & d.FileName & vbCrLf & vbCrLf &
                                reglages & " réglages" & If(avecMinuterie, " et " & lignes & " ligne(s) de minuterie.", ", sans la minuterie.") & vbCrLf & vbCrLf &
                                "Les chemins (dossiers de photos, films de la minuterie) sont repris tels quels.",
                                "Exporter les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub

    Friend Sub CommandeImporter(proprietaire As IWin32Window)
        Using d As New OpenFileDialog()
            d.Title = "Importer des paramètres"
            d.Filter = "Paramètres Slide (*" & Extension & ")|*" & Extension & "|Tous les fichiers|*.*"
            d.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            If d.ShowDialog(proprietaire) <> DialogResult.OK Then Exit Sub

            ' Le fichier a-t-il une minuterie ?
            Dim minuterieDansLeFichier As Boolean = False
            Try
                Dim racine As JObject = JObject.Parse(File.ReadAllText(d.FileName))
                Dim m As JArray = TryCast(racine("minuterie"), JArray)
                minuterieDansLeFichier = m IsNot Nothing AndAlso m.Count > 0
            Catch ex As Exception
                MessageBox.Show(proprietaire, "Ce fichier n'est pas un export de paramètres Slide.", "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End Try

            Dim avecMinuterie As Boolean = minuterieDansLeFichier
            If Not FormTransfert.Demander(proprietaire, "Importer les paramètres",
                                          "Importer « " & Path.GetFileName(d.FileName) & " » : les réglages de ce PC seront remplacés par ceux du fichier." & vbCrLf &
                                          "Vos réglages actuels sont sauvegardés avant (dossier data\settings). Slide devra être relancé ensuite.",
                                          If(minuterieDansLeFichier, "Importer AUSSI la minuterie (remplace celle de ce PC)", "Minuterie : absente du fichier"),
                                          minuterieDansLeFichier, avecMinuterie) Then Exit Sub

            Dim reglages As Integer, lignes As Integer, sauvegarde As String = ""
            If Not Importer(d.FileName, avecMinuterie, reglages, lignes, sauvegarde) Then Exit Sub
            Dim texte As String = "Import terminé : " & reglages & " réglages" & If(avecMinuterie, " et " & lignes & " ligne(s) de minuterie.", ", minuterie de ce PC conservée.") & vbCrLf &
                                  If(sauvegarde <> "", "Sauvegarde de l'état d'avant : " & sauvegarde & vbCrLf, "") & vbCrLf &
                                  "Pensez à vérifier les chemins (dossiers de photos et de films), qui sont repris tels quels." & vbCrLf & vbCrLf &
                                  "Relancer Slide maintenant ?"
            If MessageBox.Show(proprietaire, texte, "Importer les paramètres", MessageBoxButtons.YesNo, MessageBoxIcon.Information) = DialogResult.Yes Then
                Try
                    Dim exe As String = Path.Combine(AppPath, "slide.exe")
                    Process.Start(New ProcessStartInfo(exe) With {.UseShellExecute = True, .WorkingDirectory = AppPath})
                    End
                Catch ex As Exception
                    DiagLog("Import des parametres : relance impossible (" & ex.Message & ")")
                End Try
            End If
        End Using
    End Sub

End Module
