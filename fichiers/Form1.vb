Imports System.Drawing.Drawing2D
Imports System.IO
Public Class Form1

    ''' <summary>Tours de la boucle d'attente de fin de video (suivi de la radio une fois par seconde).</summary>
    Private toursVideo As Integer

    ''' <summary>Faux jusqu'au premier tour de photos : a ce tour-la, la musique ne demarre
    ''' qu'APRES l'affichage de la premiere photo (voir "debut:").</summary>
    Private _musiqueLanceeAuDemarrage As Boolean

    ''' <summary>Musique du diaporama pendant les photos : lecture, puis volume du reglage.</summary>
    Private Sub LancerLaMusiqueDuDiaporama()
        If Music = True Then
            If PIPvolume = 0 And (Music_Deezer = False OrElse MusiqueDeezer.Actif) Then MusiqueVLC.MusiqueJouer()
        End If
        SetMusicVolSlide(VolumeMusicSlide)
    End Sub

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' Mise a jour : une version preparee (voir MiseAJour) est installee avant tout, puis le nouveau programme
        ' est lance et celui-ci s'arrete. (Sans effet sur le poste de developpement.)
        If MiseAJour.InstallerAuLancement() Then End
        RegrouperLesJournaux()                ' (une fois : MigrationDiag.log, Error.log et l'ancien Log.log deviennent UN Log.log)
        AllegerLeJournalSiTropGros()          ' (au-dela de 20 Mo : on ne garde que les derniers jours)
        ViderLesJournauxChaqueMois()          ' (une fois par mois : journaux *.log de datasettings vides)
        ConsommerLaMarqueDeRelance()          ' (apres une relance automatique, mise a jour ou autre : les lignes Tuya du BOOT ne repartent pas)

        ChronoEtape("Form1_Load : debut")
        GardeTV.Demarrer()                  ' relance automatique si Slide reste fige (TV sans image, 03/10)
        SuiviMemoire.Demarrer()             ' une ligne par minute dans le journal : memoire, handles (06/10)
        RelanceDeNuit.Demarrer()            ' relance de Slide la nuit : rend la memoire gardee par VLC (07/10)
        AlternanceMusique.Demarrer()        ' plusieurs sources de musique cochees : elles alternent (14/10/2026)
        Telechargements.ReprendreApresRelance()     ' telechargements Nflix interrompus par la fermeture : repris (14/10/2026)
        MiseAJour.Surveiller()              ' mises a jour a distance : verification 1 min apres le lancement puis toutes les 6 h (14/10)
        BlinkClips.Demarrer()               ' dernier enregistrement Blink affiche tout seul s'il est nouveau (27/10/2026)




        Randomize()
        Me.BringToFront()
        Dim titre As String = My.Application.Info.Title
        If titre = "" Then titre = System.IO.Path.GetFileNameWithoutExtension(My.Application.Info.AssemblyName)
        ApplicationTitle.Text = UCase(Strings.Left(titre, 1)) & Mid(titre, 2)

        Version.Text = "Version " & SlideVersion
        ' Copyright sert de ligne d'etat pendant le chargement ("Reprise du Slide...", voir Module1).
        Copyright.Text = "Chargement de la bibliothèque…"

        ' Tout le chargement (et meme le diaporama) tourne DANS Form1_Load : Windows n'affiche une
        ' fenetre qu'a la fin de son Load, donc jamais ici. L'ancien ecran n'apparaissait que par
        ' effet de bord (changer FormBorderStyle ici recreait la fenetre, deja visible). On
        ' l'affiche donc explicitement, puis on la dessine tout de suite.
        ShowWindow(Me.Handle, 5)      ' SW_SHOW
        Me.Refresh()

        Me.KeyPreview = True
        Me.Refresh()
        Application.DoEvents()
        ' Mises a jour a distance : version publiee comparee a celle-ci, proposee si elle est plus recente (voir MiseAJour).
        If MiseAJour.ProposerAuLancement() Then End
        Copyright.Text = ""
        ' Deux travaux qui n'ont pas besoin de l'interface partent en tache de fond des maintenant
        ' (13/10/2026, "accelere aussi l'ouverture de Slide") :
        '  - la relecture des listes de la reprise du diaporama (voir Module1.PreparerLaReprise) ;
        '  - le dossier marqueur de la version, renomme sur le lecteur reseau (SaveVersion).
        If My.Settings.reprendreLeSlide Then PreparerLaReprise()
        Dim produit As String = Application.ProductName
        System.Threading.Tasks.Task.Run(Sub()
                                            Try
                                                SaveVersion(produit, SlideVersion)
                                            Catch ex As Exception
                                            End Try
                                        End Sub)
        ClosePlayer()
        ChronoEtape("apres SaveVersion + ClosePlayer")

        ' Cache de plugins de VLC : doit etre construit AVANT la creation du premier VlcControl
        ' (Form2, dans init). Sans lui, libvlc sonde ses ~360 plugins a chaque lancement.
        If CacheVLCaRegenerer() Then
            Label1.Text = "Optimisation VLC, une seule fois..."
            Me.Refresh()
            Application.DoEvents()
            GenereCacheVLC()
            Label1.Text = ""
            Me.Refresh()
            Application.DoEvents()
        End If
        ChronoEtape("apres cache VLC")

        If My.Computer.Keyboard.ShiftKeyDown Then  'sauter minuterie au boot
            LancerLePremierLancementDeLaMinuterie = False
            PremierLancementDeLaMinuterie = False
        End If

        If My.Computer.Keyboard.CtrlKeyDown Then  'access à option au boot
            DemanderLesReglages()
        End If

        Call init()
        ChronoEtape("apres init -- fenetres pretes")
        OublierLaReprisePreparee()          ' listes preparees pour la reprise et pas utilisees : memoire rendue

        ' "Réglages" clique dans les derniers instants de l'ecran de demarrage (apres le passage de
        ' init qui les ouvre) : ils s'ouvrent ici, avant le diaporama.
        Application.DoEvents()
        If ReglagesDemandesAuDemarrage Then
            ReglagesDemandesAuDemarrage = False
            Croix = False
            DiagLog("Demarrage : reglages demandes a la fin de l'ecran de demarrage -> ouverts")
            Me.Visible = False
            Form4.StartPosition = FormStartPosition.CenterScreen
            Form4.ShowDialog()
            ' (Valides par OK, Slide se relance. On ne revient ici qu'apres "Annuler".)
            OptionChange = False
            Croix = False
        End If


        'wait(2)
        Me.Visible = False




        ' Slide au premier plan des le demarrage. Avant : BringToFront (ordre entre les fenetres de
        ' Slide seulement) puis un CLIC SIMULE a un point calcule d'apres l'ecran de demarrage ; en
        ' mode fenetre il tombait souvent a cote de Slide (bureau, autre programme).
        MettreAuPremierPlan(Form3)
        ''''''
        'Call afficheProgramme()
        If StopWindowsUpdate = True Then Call WindowsUpdate("OFF")
        If ControlAdistance = True Then Call GestionControlAdistance(ControlAdistance)

        'tri date ordre croissant
        Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
        Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
        ChronoEtape("apres tri des grilles -- debut du diaporama")
        ' Sources de musique changees depuis la derniere normalisation du volume : elle se lance
        ' toute seule, en pastille "%" reduite, un peu apres le demarrage.
        NormalisationMusique.PlanifierAuDemarrage(20)


debut:


        'photo

        Form3.GroupBox1.Visible = False
        If onlyvideos = True Then GoTo video
        If NombreDePhoto <= 0 Then GoTo video

        ' Au LANCEMENT de Slide, la premiere photo d'abord, la musique ensuite (voir plus bas).
        ' Mesure du 06/10 sur le PC de Jacob : le premier morceau met 2 s et plus a demarrer (VLC
        ' ouvre la sortie son), et pendant cette attente la minuterie lancait le PIP du BOOT ;
        ' la premiere photo n'arrivait que 5 s apres la fin du chargement (7,3 s apres le
        ' lancement, contre 1,9 s quand la musique partait vite). Aux tours suivants, rien ne
        ' change : la musique joue deja, ces appels reviennent tout de suite.
        Dim musiqueApresLaPhoto As Boolean = Not _musiqueLanceeAuDemarrage
        _musiqueLanceeAuDemarrage = True

        If Not musiqueApresLaPhoto Then
            If Music = True Then
                If PIPvolume = 0 And (Music_Deezer = False OrElse MusiqueDeezer.Actif) Then MusiqueVLC.MusiqueJouer()
            End If
        End If

        Diffusion.Mode = "PHOTO"
        'VolumeActuel = VolumeMusicSlide

        If Not musiqueApresLaPhoto Then SetMusicVolSlide(VolumeMusicSlide)
        période = DurréePhoto / 100000 : Start = DateTime.Now.ToOADate()   ' heure de début.



        Do While DateTime.Now.ToOADate() < Start + période

re:
            If RDV_ProchainNumPhotoForcée = -1 Then
                NumPhoto = prochainePhoto()
            Else
                NumPhoto = RDV_ProchainNumPhotoForcée
                RDV_ProchainNumPhotoForcée = -1
            End If

            If NumPhoto = -1 Then
                RDV_ProchainNumVideoForcée = NumVideo
                Exit Do
            End If




            Form2.DataGridView1.Rows(NumPhoto).Cells(0).Value = 1
            PhotoName = Form2.DataGridView1.Rows(NumPhoto).Cells(3).Value



            Try
                If PhotoName = "" Or IsNothing(PhotoName) = True Then GoTo re
            Catch ex As Exception
                wait(1)
                GoTo re
            End Try


            ' Photo introuvable (dossier supprime, disque reseau absent) : sautee tout de suite, sans attendre la duree d'affichage ; voir PhotoDisponibleOuAttendre.
            If Not PhotoDisponibleOuAttendre(CStr(PhotoName)) Then
                If FlagExit = True Then FlagExit = False : Exit Do
                GoTo re
            End If

            Call AffichePhoto(PhotoName)
            NoterSuivantAffiche("photo")

            ' Lancement de Slide : la premiere photo est a l'ecran, la musique peut demarrer.
            If musiqueApresLaPhoto Then
                musiqueApresLaPhoto = False
                Application.DoEvents()
                ChronoEtape("premiere photo affichee -- demarrage de la musique")
                LancerLaMusiqueDuDiaporama()
            End If

            For y = 1 To DurréeAffichageDuneImage
                wait(1)
                If FlagExit = True Then Exit For

            Next
            'wait(DurréeAffichageDuneImage)

            Do Until FlagStop = False
                wait(1)
            Loop
            If FlagExit = True Then FlagExit = False : NoterSuivantPrisEnCompte("photo") : Exit Do
        Loop
        ' (aucune photo affichee a ce premier tour : la musique demarre quand meme)
        If musiqueApresLaPhoto Then LancerLaMusiqueDuDiaporama()



video:
        CumSecondeVideoPlay = 0
        'VolumeActuel = VolumeVideoSlide
        SetVideoVolSlide(VolumeVideoSlide)

        If NombreDeVideo <= 0 Then GoTo debut
        Diffusion.Mode = "VIDEO"
        NouveauCycle = True

        If RDV_ProchainNumVideoForcée = -1 Then
            NumVideo = ProchaineVideo()
        Else
            NumVideo = RDV_ProchainNumVideoForcée
            RDV_ProchainNumVideoForcée = -1
        End If

        If NumVideo = -1 Then
            RDV_ProchainNumPhotoForcée = NumPhoto
            GoTo debut
        End If

video2:

        If onlyphotos = True Then GoTo debut
        ErrorReadVideo = False
        Call AfficheVideo(NumVideo)
        NoterSuivantAffiche("video")
        ''
        SetVideoVolSlide(VolumeVideoSlide)

        If ErrorReadVideo = True Then GoTo debut


        période = DurréeVideo / 100000
        If ModeRdv = True Then
            If ModeRdvLimitVideo <> 0 Then
                période = ModeRdvLimitVideo / 100000
            Else
                période = 1 'illimité
            End If

        End If
        Start = DateTime.Now.ToOADate()   ' heure de début.
        ' Musique en pause seulement si on entend la video ; video muette = la musique continue.
        MusiquePendantLaVideo()

        Do While DateTime.Now.ToOADate() < Start + période

            ' Video suivante prete sur l'autre lecteur : on quitte celle-ci un quart de seconde
            ' avant sa fin. Arrivee au bout, VLC ferme son image (ecran noir) ; figee juste avant,
            ' sa derniere image reste a l'ecran jusqu'a ce que la suivante joue.
            ' (Mesure du 01/10 : la video suivante met ~300 ms a partir. Elle est donc LANCEE 450 ms
            '  avant la fin de celle-ci, encore reduite a son point ; on change des qu'elle bouge.)
            If PrechargementVideo.SuivantePrete AndAlso Form3.VlcControl1.Length > 1500 Then
                Dim resteVideo As Long = PrechargementVideo.ResteDeLaVideoEnCours()
                ' (2e mesure : avec 450 ms d'avance, la suivante n'avait parfois pas encore demarre
                '  au changement - 370 ms de noir. Elle est lancee 700 ms avant la fin, et on ne
                '  change qu'a 200 ms de la fin, pour ne pas ecourter la video en cours.)
                ' (3e mesure : la position donnee par VLC n'avance que par a-coups de ~300 ms ; le
                '  reste est maintenant extrapole. Lancement 500 ms avant la fin - la suivante met
                '  ~300 ms a partir -, changement a 120 ms de la fin.)
                ' (4e reglage, idee de Jacob : lancer la suivante plus tard - 400 ms avant la fin -
                '  et ne changer que lorsque VLC a REELLEMENT affiche de nouvelles images de la
                '  suivante. Si la video en cours arrive au bout avant, elle reste figee sur sa
                '  derniere image en attendant - 0,6 s au plus - plutot que de laisser passer du noir.)
                ' (6e mesure : le 2e signal de VLC arrive ~430 ms apres le lancement ; lancee 650 ms
                '  avant la fin annoncee, la suivante prend le relais ~200 ms avant cette fin.)
                ' (7e reglage : la suivante est OUVERTE et lancee ici, d'un seul coup, 600 ms avant la
                '  fin annoncee - une seule operation par video sur le second lecteur.)
                ' (21/10/2026 : "il faudrait que la nouvelle video arrive alors que l'autre n'a pas encore fini" : lancement 1 s avant la fin au lieu de 0,6 s, et l'ancienne n'est plus mise en pause : elle continue de bouger jusqu'a ce que sa photographie soit prise, au lieu de se figer avant la poussee.)
                If resteVideo <= 800 Then PrechargementVideo.LancerEnAvance()
                ' (5e mesure : la video finissait d'elle-meme ~100 ms avant la fin annoncee par VLC,
                '  et le changement n'arrivait qu'apres : 0,05 a 0,1 s fige. On change maintenant DES
                '  que la suivante bouge, pendant que celle-ci joue encore.)
                If PrechargementVideo.SuivanteEnMouvement OrElse resteVideo <= 40 Then
                    PrechargementVideo.DebutDuChangement()
                    ' (l'ancienne continue de jouer : voir plus haut)
                    PrechargementVideo.Etape("pause")
                    Dim attenteSuivante As Stopwatch = Stopwatch.StartNew()
                    Do Until PrechargementVideo.SuivanteEnMouvement OrElse attenteSuivante.ElapsedMilliseconds > 600
                        Application.DoEvents()
                        Threading.Thread.Sleep(5)
                    Loop
                    PrechargementVideo.Etape("enMouvement")
                    Exit Do
                End If
            End If
            If Form3.VlcControl1.Time >= Form3.VlcControl1.Length Then Exit Do
            If Form3.VlcControl1.IsPlaying = False And Form3.ToolStripMenuItem2.Checked = False Then Exit Do

            If Form3.ToolStripMenuItem2.Checked = True Then
                Form3.GroupBox1.Top = Form3.ClientSize.Height - Form3.GroupBox1.Height - 10
                Form3.GroupBox1.Left = Form3.Width / 2 - Form3.GroupBox1.Width / 2
                Form3.GroupBox1.Visible = True
                Form3.TrackBar2.Value = VolumeActuel
                'SetVol(Form3.TrackBar2.Value)
                'Form3.TrackBar2.Value = Form3.VlcControl1.Audio.Volume + 1 * (VolumeVLCBoost * (VLCBoost))  'true =-1 donc - par - = +

                Form3.Label1.Text = Form3.TrackBar2.Value.ToString
                pos1 = Form3.VlcControl1.Time / 1000
                Form3.Label3.Text = TimeSpan.FromSeconds(CDbl(pos1)).ToString("hh\:mm\:ss")

                pos2 = Form3.VlcControl1.Length / 1000
                Form3.Label2.Text = TimeSpan.FromSeconds(CDbl(pos2)).ToString("hh\:mm\:ss")

                Form3.TrackBar1.Maximum = pos2 '/ 60
                Form3.TrackBar1.Value = pos1 '/ 60

                Form3.GroupBox1.BringToFront()
            End If



            Application.DoEvents()

            Do Until FlagStop = False

                If Form3.VlcControl1.IsPlaying = True Then Form3.VlcControl1.Pause()
                MusiqueVLC.MusiquePause()
                wait(1)
                Application.DoEvents()
            Loop

            ' Fin de la video verifiee tous les dixiemes de seconde (avant : toutes les secondes,
            ' la derniere image restait figee jusqu'a 1 s avant la video suivante). wait(0) une
            ' fois par seconde garde le suivi de la radio fait par wait().
            ' (25 ms quand une video suivante attend : le changement se joue a quelques images pres.)
            Dim pasAttente As Integer = If(PrechargementVideo.SuivantePrete, 25, 100)
            AttendreMillisecondes(pasAttente)
            toursVideo += pasAttente
            If toursVideo >= 1000 Then toursVideo = 0 : wait(0)
            If FlagExit = True Then
                FlagExit = False
                NoterSuivantPrisEnCompte("video")

                Form3.VlcControl1.Pause()

                GoTo debut
                Exit Do
            End If
        Loop

        CumSecondeVideoPlay = CumSecondeVideoPlay + DurationLastVideo

        If CumSecondeVideoPlay < DurréeVideo And ModeRdv = False Then
            NumVideoProv = ProchaineVideoSuivante()
            Nom = Form2.DataGridView2.Rows(NumVideoProv).Cells(3).Value
            Dim DuréeDeCeFilm As String = GetDuration(Nom)

            If DuréeDeCeFilm = "" Or DuréeDeCeFilm = "00:00:00" Then GoTo saute
            MyDuration = TimeSpan.ParseExact(DuréeDeCeFilm, "c", Nothing).TotalSeconds
            If MyDuration = 0 Then GoTo saute

            If MyDuration > DurréeVideo Then DurréeVideo = DurréeVideo
            CumCum = CumSecondeVideoPlay + MyDuration
            If CumCum < DurréeVideo Then
                NumVideo = NumVideoProv
                GoTo video2
            End If
saute:
        End If



        Form3.VlcControl1.Pause()

fin:

        Application.DoEvents()

        GoTo debut



    End Sub



    Private Sub Version_Click(sender As Object, e As EventArgs) Handles Version.Click
        a = MsgBox("Etes vous sure de vouloir éffacer la mémoire cache ?", vbOKCancel)
        If a <> 1 Then Exit Sub
        Username = Environment.UserName
        appname = My.Application.Info.Title
        rep = "C:\Users\" & Username & "\AppData\Local\" & appname
        If Directory.Exists(rep) Then
            Directory.Delete(rep, True)
        End If
        'C:\Users\jacob\AppData\Local\slide\
    End Sub

    Private Sub ApplicationTitle_Click(sender As Object, e As EventArgs) Handles ApplicationTitle.Click

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        DemanderLesReglages()
    End Sub

    ''' <summary>Bouton "Réglages" ou Ctrl au lancement : les reglages s'ouvriront au demarrage.</summary>
    Private Sub DemanderLesReglages()
        Croix = True
        OptionChange = True
        ' Les reglages s'ouvrent meme si le chargement des sources est deja passe (voir Module1.init
        ' et la fin du chargement dans Form1_Load) : avant, un clic un peu tardif ne faisait rien.
        ReglagesDemandesAuDemarrage = True
        Button1.Visible = False
        Label4.Visible = True
        Me.Refresh()
        Application.DoEvents()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Ecran de demarrage : coins arrondis, ombre, portrait dans un medaillon
    ' ------------------------------------------------------------------------------------------

    Private _imagePortrait As Image

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ClassStyle = cp.ClassStyle Or &H20000     ' CS_DROPSHADOW
            Return cp
        End Get
    End Property

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        Try
            DwmSetWindowAttribute(Me.Handle, 33, 2, 4)                ' coins arrondis (Windows 11)
            DwmSetWindowAttribute(Me.Handle, 34, &H3C3A3A, 4)         ' lisere gris sombre (0x00BBGGRR)
        Catch ex As Exception
        End Try
    End Sub

    <System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="ShowWindow")>
    Private Shared Function ShowWindow(hwnd As IntPtr, commande As Integer) As Boolean
    End Function

    <System.Runtime.InteropServices.DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attribut As Integer, ByRef valeur As Integer, taille As Integer) As Integer
    End Function

    Private Function ImagePortrait() As Image
        If _imagePortrait Is Nothing Then
            Try
                Using flux As Stream = GetType(Form1).Assembly.GetManifestResourceStream("Portrait.png")
                    If flux IsNot Nothing Then _imagePortrait = New Bitmap(flux)
                End Using
            Catch ex As Exception
                _imagePortrait = Nothing
            End Try
        End If
        Return _imagePortrait
    End Function

    ''' <summary>Medaillon : disque en degrade, photo detouree par-dessus (coupee par le cercle
    ''' en bas, au niveau des epaules), fin anneau bleu.</summary>
    Private Sub Portrait_Paint(sender As Object, e As PaintEventArgs) Handles Portrait.Paint
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.InterpolationMode = InterpolationMode.HighQualityBicubic
        g.PixelOffsetMode = PixelOffsetMode.HighQuality

        Dim anneau As Single = Math.Max(2.0F, Portrait.Width / 60.0F)
        Dim cercle As New RectangleF(anneau, anneau, Portrait.Width - 2 * anneau - 1, Portrait.Height - 2 * anneau - 1)

        Using disque As New GraphicsPath()
            disque.AddEllipse(cercle)
            Using fond As New LinearGradientBrush(cercle, Color.FromArgb(58, 74, 104), Color.FromArgb(24, 30, 44), LinearGradientMode.Vertical)
                g.FillPath(fond, disque)
            End Using

            Dim photo As Image = ImagePortrait()
            If photo IsNot Nothing Then
                Dim etatAvant As Drawing2D.GraphicsState = g.Save()
                g.SetClip(disque)
                ' Photo carree (fond conserve) deja recadree avec le visage au centre, a peine
                ' plus grande que le disque pour qu'aucun bord ne se voie.
                Const CentreVisageX As Single = 0.5F, CentreVisageY As Single = 0.5F
                Dim t As Single = cercle.Width * 1.02F
                Dim cx As Single = cercle.Left + cercle.Width / 2, cy As Single = cercle.Top + cercle.Height / 2
                g.DrawImage(photo, cx - CentreVisageX * t, cy - CentreVisageY * t, t, t)
                g.Restore(etatAvant)
            End If
        End Using

        Using stylo As New Pen(Color.FromArgb(10, 132, 255), anneau)
            g.DrawEllipse(stylo, cercle)
        End Using
    End Sub

    Private Sub ApplicationTitle_KeyDown(sender As Object, e As KeyEventArgs) Handles ApplicationTitle.KeyDown

    End Sub

    Private Sub Form1_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown

        If e.KeyCode = Keys.Escape Then
            Application.DoEvents()
            End
        End If

    End Sub

    Private Sub ProgressBar1_Click(sender As Object, e As EventArgs) Handles ProgressBar1.Click

    End Sub

    Private Sub Form1_KeyPress(sender As Object, e As KeyPressEventArgs) Handles Me.KeyPress

    End Sub
End Class


