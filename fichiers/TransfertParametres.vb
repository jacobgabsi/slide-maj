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
''' Les mots de passe (Deezer, comptes IPTV, cle secrete Tuya) sont chiffres pour le poste (DPAPI) : ils sont donc ECRITS EN CLAIR dans le fichier (section
''' "secrets"), puis rechiffres pour le nouveau poste a l'import. Le fichier doit etre garde en lieu sur et supprime apres usage.
''' La liste des radios du poste (data\settings\radio.txt) est exportee aussi (section "radios") et remplace celle du poste a l'import (20/10/2026).
''' Ne sont PAS transferes : la base slide.accdb elle-meme, les listes IPTV, les niveaux sonores, gmail.txt (data\settings). Les chemins
''' (dossiers de photos, films) sont repris tels quels.
''' L'import sauvegarde d'abord les reglages actuels dans data\settings.
''' </summary>
Friend Module TransfertParametres

    ''' <summary>Reglages jamais exportes ni importes : AUCUN (demande de Jacob).</summary>
    Private ReadOnly Exclus As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>Reglages gardes CHIFFRES pour le poste (DPAPI, voir MusiqueDeezer.Proteger) : exportes en clair, rechiffres a l'import.</summary>
    Private ReadOnly NomsDesSecrets As String() = {"DeezerMotDePasse", "IPTV_MotDePasse1", "IPTV_MotDePasse2", "IPTV_MotDePasse3", "Tuya_Secret", "Tuya_Comptes"}

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
                secrets(p.Name) = If(String.Equals(p.Name, "Tuya_Comptes", StringComparison.OrdinalIgnoreCase), TuyaSlide.ExporterLesComptes(), MusiqueDeezer.Deproteger(CStr(If(My.Settings(p.Name), ""))))        ' ("" si le mot de passe n'est pas lisible ici)
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
            ElseIf TypeOf valeur Is Double Then
                parametres(p.Name) = CDbl(valeur)
            ElseIf TypeOf valeur Is String Then
                parametres(p.Name) = CStr(valeur)
            End If
        Next
        racine("parametres") = parametres
        racine("secrets") = secrets
        If avecMinuterie Then racine("minuterie") = LireLaMinuterie()
        ' La liste des radios de ce poste (data\settings\radio.txt, 20/10/2026) : exportee avec les reglages, quelle que soit l'option minuterie.
        Dim radios As JArray = LireLesRadios()
        racine("radios") = radios
        NombreDeRadios = radios.Count
        Return racine
    End Function

    ''' <summary>Nombre de radios du dernier export ou import (pour les messages).</summary>
    Friend NombreDeRadios As Integer

    ''' <summary>La liste des radios de ce poste : nom et adresse de chaque radio, dans l'ordre de la liste.</summary>
    Private Function LireLesRadios() As JArray
        Dim radios As New JArray()
        Try
            Dim fichier As String = FichierDesRadios()
            If File.Exists(fichier) Then
                For Each ligne As String In File.ReadAllLines(fichier)
                    If ligne.Trim() = "" Then Continue For
                    Dim nom As String = If(ligne.Length >= 49, ligne.Substring(0, 49), ligne).Trim()
                    Dim adresse As String = If(ligne.Length > 49, ligne.Substring(49), "").Trim()
                    If nom = "" OrElse adresse = "" Then Continue For
                    radios.Add(New JObject From {{"nom", nom}, {"adresse", adresse}})
                Next
            End If
        Catch ex As Exception
            DiagLog("Export des parametres : radios illisibles (" & ex.Message & ")")
        End Try
        Return radios
    End Function

    ''' <summary>Remplace la liste des radios de ce poste par celle du fichier (ecriture sure : fichier provisoire, version precedente gardee dans
    ''' radio.old_precedente.txt, comme SaveRadio). Rend le nombre de radios ecrites.</summary>
    Private Function EcrireLesRadios(radios As JArray) As Integer
        Dim fichier As String = FichierDesRadios()
        Dim provisoire As String = fichier & ".tmp"
        Dim ecrites As Integer = 0
        Using ecrivain As New StreamWriter(provisoire, False)
            For Each jeton As JToken In radios
                Dim r As JObject = TryCast(jeton, JObject)
                If r Is Nothing Then Continue For
                Dim nom As String = CStr(If(r("nom"), "")).Trim()
                Dim adresse As String = CStr(If(r("adresse"), "")).Trim()
                If nom = "" OrElse adresse = "" Then Continue For
                If nom.Length > 48 Then nom = nom.Substring(0, 48).TrimEnd()
                ecrivain.WriteLine(nom.PadRight(49) & adresse)
                ecrites += 1
            Next
        End Using
        If ecrites = 0 Then
            File.Delete(provisoire)                    ' (fichier sans radio utilisable : la liste de ce poste est gardee)
            Return 0
        End If
        Try
            If File.Exists(fichier) Then File.Copy(fichier, Path.Combine(DossierReglages, "radio.old_precedente.txt"), True)
        Catch ex As Exception
        End Try
        File.Move(provisoire, fichier, True)
        Return ecrites
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

    ' ------------------------------------------------------------------------------------------
    ' Familles de reglages = les CADRES de la fenetre des reglages (onglet > cadre) : a l'import, on coche ceux que l'on reprend (demande de Jacob, 21/10/2026).
    ' Un reglage qui n'est dans aucune liste va dans "Divers > Autres reglages".
    ' ------------------------------------------------------------------------------------------

    Friend Const FamilleMinuterie As String = "Minuterie : lignes du tableau"
    Friend Const FamilleRadios As String = "Audio > Radio : liste des stations"
    Private Const FamilleAutres As String = "Divers > Autres réglages"

    ''' <summary>Cadre = reglages, dans l'ordre des onglets de la fenetre des reglages.</summary>
    Private ReadOnly DefinitionDesFamilles As String() = {
        "Sources > Dossiers de photos et de vidéos=CheminPhotoVideo",
        "Sources > Tuya / Smart Life=Tuya_ClientId;Tuya_Secret;Tuya_Uid;Tuya_Region;Tuya_Appareils;Tuya_Groupes;Tuya_Comptes;Tuya_Actif",
        "Photos > Affichage=PhotoAleatoire;FormatPhoto169;PhotoModeCroissant;PhotoAlbum;AlbumNombreMax;AlbumNombreAleatoire;OnlyPhotos;reprendreLeSlide;ProchainePhoto;HasardPhotoArrivee;HasardPhotoMouvement;HasardPhotoCouleurs",
        "Photos > Durées=durréePhoto;DurréeAffichageDuneImage",
        "Photos > Localité et date=AfficheDate;AfficheLieu;LieuEnLigne;LegendeCoin;LegendeTaillePourcent;LegendeCentreeSousPip;BandesActives;BandesTaillePourcent;BandesPosition",
        "Photos > Police, relief et horloge=BandesRelief;BandesPolice;MontreAngle;MontreInclinee",
        "Photos > Animation des photos=AnimerLesPhotos;AnimationArrivee;AnimationMouvement;AnimationPhotoForce;AnimationPhotoCouleurs",
        "Vidéos > Durée et PIP=DurréeVideo;PipSizeDefault;ProchaineVideo",
        "Vidéos > Dossiers par défaut=DossierDivxParDefault;DossierDreamboxParDefault;DossierFreeboxParDefault",
        "Vidéos > Animation des vidéos=AnimerLesVideos;AnimationVideo;AnimationVideoMouvement;AnimationVideoForce;AnimationVideoCouleurs;AnimationVideoBandes;FondFlouVideo;HasardVideoTransition;HasardVideoMouvement;HasardVideoCouleurs",
        "Vidéos > Options=VideoAleatoire;VideoModeCroissant;OnlyVideos;BorderLess;VLCBoost;VolumeVLCBoost",
        "Audio > Son=VideoWithAudio;Music;Music_Mp3;Music_Radio;Music_Deezer;Music_Sources;Music_AlternanceMinutes;RadioURL",
        "Audio > Volumes=VolumeGeneral;VolumeMusicSlide;VolumeVideoSlide;VolumeTVParDefault;VolumeDreamboxParDefault;VolumeNetflixPrimeParDefault;VolumeOFFegal1",
        "Audio > Options=LisserLeSon",
        "Audio > Deezer=DeezerInterne;DeezerLogin;DeezerMotDePasse;VolumeDeezer;DeezerPlaylist;DeezerFenetre;DeezerPlaylistAuOK",
        "Audio > Pochette et titre de la musique=DeezerCarte;DeezerCarteCoin;DeezerCartePourcent;DeezerCarteSecondes;DeezerIconeSource;DeezerIconeX;DeezerIconeY;DeezerCarteX;DeezerCarteY",
        FamilleRadios & "=",
        "Divers > RDV=ModeRDV;ModeRdvDateDebut;ModeRdvDateFin;ModeRdvLimitVideo;ModeRDVenBoucle",
        "Divers > Netflix=RetourAuDebutNetflix;LatenceNetflix;Nflix_DossierTelechargement",
        "Divers > IPTV=IPTV_login;IPTV_password;IPTV_OnlyFavo;IPTV_Url;IPTV_Url2;IPTV_Url3;IPTV_Nom1;IPTV_Nom2;IPTV_Nom3;IPTV_Source;IPTV_Utilisateur1;IPTV_Utilisateur2;IPTV_Utilisateur3;IPTV_MotDePasse1;IPTV_MotDePasse2;IPTV_MotDePasse3;PlanB;GroupePardefault",
        "Divers > Système=ThemePro;Maj_Mode;Maj_VerifActive;Maj_VerifHeure;Maj_VerifDerniere;Maj_Dossier;Maj_AdresseVersion;Maj_AdresseZip;RelanceNuitActive;RelanceNuitHeure;RelanceNuitFrequence;StopWindowsUpdate;User;Comment",
        "Divers > Internet=SansInternet",
        "Divers > Répertoire réseau=DatabaseNetPath;MinuterieEnReseaux",
        "Divers > Montre=AfficheClock;ClockBIG;ClockTaillePourcent;MontreOpacite",
        "Divers > Récepteur satellite et réseau=Dreambox_IP;FluxReseaux_IP;ControlAdistance;CleUsbNom",
        "Divers > Sabbat et fêtes=AfficheHeureDeShabbat;Shabbat_Ville;Shabbat_ToutLesXminutes;AfficheVideoChabbatShalom;AfficheVideoChavouaTov;HoraireDeShabbatDansCadran;YomTov_Actif;YomTov_Liste;YomTov_Refusees",
        "Divers > Fenêtre : taille et position=FullScreen;ScreenSizeH;ScreenSizeW;ScreenPosX;ScreenPosY",
        FamilleMinuterie & "="}

    Private _familleDe As Dictionary(Of String, String)

    Private Function TableDesFamilles() As Dictionary(Of String, String)
        If _familleDe IsNot Nothing Then Return _familleDe
        Dim t As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
        For Each ligne As String In DefinitionDesFamilles
            Dim coupe As Integer = ligne.LastIndexOf("="c)
            For Each nom As String In ligne.Substring(coupe + 1).Split(";"c)
                If nom <> "" Then t(nom) = ligne.Substring(0, coupe)
            Next
        Next
        _familleDe = t
        Return t
    End Function

    ''' <summary>La famille (cadre) d'un reglage.</summary>
    Private Function FamilleDe(nom As String) As String
        Dim famille As String = Nothing
        If TableDesFamilles().TryGetValue(nom, famille) Then Return famille
        If nom.StartsWith("Minuterie", StringComparison.OrdinalIgnoreCase) Then Return FamilleMinuterie          ' (les copies de la minuterie dans les reglages)
        Return FamilleAutres
    End Function

    ''' <summary>Les familles que contient le fichier, dans l'ordre des onglets, avec le nombre de reglages ("Photos > Durées (2)"). La minuterie et les radios : selon le fichier.</summary>
    Private Function FamillesDuFichier(racine As JObject) As List(Of KeyValuePair(Of String, String))
        Dim nombre As New Dictionary(Of String, Integer)(StringComparer.OrdinalIgnoreCase)
        For Each section As String In {"parametres", "secrets"}
            Dim o As JObject = TryCast(racine(section), JObject)
            If o Is Nothing Then Continue For
            For Each p As JProperty In o.Properties()
                Dim f As String = FamilleDe(p.Name)
                nombre(f) = If(nombre.ContainsKey(f), nombre(f), 0) + 1
            Next
        Next
        Dim liste As New List(Of KeyValuePair(Of String, String))
        Dim ordre As New List(Of String)
        For Each ligne As String In DefinitionDesFamilles
            ordre.Add(ligne.Substring(0, ligne.LastIndexOf("="c)))
        Next
        ordre.Add(FamilleAutres)
        For Each f As String In ordre
            If f = FamilleRadios Then
                Dim radios As JArray = TryCast(racine("radios"), JArray)
                If radios IsNot Nothing AndAlso radios.Count > 0 Then liste.Add(New KeyValuePair(Of String, String)(f, f & " (" & radios.Count & ")"))
            ElseIf f = FamilleMinuterie Then
                Dim lignes As JArray = TryCast(racine("minuterie"), JArray)
                If lignes IsNot Nothing AndAlso lignes.Count > 0 Then liste.Add(New KeyValuePair(Of String, String)(f, f & " (" & lignes.Count & " lignes)"))
            ElseIf nombre.ContainsKey(f) Then
                liste.Add(New KeyValuePair(Of String, String)(f, f & " (" & nombre(f) & ")"))
            End If
        Next
        Return liste
    End Function

    ''' <summary>Applique un fichier d'export. Rend le nombre de reglages et de lignes de minuterie importes (-1 : echec).</summary>
    ''' <param name="choix">Les familles (cadres) a importer ; les autres gardent les valeurs de ce PC.</param>
    Friend Function Importer(chemin As String, choix As ISet(Of String), ByRef reglages As Integer, ByRef lignes As Integer, ByRef sauvegarde As String) As Boolean
        reglages = 0 : lignes = 0 : sauvegarde = ""
        Dim avecMinuterie As Boolean = choix.Contains(FamilleMinuterie)
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
                    If clair Is Nothing OrElse Not choix.Contains(FamilleDe(nom)) Then Continue For
                    Try
                        My.Settings(nom) = If(String.Equals(nom, "Tuya_Comptes", StringComparison.OrdinalIgnoreCase), TuyaSlide.ImporterLesComptes(CStr(clair)), MusiqueDeezer.Proteger(CStr(clair)))
                        reglages += 1
                    Catch ex As Exception
                        DiagLog("Import des parametres : " & nom & " ignore (" & ex.Message & ")")
                    End Try
                Next
            End If

            For Each p As Configuration.SettingsProperty In My.Settings.Properties
                If Exclus.Contains(p.Name) Then Continue For
                If NomsDesSecrets.Contains(p.Name, StringComparer.OrdinalIgnoreCase) Then Continue For
                If Not choix.Contains(FamilleDe(p.Name)) Then Continue For
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
                    ElseIf p.PropertyType Is GetType(Double) Then
                        My.Settings(p.Name) = CDbl(jeton)
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

            ' Radios (export fait par une version qui les contient) : la liste de ce poste est remplacee ; l'ancienne reste dans
            ' radio.old_precedente.txt (et dans la sauvegarde d'avant import). Un fichier sans radios laisse la liste de ce poste telle quelle.
            NombreDeRadios = 0
            Dim radios As JArray = TryCast(racine("radios"), JArray)
            If radios IsNot Nothing AndAlso radios.Count > 0 AndAlso choix.Contains(FamilleRadios) Then
                Try
                    NombreDeRadios = EcrireLesRadios(radios)
                Catch ex As Exception
                    DiagLog("Import des parametres : radios ignorees (" & ex.Message & ")")
                End Try
            End If
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
                                      "ATTENTION : les mots de passe (Deezer, comptes IPTV) et la clé secrète Tuya y sont écrits EN CLAIR : gardez ce fichier en lieu sûr et supprimez-le après usage." & vbCrLf &
                                      "La base, les listes IPTV et les niveaux sonores ne sont pas exportés.",
                                      "Exporter AUSSI la minuterie (toutes ses lignes)", True, avecMinuterie) Then Exit Sub
        Using d As New SaveFileDialog()
            d.Title = "Exporter les paramètres" & If(avecMinuterie, " et la minuterie", "")
            d.Filter = "Paramètres Slide (*" & Extension & ")|*" & Extension
            d.FileName = NomParDefaut()
            Try : Directory.CreateDirectory(DossierReglages) : Catch ex As Exception : End Try
            d.InitialDirectory = DossierReglages          ' (data\settings : la ou Slide range ses reglages)
            If d.ShowDialog(proprietaire) <> DialogResult.OK Then Exit Sub
            Dim reglages As Integer, lignes As Integer
            If Exporter(d.FileName, avecMinuterie, reglages, lignes) Then
                MessageBox.Show(proprietaire, "Export terminé :" & vbCrLf & d.FileName & vbCrLf & vbCrLf &
                                reglages & " réglages" & If(avecMinuterie, " et " & lignes & " ligne(s) de minuterie.", ", sans la minuterie.") & vbCrLf &
                                NombreDeRadios & " radio(s) de la liste." & vbCrLf & vbCrLf &
                                "Les chemins (dossiers de photos, films de la minuterie) sont repris tels quels.",
                                "Exporter les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        End Using
    End Sub

    Friend Sub CommandeImporter(proprietaire As IWin32Window)
        Try
            CommandeImporterEtapes(proprietaire)
        Catch ex As Exception
            ' Rien ne doit se passer en silence apres le choix du fichier (22/10/2026 : "il demande le fichier et apres plus rien").
            DiagLog("Import des parametres : erreur imprevue (" & ex.GetType().Name & " : " & ex.Message & ") " & ex.StackTrace)
            MessageBox.Show(proprietaire, "L'import a échoué : " & ex.Message, "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub CommandeImporterEtapes(proprietaire As IWin32Window)
        Using d As New OpenFileDialog()
            d.Title = "Importer des paramètres"
            d.Filter = "Paramètres Slide (*" & Extension & ")|*" & Extension & "|Tous les fichiers|*.*"
            Try : Directory.CreateDirectory(DossierReglages) : Catch ex As Exception : End Try
            d.InitialDirectory = DossierReglages          ' (data\settings : la ou Slide range ses reglages)
            If d.ShowDialog(proprietaire) <> DialogResult.OK Then Exit Sub
            DiagLog("Import des parametres : fichier choisi : " & d.FileName)

            ' Ce que contient le fichier : les cadres de la fenetre des reglages, a cocher (ce qui n'est pas coche garde la valeur de ce PC).
            Dim familles As List(Of KeyValuePair(Of String, String))
            Try
                Dim racine As JObject = JObject.Parse(File.ReadAllText(d.FileName))
                If TryCast(racine("parametres"), JObject) Is Nothing Then Throw New FormatException("pas de section parametres")
                familles = FamillesDuFichier(racine)
            Catch ex As Exception
                MessageBox.Show(proprietaire, "Ce fichier n'est pas un export de paramètres Slide.", "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Warning)
                Exit Sub
            End Try

            DiagLog("Import des parametres : " & familles.Count & " cadre(s) dans le fichier, fenetre de choix")
            Dim choix As HashSet(Of String) = Nothing
            If Not FormChoixImport.Demander(proprietaire, "Importer « " & Path.GetFileName(d.FileName) & " » : cochez les cadres de réglages à reprendre. Le nombre entre parenthèses est celui des réglages du fichier dans ce cadre.", familles, choix) Then Exit Sub
            DiagLog("Import des parametres : cadres choisis : " & String.Join(", ", choix))
            Dim avecMinuterie As Boolean = choix.Contains(FamilleMinuterie)

            Dim reglages As Integer, lignes As Integer, sauvegarde As String = ""
            If Not Importer(d.FileName, choix, reglages, lignes, sauvegarde) Then Exit Sub
            Dim texte As String = "Import terminé : " & reglages & " réglages" & If(avecMinuterie, " et " & lignes & " ligne(s) de minuterie.", ", minuterie de ce PC conservée.") & vbCrLf &
                                  If(NombreDeRadios > 0, NombreDeRadios & " radio(s) : la liste de ce PC est remplacée." & vbCrLf, "") &
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
