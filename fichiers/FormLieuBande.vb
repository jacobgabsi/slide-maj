Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

''' <summary>
''' Localite en grand, dans la bande noire de GAUCHE, quand la photo ou la video ne remplit pas
''' la largeur de l'ecran (photo en hauteur, video filmee en hauteur, ecran ultrawide...), avec
''' le pays puis la date en dessous, en plus petit (demande de Jacob : tout a gauche ; la bande
''' de droite reste vide). CoteDroit = True place la fenetre dans la bande de droite, si le
''' besoin revenait un jour.
'''
''' Meme style que les chiffres de la montre (FormHorloge) : Arial Black incline, relief argente
''' avec grain de pierre. La ville est ecrite le plus grand possible (coupee sur 1 a 3 lignes
''' si cela permet de l'agrandir), le pays en dessous en plus petit ; "~" devant un lieu deduit
''' d'un fichier voisin. Fenetre calque (voir CalqueWin32) : fond transparent, laisse passer les
''' clics, ne prend jamais le focus.
''' </summary>
Public Class FormLieuBande

    Private _reference As Form
    Private _premierPlan As Boolean = True
    Private _lieu As String = ""
    Private _pays As String = ""
    Private _date As String = ""
    Private _ratio As Double

    ''' <summary>False = bande de gauche (le lieu), True = bande de droite (la date).</summary>
    <System.ComponentModel.DesignerSerializationVisibility(System.ComponentModel.DesignerSerializationVisibility.Hidden)>
    Public Property CoteDroit As Boolean

    ' Le texte occupe au plus cette part de la largeur de la bande...
    Private Const PartLargeur As Single = 0.6F
    ' ... et de la hauteur de l'ecran.
    Private Const PartHauteur As Single = 0.4F
    ' Hauteur maximale des lettres de la ville (part de la hauteur de l'ecran).
    ' (0,085 au depart : trop voyant, le texte attirait l'oeil plus que la video.)
    Private Const HauteurLettresMax As Single = 0.04F
    ' Taille du pays par rapport a la ville.
    Private Const EchellePays As Single = 0.55F
    ' Espace au-dessus de la date, en interlignes (1 interligne = 0,3 x la hauteur des lettres) :
    ' 3,5 donne environ une hauteur de lettre de vide entre le lieu et la date.
    Private Const EcartAvantDate As Single = 3.5F

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            ' Pas de WS_EX_TRANSPARENT : le texte recoit la molette et le glisser (taille), comme la
            ' montre. Seul son rectangle capte la souris (fond d'alpha 1, voir RendreLieu).
            cp.ExStyle = cp.ExStyle Or WS_EX_LAYERED Or WS_EX_TOOLWINDOW Or WS_EX_NOACTIVATE
            Return cp
        End Get
    End Property

    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    ''' <summary>Affiche le lieu dans la bande noire de gauche de <paramref name="reference"/>,
    ''' et <paramref name="quand"/> (la date) juste en dessous, en plus petit.
    ''' <paramref name="ratioAffiche"/> = largeur/hauteur de l'image telle qu'elle est affichee
    ''' (0 = inconnu : rien n'est affiche). premierPlan : comme FormLegende.</summary>
    Friend Sub AfficherSur(reference As Form, lieu As LieuAffiche, deduit As Boolean,
                           ratioAffiche As Double, premierPlan As Boolean,
                           Optional quand As String = "")
        Dim ville As String = If(lieu.Localite, "").Trim()
        If deduit AndAlso ville <> "" Then ville = "~" & ville
        AfficherTexteSur(reference, ville, lieu.Pays, ratioAffiche, premierPlan, quand)
    End Sub

    ''' <summary>Meme chose avec un texte quelconque (la date seule, quand la photo n'a pas de
    ''' lieu) : <paramref name="texte"/> en grand, <paramref name="sousTitre"/> puis
    ''' <paramref name="sousTitre2"/> en dessous, en plus petit.</summary>
    Public Sub AfficherTexteSur(reference As Form, texte As String, sousTitre As String,
                                ratioAffiche As Double, premierPlan As Boolean,
                                Optional sousTitre2 As String = "")
        Rattacher(reference)
        Inscrire()
        PreparerSouris()
        _lieu = If(texte, "").Trim()
        _pays = If(sousTitre, "").Trim()
        _date = If(sousTitre2, "").Trim()
        _ratio = ratioAffiche
        _premierPlan = premierPlan
        CalqueWin32.DefinirPremierPlan(Me, reference, premierPlan)
        Rafraichir()
    End Sub

    Public Sub Masquer()
        Rattacher(Nothing)
        _lieu = ""
        _date = ""
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

    Private Sub ReferenceDeplacee(sender As Object, e As EventArgs)
        Rafraichir()
    End Sub

    ''' <summary>Vrai si les bandes noires laissees par une image de ce ratio sont assez larges
    ''' pour y ecrire (sinon rien n'y est affiche et la legende du bas garde tout).</summary>
    Public Shared Function BandeUtilisable(reference As Form, ratio As Double) As Boolean
        If reference Is Nothing OrElse reference.IsDisposed Then Return False
        Dim zone As Size = reference.ClientSize
        Dim minimum As Integer = Math.Max(CInt(120 * reference.DeviceDpi / 96.0), CInt(0.07 * zone.Width))
        Return LargeurBande(zone.Width, zone.Height, ratio) >= minimum
    End Function

    ''' <summary>Largeur de chaque bande noire laterale (0 si l'image remplit la largeur).</summary>
    Public Shared Function LargeurBande(largeur As Integer, hauteur As Integer, ratio As Double) As Integer
        If ratio <= 0 OrElse largeur <= 0 OrElse hauteur <= 0 Then Return 0
        Dim largeurImage As Double = Math.Min(largeur, hauteur * ratio)
        Return CInt(Math.Floor((largeur - largeurImage) / 2))
    End Function

    Private Sub Rafraichir()
        If _reference Is Nothing OrElse _reference.IsDisposed OrElse _lieu = "" Then
            If Me.Visible Then Me.Hide()
            Exit Sub
        End If
        If Not _premierPlan AndAlso
           (Not _reference.Visible OrElse _reference.WindowState = FormWindowState.Minimized) Then
            If Me.Visible Then Me.Hide()
            Exit Sub
        End If

        Dim zone As Rectangle = _reference.RectangleToScreen(_reference.ClientRectangle)
        Dim bande As Integer = LargeurBande(zone.Width, zone.Height, _ratio)
        ' Bande trop etroite pour y ecrire lisiblement : rien (la legende en bas a gauche suffit).
        If Not BandeUtilisable(_reference, _ratio) Then
            If Me.Visible Then Me.Hide()
            Exit Sub
        End If

        ' Taille choisie par l'utilisateur (commune aux deux bandes) ; plus large que PartLargeur
        ' si on agrandit, mais jamais plus que la bande.
        Dim echelle As Single = _pourcent / 100.0F
        Dim image As Bitmap = RendreLieu(_lieu, _pays,
                                         Math.Min(0.95F, PartLargeur * echelle) * bande,
                                         Math.Min(0.9F, PartHauteur * echelle) * zone.Height,
                                         HauteurLettresMax * echelle * zone.Height,
                                         sousTitre2:=_date)
        If image Is Nothing Then
            If Me.Visible Then Me.Hide()
            Exit Sub
        End If
        ' Meme opacite que la montre (CalqueWin32.OpaciteDiscrete).
        CalqueWin32.Attenuer(image, CalqueWin32.OpaciteDiscrete)
        Using image
            ' Centre dans la largeur de la bande ; en hauteur, en haut, au milieu ou en bas selon
            ' le choix de Form4 (PositionVerticale). En haut et en bas, un ecart de MargeVerticale
            ' de la hauteur separe le texte du bord de l'ecran.
            Dim gaucheBande As Integer = If(CoteDroit, zone.Right - bande, zone.Left)
            Dim marge As Integer = CInt(MargeVerticale * zone.Height)
            Dim haut As Integer
            Select Case _positionVerticale
                Case "Haut" : haut = zone.Top + marge
                Case "Bas" : haut = zone.Bottom - marge - image.Height
                Case Else : haut = zone.Top + (zone.Height - image.Height) \ 2
            End Select
            Dim voulu As New Rectangle(gaucheBande + (bande - image.Width) \ 2, haut,
                                       image.Width, image.Height)
            If Me.Bounds <> voulu Then Me.Bounds = voulu
            If Not Me.Visible Then Me.Show()
            CalqueWin32.Appliquer(Me, image)
        End Using
        If _premierPlan Then CalqueWin32.ReaffirmerPremierPlan(Me)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Rendu
    ' ------------------------------------------------------------------------------------------


    ' Taille de police de travail : tout est mesure a cette taille puis mis a l'echelle.
    Private Const TailleRef As Single = 100.0F

    ''' <summary>Morceau de texte insecable ; <c>Espace</c> = suivi d'une espace (sinon d'un
    ''' trait d'union deja inclus dans <c>Texte</c>, ex. "Saint-", "Jean-", "de-", "Luz").</summary>
    Private Structure Morceau
        Public Texte As String
        Public Espace As Boolean
    End Structure

    Private Shared Function Decouper(texte As String) As List(Of Morceau)
        Dim morceaux As New List(Of Morceau)
        For Each mot As String In texte.Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
            Dim parties As String() = mot.Split("-"c)
            For i As Integer = 0 To parties.Length - 1
                Dim dernier As Boolean = (i = parties.Length - 1)
                Dim t As String = If(dernier, parties(i), parties(i) & "-")
                If t = "" Then Continue For
                morceaux.Add(New Morceau With {.Texte = t, .Espace = dernier})
            Next
        Next
        Return morceaux
    End Function

    Private Shared Function Joindre(morceaux As List(Of Morceau), debut As Integer, fin As Integer) As String
        Dim sb As New Global.System.Text.StringBuilder()
        For i As Integer = debut To fin
            sb.Append(morceaux(i).Texte)
            If i < fin AndAlso morceaux(i).Espace Then sb.Append(" "c)
        Next
        Return sb.ToString()
    End Function

    ''' <summary>Chemin d'une ligne a la taille de reference, incline comme les chiffres de la
    ''' montre, recale pour que ses lettres commencent en (0, 0).</summary>
    Private Shared Function CheminLigne(texte As String, police As FontFamily, taille As Single) As GraphicsPath
        Dim chemin As New GraphicsPath()
        chemin.AddString(texte, police, CInt(FontStyle.Regular), taille, PointF.Empty, StringFormat.GenericTypographic)
        Using m As New Matrix()
            m.Shear(0.0F, 0)                                        ' droit : l'inclinaison est un reglage de la montre seule
            chemin.Transform(m)
        End Using
        Dim b As RectangleF = chemin.GetBounds()
        Using m As New Matrix()
            m.Translate(-b.Left, -b.Top)
            chemin.Transform(m)
        End Using
        Return chemin
    End Function

    ''' <summary>Toutes les coupes possibles en <paramref name="nbLignes"/> lignes (morceaux
    ''' consecutifs) ; renvoie celle dont la ligne la plus large est la plus etroite.</summary>
    Private Shared Function MeilleureCoupe(morceaux As List(Of Morceau), nbLignes As Integer,
                                           largeurDe As Func(Of String, Single)) As List(Of String)
        Dim meilleure As List(Of String) = Nothing
        Dim meilleureLargeur As Single = Single.MaxValue
        Dim n As Integer = morceaux.Count
        Dim coupes(nbLignes - 2) As Integer

        Dim essayer As Action = Sub()
                                    Dim lignes As New List(Of String)
                                    Dim debut As Integer = 0
                                    For Each c As Integer In coupes
                                        lignes.Add(Joindre(morceaux, debut, c))
                                        debut = c + 1
                                    Next
                                    lignes.Add(Joindre(morceaux, debut, n - 1))
                                    Dim l As Single = lignes.Max(Function(x) largeurDe(x))
                                    If l < meilleureLargeur Then meilleureLargeur = l : meilleure = lignes
                                End Sub

        If nbLignes = 1 Then
            essayer()
        Else
            ' Recursion simple : la coupe k se place apres la coupe k-1.
            Dim placer As Action(Of Integer, Integer) = Nothing
            placer = Sub(k As Integer, depuis As Integer)
                         If k = nbLignes - 1 Then essayer() : Exit Sub
                         ' Il doit rester au moins un morceau par ligne suivante.
                         For c As Integer = depuis To n - (nbLignes - k)
                             coupes(k) = c
                             placer(k + 1, c + 1)
                         Next
                     End Sub
            placer(0, 0)
        End If
        Return meilleure
    End Function

    ''' <summary>Le meme trace sans ses <paramref name="n"/> derniers contours (les derniers points d'une ligne qui finit par « ... » : chaque point est un contour ferme, ecrit apres les lettres).</summary>
    Private Shared Function SansLesDerniersContours(c As GraphicsPath, n As Integer) As GraphicsPath
        Dim points As PointF() = c.PathPoints
        Dim types As Byte() = c.PathTypes
        Dim debuts As New List(Of Integer)
        For i As Integer = 0 To types.Length - 1
            If (types(i) And &H7) = 0 Then debuts.Add(i)          ' (0 = debut d'un contour)
        Next
        If n <= 0 OrElse debuts.Count <= n Then Return c
        Dim fin As Integer = debuts(debuts.Count - n)
        Dim reduit As New GraphicsPath(points.Take(fin).ToArray(), types.Take(fin).ToArray(), c.FillMode)
        c.Dispose()
        Return reduit
    End Function

    ''' <summary>Ville (1 a 3 lignes, la mise en page qui l'ecrit le plus grand), puis le pays et
    ''' la date (<paramref name="sousTitre2"/>) en dessous en plus petit, le tout centre, en relief
    ''' argente. Nothing si le texte est vide.</summary>
    Public Shared Function RendreLieu(ville As String, pays As String, largeurMax As Single,
                                      hauteurMax As Single, hauteurLettresMax As Single,
                                      Optional ByRef margeImage As Integer = 0,
                                      Optional sousTitre2 As String = "",
                                      Optional pointsMasques As Integer = 0) As Bitmap
        Dim morceaux As List(Of Morceau) = Decouper(If(ville, ""))
        If morceaux.Count = 0 OrElse largeurMax < 10 OrElse hauteurMax < 10 Then Return Nothing
        pays = If(pays, "").Trim()

        Dim police As FontFamily = PoliceDeLaBande()     ' reglage Form4 (famille installee), sinon celle des chiffres de la montre
        Dim cache As New Dictionary(Of String, SizeF)
        Dim mesure As Func(Of String, SizeF) =
            Function(t As String) As SizeF
                Dim s As SizeF
                If Not cache.TryGetValue(t, s) Then
                    Using c As GraphicsPath = CheminLigne(t, police, TailleRef)
                        s = c.GetBounds().Size
                    End Using
                    cache(t) = s
                End If
                Return s
            End Function

        ' Hauteur des lettres a la taille de reference (une majuscule) : sert d'unite pour
        ' l'interligne, le plafond de taille et le relief.
        Dim hauteurLettres As Single = mesure("H").Height
        Dim interligne As Single = 0.3F * hauteurLettres

        ' Lignes secondaires, ecrites sous la ville et dans l'ordre : le pays, en plus petit, puis
        ' la date, A LA MEME TAILLE que la ville (demande de Jacob). echelles = taille par rapport
        ' a la ville ; ecarts = espace au-dessus de la ligne, en interlignes : le pays reste colle
        ' a la ville, la date en est nettement separee (demande de Jacob : plus joli).
        Dim sousLignes As New List(Of String)
        Dim echelles As New List(Of Single)
        Dim ecarts As New List(Of Single)
        If pays <> "" Then sousLignes.Add(pays) : echelles.Add(EchellePays) : ecarts.Add(1.3F)
        If If(sousTitre2, "").Trim() <> "" Then sousLignes.Add(sousTitre2.Trim()) : echelles.Add(1.0F) : ecarts.Add(EcartAvantDate)

        Dim meilleures As List(Of String) = Nothing
        Dim meilleurK As Single = 0
        For nb As Integer = 1 To Math.Min(3, morceaux.Count)
            Dim lignes As List(Of String) = MeilleureCoupe(morceaux, nb, Function(t) mesure(t).Width)
            If lignes Is Nothing Then Continue For
            Dim larg As Single = lignes.Max(Function(t) mesure(t).Width)
            Dim haut As Single = lignes.Sum(Function(t) mesure(t).Height) + (lignes.Count - 1) * interligne
            For i As Integer = 0 To sousLignes.Count - 1
                haut += interligne * ecarts(i) + mesure(sousLignes(i)).Height * echelles(i)
                ' La date, a la taille de la ville, doit tenir en largeur elle aussi : sinon ce
                ' serait elle qu'on reduirait, et les deux ne seraient plus a la meme taille.
                If echelles(i) >= 1.0F Then larg = Math.Max(larg, mesure(sousLignes(i)).Width)
            Next
            Dim k As Single = Math.Min(largeurMax / larg, hauteurMax / haut)
            k = Math.Min(k, hauteurLettresMax / hauteurLettres)
            ' Une ligne de plus seulement si elle agrandit nettement le texte.
            If k > meilleurK * 1.08F Then meilleurK = k : meilleures = lignes
        Next
        If meilleures Is Nothing OrElse meilleurK <= 0 Then Return Nothing

        ' Le pays peut etre plus large que la ville (ex. "Rome Italie") : on le reduit.
        Dim kSous As New List(Of Single)
        For i As Integer = 0 To sousLignes.Count - 1
            Dim k As Single = meilleurK * echelles(i)
            If mesure(sousLignes(i)).Width * k > largeurMax Then k = largeurMax / mesure(sousLignes(i)).Width
            kSous.Add(k)
        Next

        ' Assemblage a l'echelle finale, chaque ligne centree.
        Dim chemins As New List(Of GraphicsPath)
        Dim cheminsSous As New List(Of GraphicsPath)
        Try
            Dim y As Single = 0
            Dim largeurTotale As Single = 0
            For Each t As String In meilleures
                Dim c As GraphicsPath = CheminLigne(t, police, TailleRef * meilleurK)
                chemins.Add(c)
                Dim b As RectangleF = c.GetBounds()
                Using m As New Matrix()
                    m.Translate(-b.Width / 2.0F, y)
                    c.Transform(m)
                End Using
                y += b.Height + interligne * meilleurK
                largeurTotale = Math.Max(largeurTotale, b.Width)
            Next
            ' Pas d'interligne sous la derniere ligne : le bas de l'image = le bas des lettres.
            y -= interligne * meilleurK
            For i As Integer = 0 To sousLignes.Count - 1
                y += interligne * meilleurK * ecarts(i)
                Dim c As GraphicsPath = CheminLigne(sousLignes(i), police, TailleRef * kSous(i))
                cheminsSous.Add(c)
                Dim b As RectangleF = c.GetBounds()
                Using m As New Matrix()
                    m.Translate(-b.Width / 2.0F, y)
                    c.Transform(m)
                End Using
                y += b.Height
                largeurTotale = Math.Max(largeurTotale, b.Width)
                ' Points animes (« en preparation ... ») : la ligne est mise en page avec ses trois points (le texte ne bouge donc pas) ; les points pas encore allumes sont retires du dessin.
                If pointsMasques > 0 AndAlso sousLignes(i).EndsWith(".") Then
                    cheminsSous(cheminsSous.Count - 1) = SansLesDerniersContours(c, pointsMasques)
                End If
            Next

            ' Epaisseur du relief proportionnelle aux lettres, comme dans la montre (ou les
            ' chiffres mesurent HauteurChiffres x le cote de la montre).
            Dim tVille As Single = hauteurLettres * meilleurK / FormHorloge.HauteurChiffres
            Dim marge As Integer = CInt(Math.Ceiling(0.03F * tVille)) + 4
            margeImage = marge

            Dim largeur As Integer = CInt(Math.Ceiling(largeurTotale)) + 2 * marge
            Dim hauteur As Integer = CInt(Math.Ceiling(y)) + 2 * marge
            Dim image As New Bitmap(Math.Max(1, largeur), Math.Max(1, hauteur), PixelFormat.Format32bppArgb)
            Using g As Graphics = Graphics.FromImage(image),
                  texture As Bitmap = FormHorloge.TextureGranit(),
                  granit As New TextureBrush(texture)
                ' Fond invisible (alpha 1) : tout le rectangle du texte recoit la souris.
                g.Clear(Color.FromArgb(1, 0, 0, 0))
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.PixelOffsetMode = PixelOffsetMode.HighQuality
                Using m As New Matrix()
                    m.Translate(largeur / 2.0F, marge)
                    For Each c As GraphicsPath In chemins
                        c.Transform(m)
                        If _relief Then FormHorloge.DessinerRelief(g, c, tVille, granit) Else DessinerPlat(g, c, tVille)
                    Next
                    For i As Integer = 0 To cheminsSous.Count - 1
                        cheminsSous(i).Transform(m)
                        Dim tSous As Single = hauteurLettres * kSous(i) / FormHorloge.HauteurChiffres
                        If _relief Then FormHorloge.DessinerRelief(g, cheminsSous(i), tSous, granit) Else DessinerPlat(g, cheminsSous(i), tSous)
                    Next
                End Using
            End Using
            Return image
        Finally
            For Each c As GraphicsPath In chemins
                c.Dispose()
            Next
            For Each c As GraphicsPath In cheminsSous
                c.Dispose()
            Next
        End Try
    End Function

    Private Sub FormLieuBande_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        _instances.Remove(Me)
        Rattacher(Nothing)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Taille (comme la montre et la legende du bas) : molette, ou cliquer-glisser vers le haut
    ' pour agrandir. Taille COMMUNE aux deux bandes (lieu a gauche, date a droite) : agir sur
    ' l'une redimensionne aussi l'autre. Le geste est dans TailleALaSouris, partage avec la
    ' legende du bas.
    ' ------------------------------------------------------------------------------------------

    Public Const PourcentMini As Integer = 50
    Public Const PourcentMaxi As Integer = 300

    Private Shared _pourcent As Integer = 100
    Private Shared ReadOnly _instances As New List(Of FormLieuBande)

    Private _souris As TailleALaSouris

    ''' <summary>Leve une fois le geste termine avec la nouvelle taille en %, pour l'enregistrer.</summary>
    Public Event TailleChoisie(pourcent As Integer)

    Private Sub PreparerSouris()
        If _souris IsNot Nothing Then Exit Sub
        ' Texte aligne en bas : seul le deplacement vertical compte.
        _souris = New TailleALaSouris(Me, Function() _pourcent, AddressOf DefinirPourcent,
                                      Function() _reference)
        AddHandler _souris.TailleChoisie, Sub(pourcent As Integer) RaiseEvent TailleChoisie(pourcent)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Police du lieu, du pays et de la date (Form4, onglet Photos : "Police du lieu et de la date").
    ' ------------------------------------------------------------------------------------------

    Private Shared _nomPolice As String = "Arial Black"
    Private Shared _famille As FontFamily

    ''' <summary>La famille choisie si elle est installee (et a le style normal), sinon la police des chiffres de la montre.
    ''' Gardee en memoire : ne pas la liberer.</summary>
    Private Shared Function PoliceDeLaBande() As FontFamily
        If _famille Is Nothing Then
            Try
                Dim f As New FontFamily(_nomPolice)
                If f.IsStyleAvailable(FontStyle.Regular) Then _famille = f
            Catch ex As Exception
            End Try
        End If
        Return If(_famille, FormHorloge.PoliceDesChiffres())
    End Function

    Private Shared _relief As Boolean = True

    ''' <summary>Avec ou sans le relief argente (Form4, case "Relief argente") ; sans : texte plat, clair, avec une ombre douce.
    ''' Redessine les bandes affichees (apercu en direct).</summary>
    Public Shared Sub DefinirRelief(avecRelief As Boolean)
        FormHorloge.DefinirRelief(avecRelief)                       ' (la montre suit la meme case)
        If avecRelief = _relief Then Exit Sub
        _relief = avecRelief
        For Each f As FormLieuBande In _instances.ToArray()
            If Not f.IsDisposed AndAlso f.Visible Then f.Rafraichir()
        Next
    End Sub

    ''' <summary>Texte plat : une ombre douce puis la face claire, sans arete, reflet ni grain.</summary>
    Friend Shared Sub DessinerPlat(g As Graphics, chemin As GraphicsPath, t As Single)
        Dim ombre As Single = Math.Max(1.0F, 0.012F * t)
        Using pinceau As New SolidBrush(Color.FromArgb(70, 0, 0, 0))
            For Each d As PointF In {New PointF(0, 0), New PointF(ombre * 0.4F, 0), New PointF(0, ombre * 0.4F)}
                Using m As New Matrix()
                    m.Translate(ombre + d.X, ombre * 1.2F + d.Y)
                    Using decale As GraphicsPath = CType(chemin.Clone(), GraphicsPath)
                        decale.Transform(m)
                        g.FillPath(pinceau, decale)
                    End Using
                End Using
            Next
        End Using
        Using face As New SolidBrush(Color.FromArgb(255, 245, 245, 248))
            g.FillPath(face, chemin)
        End Using
    End Sub

    ''' <summary>Change la police des bandes ; redessine celles affichees (apercu en direct).</summary>
    Public Shared Sub DefinirPolice(nom As String)
        nom = If(String.IsNullOrWhiteSpace(nom), "Arial Black", nom.Trim())
        FormHorloge.DefinirPolice(nom)                              ' (la montre prend la meme police)
        If String.Equals(nom, _nomPolice, StringComparison.OrdinalIgnoreCase) AndAlso _famille IsNot Nothing Then Exit Sub
        _nomPolice = nom
        _famille = Nothing
        For Each f As FormLieuBande In _instances.ToArray()
            If Not f.IsDisposed AndAlso f.Visible Then f.Rafraichir()
        Next
    End Sub

    ''' <summary>Taille en % de la normale, pour les deux bandes ; redessine celles affichees.</summary>
    Public Shared Sub DefinirPourcent(pourcent As Integer)
        pourcent = Math.Max(PourcentMini, Math.Min(PourcentMaxi, pourcent))
        If pourcent = _pourcent Then Exit Sub
        _pourcent = pourcent
        For Each f As FormLieuBande In _instances.ToArray()
            If Not f.IsDisposed AndAlso f.Visible Then f.Rafraichir()
        Next
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Position en hauteur dans la bande : "Haut", "Milieu" (par defaut) ou "Bas" (Form4, onglet
    ' Photos). Commune a toutes les bandes, comme la taille.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Ecart entre le texte et le bord haut ou bas de l'ecran (part de la hauteur).</summary>
    Private Const MargeVerticale As Single = 0.05F

    Private Shared _positionVerticale As String = "Milieu"

    ''' <summary>Change la position en hauteur ; redessine les bandes affichees (apercu en direct).</summary>
    Public Shared Sub DefinirPositionVerticale(position As String)
        position = If(position = "Haut" OrElse position = "Bas", position, "Milieu")
        If position = _positionVerticale Then Exit Sub
        _positionVerticale = position
        For Each f As FormLieuBande In _instances.ToArray()
            If Not f.IsDisposed AndAlso f.Visible Then f.Rafraichir()
        Next
    End Sub

    Private Sub Inscrire()
        If Not _instances.Contains(Me) Then _instances.Add(Me)
    End Sub


End Class
