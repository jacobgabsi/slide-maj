Imports Vlc.DotNet.Forms
Imports System.IO
Imports System.ComponentModel

''' <summary>
''' Fenetre PIP (petite video posee dans un coin de la fenetre de Slide).
'''
''' PLUSIEURS PIP peuvent etre affiches en meme temps (ex. TV en bas a droite + film en bas a
''' gauche, programmes au BOOT). Chaque lecteur PIP tourne dans son propre thread et y utilise
''' "Form12", l'instance par defaut de CE thread : chaque PIP a donc sa propre fenetre. Avant, la
''' place (coin, zone, taille) et "le PIP affiche" etaient des donnees PARTAGEES : seul le dernier
''' PIP suivait la fenetre de Slide et se remettait au premier plan, l'autre retombait derriere
''' Slide (invisible). Place et coin sont maintenant propres a chaque fenetre ; la liste _pips
''' recense les PIP a l'ecran, et les operations communes (suivre la fenetre, premier plan,
''' legende) s'appliquent a tous.
''' </summary>
Public Class Form12

    Private Sub VlcControl1_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcControl1.VlcLibDirectoryNeeded
        e.VlcLibDirectory = New DirectoryInfo(VLCpath)
        OptionsDuLecteurVlc(sender, PipDuFilEstUneTV())
    End Sub

    ''' <summary>
    ''' La souris et le clavier ne sont PAS pour VLC, et il faut le lui dire AVANT la premiere
    ''' lecture : VLC ne lit ce reglage qu'a la creation de sa fenetre video (il la cree alors
    ''' "desactivee", et la souris arrive au PIP). Les lecteurs lancent la video avant d'afficher
    ''' le PIP : le meme reglage fait au chargement de la fenetre (Form12_Load) arrivait trop tard,
    ''' la fenetre video de VLC captait toute la souris (mesure du 06/10 : aucun geste ne parvenait
    ''' au PIP, la taille a la souris ne marchait pas). Passer "--no-mouse-events" en option ne
    ''' sert a rien : le lecteur a sa propre valeur, qui l'emporte.
    ''' </summary>
    Public Sub New()
        InitializeComponent()
        Try
            VlcControl1.Video.IsMouseInputEnabled = False
            VlcControl1.Video.IsKeyInputEnabled = False
        Catch ex As Exception
            DiagLog("PIP : souris de VLC non desactivee a la creation (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Un PIP ne prend jamais le clavier (il le volait a Slide : Echap, N, P ne
    ''' repondaient plus) et n'apparait pas dans la barre des taches.</summary>
    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or CalqueWin32.WS_EX_NOACTIVATE Or CalqueWin32.WS_EX_TOOLWINDOW
            Return cp
        End Get
    End Property

    Private Sub Form12_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ' Les lecteurs affichent le PIP (Show) PUIS le posent dans son coin (PlacerLePip, qui
        ' attend jusqu'a 3 s le format de la video) : entre les deux, il apparaissait a la place
        ' par defaut de Windows, hors de la fenetre de Slide. Il s'ouvre donc hors de l'ecran ;
        ' PoserDansLeCoin l'amene directement a sa place.
        Dim bureau As Rectangle = SystemInformation.VirtualScreen
        Me.Location = New Point(bureau.Left - Me.Width - 100, bureau.Top - Me.Height - 100)
        ' (Pas de ShowInTaskbar = False ici : le changer pendant le chargement recree la fenetre, et
        ' le PIP retombait derriere Slide. WS_EX_TOOLWINDOW, voir CreateParams, le masque deja.)
        'pour que vlc marche avec contextmenustrip
        VlcControl1.Video.IsMouseInputEnabled = False
        VlcControl1.Video.IsKeyInputEnabled = False
        PreparerSouris()
        Application.DoEvents()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Taille a la souris (demande de Jacob, 06/10) : molette sur le PIP, ou cliquer-glisser vers
    ' l'interieur de l'image pour l'agrandir ; il reste accroche a son coin. Meme geste que pour
    ' la legende et les bandes (TailleALaSouris). La souris arrive sur le lecteur video, qui
    ' couvre tout le PIP. Une fois le geste termine, la taille est enregistree dans la tache de
    ' la minuterie qui a lance ce PIP (voir Module2.TaillePipChoisieALaSouris).
    ' ------------------------------------------------------------------------------------------

    Public Const PourcentMini As Integer = 5
    Public Const PourcentMaxi As Integer = 100

    Private _part As Double = 0.25
    Private _formatVideo As Double
    Private _souris As TailleALaSouris

    ''' <summary>Taille du PIP en % de l'image de Slide (le "PipSize" de sa tache).</summary>
    Public ReadOnly Property PourcentTaille As Integer
        Get
            Return CInt(Math.Round(_part * 100))
        End Get
    End Property

    Private Sub PreparerSouris()
        If _souris IsNot Nothing Then Exit Sub
        ' Pas de menu au clic droit : celui de Slide appartient a un autre thread.
        _souris = New TailleALaSouris(Me, Function() PourcentTaille, AddressOf DefinirPourcentEtLesAutres,
                                      Function() Nothing, poidsHorizontal:=0.25, surface:=VlcControl1)
        AddHandler _souris.TailleChoisie, Sub(pourcent As Integer)
                                              TaillePipChoisieALaSouris(Me, pourcent)
                                              If TaillesLiees Then TousALaMemeTaille(pourcent)          ' (tailles liees : les autres PIP suivent)
                                          End Sub
        _souris.IgnorerLAppui = Function() Not CtrlEnfonce()          ' sans Ctrl : cliquer-glisser DEPLACE le PIP ; avec Ctrl : il le redimensionne
        AddHandler VlcControl1.MouseDown, AddressOf PipSouris_Down
        AddHandler VlcControl1.MouseMove, AddressOf PipSouris_Move
        AddHandler VlcControl1.MouseUp, AddressOf PipSouris_Up
        AddHandler VlcControl1.MouseDoubleClick, AddressOf PipSouris_DoubleClic
        PreparerLeMenu()
        OrienterLaSouris()
    End Sub

    ''' <summary>Tirer vers l'interieur de l'image agrandit : le sens depend du coin du PIP.</summary>
    Private Sub OrienterLaSouris()
        If _souris Is Nothing Then Exit Sub
        _souris.SensVertical = If(_coin.StartsWith("H"), -1, 1)          ' (au milieu : comme en bas)
        _souris.SensHorizontal = If(_coin.EndsWith("droite"), 1, -1)
        ' Fleche en diagonale, dans le sens du coin libre du PIP.
        Dim diagonale As Cursor = Cursors.SizeAll          '' cliquer-glisser deplace le PIP (Ctrl : redimensionne)
        If VlcControl1.Cursor IsNot diagonale Then VlcControl1.Cursor = diagonale
    End Sub

    ''' <summary>Taille a la molette ou au glisser : ce PIP, et les autres PIP aussi si les tailles sont liees (en direct).</summary>
    Public Sub DefinirPourcentEtLesAutres(pourcent As Integer)
        DefinirPourcent(pourcent)
        If TaillesLiees Then PropagerLaTaille(pourcent)
    End Sub

    Private _dernierPropage As Integer = -1

    ''' <summary>Les autres PIP prennent cette taille tout de suite (chacun dans son thread), une fois par valeur.</summary>
    Private Sub PropagerLaTaille(pourcent As Integer)
        ' "Meme taille" = meme LARGEUR que ce PIP (23/10/2026 : le meme pourcentage ne donnait pas des PIP de meme largeur, un 4/3 etant limite par sa hauteur) ;
        ' la hauteur suit le format de chaque video.
        Dim largeur As Integer = Me.Width
        If largeur = _dernierPropage Then Exit Sub
        _dernierPropage = largeur
        For Each pip As Form12 In PipsAffiches()
            If ReferenceEquals(pip, Me) Then Continue For
            Dim p As Form12 = pip
            Try
                p.BeginInvoke(New Action(Sub() If Not p.IsDisposed Then p.TailleSurLargeur(largeur)))
            Catch ex As Exception
            End Try
        Next
    End Sub

    ''' <summary>Donne a ce PIP la largeur demandee (en pixels), au format de sa video, dans la limite de l'image de Slide. Sur le thread du PIP.</summary>
    Public Sub TailleSurLargeur(largeur As Integer)
        If _zoneFenetre.IsEmpty OrElse largeur <= 0 Then Exit Sub
        Dim format As Double = If(_formatVideo > 0, _formatVideo, _zoneFenetre.Width / CDbl(Math.Max(1, _zoneFenetre.Height)))
        Dim part As Double = Math.Max(largeur / CDbl(_zoneFenetre.Width), largeur / (format * _zoneFenetre.Height))
        part = Math.Max(PourcentMini / 100.0, Math.Min(PourcentMaxi / 100.0, part))
        PoserDansLeCoin(_zoneFenetre, _coin, _formatVideo, part)
    End Sub

    ''' <summary>Change la taille en direct (sur le thread du PIP).</summary>
    Public Sub DefinirPourcent(pourcent As Integer)
        pourcent = Math.Max(PourcentMini, Math.Min(PourcentMaxi, pourcent))
        If pourcent = PourcentTaille OrElse _zoneFenetre.IsEmpty Then Exit Sub
        PoserDansLeCoin(_zoneFenetre, _coin, _formatVideo, pourcent / 100.0)
    End Sub

    ''' <summary>Reaffirme le premier plan toutes les 2 s (voir Form12_Shown).</summary>
    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick
        If Not Me.Visible OrElse Me.IsDisposed OrElse Not EstUnPipAffiche(Me) Then Exit Sub
        Ranger()
    End Sub

    ''' <summary>Met ce PIP a sa place dans l'empilement des fenetres (sur son propre thread) :
    '''  - menu ouvert : plus de premier plan, le menu passe devant ;
    '''  - reglages ouverts ET devant l'image de Slide : juste derriere eux ;
    '''  - sinon au premier plan si l'image de Slide est la fenetre active, ou juste au-dessus de
    '''    l'image si une autre fenetre est active (autre programme ou autre fenetre de Slide).
    ''' Des reglages restes ouverts DERRIERE l'image ne comptent pas : se ranger derriere eux
    ''' cachait le PIP sous l'image (PIP "disparu" au retour d'un film lance depuis la minuterie).</summary>
    Private _derniereAlerteDerriere As Long

    Private Sub Ranger()
        Try
            Dim ceder As Boolean
            Dim derriere As IntPtr = IntPtr.Zero
            Dim menu As Boolean
            SyncLock _raisonsDeCeder
                For Each raison As KeyValuePair(Of String, IntPtr) In _raisonsDeCeder
                    If raison.Value = IntPtr.Zero Then
                        menu = True
                    ElseIf CalqueWin32.EstDevantSlide(raison.Value) Then
                        derriere = raison.Value
                    End If
                Next
            End SyncLock
            ceder = menu OrElse derriere <> IntPtr.Zero
            If Not CalqueWin32.ImageDeSlideActive() Then
                ' Une autre fenetre que l'image de Slide est active (autre programme, ou reglages /
                ' boite "Ouvrir" / message de Slide) : le PIP passe derriere elle, avec l'image, DANS
                ' TOUS LES CAS (menu ou reglages ouverts compris : "retirer le premier plan" le
                ' posait en tete des fenetres normales, donc devant l'autre programme).
                CalqueWin32.PlacerAuDessusDeSlide(Me)
            ElseIf Not ceder Then
                CalqueWin32.DefinirPremierPlanSeul(Me, True)
            ElseIf derriere <> IntPtr.Zero Then
                CalqueWin32.PlacerDerriere(Me, derriere)
            Else
                CalqueWin32.DefinirPremierPlanSeul(Me, False)
            End If
            ' Garde-fou (16/10/2026 : "quand je clique droit, certains PIP disparaissent, puis reviennent") : un PIP qui s'est retrouve
            ' DERRIERE l'image de Slide est invisible ; il est remis juste devant elle, et l'evenement est note dans le journal.
            If Me.Visible AndAlso Not CalqueWin32.EstAuDessusDeLImage(Me.Handle) Then
                If Environment.TickCount64 - _derniereAlerteDerriere > 5000 Then
                    _derniereAlerteDerriere = Environment.TickCount64
                    DiagLog("PIP : passe DERRIERE l'image de Slide (" & _coin & If(ceder, ", menu ou reglages ouverts", "") & ") -> remis devant")
                End If
                CalqueWin32.PlacerJusteAuDessus(Me.Handle, CalqueWin32.FenetreDeSlide)
            End If
        Catch ex As Exception
            ' un reglage d'affichage ne doit jamais faire tomber la lecture
        End Try
    End Sub

    ''' <summary>
    ''' Au lancement de Slide (tache BOOT), le PIP recevait bien son premier plan puis se
    ''' retrouvait TOUT EN BAS de la pile des fenetres, derriere Slide et le bureau (fenetre
    ''' visible mais invisible a l'ecran) : il n'apparaissait qu'apres un clic. Le minuteur remet
    ''' le premier plan regulierement, sur le thread du PIP (ses messages passent par les DoEvents
    ''' du lecteur) ; DefinirPremierPlanSeul le retire puis le remet si Windows l'ignore.
    ''' </summary>
    Private Sub Form12_Shown(sender As Object, e As EventArgs) Handles Me.Shown
        ' Toutes les 0,5 s (au lieu de 2) : le PIP suit vite Slide quand un autre programme passe
        ' devant, ou quand Slide revient au premier plan.
        Timer1.Interval = 500
        Timer1.Start()
        ' Lance alors que la fenetre de Slide est reduite ou cachee : il attend qu'elle revienne.
        If _fenetreMasquee Then BeginInvoke(New Action(Sub() MasquerAvecLaFenetre(True)))
    End Sub

    Private Sub Form12_Closed(sender As Object, e As EventArgs) Handles Me.Closed
        Timer1.Stop()
        Dim etaitAffiche As Boolean
        SyncLock _pips
            etaitAffiche = _pips.Remove(Me)
        End SyncLock
        _zoneFenetre = Rectangle.Empty
        _zoneDepart = Rectangle.Empty
        If etaitAffiche Then
            EcarterDeLaLegende()                           ' les autres PIP recalent leur rangee
            RaiseEvent PlaceChangee()                      ' l'etiquette du bas reprend sa place
        End If
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Les PIP a l'ecran
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les PIP visibles (chacun dans son thread).</summary>
    Private Shared ReadOnly _pips As New List(Of Form12)

    Private Shared Function PipsAffiches() As List(Of Form12)
        SyncLock _pips
            Return _pips.Where(Function(p) p IsNot Nothing AndAlso Not p.IsDisposed AndAlso (p.Visible OrElse p._masqueAvecLaFenetre)).ToList()
        End SyncLock
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Les PIP ne sont visibles QUE si la fenetre de Slide (Form3) l'est (demande de Jacob) :
    ' fenetre reduite ou cachee -> PIP caches (le lecteur continue), fenetre revenue -> PIP
    ' reaffiches a leur place. Un PIP cache ainsi reste dans la liste des PIP a l'ecran.
    ' ------------------------------------------------------------------------------------------

    Private _masqueAvecLaFenetre As Boolean
    Private Shared _fenetreMasquee As Boolean

    ''' <summary>Etat de la fenetre de Slide a appliquer aux PIP (thread de l'interface).</summary>
    Private Shared Sub SuivreLaVisibilite(fenetre As Form)
        Dim masquee As Boolean
        Try
            masquee = fenetre Is Nothing OrElse fenetre.IsDisposed OrElse Not fenetre.Visible OrElse
                      fenetre.WindowState = FormWindowState.Minimized
        Catch ex As Exception
            Exit Sub
        End Try
        If masquee = _fenetreMasquee Then Exit Sub
        _fenetreMasquee = masquee
        For Each pip As Form12 In PipsAffiches()
            Dim p As Form12 = pip
            Try
                p.BeginInvoke(New Action(Sub() p.MasquerAvecLaFenetre(masquee)))
            Catch ex As Exception
            End Try
        Next
    End Sub

    ''' <summary>Cache ou reaffiche ce PIP (sur son propre thread).</summary>
    Private Sub MasquerAvecLaFenetre(masquer As Boolean)
        If Me.IsDisposed Then Exit Sub
        Try
            If masquer Then
                If Not Me.Visible Then Exit Sub
                _masqueAvecLaFenetre = True
                Me.Hide()
            ElseIf _masqueAvecLaFenetre Then
                _masqueAvecLaFenetre = False
                Me.Show()                                   ' sans prendre le clavier (ShowWithoutActivation)
                CalqueWin32.DefinirPremierPlanSeul(Me, True)
            End If
        Catch ex As Exception
            ' un reglage d'affichage ne doit jamais faire tomber la lecture
        End Try
    End Sub

    Private Shared Function EstUnPipAffiche(pip As Form12) As Boolean
        SyncLock _pips
            Return _pips.Contains(pip)
        End SyncLock
    End Function

    ''' <summary>Vrai si au moins un PIP est a l'ecran. Membre partage : l'interroger ne cree pas
    ''' de fenetre (l'instance par defaut depend du thread).</summary>
    Public Shared ReadOnly Property EstAffiche As Boolean
        Get
            Return PipsAffiches().Count > 0
        End Get
    End Property

    ''' <summary>
    ''' Tient a jour la liste des PIP a l'ecran. Plusieurs Form12 existent aussi sans etre des PIP
    ''' (VB en cree une par thread des qu'on ecrit "Form12", ex. Form12.Close() depuis l'interface) :
    ''' une fenetre invisible ne fait que se retirer de la liste.
    ''' </summary>
    Private Sub PipDeplaceOuAffiche() Handles Me.Shown, Me.VisibleChanged, Me.LocationChanged, Me.SizeChanged
        Dim concerne As Boolean
        Dim listeChangee As Boolean
        If _masqueAvecLaFenetre AndAlso Not Me.Visible Then Exit Sub   ' cache avec la fenetre : reste un PIP
        SyncLock _pips
            If Me.Visible AndAlso Not Me.IsDisposed Then
                If Not _pips.Contains(Me) Then _pips.Add(Me) : listeChangee = True
                concerne = True
            Else
                listeChangee = _pips.Remove(Me)
                concerne = listeChangee
            End If
        End SyncLock
        If Not concerne Then Exit Sub
        ' Pendant un cliquer-glisser (deplacer ou changer la taille), rien ne bouge autour du PIP : la legende, la pochette et les autres PIP ne se recalent qu'au relachement
        ' (sinon chaque pixel parcouru les faisait reagir, et le PIP tremblait : 21/10/2026).
        If _glisse Then Exit Sub
        ' Un PIP apparait ou disparait (ou change de taille) : tous les PIP recalent leur rangee.
        ' Un simple deplacement ne relance rien (sinon les PIP se recaleraient sans fin).
        If listeChangee OrElse Me.Size <> _derniereTaille Then
            _derniereTaille = Me.Size
            EcarterDeLaLegende()
        End If
        RaiseEvent PlaceChangee()
    End Sub

    Private _derniereTaille As Size

    ' ------------------------------------------------------------------------------------------
    ' Place du PIP dans son coin
    '
    ' - MEME distance au bord horizontal et au bord vertical (demande de Jacob : a 2 cm du bas,
    '   donc a 2 cm du cote), l'ecart etant proportionnel a la fenetre de Slide.
    ' - En bas (Bdroite, Bgauche), cette distance grandit s'il le faut pour laisser libre la
    '   legende "lieu · date" du diaporama qui continue derriere ; l'ecart sur le cote suit.
    '
    ' Refait a l'affichage, a chaque deplacement / changement de taille, et a chaque photo
    ' (EcarterDeLaLegende, appele par Module1.AfficheLegende). La bande de la legende vient de
    ' Module1.ZoneLegendeReservee, calculee sur le fil de l'interface : rien n'est demande ici a
    ' Form3, qui n'appartient pas au thread du PIP.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Fenetre de Slide (coordonnees ecran), coin choisi, et taille du PIP pour la fenetre
    ''' telle qu'elle etait a son lancement : de quoi tout recalculer quand la fenetre bouge.
    ''' Propres a CHAQUE PIP.</summary>
    Private _zoneFenetre As Rectangle = Rectangle.Empty
    Private _coin As String = "Bdroite"
    Private _ecartBase As Integer
    Private _zoneDepart As Rectangle = Rectangle.Empty
    Private _tailleDepart As Size

    ''' <summary>Coin de ce PIP (Bdroite, Bgauche, Hdroite, Hgauche).</summary>
    Public ReadOnly Property Coin As String
        Get
            Return _coin
        End Get
    End Property

    ''' <summary>Ecart entre le PIP et les bords de l'image, en part de sa hauteur : 3,5 %.</summary>
    Private Const PartEcart As Double = 0.035

    ''' <summary>Pose le PIP dans <paramref name="coin"/> (Bdroite, Bgauche, Hdroite, Hgauche) de
    ''' <paramref name="zoneFenetre"/>, au format de la video (<paramref name="formatVideo"/> =
    ''' largeur / hauteur, 0 = inconnu : format de la fenetre).</summary>
    ''' <param name="part">PipSize : part de la FENETRE de Slide (0,15 = 15 % de la largeur et de la
    ''' hauteur de son image). En plein ecran, c'est l'ecran. C'est la taille au lancement ; ensuite
    ''' le PIP grandit ou retrecit avec la fenetre.</param>
    Public Sub PoserDansLeCoin(zoneFenetre As Rectangle, coin As String, formatVideo As Double, part As Double)
        _coin = If(String.IsNullOrEmpty(coin), "Bdroite", coin)
        If part <= 0 Then part = 0.25
        _part = part
        _formatVideo = formatVideo
        OrienterLaSouris()

        ' Fenetre introuvable : l'ecran, comme avant.
        Dim reference As Rectangle = If(zoneFenetre.Width > 0 AndAlso zoneFenetre.Height > 0, zoneFenetre, Screen.PrimaryScreen.Bounds)
        Dim maxL As Double = reference.Width * part
        Dim maxH As Double = reference.Height * part
        Dim format As Double = If(formatVideo > 0, formatVideo, maxL / maxH)
        ' Au format de la video, dans la limite de la taille reglee : pas de bandes noires.
        If format >= maxL / maxH Then
            _tailleDepart = New Size(CInt(maxL), CInt(Math.Round(maxL / format)))
        Else
            _tailleDepart = New Size(CInt(Math.Round(maxH * format)), CInt(maxH))
        End If
        _zoneDepart = zoneFenetre

        ' Le lecteur remplit toujours le PIP, quelle que soit sa taille.
        VlcControl1.Dock = DockStyle.Fill
        AppliquerZone(zoneFenetre)
    End Sub

    ''' <summary>Taille et place du PIP pour une fenetre de Slide a <paramref name="zone"/>.</summary>
    Private Sub AppliquerZone(zone As Rectangle)
        If zone.Width <= 0 OrElse zone.Height <= 0 OrElse _zoneDepart.Width <= 0 OrElse _zoneDepart.Height <= 0 Then Exit Sub
        _zoneFenetre = zone
        _ecartBase = CInt(Math.Round(PartEcart * zone.Height))

        ' La taille reglee au lancement, puis dans la meme proportion que la fenetre (demande de
        ' Jacob : la fenetre change de taille, le PIP aussi). Le format de l'image est conserve.
        Dim echelle As Double = Math.Min(zone.Width / _zoneDepart.Width, zone.Height / _zoneDepart.Height)
        Dim taille As New Size(Math.Max(1, CInt(Math.Round(_tailleDepart.Width * echelle))),
                               Math.Max(1, CInt(Math.Round(_tailleDepart.Height * echelle))))
        If _agrandi Then taille = TailleAgrandie(zone)          ' (double-clic : le PIP remplit l'image de Slide)
        If Me.Size <> taille Then Me.Size = taille
        Repositionner()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' PIP attaches a la fenetre de Slide : ils la suivent quand on la deplace ou la redimensionne
    ' (passage en plein ecran compris). Les evenements de la fenetre arrivent sur le thread de
    ' l'interface ; chaque PIP, lui, vit dans le thread de son lecteur : on lui passe la nouvelle
    ' zone par BeginInvoke, sans attendre.
    ' ------------------------------------------------------------------------------------------

    Private Shared _fenetreSuivie As Form

    ''' <summary>Accroche les PIP a <paramref name="fenetre"/> (Nothing = decrocher).</summary>
    Public Shared Sub SuivreLaFenetre(fenetre As Form)
        Dim ancienne As Form = _fenetreSuivie
        If ReferenceEquals(ancienne, fenetre) Then Exit Sub
        If ancienne IsNot Nothing Then
            RemoveHandler ancienne.LocationChanged, AddressOf FenetreDeplacee
            RemoveHandler ancienne.SizeChanged, AddressOf FenetreDeplacee
            RemoveHandler ancienne.ClientSizeChanged, AddressOf FenetreDeplacee
            RemoveHandler ancienne.VisibleChanged, AddressOf FenetreDeplacee
        End If
        _fenetreSuivie = fenetre
        If fenetre IsNot Nothing Then
            AddHandler fenetre.LocationChanged, AddressOf FenetreDeplacee
            AddHandler fenetre.SizeChanged, AddressOf FenetreDeplacee
            ' La zone client change SEULE quand la bordure apparait ou disparait (passage plein
            ' ecran <-> fenetre) : sans cet evenement, le PIP restait cale sur l'ancienne zone.
            AddHandler fenetre.ClientSizeChanged, AddressOf FenetreDeplacee
            ' Fenetre cachee / reduite / revenue : les PIP la suivent (SuivreLaVisibilite).
            AddHandler fenetre.VisibleChanged, AddressOf FenetreDeplacee
            SuivreLaVisibilite(fenetre)
        End If
    End Sub

    Private Shared _recalagePrevu As Boolean

    ''' <summary>La fenetre bouge : on recale les PIP une fois la rafale de changements terminee.</summary>
    Private Shared Sub FenetreDeplacee(sender As Object, e As EventArgs)
        Dim fenetre As Form = TryCast(sender, Form)
        If fenetre Is Nothing OrElse _recalagePrevu Then Exit Sub
        _recalagePrevu = True
        Try
            fenetre.BeginInvoke(New Action(Sub() RecalerSur(fenetre)))
        Catch ex As Exception
            _recalagePrevu = False
        End Try
    End Sub

    ''' <summary>Recale les PIP tout de suite (thread de l'interface), sans attendre la fin de
    ''' l'action en cours : passage plein ecran / fenetre.</summary>
    Public Shared Sub RecalerMaintenant(fenetre As Form)
        RecalerSur(fenetre)
    End Sub

    Private Shared Sub RecalerSur(fenetre As Form)
        _recalagePrevu = False
        Try
            If fenetre Is Nothing Then Exit Sub
            SuivreLaVisibilite(fenetre)
            ' Reduite dans la barre des taches : ses coordonnees n'ont plus de sens, les PIP (caches)
            ' gardent leur place.
            If fenetre.WindowState = FormWindowState.Minimized OrElse Not fenetre.Visible Then Exit Sub
            ' Sur le thread de l'interface : la bande de l'etiquette du bas suit la fenetre.
            CalculeLaZoneDeLaLegende()
            ' La zone de l'IMAGE (zone client), pas le cadre exterieur.
            Dim zone As Rectangle = fenetre.RectangleToScreen(fenetre.ClientRectangle)
            For Each pip As Form12 In PipsAffiches()
                Dim p As Form12 = pip
                p.BeginInvoke(New Action(Sub() If Not p.IsDisposed Then p.AppliquerZone(zone)))
            Next
        Catch ex As Exception
            ' un ajustement de position ne doit jamais faire tomber la lecture
        End Try
    End Sub

    ''' <summary>Ecart reellement applique aux deux bords par ce PIP (utile au journal).</summary>
    Public Function EcartActuel() As Integer
        Dim ecart As Integer = _ecartBase
        Dim legende As Rectangle = ZoneLegendeReservee
        If legende.IsEmpty OrElse _zoneFenetre.IsEmpty Then Return ecart
        ' Seul un PIP dans le MEME coin que l'etiquette (coin choisi dans Form4) doit s'en ecarter.
        If Not String.Equals(LegendeCoin, _coin, StringComparison.OrdinalIgnoreCase) Then Return ecart
        Dim legendeEnHaut As Boolean = legende.Top < _zoneFenetre.Top + _zoneFenetre.Height \ 2
        Dim pipEnHaut As Boolean = _coin.StartsWith("H")
        If pipEnHaut AndAlso legendeEnHaut Then
            ecart = Math.Max(ecart, legende.Bottom - _zoneFenetre.Top + 2)
        ElseIf Not pipEnHaut AndAlso Not legendeEnHaut Then
            ecart = Math.Max(ecart, _zoneFenetre.Bottom - legende.Top + 2)
        End If
        Return ecart
    End Function

    ''' <summary>Marge commune aux PIP de la MEME rangee (tous en bas, ou tous en haut) : la plus
    ''' grande. Avant, chacun avait la sienne : le PIP du coin de la legende remontait pour la
    ''' laisser libre, l'autre non, et les deux PIP du bas n'etaient pas au meme niveau.</summary>
    Private Function EcartDeLaRangee() As Integer
        Dim ecart As Integer = EcartActuel()
        Dim enHaut As Boolean = _coin.StartsWith("H")
        For Each autre As Form12 In PipsAffiches()
            If ReferenceEquals(autre, Me) OrElse autre._zoneFenetre.IsEmpty Then Continue For
            If autre._coin.StartsWith("M") OrElse autre._coin.StartsWith("H") <> enHaut Then Continue For
            ecart = Math.Max(ecart, autre.EcartActuel())
        Next
        Return ecart
    End Function

    ''' <summary>Rangee du bas : les PIP sont alignes par le HAUT (demande de Jacob), sur le plus
    ''' haut d'entre eux, pour qu'aucun ne descende sur la legende. Les PIP n'ont pas tous la meme
    ''' hauteur (taille reglee par tache) : aligner le bas donnait des hauts decales.</summary>
    Private Function HautDeLaRangeeDuBas(ecart As Integer) As Integer
        Dim y As Integer = _zoneFenetre.Bottom - Me.Height - ecart
        For Each autre As Form12 In PipsAffiches()
            If ReferenceEquals(autre, Me) OrElse autre._zoneFenetre.IsEmpty OrElse autre._coin.StartsWith("H") OrElse autre._coin.StartsWith("M") Then Continue For
            y = Math.Min(y, autre._zoneFenetre.Bottom - autre.Height - ecart)
        Next
        Return y
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Deplacer le PIP a la souris (demande de Jacob, 21/10/2026) : cliquer-glisser. Sa place est enregistree en proportion de l'image de Slide dans la tache de la minuterie
    ' qui l'a lance ("!/PipX:0,42!/PipY:0,30", voir Module2.PositionPipChoisieALaSouris) et remplace son coin ; double-clic : retour au coin. La taille se change avec la
    ' molette, ou Ctrl + cliquer-glisser (geste d'avant).
    ' ------------------------------------------------------------------------------------------

    Private _libreX As Double = -1
    Private _libreY As Double = -1
    Private _glisse As Boolean
    Private _pointeurDepart As Point
    Private _placeDepart As Point

    Private Shared Function CtrlEnfonce() As Boolean
        Return (Control.ModifierKeys And Keys.Control) = Keys.Control
    End Function

    ''' <summary>Place libre du PIP (proportions de 0 a 1 ; -1 = le coin). Appele dans le thread du PIP.</summary>
    Public Sub DefinirPlaceLibre(fx As Double, fy As Double)
        _libreX = If(fx >= 0 AndAlso fy >= 0, Math.Min(1.0, fx), -1)
        _libreY = If(fx >= 0 AndAlso fy >= 0, Math.Min(1.0, fy), -1)
        Repositionner()
    End Sub

    ' Saisir un COIN du PIP change sa taille (le coin oppose reste en place, le format de l'image est garde) ; saisir ailleurs le deplace.
    Private _tailleCoin As Boolean
    Private _coinOppose As Point
    Private _formatPip As Double = 1.78
    Private _coinGauche As Boolean
    Private _coinHaut As Boolean

    ''' <summary>Le pointeur (coordonnees du lecteur) est-il dans un coin du PIP ? (zone : le sixieme de sa petite dimension, au moins 14 pixels).</summary>
    Private Function CoinSousLePointeur(p As Point, ByRef gauche As Boolean, ByRef haut As Boolean) As Boolean
        Dim zone As Integer = Math.Max(14, Math.Min(Me.Width, Me.Height) \ 6)
        Dim aGauche As Boolean = p.X <= zone
        Dim aDroite As Boolean = p.X >= Me.Width - zone
        Dim enHaut As Boolean = p.Y <= zone
        Dim enBas As Boolean = p.Y >= Me.Height - zone
        If (aGauche OrElse aDroite) AndAlso (enHaut OrElse enBas) Then
            gauche = aGauche
            haut = enHaut
            Return True
        End If
        Return False
    End Function

    Private Sub PipSouris_Down(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left OrElse CtrlEnfonce() OrElse _agrandi Then Exit Sub          ' (agrandi a l'image : rien a deplacer)
        _glisse = True
        _pointeurDepart = Cursor.Position
        _placeDepart = Me.Location
        _tailleCoin = CoinSousLePointeur(e.Location, _coinGauche, _coinHaut)
        If _tailleCoin Then
            _formatPip = Me.Width / CDbl(Math.Max(1, Me.Height))
            _coinOppose = New Point(If(_coinGauche, Me.Right, Me.Left), If(_coinHaut, Me.Bottom, Me.Top))
        End If
        VlcControl1.Capture = True
    End Sub

    Private Sub PipSouris_Move(sender As Object, e As MouseEventArgs)
        If Not _glisse Then
            ' Sans bouton : la fleche annonce le geste (diagonale sur un coin = taille, croix ailleurs = deplacer).
            Dim g As Boolean, h As Boolean
            Dim voulue As Cursor = If(CoinSousLePointeur(e.Location, g, h), If(g = h, Cursors.SizeNWSE, Cursors.SizeNESW), Cursors.SizeAll)
            If VlcControl1.Cursor IsNot voulue Then VlcControl1.Cursor = voulue
            Exit Sub
        End If
        If _zoneFenetre.IsEmpty Then Exit Sub
        Dim p As Point = Cursor.Position
        If _tailleCoin Then
            ' Taille d'apres la distance du pointeur au coin oppose, au format de l'image ; dans l'image de Slide, de 5 % a 100 %.
            Dim l As Double = Math.Max(Math.Abs(p.X - _coinOppose.X), Math.Abs(p.Y - _coinOppose.Y) * _formatPip)
            l = Math.Max(_zoneFenetre.Width * PourcentMini / 100.0, Math.Min(l, _zoneFenetre.Width))
            Dim hauteur As Double = l / _formatPip
            If hauteur > _zoneFenetre.Height Then hauteur = _zoneFenetre.Height : l = hauteur * _formatPip
            Dim x As Integer = If(_coinGauche, _coinOppose.X - CInt(l), _coinOppose.X)
            Dim y As Integer = If(_coinHaut, _coinOppose.Y - CInt(hauteur), _coinOppose.Y)
            Me.Bounds = New Rectangle(x, y, CInt(l), CInt(hauteur))
            ' Tailles liees : les autres PIP suivent PENDANT le geste, on voit le resultat tout de suite.
            If TaillesLiees AndAlso _zoneFenetre.Width > 0 AndAlso _zoneFenetre.Height > 0 Then
                PropagerLaTaille(CInt(Math.Round(100 * Math.Max(Me.Width / CDbl(_zoneFenetre.Width), Me.Height / CDbl(_zoneFenetre.Height)))))
            End If
            Exit Sub
        End If
        Dim nx As Integer = _placeDepart.X + p.X - _pointeurDepart.X
        Dim ny As Integer = _placeDepart.Y + p.Y - _pointeurDepart.Y
        ' Le PIP reste dans l'image de Slide.
        nx = Math.Max(_zoneFenetre.Left, Math.Min(nx, _zoneFenetre.Right - Me.Width))
        ny = Math.Max(_zoneFenetre.Top, Math.Min(ny, _zoneFenetre.Bottom - Me.Height))
        Me.Location = New Point(nx, ny)
    End Sub

    Private Sub PipSouris_Up(sender As Object, e As MouseEventArgs)
        If Not _glisse Then Exit Sub
        _glisse = False
        VlcControl1.Capture = False
        If _tailleCoin Then
            _tailleCoin = False
            ' La taille choisie devient la taille du PIP (part de l'image de Slide) ; le coin oppose reste en place, donc le PIP prend une place libre.
            Dim coinHautGauche As Point = Me.Location
            Dim part As Double = Math.Max(Me.Width / CDbl(Math.Max(1, _zoneFenetre.Width)), Me.Height / CDbl(Math.Max(1, _zoneFenetre.Height)))
            part = Math.Max(PourcentMini / 100.0, Math.Min(PourcentMaxi / 100.0, part))
            PoserDansLeCoin(_zoneFenetre, _coin, _formatVideo, part)
            Dim x As Integer = If(_coinGauche, _coinOppose.X - Me.Width, _coinOppose.X)
            Dim y As Integer = If(_coinHaut, _coinOppose.Y - Me.Height, _coinOppose.Y)
            Me.Location = New Point(x, y)
            TaillePipChoisieALaSouris(Me, CInt(Math.Round(part * 100)))
            If TaillesLiees Then TousALaMemeTaille(CInt(Math.Round(part * 100)))          ' (tailles liees : les autres PIP suivent)
        End If
        Dim largeurLibre As Integer = _zoneFenetre.Width - Me.Width
        Dim hauteurLibre As Integer = _zoneFenetre.Height - Me.Height
        _libreX = If(largeurLibre <= 0, 0.0, Math.Max(0.0, Math.Min(1.0, (Me.Left - _zoneFenetre.Left) / CDbl(largeurLibre))))
        _libreY = If(hauteurLibre <= 0, 0.0, Math.Max(0.0, Math.Min(1.0, (Me.Top - _zoneFenetre.Top) / CDbl(hauteurLibre))))
        PositionPipChoisieALaSouris(Me, _libreX, _libreY)
        RaiseEvent PlaceChangee()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Clic droit sur un PIP (22/10/2026) : "Meme taille pour tous les PIP" (celle du PIP clique) et plusieurs facons d'aligner les PIP affiches sur celui-ci. Chaque PIP vit dans son
    ' propre thread : le menu est celui de CE PIP, et le travail est confie a chaque PIP par BeginInvoke. Les places et tailles sont enregistrees dans les taches comme pour un geste a la souris.
    ' ------------------------------------------------------------------------------------------

    Private _menuPip As ContextMenuStrip

    Private Sub PreparerLeMenu()
        If _menuPip IsNot Nothing Then Exit Sub
        _menuPip = New ContextMenuStrip()
        Dim ajouter As Func(Of ToolStripItemCollection, String, Action, ToolStripMenuItem) =
            Function(liste, texte, action)
                Dim item As New ToolStripMenuItem(texte)
                AddHandler item.Click, Sub() action()
                liste.Add(item)
                Return item
            End Function
        ' Menu simplifie (23/10/2026 : "pas adapte, pense a d'autres menus plus efficaces") : le plus courant d'abord, a plat ; chaque sous-menu dit ce qu'il fait.
        '   - Placer ce PIP : les 9 places de l'image (comme une grille 3 x 3), en un clic ;
        '   - Meme taille pour tous ;
        '   - Ranger tous les PIP : la disposition d'ensemble (cote a cote, en colonne, chacun dans son coin) ;
        '   - Aligner sur ce PIP : meme bord, meme hauteur, meme colonne.
        Dim placer As New ToolStripMenuItem("Placer ce PIP")
        _menuPip.Items.Add(placer)
        ajouter(placer.DropDownItems, "↖  En haut à gauche", Sub() PlacerCePip(0.0, 0.0))
        ajouter(placer.DropDownItems, "↑  En haut au centre", Sub() PlacerCePip(0.5, 0.0))
        ajouter(placer.DropDownItems, "↗  En haut à droite", Sub() PlacerCePip(1.0, 0.0))
        placer.DropDownItems.Add(New ToolStripSeparator())
        ajouter(placer.DropDownItems, "←  Au milieu à gauche", Sub() PlacerCePip(0.0, 0.5))
        ajouter(placer.DropDownItems, "●  Au centre", Sub() PlacerCePip(0.5, 0.5))
        ajouter(placer.DropDownItems, "→  Au milieu à droite", Sub() PlacerCePip(1.0, 0.5))
        placer.DropDownItems.Add(New ToolStripSeparator())
        ajouter(placer.DropDownItems, "↙  En bas à gauche", Sub() PlacerCePip(0.0, 1.0))
        ajouter(placer.DropDownItems, "↓  En bas au centre", Sub() PlacerCePip(0.5, 1.0))
        ajouter(placer.DropDownItems, "↘  En bas à droite", Sub() PlacerCePip(1.0, 1.0))
        ajouter(_menuPip.Items, "Répartir les PIP à l'écran (1/8 chacun)", Sub() RepartirLesPip())
        ajouter(_menuPip.Items, "Même taille pour tous les PIP", Sub() TousALaMemeTaille(PourcentTaille))
        ' Tailles liees (23/10/2026) : agrandir ou reduire UN PIP (molette, coin) fait suivre tous les autres, qui gardent toujours la meme taille. Choix retenu pour le poste.
        Dim lie As New ToolStripMenuItem("Agrandir tous les PIP ensemble (même taille toujours)")
        AddHandler lie.Click, Sub()
                                  TaillesLiees = Not TaillesLiees
                                  If TaillesLiees Then TousALaMemeTaille(PourcentTaille)
                              End Sub
        AddHandler _menuPip.Opening, Sub() lie.Checked = TaillesLiees
        _menuPip.Items.Add(lie)
        _menuPip.Items.Add(New ToolStripSeparator())
        Dim ranger As New ToolStripMenuItem("Ranger tous les PIP")
        _menuPip.Items.Add(ranger)
        ajouter(ranger.DropDownItems, "Côte à côte, en bas de l'image", Sub() AlignerLesPip("cote-bas"))
        ajouter(ranger.DropDownItems, "Côte à côte, en haut de l'image", Sub() AlignerLesPip("cote-haut"))
        ajouter(ranger.DropDownItems, "En colonne, à gauche de l'image", Sub() AlignerLesPip("colonne-gauche"))
        ajouter(ranger.DropDownItems, "En colonne, à droite de l'image", Sub() AlignerLesPip("colonne-droite"))
        ranger.DropDownItems.Add(New ToolStripSeparator())
        ajouter(ranger.DropDownItems, "Chacun dans son coin", Sub() AlignerLesPip("coins"))
        ' Pas d'alignement "bord a bord" seul : coller les bords des PIP les empile les uns sur les autres (23/10/2026 : "il ne serait plus visible"). Les deux choix suivants alignent TOUT EN GARDANT chaque PIP visible.
        Dim aligner As New ToolStripMenuItem("Aligner les autres PIP sur celui-ci")
        _menuPip.Items.Add(aligner)
        ajouter(aligner.DropDownItems, "Verticalement : en colonne, sous celui-ci", Sub() AlignerLesPip("colonne-ici"))
        ajouter(aligner.DropDownItems, "Horizontalement : sur une ligne, à côté de celui-ci", Sub() AlignerLesPip("ligne-ici"))
        aligner.DropDownItems.Add(New ToolStripSeparator())
        ' Alignements de bords (23/10/2026, redemandes) : les PIP gardent leur autre coordonnee, donc deux PIP au meme niveau peuvent se recouvrir.
        Dim bords As ToolStripMenuItem = ajouter(aligner.DropDownItems, "Sur le même bord (ils peuvent se recouvrir) ▸", Sub()
                                                                                                                            End Sub)
        bords.DropDownItems.Add(New ToolStripMenuItem("Bord gauche", Nothing, Sub() AlignerLesPip("gauche")))
        bords.DropDownItems.Add(New ToolStripMenuItem("Bord droit", Nothing, Sub() AlignerLesPip("droite")))
        bords.DropDownItems.Add(New ToolStripMenuItem("Bord du haut", Nothing, Sub() AlignerLesPip("haut")))
        bords.DropDownItems.Add(New ToolStripMenuItem("Bord du bas", Nothing, Sub() AlignerLesPip("bas")))
        bords.DropDownItems.Add(New ToolStripSeparator())
        bords.DropDownItems.Add(New ToolStripMenuItem("Centres à la même hauteur", Nothing, Sub() AlignerLesPip("centre-h")))
        bords.DropDownItems.Add(New ToolStripMenuItem("Centres sur la même verticale", Nothing, Sub() AlignerLesPip("centre-v")))
        AddHandler VlcControl1.MouseUp, Sub(s As Object, e As MouseEventArgs)
                                            If e.Button = MouseButtons.Right AndAlso Not _glisse Then _menuPip.Show(Cursor.Position)
                                        End Sub
    End Sub

    Private Shared _taillesLiees As Boolean
    Private Shared _taillesLieesLues As Boolean

    ''' <summary>Vrai si les PIP gardent toujours la meme taille (choix du menu, retenu dans data\settings\PipTaillesLiees.txt).</summary>
    Friend Shared Property TaillesLiees As Boolean
        Get
            If Not _taillesLieesLues Then
                _taillesLieesLues = True
                Try
                    _taillesLiees = System.IO.File.Exists(DossierReglages & "\PipTaillesLiees.txt") AndAlso System.IO.File.ReadAllText(DossierReglages & "\PipTaillesLiees.txt").Trim() = "1"
                Catch ex As Exception
                End Try
            End If
            Return _taillesLiees
        End Get
        Set(valeur As Boolean)
            _taillesLiees = valeur
            _taillesLieesLues = True
            Try
                System.IO.File.WriteAllText(DossierReglages & "\PipTaillesLiees.txt", If(valeur, "1", "0"))
            Catch ex As Exception
            End Try
        End Set
    End Property

    ''' <summary>Tous les PIP affiches prennent la taille (en % de l'image de Slide) de celui-ci.</summary>
    Private Sub TousALaMemeTaille(pourcent As Integer)
        ' Meme LARGEUR que ce PIP (la hauteur suit le format de chaque video) ; le pourcentage de chacun est enregistre dans sa tache.
        Dim largeur As Integer = Me.Width
        _dernierPropage = largeur
        For Each pip As Form12 In PipsAffiches()
            Dim p As Form12 = pip
            Try
                p.BeginInvoke(New Action(Sub()
                                             If p.IsDisposed Then Exit Sub
                                             p.TailleSurLargeur(largeur)
                                             TaillePipChoisieALaSouris(p, p.PourcentTaille)
                                             RaiseEvent PlaceChangee()
                                         End Sub))
            Catch ex As Exception
            End Try
        Next
    End Sub

    ''' <summary>Aligne les PIP affiches (voir PreparerLeMenu) sur ce PIP, ou les range.</summary>
    Private Sub AlignerLesPip(mode As String)
        Try
            Dim reference As Rectangle = Me.Bounds
            Dim zone As Rectangle = _zoneFenetre
            Dim pips As List(Of Form12) = PipsAffiches().Where(Function(x) Not x._zoneFenetre.IsEmpty).ToList()
            Dim ecart As Integer = Math.Max(4, _ecartBase)
            Dim cibles As New Dictionary(Of Form12, Point)
            Select Case mode
                Case "coins"
                    For Each p As Form12 In pips
                        Dim q As Form12 = p
                        q.BeginInvoke(New Action(Sub()
                                                     If q.IsDisposed Then Exit Sub
                                                     q._libreX = -1 : q._libreY = -1
                                                     PositionPipChoisieALaSouris(q, -1, -1)
                                                     q.Repositionner()
                                                     RaiseEvent PlaceChangee()
                                                 End Sub))
                    Next
                    Exit Sub
                Case "cote-bas", "cote-haut"
                    Dim x As Integer = zone.Left + ecart
                    For Each p As Form12 In pips.OrderBy(Function(v) v.Left)
                        Dim y As Integer = If(mode = "cote-bas", zone.Bottom - p.Height - ecart, zone.Top + ecart)
                        cibles(p) = New Point(x, y)
                        x += p.Width + ecart
                    Next
                Case "colonne-gauche", "colonne-droite"
                    Dim y As Integer = zone.Top + ecart
                    For Each p As Form12 In pips.OrderBy(Function(v) v.Top)
                        Dim x As Integer = If(mode = "colonne-gauche", zone.Left + ecart, zone.Right - p.Width - ecart)
                        cibles(p) = New Point(x, y)
                        y += p.Height + ecart
                    Next
                Case "ligne-ici"
                    ' Les autres PIP prennent la hauteur de celui-ci et se rangent a sa droite, l'un apres l'autre ; sans place a droite, a sa gauche. Aucun ne recouvre un autre.
                    Dim droite As Integer = reference.Right + ecart
                    Dim gauche As Integer = reference.Left - ecart
                    For Each p As Form12 In pips.Where(Function(v) Not ReferenceEquals(v, Me)).OrderBy(Function(v) v.Left)
                        If droite + p.Width <= zone.Right Then
                            cibles(p) = New Point(droite, reference.Top + (reference.Height - p.Height) \ 2)
                            droite += p.Width + ecart
                        Else
                            gauche -= p.Width
                            cibles(p) = New Point(Math.Max(zone.Left, gauche), reference.Top + (reference.Height - p.Height) \ 2)
                            gauche -= ecart
                        End If
                    Next
                Case "colonne-ici"
                    ' Les autres PIP prennent la verticale de celui-ci et s'empilent dessous ; sans place en dessous, au-dessus. Aucun ne recouvre un autre.
                    Dim dessous As Integer = reference.Bottom + ecart
                    Dim dessus As Integer = reference.Top - ecart
                    For Each p As Form12 In pips.Where(Function(v) Not ReferenceEquals(v, Me)).OrderBy(Function(v) v.Top)
                        If dessous + p.Height <= zone.Bottom Then
                            cibles(p) = New Point(reference.Left + (reference.Width - p.Width) \ 2, dessous)
                            dessous += p.Height + ecart
                        Else
                            dessus -= p.Height
                            cibles(p) = New Point(reference.Left + (reference.Width - p.Width) \ 2, Math.Max(zone.Top, dessus))
                            dessus -= ecart
                        End If
                    Next
                Case Else
                    ' Le bord choisi est celui du PIP clique, mais ramene dans l'image si un PIP plus grand ne peut pas s'y aligner (il sortirait de l'image : sa place etait alors
                    ' ramenee a l'ancienne et l'alignement "ne marchait pas", 23/10/2026).
                    Dim plusLarge As Integer = pips.Max(Function(v) v.Width)
                    Dim plusHaut As Integer = pips.Max(Function(v) v.Height)
                    Dim bordGauche As Integer = Math.Min(reference.Left, zone.Right - plusLarge)
                    Dim bordDroit As Integer = Math.Max(reference.Right, zone.Left + plusLarge)
                    Dim bordHaut As Integer = Math.Min(reference.Top, zone.Bottom - plusHaut)
                    Dim bordBas As Integer = Math.Max(reference.Bottom, zone.Top + plusHaut)
                    For Each p As Form12 In pips
                        Select Case mode
                            Case "haut" : cibles(p) = New Point(p.Left, bordHaut)
                            Case "bas" : cibles(p) = New Point(p.Left, bordBas - p.Height)
                            Case "gauche" : cibles(p) = New Point(bordGauche, p.Top)
                            Case "droite" : cibles(p) = New Point(bordDroit - p.Width, p.Top)
                            Case "centre-h" : cibles(p) = New Point(p.Left, reference.Top + (reference.Height - p.Height) \ 2)
                            Case "centre-v" : cibles(p) = New Point(reference.Left + (reference.Width - p.Width) \ 2, p.Top)
                        End Select
                    Next
            End Select
            For Each kv As KeyValuePair(Of Form12, Point) In cibles
                Dim p As Form12 = kv.Key
                Dim cible As Point = kv.Value
                p.BeginInvoke(New Action(Sub() If Not p.IsDisposed Then p.PlacerA(cible)))
            Next
            DiagLog("PIP : alignement « " & mode & " » de " & cibles.Count & " PIP : " &
                    String.Join(" ; ", cibles.Select(Function(kv) kv.Key.Left & "," & kv.Key.Top & " " & kv.Key.Width & "x" & kv.Key.Height & " -> " & kv.Value.X & "," & kv.Value.Y)) &
                    " (image " & zone.Left & "," & zone.Top & " " & zone.Width & "x" & zone.Height & ")")
        Catch ex As Exception
            DiagLog("PIP : alignement impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Repartit les PIP affiches (8 au maximum) a l'ecran (menu « Répartir les PIP à l'écran (1/8 chacun) ») : chaque PIP occupe 1/8 de la largeur et 1/8 de la hauteur de l'image de Slide
    ''' (au format de sa video, dans cette boite), colle au bord selon son coin de la minuterie : Bdroite = en bas collé à droite, Hdroite = en haut collé à droite, Mgauche = au milieu collé à gauche...
    ''' Deux PIP du meme coin se suivent vers l'interieur de l'image.</summary>
    Private Sub RepartirLesPip()
        Try
            Dim zone As Rectangle = _zoneFenetre
            If zone.IsEmpty Then Exit Sub
            Dim pips As List(Of Form12) = PipsAffiches().Where(Function(x) Not x._zoneFenetre.IsEmpty).Take(8).ToList()
            Dim boiteL As Double = zone.Width / 8.0
            Dim boiteH As Double = zone.Height / 8.0
            Dim dejaDansLeCoin As New Dictionary(Of String, Integer)
            For Each pip As Form12 In pips
                Dim p As Form12 = pip
                Dim coin As String = p._coin
                Dim rang As Integer = 0
                If dejaDansLeCoin.TryGetValue(coin, rang) Then dejaDansLeCoin(coin) = rang + 1 Else dejaDansLeCoin(coin) = 1
                Dim rangDansLeCoin As Integer = rang
                p.BeginInvoke(New Action(Sub()
                                             If p.IsDisposed Then Exit Sub
                                             p._agrandi = False
                                             Dim format As Double = If(p._formatVideo > 0, p._formatVideo, zone.Width / CDbl(Math.Max(1, zone.Height)))
                                             Dim largeur As Integer = CInt(Math.Floor(If(format >= boiteL / boiteH, boiteL, boiteH * format)))
                                             p.TailleSurLargeur(largeur)
                                             TaillePipChoisieALaSouris(p, p.PourcentTaille)
                                             Dim decalage As Integer = rangDansLeCoin * p.Width
                                             Dim x As Integer = If(coin.EndsWith("droite"), zone.Right - p.Width - decalage, zone.Left + decalage)
                                             Dim y As Integer = If(coin.StartsWith("H"), zone.Top, If(coin.StartsWith("M"), zone.Top + (zone.Height - p.Height) \ 2, zone.Bottom - p.Height))
                                             p.PlacerA(New Point(x, y))
                                         End Sub))
            Next
            DiagLog("PIP : repartition de " & pips.Count & " PIP, 1/8 de l'image en largeur et en hauteur, colles a leur coin")
        Catch ex As Exception
            DiagLog("PIP : repartition impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Place CE PIP a l'une des 9 places de l'image de Slide : <paramref name="fx"/> et <paramref name="fy"/> valent 0 (gauche / haut), 0,5 (centre) ou 1 (droite / bas). Sur le thread du PIP.</summary>
    Private Sub PlacerCePip(fx As Double, fy As Double)
        If _zoneFenetre.IsEmpty Then Exit Sub
        Dim ecart As Integer = Math.Max(4, _ecartBase)
        Dim libreL As Integer = _zoneFenetre.Width - Me.Width - 2 * ecart
        Dim libreH As Integer = _zoneFenetre.Height - Me.Height - 2 * ecart
        PlacerA(New Point(_zoneFenetre.Left + ecart + CInt(fx * Math.Max(0, libreL)), _zoneFenetre.Top + ecart + CInt(fy * Math.Max(0, libreH))))
    End Sub

    ''' <summary>Pose ce PIP a <paramref name="cible"/> (dans l'image de Slide) et retient cette place libre dans sa tache (sur le thread du PIP).</summary>
    Private Sub PlacerA(cible As Point)
        If _zoneFenetre.IsEmpty Then Exit Sub
        Dim x As Integer = Math.Max(_zoneFenetre.Left, Math.Min(cible.X, _zoneFenetre.Right - Me.Width))
        Dim y As Integer = Math.Max(_zoneFenetre.Top, Math.Min(cible.Y, _zoneFenetre.Bottom - Me.Height))
        Me.Location = New Point(x, y)
        Dim largeurLibre As Integer = _zoneFenetre.Width - Me.Width
        Dim hauteurLibre As Integer = _zoneFenetre.Height - Me.Height
        _libreX = If(largeurLibre <= 0, 0.0, Math.Max(0.0, Math.Min(1.0, (x - _zoneFenetre.Left) / CDbl(largeurLibre))))
        _libreY = If(hauteurLibre <= 0, 0.0, Math.Max(0.0, Math.Min(1.0, (y - _zoneFenetre.Top) / CDbl(hauteurLibre))))
        PositionPipChoisieALaSouris(Me, _libreX, _libreY)
        RaiseEvent PlaceChangee()
    End Sub

    ''' <summary>Double-clic (27/10/2026, Jacob : « un double clic sur un PIP devient en fullscreen et inversement ») : le PIP prend toute l'image de Slide, au format de sa video, sans relancer
    ''' la lecture ; un second double-clic le remet a sa place et a sa taille. Pour le remettre dans son coin : menu « Placer ce PIP ».</summary>
    Private _agrandi As Boolean

    Private Function TailleAgrandie(zone As Rectangle) As Size
        Dim format As Double = If(_formatVideo > 0, _formatVideo, zone.Width / CDbl(Math.Max(1, zone.Height)))
        If format >= zone.Width / CDbl(Math.Max(1, zone.Height)) Then Return New Size(zone.Width, Math.Max(1, CInt(Math.Round(zone.Width / format))))
        Return New Size(Math.Max(1, CInt(Math.Round(zone.Height * format))), zone.Height)
    End Function

    ''' <summary>Le PIP prend toute l'image de Slide des son affichage (clip Blink en plein ecran pendant qu'un film est en pause) ; un double-clic le remet a sa place. Sur le thread du PIP.</summary>
    Public Sub AgrandirALImage()
        _agrandi = True
        If Not _zoneFenetre.IsEmpty Then AppliquerZone(_zoneFenetre)
    End Sub

    Private Sub PipSouris_DoubleClic(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left OrElse CtrlEnfonce() Then Exit Sub
        _glisse = False
        VlcControl1.Capture = False
        _agrandi = Not _agrandi
        DiagLog("PIP : " & If(_agrandi, "agrandi a l'image de Slide", "remis a sa place") & " par double-clic (" & _coin & ")")
        AppliquerZone(_zoneFenetre)
        If _agrandi Then CalqueWin32.DefinirPremierPlanSeul(Me, True)
        RaiseEvent PlaceChangee()
    End Sub

    Private Sub Repositionner()
        If _zoneFenetre.IsEmpty Then Exit Sub
        If _glisse Then Exit Sub          ' (la souris tient le PIP : aucun recalage ne le remet dans son coin pendant le geste)
        If _agrandi Then
            Dim centre As New Point(_zoneFenetre.Left + (_zoneFenetre.Width - Me.Width) \ 2, _zoneFenetre.Top + (_zoneFenetre.Height - Me.Height) \ 2)
            If Me.Location <> centre Then Me.Location = centre
            Exit Sub
        End If
        Dim ecart As Integer = If(_coin.StartsWith("M"), _ecartBase, EcartDeLaRangee())          ' (au milieu : ni rangee du haut ni du bas)
        Dim x As Integer = If(_coin.EndsWith("droite"), _zoneFenetre.Right - Me.Width - ecart, _zoneFenetre.Left + ecart)
        Dim y As Integer
        If _coin.StartsWith("M") Then
            y = _zoneFenetre.Top + (_zoneFenetre.Height - Me.Height) \ 2          ' au milieu (22/10/2026 : Mgauche, Mdroite)
        ElseIf _coin.StartsWith("H") Then
            y = _zoneFenetre.Top + ecart
        Else
            y = HautDeLaRangeeDuBas(ecart)
        End If
        ' Jamais au-dessus du haut de la fenetre (PIP tres grand).
        y = Math.Max(y, _zoneFenetre.Top)
        ' Place choisie a la souris : elle remplace le coin.
        If _libreX >= 0 AndAlso _libreY >= 0 Then
            x = _zoneFenetre.Left + CInt(_libreX * Math.Max(0, _zoneFenetre.Width - Me.Width))
            y = _zoneFenetre.Top + CInt(_libreY * Math.Max(0, _zoneFenetre.Height - Me.Height))
        End If
        Dim voulu As New Point(x, y)
        If Me.Location <> voulu Then Me.Location = voulu
    End Sub

    ''' <summary>Recale les PIP affiches (appele a chaque photo : la taille de la legende a pu
    ''' changer dans les reglages).</summary>
    Public Shared Sub EcarterDeLaLegende()
        For Each pip As Form12 In PipsAffiches()
            Try
                Dim p As Form12 = pip
                If p.InvokeRequired Then
                    ' Chaque PIP vit dans le thread de son lecteur : on lui confie le travail, sans attendre.
                    p.BeginInvoke(New Action(Sub()
                                                 If Not p.IsDisposed AndAlso p.Visible Then p.Repositionner()
                                             End Sub))
                Else
                    p.Repositionner()
                End If
            Catch ex As Exception
                ' un ajustement de position ne doit jamais faire tomber la lecture
            End Try
        Next
    End Sub

    ''' <summary>Leve quand un PIP apparait, bouge, change de taille ou disparait (depuis le
    ''' thread du PIP) : l'etiquette du bas s'aligne sur lui (voir FormLegende).</summary>
    Public Shared Event PlaceChangee()

    ''' <summary>Rectangle (coordonnees ecran) du PIP affiche dans <paramref name="coin"/>, ou
    ''' Nothing s'il n'y en a pas dans ce coin.</summary>
    Public Shared Function ZoneDuPipDansLeCoin(coin As String) As Rectangle?
        For Each pip As Form12 In PipsAffiches()
            If pip._zoneFenetre.IsEmpty Then Continue For
            If String.Equals(coin, pip._coin, StringComparison.OrdinalIgnoreCase) Then Return pip.Bounds
        Next
        Return Nothing
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Les PIP cedent le premier plan au menu et aux reglages
    '
    ' Ils sont au premier plan pour rester devant le diaporama. Mais un menu contextuel WinForms
    ' s'ouvre SOUS une fenetre de premier plan, et Form4 (reglages) aussi : tant qu'une "raison"
    ' est active (menu ouvert, reglages ouverts), les PIP sont ranges JUSTE DERRIERE la fenetre
    ' concernee ; ils reviennent au premier plan quand la derniere raison se termine.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Raison -> fenetre devant laquelle les PIP doivent s'effacer.</summary>
    Private Shared ReadOnly _raisonsDeCeder As New Dictionary(Of String, IntPtr)

    ''' <summary>Active (<paramref name="ceder"/> = True) ou termine une raison de laisser passer
    ''' <paramref name="fenetre"/> devant les PIP. A appeler depuis le thread de l'interface.</summary>
    Public Shared Sub CederLePremierPlan(raison As String, ceder As Boolean, Optional fenetre As Form = Nothing)
        Dim poignee As IntPtr = IntPtr.Zero
        Try
            If ceder AndAlso fenetre IsNot Nothing AndAlso Not fenetre.IsDisposed Then poignee = fenetre.Handle
        Catch ex As Exception
        End Try
        SyncLock _raisonsDeCeder
            If ceder Then _raisonsDeCeder(raison) = poignee Else _raisonsDeCeder.Remove(raison)
        End SyncLock
        AppliquerPremierPlan()
    End Sub

    ''' <summary>Met chaque PIP a sa place dans l'empilement des fenetres : au premier plan, ou
    ''' derriere la fenetre a laquelle il cede. Appele aussi par les lecteurs a chaque lancement.</summary>
    Public Shared Sub AppliquerPremierPlan()
        For Each pip As Form12 In PipsAffiches()
            Try
                Dim p As Form12 = pip
                ' Chaque PIP appartient au thread de son lecteur : on lui passe la consigne sans l'attendre.
                p.BeginInvoke(New Action(Sub() If Not p.IsDisposed AndAlso p.Visible Then p.Ranger()))
            Catch ex As Exception
                ' un reglage d'affichage ne doit jamais faire tomber la lecture
            End Try
        Next
    End Sub
End Class
