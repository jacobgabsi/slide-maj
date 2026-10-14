Imports System.Drawing.Drawing2D

''' <summary>
''' Annonce de l'enchainement (22/10/2026 : "quand les films sont enchaines, 30 s avant la fin, une jolie barre de progression annonce l'enchainement dans ... s") : une carte sombre,
''' en bas de l'image de Slide, "A suivre dans 27 s", le titre de la suite et une barre qui se remplit jusqu'au changement de film. Elle ne prend ni le clavier ni la souris.
''' Appelee par la boucle de lecture d'un film (Module2.PlayFILM_VLC_P2) tant que la ligne suivante de la minuterie est "a la suite" (Module1.SuiteTitre).
''' Les controles sont dans le Concepteur ; le bord et la barre sont dessines ici.
''' </summary>
Public Class FormAnnonceSuite

    ''' <summary>L'annonce apparait a partir de ce nombre de secondes avant la fin du film.</summary>
    Friend Const Secondes As Integer = 30

    Private Shared _fenetre As FormAnnonceSuite
    Private _restant As Double = Secondes
    Private ReadOnly _accent As Color = Color.FromArgb(10, 132, 255)
    Private ReadOnly _piste As Color = Color.FromArgb(58, 58, 60)

    ''' <summary>Montre ou met a jour l'annonce : <paramref name="restant"/> secondes avant la fin du film. Rien avant 30 s, et plus rien apres la fin.</summary>
    Friend Shared Sub Mettre(restant As Double, titre As String, Optional devant As Form = Nothing)
        If ConfierAuFilPrincipal(Sub() Mettre(restant, titre, devant), "Annonce de la suite") Then Exit Sub
        Try
            If restant > Secondes OrElse restant < 0 Then
                Cacher()
                Exit Sub
            End If
            If _fenetre Is Nothing OrElse _fenetre.IsDisposed Then _fenetre = New FormAnnonceSuite()
            Dim f As FormAnnonceSuite = _fenetre
            f.LabelSuite.Text = titre
            f._restant = restant
            f.LabelDans.Text = "À suivre dans " & Math.Max(0, CInt(Math.Ceiling(restant))) & " s"
            If Not f.Visible Then
                f.Placer(devant)
                f.Show()
            ElseIf Not f._glisse Then
                f.PlacerSelonLibre()                                           ' (la fenetre de Slide a pu bouger : la carte la suit)
            End If
            f.PanelBarre.Invalidate()
        Catch ex As Exception
            DiagLog("Annonce de la suite : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Vrai tant que la carte est a l'ecran.</summary>
    Friend Shared ReadOnly Property EstAffichee As Boolean
        Get
            Return _fenetre IsNot Nothing AndAlso Not _fenetre.IsDisposed AndAlso _fenetre.Visible
        End Get
    End Property

    ''' <summary>Retire l'annonce (fin du film, arret).</summary>
    Friend Shared Sub Cacher()
        ' Toujours dans le fil de l'interface (le film tourne dans son propre fil), et sur une copie locale : pendant Close, un autre appel pouvait
        ' vider _fenetre (NullReferenceException sur Dispose, 23/10/2026). Close() libere deja une fenetre affichee.
        If ConfierAuFilPrincipal(Sub() Cacher(), "Annonce de la suite") Then Exit Sub
        Dim f As FormAnnonceSuite = _fenetre
        _fenetre = Nothing
        Try
            If f IsNot Nothing AndAlso Not f.IsDisposed Then
                f.Close()
                If Not f.IsDisposed Then f.Dispose()
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>En bas, a l'extreme droite de l'image de Slide (ou de l'ecran), sauf si Jacob l'a deplacee / agrandie a la souris.</summary>
    Private Sub Placer(devant As Form)
        _devant = devant
        Dim zone As Rectangle = ZoneImage()
        Dim marge As Integer = Math.Max(8, CInt(zone.Height * 0.015))
        Dim x As Double, y As Double, echelle As Double
        If LirePlaceChoisie(x, y, echelle) Then
            AppliquerEchelle(echelle)
            _libreX = x
            _libreY = y
        Else
            Dim libreL As Integer = zone.Width - Me.Width, libreH As Integer = zone.Height - Me.Height
            _libreX = If(libreL <= 0, 1.0, Math.Max(0.0, Math.Min(1.0, (libreL - marge) / CDbl(libreL))))
            _libreY = If(libreH <= 0, 1.0, Math.Max(0.0, Math.Min(1.0, (libreH - marge) / CDbl(libreH))))
        End If
        PlacerSelonLibre()
        ' La carte suit la fenetre de Slide quand elle bouge ou change de taille.
        If _devant IsNot Nothing AndAlso Not _suitLaFenetre Then
            AddHandler _devant.LocationChanged, AddressOf SuivreLaFenetre
            AddHandler _devant.SizeChanged, AddressOf SuivreLaFenetre
            _suitLaFenetre = True
        End If
    End Sub

    Private _devant As Form
    Private _suitLaFenetre As Boolean
    Private _libreX As Double = 1.0              ' place dans l'image de Slide, de 0 (a gauche / en haut) a 1 (a droite / en bas) de l'espace libre
    Private _libreY As Double = 1.0

    Private Sub SuivreLaFenetre(sender As Object, e As EventArgs)
        If Not _glisse AndAlso Me.Visible AndAlso Not Me.IsDisposed Then PlacerSelonLibre()
    End Sub

    Private Sub FormAnnonceSuite_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        If _devant IsNot Nothing AndAlso _suitLaFenetre Then
            RemoveHandler _devant.LocationChanged, AddressOf SuivreLaFenetre
            RemoveHandler _devant.SizeChanged, AddressOf SuivreLaFenetre
            _suitLaFenetre = False
        End If
    End Sub

    ''' <summary>Pose la carte a sa place relative dans l'image de Slide (suit donc la fenetre).</summary>
    Private Sub PlacerSelonLibre()
        Dim zone As Rectangle = ZoneImage()
        Dim libreL As Integer = Math.Max(0, zone.Width - Me.Width), libreH As Integer = Math.Max(0, zone.Height - Me.Height)
        Dim p As New Point(zone.Left + CInt(_libreX * libreL), zone.Top + CInt(_libreY * libreH))
        If p <> Me.Location Then Me.Location = p
    End Sub

    ''' <summary>Retient la place actuelle comme place relative dans l'image de Slide.</summary>
    Private Sub MemoriserLibre()
        Dim zone As Rectangle = ZoneImage()
        Dim libreL As Integer = zone.Width - Me.Width, libreH As Integer = zone.Height - Me.Height
        _libreX = If(libreL <= 0, 1.0, Math.Max(0.0, Math.Min(1.0, (Me.Left - zone.Left) / CDbl(libreL))))
        _libreY = If(libreH <= 0, 1.0, Math.Max(0.0, Math.Min(1.0, (Me.Top - zone.Top) / CDbl(libreH))))
    End Sub

    ''' <summary>L'image de Slide (sa zone client a l'ecran), sinon l'ecran principal.</summary>
    Private Function ZoneImage() As Rectangle
        Try
            If _devant IsNot Nothing AndAlso Not _devant.IsDisposed AndAlso _devant.Visible AndAlso _devant.WindowState <> FormWindowState.Minimized Then
                Return _devant.RectangleToScreen(_devant.ClientRectangle)
            End If
        Catch ex As Exception
        End Try
        Return Screen.PrimaryScreen.WorkingArea
    End Function

    Private Function DansLaZone(p As Point, zone As Rectangle) As Point
        Return New Point(Math.Max(zone.Left, Math.Min(p.X, zone.Right - Me.Width)), Math.Max(zone.Top, Math.Min(p.Y, zone.Bottom - Me.Height)))
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Deplacer et agrandir comme les PIP (23/10/2026) : cliquer-glisser n'importe ou deplace la carte (elle reste dans l'image de Slide) ; saisir un COIN
    ' la redimensionne (le coin oppose reste en place, le format est garde ; texte et barre suivent). Place et taille sont retenues pour les annonces
    ' suivantes (data\settings\AnnonceSuite.txt, propre au poste). La carte n'est jamais activee : les touches restent a Slide.
    ' ------------------------------------------------------------------------------------------

    Private Const EchelleMini As Double = 0.5
    Private Const EchelleMaxi As Double = 4.0

    Private _echelle As Double = 1.0
    Private _baseClient As Size
    Private ReadOnly _bases As New Dictionary(Of Control, Tuple(Of Rectangle, Single))
    Private _glisse As Boolean
    Private _tailleCoin As Boolean
    Private _coinGauche As Boolean
    Private _coinHaut As Boolean
    Private _coinOppose As Point
    Private _pointeurDepart As Point
    Private _placeDepart As Point

    Private Shared Function FichierPlace() As String
        Return DossierReglages & "\AnnonceSuite.txt"
    End Function

    Private Shared Function LirePlaceChoisie(ByRef x As Double, ByRef y As Double, ByRef echelle As Double) As Boolean
        Try
            If Not System.IO.File.Exists(FichierPlace()) Then Return False
            Dim m As String() = System.IO.File.ReadAllText(FichierPlace()).Trim().Split(";"c)
            x = Double.Parse(m(0), Globalization.CultureInfo.InvariantCulture)
            y = Double.Parse(m(1), Globalization.CultureInfo.InvariantCulture)
            echelle = If(m.Length > 2, Double.Parse(m(2), Globalization.CultureInfo.InvariantCulture), 1.0)
            If x < 0 OrElse x > 1 OrElse y < 0 OrElse y > 1 Then Return False          ' (ancien format : coordonnees d'ecran)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Sub RetenirLaPlace()
        Try
            Dim c As Globalization.CultureInfo = Globalization.CultureInfo.InvariantCulture
            System.IO.File.WriteAllText(FichierPlace(), _libreX.ToString("0.####", c) & ";" & _libreY.ToString("0.####", c) & ";" & _echelle.ToString("0.###", c))
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Garde les dimensions d'origine de la carte et de ses controles (pour agrandir sans deformer).</summary>
    Private Sub MemoriserLesBases()
        If _bases.Count > 0 Then Exit Sub
        _baseClient = Me.ClientSize
        For Each c As Control In Me.Controls
            _bases(c) = Tuple.Create(c.Bounds, c.Font.Size)
        Next
    End Sub

    ''' <summary>Taille de la carte = taille d'origine x echelle : texte, barre et fenetre.</summary>
    Private Sub AppliquerEchelle(echelle As Double)
        MemoriserLesBases()
        echelle = Math.Max(EchelleMini, Math.Min(EchelleMaxi, echelle))
        _echelle = echelle
        Me.SuspendLayout()
        Me.ClientSize = New Size(CInt(Math.Round(_baseClient.Width * echelle)), CInt(Math.Round(_baseClient.Height * echelle)))
        For Each paire In _bases
            Dim c As Control = paire.Key, r As Rectangle = paire.Value.Item1
            Dim ancienne As Font = c.Font
            c.Font = New Font(ancienne.FontFamily, Math.Max(4.0F, CSng(paire.Value.Item2 * echelle)), ancienne.Style)
            If Not c.AutoSize Then
                c.Bounds = New Rectangle(CInt(r.X * echelle), CInt(r.Y * echelle), CInt(r.Width * echelle), CInt(r.Height * echelle))
            Else
                c.Location = New Point(CInt(r.X * echelle), CInt(r.Y * echelle))
            End If
        Next
        Me.ResumeLayout()
        Me.Invalidate()
    End Sub

    ''' <summary>Le pointeur (coordonnees de la carte) est-il dans un coin ? (le sixieme de la petite dimension, au moins 14 pixels)</summary>
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

    Private Sub Souris_Down(sender As Object, e As MouseEventArgs) Handles MyBase.MouseDown, LabelDans.MouseDown, LabelSuite.MouseDown, PanelBarre.MouseDown
        If e.Button <> MouseButtons.Left Then Exit Sub
        MemoriserLesBases()
        _glisse = True
        _pointeurDepart = Cursor.Position
        _placeDepart = Me.Location
        _tailleCoin = CoinSousLePointeur(PointToClient(Cursor.Position), _coinGauche, _coinHaut)
        If _tailleCoin Then _coinOppose = New Point(If(_coinGauche, Me.Right, Me.Left), If(_coinHaut, Me.Bottom, Me.Top))
    End Sub

    Private Sub Souris_Move(sender As Object, e As MouseEventArgs) Handles MyBase.MouseMove, LabelDans.MouseMove, LabelSuite.MouseMove, PanelBarre.MouseMove
        Dim p As Point = Cursor.Position
        If Not _glisse Then
            ' Sans bouton : la fleche annonce le geste (diagonale sur un coin = taille, croix ailleurs = deplacer).
            Dim g As Boolean, h As Boolean
            Dim voulue As Cursor = If(CoinSousLePointeur(PointToClient(p), g, h), If(g = h, Cursors.SizeNWSE, Cursors.SizeNESW), Cursors.SizeAll)
            If Me.Cursor IsNot voulue Then Me.Cursor = voulue
            Exit Sub
        End If
        Dim zone As Rectangle = ZoneImage()
        If _tailleCoin Then
            ' Largeur d'apres la distance du pointeur au coin oppose, au format de la carte ; jamais plus grande que l'image de Slide.
            Dim format As Double = _baseClient.Width / CDbl(Math.Max(1, _baseClient.Height))
            Dim l As Double = Math.Max(Math.Abs(p.X - _coinOppose.X), Math.Abs(p.Y - _coinOppose.Y) * format)
            Dim echelle As Double = Math.Min(l / _baseClient.Width, Math.Min(zone.Width / CDbl(_baseClient.Width), zone.Height / CDbl(_baseClient.Height)))
            AppliquerEchelle(echelle)
            Dim x As Integer = If(_coinGauche, _coinOppose.X - Me.Width, _coinOppose.X)
            Dim y As Integer = If(_coinHaut, _coinOppose.Y - Me.Height, _coinOppose.Y)
            Me.Location = DansLaZone(New Point(x, y), zone)
            Exit Sub
        End If
        Me.Location = DansLaZone(New Point(_placeDepart.X + p.X - _pointeurDepart.X, _placeDepart.Y + p.Y - _pointeurDepart.Y), zone)
    End Sub

    Private Sub Souris_Up(sender As Object, e As MouseEventArgs) Handles MyBase.MouseUp, LabelDans.MouseUp, LabelSuite.MouseUp, PanelBarre.MouseUp
        If Not _glisse Then Exit Sub
        _glisse = False
        _tailleCoin = False
        MemoriserLibre()
        RetenirLaPlace()
    End Sub

    ''' <summary>Jamais le clavier : les touches restent a Slide.</summary>
    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or CalqueWin32.WS_EX_TOOLWINDOW Or CalqueWin32.WS_EX_NOACTIVATE
            Return cp
        End Get
    End Property

    Private Sub FormAnnonceSuite_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        CalqueWin32.DefinirPremierPlanSeul(Me, True)       ' devant tout, sans activer la fenetre
    End Sub

    Private Sub FormAnnonceSuite_Paint(sender As Object, e As PaintEventArgs) Handles MyBase.Paint
        ' Bord arrondi fin et une pastille bleue a gauche du titre.
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Using trait As New Pen(Color.FromArgb(70, 70, 74), 1.5F)
            e.Graphics.DrawRectangle(trait, 0.75F, 0.75F, Me.ClientSize.Width - 1.5F, Me.ClientSize.Height - 1.5F)
        End Using
    End Sub

    ''' <summary>La barre se remplit pendant le compte a rebours.</summary>
    Private Sub PanelBarre_Paint(sender As Object, e As PaintEventArgs) Handles PanelBarre.Paint
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias
        Dim piste As New RectangleF(0, 0, PanelBarre.Width - 1, PanelBarre.Height - 1)
        Dim part As Double = Math.Max(0.0, Math.Min(1.0, 1.0 - _restant / Secondes))
        Using chemin As GraphicsPath = Arrondi(piste, piste.Height / 2),
              fond As New SolidBrush(_piste)
            e.Graphics.FillPath(fond, chemin)
        End Using
        If part > 0 Then
            Dim rempli As New RectangleF(0, 0, Math.Max(piste.Height, CSng(piste.Width * part)), piste.Height)
            Using chemin As GraphicsPath = Arrondi(rempli, rempli.Height / 2),
                  pinceau As New SolidBrush(_accent)
                e.Graphics.FillPath(pinceau, chemin)
            End Using
        End If
    End Sub

    Private Shared Function Arrondi(r As RectangleF, rayon As Single) As GraphicsPath
        Dim chemin As New GraphicsPath()
        Dim d As Single = Math.Max(1.0F, Math.Min(Math.Min(r.Width, r.Height), rayon * 2))
        chemin.AddArc(r.Left, r.Top, d, d, 180, 90)
        chemin.AddArc(r.Right - d, r.Top, d, d, 270, 90)
        chemin.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        chemin.AddArc(r.Left, r.Bottom - d, d, d, 90, 90)
        chemin.CloseFigure()
        Return chemin
    End Function

End Class
