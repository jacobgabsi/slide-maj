Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices
Imports System.Threading
Imports W = System.Windows
Imports WC = System.Windows.Controls
Imports WM = System.Windows.Media

''' <summary>
''' Photos animees (demande de Jacob, 07/10 : onglet Photos des reglages, "Animer les photos") :
''' au lieu d'etre posee immobile, chaque photo ARRIVE (fondu, poussee, volet, zoom, flou, cercle...)
''' puis BOUGE tres lentement pendant son affichage (avance, recule, glisse, se balance). Les deux
''' se choisissent dans les reglages, ou sont tires au sort a chaque photo.
'''
''' Le mouvement est fait par la carte graphique (WPF), pas par le dessin habituel de Windows.
''' Mesure du 07/10 : dessinee par GDI+, une image de l'animation prend 15 a 23 ms en 1920 x 1080,
''' mais 57 a 91 ms en 3840 x 2160 et 79 a 122 ms sur l'ecran 5120 x 2160 de Jacob, soit 8 a
''' 18 images par seconde : saccade. La carte graphique, elle, deplace une image deja chargee.
'''
''' Principe : une petite fenetre WPF est posee DANS la zone de la photo (Form3.PictureBox1) et la
''' recouvre tant qu'une photo animee est affichee. Elle est inactive pour la souris : clic droit,
''' double clic et glisser arrivent a la photo comme avant. La photo est cadree exactement comme
''' la photo immobile (meme bande 16:9, meme place) et ne bouge qu'A L'INTERIEUR de ce cadre : les
''' bandes noires, la legende et le lieu restent ou ils sont.
'''
''' Tout ce qui affiche autre chose dans la zone de la photo doit appeler Masquer() : c'est fait
''' dans Module1.DefinirImageAffichee et au debut des affichages immobiles.
''' </summary>
Friend Module AnimationPhoto

    ''' <summary>Facon dont une photo remplace la precedente. Memes numeros que la liste "Arrivée"
    ''' des reglages (Form4.ComboBoxAnimationArrivee) et que My.Settings.AnimationArrivee.</summary>
    Friend Enum Arrivee
        Aucune = -1         ' (pas dans la liste) : la photo est la d'un coup, sous l'ancienne image d'une video qui se retire
        AuHasard = 0
        Fondu = 1           ' apparait par transparence
        Poussee = 2         ' entre par un cote en poussant l'ancienne dehors
        Volet = 3           ' un rideau decouvre la nouvelle photo
        Zoom = 4            ' arrive agrandie et se pose, en fondu
        Flou = 5            ' arrive floue et devient nette, en fondu
        Cercle = 6          ' un rond s'ouvre depuis le centre
        ' (demande de Jacob, 07/10 : "d'autres transitions pour les photos")
        FonduAuNoir = 7     ' l'ancienne s'eteint dans le noir, puis la nouvelle s'allume
        Ouverture = 8       ' la nouvelle se decouvre depuis le milieu, comme des portes qui s'ecartent
        Stores = 9          ' des lames s'ouvrent, comme un store
        Losange = 10        ' un losange s'ouvre depuis le centre
        Recouvrement = 11   ' glisse par-dessus l'ancienne, qui ne bouge pas
        Rotation = 12       ' arrive penchee et agrandie, se redresse en fondu
        Retournement = 13   ' l'ancienne se referme comme une carte qu'on retourne, la nouvelle s'ouvre
        FonduLent = 14      ' fondu de deux secondes
        SansArrivee = 15    ' "Aucune" de la liste des reglages (13/10) : la photo est la d'un coup
    End Enum

    ''' <summary>Mouvement de la photo pendant son affichage. Memes numeros que la liste "Mouvement"
    ''' des reglages (Form4.ComboBoxAnimationMouvement) et que My.Settings.AnimationMouvement.</summary>
    Friend Enum Mouvement
        AuHasard = 0
        Avancer = 1         ' grossit lentement
        Reculer = 2
        Glisser = 3         ' de cote (photo large) ou de haut en bas (photo haute)
        Balancer = 4        ' penche tres legerement d'un cote puis de l'autre
        Aucun = 5           ' immobile : seule l'arrivee est animee
    End Enum

    Private Const ArriveeSecondes As Double = 0.8
    Private Const ArriveeLenteSecondes As Double = 2.0      ' "Fondu lent"
    Private Const ArriveeAuNoirSecondes As Double = 1.3     ' "Fondu au noir" : deux temps
    Private Const ArriveeRetourneeSecondes As Double = 1.0  ' "Retournement" : deux temps aussi
    Private Const NombreDeLames As Integer = 10             ' "Stores"
    Private Const AngleDArrivee As Double = 7               ' "Rotation" : degres au depart

    ' Force du mouvement (demande de Jacob, 07/10 : "un zoom plus fort"), choisie dans la liste
    ' "Zoom" des reglages (Form4.ComboBoxAnimationPhotoForce, My.Settings.AnimationPhotoForce) :
    ' 0 normal, 1 fort, 2 tres fort. Pour chacune : agrandissement maximal, agrandissement gagne
    ' par seconde d'affichage, agrandissement minimal (photo affichee peu de temps), et
    ' inclinaison du balancement en degres. En "normal", une photo affichee 5 secondes grossit de
    ' 6 % ; en "fort" de 12,5 % ; en "tres fort" de 22,5 %.
    Private ReadOnly _zoomsMaxi As Double() = {1.1, 1.2, 1.35}
    Private ReadOnly _zoomsParSeconde As Double() = {0.012, 0.025, 0.045}
    Private ReadOnly _zoomsMini As Double() = {0.03, 0.06, 0.1}
    Private ReadOnly _inclinaisons As Double() = {1.2, 2.0, 3.0}

    Private Function Force() As Integer
        If AnimationPhotoForce >= 3 Then Return 0           ' "Aucun" : pas de mouvement (voir ChoisirLeMouvement)
        Return Math.Max(0, Math.Min(2, AnimationPhotoForce))
    End Function

    ''' <summary>Agrandissement maximal de la photo (1,1 = 10 %), selon la force choisie.</summary>
    Private Function ZoomMaxi() As Double
        Return _zoomsMaxi(Force())
    End Function

    ''' <summary>Photo prete a etre animee : son image (deja cadree, un peu plus grande que sa
    ''' place pour rester nette en zoom) et sa place dans l'image de Slide, en pixels.</summary>
    Friend Class ImagePrete
        Public Image As WM.Imaging.BitmapSource
        Public Zone As Rectangle
        ''' <summary>La meme photo en tout petit : etiree a sa place, elle est floue (arrivee "Flou").
        ''' Mesure du 07/10 : flouter la photo a chaque image (BlurEffect) ne donne que 18 images
        ''' par seconde en 5120 x 2160, 39 en 1920 x 1080 ; etirer une petite image ne coute rien.</summary>
        Public Floue As WM.Imaging.BitmapSource
    End Class

    ''' <summary>Une photo a l'ecran. Il y en a deux pendant une arrivee : celle qui arrive, devant,
    ''' et celle qui part.</summary>
    Friend Class Couche
        Public Fond As WC.Grid                      ' toute l'image de Slide, noire
        Public Cadre As WC.Border                   ' la place de la photo : rien n'en depasse
        Public Mobile As WC.Grid                    ' ce qui bouge dans le cadre : la photo (et sa copie floue)
        Public Photo As WC.Image
        Public Brume As WC.Image                    ' copie floue, sous la photo, pendant l'arrivee "Flou"
        ' Mouvement de la photo dans son cadre, du debut (0) a la fin (1) : agrandissement,
        ' inclinaison (degres), decalage en part de la largeur et de la hauteur de la photo.
        Public Zoom As WM.ScaleTransform
        Public Tourne As WM.RotateTransform
        Public Glisse As WM.TranslateTransform
        Public Z0, Z1, R0, R1, X0, X1, Y0, Y1 As Double
        Public Duree As Double
        ' Arrivee : elle agit sur toute la couche (photo et noir autour).
        Public Maniere As Arrivee
        Public SensX, SensY As Integer              ' poussee et volet : d'ou vient la photo
        Public EntreeZoom As WM.ScaleTransform
        Public EntreeGlisse As WM.TranslateTransform
        Public EntreeTourne As WM.RotateTransform
        Public Rideau As WM.RectangleGeometry
        Public Rond As WM.EllipseGeometry
        Public Lames As WM.RectangleGeometry()      ' "Stores"
        Public Store As WM.GeometryGroup
        Public Carre As WM.RectangleGeometry        ' "Losange" : un carre tourne d'un huitieme de tour
        Public CarreTourne As WM.RotateTransform
        Public DureeArrivee As Double = ArriveeSecondes
        Public Avancement As Double                 ' de l'arrivee, de 0 a 1 (adouci)
        Public ArriveeFinie As Boolean
        Public Depuis As Stopwatch
        Public Active As Boolean
    End Class

    Private _hote As W.Interop.HwndSource
    Private _fond As Control
    Private _poigneeDuFond As IntPtr
    Private _scene As WC.Grid                       ' en pixels de l'image de Slide
    Private _tailleDeLaScene As Size
    Private ReadOnly _couches(1) As Couche
    Private _devant As Integer
    Private _rang As Integer
    Private _enMarche As Boolean
    Private _dejaDit As Boolean
    Private ReadOnly _hasard As New Random()

    ''' <summary>Affiche <paramref name="fichier"/> anime dans <paramref name="fond"/> (la zone de la
    ''' photo). Renvoie False si ce n'est pas possible : l'appelant affiche alors la photo immobile.</summary>
    ''' <param name="format169">Vrai : cadrage en bande 16:9 (comme AffichePhoto169) ; Faux : photo entiere.</param>
    ''' <param name="dureeSecondes">Temps d'affichage d'une photo : le mouvement s'etale dessus.</param>
    Public Function Afficher(fichier As String, fond As Control, format169 As Boolean, dureeSecondes As Double) As Boolean
        Try
            Dim taille As Size = fond.ClientSize
            If taille.Width < 32 OrElse taille.Height < 32 Then Return False
            ' La photo est lue et cadree a part, pendant que la precedente continue de bouger
            ' (une photo de 24 millions de points demande plusieurs dixiemes de seconde).
            Dim maniere As Arrivee = TirerLArrivee()
            Dim noirEtBlanc As Boolean = CouleursDuDiaporama.PhotoEnNoirEtBlanc()      ' reglage "Couleurs" des photos animees
            Dim sepia As Boolean = CouleursDuDiaporama.PhotoEnSepia()                  ' reglage "Couleurs" sur "Sepia"
            Dim preparation As Task(Of ImagePrete) = Task.Run(Function() Preparer(fichier, taille, format169, maniere = Arrivee.Flou, noirEtBlanc, sepia))
            Do Until preparation.IsCompleted
                Application.DoEvents()
                Thread.Sleep(5)
            Loop
            If preparation.IsFaulted Then
                DiagLog("Photo animee : " & IO.Path.GetFileName(fichier) & " illisible (" & preparation.Exception.GetBaseException().Message & "), affichee immobile")
                Return False
            End If
            Installer(fond, taille)
            Montrer(preparation.Result, dureeSecondes, maniere)
            If Not _dejaDit Then
                _dejaDit = True
                DiagLog("Photos animees : en service (image de " & taille.Width & " x " & taille.Height & ", rendu " &
                        If((WM.RenderCapability.Tier >> 16) >= 2, "par la carte graphique", "logiciel, niveau " & (WM.RenderCapability.Tier >> 16)) & ")")
            End If
            Return True
        Catch ex As Exception
            DiagLog("Photo animee : " & ex.Message & " -> photo immobile")
            Masquer()
            Return False
        End Try
    End Function

    ''' <summary>Retire l'animation : la zone de la photo montre de nouveau ce qu'on y dessine.</summary>
    Public Sub Masquer()
        Try
            If _enMarche Then
                RemoveHandler WM.CompositionTarget.Rendering, AddressOf ChaqueImage
                _enMarche = False
            End If
            For Each c As Couche In _couches
                Vider(c)
            Next
            If _hote IsNot Nothing AndAlso Not _hote.IsDisposed Then ShowWindow(_hote.Handle, SW_HIDE)
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Preparation de la photo (dans un autre fil)
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Lit la photo, la remet a l'endroit, la cadre comme la photo immobile et la reduit a
    ''' la taille utile. Memes calculs que Module1.AffichePhoto169 (bande 16:9 remontee d'un
    ''' sixieme pour les visages, jamais etiree) et que le PictureBox en mode Zoom (photo entiere).</summary>
    ''' <param name="avecCopieFloue">Vrai pour l'arrivee "Flou" : prepare aussi la petite copie.</param>
    ''' <param name="noirEtBlanc">Vrai : la photo est mise en gris (reglage "Couleurs", voir CouleursDuDiaporama).</param>
    Friend Function Preparer(fichier As String, taille As Size, format169 As Boolean, avecCopieFloue As Boolean,
                             Optional noirEtBlanc As Boolean = False, Optional sepia As Boolean = False) As ImagePrete
        Using brut As New Bitmap(fichier)
            RemettreALEndroit(brut)
            Dim largeur As Integer = taille.Width, hauteur As Integer = taille.Height
            Dim morceau As Rectangle, zone As Rectangle
            If format169 AndAlso brut.Width / brut.Height >= 1.1 Then
                Dim ratioBande As Double = Math.Min(16 / 9, largeur / hauteur)
                Dim hauteurBande As Integer = CInt(Math.Max(1, Math.Min(brut.Height, brut.Width / ratioBande)))
                Dim hautBande As Integer = CInt((brut.Height - hauteurBande) / 6)
                Dim ratioReel As Double = brut.Width / hauteurBande
                Dim dw As Integer = largeur
                Dim dh As Integer = CInt(largeur / ratioReel)
                If dh > hauteur Then
                    dh = hauteur
                    dw = CInt(hauteur * ratioReel)
                End If
                morceau = New Rectangle(0, hautBande, brut.Width, hauteurBande)
                zone = New Rectangle((largeur - dw) \ 2, (hauteur - dh) \ 2, dw, dh)
            Else
                ' Photo entiere, la plus grande possible sans la deformer, centree.
                Dim echelle As Double = Math.Min(largeur / brut.Width, hauteur / brut.Height)
                Dim dw As Integer = Math.Max(1, Math.Min(largeur, CInt(brut.Width * echelle)))
                Dim dh As Integer = Math.Max(1, Math.Min(hauteur, CInt(brut.Height * echelle)))
                morceau = New Rectangle(0, 0, brut.Width, brut.Height)
                zone = New Rectangle((largeur - dw) \ 2, (hauteur - dh) \ 2, dw, dh)
            End If

            ' Image de travail : assez grande pour rester nette au plus fort du zoom, jamais plus
            ' grande que la photo elle-meme.
            Dim facteur As Double = Math.Min(ZoomMaxi(), morceau.Width / zone.Width)
            Dim tw As Integer = Math.Max(1, CInt(zone.Width * facteur)), th As Integer = Math.Max(1, CInt(zone.Height * facteur))
            Using travail As New Bitmap(tw, th, PixelFormat.Format32bppPArgb)
                Using g As Graphics = Graphics.FromImage(travail), bords As New ImageAttributes()
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality
                    g.CompositingMode = CompositingMode.SourceCopy
                    bords.SetWrapMode(WrapMode.TileFlipXY)          ' pas de lisere sombre sur les bords
                    If sepia Then
                        bords.SetColorMatrix(CouleursDuDiaporama.MatriceDeSepia())
                    ElseIf noirEtBlanc Then
                        bords.SetColorMatrix(CouleursDuDiaporama.MatriceDeGris())
                    End If
                    g.DrawImage(brut, New Rectangle(0, 0, tw, th), morceau.X, morceau.Y, morceau.Width, morceau.Height, GraphicsUnit.Pixel, bords)
                End Using
                Dim prete As New ImagePrete With {.Image = PourWpf(travail), .Zone = zone}
                If avecCopieFloue Then
                    ' Seize fois plus petite : etiree a la place de la photo, elle en garde les
                    ' couleurs et les masses, sans aucun detail.
                    Using petite As New Bitmap(Math.Max(4, tw \ 16), Math.Max(4, th \ 16), PixelFormat.Format32bppPArgb)
                        Using g As Graphics = Graphics.FromImage(petite), bords As New ImageAttributes()
                            g.InterpolationMode = InterpolationMode.HighQualityBicubic
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality
                            g.CompositingMode = CompositingMode.SourceCopy
                            bords.SetWrapMode(WrapMode.TileFlipXY)
                            g.DrawImage(travail, New Rectangle(0, 0, petite.Width, petite.Height), 0, 0, tw, th, GraphicsUnit.Pixel, bords)
                        End Using
                        prete.Floue = PourWpf(petite)
                    End Using
                End If
                Return prete
            End Using
        End Using
    End Function

    ''' <summary>Copie une image GDI+ (32 bits, transparence premultipliee) en image WPF, figee :
    ''' utilisable depuis le fil de l'interface.</summary>
    Private Function PourWpf(image As Bitmap) As WM.Imaging.BitmapSource
        Dim points As BitmapData = image.LockBits(New Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppPArgb)
        Try
            Dim copie As WM.Imaging.BitmapSource = WM.Imaging.BitmapSource.Create(
                image.Width, image.Height, 96, 96, WM.PixelFormats.Pbgra32, Nothing, points.Scan0, points.Stride * image.Height, points.Stride)
            copie.Freeze()
            Return copie
        Finally
            image.UnlockBits(points)
        End Try
    End Function

    ''' <summary>Orientation notee par l'appareil photo (meme correspondance que AffichePhoto169).</summary>
    Private Sub RemettreALEndroit(photo As Bitmap)
        Const Orientation As Integer = &H112
        If Array.IndexOf(photo.PropertyIdList, Orientation) < 0 Then Exit Sub
        Select Case photo.GetPropertyItem(Orientation).Value(0)
            Case 3 : photo.RotateFlip(RotateFlipType.Rotate180FlipNone)
            Case 4 : photo.RotateFlip(RotateFlipType.Rotate180FlipX)
            Case 5, 6 : photo.RotateFlip(RotateFlipType.Rotate90FlipNone)
            Case 8 : photo.RotateFlip(RotateFlipType.Rotate270FlipNone)
        End Select
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Affichage
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Cree (une fois) la fenetre WPF dans la zone de la photo et la met a sa taille.</summary>
    Private Sub Installer(fond As Control, taille As Size)
        If _hote Is Nothing OrElse _hote.IsDisposed OrElse fond IsNot _fond OrElse fond.Handle <> _poigneeDuFond Then
            If _hote IsNot Nothing AndAlso Not _hote.IsDisposed Then _hote.Dispose()
            If _fond IsNot Nothing Then RemoveHandler _fond.Resize, AddressOf FondRedimensionne
            _fond = fond
            _poigneeDuFond = fond.Handle
            ' WS_DISABLED : la souris traverse (clic droit, double clic, glisser restent a la photo).
            Dim reglages As New W.Interop.HwndSourceParameters("Photo animee") With {
                .ParentWindow = fond.Handle,
                .WindowStyle = WS_CHILD Or WS_DISABLED Or WS_CLIPCHILDREN,
                .PositionX = 0, .PositionY = 0, .Width = taille.Width, .Height = taille.Height}
            _hote = New W.Interop.HwndSource(reglages)
            _hote.SizeToContent = W.SizeToContent.Manual
            _hote.CompositionTarget.BackgroundColor = WM.Colors.Black
            ' La scene est en pixels de l'image de Slide ; la boite l'ajuste a la fenetre (ecran en
            ' 150 %, fenetre redimensionnee pendant qu'une photo est affichee).
            _scene = New WC.Grid With {.Background = WM.Brushes.Black, .ClipToBounds = True}
            _hote.RootVisual = New WC.Viewbox With {.Stretch = WM.Stretch.Fill, .Child = _scene}
            _couches(0) = Nothing
            _couches(1) = Nothing
            _tailleDeLaScene = Size.Empty
            AddHandler fond.Resize, AddressOf FondRedimensionne
        End If
        If taille <> _tailleDeLaScene Then
            ' Autre taille d'image : la photo en place, cadree pour l'ancienne, ne sert plus.
            For Each c As Couche In _couches
                Vider(c)
            Next
            _scene.Width = taille.Width
            _scene.Height = taille.Height
            _tailleDeLaScene = taille
        End If
        SetWindowPos(_hote.Handle, IntPtr.Zero, 0, 0, fond.ClientSize.Width, fond.ClientSize.Height, SWP_NOZORDER Or SWP_NOACTIVATE)
    End Sub

    Private Sub FondRedimensionne(sender As Object, e As EventArgs)
        Try
            If _hote IsNot Nothing AndAlso Not _hote.IsDisposed AndAlso _fond IsNot Nothing Then
                SetWindowPos(_hote.Handle, IntPtr.Zero, 0, 0, _fond.ClientSize.Width, _fond.ClientSize.Height, SWP_NOZORDER Or SWP_NOACTIVATE)
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>La nouvelle photo passe devant et commence son arrivee et son mouvement ; l'ancienne
    ''' reste dessous le temps de l'arrivee.</summary>
    Private Sub Montrer(prete As ImagePrete, dureeSecondes As Double, maniere As Arrivee)
        Dim place As Integer = 1 - _devant
        Dim ancienne As Couche = _couches(_devant)
        Dim c As Couche = _couches(place)
        If c Is Nothing Then
            c = NouvelleCouche()
            _scene.Children.Add(c.Fond)
            _couches(place) = c
        End If
        ' Photos qui se suivent de pres (touche N) : l'ancienne n'avait pas fini d'arriver.
        If ancienne IsNot Nothing AndAlso ancienne.Active Then FinirLArrivee(ancienne)
        Dim zone As Rectangle = prete.Zone
        c.Cadre.Margin = New W.Thickness(zone.X, zone.Y, 0, 0)
        c.Cadre.Width = zone.Width
        c.Cadre.Height = zone.Height
        c.Mobile.Width = zone.Width
        c.Mobile.Height = zone.Height
        c.Photo.Source = prete.Image
        ChoisirLeMouvement(c, zone, dureeSecondes)
        PreparerLArrivee(c, maniere, prete.Floue)
        c.Depuis = Stopwatch.StartNew()
        c.Active = True
        c.Fond.Visibility = W.Visibility.Visible
        _rang += 1
        WC.Panel.SetZIndex(c.Fond, _rang)
        _devant = place
        Placer(c, 0)
        Accompagner()
        ShowWindow(_hote.Handle, SW_SHOWNA)
        If Not _enMarche Then
            AddHandler WM.CompositionTarget.Rendering, AddressOf ChaqueImage
            _enMarche = True
        End If
    End Sub

    Private Function NouvelleCouche() As Couche
        Dim c As New Couche With {
            .Zoom = New WM.ScaleTransform(1, 1), .Tourne = New WM.RotateTransform(0), .Glisse = New WM.TranslateTransform(0, 0),
            .EntreeZoom = New WM.ScaleTransform(1, 1), .EntreeGlisse = New WM.TranslateTransform(0, 0),
            .EntreeTourne = New WM.RotateTransform(0),
            .Rideau = New WM.RectangleGeometry(), .Rond = New WM.EllipseGeometry(),
            .Store = New WM.GeometryGroup With {.FillRule = WM.FillRule.Nonzero},
            .CarreTourne = New WM.RotateTransform(45)}
        c.Carre = New WM.RectangleGeometry With {.Transform = c.CarreTourne}
        c.Lames = New WM.RectangleGeometry(NombreDeLames - 1) {}
        For i As Integer = 0 To NombreDeLames - 1
            c.Lames(i) = New WM.RectangleGeometry()
            c.Store.Children.Add(c.Lames(i))
        Next
        Dim mouvement As New WM.TransformGroup()
        mouvement.Children.Add(c.Zoom)
        mouvement.Children.Add(c.Tourne)
        mouvement.Children.Add(c.Glisse)
        c.Brume = New WC.Image With {.Stretch = WM.Stretch.Fill, .Visibility = W.Visibility.Collapsed}
        c.Photo = New WC.Image With {.Stretch = WM.Stretch.Fill}
        c.Mobile = New WC.Grid With {.RenderTransformOrigin = New W.Point(0.5, 0.5), .RenderTransform = mouvement}
        c.Mobile.Children.Add(c.Brume)
        c.Mobile.Children.Add(c.Photo)
        c.Cadre = New WC.Border With {.ClipToBounds = True, .Child = c.Mobile,
                                      .HorizontalAlignment = W.HorizontalAlignment.Left, .VerticalAlignment = W.VerticalAlignment.Top}
        Dim entree As New WM.TransformGroup()
        entree.Children.Add(c.EntreeZoom)
        entree.Children.Add(c.EntreeTourne)
        entree.Children.Add(c.EntreeGlisse)
        c.Fond = New WC.Grid With {.Background = WM.Brushes.Black, .RenderTransformOrigin = New W.Point(0.5, 0.5), .RenderTransform = entree}
        c.Fond.Children.Add(c.Cadre)
        Return c
    End Function

    ''' <summary>Mouvement de cette photo pendant son affichage : celui des reglages, ou tire au
    ''' sort. L'ampleur suit le temps d'affichage et la force choisie (voir _zoomsMaxi), pour
    ''' une vitesse toujours lente.</summary>
    Friend Sub ChoisirLeMouvement(c As Couche, zone As Rectangle, dureeSecondes As Double)
        c.Duree = Math.Max(1, dureeSecondes)
        Dim ampleur As Double = Math.Max(_zoomsMini(Force()), Math.Min(ZoomMaxi() - 1, _zoomsParSeconde(Force()) * c.Duree))
        ' Decalage possible sans decouvrir le bord : la moitie de l'agrandissement (un peu moins).
        Dim course As Double = ampleur / 2 * 0.9
        Dim sens As Integer = If(_hasard.Next(2) = 0, 1, -1)
        c.Z0 = 1 : c.Z1 = 1 : c.R0 = 0 : c.R1 = 0 : c.X0 = 0 : c.X1 = 0 : c.Y0 = 0 : c.Y1 = 0
        Dim sorte As Mouvement = Mouvement.AuHasard
        If [Enum].IsDefined(GetType(Mouvement), AnimationMouvement) Then sorte = CType(AnimationMouvement, Mouvement)
        ' Au hasard : parmi les mouvements coches dans les reglages ("Choix du hasard...").
        If sorte = Mouvement.AuHasard Then sorte = CType(TirageAuHasard.Tirer(TirageAuHasard.SansNoirEtBlanc(HasardPhotoMouvement, Mouvement.Aucun), TirageAuHasard.PhotoMouvementParDefaut, Mouvement.Aucun, _hasard), Mouvement)
        ' Zoom : "Aucun" (13/10) : la photo ne bouge pas, quel que soit le mouvement choisi.
        If AnimationPhotoForce >= 3 Then sorte = Mouvement.Aucun
        Select Case sorte
            Case Mouvement.Avancer, Mouvement.Reculer
                ' Vers un point un peu a cote du centre (ou en en revenant).
                Dim versX As Double = (_hasard.NextDouble() * 2 - 1) * course * 0.7
                Dim versY As Double = (_hasard.NextDouble() * 2 - 1) * course * 0.7
                If sorte = Mouvement.Avancer Then
                    c.Z1 = 1 + ampleur : c.X1 = versX : c.Y1 = versY
                Else
                    c.Z0 = 1 + ampleur : c.X0 = versX : c.Y0 = versY
                End If
            Case Mouvement.Glisser
                c.Z0 = 1 + ampleur : c.Z1 = 1 + ampleur
                If zone.Height > zone.Width Then
                    c.Y0 = -course * sens : c.Y1 = course * sens
                Else
                    c.X0 = -course * sens : c.X1 = course * sens
                End If
            Case Mouvement.Balancer
                ' Penchee, la photo doit rester assez grande pour couvrir les coins de son cadre :
                ' d'autant plus qu'elle est allongee. L'inclinaison est reduite si le zoom permis
                ' n'y suffit pas (panorama).
                Dim allongement As Double = Math.Max(zone.Width / zone.Height, zone.Height / zone.Width)
                Dim angle As Double = _inclinaisons(Force())
                Dim zoom As Double
                Do
                    Dim radians As Double = angle * Math.PI / 180
                    zoom = Math.Cos(radians) + Math.Sin(radians) * allongement + 0.004
                    If zoom <= ZoomMaxi() Then Exit Do
                    angle *= 0.8
                Loop
                c.Z0 = zoom : c.Z1 = zoom
                c.R0 = -angle * sens : c.R1 = angle * sens
            Case Else
                ' Aucun : immobile.
        End Select
    End Sub

    ''' <summary>Arrivee de la prochaine photo : celle des reglages, ou tiree au sort.</summary>
    Friend Function TirerLArrivee() As Arrivee
        ' Apres une video dont l'image se retire en transition (FormTransitionVideo) : c'est elle
        ' qui fait l'arrivee ; deux effets superposes se generaient.
        If FormTransitionVideo.Couvre Then Return Arrivee.Aucune
        Dim maniere As Arrivee = Arrivee.AuHasard
        If AnimationArrivee > 0 AndAlso [Enum].IsDefined(GetType(Arrivee), AnimationArrivee) Then maniere = CType(AnimationArrivee, Arrivee)
        If maniere = Arrivee.SansArrivee Then Return Arrivee.Aucune      ' "Aucune" : la photo est la d'un coup
        If maniere = Arrivee.AuHasard Then
            ' Parmi les arrivees cochees dans les reglages ("Choix du hasard...") ; le
            ' numero 0 y veut dire "aucune" : la photo est la d'un coup.
            Dim tiree As Integer = TirageAuHasard.Tirer(HasardPhotoArrivee, TirageAuHasard.PhotoArriveeParDefaut, Arrivee.FonduLent, _hasard)
            maniere = If(tiree = 0, Arrivee.Aucune, CType(tiree, Arrivee))
        End If
        Return maniere
    End Function

    ''' <summary>Met la couche (peut-etre resservie) au depart de cette arrivee.</summary>
    Private Sub PreparerLArrivee(c As Couche, maniere As Arrivee, floue As WM.Imaging.BitmapSource)
        If maniere = Arrivee.Flou AndAlso floue Is Nothing Then maniere = Arrivee.Fondu
        FinirLArrivee(c)                                            ' repart de zero
        c.Maniere = maniere
        If maniere = Arrivee.Aucune Then Exit Sub                   ' deja en place
        c.ArriveeFinie = False
        c.Avancement = 0
        Select Case maniere
            Case Arrivee.FonduLent : c.DureeArrivee = ArriveeLenteSecondes
            Case Arrivee.FonduAuNoir : c.DureeArrivee = ArriveeAuNoirSecondes
            Case Arrivee.Retournement : c.DureeArrivee = ArriveeRetourneeSecondes
            Case Else : c.DureeArrivee = ArriveeSecondes
        End Select
        ' Poussee, volet, recouvrement : par la gauche, la droite, le haut ou le bas. Ouverture,
        ' stores, retournement : en largeur ou en hauteur. Rotation : d'un cote ou de l'autre.
        Dim cote As Integer = _hasard.Next(4)
        c.SensX = If(cote = 0, 1, If(cote = 1, -1, 0))
        c.SensY = If(cote = 2, 1, If(cote = 3, -1, 0))
        Select Case maniere
            Case Arrivee.Volet, Arrivee.Ouverture : c.Fond.Clip = c.Rideau
            Case Arrivee.Cercle : c.Fond.Clip = c.Rond
            Case Arrivee.Stores : c.Fond.Clip = c.Store
            Case Arrivee.Losange : c.Fond.Clip = c.Carre
            Case Arrivee.Flou
                c.Brume.Source = floue
                c.Brume.Visibility = W.Visibility.Visible
        End Select
    End Sub

    ''' <summary>Met la photo ou elle doit etre <paramref name="secondes"/> apres son arrivee :
    ''' son mouvement dans son cadre, et son arrivee tant qu'elle dure.</summary>
    Friend Sub Placer(c As Couche, secondes As Double)
        Dim p As Double = Math.Max(0, Math.Min(1, secondes / c.Duree))
        Dim zoom As Double = c.Z0 + (c.Z1 - c.Z0) * p
        c.Zoom.ScaleX = zoom
        c.Zoom.ScaleY = zoom
        c.Tourne.Angle = c.R0 + (c.R1 - c.R0) * p
        c.Glisse.X = (c.X0 + (c.X1 - c.X0) * p) * c.Cadre.Width
        c.Glisse.Y = (c.Y0 + (c.Y1 - c.Y0) * p) * c.Cadre.Height

        If c.ArriveeFinie Then Exit Sub
        Dim t As Double = Math.Max(0, Math.Min(1, secondes / c.DureeArrivee))
        If t >= 1 Then
            FinirLArrivee(c)
            Exit Sub
        End If
        Dim q As Double = t * t * (3 - 2 * t)                       ' depart et fin en douceur
        c.Avancement = q
        Dim largeur As Double = _tailleDeLaScene.Width, hauteur As Double = _tailleDeLaScene.Height
        Select Case c.Maniere
            Case Arrivee.Poussee, Arrivee.Recouvrement
                c.EntreeGlisse.X = -c.SensX * (1 - q) * largeur
                c.EntreeGlisse.Y = -c.SensY * (1 - q) * hauteur
            Case Arrivee.FonduAuNoir
                ' Le noir de la couche couvre d'abord l'ancienne photo, puis la nouvelle s'y allume.
                c.Fond.Opacity = Math.Min(1, t * 2)
                c.Cadre.Opacity = Math.Max(0, t * 2 - 1)
            Case Arrivee.Ouverture
                Dim l As Double = If(c.SensX <> 0, largeur * q, largeur), h As Double = If(c.SensY <> 0, hauteur * q, hauteur)
                c.Rideau.Rect = New W.Rect((largeur - l) / 2, (hauteur - h) / 2, l, h)
            Case Arrivee.Stores
                For i As Integer = 0 To NombreDeLames - 1
                    If c.SensX <> 0 Then
                        Dim pas As Double = largeur / NombreDeLames
                        c.Lames(i).Rect = New W.Rect(i * pas + If(c.SensX < 0, pas * (1 - q), 0), 0, pas * q, hauteur)
                    Else
                        Dim pas As Double = hauteur / NombreDeLames
                        c.Lames(i).Rect = New W.Rect(0, i * pas + If(c.SensY < 0, pas * (1 - q), 0), largeur, pas * q)
                    End If
                Next
            Case Arrivee.Losange
                ' Carre tourne de 45 degres : sa demi-diagonale doit atteindre les coins de l'image.
                Dim demi As Double = (largeur + hauteur) / (2 * Math.Sqrt(2)) * q
                c.CarreTourne.CenterX = largeur / 2
                c.CarreTourne.CenterY = hauteur / 2
                c.Carre.Rect = New W.Rect(largeur / 2 - demi, hauteur / 2 - demi, demi * 2, demi * 2)
            Case Arrivee.Rotation
                ' Penchee, la couche est agrandie juste assez pour couvrir les coins de l'image.
                Dim angle As Double = AngleDArrivee * (1 - q) * If(c.SensX + c.SensY > 0, 1, -1)
                Dim radians As Double = Math.Abs(angle) * Math.PI / 180
                Dim echelle As Double = Math.Cos(radians) + Math.Sin(radians) * Math.Max(largeur / hauteur, hauteur / largeur) + 0.004
                c.Fond.Opacity = Math.Min(1, t * 1.6)
                c.EntreeTourne.Angle = angle
                c.EntreeZoom.ScaleX = echelle
                c.EntreeZoom.ScaleY = echelle
            Case Arrivee.Retournement
                ' Seconde moitie : la nouvelle s'ouvre (l'ancienne s'est refermee, voir Accompagner).
                Dim ouverte As Double = Math.Max(0.001, -Math.Cos(Math.PI * t))
                c.Fond.Opacity = If(t < 0.5, 0, 1)
                If c.SensX <> 0 Then c.EntreeZoom.ScaleX = ouverte Else c.EntreeZoom.ScaleY = ouverte
            Case Arrivee.Volet
                Dim l As Double = If(c.SensX <> 0, largeur * q, largeur), h As Double = If(c.SensY <> 0, hauteur * q, hauteur)
                c.Rideau.Rect = New W.Rect(If(c.SensX < 0, largeur - l, 0), If(c.SensY < 0, hauteur - h, 0), l, h)
            Case Arrivee.Zoom
                c.Fond.Opacity = t
                c.EntreeZoom.ScaleX = 1.25 - 0.25 * q
                c.EntreeZoom.ScaleY = c.EntreeZoom.ScaleX
            Case Arrivee.Flou
                ' La copie floue apparait vite ; la photo nette se revele par-dessus.
                c.Fond.Opacity = Math.Min(1, t * 2.5)
                c.Photo.Opacity = q
            Case Arrivee.Cercle
                Dim rayon As Double = Math.Sqrt(largeur * largeur + hauteur * hauteur) / 2 * q
                c.Rond.Center = New W.Point(largeur / 2, hauteur / 2)
                c.Rond.RadiusX = rayon
                c.Rond.RadiusY = rayon
            Case Else
                c.Fond.Opacity = t                                   ' fondu
        End Select
    End Sub

    ''' <summary>La photo est arrivee : plus de transparence, de rideau ni de flou.</summary>
    Private Sub FinirLArrivee(c As Couche)
        c.ArriveeFinie = True
        c.Fond.Opacity = 1
        c.Fond.Clip = Nothing
        c.Photo.Opacity = 1
        c.Cadre.Opacity = 1
        c.Brume.Visibility = W.Visibility.Collapsed
        c.Brume.Source = Nothing
        c.EntreeZoom.ScaleX = 1 : c.EntreeZoom.ScaleY = 1
        c.EntreeTourne.Angle = 0
        c.EntreeGlisse.X = 0 : c.EntreeGlisse.Y = 0
    End Sub

    ''' <summary>Poussee : l'ancienne photo sort du cote oppose, au meme pas que la nouvelle entre.
    ''' Arrivee finie : l'ancienne photo ne sert plus.</summary>
    Friend Sub Accompagner()
        Dim devant As Couche = _couches(_devant), ancienne As Couche = _couches(1 - _devant)
        If devant Is Nothing OrElse Not devant.Active OrElse ancienne Is Nothing OrElse Not ancienne.Active Then Exit Sub
        If devant.ArriveeFinie Then
            Vider(ancienne)
        ElseIf devant.Maniere = Arrivee.Poussee Then
            ancienne.EntreeGlisse.X = devant.EntreeGlisse.X + devant.SensX * _tailleDeLaScene.Width
            ancienne.EntreeGlisse.Y = devant.EntreeGlisse.Y + devant.SensY * _tailleDeLaScene.Height
        ElseIf devant.Maniere = Arrivee.Retournement Then
            ' Premiere moitie : l'ancienne se referme sur son milieu, puis disparait.
            Dim t As Double = Math.Max(0, Math.Min(1, devant.Depuis.Elapsed.TotalSeconds / devant.DureeArrivee))
            Dim fermee As Double = Math.Max(0.001, Math.Cos(Math.PI * t))
            ancienne.Fond.Opacity = If(t < 0.5, 1, 0)
            If devant.SensX <> 0 Then ancienne.EntreeZoom.ScaleX = fermee Else ancienne.EntreeZoom.ScaleY = fermee
        End If
    End Sub

    ''' <summary>A chaque image de l'ecran (appele par WPF, dans le fil de l'interface).</summary>
    Private Sub ChaqueImage(sender As Object, e As EventArgs)
        Try
            ' La zone de la photo n'est plus affichee (video, film, TV...) : l'animation s'arrete.
            If _fond Is Nothing OrElse _fond.IsDisposed OrElse Not _fond.Visible Then
                Masquer()
                Exit Sub
            End If
            For Each c As Couche In _couches
                If c IsNot Nothing AndAlso c.Active Then Placer(c, c.Depuis.Elapsed.TotalSeconds)
            Next
            Accompagner()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub Vider(c As Couche)
        If c Is Nothing OrElse Not c.Active Then Exit Sub
        c.Active = False
        c.Photo.Source = Nothing
        c.Fond.Visibility = W.Visibility.Collapsed
        FinirLArrivee(c)
    End Sub

    Private Const WS_CHILD As Integer = &H40000000
    Private Const WS_DISABLED As Integer = &H8000000
    Private Const WS_CLIPCHILDREN As Integer = &H2000000
    Private Const SW_HIDE As Integer = 0
    Private Const SW_SHOWNA As Integer = 8
    Private Const SWP_NOZORDER As Integer = &H4
    Private Const SWP_NOACTIVATE As Integer = &H10

    <DllImport("user32.dll")>
    Private Function ShowWindow(fenetre As IntPtr, commande As Integer) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Function SetWindowPos(fenetre As IntPtr, apres As IntPtr, x As Integer, y As Integer, largeur As Integer, hauteur As Integer, options As Integer) As Boolean
    End Function

End Module
