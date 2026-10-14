Imports System.ComponentModel
Imports System.Threading
Imports Vlc.DotNet.Forms
Imports Vlc.DotNet.Core
Imports System.IO



Public Class Form3

    Inherits System.Windows.Forms.Form
    Implements IMessageFilter

    ' ------------------------------------------------------------------------------------------
    ' Lecteur video du diaporama : DEUX lecteurs (A, B) qui alternent. Pendant qu'une video joue
    ' sur l'un, la suivante est ouverte en pause, muette et cachee sur l'autre (PrechargementVideo) :
    ' a la fin, on bascule au lieu d'ouvrir le fichier, et les videos s'enchainent sans attente.
    ' Tout le programme continue d'utiliser VlcControl1, qui designe le lecteur a l'ecran.
    ' ------------------------------------------------------------------------------------------

    Friend WithEvents VlcLecteurB As Vlc.DotNet.Forms.VlcControl
    Private _lecteurActif As Vlc.DotNet.Forms.VlcControl

    ''' <summary>Le lecteur video du diaporama a l'ecran (A ou B).</summary>
    Public ReadOnly Property VlcControl1 As Vlc.DotNet.Forms.VlcControl
        Get
            Return If(_lecteurActif, VlcLecteurA)
        End Get
    End Property

    ''' <summary>L'autre lecteur, cache : celui ou la video suivante est prechargee.</summary>
    Public ReadOnly Property VlcLecteurCache As Vlc.DotNet.Forms.VlcControl
        Get
            Return If(ReferenceEquals(VlcControl1, VlcLecteurA), VlcLecteurB, VlcLecteurA)
        End Get
    End Property

    Public Sub New()
        InitializeComponent()
        ' Second lecteur, memes reglages que le premier (cree par le concepteur).
        VlcLecteurB = New Vlc.DotNet.Forms.VlcControl()
        CType(VlcLecteurB, System.ComponentModel.ISupportInitialize).BeginInit()
        VlcLecteurB.BackColor = VlcLecteurA.BackColor
        VlcLecteurB.ContextMenuStrip = VlcLecteurA.ContextMenuStrip
        VlcLecteurB.Name = "VlcLecteurB"
        VlcLecteurB.Spu = -1
        VlcLecteurB.Visible = False
        VlcLecteurB.Bounds = VlcLecteurA.Bounds
        Controls.Add(VlcLecteurB)
        CType(VlcLecteurB, System.ComponentModel.ISupportInitialize).EndInit()   ' VlcLibDirectoryNeeded
        ' Signal de VLC "la position de lecture avance" : sert a savoir que la video suivante,
        ' lancee en avance, joue REELLEMENT (voir PrechargementVideo.LectureAvance).
        AddHandler VlcLecteurA.TimeChanged, Sub(s, ev) PrechargementVideo.LectureAvance(VlcLecteurA, ev.NewTime)
        AddHandler VlcLecteurB.TimeChanged, Sub(s, ev) PrechargementVideo.LectureAvance(VlcLecteurB, ev.NewTime)
        ' (Sa fenetre interne n'est PAS creee ici : cela creait aussi celle de Form3 des la
        '  construction, avant son affichage, et Slide n'apparaissait plus dans la barre des taches.
        '  Elle est creee dans Form3_Load.)
    End Sub

    ''' <summary>Le lecteur cache (video suivante prechargee) devient le lecteur du diaporama.
    ''' L'ancien, fige sur sa derniere image, RESTE AFFICHE devant lui tant que la nouvelle video
    ''' n'a pas vraiment commence (FinirLaBascule) : pas d'ecran noir entre deux videos.</summary>
    ''' <param name="sousCache">Vrai si une photo de l'ecran couvre l'image (FormTransitionVideo) :
    ''' le nouveau lecteur passe alors devant tout de suite, il se dessine a l'abri du cache.</param>
    Public Sub BasculerLeLecteur(Optional sousCache As Boolean = False)
        FinirLaBascule()                                   ' une bascule precedente restee en attente
        Dim ancien As Vlc.DotNet.Forms.VlcControl = VlcControl1
        Dim nouveau As Vlc.DotNet.Forms.VlcControl = VlcLecteurCache
        _lecteurActif = nouveau
        nouveau.Bounds = New Rectangle(0, 0, ClientSize.Width, ClientSize.Height)
        nouveau.Visible = True
        If ancien.Visible AndAlso Not sousCache Then
            ancien.BringToFront()                          ' l'ancienne image masque le demarrage
            _ancienLecteur = ancien
        Else
            nouveau.BringToFront()
            ancien.Visible = False
            ArreterEnArrierePlan(ancien)
        End If
    End Sub

    Private _ancienLecteur As Vlc.DotNet.Forms.VlcControl

    ''' <summary>La nouvelle video joue : elle passe devant, l'ancien lecteur est cache et arrete.</summary>
    Public Sub FinirLaBascule()
        Dim ancien As Vlc.DotNet.Forms.VlcControl = _ancienLecteur
        If ancien Is Nothing Then Exit Sub
        _ancienLecteur = Nothing
        VlcControl1.BringToFront()
        ancien.Visible = False
        ArreterEnArrierePlan(ancien)
    End Sub

    ''' <summary>
    ''' L'ancien lecteur est simplement laisse en PAUSE, cache. Surtout pas de Stop() ici :
    '''  - sur le thread de l'interface, Stop() attend que VLC ferme son image, et VLC attend ce
    '''    meme thread pour fermer sa fenetre : Slide se bloquait ("ne repond pas", 01/10 18:41) ;
    '''  - en tache de fond, il pouvait arriver APRES le prechargement de la video d'apres sur ce
    '''    meme lecteur, et l'interrompre.
    ''' Le lecteur resservira au prochain prechargement : SetMedia + Play y remplacent la video en
    ''' pause, comme le diaporama l'a toujours fait d'une video a l'autre.
    ''' </summary>
    Private Shared Sub ArreterEnArrierePlan(lecteur As Vlc.DotNet.Forms.VlcControl)
        Try
            lecteur.SetPause(True)
        Catch ex As Exception
        End Try
        ' ... puis sa memoire est rendue, en tache de fond et sans risque (voir LibererLeLecteur).
        LibererLeLecteur(lecteur)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Memoire des lecteurs video du diaporama (06/10).
    '
    ' Mesure sur une video 4K d'iPhone (3840 x 2160, HEVC) : tant qu'elle est ouverte - en lecture
    ' OU EN PAUSE - VLC reserve 1,7 Go (48 images d'avance) ; a l'arret, il ne reste que 28 Mo.
    ' Une video 1080p : environ 300 Mo. Or les lecteurs n'etaient jamais arretes, seulement mis
    ' en pause : apres une video, sa memoire restait prise pendant toute la sequence de photos,
    ' et avec les deux lecteurs (video en cours + precedente), c'etait le double. D'ou les
    ' montees a 1,5 Go observees, et bien plus avec des videos 4K.
    '
    ' Un lecteur dont on n'a plus besoin est donc ARRETE, mais :
    '  - en tache de fond : Stop() appele sur le thread de l'interface se bloquait (voir plus
    '    haut) ; ici ce thread continue de tourner, VLC peut fermer sa fenetre ;
    '  - avec une garde : avant de rouvrir une video sur ce lecteur (AttendreLeLecteur), on
    '    attend la fin de son arret, qui ne peut donc plus interrompre la video d'apres.
    ' ------------------------------------------------------------------------------------------

    Private Shared ReadOnly _arrets As New Dictionary(Of Vlc.DotNet.Forms.VlcControl, Task)

    ''' <summary>Arrete <paramref name="lecteur"/> en tache de fond pour rendre sa memoire video.
    ''' Sans effet s'il est deja arrete ou en cours d'arret.</summary>
    Public Shared Sub LibererLeLecteur(lecteur As Vlc.DotNet.Forms.VlcControl)
        If lecteur Is Nothing Then Exit Sub
        Try
            SyncLock _arrets
                Dim enCours As Task = Nothing
                If _arrets.TryGetValue(lecteur, enCours) AndAlso Not enCours.IsCompleted Then Exit Sub
                Dim etat As Vlc.DotNet.Core.Interops.Signatures.MediaStates = lecteur.State
                If etat = Vlc.DotNet.Core.Interops.Signatures.MediaStates.Stopped OrElse
                   etat = Vlc.DotNet.Core.Interops.Signatures.MediaStates.NothingSpecial Then Exit Sub
                _arrets(lecteur) = Task.Run(Sub()
                                                Try
                                                    lecteur.Stop()
                                                Catch ex As Exception
                                                End Try
                                            End Sub)
            End SyncLock
        Catch ex As Exception
            DiagLog("Memoire video : arret du lecteur impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Retour aux photos : les deux lecteurs video du diaporama rendent leur memoire.</summary>
    Public Sub LibererLesLecteursVideo()
        LibererLeLecteur(VlcLecteurA)
        LibererLeLecteur(VlcLecteurB)
    End Sub

    ''' <summary>A appeler avant d'ouvrir une video sur <paramref name="lecteur"/> : attend la fin
    ''' de son arret en cours, s'il y en a un (le thread de l'interface continue de tourner).
    ''' Renvoie False si l'arret n'a pas fini dans le temps donne.</summary>
    Public Shared Function AttendreLeLecteur(lecteur As Vlc.DotNet.Forms.VlcControl, Optional attenteMs As Integer = 2000) As Boolean
        Dim enCours As Task = Nothing
        SyncLock _arrets
            If lecteur Is Nothing OrElse Not _arrets.TryGetValue(lecteur, enCours) Then Return True
        End SyncLock
        If enCours.IsCompleted Then Return True
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Do Until enCours.IsCompleted OrElse chrono.ElapsedMilliseconds > attenteMs
            Application.DoEvents()
            Threading.Thread.Sleep(10)
        Loop
        If Not enCours.IsCompleted Then DiagLog("Memoire video : l'arret du lecteur dure plus de " & attenteMs & " ms")
        Return enCours.IsCompleted
    End Function

    Public m_Ymid As Integer
    Public m_Y As Integer
    Public m_GraphThread As Thread
    Public Const GRID_STEP As Integer = 40




    ' ------------------------------------------------------------------------------------------
    ' Souris sur l'image (demandes de Jacob, 07/10) :
    '   - double clic : plein ecran <-> fenetre, comme "FullScreen" du menu du clic droit ;
    '   - clic droit, "Fenetre sans bordure" : le reglage "Borderless" des options (onglet Videos),
    '     enregistre de la meme facon. L'image reste exactement a la meme place a l'ecran : seul le
    '     cadre apparait ou disparait autour d'elle. (D'abord mis sur le double clic, puis deplace
    '     dans le menu a la demande de Jacob.)
    '
    '   - fenetre sans bordure : on la deplace en la tirant par l'image, on la redimensionne en la
    '     tirant par un bord ou un coin (le curseur change au bord), comme une fenetre normale.
    '
    ' Tout cela passe par un filtre de messages : la photo, la video, le fond et les textes poses
    ' sur l'image (legende, lieu) sont des fenetres differentes, et la souris doit agir de la meme
    ' facon sur toutes. Les boutons et curseurs de Form3 gardent leur double clic.
    ' ------------------------------------------------------------------------------------------

    Private Const WM_MOUSEMOVE As Integer = &H200
    Private Const WM_LBUTTONDOWN As Integer = &H201
    Private Const WM_LBUTTONUP As Integer = &H202
    Private Const WM_LBUTTONDBLCLK As Integer = &H203
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const WM_SIZING As Integer = &H214
    Private Const MK_LBUTTON As Integer = 1
    ' Zones d'une fenetre pour Windows (WM_NCHITTEST) : barre de titre, bords, coins.
    Private Const ZoneTitre As Integer = 2
    Private Const ZoneGauche As Integer = 10
    Private Const ZoneDroite As Integer = 11
    Private Const ZoneHaut As Integer = 12
    Private Const ZoneHautGauche As Integer = 13
    Private Const ZoneHautDroite As Integer = 14
    Private Const ZoneBas As Integer = 15
    Private Const ZoneBasGauche As Integer = 16
    Private Const ZoneBasDroite As Integer = 17

    Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
        Select Case m.Msg
            Case WM_LBUTTONDBLCLK
                _appuiEnCours = False
                Try
                    If SurLImage(Control.FromHandle(m.HWnd)) Then BeginInvoke(Sub() FullScreenToolStripMenuItem_Click(Me, EventArgs.Empty))
                Catch ex As Exception
                End Try
            Case WM_LBUTTONDOWN, WM_LBUTTONUP, WM_MOUSEMOVE
                Dim zone As Integer = 0
                Try
                    zone = GlisserDemande(m)
                Catch ex As Exception
                End Try
                If zone <> 0 Then
                    ' Windows prend la suite, comme si la souris tenait la barre de titre ou le bord
                    ' d'une fenetre normale (rend la main quand le bouton est relache).
                    Dim cible As Control = Control.FromHandle(m.HWnd)
                    If cible IsNot Nothing Then cible.Capture = False
                    SendMessage(Me.Handle, WM_NCLBUTTONDOWN, zone, 0)
                    Return True
                End If
        End Select
        Return False                                ' le message suit son chemin
    End Function

    Private _appuiEnCours As Boolean                ' bouton gauche enfonce sur l'image sans bordure
    Private _appui As Point                         ' ou (a l'ecran)
    Private _appuiZone As Integer                   ' dans quelle zone de la fenetre

    ''' <summary>Fenetre sans bordure : suit le bouton gauche sur l'image. Renvoie la zone par
    ''' laquelle Windows doit tirer la fenetre (ZoneTitre : la deplacer ; un bord ou un coin : la
    ''' redimensionner), 0 sinon. Le glisser ne commence qu'une fois la souris REELLEMENT deplacee,
    ''' bouton enfonce : un clic ou un double clic (plein ecran) ne bouge pas la fenetre. Sans
    ''' bouton, regle le curseur de la souris au bord de l'image.</summary>
    Private Function GlisserDemande(ByRef m As Message) As Integer
        If m.Msg = WM_LBUTTONUP Then
            _appuiEnCours = False
            Return 0
        End If
        Dim cible As Control = Control.FromHandle(m.HWnd)
        If cible Is Nothing OrElse Not (cible Is Me OrElse (cible.FindForm() Is Me AndAlso
           (TypeOf cible Is PictureBox OrElse TypeOf cible Is VlcControl))) Then Return 0
        Dim deplacable As Boolean = Borderless = True AndAlso FullScreen = False AndAlso
                                    Me.WindowState = FormWindowState.Normal AndAlso
                                    Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        ' Position de la souris a l'ecran (dans le message : par rapport a la fenetre visee).
        Dim brut As Long = m.LParam.ToInt64()
        Dim souris As Point = cible.PointToScreen(New Point(CShort(brut And &HFFFF), CShort((brut >> 16) And &HFFFF)))
        Dim zone As Integer = If(deplacable, ZoneDeLaFenetre(souris), 0)

        If m.Msg = WM_LBUTTONDOWN Then
            _appuiEnCours = deplacable
            _appui = souris
            _appuiZone = zone
            Return 0
        End If

        If _appuiEnCours Then
            If Not deplacable OrElse (m.WParam.ToInt64() And MK_LBUTTON) = 0 Then
                _appuiEnCours = False
                Return 0
            End If
            Dim seuil As Size = SystemInformation.DragSize
            If Math.Abs(souris.X - _appui.X) <= seuil.Width \ 2 AndAlso Math.Abs(souris.Y - _appui.Y) <= seuil.Height \ 2 Then Return 0
            _appuiEnCours = False
            Return _appuiZone
        End If

        ' Souris libre : le curseur annonce ce que ferait un glisser. (Pas sur le fond de la
        ' fenetre : son curseur serait herite par tous ses controles.)
        If cible IsNot Me Then
            Dim voulu As Cursor = CurseurDeLaZone(zone)
            If cible.Cursor IsNot voulu AndAlso (voulu IsNot Cursors.Default OrElse CurseurDeLaZone(ZoneGauche) Is cible.Cursor OrElse
               CurseurDeLaZone(ZoneHaut) Is cible.Cursor OrElse CurseurDeLaZone(ZoneHautGauche) Is cible.Cursor OrElse
               CurseurDeLaZone(ZoneHautDroite) Is cible.Cursor) Then cible.Cursor = voulu
        End If
        Return 0
    End Function

    ''' <summary>Zone de la fenetre sous ce point de l'ecran : un coin, un bord, sinon l'interieur
    ''' (ZoneTitre). Bande de 8 a 16 pixels selon la taille de la fenetre, le double dans les coins.</summary>
    Private Function ZoneDeLaFenetre(p As Point) As Integer
        Dim r As Rectangle = Me.Bounds
        Dim bord As Integer = Math.Max(8, Math.Min(16, Math.Min(r.Width, r.Height) \ 25))
        Dim coin As Integer = bord * 2
        Dim gauche As Boolean = p.X < r.Left + bord, droite As Boolean = p.X >= r.Right - bord
        Dim haut As Boolean = p.Y < r.Top + bord, bas As Boolean = p.Y >= r.Bottom - bord
        Dim presGauche As Boolean = p.X < r.Left + coin, presDroite As Boolean = p.X >= r.Right - coin
        Dim presHaut As Boolean = p.Y < r.Top + coin, presBas As Boolean = p.Y >= r.Bottom - coin
        If (haut AndAlso presGauche) OrElse (gauche AndAlso presHaut) Then Return ZoneHautGauche
        If (haut AndAlso presDroite) OrElse (droite AndAlso presHaut) Then Return ZoneHautDroite
        If (bas AndAlso presGauche) OrElse (gauche AndAlso presBas) Then Return ZoneBasGauche
        If (bas AndAlso presDroite) OrElse (droite AndAlso presBas) Then Return ZoneBasDroite
        If gauche Then Return ZoneGauche
        If droite Then Return ZoneDroite
        If haut Then Return ZoneHaut
        If bas Then Return ZoneBas
        Return ZoneTitre
    End Function

    Private Shared Function CurseurDeLaZone(zone As Integer) As Cursor
        Select Case zone
            Case ZoneGauche, ZoneDroite : Return Cursors.SizeWE
            Case ZoneHaut, ZoneBas : Return Cursors.SizeNS
            Case ZoneHautGauche, ZoneBasDroite : Return Cursors.SizeNWSE
            Case ZoneHautDroite, ZoneBasGauche : Return Cursors.SizeNESW
            Case Else : Return Cursors.Default
        End Select
    End Function

    <System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)>
    Private Structure CadreFenetre
        Public Gauche As Integer
        Public Haut As Integer
        Public Droite As Integer
        Public Bas As Integer
    End Structure

    ''' <summary>Pendant qu'on tire un bord ou un coin (avec ou sans bordure) : la fenetre garde le
    ''' format de l'image, quel que soit le bord tire. Meme regle que Form3_Resize (largeur de la
    ''' fenetre = hauteur de l'image x 1,7779), mais appliquee AVANT que Windows ne redessine : tirer
    ''' le bord gauche ou droit agrandit maintenant la fenetre (avant, Form3_Resize annulait le
    ''' mouvement, la hauteur n'ayant pas change).</summary>
    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_SIZING Then
            Try
                Dim r As CadreFenetre = System.Runtime.InteropServices.Marshal.PtrToStructure(Of CadreFenetre)(m.LParam)
                Dim bord As Integer = m.WParam.ToInt32()         ' 1 gauche, 2 droite, 3 haut, 4 haut gauche, 5 haut droite, 6 bas, 7 bas gauche, 8 bas droite
                Dim cadre As Integer = Me.Height - Me.ClientSize.Height
                Dim largeur As Integer = r.Droite - r.Gauche, hauteurImage As Integer = r.Bas - r.Haut - cadre
                Dim parLaLargeur As Boolean
                Select Case bord
                    Case 1, 2 : parLaLargeur = True
                    Case 3, 6 : parLaLargeur = False
                    Case Else : parLaLargeur = largeur > CInt(hauteurImage * 1.7779)      ' un coin : le plus grand des deux
                End Select
                If parLaLargeur Then hauteurImage = CInt(largeur / 1.7779)
                hauteurImage = Math.Max(90, hauteurImage)
                largeur = CInt(hauteurImage * 1.7779)
                ' Le cote oppose a celui qu'on tire ne bouge pas.
                If bord = 1 OrElse bord = 4 OrElse bord = 7 Then r.Gauche = r.Droite - largeur Else r.Droite = r.Gauche + largeur
                If bord = 3 OrElse bord = 4 OrElse bord = 5 Then r.Haut = r.Bas - (hauteurImage + cadre) Else r.Bas = r.Haut + hauteurImage + cadre
                System.Runtime.InteropServices.Marshal.StructureToPtr(r, m.LParam, False)
            Catch ex As Exception
            End Try
        End If
        MyBase.WndProc(m)
    End Sub

    Private Sub SansBordureToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SansBordureToolStripMenuItem.Click
        BasculerSansBordure()
    End Sub

    ''' <summary>Vrai si <paramref name="cible"/> fait partie de l'image de Slide : le fond de la
    ''' fenetre, la photo, un lecteur video, ou un texte pose sur l'image (legende, lieu).</summary>
    Private Function SurLImage(cible As Control) As Boolean
        If cible Is Nothing Then Return False
        If cible Is Me Then Return True
        If TypeOf cible Is FormLegende OrElse TypeOf cible Is FormLieuBande OrElse TypeOf cible Is FormCalqueImage Then Return True
        Return cible.FindForm() Is Me AndAlso (TypeOf cible Is PictureBox OrElse TypeOf cible Is VlcControl)
    End Function

    ''' <summary>Retient la place et la taille de la fenetre pour le prochain lancement
    ''' (Module1.PlaceEnregistree) : apres un deplacement ou un redimensionnement, a la fermeture, a
    ''' la relance de nuit. Jamais en plein ecran ni reduite : c'est la place de la FENETRE qu'on
    ''' veut retrouver. L'appelant enregistre les reglages (My.Settings.Save).</summary>
    Public Sub RetenirLaPlace()
        If FullScreen = True OrElse Me.WindowState <> FormWindowState.Normal Then Exit Sub
        My.Settings.ScreenPosX = Me.Left : My.Settings.ScreenPosY = Me.Top
        My.Settings.ScreenSizeH = Me.Height : My.Settings.ScreenSizeW = Me.Width
    End Sub

    Public Sub BasculerSansBordure()
        Try
            Borderless = Not Borderless
            My.Settings.BorderLess = Borderless
            SansBordureToolStripMenuItem.Checked = Borderless
            ' La case des options suit, si la fenetre des options est ouverte.
            For Each options As Form4 In Application.OpenForms.OfType(Of Form4)().ToList()
                options.CheckBox7.Checked = Borderless
            Next
            ' En plein ecran (ou fenetre agrandie) : seul le reglage change, comme avec la case des
            ' options ; il s'applique au retour en fenetre (FullScreen_SWITCH_ON).
            If FullScreen = True OrElse Me.WindowState <> FormWindowState.Normal Then
                My.Settings.Save()
                DiagLog("Menu : fenetre " & If(Borderless, "sans bordure", "normale") & " au retour du plein ecran")
                Exit Sub
            End If
            Dim image As Rectangle = Me.RectangleToScreen(Me.ClientRectangle)      ' place de l'image a l'ecran
            Me.FormBorderStyle = If(Borderless, System.Windows.Forms.FormBorderStyle.None, System.Windows.Forms.FormBorderStyle.Sizable)
            ' Meme image, au meme endroit. (La largeur de la fenetre suit sa hauteur : Form3_Resize.)
            Me.ClientSize = image.Size
            Dim apres As Rectangle = Me.RectangleToScreen(Me.ClientRectangle)
            Dim x As Integer = Me.Left + image.Left - apres.Left
            Dim y As Integer = Me.Top + image.Top - apres.Top
            ' La barre de titre qui revient doit rester attrapable : pas au-dessus de l'ecran.
            If Not Borderless Then y = Math.Max(y, Screen.FromRectangle(image).WorkingArea.Top)
            Me.Location = New Point(x, y)
            ' Comme apres un redimensionnement a la souris (Form3_ResizeEnd) : taille retenue pour
            ' le prochain lancement.
            RetenirLaPlace()
            My.Settings.Save()
            RecalerApresChangementDeMode()          ' PIP, legende, montre, pastille : tout de suite
            DiagLog("Menu : fenetre " & If(Borderless, "sans bordure", "normale") & " (" & Me.Bounds.ToString() & ")")
        Catch ex As Exception
            DiagLog("Menu (fenetre sans bordure) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>L'image de Slide devient ou cesse d'etre la fenetre active : les PIP se rangent
    ''' TOUT DE SUITE (devant tout, ou juste au-dessus de l'image), sans attendre leur minuteur.</summary>
    Private Sub Form3_ActiviteChangee(sender As Object, e As EventArgs) Handles Me.Activated, Me.Deactivate
        Form12.AppliquerPremierPlan()
    End Sub

    ''' <summary>Menu referme : le PIP revient au premier plan (voir Form12.CederLePremierPlan).</summary>
    Private Sub ContextMenuStrip1_Closed(sender As Object, e As ToolStripDropDownClosedEventArgs) Handles ContextMenuStrip1.Closed
        Form12.CederLePremierPlan("menu", False)
    End Sub

    Private Sub ContextMenuStrip1_Opening(sender As Object, e As System.ComponentModel.CancelEventArgs) Handles ContextMenuStrip1.Opening
        ' Des l'ouverture (avant l'affichage du menu), pour qu'il apparaisse deja devant le PIP.
        Form12.CederLePremierPlan("menu", True)
        If PhotoAleatoire > 0 Then
            If PhotoAleatoire = 1 Then PhotoAléatoireToolStripMenuItem.Checked = True : ClassicToolStripMenuItem.Checked = True : OffToolStripMenuItem.Checked = False
            If PhotoAleatoire = 2 Then PhotoAléatoireToolStripMenuItem.Checked = True : SéquentielToolStripMenuItem.Checked = True : OffToolStripMenuItem.Checked = False
        Else
            PhotoAléatoireToolStripMenuItem.Checked = False
            ClassicToolStripMenuItem.Checked = False
            SéquentielToolStripMenuItem.Checked = False
            OffToolStripMenuItem.Checked = True
        End If



        If VideoAleatoire = True Then VideoAléatoireToolStripMenuItem.Checked = True Else VideoAléatoireToolStripMenuItem.Checked = False
        If Music = True Then MusicToolStripMenuItem.Checked = True Else MusicToolStripMenuItem.Checked = False
        If VideoWithAudio = True Then VideoAvecAudioToolStripMenuItem.Checked = True Else VideoAvecAudioToolStripMenuItem.Checked = False
        If FormatPhoto169 = True Then FormatPhoto169ToolStripMenuItem.Checked = True Else FormatPhoto169ToolStripMenuItem.Checked = False
        If PhotoAlbum = True Then FormatPhoto169ToolStripMenuItem.Checked = True Else FormatPhoto169ToolStripMenuItem.Checked = False

        If onlyphotos = True Then QueLesPhotosToolStripMenuItem.Checked = True : QueLesSlideVideosToolStripMenuItem.Checked = False
        If onlyvideos = True Then QueLesSlideVideosToolStripMenuItem.Checked = True : QueLesPhotosToolStripMenuItem.Checked = False
        SansBordureToolStripMenuItem.Checked = Borderless
    End Sub


    Private Sub Form3_Load(sender As Object, e As EventArgs) Handles MyBase.Load

        ' PIP et pastille de la normalisation se rangent au-dessus de cette fenetre quand un autre
        ' programme passe devant Slide (voir CalqueWin32.PlacerAuDessusDeSlide).
        CalqueWin32.FenetreDeSlide = Me.Handle
        ' Double clic sur l'image : fenetre sans bordure <-> fenetre normale (PreFilterMessage).
        Application.AddMessageFilter(Me)
        ' Icone du programme (la photo de Jacob) : barre des taches et barre de titre. Sans elle,
        ' la fenetre montrait l'icone generique de Windows.
        Try
            Using flux As IO.Stream = GetType(Form3).Assembly.GetManifestResourceStream("jacob55.ico")
                If flux IsNot Nothing Then Me.Icon = New Icon(flux)
            End Using
        Catch ex As Exception
        End Try
        Me.ShowInTaskbar = True
        Me.KeyPreview = True
        _veilleClavier.Interval = 40
        AddHandler _veilleClavier.Tick, AddressOf VeillerLeClavier
        _veilleClavier.Start()
        Me.BackColor = Color.Black
        MaximizeBox = False

        'pour que vlc marche avec contextmenustrip
        ' Les DEUX lecteurs du diaporama (A et B alternent, voir BasculerLeLecteur) : sinon, sur une
        ' video prechargee, VLC gardait la souris et le clic droit n'ouvrait plus le menu.
        ' Fenetre interne du second lecteur : VLC dessinera dedans, meme cache.
        Dim poigneeDuLecteurB As IntPtr = VlcLecteurB.Handle
        For Each lecteur As Vlc.DotNet.Forms.VlcControl In {VlcLecteurA, VlcLecteurB}
            lecteur.Video.IsMouseInputEnabled = False
            lecteur.Video.IsKeyInputEnabled = False
        Next
        VlcControl2.Video.IsMouseInputEnabled = False
        VlcControl2.Video.IsKeyInputEnabled = False

        'm_GraphThread = New Thread(AddressOf DrawGraph)
        'm_GraphThread.Priority = ThreadPriority.BelowNormal
        'm_GraphThread.IsBackground = True
        'm_GraphThread.Start()

        Me.StartPosition = FormStartPosition.CenterScreen
        'Me.BringToFront()

    End Sub

    Private Sub VoirListeToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VoirListeToolStripMenuItem.Click
        Form2.Show()
        Form2.Focus()

        If Diffusion.mode = "VIDEO" Then
            Me.PictureBox1.Visible = False
        Else
            Me.PictureBox1.Visible = True
        End If



        Form2.Button1.Visible = True
        Form2.Button2.Visible = False
        Form2.VlcControl1.Visible = False
        L = 1
        Try
            nom = Form2.DataGridView1.Rows(L).Cells(3).Value
            Form2.PictureBox1.Image = Image.FromFile(nom)

        Catch ex As Exception
            'MsgBox(ex.Message)

            Dim myGraphics As Graphics = Me.PictureBox1.CreateGraphics
            myGraphics.FillRectangle(Brushes.Gray, 0, 0, Me.PictureBox1.Width, Me.PictureBox1.Height)
            myGraphics.DrawString(ex.Message, New Font("Arial", 12), Brushes.Black, 0, 0)

        End Try

        Form2.PictureBox1.Refresh()
        If Form2.DataGridView1.RowCount > 0 Then Form2.HScrollBar1.Value = L
        Form2.DataGridView1.ClearSelection()
        Form2.DataGridView2.ClearSelection()


    End Sub

    ''' <summary>Remet le lecteur video de la fenetre a la taille de l'image, en le "secouant" d'un pixel pour que VLC refasse sa surface de dessin.
    ''' (22/10/2026, vu deux fois sur l'autre PC apres un passage du plein ecran a la fenetre, pendant une video : la video n'occupait que la partie droite,
    ''' le reste restait noir. Aucune transition n'etait en cause : le journal n'en porte pas trace.)</summary>
    Public Sub RecalerLeLecteurVideo()
        Try
            Dim lecteur As Vlc.DotNet.Forms.VlcControl = VlcControl1
            If lecteur Is Nothing OrElse Not lecteur.Visible Then Exit Sub
            Dim l As Integer = ClientSize.Width, h As Integer = ClientSize.Height
            If l < 16 OrElse h < 16 Then Exit Sub
            lecteur.SetBounds(0, 0, l + 1, h)
            lecteur.SetBounds(0, 0, l, h)
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Apres un changement de mode (plein ecran / fenetre) : le lecteur est recale tout de suite, puis trois fois encore (VLC refait sa surface apres coup).</summary>
    Public Sub RecalerLeLecteurVideoApresChangementDeMode()
        RecalerLeLecteurVideo()
        Dim essais As Integer = 0
        Dim minuteur As New System.Windows.Forms.Timer With {.Interval = 300}
        AddHandler minuteur.Tick, Sub()
                                      essais += 1
                                      RecalerLeLecteurVideo()
                                      If essais >= 3 Then minuteur.Stop() : minuteur.Dispose()
                                  End Sub
        minuteur.Start()
    End Sub

    Private Sub FullScreenToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FullScreenToolStripMenuItem.Click

        ' Duree de chaque etape, dans le journal : pendant qu'elles tournent, Slide ne repond pas et
        ' la video n'est pas encore agrandie (07/10 : 5 s de blocage sous Visual Studio, voir
        ' Module4.HeureDeLancement).
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim fenetre, calques, programmes As Long

        If FullScreen = False Then
            FullScreen_SWITCH_ON(True)
            'Me.FormBorderStyle = Windows.Forms.FormBorderStyle.None
            'Me.WindowState = FormWindowState.Maximized
            'Me.BackColor = Color.Black
            'FullScreenToolStripMenuItem.Checked = True
            FullScreen = True
            fenetre = chrono.ElapsedMilliseconds
            RecalerApresChangementDeMode()   ' PIP, legende, montre, pastille : tout de suite
            RecalerLeLecteurVideoApresChangementDeMode()
            calques = chrono.ElapsedMilliseconds
            PleinEcranChange(True)    ' les programmes lances a partir de maintenant seront fermes
            programmes = chrono.ElapsedMilliseconds

            My.Settings.FullScreen = True : My.Settings.Save()
            DiagLog("Plein ecran : bascule en " & chrono.ElapsedMilliseconds & " ms (fenetre " & fenetre & ", calques " & (calques - fenetre) &
                    ", programmes " & (programmes - calques) & ", reglages " & (chrono.ElapsedMilliseconds - programmes) & ")")
            Exit Sub
        End If

        If FullScreen = True Then

            FullScreen_SWITCH_ON(False)
            'Me.FormBorderStyle = Windows.Forms.FormBorderStyle.Fixed3D
            'Me.WindowState = FormWindowState.Normal
            ''Me.BackColor = Color.Black
            'FullScreenToolStripMenuItem.Checked = False
            FullScreen = False
            fenetre = chrono.ElapsedMilliseconds
            RecalerApresChangementDeMode()   ' PIP, legende, montre, pastille : tout de suite
            RecalerLeLecteurVideoApresChangementDeMode()
            calques = chrono.ElapsedMilliseconds
            PleinEcranChange(False)
            ' (plus de CenterToScreen : FullScreen_SWITCH_ON rend la position d'avant le plein ecran)
            My.Settings.FullScreen = False : My.Settings.Save()
            DiagLog("Retour en fenetre : bascule en " & chrono.ElapsedMilliseconds & " ms (fenetre " & fenetre & ", calques " & (calques - fenetre) &
                    ", reglages " & (chrono.ElapsedMilliseconds - calques) & ")")

        End If

    End Sub

    ''' <summary>Copie le dossier du programme sur la cle USB JACOB_1TO (voir SynchroniserSurCleUsb).</summary>
    Private Sub SynchroniserUsbKeyToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SynchroniserUsbKeyToolStripMenuItem.Click
        SynchroniserSurCleUsb()
    End Sub


    ''' <summary>Mises a jour de Slide (voir MiseAJour) : reglages, verification, installation, publication.</summary>
    Private Sub MiseAJourToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MiseAJourToolStripMenuItem.Click
        Using f As New FormMiseAJour
            f.ShowDialog(Me)
        End Using
    End Sub

    ''' <summary>
    ''' Clic droit > "Rechercher une mise a jour" (14/10/2026) : cherche tout de suite la version publiee ; si elle est plus
    ''' recente, la propose (telechargement, controle) puis Slide se relance pour l'installer en gardant son etat.
    ''' </summary>
    Private Async Sub RechercherMiseAJourToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RechercherMiseAJourToolStripMenuItem.Click
        Dim titre As String = "Mise à jour de Slide"
        Dim bilan As BilanMaj
        Cursor = Cursors.WaitCursor
        Try
            bilan = Await MiseAJour.Verifier(False, Nothing)
        Finally
            Cursor = Cursors.Default
        End Try
        DiagLog("Mise a jour (demandee) : " & bilan.Message)
        Dim installer As Boolean = False
        Select Case bilan.Resultat
            Case ResultatMaj.Disponible
                Using fenetre As New FormProposerMaj(bilan.Info)
                    installer = (fenetre.ShowDialog(Me) = DialogResult.Yes)
                End Using
            Case ResultatMaj.Preparee
                installer = MessageBox.Show(Me, bilan.Message & vbCrLf & vbCrLf & "L'installer maintenant ? Slide va redémarrer et reprendre là où il en était.",
                                            titre, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes
            Case ResultatMaj.AJour, ResultatMaj.ALaSource
                MessageBox.Show(Me, bilan.Message, titre, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Case Else
                MessageBox.Show(Me, bilan.Message, titre, MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Select
        If installer Then RelanceDeNuit.RelancerMaintenant()
    End Sub

    ''' <summary>Clic droit > "Voir les telechargements en cours" : la fenetre des telechargements de Nflix (films et
    ''' episodes, avancement, reprise), la meme que celle qui s'ouvre quand on en ajoute.</summary>
    ''' <summary>Clic droit > Parametres > Exporter / Importer : les reglages et la minuterie, pour configurer un autre PC (voir TransfertParametres).</summary>
    Private Sub ExporterParametresToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ExporterParametresToolStripMenuItem.Click
        TransfertParametres.CommandeExporter(Me)
    End Sub

    Private Sub ImporterParametresToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ImporterParametresToolStripMenuItem.Click
        TransfertParametres.CommandeImporter(Me)
    End Sub

    Private Sub VoirTelechargementsToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VoirTelechargementsToolStripMenuItem.Click
        FormTelechargements.Montrer(Me)
    End Sub

    ''' <summary>Clic droit > Telechargement > "1. Telecharger" : ouvre la fenetre Nflix sur les films ; son bouton
    ''' "Telecharger" copie les titres choisis (le suivi est dans "2. Voir les telechargements en cours").</summary>
    Private Sub TelechargerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TelechargerToolStripMenuItem.Click
        Try
            ChoixNflix.Ouvrir(Nothing, IptvSources.GenreIptv.Films)          ' (ouverte hors de la minuterie : aucune ligne a remplir)
        Catch ex As Exception
            DiagLog("Telechargement (fenetre Nflix) : " & ex.Message)
        End Try
    End Sub

    Private Sub EndToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles EndToolStripMenuItem.Click

        ' Chaque etape de la fermeture est chronometree dans le journal ("Fermeture : ...") : c'est ce
        ' qui dira ou part le temps s'il en reste (demande de Jacob, 13/10/2026 : "peux-tu accelerer la
        ' fermeture du programme").
        DiagLog("Fermeture : demandee")
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim temps As New List(Of String)
        Dim depuis As Long = 0
        Dim noter As Action(Of String) = Sub(quoi As String)
                                             temps.Add(quoi & " " & (chrono.ElapsedMilliseconds - depuis) & " ms")
                                             depuis = chrono.ElapsedMilliseconds
                                         End Sub

        ' Fenetre de fermeture : etapes reelles et barre de progression (voir FormFermeture). Elle s'affiche EN PREMIER :
        ' arreter Deezer prend jusqu'a 2,5 s (14/10/2026 : "la fenetre de fin met du temps a apparaitre").
        Dim fermeture As FormFermeture = FormFermeture.Afficher(Me)
        noter("fenetre (" & FormFermeture.DetailDeLOuverture & ")")

        ' La musique de Deezer s'arrete ensuite, avant les autres etapes.
        DiagLog("Fermeture : fenetre affichee")
        fermeture.Etape("Arrêt de la musique…", 8)
        MusiqueDeezer.ArreterALaSortie()
        noter("Deezer")
        DiagLog("Fermeture : musique arretee")

        If reprendreLeSlide = True Then
            fermeture.Etape("Sauvegarde de la position du diaporama…", 20)
            DiagLog("Fermeture : sauvegarde de la position...")
            SaveRestoreLeSlide(1)
            noter("position du diaporama")
        End If
        If StopWindowsUpdate = True Then
            fermeture.Etape("Réactivation de Windows Update…", 40)
            Call WindowsUpdate("ON")
            noter("Windows Update")
        End If
        ' (Chaque etape est ecrite au journal AVANT d'etre faite : 22/10/2026, sur l'autre PC la fermeture est restee bloquee apres la sauvegarde de la position, sans qu'on sache ou.)
        DiagLog("Fermeture : montre...")
        ' (L'ancienne montre externe TheAeroClock n'est plus utilisee - la montre est integree a Slide (FormHorloge) : plus de recherche ni d'arret de ce processus a la fermeture, qui enumerait tous les processus du PC ; 22/10/2026, fermeture bloquee sur l'autre PC.)
        ' (Plus de DoEvents pendant la fermeture : il redonnait la main a un message en attente qui bloquait, journaux de l'autre PC du 22/10/2026 - gel juste apres "montre...".)
        noter("montre")

        fermeture.Etape("Arrêt des lecteurs…", 60)
        DiagLog("Fermeture : arret des lecteurs...")
        ClosePlayer()
        noter("lecteurs")

        fermeture.Etape("Enregistrement des réglages…", 85)
        DiagLog("Fermeture : enregistrement des reglages...")
        RetenirLaPlace()
        My.Settings.Save()
        noter("reglages")

        fermeture.Etape("À bientôt", 100)
        ' (Plus de pause pour montrer la barre complete : 400 ms de fermeture en moins.)
        DiagLog("Fermeture : " & chrono.ElapsedMilliseconds & " ms au total | " & String.Join(" | ", temps))


        If My.Computer.Keyboard.CtrlKeyDown Then

            'MsgBox("etteindre pc ? ")
            eteintPC()

        End If

        End




    End Sub

    Private Sub MusicToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MusicToolStripMenuItem.Click

        If MusicToolStripMenuItem.Checked = True Then
            Music = False
            Music_SWITCH_ON(Music)
            'Me.AxWindowsMediaPlayer2.Ctlcontrols.pause()
            'MusicToolStripMenuItem.Checked = False
            My.Settings.Music = False : My.Settings.Save()
        Else

            Music = True
            Music_SWITCH_ON(Music)
            My.Settings.Music = True : My.Settings.Save()
        End If


    End Sub

    Private Sub VideoAvecAudioToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VideoAvecAudioToolStripMenuItem.Click

        If VideoAvecAudioToolStripMenuItem.Checked = True Then
            VideoWithAudio = False
            VideoWithAudio_SWITCH_ON(VideoWithAudio)
            'VideoAvecAudioToolStripMenuItem.Checked = False
            'Me.AxWindowsMediaPlayer1.settings.mute = True
            'If Music = True Then Me.AxWindowsMediaPlayer2.Ctlcontrols.play()
            My.Settings.VideoWithAudio = False : My.Settings.Save()
        Else
            VideoWithAudio = True
            VideoWithAudio_SWITCH_ON(VideoWithAudio)
            'VideoAvecAudioToolStripMenuItem.Checked = True
            'Me.AxWindowsMediaPlayer1.settings.mute = False
            'If Music = True And Me.AxWindowsMediaPlayer1.playState = WMPLib.WMPPlayState.wmppsPlaying Then Me.AxWindowsMediaPlayer2.Ctlcontrols.pause()
            My.Settings.VideoWithAudio = True : My.Settings.Save()
        End If

    End Sub

    Private Sub NextToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles NextToolStripMenuItem.Click

        SuivantSansFermerLesPip()         ' comme la touche N
        'If Me.AxWindowsMediaPlayer1.Visible = True Then
        'Me.AxWindowsMediaPlayer1.Ctlcontrols.stop()
        'End If
    End Sub

    Private Sub Form3_KeyDown(sender As Object, e As KeyEventArgs) Handles MyBase.KeyDown

        ' Appui long sur M par la voie normale (la repetition automatique de la touche arrive ici) : voir MettreAJourAppuiM.
        If e.KeyCode = Keys.M AndAlso Not e.Control AndAlso Not e.Alt Then MettreAJourAppuiM(True)

        ' Reglages des mises a jour (plus de menu : la publication est automatique a chaque compilation) : Ctrl+Maj+U.
        If e.Control AndAlso e.Shift AndAlso e.KeyCode = Keys.U Then
            MiseAJourToolStripMenuItem.PerformClick()
            Exit Sub
        End If

        If e.KeyCode = Keys.Escape Then
            EndToolStripMenuItem.PerformClick()
            End
        End If

        If e.KeyCode = Keys.Space Then
            Cursor.Position = New Point(Me.Width / 2, Me.Height / 2)
            Me.ContextMenuStrip.Show(Cursor.Position)
            'MsgBox("mouse:" & Cursor.ToString)
        End If

        ' N : suivant, les PIP restent tous ; P : suivant ET fermeture des PIP ; M : la pochette de la musique
        ' de nouveau ; H : l'aide des touches. Une autre touche referme l'aide.
        If e.KeyCode = Keys.N OrElse e.KeyCode = Keys.P OrElse e.KeyCode = Keys.M OrElse e.KeyCode = Keys.H OrElse e.KeyCode = Keys.S Then _toucheRecueLe = Environment.TickCount64
        If e.KeyCode <> Keys.H AndAlso e.KeyCode <> Keys.ShiftKey AndAlso e.KeyCode <> Keys.ControlKey AndAlso e.KeyCode <> Keys.Menu Then FormAideTouches.Cacher()
        If e.KeyCode = Keys.N OrElse e.KeyCode = Keys.P OrElse e.KeyCode = Keys.M OrElse e.KeyCode = Keys.H OrElse e.KeyCode = Keys.S Then FaireLaTouche(e.KeyCode)

    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Touches N et P qui "ne marchent pas de temps en temps" : la fenetre ne recoit le clavier que
    ' si c'est elle (ou un de ses controles) qui l'a. Quand le clavier est reste sur une fenetre
    ' interne de VLC, la touche n'arrive jamais ici. On surveille donc la touche elle-meme : si
    ' elle est enfoncee alors que Slide est au premier plan et que rien n'est arrive par la voie
    ' normale, on fait l'action quand meme (et on note qui avait le clavier).
    ' ------------------------------------------------------------------------------------------
    Private ReadOnly _veilleClavier As New System.Windows.Forms.Timer()
    Private _toucheRecueLe As Long
    Private ReadOnly _enfonceeDepuis As New Dictionary(Of Keys, Long)

    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetAsyncKeyState(vKey As Integer) As Short
    End Function
    <Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="GetForegroundWindow")>
    Private Shared Function FenetreAuPremierPlan() As IntPtr
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Shared Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef processId As UInteger) As UInteger
    End Function
    <Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="GetFocus")>
    Private Shared Function ControleQuiALeClavier() As IntPtr
    End Function

    ' Appui de plus de 3 secondes sur M (19/10/2026) : la pochette de la musique reste a l'ecran tant que M est maintenue ;
    ' relachee, elle reste encore 3 secondes, puis le cours normal reprend (voir CarteDeezer.Epingler / Liberer).
    Private _debutAppuiM As Long
    Private _appuiLongMFait As Boolean
    Private Const DureeAppuiLongMs As Long = 3000

    ''' <summary>Surveillance par la veille du clavier (la touche est lue directement, meme si la fenetre n'a pas le clavier).</summary>
    Private Sub SurveillerAppuiLongSurM()
        Dim enfoncee As Boolean = (GetAsyncKeyState(Keys.M) And &H8000) <> 0
        If enfoncee AndAlso (Not SlideAuPremierPlanPourM() OrElse My.Computer.Keyboard.CtrlKeyDown OrElse My.Computer.Keyboard.AltKeyDown) Then enfoncee = False
        MettreAJourAppuiM(enfoncee)
    End Sub

    ''' <summary>Premier plan = une fenetre de Slide qui ne sert pas a saisir du texte. Comme SlideAvecLeClavier, mais sans refuser a cause de
    ''' la fenetre des lecteurs (Form2) : elle peut rester visible derriere le diaporama (l'appui long sur M n'agissait alors jamais).</summary>
    Private Function SlideAuPremierPlanPourM() As Boolean
        Dim premierPlan As IntPtr = FenetreAuPremierPlan()
        If premierPlan = IntPtr.Zero Then Return False
        Dim pid As UInteger = 0
        GetWindowThreadProcessId(premierPlan, pid)
        If pid <> CUInt(Environment.ProcessId) Then Return False
        Try
            For Each fenetre As Form In Application.OpenForms.Cast(Of Form)().ToList()
                If Not fenetre.Visible OrElse fenetre Is Me Then Continue For
                Dim nom As String = fenetre.GetType().Name
                If nom = "Form4" OrElse nom = "Form6" OrElse nom = "Form8" OrElse nom = "Form11" OrElse nom = "Form14" OrElse
                   nom.StartsWith("FormMiseAJour") OrElse nom.StartsWith("FormProposer") OrElse nom = "FormChoixHasard" OrElse
                   nom = "FormCoherence" OrElse nom = "FormTelechargements" OrElse nom = "FormEpisodes" OrElse nom.StartsWith("FormRecherche") OrElse
                   nom = "FormInfoFilm" Then Return False
            Next
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

    ''' <summary>M enfoncee ou relachee : a 3 secondes d'appui un bip dit que c'est pris en compte et la pochette est laissee a l'ecran ;
    ''' au relachement elle reste encore 3 secondes. Appelee par la veille du clavier ET par les evenements clavier de la fenetre.</summary>
    Private Sub MettreAJourAppuiM(enfoncee As Boolean)
        If Not enfoncee Then
            _debutAppuiM = 0
            _appuiLongMFait = False                          ' (la pochette epinglee reste, M relachee : voir CarteDeezer.BasculerEpinglee)
            Exit Sub
        End If
        Dim maintenant As Long = Environment.TickCount64
        If _debutAppuiM = 0 Then _debutAppuiM = maintenant
        If Not _appuiLongMFait AndAlso maintenant - _debutAppuiM >= DureeAppuiLongMs Then
            _appuiLongMFait = True                           ' un seul basculement par appui, meme si M reste enfoncee
            Dim estEpinglee As Boolean = CarteDeezer.BasculerEpinglee()
            DiagLog("Touche M maintenue 3 s : appui long pris en compte (pochette " & If(estEpinglee, "epinglee", "au cours normal") & ")")
            Dim frequence As Integer = If(estEpinglee, 1200, 700)      ' bip aigu : epinglee ; plus grave : cours normal
            Task.Run(Sub()
                         Try
                             Console.Beep(frequence, 150)           ' bip : l'appui long est pris en compte
                         Catch ex As Exception
                         End Try
                     End Sub)
        End If
    End Sub

    Private Sub Form3_ToucheRelachee(sender As Object, e As KeyEventArgs) Handles MyBase.KeyUp
        If e.KeyCode = Keys.M Then MettreAJourAppuiM(False)
    End Sub

    Private Sub VeillerLeClavier(sender As Object, e As EventArgs)
        SurveillerAppuiLongSurM()
        For Each touche As Keys In {Keys.N, Keys.P, Keys.M, Keys.H, Keys.S}
            Dim etat As Short = GetAsyncKeyState(touche)
            Dim enfoncee As Boolean = (etat And &H8000) <> 0
            ' Appui tres court : la touche n'est plus enfoncee a la lecture suivante, mais Windows a retenu qu'elle l'a ete
            ' (bit 0). Avant, seuls les appuis de plus de 120 ms etaient rattrapes : un coup de doigt rapide sur N se perdait.
            Dim tapee As Boolean = (etat And &H1) <> 0 AndAlso Not enfoncee
            If Not enfoncee AndAlso Not tapee Then
                _enfonceeDepuis.Remove(touche)
                Continue For
            End If
            Dim maintenant As Long = Environment.TickCount64
            If enfoncee AndAlso Not _enfonceeDepuis.ContainsKey(touche) Then
                _enfonceeDepuis(touche) = maintenant            ' debut de l'appui
                Continue For
            End If
            Dim debut As Long = If(tapee, maintenant - 1000, _enfonceeDepuis(touche))
            If debut = 0 OrElse maintenant - debut < 50 Then Continue For
            _enfonceeDepuis(touche) = 0                         ' un seul rattrapage par appui
            ' Arrivee par la voie normale (l'action bloque la fenetre un instant : l'appui peut etre vu
            ' ici APRES son traitement, d'ou la marge d'une seconde - sinon N comptait double).
            If maintenant - _toucheRecueLe < 1000 Then Continue For
            If Not SlideAvecLeClavier() Then Continue For          ' Slide n'a pas la main (ou reglages ouverts)
            If My.Computer.Keyboard.CtrlKeyDown OrElse My.Computer.Keyboard.AltKeyDown Then Continue For
            Dim qui As String = "aucun controle de Slide"
            Try
                Dim c As Control = Control.FromChildHandle(ControleQuiALeClavier())
                If c IsNot Nothing Then qui = c.Name & " (" & c.GetType().Name & ")"
            Catch ex As Exception
            End Try
            DiagLog("Touche " & touche.ToString() & " : non recue par la fenetre (clavier sur : " & qui & ") -> action faite quand meme")
            _toucheRecueLe = maintenant
            FaireLaTouche(touche)
        Next
    End Sub

    ''' <summary>L'action d'une des touches N, P, M, H (par la voie normale ou rattrapee par la veille du clavier).</summary>
    Private Sub FaireLaTouche(touche As Keys)
        Select Case touche
            Case Keys.N : SuivantSansFermerLesPip()
            Case Keys.P : SuivantEtFermerLePip()
            Case Keys.M
                DiagLog("Touche M : pochette de la musique de nouveau a l'ecran")
                CarteDeezer.Remontrer()
            Case Keys.H
                FormAideTouches.Basculer(Me)
            Case Keys.S
                ' Source de musique suivante parmi celles cochees (onglet Audio) : Fichiers MP3, Radio, Deezer.
                If Not Music Then
                    DiagLog("Touche S : la musique est coupee, source inchangee")
                ElseIf Not AlternanceMusique.PasserALaSourceSuivante("touche S : source") Then
                    DiagLog("Touche S : une seule source de musique cochee (ou aucune autre utilisable), source inchangee")
                End If
        End Select
    End Sub

    ''' <summary>Vrai si le premier plan est une fenetre de Slide ou le clavier ne sert pas a saisir du texte : la fenetre
    ''' principale, un PIP, les cartes et horloges. Avant : la fenetre principale SEULEMENT, donc N ne marchait pas quand
    ''' le premier plan etait le PIP ou un calque (reglages et fenetres de saisie ouverts : jamais).</summary>
    Private Function SlideAvecLeClavier() As Boolean
        Dim premierPlan As IntPtr = FenetreAuPremierPlan()
        If premierPlan = IntPtr.Zero Then Return False
        If premierPlan = Me.Handle Then Return True
        Dim pid As UInteger = 0
        GetWindowThreadProcessId(premierPlan, pid)
        If pid <> CUInt(Environment.ProcessId) Then Return False
        Try
            For Each fenetre As Form In Application.OpenForms.Cast(Of Form)().ToList()
                If Not fenetre.Visible OrElse fenetre Is Me Then Continue For
                Dim nom As String = fenetre.GetType().Name
                ' les fenetres ou l'on saisit du texte ou ou l'on regle quelque chose gardent le clavier
                If nom = "Form4" OrElse nom = "Form6" OrElse nom = "Form8" OrElse nom = "Form11" OrElse nom = "Form14" OrElse
                   nom.StartsWith("FormMiseAJour") OrElse nom.StartsWith("FormProposer") OrElse nom = "FormChoixHasard" OrElse
                   nom = "FormCoherence" OrElse nom = "FormTelechargements" OrElse nom = "FormEpisodes" OrElse nom = "Form2" Then Return False
            Next
        Catch ex As Exception
            Return False
        End Try
        Return True
    End Function

    ''' <summary>
    ''' Touche P : element suivant ET fermeture du PIP. Avant, N appelait
    ''' EndAndContinue(False) ("ne pas fermer le PIP"), et un PIP "Rester affiche" n'etait ferme
    ''' par aucune des deux (EndAndContinue sert aussi aux evenements programmes, que ce PIP doit
    ''' traverser : on n'y touche donc pas). Ici, c'est une demande de l'utilisateur : on ferme.
    ''' Le PIP BOOT revient a la prochaine relance "comme au demarrage" (fin d'un evenement).
    ''' </summary>
    ''' <remarks>02/10, demande de Jacob : s'il n'y a PLUS de PIP (ferme par un premier appui), un
    ''' second appui sur P le relance - les taches BOOT de la minuterie sont rejouees, comme au
    ''' demarrage de Slide - sans passer a l'element suivant.</remarks>
    Private Sub SuivantEtFermerLePip()
        If PipsActifs() = 0 Then
            DiagLog("Touche P : aucun PIP a l'ecran -> relance des taches de demarrage (PIP)")
            RelancerCommeAuDemarrage()
            Exit Sub
        End If
        ArreterTousLesPip()               ' tous les PIP, "Rester affiche" ou non (5 s au plus)
        EndAndContinue()                  ' element suivant ; FlagExit ferme un PIP non persistant
    End Sub


    ' (L'ancien gestionnaire du clic droit sur le lecteur Windows Media a ete retire avec lui :
    '  la musique passe par VLC, voir MusiqueVLC.vb.)

    Private Sub AscendantToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles AscendantToolStripMenuItem1.Click

        Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
        NumPhoto = 0
        CaseAcocherVide(1)


    End Sub


    Private Sub PauseToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles PauseToolStripMenuItem.Click

        If PauseToolStripMenuItem.Checked = True Then
            FlagStop = False
            PauseToolStripMenuItem.Checked = False
        Else
            FlagStop = True
            PauseToolStripMenuItem.Checked = True
        End If



    End Sub

    Private Sub DescendantToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles DescendantToolStripMenuItem2.Click
        Form2.DataGridView1.Sort(Form2.DataGridView1.Columns(2), System.ComponentModel.ListSortDirection.Descending)
        NumPhoto = 0
        CaseAcocherVide(1)


    End Sub

    Private Sub AscendantToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles AscendantToolStripMenuItem2.Click
        Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Ascending)
        NumVideo = 0
        CaseAcocherVide(2)

    End Sub

    Private Sub DescendantToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles DescendantToolStripMenuItem1.Click

        Form2.DataGridView2.Sort(Form2.DataGridView2.Columns(2), System.ComponentModel.ListSortDirection.Descending)
        NumVideo = 0

        CaseAcocherVide(2)

    End Sub

    Private Sub FormatPhoto169ToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles FormatPhoto169ToolStripMenuItem.Click
        If FormatPhoto169ToolStripMenuItem.Checked = True Then
            FormatPhoto169 = False
            FormatPhoto169ToolStripMenuItem.Checked = False
            My.Settings.FormatPhoto169 = False : My.Settings.Save()
        Else
            FormatPhoto169 = True
            FormatPhoto169ToolStripMenuItem.Checked = True
            My.Settings.FormatPhoto169 = True : My.Settings.Save()
        End If
    End Sub

    Private Sub OptionToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OptionToolStripMenuItem.Click
        Form4.TabControl1.SelectedIndex = 5
        Form4.StartPosition = FormStartPosition.CenterScreen
        Form4.ShowDialog()
    End Sub


    Private Sub VideoAléatoireToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles VideoAléatoireToolStripMenuItem.Click
        If VideoAleatoire = True Then
            VideoAleatoire = False
            VideoAléatoireToolStripMenuItem.Checked = False
            My.Settings.VideoAleatoire = False : My.Settings.Save()
        Else
            VideoAleatoire = True
            VideoAléatoireToolStripMenuItem.Checked = True
            My.Settings.VideoAleatoire = True : My.Settings.Save()
        End If
    End Sub
    Private Sub Form3_Resize(sender As Object, e As EventArgs) Handles MyBase.Resize
        ' Reduite dans la barre des taches : zone d'image de hauteur 0. Imposer alors la largeur
        ' (0 x 1,78) cassait la fenetre a la restauration : Windows la rendait sans barre de titre
        ' ni bordure (zone d'image = fenetre entiere), et les PIP, la montre et la pastille de la
        ' normalisation se recalaient a cote.
        If Me.WindowState = FormWindowState.Minimized OrElse Me.ClientSize.Height <= 0 Then Exit Sub
        ' Une transition video en cours (photographie de l'ancienne image, voile) est a la taille d'AVANT : retiree tout de suite (22/10/2026 : bande noire a gauche apres un
        ' passage du plein ecran a la fenetre, vue sur l'autre PC).
        FormTransitionVideo.Masquer(True)
        Me.PictureBox1.Left = 0 : Me.PictureBox1.Top = 0
        Me.VlcControl1.Left = 0 : Me.VlcControl1.Top = 0
        Me.Width = Me.ClientSize.Height * 1.7779
        Me.PictureBox1.Height = Me.ClientSize.Height
        Me.PictureBox1.Width = Me.ClientSize.Height * 1.7779 'Me.Width
        Me.VlcControl1.Height = Me.ClientSize.Height
        Me.VlcControl1.Width = Me.Width
        ' Le lecteur cache (video suivante prechargee) garde lui aussi la taille de l'image.
        ' (Pas encore cree pendant la construction de la fenetre, ou elle se redimensionne deja.)
        ' (Le lecteur de prechargement reste reduit a son point : voir PrechargementVideo.)
        Me.VlcControl2.Height = Me.ClientSize.Height
        Me.VlcControl2.Width = Me.Width
        Me.Refresh()
    End Sub

    Private Sub Form3_ResizeEnd(sender As Object, e As EventArgs) Handles Me.ResizeEnd

        Me.PictureBox1.Height = Me.ClientSize.Height
        Me.PictureBox1.Width = Me.ClientSize.Height * 1.7779 'Me.Width
        Me.VlcControl1.Height = Me.ClientSize.Height
        Me.VlcControl1.Width = Me.Width
        Me.VlcControl2.Height = Me.ClientSize.Height
        Me.VlcControl2.Width = Me.Width
        Me.Refresh()
        Application.DoEvents()
        My.Settings.ScreenSizeH = Me.Height : My.Settings.ScreenSizeW = Me.Width
        RetenirLaPlace()                    ' la place aussi : ResizeEnd suit un deplacement comme un redimensionnement
        My.Settings.Save()
        'Debug.Print("me.with:" & Me.Width & " settingW:" & My.Settings.ScreenSizeW)


    End Sub




    Private Sub ClassicToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ClassicToolStripMenuItem.Click

        ClassicToolStripMenuItem.Checked = Not ClassicToolStripMenuItem.Checked
        PhotoAléatoireToolStripMenuItem.Checked = True
        SéquentielToolStripMenuItem.Checked = False
        OffToolStripMenuItem.Checked = False
        PhotoAleatoire = 1
        My.Settings.PhotoAleatoire = PhotoAleatoire : My.Settings.Save()
    End Sub

    Private Sub SéquentielToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles SéquentielToolStripMenuItem.Click
        SéquentielToolStripMenuItem.Checked = Not SéquentielToolStripMenuItem.Checked
        PhotoAléatoireToolStripMenuItem.Checked = True
        ClassicToolStripMenuItem.Checked = False
        OffToolStripMenuItem.Checked = False
        PhotoAleatoire = 2
        My.Settings.PhotoAleatoire = PhotoAleatoire : My.Settings.Save()
    End Sub

    Private Sub OffToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles OffToolStripMenuItem.Click

        OffToolStripMenuItem.Checked = Not OffToolStripMenuItem.Checked
        PhotoAléatoireToolStripMenuItem.Checked = False
        PhotoAleatoire = 0
        My.Settings.PhotoAleatoire = PhotoAleatoire : My.Settings.Save()
    End Sub



    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick

        If FullScreen = True Then
            trouve = 0
            If ContextMenuStrip1.Visible = True Then trouve = 1
            If Form2.Visible = True Then trouve = 1
            If Form4.Visible = True Then trouve = 1
            If Form5.Visible = True Then trouve = 1
            If Form10.Visible = True Then trouve = 1
            If trouve = 0 Then CacherSouris()

        End If

        'If FullScreen = True And Me.ContextMenuStrip1.Visible = False And Form2.Visible = False And Form4.Visible = False And Form5.Visible = False Then CacherSouris()

        'Call afficheProgramme()



        'If AfficheHeureDeShabbat <> True Then Exit Sub
        If Shabbat.ToutLesXminutes = 0 Then Exit Sub

        Shabbat.counter1 = Shabbat.counter1 + 0.5

        If Shabbat.counter1 >= Shabbat.ToutLesXminutes Then
            'Call afficheProgramme()
            Shabbat.counter1 = 0
            Call HeureDeShabbat(Shabbat.Ville)


            TextBox2.Text = "Shabbat à " & Shabbat.Ville & " Entrée à " & Shabbat.Debut & " Sortie à " & Shabbat.Fin
            TextBox2.Font = New Font("Arial", (ClientSize.Height / 30) + 1)
            Dim g As Graphics = TextBox2.CreateGraphics()
            TextBox2.Width = CInt(g.MeasureString(TextBox2.Text, TextBox2.Font).Width)
            TextBox2.Height = CInt(g.MeasureString(TextBox2.Text, TextBox2.Font).Height)
            TextBox2.Top = ClientSize.Height - TextBox2.Height - TextBox1.Height

            TextBox3.Font = New Font("Arial", ClientSize.Height / 30)
            TextBox3.Width = Me.Width
            TextBox3.Top = TextBox2.Top
            TextBox3.Left = 0

            TextBox3.BringToFront()
            TextBox2.BringToFront()
            Shabbat.position = Me.Width
            Timer2.Start()
            'TextBox3.Visible = True
            TextBox2.Visible = True

        End If


    End Sub

    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick

        j = -TextBox2.Width
        If Shabbat.position < j Then TextBox2.Visible = False : TextBox3.Visible = False : TextBox3.SendToBack() : Application.DoEvents() : Timer2.Stop()

        Shabbat.position = Shabbat.position - 2
        TextBox2.Left = Shabbat.position
        'Application.DoEvents()
        'TextBox2.Refresh()


    End Sub

    Private Sub ToolStripMenuItem1_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.Click


        'Call PreviewTV(1, VolumeTVParDefault)

    End Sub



    Private Sub ToolStripMenuItem1_MouseHover(sender As Object, e As EventArgs) Handles ToolStripMenuItem1.MouseHover

        Dim Chanel(50) As String
        For i = 0 To 50
            Chanel(i) = ChannelID(i, 1) & " (" & Format(i, "00") & ")"
        Next
        Chanel(0) = "Chaine OFF"
        ToolStripComboBox1.Items.AddRange(Chanel)
        ToolStripComboBox1.SelectedIndex = 0
    End Sub


    Private Sub ToolStripComboBox1_TextChanged(sender As Object, e As EventArgs) Handles ToolStripComboBox1.TextChanged
        If ToolStripComboBox1.SelectedIndex = 0 Then Exit Sub

        Call PreviewTV(ToolStripComboBox1.SelectedIndex, VolumeTVParDefault)
    End Sub

    Private Sub AxWindowsMediaPlayer1_Enter(sender As Object, e As EventArgs)

    End Sub

    Private Sub ToolStripMenuItem2_Click(sender As Object, e As EventArgs) Handles ToolStripMenuItem2.Click

        ToolStripMenuItem2.Checked = Not ToolStripMenuItem2.Checked

        If ToolStripMenuItem2.Checked = True Then
            If Diffusion.mode <> "PHOTO" Then GroupBox1.Visible = True

        Else

            GroupBox1.Visible = False
        End If

    End Sub

    Private Sub AfficheGrilleProgrammesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles AfficheGrilleProgrammesToolStripMenuItem.Click
        FormDeSlide("x", h)
        Exit Sub
        Call AfficheProgramme(DatabasePath, "TOTO", 0)
        Call AfficheProgramme(DatabasePath, "TOTO", 1)
    End Sub

    Private Sub AfficheGrilleProgrammes1ToolStripMenuItem_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub QueLesPhotosToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles QueLesPhotosToolStripMenuItem.Click

        QueLesPhotosToolStripMenuItem.Checked = Not QueLesPhotosToolStripMenuItem.Checked
        If QueLesPhotosToolStripMenuItem.Checked = True Then
            QueLesSlideVideosToolStripMenuItem.Checked = False
            onlyphotos = True
        Else
            onlyphotos = False
        End If
        My.Settings.OnlyPhotos = True : My.Settings.Save()
    End Sub

    Private Sub QueLesSlideVideosToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles QueLesSlideVideosToolStripMenuItem.Click
        QueLesSlideVideosToolStripMenuItem.Checked = Not QueLesSlideVideosToolStripMenuItem.Checked
        If QueLesSlideVideosToolStripMenuItem.Checked = True Then
            QueLesPhotosToolStripMenuItem.Checked = False
            onlyvideos = True
        Else
            onlyvideos = False
        End If
        My.Settings.OnlyVideos = True : My.Settings.Save()
    End Sub

    Private Sub CloseAllPlayerToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CloseAllPlayerToolStripMenuItem.Click
        ArreterTousLesPip()            ' "Close All Player" arrete tous les PIP, meme "Rester affiche"
        ClosePlayer()

    End Sub

    Private Sub TesterTouteLesVideosToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles TesterTouteLesVideosToolStripMenuItem.Click
        AfficheVideoForTEST()

    End Sub

    Private Sub MajCommentairesToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles MajCommentairesToolStripMenuItem.Click

        Form10.Show()
        Form10.TopMost = True
    End Sub


    Private Sub Panel2_Paint(sender As Object, e As PaintEventArgs)

    End Sub

    Private Sub Panel2_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Form3_MouseDown(sender As Object, e As MouseEventArgs) Handles Me.MouseDown
        'If e.Button = 2 Then Me.ContextMenuStrip.Show(Cursor.Position)

    End Sub


    Private Sub VlcControl1_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcLecteurA.VlcLibDirectoryNeeded, VlcLecteurB.VlcLibDirectoryNeeded

        e.VlcLibDirectory = New DirectoryInfo(VLCpath)

        OptionsDuLecteurVlc(sender)
        ' Lecteurs video du diaporama : 4 fils de decodage au lieu d'un par coeur du processeur.
        ' VLC reserve des images d'avance pour chaque fil : sur une video 4K, 36 images au lieu de
        ' 48, soit 1,26 Go au lieu de 1,7 Go (mesure du 06/10, aucune image perdue). Sans effet sur
        ' la fluidite quand la carte graphique decode, ce qui est le cas des videos de telephone.
        Try
            Dim lecteur As Vlc.DotNet.Forms.VlcControl = TryCast(sender, Vlc.DotNet.Forms.VlcControl)
            If lecteur IsNot Nothing AndAlso lecteur.VlcMediaplayerOptions IsNot Nothing AndAlso
               Not lecteur.VlcMediaplayerOptions.Contains("--avcodec-threads=4") Then
                lecteur.VlcMediaplayerOptions = lecteur.VlcMediaplayerOptions.Concat({"--avcodec-threads=4"}).ToArray()
            End If
        Catch ex As Exception
        End Try

        'Dim aP As String
        'aP = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "VideoLAN\VLC")

        'If Not File.Exists(Path.Combine(aP, "libvlc.dll")) Then
        'e.VlcLibDirectory = New DirectoryInfo(AppPath & "\data\VLC")
        'Else
        'e.VlcLibDirectory = New DirectoryInfo(aP)
        'End If


    End Sub


    Private Sub VlcControl1_MouseClick(sender As Object, e As MouseEventArgs) Handles VlcLecteurA.MouseClick, VlcLecteurB.MouseClick


    End Sub




    Private Sub ProgressBar1_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub ProgressBar1_MouseDown(sender As Object, e As MouseEventArgs)

    End Sub

    Private Sub TrackBar2_Scroll(sender As Object, e As EventArgs) Handles TrackBar2.Scroll
        Label1.Text = TrackBar2.Value
        SetVol(TrackBar2.Value)
    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll
        'm = 60
        m = 1

        Label3.Text = TimeSpan.FromSeconds(CDbl(TrackBar1.Value * m)).ToString("hh\:mm\:ss")
        VlcControl1.Time = TrackBar1.Value * 1000 * m

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        If VlcControl1.IsPlaying Then
            VlcControl1.Pause()
            Button2.Text = ">"
        Else
            VlcControl1.Play()
            Button2.Text = "II"
        End If


    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        If VlcControl1.Audio.IsMute = False Then
            VlcControl1.Audio.IsMute = True
            Button3.Text = "Vo"
        Else
            VlcControl1.Audio.IsMute = False
            Button3.Text = "Vm"
        End If



    End Sub

    Private Sub Timer3_Tick(sender As Object, e As EventArgs) Handles Timer3.Tick
        ' Compteur : tant qu'une tache bloquante (film, TV, ecran noir...) n'a pas rendu la main,
        ' RelanceDeNuit sait qu'il ne faut pas relancer Slide.
        Module1.MinuterieEnCours += 1
        Try
            Call Module1.Minuterie()
        Finally
            Module1.MinuterieEnCours -= 1
        End Try
    End Sub

    Private Sub VlcControl2_Click(sender As Object, e As EventArgs) Handles VlcControl2.Click


    End Sub

    ''' <summary>Le lecteur de la musique (VLC) utilise le meme dossier VLC que les autres.</summary>
    Private Sub VlcMusique_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcMusique.VlcLibDirectoryNeeded
        e.VlcLibDirectory = New DirectoryInfo(VLCpath)
        OptionsDuLecteurVlc(sender)
    End Sub

    Private Sub VlcControl2_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcControl2.VlcLibDirectoryNeeded

        e.VlcLibDirectory = New DirectoryInfo(VLCpath)

        OptionsDuLecteurVlc(sender)

        'Dim aP As String
        'aP = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "VideoLAN\VLC")

        'If Not File.Exists(Path.Combine(aP, "libvlc.dll")) Then
        'e.VlcLibDirectory = New DirectoryInfo(AppPath & "\data\VLC")
        'Else
        'e.VlcLibDirectory = New DirectoryInfo(aP)
        'End If

    End Sub

    Private Sub Timer4_Tick(sender As Object, e As EventArgs) Handles Timer4.Tick
        If TaskIsPlayingState <> TaskIsPlaying Then
            TaskIsPlayingState = TaskIsPlaying
            retablieTousLesVolumes()
            Timer4.Stop()
        End If
    End Sub

    Private Sub VlcControl1_Click(sender As Object, e As EventArgs) Handles VlcLecteurA.Click, VlcLecteurB.Click

    End Sub

    Private Sub Timer5_Tick(sender As Object, e As EventArgs) Handles Timer5.Tick
        trouve = 0
        If WindowExist("prime video") = True Then trouve = 1
        If WindowExist("netflix") = True Then trouve = 1

        If trouve = 0 Then
            FlagStop = False
            Application.DoEvents()
            Timer5.Stop()
        End If
    End Sub

    Private Sub ChargerLaMinuterieDuProfileToolStripMenuItem_Click(sender As Object, e As EventArgs)
        LoadMinuterieFromSettings()

    End Sub

    Private Sub Timer7_Tick(sender As Object, e As EventArgs) Handles Timer7.Tick
        'Call GestionControlAdistanceSub
    End Sub

    Private Sub Timer6_Tick(sender As Object, e As EventArgs) Handles Timer6.Tick

    End Sub

    Private Sub ModifierDateParExifDateToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ModifierDateParExifDateToolStripMenuItem.Click
        Call ModifDateParExifDate

    End Sub

    Private Sub Form3_Move(sender As Object, e As EventArgs) Handles Me.Move

    End Sub
End Class



