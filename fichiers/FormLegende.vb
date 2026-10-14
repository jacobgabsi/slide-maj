Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

''' <summary>
''' Legende "lieu [mois an]" dans un coin de l'ecran (choisi dans Form4, en bas a gauche par defaut), sur fond TRANSPARENT.
'''
''' Elle etait affichee dans une zone de texte de Form3 (Form3.TextBox1), a fond noir opaque :
''' une TextBox ne peut pas etre transparente, et elle ne passerait de toute facon pas proprement
''' au-dessus d'une video VLC. C'est donc une fenetre calque (comme la montre, voir CalqueWin32),
''' posee au-dessus des photos comme des videos. Form3.TextBox1 reste masquee ; elle ne sert plus
''' qu'a garder sa hauteur pour la banniere Chabbat qui se cale au-dessus (Form3, TextBox2).
'''
''' Meme police (Arial) et meme taille qu'avant, texte gris avec ombre pour rester lisible sur
''' une photo claire comme sombre. Laisse passer les clics et ne prend jamais le focus.
''' </summary>
Public Class FormLegende

    Private _reference As Form
    Private _premierPlan As Boolean = True
    Private _texte As String = ""
    Private _taillePoints As Single = 12
    Private _hauteurDuReglage As Integer      ' hauteur de la fenetre pour laquelle _taillePoints a ete donnee

    ' Dans le coin choisi dans Form4 (Bgauche par defaut, Bdroite, Hgauche, Hdroite), a la meme
    ' distance du bord que la montre : 12 pixels a 96 ppp.
    Private Const MargeBordLogique As Integer = 12
    Private _coin As String = "Bgauche"

    ''' <summary>Marge transparente autour du texte dans l'image, pour laisser la place a l'ombre.</summary>
    Private Shared Function MargeInterne(taillePixels As Single) As Integer
        Dim ombre As Single = Math.Max(1.0F, 0.06F * taillePixels)
        Dim flou As Single = Math.Max(0.6F, 0.03F * taillePixels)
        Return CInt(Math.Ceiling(ombre + flou)) + 2
    End Function

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            ' Pas de WS_EX_TRANSPARENT : la legende recoit la molette et le glisser (taille), comme
            ' la montre. Seul son rectangle capte la souris (fond d'alpha 1, voir RendreTexte).
            cp.ExStyle = cp.ExStyle Or WS_EX_LAYERED Or WS_EX_TOOLWINDOW Or WS_EX_NOACTIVATE
            Return cp
        End Get
    End Property

    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    ''' <summary>Affiche <paramref name="texte"/> dans le coin <paramref name="coin"/> de la zone
    ''' client de <paramref name="reference"/>. premierPlan : True en plein ecran (independante, au
    ''' premier plan, comme la montre) ; False en mode fenetre (rattachee a la fenetre, la suit,
    ''' disparait quand on la reduit).</summary>
    ''' <param name="taillePoints">Taille normale (100 %), proportionnelle a la hauteur de la fenetre.</param>
    ''' <param name="pourcent">Taille choisie par l'utilisateur (Form4 ou souris), en % de la normale.</param>
    ''' <param name="coin">Bgauche, Bdroite, Hgauche ou Hdroite (Form4, onglet Photos).</param>
    ''' <param name="centreeSousPip">Avec un PIP dans le meme coin : centree sous lui au lieu d'etre
    ''' alignee sur son bord exterieur (Form4, onglet Photos).</param>
    Public Sub AfficherSur(reference As Form, texte As String, taillePoints As Single, premierPlan As Boolean,
                           Optional pourcent As Integer = 100, Optional coin As String = "Bgauche",
                           Optional centreeSousPip As Boolean = False)
        Rattacher(reference)
        PreparerSouris()
        _texte = If(texte, "").Trim()
        _taillePoints = Math.Max(4.0F, taillePoints)
        _hauteurDuReglage = reference.ClientSize.Height
        _pourcent = Math.Max(PourcentMini, Math.Min(PourcentMaxi, pourcent))
        _premierPlan = premierPlan
        _centreeSousPip = centreeSousPip
        AppliquerCoin(coin)
        CalqueWin32.DefinirPremierPlan(Me, reference, premierPlan)
        Rafraichir()
    End Sub

    ''' <summary>Change de coin en direct (apercu de Form4).</summary>
    Public Sub DefinirCoin(coin As String)
        AppliquerCoin(coin)
        If Me.Visible Then Rafraichir()
    End Sub

    Private _centreeSousPip As Boolean

    ''' <summary>Centree sous le PIP ou alignee sur son bord, en direct (apercu de Form4).</summary>
    Public Sub DefinirCentreeSousPip(centree As Boolean)
        If centree = _centreeSousPip Then Exit Sub
        _centreeSousPip = centree
        If Me.Visible Then Rafraichir()
    End Sub

    Private Sub AppliquerCoin(coin As String)
        _coin = If(String.IsNullOrEmpty(coin), "Bgauche", coin)
        ' Le glisser agrandit en tirant vers l'interieur de l'ecran, quel que soit le coin.
        If _souris IsNot Nothing Then
            _souris.SensHorizontal = If(EstAGauche, -1, 1)
            _souris.SensVertical = If(EstEnHaut, -1, 1)
        End If
    End Sub

    Private ReadOnly Property EstEnHaut As Boolean
        Get
            Return _coin.StartsWith("H", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    Private ReadOnly Property EstAGauche As Boolean
        Get
            Return _coin.EndsWith("gauche", StringComparison.OrdinalIgnoreCase)
        End Get
    End Property

    Public Sub Masquer()
        Rattacher(Nothing)
        _texte = ""
        Me.Hide()
    End Sub

    Private Sub Rattacher(reference As Form)
        If ReferenceEquals(_reference, reference) Then Exit Sub
        If _reference IsNot Nothing Then
            RemoveHandler _reference.LocationChanged, AddressOf ReferenceDeplacee
            RemoveHandler _reference.SizeChanged, AddressOf ReferenceDeplacee
            RemoveHandler _reference.ClientSizeChanged, AddressOf ReferenceDeplacee
        End If
        _reference = reference
        If _reference IsNot Nothing Then
            AddHandler _reference.LocationChanged, AddressOf ReferenceDeplacee
            AddHandler _reference.SizeChanged, AddressOf ReferenceDeplacee
            ' La bordure qui apparait / disparait (plein ecran <-> fenetre) ne change que la zone client.
            AddHandler _reference.ClientSizeChanged, AddressOf ReferenceDeplacee
        End If
    End Sub

    Private Sub ReferenceDeplacee(sender As Object, e As EventArgs)
        Rafraichir()
    End Sub

    Private Sub Rafraichir()
        If _reference Is Nothing OrElse _reference.IsDisposed OrElse _texte = "" Then
            If Me.Visible Then Me.Hide()
            Exit Sub
        End If
        ' En mode fenetre, la legende disparait avec la fenetre de Slide.
        If Not _premierPlan AndAlso
           (Not _reference.Visible OrElse _reference.WindowState = FormWindowState.Minimized) Then
            If Me.Visible Then Me.Hide()
            Exit Sub
        End If

        ' La taille donnee a l'affichage de la photo valait pour la hauteur de fenetre d'alors. Si la
        ' fenetre a change depuis (passage en plein ecran...), on la met a l'echelle tout de suite,
        ' sans attendre la photo suivante - ni la fin d'une longue video.
        Dim points As Single = _taillePoints
        If _hauteurDuReglage > 0 AndAlso _reference.ClientSize.Height > 0 Then
            points = _taillePoints * _reference.ClientSize.Height / _hauteurDuReglage
        End If
        Dim taillePixels As Single = points * _pourcent / 100.0F * _reference.DeviceDpi / 72.0F
        Dim zone As Rectangle = _reference.RectangleToScreen(_reference.ClientRectangle)
        Dim image As Bitmap = RendreTexte(_texte, taillePixels)
        ' Agrandie, une longue legende ne doit jamais depasser la largeur de la fenetre.
        Dim largeurMax As Integer = CInt(zone.Width * 0.95)
        If image.Width > largeurMax AndAlso largeurMax > 20 Then
            taillePixels *= CSng(largeurMax / image.Width)
            image.Dispose()
            image = RendreTexte(_texte, taillePixels)
        End If
        Using image
            ' Le bord des LETTRES (et non de l'image, qui a une marge pour l'ombre) tombe a
            ' MargeBordLogique du bord lateral de la fenetre, dans le coin choisi. En hauteur,
            ' l'image touche le bord : sa marge interne suffit, comme avant en bas.
            Dim marge As Integer = CInt(MargeBordLogique * _reference.DeviceDpi / 96.0)
            Dim x As Integer = If(EstAGauche,
                                  zone.Left + marge - MargeInterne(taillePixels),
                                  zone.Right - marge - image.Width + MargeInterne(taillePixels))
            ' Un PIP dans le meme coin (juste au-dessus de l'etiquette) : les lettres s'alignent sur
            ' son bord exterieur - fin de l'etiquette sur son bord droit, ou debut sur son bord
            ' gauche - ou, au choix (Form4), l'etiquette est centree sous lui (demande de Jacob).
            Dim pip As Rectangle? = Form12.ZoneDuPipDansLeCoin(_coin)
            If pip.HasValue Then
                Dim p As Rectangle = pip.Value
                If _centreeSousPip Then
                    x = p.Left + (p.Width - image.Width) \ 2
                    ' Plus large que le PIP : elle ne doit pas sortir de la fenetre pour autant.
                    x = Math.Min(x, zone.Right - marge - image.Width + MargeInterne(taillePixels))
                    x = Math.Max(x, zone.Left + marge - MargeInterne(taillePixels))
                Else
                    x = If(EstAGauche,
                           p.Left - MargeInterne(taillePixels),
                           p.Right - image.Width + MargeInterne(taillePixels))
                End If
            End If
            Dim y As Integer = If(EstEnHaut, zone.Top, zone.Bottom - image.Height)
            Dim voulu As New Rectangle(x, y, image.Width, image.Height)
            If Me.Bounds <> voulu Then Me.Bounds = voulu
            If Not Me.Visible Then Me.Show()
            CalqueWin32.Appliquer(Me, image)
        End Using
        ' La legende ne bouge pas : c'est le PIP qui remonte s'il la recouvre (demande de Jacob).
        Form12.EcarterDeLaLegende()
        If _premierPlan Then CalqueWin32.ReaffirmerPremierPlan(Me)
    End Sub

    ''' <summary>Le texte sur fond transparent, avec une marge pour l'ombre.</summary>
    Public Shared Function RendreTexte(texte As String, taillePixels As Single) As Bitmap
        Dim ombre As Single = Math.Max(1.0F, 0.06F * taillePixels)
        Dim flou As Single = Math.Max(0.6F, 0.03F * taillePixels)

        Using chemin As New GraphicsPath(), arial As FontFamily = FormHorloge.PoliceChoisieOu("Arial")
            chemin.AddString(texte, arial, CInt(FontStyle.Regular), taillePixels, PointF.Empty, StringFormat.GenericTypographic)
            Dim b As RectangleF = chemin.GetBounds()
            Dim marge As Integer = MargeInterne(taillePixels)
            Dim largeur As Integer = CInt(Math.Ceiling(b.Width)) + 2 * marge
            Dim hauteur As Integer = CInt(Math.Ceiling(b.Height)) + 2 * marge
            Using m As New Matrix()
                m.Translate(marge - b.Left, marge - b.Top)
                chemin.Transform(m)
            End Using

            Dim image As New Bitmap(Math.Max(1, largeur), Math.Max(1, hauteur), PixelFormat.Format32bppArgb)
            Using g As Graphics = Graphics.FromImage(image)
                ' Fond invisible (alpha 1) : tout le rectangle de la legende recoit la souris,
                ' pas seulement les traits des lettres.
                g.Clear(Color.FromArgb(1, 0, 0, 0))
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.PixelOffsetMode = PixelOffsetMode.HighQuality
                ' Ombre douce (meme principe que les horaires de Chabbat dans la montre)
                Using pinceau As New SolidBrush(Color.FromArgb(55, 0, 0, 0))
                    For Each d As PointF In {New PointF(0, 0), New PointF(flou, 0), New PointF(-flou, 0),
                                             New PointF(0, flou), New PointF(0, -flou), New PointF(flou, flou)}
                        g.TranslateTransform(ombre + d.X, ombre + d.Y)
                        g.FillPath(pinceau, chemin)
                        g.ResetTransform()
                    Next
                End Using
                ' Gris de l'ancienne legende (DarkGray)
                Using remplissage As New SolidBrush(Color.FromArgb(255, 169, 169, 169))
                    g.FillPath(remplissage, chemin)
                End Using
            End Using
            Return image
        End Using
    End Function

    Private Sub FormLegende_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        Rattacher(Nothing)
        RemoveHandler Form12.PlaceChangee, AddressOf PipABouge
    End Sub

    ''' <summary>Le PIP est apparu, a bouge ou a disparu : l'etiquette se realigne. L'evenement
    ''' arrive du thread du PIP : on repasse par celui de l'etiquette, sans l'attendre.</summary>
    Private Sub PipABouge()
        Try
            If Me.IsDisposed OrElse Not Me.IsHandleCreated Then Exit Sub
            Me.BeginInvoke(New Action(Sub()
                                          If Not Me.IsDisposed AndAlso Me.Visible Then Rafraichir()
                                      End Sub))
        Catch ex As Exception
            ' fenetre en cours de fermeture
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Place reservee a la legende
    '
    ' Le PIP (Form12) est une petite fenetre video posee dans un coin de Form3 pendant que le
    ' diaporama continue derriere. En bas a droite (sa position par defaut), il recouvrait la
    ' legende : c'est LUI qui remonte, la legende ne bouge pas.
    '
    ' La hauteur est calculee a partir de la taille reglee, sans attendre qu'une legende soit
    ' affichee : au moment ou le PIP se place, la photo en cours n'en a peut-etre pas (pas de lieu
    ' ni de date, mosaique, video...), mais les photos continuent de tourner et la suivante en
    ' aura une. Elle ne depend pas du texte : la police fixe la hauteur des lettres.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Hauteur, en pixels, de la bande du bas a reserver a la legende.</summary>
    ''' <param name="taillePoints">Taille normale (100 %), voir Module1.TaillePointsLegende.</param>
    ''' <param name="pourcent">Taille choisie par l'utilisateur, en % de la normale.</param>
    Public Shared Function HauteurReservee(reference As Form, taillePoints As Single, pourcent As Integer) As Integer
        If reference Is Nothing OrElse reference.IsDisposed Then Return 0
        Dim taillePixels As Single = Math.Max(4.0F, taillePoints) *
                                     Math.Max(PourcentMini, Math.Min(PourcentMaxi, pourcent)) / 100.0F *
                                     reference.DeviceDpi / 72.0F
        ' Rendu d'un echantillon qui contient les extremes de hauteur (majuscule accentuee,
        ' jambage descendant, parenthese) : meme calcul, marge de l'ombre comprise, que le texte
        ' reellement affiche.
        ' (02/10, demande de Jacob : moins de place entre la legende et le PIP. L'echantillon etait
        '  "Ég(j" - majuscule accentuee et parenthese, plus hautes que le texte habituel - et la
        '  hauteur comptait la marge de l'ombre cote PIP. On reserve maintenant la hauteur d'une
        '  majuscule simple et d'un jambage, sans cette marge : une majuscule accentuee, rare,
        '  passe de 2 ou 3 pixels sous le bord du PIP.)
        Using image As Bitmap = RendreTexte("Egj", taillePixels)
            Return Math.Max(1, image.Height - MargeInterne(taillePixels))
        End Using
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Taille a la souris : molette, ou cliquer-glisser vers l'interieur de l'ecran pour agrandir
    ' (la legende reste accrochee a son coin, voir AppliquerCoin). Le geste lui-meme est dans TailleALaSouris,
    ' partage avec le lieu et la date des bandes noires.
    ' ------------------------------------------------------------------------------------------

    Public Const PourcentMini As Integer = 50
    Public Const PourcentMaxi As Integer = 500

    Private _pourcent As Integer = 100
    Private _souris As TailleALaSouris

    ''' <summary>Leve une fois le geste termine avec la nouvelle taille en %, pour l'enregistrer.</summary>
    Public Event TailleChoisie(pourcent As Integer)

    Private Sub PreparerSouris()
        If _souris IsNot Nothing Then Exit Sub
        ' La legende est large : tirer vers l'interieur de l'ecran compte pour un quart.
        _souris = New TailleALaSouris(Me, Function() _pourcent, AddressOf DefinirPourcent,
                                      Function() _reference, poidsHorizontal:=0.25)
        AddHandler _souris.TailleChoisie, Sub(pourcent As Integer) RaiseEvent TailleChoisie(pourcent)
        ' Une seule fois, comme la souris : l'etiquette suit le PIP (voir PipABouge).
        AddHandler Form12.PlaceChangee, AddressOf PipABouge
    End Sub

    ''' <summary>Change la taille en direct (apercu de la glissiere de Form4).</summary>
    Public Sub DefinirPourcent(pourcent As Integer)
        pourcent = Math.Max(PourcentMini, Math.Min(PourcentMaxi, pourcent))
        If pourcent = _pourcent Then Exit Sub
        _pourcent = pourcent
        If Me.Visible Then Rafraichir()
    End Sub

End Class
