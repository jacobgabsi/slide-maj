Option Strict On
Option Explicit On

''' <summary>
''' Redimensionnement a la souris d'une fenetre calque : molette (pas de 5 %) ou cliquer-glisser
''' vers le haut pour agrandir. La taille choisie n'est signalee qu'une fois le geste termine
''' (bouton relache, ou 0,5 s apres le dernier cran de molette), pour ne l'enregistrer qu'une fois.
''' Le clic droit ouvre le menu de la fenetre de Slide, comme a cote du texte.
'''
''' Ce code etait recopie a l'identique dans FormLegende (legende du bas) et FormLieuBande
''' (lieu et date dans les bandes noires) ; la montre (FormHorloge) garde le sien, qui raisonne
''' en pixels de cote et non en pourcentage.
''' </summary>
Public Class TailleALaSouris

    Private Const PasMolette As Integer = 5

    Private ReadOnly _fenetre As Form
    Private ReadOnly _lire As Func(Of Integer)                 ' pourcentage actuel
    Private ReadOnly _ecrire As Action(Of Integer)             ' applique un nouveau pourcentage
    Private ReadOnly _proprietaire As Func(Of Form)            ' fenetre dont on ouvre le menu
    Private ReadOnly _poidsHorizontal As Double                ' part du deplacement gauche/droite

    ''' <summary>Geste termine : la taille peut etre enregistree.</summary>
    Public Event TailleChoisie(pourcent As Integer)

    Private _glisse As Boolean
    Private _departGlisse As Point
    Private _departHauteur As Integer
    Private _departPourcent As Integer
    Private WithEvents TimerFinMolette As New Timer With {.Interval = 500}

    ''' <summary>Sens du geste, selon le coin ou le texte est accroche : +1 = tirer vers la gauche
    ''' (SensHorizontal) ou vers le haut (SensVertical) agrandit, soit l'oppose du coin bas-droit ;
    ''' -1 = l'inverse (texte accroche a gauche, ou en haut). Modifiable a tout moment.</summary>
    Public Property SensHorizontal As Integer = 1
    Public Property SensVertical As Integer = 1

    ''' <param name="poidsHorizontal">0 = seul le deplacement vertical compte (texte aligne en bas
    ''' des bandes) ; 0,25 = un texte large, ou tirer vers l'interieur de l'ecran agrandit aussi un
    ''' peu (sens : SensHorizontal).</param>
    ''' <param name="surface">Controle qui recoit la souris, s'il couvre la fenetre (le lecteur video
    ''' d'un PIP) ; par defaut, la fenetre elle-meme.</param>
    Public Sub New(fenetre As Form, lire As Func(Of Integer), ecrire As Action(Of Integer),
                   proprietaire As Func(Of Form), Optional poidsHorizontal As Double = 0,
                   Optional surface As Control = Nothing)
        _fenetre = fenetre
        _surface = If(surface, fenetre)
        _lire = lire
        _ecrire = ecrire
        _proprietaire = proprietaire
        _poidsHorizontal = poidsHorizontal

        AddHandler _surface.MouseDown, AddressOf Appui
        AddHandler _surface.MouseMove, AddressOf Deplacement
        AddHandler _surface.MouseUp, AddressOf Relache
        AddHandler _surface.MouseWheel, AddressOf Molette
        AddHandler _fenetre.FormClosed, Sub() TimerFinMolette.Stop()
    End Sub

    Private ReadOnly _surface As Control

    ''' <summary>Quand elle rend Vrai, le bouton gauche n'agrandit pas (un autre geste le prend : par exemple Ctrl + glisser pour DEPLACER la pochette).</summary>
    Public Property IgnorerLAppui As Func(Of Boolean)

    Private Sub Appui(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Exit Sub
        If IgnorerLAppui IsNot Nothing AndAlso IgnorerLAppui.Invoke() Then Exit Sub
        _glisse = True
        _departGlisse = _surface.PointToScreen(e.Location)
        _departHauteur = Math.Max(1, _fenetre.Height)
        _departPourcent = _lire()
        _surface.Capture = True
    End Sub

    Private Sub Deplacement(sender As Object, e As MouseEventArgs)
        If Not _glisse Then Exit Sub
        Dim p As Point = _surface.PointToScreen(e.Location)
        ' Vers l'interieur de l'ecran (le haut pour un texte pose en bas), la hauteur du texte
        ' suit le curseur.
        Dim delta As Double = (_departGlisse.Y - p.Y) * SensVertical +
                              (_departGlisse.X - p.X) * _poidsHorizontal * SensHorizontal
        _ecrire(CInt(Math.Round(_departPourcent * (_departHauteur + delta) / _departHauteur)))
    End Sub

    Private Sub Relache(sender As Object, e As MouseEventArgs)
        If e.Button = MouseButtons.Right Then
            Dim proprietaire As Form = _proprietaire()
            If proprietaire IsNot Nothing Then proprietaire.ContextMenuStrip?.Show(Cursor.Position)
            Exit Sub
        End If
        If Not _glisse Then Exit Sub
        _glisse = False
        _surface.Capture = False
        RaiseEvent TailleChoisie(_lire())
    End Sub

    Private Sub Molette(sender As Object, e As MouseEventArgs)
        _ecrire(_lire() + Math.Sign(e.Delta) * PasMolette)
        ' On n'enregistre qu'a la fin de la serie de crans.
        TimerFinMolette.Stop()
        TimerFinMolette.Start()
    End Sub

    Private Sub TimerFinMolette_Tick(sender As Object, e As EventArgs) Handles TimerFinMolette.Tick
        TimerFinMolette.Stop()
        RaiseEvent TailleChoisie(_lire())
    End Sub

End Class
