
Imports System.Diagnostics.Eventing
Imports System.IO
Imports System.Net
Imports System.Runtime.InteropServices
Imports System.Security.Permissions




Public Class Form4




    ''' <summary>
    ''' Ajouter des dossiers sources : plusieurs a la fois (14/10/2026, "la multiselection de repertoire ne marche pas") ;
    ''' le selecteur Windows de dossiers accepte Ctrl et Maj + clic. Un dossier deja dans la liste n'est pas ajoute deux fois.
    ''' </summary>
    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim choix As New Microsoft.Win32.OpenFolderDialog With {
            .Multiselect = True,
            .Title = "Choisir un ou plusieurs dossiers (Ctrl ou Maj + clic)"}
        If choix.ShowDialog() <> True Then Exit Sub

        Dim dejaLa As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For i As Integer = 0 To Me.ListView1.Items.Count - 1
            dejaLa.Add(Me.ListView1.Items(i).Text.TrimEnd("\"c))
        Next

        Me.ListView1.SelectedItems.Clear()
        For Each dossier As String In choix.FolderNames
            If dossier = "" OrElse Not dejaLa.Add(dossier.TrimEnd("\"c)) Then Continue For
            Dim ligne As New ListViewItem()
            ligne.Text = dossier
            ligne.SubItems.Add("")
            ligne.SubItems.Add("")
            ligne.SubItems.Add("")
            ligne.Checked = True
            ListView1.Items.Add(ligne)
            ligne.Selected = True                          ' les nouveaux dossiers restent choisis (clic droit : photos/videos/musique...)
        Next
        OptionChange = True
    End Sub


    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        SupprimerLesDossiersChoisis()
    End Sub

    ''' <summary>Retire de la liste les dossiers choisis (plusieurs avec Ctrl ou Maj + clic) : la liste est copiee avant de les retirer.</summary>
    Private Sub SupprimerLesDossiersChoisis()
        For Each lvItem As ListViewItem In ListView1.SelectedItems.Cast(Of ListViewItem)().ToList()
            lvItem.Remove()
        Next

        OptionChange = True
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Sources : plusieurs dossiers a la fois (demande de Jacob, 14/10/2026 : "onglet source, permettre la
    ' multiselection"). Ctrl ou Maj + clic choisit plusieurs dossiers ; le clic droit agit sur tous.
    ' ------------------------------------------------------------------------------------------

    Private Function DossiersChoisis() As List(Of ListViewItem)
        Return ListView1.SelectedItems.Cast(Of ListViewItem)().ToList()
    End Function

    Private Sub CocherLaSelection(valeur As Boolean)
        For Each dossier As ListViewItem In DossiersChoisis()
            dossier.Checked = valeur
        Next
        OptionChange = True
    End Sub

    ''' <summary>Photos (1), videos (2) ou musique (3) : "NON" = on ne les prend pas, vide = on les prend.</summary>
    Private Sub PrendreDansLaSelection(colonne As Integer, prendre As Boolean)
        For Each dossier As ListViewItem In DossiersChoisis()
            If dossier.SubItems.Count > colonne Then dossier.SubItems(colonne).Text = If(prendre, "", "NON")
        Next
        OptionChange = True
    End Sub

    Private Sub CocherSelectionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CocherSelectionToolStripMenuItem.Click
        CocherLaSelection(True)
    End Sub

    Private Sub DecocherSelectionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles DecocherSelectionToolStripMenuItem.Click
        CocherLaSelection(False)
    End Sub

    Private Sub PhotosNonToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PhotosNonToolStripMenuItem.Click
        PrendreDansLaSelection(1, False)
    End Sub

    Private Sub PhotosOuiToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PhotosOuiToolStripMenuItem.Click
        PrendreDansLaSelection(1, True)
    End Sub

    Private Sub VideosNonToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VideosNonToolStripMenuItem.Click
        PrendreDansLaSelection(2, False)
    End Sub

    Private Sub VideosOuiToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VideosOuiToolStripMenuItem.Click
        PrendreDansLaSelection(2, True)
    End Sub

    Private Sub MusiqueNonToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MusiqueNonToolStripMenuItem.Click
        PrendreDansLaSelection(3, False)
    End Sub

    Private Sub MusiqueOuiToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MusiqueOuiToolStripMenuItem.Click
        PrendreDansLaSelection(3, True)
    End Sub

    Private Sub SupprimerSelectionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SupprimerSelectionToolStripMenuItem.Click
        SupprimerLesDossiersChoisis()
    End Sub

    Private Sub MenuSources_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles MenuSources.Opening
        If ListView1.SelectedItems.Count = 0 Then e.Cancel = True          ' rien de choisi : pas de menu
    End Sub

    ' Traces de l'enregistrement par OK ("Form4 OK : ... ms" : duree de chaque etape, pour trouver ce qui est lent sur un PC lent).
    Private _chronoOk As Stopwatch
    Private _chronoOkTotal As Long
    Private Sub EtapeOk(nom As String)
        If _chronoOk Is Nothing Then Exit Sub
        Dim ms As Long = _chronoOk.ElapsedMilliseconds
        _chronoOkTotal += ms
        DiagLog("Form4 OK : " & nom & " " & ms & " ms (cumul " & _chronoOkTotal & " ms)")
        _chronoOk.Restart()
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        _chronoOkTotal = 0
        'If Diffusion.Mode = "TV_VLC_INT" Then FlagExit = True 'au cas ou la TV est bloqué...

        ' Tous les controles d'abord, AVANT de toucher au moindre reglage, et presentes ensemble
        ' dans une seule fenetre (FormCoherence). Avant : une boite de message par probleme, au fil
        ' de l'enregistrement, alors qu'une partie des reglages etait deja enregistree.
        _chronoOk = Stopwatch.StartNew()
        If Not ReglagesCoherents() Then Exit Sub
        EtapeOk("verification de coherence")

        My.Settings.CheminPhotoVideo = New Specialized.StringCollection

        For i = 0 To Me.ListView1.Items.Count - 1
            Opt = "1111"

            If Me.ListView1.Items(i).Checked = False Then
                mid(Opt, 1, 1) = "0"
            End If
            For q = 2 To 4
                Try
                    If ListView1.Items(i).SubItems(q - 1).Text = "NON" Then
                        mid(Opt, q, 1) = "0"
                    End If
                Catch ex As Exception

                End Try


            Next

            If Me.ListView1.Items(i).SubItems(0).Text <> "" Then
                My.Settings.CheminPhotoVideo.Add(Me.ListView1.Items(i).SubItems(0).Text & "@" & Opt)
            End If



        Next

        If Me.ListView1.Items.Count = 0 Then
            MsgBox("Attention Source photo/video vide " & vbCrLf & "Veuillez selectionner le repertoire") : Exit Sub

        End If
        If CheckBox31.Checked = True And (IsDate(TextBox26.Text) = False Or IsDate(TextBox27.Text) = False) Then MsgBox("Attention modeRDV date invalide " & vbCrLf & "Veuillez resaisir la date ou desactiver ce mode") : Exit Sub


        VolumeTVParDefault = Val(TextBox11.Text) : My.Settings.VolumeTVParDefault = VolumeTVParDefault
        VolumeDreamboxParDefault = Val(TextBox14.Text) : My.Settings.VolumeDreamboxParDefault = VolumeDreamboxParDefault
        VolumeNetflixPrimeParDefault = Val(TextBox25.Text) : My.Settings.VolumeNetflixPrimeParDefault = VolumeNetflixPrimeParDefault

        ' (Les volumes trop forts, la coherence des taches et les Sonoff non connectes sont
        '  verifies au debut, par ReglagesCoherents.)


        VolumeGeneral = Val(TextBox7.Text) : My.Settings.VolumeGeneral = VolumeGeneral ': SetMasterVol(VolumeGeneral)
        VolumeMusicSlide = Val(TextBox8.Text) : My.Settings.VolumeMusicSlide = VolumeMusicSlide ': SetMusicVolSlide(VolumeMusicSlide)
        VolumeVideoSlide = Val(TextBox9.Text) : My.Settings.VolumeVideoSlide = VolumeVideoSlide ' : SetVideoVolSlide(VolumeVideoSlide)

        If Diffusion.Mode = "TV" Then SetVol(Diffusion.Vol)

        DurréePhoto = TextBox1.Text : My.Settings.durréePhoto = DurréePhoto
        Dim dureeSequenceAvant As Integer = CInt(Val(DurréeVideo))
        Dim dureeSequenceAvantTexte As String = CStr(DurréeVideo)
        DurréeVideo = TextBox2.Text : My.Settings.DurréeVideo = DurréeVideo
        If CInt(Val(DurréeVideo)) <> dureeSequenceAvant Then
            ' Nouvelle duree de sequence : les videos deja decoupees sont redecoupees, et on le dit.
            ' Annuler : l'ancienne duree est remise (champ compris), les decoupages restent ceux de l'ancienne duree.
            If DemanderLeRedecoupage(CStr(dureeSequenceAvant), CStr(CInt(Val(DurréeVideo))), CompterLesDecoupagesDesVideos(), Me) = "annuler" Then
                DurréeVideo = dureeSequenceAvantTexte : My.Settings.DurréeVideo = DurréeVideo
                TextBox2.Text = dureeSequenceAvantTexte
            End If
        End If
        DurréeAffichageDuneImage = TextBox3.Text : My.Settings.DurréeAffichageDuneImage = DurréeAffichageDuneImage
        FormatPhoto169 = CheckBox1.Checked : My.Settings.FormatPhoto169 = FormatPhoto169
        AnimerLesPhotos = CheckBoxAnimerPhotos.Checked : My.Settings.AnimerLesPhotos = AnimerLesPhotos
        AnimationArrivee = Math.Max(0, ComboBoxAnimationArrivee.SelectedIndex) : My.Settings.AnimationArrivee = AnimationArrivee
        AnimationMouvement = Math.Max(0, ComboBoxAnimationMouvement.SelectedIndex) : My.Settings.AnimationMouvement = AnimationMouvement
        AnimationPhotoForce = Math.Max(0, ComboBoxAnimationPhotoForce.SelectedIndex) : My.Settings.AnimationPhotoForce = AnimationPhotoForce
        AnimationPhotoCouleurs = Math.Max(0, ComboBoxAnimationPhotoCouleurs.SelectedIndex) : My.Settings.AnimationPhotoCouleurs = AnimationPhotoCouleurs
        HasardPhotoArrivee = _hasardPhotoArrivee : My.Settings.HasardPhotoArrivee = HasardPhotoArrivee
        HasardPhotoMouvement = _hasardPhotoMouvement : My.Settings.HasardPhotoMouvement = HasardPhotoMouvement
        HasardVideoTransition = _hasardVideoTransition : My.Settings.HasardVideoTransition = HasardVideoTransition
        HasardVideoMouvement = _hasardVideoMouvement : My.Settings.HasardVideoMouvement = HasardVideoMouvement
        HasardPhotoCouleurs = _hasardPhotoCouleurs : My.Settings.HasardPhotoCouleurs = HasardPhotoCouleurs
        HasardVideoCouleurs = _hasardVideoCouleurs : My.Settings.HasardVideoCouleurs = HasardVideoCouleurs
        PhotoAlbum = CheckBox29.Checked : My.Settings.PhotoAlbum = PhotoAlbum
        AlbumNombreMax = CInt(NumericAlbumNombre.Value) : My.Settings.AlbumNombreMax = AlbumNombreMax
        AlbumNombreAleatoire = CheckBoxAlbumAleatoire.Checked : My.Settings.AlbumNombreAleatoire = AlbumNombreAleatoire

        If CheckBox2.Checked = True Then
            If RadioButton1.Checked = True Then PhotoAleatoire = 1
            If RadioButton2.Checked = True Then PhotoAleatoire = 2
            If RadioButton5.Checked = True Then PhotoAleatoire = 3
        Else
            PhotoAleatoire = 0
        End If

        My.Settings.PhotoAleatoire = PhotoAleatoire

        If CheckBox11.Checked <> PhotoModeCroissant Then
            If CheckBox11.Checked = True Then
                Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
                'NumPhoto = 0
                PhotoModeCroissant = True
                My.Settings.PhotoModeCroissant = PhotoModeCroissant
            Else
                Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Descending)
                'NumPhoto = 0
                PhotoModeCroissant = False
                My.Settings.PhotoModeCroissant = PhotoModeCroissant
            End If
        End If


        If CheckBox12.Checked <> VideoModeCroissant Then
            If CheckBox12.Checked = True Then
                Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
                'NumVideo = 0
                VideoModeCroissant = True
                My.Settings.VideoModeCroissant = VideoModeCroissant
            Else
                Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Descending)
                'NumVideo = 0
                VideoModeCroissant = False
                My.Settings.VideoModeCroissant = VideoModeCroissant
            End If
        End If





        VideoAleatoire = CheckBox3.Checked : My.Settings.VideoAleatoire = VideoAleatoire
        VideoWithAudio = CheckBox4.Checked : My.Settings.VideoWithAudio = VideoWithAudio

        Borderless = CheckBox7.Checked : My.Settings.BorderLess = Borderless
        AnimerLesVideos = CheckBoxAnimerVideos.Checked : My.Settings.AnimerLesVideos = AnimerLesVideos
        AnimationVideo = Math.Max(0, ComboBoxAnimationVideo.SelectedIndex) : My.Settings.AnimationVideo = AnimationVideo
        AnimationVideoMouvement = Math.Max(0, ComboBoxAnimationVideoMouvement.SelectedIndex) : My.Settings.AnimationVideoMouvement = AnimationVideoMouvement
        AnimationVideoForce = Math.Max(0, ComboBoxAnimationVideoForce.SelectedIndex) : My.Settings.AnimationVideoForce = AnimationVideoForce
        AnimationVideoCouleurs = Math.Max(0, ComboBoxAnimationVideoCouleurs.SelectedIndex) : My.Settings.AnimationVideoCouleurs = AnimationVideoCouleurs
        AnimationVideoBandes = If(ComboBoxAnimationVideoBandes.SelectedIndex = 1, 2, 0) : My.Settings.AnimationVideoBandes = AnimationVideoBandes     ' (2 : agrandir, voir VideoVivante)
        FondFlouVideo = CheckBoxFondFlouVideo.Checked : My.Settings.FondFlouVideo = FondFlouVideo
        If FullScreen <> True Then
            If Borderless = True Then Form3.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
            If Borderless = False Then Form3.FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable
        End If


        AfficheDate = CheckBox8.Checked : My.Settings.AfficheDate = AfficheDate
        AfficheLieu = CheckBoxAfficheLieu.Checked : My.Settings.AfficheLieu = AfficheLieu
        LieuEnLigne = RadioLieuEnLigne.Checked : My.Settings.LieuEnLigne = LieuEnLigne
        AfficheClock = CheckBox13.Checked : My.Settings.AfficheClock = AfficheClock
        ClockTaillePourcent = TrackBarTailleMontre.Value : My.Settings.ClockTaillePourcent = ClockTaillePourcent
        My.Settings.MontreOpacite = TrackBarOpaciteMontre.Value
        FormHorloge.DefinirOpacite(TrackBarOpaciteMontre.Value)
        LegendeTaillePourcent = TrackBarTailleLegende.Value : My.Settings.LegendeTaillePourcent = LegendeTaillePourcent
        LegendeCoin = CoinDeLaListe() : My.Settings.LegendeCoin = LegendeCoin
        BandesPosition = PositionBandesDeLaListe() : My.Settings.BandesPosition = BandesPosition
        If ComboBoxPoliceBandes.SelectedItem IsNot Nothing Then BandesPolice = CStr(ComboBoxPoliceBandes.SelectedItem)
        My.Settings.BandesPolice = BandesPolice
        BandesRelief = CheckBoxReliefBandes.Checked : My.Settings.BandesRelief = BandesRelief
        My.Settings.MontreInclinee = CheckBoxInclinerMontre.Checked
        FormHorloge.DefinirInclinaison(CheckBoxInclinerMontre.Checked)
        My.Settings.MontreAngle = CInt(NumericAngleMontre.Value)
        FormHorloge.DefinirAngle(CInt(NumericAngleMontre.Value))
        LegendeCentreeSousPip =CheckBoxLegendeSousPip.Checked : My.Settings.LegendeCentreeSousPip = LegendeCentreeSousPip
        BandesActives = CheckBoxBandesActives.Checked : My.Settings.BandesActives = BandesActives
        BandesTaillePourcent = TrackBarTailleBandes.Value : My.Settings.BandesTaillePourcent = BandesTaillePourcent
        If DejaClock = False Then Call AfficheMontre()



        RetourAuDebutNetflix = CheckBox10.Checked
        My.Settings.RetourAuDebutNetflix = RetourAuDebutNetflix

        VLCBoost = CheckBox21.Checked
        My.Settings.VLCBoost = VLCBoost

        VolumeVLCBoost = TextBox17.Text
        My.Settings.VolumeVLCBoost = VolumeVLCBoost


        PipSizeDefault = TextBox18.Text
        My.Settings.PipSizeDefault = PipSizeDefault

        SansInternet = CheckBox20.Checked
        My.Settings.SansInternet = SansInternet

        StopWindowsUpdate = CheckBox23.Checked
        My.Settings.StopWindowsUpdate = StopWindowsUpdate

        ScreenSaverOff = CheckBoxEcranDeVeille.Checked
        My.Settings.ScreenSaverOff = ScreenSaverOff
        My.Settings.AnnonceMinuterie = CheckBoxAnnonceMinuterie.Checked
        AppliquerLEcranDeVeille()


        If ControlAdistance <> CheckBox30.Checked Then
            ControlAdistance = CheckBox30.Checked
            My.Settings.ControlAdistance = ControlAdistance
            Call GestionControlAdistance(ControlAdistance)
        End If


        IptvEnregistrerLesChamps()

        'IPTV_Login = TextBox19.Text
        'My.Settings.IPTV_login = IPTV_Login
        'IPTV_Password = TextBox20.Text
        'My.Settings.IPTV_password = IPTV_Password
        '

        'GroupePardefault = TextBox22.Text
        'My.Settings.GroupePardefault = GroupePardefault
        'User = TextBox23.Text
        'My.Settings.User = User
        'IPTV_OnlyFavo = CheckBox24.Checked
        'My.Settings.User = User
        'Call Button27.PerformClick()

        PlanB = CheckBox25.Checked
        My.Settings.PlanB = PlanB

        HoraireDeShabbatDansCadran = CheckBox27.Checked
        My.Settings.HoraireDeShabbatDansCadran = HoraireDeShabbatDansCadran

        If reprendreLeSlide <> CheckBox6.Checked Then
            If File.Exists(GridPhoto) Then My.Computer.FileSystem.DeleteFile(GridPhoto)
            If File.Exists(GridVideo) Then My.Computer.FileSystem.DeleteFile(GridVideo)
            If File.Exists(GridMusic) Then My.Computer.FileSystem.DeleteFile(GridMusic)
            If File.Exists(GridName) Then My.Computer.FileSystem.DeleteFile(GridName)
            ' slide.accdb n'est plus efface ici : il ne contient pas l'etat du diaporama mais la
            ' minuterie et la table Cities des villes. L'effacer (a chaque changement de cette case)
            ' faisait disparaitre les lieux et la minuterie (base recreee vide au demarrage suivant).
            Try
                FileSystem.Kill(AppPath & "\data\iptv*.m3u")
            Catch ex As Exception
            End Try

        End If

        reprendreLeSlide = CheckBox6.Checked : My.Settings.reprendreLeSlide = reprendreLeSlide

        DossierDivxParDefault = TextBox4.Text : My.Settings.DossierDivxParDefault = DossierDivxParDefault
        DossierDreamboxParDefault = TextBox5.Text : My.Settings.DossierDreamboxParDefault = DossierDreamboxParDefault
        DossierFreeboxParDefault = TextBox6.Text : My.Settings.DossierFreeboxParDefault = DossierFreeboxParDefault
        Dreambox_IP = TextBox10.Text : My.Settings.Dreambox_IP = Dreambox_IP
        FluxReseaux_IP = TextBox16.Text : My.Settings.FluxReseaux_IP = FluxReseaux_IP
        LatenceNetflix = TextBox24.Text : My.Settings.LatenceNetflix = LatenceNetflix

        AfficheHeureDeShabbat = CheckBox9.Checked : My.Settings.AfficheHeureDeShabbat = AfficheHeureDeShabbat
        AfficheVideoChavouaTov = CheckBox16.Checked : My.Settings.AfficheVideoChavouaTov = AfficheVideoChavouaTov
        AfficheVideoChabbatShalom = CheckBox26.Checked : My.Settings.AfficheVideoChabbatShalom = AfficheVideoChabbatShalom

        If onlyvideos <> CheckBox18.Checked Then FlagExit = True : FlagStop = False
        If onlyphotos <> CheckBox17.Checked Then FlagExit = True : FlagStop = False

        onlyvideos = CheckBox18.Checked : My.Settings.OnlyVideos = onlyvideos
        onlyphotos = CheckBox17.Checked : My.Settings.OnlyPhotos = onlyphotos

        If (CheckBox31.Checked = True And ModeRdv <> CheckBox31.Checked) Or ModeRdvDateDebut <> TextBox26.Text Then
            'CaseAcocherVide(1)
            'CaseAcocherVide(2)
            ModeRdvLastDatePhoto = "01/01/1900"
            ModeRdvLastDateVideo = "01/01/1900"

        End If

        ModeRdv = CheckBox31.Checked : My.Settings.ModeRDV = ModeRdv

        ModeRdvDateDebut = TextBox26.Text
        ModeRdvDateFin = TextBox27.Text
        ModeRdvLimitVideo = CInt(Val(TextBox28.Text))      ' (case vide : 0 = pas de limite ; avant, erreur de conversion)
        ModeRdvEnBoucle = CheckBox32.Checked

        If ModeRdv Then
            Dim dt1 As Date = ModeRdvDateDebut
            Dim dt2 As Date = ModeRdvDateFin
            If dt1 > dt2 Then
                TextBox27.Text = ModeRdvDateDebut
                TextBox26.Text = ModeRdvDateFin
                ModeRdvDateDebut = TextBox26.Text
                ModeRdvDateFin = TextBox27.Text
            End If
            RDV_AucunePhotoTrouvé = False
            RDV_AucuneVideoTrouvé = False
        End If

        My.Settings.ModeRdvDateDebut = ModeRdvDateDebut
        My.Settings.ModeRdvDateFin = ModeRdvDateFin
        My.Settings.ModeRdvLimitVideo = ModeRdvLimitVideo
        My.Settings.ModeRDVenBoucle = ModeRdvEnBoucle

        trouve = 0

        If Shabbat.Ville <> ComboBox1.Text Or Shabbat.ToutLesXminutes <> Val(TextBox12.Text) Or AfficheVideoChavouaTov = True Or AfficheVideoChabbatShalom = True Or AfficheHeureDeShabbat = True Then trouve = 1
        Shabbat.Ville = ComboBox1.Text : My.Settings.Shabbat_Ville = Shabbat.Ville
        Shabbat.ToutLesXminutes = Val(TextBox12.Text) : My.Settings.Shabbat_ToutLesXminutes = Shabbat.ToutLesXminutes

        If trouve = 1 Then Call HeureDeShabbat(Shabbat.Ville)
        'If AfficheHeureDeShabbat = True Then Form3.Timer1.Start() Else Form3.Timer1.Stop()

        Music = CheckBox5.Checked

        ' Plusieurs sources peuvent etre cochees (voir AlternanceMusique) : celle qui joue reste la source en cours
        ' si elle est toujours cochee, sinon la premiere (Fichiers MP3, Radio, Deezer). Les reglages Music_* restent
        ' exclusifs : ils designent la source en cours.
        Dim sourcesCochees As New List(Of String)
        If RadioButton4.Checked Then sourcesCochees.Add("mp3")
        If RadioButton3.Checked Then sourcesCochees.Add("radio")
        If RadioButton6.Checked Then sourcesCochees.Add("deezer")
        Dim sourceEnCours As String = AlternanceMusique.SourceEnCours()
        If sourcesCochees.Count > 0 AndAlso Not sourcesCochees.Contains(sourceEnCours) Then sourceEnCours = AlternanceMusique.SuivanteApres("", sourcesCochees)
        If sourcesCochees.Count = 0 Then sourceEnCours = ""
        Music_Radio = (sourceEnCours = "radio")
        Music_Deezer = (sourceEnCours = "deezer")
        Music_Mp3 = (sourceEnCours = "mp3")
        AlternanceMusique.Enregistrer(sourcesCochees, CInt(NumericAlternance.Value))

        EtapeOk("reglages jusqu'aux sources de musique")
        Application.DoEvents()

        ' Deezer dans Slide (MusiqueDeezer) ou application Deezer externe.
        DeezerInterne = CheckBoxDeezerInterne.Checked
        ' Volume du Deezer integre : enregistre, et applique s'il joue (la minuterie garde le
        ' dernier mot quand elle impose un volume de musique).
        VolumeDeezer = TrackBarVolumeDeezer.Value : My.Settings.VolumeDeezer = VolumeDeezer
        If MusiqueDeezer.Actif Then MusiqueDeezer.DefinirVolume(If(VolumeDeezerParMinuterie >= 0, VolumeDeezerParMinuterie, VolumeDeezer))
        ' Playlist que le Deezer integre lance (vide : sa file d'attente). Elle est enregistree
        ' ici, et jouee un peu plus bas (MusiqueDeezer.AppliquerLaPlaylistDesReglages).
        Dim playlistDeezer As String = TextBoxDeezerPlaylist.Text.Trim()
        If playlistDeezer <> "" AndAlso MusiqueDeezer.AdresseDeezer(playlistDeezer) = "" Then
            MsgBox("Playlist Deezer : ce n'est pas une adresse de Deezer, elle n'est pas enregistrée." & vbCrLf & playlistDeezer, MsgBoxStyle.Exclamation)
            playlistDeezer = DeezerPlaylist
        End If
        If playlistDeezer <> DeezerPlaylist Then
            DeezerPlaylist = playlistDeezer : My.Settings.DeezerPlaylist = DeezerPlaylist
        End If
        ' Case "La lancer quand je clique sur OK" (demande de Jacob, 08/10). Decochee : OK ne
        ' change pas la musique en cours ; la playlist du champ servira au prochain lancement.
        DeezerPlaylistAuOK = CheckBoxDeezerPlaylistOK.Checked : My.Settings.DeezerPlaylistAuOK = DeezerPlaylistAuOK
        If Not DeezerPlaylistAuOK Then MusiqueDeezer.GarderLaMusiqueEnCours()
        ' Pochette et titre du morceau de Deezer sur l'image, comme un PIP (demande de Jacob, 08/10/2026).
        DeezerCarte = CheckBoxDeezerCarte.Checked : My.Settings.DeezerCarte = DeezerCarte
        DeezerCarteCoin = CoinDeLaCarteDeezer() : My.Settings.DeezerCarteCoin = DeezerCarteCoin
        DeezerCartePourcent = TrackBarDeezerCarte.Value : My.Settings.DeezerCartePourcent = DeezerCartePourcent
        DeezerCarteSecondes = SecondesDeLaCarteDeezer() : My.Settings.DeezerCarteSecondes = DeezerCarteSecondes
        DeezerIconeSource = CheckBoxDeezerIconeSource.Checked : My.Settings.DeezerIconeSource = DeezerIconeSource
        ControlTuya1.EnregistrerLesReglages()           ' (compte Tuya et commandes de la minuterie)
        ' Apparence professionnelle (sombre) : prise en compte au prochain lancement de Slide (voir StylePro).
        My.Settings.ThemePro = CheckBoxThemePro.Checked
        ' Compte Deezer : Slide remplit lui-meme la page de connexion du Deezer integre. Le mot
        ' de passe est enregistre CHIFFRE par Windows (MusiqueDeezer.Proteger), jamais en clair.
        EnregistrerLeCompteDeezer()

        EtapeOk("compte et carte Deezer")
        ' (La radio comptait pour rien dans cette comparaison : decocher "Radio Internet", seule source, laissait la radio jouer - 21/10/2026.)
        If Music_Mp3 <> My.Settings.Music_Mp3 Or Music_Radio <> My.Settings.Music_Radio Or Music_Deezer <> My.Settings.Music_Deezer Or Music <> My.Settings.Music Or RadioUrl <> TextBox13.Text Or
           DeezerInterne <> My.Settings.DeezerInterne Then
            Music_SWITCH_ON(Music)
        End If
        EtapeOk("changement de source de musique")
        My.Settings.DeezerInterne = DeezerInterne
        ' OK des reglages : le Deezer integre joue la playlist du champ Playlist (demande de
        ' Jacob, 08/10), meme si elle n'a pas change : on a pu lancer autre chose a la main dans la
        ' fenetre Deezer. Tout de suite si la musique du diaporama doit jouer en ce moment (photos,
        ' video muette, pas de PIP avec son) ; sinon quand elle reprendra.
        Try
            Dim musiqueAttendue As Boolean = Music = True AndAlso PIPvolume = 0 AndAlso
                (Diffusion.Mode = "PHOTO" OrElse (Diffusion.Mode = "VIDEO" AndAlso VideoDuDiaporamaMuette()))
            If DeezerPlaylistAuOK Then MusiqueDeezer.AppliquerLaPlaylistDesReglages(musiqueAttendue)
        Catch ex As Exception
            DiagLog("Deezer (OK des reglages) : " & ex.Message)
        End Try
        EtapeOk("playlist Deezer")

        RadioUrl = TextBox13.Text
        My.Settings.RadioURL = RadioUrl
        My.Settings.Music_Radio = Music_Radio
        My.Settings.Music_Deezer = Music_Deezer
        My.Settings.Music_Mp3 = Music_Mp3
        My.Settings.Music = Music

        VolumeOFFegal1 = CheckBox28.Checked
        LisserLeSon = CheckBoxLisserSon.Checked : My.Settings.LisserLeSon = LisserLeSon
        RelanceNuitActive = CheckBoxRelanceNuit.Checked : My.Settings.RelanceNuitActive = RelanceNuitActive
        If ComboBoxRelanceNuit.SelectedIndex >= 0 Then RelanceNuitHeure = ComboBoxRelanceNuit.SelectedIndex
        My.Settings.RelanceNuitHeure = RelanceNuitHeure
        ' Frequence, en jours (ordre de la liste : une fois par semaine, une fois par mois).
        RelanceNuitFrequence = If(ComboBoxRelanceFrequence.SelectedIndex = 1, 30, 7)
        My.Settings.RelanceNuitFrequence = RelanceNuitFrequence
        ' Verification et installation automatiques des mises a jour a une heure precise (voir MiseAJour.Surveiller).
        My.Settings.Maj_VerifActive = CheckBoxMajHeure.Checked
        My.Settings.Maj_VerifHeure = DateTimePickerMajHeure.Value.ToString("HH:mm")
        My.Settings.VolumeOFFegal1 = VolumeOFFegal1

        'Form3.VlcControl1.Audio.Volume = 100 - 1 * (VolumeVLCBoost * (VLCBoost))  'true =-1 donc - par - = +
        'Form3.VlcControl2.Audio.Volume = 100 - 1 * (VolumeVLCBoost * (VLCBoost))  'true =-1 donc - par - = +
        'Form11.VlcControl1.Audio.Volume = 100 - 1 * (VolumeVLCBoost * (VLCBoost))  'true =-1 donc - par - = +
        'Form6.VlcControl1.Audio.Volume = 100 - 1 * (VolumeVLCBoost * (VLCBoost))  'true =-1 donc - par - = +
        'Form7.VlcControl1.Audio.Volume = 100 - 1 * (VolumeVLCBoost * (VLCBoost))  'true =-1 donc - par - = +
        'Form12.VlcControl1.Audio.Volume = 100 - 1 * (VolumeVLCBoost * (VLCBoost))  'true =-1 donc - par - = +

        MinuterieEnReseaux = CheckBox15.Checked : My.Settings.MinuterieEnReseaux = MinuterieEnReseaux
        DatabaseNetPath = TextBox15.Text : My.Settings.DatabaseNetPath = DatabaseNetPath


        If CheckBox15.Checked = True And TextBox15.Text = "" Then
            CheckBox15.Checked = False
            DatabasePath = DatabaseLocalPath
        End If

        ' (Ville non definie alors que VLC est le lecteur de TV : signale au debut, par ReglagesCoherents.)

        If ControlAdistance = True And DatabaseNetPath = "" Then
            a = MsgBox("Veuillez definir Le répertoire reseaux partagé ", MsgBoxStyle.OkCancel)
            Exit Sub
        End If



        If Button25.BackColor = CouleurDuChoix Then 'reactualisation DossierNflixParDefault si changement  de login iptv
            CheminFilm = DossierNflixParDefault
        End If

        ' Lignes sans action : retirees (jamais la ligne vierge du bas, ni pendant le parcours : "Impossible de supprimer la nouvelle ligne non
        ' validee" fermait Slide - journal du 20/10/2026 16:22).
        Dim lignesVides As New List(Of DataGridViewRow)
        For Each ligneVide As DataGridViewRow In DataGridView1.Rows
            If Not ligneVide.IsNewRow AndAlso Convert.ToString(ligneVide.Cells("Action").FormattedValue) = "" Then lignesVides.Add(ligneVide)
        Next
        For Each ligneVide As DataGridViewRow In lignesVides
            Try
                DataGridView1.Rows.Remove(ligneVide)
            Catch ex As Exception
                DiagLog("Minuterie : ligne vide non retiree : " & ex.Message)
            End Try
        Next
        For Each row As DataGridViewRow In DataGridView1.Rows
            If row.IsNewRow Then Continue For
            L_Action = row.Cells("Action").FormattedValue
            If Len(L_Action) > 255 Then
                trouve = row.Index
                DataGridView1.Rows(row.Index).Selected = True
                DataGridView1.FirstDisplayedScrollingRowIndex = trouve
                a = MsgBox("Ligne: " & (trouve + 1).ToString & "  Colonne Action texte trop long (max=255)", MsgBoxStyle.OkOnly)
                Exit Sub

            End If
        Next




        '''''''''''''
        EtapeOk("verifications avant l'enregistrement")
        My.Settings.Save()
        EtapeOk("My.Settings.Save")
        Me.Close()
        EtapeOk("fermeture de la fenetre")
        'Me.Dispose()
        If DataGridView1.RowCount <> 0 Then AjouterUneLigne(2, LigneCouranteMinuterie())
        EtapeOk("tri du tableau")




        If Not If(Diffusion.Mode, "").StartsWith("FILM") And Not If(Diffusion.Mode, "").StartsWith("TV") And Diffusion.Mode <> "VIDEO" Then retablieTousLesVolumes()
        EtapeOk("volumes retablis")
        Call UpdateData()
        EtapeOk("enregistrement de la minuterie (base)")

        If OptionChange = True Then

            Try
                If File.Exists(GridPhoto) Then My.Computer.FileSystem.DeleteFile(GridPhoto)
                If File.Exists(GridVideo) Then My.Computer.FileSystem.DeleteFile(GridVideo)
                If File.Exists(GridMusic) Then My.Computer.FileSystem.DeleteFile(GridMusic)
                If File.Exists(GridName) Then My.Computer.FileSystem.DeleteFile(GridName)
            Catch ex As Exception

            End Try
#If DEBUG Then
            Stop

#End If
            Application.Exit()
            Application.Restart()
            'wait(1)
            End
        End If


        NouvelleMinute = "ZZZZ" ' pour obligé à rerentrer dans la minuterie sans attendre d'arriver 1 minute


        If LancerLePremierLancementDeLaMinuterie = True Then
            PremierLancementDeLaMinuterie = True
        Else
            PremierLancementDeLaMinuterie = False
        End If


        If ModeRdv = True Then Call EndAndContinue()

    End Sub

    ''' <summary>Abscisse du bord droit d'un controle, exprimee dans les coordonnees client du form
    ''' (en cumulant les decalages des conteneurs : TabControl, TabPage...).</summary>
    Private Function BordDroitDansLeForm(c As Control) As Integer
        Dim x As Integer = c.Right
        Dim conteneur As Control = c.Parent
        Do While conteneur IsNot Nothing AndAlso Not ReferenceEquals(conteneur, Me)
            x += conteneur.Left
            conteneur = conteneur.Parent
        Loop
        Return x
    End Function

    ''' <summary>Hauteur de ligne confortable pour la police reellement utilisee au runtime.</summary>
    Private Sub AjusteHauteurDesLignes(grille As DataGridView)
        Dim h As Integer = grille.Font.Height + 10
        grille.RowTemplate.Height = h          ' lignes ajoutees plus tard (remplissage depuis Module2)
        For Each ligne As DataGridViewRow In grille.Rows
            ligne.Height = h                   ' lignes deja presentes
        Next
    End Sub

    Sub Form4_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim chronoLoad As Stopwatch = Stopwatch.StartNew()       ' (traces "Form4_Load : ... ms" : ou passe le temps d'ouverture sur un PC lent)

        ' Les hauteurs de ligne figees a 20 px dans le concepteur tronquent le texte avec la
        ' police par defaut de .NET : on les recalcule sur la police reelle.
        AjusteHauteurDesLignes(DataGridView1)
        AjusteHauteurDesLignes(DataGridView2)

        ' Taille de la fenetre : largeur jusqu'au bord droit du tableau, hauteur jusqu'en dessous
        ' du bouton "Quitter le programme" (Button24). On fixe ClientSize et non Width/Height,
        ' sinon les bordures et la barre de titre (dont l'epaisseur depend du DPI) rognent le
        ' contenu -- c'est ce que faisait l'ancien calcul base sur Button3.
        Const marge As Integer = 20
        Dim largeur As Integer = Math.Max(BordDroitDansLeForm(DataGridView1),
                                          BordDroitDansLeForm(DataGridView2)) + marge
        Dim hauteur As Integer = Button24.Bottom + marge

        ' Sans depasser l'ecran, sinon le bouton Quitter se retrouve hors champ.
        Dim ecran As Rectangle = Screen.FromControl(Me).WorkingArea
        largeur = Math.Min(largeur, ecran.Width - (Me.Width - Me.ClientSize.Width))
        hauteur = Math.Min(hauteur, ecran.Height - (Me.Height - Me.ClientSize.Height))

        Me.ClientSize = New Size(largeur, hauteur)

        DiagLog("Form4 : DPI=" & Me.DeviceDpi &
                " police=" & Me.Font.Name & " " & Me.Font.SizeInPoints & "pt (hauteur " & Me.Font.Height & "px)" &
                " echelle=" & Me.CurrentAutoScaleDimensions.ToString() &
                " ClientSize=" & Me.ClientSize.ToString() &
                " bordTableau=" & largeur - marge &
                " basQuitter=" & Button24.Bottom &
                " hauteurLigne=" & DataGridView1.RowTemplate.Height)

        Me.StartPosition = FormStartPosition.CenterScreen

        ' Version du programme, en haut a droite de l'onglet Sources (numero compile dans l'exe, voir VersionAuto.vb).
        LabelVersion.Text = "Slide version " & SlideVersion
        LabelVersionMinuterie.Text = "Slide version " & SlideVersion      ' (meme texte, en haut a droite de l'onglet Minuterie)

        Me.ListView1.Items.Clear()
        If My.Settings.CheminPhotoVideo IsNot Nothing Then



            Try
                For Each item In My.Settings.CheminPhotoVideo

                    Dim Litem As ListViewItem
                    Litem = New ListViewItem()


                    opt = "1111"
                    op = InStr(item, "@")
                    If op > 0 Then
                        opt = Mid(item, op + 1)
                        item = Mid(item, 1, op - 1)
                    End If

                    'If OptActif = 1 Then Litem.Checked = True
                    Litem.Tag = opt
                    Litem.Text = item
                    OptActif = Mid(opt, 1, 1)
                    OptPhoto = Mid(opt, 2, 1) : OptPhotoNB = "" : If OptPhoto <> "1" Then OptPhotoNB = "NON"
                    OptVideo = Mid(opt, 3, 1) : OptVideoNB = "" : If OptVideo <> "1" Then OptVideoNB = "NON"
                    OptMusic = Mid(opt, 4, 1) : OptMusicNB = "" : If OptMusic <> "1" Then OptMusicNB = "NON"

                    ''''''''''''
                    Litem.SubItems(0).Text = item
                    Litem.SubItems.Add(OptPhotoNB)
                    Litem.SubItems.Add(OptVideoNB)
                    Litem.SubItems.Add(OptMusicNB)

                    'Litem.ForeColor = Color.Red
                    Litem.Checked = OptActif
                    ListView1.Items.Add(Litem)

                    ''''''''''''''
                    'ListView1.Items.Add(Litem)


                Next
            Catch ex As Exception
            End Try

        End If


        DiagLog("Form4_Load : sources lues a " & chronoLoad.ElapsedMilliseconds & " ms")
        'Call LoadUserValue()

        TextBox1.Text = DurréePhoto : TrackBar5.Value = DurréePhoto
        TextBox2.Text = DurréeVideo : TrackBar4.Value = DurréeVideo
        TextBox3.Text = DurréeAffichageDuneImage : TrackBar6.Value = DurréeAffichageDuneImage
        TextBox4.Text = DossierDivxParDefault
        TextBox5.Text = DossierDreamboxParDefault
        TextBox6.Text = DossierFreeboxParDefault
        TextBox7.Text = VolumeGeneral : TrackBar1.Value = VolumeGeneral
        TextBox8.Text = VolumeMusicSlide : TrackBar2.Value = VolumeMusicSlide
        TextBox9.Text = VolumeVideoSlide : TrackBar3.Value = VolumeVideoSlide
        TextBox11.Text = VolumeTVParDefault : TrackBar7.Value = VolumeTVParDefault
        TextBox14.Text = VolumeDreamboxParDefault : TrackBar9.Value = VolumeDreamboxParDefault
        TextBox25.Text = VolumeNetflixPrimeParDefault : TrackBar12.Value = VolumeNetflixPrimeParDefault

        TextBox10.Text = Dreambox_IP
        TextBox16.Text = FluxReseaux_IP
        TextBox15.Text = DatabaseNetPath
        TextBox17.Text = VolumeVLCBoost : TrackBar10.Value = VolumeVLCBoost
        TextBox18.Text = PipSizeDefault : TrackBar11.Value = PipSizeDefault
        TextBox24.Text = LatenceNetflix

        CheckBox1.Checked = FormatPhoto169
        CheckBoxAnimerPhotos.Checked = AnimerLesPhotos
        ' (memes numeros que AnimationPhoto.Arrivee et AnimationPhoto.Mouvement)
        ComboBoxAnimationArrivee.SelectedIndex = Math.Max(0, Math.Min(ComboBoxAnimationArrivee.Items.Count - 1, AnimationArrivee))
        ComboBoxAnimationMouvement.SelectedIndex = Math.Max(0, Math.Min(ComboBoxAnimationMouvement.Items.Count - 1, AnimationMouvement))
        ComboBoxAnimationArrivee.Enabled = AnimerLesPhotos
        ComboBoxAnimationMouvement.Enabled = AnimerLesPhotos
        ComboBoxAnimationPhotoForce.SelectedIndex = Math.Max(0, Math.Min(ComboBoxAnimationPhotoForce.Items.Count - 1, AnimationPhotoForce))
        ComboBoxAnimationPhotoForce.Enabled = AnimerLesPhotos
        ComboBoxAnimationPhotoCouleurs.SelectedIndex = Math.Max(0, Math.Min(ComboBoxAnimationPhotoCouleurs.Items.Count - 1, If(AnimationPhotoCouleurs >= 2, 0, AnimationPhotoCouleurs)))
        ComboBoxAnimationPhotoCouleurs.Enabled = AnimerLesPhotos
        ' Animations tirees au hasard : ce qui est coche dans la fenetre "Choix du hasard...".
        _hasardPhotoArrivee = HasardPhotoArrivee
        _hasardPhotoMouvement = HasardPhotoMouvement
        _hasardVideoTransition = HasardVideoTransition
        _hasardVideoMouvement = HasardVideoMouvement
        _hasardPhotoCouleurs = HasardPhotoCouleurs
        _hasardVideoCouleurs = HasardVideoCouleurs
        ButtonHasardPhotos.Enabled = AnimerLesPhotos
        ButtonHasardVideos.Enabled = AnimerLesVideos
        CheckBox29.Checked = PhotoAlbum
        NumericAlbumNombre.Value = Math.Max(NumericAlbumNombre.Minimum, Math.Min(NumericAlbumNombre.Maximum, AlbumNombreMax))
        NumericAlbumNombre.Enabled = PhotoAlbum
        LabelAlbumNombre.Enabled = PhotoAlbum
        CheckBoxAlbumAleatoire.Checked = AlbumNombreAleatoire
        CheckBoxAlbumAleatoire.Enabled = PhotoAlbum
        MettreAJourLeNombreAlbum()

        If PhotoAleatoire = 0 Then CheckBox2.Checked = False
        If PhotoAleatoire = 1 Then CheckBox2.Checked = True : RadioButton1.Checked = True
        If PhotoAleatoire = 2 Then CheckBox2.Checked = True : RadioButton2.Checked = True
        If PhotoAleatoire = 3 Then CheckBox2.Checked = True : RadioButton5.Checked = True

        CheckBox3.Checked = VideoAleatoire
        CheckBox4.Checked = VideoWithAudio
        CheckBox5.Checked = Music
        CheckBox6.Checked = reprendreLeSlide
        CheckBox7.Checked = Borderless
        CheckBoxAnimerVideos.Checked = AnimerLesVideos
        ComboBoxAnimationVideo.SelectedIndex = Math.Max(0, Math.Min(ComboBoxAnimationVideo.Items.Count - 1, AnimationVideo))
        ComboBoxAnimationVideo.Enabled = AnimerLesVideos
        ComboBoxAnimationVideoMouvement.SelectedIndex = Math.Max(0, Math.Min(ComboBoxAnimationVideoMouvement.Items.Count - 1, AnimationVideoMouvement))
        ComboBoxAnimationVideoMouvement.Enabled = AnimerLesVideos
        ComboBoxAnimationVideoForce.SelectedIndex = Math.Max(0, Math.Min(ComboBoxAnimationVideoForce.Items.Count - 1, AnimationVideoForce))
        ComboBoxAnimationVideoForce.Enabled = AnimerLesVideos
        ComboBoxAnimationVideoCouleurs.SelectedIndex = Math.Max(0, Math.Min(ComboBoxAnimationVideoCouleurs.Items.Count - 1, If(AnimationVideoCouleurs >= 2, 0, AnimationVideoCouleurs)))
        ComboBoxAnimationVideoCouleurs.Enabled = AnimerLesVideos
        ComboBoxAnimationVideoBandes.SelectedIndex = If(AnimationVideoBandes = 2, 1, 0)
        CheckBoxFondFlouVideo.Checked = FondFlouVideo
        ComboBoxAnimationVideoBandes.Enabled = AnimerLesVideos
        CheckBox8.Checked = AfficheDate
        CheckBoxAfficheLieu.Checked = AfficheLieu
        RadioLieuEnLigne.Checked = LieuEnLigne
        RadioLieuHorsLigne.Checked = Not LieuEnLigne
        ActiveChoixLocalite()
        CheckBox9.Checked = AfficheHeureDeShabbat
        CheckBox10.Checked = RetourAuDebutNetflix
        CheckBox11.Checked = PhotoModeCroissant
        CheckBox12.Checked = VideoModeCroissant
        CheckBox13.Checked = AfficheClock
        CheckBoxInclinerMontre.Checked = My.Settings.MontreInclinee
        NumericAngleMontre.Value = Math.Max(NumericAngleMontre.Minimum, Math.Min(NumericAngleMontre.Maximum, My.Settings.MontreAngle))
        NumericAngleMontre.Enabled = CheckBoxInclinerMontre.Checked
        _chargementTailleMontre = True
        TrackBarTailleMontre.Value = Math.Max(TrackBarTailleMontre.Minimum, Math.Min(TrackBarTailleMontre.Maximum, ClockTaillePourcent))
        TrackBarOpaciteMontre.Value = Math.Max(TrackBarOpaciteMontre.Minimum, Math.Min(TrackBarOpaciteMontre.Maximum, My.Settings.MontreOpacite))
        LabelValeurOpaciteMontre.Text = TrackBarOpaciteMontre.Value & " %"
        LabelValeurTailleMontre.Text = TrackBarTailleMontre.Value & " %"
        TrackBarTailleLegende.Value = Math.Max(TrackBarTailleLegende.Minimum, Math.Min(TrackBarTailleLegende.Maximum, LegendeTaillePourcent))
        LabelValeurTailleLegende.Text = TrackBarTailleLegende.Value & " %"
        ComboBoxCoinLegende.SelectedIndex = Math.Max(0, Array.IndexOf(CodesCoinLegende, LegendeCoin))
        ComboBoxPositionBandes.SelectedIndex = Math.Max(0, Array.IndexOf(CodesPositionBandes, BandesPosition))
        ' Polices installees (celle des reglages en tete de selection ; Arial Black s'il n'est pas installe).
        DiagLog("Form4_Load : reglages affiches a " & chronoLoad.ElapsedMilliseconds & " ms")
        ComboBoxPoliceBandes.Items.Clear()
        ' Noms seulement, sans IsStyleAvailable : ce test charge chaque police une a une (plusieurs secondes sur un PC lent). Une police
        ' sans style "normal" (rare) se voit tout de meme : la police choisie est dessinee telle quelle par GDI+, avec son style de repli.
        Dim chronoPolices As Stopwatch = Stopwatch.StartNew()
        ComboBoxPoliceBandes.BeginUpdate()
        Try
            ComboBoxPoliceBandes.Items.AddRange(FontFamily.Families.Select(Function(f) CObj(f.Name)).ToArray())
        Finally
            ComboBoxPoliceBandes.EndUpdate()
        End Try
        DiagLog("Form4_Load : " & ComboBoxPoliceBandes.Items.Count & " polices listees en " & chronoPolices.ElapsedMilliseconds & " ms")
        Dim iPolice As Integer = ComboBoxPoliceBandes.FindStringExact(BandesPolice)
        If iPolice < 0 Then iPolice = ComboBoxPoliceBandes.FindStringExact("Arial Black")
        If iPolice < 0 AndAlso ComboBoxPoliceBandes.Items.Count > 0 Then iPolice = 0
        If iPolice >= 0 Then ComboBoxPoliceBandes.SelectedIndex = iPolice
        CheckBoxReliefBandes.Checked = BandesRelief
        CheckBoxLegendeSousPip.Checked = LegendeCentreeSousPip
        CheckBoxBandesActives.Checked = BandesActives
        TrackBarTailleBandes.Value = Math.Max(TrackBarTailleBandes.Minimum, Math.Min(TrackBarTailleBandes.Maximum, BandesTaillePourcent))
        LabelValeurTailleBandes.Text = TrackBarTailleBandes.Value & " %"
        _chargementTailleMontre = False
        CheckBox15.Checked = MinuterieEnReseaux
        CheckBox16.Checked = AfficheVideoChavouaTov
        CheckBox26.Checked = AfficheVideoChabbatShalom
        CheckBox17.Checked = onlyphotos
        CheckBox18.Checked = onlyvideos
        CheckBox20.Checked = SansInternet
        CheckBox21.Checked = VLCBoost
        CheckBox23.Checked = StopWindowsUpdate
        CheckBoxEcranDeVeille.Checked = ScreenSaverOff
        CheckBoxAnnonceMinuterie.Checked = My.Settings.AnnonceMinuterie
        TextBoxIptvNom1.Text = IptvSources.NomIptv(0) : TextBox21.Text = IptvSources.UrlIptv(0)
        TextBoxIptvNom2.Text = IptvSources.NomIptv(1) : TextBoxIptvUrl2.Text = IptvSources.UrlIptv(1)
        TextBoxIptvNom3.Text = IptvSources.NomIptv(2) : TextBoxIptvUrl3.Text = IptvSources.UrlIptv(2)
        TextBoxIptvUtilisateur1.Text = IptvSources.UtilisateurIptv(0) : TextBoxIptvMotDePasse1.Text = IptvSources.MotDePasseIptv(0)
        TextBoxIptvUtilisateur2.Text = IptvSources.UtilisateurIptv(1) : TextBoxIptvMotDePasse2.Text = IptvSources.MotDePasseIptv(1)
        TextBoxIptvUtilisateur3.Text = IptvSources.UtilisateurIptv(2) : TextBoxIptvMotDePasse3.Text = IptvSources.MotDePasseIptv(2)
        CheckBox25.Checked = PlanB
        CheckBox27.Checked = HoraireDeShabbatDansCadran
        CheckBox28.Checked = VolumeOFFegal1
        CheckBoxLisserSon.Checked = LisserLeSon
        CheckBoxRelanceNuit.Checked = RelanceNuitActive
        ComboBoxRelanceNuit.SelectedIndex = Math.Max(0, Math.Min(23, RelanceNuitHeure))
        ComboBoxRelanceNuit.Enabled = RelanceNuitActive
        ComboBoxRelanceFrequence.SelectedIndex = If(RelanceNuitFrequence >= 30, 1, 0)
        ComboBoxRelanceFrequence.Enabled = RelanceNuitActive
        CheckBoxMajHeure.Checked = My.Settings.Maj_VerifActive
        Dim heureMaj As Date
        If Not Date.TryParseExact(My.Settings.Maj_VerifHeure, "HH:mm", Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, heureMaj) Then heureMaj = Date.Today.AddHours(4)
        DateTimePickerMajHeure.Value = heureMaj
        DateTimePickerMajHeure.Enabled = CheckBoxMajHeure.Checked
        CheckBox30.Checked = ControlAdistance
        CheckBox31.Checked = ModeRdv
        TextBox26.Text = ModeRdvDateDebut
        TextBox27.Text = ModeRdvDateFin
        TextBox28.Text = ModeRdvLimitVideo
        CheckBox32.Checked = ModeRdvEnBoucle
        ' Par defaut : de la photo / video la plus ancienne a la plus recente (voir DatesRdvParDefaut).
        DatesRdvParDefaut(seulementSiAbsentes:=ModeRdv)

        If CheckBox9.Checked = True Then
            CheckBox27.Enabled = True
        Else
            CheckBox27.Enabled = False
        End If




        Button21.Enabled = True
        TextBox15.Enabled = True

        ComboBox1.Items.Clear()

        For i = 1 To 50
            If City(i, 0) <> "" Then ComboBox1.Items.Add(City(i, 0))
        Next

        'ComboBox1.SelectedIndex = 0

        'If Shabbat.ToutLesXminutes = 0 Then Shabbat.ToutLesXminutes = 120

        TextBox12.Text = Shabbat.ToutLesXminutes : TrackBar8.Value = Shabbat.ToutLesXminutes

        trouve = 0
        For i = 0 To ComboBox1.Items.Count - 1
            If ComboBox1.Items(i) = Shabbat.Ville Then ComboBox1.SelectedIndex = i : Call HeureDeShabbat(Shabbat.Ville) : Label25.Text = "Entrée" & Shabbat.Debut & " Sortie" & Shabbat.Fin : trouve = 1
        Next

        If trouve = 0 Then
            Shabbat.Ville = "Paris" : ComboBox1.SelectedIndex = 0 : Call HeureDeShabbat(Shabbat.Ville) : Label25.Text = "Entrée" & Shabbat.Debut & " Sortie" & Shabbat.Fin
        End If

        DiagLog("Form4_Load : Shabbat/TV faits a " & chronoLoad.ElapsedMilliseconds & " ms")
        For i = 0 To Me.ComboBox2.Items.Count - 1
            If Trim(UCase(Radio(i, 1))) = Trim(UCase(RadioUrl)) Then Me.ComboBox2.SelectedIndex = i
        Next
        DiagLog("Form4_Load : radios faites a " & chronoLoad.ElapsedMilliseconds & " ms")




        Dim sourcesGardees As List(Of String) = AlternanceMusique.Sources()
        RadioButton3.Checked = sourcesGardees.Contains("radio")
        RadioButton6.Checked = sourcesGardees.Contains("deezer")
        RadioButton4.Checked = sourcesGardees.Contains("mp3")
        NumericAlternance.Value = Math.Max(1, Math.Min(240, My.Settings.Music_AlternanceMinutes))
        CheckBoxDeezerInterne.Checked = DeezerInterne
        TrackBarVolumeDeezer.Value = Math.Max(0, Math.Min(100, VolumeDeezer)) : TextBoxVolumeDeezer.Text = TrackBarVolumeDeezer.Value
        TextBoxDeezerPlaylist.Text = DeezerPlaylist
        CheckBoxDeezerPlaylistOK.Checked = DeezerPlaylistAuOK
        _chargementCarteDeezer = True
        CheckBoxDeezerCarte.Checked = DeezerCarte
        CheckBoxDeezerIconeSource.Checked = DeezerIconeSource
        ControlTuya1.ChargerLesReglages()
        ComboBoxDeezerCarteCoin.SelectedIndex = Math.Max(0, Array.IndexOf(CodesCoinLegende, DeezerCarteCoin))
        TrackBarDeezerCarte.Value = Math.Max(TrackBarDeezerCarte.Minimum, Math.Min(TrackBarDeezerCarte.Maximum, DeezerCartePourcent))
        LabelValeurDeezerCarte.Text = TrackBarDeezerCarte.Value & " %"
        ' Toujours affichee (0), ou seulement quelques secondes a chaque nouveau morceau.
        RadioDeezerCarteDuree.Checked = DeezerCarteSecondes > 0
        RadioDeezerCarteToujours.Checked = DeezerCarteSecondes <= 0
        If DeezerCarteSecondes > 0 Then NumericDeezerCarteSecondes.Value = Math.Max(NumericDeezerCarteSecondes.Minimum, Math.Min(NumericDeezerCarteSecondes.Maximum, DeezerCarteSecondes))
        NumericDeezerCarteSecondes.Enabled = RadioDeezerCarteDuree.Checked
        _chargementCarteDeezer = False
        CheckBoxThemePro.Checked = My.Settings.ThemePro
        TextBoxDeezerLogin.Text = DeezerLogin
        TextBoxDeezerMotDePasse.Text = MusiqueDeezer.Deproteger(DeezerMotDePasse)

        Dim cbJour As DataGridViewComboBoxColumn = DirectCast(DataGridView1.Columns("Jour"), DataGridViewComboBoxColumn)
        cbJour.Items.Clear()
        cbJour.Items.AddRange(jourDeLaSemaine)

        Dim cbType As DataGridViewComboBoxColumn = DirectCast(DataGridView1.Columns("Type"), DataGridViewComboBoxColumn)
        cbType.Items.Clear()
        cbType.Items.AddRange({"Film", "TV", "Sat", "IPTV", "Radio", "Deezer", "Music Source", "Son", "Yom Tov", "Volume", "Ecran Noir", "Mode RDV", "PhotoVideo Mode", "Photo Mode", "Video Mode", "Sonoff", "Tuya", "Lancer", "Autres", "Divers"})


        Dim vol(100) As String
        For i = 0 To 100
            vol(i) = i
        Next

        Dim cbVolume As DataGridViewComboBoxColumn = DirectCast(DataGridView1.Columns("Volume"), DataGridViewComboBoxColumn)
        cbVolume.Items.Clear()
        cbVolume.Items.AddRange(vol)
        cbVolume.ToolTipText = "Molette de la souris sur la case : volume par pas de 5 (Ctrl + molette : pas de 1). Plusieurs lignes selectionnees : elles changent ensemble."
        lt = DataGridView1.Width - DataGridView1.RowHeadersWidth - DataGridView1.ColumnHeadersHeight

        DataGridView1.Columns("Actif").Width = lt * 30 / 784.5
        DataGridView1.Columns("Jour").Width = lt * 80 / 784.5
        DataGridView1.Columns("Heure").Width = lt * 50 / 784.5
        DataGridView1.Columns("Type").Width = lt * 88 / 784.5
        ' Colonne Play elargie (110 -> 160) pour le repere PIP et sa taille, a cote de la duree ou
        ' de la plage "debut - fin" ; la place est prise sur la colonne Action (276 -> 226).
        ' Colonne Action elargie de 25 % (226 -> 282,5 ; demande de Jacob, 09/10/2026) : les grilles et
        ' la fenetre sont elargies d'autant dans le concepteur, les autres colonnes gardent leur largeur.
        DataGridView1.Columns("Action").Width = lt * 282.5 / 784.5
        DataGridView1.Columns("Play").Width = lt * 160 / 784.5
        DataGridView1.Columns("Durée").Width = lt * 50 / 784.5
        DataGridView1.Columns("Volume").Width = lt * 45 / 784.5

        lt = DataGridView2.Width - DataGridView2.RowHeadersWidth - DataGridView2.ColumnHeadersHeight

        DataGridView2.Columns("Inactif").Width = lt * 30 / 784.5
        DataGridView2.Columns("Jour2").Width = lt * 80 / 784.5
        DataGridView2.Columns("Heure2").Width = lt * 50 / 784.5
        DataGridView2.Columns("Type2").Width = lt * 88 / 784.5
        DataGridView2.Columns("Action2").Width = lt * 282.5 / 784.5
        DataGridView2.Columns("Play2").Width = lt * 160 / 784.5
        DataGridView2.Columns("Durée2").Width = lt * 50 / 784.5
        DataGridView2.Columns("Volume2").Width = lt * 45 / 784.5

        'Button16.BackgroundImageLayout = ImageLayout.Stretch
        'Button16.BackgroundImage = Image.FromFile(AppPath & "\ico\Imprim2.ico")





        DiagLog("Form4_Load : musique et Deezer faits a " & chronoLoad.ElapsedMilliseconds & " ms")
        ChargeDataMinuterieActive()
        For Each ligneVolume As DataGridViewRow In DataGridView1.Rows
            NettoyerLeVolume(ligneVolume)             ' (volume inutile pour la tache : case videe)
        Next
        YomTov.CompleterLesCasesPlay()                ' (fetes juives : la case Play montre la video de l'annonce)
        DiagLog("Form4_Load : fin a " & chronoLoad.ElapsedMilliseconds & " ms")
        MettreAJourLIconeRadio()
        TimerIconeRadio.Start()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Me.Close()
        Exit Sub

    End Sub



    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click

        Me.Cursor = Cursors.WaitCursor

        My.Settings.CheminPhotoVideo = New Specialized.StringCollection

        For i = 0 To Me.ListView1.Items.Count - 1
            Opt = "1111"
            If Me.ListView1.Items(i).Checked = False Then
                mid(Opt, 1, 1) = "0"
            End If
            For q = 2 To 4
                Try
                    If ListView1.Items(i).SubItems(q - 1).Text = "NON" Then
                        mid(Opt, q, 1) = "0"
                    End If
                Catch ex As Exception
                    MsgBox(ex.Message)
                End Try
            Next

            My.Settings.CheminPhotoVideo.Add(Me.ListView1.Items(i).SubItems(0).Text & "@" & Opt)
        Next


        For Each item In ListView1.Items
            item.Remove()
        Next



        For Each item In My.Settings.CheminPhotoVideo

            Opt = "1111"

            op = InStr(item, "@")
            If op > 0 Then
                Opt = Mid(item, op + 1)
                item = Mid(item, 1, op - 1)
            End If
            OptActif = Mid(Opt, 1, 1)

            OptPhoto = Mid(Opt, 2, 1) : OptPhotoNB = "NON" : If OptPhoto = 1 Then OptPhotoNB = NBPhotosInDir(item)
            OptVideo = Mid(Opt, 3, 1) : OptVideoNB = "NON" : If OptVideo = 1 Then OptVideoNB = NBVideosInDir(item)
            OptMusic = Mid(Opt, 4, 1) : OptMusicNB = "NON" : If OptMusic = 1 Then OptMusicNB = NBMusicInDir(item)

            Application.DoEvents()

            Dim Litem As New ListViewItem
            'Dim Litem As ListViewItem = New ListViewItem(New String() {item, OptPhotoNB, OptVideoNB, OptMusicNB})

            Litem.SubItems(0).Text = item
            Litem.SubItems.Add(OptPhotoNB)
            Litem.SubItems.Add(OptVideoNB)
            Litem.SubItems.Add(OptMusicNB)

            'Litem.ForeColor = Color.Red
            Litem.Checked = OptActif
            ListView1.Items.Add(Litem)
        Next



        Me.Cursor = Cursors.Default
    End Sub




    Private Sub Button7_Click(sender As Object, e As EventArgs) Handles Button7.Click

        'On Error Resume Next
        For Each row As DataGridViewRow In DataGridView1.SelectedRows
            DataGridView1.Rows.Remove(row)
        Next
    End Sub





    Private Sub DataGridView1_EditingControlShowing(ByVal sender As System.Object, ByVal e As System.Windows.Forms.DataGridViewEditingControlShowingEventArgs) Handles DataGridView1.EditingControlShowing
        ' Only for a DatagridComboBoxColumn at ColumnIndex 3.
        If DataGridView1.CurrentCell.ColumnIndex = 3 Then
            Dim combo As ComboBox = CType(e.Control, ComboBox)
            If (combo IsNot Nothing) Then
                ' Remove an existing event-handler, if present, to avoid 
                ' adding multiple handlers when the editing control is reused.
                RemoveHandler combo.SelectionChangeCommitted, New EventHandler(AddressOf ComboBox_SelectionChangeCommitted)
                ' Add the event handler. 
                AddHandler combo.SelectionChangeCommitted, New EventHandler(AddressOf ComboBox_SelectionChangeCommitted)
            End If
        End If

        ' Colonne Volume : la molette sur la liste ouverte dans la case regle le volume par pas de 5
        ' (voir VolumeALaMolette). La liste d'edition est reutilisee pour toutes les colonnes a
        ' liste : le gestionnaire verifie lui-meme qu'on est bien dans la colonne Volume.
        Dim liste As ComboBox = TryCast(e.Control, ComboBox)
        If liste IsNot Nothing Then
            RemoveHandler liste.MouseWheel, AddressOf ListeVolume_MouseWheel
            AddHandler liste.MouseWheel, AddressOf ListeVolume_MouseWheel
        End If


    End Sub



    ' ------------------------------------------------------------------------------------------
    ' Colonne Volume : reglage rapide a la molette (demande de Jacob, 06/10).
    '
    ' La case est une liste de 101 valeurs (0 a 100) : il fallait l'ouvrir et la faire defiler.
    ' Desormais la molette, souris posee sur la case Volume de n'importe quelle ligne (sans
    ' cliquer), change le volume par pas de 5 ; avec Ctrl, par pas de 1. La liste reste la pour
    ' choisir une valeur precise. Une tache sans volume (case vide) n'est pas touchee.
    ' ------------------------------------------------------------------------------------------

    Private Const PasVolumeMolette As Integer = 5

    ''' <summary>Volume suivant : le multiple du pas juste au-dessus (ou en dessous), de 0 a 100.
    ''' 37 + un cran donne 40, 37 - un cran donne 35.</summary>
    Private Shared Function VolumeApresMolette(actuel As Integer, sens As Integer) As Integer
        Dim pas As Integer = If((Control.ModifierKeys And Keys.Control) = Keys.Control, 1, PasVolumeMolette)
        Dim nouveau As Integer
        If sens > 0 Then
            nouveau = (actuel \ pas) * pas + pas
        Else
            nouveau = ((actuel + pas - 1) \ pas) * pas - pas
        End If
        Return Math.Max(0, Math.Min(100, nouveau))
    End Function

    ''' <summary>Molette sur le tableau : sur une case Volume, elle regle le volume de cette ligne
    ''' au lieu de faire defiler le tableau.</summary>
    Private ReadOnly _infobulleTuya As New ToolTip With {.ToolTipTitle = "Tuya", .ToolTipIcon = ToolTipIcon.Info}

    ''' <summary>Clic sur la case Play d'une ligne Tuya : la ou les commandes de la ligne partent et leur RESULTAT s'affiche sous la case (« X » : envoyee, ou echec et sa raison :
    ''' appareil hors ligne, compte refuse...).</summary>
    Private Async Sub MontrerLeResultatTuya(r As Integer, c As Integer)
        Try
            Dim action As String = CStr(DataGridView1.Rows(r).Cells("Action").FormattedValue)
            Dim argument As String = CStr(If(DataGridView1.Rows(r).Cells("Argument").Value, ""))
            Dim zone As Rectangle = DataGridView1.GetCellDisplayRectangle(c, r, True)
            _infobulleTuya.Show("Envoi à Tuya...", DataGridView1, zone.Left, zone.Bottom, 15000)
            Dim texte As String = Await TuyaSlide.EnvoyerEtRapporter(action, argument)
            Dim echec As Boolean = texte.Contains("ÉCHEC") OrElse texte.Contains("inconnue") OrElse texte.Contains("désactivé")
            _infobulleTuya.ToolTipIcon = If(echec, ToolTipIcon.Warning, ToolTipIcon.Info)
            _infobulleTuya.Show(texte, DataGridView1, zone.Left, zone.Bottom, If(echec, 9000, 3500))
        Catch ex As Exception
            DiagLog("Tuya : clic Play : " & ex.Message)
        End Try
    End Sub

    Private Sub DataGridView1_MouseWheel(sender As Object, e As MouseEventArgs) Handles DataGridView1.MouseWheel
        Try
            Dim touche As DataGridView.HitTestInfo = DataGridView1.HitTest(e.X, e.Y)
            If touche.Type <> DataGridViewHitTestType.Cell OrElse touche.RowIndex < 0 Then Exit Sub
            If DataGridView1.Columns(touche.ColumnIndex).Name <> "Volume" Then Exit Sub
            ' Sur la colonne Volume, la molette ne fait jamais defiler le tableau.
            Dim traite As HandledMouseEventArgs = TryCast(e, HandledMouseEventArgs)
            If traite IsNot Nothing Then traite.Handled = True

            Dim cellule As DataGridViewCell = DataGridView1.Rows(touche.RowIndex).Cells("Volume")
            Dim actuel As Integer
            If Not Integer.TryParse(CStr(If(cellule.Value, "")).Trim(), actuel) Then Exit Sub    ' tache sans volume
            ' La ligne survolee fait partie d'une selection de plusieurs lignes (une serie, par
            ' exemple) : toutes prennent le nouveau volume. Sinon, elle seule.
            AppliquerLeVolume(LignesDeLaSelectionAvec(touche.RowIndex), VolumeApresMolette(actuel, Math.Sign(e.Delta)))
        Catch ex As Exception
            DiagLog("Volume a la molette : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Molette sur la liste de la case Volume en cours de saisie : pas de 5, comme sur
    ''' les autres lignes (sans cela, la liste avance d'une valeur par cran).</summary>
    Private Sub ListeVolume_MouseWheel(sender As Object, e As MouseEventArgs)
        Try
            Dim liste As ComboBox = TryCast(sender, ComboBox)
            If liste Is Nothing OrElse liste.DroppedDown Then Exit Sub             ' liste deroulee : defilement normal
            If DataGridView1.CurrentCell Is Nothing OrElse DataGridView1.Columns(DataGridView1.CurrentCell.ColumnIndex).Name <> "Volume" Then Exit Sub
            Dim traite As HandledMouseEventArgs = TryCast(e, HandledMouseEventArgs)
            If traite IsNot Nothing Then traite.Handled = True
            Dim actuel As Integer
            If Not Integer.TryParse(CStr(If(liste.SelectedItem, "")).Trim(), actuel) Then Exit Sub
            ' (toute la selection si la ligne en fait partie, comme sur les autres cases)
            AppliquerLeVolume(LignesDeLaSelectionAvec(DataGridView1.CurrentCell.RowIndex), VolumeApresMolette(actuel, Math.Sign(e.Delta)))
        Catch ex As Exception
            DiagLog("Volume a la molette : " & ex.Message)
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Colonne Play : repere "PIP" (demande de Jacob, 06/10).
    '
    ' Une tache qui s'affiche en PIP (film, TV, IPTV, satellite) porte une pastille bleue avec un
    ' petit ecran montrant le coin du PIP ; orange avec une epingle si "Rester affiche". Le reste de la
    ' case garde son texte : la duree pour un film, la taille du PIP pour une chaine. Tout vient
    ' des reglages de la tache (colonne Argument) et non du texte memorise dans la case : celui
    ' des chaines ("Pip:15% Bdroite") n'etait ecrit qu'a la validation de l'apercu et restait
    ' faux des que la taille changeait autrement (a la souris, par exemple).
    ' C'est un dessin : la valeur de la case, relue a l'enregistrement, n'est pas touchee.
    ' ------------------------------------------------------------------------------------------
    Private Sub DataGridView1_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles DataGridView1.CellPainting
        PeindreLeReperePip(DataGridView1, e, "Play", "Argument")
    End Sub

    ''' <summary>Le meme repere dans le tableau des taches inactives (colonne Play, case de texte).</summary>
    Private Sub DataGridView2_CellPainting(sender As Object, e As DataGridViewCellPaintingEventArgs) Handles DataGridView2.CellPainting
        PeindreLeReperePip(DataGridView2, e, "Play2", "Argument2")
    End Sub

    Private Sub PeindreLeReperePip(grille As DataGridView, e As DataGridViewCellPaintingEventArgs, colonnePlay As String, colonneReglages As String)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 Then Exit Sub
        If grille.Columns(e.ColumnIndex).Name <> colonnePlay Then Exit Sub
        Try
            Dim reglages As String = CStr(If(grille.Rows(e.RowIndex).Cells(colonneReglages).Value, ""))
            Dim pip As Boolean = UCase(CStr(Arg("Pip", reglages))) = "TRUE"
            ' La taille du PIP est ecrite dans la pastille ("PIP 28 %"). L'ancien texte des chaines
            ' ("Pip:15% Bdroite") ferait double emploi, et il n'est pas tenu a jour : pas affiche.
            Dim texte As String = CStr(If(e.Value, "")).Trim()
            If pip AndAlso texte.StartsWith("Pip:", StringComparison.OrdinalIgnoreCase) Then texte = ""
            ' Les cases SANS PIP sont dessinees ici aussi : leur texte se range a la meme place que
            ' celui des lignes a pastille (toute la colonne est alignee).
            Dim bouton As Boolean = TypeOf grille.Rows(e.RowIndex).Cells(e.ColumnIndex) Is DataGridViewButtonCell
            If Not pip AndAlso texte = "" AndAlso Not (bouton AndAlso StylePro.Actif) Then Exit Sub

            ' La case (ou le bouton), sans son texte ; puis la pastille et le texte.
            Dim zone As Rectangle
            If bouton AndAlso StylePro.Actif Then
                zone = Rectangle.Inflate(StylePro.PeindreLeBouton(e), -Math.Max(4, e.CellBounds.Height \ 6), -1)
            Else
                e.Paint(e.CellBounds, DataGridViewPaintParts.All And Not DataGridViewPaintParts.ContentForeground)
                zone = Rectangle.Inflate(e.CellBounds, -Math.Max(4, e.CellBounds.Height \ 6), -3)
            End If
            ' Bouton (taches actives) : texte noir (clair avec l'apparence professionnelle). Case de texte
            ' (taches inactives) : la couleur de la case, celle de la selection si la ligne est selectionnee.
            Dim couleur As Color = If(StylePro.Actif, StylePro.Texte, SystemColors.ControlText)
            If Not bouton Then
                couleur = If((e.State And DataGridViewElementStates.Selected) <> 0, e.CellStyle.SelectionForeColor, e.CellStyle.ForeColor)
            End If
            ' Fetes juives (Yom Tov) : une petite icone "lecture" a la place de la pastille PIP ; le texte (nom de la video) part du meme endroit que les autres.
            Dim videoYomTov As Boolean = texte <> "" AndAlso UCase(CStr(If(grille.Rows(e.RowIndex).Cells("Type").Value, ""))).Trim() = UCase(YomTov.TypeMinuterie)
            If videoYomTov Then texte = texte.TrimStart("▶"c, " "c)             ' (anciennes lignes : l'icone etait devant le texte)
            ReperePip.Dessiner(e.Graphics, zone, e.CellStyle.Font, couleur,
                               CoinDuPip(reglages), LireTaillePip(reglages), texte, UCase(CStr(Arg("PipPersistant", reglages))) = "TRUE", pip, videoYomTov)
            e.Handled = True
        Catch ex As Exception
            ' un dessin rate ne doit pas empecher l'affichage normal de la case
        End Try
    End Sub

    Private Sub ComboBox_SelectionChangeCommitted(ByVal sender As System.Object, ByVal e As System.EventArgs)
        ' La liste d'edition est reutilisee par la grille pour les autres colonnes (chaines,
        ' volume...) : ce gestionnaire, pose pour la colonne Type, y restait accroche.
        If DataGridView1.CurrentCell Is Nothing OrElse DataGridView1.CurrentCell.ColumnIndex <> 3 Then Exit Sub
        Dim combo As ComboBox = CType(sender, ComboBox)
        'c = DataGridView1.CurrentCell.ColumnIndex
        r = DataGridView1.CurrentCell.RowIndex
        choix = UCase(combo.SelectedItem)

        If choix = "FILM" Then Call Choix_Film(r)
        If choix = "TV" Then Call Choix_TV(r)
        If choix = "VOLUME" Then Call Choix_Volume(r)
        If choix = "ECRAN NOIR" Then Call Choix_EcranNoir(r)
        If choix = "MODE RDV" Then Call Choix_ModeRdv(r)
        If choix = "SAT" Then Call Choix_SAT(r)
        If choix = "IPTV" Then Call Choix_IPTV(r)
        If choix = "PHOTOVIDEO MODE" Then Call Choix_PhotoVideoMode(r)
        If choix = "PHOTO MODE" Then Call Choix_PhotoMode(r)
        If choix = "VIDEO MODE" Then Call Choix_VideoMode(r)
        If choix = "SONOFF" Then Call Choix_Sonoff(r)
        If choix = "TUYA" Then Call Choix_Tuya(r)
        If choix = "LANCER" Then Call Choix_Lancer(r)
        If choix = "DIVERS" Then Call Choix_Divers(r)
        If choix = "MUSIC SOURCE" Then Call Choix_MusicSource(r)
        If choix = "DEEZER" Then Call Choix_Deezer(r)
        If choix = "RADIO" Then Call Choix_Radio(r)
        If choix = "SON" Then Call Choix_Son(r)
        If choix = "AUTRES" Then Call Choix_Autres(r)
        ' Type Yom Tov : la fenetre des fetes ajoute des lignes ; elle s'ouvre une fois la case validee (ligne vierge du bas devenue une vraie ligne).
        If choix = "YOM TOV" Then
            Dim ligneYomTov As Integer = r
            BeginInvoke(Sub() Call Choix_YomTov(ligneYomTov))
        End If



    End Sub


    Private Sub Button11_Click(sender As Object, e As EventArgs) Handles Button11.Click
        If DossierDreamboxParDefault <> "" Then FolderBrowserDialog1.SelectedPath = DossierDreamboxParDefault
        FolderBrowserDialog1.ShowDialog()
        folderName = FolderBrowserDialog1.SelectedPath
        TextBox5.Text = folderName
        DossierDreamboxParDefault = folderName
    End Sub

    Private Sub Button10_Click(sender As Object, e As EventArgs) Handles Button10.Click
        If DossierDivxParDefault <> "" Then FolderBrowserDialog1.SelectedPath = DossierDivxParDefault
        FolderBrowserDialog1.ShowDialog()
        folderName = FolderBrowserDialog1.SelectedPath
        TextBox4.Text = folderName
        DossierDivxParDefault = folderName
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        CheminFilm = DossierDivxParDefault
        OpenFileDialog1.InitialDirectory = CheminFilm
        Button8.BackColor = CouleurDuChoix
        Button8.ForeColor = Color.White
        Button9.BackColor = CouleurHorsChoix
        Button13.BackColor = CouleurHorsChoix
        Button25.BackColor = CouleurHorsChoix

    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click

        CheminFilm = DossierDreamboxParDefault
        OpenFileDialog1.InitialDirectory = CheminFilm
        Button9.BackColor = CouleurDuChoix
        Button9.ForeColor = Color.White
        Button8.BackColor = CouleurHorsChoix
        Button13.BackColor = CouleurHorsChoix
        Button25.BackColor = CouleurHorsChoix
    End Sub



    Private Sub Button12_Click(sender As Object, e As EventArgs) Handles Button12.Click
        If DossierFreeboxParDefault <> "" Then FolderBrowserDialog1.SelectedPath = DossierFreeboxParDefault
        FolderBrowserDialog1.ShowDialog()
        folderName = FolderBrowserDialog1.SelectedPath
        TextBox6.Text = folderName
        DossierFreeboxParDefault = folderName
    End Sub

    Private Sub Button13_Click(sender As Object, e As EventArgs) Handles Button13.Click
        CheminFilm = DossierFreeboxParDefault
        OpenFileDialog1.InitialDirectory = CheminFilm
        Button13.BackColor = CouleurDuChoix
        Button13.ForeColor = Color.White
        Button8.BackColor = CouleurHorsChoix
        Button9.BackColor = CouleurHorsChoix
        Button25.BackColor = CouleurHorsChoix
    End Sub

    Sub dataGridView1_CurrentCellDirtyStateChanged(
      ByVal sender As Object, ByVal e As EventArgs) _
      Handles DataGridView1.CurrentCellDirtyStateChanged

        If DataGridView1.IsCurrentCellDirty Then
            DataGridView1.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub


    Public Sub dataGridView1_CellValueChanged(ByVal sender As Object, ByVal e As DataGridViewCellEventArgs) Handles DataGridView1.CellValueChanged

        If DataGridView1.Columns(e.ColumnIndex).Name = "Actif" Then

            Dim checkCell As DataGridViewCheckBoxCell =
            CType(DataGridView1.Rows(e.RowIndex).Cells("Actif"), DataGridViewCheckBoxCell)

            If CType(checkCell.Value, [String]) = "False" Then

                DataGridView2.Rows.Add({False, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Jour").FormattedValue, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Heure").FormattedValue, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Type").FormattedValue, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Action").FormattedValue, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Play").FormattedValue, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Durée").FormattedValue, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Volume").FormattedValue, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("tri").FormattedValue, DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Argument").FormattedValue})
                DataGridView1.Rows.Remove(DataGridView1.CurrentRow)

                DataGridView1.Sort(DataGridView1.Columns("tri"), System.ComponentModel.ListSortDirection.Ascending)
                DataGridView2.Sort(DataGridView2.Columns("tri2"), System.ComponentModel.ListSortDirection.Ascending)
            End If
        End If

        If DataGridView1.Columns(e.ColumnIndex).Name = "Jour" Then
            If DataGridView1.Rows(e.RowIndex).Cells("Jour").Value = "BOOT" Then
                DataGridView1.Rows(e.RowIndex).Cells("Heure").Value = "00:00"
            End If
        End If

        If e.RowIndex < 0 Then Exit Sub

        If DataGridView1.Columns(e.ColumnIndex).Name = "Action" Then
            If DataGridView1.Rows(e.RowIndex).Cells("Type").Value = "TV" Then
                If DataGridView1.Rows(e.RowIndex).Cells("Action").Value = "Chaine OFF" Then
                    DataGridView1.Rows(e.RowIndex).Cells("Volume").Value = ""
                ElseIf CStr(If(DataGridView1.Rows(e.RowIndex).Cells("Volume").Value, "")).Trim() = "" Then
                    ' Volume par defaut seulement si la ligne n'en a pas (apres "Chaine OFF"). Changer
                    ' de chaine ne touche plus au volume choisi (0 pour un PIP muet, par exemple) :
                    ' avant, il etait remplace par le volume TV par defaut a chaque changement.
                    DataGridView1.Rows(e.RowIndex).Cells("Volume").Value = VolumeTVParDefault.ToString
                End If
            End If
            ' Deezer programme ("Music Source / Deezer", "Divers / Lance Deezer", "Volume / Volume
            ' Deezer") : volume du Deezer integre par defaut si la ligne n'en a pas, et la case
            ' Play de "Music Source / Deezer" montre la playlist de la tache.
            Dim typeLigne As String = UCase(CStr(If(DataGridView1.Rows(e.RowIndex).Cells("Type").Value, "")))
            Dim actionLigne As String = UCase(CStr(If(DataGridView1.Rows(e.RowIndex).Cells("Action").Value, "")))
            ' (Type "Deezer" depuis le 08/10 ; les anciens rangements sont gardes pour les lignes d'avant.)
            Dim deezerAvecVolume As Boolean = (typeLigne = "DEEZER" AndAlso (actionLigne = "LANCE DEEZER" OrElse actionLigne = "DEEZER FAVORIS" OrElse actionLigne = "VOLUME DEEZER")) OrElse
               (typeLigne = "MUSIC SOURCE" AndAlso actionLigne = "DEEZER") OrElse (typeLigne = "DIVERS" AndAlso actionLigne = "LANCE DEEZER") OrElse
               (typeLigne = "VOLUME" AndAlso actionLigne = "VOLUME DEEZER")
            Dim volumeLigne As String = CStr(If(DataGridView1.Rows(e.RowIndex).Cells("Volume").Value, "")).Trim()
            If deezerAvecVolume AndAlso volumeLigne = "" Then
                DataGridView1.Rows(e.RowIndex).Cells("Volume").Value = Math.Max(0, Math.Min(100, VolumeDeezer)).ToString
            ElseIf typeLigne = "DEEZER" AndAlso Not deezerAvecVolume AndAlso actionLigne <> "" AndAlso volumeLigne <> "" Then
                ' Play, Pause, Next, Kill : le volume n'a pas de sens, la case est videe.
                DataGridView1.Rows(e.RowIndex).Cells("Volume").Value = ""
            End If
            If typeLigne = "MUSIC SOURCE" OrElse typeLigne = "DEEZER" Then AfficherLaPlaylistDeLaLigne(e.RowIndex)
            NettoyerLeVolume(DataGridView1.Rows(e.RowIndex))          ' volume inutile pour cette tache : case videe
        End If

        ' Les reglages de la tache ont change (PIP coche, coin, taille...) : la case Play, qui
        ' dessine son repere PIP d'apres eux, est redessinee.
        If DataGridView1.Columns(e.ColumnIndex).Name = "Argument" Then
            DataGridView1.InvalidateCell(DataGridView1.Columns("Play").Index, e.RowIndex)
        End If

    End Sub

    Sub dataGridView2_CurrentCellDirtyStateChanged(
      ByVal sender As Object, ByVal e As EventArgs) _
      Handles DataGridView2.CurrentCellDirtyStateChanged

        If DataGridView2.IsCurrentCellDirty Then
            DataGridView2.CommitEdit(DataGridViewDataErrorContexts.Commit)
        End If
    End Sub


    Public Sub dataGridView2_CellValueChanged(ByVal sender As Object, ByVal e As DataGridViewCellEventArgs) Handles DataGridView2.CellValueChanged


        If DataGridView2.Columns(e.ColumnIndex).Name = "Inactif" Then

            DataGridView1.Rows.Add({True, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("Jour2").FormattedValue, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("Heure2").FormattedValue, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("Type2").FormattedValue, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("Action2").FormattedValue, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("Play2").FormattedValue, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("Durée2").FormattedValue, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("Volume2").FormattedValue, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("tri2").FormattedValue, DataGridView2.Rows(DataGridView2.CurrentCell.RowIndex).Cells("Argument2").FormattedValue})
            DataGridView2.Rows.Remove(DataGridView2.CurrentRow)

            DataGridView1.Sort(DataGridView1.Columns("tri"), System.ComponentModel.ListSortDirection.Ascending)
            DataGridView2.Sort(DataGridView2.Columns("tri2"), System.ComponentModel.ListSortDirection.Ascending)
        End If





    End Sub




    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click

        If DataGridView1.RowCount = 0 Then AjouterUneLigne(1, 1) : Exit Sub

        ' Une ligne entiere selectionnee : la nouvelle ligne vient juste apres elle ; sinon (aucune ligne entiere), a la fin du tableau.
        AjouterUneLigne(3, LigneCouranteMinuterie(), DataGridView1.SelectedRows.Count = 0)

        LigneMinuterieEnCours.Index = LigneCouranteMinuterie()
    End Sub

    Private Sub Button14_Click(sender As Object, e As EventArgs) Handles Button14.Click
        If DataGridView1.RowCount <> 0 Then AjouterUneLigne(2, LigneCouranteMinuterie())

    End Sub

    ''' <summary>Ligne de la cellule courante du tableau des taches actives, ou 0 s'il n'y en a pas.
    ''' Apres un clic dans le tableau des taches inactives, DataGridView1.CurrentCell vaut
    ''' Nothing : le lire directement faisait planter OK (NullReferenceException).</summary>
    Private Function LigneCouranteMinuterie() As Integer
        Dim cellule As DataGridViewCell = DataGridView1.CurrentCell
        If cellule IsNot Nothing Then Return cellule.RowIndex
        If DataGridView1.SelectedRows.Count > 0 Then Return DataGridView1.SelectedRows(0).Index
        Return 0
    End Function

    Private Sub CheckBox2_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox2.CheckedChanged
        If CheckBox2.Checked = True Then
            GroupBox1.Enabled = True
        Else
            GroupBox1.Enabled = False
        End If
    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll
        TextBox7.Text = TrackBar1.Value
        SetMasterVol(Val(TextBox7.Text))

    End Sub

    Private Sub TrackBar2_Scroll(sender As Object, e As EventArgs) Handles TrackBar2.Scroll
        TextBox8.Text = TrackBar2.Value
        SetMusicVolSlide(Val(TextBox8.Text))

    End Sub

    Private Sub TrackBar3_Scroll(sender As Object, e As EventArgs) Handles TrackBar3.Scroll
        TextBox9.Text = TrackBar3.Value
        If Diffusion.Mode = "VIDEO" Then SetVideoVolSlide(Val(TextBox9.Text))
    End Sub

    Private Sub TextBox7_TextChanged(sender As Object, e As EventArgs) Handles TextBox7.TextChanged

    End Sub

    Private Sub TextBox8_TextChanged(sender As Object, e As EventArgs) Handles TextBox8.TextChanged

    End Sub

    ''' <summary>Volume du Deezer integre a Slide : applique tout de suite, pour l'entendre en le
    ''' reglant (comme "Volume Music Slide") ; enregistre par le bouton OK.</summary>
    Private Sub TrackBarVolumeDeezer_Scroll(sender As Object, e As EventArgs) Handles TrackBarVolumeDeezer.Scroll
        TextBoxVolumeDeezer.Text = TrackBarVolumeDeezer.Value
        If MusiqueDeezer.Actif Then MusiqueDeezer.DefinirVolume(TrackBarVolumeDeezer.Value)
    End Sub

    Private Sub TextBox9_TextChanged(sender As Object, e As EventArgs) Handles TextBox9.TextChanged

    End Sub

    Private Sub TrackBar4_Scroll(sender As Object, e As EventArgs) Handles TrackBar4.Scroll
        TextBox2.Text = TrackBar4.Value
    End Sub

    Private Sub TrackBar6_Scroll(sender As Object, e As EventArgs) Handles TrackBar6.Scroll
        TextBox3.Text = TrackBar6.Value

    End Sub

    Private Sub TrackBar5_Scroll(sender As Object, e As EventArgs) Handles TrackBar5.Scroll
        TextBox1.Text = TrackBar5.Value
    End Sub

    Private Sub TabPage2_Click(sender As Object, e As EventArgs) Handles TabPage2.Click

    End Sub

    Private Sub TrackBar7_Scroll(sender As Object, e As EventArgs) Handles TrackBar7.Scroll
        TextBox11.Text = TrackBar7.Value
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Clic droit sur la colonne Heure (ContextMenuStrip1) : utilitaires communs.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Ligne cliquee (clic droit), -1 si aucune.</summary>
    Private Function LigneCliquee() As Integer
        Try
            If clickedCell Is Nothing Then Return -1
            Dim r As Integer = CInt(clickedCell.RowIndex)
            If r < 0 OrElse r >= DataGridView1.RowCount OrElse DataGridView1.Rows(r).IsNewRow Then Return -1
            Return r
        Catch ex As Exception
            Return -1
        End Try
    End Function

    ''' <summary>Heure d'une ligne en minutes depuis minuit, -1 si la cellule n'a pas d'heure.</summary>
    Private Function MinutesDeLaLigne(r As Integer) As Integer
        Dim h As DateTime? = CalendarCell.LireHeure(DataGridView1.Rows(r).Cells("Heure").Value)
        If h Is Nothing Then Return -1
        Return h.Value.Hour * 60 + h.Value.Minute
    End Function

    ''' <summary>Duree de la colonne Durée en minutes : "01:30:17" (secondes -> minute
    ''' superieure, pour que la tache suivante demarre apres la fin), "01:30", ou "0J2h44m"
    ''' (ecran noir, avant ignore). 0 si vide ou illisible.</summary>
    Private Shared Function DureeEnMinutes(texte As String) As Integer
        Dim t As String = CStr(If(texte, "")).Trim()
        If t = "" Then Return 0
        ' Avec unites : "0J2h44m", "1h30", "90 min", "45m", "5400 s" (jours, heures, minutes, secondes ; "h30" = 30 minutes).
        Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(t, "^\s*(?:(\d+)\s*[jJ])?\s*(?:(\d+)\s*[hH])?\s*(?:(\d+)\s*(?:[mM](?:in)?)?)?\s*(?:(\d+)\s*[sS])?\s*$")
        If m.Success AndAlso (m.Groups(1).Success OrElse m.Groups(2).Success OrElse m.Groups(3).Success OrElse m.Groups(4).Success) AndAlso t.IndexOf(":"c) < 0 Then
            Return If(m.Groups(1).Success, CInt(m.Groups(1).Value) * 1440, 0) +
                   If(m.Groups(2).Success, CInt(m.Groups(2).Value) * 60, 0) +
                   If(m.Groups(3).Success, CInt(m.Groups(3).Value), 0) +
                   If(m.Groups(4).Success AndAlso CInt(m.Groups(4).Value) > 0, (CInt(m.Groups(4).Value) + 59) \ 60, 0)
        End If
        Dim p As String() = t.Split(":"c)
        Try
            If p.Length = 3 Then Return CInt(p(0)) * 60 + CInt(p(1)) + If(CInt(p(2)) > 0, 1, 0)
            If p.Length = 2 Then Return CInt(p(0)) * 60 + CInt(p(1))
        Catch ex As Exception
        End Try
        Return 0
    End Function

    ''' <summary>Jour decale de <paramref name="jours"/> (passage de minuit) : seulement pour
    ''' Lundi..Dimanche ; Lun-Ven, Sam-Dim, TLJ et BOOT restent tels quels.</summary>
    Private Shared Function JourDecale(jour As String, jours As Integer) As String
        Dim i As Integer = Array.FindIndex(jourDeLaSemaine, Function(s) String.Equals(s, jour, StringComparison.OrdinalIgnoreCase))
        If i < 0 OrElse i > 6 OrElse jours = 0 Then Return jour
        Return jourDeLaSemaine(((i + jours) Mod 7 + 7) Mod 7)
    End Function

    ''' <summary>Ecrit heure (minutes depuis minuit, hors bornes admis) et jour d'une ligne :
    ''' le passage de minuit, dans un sens ou dans l'autre, decale le jour.</summary>
    Private Sub PoserHeureEtJour(r As Integer, minutes As Integer, jourDeDepart As String)
        Dim jours As Integer = CInt(Math.Floor(minutes / 1440.0))
        Dim m As Integer = minutes - jours * 1440
        DataGridView1.Rows(r).Cells("Heure").Value = String.Format("{0:00}:{1:00}", m \ 60, m Mod 60)
        DataGridView1.Rows(r).Cells("Jour").Value = JourDecale(jourDeDepart, jours)
        DataGridView1.CurrentCell = DataGridView1.Rows(r).Cells("Jour")      ' valide et rafraichit
        DataGridView1.CurrentCell = DataGridView1.Rows(r).Cells("Heure")
    End Sub

    Private Function JourDeLaLigne(r As Integer) As String
        Return CStr(If(DataGridView1.Rows(r).Cells("Jour").FormattedValue, ""))
    End Function

    ''' <summary>Heure de la minute en cours (ou de la suivante apres 50 s : le temps de valider),
    ''' avec le jour correspondant (avant : le jour d'aujourd'hui meme a 23:59:55).</summary>
    Private Sub HeureProchaineMinuteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HeureProchaineMinuteToolStripMenuItem.Click
        Dim r As Integer = LigneCliquee() : If r < 0 Then Exit Sub
        Dim quand As Date = If(Now.Second > 50, Now.AddMinutes(1), Now)
        Dim jour As String = jourDeLaSemaine(Weekday(quand, FirstDayOfWeek.Monday) - 1)
        PoserHeureEtJour(r, quand.Hour * 60 + quand.Minute, jour)
    End Sub

    ''' <summary>Heure actuelle + 1 minute, avec le jour correspondant (23:59 -> 00:00 le lendemain).</summary>
    Private Sub HeurePlus1MinuteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HeurePlus1MinuteToolStripMenuItem.Click
        Dim r As Integer = LigneCliquee() : If r < 0 Then Exit Sub
        Dim quand As Date = Now.AddMinutes(1)
        Dim jour As String = jourDeLaSemaine(Weekday(quand, FirstDayOfWeek.Monday) - 1)
        PoserHeureEtJour(r, quand.Hour * 60 + quand.Minute, jour)
    End Sub

    Private Sub ContextMenuStrip1_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles ContextMenuStrip1.Opening
        MontrerCopierColler(ContextMenuStrip1)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' OK : tous les controles des reglages, presentes ensemble dans FormCoherence.
    '
    ' Deux sortes de problemes :
    '   - a corriger (on ne peut pas enregistrer ainsi) : aucune source, dates du mode RDV
    '     invalides, controle a distance sans repertoire reseau, texte trop long pour la base ;
    '   - avertissements ("Enregistrer quand meme" reste possible) : volume trop fort, Sonoff
    '     qui ne repond pas, ville non definie, et la coherence des taches de la minuterie
    '     (voir CoherenceMinuterie).
    ' Les controles hors du tableau lisent les CASES de la fenetre (et non les reglages en
    ' memoire) : ils sont faits avant que quoi que ce soit ne soit enregistre.
    ' ------------------------------------------------------------------------------------------

    Private Function Reglage(rubrique As String, titre As String, detail As String, bloquant As Boolean, cible As Control) As CoherenceMinuterie.Incoherence
        Return New CoherenceMinuterie.Incoherence With {.Rubrique = rubrique, .Titre = titre, .Detail = detail, .Bloquant = bloquant, .Cible = cible}
    End Function

    ''' <summary>Les controles qui ne concernent pas une ligne du tableau.</summary>
    Private Function ProblemesHorsDuTableau() As List(Of CoherenceMinuterie.Incoherence)
        Dim liste As New List(Of CoherenceMinuterie.Incoherence)
        If ListView1.Items.Count = 0 Then
            liste.Add(Reglage("Sources", "Aucune source de photos et de vidéos",
                              "Ajoute au moins un dossier dans l'onglet Sources.", True, ListView1))
        End If
        If CheckBox31.Checked AndAlso (Not IsDate(TextBox26.Text) OrElse Not IsDate(TextBox27.Text)) Then
            liste.Add(Reglage("Mode RDV", "Dates du mode RDV invalides",
                              "Corrige la date de début et la date de fin, ou désactive le mode RDV.", True, TextBox26))
        End If
        If CheckBox30.Checked AndAlso TextBox15.Text.Trim() = "" Then
            liste.Add(Reglage("Réseau", "Répertoire réseau partagé non défini",
                              "Le contrôle à distance est activé : indique le répertoire réseau partagé, ou désactive le contrôle à distance.", True, TextBox15))
        End If
        If ComboBox1.Text.Trim() = "" Then
            liste.Add(Reglage("Chabbat", "Ville non définie",
                              "La liste des chaînes de la TV dépend de la ville : indique la ville.", False, ComboBox1))
        End If
        Return liste
    End Function

    ''' <summary>Les Sonoff de la minuterie qui ne repondent pas (essai sur le reseau).</summary>
    Private Function SonoffNonConnectes() As List(Of CoherenceMinuterie.Incoherence)
        Dim liste As New List(Of CoherenceMinuterie.Incoherence)
        For Each row As DataGridViewRow In DataGridView1.Rows
            If row.IsNewRow OrElse CStr(row.Cells("Type").FormattedValue) <> "Sonoff" Then Continue For
            Dim action As String = CStr(row.Cells("Action").FormattedValue)
            If action.Trim() = "" Then Continue For
            Dim repond As Boolean = True
            Try
                repond = CBool(Sonoff_Test(action))
            Catch ex As Exception
                repond = False
            End Try
            If Not repond Then
                liste.Add(New CoherenceMinuterie.Incoherence With {.Lignes = {row.Index + 1}, .Titre = "Sonoff non connecté",
                                                                   .Detail = "« " & action.Trim() & " » ne répond pas : la commande ne sera pas exécutée."})
            End If
        Next
        Return liste
    End Function

    ''' <summary>Vrai si l'on peut enregistrer : aucun probleme, ou "Enregistrer quand meme".
    ''' Faux si l'on revient corriger : l'onglet concerne est affiche, sur les lignes du tableau
    ''' ou sur le reglage du probleme choisi.</summary>
    Private Function ReglagesCoherents() As Boolean
        Try
            DataGridView1.EndEdit()
            Dim lignes As New List(Of CoherenceMinuterie.LigneTache)
            For Each row As DataGridViewRow In DataGridView1.Rows
                If row.IsNewRow Then Continue For
                lignes.Add(New CoherenceMinuterie.LigneTache With {
                    .Numero = row.Index + 1,
                    .Jour = CStr(row.Cells("Jour").FormattedValue),
                    .Heure = CStr(row.Cells("Heure").FormattedValue),
                    .Type = CStr(row.Cells("Type").FormattedValue),
                    .Action = CStr(row.Cells("Action").FormattedValue),
                    .Play = CStr(row.Cells("Play").FormattedValue),
                    .Duree = CStr(row.Cells("Durée").FormattedValue),
                    .Volume = CStr(row.Cells("Volume").FormattedValue),
                    .Reglages = CStr(If(row.Cells("Argument").Value, ""))})
            Next
            ' Volumes par defaut tels qu'ils sont saisis dans l'onglet Audio (pas encore enregistres).
            Dim seuils As New CoherenceMinuterie.Seuils With {
                .VolumeTv = CInt(Val(TextBox11.Text)),
                .VolumeRecepteur = CInt(Val(TextBox14.Text)),
                .Recepteur = CStr(recepteurSat)}
            Dim problemes As New List(Of CoherenceMinuterie.Incoherence)
            Dim chronoCoherence As Stopwatch = Stopwatch.StartNew()
            problemes.AddRange(ProblemesHorsDuTableau())
            Dim msHors As Long = chronoCoherence.ElapsedMilliseconds
            problemes.AddRange(CoherenceMinuterie.Verifier(lignes, seuils))
            Dim msVerifier As Long = chronoCoherence.ElapsedMilliseconds
            problemes.AddRange(CoherenceMinuterie.FilmsIntrouvables(lignes))       ' (2 secondes au plus)
            Dim msFilms As Long = chronoCoherence.ElapsedMilliseconds
            problemes.AddRange(SonoffNonConnectes())
            DiagLog("Form4 OK : coherence : hors tableau " & msHors & " ms ; regles " & (msVerifier - msHors) & " ms ; films introuvables " & (msFilms - msVerifier) &
                    " ms ; Sonoff " & (chronoCoherence.ElapsedMilliseconds - msFilms) & " ms")
            If problemes.Count = 0 Then Return True
            problemes = CoherenceMinuterie.Trier(problemes)

            ' Fenetre des incoherences (FormCoherence) : "Enregistrer quand meme", ou "Corriger".
            Dim aCorriger As CoherenceMinuterie.Incoherence = Nothing
            If FormCoherence.Demander(Me, problemes, aCorriger) Then Return True
            If aCorriger Is Nothing Then aCorriger = problemes(0)

            If aCorriger.Lignes IsNot Nothing AndAlso aCorriger.Lignes.Length > 0 Then
                ' Retour au tableau, sur les lignes du probleme choisi.
                AfficherLOngletDe(DataGridView1)
                DataGridView1.ClearSelection()
                For Each numero As Integer In aCorriger.Lignes
                    If numero >= 1 AndAlso numero <= DataGridView1.RowCount Then DataGridView1.Rows(numero - 1).Selected = True
                Next
                Dim premiere As Integer = aCorriger.Lignes.Min() - 1
                If premiere >= 0 AndAlso premiere < DataGridView1.RowCount Then DataGridView1.FirstDisplayedScrollingRowIndex = premiere
            Else
                ' Reglage hors du tableau : son onglet, le curseur dans la case a corriger.
                Dim cible As Control = TryCast(aCorriger.Cible, Control)
                If cible IsNot Nothing Then
                    AfficherLOngletDe(cible)
                    cible.Focus()
                End If
            End If
            Return False
        Catch ex As Exception
            ' Un controle qui echoue ne doit jamais empecher d'enregistrer.
            DiagLog("Reglages, verification de coherence : " & ex.Message)
            Return True
        End Try
    End Function

    ''' <summary>Affiche le ou les onglets qui contiennent <paramref name="controle"/> (onglets
    ''' imbriques compris).</summary>
    Private Sub AfficherLOngletDe(controle As Control)
        Dim c As Control = controle
        Do While c IsNot Nothing
            Dim onglet As TabPage = TryCast(c, TabPage)
            Dim onglets As TabControl = If(onglet Is Nothing, Nothing, TryCast(onglet.Parent, TabControl))
            If onglets IsNot Nothing Then onglets.SelectedTab = onglet
            c = c.Parent
        Loop
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Clic droit sur le tableau : "Copier la ligne" / "Coller la ligne" (demande de Jacob, 06/10).
    '
    ' La ligne est copiee EN ENTIER : toutes ses colonnes, y compris les reglages caches (colonne
    ' Argument : titre, PIP, taille, coin, boucle...) et les infobulles. Les boutons Copier /
    ' Coller existants passent par le presse-papiers de Windows, qui ne contient que les colonnes
    ' visibles : la ligne collee perdait ses reglages. La copie reste disponible tant que la
    ' fenetre des reglages est ouverte ; on peut la coller plusieurs fois.
    ' "Coller" insere la copie juste sous la ligne cliquee (ou a la fin, clic hors des lignes).
    ' Les deux choix apparaissent sur toutes les colonnes : dans le menu du tableau, et a la fin
    ' des menus particuliers des colonnes Heure et Action (film).
    ' ------------------------------------------------------------------------------------------

    ' PLUSIEURS LIGNES (demande de Jacob, 06/10) : si la ligne cliquee fait partie d'une selection
    ' de plusieurs lignes (lignes entieres, ou cases de plusieurs lignes), toute la selection est
    ' copiee, dans l'ordre du tableau ; sinon, seulement la ligne cliquee. "Coller" insere toutes
    ' les lignes copiees, dans le meme ordre.

    Private _ligneDuMenu As Integer = -1                 ' ligne sous le clic droit, -1 si aucune
    Private _lignesDuMenu As New List(Of Integer)        ' lignes que "Copier" prendra (au moins la ligne cliquee)
    Private _lignesCopiees As New List(Of Object())      ' valeurs de toutes les colonnes, une entree par ligne
    Private _infobullesCopiees As New List(Of String())
    Private _separateurCopier As ToolStripSeparator
    Private _colonneDuMenu As Integer = -1
    Private _separateurOptions As ToolStripSeparator

    ''' <summary>Retient la ligne sous le clic droit, et la selection si elle la contient (avant
    ''' l'ouverture du menu).</summary>
    Private Sub DataGridView1_MouseDown_LigneDuMenu(sender As Object, e As MouseEventArgs) Handles DataGridView1.MouseDown
        If e.Button <> MouseButtons.Right Then Exit Sub
        Dim touche As DataGridView.HitTestInfo = DataGridView1.HitTest(e.X, e.Y)
        _ligneDuMenu = If(touche.Type = DataGridViewHitTestType.Cell AndAlso touche.RowIndex >= 0 AndAlso
                          Not DataGridView1.Rows(touche.RowIndex).IsNewRow, touche.RowIndex, -1)
        _colonneDuMenu = If(_ligneDuMenu >= 0, touche.ColumnIndex, -1)
        ' La colonne Action n'a son menu propre que pour un Film (Option, Coller...) : pour les autres types, c'est le menu des lignes, avec
        ' "Modifier les options". (Avant, le menu du Film restait colle a la colonne apres un clic droit sur un film.)
        If _ligneDuMenu >= 0 AndAlso touche.ColumnIndex = DataGridView1.Columns("Action").Index Then
            Dim estFilm As Boolean = (Convert.ToString(DataGridView1.Rows(_ligneDuMenu).Cells("Type").FormattedValue) = "Film")
            DataGridView1.Columns("Action").ContextMenuStrip = If(estFilm, ContextMenuStrip2, Nothing)
        End If
        _lignesDuMenu.Clear()
        If _ligneDuMenu < 0 Then Exit Sub
        ' Lignes selectionnees : entieres, sinon celles des cases selectionnees.
        Dim choisies As New SortedSet(Of Integer)
        For Each r As DataGridViewRow In DataGridView1.SelectedRows
            If Not r.IsNewRow Then choisies.Add(r.Index)
        Next
        If choisies.Count = 0 Then
            For Each c As DataGridViewCell In DataGridView1.SelectedCells
                If c.RowIndex >= 0 AndAlso Not DataGridView1.Rows(c.RowIndex).IsNewRow Then choisies.Add(c.RowIndex)
            Next
        End If
        If choisies.Count > 1 AndAlso choisies.Contains(_ligneDuMenu) Then
            _lignesDuMenu.AddRange(choisies)
        Else
            _lignesDuMenu.Add(_ligneDuMenu)
        End If
    End Sub

    ''' <summary>Clic droit sur une case de la colonne Action (tableau des taches) : "Modifier les options de la tâche..." rouvre la fenetre
    ''' d'options du type de la ligne (Lancer : programme et "Attend la fin...", TV, Sat, IPTV, Volume, Ecran noir, Mode RDV, Sonoff, Divers,
    ''' Music Source, Deezer, Photo / Video Mode). Pour un Film, le menu propre a la colonne garde son "Option". 20/10/2026.</summary>
    Private Sub PreparerLeMenuOptions(menu As ContextMenuStrip)
        ' Un element de menu n'appartient qu'a un menu a la fois : "Modifier les options" suit celui qu'on ouvre (menu des lignes, ou menu du Film).
        If _separateurOptions Is Nothing Then _separateurOptions = SeparateurOptionsToolStripMenuItem
        For Each element As ToolStripItem In {ChangerDeFilmToolStripMenuItem, EnchainerToolStripMenuItem, ModifierLesOptionsToolStripMenuItem, SeparateurOptionsToolStripMenuItem}
            menu.Items.Remove(element)
        Next
        If menu Is ContextMenuStrip2 Then
            ' Menu du Film : Changer de film, Modifier les options, Option Replay, puis le separateur, puis Coller...
            menu.Items.Insert(0, ModifierLesOptionsToolStripMenuItem)
            menu.Items.Insert(0, EnchainerToolStripMenuItem)
            menu.Items.Insert(0, ChangerDeFilmToolStripMenuItem)
            menu.Items.Insert(menu.Items.IndexOf(OptionToolStripMenuItem) + 1, SeparateurOptionsToolStripMenuItem)
        Else
            menu.Items.Insert(0, SeparateurOptionsToolStripMenuItem)
            menu.Items.Insert(0, ModifierLesOptionsToolStripMenuItem)
            menu.Items.Insert(0, EnchainerToolStripMenuItem)
            menu.Items.Insert(0, ChangerDeFilmToolStripMenuItem)         ' (Film seulement : au-dessus de "Modifier les options")
        End If
        Dim colonneAction As Integer = DataGridView1.Columns("Action").Index
        Dim type As String = ""
        If _ligneDuMenu >= 0 AndAlso _ligneDuMenu < DataGridView1.RowCount Then
            type = UCase(Convert.ToString(DataGridView1.Rows(_ligneDuMenu).Cells("Type").FormattedValue)).Trim()
        End If
        Dim montrer As Boolean = (_colonneDuMenu = colonneAction AndAlso OptionsDeLaTacheDisponibles(type))
        ModifierLesOptionsToolStripMenuItem.Visible = montrer
        ChangerDeFilmToolStripMenuItem.Visible = montrer AndAlso type = "FILM"
        Dim afficheEnchainer As Boolean = montrer AndAlso (type = "FILM" OrElse type = "TV" OrElse type = "IPTV" OrElse type = "SAT")
        EnchainerToolStripMenuItem.Visible = afficheEnchainer
        ' Libelle fixe, avec une coche quand la ligne est deja enchainee (22/10/2026 : "rajoute une coche quand c'est valide") ; un clic bascule.
        ' (Pas "EnchainerToolStripMenuItem.Visible" : pour un element de menu, ce test vaut Faux tant que le menu n'est pas affiche - le libelle et la coche n'etaient donc jamais mis a jour.)
        If afficheEnchainer Then
            ' La coche systeme n'est pas toujours visible (menus sombres) : un signe ✔ est aussi ecrit devant le libelle quand la ligne est enchainee.
            Dim enchainee As Boolean = LigneEstEnSuite(_ligneDuMenu)
            EnchainerToolStripMenuItem.Text = If(enchainee, "✔  ", "") & "Enchaîner avec la ligne précédente (sans attendre)"
            EnchainerToolStripMenuItem.Checked = enchainee
        End If
        SeparateurOptionsToolStripMenuItem.Visible = montrer
        If montrer Then ModifierLesOptionsToolStripMenuItem.Text = "Modifier les options de la tâche (" & Convert.ToString(DataGridView1.Rows(_ligneDuMenu).Cells("Type").FormattedValue) & ")..."
    End Sub

    Private Shared Function OptionsDeLaTacheDisponibles(type As String) As Boolean
        Select Case type
            Case "FILM", "TV", "VOLUME", "ECRAN NOIR", "MODE RDV", "SAT", "IPTV", "PHOTOVIDEO MODE", "PHOTO MODE", "VIDEO MODE", "SONOFF", "TUYA", "LANCER", "DIVERS", "MUSIC SOURCE", "DEEZER", "RADIO", "SON", "AUTRES", "YOM TOV"
                Return True
        End Select
        Return False
    End Function

    ''' <summary>La ligne est un episode "a la suite" d'une serie enchainee (reglage Suite de la colonne Argument, voir Module1.JouerLaSuiteDeLaSerie).</summary>
    Private Function LigneEstEnSuite(ligne As Integer) As Boolean
        If ligne < 0 OrElse ligne >= DataGridView1.RowCount Then Return False
        Return UCase(Convert.ToString(Arg("Suite", Convert.ToString(DataGridView1.Rows(ligne).Cells("Argument").Value)))) = "TRUE"
    End Function

    ''' <summary>Menu des films : le film (ou les films selectionnes) s'enchaine avec le precedent, sans attendre sa minute (22/10/2026), ou ne s'enchaine plus. Pour les series
    ''' deja programmees avant l'enchainement automatique.</summary>
    Private Sub EnchainerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EnchainerToolStripMenuItem.Click
        Try
            Dim lignes As List(Of Integer) = _lignesDuMenu.Where(Function(i) i >= 0 AndAlso i < DataGridView1.RowCount AndAlso
                Array.IndexOf({"FILM", "TV", "IPTV", "SAT"}, UCase(Convert.ToString(DataGridView1.Rows(i).Cells("Type").FormattedValue)).Trim()) >= 0).Distinct().ToList()
            If lignes.Count = 0 Then Exit Sub
            Dim activer As Boolean = Not LigneEstEnSuite(lignes(0))
            For Each i As Integer In lignes
                Dim cellule As DataGridViewCell = DataGridView1.Rows(i).Cells("Argument")
                Dim sans As String = System.Text.RegularExpressions.Regex.Replace(Convert.ToString(cellule.Value), "!/Suite:[^!]*", "", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
                cellule.Value = If(activer, sans & "!/Suite:True", sans)
            Next
            DiagLog("Minuterie : " & lignes.Count & " film(s) " & If(activer, "enchaine(s) avec le precedent", "plus enchaine(s)"))
        Catch ex As Exception
            DiagLog("Minuterie, enchainer : " & ex.Message)
        End Try
    End Sub

    ''' <summary>"Changer de film" (menu du Film) : le choix du film de la ligne est refait (fichier, dossier ou liste Nflix), comme a la creation.</summary>
    Private Sub ChangerDeFilmToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ChangerDeFilmToolStripMenuItem.Click
        Dim ligne As Integer = _ligneDuMenu
        If ligne < 0 OrElse ligne >= DataGridView1.RowCount Then Exit Sub
        Try
            DataGridView1.CurrentCell = DataGridView1.Rows(ligne).Cells("Action")
            Call Choix_Film(ligne)
        Catch ex As Exception
            DiagLog("Minuterie : changer de film (ligne " & (ligne + 1) & ") : " & ex.Message)
        End Try
    End Sub

    Private Sub ModifierLesOptionsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ModifierLesOptionsToolStripMenuItem.Click
        Dim ligne As Integer = _ligneDuMenu
        If ligne < 0 OrElse ligne >= DataGridView1.RowCount Then Exit Sub
        Try
            DataGridView1.CurrentCell = DataGridView1.Rows(ligne).Cells("Action")
            Select Case UCase(Convert.ToString(DataGridView1.Rows(ligne).Cells("Type").FormattedValue)).Trim()
                Case "FILM" : Call PreviewFilm(ligne)          ' (les options du film : la meme fenetre que la case Play)
                Case "TV"
                    ' Comme un clic sur la case Play de la ligne : l'apercu de la chaine (voir DataGridView1_CellClick, type TV).
                    Dim chaineTv As String = Convert.ToString(DataGridView1.Rows(ligne).Cells("Action").FormattedValue)
                    Dim volumeTv As String = Convert.ToString(DataGridView1.Rows(ligne).Cells("Volume").FormattedValue)
                    If chaineTv = "Chaine OFF" Then
                        ClosePlayer()
                    Else
                        Dim numeroTv As Integer = CInt(Val(Mid(chaineTv, 8, 2)))
                        If numeroTv > 0 Then Call PreviewTV(numeroTv, volumeTv)
                    End If
                Case "VOLUME" : Call Choix_Volume(ligne)
                Case "ECRAN NOIR" : Call Choix_EcranNoir(ligne)
                Case "MODE RDV" : Call Choix_ModeRdv(ligne)
                Case "SAT"
                    ' Comme la case Play : l'apercu du satellite.
                    Dim chaineSat As String = Convert.ToString(DataGridView1.Rows(ligne).Cells("Action").FormattedValue)
                    Dim volumeSat As String = Convert.ToString(DataGridView1.Rows(ligne).Cells("Volume").FormattedValue)
                    If chaineSat = "Sat OFF" Then
                        ClosePlayer()
                    Else
                        Call PreviewSat(CInt(Val(Mid(chaineSat, 8, 3))), volumeSat, ligne)
                    End If
                Case "IPTV"
                    ' Comme la case Play : l'apercu de la chaine IPTV.
                    Dim chaineIptv As String = Convert.ToString(DataGridView1.Rows(ligne).Cells("Action").FormattedValue)
                    Dim volumeIptv As String = Convert.ToString(DataGridView1.Rows(ligne).Cells("Volume").FormattedValue)
                    If chaineIptv = "IPTV OFF" Then
                        ClosePlayer()
                        EndAndContinue()
                    Else
                        Call PreviewIPTV_VLC(chaineIptv, volumeIptv)
                    End If
                Case "PHOTOVIDEO MODE" : Call Choix_PhotoVideoMode(ligne)
                Case "PHOTO MODE" : Call ModifierLaLigneReglages(ligne, "Photo Mode")        ' (fenetre de choix multiples)
                Case "VIDEO MODE" : Call ModifierLaLigneReglages(ligne, "Video Mode")
                Case "AUTRES" : Call ModifierLaLigneReglages(ligne, "Autres")
                Case "YOM TOV" : Call ModifierLaLigneYomTov(ligne)
                Case "SONOFF" : Call Choix_Sonoff(ligne)
                Case "TUYA" : Call ModifierLaLigneTuya(ligne)          ' (fenetre de choix multiples)
                Case "LANCER" : Call Choix_Lancer(ligne)
                Case "DIVERS" : Call Choix_Divers(ligne)
                Case "MUSIC SOURCE"
                    ' Comme la case Play (choix de la playlist) quand la ligne en a une ; sinon le choix de la source.
                    If LigneDeezerAvecPlaylist(ligne) Then ChoisirLaPlaylistDeLaLigne(ligne) Else Call Choix_MusicSource(ligne)
                Case "DEEZER"
                    If LigneDeezerAvecPlaylist(ligne) Then ChoisirLaPlaylistDeLaLigne(ligne) Else Call Choix_Deezer(ligne)
                Case "SON" : Call ModifierLaLigneSon(ligne)          ' (la fenetre des reglages du cadre Son)
                Case "RADIO"
                    ' La liste des radios s'ouvre sur la case Action, pour choisir une autre station.
                    DataGridView1.BeginEdit(True)
                    Dim liste As ComboBox = TryCast(DataGridView1.EditingControl, ComboBox)
                    If liste IsNot Nothing Then liste.DroppedDown = True
            End Select
        Catch ex As Exception
            DiagLog("Minuterie : options de la tache (ligne " & (ligne + 1) & ") : " & ex.Message)
        End Try
    End Sub

    Private Sub ContextMenuStripLigne_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles ContextMenuStripLigne.Opening
        PreparerLeMenuOptions(ContextMenuStripLigne)
        MontrerCopierColler(ContextMenuStripLigne)
        ' Rien a copier (clic hors des lignes) et rien a coller : pas de menu. Sinon il faut le dire
        ' explicitement : quand ses deux choix sont partis dans un autre menu, celui-ci est vide a
        ' l'ouverture et Windows Forms l'annule d'office.
        e.Cancel = (_ligneDuMenu < 0 AndAlso _lignesCopiees.Count = 0)
    End Sub

    ''' <summary>Place "Copier" et "Coller" a la fin du menu qui s'ouvre (un element de menu
    ''' n'appartient qu'a un menu a la fois : ils suivent celui qu'on ouvre), avec le nombre de
    ''' lignes concernees.</summary>
    Private Sub MontrerCopierColler(menu As ContextMenuStrip)
        Try
            If _separateurCopier Is Nothing Then _separateurCopier = New ToolStripSeparator()
            If menu IsNot ContextMenuStripLigne Then menu.Items.Add(_separateurCopier)
            menu.Items.Add(CopierLaLigneToolStripMenuItem)
            menu.Items.Add(CollerLaLigneToolStripMenuItem)
            ' "Ajouter au Boot" (15/10/2026) : les lignes choisies sont copiees en tache de demarrage (Jour BOOT, 00:00).
            menu.Items.Add(AjouterAuBootToolStripMenuItem)
            Dim aAjouter As Integer = LignesAjoutablesAuBoot().Count
            AjouterAuBootToolStripMenuItem.Enabled = aAjouter > 0
            AjouterAuBootToolStripMenuItem.Text = If(aAjouter > 1, "Ajouter les " & aAjouter & " lignes au Boot", "Ajouter au Boot")
            ' "Volume 40 pour les 6 lignes selectionnees" : seulement sur une selection de plusieurs
            ' lignes, quand la ligne cliquee a un volume.
            menu.Items.Add(MemeVolumeToolStripMenuItem)
            Dim volumeClique As Integer
            Dim proposer As Boolean = _lignesDuMenu.Count > 1 AndAlso _ligneDuMenu >= 0 AndAlso _ligneDuMenu < DataGridView1.RowCount AndAlso
                                      Integer.TryParse(CStr(If(DataGridView1.Rows(_ligneDuMenu).Cells("Volume").Value, "")).Trim(), volumeClique)
            MemeVolumeToolStripMenuItem.Visible = proposer
            If proposer Then MemeVolumeToolStripMenuItem.Text = "Volume " & volumeClique & " pour les " & _lignesDuMenu.Count & " lignes sélectionnées"
            ' Sous-menu "Volume" : regler le volume sans la molette (voir PreparerLeMenuVolume).
            menu.Items.Add(VolumeToolStripMenuItem)
            PreparerLeMenuVolume()
            CopierLaLigneToolStripMenuItem.Enabled = _lignesDuMenu.Count > 0
            CopierLaLigneToolStripMenuItem.Text = If(_lignesDuMenu.Count > 1, "Copier les " & _lignesDuMenu.Count & " lignes", "Copier la ligne")
            CollerLaLigneToolStripMenuItem.Enabled = _lignesCopiees.Count > 0
            Select Case _lignesCopiees.Count
                Case 0 : CollerLaLigneToolStripMenuItem.Text = "Coller la ligne"
                Case 1 : CollerLaLigneToolStripMenuItem.Text = "Coller la ligne (" & ResumeDeLaLigneCopiee(_lignesCopiees(0)) & ")"
                Case Else : CollerLaLigneToolStripMenuItem.Text = "Coller les " & _lignesCopiees.Count & " lignes (" & ResumeDeLaLigneCopiee(_lignesCopiees(0)) & ", ...)"
            End Select
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>"Vendredi 16:00 Film" : ce que contient une ligne copiee, rappele dans le menu.</summary>
    Private Function ResumeDeLaLigneCopiee(valeurs As Object()) As String
        Try
            Dim morceaux As New List(Of String)
            For Each colonne As String In {"Jour", "Heure", "Type"}
                Dim v As String = CStr(If(valeurs(DataGridView1.Columns(colonne).Index), "")).Trim()
                If colonne = "Heure" AndAlso v.Length > 5 Then v = v.Substring(0, 5)
                If v <> "" Then morceaux.Add(v)
            Next
            Return String.Join(" ", morceaux)
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Le meme volume pour plusieurs lignes, par exemple tous les episodes d'une serie (demande de
    ' Jacob, 06/10). Deux facons, sur une selection de plusieurs lignes :
    '   - clic droit sur l'une d'elles > "Volume 40 pour les 6 lignes selectionnees" : toutes
    '     prennent le volume de la ligne cliquee ;
    '   - molette sur la case Volume de l'une d'elles : toutes changent ensemble (voir
    '     DataGridView1_MouseWheel).
    ' Une ligne sans volume (case vide : "Chaine OFF"...) n'est pas touchee.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Lignes d'une selection de plusieurs lignes qui contient <paramref name="ligne"/> ;
    ''' sinon cette seule ligne.</summary>
    Private Function LignesDeLaSelectionAvec(ligne As Integer) As List(Of Integer)
        Dim choisies As New SortedSet(Of Integer)
        For Each r As DataGridViewRow In DataGridView1.SelectedRows
            If Not r.IsNewRow Then choisies.Add(r.Index)
        Next
        If choisies.Count = 0 Then
            For Each c As DataGridViewCell In DataGridView1.SelectedCells
                If c.RowIndex >= 0 AndAlso Not DataGridView1.Rows(c.RowIndex).IsNewRow Then choisies.Add(c.RowIndex)
            Next
        End If
        If choisies.Count > 1 AndAlso choisies.Contains(ligne) Then Return choisies.ToList()
        Return New List(Of Integer) From {ligne}
    End Function

    ''' <summary>Met <paramref name="volume"/> dans la case Volume des lignes donnees (celles qui
    ''' ont deja un volume). Renvoie le nombre de lignes changees.</summary>
    Private Function AppliquerLeVolume(lignes As IEnumerable(Of Integer), volume As Integer) As Integer
        Dim texte As String = Math.Max(0, Math.Min(100, volume)).ToString()
        Dim nombre As Integer = 0
        Dim liste As ComboBox = TryCast(DataGridView1.EditingControl, ComboBox)
        For Each numero As Integer In lignes
            If numero < 0 OrElse numero >= DataGridView1.RowCount Then Continue For
            Dim cellule As DataGridViewCell = DataGridView1.Rows(numero).Cells("Volume")
            Dim actuel As Integer
            If Not Integer.TryParse(CStr(If(cellule.Value, "")).Trim(), actuel) Then Continue For    ' tache sans volume
            ' Case en cours de saisie : c'est sa liste qu'on regle (elle met la case a jour).
            If liste IsNot Nothing AndAlso DataGridView1.CurrentCell Is cellule Then
                liste.SelectedItem = texte
            Else
                cellule.Value = texte
            End If
            nombre += 1
        Next
        Return nombre
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Sous-menu "Volume" du clic droit : regler le volume SANS la molette (demande de Jacob,
    ' 06/10 ; pave tactile, souris sans molette). Il agit sur la ligne cliquee, ou sur toute la
    ' selection si elle en fait partie :
    '   + 5 / - 5   a partir du volume de la ligne cliquee ;
    '   0, 10, 20 ... 100   valeur directe (la valeur actuelle est cochee).
    ' L'element "Volume" est dans le Concepteur ; ses choix sont crees ici, une fois.
    ' ------------------------------------------------------------------------------------------

    Private Sub PreparerLeMenuVolume()
        Dim actuel As Integer
        Dim possible As Boolean = _ligneDuMenu >= 0 AndAlso _ligneDuMenu < DataGridView1.RowCount AndAlso
                                  Integer.TryParse(CStr(If(DataGridView1.Rows(_ligneDuMenu).Cells("Volume").Value, "")).Trim(), actuel)
        VolumeToolStripMenuItem.Visible = possible
        If Not possible Then Exit Sub
        VolumeToolStripMenuItem.Text = If(_lignesDuMenu.Count > 1, "Volume des " & _lignesDuMenu.Count & " lignes sélectionnées", "Volume de la ligne") &
                                       "  (" & actuel & ")"
        If VolumeToolStripMenuItem.DropDownItems.Count = 0 Then
            Dim plus As New ToolStripMenuItem("+ 5") With {.Tag = "+"}
            Dim moins As New ToolStripMenuItem("- 5") With {.Tag = "-"}
            AddHandler plus.Click, AddressOf ChoixDuMenuVolume_Click
            AddHandler moins.Click, AddressOf ChoixDuMenuVolume_Click
            VolumeToolStripMenuItem.DropDownItems.Add(plus)
            VolumeToolStripMenuItem.DropDownItems.Add(moins)
            VolumeToolStripMenuItem.DropDownItems.Add(New ToolStripSeparator())
            For v As Integer = 100 To 0 Step -10
                Dim choix As New ToolStripMenuItem(v.ToString()) With {.Tag = v}
                AddHandler choix.Click, AddressOf ChoixDuMenuVolume_Click
                VolumeToolStripMenuItem.DropDownItems.Add(choix)
            Next
        End If
        ' La valeur actuelle est cochee ; + 5 et - 5 grises aux limites.
        For Each element As ToolStripItem In VolumeToolStripMenuItem.DropDownItems
            Dim choix As ToolStripMenuItem = TryCast(element, ToolStripMenuItem)
            If choix Is Nothing Then Continue For
            If TypeOf choix.Tag Is Integer Then
                choix.Checked = (CInt(choix.Tag) = actuel)
            ElseIf CStr(choix.Tag) = "+" Then
                choix.Enabled = actuel < 100
            Else
                choix.Enabled = actuel > 0
            End If
        Next
    End Sub

    Private Sub ChoixDuMenuVolume_Click(sender As Object, e As EventArgs)
        Try
            Dim choix As ToolStripMenuItem = TryCast(sender, ToolStripMenuItem)
            If choix Is Nothing OrElse _ligneDuMenu < 0 OrElse _ligneDuMenu >= DataGridView1.RowCount Then Exit Sub
            Dim actuel As Integer
            If Not Integer.TryParse(CStr(If(DataGridView1.Rows(_ligneDuMenu).Cells("Volume").Value, "")).Trim(), actuel) Then Exit Sub
            Dim nouveau As Integer
            If TypeOf choix.Tag Is Integer Then
                nouveau = CInt(choix.Tag)
            ElseIf CStr(choix.Tag) = "+" Then
                nouveau = (actuel \ PasVolumeMolette) * PasVolumeMolette + PasVolumeMolette
            Else
                nouveau = ((actuel + PasVolumeMolette - 1) \ PasVolumeMolette) * PasVolumeMolette - PasVolumeMolette
            End If
            DataGridView1.EndEdit()
            AppliquerLeVolume(If(_lignesDuMenu.Count > 0, _lignesDuMenu, New List(Of Integer) From {_ligneDuMenu}), nouveau)
        Catch ex As Exception
            DiagLog("Minuterie, menu Volume : " & ex.Message)
        End Try
    End Sub

    Private Sub MemeVolumeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MemeVolumeToolStripMenuItem.Click
        Try
            If _ligneDuMenu < 0 OrElse _ligneDuMenu >= DataGridView1.RowCount OrElse _lignesDuMenu.Count < 2 Then Beep() : Exit Sub
            Dim volume As Integer
            If Not Integer.TryParse(CStr(If(DataGridView1.Rows(_ligneDuMenu).Cells("Volume").Value, "")).Trim(), volume) Then Beep() : Exit Sub
            DataGridView1.EndEdit()
            AppliquerLeVolume(_lignesDuMenu, volume)
        Catch ex As Exception
            DiagLog("Minuterie, meme volume : " & ex.Message)
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Plusieurs films choisis a la fois (les episodes d'une serie) : mis a la suite dans la minuterie.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>
    ''' Ajoute une ligne "Film" par fichier de <paramref name="fichiers"/>, juste sous la ligne
    ''' <paramref name="premiere"/> (qui porte deja le premier episode). Chaque episode commence a
    ''' l'heure ou le precedent se termine (duree arrondie a la minute superieure, comme "Horaire a
    ''' la suite"), le jour suivant apres minuit ; il reprend le volume et les reglages du premier
    ''' (PIP, boucle...), mais pas son titre. Les lignes de la serie sont ensuite selectionnees :
    ''' il ne reste qu'a regler le volume pour toutes (molette, ou clic droit).
    ''' </summary>
    Public Sub AjouterLaSuiteDeLaSerie(premiere As Integer, fichiers As List(Of String))
        Try
            If fichiers Is Nothing OrElse fichiers.Count = 0 Then Exit Sub
            If premiere < 0 OrElse premiere >= DataGridView1.RowCount Then Exit Sub
            DataGridView1.EndEdit()
            Dim modele As DataGridViewRow = DataGridView1.Rows(premiere)
            Dim jourDepart As String = JourDeLaLigne(premiere)
            Dim volume As String = CStr(If(modele.Cells("Volume").Value, "100"))
            If volume.Trim() = "" Then volume = "100"
            ' Reglages du premier episode sans son titre (chaque episode affiche son nom de fichier).
            Dim reglages As String = System.Text.RegularExpressions.Regex.Replace(CStr(If(modele.Cells("Argument").Value, "")),
                                                                                  "!/Titre:[^!]*", "!/Titre:", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            Dim debut As Integer = MinutesDeLaLigne(premiere)
            Dim enBoot As Boolean = String.Equals(jourDepart, "BOOT", StringComparison.OrdinalIgnoreCase)
            Dim lignes As New List(Of DataGridViewRow) From {modele}
            Dim precedente As DataGridViewRow = modele
            Dim position As Integer = premiere + 1

            For Each fichier As String In fichiers
                ' Heure de debut : fin de l'episode precedent.
                If debut >= 0 Then debut += Math.Max(1, DureeEnMinutes(CStr(precedente.Cells("Durée").FormattedValue)))
                DataGridView1.Rows.Insert(position, {True, jourDepart, "00:00", "Film", "", "", "", volume, "999999"})
                Dim ligne As DataGridViewRow = DataGridView1.Rows(position)
                RemplirLaLigneFilm(position, fichier, reglages & "!/Suite:True", False)       ' (la serie garde les reglages du premier episode : rien a proposer ; "Suite" = enchaine sans attendre l'heure, voir Module1.JouerLaSuiteDeLaSerie)
                ligne.Cells("Volume").Value = volume            ' (RemplirLaLigneFilm peut l'adapter au fichier : la serie garde un seul volume)
                If debut >= 0 AndAlso Not enBoot Then PoserHeureEtJour(position, debut, jourDepart)
                lignes.Add(ligne)
                precedente = ligne
                position += 1
            Next

            ' Ordre de tri recalcule, tableau retrie, puis toute la serie selectionnee (ses lignes
            ' sont retrouvees par leur fichier : le tri les deplace).
            Dim fichiersDeLaSerie As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For Each ligne As DataGridViewRow In lignes
                fichiersDeLaSerie.Add(CStr(If(ligne.Cells("Action").Value, "")))
            Next
            AjouterUneLigne(2, premiere)
            Dim deLaSerie As New List(Of DataGridViewRow)
            For Each ligne As DataGridViewRow In DataGridView1.Rows
                If Not ligne.IsNewRow AndAlso fichiersDeLaSerie.Contains(CStr(If(ligne.Cells("Action").Value, ""))) Then deLaSerie.Add(ligne)
            Next
            If deLaSerie.Count > 0 Then
                ' Dans cet ordre : amener la serie a l'ecran, poser la case courante sur le bouton
                ' Play du premier episode (pas de saisie en cours), PUIS selectionner les lignes.
                ' Verifie au banc : sans case courante, le simple fait de faire defiler le tableau
                ' en designait une et ne laissait qu'une ligne selectionnee.
                DataGridView1.FirstDisplayedScrollingRowIndex = deLaSerie(0).Index
                DataGridView1.CurrentCell = deLaSerie(0).Cells("Play")
                DataGridView1.ClearSelection()
                For Each ligne As DataGridViewRow In deLaSerie
                    ligne.Selected = True
                Next
            End If
            DiagLog("Minuterie : serie de " & lignes.Count & " films mise a la suite" & If(enBoot, " (au BOOT : pas d'horaire)", "") &
                    ", " & DataGridView1.SelectedRows.Count & " ligne(s) selectionnee(s)")
        Catch ex As Exception
            DiagLog("Minuterie, serie de films : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Les lignes du menu qui ne sont pas deja des taches de demarrage (Jour BOOT).</summary>
    Private Function LignesAjoutablesAuBoot() As List(Of DataGridViewRow)
        Dim liste As New List(Of DataGridViewRow)
        For Each i As Integer In _lignesDuMenu
            If i < 0 OrElse i >= DataGridView1.RowCount Then Continue For
            Dim ligne As DataGridViewRow = DataGridView1.Rows(i)
            If ligne.IsNewRow Then Continue For
            If String.Equals(CStr(If(ligne.Cells("Jour").Value, "")).Trim(), "BOOT", StringComparison.OrdinalIgnoreCase) Then Continue For
            liste.Add(ligne)
        Next
        Return liste
    End Function

    Private Sub AjouterAuBootToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AjouterAuBootToolStripMenuItem.Click
        DataGridView1.EndEdit()
        _lignesACopier = LignesAjoutablesAuBoot()
        If _lignesACopier.Count = 0 Then Beep() : Exit Sub
        CopierVersLeJour("BOOT")
    End Sub

    Private Sub CopierLaLigneToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopierLaLigneToolStripMenuItem.Click
        Try
            If _lignesDuMenu.Count = 0 Then Beep() : Exit Sub
            DataGridView1.EndEdit()
            Dim copies As New List(Of Object())
            Dim infobulles As New List(Of String())
            For Each numero As Integer In _lignesDuMenu
                If numero < 0 OrElse numero >= DataGridView1.RowCount Then Continue For
                Dim source As DataGridViewRow = DataGridView1.Rows(numero)
                Dim valeurs(DataGridView1.ColumnCount - 1) As Object
                Dim bulles(DataGridView1.ColumnCount - 1) As String
                For c As Integer = 0 To DataGridView1.ColumnCount - 1
                    valeurs(c) = source.Cells(c).Value
                    bulles(c) = source.Cells(c).ToolTipText
                Next
                copies.Add(valeurs)
                infobulles.Add(bulles)
            Next
            If copies.Count = 0 Then Beep() : Exit Sub
            _lignesCopiees = copies
            _infobullesCopiees = infobulles
        Catch ex As Exception
            DiagLog("Minuterie, copier les lignes : " & ex.Message)
        End Try
    End Sub

    Private Sub CollerLaLigneToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CollerLaLigneToolStripMenuItem.Click
        Try
            If _lignesCopiees.Count = 0 Then Beep() : Exit Sub
            DataGridView1.EndEdit()
            Dim position As Integer = If(_ligneDuMenu >= 0 AndAlso _ligneDuMenu < DataGridView1.RowCount, _ligneDuMenu + 1, DataGridView1.RowCount)
            Dim premiere As DataGridViewRow = Nothing
            Dim colAction As Integer = DataGridView1.Columns("Action").Index
            For i As Integer = 0 To _lignesCopiees.Count - 1
                If _lignesCopiees(i).Length <> DataGridView1.ColumnCount Then Continue For
                Dim valeurs As Object() = CType(_lignesCopiees(i).Clone(), Object())
                DataGridView1.Rows.Insert(position, valeurs)
                Dim copie As DataGridViewRow = DataGridView1.Rows(position)
                For c As Integer = 0 To DataGridView1.ColumnCount - 1
                    copie.Cells(c).ToolTipText = _infobullesCopiees(i)(c)
                Next
                ' Ligne TV : sa liste de chaines, sur la meme chaine (voir ListeDesChainesTV).
                ListeDesActionsDuType(position, CStr(If(copie.Cells("Type").Value, "")), CStr(If(valeurs(colAction), "")))
                If premiere Is Nothing Then premiere = copie
                position += 1
            Next
            If premiere Is Nothing Then Beep() : Exit Sub
            ' Ordre de tri recalcule et tableau retrie (chaque copie a le meme jour et la meme heure
            ' que son modele : elle reste a cote de lui), puis la premiere copie est amenee a l'ecran.
            AjouterUneLigne(2, premiere.Index)
            If premiere.DataGridView IsNot Nothing Then
                DataGridView1.ClearSelection()
                DataGridView1.CurrentCell = premiere.Cells("Jour")
            End If
        Catch ex As Exception
            DiagLog("Minuterie, coller les lignes : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Multiple de 5 minutes suivant (un clic de plus avance encore de 5 min, comme
    ''' avant). Avant : plantage si la cellule etait vide ; jour inchange apres minuit.</summary>
    Private Sub HeureArrondiAu5MinuteLesPlusProchesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HeureArrondiAu5MinuteLesPlusProchesToolStripMenuItem.Click
        Dim r As Integer = LigneCliquee() : If r < 0 Then Exit Sub
        Dim m As Integer = MinutesDeLaLigne(r)
        If m < 0 Then Beep() : Exit Sub
        PoserHeureEtJour(r, (m \ 5 + 1) * 5, JourDeLaLigne(r))
    End Sub

    ''' <summary>Heure = debut de la ligne du dessus + sa duree (le jour suit apres minuit).
    ''' Avant : plantage si le jour du dessus n'etait pas dans la liste ; Lun-Ven, TLJ...
    ''' devenaient "Lundi" apres minuit ; secondes de la duree ignorees (chevauchement).</summary>
    Private Sub HeureSuivanteToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HeureSuivanteToolStripMenuItem.Click
        ' Reecrite le 20/10/2026 ("faux resultat parfois") : la ligne cliquee commence quand la ligne du DESSUS se termine
        ' (son heure + sa duree), le jour de cette ligne, avec le passage de minuit. Rien d'autre n'est regarde.
        Dim r As Integer = LigneCliquee()
        If r < 1 Then Beep() : Exit Sub                               ' (aucune ligne cliquee, ou pas de ligne au-dessus)
        Dim dessus As DataGridViewRow = DataGridView1.Rows(r - 1)

        Dim debut As Integer = MinutesDeLaLigne(r - 1)
        If debut < 0 Then Beep() : Exit Sub                           ' (la ligne du dessus n'a pas d'heure : rien a faire, sans message)

        ' Duree inconnue (un film choisi dans Nflix n'en a pas tant qu'on ne l'a pas lu avec Play) : 1 minute, sans message.
        Dim connue As Boolean = False
        Dim duree As Integer = DureeDeLaLigne(dessus, connue)
        If Not connue OrElse duree <= 0 Then duree = 1

        PoserHeureEtJour(r, debut + duree, JourDeLaLigne(r - 1))
    End Sub

    ''' <summary>Duree d'une ligne en minutes (une seconde de plus compte pour une minute entiere : la suite ne doit pas deborder sur la
    ''' fin). Cherchee dans la colonne Durée, puis dans Play ("hh:mm:ss" ou "debut - fin"), puis, pour un film d'un fichier, dans le fichier.
    ''' <paramref name="connue"/> : Faux si aucune de ces sources ne donne de duree.</summary>
    Private Function DureeDeLaLigne(ligne As DataGridViewRow, ByRef connue As Boolean) As Integer
        connue = False
        Dim m As Integer = DureeEnMinutes(Convert.ToString(ligne.Cells("Durée").FormattedValue))
        If m > 0 Then connue = True : Return m

        ' Play : "01:36:11" (duree) ou "00:10:00 - 00:45:00" (plage).
        Dim play As String = Convert.ToString(ligne.Cells("Play").FormattedValue).Trim()
        If play <> "" Then
            Dim morceaux As String() = play.Split({"-"c}, StringSplitOptions.RemoveEmptyEntries)
            If morceaux.Length = 2 Then
                Dim t1 As TimeSpan, t2 As TimeSpan
                If TimeSpan.TryParse(morceaux(0).Trim(), t1) AndAlso TimeSpan.TryParse(morceaux(1).Trim(), t2) AndAlso t2 > t1 Then
                    connue = True
                    Return CInt(Math.Ceiling((t2 - t1).TotalMinutes - 0.0001))
                End If
            ElseIf morceaux.Length = 1 Then
                m = DureeEnMinutes(morceaux(0))
                If m > 0 Then connue = True : Return m
            End If
        End If

        ' Film d'un fichier du disque : sa duree est lue dans le fichier.
        If String.Equals(Convert.ToString(ligne.Cells("Type").FormattedValue), "Film", StringComparison.OrdinalIgnoreCase) Then
            Try
                Dim fichier As String = Convert.ToString(ligne.Cells("Action").Value)
                If fichier <> "" AndAlso IO.File.Exists(fichier) Then
                    m = DureeEnMinutes(GetDuration(fichier))
                    If m > 0 Then connue = True : Return m
                End If
            Catch ex As Exception
            End Try
        End If
        Return 0
    End Function

    Private Sub ContextMenuStrip2_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs)


    End Sub

    Private Sub SupprimerLaLigneToolStripMenuItem_Click(sender As Object, e As EventArgs)
        On Error Resume Next
        r = DataGridView1.CurrentCell.RowIndex
        Me.DataGridView1.Rows(r).Selected = True

        For Each row As DataGridViewRow In DataGridView1.SelectedRows
            DataGridView1.Rows.Remove(row)
        Next
    End Sub


    Private Sub DataGridView1_MouseDown(sender As Object, e As MouseEventArgs) Handles DataGridView1.MouseDown

        Dim hit As DataGridView.HitTestInfo = DataGridView1.HitTest(e.X, e.Y)
        c = hit.ColumnIndex
        r = hit.RowIndex

        If hit.Type = DataGridViewHitTestType.Cell Then
            clickedCell = DataGridView1.Rows(hit.RowIndex).Cells(hit.ColumnIndex)
        End If
        '''
        LigneMinuterieEnCours.Index = CInt(r)
        '

        If r = -1 Then Exit Sub
        If c = -1 Then Exit Sub
        'r = DataGridView1.CurrentCell.RowIndex
        'c = DataGridView1.CurrentCell.ColumnIndex

        'DataGridView1.Rows(r).Cells(c).Selected = True

        'DataGridView1.Rows(r).Cells(c).Selected = True
        'DataGridView1.ContextMenuStrip = ContextMenuStrip2

        If c = ColSelection - 2 And e.Button = MouseButtons.Right Then
            DataGridView1.Columns("Heure").ContextMenuStrip = ContextMenuStrip1
            Exit Sub
        End If
        Me.DataGridView1.Refresh()

        If c = ColSelection And e.Button = MouseButtons.Right Then
            If Me.DataGridView1.Rows(clickedCell.rowindex).Cells("Type").FormattedValue = "Film" Then DataGridView1.Columns("Action").ContextMenuStrip = ContextMenuStrip2
            Exit Sub
        End If

        If e.Button = MouseButtons.Right Then Exit Sub

        ' Trace du clic sur la case "Play" (14/10/2026 : "beep sur un film IPTV") : de quoi savoir quelle ligne, quel type.
        If c = ColSelection + 1 Then
            DiagLog("Minuterie : clic Play ligne " & (r + 1) & " : type=" & CStr(Me.DataGridView1.Rows(r).Cells("Type").FormattedValue) &
                    " ; action=" & CStr(Me.DataGridView1.Rows(r).Cells("Action").FormattedValue) &
                    " ; reglages=" & CStr(If(Me.DataGridView1.Rows(r).Cells("Argument").Value, "")))
        End If

        If c = ColSelection And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "Film" Then
            vide = Me.DataGridView1.Rows(r).Cells("Action").FormattedValue
            If vide = "" Then Call Choix_Film(r)
            Exit Sub
        End If

        ' Fetes juives : la case Play montre la video de l'annonce (entree ou sortie).
        If c = ColSelection + 1 And UCase(CStr(Me.DataGridView1.Rows(r).Cells("Type").FormattedValue)) = "YOM TOV" Then
            YomTov.MontrerLaVideo(UCase(CStr(Me.DataGridView1.Rows(r).Cells("Action").FormattedValue)).StartsWith("ENTR"))
            Exit Sub
        End If

        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "Lancer" Then
            Call Choix_Lancer(r)
            Exit Sub
        End If

        't = Me.DataGridView1.Rows(r).Cells("Type").FormattedValue
        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "TV" Then
            Chaine = Me.DataGridView1.Rows(r).Cells("Action").FormattedValue
            VoL = Me.DataGridView1.Rows(r).Cells("Volume").FormattedValue
            If Chaine = "Chaine OFF" Then ClosePlayer() : Exit Sub
            NumChaine = Val(Mid(Chaine, 8, 2))
            If NumChaine > 0 Then Call PreviewTV(NumChaine, VoL)
            Exit Sub
        End If

        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "Sat" Then
            Chaine = Me.DataGridView1.Rows(r).Cells("Action").FormattedValue
            VoL = Me.DataGridView1.Rows(r).Cells("Volume").FormattedValue
            If Chaine = "Sat OFF" Then ClosePlayer() : Exit Sub
            NumChaine = Val(Mid(Chaine, 8, 3))
            Call PreviewSat(NumChaine, VoL, r)
            Exit Sub
        End If

        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "IPTV" Then
            Chaine = Me.DataGridView1.Rows(r).Cells("Action").FormattedValue
            VoL = Me.DataGridView1.Rows(r).Cells("Volume").FormattedValue
            If Chaine = "IPTV OFF" Then
                ClosePlayer()
                EndAndContinue()
                Exit Sub
            End If
            'NumChaine = Val(Mid(Chaine, 7, 4))
            Call PreviewIPTV_VLC(Chaine, VoL)
            Exit Sub
        End If

        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "Ecran Noir" Then
            Choix_EcranNoir(r)
            Exit Sub
        End If
        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "Mode RDV" Then
            Choix_ModeRdv(r)
            Exit Sub
        End If

        ' Tache "Music Source / Deezer" : la case Play sert a choisir la playlist de la tache.
        ' (Type "Deezer", action "Lance Deezer" ; ou l'ancienne "Music Source / Deezer".)
        Dim typeClique As String = UCase(CStr(Me.DataGridView1.Rows(r).Cells("Type").FormattedValue))
        If c = ColSelection + 1 AndAlso (typeClique = "MUSIC SOURCE" OrElse typeClique = "DEEZER") Then
            If LigneDeezerAvecPlaylist(r) Then ChoisirLaPlaylistDeLaLigne(r)
            Exit Sub
        End If

        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "Divers" Then
            Choix_Divers(r)
            Exit Sub
        End If

        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "Tuya" Then
            MontrerLeResultatTuya(r, c)          ' (la commande part, et son resultat s'affiche : envoyee, ou la raison de l'echec)
            Exit Sub
        End If

        If c = ColSelection + 1 And Me.DataGridView1.Rows(r).Cells("Type").FormattedValue = "Sonoff" Then
            result = Sonoff_Test(Me.DataGridView1.Rows(r).Cells("Action").FormattedValue)
            If result = False Then
                MsgBox("Attention" & vbCrLf & Me.DataGridView1.Rows(r).Cells("Action").FormattedValue & " non connecté !")
                Exit Sub
            End If

            Sonoff_Send(Me.DataGridView1.Rows(r).Cells("Action").FormattedValue)


            Exit Sub
        End If

        'Sonoff_Send(x)

        If c <> ColSelection + 1 Or Me.DataGridView1.Rows(r).Cells("Type").FormattedValue <> "Film" Then Exit Sub

        Call PreviewFilm(r)

    End Sub


    ''' <summary>
    ''' Modifier horaire source : la ligne du DESSUS (la "source", ex. un film) est avancee pour
    ''' se terminer juste quand commence la ligne cliquee : debut = heure cliquee - sa duree.
    ''' Avant : duree d'ecran noir ("0J2h44m") ignoree, donc rien ne changeait ; secondes
    ''' ignorees (le film debordait sur la tache suivante) ; avant minuit, TLJ devenait "Sam-Dim" ;
    ''' une erreur etait avalee en silence et la ligne restait a moitie modifiee.
    ''' </summary>
    Private Sub ModifierHoraireSourceToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ModifierHoraireSourceToolStripMenuItem.Click
        ' Reecrite le 20/10/2026. La ligne cliquee garde son horaire ; la ligne du DESSUS (la "source") est avancee pour se terminer
        ' juste quand elle commence : nouveau debut de la source = heure de la ligne cliquee - duree de la source. Le jour de la source
        ' devient celui de la ligne cliquee, ramene en arriere si le calcul passe avant minuit (Lundi 00:30 - 2 h = Dimanche 22:30) ;
        ' Lun-Ven, Sam-Dim, TLJ et BOOT restent tels quels. Rien n'est modifie si la duree de la source est inconnue (le deplacer d'une
        ' minute n'aurait aucun sens) : un message le dit.   -> 20/10/2026, a la demande de Jacob : plus de message, 1 minute comme avant.
        Dim r As Integer = LigneCliquee()
        If r < 1 Then Beep() : Exit Sub                               ' (aucune ligne cliquee, ou pas de ligne au-dessus)
        Dim source As DataGridViewRow = DataGridView1.Rows(r - 1)

        Dim debutDeLaCliquee As Integer = MinutesDeLaLigne(r)
        If debutDeLaCliquee < 0 Then Beep() : Exit Sub                ' (la ligne cliquee n'a pas d'heure : rien a faire, sans message)

        ' Duree inconnue : 1 minute, comme avant, sans message.
        Dim connue As Boolean = False
        Dim duree As Integer = DureeDeLaLigne(source, connue)
        If Not connue OrElse duree <= 0 Then duree = 1

        PoserHeureEtJour(r - 1, debutDeLaCliquee - duree, JourDeLaLigne(r))
        DataGridView1.CurrentCell = DataGridView1.Rows(r).Cells("Heure")
    End Sub

    ''' <summary>1 minute avant la sortie de Chabbat (samedi), voulu ainsi. Avant : heure videe
    ''' si l'horaire etait inconnu.</summary>
    Private Sub HoraireSortieDeShabatToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles HoraireSortieDeShabatToolStripMenuItem.Click
        Dim r As Integer = LigneCliquee() : If r < 0 Then Exit Sub
        If Not IsDate(Shabbat.Fin) Then
            MsgBox("Horaire de sortie de Chabbat inconnu (pas de connexion Internet ?)", vbExclamation)
            Exit Sub
        End If
        Dim dt As Date = CDate(Shabbat.Fin).AddMinutes(-1)
        PoserHeureEtJour(r, dt.Hour * 60 + dt.Minute, jourDeLaSemaine(5))       ' Samedi
    End Sub

    ''' <summary>Bouton "Imprimer" de la minuterie : le programme des taches actives, mis en page pour le
    ''' papier (voir ImpressionMinuterie). Le tableau est seulement lu : ni ses colonnes ni leurs largeurs
    ''' ne sont touchees (l'ancienne impression elargissait Action, cachait Actif et Play et ajoutait une
    ''' colonne "Titre" le temps d'imprimer).</summary>
    Private Sub Button15_Click(sender As Object, e As EventArgs) Handles Button15.Click
        DataGridView1.EndEdit()
        ImpressionMinuterie.Imprimer(Me, DataGridView1)
    End Sub






    Private Sub TrackBar8_Scroll(sender As Object, e As EventArgs) Handles TrackBar8.Scroll
        TextBox12.Text = TrackBar8.Value
    End Sub

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged

        Call HeureDeShabbat(ComboBox1.Text) : Label25.Text = "Entrée" & Shabbat.Debut & " Sortie" & Shabbat.Fin
        My.Settings.Shabbat_Ville = ComboBox1.Text
        Call frequenceTV()
    End Sub

    Private Sub GroupBox2_Enter(sender As Object, e As EventArgs) Handles GroupBox2.Enter

    End Sub

    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged
        Me.TextBox13.Text = Radio(ComboBox2.SelectedIndex, 1)
    End Sub

    ''' <summary>Icone "ordre" du cadre Radio (20/10/2026) : une liste de toutes les radios ou on les deplace (glisser-deposer, boutons, tri A-Z)
    ''' (FormOrdreRadios) ; "Enregistrer" ecrit la liste dans le nouvel ordre et la radio choisie reste choisie.</summary>
    Private Sub ButtonRadioOrdre_Click(sender As Object, e As EventArgs) Handles ButtonRadioOrdre.Click
        Dim n As Integer = ComboBox2.Items.Count
        If n < 2 Then
            MessageBox.Show(Me, "Il faut au moins deux radios pour changer leur ordre.", "Ordre des radios", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim noms As New List(Of String)
        For i As Integer = 0 To n - 1
            noms.Add(Convert.ToString(ComboBox2.Items(i)))
        Next
        Dim choisieAvant As String = If(ComboBox2.SelectedIndex >= 0, noms(ComboBox2.SelectedIndex), "")
        Dim nouvelOrdre As New List(Of Integer)
        If Not FormOrdreRadios.Demander(Me, noms, ComboBox2.SelectedIndex, nouvelOrdre) Then Exit Sub
        Try
            ' Les radios (nom, adresse) sont recopiees dans leur nouvel ordre, puis la liste est ecrite et relue.
            Dim anciennes As New List(Of String())
            For i As Integer = 0 To n - 1
                anciennes.Add({Radio(i, 0), Radio(i, 1)})
            Next
            For place As Integer = 0 To nouvelOrdre.Count - 1
                Dim source As String() = anciennes(nouvelOrdre(place))
                Radio(place, 0) = source(0)
                Radio(place, 1) = source(1)
                ComboBox2.Items(place) = noms(nouvelOrdre(place))
            Next
            SaveRadio()
            If choisieAvant <> "" Then
                Dim rang As Integer = ComboBox2.FindStringExact(choisieAvant)
                If rang >= 0 Then ComboBox2.SelectedIndex = rang
            End If
            DiagLog("Radios : nouvel ordre enregistre (" & n & " radios)")
        Catch ex As Exception
            DiagLog("Radios (ordre) : " & ex.Message)
            MessageBox.Show(Me, "Le nouvel ordre n'a pas pu être enregistré : " & ex.Message, "Ordre des radios", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Cadre Radio : le bouton Play devient "Pause" tant que la radio affichee joue (20/10/2026). Un clic dessus arrete la radio (la
    ' station est coupee : une radio en direct ne se met pas en pause), un nouveau clic la relance en direct. L'icone suit l'etat REEL
    ' du lecteur (minuteur de 0,7 s) : changer de station, ou la radio coupee par ailleurs, la remet sur Play.
    ' ------------------------------------------------------------------------------------------
    Private WithEvents TimerIconeRadio As New System.Windows.Forms.Timer With {.Interval = 700}
    Private _imagePlayRadio As Image
    Private _iconeRadioEnPause As Boolean = False

    Private Function LaRadioAfficheeJoue() As Boolean
        Try
            Dim url As String = TextBox13.Text.Trim()
            If url = "" Then Return False
            Return MusiqueVLC.RadioDemandeeSur(url)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub MettreAJourLIconeRadio()
        Dim joue As Boolean = LaRadioAfficheeJoue()
        If joue = _iconeRadioEnPause AndAlso _imagePlayRadio IsNot Nothing Then Exit Sub
        If _imagePlayRadio Is Nothing Then _imagePlayRadio = Button16.BackgroundImage
        _iconeRadioEnPause = joue
        If joue Then
            Button16.BackgroundImage = Nothing
            Button16.Font = New Font("Segoe UI Symbol", 16.0F, FontStyle.Regular, GraphicsUnit.Point)
            Button16.TextAlign = ContentAlignment.MiddleCenter
            Button16.Text = ChrW(&H23F8)                 ' pause
            ToolTip1.SetToolTip(Button16, "Pause (arrête la radio)")
        Else
            Button16.Text = ""
            Button16.BackgroundImage = _imagePlayRadio
            ToolTip1.SetToolTip(Button16, "Play")
        End If
    End Sub

    Private Sub TimerIconeRadio_Tick(sender As Object, e As EventArgs) Handles TimerIconeRadio.Tick
        If Me.Visible Then MettreAJourLIconeRadio()
    End Sub

    Private Sub Button16_Click(sender As Object, e As EventArgs) Handles Button16.Click
        If LaRadioAfficheeJoue() Then
            MusiqueVLC.MusiqueArreter()                  ' la radio s'arrete ; un nouveau clic la relance
            DiagLog("Radio (reglages) : arretee par le bouton Pause")
        Else
            MusiqueVLC.MusiqueJouerRadio(TextBox13.Text)
            DiagLog("Radio (reglages) : lancee par le bouton Play (" & TextBox13.Text & ")")
        End If
        MettreAJourLIconeRadio()
        TimerIconeRadio.Start()
    End Sub

    ''' <summary>Icone "verifier" du cadre Radio (20/10/2026) : controle toutes les radios de la liste (FormVerifierRadios), puis propose une liste a
    ''' cocher des radios qui ne repondent pas pour les supprimer d'un coup (enregistre tout de suite).</summary>
    Private Sub ButtonVerifierRadios_Click(sender As Object, e As EventArgs) Handles ButtonVerifierRadios.Click
        Dim radios As New List(Of FormVerifierRadios.RadioAVerifier)
        For i As Integer = 0 To ComboBox2.Items.Count - 1
            Dim nom As String = Convert.ToString(ComboBox2.Items(i))
            If nom.StartsWith("# DELETED # ", StringComparison.Ordinal) Then Continue For
            If String.IsNullOrWhiteSpace(Radio(i, 1)) Then Continue For
            radios.Add(New FormVerifierRadios.RadioAVerifier With {.Rang = i, .Nom = nom, .Adresse = Convert.ToString(Radio(i, 1))})
        Next
        If radios.Count = 0 Then
            MessageBox.Show(Me, "La liste ne contient aucune radio à vérifier.", "Vérifier les radios", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim aSupprimer As New List(Of Integer)
        If Not FormVerifierRadios.Demander(Me, radios, aSupprimer) Then Exit Sub
        Try
            For Each rang As Integer In aSupprimer
                If rang >= 0 AndAlso rang < ComboBox2.Items.Count Then ComboBox2.Items(rang) = "# DELETED # " & Convert.ToString(ComboBox2.Items(rang))
            Next
            SaveRadio()                                   ' (ecrit la liste sans les radios marquees, puis la relit)
            If ComboBox2.Items.Count > 0 Then ComboBox2.SelectedIndex = 0
            DiagLog("Radios : " & aSupprimer.Count & " radio(s) supprimee(s) apres verification")
        Catch ex As Exception
            DiagLog("Radios (suppression apres verification) : " & ex.Message)
            MessageBox.Show(Me, "Les radios n'ont pas pu être supprimées : " & ex.Message, "Vérifier les radios", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ''' <summary>Icone "radio suivante" du cadre Radio (20/10/2026) : passe a la radio d'apres dans la liste (de la derniere, retour a la
    ''' premiere ; les radios marquees "# DELETED #" sont sautees) et la joue.</summary>
    Private Sub ButtonRadioSuivante_Click(sender As Object, e As EventArgs) Handles ButtonRadioSuivante.Click
        Dim n As Integer = ComboBox2.Items.Count
        If n = 0 Then Exit Sub
        Dim depart As Integer = ComboBox2.SelectedIndex
        For pas As Integer = 1 To n
            Dim i As Integer = ((depart + pas) Mod n + n) Mod n
            Dim nom As String = Convert.ToString(ComboBox2.Items(i))
            If nom.StartsWith("# DELETED # ", StringComparison.Ordinal) Then Continue For
            ComboBox2.SelectedIndex = i                 ' (met a jour l'URL : ComboBox2_SelectedIndexChanged)
            MusiqueVLC.MusiqueJouerRadio(TextBox13.Text)
            Exit Sub
        Next
    End Sub

    Private Sub Button17_Click(sender As Object, e As EventArgs) Handles Button17.Click

        ' Corbeille du cadre Radio : demande confirmation avant de supprimer (20/10/2026). Une radio deja marquee "# DELETED #" est
        ' simplement restauree (pas de question). La suppression est definitive avec "Enregistrer les changements".
        Dim rang As Integer = ComboBox2.SelectedIndex
        If rang < 0 Then Beep() : Exit Sub
        Dim texte As String = Convert.ToString(ComboBox2.Items(rang))
        If texte.StartsWith("# DELETED # ", StringComparison.Ordinal) Then
            ComboBox2.Items(rang) = texte.Substring(12)
            Exit Sub
        End If
        Dim reponse As DialogResult = MessageBox.Show(Me, "Supprimer la radio « " & texte & " » de la liste ?", "Supprimer une radio",
                                                      MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
        If reponse <> DialogResult.Yes Then Exit Sub
        ' Supprimee et enregistree tout de suite (le bouton "Enregistrer les changements" n'existe plus).
        Try
            ComboBox2.Items(rang) = "# DELETED # " & texte
            SaveRadio()                                  ' (ecrit la liste sans la radio, puis la relit)
            If ComboBox2.Items.Count > 0 Then
                ' La radio qui prend la place de celle supprimee est selectionnee (la derniere si c'etait la derniere).
                ComboBox2.SelectedIndex = Math.Min(rang, ComboBox2.Items.Count - 1)
            Else
                TextBox13.Text = ""
            End If
            DiagLog("Radio supprimee : " & texte)
        Catch ex As Exception
            DiagLog("Radio (suppression) : " & ex.Message)
            MessageBox.Show(Me, "La radio n'a pas pu être supprimée : " & ex.Message, "Supprimer une radio", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub



    Private Sub ComboBox2_TextChanged(sender As Object, e As EventArgs) Handles ComboBox2.TextChanged

    End Sub

    ''' <summary>Bouton "+" du cadre Radio : fenetre d'ajout (FormAjoutRadio : nom, adresse, essai), puis la radio est ajoutee a la liste,
    ''' enregistree tout de suite (data\radio.txt) et selectionnee. Avant (20/10/2026) le bouton ne faisait rien.</summary>
    Private Sub Button19_Click(sender As Object, e As EventArgs) Handles Button19.Click
        Dim n As Integer = ComboBox2.Items.Count
        If n >= 499 Then
            MessageBox.Show(Me, "La liste des radios est pleine (499 radios au plus).", "Ajouter une radio", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If
        Dim noms As New List(Of String)
        Dim adresses As New List(Of String)
        For i As Integer = 0 To n - 1
            Dim nom As String = Convert.ToString(ComboBox2.Items(i))
            If nom.StartsWith("# DELETED # ", StringComparison.Ordinal) Then Continue For      ' (radio supprimee : son nom est libre)
            noms.Add(nom)
            adresses.Add(Convert.ToString(Radio(i, 1)))
        Next
        Dim nomChoisi As String = "", adresseChoisie As String = ""
        Dim proposees As New List(Of AnnuaireRadios.RadioProposee)
        If Not FormAjoutRadio.Demander(Me, noms, adresses, nomChoisi, adresseChoisie, proposees) Then Exit Sub

        ' Une radio saisie, ou plusieurs radios cochees parmi celles de l'annuaire d'Internet.
        Dim aAjouter As New List(Of KeyValuePair(Of String, String))
        If proposees.Count > 0 Then
            For Each p As AnnuaireRadios.RadioProposee In proposees
                aAjouter.Add(New KeyValuePair(Of String, String)(p.Nom, p.Adresse))
            Next
        Else
            aAjouter.Add(New KeyValuePair(Of String, String)(nomChoisi, adresseChoisie))
        End If
        If n + aAjouter.Count > 499 Then
            MessageBox.Show(Me, "La liste ne peut pas contenir plus de 499 radios : il reste de la place pour " & (499 - n) & " radio(s).", "Ajouter une radio", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If

        Try
            Dim rangAjout As Integer = n
            For Each r As KeyValuePair(Of String, String) In aAjouter
                Radio(rangAjout, 0) = r.Key
                Radio(rangAjout, 1) = r.Value
                ComboBox2.Items.Add(r.Key)
                rangAjout += 1
            Next
            SaveRadio()                                  ' (ecrit la liste, puis la relit : les "# DELETED #" disparaissent)
            Dim rang As Integer = ComboBox2.FindStringExact(aAjouter(0).Key)
            If rang >= 0 Then ComboBox2.SelectedIndex = rang
            DiagLog("Radio(s) ajoutee(s) : " & String.Join(" ; ", aAjouter.Select(Function(r) r.Key & " (" & r.Value & ")")))
        Catch ex As Exception
            DiagLog("Radio (ajout) : " & ex.Message)
            MessageBox.Show(Me, "La radio n'a pas pu être enregistrée : " & ex.Message, "Ajouter une radio", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    ' (Le bouton "Enregistrer les changements" du cadre Radio a ete retire le 20/10/2026 : l'ajout, la corbeille et la verification
    '  enregistrent la liste tout de suite, voir SaveRadio.)

    Private Sub ListView1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView1.SelectedIndexChanged

    End Sub

    Private Sub Label28_Click(sender As Object, e As EventArgs)

    End Sub

    Sub Button20_Click(sender As Object, e As EventArgs) Handles Button20.Click
        a = MsgBox("Effacer le fichier cache des réglages ?" & vbCrLf & vbCrLf &
                   "Les réglages enregistrés (user.config) seront supprimés : Slide reprendra ses valeurs par défaut." & vbCrLf &
                   "La connexion Deezer (dossier Deezer) et le dépôt de publication (depot_maj) ne sont pas touchés." & vbCrLf & vbCrLf &
                   "Slide va se relancer tout de suite, sans enregistrer les réglages actuels.", vbOKCancel)
        If a <> 1 Then Exit Sub
        ' Seuls les dossiers de reglages "<exe>_Url_<hash>" de %LocalAppData%\Slide sont supprimes (plus Deezer ni depot_maj).
        Dim rep As String = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), My.Application.Info.Title)
        Dim supprimes As Integer = 0, echecs As New List(Of String)
        If Directory.Exists(rep) Then
            For Each dossier As String In Directory.GetDirectories(rep, "*_Url_*")
                Try
                    Directory.Delete(dossier, True)
                    supprimes += 1
                Catch ex As Exception
                    echecs.Add(Path.GetFileName(dossier) & " : " & ex.Message)
                End Try
            Next
        End If
        DiagLog("Fichier cache des reglages : " & supprimes & " dossier(s) supprime(s), " & echecs.Count & " echec(s)")
        If echecs.Count > 0 Then
            MsgBox("Certains dossiers n'ont pas pu être supprimés :" & vbCrLf & String.Join(vbCrLf, echecs), MsgBoxStyle.Exclamation)
            Exit Sub
        End If

        ' Relance immediate SANS enregistrer les reglages : la fermeture normale (My.Settings.Save, SaveMySettingsOnExit)
        ' recreerait le fichier qui vient d'etre supprime. Deezer est ferme proprement avant le nouveau programme
        ' (meme dossier de donnees WebView2), comme dans la relance de nuit.
        Try
            Dim exe As String = Environment.ProcessPath
            If String.IsNullOrEmpty(exe) Then Throw New InvalidOperationException("chemin du programme inconnu")
            SaveRestoreLeSlide(1)                            ' position du diaporama
            ArreterTousLesPip()
            Try
                MusiqueDeezer.Fermer()
                Dim attente As Stopwatch = Stopwatch.StartNew()
                Do While attente.ElapsedMilliseconds < 2500
                    Application.DoEvents()
                    Threading.Thread.Sleep(50)
                Loop
            Catch ex As Exception
                DiagLog("Fichier cache des reglages : fermeture de Deezer : " & ex.Message)
            End Try
            Process.Start(New ProcessStartInfo(exe) With {.UseShellExecute = True, .WorkingDirectory = Path.GetDirectoryName(exe)})
            DiagLog("Fichier cache des reglages : nouveau programme lance, fermeture de celui-ci sans enregistrer")
            Environment.Exit(0)
        Catch ex As Exception
            DiagLog("Fichier cache des reglages : relance impossible (" & ex.Message & ")")
            MsgBox("Réglages supprimés, mais Slide n'a pas pu se relancer : " & ex.Message & vbCrLf & "Fermez puis relancez Slide.", MsgBoxStyle.Exclamation)
        End Try
    End Sub

    Private Sub TrackBar9_Scroll(sender As Object, e As EventArgs) Handles TrackBar9.Scroll
        TextBox14.Text = TrackBar9.Value
    End Sub

    Private Sub Label24_Click(sender As Object, e As EventArgs) Handles Label24.Click

    End Sub

    Private Sub Button21_Click(sender As Object, e As EventArgs) Handles Button21.Click


        FolderBrowserDialog1.ShowDialog()
        folderName = FolderBrowserDialog1.SelectedPath
        TextBox15.Text = folderName
        DatabasePath = DatabaseNetPath & "\slide.accdb"
        ChargeDataMinuterieActive()


    End Sub

    Private Sub CheckBox15_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox15.CheckedChanged


    End Sub

    Private Sub CheckBox15_Click(sender As Object, e As EventArgs) Handles CheckBox15.Click

        If CheckBox15.Checked = True Then
            MinuterieEnReseaux = True
            Button21.Enabled = True
            TextBox15.Enabled = True

            If TextBox15.Text = "" Then
                Call Button21.PerformClick()
            Else
                DatabasePath = DatabaseNetPath & "\slide.accdb"
                ChargeDataMinuterieActive()
            End If
        Else
            MinuterieEnReseaux = False
            Button21.Enabled = False
            TextBox15.Enabled = False
            DatabasePath = DatabaseLocalPath
            ChargeDataMinuterieActive()
        End If
    End Sub

    Private Sub Button22_Click(sender As Object, e As EventArgs) Handles Button22.Click

        Try
            Dim Ligne = DataGridView1.CurrentCell.RowIndex
            DataGridView1.Rows(Ligne).Selected = True
            Clipboard.SetDataObject(Me.DataGridView1.GetClipboardContent)
        Catch ex As Exception

        End Try

    End Sub

    Private Sub Button23_Click(sender As Object, e As EventArgs) Handles Button23.Click




        If Clipboard.ContainsText Then
            Dim Ligne = DataGridView1.CurrentCell.RowIndex
            Dim CopyText As String
            CopyText = Clipboard.GetText

            nb = CopyText.Split(vbCrLf).Count

            For i = 0 To nb - 1
                L = CopyText.Split(vbCrLf)(i)
                Dim Items() As String
                Items = L.Split(ChrW(Keys.Tab))
                Try


                    DataGridView1.Rows.Insert(Ligne, TexteEnBooleen(Items(1)), Items(2), Items(3), TypeSansVlcExterne(Items(4)), Items(5), Items(6), Items(7), Items(8), "999999")
                Catch ex As Exception
                    Beep()
                End Try
            Next

        End If





    End Sub



    Private Sub CheckBox17_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox17.CheckedChanged
        If CheckBox17.Checked = True Then CheckBox18.Checked = False

    End Sub

    Private Sub CheckBox18_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox18.CheckedChanged
        If CheckBox18.Checked = True Then CheckBox17.Checked = False
    End Sub

    Private Sub ContextMenuStrip2_Opening_1(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles ContextMenuStrip2.Opening
        PreparerLeMenuOptions(ContextMenuStrip2)
        MontrerCopierColler(ContextMenuStrip2)
    End Sub

    Private Sub OptionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OptionToolStripMenuItem.Click

        Try
            Dim cb As DataGridViewTextBoxCell = DirectCast(DataGridView1.Rows(clickedCell.rowindex).Cells("Action"), DataGridViewTextBoxCell)
            Nom = cb.Value
        Catch ex As Exception
            Beep()
            Exit Sub
        End Try


        Form8.Show()
        ' Centree sur l'ecran a chaque ouverture (la fenetre peut etre reutilisee, ou deplacee la fois d'avant).
        Dim zoneEcran As Rectangle = Screen.FromControl(Me).WorkingArea
        Form8.Location = New Point(zoneEcran.Left + (zoneEcran.Width - Form8.Width) \ 2, zoneEcran.Top + (zoneEcran.Height - Form8.Height) \ 2)
        Form8.TopMost = True
        trouve = 0
        trouve2 = 0

        If InStr(Nom, "#") > 0 Then
            For y = Len(Nom) To 1 Step -1
                If Mid(Nom, y, 1) = "\" Then trouve = y : Exit For
            Next
            'SourceDir = Left(Nom, trouve - 1)
            SourceDir = Nom.substring(0, trouve - 1)
            trouve2 = InStr(Nom, "*")
            'mot = Mid(Nom, trouve + 3, trouve2 - trouve - 3)
            mot = Nom.substring(trouve + 2, trouve2 - trouve - 3)
            'ext = Mid(Nom, trouve2)
            ext = Nom.substring(trouve2 - 1)

            Form8.TextBox1.Text = SourceDir
            Form8.TextBox2.Text = mot
            Form8.TextBox3.Text = ext
            Form8.TextBox4.Text = ReplayCherche(SourceDir, mot, ext)

        End If

    End Sub

    ''' <summary>"Quitter le programme" : meme fermeture que le menu End de Slide (fenetre de
    ''' fermeture, position et reglages sauvegardes). Avant, "End" coupait tout sans rien enregistrer.</summary>
    Private Sub Button24_Click(sender As Object, e As EventArgs) Handles Button24.Click
        Dim principale As Form3 = TryCast(FenetrePrincipale(), Form3)
        If principale IsNot Nothing Then principale.EndToolStripMenuItem.PerformClick()
        End
    End Sub

    Private Sub TabPage3_Click(sender As Object, e As EventArgs) Handles TabPage3.Click

    End Sub



    Private Sub CheckBox5_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox5.CheckedChanged
        If CheckBox5.Checked = True Then
            GroupBox9.Enabled = True
        Else
            GroupBox9.Enabled = False
        End If
    End Sub

    Private Sub ListView1_MouseClick(sender As Object, e As MouseEventArgs) Handles ListView1.MouseClick
        IndexOfSubItemAtXY = 0
        Dim ht As ListViewHitTestInfo = ListView1.HitTest(e.X, e.Y)
        If ht.Item IsNot Nothing Then
            IndexOfSubItemAtXY = ht.Item.SubItems.IndexOf(ht.SubItem)
        Else
            Exit Sub
        End If

        ' Ctrl ou Maj + clic : on choisit des dossiers, on ne bascule pas "NON" (menu du clic droit pour la selection).
        If (Control.ModifierKeys And (Keys.Control Or Keys.Shift)) <> Keys.None Then Exit Sub


        Try
            l = ListView1.SelectedItems(0).Index
        Catch ex As Exception
            OptionChange = True
            Exit Sub
        End Try




        If IndexOfSubItemAtXY = 0 Then
            ''ListView1.Items(l).checked = Not ListView1.Items(l).checked
            OptionChange = True
            Exit Sub
        End If

        Try
            a = ListView1.Items(l).SubItems(IndexOfSubItemAtXY).Text
            If a = "NON" Then
                a = ""
            Else
                a = "NON"
            End If

            ListView1.Items(l).SubItems(IndexOfSubItemAtXY).Text = a
            OptionChange = True
        Catch ex As Exception

        End Try

        'For Each item In ListView1.CheckedItems
        'MsgBox(item.Text)
        'Next

    End Sub

    Private Sub NePasPrendreToolStripMenuItem_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub TrackBar10_Scroll(sender As Object, e As EventArgs) Handles TrackBar10.Scroll
        TextBox17.Text = TrackBar10.Value
        VolumeVLCBoost = Val(TextBox7.Text)

    End Sub

    Private Sub TrackBar11_Scroll(sender As Object, e As EventArgs) Handles TrackBar11.Scroll
        TextBox18.Text = TrackBar11.Value
    End Sub

    Private Sub Button25_Click(sender As Object, e As EventArgs) Handles Button25.Click
        CheminFilm = DossierNflixParDefault
        OpenFileDialog1.InitialDirectory = CheminFilm
        Button25.BackColor = CouleurDuChoix
        Button25.ForeColor = Color.White
        Button9.BackColor = CouleurHorsChoix
        Button13.BackColor = CouleurHorsChoix
        Button8.BackColor = CouleurHorsChoix

        ' La ligne a remplir : la ligne courante de la minuterie (une ligne est ajoutee si le tableau est vide).
        If DataGridView1.RowCount = 0 Then AjouterUneLigne(1, 1)
        ChoixNflix.Ouvrir(DataGridView1.Rows(Math.Min(LigneCouranteMinuterie(), DataGridView1.RowCount - 1)), IptvSources.GenreIptv.Chaines)

    End Sub

    Private Sub DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick

    End Sub

    Private Sub Button26_Click(sender As Object, e As EventArgs) Handles Button26.Click
        a = MsgBox("Vraiment éffacer l'Historique ?" & vbCrLf & "Le programme va redemarrer", vbOKCancel)
        If a = vbOK Then
            If File.Exists(GridPhoto) Then My.Computer.FileSystem.DeleteFile(GridPhoto)
            If File.Exists(GridVideo) Then My.Computer.FileSystem.DeleteFile(GridVideo)
            If File.Exists(GridMusic) Then My.Computer.FileSystem.DeleteFile(GridMusic)
            If File.Exists(GridName) Then My.Computer.FileSystem.DeleteFile(GridName)
            CaseAcocherVide(1)
            CaseAcocherVide(2)
            NumPhoto = -1
            NumVideo = -1

#If DEBUG Then
            Stop
#End If
            Application.Exit()
            Application.Restart()
            'wait(1)
            End

        End If

    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Trois IPTV (voir IptvSources) : "Maj" de chaque ligne, et "Tout mettre a jour".
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Retient les champs du cadre IPTV (noms, adresses, et pour un compte Xtream l'utilisateur et le
    ''' mot de passe). Liste .m3u : laisser Utilisateur et Mot de passe vides.</summary>
    Private Sub IptvEnregistrerLesChamps()
        IptvSources.Enregistrer({TextBoxIptvNom1.Text, TextBoxIptvNom2.Text, TextBoxIptvNom3.Text},
                                {TextBox21.Text, TextBoxIptvUrl2.Text, TextBoxIptvUrl3.Text},
                                {TextBoxIptvUtilisateur1.Text, TextBoxIptvUtilisateur2.Text, TextBoxIptvUtilisateur3.Text},
                                {TextBoxIptvMotDePasse1.Text, TextBoxIptvMotDePasse2.Text, TextBoxIptvMotDePasse3.Text})
    End Sub

    ''' <summary>Retient les noms et adresses ecrits dans la fenetre, puis telecharge la liste de la source
    ''' <paramref name="i"/>. Rend Vrai si la liste est a jour.</summary>
    Private Function IptvMettreAJour(i As Integer, Optional avecMessages As Boolean = True) As Boolean
        IptvEnregistrerLesChamps()
        If IptvSources.UrlIptv(i) = "" Then
            If avecMessages Then MessageBox.Show(Me, "L'IPTV « " & IptvSources.NomIptv(i) & " » n'a pas d'adresse.", "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Cursor = Cursors.WaitCursor
        Dim erreur As String = ""
        Dim ok As Boolean
        ' Compte Xtream : les films et les series sont remis a jour avec les chaines (quelques secondes de plus).
        Dim erreurFilms As String = "", erreurSeries As String = ""
        Dim xtream As Boolean = IptvSources.EstXtream(i)
        Dim filmsOk As Boolean, seriesOk As Boolean
        Try
            ok = IptvSources.Telecharger(i, erreur)
            If ok AndAlso xtream Then
                filmsOk = IptvSources.TelechargerCatalogue(i, IptvSources.GenreIptv.Films, erreurFilms)
                seriesOk = IptvSources.TelechargerCatalogue(i, IptvSources.GenreIptv.Series, erreurSeries)
            End If
        Finally
            Cursor = DefaultCursor
        End Try
        If ok Then
            If i = IptvSources.SelectionIptv Then CheminFilm = DossierNflixParDefault
            If xtream AndAlso avecMessages Then
                Dim bilan As String = "« " & IptvSources.NomIptv(i) & " » est à jour :" & vbCrLf &
                    "  " & IptvSources.NombreDEntrees(IptvSources.FichierIptv(i)) & " chaînes en direct" & vbCrLf &
                    "  " & If(filmsOk, IptvSources.NombreDEntrees(IptvSources.FichierDuGenre(i, IptvSources.GenreIptv.Films)) & " films", "films : " & erreurFilms) & vbCrLf &
                    "  " & If(seriesOk, IptvSources.NombreDEntrees(IptvSources.FichierDuGenre(i, IptvSources.GenreIptv.Series)) & " séries", "séries : " & erreurSeries)
                MessageBox.Show(Me, bilan, "IPTV", MessageBoxButtons.OK, If(filmsOk AndAlso seriesOk, MessageBoxIcon.Information, MessageBoxIcon.Warning))
            End If
        ElseIf avecMessages Then
            MessageBox.Show(Me, "La liste de « " & IptvSources.NomIptv(i) & " » n'a pas pu être téléchargée :" & vbCrLf & erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
        Return ok
    End Function

    Private Sub Button27_Click(sender As Object, e As EventArgs) Handles Button27.Click
        IptvMettreAJour(0)
    End Sub

    Private Sub ButtonIptvMaj2_Click(sender As Object, e As EventArgs) Handles ButtonIptvMaj2.Click
        IptvMettreAJour(1)
    End Sub

    Private Sub ButtonIptvMaj3_Click(sender As Object, e As EventArgs) Handles ButtonIptvMaj3.Click
        IptvMettreAJour(2)
    End Sub

    Private Sub ButtonIptvToutMaj_Click(sender As Object, e As EventArgs) Handles ButtonIptvToutMaj.Click
        Dim faits As New List(Of String), ratees As New List(Of String)
        For i As Integer = 0 To IptvSources.NombreIptv - 1
            Dim adresse As String = {TextBox21.Text, TextBoxIptvUrl2.Text, TextBoxIptvUrl3.Text}(i).Trim()
            If adresse = "" Then Continue For
            If IptvMettreAJour(i, False) Then faits.Add(IptvSources.NomIptv(i)) Else ratees.Add(IptvSources.NomIptv(i))
        Next
        If faits.Count = 0 AndAlso ratees.Count = 0 Then
            MessageBox.Show(Me, "Aucune IPTV n'a d'adresse.", "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
        ElseIf ratees.Count = 0 Then
            MessageBox.Show(Me, "Listes à jour : " & String.Join(", ", faits) & ".", "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Else
            MessageBox.Show(Me, "Listes non téléchargées : " & String.Join(", ", ratees) & "." &
                            If(faits.Count > 0, vbCrLf & "À jour : " & String.Join(", ", faits) & ".", ""), "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End If
    End Sub


    Private Sub TextBox23_TextChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub GroupBox6_Enter(sender As Object, e As EventArgs) Handles GroupBox6.Enter

    End Sub

    Private Sub LancerAuBOOTToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles LancerAuBOOTToolStripMenuItem.Click
        Dim r As Integer = LigneCliquee() : If r < 0 Then Exit Sub
        PoserHeureEtJour(r, 0, "BOOT")
    End Sub

    ''' <summary>1 minute avant l'entree de Chabbat (vendredi). Avant : heure videe si
    ''' l'horaire etait inconnu.</summary>
    Private Sub Horaire1mnAvantEntréeDeShabbatToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles Horaire1mnAvantEntréeDeShabbatToolStripMenuItem.Click
        Dim r As Integer = LigneCliquee() : If r < 0 Then Exit Sub
        If Not IsDate(Shabbat.Debut) Then
            MsgBox("Horaire d'entrée de Chabbat inconnu (pas de connexion Internet ?)", vbExclamation)
            Exit Sub
        End If
        Dim dt As Date = CDate(Shabbat.Debut).AddMinutes(-1)
        PoserHeureEtJour(r, dt.Hour * 60 + dt.Minute, jourDeLaSemaine(4))       ' Vendredi

    End Sub

    Private Sub ContextMenuStrip3_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles ContextMenuStrip3.Opening

    End Sub

    Private Sub StreamTestToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles StreamTestToolStripMenuItem.Click
        NasSpeed()
    End Sub

    Private Sub Button3_KeyDown(sender As Object, e As KeyEventArgs) Handles Button3.KeyDown
        If e.KeyCode = Keys.ControlKey Then LancerLePremierLancementDeLaMinuterie = False
    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs)

    End Sub

    Private Sub CheckBox9_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox9.CheckedChanged
        If CheckBox9.Checked = True Then
            CheckBox27.Enabled = True
        Else
            CheckBox27.Enabled = False
        End If
    End Sub



    Private Sub Button28_Click(sender As Object, e As EventArgs) Handles Button28.Click

        Button6.PerformClick()
        DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Volume").Value = VolumeNetflixPrimeParDefault.ToString
        FlagExit = False
        Application.DoEvents()
        Dim startInfo As New ProcessStartInfo()
        Dim myprocess As New Process()
        nom = "www.netflix.fr"
        startInfo.FileName = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe "
        startInfo.Arguments = nom & " -new-window"
        myprocess.StartInfo = startInfo
        myprocess.Start()
        SetVideoVolSlide(0)
        SetMusicVolSlide(0)
        SetVol(VolumeTVParDefault)
    End Sub

    Private Sub Button29_Click(sender As Object, e As EventArgs) Handles Button29.Click
        Button6.PerformClick()
        DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex).Cells("Volume").Value = VolumeNetflixPrimeParDefault.ToString
        FlagExit = False
        Application.DoEvents()
        Dim startInfo As New ProcessStartInfo()
        Dim myprocess As New Process()
        nom = "www.primevideo.fr"
        startInfo.FileName = "C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe "
        startInfo.Arguments = nom & " -new-window"
        myprocess.StartInfo = startInfo
        myprocess.Start()
        SetVideoVolSlide(0)
        SetMusicVolSlide(0)
        SetVol(VolumeTVParDefault)
    End Sub

    Private Sub CollerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CollerToolStripMenuItem.Click
        Dim cb As DataGridViewTextBoxCell = DirectCast(DataGridView1.Rows(clickedCell.rowindex).Cells("Action"), DataGridViewTextBoxCell)
        cb.Value = Clipboard.GetText
        DataGridView1.ClearSelection()
    End Sub

    Private Sub Label51_Click(sender As Object, e As EventArgs) Handles Label51.Click

    End Sub

    Private Sub TrackBar12_Scroll(sender As Object, e As EventArgs) Handles TrackBar12.Scroll
        TextBox25.Text = TrackBar12.Value
    End Sub

    Private Sub TabPage1_Click(sender As Object, e As EventArgs) Handles TabPage1.Click

    End Sub

    Private Sub TabPage4_Click(sender As Object, e As EventArgs) Handles TabPage4.Click

    End Sub

    Private Sub CheckBox31_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox31.CheckedChanged

    End Sub

    Private Sub CheckBox31_Click(sender As Object, e As EventArgs) Handles CheckBox31.Click
        ' Case cochee alors qu'une date manque ou n'est pas valide : toute la periode.
        If CheckBox31.Checked = True Then DatesRdvParDefaut(seulementSiAbsentes:=True)
    End Sub

    ''' <summary>
    ''' Dates du mode RDV par defaut (demande de Jacob, 06/10) : date de debut = la photo ou la
    ''' video la plus ancienne, date de fin = la plus recente. Proposees a l'ouverture des reglages
    ''' tant que le mode RDV n'est pas actif ; un RDV actif garde les dates choisies (sauf si
    ''' l'une manque ou n'est pas valide). Les dates viennent des listes de photos et de videos
    ''' elles-memes : l'ancien calcul, d'apres les dates mini/maxi memorisees, donnait 01/01/0001
    ''' quand il n'y avait pas de video.
    ''' </summary>
    Private Sub DatesRdvParDefaut(seulementSiAbsentes As Boolean)
        Try
            If seulementSiAbsentes AndAlso IsDate(TextBox26.Text) AndAlso IsDate(TextBox27.Text) Then Exit Sub
            Dim ancienne As Date, recente As Date
            If Not ModeRendezVous.PeriodeComplete(ancienne, recente) Then Exit Sub      ' aucune photo ni video datee
            TextBox26.Text = Format(ancienne, "dd/MM/yyyy")
            TextBox27.Text = Format(recente, "dd/MM/yyyy")
        Catch ex As Exception
            DiagLog("Mode RDV, dates par defaut : " & ex.Message)
        End Try
    End Sub

    Private Sub Button30_Click(sender As Object, e As EventArgs) Handles Button30.Click

        LoadMinuterieFromSettings()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Normalisation a refaire ? A l'ouverture des reglages, Slide previent SEULEMENT si la liste
    ' des sources de musique (onglet Sources : dossiers coches, musique activee) a change depuis la
    ' derniere normalisation (demande de Jacob). L'empreinte de la liste est gardee dans
    ' data\NormalisationMusique.txt a la fin de chaque normalisation.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Dossiers de musique actifs de l'onglet Sources (coches, musique non desactivee),
    ''' tries : l'empreinte de la liste.</summary>
    Private Function EmpreinteDesSourcesMusique() As List(Of String)
        Dim dossiers As New List(Of String)
        For Each it As ListViewItem In ListView1.Items
            If Not it.Checked OrElse it.SubItems.Count < 4 Then Continue For
            If String.Equals(it.SubItems(3).Text, "NON", StringComparison.OrdinalIgnoreCase) Then Continue For
            dossiers.Add(it.SubItems(0).Text.Trim().TrimEnd("\"c).ToLowerInvariant())
        Next
        dossiers.Sort(StringComparer.Ordinal)
        Return dossiers.Distinct().ToList()
    End Function

    Private Async Sub Form4_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        If NormalisationMusique.EnCours Then Exit Sub              ' deja en train de la faire
        Dim actuelle As List(Of String) = EmpreinteDesSourcesMusique()
        If actuelle.Count = 0 Then Exit Sub
        Dim derniere As New List(Of String)
        Try
            Dim fichier As String = NormalisationMusique.FichierEmpreinte()
            If IO.File.Exists(fichier) Then derniere = IO.File.ReadAllLines(fichier).Where(Function(l) l.Trim() <> "").ToList()
        Catch ex As Exception
        End Try
        If derniere.SequenceEqual(actuelle) Then Exit Sub        ' liste inchangee : rien a dire

        ' La question n'a de sens que s'il y a des MP3 dans ces sources (mp3gain ne traite que les MP3) ; la recherche se
        ' fait hors du fil de l'interface : un dossier de photos est parcouru en entier.
        Dim avecMp3 As List(Of String) = Await Task.Run(Function() NormalisationMusique.DossiersAvecMp3(actuelle))
        If IsDisposed Then Exit Sub
        If avecMp3.Count = 0 Then
            NormalisationMusique.MemoriserEmpreinte(actuelle)   ' rien a normaliser : plus de question tant que la liste ne change pas
            DiagLog("Normalisation : aucun MP3 dans les sources de musique, pas de proposition")
            Exit Sub
        End If

        Dim pourquoi As String = If(derniere.Count = 0,
            "La normalisation du volume des musiques n'a jamais été faite avec ces sources.",
            "La liste des sources de musique a changé depuis la dernière normalisation du volume.")
        Dim reponse As MsgBoxResult = MsgBox(pourquoi & vbCrLf & vbCrLf &
            "Sans normalisation, certaines musiques démarrent trop fort pendant quelques secondes." & vbCrLf & vbCrLf &
            "Lancer la normalisation maintenant ?", vbYesNo Or vbQuestion, "Volume des musiques")
        If reponse = MsgBoxResult.Yes Then
            TabControl1.SelectedTab = TabPage1                    ' onglet Sources
            BeginInvoke(New Action(Sub() Button31.PerformClick()))
        Else
            NormalisationMusique.MemoriserEmpreinte(actuelle)   ' plus de question tant que la liste ne change pas
            NormalisationMusique.NePasLancerCetteFois()         ' ni de lancement automatique pendant cette utilisation
        End If
    End Sub

    ''' <summary>
    ''' "Volume Normalizer" : met toutes les musiques MP3 au meme niveau, une fois pour toutes, avec
    ''' mp3gain (gain applique sans perte de qualite, sans saturation). Chaque morceau demarre alors
    ''' directement au bon volume. Le lissage du son (NiveauSonore), lui, ne corrige qu'apres 2,5 s
    ''' d'ecoute : un morceau plus fort demarrait fort, puis baissait.
    '''
    ''' Avant : chemin non protege (un dossier avec une espace, "FAVO IPHONE", etait coupe en deux :
    ''' rien n'etait traite), sous-dossiers ignores, fenetre de l'interface figee, et l'attribut
    ''' lecture seule retire de TOUS les fichiers (photos et videos comprises).
    ''' </summary>
    ''' <summary>RAZ du lissage du son : oublie les niveaux mesures (tous, ou ceux des videos).
    ''' Chaque fichier est remesure a sa prochaine lecture, en quelques secondes.</summary>
    Private Sub ButtonRazLissage_Click(sender As Object, e As EventArgs) Handles ButtonRazLissage.Click
        Dim reponse As MsgBoxResult = MsgBox(
            "Remettre à zéro le lissage du son ?" & vbCrLf & vbCrLf &
            "Les niveaux mesurés sont oubliés : chaque fichier sera remesuré automatiquement à sa prochaine lecture (quelques secondes)." & vbCrLf & vbCrLf &
            "Oui : tout (musiques et vidéos)" & vbCrLf &
            "Non : seulement les vidéos" & vbCrLf &
            "Annuler : ne rien changer",
            vbYesNoCancel Or vbQuestion, "Lissage du son")
        If reponse = MsgBoxResult.Cancel Then Exit Sub
        Dim oublies As Integer = NiveauSonore.RemettreAZero(videosSeulement:=(reponse = MsgBoxResult.No))
        MsgBox(oublies & " niveau(x) oublié(s). Une sauvegarde est gardée dans data\settings\NiveauxSonores.sauvegarde.txt.",
               vbInformation, "Lissage du son")
    End Sub

    Private Sub Button31_Click(sender As Object, e As EventArgs) Handles Button31.Click
        ' Deja en cours (en tache de fond) : on rouvre simplement sa fenetre.
        If NormalisationMusique.EnCours Then
            NormalisationMusique.Montrer()
            Exit Sub
        End If

        Button5.PerformClick()                              ' recompte des sources

        ' Dossiers de musique coches ; leurs MP3 (sous-dossiers compris) sont cherches en tache de fond.
        Dim dossiers As New List(Of String)
        For i As Integer = 0 To ListView1.Items.Count - 1
            If Not ListView1.Items(i).Checked OrElse Val(ListView1.Items(i).SubItems(3).Text) <= 0 Then Continue For
            dossiers.Add(ListView1.Items(i).SubItems(0).Text)
        Next
        If dossiers.Count = 0 Then
            MsgBox("Aucun dossier de musique coché dans l'onglet Sources.", vbInformation)
            Exit Sub
        End If
        NormalisationMusique.Lancer(dossiers, EmpreinteDesSourcesMusique(), Me)
    End Sub

    Sub ShellAndWait(ByVal strProg As String, ByVal lStyle As System.Windows.Forms.FormWindowState)
        Dim ProcessId As Long
        Dim ProcessHandle As Long

        Const Access As Long = &H100005



        ProcessId = Shell("C:\Windows\System32\cmd.exe /c " & strProg, vbNormalFocus)
        Do
            ProcessHandle = OpenProcess(Access, False, ProcessId)
            If ProcessHandle <> 0 Then
                CloseHandle(ProcessHandle)
            End If
            Application.DoEvents()
            wait(1)
        Loop Until ProcessHandle = 0


    End Sub



    Declare Function OpenProcess Lib "kernel32" (ByVal dwDesiredAccess As Long, ByVal bInheritHandle As Long, ByVal dwProcessId As Long) As Long
    Declare Function CloseHandle Lib "kernel32" (ByVal hObject As Long) As Long
    Declare Function FindWindow Lib "user32" Alias "FindWindowA" (ByVal lpClassName As String, ByVal lpwindowname As String) As Long
    Declare Function SetForegroundWindow& Lib "user32" (ByVal hwnd As Long)

    ' ------------------------------------------------------------------------------------------
    ' Localite dans la legende (onglet Photos) : "Avec la localite" n'a de sens que si la date est
    ' affichee, et le choix hors ligne / en ligne que si la localite l'est.
    ' ------------------------------------------------------------------------------------------

    Private Sub ActiveChoixLocalite()
        CheckBoxAfficheLieu.Enabled = CheckBox8.Checked
        PanelSourceLieu.Enabled = CheckBox8.Checked AndAlso CheckBoxAfficheLieu.Checked
    End Sub

    Private Sub ChoixLocalite_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox8.CheckedChanged, CheckBoxAfficheLieu.CheckedChanged
        ActiveChoixLocalite()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Taille de la montre : glissiere avec apercu en direct
    '
    ' La montre change de taille pendant qu'on deplace le curseur, sans attendre OK. La valeur
    ' n'est enregistree qu'au clic sur OK (Button3_Click), comme les autres options : si on ferme
    ' Form4 autrement, la montre reprend la taille enregistree.
    ' ------------------------------------------------------------------------------------------

    Private _chargementTailleMontre As Boolean   ' pas d'apercu quand Form4_Load positionne le curseur

    ' Cadence : quand on fait glisser, les valeurs arrivent en rafale (toutes les ~30 ms). On
    ' applique au plus une mise a jour toutes les 40 ms, toujours avec la DERNIERE valeur : la
    ' montre suit le curseur pendant le glissement et le curseur reste fluide (un redimensionnement
    ' coute 5 a 35 ms). Ne pas relancer le minuteur a chaque valeur : il n'expirerait qu'a l'arret
    ' du glissement et l'apercu ne serait plus "en direct".
    Private WithEvents TimerApercuTailleMontre As New Timer With {.Interval = 40}

    ''' <summary>Opacite de la montre : apercu en direct (la montre est redessinee tout de suite), enregistre par OK.</summary>
    Private Sub TrackBarOpaciteMontre_ValueChanged(sender As Object, e As EventArgs) Handles TrackBarOpaciteMontre.ValueChanged
        LabelValeurOpaciteMontre.Text = TrackBarOpaciteMontre.Value & " %"
        If _chargementTailleMontre Then Exit Sub
        FormHorloge.DefinirOpacite(TrackBarOpaciteMontre.Value)
        If Not TimerApercuTailleMontre.Enabled Then TimerApercuTailleMontre.Start()
    End Sub

    Private Sub TrackBarTailleMontre_ValueChanged(sender As Object, e As EventArgs) Handles TrackBarTailleMontre.ValueChanged
        LabelValeurTailleMontre.Text = TrackBarTailleMontre.Value & " %"
        If _chargementTailleMontre Then Exit Sub
        If Not TimerApercuTailleMontre.Enabled Then TimerApercuTailleMontre.Start()
    End Sub

    Private Sub TimerApercuTailleMontre_Tick(sender As Object, e As EventArgs) Handles TimerApercuTailleMontre.Tick
        TimerApercuTailleMontre.Stop()
        ' Apercu seulement si la montre doit etre affichee (case "Afficher Gadget Clock" cochee).
        If CheckBox13.Checked Then
            FormHorloge.AfficherSur(Form3, premierPlan:=FullScreen, pourcent:=TrackBarTailleMontre.Value)
        End If
    End Sub

    Private Sub Form4_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        TimerApercuTailleMontre.Stop()
        ConnexionRecepteur.Rendre()
        CarteDeezer.FinDeLApercu()                    ' la pochette de Deezer revient aux reglages enregistres
        Form12.CederLePremierPlan("reglages", False)   ' le PIP revient au premier plan
        ' Pendant l'arret de l'application, ne pas toucher a Form3 : VB en recreerait une instance.
        If e.CloseReason = CloseReason.ApplicationExitCall OrElse
           e.CloseReason = CloseReason.WindowsShutDown OrElse
           e.CloseReason = CloseReason.TaskManagerClosing Then Exit Sub
        SilenceDesReglages.Rendre()                    ' les sons arretes a l'ouverture reprennent
        ' Remet la montre dans l'etat enregistre : taille validee par OK, ou taille d'avant si
        ' Form4 a ete fermee sans valider apres un apercu.
        AfficheMontre()
        ' Idem pour la legende (taille et coin).
        FormLegende.DefinirPourcent(LegendeTaillePourcent)
        FormLegende.DefinirCoin(LegendeCoin)
        FormLegende.DefinirCentreeSousPip(LegendeCentreeSousPip)
        ReappliquerBandes(My.Settings.BandesActives)
        FormLieuBande.DefinirPourcent(BandesTaillePourcent)
        FormLieuBande.DefinirPositionVerticale(BandesPosition)
        FormLieuBande.DefinirPolice(BandesPolice)
        FormLieuBande.DefinirRelief(BandesRelief)
        FormHorloge.DefinirInclinaison(My.Settings.MontreInclinee)
        FormHorloge.DefinirAngle(My.Settings.MontreAngle)
        FormHorloge.DefinirOpacite(My.Settings.MontreOpacite)
        FormHorloge.DefinirRelief(BandesRelief)
    End Sub

    ''' <summary>Reglages ouverts : le PIP ne passe plus devant eux (voir Form12.CederLePremierPlan) ;
    ''' il revient au premier plan a la fermeture.</summary>
    Private Sub Form4_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        Form12.CederLePremierPlan("reglages", Me.Visible, Me)
        ' Tous les sons qui jouent a cet instant s'arretent ; rendus a la fermeture (Form4_FormClosed).
        If Me.Visible Then SilenceDesReglages.Couper()
    End Sub

    ''' <summary>Partage du recepteur satellite : connexion etablie en tache de fond et gardee
    ''' pendant les reglages, pour que l'apercu de ses films s'ouvre sans attendre (voir
    ''' ConnexionRecepteur). Dans Shown : la minuterie est alors chargee dans la grille.</summary>
    ''' <summary>Fetes juives (YomTov) : si une fete arrive (30 jours) et n'est pas dans la liste, Slide propose d'inserer son entree et sa sortie.</summary>
    Private Sub Form4_Shown_YomTov(sender As Object, e As EventArgs) Handles Me.Shown
        BeginInvoke(Sub() YomTov.ProposerAuxReglages(Me))
    End Sub

    Private Sub ButtonYomTov_Click(sender As Object, e As EventArgs) Handles ButtonYomTov.Click
        FormYomTov.Choisir(Me, Nothing, -1)
    End Sub

    ''' <summary>Erreur d'affichage d'une case des tableaux de la minuterie (21/10/2026 : "La valeur mise en forme de la cellule est d'un type incorrect") :
    ''' la fenetre par defaut ne dit pas laquelle ; elle est remplacee par une ligne du journal (ligne, colonne, valeur, erreur) pour la trouver.</summary>
    ''' <summary>Album : le nombre de photos affichees a la fois ne se regle que si le format album est coche.</summary>
    ''' <summary>Le nombre de photos a la fois (champ et son libelle) est grise quand le nombre est aleatoire, ou quand le format album n'est pas coche (22/10/2026 : "si nombre aleatoire coche, grise le nombre de photos a la fois").</summary>
    Private Sub MettreAJourLeNombreAlbum()
        Dim actif As Boolean = CheckBox29.Checked AndAlso Not CheckBoxAlbumAleatoire.Checked
        NumericAlbumNombre.Enabled = actif
        LabelAlbumNombre.Enabled = actif
    End Sub

    Private Sub CheckBoxAlbumAleatoire_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxAlbumAleatoire.CheckedChanged
        MettreAJourLeNombreAlbum()
    End Sub

    Private Sub CheckBox29_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox29.CheckedChanged
        MettreAJourLeNombreAlbum()

        CheckBoxAlbumAleatoire.Enabled = CheckBox29.Checked
    End Sub

    Private Sub GrilleErreur(sender As Object, e As DataGridViewDataErrorEventArgs) Handles DataGridView1.DataError, DataGridView2.DataError
        Try
            Dim grille As DataGridView = DirectCast(sender, DataGridView)
            Dim colonne As String = If(e.ColumnIndex >= 0 AndAlso e.ColumnIndex < grille.Columns.Count, grille.Columns(e.ColumnIndex).Name, e.ColumnIndex.ToString())
            Dim valeur As String = ""
            Try
                valeur = Convert.ToString(grille.Rows(e.RowIndex).Cells(e.ColumnIndex).Value) & " (" & If(grille.Rows(e.RowIndex).Cells(e.ColumnIndex).Value Is Nothing, "Nothing", grille.Rows(e.RowIndex).Cells(e.ColumnIndex).Value.GetType().Name) & ")"
            Catch ex As Exception
            End Try
            DiagLog("Minuterie : erreur de case (" & grille.Name & ") ligne " & (e.RowIndex + 1) & ", colonne " & colonne & ", valeur " & valeur & ", contexte " & e.Context.ToString() &
                    " : " & If(e.Exception Is Nothing, "", e.Exception.GetType().Name & " " & e.Exception.Message))
        Catch ex As Exception
        End Try
        e.ThrowException = False
        e.Cancel = False
    End Sub

    Private Sub Form4_Shown_Recepteur(sender As Object, e As EventArgs) Handles Me.Shown
        ConnexionRecepteur.Garder()
    End Sub

    ''' <summary>Taille du lieu et de la date dans les bandes noires : apercu en direct, enregistree par OK.</summary>
    Private Sub TrackBarTailleBandes_ValueChanged(sender As Object, e As EventArgs) Handles TrackBarTailleBandes.ValueChanged
        LabelValeurTailleBandes.Text = TrackBarTailleBandes.Value & " %"
        If _chargementTailleMontre Then Exit Sub
        FormLieuBande.DefinirPourcent(TrackBarTailleBandes.Value)
    End Sub

    ''' <summary>Codes des coins de l'etiquette lieu · date, dans l'ordre de ComboBoxCoinLegende.</summary>
    Private Shared ReadOnly CodesCoinLegende As String() = {"Bgauche", "Bdroite", "Hgauche", "Hdroite"}

    ''' <summary>Codes de la place du lieu et de la date dans les bandes noires, dans l'ordre de
    ''' ComboBoxPositionBandes.</summary>
    Private Shared ReadOnly CodesPositionBandes As String() = {"Haut", "Milieu", "Bas"}

    Private Function PositionBandesDeLaListe() As String
        Dim i As Integer = ComboBoxPositionBandes.SelectedIndex
        Return If(i >= 0 AndAlso i < CodesPositionBandes.Length, CodesPositionBandes(i), "Milieu")
    End Function

    ''' <summary>Localite centree sous le PIP (ou alignee sur son bord) : apercu en direct, enregistree par OK.</summary>
    Private Sub CheckBoxLegendeSousPip_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxLegendeSousPip.CheckedChanged
        If _chargementTailleMontre Then Exit Sub
        FormLegende.DefinirCentreeSousPip(CheckBoxLegendeSousPip.Checked)
    End Sub

    ''' <summary>Utiliser ou non les bandes noires pour le lieu et la date : apercu en direct sur la
    ''' photo affichee, enregistre par OK (annule en fermant sans valider).</summary>
    Private Sub CheckBoxBandesActives_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxBandesActives.CheckedChanged
        If _chargementTailleMontre Then Exit Sub
        ReappliquerBandes(CheckBoxBandesActives.Checked)
    End Sub

    ''' <summary>Refait la legende de la photo en cours (son texte complet reste dans
    ''' Form3.TextBox1) : le lieu et la date passent dans les bandes, ou reviennent en bas.</summary>
    Private Sub ReappliquerBandes(actives As Boolean)
        If actives = BandesActives Then Exit Sub
        BandesActives = actives
        Try
            If Form3.TextBox1.Text <> "" Then AfficheLegende(Form3.TextBox1.Text)
        Catch ex As Exception
            DiagLog("Form4 : bandes non reappliquees (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Place du lieu et de la date dans les bandes noires : apercu en direct, enregistree par OK.</summary>
    ''' <summary>Police du lieu et de la date dans les bandes : apercu en direct, enregistree par OK (annulee en fermant).</summary>
    Private Sub ComboBoxPoliceBandes_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxPoliceBandes.SelectedIndexChanged
        If _chargementTailleMontre Then Exit Sub
        If ComboBoxPoliceBandes.SelectedItem IsNot Nothing Then FormLieuBande.DefinirPolice(CStr(ComboBoxPoliceBandes.SelectedItem))
    End Sub

    ''' <summary>Angle d'inclinaison des chiffres (onglet Photos, 13 degres par defaut) : apercu en direct, enregistre par OK.</summary>
    Private Sub NumericAngleMontre_ValueChanged(sender As Object, e As EventArgs) Handles NumericAngleMontre.ValueChanged
        If _chargementTailleMontre Then Exit Sub
        FormHorloge.DefinirAngle(CInt(NumericAngleMontre.Value))
    End Sub

    ''' <summary>Chiffres de la montre inclines (angle choisi a cote, 13 degres par defaut) : apercu en direct, enregistre par OK.</summary>
    Private Sub CheckBoxInclinerMontre_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxInclinerMontre.CheckedChanged
        NumericAngleMontre.Enabled = CheckBoxInclinerMontre.Checked
        FormHorloge.DefinirInclinaison(CheckBoxInclinerMontre.Checked)
    End Sub

    ''' <summary>Relief argente du lieu et de la date : apercu en direct, enregistre par OK.</summary>
    Private Sub CheckBoxReliefBandes_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxReliefBandes.CheckedChanged
        If _chargementTailleMontre Then Exit Sub
        FormLieuBande.DefinirRelief(CheckBoxReliefBandes.Checked)
    End Sub

    Private Sub ComboBoxPositionBandes_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxPositionBandes.SelectedIndexChanged
        If _chargementTailleMontre Then Exit Sub
        FormLieuBande.DefinirPositionVerticale(PositionBandesDeLaListe())
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Pochette et titre de Deezer sur l'image (onglet Audio, cadre Deezer) : apercu en direct,
    ' enregistre par OK. Memes coins que l'etiquette lieu · date.
    ' ------------------------------------------------------------------------------------------

    Private _chargementCarteDeezer As Boolean = True     ' pas d'apercu pendant que la fenetre se remplit

    Private Function CoinDeLaCarteDeezer() As String
        Dim i As Integer = ComboBoxDeezerCarteCoin.SelectedIndex
        Return If(i >= 0 AndAlso i < CodesCoinLegende.Length, CodesCoinLegende(i), "Hgauche")
    End Function

    ''' <summary>0 : la carte reste affichee ; sinon le nombre de secondes ou elle se montre a chaque morceau.</summary>
    Private Function SecondesDeLaCarteDeezer() As Integer
        Return If(RadioDeezerCarteDuree.Checked, CInt(NumericDeezerCarteSecondes.Value), 0)
    End Function

    Private Sub CarteDeezer_Changee(sender As Object, e As EventArgs) Handles CheckBoxDeezerCarte.CheckedChanged,
            ComboBoxDeezerCarteCoin.SelectedIndexChanged, TrackBarDeezerCarte.ValueChanged, CheckBoxDeezerIconeSource.CheckedChanged,
            RadioDeezerCarteToujours.CheckedChanged, RadioDeezerCarteDuree.CheckedChanged, NumericDeezerCarteSecondes.ValueChanged
        LabelValeurDeezerCarte.Text = TrackBarDeezerCarte.Value & " %"
        NumericDeezerCarteSecondes.Enabled = RadioDeezerCarteDuree.Checked
        If _chargementCarteDeezer Then Exit Sub
        CarteDeezer.Apercu(CheckBoxDeezerCarte.Checked, CoinDeLaCarteDeezer(), TrackBarDeezerCarte.Value, SecondesDeLaCarteDeezer(), CheckBoxDeezerIconeSource.Checked)
    End Sub

    Private Function CoinDeLaListe() As String
        Dim i As Integer = ComboBoxCoinLegende.SelectedIndex
        Return If(i >= 0 AndAlso i < CodesCoinLegende.Length, CodesCoinLegende(i), "Bgauche")
    End Function

    ''' <summary>Coin de l'etiquette lieu · date : apercu en direct, enregistre par OK.</summary>
    Private Sub ComboBoxCoinLegende_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxCoinLegende.SelectedIndexChanged
        If _chargementTailleMontre Then Exit Sub
        FormLegende.DefinirCoin(CoinDeLaListe())
    End Sub

    ''' <summary>Taille de la legende du bas (lieu [date]) : apercu en direct, enregistree par OK.</summary>
    Private Sub TrackBarTailleLegende_ValueChanged(sender As Object, e As EventArgs) Handles TrackBarTailleLegende.ValueChanged
        LabelValeurTailleLegende.Text = TrackBarTailleLegende.Value & " %"
        If _chargementTailleMontre Then Exit Sub
        FormLegende.DefinirPourcent(TrackBarTailleLegende.Value)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Minuterie : copier les lignes selectionnees vers un autre jour (Button32)
    '
    ' Clic : liste des jours sous le bouton ; chaque ligne selectionnee (ou, a defaut, celle de la
    ' cellule courante) est dupliquee a l'identique (heure, type, action, play, duree, volume,
    ' argument, infobulles) avec le jour choisi, puis l'ordre de tri est recalcule.
    ' ------------------------------------------------------------------------------------------

    Private _menuJours As ContextMenuStrip
    Private _lignesACopier As New List(Of DataGridViewRow)

    Private Sub Button32_HandleCreated(sender As Object, e As EventArgs) Handles Button32.HandleCreated
        If Button32.BackgroundImage Is Nothing Then Button32.BackgroundImage = DessinerIconeCopierJour(96)
    End Sub

    ''' <summary>Lignes a copier : lignes selectionnees, sinon lignes des cellules selectionnees,
    ''' sinon ligne de la cellule courante ; dans l'ordre de la grille.</summary>
    Private Function LignesACopier() As List(Of DataGridViewRow)
        Dim lignes As New List(Of DataGridViewRow)
        For Each r As DataGridViewRow In DataGridView1.SelectedRows
            If Not r.IsNewRow Then lignes.Add(r)
        Next
        If lignes.Count = 0 Then
            For Each c As DataGridViewCell In DataGridView1.SelectedCells
                Dim r As DataGridViewRow = DataGridView1.Rows(c.RowIndex)
                If Not r.IsNewRow AndAlso Not lignes.Contains(r) Then lignes.Add(r)
            Next
        End If
        If lignes.Count = 0 AndAlso DataGridView1.CurrentCell IsNot Nothing Then
            Dim r As DataGridViewRow = DataGridView1.Rows(DataGridView1.CurrentCell.RowIndex)
            If Not r.IsNewRow Then lignes.Add(r)
        End If
        lignes.Sort(Function(a, b) a.Index.CompareTo(b.Index))
        Return lignes
    End Function

    Private Sub Button32_Click(sender As Object, e As EventArgs) Handles Button32.Click
        DataGridView1.EndEdit()
        _lignesACopier = LignesACopier()
        If _lignesACopier.Count = 0 Then
            Beep()
            Exit Sub
        End If

        If _menuJours Is Nothing Then
            _menuJours = New ContextMenuStrip() With {.ShowImageMargin = False, .Font = New Font("Segoe UI", 10.0F)}
            _menuJours.Items.Add(New ToolStripLabel("") With {.ForeColor = Color.DimGray, .Font = New Font("Segoe UI", 9.0F, FontStyle.Italic)})
            _menuJours.Items.Add(New ToolStripSeparator())
            For Each jour As String In jourDeLaSemaine
                Dim choix As ToolStripItem = _menuJours.Items.Add(jour)
                AddHandler choix.Click, Sub(s, a) CopierVersLeJour(CType(s, ToolStripItem).Text)
            Next
        End If
        _menuJours.Items(0).Text = "Copier " & _lignesACopier.Count & If(_lignesACopier.Count > 1, " lignes", " ligne") & " vers :"
        _menuJours.Show(Button32, New Point(0, Button32.Height))
    End Sub

    Private Sub CopierVersLeJour(jour As String)
        Dim colJour As Integer = DataGridView1.Columns("Jour").Index
        Dim colHeure As Integer = DataGridView1.Columns("Heure").Index
        Dim colTri As Integer = DataGridView1.Columns("tri").Index
        Dim premiere As Integer = -1

        For Each source As DataGridViewRow In _lignesACopier
            If source.DataGridView Is Nothing Then Continue For             ' ligne supprimee entre-temps
            Dim valeurs(DataGridView1.ColumnCount - 1) As Object
            For c As Integer = 0 To DataGridView1.ColumnCount - 1
                valeurs(c) = source.Cells(c).Value
            Next
            valeurs(colJour) = jour
            If jour = "BOOT" Then valeurs(colHeure) = "00:00"
            valeurs(colTri) = "999999"                                       ' recalcule plus bas

            Dim i As Integer = DataGridView1.Rows.Add(valeurs)
            Dim copie As DataGridViewRow = DataGridView1.Rows(i)
            For c As Integer = 0 To DataGridView1.ColumnCount - 1
                copie.Cells(c).ToolTipText = source.Cells(c).ToolTipText
            Next
            ' Ligne TV : sa liste de chaines, sur la meme chaine (voir ListeDesChainesTV).
            ListeDesActionsDuType(i, CStr(If(copie.Cells("Type").Value, "")), CStr(If(source.Cells("Action").Value, "")))
            If premiere = -1 Then premiere = i
        Next

        If premiere >= 0 Then AjouterUneLigne(2, premiere)                  ' tri recalcule + grille retriee
    End Sub

    ''' <summary>Icone du bouton : deux calendriers superposes (copie) et un badge bleu a fleche.</summary>
    Private Shared Function DessinerIconeCopierJour(t As Integer) As Bitmap
        Dim b As New Bitmap(t, t)
        Using g As Graphics = Graphics.FromImage(b)
            g.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            g.Clear(Color.Transparent)
            Dim u As Single = t / 32.0F

            ' Calendrier de derriere (la copie), decale en haut a droite
            DessinerCalendrier(g, New RectangleF(11 * u, 3 * u, 18 * u, 17 * u), u, Color.FromArgb(150, 150, 155), Color.FromArgb(235, 235, 238))
            ' Calendrier de devant
            DessinerCalendrier(g, New RectangleF(3 * u, 9 * u, 19 * u, 19 * u), u, Color.FromArgb(214, 48, 49), Color.White)

            ' Badge bleu avec fleche vers la droite
            Dim badge As New RectangleF(18 * u, 17 * u, 13 * u, 13 * u)
            Using bleu As New SolidBrush(Color.FromArgb(10, 110, 230))
                g.FillEllipse(bleu, badge)
            End Using
            Using blanc As New Pen(Color.White, 2.0F * u) With {.StartCap = Drawing2D.LineCap.Round, .EndCap = Drawing2D.LineCap.Round, .LineJoin = Drawing2D.LineJoin.Round}
                Dim cy As Single = badge.Top + badge.Height / 2
                g.DrawLine(blanc, badge.Left + 3.2F * u, cy, badge.Right - 3.4F * u, cy)
                g.DrawLines(blanc, {New PointF(badge.Right - 6.2F * u, cy - 2.8F * u), New PointF(badge.Right - 3.4F * u, cy), New PointF(badge.Right - 6.2F * u, cy + 2.8F * u)})
            End Using
        End Using
        Return b
    End Function

    Private Shared Sub DessinerCalendrier(g As Graphics, r As RectangleF, u As Single, entete As Color, fond As Color)
        Dim rayon As Single = 2.5F * u
        Using chemin As New Drawing2D.GraphicsPath()
            chemin.AddArc(r.Left, r.Top, 2 * rayon, 2 * rayon, 180, 90)
            chemin.AddArc(r.Right - 2 * rayon, r.Top, 2 * rayon, 2 * rayon, 270, 90)
            chemin.AddArc(r.Right - 2 * rayon, r.Bottom - 2 * rayon, 2 * rayon, 2 * rayon, 0, 90)
            chemin.AddArc(r.Left, r.Bottom - 2 * rayon, 2 * rayon, 2 * rayon, 90, 90)
            chemin.CloseFigure()
            Using p As New SolidBrush(fond)
                g.FillPath(p, chemin)
            End Using
            ' Bandeau du haut
            Dim etat As Drawing2D.GraphicsState = g.Save()
            g.SetClip(chemin)
            Using p As New SolidBrush(entete)
                g.FillRectangle(p, r.Left, r.Top, r.Width, r.Height * 0.3F)
            End Using
            g.Restore(etat)
            Using stylo As New Pen(Color.FromArgb(90, 90, 95), 1.2F * u)
                g.DrawPath(stylo, chemin)
            End Using
        End Using
        ' Anneaux
        Using stylo As New Pen(Color.FromArgb(70, 70, 75), 1.6F * u) With {.StartCap = Drawing2D.LineCap.Round, .EndCap = Drawing2D.LineCap.Round}
            g.DrawLine(stylo, r.Left + r.Width * 0.3F, r.Top - 1.5F * u, r.Left + r.Width * 0.3F, r.Top + 2.5F * u)
            g.DrawLine(stylo, r.Left + r.Width * 0.7F, r.Top - 1.5F * u, r.Left + r.Width * 0.7F, r.Top + 2.5F * u)
        End Using
        ' Cases des jours (3 x 2)
        Using p As New SolidBrush(Color.FromArgb(120, 120, 128))
            Dim cw As Single = r.Width / 5.0F, ch As Single = r.Height * 0.16F
            For ligne As Integer = 0 To 1
                For col As Integer = 0 To 2
                    g.FillRectangle(p, r.Left + cw * (0.7F + col * 1.3F), r.Top + r.Height * (0.42F + ligne * 0.26F), cw * 0.8F, ch)
                Next
            Next
        End Using
    End Sub


    ''' <summary>Verification quotidienne decochee : l'heure n'a plus d'objet.</summary>
    Private Sub CheckBoxMajHeure_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxMajHeure.CheckedChanged
        DateTimePickerMajHeure.Enabled = CheckBoxMajHeure.Checked
    End Sub

    ''' <summary>Relance de nuit decochee : l'heure n'a plus d'objet (voir RelanceDeNuit).</summary>
    Private Sub CheckBoxRelanceNuit_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxRelanceNuit.CheckedChanged
        ComboBoxRelanceNuit.Enabled = CheckBoxRelanceNuit.Checked
        ComboBoxRelanceFrequence.Enabled = CheckBoxRelanceNuit.Checked
    End Sub

    ''' <summary>Photos animees decochees : les choix d'arrivee et de mouvement n'ont plus d'objet.</summary>
    Private Sub CheckBoxAnimerPhotos_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxAnimerPhotos.CheckedChanged
        ComboBoxAnimationArrivee.Enabled = CheckBoxAnimerPhotos.Checked
        ComboBoxAnimationMouvement.Enabled = CheckBoxAnimerPhotos.Checked
        ComboBoxAnimationPhotoForce.Enabled = CheckBoxAnimerPhotos.Checked
        ComboBoxAnimationPhotoCouleurs.Enabled = CheckBoxAnimerPhotos.Checked
        ButtonHasardPhotos.Enabled = CheckBoxAnimerPhotos.Checked
    End Sub

    ''' <summary>Videos animees decochees : le choix de la transition n'a plus d'objet.</summary>
    Private Sub CheckBoxAnimerVideos_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxAnimerVideos.CheckedChanged
        ComboBoxAnimationVideo.Enabled = CheckBoxAnimerVideos.Checked
        ComboBoxAnimationVideoMouvement.Enabled = CheckBoxAnimerVideos.Checked
        ComboBoxAnimationVideoForce.Enabled = CheckBoxAnimerVideos.Checked
        ComboBoxAnimationVideoCouleurs.Enabled = CheckBoxAnimerVideos.Checked
        ComboBoxAnimationVideoBandes.Enabled = CheckBoxAnimerVideos.Checked
        ButtonHasardVideos.Enabled = CheckBoxAnimerVideos.Checked
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Animations tirees au hasard (demande de Jacob, 07/10) : quand une liste est sur "Au hasard",
    ' le tirage se fait parmi les animations cochees dans la fenetre FormChoixHasard. Ce qui y
    ' est coche est retenu ici, et enregistre avec le reste par le bouton OK des reglages.
    ' Un bit par animation, au numero de sa ligne dans la liste ; bit 0 : aucune (TirageAuHasard).
    ' ------------------------------------------------------------------------------------------
    Private _hasardPhotoArrivee As Integer = HasardPhotoArrivee
    Private _hasardPhotoMouvement As Integer = HasardPhotoMouvement
    Private _hasardVideoTransition As Integer = HasardVideoTransition
    Private _hasardVideoMouvement As Integer = HasardVideoMouvement
    Private _hasardPhotoCouleurs As Integer = HasardPhotoCouleurs
    Private _hasardVideoCouleurs As Integer = HasardVideoCouleurs
    ''' <summary>Ajoute la ligne « Noir et blanc » au tableau des mouvements du hasard (15/10/2026 : "rajoute seulement noir et
    ''' blanc dans les tableaux mouvements") : cochee, une photo (ou video) sur n est montree en noir et blanc, avec un des
    ''' autres mouvements coches.</summary>
    Private Shared Sub AjouterNoirEtBlanc(ByRef noms As String(), ByRef numeros As Integer())
        noms = noms.Concat({"Noir et blanc"}).ToArray()
        numeros = numeros.Concat({TirageAuHasard.NumeroNoirEtBlanc}).ToArray()
    End Sub

    ''' <summary>Les animations d'une liste des reglages, sans sa premiere ligne "Au hasard" : leur
    ''' nom et leur numero (leur rang dans la liste). <paramref name="aucune"/> : ajoute cette
    ''' ligne, au numero 0, pour "pas d'animation".</summary>
    Private Shared Sub AnimationsDeLaListe(liste As ComboBox, aucune As String, ByRef noms As String(), ByRef numeros As Integer(),
                                           Optional sansLaDerniere As Boolean = False)
        Dim n As New List(Of String), r As New List(Of Integer)
        ' sansLaDerniere : le "Aucune" de la liste des reglages (ajoute en fin de liste le 13/10) ne figure pas parmi
        ' les animations du hasard : "Aucune animation" y a deja sa ligne (numero 0).
        For i As Integer = 1 To liste.Items.Count - 1 - If(sansLaDerniere, 1, 0)
            n.Add(liste.Items(i).ToString())
            r.Add(i)
        Next
        If aucune <> "" Then
            n.Add(aucune)
            r.Add(0)
        End If
        noms = n.ToArray()
        numeros = r.ToArray()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Minuterie : Deezer a une heure precise, avec son volume et sa playlist (demande de Jacob,
    ' 07/10). Type "Deezer", action "Lance Deezer" (avant le 08/10 : "Music Source / Deezer") : la
    ' colonne Volume est le volume de Deezer, et
    ' la case Play (un clic) la playlist de la tache, gardee dans la colonne cachee Argument
    ' sous la forme "!/Playlist:adresse". Voir Module1.DeezerParLaMinuterie.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Adresse de la playlist Deezer d'une ligne de la minuterie ("" : aucune).</summary>
    Friend Function PlaylistDeLaLigne(r As Integer) As String
        Return CStr(Arg("Playlist", CStr(If(DataGridView1.Rows(r).Cells("Argument").Value, "")))).Trim()
    End Function

    ''' <summary>Vrai pour la tache qui lance Deezer avec une playlist : type "Deezer", action
    ''' "Lance Deezer" (ou, pour une ligne d'avant le 08/10, "Music Source / Deezer").</summary>
    Friend Function LigneDeezerAvecPlaylist(r As Integer) As Boolean
        Dim type As String = UCase(CStr(If(DataGridView1.Rows(r).Cells("Type").Value, "")))
        Dim action As String = UCase(CStr(If(DataGridView1.Rows(r).Cells("Action").Value, "")))
        Return (type = "DEEZER" AndAlso action = "LANCE DEEZER") OrElse (type = "MUSIC SOURCE" AndAlso action = "DEEZER")
    End Function

    ''' <summary>Case Play d'une ligne Deezer : pour "Lance Deezer", sa playlist.</summary>
    Friend Sub AfficherLaPlaylistDeLaLigne(r As Integer)
        Try
            Dim ligne As DataGridViewRow = DataGridView1.Rows(r)
            Dim typeDeLaLigne As String = UCase(CStr(If(ligne.Cells("Type").Value, "")))
            If typeDeLaLigne <> "MUSIC SOURCE" AndAlso typeDeLaLigne <> "DEEZER" Then Exit Sub
            Dim casePlay As DataGridViewCell = ligne.Cells("Play")
            If LigneDeezerAvecPlaylist(r) Then
                Dim playlist As String = PlaylistDeLaLigne(r)
                ' "Playlist..." (aucune), "Playlist Flow", "Playlist Flow genre-variete", "Playlist 53362031".
                Dim texte As String = "Playlist..."
                If playlist <> "" Then
                    Dim adresse As String = MusiqueDeezer.AdresseDeezer(playlist)
                    If adresse = MusiqueDeezer.Favoris Then
                        texte = "Playlist Favoris"
                    ElseIf MusiqueDeezer.EstUnFlow(adresse) Then
                        texte = ("Playlist Flow " & MusiqueDeezer.VarianteDuFlow(adresse)).Trim()
                    Else
                        Dim numero As String = System.Text.RegularExpressions.Regex.Match(playlist, "\d+$").Value
                        texte = If(numero = "", "Playlist choisie", "Playlist " & numero)
                    End If
                End If
                If CStr(If(casePlay.Value, "")) <> texte Then casePlay.Value = texte
                casePlay.ToolTipText = If(playlist = "", "Cliquer pour choisir la playlist Deezer de cette tâche." & vbCrLf & "Sans playlist : celle des réglages (onglet Audio), sinon la file d'attente de Deezer.",
                                          playlist & vbCrLf & "Cliquer pour la changer.")
            ElseIf CStr(If(casePlay.Value, "")).StartsWith("Playlist") Then
                casePlay.Value = ""
                casePlay.ToolTipText = ""
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Demande l'adresse de la playlist Deezer de la tache (clic sur sa case Play).</summary>
    Private Sub ChoisirLaPlaylistDeLaLigne(r As Integer)
        Dim actuelle As String = PlaylistDeLaLigne(r)
        Dim saisie As String = InputBox("Adresse Deezer de la playlist (ou de l'album) que cette tâche doit lancer." & vbCrLf & vbCrLf &
                                        "Dans Deezer : « Partager », puis « Copier le lien »." & vbCrLf &
                                        "Pour le Flow, écrire simplement : flow ; pour vos Coups de cœur : favoris" & vbCrLf &
                                        "Vide : la playlist des réglages (onglet Audio).", "Playlist Deezer de la tâche", actuelle).Trim()
        If saisie = actuelle Then Exit Sub
        If saisie = "" Then
            ' (Une saisie vide est aussi ce que renvoie "Annuler" : on demande avant de retirer.)
            If MsgBox("Retirer la playlist de cette tâche ?", MsgBoxStyle.YesNo Or MsgBoxStyle.Question, "Playlist Deezer de la tâche") <> MsgBoxResult.Yes Then Exit Sub
            DataGridView1.Rows(r).Cells("Argument").Value = ""
        Else
            Dim adresse As String = MusiqueDeezer.AdresseDeezer(saisie)
            If adresse = "" Then
                MsgBox("Ce n'est pas une adresse de Deezer :" & vbCrLf & saisie, MsgBoxStyle.Exclamation, "Playlist Deezer de la tâche")
                Exit Sub
            End If
            DataGridView1.Rows(r).Cells("Argument").Value = "!/Playlist:" & adresse
        End If
        AfficherLaPlaylistDeLaLigne(r)
    End Sub

    ''' <summary>Montre la fenetre du Deezer integre a Slide : pour se connecter au compte (une
    ''' fois) ou choisir la musique. Voir MusiqueDeezer.</summary>
    ''' <summary>
    ''' Identifiant et mot de passe de Deezer saisis dans l'onglet Audio : enregistres s'ils ont
    ''' change (bouton OK, et bouton "Ouvrir Deezer..." pour ne pas attendre OK). Si c'est
    ''' l'IDENTIFIANT qui a change, le compte en place dans le lecteur est deconnecte et Slide se
    ''' connecte avec le nouveau (voir MusiqueDeezer.ChangerDeCompte) ; pour un simple changement de
    ''' mot de passe, la connexion automatique pourra etre retentee.
    ''' </summary>
    Private Sub EnregistrerLeCompteDeezer()
        Dim identifiantDeezer As String = TextBoxDeezerLogin.Text.Trim()
        Dim motDePasseDeezer As String = TextBoxDeezerMotDePasse.Text
        Dim autreCompte As Boolean = Not String.Equals(identifiantDeezer, DeezerLogin, StringComparison.OrdinalIgnoreCase)
        If Not autreCompte AndAlso identifiantDeezer = DeezerLogin AndAlso motDePasseDeezer = MusiqueDeezer.Deproteger(DeezerMotDePasse) Then Exit Sub
        DeezerLogin = identifiantDeezer : My.Settings.DeezerLogin = DeezerLogin
        DeezerMotDePasse = MusiqueDeezer.Proteger(motDePasseDeezer) : My.Settings.DeezerMotDePasse = DeezerMotDePasse
        My.Settings.Save()
        If autreCompte Then MusiqueDeezer.ChangerDeCompte() Else MusiqueDeezer.IdentifiantsChanges()
    End Sub

    Private Sub ButtonDeezerOuvrir_Click(sender As Object, e As EventArgs) Handles ButtonDeezerOuvrir.Click
        ' Un autre identifiant vient d'etre saisi : il est pris tout de suite, sans attendre OK.
        EnregistrerLeCompteDeezer()
        MusiqueDeezer.Montrer()
    End Sub

    Private Sub ButtonHasardPhotos_Click(sender As Object, e As EventArgs) Handles ButtonHasardPhotos.Click
        Dim noms1 As String() = Nothing, numeros1 As Integer() = Nothing, noms2 As String() = Nothing, numeros2 As Integer() = Nothing
        AnimationsDeLaListe(ComboBoxAnimationArrivee, "Aucune animation", noms1, numeros1, True)
        AnimationsDeLaListe(ComboBoxAnimationMouvement, "", noms2, numeros2)
        AjouterNoirEtBlanc(noms2, numeros2)
        ' « Album photo » (21/10/2026) : cochee, un ecran sur n (n = lignes cochees) montre plusieurs photos en album (nombre : reglage « Album : photos affichees a la fois »).
        noms2 = noms2.Concat({"Album photo"}).ToArray()
        numeros2 = numeros2.Concat({TirageAuHasard.NumeroAlbum}).ToArray()
        FormChoixHasard.Choisir(Me, "Photos : animations du hasard", "Arrivées :", noms1, numeros1, _hasardPhotoArrivee,
                                "Mouvements :", noms2, numeros2, _hasardPhotoMouvement)
    End Sub

    Private Sub ButtonHasardVideos_Click(sender As Object, e As EventArgs) Handles ButtonHasardVideos.Click
        Dim noms1 As String() = Nothing, numeros1 As Integer() = Nothing, noms2 As String() = Nothing, numeros2 As Integer() = Nothing
        AnimationsDeLaListe(ComboBoxAnimationVideo, "Aucune animation", noms1, numeros1, True)
        AnimationsDeLaListe(ComboBoxAnimationVideoMouvement, "", noms2, numeros2)
        AjouterNoirEtBlanc(noms2, numeros2)
        FormChoixHasard.Choisir(Me, "Vidéos : animations du hasard", "Transitions :", noms1, numeros1, _hasardVideoTransition,
                                "Mouvements :", noms2, numeros2, _hasardVideoMouvement)
    End Sub
    ''' <summary>Couleur du bouton de source CHOISI dans la minuterie (Divx, Vu+, Free, Nflix) : bleu avec
    ''' l'apparence professionnelle, noir sinon (comme avant). Le code s'en sert aussi pour savoir lequel
    ''' est choisi.</summary>
    Private ReadOnly Property CouleurDuChoix As Color
        Get
            Return If(StylePro.Actif, StylePro.Accent, Color.Black)
        End Get
    End Property

    Private ReadOnly Property CouleurHorsChoix As Color
        Get
            Return If(StylePro.Actif, StylePro.BoutonNeutre, Color.Gray)
        End Get
    End Property

    ''' <summary>Onglet Sources : la colonne des chemins (fichiers et dossiers) prend toute la place que laissent les trois colonnes de comptes (21/10/2026).</summary>
    Private Sub AjusterLaColonneSource(sender As Object, e As EventArgs) Handles ListView1.SizeChanged, ListView1.VisibleChanged
        Try
            Dim reste As Integer = ListView1.ClientSize.Width - NBphotos.Width - NBvideos.Width - NBmusic.Width
            If reste > 100 Then Source.Width = reste
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form4_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        MoletteSousLaSouris.Suivre(Me)          ' la molette agit sur la liste sous le pointeur, sans cliquer d'abord (listes Tuya...)
    End Sub

End Class


