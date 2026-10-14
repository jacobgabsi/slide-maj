Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Threading

''' <summary>
''' Mouvement lent d'une VIDEO pendant qu'elle joue (demande de Jacob, 07/10 : "je voulais que la
''' video bouge", comme les photos animees) : elle avance, recule ou glisse tres lentement. Et,
''' pour une video qui laisse des bandes noires (video en hauteur, 4/3...), de quoi lui faire
''' occuper tout l'ecran : un fond flou anime, ou la video agrandie.
'''
''' L'image d'une video est dessinee par VLC directement dans sa fenetre : Slide ne peut ni
''' l'agrandir ni la deplacer lui-meme. C'est Windows qui le fait : cette fenetre, posee juste
''' au-dessus de l'image de Slide, affiche une copie VIVANTE de ce que Slide montre dessous (la
''' technique des apercus de la barre des taches, DwmRegisterThumbnail), et l'on choisit a chaque
''' instant quelle partie de l'image est copiee, et ou : une partie plus petite, c'est un
''' agrandissement ; une partie decalee, c'est un glissement. La carte graphique fait le travail.
'''
''' Verifie le 07/10 avec une mire : la copie est exacte au point pres, vivante (une video VLC y
''' continue de jouer), et un point de moins dans la partie copiee suffit a la faire bouger.
''' Deux limites de Windows, mesurees aussi : la copie ne peut pas commencer hors de sa fenetre
''' (coordonnees negatives : zone noire), et elle ignore la decoupe de la fenetre (SetWindowRgn).
''' On ne s'en sert donc que pour le mouvement ; l'arrivee d'une video (fondu, volet...) reste
''' faite par l'ancienne image qui se retire PAR-DESSUS (FormTransitionVideo).
'''
''' Fond flou : Windows ne sait pas flouter une copie, mais il accepte la copie d'une copie
''' (mesure du 07/10). Une petite fenetre, cachee DERRIERE celle de Slide, montre la video en tout
''' petit ; sa copie, etiree sur toute l'image et assombrie, est floue. Elle doit faire au moins
''' 23 points de haut (plus petite, Windows l'agrandit et laisse du noir).
'''
''' Regularite (mesure du 07/10) : un minuteur de 15 ms dans le fil de l'interface, dont la boucle
''' d'attente dort 10 ms, ne bat que 41 fois par seconde, avec jusqu'a 50 ms entre deux
''' battements : le mouvement accrochait. Un fil a part, cale sur l'affichage de Windows
''' (DwmFlush), bat 60 fois par seconde a 1 ms pres, meme quand l'interface est occupee 120 ms
''' d'affilee. C'est lui qui place les copies ; le fil de l'interface ne fait que suivre la
''' fenetre de Slide et arreter le mouvement quand il n'a plus lieu d'etre.
'''
''' La fenetre ne prend ni le clavier ni la souris (les clics la traversent) et reste sous les
''' PIP, la legende et la montre.
''' </summary>
Public Class VideoVivante
    Inherits Form

    ''' <summary>Mouvement de la video. Memes numeros que la liste "Mouvement" des reglages
    ''' (Form4.ComboBoxAnimationVideoMouvement) et que My.Settings.AnimationVideoMouvement.</summary>
    Friend Enum Mouvement
        AuHasard = 0
        Avancer = 1
        Reculer = 2
        Glisser = 3
        Aucun = 4
    End Enum

    ' Bandes noires d'une video qui ne remplit pas l'image : deux reglages SEPARES (demande de Jacob,
    ' 07/10 : "fond flou anime, fais-en une option separee").
    '   - FondFlouVideo (case "Fond flou anime", My.Settings.FondFlouVideo) : la meme video,
    '     agrandie, floue et assombrie, remplit le fond et bouge lentement. Independant de
    '     "Animer les videos" : il marche aussi sans transitions ni mouvement.
    '   - AnimationVideoBandes (liste "Bandes noires", My.Settings.AnimationVideoBandes) : 0 les
    '     laisser, 2 agrandir la video (elle grandit jusqu'a remplir l'image, deux fois au plus,
    '     puis defile). Fait partie des videos animees. (La valeur 1 etait le fond flou, quand il
    '     etait dans cette liste : elle vaut maintenant "les laisser".)
    Private Const BandesAgrandir As Integer = 2

    ' Force du mouvement (demande de Jacob, 07/10 : "un zoom plus fort"), choisie dans la liste
    ' "Zoom" des reglages (Form4.ComboBoxAnimationVideoForce, My.Settings.AnimationVideoForce) :
    ' 0 normal, 1 fort, 2 tres fort. Pour chacune : agrandissement maximal, agrandissement gagne
    ' par seconde de video, agrandissement minimal (video courte). Une video agrandie est un peu
    ' moins nette (c'est Windows qui l'agrandit) : a 25 %, cela se voit sur une video tres detaillee.
    Private Shared ReadOnly _zoomsMaxi As Double() = {1.08, 1.15, 1.25}
    Private Shared ReadOnly _zoomsParSeconde As Double() = {0.012, 0.025, 0.045}
    Private Shared ReadOnly _zoomsMini As Double() = {0.03, 0.06, 0.1}
    Private Const PoseSecondes As Double = 1.0      ' arrivee "Zoom" : le temps de se poser
    Private Const ZoomDArrivee As Double = 0.2      ' ... en partant de 20 % plus grand
    Private Const AgrandirMaxi As Double = 2.0      ' "Agrandir" : au-dela, la video (agrandie par Windows) serait trop floue
    Private Const AgrandirDebut As Double = 1.0     ' secondes avant de commencer a grandir, puis duree
    Private Const AgrandirDuree As Double = 2.5
    Private Const FondOpacite As Integer = 115      ' sur 255 : le fond est assombri, la video reste le sujet
    Private Const FondApparition As Double = 1.5    ' secondes
    Private Const ReductionDuFond As Integer = 24   ' le fond est copie 24 fois plus petit, puis etire : flou

    Private Shared _fenetre As VideoVivante
    Private Shared ReadOnly _hasard As New Random()

    ' Ce que le fil du mouvement lit a chaque image : protege par _verrou.
    Private Shared ReadOnly _verrou As New Object()
    Private Shared _enMouvement As Boolean
    Private Shared _fil As Thread
    Private Shared ReadOnly _reveil As New AutoResetEvent(False)
    ''' <summary>Nombre de battements du fil du mouvement depuis le lancement (pour le mesurer).</summary>
    Friend Shared Battements As Long

    Private _reference As Form
    Private _valide As Func(Of Boolean)
    Private _vignette As IntPtr                     ' la video (copie de l'image de Slide)
    Private _vignetteFond As IntPtr                 ' le fond flou (copie de la petite fenetre, etiree)
    Private _vignetteReduite As IntPtr              ' dans la petite fenetre : la video en tout petit
    Private _reduite As FenetreCalque
    Private _format As Double                       ' largeur / hauteur de la video (0 : toute l'image)
    Private _zone As Rectangle                      ' la video dans l'image de Slide
    Private _z0, _z1, _x0, _x1 As Double            ' du debut (0) a la fin (1) : agrandissement, glissement (part de la zone)
    Private _enHauteur As Boolean                   ' le glissement est vertical
    Private _duree As Double
    Private _seposer As Boolean
    Private _agrandir As Boolean                    ' la video grandit pour remplir l'image
    Private _avecFond As Boolean                    ' fond flou dans les bandes noires
    Private _defile0, _defile1 As Double            ' "Agrandir" et fond : quelle part de ce qui depasse est montree (0 a 1)
    Private _dernier, _dernierOu, _dernierFond As Rectangle
    Private _derniereOpacite As Integer
    Private ReadOnly _depuis As New Stopwatch()
    Private ReadOnly _minuteur As New System.Windows.Forms.Timer With {.Interval = 100}

    ''' <summary>Fenetre calque qui ne prend ni le clavier ni la souris.</summary>
    Private Class FenetreCalque
        Inherits Form
        Public Sub New()
            Me.FormBorderStyle = FormBorderStyle.None
            Me.ShowInTaskbar = False
            Me.StartPosition = FormStartPosition.Manual
            Me.BackColor = Color.Black
        End Sub
        Protected Overrides ReadOnly Property CreateParams As CreateParams
            Get
                Dim cp As CreateParams = MyBase.CreateParams
                cp.ExStyle = cp.ExStyle Or CalqueWin32.WS_EX_LAYERED Or CalqueWin32.WS_EX_TOOLWINDOW Or
                             CalqueWin32.WS_EX_NOACTIVATE Or CalqueWin32.WS_EX_TRANSPARENT
                Return cp
            End Get
        End Property
        Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
            Get
                Return True
            End Get
        End Property
        Protected Overrides Sub OnHandleCreated(e As EventArgs)
            MyBase.OnHandleCreated(e)
            SetLayeredWindowAttributes(Me.Handle, 0, 255, LWA_ALPHA)    ' entierement opaque
        End Sub
    End Class

    Public Sub New()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ShowInTaskbar = False
        Me.StartPosition = FormStartPosition.Manual
        Me.BackColor = Color.Black                  ' les bandes noires d'une video en hauteur
        AddHandler _minuteur.Tick, AddressOf Surveiller
    End Sub

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or CalqueWin32.WS_EX_LAYERED Or CalqueWin32.WS_EX_TOOLWINDOW Or
                         CalqueWin32.WS_EX_NOACTIVATE Or CalqueWin32.WS_EX_TRANSPARENT
            Return cp
        End Get
    End Property

    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        SetLayeredWindowAttributes(Me.Handle, 0, 255, LWA_ALPHA)        ' fenetre calque, entierement opaque
    End Sub

    ''' <summary>Vrai tant qu'une video bouge.</summary>
    Public Shared ReadOnly Property EnMarche As Boolean
        Get
            Dim f As VideoVivante = _fenetre
            Return f IsNot Nothing AndAlso Not f.IsDisposed AndAlso f.Visible
        End Get
    End Property

    ''' <summary>La video qui vient de commencer dans <paramref name="reference"/> se met a bouger,
    ''' selon les reglages. A appeler pendant que l'ancienne image la couvre encore
    ''' (FormTransitionVideo), juste avant qu'elle se retire.</summary>
    ''' <param name="format">Largeur / hauteur de la video (0 si inconnu : toute l'image bouge).</param>
    ''' <param name="dureeSecondes">Temps pendant lequel elle va jouer : le mouvement s'etale dessus.</param>
    ''' <param name="valide">Vrai tant que c'est bien cette sorte d'image qui est a l'ecran ; des
    ''' que ce n'est plus le cas (photo, film, TV...), le mouvement s'arrete.</param>
    Public Shared Sub Demarrer(reference As Form, format As Double, dureeSecondes As Double, valide As Func(Of Boolean))
        Try
            Arreter()
            ' Deux raisons d'etre la : les videos animees (mouvement, video agrandie), ou le fond
            ' flou, qui se regle a part.
            If Not AnimerLesVideos AndAlso Not FondFlouVideo Then Exit Sub
            If reference Is Nothing OrElse reference.IsDisposed OrElse Not reference.Visible OrElse
               reference.WindowState = FormWindowState.Minimized Then Exit Sub
            Dim sorte As Mouvement = Mouvement.Aucun
            Dim seposer As Boolean = False
            If AnimerLesVideos Then
                sorte = Mouvement.AuHasard
                If [Enum].IsDefined(GetType(Mouvement), AnimationVideoMouvement) Then sorte = CType(AnimationVideoMouvement, Mouvement)
                ' Au hasard : parmi les mouvements coches dans les reglages ("Choix du hasard...").
                If sorte = Mouvement.AuHasard Then sorte = CType(TirageAuHasard.Tirer(TirageAuHasard.SansNoirEtBlanc(HasardVideoMouvement, Mouvement.Aucun), TirageAuHasard.VideoMouvementParDefaut, Mouvement.Aucun, _hasard), Mouvement)
                ' Zoom : "Aucun" (13/10) : la video ne bouge pas (la video agrandie des bandes noires, elle, reste).
                If AnimationVideoForce >= 3 Then sorte = Mouvement.Aucun
                ' Arrivee "Zoom" : la video arrive agrandie et se pose, meme si elle ne bouge pas ensuite.
                seposer = FormTransitionVideo.ArriveeEnZoom
            End If

            If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then _fenetre = New VideoVivante()
            Dim f As VideoVivante = _fenetre
            f._reference = reference
            f._valide = valide
            f._format = format
            f._seposer = seposer
            f.Bounds = reference.RectangleToScreen(reference.ClientRectangle)
            f.CalculerLaZone()
            ' Les bandes noires ne comptent que s'il y en a.
            Dim aRemplir As Boolean = f.DesBandes()
            f._agrandir = aRemplir AndAlso AnimerLesVideos AndAlso AnimationVideoBandes = BandesAgrandir
            f._avecFond = aRemplir AndAlso FondFlouVideo
            If sorte = Mouvement.Aucun AndAlso Not seposer AndAlso Not f._agrandir AndAlso Not f._avecFond Then Exit Sub
            f.ChoisirLeMouvement(sorte, dureeSecondes)
            If Not f.Copier() Then Exit Sub
            f._dernier = Rectangle.Empty
            f._dernierOu = Rectangle.Empty
            f._dernierFond = Rectangle.Empty
            f._derniereOpacite = -1
            f._depuis.Restart()
            f.Placer()
            If Not f.Visible Then f.Show()
            f.Bounds = reference.RectangleToScreen(reference.ClientRectangle)
            f.Ranger()
            f._minuteur.Start()
            ' Le fil du mouvement prend la suite.
            SyncLock _verrou
                _enMouvement = True
                If _fil Is Nothing Then
                    _fil = New Thread(AddressOf Battre) With {.IsBackground = True, .Name = "Mouvement de la video"}
                    _fil.Start()
                End If
            End SyncLock
            _reveil.Set()
        Catch ex As Exception
            DiagLog("Video animee : " & ex.Message)
            Arreter()
        End Try
    End Sub

    ''' <summary>La video ne bouge plus : la copie est retiree, l'image de Slide est de nouveau vue
    ''' telle quelle.</summary>
    Public Shared Sub Arreter()
        Try
            Dim f As VideoVivante = _fenetre
            If f Is Nothing OrElse f.IsDisposed Then Exit Sub
            f._minuteur.Stop()
            SyncLock _verrou                        ' (le fil du mouvement n'est pas en train de placer les copies)
                _enMouvement = False
                For Each vignette As IntPtr In {f._vignette, f._vignetteFond, f._vignetteReduite}
                    If vignette <> IntPtr.Zero Then DwmUnregisterThumbnail(vignette)
                Next
                f._vignette = IntPtr.Zero
                f._vignetteFond = IntPtr.Zero
                f._vignetteReduite = IntPtr.Zero
            End SyncLock
            If f.Visible Then f.Hide()
            If f._reduite IsNot Nothing AndAlso Not f._reduite.IsDisposed AndAlso f._reduite.Visible Then f._reduite.Hide()
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Fil du mouvement : une fois par image de l'ecran (DwmFlush attend la prochaine),
    ''' les copies sont mises a leur place du moment. Dort tant qu'aucune video ne bouge.</summary>
    Private Shared Sub Battre()
        Do
            Dim actif As Boolean
            SyncLock _verrou
                actif = _enMouvement
            End SyncLock
            If Not actif Then
                _reveil.WaitOne()
                Continue Do
            End If
            Dim avant As Long = Stopwatch.GetTimestamp()
            ' Composition de Windows arretee (ne devrait pas arriver) : DwmFlush echoue et rend la
            ' main tout de suite ; on ne tourne pas a vide.
            If DwmFlush() <> 0 AndAlso Stopwatch.GetElapsedTime(avant).TotalMilliseconds < 2 Then Thread.Sleep(15)
            Battements += 1
            Try
                SyncLock _verrou
                    Dim f As VideoVivante = _fenetre
                    If _enMouvement AndAlso f IsNot Nothing Then f.Placer()
                End SyncLock
            Catch ex As Exception
            End Try
        Loop
    End Sub

    ''' <summary>Mouvement de cette video. L'ampleur suit sa duree et la force choisie (voir
    ''' _zoomsMaxi). Avancer et reculer se font exactement autour du centre.</summary>
    Friend Sub ChoisirLeMouvement(sorte As Mouvement, dureeSecondes As Double)
        _duree = Math.Max(2, dureeSecondes)
        Dim force As Integer = If(AnimationVideoForce >= 3, 0, Math.Max(0, Math.Min(2, AnimationVideoForce)))
        Dim ampleur As Double = Math.Max(_zoomsMini(force), Math.Min(_zoomsMaxi(force) - 1, _zoomsParSeconde(force) * _duree))
        Dim course As Double = ampleur / 2 * 0.9     ' glissement possible sans sortir de l'image
        Dim sens As Integer = If(_hasard.Next(2) = 0, 1, -1)
        _z0 = 1 : _z1 = 1 : _x0 = 0 : _x1 = 0
        _enHauteur = _format > 0 AndAlso _format < 1
        Select Case sorte
            Case Mouvement.Avancer
                _z1 = 1 + ampleur
            Case Mouvement.Reculer
                _z0 = 1 + ampleur
            Case Mouvement.Glisser
                _z0 = 1 + ampleur : _z1 = 1 + ampleur
                _x0 = -course * sens : _x1 = course * sens
        End Select
        ' Ce qui depasse (video agrandie, fond) defile lentement, du haut vers le bas ou l'inverse,
        ' sans aller jusqu'aux bords.
        If sens > 0 Then
            _defile0 = 0.25 : _defile1 = 0.7
        Else
            _defile0 = 0.7 : _defile1 = 0.25
        End If
    End Sub

    ''' <summary>Place de la video dans l'image de Slide : la plus grande possible a son format,
    ''' centree (c'est ainsi que VLC la dessine). Format inconnu : toute l'image.</summary>
    Private Sub CalculerLaZone()
        Dim l As Integer = Me.ClientSize.Width, h As Integer = Me.ClientSize.Height
        If _format <= 0 OrElse l <= 0 OrElse h <= 0 Then
            _zone = New Rectangle(0, 0, l, h)
            Exit Sub
        End If
        Dim zl As Integer = l, zh As Integer = CInt(l / _format)
        If zh > h Then
            zh = h
            zl = CInt(h * _format)
        End If
        _zone = New Rectangle((l - zl) \ 2, (h - zh) \ 2, Math.Max(1, zl), Math.Max(1, zh))
    End Sub

    ''' <summary>Vrai si la video laisse des bandes noires qui valent d'etre remplies (plus de 4 %
    ''' de l'image).</summary>
    Private Function DesBandes() As Boolean
        Return _zone.Width < Me.ClientSize.Width * 0.96 OrElse _zone.Height < Me.ClientSize.Height * 0.96
    End Function

    ''' <summary>Demande a Windows les copies vivantes (une fois par video) : pour le fond flou, la
    ''' video en tout petit dans la petite fenetre, puis cette petite fenetre etiree ; et, dessinee
    ''' par-dessus parce que demandee en dernier, la video elle-meme.</summary>
    Private Function Copier() As Boolean
        If _avecFond Then PreparerLeFond()
        Dim resultat As Integer = DwmRegisterThumbnail(Me.Handle, _reference.Handle, _vignette)
        If resultat <> 0 OrElse _vignette = IntPtr.Zero Then
            DiagLog("Video animee : Windows refuse la copie de l'image (" & resultat.ToString("X") & ")")
            _vignette = IntPtr.Zero
            Return False
        End If
        Return True
    End Function

    ''' <summary>Les deux copies du fond flou (la video en tout petit, puis cette petite fenetre etiree). Doit passer AVANT la copie de la video : demandee en dernier, c'est elle qui est dessinee par-dessus.</summary>
    Private Sub PreparerLeFond()
        If True Then
            If _reduite Is Nothing OrElse _reduite.IsDisposed Then _reduite = New FenetreCalque()
            PlacerLaPetiteFenetre()
            If Not _reduite.Visible Then _reduite.Show()
            PlacerLaPetiteFenetre()
            If DwmRegisterThumbnail(_reduite.Handle, _reference.Handle, _vignetteReduite) <> 0 OrElse
               DwmRegisterThumbnail(Me.Handle, _reduite.Handle, _vignetteFond) <> 0 Then
                ' Pas de fond flou possible : la video garde ses bandes noires.
                _avecFond = False
                For Each vignette As IntPtr In {_vignetteReduite, _vignetteFond}
                    If vignette <> IntPtr.Zero Then DwmUnregisterThumbnail(vignette)
                Next
                _vignetteReduite = IntPtr.Zero
                _vignetteFond = IntPtr.Zero
                _reduite.Hide()
            End If
        End If
    End Sub

    ''' <summary>Fond flou demande en cours de route : la copie de la video est redemandee APRES lui (elle doit rester dessinee par-dessus).</summary>
    Private Sub MettreLeFondEnPlace()
        Try
            If _vignette <> IntPtr.Zero Then DwmUnregisterThumbnail(_vignette)
            _vignette = IntPtr.Zero
            _avecFond = True
            PreparerLeFond()
            If DwmRegisterThumbnail(Me.Handle, _reference.Handle, _vignette) <> 0 Then _vignette = IntPtr.Zero
            _dernier = Rectangle.Empty
            _dernierOu = Rectangle.Empty
            _dernierFond = Rectangle.Empty
            _derniereOpacite = -1
        Catch ex As Exception
            DiagLog("Video animee : fond flou apres redimensionnement : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La petite fenetre du fond flou : 24 fois plus petite que l'image (23 points de haut
    ''' au moins), dans le coin de l'image de Slide et juste DERRIERE sa fenetre, donc invisible.</summary>
    Private Sub PlacerLaPetiteFenetre()
        Dim image As Rectangle = _reference.RectangleToScreen(_reference.ClientRectangle)
        Dim echelle As Double = Math.Max(1.0 / ReductionDuFond, 24.0 / Math.Max(1, image.Height))
        _reduite.Bounds = New Rectangle(image.Left + 8, image.Top + 8, Math.Max(40, CInt(image.Width * echelle)), Math.Max(24, CInt(image.Height * echelle)))
        SetWindowPos(_reduite.Handle, _reference.Handle, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
    End Sub

    ''' <summary>Fil de l'interface, dix fois par seconde : la copie suit la fenetre de Slide
    ''' (deplacee, redimensionnee, plein ecran) et s'arrete quand il n'y a plus de video.</summary>
    Private Sub Surveiller(sender As Object, e As EventArgs)
        Try
            If _reference Is Nothing OrElse _reference.IsDisposed OrElse Not _reference.Visible OrElse
               _reference.WindowState = FormWindowState.Minimized OrElse (_valide IsNot Nothing AndAlso Not _valide()) Then
                Arreter()
                Exit Sub
            End If
            Dim place As Rectangle = _reference.RectangleToScreen(_reference.ClientRectangle)
            If place <> Me.Bounds Then
                Me.Bounds = place
                If _reduite IsNot Nothing AndAlso Not _reduite.IsDisposed AndAlso _reduite.Visible Then PlacerLaPetiteFenetre()
                SyncLock _verrou
                    CalculerLaZone()
                    _dernier = Rectangle.Empty
                    _dernierOu = Rectangle.Empty
                    _dernierFond = Rectangle.Empty
                    ' Le fond flou etire (destination = toute l'image) doit lui aussi etre redimensionne : sans cela il gardait la taille de l'ANCIENNE fenetre (21/10/2026).
                    _derniereOpacite = -1
                End SyncLock
                ' Des bandes noires apparaissent apres le redimensionnement alors qu'il n'y en avait pas au depart : le fond flou est mis en place maintenant.
                If FondFlouVideo AndAlso Not _avecFond AndAlso _vignetteFond = IntPtr.Zero AndAlso _vignette <> IntPtr.Zero AndAlso DesBandes() Then MettreLeFondEnPlace()
            End If
        Catch ex As Exception
            Arreter()
        End Try
    End Sub

    Private Shared Function Doux(t As Double) As Double
        t = Math.Max(0, Math.Min(1, t))
        Return t * t * t * (t * (t * 6 - 15) + 10)
    End Function

    ''' <summary>Partie de la video copiee en ce moment, pour son mouvement. Plus petite que la
    ''' video : agrandissement ; decalee : glissement. Elle est retrecie du MEME nombre de points
    ''' des deux cotes : l'image reste exactement centree. (Premiere version : quatre bords
    ''' arrondis separement ; ils avancaient chacun a leur tour, et l'image tremblait d'un demi-point.)</summary>
    Friend Function PartieCopiee(secondes As Double) As Rectangle
        Dim p As Double = Math.Max(0, Math.Min(1, secondes / _duree))
        Dim zoom As Double = _z0 + (_z1 - _z0) * p
        If _seposer AndAlso secondes < PoseSecondes Then zoom *= 1 + ZoomDArrivee * (1 - Doux(secondes / PoseSecondes))
        ' Retrait de chaque cote, en points entiers ; la hauteur suit la largeur.
        Dim retraitX As Integer = CInt(Math.Round(_zone.Width * (1 - 1 / zoom) / 2))
        Dim retraitY As Integer = CInt(Math.Round(retraitX * CDbl(_zone.Height) / Math.Max(1, _zone.Width)))
        Dim glisse As Double = _x0 + (_x1 - _x0) * p
        Dim dx As Integer = 0, dy As Integer = 0
        If _enHauteur Then
            dy = Math.Max(-retraitY, Math.Min(retraitY, CInt(Math.Round(glisse * _zone.Height))))
        Else
            dx = Math.Max(-retraitX, Math.Min(retraitX, CInt(Math.Round(glisse * _zone.Width))))
        End If
        Return Rectangle.FromLTRB(_zone.Left + retraitX + dx, _zone.Top + retraitY + dy,
                                  _zone.Right - retraitX + dx, _zone.Bottom - retraitY + dy)
    End Function

    ''' <summary>"Agrandir" : ou la video est dessinee en ce moment (elle grandit depuis sa place
    ''' jusqu'a remplir l'image, deux fois au plus), et quelle part de <paramref name="partie"/>
    ''' y est montree (ce qui depasse de l'image n'est pas copie, et defile lentement).</summary>
    Friend Function PlaceAgrandie(secondes As Double, ByRef partie As Rectangle) As Rectangle
        Dim l As Integer = Me.ClientSize.Width, h As Integer = Me.ClientSize.Height
        If Not _agrandir Then Return _zone
        Dim plein As Double = Math.Min(AgrandirMaxi, Math.Max(l / CDbl(Math.Max(1, _zone.Width)), h / CDbl(Math.Max(1, _zone.Height))))
        Dim grandit As Double = 1 + (plein - 1) * Doux((secondes - AgrandirDebut) / AgrandirDuree)
        ' Place : agrandie autour du centre, sans depasser l'image.
        Dim pl As Integer = Math.Min(l, CInt(Math.Round(_zone.Width * grandit / 2)) * 2)
        Dim ph As Integer = Math.Min(h, CInt(Math.Round(_zone.Height * grandit / 2)) * 2)
        Dim place As New Rectangle((l - pl) \ 2, (h - ph) \ 2, pl, ph)
        ' Part de la video qui tient dans cette place, et laquelle (defilement).
        Dim partL As Double = Math.Min(1, pl / (_zone.Width * grandit)), partH As Double = Math.Min(1, ph / (_zone.Height * grandit))
        Dim defile As Double = _defile0 + (_defile1 - _defile0) * Math.Max(0, Math.Min(1, secondes / _duree))
        Dim sl As Integer = Math.Max(2, CInt(Math.Round(partie.Width * partL))), sh As Integer = Math.Max(2, CInt(Math.Round(partie.Height * partH)))
        partie = New Rectangle(partie.Left + CInt(Math.Round((partie.Width - sl) * If(partL < 1, defile, 0.5))),
                               partie.Top + CInt(Math.Round((partie.Height - sh) * If(partH < 1, defile, 0.5))), sl, sh)
        Return place
    End Function

    ''' <summary>Fond flou : la partie de la video qui, etiree, remplit toute l'image sans la
    ''' deformer (la plus grande possible), et qui defile lentement.</summary>
    Friend Function PartieDuFond(secondes As Double) As Rectangle
        Dim l As Integer = Me.ClientSize.Width, h As Integer = Me.ClientSize.Height
        Dim fl As Integer = _zone.Width, fh As Integer = CInt(Math.Round(_zone.Width * CDbl(h) / Math.Max(1, l)))
        If fh > _zone.Height Then
            fh = _zone.Height
            fl = CInt(Math.Round(_zone.Height * CDbl(l) / Math.Max(1, h)))
        End If
        fl = Math.Max(2, fl) : fh = Math.Max(2, fh)
        Dim defile As Double = _defile1 + (_defile0 - _defile1) * Math.Max(0, Math.Min(1, secondes / _duree))     ' a contresens de la video
        Return New Rectangle(_zone.Left + CInt(Math.Round((_zone.Width - fl) * defile)), _zone.Top + CInt(Math.Round((_zone.Height - fh) * defile)), fl, fh)
    End Function

    ''' <summary>Met les copies a leur place du moment (rien a faire pour celles qui n'ont pas
    ''' change). Appele par le fil du mouvement, sous _verrou.</summary>
    Private Sub Placer()
        If _vignette = IntPtr.Zero Then Exit Sub
        Dim secondes As Double = _depuis.Elapsed.TotalSeconds
        Dim partie As Rectangle = PartieCopiee(secondes)
        Dim ou As Rectangle = PlaceAgrandie(secondes, partie)
        If partie.Width < 2 OrElse partie.Height < 2 Then Exit Sub

        ' Le fond flou, tant que la video ne remplit pas toute l'image ; il apparait en fondu.
        If _vignetteFond <> IntPtr.Zero Then
            Dim resteDesBandes As Boolean = ou.Width < Me.ClientSize.Width OrElse ou.Height < Me.ClientSize.Height
            Dim opacite As Integer = If(resteDesBandes, CInt(FondOpacite * Math.Max(0, Math.Min(1, secondes / FondApparition))), 0)
            Dim fond As Rectangle = PartieDuFond(secondes)
            If fond <> _dernierFond Then
                _dernierFond = fond
                Dim petit As New ReglagesDeVignette With {
                    .Quoi = DWM_TNP_RECTDESTINATION Or DWM_TNP_RECTSOURCE Or DWM_TNP_OPACITY Or DWM_TNP_VISIBLE Or DWM_TNP_SOURCECLIENTAREAONLY,
                    .Destination = New CadreNatif With {.Droite = _reduite.ClientSize.Width, .Bas = _reduite.ClientSize.Height},
                    .Source = New CadreNatif With {.Gauche = fond.Left, .Haut = fond.Top, .Droite = fond.Right, .Bas = fond.Bottom},
                    .Opacite = 255, .Visible = True, .ZoneClienteSeule = True}
                DwmUpdateThumbnailProperties(_vignetteReduite, petit)
            End If
            If opacite <> _derniereOpacite Then
                _derniereOpacite = opacite
                Dim etire As New ReglagesDeVignette With {
                    .Quoi = DWM_TNP_RECTDESTINATION Or DWM_TNP_OPACITY Or DWM_TNP_VISIBLE Or DWM_TNP_SOURCECLIENTAREAONLY,
                    .Destination = New CadreNatif With {.Droite = Me.ClientSize.Width, .Bas = Me.ClientSize.Height},
                    .Opacite = CByte(opacite), .Visible = opacite > 0, .ZoneClienteSeule = True}
                DwmUpdateThumbnailProperties(_vignetteFond, etire)
            End If
        End If

        If partie = _dernier AndAlso ou = _dernierOu Then Exit Sub
        _dernier = partie
        _dernierOu = ou
        Dim reglages As New ReglagesDeVignette With {
            .Quoi = DWM_TNP_RECTDESTINATION Or DWM_TNP_RECTSOURCE Or DWM_TNP_OPACITY Or DWM_TNP_VISIBLE Or DWM_TNP_SOURCECLIENTAREAONLY,
            .Destination = New CadreNatif With {.Gauche = ou.Left, .Haut = ou.Top, .Droite = ou.Right, .Bas = ou.Bottom},
            .Source = New CadreNatif With {.Gauche = partie.Left, .Haut = partie.Top, .Droite = partie.Right, .Bas = partie.Bottom},
            .Opacite = 255, .Visible = True, .ZoneClienteSeule = True}
        DwmUpdateThumbnailProperties(_vignette, reglages)
    End Sub

    ''' <summary>Juste au-dessus de la fenetre de Slide : sous l'ancienne image qui se retire, sous
    ''' les PIP, la legende, la montre et le texte des bandes (voir CalqueWin32.PlacerJusteAuDessus).</summary>
    Private Sub Ranger()
        CalqueWin32.PlacerJusteAuDessus(Me.Handle, _reference.Handle)
    End Sub

    <StructLayout(LayoutKind.Sequential)>
    Private Structure CadreNatif
        Public Gauche As Integer
        Public Haut As Integer
        Public Droite As Integer
        Public Bas As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure ReglagesDeVignette
        Public Quoi As Integer
        Public Destination As CadreNatif
        Public Source As CadreNatif
        Public Opacite As Byte
        <MarshalAs(UnmanagedType.Bool)> Public Visible As Boolean
        <MarshalAs(UnmanagedType.Bool)> Public ZoneClienteSeule As Boolean
    End Structure

    Private Const DWM_TNP_RECTDESTINATION As Integer = &H1
    Private Const DWM_TNP_RECTSOURCE As Integer = &H2
    Private Const DWM_TNP_OPACITY As Integer = &H4
    Private Const DWM_TNP_VISIBLE As Integer = &H8
    Private Const DWM_TNP_SOURCECLIENTAREAONLY As Integer = &H10
    Private Const LWA_ALPHA As Integer = 2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOACTIVATE As UInteger = &H10

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmRegisterThumbnail(destination As IntPtr, source As IntPtr, ByRef vignette As IntPtr) As Integer
    End Function
    <DllImport("dwmapi.dll")>
    Private Shared Function DwmUpdateThumbnailProperties(vignette As IntPtr, ByRef reglages As ReglagesDeVignette) As Integer
    End Function
    <DllImport("dwmapi.dll")>
    Private Shared Function DwmUnregisterThumbnail(vignette As IntPtr) As Integer
    End Function
    <DllImport("dwmapi.dll")>
    Private Shared Function DwmFlush() As Integer
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SetLayeredWindowAttributes(fenetre As IntPtr, cle As Integer, opacite As Byte, options As Integer) As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(fenetre As IntPtr, apres As IntPtr, x As Integer, y As Integer, largeur As Integer, hauteur As Integer, options As UInteger) As Boolean
    End Function

End Class
