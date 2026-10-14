Option Strict On
Option Explicit On

Imports System.Globalization

''' <summary>
''' Relance automatique de Slide pendant la nuit (demande de Jacob, 07/10 : Slide tourne des mois).
'''
''' Pourquoi : VLC (libvlc 3.0.24) garde 3 objets de Windows et 150 a 250 Ko a chaque video jouee,
''' soit environ 0,3 Go par jour dans Slide. Mesure du 07/10 : rien ne les rend, ni l'arret du
''' lecteur, ni sa destruction, ni meme la fermeture de VLC dans le programme ; seul l'arret du
''' programme les rend. Slide est donc relance de temps en temps, la nuit.
'''
''' Quand : une fois par semaine ou une fois par mois (Jacob, 07/10 : pas chaque nuit), a partir de
''' l'heure choisie et pendant 3 heures, au premier moment ou Slide est en simple diaporama.
''' Reglages, onglet Divers : chaque semaine a 3 h par defaut ; la case permet de l'arreter.
''' La relance est REPORTEE (nouvel essai toutes les 20 s, jusqu'a la fin des 3 heures, sinon la
''' nuit suivante) tant que :
'''   - Slide tourne depuis moins longtemps que la frequence choisie (voir DureeMiniHeures) ;
'''   - un film, la TV, un ecran noir ou une autre tache de la minuterie est en cours ;
'''   - une fenetre de reglages ou d'apercu est ouverte ;
'''   - une tache de la minuterie est prevue dans les 6 minutes.
'''
''' Etat conserve : un lancement ordinaire repart des reglages enregistres et rejoue les taches
''' BOOT. La relance de nuit, elle, doit retrouver l'etat EXACT d'avant (un "Volume OFF" de 23 h
''' doit rester coupe a 4 h, un PIP arrete pour la nuit ne doit pas revenir). L'ancien programme
''' ecrit donc son etat dans data\RelanceDeNuit.txt ; le nouveau le relit (LireEtat) et :
'''   - reprend le diaporama ou il en etait (listes et cases "deja vu") ;
'''   - remet volumes, "Volume OFF", musique, modes photo/video, mode RDV ;
'''   - ne joue PAS les taches BOOT, mais relance les PIP qui etaient affiches.
''' </summary>
Friend Module RelanceDeNuit

    Private Const DureeFenetreHeures As Integer = 3         ' a partir de RelanceNuitHeure (reglages)
    ''' <summary>Slide doit tourner depuis au moins ce temps. La fuite est celle du programme en cours :
    ''' on compte donc depuis SON lancement (un lancement a la main remet le compteur a zero), sans
    ''' date a retenir. Douze heures de moins que la frequence (7 ou 30 jours) : lance un matin,
    ''' Slide est relance la 7e nuit (ou la 30e), et non la suivante.</summary>
    Private Function DureeMiniHeures() As Double
        Dim jours As Integer = If(RelanceNuitFrequence >= 30, 30, 7)
        Return jours * 24 - 12
    End Function
    Private Const MinutesSansTache As Integer = 6
    Private Const ValiditeEtatMinutes As Double = 10

    Private ReadOnly _depuisLeLancement As Stopwatch = Stopwatch.StartNew()
    Private _minuteur As System.Windows.Forms.Timer
    Private _dernierMotif As String = ""
    Private _dansLaFenetre As Boolean
    Private _enCours As Boolean
    Private _urgenceNotee As Boolean

    ' Fenetres de reglages, de liste ou d'apercu : ouvertes, quelqu'un se sert de Slide.
    Private ReadOnly _fenetresDeTravail As String() = {
        "Form2", "Form4", "Form5", "Form6", "Form7", "Form8", "Form9", "Form10", "Form11", "Form13", "Form14",
        "Form15", "Form16", "Form17", "FormCoherence", "FormSynchroUsb", "FormProgression", "FormFermeture"}

    Private Function FichierEtat() As String
        Return IO.Path.Combine(DossierReglages, "RelanceDeNuit.txt")
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Ancien programme : surveillance et relance
    ' ------------------------------------------------------------------------------------------

    ''' <summary>A appeler une fois, depuis le fil de l'interface, au demarrage.</summary>
    Public Sub Demarrer()
        If _minuteur IsNot Nothing Then Exit Sub
        GardienSlide.Demarrer()                              ' relance Slide s'il s'arrete anormalement (reglage par poste)
        _minuteur = New System.Windows.Forms.Timer With {.Interval = 20000}
        AddHandler _minuteur.Tick, Sub() Verifier()
        _minuteur.Start()
    End Sub

    Private Sub Verifier()
        If _enCours Then Exit Sub
        GardienSlide.OublierLesPlantagesSiStable(_depuisLeLancement.Elapsed.TotalMinutes)
        Try
            Dim heure As Integer = Now.Hour
            ' (modulo 24 : une heure de debut a 23 h couvre 23 h, 0 h et 1 h)
            ' Une version de Slide preparee par les mises a jour (MiseAJour) se relance aussi la nuit, meme sans la
            ' frequence habituelle (14/10/2026) : sans cela, elle attendrait le prochain lancement a la main.
            Dim heureVoulue As Boolean = (heure - RelanceNuitHeure + 24) Mod 24 < DureeFenetreHeures
            Dim frequence As Boolean = RelanceNuitActive AndAlso _depuisLeLancement.Elapsed.TotalHours >= DureeMiniHeures()
            ' Garde-fou (26/10/2026) : Windows plafonne a 10 000 objets USER par programme ; une fuite d'objets (relances de flux)
            ' arretait Slide net apres 24 h. Au-dela de 8 000, Slide se relance des que rien ne l'en empeche, a toute heure.
            Dim urgence As Boolean = SuiviMemoire.ObjetsUser() >= 8000
            If urgence AndAlso Not _urgenceNotee Then
                _urgenceNotee = True
                DiagLog("Relance de nuit : " & SuiviMemoire.ObjetsUser() & " objets USER (plafond Windows 10 000) -> relance des que possible, a toute heure")
            End If
            Dim fenetre As Boolean = urgence OrElse (heureVoulue AndAlso (frequence OrElse MiseAJour.PaquetPret()))
            If Not fenetre Then
                If _dansLaFenetre AndAlso _dernierMotif <> "" Then
                    DiagLog("Relance de nuit : pas faite cette nuit (" & _dernierMotif & ")")
                End If
                _dansLaFenetre = False
                _dernierMotif = ""
                Exit Sub
            End If
            _dansLaFenetre = True
            Dim motif As String = Empechement()
            If motif <> "" Then
                If motif <> _dernierMotif Then DiagLog("Relance de nuit : reportee (" & motif & ")")
                _dernierMotif = motif
                Exit Sub
            End If
            Relancer()
        Catch ex As Exception
            DiagLog("Relance de nuit : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Ce qui empeche la relance maintenant ("" si rien).</summary>
    Private Function Empechement() As String
        Dim mode As String = If(Diffusion.Mode, "")
        If mode <> "PHOTO" AndAlso mode <> "VIDEO" Then Return "diffusion en cours : " & mode
        ' Tache bloquante (film, TV, ecran noir, programme lance...) : la minuterie n'a pas rendu
        ' la main (voir Form3.Timer3_Tick).
        If MinuterieEnCours > 0 Then Return "tache de la minuterie en cours"
        Try
            For Each f As Form In Application.OpenForms.Cast(Of Form)().ToList()
                If f.Visible AndAlso _fenetresDeTravail.Contains(f.GetType().Name) Then Return "fenetre ouverte : " & f.GetType().Name
            Next
        Catch ex As Exception
            Return "fenetres en cours de changement"
        End Try
        Dim prochaine As String = TacheProche()
        If prochaine <> "" Then Return "tache prevue a " & prochaine
        Return ""
    End Function

    ''' <summary>Heure ("HH:mm") d'une tache de la minuterie prevue dans les minutes qui viennent
    ''' ("" si aucune) : le nouveau programme ne doit pas la manquer pendant son demarrage.</summary>
    Private Function TacheProche() As String
        For k As Integer = 0 To MinutesSansTache
            Dim t As Date = Now.AddMinutes(k)
            Dim jour As Integer = CInt(t.DayOfWeek) - 1
            If jour = -1 Then jour = 6                       ' lundi = 0 ... dimanche = 6
            Dim minute As String = jour.ToString("00") & t.ToString("HHmm")
            For Each ligne As Object In LigneMinuterie
                Dim tache As Minuterie = CType(ligne, Minuterie)
                If TableauTuya.Ignoree(tache) Then Continue For
                If String.Equals(CStr(RTConvert(tache.Tri, jour)), minute) Then Return t.ToString("HH:mm")
            Next
        Next
        Return ""
    End Function

    ''' <summary>Relance Slide tout de suite, en gardant son etat (bouton "Installer maintenant" des mises a jour).</summary>
    Friend Sub RelancerMaintenant()
        Relancer()
    End Sub

    Private Sub Relancer()
        _enCours = True
        Dim pips As List(Of String()) = PipsEnCoursPourRelance()
        Try
            Using p As Process = Process.GetCurrentProcess()
                DiagLog("Relance de nuit : Slide tourne depuis " & _depuisLeLancement.Elapsed.TotalHours.ToString("0.0") & " h (memoire " &
                        (p.PrivateMemorySize64 \ 1048576) & " Mo, " & p.HandleCount & " handles), " & pips.Count & " PIP -> relance")
            End Using
            EcrireEtat(pips)
            SaveRestoreLeSlide(1)                            ' listes et cases "deja vu" (position du diaporama)
            ArreterTousLesPip()                              ' le tuner TV doit etre rendu avant le nouveau programme
            Form3.RetenirLaPlace()                           ' la fenetre reviendra a la meme place
            My.Settings.Save()
            MiseAJour.InstallerAvantRelance()                ' la version preparee prend la place de l'ancienne
            ' Le lecteur Deezer (WebView2) est ferme PROPREMENT avant le lancement du nouveau programme : les deux auraient
            ' partage le meme navigateur (meme dossier de donnees), qui s'arretait sous le nouveau ("navigateur arrete"),
            ' puis Slide se fermait tout seul peu apres le lancement (autre PC, 15/10/2026 10:54).
            Try
                MusiqueDeezer.Fermer()
                Dim attente As Stopwatch = Stopwatch.StartNew()
                Do While attente.ElapsedMilliseconds < 2500
                    Application.DoEvents()
                    Threading.Thread.Sleep(50)
                Loop
            Catch ex As Exception
                DiagLog("Relance de nuit : fermeture de Deezer : " & ex.Message)
            End Try
            Dim exe As String = Environment.ProcessPath
            If String.IsNullOrEmpty(exe) Then Throw New InvalidOperationException("chemin du programme inconnu")
            MarquerLaRelance()
            ' Le nouveau programme part 15 s APRES la fermeture de celui-ci (et non en meme temps) : l'ancien rend ses ~1 Go, le disque finit d'ecrire les
            ' fichiers de la mise a jour. Autre PC (4 Go, disque dur a 100 %), 30/10/2026 : Slide s'etait arrete net pendant ce demarrage charge.
            GardienSlide.Arreter()                           ' fermeture voulue : le gardien de l'ancien programme ne relance rien
            GardienSlide.LancerPlusTard(exe, 15)
        Catch ex As Exception
            ' Le nouveau programme n'est pas parti : celui-ci continue, avec ses PIP.
            DiagLog("Relance de nuit : impossible (" & ex.Message & ") -> Slide continue")
            Try
                IO.File.Delete(FichierEtat())
            Catch
            End Try
            If PipsActifs() = 0 Then LancerLesPip(pips)
            _dernierMotif = "echec : " & ex.Message
            _minuteur.Stop()                                 ' pas de nouvel essai avant un lancement
            _enCours = False
            Exit Sub
        End Try
        DiagLog("Relance de nuit : nouveau programme lance, fermeture de celui-ci")
        End
    End Sub

    Private Sub EcrireEtat(pips As List(Of String()))
        Dim jour As Integer = CInt(Now.DayOfWeek) - 1
        If jour = -1 Then jour = 6
        Dim lignes As New List(Of String) From {
            "Quand=" & Now.ToString("o", CultureInfo.InvariantCulture),
            "Minute=" & jour.ToString("00") & Now.ToString("HHmm"),
            "VolumeOFF=" & VolumeOFF,
            "VolumeGeneral=" & VolumeGeneral,
            "VolumeMusicSlide=" & VolumeMusicSlide,
            "VolumeVideoSlide=" & VolumeVideoSlide,
            "VolumeMusicSlideParMinuterie=" & VolumeMusicSlideParMinuterie,
            "VolumeVideoSlideParMinuterie=" & VolumeVideoSlideParMinuterie,
            "VolumeDeezerParMinuterie=" & VolumeDeezerParMinuterie,
            "DeezerPlaylistParMinuterie=" & DeezerPlaylistParMinuterie,
            "Music=" & Music,
            "Music_Mp3=" & Music_Mp3,
            "Music_Radio=" & Music_Radio,
            "Music_Deezer=" & Music_Deezer,
            "VideoWithAudio=" & VideoWithAudio,
            "PhotoAleatoire=" & PhotoAleatoire,
            "VideoAleatoire=" & VideoAleatoire,
            "PhotoModeCroissant=" & PhotoModeCroissant,
            "VideoModeCroissant=" & VideoModeCroissant,
            "OnlyPhotos=" & onlyphotos,
            "OnlyVideos=" & onlyvideos,
            "ModeRdv=" & ModeRdv,
            "ModeRdvDateDebut=" & ModeRdvDateDebut,
            "ModeRdvDateFin=" & ModeRdvDateFin,
            "ModeRdvLimitVideo=" & ModeRdvLimitVideo,
            "ModeRdvEnBoucle=" & ModeRdvEnBoucle}
        For Each pip As String() In pips
            lignes.Add("Pip=" & String.Join(vbTab, pip.Select(Function(x) If(x, "").Replace(vbTab, " ").Replace(vbCr, " ").Replace(vbLf, " "))))
        Next
        IO.File.WriteAllLines(FichierEtat(), lignes, New Text.UTF8Encoding(False))
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Nouveau programme : l'etat d'avant est remis
    ' ------------------------------------------------------------------------------------------

    Private _etat As Dictionary(Of String, String)
    Private ReadOnly _pips As New List(Of String())
    Private _premierPassage As Boolean

    ''' <summary>Vrai si ce lancement est une relance de nuit (etat de l'ancien programme relu).</summary>
    Public ReadOnly Property Reprise As Boolean
        Get
            Return _etat IsNot Nothing
        End Get
    End Property

    ''' <summary>Minute (jour + HHmm) dont l'ancien programme avait deja joue les taches.</summary>
    Public ReadOnly Property MinuteJouee As String
        Get
            Return Valeur("Minute")
        End Get
    End Property

    ''' <summary>A appeler au tout debut de l'initialisation : relit l'etat laisse par l'ancien
    ''' programme, s'il vient d'etre ecrit (sinon c'est un lancement ordinaire).</summary>
    Public Sub LireEtat()
        Try
            Dim fichier As String = FichierEtat()
            If Not IO.File.Exists(fichier) Then Exit Sub
            Dim lu As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)
            Dim pips As New List(Of String())
            For Each ligne As String In IO.File.ReadAllLines(fichier, Text.Encoding.UTF8)
                Dim p As Integer = ligne.IndexOf("="c)
                If p <= 0 Then Continue For
                Dim cle As String = ligne.Substring(0, p), texte As String = ligne.Substring(p + 1)
                If cle = "Pip" Then pips.Add(texte.Split(CChar(vbTab))) Else lu(cle) = texte
            Next
            IO.File.Delete(fichier)                          ' sert une seule fois
            Dim quand As Date
            If Not lu.ContainsKey("Quand") OrElse
               Not Date.TryParse(lu("Quand"), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, quand) OrElse
               Math.Abs((Now - quand).TotalMinutes) > ValiditeEtatMinutes Then
                DiagLog("Relance de nuit : etat trop ancien, ignore (lancement ordinaire)")
                Exit Sub
            End If
            _etat = lu
            _pips.AddRange(pips.Where(Function(x) x.Length >= 6))
            _premierPassage = True
            DiagLog("Relance de nuit : etat d'avant relu (Volume OFF=" & Valeur("VolumeOFF") & ", musique=" & Valeur("Music") &
                    ", " & _pips.Count & " PIP) ; les taches BOOT ne seront pas jouees")
        Catch ex As Exception
            _etat = Nothing
            DiagLog("Relance de nuit : etat illisible (" & ex.Message & "), lancement ordinaire")
        End Try
    End Sub

    Private Function Valeur(cle As String) As String
        Dim texte As String = Nothing
        If _etat IsNot Nothing AndAlso _etat.TryGetValue(cle, texte) Then Return texte
        Return ""
    End Function

    Private Sub Remettre(cle As String, ByRef variable As Boolean)
        Dim v As Boolean
        If Boolean.TryParse(Valeur(cle), v) Then variable = v
    End Sub

    Private Sub Remettre(cle As String, ByRef variable As Integer)
        Dim v As Integer
        If Integer.TryParse(Valeur(cle), NumberStyles.Integer, CultureInfo.InvariantCulture, v) Then variable = v
    End Sub

    ''' <summary>Musique (source, marche/arret) : a appeler avant que l'initialisation la lance.</summary>
    Public Sub RemettreLaMusique()
        If Not Reprise Then Exit Sub
        Remettre("Music", Music)
        Remettre("Music_Mp3", Music_Mp3)
        Remettre("Music_Radio", Music_Radio)
        Remettre("Music_Deezer", Music_Deezer)
        ' Deezer programme par la minuterie : son volume et sa playlist (lus avant que la musique soit lancee).
        Remettre("VolumeDeezerParMinuterie", VolumeDeezerParMinuterie)
        If Valeur("DeezerPlaylistParMinuterie") <> "" Then DeezerPlaylistParMinuterie = Valeur("DeezerPlaylistParMinuterie")
        Remettre("VideoWithAudio", VideoWithAudio)
    End Sub

    ''' <summary>Volume general et "Volume OFF" : a appeler avant le reglage du volume de Windows.</summary>
    Public Sub RemettreLeVolumeGeneral()
        If Not Reprise Then Exit Sub
        Remettre("VolumeGeneral", VolumeGeneral)
        Remettre("VolumeOFF", VolumeOFF)
    End Sub

    ''' <summary>Volumes du diaporama, modes photo/video, mode RDV.</summary>
    Public Sub RemettreLesReglages()
        If Not Reprise Then Exit Sub
        Remettre("VolumeMusicSlide", VolumeMusicSlide)
        Remettre("VolumeVideoSlide", VolumeVideoSlide)
        Remettre("VolumeMusicSlideParMinuterie", VolumeMusicSlideParMinuterie)
        Remettre("VolumeVideoSlideParMinuterie", VolumeVideoSlideParMinuterie)
        Remettre("PhotoAleatoire", PhotoAleatoire)
        Remettre("VideoAleatoire", VideoAleatoire)
        Remettre("PhotoModeCroissant", PhotoModeCroissant)
        Remettre("VideoModeCroissant", VideoModeCroissant)
        Remettre("OnlyPhotos", onlyphotos)
        Remettre("OnlyVideos", onlyvideos)
        Remettre("ModeRdv", ModeRdv)
        If _etat.ContainsKey("ModeRdvDateDebut") Then ModeRdvDateDebut = Valeur("ModeRdvDateDebut")
        If _etat.ContainsKey("ModeRdvDateFin") Then ModeRdvDateFin = Valeur("ModeRdvDateFin")
        Remettre("ModeRdvLimitVideo", ModeRdvLimitVideo)
        Remettre("ModeRdvEnBoucle", ModeRdvEnBoucle)
    End Sub

    ''' <summary>Vrai une seule fois, au premier passage de la minuterie apres une relance de nuit.</summary>
    Public Function PremierPassageApresRelance() As Boolean
        If Not _premierPassage Then Return False
        _premierPassage = False
        Return True
    End Function

    ''' <summary>Les PIP d'avant la relance ne sont pas remis a part : les taches BOOT sont rejouees (30/10/2026).</summary>
    Public Sub OublierLesPip()
        _pips.Clear()
    End Sub

    ''' <summary>Relance les PIP qui etaient affiches avant la relance (a la place des taches BOOT).</summary>
    Public Sub RemettreLesPip()
        Dim pips As List(Of String()) = _pips.ToList()
        _pips.Clear()
        LancerLesPip(pips)
    End Sub

    ''' <summary>Chaque PIP : type, cible (chaine, film...), debut, duree, volume, reglages. Memes
    ''' appels que la minuterie.</summary>
    Private Sub LancerLesPip(pips As List(Of String()))
        For Each pip As String() In pips
            Try
                Dim type As String = pip(0).ToUpperInvariant(), cible As String = pip(1)
                Dim volume As Integer = CInt(Val(pip(4))), reglages As String = pip(5)
                DiagLog("Relance de nuit : PIP " & type & " " & cible & " remis (volume " & volume & ")")
                Select Case type
                    Case "TV"
                        If Not CleTNTPrete() Then
                            DiagLog("Relance de nuit : PIP TV non remis (cle TNT indisponible)")
                        Else
                            PlayTV_VLC(CInt(Val(cible)), volume, reglages)
                            wait(2)
                        End If
                    Case "IPTV", "CAMÉRA BLINK", "CAMÉRA IP"
                        PlayIPTV_VLC(cible, volume, reglages)
                        wait(2)
                    Case "SAT"
                        PlaySAT_VLC(CInt(Val(cible)), volume, reglages)
                    Case "FILM"
                        PlayFILM_VLC(cible, pip(2), pip(3), volume, reglages)
                        wait(2)
                End Select
                Application.DoEvents()
            Catch ex As Exception
                DiagLog("Relance de nuit : PIP non remis (" & ex.Message & ")")
            End Try
        Next
    End Sub

End Module
