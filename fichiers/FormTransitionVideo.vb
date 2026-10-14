Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

''' <summary>
''' Transitions animees des videos (demande de Jacob, 07/10 : onglet Videos des reglages, "Animer
''' les vidéos"). Quand une video arrive (apres une photo ou une autre video) ou s'en va (une photo
''' la remplace), l'ancienne image ne disparait plus d'un coup : elle s'efface en fondu, glisse
''' dehors, se replie comme un volet ou s'ouvre en cercle sur la nouvelle.
'''
''' L'image d'une video est dessinee par VLC directement dans sa fenetre : on ne peut pas la
''' rendre transparente ni la decouper comme une photo (voir AnimationPhoto). On anime donc
''' l'ANCIENNE image : une photographie de l'image de Slide est posee par-dessus (Couvrir), le
''' changement se fait dessous, a l'abri, puis cette photographie se retire (Retirer). Dessous,
''' la nouvelle video joue deja, et bouge si les reglages le demandent (VideoVivante).
'''
''' Fenetre calque (UpdateLayeredWindow), rangee juste au-dessus de la fenetre de Slide et SOUS
''' les PIP, la legende et la montre, qui restent donc visibles. Elle ne prend ni le clavier ni la
''' souris (les clics la traversent).
'''
''' La photographie est celle de l'ECRAN a l'endroit de l'image de Slide. PrintWindow, qui ne
''' prendrait que la fenetre de Slide, rend une image NOIRE pour une video de VLC (mesure du
''' 07/10 ; il prend bien une photo, animee ou non). Elle contient donc aussi ce qui est pose sur
''' l'image : un PIP, la legende. Sans consequence tant qu'elle ne bouge pas (la copie est
''' exactement sous l'original) ; c'est pourquoi la poussee n'est pas utilisee quand un PIP est
''' affiche : sa copie glisserait avec l'ancienne image.
'''
''' (A l'origine : simple cache pose pendant le passage d'une video prechargee a la suivante, pour
''' eviter un flash noir. Ce cache n'etait plus utilise.)
''' </summary>
Public Class FormTransitionVideo
    Inherits Form

    ''' <summary>Facon dont l'ancienne image se retire. Memes numeros que la liste "Transition" des
    ''' reglages (Form4.ComboBoxAnimationVideo) et que My.Settings.AnimationVideo.</summary>
    Friend Enum Sortie
        Aucune = -1         ' (pas dans la liste, tiree au hasard si elle est cochee) : l'ancienne image est retiree d'un coup
        AuHasard = 0
        Fondu = 1           ' devient transparente
        Poussee = 2         ' glisse dehors par un cote
        Volet = 3           ' se replie d'un cote vers l'autre
        Cercle = 4          ' un rond s'ouvre depuis le centre
        Zoom = 5            ' fondu, pendant que la nouvelle video arrive agrandie et se pose (VideoVivante)
        FonduAuNoir = 6     ' s'eteint dans le noir, puis la nouvelle image s'allume
        Ouverture = 7       ' deux moities s'ecartent depuis le milieu, comme des portes
        Stores = 8          ' des lames se replient, comme un store
        FonduLent = 9       ' fondu de deux secondes et demie
        SansTransition = 10 ' "Aucune" de la liste des reglages (13/10) : l'ancienne image est retiree d'un coup
    End Enum

    Private Const DureeSecondes As Double = 1.0
    Private Const DureeLente As Double = 2.5
    Private Const DureeFlashMontee As Integer = 180       ' ms : le flash blanc apparait
    Private Const DureeFlashDescente As Double = 0.8      ' s : il s'efface sur la nouvelle video
    Private Const Lames As Integer = 12
    Private Const AttenteMaxiSecondes As Double = 20      ' jamais d'ancienne image oubliee a l'ecran

    Private Shared _fenetre As FormTransitionVideo
    Private Shared ReadOnly _hasard As New Random()
    Private Shared _prochaine As Sortie = Sortie.Fondu      ' choisie des que l'ancienne image est posee
    Private Shared _flash As Boolean                        ' entre deux videos : flash blanc au lieu de la photographie
    Private Shared _horizontalSeul As Boolean              ' entre deux videos : la poussee se fait a gauche ou a droite

    ''' <summary>Vrai si l'ancienne image, posee, va se retirer en "Zoom" : la video qui arrive doit
    ''' alors commencer agrandie (voir VideoVivante.Demarrer).</summary>
    Friend Shared ReadOnly Property ArriveeEnZoom As Boolean
        Get
            Return Couvre AndAlso _prochaine = Sortie.Zoom
        End Get
    End Property

    Private _zone As Rectangle                      ' l'image de Slide, a l'ecran
    Private _maniere As Sortie
    Private _sensX, _sensY As Integer
    Private _enSortie As Boolean
    Private ReadOnly _depuis As New Stopwatch()
    ' Regularite (mesure du 07/10) : un minuteur de 15 ms, dans la boucle d'attente de Slide qui dort
    ' 10 ms, ne bat que 41 fois par seconde ; l'horloge de Windows mise a 1 ms pendant le retrait
    ' (timeBeginPeriod) et un minuteur de 10 ms : 62 fois par seconde.
    Private ReadOnly _minuteur As New Timer With {.Interval = 10}
    Private _horlogeFine As Boolean
    ''' <summary>Nombre d'etapes du dernier retrait (pour le mesurer).</summary>
    Friend Shared Etapes As Integer

    Public Sub New()
        Me.FormBorderStyle = FormBorderStyle.None
        Me.ShowInTaskbar = False
        Me.StartPosition = FormStartPosition.Manual
        AddHandler _minuteur.Tick, AddressOf ChaqueInstant
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

    ''' <summary>Vrai tant que l'ancienne image couvre celle de Slide, avant de se retirer.</summary>
    Public Shared ReadOnly Property Couvre As Boolean
        Get
            Dim f As FormTransitionVideo = _fenetre
            Return f IsNot Nothing AndAlso Not f.IsDisposed AndAlso f.Visible AndAlso Not f._enSortie
        End Get
    End Property

    ''' <summary>Photographie l'image de <paramref name="reference"/> et la pose par-dessus : ce qui
    ''' change ensuite dans la fenetre ne se voit pas. Renvoie False si les videos ne sont pas
    ''' animees ou si ce n'est pas possible (fenetre reduite...) : le changement se fait a vue.</summary>
    ''' <param name="entreVideos">Une video en suit une autre (21/10/2026 : "pour eviter les coupures entre deux videos consecutives, pousser la prochaine video vers la droite ou la gauche, independamment des parametres des videos animees") :
    ''' la transition "poussee" (a gauche ou a droite) est faite meme si les videos animees sont desactivees, quel que soit le reglage de transition.</param>
    Public Shared Function Couvrir(reference As Form, Optional entreVideos As Boolean = False, Optional forcer As Boolean = False) As Boolean
        Try
            Dim voile As Boolean = forcer AndAlso Not entreVideos          ' photo <-> video : voile noir ; entre deux videos : poussee de cote
            Dim zone As Rectangle
            If reference IsNot Nothing AndAlso Not reference.IsDisposed Then zone = reference.RectangleToScreen(reference.ClientRectangle)
            If (Not AnimerLesVideos AndAlso Not entreVideos AndAlso Not forcer) OrElse reference Is Nothing OrElse reference.IsDisposed OrElse Not reference.Visible OrElse
               reference.WindowState = FormWindowState.Minimized OrElse zone.Width < 16 OrElse zone.Height < 16 Then
                ' Pas de transition : le changement se fait a vue. La copie vivante de la video qui
                ' finit (fond flou, mouvement) est retiree tout de suite : elle est cadree pour ELLE,
                ' pas pour l'image qui arrive.
                VideoVivante.Arreter()
                Return False
            End If
            If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then _fenetre = New FormTransitionVideo()
            Dim f As FormTransitionVideo = _fenetre
            ' Photographie prise directement par GDI (BitBlt de l'ecran vers une image en memoire, posee telle quelle dans la fenetre calque) :
            ' plus de bitmap .NET de 33 Mo en 4K, ni de boucle octet par octet, ni de GetHbitmap. Mesure du 21/10/2026 sur le PC lent : la prise
            ' coutait ~400 ms pendant lesquelles l'ancienne image restait figee (la seconde d'arret avant la transition).
            Dim ecranDC As IntPtr = GetDC(IntPtr.Zero)
            Dim memoireDC As IntPtr = CreateCompatibleDC(ecranDC)
            Dim hBitmap As IntPtr = CreateCompatibleBitmap(ecranDC, zone.Width, zone.Height)
            Dim ancienne As IntPtr = SelectObject(memoireDC, hBitmap)
            Try
                If voile Then
                    PatBlt(memoireDC, 0, 0, zone.Width, zone.Height, BLACKNESS)       ' voile noir (le blanc fatiguait les yeux, 21/10/2026), pas une photographie
                Else
                    BitBlt(memoireDC, 0, 0, zone.Width, zone.Height, ecranDC, zone.X, zone.Y, SRCCOPY)
                End If
                ' VLC n'accepte pas toujours d'etre photographie (sortie video du PC) : la photographie est alors toute noire, et la poussee
                ' faisait glisser une grande bande noire sur la nouvelle video (22/10/2026, vu sur l'autre PC). Rien n'est pose : le changement se fait a vue.
                If Not voile AndAlso EstToutNoir(memoireDC, zone.Width, zone.Height) Then
                    DiagLog("Transition video : photographie noire ou verte (l'image de la video ne se laisse pas photographier), transition sautee")
                    VideoVivante.Arreter()
                    Return False
                End If
                f._minuteur.Stop()
                f._enSortie = False
                f._zone = zone
                SetWindowRgn(f.Handle, IntPtr.Zero, False)
                f.Bounds = zone
                If Not f.Visible Then f.Show()
                f.Bounds = zone
                Dim destination As New Point(zone.Left, zone.Top), taille As New Size(zone.Width, zone.Height), origine As New Point(0, 0)
                Dim melange As New Melange With {.BlendOp = 0, .BlendFlags = 0, .SourceConstantAlpha = CByte(If(voile, 0, 255)), .AlphaFormat = 0}
                UpdateLayeredWindowImage(f.Handle, ecranDC, destination, taille, memoireDC, origine, 0, melange, ULW_ALPHA)
                f.Ranger(reference)
            Finally
                SelectObject(memoireDC, ancienne)
                DeleteObject(hBitmap)
                DeleteDC(memoireDC)
                ReleaseDC(IntPtr.Zero, ecranDC)
            End Try
            If voile Then
                ' Le voile monte en quelques centiemes de seconde : ce qui change ensuite dessous ne se voit pas.
                Dim montee As Stopwatch = Stopwatch.StartNew()
                Do While montee.ElapsedMilliseconds < DureeFlashMontee
                    f.Transparence(montee.ElapsedMilliseconds / CDbl(DureeFlashMontee))
                    Threading.Thread.Sleep(3)
                Loop
                f.Transparence(1)
            End If
            f._depuis.Restart()
            f._minuteur.Start()                     ' (garde : voir AttenteMaxiSecondes)
            ' La video qui bougeait dessous est maintenant cachee par sa photographie : sa copie
            ' vivante s'arrete (la suivante aura la sienne).
            VideoVivante.Arreter()
            ' Facon dont cette image se retirera : celle des reglages, ou tiree au sort. Un PIP est
            ' affiche : sa copie, prise avec l'image, glisserait avec elle ; volet a la place.
            Dim maniere As Sortie = Sortie.AuHasard
            If AnimationVideo > 0 AndAlso [Enum].IsDefined(GetType(Sortie), AnimationVideo) Then maniere = CType(AnimationVideo, Sortie)
            If maniere = Sortie.SansTransition Then maniere = Sortie.Aucune
            ' Au hasard : parmi les transitions cochees dans les reglages ("Choix du hasard...")
            '  ; d'origine, les transitions douces (fondu, zoom, poussee, fondu au noir).
            ' Le numero 0 y veut dire "aucune" : l'ancienne image est retiree d'un coup.
            If maniere = Sortie.AuHasard Then
                Dim tiree As Integer = TirageAuHasard.Tirer(HasardVideoTransition, TirageAuHasard.VideoTransitionParDefaut, Sortie.FonduLent, _hasard,
                                                            If(PipsActifs() > 0, CInt(Sortie.Poussee), -1))
                maniere = If(tiree = 0, Sortie.Aucune, CType(tiree, Sortie))
            End If
            If maniere = Sortie.Poussee AndAlso PipsActifs() > 0 Then maniere = Sortie.Volet
            ' Entre deux videos : toujours une poussee de cote (un volet si un PIP est affiche : sa copie glisserait avec l'ancienne image), a gauche ou a droite.
            _flash = voile
            _horizontalSeul = entreVideos
            If voile Then maniere = Sortie.Fondu
            If entreVideos Then maniere = If(PipsActifs() > 0, Sortie.Volet, Sortie.Poussee)
            _prochaine = maniere
            Return True
        Catch ex As Exception
            DiagLog("Transition video : " & ex.Message)
            Masquer(True)
            Return False
        End Try
    End Function

    ''' <summary>L'ancienne image se retire, de la facon choisie dans les reglages (ou tiree au
    ''' sort). Sans effet si elle n'est pas posee. Rend la main tout de suite : le mouvement se
    ''' fait pendant que le diaporama continue.</summary>
    Public Shared Sub Retirer()
        Try
            Dim f As FormTransitionVideo = _fenetre
            If f Is Nothing OrElse f.IsDisposed OrElse Not f.Visible OrElse f._enSortie Then Exit Sub
            If _prochaine = Sortie.Aucune Then
                f.Finir()                               ' aucune transition (tiree au hasard) : d'un coup
                Exit Sub
            End If
            Dim cote As Integer = If(_horizontalSeul, _hasard.Next(2), _hasard.Next(4))          ' (entre deux videos : a gauche ou a droite seulement)
            f.Commencer(_prochaine, If(cote = 0, 1, If(cote = 1, -1, 0)), If(cote = 2, 1, If(cote = 3, -1, 0)))
        Catch ex As Exception
            Masquer(True)
        End Try
    End Sub

    ''' <summary>Retire l'ancienne image d'un coup. Sans effet pendant qu'elle se retire d'elle-meme
    ''' (sauf <paramref name="toutDeSuite"/>) : AfficheVideo l'appelle en sortant, dans tous les cas.</summary>
    Public Shared Sub Masquer(Optional toutDeSuite As Boolean = False)
        Try
            Dim f As FormTransitionVideo = _fenetre
            If f Is Nothing OrElse f.IsDisposed OrElse Not f.Visible Then Exit Sub
            If f._enSortie AndAlso Not toutDeSuite Then Exit Sub
            f.Finir()
        Catch ex As Exception
        End Try
    End Sub

    Friend Sub Commencer(maniere As Sortie, sensX As Integer, sensY As Integer)
        _maniere = maniere
        _sensX = sensX
        _sensY = sensY
        _enSortie = True
        _duree = If(_flash, DureeFlashDescente, If(maniere = Sortie.FonduLent, DureeLente, If(_horizontalSeul, 0.45, DureeSecondes)))
        Etapes = 0
        If maniere = Sortie.FonduAuNoir Then
            ' Un fond noir juste sous l'ancienne image : elle s'eteint dessus, puis il s'efface.
            If _noir Is Nothing OrElse _noir.IsDisposed Then _noir = New FenetreNoire()
            _noir.Bounds = _zone
            SetLayeredWindowAttributes(_noir.Handle, 0, 255, LWA_ALPHA)
            If Not _noir.Visible Then _noir.Show()
            _noir.Bounds = _zone
            SetWindowPos(_noir.Handle, Me.Handle, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        End If
        If Not _horlogeFine Then
            timeBeginPeriod(1)
            _horlogeFine = True
        End If
        _depuis.Restart()
        _minuteur.Start()
    End Sub

    Private _duree As Double = DureeSecondes
    Private _noir As FenetreNoire

    ''' <summary>Fond noir du "fondu au noir" : fenetre calque qui ne prend ni le clavier ni la souris.</summary>
    Private Class FenetreNoire
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
    End Class

    Private Sub ChaqueInstant(sender As Object, e As EventArgs)
        Try
            If Not _enSortie Then
                If _depuis.Elapsed.TotalSeconds > AttenteMaxiSecondes Then Finir()
                Exit Sub
            End If
            Dim t As Double = _depuis.Elapsed.TotalSeconds / _duree
            If t >= 1 Then
                Finir()
            Else
                Etapes += 1
                ' Entre deux videos : depart franc (la poussee demarre tout de suite, sinon l'ancienne image semble figee pendant le debut tres doux), arrivee douce.
                Etape(If(_horizontalSeul, t * (2 - t), t * t * t * (t * (t * 6 - 15) + 10)))      ' (sinon : depart et fin tres doux)
            End If
        Catch ex As Exception
            Finir()
        End Try
    End Sub

    ''' <summary>Met l'ancienne image ou elle doit etre a cette part (0 a 1) de son retrait.</summary>
    Friend Sub Etape(q As Double)
        Dim l As Integer = _zone.Width, h As Integer = _zone.Height
        Select Case _maniere
            Case Sortie.Poussee
                ' La fenetre glisse ; ce qui depasse de l'image de Slide est coupe (elle ne doit
                ' pas deborder sur le bureau quand Slide est en fenetre).
                Dim dx As Integer = CInt(_sensX * q * l), dy As Integer = CInt(_sensY * q * h)
                SetWindowRgn(Me.Handle, CreateRectRgn(-dx, -dy, l - dx, h - dy), False)
                SetWindowPos(Me.Handle, IntPtr.Zero, _zone.Left + dx, _zone.Top + dy, 0, 0, SWP_NOSIZE Or SWP_NOZORDER Or SWP_NOACTIVATE)
            Case Sortie.Volet
                Dim reste As Rectangle
                If _sensX > 0 Then
                    reste = Rectangle.FromLTRB(CInt(l * q), 0, l, h)
                ElseIf _sensX < 0 Then
                    reste = Rectangle.FromLTRB(0, 0, CInt(l * (1 - q)), h)
                ElseIf _sensY > 0 Then
                    reste = Rectangle.FromLTRB(0, CInt(h * q), l, h)
                Else
                    reste = Rectangle.FromLTRB(0, 0, l, CInt(h * (1 - q)))
                End If
                SetWindowRgn(Me.Handle, CreateRectRgn(reste.Left, reste.Top, reste.Right, reste.Bottom), True)
            Case Sortie.Cercle
                Dim rayon As Integer = CInt(Math.Sqrt(CDbl(l) * l + CDbl(h) * h) / 2 * q)
                Dim tout As IntPtr = CreateRectRgn(0, 0, l, h)
                Dim rond As IntPtr = CreateEllipticRgn(l \ 2 - rayon, h \ 2 - rayon, l \ 2 + rayon, h \ 2 + rayon)
                CombineRgn(tout, tout, rond, RGN_DIFF)
                DeleteObject(rond)
                SetWindowRgn(Me.Handle, tout, True)         ' (Windows garde la region : pas de DeleteObject)
            Case Sortie.Ouverture
                ' Deux moities qui s'ecartent du milieu : de cote, ou vers le haut et le bas.
                Dim a As IntPtr, b As IntPtr
                If _sensX <> 0 Then
                    Dim reste As Integer = CInt(l / 2.0 * (1 - q))
                    a = CreateRectRgn(0, 0, reste, h)
                    b = CreateRectRgn(l - reste, 0, l, h)
                Else
                    Dim reste As Integer = CInt(h / 2.0 * (1 - q))
                    a = CreateRectRgn(0, 0, l, reste)
                    b = CreateRectRgn(0, h - reste, l, h)
                End If
                CombineRgn(a, a, b, RGN_OR)
                DeleteObject(b)
                SetWindowRgn(Me.Handle, a, True)
            Case Sortie.Stores
                ' Des lames, couchees ou debout, qui se replient chacune sur elle-meme.
                Dim region As IntPtr = CreateRectRgn(0, 0, 0, 0)
                For i As Integer = 0 To Lames - 1
                    Dim lame As IntPtr
                    If _sensX <> 0 Then
                        Dim debut As Integer = CInt(l * i / CDbl(Lames)), fin As Integer = CInt(l * (i + 1) / CDbl(Lames))
                        lame = CreateRectRgn(debut + CInt((fin - debut) * q), 0, fin, h)
                    Else
                        Dim debut As Integer = CInt(h * i / CDbl(Lames)), fin As Integer = CInt(h * (i + 1) / CDbl(Lames))
                        lame = CreateRectRgn(0, debut + CInt((fin - debut) * q), l, fin)
                    End If
                    CombineRgn(region, region, lame, RGN_OR)
                    DeleteObject(lame)
                Next
                SetWindowRgn(Me.Handle, region, True)
            Case Sortie.FonduAuNoir
                ' Premiere moitie : l'ancienne image s'eteint sur le fond noir ; seconde : le fond
                ' noir s'efface sur la nouvelle image.
                Transparence(If(q < 0.5, 1 - q * 2, 0))
                If _noir IsNot Nothing AndAlso Not _noir.IsDisposed Then
                    SetLayeredWindowAttributes(_noir.Handle, 0, CByte(Math.Max(0, Math.Min(255, 255 * If(q < 0.5, 1, 2 - q * 2)))), LWA_ALPHA)
                End If
            Case Else
                Transparence(1 - q)                         ' fondu, fondu lent, zoom
        End Select
    End Sub

    ''' <summary>Opacite d'ensemble de l'ancienne image (1 : opaque, 0 : invisible).</summary>
    Private Sub Transparence(opacite As Double)
        Dim melange As New Melange With {.SourceConstantAlpha = CByte(Math.Max(0, Math.Min(255, 255 * opacite))), .AlphaFormat = 0}
        UpdateLayeredWindow(Me.Handle, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, 0, melange, ULW_ALPHA)
    End Sub

    Private Sub Finir()
        _minuteur.Stop()
        _enSortie = False
        If _horlogeFine Then
            timeEndPeriod(1)
            _horlogeFine = False
        End If
        Me.Hide()
        SetWindowRgn(Me.Handle, IntPtr.Zero, False)
        If _noir IsNot Nothing AndAlso Not _noir.IsDisposed AndAlso _noir.Visible Then _noir.Hide()
    End Sub

    ''' <summary>Juste au-dessus de la fenetre de Slide, donc sous ses PIP, sa legende, sa montre et
    ''' le texte de ses bandes (voir CalqueWin32.PlacerJusteAuDessus).</summary>
    Private Sub Ranger(reference As Form)
        CalqueWin32.PlacerJusteAuDessus(Me.Handle, reference.Handle)
    End Sub

    ''' <summary>Aucun point transparent dans la photographie (la fenetre calque en tient compte).</summary>
    Private Shared Sub RendreOpaque(image As Bitmap)
        Dim donnees As Imaging.BitmapData = image.LockBits(New Rectangle(0, 0, image.Width, image.Height),
                                                           Imaging.ImageLockMode.ReadWrite, Imaging.PixelFormat.Format32bppArgb)
        Try
            Dim octets(donnees.Stride * image.Height - 1) As Byte
            Marshal.Copy(donnees.Scan0, octets, 0, octets.Length)
            For i As Integer = 3 To octets.Length - 1 Step 4
                octets(i) = 255
            Next
            Marshal.Copy(octets, 0, donnees.Scan0, octets.Length)
        Finally
            image.UnlockBits(donnees)
        End Try
    End Sub

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Private Structure Melange
        Public BlendOp As Byte
        Public BlendFlags As Byte
        Public SourceConstantAlpha As Byte
        Public AlphaFormat As Byte
    End Structure

    Private Const AC_SRC_ALPHA As Byte = 1
    Private Const ULW_ALPHA As Integer = 2
    Private Const RGN_DIFF As Integer = 4
    Private Const RGN_OR As Integer = 2
    Private Const LWA_ALPHA As Integer = 2
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOZORDER As UInteger = &H4
    Private Const SWP_NOACTIVATE As UInteger = &H10

    ''' <summary>Vrai si une grille de points de l'image est noire, ou presque toute d'un vert pur
    ''' (27/10/2026, vu sur un autre PC : la surface video non photographiable donne un "rideau vert"
    ''' entre deux videos, pas du noir).</summary>
    Private Shared Function EstToutNoir(dc As IntPtr, largeur As Integer, hauteur As Integer) As Boolean
        Dim noirs As Integer = 0, verts As Integer = 0
        For i As Integer = 1 To 5
            For j As Integer = 1 To 5
                Dim c As Integer = GetPixel(dc, largeur * i \ 6, hauteur * j \ 6)      ' &H00BBGGRR
                Dim r As Integer = c And &HFF, g As Integer = (c >> 8) And &HFF, b As Integer = (c >> 16) And &HFF
                If c = 0 Then noirs += 1
                If g > 60 AndAlso r < 40 AndAlso b < 40 Then verts += 1
            Next
        Next
        Return noirs = 25 OrElse verts >= 20
    End Function

    <DllImport("gdi32.dll")>
    Private Shared Function GetPixel(dc As IntPtr, x As Integer, y As Integer) As Integer
    End Function

    Private Const BLACKNESS As Integer = &H42
    Private Const SRCCOPY As Integer = &HCC0020
    <DllImport("user32.dll", EntryPoint:="UpdateLayeredWindow")>
    Private Shared Function UpdateLayeredWindowImage(fenetre As IntPtr, dcEcran As IntPtr, ByRef place As Point, ByRef taille As Size, dcImage As IntPtr,
                                                     ByRef origine As Point, couleur As Integer, ByRef melange As Melange, options As Integer) As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function GetDC(fenetre As IntPtr) As IntPtr
    End Function
    <DllImport("user32.dll")>
    Private Shared Function ReleaseDC(fenetre As IntPtr, dc As IntPtr) As Integer
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function CreateCompatibleDC(dc As IntPtr) As IntPtr
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function CreateCompatibleBitmap(dc As IntPtr, largeur As Integer, hauteur As Integer) As IntPtr
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function SelectObject(dc As IntPtr, objet As IntPtr) As IntPtr
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function DeleteDC(dc As IntPtr) As Boolean
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function BitBlt(dcDest As IntPtr, x As Integer, y As Integer, largeur As Integer, hauteur As Integer, dcSource As IntPtr, xs As Integer, ys As Integer, operation As Integer) As Boolean
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function PatBlt(dc As IntPtr, x As Integer, y As Integer, largeur As Integer, hauteur As Integer, operation As Integer) As Boolean
    End Function

    ' (image et place inchangees : seule la transparence d'ensemble est mise a jour)
    <DllImport("user32.dll")>
    Private Shared Function UpdateLayeredWindow(fenetre As IntPtr, dcEcran As IntPtr, place As IntPtr, taille As IntPtr, dcImage As IntPtr,
                                                origine As IntPtr, couleur As Integer, ByRef melange As Melange, options As Integer) As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SetWindowRgn(fenetre As IntPtr, region As IntPtr, redessiner As Boolean) As Integer
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(fenetre As IntPtr, apres As IntPtr, x As Integer, y As Integer, largeur As Integer, hauteur As Integer, options As UInteger) As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SetLayeredWindowAttributes(fenetre As IntPtr, cle As Integer, opacite As Byte, options As Integer) As Boolean
    End Function
    <DllImport("winmm.dll")>
    Private Shared Function timeBeginPeriod(millisecondes As Integer) As Integer
    End Function
    <DllImport("winmm.dll")>
    Private Shared Function timeEndPeriod(millisecondes As Integer) As Integer
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function CreateRectRgn(gauche As Integer, haut As Integer, droite As Integer, bas As Integer) As IntPtr
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function CreateEllipticRgn(gauche As Integer, haut As Integer, droite As Integer, bas As Integer) As IntPtr
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function CombineRgn(resultat As IntPtr, a As IntPtr, b As IntPtr, mode As Integer) As Integer
    End Function
    <DllImport("gdi32.dll")>
    Private Shared Function DeleteObject(objet As IntPtr) As Boolean
    End Function

End Class
