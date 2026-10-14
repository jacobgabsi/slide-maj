Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging
Imports System.Runtime.InteropServices

''' <summary>
''' Montre analogique integree, reproduction du widget TheAeroClock (skin n° 18) qui etait
''' lance en externe : pas de cadran, douze chiffres gras italiques en relief argente texture
''' avec ombre portee, aiguilles effilees metalliques a deux facettes, trotteuse fine.
'''
''' Fenetre "calque" (WS_EX_LAYERED + UpdateLayeredWindow) : la transparence est geree pixel
''' par pixel, donc les bords lisses et les ombres se fondent dans la photo en dessous, comme
''' avec le widget. Elle ne prend jamais le focus.
'''
''' Souris : la montre se redimensionne a la molette ou en cliquant-glissant sur elle. Elle ne
''' capte la souris que dans son disque (fond d'opacite 1/255, invisible) : autour, les clics
''' traversent. La nouvelle taille est signalee par l'evenement TailleChoisie.
'''
''' Autonome : ne depend d'aucun autre form ni module, pour pouvoir etre testee seule.
''' </summary>
Public Class FormHorloge

    ''' <summary>Taille du widget d'origine a 100 %, en pixels logiques (96 ppp) : cx=128.
    ''' L'ancien mode "BIG" (cx=480) correspond a 375 %.</summary>
    Public Const TailleDeBase As Integer = 128

    ' Geometrie des chiffres, en fraction du cote (relevee sur une capture du widget). Chaque chiffre
    ' est mis a la hauteur HauteurChiffres et centre sur le cercle de rayon RayonChiffres ; son
    ' inclinaison (cisaillement horizontal) ne change pas ses bords haut et bas.
    Private Const RayonChiffres As Single = 0.44F
    Friend Const HauteurChiffres As Single = 0.095F
    Friend Const InclinaisonChiffres As Single = -0.23F                     ' cisaillement = italique ~13 deg
    Private Const Pivot As Single = 0.5F                                       ' axe des aiguilles
    Private Const BasDu12 As Single = Pivot - RayonChiffres + HauteurChiffres / 2
    Private Const HautDu6 As Single = Pivot + RayonChiffres - HauteurChiffres / 2
    ' Horaires de Chabbat : a mi-chemin entre le bas du 12 et le pivot, et entre le pivot et le haut du 6.
    Private Const CentreHoraireHaut As Single = (BasDu12 + Pivot) / 2
    Private Const CentreHoraireBas As Single = (Pivot + HautDu6) / 2

    Private _taille As Integer = 0          ' cote de la montre en pixels physiques
    Private _chiffres As Bitmap              ' couche des chiffres, calculee une fois par taille
    Private _versionPoliceRendue As Integer  ' version de la police avec laquelle _chiffres a ete calcule
    Private Shared ReadOnly GrainAiguilles As New TextureBrush(TextureGranit())

    Private _reference As Form               ' fenetre dont la montre occupe le coin haut-droit
    Private _premierPlan As Boolean = True   ' True = plein ecran (widget), False = mode fenetre
    Private _pourcent As Integer = 100       ' taille choisie dans Form4, en % de TailleDeBase
    Private _horaireHaut As String = ""      ' debut du Chabbat, dessine dans la moitie haute
    Private _horaireBas As String = ""       ' fin du Chabbat, dessinee dans la moitie basse

    ''' <summary>Horaires de Chabbat a afficher dans le cadran ("" pour les retirer). Dessines dans
    ''' l'image de la montre elle-meme : fond transparent et centrage exact sur les chiffres,
    ''' ce que ne permettaient pas les zones de texte de Form3.</summary>
    Public Sub DefinirHoraires(haut As String, bas As String)
        haut = If(haut, "").Trim()
        bas = If(bas, "").Trim()
        If haut = _horaireHaut AndAlso bas = _horaireBas Then Exit Sub
        _horaireHaut = haut
        _horaireBas = bas
        If Me.Visible Then Rafraichir()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Fenetre calque, sans activation ni bouton de barre des taches
    ' ------------------------------------------------------------------------------------------

    ' Styles WS_EX_* : voir CalqueWin32.
    ' Pas de WS_EX_TRANSPARENT : la montre doit recevoir la molette et le glisser. Les clics
    ' traversent quand meme partout ou l'image est totalement transparente (alpha 0), c'est-a-dire
    ' hors du disque de la montre (voir RendreChiffres).
    ' Le "premier plan" n'est pas fige ici : il est regle selon le mode (plein ecran ou fenetre) par
    ' CalqueWin32.DefinirPremierPlan, sans activation (jamais par la propriete TopMost).
    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_LAYERED Or WS_EX_TOOLWINDOW Or WS_EX_NOACTIVATE
            Return cp
        End Get
    End Property

    ' ------------------------------------------------------------------------------------------
    ' Redimensionnement a la souris
    ' ------------------------------------------------------------------------------------------

    Public Const PourcentMini As Integer = 25
    ' 400 au depart ; releve depuis que la taille suit la hauteur de la fenetre (EchelleTaille) :
    ' en mode fenetre, 400 % ne donnait plus qu'une petite montre. La vraie limite reste la place
    ' disponible (ZoneDansEcran).
    Public Const PourcentMaxi As Integer = 1000
    Private Const PasMolette As Integer = 5          ' % par cran de molette

    ''' <summary>Leve une fois le geste termine (bouton relache, ou 0,5 s apres le dernier cran de
    ''' molette) avec la nouvelle taille en %, pour que l'appelant l'enregistre.</summary>
    Public Event TailleChoisie(pourcent As Integer)

    Private _glisse As Boolean
    Private _departGlisse As Point                   ' position ecran du curseur au clic
    Private _departTaille As Integer                 ' cote de la montre au clic, en pixels
    Private WithEvents TimerFinMolette As New Timer With {.Interval = 500}

    Private Sub FormHorloge_MouseDown(sender As Object, e As MouseEventArgs) Handles Me.MouseDown
        If e.Button <> MouseButtons.Left OrElse _taille = 0 Then Exit Sub
        _glisse = True
        _departGlisse = Me.PointToScreen(e.Location)
        _departTaille = _taille
        Me.Capture = True
    End Sub

    Private Sub FormHorloge_MouseMove(sender As Object, e As MouseEventArgs) Handles Me.MouseMove
        If Not _glisse Then Exit Sub
        ' La montre reste accrochee au coin haut-droit : tirer vers le bas et/ou vers la gauche
        ' l'agrandit, revenir vers le coin la reduit.
        Dim p As Point = Me.PointToScreen(e.Location)
        Dim delta As Double = ((_departGlisse.X - p.X) + (p.Y - _departGlisse.Y)) / 2.0
        ChangerTaille(PourcentPourCote(_departTaille + delta))
    End Sub

    Private Sub FormHorloge_MouseUp(sender As Object, e As MouseEventArgs) Handles Me.MouseUp
        If Not _glisse Then Exit Sub
        _glisse = False
        Me.Capture = False
        RaiseEvent TailleChoisie(_pourcent)
    End Sub

    Private Sub FormHorloge_MouseWheel(sender As Object, e As MouseEventArgs) Handles Me.MouseWheel
        ChangerTaille(_pourcent + Math.Sign(e.Delta) * PasMolette)
        ' On n'enregistre qu'a la fin de la serie de crans.
        TimerFinMolette.Stop()
        TimerFinMolette.Start()
    End Sub

    Private Sub TimerFinMolette_Tick(sender As Object, e As EventArgs) Handles TimerFinMolette.Tick
        TimerFinMolette.Stop()
        RaiseEvent TailleChoisie(_pourcent)
    End Sub

    Private Sub ChangerTaille(pourcent As Integer)
        Dim maxi As Integer = PourcentMaxi
        If _reference IsNot Nothing Then
            ' Plafond reel : la plus grande montre qui tient dans la zone (voir ZoneDansEcran). Sans
            ' lui, le % monterait "dans le vide" au-dela de ce qui s'affiche, et il faudrait
            ' redescendre plusieurs crans avant de voir la montre rapetisser.
            maxi = Math.Min(maxi, PourcentPourCote(ZoneDansEcran(_reference, PourcentMaxi).Width))
        End If
        pourcent = Math.Max(PourcentMini, Math.Min(maxi, pourcent))
        If pourcent = _pourcent Then Exit Sub
        _pourcent = pourcent
        Rafraichir()
    End Sub

    ''' <summary>Pourcentage correspondant a un cote de montre en pixels physiques.</summary>
    Private Function PourcentPourCote(cotePixels As Double) As Integer
        Dim echelle As Double = If(_reference IsNot Nothing, EchelleTaille(_reference), Me.DeviceDpi / 96.0)
        Return CInt(Math.Round(cotePixels / (TailleDeBase * echelle) * 100.0))
    End Function

    ''' <summary>Pixels physiques par pixel logique de la montre : echelle DPI, multipliee par la
    ''' part de la hauteur de l'ecran qu'occupe la fenetre. Le % choisi vaut donc pour le plein
    ''' ecran, et la montre rapetisse et grandit avec la fenetre de Slide, comme la legende du bas
    ''' (dont la police est proportionnelle a la hauteur de la fenetre).</summary>
    Private Shared Function EchelleTaille(reference As Form) As Double
        Dim echelle As Double = reference.DeviceDpi / 96.0
        Dim ecran As Integer = Screen.FromControl(reference).Bounds.Height
        If ecran > 0 AndAlso reference.ClientSize.Height > 0 Then
            echelle *= Math.Min(1.0, reference.ClientSize.Height / ecran)
        End If
        Return echelle
    End Function

    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    ' ------------------------------------------------------------------------------------------
    ' Utilisation
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Affiche la montre en haut a droite de la zone client de <paramref name="reference"/>,
    ''' aux memes marges que le widget (12 px a droite, 10 px en haut, a 96 ppp).</summary>
    ''' <param name="premierPlan">True en plein ecran : fenetre independante toujours au premier
    ''' plan, comme le widget. False en mode fenetre : rattachee a <paramref name="reference"/>, elle
    ''' la suit quand on la deplace ou la redimensionne, reste juste au-dessus d'elle sans flotter
    ''' au-dessus des autres programmes, et disparait quand on la reduit ou la masque.</param>
    ''' <param name="pourcent">Taille en % de celle du widget (128) : 100 = identique, 375 = ancien BIG.</param>
    Public Sub AfficherSur(reference As Form, Optional premierPlan As Boolean = True,
                           Optional pourcent As Integer = 100)
        Rattacher(reference)
        _premierPlan = premierPlan
        _pourcent = Math.Max(1, pourcent)

        CalqueWin32.DefinirPremierPlan(Me, reference, premierPlan)

        Rafraichir()
        TimerSeconde.Start()
    End Sub

    Public Sub Masquer()
        TimerSeconde.Stop()
        Rattacher(Nothing)
        Me.Hide()
    End Sub

    Private Sub Rattacher(reference As Form)
        If ReferenceEquals(_reference, reference) Then Exit Sub
        If _reference IsNot Nothing Then
            RemoveHandler _reference.LocationChanged, AddressOf ReferenceDeplacee
            RemoveHandler _reference.SizeChanged, AddressOf ReferenceDeplacee
        End If
        _reference = reference
        If _reference IsNot Nothing Then
            AddHandler _reference.LocationChanged, AddressOf ReferenceDeplacee
            AddHandler _reference.SizeChanged, AddressOf ReferenceDeplacee
        End If
    End Sub

    ''' <summary>Suit la fenetre de reference immediatement, sans attendre la seconde suivante.</summary>
    Private Sub ReferenceDeplacee(sender As Object, e As EventArgs)
        If TimerSeconde.Enabled Then Rafraichir()
    End Sub

    Private Sub TimerSeconde_Tick(sender As Object, e As EventArgs) Handles TimerSeconde.Tick
        Rafraichir()
    End Sub

    Private Sub Rafraichir()
        ' Recale le prochain tic juste apres le changement de seconde, comme une vraie trotteuse.
        TimerSeconde.Interval = Math.Max(50, 1000 - Date.Now.Millisecond + 15)
        If _reference Is Nothing OrElse _reference.IsDisposed Then Exit Sub

        ' En mode fenetre, la montre disparait avec la fenetre de Slide (reduite ou masquee).
        If Not _premierPlan AndAlso
           (Not _reference.Visible OrElse _reference.WindowState = FormWindowState.Minimized) Then
            If Me.Visible Then Me.Hide()
            Exit Sub
        End If

        Placer()
        If Not Me.Visible Then Me.Show()
        Using image As Bitmap = RendreImage(Date.Now)
            ' Legerement attenuee, comme le lieu et la date dans les bandes noires (mais moins).
            CalqueWin32.Attenuer(image, OpaciteReglee())
            CalqueWin32.Appliquer(Me, image)
        End Using

        ' En plein ecran, plusieurs fenetres de Slide passent "au premier plan" en cours de route
        ' (reglages video, incrustation...) : on se replace au-dessus a chaque seconde, comme un
        ' widget. Sans consequence puisque la montre laisse passer les clics.
        If _premierPlan Then
            CalqueWin32.ReaffirmerPremierPlan(Me)
        End If
    End Sub

    ''' <summary>Emplacement de la montre (coordonnees ecran) dans le coin haut-droit de la zone client
    ''' de <paramref name="reference"/> : taille = 128 x echelle DPI x pourcentage, marges du widget.
    ''' Plafonnee a la zone disponible : un grand pourcentage ou une petite fenetre ne la font jamais
    ''' deborder. Partagee avec l'affichage des horaires de Chabbat dans le cadran (Module1), qui
    ''' doit tomber exactement dans la montre, qu'elle soit affichee ou non.</summary>
    Public Shared Function ZoneDansEcran(reference As Form, pourcent As Integer) As Rectangle
        Dim echelle As Double = reference.DeviceDpi / 96.0
        Dim zone As Rectangle = reference.RectangleToScreen(reference.ClientRectangle)
        Dim margeDroite As Integer = CInt(12 * echelle)
        Dim margeHaut As Integer = CInt(10 * echelle)

        ' Taille proportionnelle a la hauteur de la fenetre (voir EchelleTaille) ; les marges
        ' restent celles du widget.
        Dim taille As Integer = CInt(Math.Round(TailleDeBase * EchelleTaille(reference) * Math.Max(1, pourcent) / 100.0))
        Dim maximum As Integer = Math.Min(zone.Width - 2 * margeDroite, zone.Height - 2 * margeHaut)
        taille = Math.Max(16, Math.Min(taille, maximum))

        Return New Rectangle(zone.Right - taille - margeDroite, zone.Top + margeHaut, taille, taille)
    End Function

    Private Sub Placer()
        Dim voulu As Rectangle = ZoneDansEcran(_reference, _pourcent)

        If voulu.Width <> _taille OrElse _chiffres Is Nothing OrElse _versionPoliceRendue <> _versionPolice Then
            _chiffres?.Dispose()
            _versionPoliceRendue = _versionPolice
            _chiffres = RendreChiffres(voulu.Width)
            _taille = voulu.Width
        End If

        If Me.Bounds <> voulu Then Me.Bounds = voulu
    End Sub

    Private Sub FormHorloge_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        TimerSeconde.Stop()
        TimerFinMolette.Stop()
        Rattacher(Nothing)
        _chiffres?.Dispose()
        _chiffres = Nothing
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Rendu
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Image complete (chiffres + aiguilles) a l'heure donnee, fond transparent.
    ''' Publique pour pouvoir etre testee / comparee au widget sans afficher la fenetre.</summary>
    Public Function RendreImage(heure As Date, Optional taille As Integer = 0) As Bitmap
        If taille = 0 Then taille = _taille
        Dim chiffres As Bitmap = If(taille = _taille AndAlso _chiffres IsNot Nothing, _chiffres, Nothing)
        Dim chiffresTemporaires As Boolean = chiffres Is Nothing
        If chiffresTemporaires Then chiffres = RendreChiffres(taille)

        Dim image As New Bitmap(taille, taille, PixelFormat.Format32bppArgb)
        Using g As Graphics = Graphics.FromImage(image)
            g.DrawImageUnscaled(chiffres, 0, 0)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.HighQuality

            ' Horaires de Chabbat (sous les aiguilles) : celui du haut centre entre le bas du 12 et
            ' le pivot des aiguilles, celui du bas entre le pivot et le haut du 6.
            DessinerHoraire(g, _horaireHaut, taille * CentreHoraireHaut, taille)
            DessinerHoraire(g, _horaireBas, taille * CentreHoraireBas, taille)

            Dim angleHeure As Single = CSng(((heure.Hour Mod 12) + heure.Minute / 60.0) * 30.0)
            Dim angleMinute As Single = CSng((heure.Minute + heure.Second / 60.0) * 6.0)
            Dim angleSeconde As Single = CSng(heure.Second * 6.0)

            ' Profils releves sur une capture du widget : demi-largeur (x) le long de l'aiguille (y),
            ' de la pointe (y negatif) a la queue, en fractions du cote de la montre. Tiges fines ;
            ' l'aiguille des heures s'elargit en petite pointe de fleche pres du centre.
            Dim aiguilles = {
                New Aiguille With {.Angle = angleHeure, .Profil = {
                    New PointF(0, -0.32F), New PointF(0.008F, -0.31F), New PointF(0.013F, -0.1F),
                    New PointF(0.022F, -0.045F), New PointF(0.011F, -0.03F), New PointF(0.011F, 0),
                    New PointF(0.005F, 0.06F), New PointF(0, 0.07F)}},
                New Aiguille With {.Angle = angleMinute, .Profil = {
                    New PointF(0, -0.42F), New PointF(0.007F, -0.41F), New PointF(0.011F, -0.1F),
                    New PointF(0.013F, -0.02F), New PointF(0.01F, 0), New PointF(0.005F, 0.06F), New PointF(0, 0.07F)}},
                New Aiguille With {.Angle = angleSeconde, .Profil = {
                    New PointF(0, -0.44F), New PointF(0.002F, -0.435F), New PointF(0.0035F, -0.03F), New PointF(0.0035F, 0),
                    New PointF(0.0025F, 0.07F), New PointF(0, 0.08F)}}
            }

            ' Toutes les ombres d'abord, sinon l'ombre d'une aiguille salirait celle du dessous.
            For Each a As Aiguille In aiguilles
                DessinerOmbreAiguille(g, a, taille)
            Next
            For Each a As Aiguille In aiguilles
                DessinerAiguille(g, a, taille)
            Next
        End Using

        If chiffresTemporaires Then chiffres.Dispose()
        Return image
    End Function

    Private Structure Aiguille
        Public Angle As Single         ' degres, sens horaire depuis midi
        Public Profil As PointF()      ' flanc droit, de la pointe a la queue (fractions du cote)
    End Structure

    ''' <summary>Facette droite de l'aiguille en pixels (le profil ; l'arete centrale la referme).
    ''' La facette gauche est son symetrique.</summary>
    Private Shared Function Facette(a As Aiguille, taille As Integer, cote As Integer) As PointF()
        Dim t As Single = taille
        Dim minimum As Single = 0.4F   ' reste visible en petite taille
        Return a.Profil.Select(Function(p) New PointF(
                   cote * If(p.X = 0, 0, Math.Max(minimum, p.X * t)), p.Y * t)).ToArray()
    End Function

    ''' <summary>Contour complet : facette droite puis facette gauche parcourue a l'envers.</summary>
    Private Shared Function Contour(a As Aiguille, taille As Integer) As PointF()
        Dim droite As PointF() = Facette(a, taille, 1)
        Dim gauche As PointF() = Facette(a, taille, -1).Reverse().ToArray()
        Return droite.Concat(gauche.Skip(1).Take(gauche.Length - 2)).ToArray()
    End Function

    Private Shared Sub DessinerOmbreAiguille(g As Graphics, a As Aiguille, taille As Integer)
        Dim c As Single = taille / 2.0F
        Dim decalage As Single = Math.Max(1.0F, 0.01F * taille)
        Dim flou As Single = Math.Max(0.5F, 0.004F * taille)
        Dim pts As PointF() = Contour(a, taille)
        Using pinceau As New SolidBrush(Color.FromArgb(45, 0, 0, 0))
            ' Ombre douce : plusieurs passages legerement decales.
            For Each d As PointF In {New PointF(0, 0), New PointF(flou, 0), New PointF(-flou, 0),
                                     New PointF(0, flou), New PointF(0, -flou)}
                g.ResetTransform()
                g.TranslateTransform(c + decalage + d.X, c + decalage * 1.3F + d.Y)
                g.RotateTransform(a.Angle)
                g.FillPolygon(pinceau, pts)
            Next
        End Using
        g.ResetTransform()
    End Sub

    Private Shared Sub DessinerAiguille(g As Graphics, a As Aiguille, taille As Integer)
        Dim c As Single = taille / 2.0F
        Dim pts As PointF() = Contour(a, taille)

        ' Deux facettes separees par l'arete centrale ; leur eclat depend de leur orientation
        ' par rapport a une lumiere venant d'en haut a gauche, comme le metal du widget.
        Dim rad As Double = a.Angle * Math.PI / 180.0
        Dim normaleGaucheX As Double = -Math.Cos(rad)
        Dim normaleGaucheY As Double = -Math.Sin(rad)
        Const lumiereX As Double = -0.7071, lumiereY As Double = -0.7071
        Dim eclatGauche As Double = 0.5 + 0.5 * (normaleGaucheX * lumiereX + normaleGaucheY * lumiereY)

        ' Chaque facette est refermee par l'arete centrale (de la queue a la pointe).
        Dim facetteGauche As PointF() = Facette(a, taille, -1)
        Dim facetteDroite As PointF() = Facette(a, taille, 1)

        g.ResetTransform()
        g.TranslateTransform(c, c)
        g.RotateTransform(a.Angle)
        Using gauche As New SolidBrush(Argent(eclatGauche)),
              droite As New SolidBrush(Argent(1.0 - eclatGauche)),
              bord As New Pen(Color.FromArgb(110, 70, 70, 76), Math.Max(0.5F, 0.0015F * taille))
            g.FillPolygon(gauche, facetteGauche)
            g.FillPolygon(droite, facetteDroite)
            g.FillPolygon(GrainAiguilles, pts)     ' meme grain de metal que les chiffres
            g.DrawPolygon(bord, pts)
        End Using
        g.ResetTransform()
    End Sub

    ''' <summary>Teinte argentee : 0 = facette dans l'ombre, 1 = facette eclairee.</summary>
    Private Shared Function Argent(eclat As Double) As Color
        ' Legere dominante bleutee, comme le metal du widget.
        Dim v As Integer = CInt(135 + 110 * Math.Max(0.0, Math.Min(1.0, eclat)))
        Return Color.FromArgb(255, v, v, Math.Min(255, v + 16))
    End Function

    ''' <summary>Les douze chiffres en relief (couche statique, calculee une fois par taille).</summary>
    Private Shared Function RendreChiffres(taille As Integer) As Bitmap
        Dim image As New Bitmap(taille, taille, PixelFormat.Format32bppArgb)
        Dim t As Single = taille
        Dim centre As Single = t / 2.0F
        Dim rayon As Single = RayonChiffres * t
        Dim hauteurVoulue As Single = HauteurChiffres * t

        Dim famille As FontFamily = PoliceDesChiffres()
        Using g As Graphics = Graphics.FromImage(image),
              texture As Bitmap = TextureGranit(),
              granit As New TextureBrush(texture)
            g.SmoothingMode = SmoothingMode.AntiAlias
            g.PixelOffsetMode = PixelOffsetMode.HighQuality

            ' Disque d'opacite 1/255 : invisible, mais une fenetre calque ne recoit la souris que
            ' la ou l'alpha n'est pas nul. Toute la surface de la montre attrape donc la molette
            ' et le glisser, tandis que les coins du carre laissent passer les clics.
            Using prise As New SolidBrush(Color.FromArgb(1, 0, 0, 0))
                g.FillEllipse(prise, 0, 0, taille - 1, taille - 1)
            End Using

            For h As Integer = 1 To 12
                Using chemin As New GraphicsPath()
                    chemin.AddString(h.ToString(), famille, CInt(FontStyle.Regular), 100.0F,
                                     PointF.Empty, StringFormat.GenericTypographic)

                    ' Mise a l'echelle sur la hauteur reelle du chiffre, puis inclinaison
                    ' (italique, ~13°) et centrage sur sa position autour du cadran.
                    Dim b As RectangleF = chemin.GetBounds()
                    Dim k As Single = hauteurVoulue / b.Height
                    Dim angle As Double = h * 30.0 * Math.PI / 180.0
                    Dim cx As Single = centre + CSng(rayon * Math.Sin(angle))
                    Dim cy As Single = centre - CSng(rayon * Math.Cos(angle))
                    Using m As New Matrix()
                        m.Translate(cx, cy)
                        m.Shear(InclinaisonCourante(), 0)
                        m.Scale(k, k)
                        m.Translate(-(b.Left + b.Width / 2), -(b.Top + b.Height / 2))
                        chemin.Transform(m)
                    End Using

                    If _relief Then DessinerRelief(g, chemin, t, granit) Else FormLieuBande.DessinerPlat(g, chemin, t)
                End Using
            Next
        End Using
        Return image
    End Function

    ''' <summary>Relief argente : ombre portee floue, arete sombre en bas a droite, reflet en haut
    ''' a gauche, face en degrade avec grain de pierre.</summary>
    Friend Shared Sub DessinerRelief(g As Graphics, chemin As GraphicsPath, t As Single, granit As TextureBrush)
        Dim ombre As Single = Math.Max(1.0F, 0.012F * t)
        Dim flou As Single = Math.Max(0.6F, 0.005F * t)
        Dim arete As Single = Math.Max(0.6F, 0.0035F * t)

        ' Ombre portee douce
        Using pinceau As New SolidBrush(Color.FromArgb(28, 0, 0, 0))
            For Each d As PointF In {New PointF(0, 0), New PointF(flou, 0), New PointF(-flou, 0),
                                     New PointF(0, flou), New PointF(0, -flou),
                                     New PointF(flou, flou), New PointF(-flou, -flou)}
                RemplirDecale(g, chemin, pinceau, ombre + d.X, ombre * 1.3F + d.Y)
            Next
        End Using

        ' Arete sombre (bas-droite) et reflet (haut-gauche) : donnent l'epaisseur
        Using sombre As New SolidBrush(Color.FromArgb(210, 88, 88, 110)),
              clair As New SolidBrush(Color.FromArgb(200, 255, 255, 255))
            RemplirDecale(g, chemin, sombre, arete, arete * 1.2F)
            RemplirDecale(g, chemin, clair, -arete * 0.5F, -arete * 0.6F)
        End Using

        ' Face : degrade vertical argente legerement bleute
        Dim b As RectangleF = chemin.GetBounds()
        Using face As New LinearGradientBrush(New RectangleF(b.X, b.Y - 1, b.Width, b.Height + 2),
                                              Color.FromArgb(255, 240, 240, 252),
                                              Color.FromArgb(255, 176, 176, 196),
                                              LinearGradientMode.Vertical)
            g.FillPath(face, chemin)
        End Using

        ' Grain de pierre
        g.FillPath(granit, chemin)

        ' Bombe : ombrage A L'INTERIEUR des traits. Le contour decale vers le haut-gauche tombe
        ' dans le chiffre le long de ses bords bas-droite (ombre) ; decale vers le bas-droite,
        ' le long des bords haut-gauche (reflet). Le tout limite a la forme du chiffre.
        Dim decalBombe As Single = Math.Max(0.5F, 0.0045F * t)
        g.SetClip(chemin, CombineMode.Replace)
        Using ombreInterne As New Pen(Color.FromArgb(90, 55, 55, 85), Math.Max(1.0F, 0.011F * t)),
              reflet As New Pen(Color.FromArgb(110, 255, 255, 255), Math.Max(0.8F, 0.007F * t))
            ombreInterne.LineJoin = LineJoin.Round
            reflet.LineJoin = LineJoin.Round
            g.TranslateTransform(-decalBombe, -decalBombe)
            g.DrawPath(ombreInterne, chemin)
            g.ResetTransform()
            g.TranslateTransform(decalBombe, decalBombe)
            g.DrawPath(reflet, chemin)
            g.ResetTransform()
        End Using
        g.ResetClip()
    End Sub

    ''' <summary>Un horaire de Chabbat, sur fond transparent, centre horizontalement et centre en
    ''' hauteur sur <paramref name="centreY"/> d'apres la forme reelle des chiffres (et non d'apres
    ''' la hauteur de ligne de la police, qui laisse un vide sous les chiffres). Meme famille
    ''' (Arial) et meme inclinaison que les chiffres du cadran, mais en Arial Gras, moins epais
    ''' que l'Arial Black des chiffres ; gris moyen avec ombre, sans contour.</summary>
    Private Shared Sub DessinerHoraire(g As Graphics, texte As String, centreY As Single, taille As Integer)
        If String.IsNullOrEmpty(texte) Then Exit Sub
        Dim t As Single = taille

        ' Police du lieu et de la date (Form4) : la meme ici. Par defaut (Arial Black) : Arial Gras, moins epais que les chiffres.
        Dim choisie As Boolean = _nomPolice <> "" AndAlso Not String.Equals(_nomPolice, "Arial Black", StringComparison.OrdinalIgnoreCase)
        Using chemin As New GraphicsPath(), arial As FontFamily = If(choisie, PoliceDesChiffres(), New FontFamily("Arial"))
            ' Taille de police : 12 % du cote de la montre, comme les anciennes zones de texte.
            Dim graisse As FontStyle = If(arial.IsStyleAvailable(FontStyle.Bold), FontStyle.Bold, FontStyle.Regular)
            If Not arial.IsStyleAvailable(graisse) Then graisse = FontStyle.Regular
            chemin.AddString(texte, arial, CInt(graisse), 0.12F * t,
                             PointF.Empty, StringFormat.GenericTypographic)
            ' Centrage sur la forme reelle, puis meme inclinaison que les chiffres (le cisaillement
            ' horizontal autour du centre du texte ne deplace ni son centre ni ses bords haut/bas).
            Dim b As RectangleF = chemin.GetBounds()
            Using m As New Matrix()
                m.Translate(t / 2.0F, centreY)
                m.Shear(InclinaisonCourante(), 0)
                m.Translate(-(b.Left + b.Width / 2.0F), -(b.Top + b.Height / 2.0F))
                chemin.Transform(m)
            End Using

            ' Ombre douce
            ' Ombre renforcee (demande de Jacob) : plus dense, un peu plus decalee et plus diffuse.
            Dim ombre As Single = Math.Max(1.0F, 0.011F * t)
            Dim flou As Single = Math.Max(0.6F, 0.005F * t)
            Using pinceau As New SolidBrush(Color.FromArgb(55, 0, 0, 0))
                For Each d As PointF In {New PointF(0, 0), New PointF(flou, 0), New PointF(-flou, 0),
                                         New PointF(0, flou), New PointF(0, -flou), New PointF(flou, flou)}
                    RemplirDecale(g, chemin, pinceau, ombre + d.X, ombre + d.Y)
                Next
            End Using

            ' Gris moyen legerement bleute, sans contour (demandes de Jacob).
            Using remplissage As New SolidBrush(Color.FromArgb(255, 160, 160, 170))
                g.FillPath(remplissage, chemin)
            End Using
        End Using
    End Sub

    Private Shared Sub RemplirDecale(g As Graphics, chemin As GraphicsPath, pinceau As Brush, dx As Single, dy As Single)
        g.TranslateTransform(dx, dy)
        g.FillPath(pinceau, chemin)
        g.ResetTransform()
    End Sub

    ''' <summary>Petit motif de bruit gris semi-transparent, graine fixe (rendu identique a chaque fois).</summary>
    Friend Shared Function TextureGranit() As Bitmap
        Const cote As Integer = 64
        Dim texture As New Bitmap(cote, cote, PixelFormat.Format32bppArgb)
        Dim hasard As New Random(18)
        For y As Integer = 0 To cote - 1
            For x As Integer = 0 To cote - 1
                Dim v As Integer = hasard.Next(0, 256)
                Dim alpha As Integer = If(v < 128, 28, 16)
                texture.SetPixel(x, y, Color.FromArgb(alpha, v, v, v))
            Next
        Next
        Return texture
    End Function

    Private Shared _nomPolice As String = ""
    Private Shared _versionPolice As Integer

    ''' <summary>Police des chiffres et de l'horaire de la montre : la meme que le lieu et la date des bandes (Form4, "Police du
    ''' lieu et de la date" ; 15/10/2026 : "pour uniformiser, le changement de police change aussi celle de l'horloge").
    ''' Les montres affichees se redessinent a la seconde suivante.</summary>
    ''' <summary>Inclinaison (italique ~13 deg) des chiffres, de l'horaire et du lieu/date : celle d'origine avec la police par defaut
    ''' (Arial Black, sans italique propre). Une police CHOISIE dans Form4 est dessinee telle quelle, sans etre penchee (15/10/2026 :
    ''' "la police subit une transformation").</summary>
    Friend Shared Function InclinaisonCourante() As Single
        Return If(_incline, InclinaisonChiffres, 0.0F)       ' case de Form4 (Montre) : chiffres inclines de 13 deg, sinon droits
    End Function

    Private Shared _relief As Boolean = True
    Private Shared _incline As Boolean = My.Settings.MontreInclinee
    Private Shared _opacitePourcent As Integer = My.Settings.MontreOpacite

    ''' <summary>Opacite de la montre (Form4, groupe Montre, curseur "Opacite" : 20 a 100 %, 75 par defaut).</summary>
    Friend Shared Sub DefinirOpacite(pourcent As Integer)
        _opacitePourcent = Math.Max(20, Math.Min(100, pourcent))
    End Sub

    Private Shared Function OpaciteReglee() As Single
        Return _opacitePourcent / 100.0F
    End Function

    ''' <summary>Chiffres (et horaire) de la montre inclines de 13 degres, ou droits (case de Form4, groupe Montre).</summary>
    Friend Shared Sub DefinirInclinaison(incline As Boolean)
        If incline = _incline Then Exit Sub
        _incline = incline
        _versionPolice += 1
    End Sub

    ''' <summary>Relief argente des chiffres (case de Form4, la meme que pour le lieu et la date) ; sinon chiffres plats.</summary>
    Friend Shared Sub DefinirRelief(avecRelief As Boolean)
        If avecRelief = _relief Then Exit Sub
        _relief = avecRelief
        _versionPolice += 1
    End Sub

    Friend Shared Sub DefinirPolice(nom As String)
        nom = If(String.IsNullOrWhiteSpace(nom), "", nom.Trim())
        If String.Equals(nom, _nomPolice, StringComparison.OrdinalIgnoreCase) Then Exit Sub
        _nomPolice = nom
        _versionPolice += 1
    End Sub

    ''' <summary>La police CHOISIE dans Form4 ("Police du lieu, de la date et de l'horloge") si ce n'est pas Arial Black, sinon la
    ''' police habituelle du texte (<paramref name="parDefaut"/>) : les autres textes de Slide (legende, titres, annonces)
    ''' suivent le choix, sans changer d'allure tant qu'on n'a rien choisi. A liberer par l'appelant (Using).</summary>
    Friend Shared Function PoliceChoisieOu(parDefaut As String) As FontFamily
        If _nomPolice <> "" AndAlso Not String.Equals(_nomPolice, "Arial Black", StringComparison.OrdinalIgnoreCase) Then
            Try
                Dim f As New FontFamily(_nomPolice)
                If f.IsStyleAvailable(FontStyle.Regular) Then Return f
                f.Dispose()
            Catch
            End Try
        End If
        Return New FontFamily(parDefaut)
    End Function

    Friend Shared Function PoliceDesChiffres() As FontFamily
        If _nomPolice <> "" Then
            Try
                Dim choisie As New FontFamily(_nomPolice)
                If choisie.IsStyleAvailable(FontStyle.Regular) Then Return choisie
            Catch
            End Try
        End If
        For Each nom As String In {"Arial Black", "Arial"}
            Try
                Dim f As New FontFamily(nom)
                If f.IsStyleAvailable(FontStyle.Regular) Then Return f
            Catch
            End Try
        Next
        Return FontFamily.GenericSansSerif
    End Function


End Class
