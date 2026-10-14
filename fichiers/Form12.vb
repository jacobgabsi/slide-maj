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
        _souris = New TailleALaSouris(Me, Function() PourcentTaille, AddressOf DefinirPourcent,
                                      Function() Nothing, poidsHorizontal:=0.25, surface:=VlcControl1)
        AddHandler _souris.TailleChoisie, Sub(pourcent As Integer) TaillePipChoisieALaSouris(Me, pourcent)
        OrienterLaSouris()
    End Sub

    ''' <summary>Tirer vers l'interieur de l'image agrandit : le sens depend du coin du PIP.</summary>
    Private Sub OrienterLaSouris()
        If _souris Is Nothing Then Exit Sub
        _souris.SensVertical = If(_coin.StartsWith("H"), -1, 1)
        _souris.SensHorizontal = If(_coin.EndsWith("droite"), 1, -1)
        ' Fleche en diagonale, dans le sens du coin libre du PIP.
        Dim diagonale As Cursor = If(_coin = "Bdroite" OrElse _coin = "Hgauche", Cursors.SizeNWSE, Cursors.SizeNESW)
        If VlcControl1.Cursor IsNot diagonale Then VlcControl1.Cursor = diagonale
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
            If autre._coin.StartsWith("H") <> enHaut Then Continue For
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
            If ReferenceEquals(autre, Me) OrElse autre._zoneFenetre.IsEmpty OrElse autre._coin.StartsWith("H") Then Continue For
            y = Math.Min(y, autre._zoneFenetre.Bottom - autre.Height - ecart)
        Next
        Return y
    End Function

    Private Sub Repositionner()
        If _zoneFenetre.IsEmpty Then Exit Sub
        Dim ecart As Integer = EcartDeLaRangee()
        Dim x As Integer = If(_coin.EndsWith("droite"), _zoneFenetre.Right - Me.Width - ecart, _zoneFenetre.Left + ecart)
        Dim y As Integer = If(_coin.StartsWith("H"), _zoneFenetre.Top + ecart, HautDeLaRangeeDuBas(ecart))
        ' Jamais au-dessus du haut de la fenetre (PIP tres grand).
        y = Math.Max(y, _zoneFenetre.Top)
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
