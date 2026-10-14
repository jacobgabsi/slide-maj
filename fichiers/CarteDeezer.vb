Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

''' <summary>
''' La pochette de l'album et le nom de la chanson que joue Deezer, affiches sur l'image de Slide
''' comme un PIP (demande de Jacob, 08/10/2026) : dans un des quatre coins, a la taille voulue,
''' avec une case pour l'afficher ou non (Form4, onglet Audio, cadre Deezer).
''' Depuis le 08/10/2026 au soir elle sert aussi aux autres musiques de Slide (demande de Jacob :
''' "fais le meme PIP affichage album pour les autres types de musique") : fichiers MP3 et radio,
''' nourris par MusiqueVLC (voir SuivreMorceau). Le nom du module et des reglages est reste.
''' Elle peut rester affichee tout le temps, ou seulement quelques secondes a chaque nouveau morceau
''' (meme cadre : "Toujours" ou "Pendant xx secondes").
'''
''' C'est une fenetre calque (FormCalqueImage), comme la legende et la montre : au-dessus des
''' photos comme des videos, sans prendre le clavier. Molette ou cliquer-glisser dessus : la
''' taille change et elle est enregistree (comme pour la legende).
'''
''' Elle se range d'elle-meme : au-dessus (ou au-dessous) d'un PIP qui occupe le meme coin, hors
''' de la bande de la legende, et sous la montre en haut a droite.
'''
''' Tout se passe dans le fil de l'interface : MusiqueDeezer appelle Suivre a chaque lecture de
''' l'etat du lecteur.
''' </summary>
Friend Module CarteDeezer

    Friend Const PourcentMini As Integer = 50
    Friend Const PourcentMaxi As Integer = 500
    ''' <summary>Marge avec le bord de l'image de Slide, en pixels a 96 ppp (comme la legende).</summary>
    Private Const MargeBordLogique As Integer = 12
    ''' <summary>La musique s'arrete un instant entre deux morceaux : la carte ne disparait
    ''' qu'apres ce delai sans musique.</summary>
    Private Const DelaiSansMusiqueMs As Integer = 3000

    Private _calque As FormCalqueImage
    Private _souris As TailleALaSouris

    ' Le morceau montre
    Private _titre As String = ""
    Private _artiste As String = ""
    Private _pochette As Image
    Private _cleDemandee As String = ""         ' morceau dont la pochette a ete demandee (titre, artiste, adresse)
    Private _nomRadio As String = ""            ' radio : nom de la station, ecrit dans la photo
    Private _source As String = ""            ' d'ou vient la musique : "deezer", "mp3" ou "radio" (petite icone sur la pochette)
    Private _vuJouerLe As Date = Date.MinValue
    ' Vrai quand la musique a ete VUE arretee (ou retiree expres) : seule une vraie reprise remontre la carte. Un blocage de l'interface (lecture des photos d'un album sur un
    ' PC lent) ne compte pas : aucune lecture de l'etat de la musique n'a lieu pendant ce temps, ce n'est pas un arret (21/10/2026 : la pochette revenait a chaque album).
    Private _arretVu As Boolean = True

    ' Ce qui est a l'ecran (pour ne redessiner que si quelque chose a change)
    Private _signature As String = ""

    ' Apercu des reglages pendant que Form4 est ouverte (Nothing : les reglages enregistres)
    Private _apercuActif As Boolean?
    Private _apercuCoin As String
    Private _apercuPourcent As Integer?
    Private _apercuSecondes As Integer?
    Private _apercuIcone As Boolean?
    Private _pourcentALaSouris As Integer?      ' taille en cours de reglage a la souris

    ''' <summary>Vrai si la carte est demandee (case des reglages, ou apercu en cours).</summary>
    Friend ReadOnly Property EstActive As Boolean
        Get
            Return Active
        End Get
    End Property

    Private ReadOnly Property Active As Boolean
        Get
            Return If(_apercuActif, DeezerCarte)
        End Get
    End Property

    ''' <summary>Vrai si la petite icone de la source est demandee (case des reglages, ou apercu en cours).</summary>
    Private ReadOnly Property IconeSourceDemandee As Boolean
        Get
            Return If(_apercuIcone, DeezerIconeSource)
        End Get
    End Property

    Private ReadOnly Property Coin As String
        Get
            Dim c As String = If(_apercuCoin, DeezerCarteCoin)
            Return If(String.IsNullOrEmpty(c), "Hgauche", c)
        End Get
    End Property

    Private ReadOnly Property Pourcent As Integer
        Get
            Dim p As Integer = If(_pourcentALaSouris, If(_apercuPourcent, DeezerCartePourcent))
            Return Math.Max(PourcentMini, Math.Min(PourcentMaxi, p))
        End Get
    End Property

    ''' <summary>Duree d'affichage a chaque nouveau morceau, en secondes ; 0 : la carte reste affichee.</summary>
    Private ReadOnly Property Secondes As Integer
        Get
            Return Math.Max(0, If(_apercuSecondes, DeezerCarteSecondes))
        End Get
    End Property

    ''' <summary>Moment ou la carte du morceau en cours a commence a se montrer (nouveau morceau,
    ''' reprise de la musique, reglage change) : avec une duree, elle part ce nombre de secondes apres.</summary>
    Private _debutAffichage As Date = Date.MinValue
    Private WithEvents _minuteurFin As New System.Windows.Forms.Timer()

    ' Avancement du morceau (0 a 1) : petite barre sous la pochette (15/10/2026). -1 : duree inconnue (radio), pas de barre.
    Private _progression As Double = -1

    ''' <summary>Avancement du morceau en cours, de 0 a 1 ; -1 si la duree est inconnue. A appeler juste avant SuivreMorceau.</summary>
    Friend Sub DefinirProgression(fraction As Double, Optional positionSecondes As Double = -1, Optional dureeSecondes As Double = -1)
        If Double.IsNaN(fraction) OrElse fraction < 0 OrElse fraction > 1.0001 Then fraction = -1
        _progression = fraction
        ' Duree du morceau, ecrite sous l'artiste (15/10/2026) : seulement quand elle est connue.
        If dureeSecondes > 1 AndAlso Not Double.IsNaN(dureeSecondes) AndAlso positionSecondes >= 0 Then
            _positionS = Math.Min(positionSecondes, dureeSecondes) : _dureeS = dureeSecondes
        Else
            _positionS = -1 : _dureeS = -1
        End If
    End Sub

    Private _positionS As Double = -1
    Private _dureeS As Double = -1

    ''' <summary>"m:ss / m:ss" (position / duree) ; "" si la duree est inconnue.</summary>
    Private Function TexteDuTemps(positionS As Double, dureeS As Double) As String
        If dureeS <= 1 OrElse positionS < 0 Then Return ""
        Dim Mmss As Func(Of Double, String) = Function(s) CInt(Math.Floor(s / 60)).ToString() & ":" & (CInt(Math.Floor(s)) Mod 60).ToString("00")
        Return Mmss(positionS) & " / " & Mmss(dureeS)
    End Function

    ' Bandeau au-dessus de la carte (15/10/2026, touche S : "affiche au dessus de l'album changement de source vers Deezer").
    Private _bandeau As String = ""
    Private _bandeauJusqua As Date = Date.MinValue
    Private WithEvents _minuteurBandeau As New System.Windows.Forms.Timer()

    Private ReadOnly Property BandeauActif As Boolean
        Get
            Return _bandeau <> "" AndAlso Now < _bandeauJusqua
        End Get
    End Property

    Private Sub MinuteurBandeau_Tick(sender As Object, e As EventArgs) Handles _minuteurBandeau.Tick
        _minuteurBandeau.Stop()
        _signature = ""
        Rafraichir()
    End Sub

    ''' <summary>Annonce, au-dessus de la carte et quelques secondes, que la source de la musique change ("Deezer", "Fichiers MP3", "Radio").</summary>
    Friend Sub AnnoncerLaSource(nom As String)
        If ConfierAuFilPrincipal(Sub() AnnoncerLaSource(nom), "Deezer") Then Exit Sub
        _bandeau = "Changement de source vers " & nom
        _bandeauJusqua = Now.AddSeconds(5)
        _debutAffichage = Now
        _signature = ""
        _minuteurBandeau.Stop()
        _minuteurBandeau.Interval = 5100
        _minuteurBandeau.Start()
        Rafraichir()
    End Sub

    ''' <summary>La carte, avec le bandeau de changement de source au-dessus quand il y en a un.</summary>
    Private Function AvecBandeau(carte As Bitmap, texte As String, cote As Integer, aGauche As Boolean) As Bitmap
        If texte = "" Then Return New Bitmap(carte)
        Dim bord As Integer = Math.Max(3, cote \ 10)
        Using police As New Font("Segoe UI Semibold", Math.Max(11.0F, cote * 0.2F), FontStyle.Regular, GraphicsUnit.Pixel)
            Dim largeurTexte As Integer, hauteurTexte As Integer
            Using mesure As New Bitmap(1, 1), gm As Graphics = Graphics.FromImage(mesure)
                largeurTexte = CInt(Math.Ceiling(gm.MeasureString(texte, police).Width))
                hauteurTexte = CInt(Math.Ceiling(police.GetHeight(gm)))
            End Using
            Dim largeurPastille As Integer = largeurTexte + 3 * bord
            Dim largeur As Integer = Math.Max(carte.Width, largeurPastille)
            Dim hBandeau As Integer = hauteurTexte + bord
            Dim ecart As Integer = Math.Max(2, bord \ 2)
            Dim resultat As New Bitmap(largeur, carte.Height + hBandeau + ecart, PixelFormat.Format32bppArgb)
            Using g As Graphics = Graphics.FromImage(resultat)
                g.Clear(Color.FromArgb(0, 0, 0, 0))
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
                Dim zonePastille As New RectangleF(If(aGauche, 0, largeur - largeurPastille), 0, largeurPastille, hBandeau)
                Using pastille As GraphicsPath = AnnonceEcran.Arrondi(zonePastille, bord * 1.6F),
                      fond As New SolidBrush(Color.FromArgb(205, 12, 12, 14)),
                      centre As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                    g.FillPath(fond, pastille)
                    g.DrawString(texte, police, Brushes.White, zonePastille, centre)
                End Using
                g.DrawImage(carte, If(aGauche, 0, largeur - carte.Width), hBandeau + ecart, carte.Width, carte.Height)
            End Using
            Return resultat
        End Using
    End Function

    Private Sub MinuteurFin_Tick(sender As Object, e As EventArgs) Handles _minuteurFin.Tick
        _minuteurFin.Stop()
        Rafraichir()
    End Sub

    ''' <summary>Apercu en direct depuis Form4 (case, coin, taille) ; enregistre par OK.</summary>
    Friend Sub Apercu(active As Boolean, coin As String, pourcent As Integer, Optional secondes As Integer = 0, Optional iconeSource As Boolean = False)
        _apercuActif = active : _apercuCoin = coin : _apercuPourcent = pourcent : _apercuSecondes = secondes : _apercuIcone = iconeSource
        _debutAffichage = Now                       ' (on la voit pendant qu'on regle, meme avec une duree)
        Rafraichir()
    End Sub

    ''' <summary>Form4 se ferme : retour aux reglages enregistres (ceux de OK, ou ceux d'avant).</summary>
    Friend Sub FinDeLApercu()
        _apercuActif = Nothing : _apercuCoin = Nothing : _apercuPourcent = Nothing : _apercuSecondes = Nothing : _apercuIcone = Nothing
        _debutAffichage = Now
        Rafraichir()
    End Sub

    ''' <summary>A chaque lecture de l'etat du lecteur de Deezer : montre, met a jour ou retire la carte.</summary>
    Friend Sub Suivre(etat As MusiqueDeezer.EtatDuLecteur, lecteurOuvert As Boolean)
        If lecteurOuvert AndAlso etat IsNot Nothing AndAlso etat.Joue AndAlso etat.Titre <> "" Then
            Dim adresse As String = etat.Pochette
            DefinirProgression(If(etat.Duree > 5 AndAlso etat.Position <= etat.Duree + 1, etat.Position / etat.Duree, -1), etat.Position, etat.Duree)
            SuivreMorceau(True, etat.Titre & vbLf & etat.Artiste & vbLf & adresse, etat.Titre, etat.Artiste,
                          Function() MusiqueDeezer.TelechargerLaPochette(adresse), "deezer")
        Else
            SuivreMorceau(False, "", "", "", Nothing)
        End If
    End Sub

    ''' <summary>
    ''' Ce que joue la musique en ce moment, quelle que soit sa source (Deezer, fichier, radio).
    ''' </summary>
    ''' <param name="joue">La musique joue vraiment.</param>
    ''' <param name="cle">Ce qui distingue ce morceau du precedent (la carte n'est refaite que s'il change).</param>
    ''' <param name="charger">Va chercher la pochette (hors du fil de l'interface) ; Nothing ou une
    ''' reponse Nothing : une note de musique a la place.</param>
    Friend Sub SuivreMorceau(joue As Boolean, cle As String, titre As String, artiste As String,
                             charger As Func(Of Threading.Tasks.Task(Of Image)), Optional source As String = "", Optional nomRadio As String = "")
        Try
            If joue AndAlso titre <> "" Then
                ' La musique reprend apres un arret : la carte se montre de nouveau (avec une duree, pour ce temps-la).
                If _arretVu Then _debutAffichage = Now
                _arretVu = False
                _vuJouerLe = Now
                If cle <> _cleDemandee Then
                    _cleDemandee = cle
                    Preparer(cle, titre, artiste, charger, source, nomRadio)
                End If
            ElseIf Not joue AndAlso _vuJouerLe <> Date.MinValue AndAlso (Now - _vuJouerLe).TotalMilliseconds >= DelaiSansMusiqueMs Then
                _arretVu = True                     ' (la musique est reellement arretee depuis plus longtemps que le delai entre deux morceaux)
            End If
            Rafraichir()
        Catch ex As Exception
            DiagLog("Musique (pochette a l'ecran) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>La musique est arretee expres (pause demandee, video, TV, Deezer ferme) : la carte
    ''' part tout de suite, sans attendre le delai.</summary>
    Friend Sub Retirer()
        If ConfierAuFilPrincipal(Sub() Retirer(), "Deezer") Then Exit Sub
        _vuJouerLe = Date.MinValue
        _arretVu = True
        If Epinglee Then Exit Sub                   ' epinglee (appui long sur M) : elle reste, meme si la musique change ou s'arrete
        Masquer()
    End Sub

    ''' <summary>
    ''' Touche M (15/10/2026) : la carte du morceau est montree de nouveau, pour la meme duree que celle prevue dans
    ''' les reglages (rien si elle est reglee sur "toujours" : elle est deja la). Sans morceau connu, rien a montrer.
    ''' </summary>
    Friend Sub Remontrer()
        If ConfierAuFilPrincipal(Sub() Remontrer(), "Deezer") Then Exit Sub
        If _titre = "" Then Exit Sub
        ' Seulement si la musique joue : sans musique, une pochette sans son serait trompeuse (15/10/2026).
        If (Now - _vuJouerLe).TotalMilliseconds >= DelaiSansMusiqueMs Then
            DiagLog("Touche M : aucune musique ne joue, pochette non affichee")
            Exit Sub
        End If
        _debutAffichage = Now
        _signature = ""
        Rafraichir()
    End Sub

    ''' <summary>La carte reste a l'ecran, meme M relachee, quel que soit le delai des reglages : un appui de 3 secondes sur M
    ''' l'epingle (bip), un nouvel appui de 3 secondes (bip) rend le cours normal (20/10/2026). Epinglee, elle reste aussi quand la
    ''' musique change de morceau ou de source, ou s'arrete (elle montre alors le dernier morceau) ; seul un appui long la libere.</summary>
    Friend Epinglee As Boolean = False

    ''' <summary>Appui long sur M : epingle la carte, ou, si elle l'est deja, la libere (cours normal : duree des reglages).
    ''' Rend l'etat obtenu : Vrai = epinglee ; Faux = liberee, ou rien a epingler (aucune musique ne joue).</summary>
    Friend Function BasculerEpinglee() As Boolean
        Dim etat As Boolean = False
        If ConfierAuFilPrincipal(Sub() etat = BasculerEpinglee(), "Deezer") Then Return Epinglee
        If Epinglee Then
            Epinglee = False
            _debutAffichage = Now                     ' (le compte des reglages repart d'ici)
            DiagLog("Touche M (appui long) : pochette liberee, cours normal")
            Rafraichir()
            Return False
        End If
        If _titre = "" OrElse (Now - _vuJouerLe).TotalMilliseconds >= DelaiSansMusiqueMs Then
            DiagLog("Touche M (appui long) : aucune musique ne joue, pochette non epinglee")
            Return False
        End If
        Epinglee = True
        _signature = ""
        DiagLog("Touche M (appui long) : pochette laissee a l'ecran jusqu'au prochain appui long")
        Rafraichir()
        Return True
    End Function

    ''' <summary>Le morceau change : la carte garde l'ancien morceau le temps que la nouvelle
    ''' pochette arrive (un dixieme de seconde en general), puis tout change d'un coup.</summary>
    Private Async Sub Preparer(cle As String, titre As String, artiste As String, charger As Func(Of Threading.Tasks.Task(Of Image)), source As String, nomRadio As String)
        Try
            Dim image As Image = Nothing
            If charger IsNot Nothing Then image = Await charger()
            If cle <> _cleDemandee Then
                If image IsNot Nothing Then image.Dispose()         ' un autre morceau a ete demande entre-temps
                Exit Sub
            End If
            Dim ancienne As Image = _pochette
            _pochette = image : _titre = titre : _artiste = artiste : _source = If(source, "") : _nomRadio = If(nomRadio, "")
            _debutAffichage = Now
            _signature = ""
            If ancienne IsNot Nothing Then ancienne.Dispose()
            Rafraichir()
        Catch ex As Exception
            DiagLog("Deezer (pochette a l'ecran) : " & ex.Message)
        End Try
    End Sub

    Private Sub Masquer()
        _minuteurFin.Stop()
        If _calque IsNot Nothing AndAlso Not _calque.IsDisposed AndAlso _calque.Visible Then _calque.Masquer()
        _signature = ""
    End Sub

    ''' <summary>Met la carte a l'ecran a sa place, ou la retire. Ne redessine que si le morceau,
    ''' les reglages ou la place ont change.</summary>
    Friend Sub Rafraichir()
        Try
            Dim principale As Form = FenetrePrincipale()
            Dim visible As Boolean = Active AndAlso _titre <> "" AndAlso
                                     (Secondes <= 0 OrElse Epinglee OrElse (Now - _debutAffichage).TotalSeconds < Secondes) AndAlso
                                     ((Now - _vuJouerLe).TotalMilliseconds < DelaiSansMusiqueMs OrElse BandeauActif OrElse Epinglee) AndAlso
                                     principale IsNot Nothing AndAlso Not principale.IsDisposed AndAlso
                                     principale.Visible AndAlso principale.WindowState <> FormWindowState.Minimized
            If Not visible Then
                Masquer()
                MontrerLIconeSource(principale)
                Exit Sub
            End If
            MasquerLIconeSource()

            Dim zone As Rectangle = principale.RectangleToScreen(principale.ClientRectangle)
            If zone.Width < 200 OrElse zone.Height < 150 Then Masquer() : MasquerLIconeSource() : Exit Sub
            ' Taille normale (100 %) : la pochette fait un neuvieme de la hauteur de l'image de Slide.
            Dim cote As Integer = Math.Max(24, CInt(zone.Height / 9.0 * Pourcent / 100.0))
            cote = Math.Min(cote, CInt(zone.Height * 0.6))
            Dim marge As Integer = CInt(MargeBordLogique * principale.DeviceDpi / 96.0)
            Dim pip As Rectangle? = Form12.ZoneDuPipDansLeCoin(Coin)
            Dim legende As Rectangle = If(AfficheDate OrElse AfficheLieu, ZoneLegendeReservee, Rectangle.Empty)
            Dim montre As Rectangle = If(AfficheClock, FormHorloge.ZoneDansEcran(principale, ClockTaillePourcent), Rectangle.Empty)

            Dim signature As String = String.Join("|", _titre, _artiste, _source, _nomRadio, If(_pochette Is Nothing, "0", _pochette.GetHashCode().ToString()),
                                                  cote, Coin, zone.ToString(), FullScreen, If(pip.HasValue, pip.Value.ToString(), ""),
                                                  legende.ToString(), montre.ToString(), If(BandeauActif, _bandeau, ""),
                                                  If(_progression >= 0, CInt(_progression * 200).ToString(), "-"), TexteDuTemps(_positionS, _dureeS),
                                                  My.Settings.DeezerCarteX, My.Settings.DeezerCarteY)
            If _calque IsNot Nothing AndAlso Not _calque.IsDisposed AndAlso _calque.Visible AndAlso signature = _signature Then
                ArmerLaFin()
                Exit Sub
            End If

            If _calque Is Nothing OrElse _calque.IsDisposed Then
                _calque = New FormCalqueImage()
                _calque.Cursor = Cursors.SizeAll                          ' cliquer-glisser la deplace ; Ctrl + glisser ou la molette changent sa taille
                _souris = New TailleALaSouris(_calque, Function() Pourcent, AddressOf TailleALaSourisChangee,
                                              Function() FenetrePrincipale(), poidsHorizontal:=0.25)
                AddHandler _souris.TailleChoisie, AddressOf TailleALaSourisChoisie
                _souris.IgnorerLAppui = Function() Not CtrlEnfonce()          ' sans Ctrl : cliquer-glisser DEPLACE la pochette ; avec Ctrl : il l'agrandit
                AddHandler _calque.MouseDown, AddressOf CarteSouris_Down
                AddHandler _calque.MouseMove, AddressOf CarteSouris_Move
                AddHandler _calque.MouseUp, AddressOf CarteSouris_Up
                AddHandler _calque.MouseDoubleClick, AddressOf CarteSouris_DoubleClic
                AddHandler Form12.PlaceChangee, AddressOf PipABouge
            End If
            ' Le glisser agrandit en tirant vers l'interieur de l'ecran, quel que soit le coin.
            Dim enHaut As Boolean = Coin.StartsWith("H", StringComparison.OrdinalIgnoreCase)
            Dim aGauche As Boolean = Coin.EndsWith("gauche", StringComparison.OrdinalIgnoreCase)
            _souris.SensHorizontal = If(aGauche, -1, 1)
            _souris.SensVertical = If(enHaut, -1, 1)

            Using carte As Bitmap = Rendre(_titre, _artiste, _pochette, cote, CInt(zone.Width * 0.6), _source, _progression, TexteDuTemps(_positionS, _dureeS), _nomRadio),
                  image As Bitmap = AvecBandeau(carte, If(BandeauActif, _bandeau, ""), cote, aGauche)
                Dim x As Integer = If(aGauche, zone.Left + marge, zone.Right - marge - image.Width)
                Dim y As Integer = If(enHaut, zone.Top + marge, zone.Bottom - marge - image.Height)
                Dim ecart As Integer = Math.Max(4, marge \ 2)
                ' Hors de la bande de la legende (lieu, date), qui prend toute la largeur.
                If Not legende.IsEmpty AndAlso legende.IntersectsWith(New Rectangle(x, y, image.Width, image.Height)) Then
                    y = If(enHaut, legende.Bottom + ecart, legende.Top - ecart - image.Height)
                End If
                ' Sous la montre (en haut a droite).
                If Not montre.IsEmpty AndAlso montre.IntersectsWith(New Rectangle(x, y, image.Width, image.Height)) Then
                    y = montre.Bottom + ecart
                End If
                ' Un PIP dans le meme coin : la carte se met juste au-dessus de lui (ou au-dessous, en haut).
                If pip.HasValue Then
                    y = If(enHaut, Math.Max(y, pip.Value.Bottom + ecart), Math.Min(y, pip.Value.Top - ecart - image.Height))
                End If
                y = Math.Max(zone.Top, Math.Min(y, zone.Bottom - image.Height))
                ' Place choisie a la souris (Ctrl + glisser) : elle remplace le coin des reglages.
                If My.Settings.DeezerCarteX >= 0 AndAlso My.Settings.DeezerCarteY >= 0 Then
                    x = zone.Left + CInt(Math.Min(1.0, My.Settings.DeezerCarteX) * Math.Max(0, zone.Width - image.Width))
                    y = zone.Top + CInt(Math.Min(1.0, My.Settings.DeezerCarteY) * Math.Max(0, zone.Height - image.Height))
                End If
                _carteZone = zone
                _calque.Montrer(image, New Point(x, y), principale, FullScreen)
            End Using
            _signature = signature
            ArmerLaFin()
        Catch ex As Exception
            DiagLog("Deezer (pochette a l'ecran) : " & ex.Message)
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Icone de la source, seule, en bas a gauche (demande de Jacob, 21/10/2026 : "afficher icone de la source ... qui va rester
    ' constamment a l'ecran quand la pochette de l'album n'est pas affichee"). Tant que la musique joue et que la carte est cachee
    ' (delai ecoule, ou carte non demandee), la petite icone reste. Meme calque que la carte, sans souris.
    ' ------------------------------------------------------------------------------------------

    Private _calqueIcone As FormCalqueImage
    Private _signatureIcone As String = ""

    Private _iconeD As Single, _iconeM As Single, _iconeZone As Rectangle
    Private _iconeGlisse As Boolean, _iconePointeurDepart As Point, _iconePlaceDepart As Point

    Private Sub IconeSouris_Down(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Exit Sub
        _iconeGlisse = True
        _iconePointeurDepart = Cursor.Position
        _iconePlaceDepart = _calqueIcone.Location
    End Sub

    Private Sub IconeSouris_Move(sender As Object, e As MouseEventArgs)
        If Not _iconeGlisse Then Exit Sub
        Dim p As Point = Cursor.Position
        Dim m As Integer = CInt(_iconeM), d As Integer = CInt(_iconeD)
        Dim x As Integer = _iconePlaceDepart.X + p.X - _iconePointeurDepart.X
        Dim y As Integer = _iconePlaceDepart.Y + p.Y - _iconePointeurDepart.Y
        ' Le rond reste dans l'image de Slide.
        x = Math.Max(_iconeZone.Left - m, Math.Min(x, _iconeZone.Right - d - m))
        y = Math.Max(_iconeZone.Top - m, Math.Min(y, _iconeZone.Bottom - d - m))
        _calqueIcone.Location = New Point(x, y)
    End Sub

    Private Sub IconeSouris_Up(sender As Object, e As MouseEventArgs)
        If Not _iconeGlisse Then Exit Sub
        _iconeGlisse = False
        Dim m As Integer = CInt(_iconeM), d As Integer = CInt(_iconeD)
        Dim course As Func(Of Integer, Integer, Double) = Function(v, total) If(total <= 0, 0.0, Math.Max(0.0, Math.Min(1.0, v / total)))
        My.Settings.DeezerIconeX = course(_calqueIcone.Left + m - _iconeZone.Left, _iconeZone.Width - d)
        My.Settings.DeezerIconeY = course(_calqueIcone.Top + m - _iconeZone.Top, _iconeZone.Height - d)
        My.Settings.Save()
        _signatureIcone = ""
    End Sub

    ' Double-clic : l'icone retourne tout en bas a gauche (sa place par defaut).
    Private Sub IconeSouris_DoubleClic(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left Then Exit Sub
        _iconeGlisse = False
        My.Settings.DeezerIconeX = 0
        My.Settings.DeezerIconeY = 1
        My.Settings.Save()
        _signatureIcone = ""
        Rafraichir()
    End Sub

    Private Sub MasquerLIconeSource()
        _signatureIcone = ""
        If _calqueIcone IsNot Nothing AndAlso Not _calqueIcone.IsDisposed AndAlso _calqueIcone.Visible Then _calqueIcone.Masquer()
    End Sub

    Private Sub MontrerLIconeSource(principale As Form)
        Try
            If Not IconeSourceDemandee OrElse _titre = "" OrElse principale Is Nothing OrElse principale.IsDisposed OrElse Not principale.Visible OrElse
               principale.WindowState = FormWindowState.Minimized OrElse
               ((Now - _vuJouerLe).TotalMilliseconds >= DelaiSansMusiqueMs AndAlso Not Epinglee) Then
                MasquerLIconeSource()
                Exit Sub
            End If
            Dim s As String = If(_source, "").ToLowerInvariant()
            If s <> "deezer" AndAlso s <> "mp3" AndAlso s <> "radio" Then MasquerLIconeSource() : Exit Sub
            Dim zone As Rectangle = principale.RectangleToScreen(principale.ClientRectangle)
            If zone.Width < 200 OrElse zone.Height < 150 Then MasquerLIconeSource() : Exit Sub
            Dim cote As Integer = Math.Max(24, CInt(zone.Height / 9.0 * Pourcent / 100.0))
            cote = Math.Min(cote, CInt(zone.Height * 0.6))
            Dim marge As Integer = CInt(MargeBordLogique * principale.DeviceDpi / 96.0)
            Dim pip As Rectangle? = Form12.ZoneDuPipDansLeCoin("Bgauche")
            Dim legende As Rectangle = If(AfficheDate OrElse AfficheLieu, ZoneLegendeReservee, Rectangle.Empty)
            Dim signature As String = String.Join("|", s, cote, zone.ToString(), FullScreen, My.Settings.DeezerIconeX, My.Settings.DeezerIconeY)
            If _calqueIcone IsNot Nothing AndAlso Not _calqueIcone.IsDisposed AndAlso _calqueIcone.Visible AndAlso signature = _signatureIcone Then Exit Sub
            If _calqueIcone Is Nothing OrElse _calqueIcone.IsDisposed Then
                _calqueIcone = New FormCalqueImage()
                _calqueIcone.Cursor = Cursors.SizeAll                       ' se deplace a la souris (cliquer-glisser)
                AddHandler _calqueIcone.MouseDown, AddressOf IconeSouris_Down
                AddHandler _calqueIcone.MouseMove, AddressOf IconeSouris_Move
                AddHandler _calqueIcone.MouseUp, AddressOf IconeSouris_Up
                AddHandler _calqueIcone.MouseDoubleClick, AddressOf IconeSouris_DoubleClic
            End If
            Dim c As Single = cote * 1.4F                                   ' (DessinerLaSource trace un rond de 0,36 c : ici la moitie de la pochette)
            Dim d As Single = Math.Max(14.0F, c * 0.36F)
            Dim m As Single = Math.Max(2.0F, c * 0.05F)
            Dim cote2 As Integer = CInt(Math.Ceiling(d + 2 * m))
            Using image As New Bitmap(cote2, cote2, PixelFormat.Format32bppArgb)
                Using g As Graphics = Graphics.FromImage(image)
                    g.Clear(Color.FromArgb(0, 0, 0, 0))
                    g.SmoothingMode = SmoothingMode.AntiAlias
                    g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
                    DessinerLaSource(g, s, New Rectangle(0, 0, cote2, cote2), CInt(c))
                End Using
                _iconeD = d : _iconeM = m
                _iconeZone = zone
                ' Place enregistree : fraction de la course horizontale et verticale du rond (0,0 = haut gauche ; 0,1 = bas gauche, defaut).
                Dim fx As Double = Math.Max(0, Math.Min(1, My.Settings.DeezerIconeX))
                Dim fy As Double = Math.Max(0, Math.Min(1, My.Settings.DeezerIconeY))
                Dim x As Integer = zone.Left + CInt(fx * (zone.Width - d)) - CInt(m)
                Dim y As Integer = zone.Top + CInt(fy * (zone.Height - d)) - CInt(m)
                Dim ecart As Integer = Math.Max(4, marge \ 2)
                _calqueIcone.Montrer(image, New Point(x, y), principale, FullScreen)
            End Using
            _signatureIcone = signature
        Catch ex As Exception
            DiagLog("Icone de la source : " & ex.Message)
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Deplacer la pochette a la souris (demande de Jacob, 21/10/2026) : Ctrl + cliquer-glisser. Sa place est enregistree en proportion de l'image (DeezerCarteX / Y, de 0 a 1) et
    ' remplace le coin des reglages ; Ctrl + double-clic la remet dans le coin choisi. (Sans Ctrl, glisser ou la molette changent toujours sa taille.)
    ' ------------------------------------------------------------------------------------------

    Private _carteZone As Rectangle
    Private _carteGlisse As Boolean
    Private _cartePointeurDepart As Point
    Private _cartePlaceDepart As Point

    Private Function CtrlEnfonce() As Boolean
        Return (Control.ModifierKeys And Keys.Control) = Keys.Control
    End Function

    Private Sub CarteSouris_Down(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left OrElse CtrlEnfonce() Then Exit Sub
        _carteGlisse = True
        _cartePointeurDepart = Cursor.Position
        _cartePlaceDepart = _calque.Location
        _calque.Capture = True
    End Sub

    Private Sub CarteSouris_Move(sender As Object, e As MouseEventArgs)
        If Not _carteGlisse Then Exit Sub
        Dim p As Point = Cursor.Position
        Dim x As Integer = _cartePlaceDepart.X + p.X - _cartePointeurDepart.X
        Dim y As Integer = _cartePlaceDepart.Y + p.Y - _cartePointeurDepart.Y
        ' La pochette reste dans l'image de Slide.
        x = Math.Max(_carteZone.Left, Math.Min(x, _carteZone.Right - _calque.Width))
        y = Math.Max(_carteZone.Top, Math.Min(y, _carteZone.Bottom - _calque.Height))
        _calque.Location = New Point(x, y)
    End Sub

    Private Sub CarteSouris_Up(sender As Object, e As MouseEventArgs)
        If Not _carteGlisse Then Exit Sub
        _carteGlisse = False
        _calque.Capture = False
        Dim largeurLibre As Integer = _carteZone.Width - _calque.Width
        Dim hauteurLibre As Integer = _carteZone.Height - _calque.Height
        My.Settings.DeezerCarteX = If(largeurLibre <= 0, 0.0, Math.Max(0.0, Math.Min(1.0, (_calque.Left - _carteZone.Left) / CDbl(largeurLibre))))
        My.Settings.DeezerCarteY = If(hauteurLibre <= 0, 0.0, Math.Max(0.0, Math.Min(1.0, (_calque.Top - _carteZone.Top) / CDbl(hauteurLibre))))
        My.Settings.Save()
        _signature = ""
        Rafraichir()
    End Sub

    Private Sub CarteSouris_DoubleClic(sender As Object, e As MouseEventArgs)
        If e.Button <> MouseButtons.Left OrElse CtrlEnfonce() Then Exit Sub
        _carteGlisse = False
        My.Settings.DeezerCarteX = -1
        My.Settings.DeezerCarteY = -1
        My.Settings.Save()
        _signature = ""
        Rafraichir()
    End Sub

    ''' <summary>Avec une duree : la carte partira a l'heure dite, sans attendre la prochaine lecture
    ''' de l'etat du lecteur.</summary>
    Private Sub ArmerLaFin()
        If Secondes <= 0 Then
            _minuteurFin.Stop()
            Exit Sub
        End If
        Dim reste As Double = Secondes * 1000.0 - (Now - _debutAffichage).TotalMilliseconds
        _minuteurFin.Stop()
        _minuteurFin.Interval = CInt(Math.Max(50, Math.Min(reste + 50, 3600000)))
        _minuteurFin.Start()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Taille a la souris (molette, ou cliquer-glisser vers l'interieur de l'ecran)
    ' ------------------------------------------------------------------------------------------

    Private Sub TailleALaSourisChangee(pourcent As Integer)
        _pourcentALaSouris = Math.Max(PourcentMini, Math.Min(PourcentMaxi, pourcent))
        Rafraichir()
    End Sub

    Private Sub TailleALaSourisChoisie(pourcent As Integer)
        pourcent = Math.Max(PourcentMini, Math.Min(PourcentMaxi, pourcent))
        _pourcentALaSouris = Nothing
        DeezerCartePourcent = pourcent
        My.Settings.DeezerCartePourcent = pourcent
        My.Settings.Save()
        Rafraichir()
    End Sub

    ''' <summary>Un PIP est apparu, a bouge ou a disparu (evenement du fil du PIP) : la carte se replace.</summary>
    Private Sub PipABouge()
        Try
            Dim principale As Form = FenetrePrincipale()
            If principale Is Nothing OrElse principale.IsDisposed OrElse Not principale.IsHandleCreated Then Exit Sub
            principale.BeginInvoke(New Action(AddressOf Rafraichir))
        Catch ex As Exception
            ' fenetre en cours de fermeture
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Dessin
    ' ------------------------------------------------------------------------------------------

    ''' <summary>La carte : pochette carree de <paramref name="cote"/> pixels a gauche, titre (gras)
    ''' et artiste a droite, sur un panneau sombre arrondi a demi transparent. Sans pochette : une
    ''' note de musique. Toutes les tailles sont en pixels.</summary>
    Friend Function Rendre(titre As String, artiste As String, pochette As Image, cote As Integer, largeurMax As Integer,
                           Optional source As String = "", Optional progression As Double = -1, Optional texteTemps As String = "", Optional nomRadio As String = "") As Bitmap
        Dim bord As Integer = Math.Max(3, cote \ 10)
        Dim tailleTitre As Single = cote * 0.27F
        Dim tailleArtiste As Single = cote * 0.21F
        Using policeTitre As New Font("Segoe UI Semibold", tailleTitre, FontStyle.Regular, GraphicsUnit.Pixel),
              policeArtiste As New Font("Segoe UI", tailleArtiste, FontStyle.Regular, GraphicsUnit.Pixel),
              policeTemps As New Font("Segoe UI", cote * 0.17F, FontStyle.Regular, GraphicsUnit.Pixel),
              format As New StringFormat() With {.Trimming = StringTrimming.EllipsisWord, .LineAlignment = StringAlignment.Near}
            ' Largeur du texte : celle du plus long des deux, bornee.
            Dim largeurTexte As Integer
            Dim interligne As Single = cote * 0.06F
            Dim hTitreM As Single, hArtisteM As Single, hTempsM As Single
            Using mesure As New Bitmap(1, 1), gm As Graphics = Graphics.FromImage(mesure)
                Dim l As Single = Math.Max(gm.MeasureString(titre, policeTitre).Width, gm.MeasureString(artiste, policeArtiste).Width)
                If texteTemps <> "" Then l = Math.Max(l, gm.MeasureString(texteTemps, policeTemps).Width)
                Dim maxi As Integer = Math.Max(cote, Math.Min(largeurMax - cote - 3 * bord, CInt(cote * 5.5)))
                largeurTexte = Math.Max(cote, Math.Min(maxi, CInt(Math.Ceiling(l)) + 2))
                ' Texte 40 % moins large, sur plusieurs lignes (15/10/2026) ; la pochette ne change pas.
                largeurTexte = Math.Max(CInt(cote * 0.9), CInt(largeurTexte * 0.6))
                ' Rectangle elargi de 20 % (15/10/2026), la pochette ne change pas : la largeur gagnee va au texte.
                Dim largeurPanneau As Integer = 3 * bord + bord \ 2 + cote + largeurTexte
                largeurTexte += CInt(Math.Round(largeurPanneau * 0.2))
                ' Hauteur de chaque texte une fois retourne a la ligne : trois lignes au plus pour le titre, deux pour l'artiste.
                hTitreM = Math.Min(gm.MeasureString(titre, policeTitre, largeurTexte, format).Height, policeTitre.GetHeight(gm) * 3.0F)
                hArtisteM = Math.Min(gm.MeasureString(artiste, policeArtiste, largeurTexte, format).Height, policeArtiste.GetHeight(gm) * 2.0F)
                hTempsM = If(texteTemps <> "", policeTemps.GetHeight(gm) + interligne * 0.6F, 0.0F)
            End Using
            Dim hTexteTotal As Single = hTitreM + interligne + hArtisteM + hTempsM
            Dim hInterieure As Integer = Math.Max(cote, CInt(Math.Ceiling(hTexteTotal)))
            ' La pochette est agrandie a la hauteur du texte (19/10/2026) : son haut est au niveau du haut du titre, son bas
            ' au niveau du bas du texte. (Texte plus court que la pochette : elle garde sa taille.)
            Dim coteImage As Integer = hInterieure
            Dim largeur As Integer = bord + coteImage + bord + largeurTexte + bord + bord \ 2
            Dim hauteur As Integer = hInterieure + 2 * bord
            Dim image As New Bitmap(largeur, hauteur, PixelFormat.Format32bppArgb)
            Using g As Graphics = Graphics.FromImage(image)
                g.Clear(Color.FromArgb(0, 0, 0, 0))
                g.SmoothingMode = SmoothingMode.AntiAlias
                g.PixelOffsetMode = PixelOffsetMode.HighQuality
                g.InterpolationMode = InterpolationMode.HighQualityBicubic
                g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
                Using panneau As GraphicsPath = AnnonceEcran.Arrondi(New RectangleF(0, 0, largeur, hauteur), bord * 1.6F),
                      fond As New SolidBrush(Color.FromArgb(165, 12, 12, 14))
                    g.FillPath(fond, panneau)
                End Using
                Dim cadre As New Rectangle(bord, bord + (hInterieure - coteImage) \ 2, coteImage, coteImage)     ' (a la hauteur du texte)
                Using contour As GraphicsPath = AnnonceEcran.Arrondi(cadre, bord * 0.9F)
                    If pochette IsNot Nothing AndAlso source = "radio" AndAlso nomRadio <> "" Then
                        ' Logo de la station : entier (sans rognage), au-dessus du bandeau du nom, sur fond clair.
                        Dim hZone As Single = coteImage * 0.7F
                        Dim marge As Single = coteImage * 0.06F
                        Dim lMax As Single = coteImage - 2 * marge, hMax As Single = hZone - 2 * marge
                        Dim ech As Single = Math.Min(lMax / pochette.Width, hMax / pochette.Height)
                        Dim lw As Single = pochette.Width * ech, lh As Single = pochette.Height * ech
                        g.SetClip(contour)
                        Using fondClair As New SolidBrush(Color.FromArgb(255, 236, 236, 240))
                            g.FillRectangle(fondClair, cadre)
                        End Using
                        g.DrawImage(pochette, New RectangleF(cadre.Left + (coteImage - lw) / 2, cadre.Top + (hZone - lh) / 2, lw, lh))
                        g.ResetClip()
                    ElseIf pochette IsNot Nothing Then
                        Dim c As Integer = Math.Min(pochette.Width, pochette.Height)       ' (le carre du milieu, sans deformer)
                        g.SetClip(contour)
                        g.DrawImage(pochette, cadre, New Rectangle((pochette.Width - c) \ 2, (pochette.Height - c) \ 2, c, c), GraphicsUnit.Pixel)
                        g.ResetClip()
                    Else
                        Using gris As New SolidBrush(Color.FromArgb(255, 58, 58, 62)),
                              note As New Font("Segoe UI Symbol", coteImage * 0.5F, FontStyle.Regular, GraphicsUnit.Pixel),
                              centre As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                            g.FillPath(gris, contour)
                            g.DrawString(ChrW(&H266B), note, Brushes.Gainsboro, cadre, centre)
                        End Using
                    End If
                End Using
                ' Radio : le nom de la station est ecrit dans la photo, sur un bandeau sombre en bas (20/10/2026).
                If source = "radio" AndAlso nomRadio <> "" Then
                    Dim hBande As Single = coteImage * 0.3F
                    Dim bande As New RectangleF(cadre.Left, cadre.Bottom - hBande, cadre.Width, hBande)
                    Using contour As GraphicsPath = AnnonceEcran.Arrondi(cadre, bord * 0.9F),
                          voile As New SolidBrush(Color.FromArgb(235, 0, 0, 0)),
                          police As New Font("Segoe UI Semibold", Math.Max(6.0F, coteImage * 0.12F), FontStyle.Regular, GraphicsUnit.Pixel),
                          fmt As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center,
                                                        .Trimming = StringTrimming.EllipsisWord}
                        g.SetClip(contour)
                        g.IntersectClip(bande)
                        g.FillRectangle(voile, bande)
                        g.ResetClip()
                        g.DrawString(nomRadio, police, Brushes.White, New RectangleF(bande.Left + bord * 0.3F, bande.Top, bande.Width - bord * 0.6F, bande.Height), fmt)
                    End Using
                End If
                DessinerLaSource(g, source, cadre, coteImage, source = "radio" AndAlso nomRadio <> "")
                ' Titre et artiste, centres en hauteur a cote de la pochette.
                Dim xTexte As Single = bord + coteImage + bord
                Dim yTexte As Single = bord + (hInterieure - hTexteTotal) / 2
                g.DrawString(titre, policeTitre, Brushes.White, New RectangleF(xTexte, yTexte, largeurTexte, hTitreM + 1), format)
                Using gris As New SolidBrush(Color.FromArgb(255, 205, 205, 210))
                    g.DrawString(artiste, policeArtiste, gris, New RectangleF(xTexte, yTexte + hTitreM + interligne, largeurTexte, hArtisteM + 1), format)
                    If texteTemps <> "" Then
                        g.DrawString(texteTemps, policeTemps, gris, New RectangleF(xTexte, yTexte + hTitreM + interligne + hArtisteM + interligne * 0.6F, largeurTexte, hTempsM + 2), format)
                    End If
                End Using
                ' Toute petite barre d'avancement du morceau, sous la pochette et le texte.
                If progression >= 0 Then
                    Dim hBarre As Single = Math.Max(2.0F, bord * 0.34F)
                    Dim xBarre As Single = bord
                    Dim lBarre As Single = largeur - 2 * bord
                    Dim yBarre As Single = hauteur - bord * 0.72F - hBarre / 2
                    Using rail As New SolidBrush(Color.FromArgb(120, 255, 255, 255)),
                          rempli As New SolidBrush(Color.FromArgb(255, 255, 255, 255))
                        g.FillRectangle(rail, xBarre, yBarre, lBarre, hBarre)
                        g.FillRectangle(rempli, xBarre, yBarre, Math.Max(1.0F, lBarre * CSng(Math.Min(1.0, progression))), hBarre)
                    End Using
                End If
            End Using
            Return image
        End Using
    End Function

    ''' <summary>
    ''' Petite icone de la SOURCE de la musique, dans le coin bas droit de la pochette (14/10/2026 : "rajoute dans la
    ''' photo de l'album la source de la musique qui passe (Deezer, mp3, radio) sous forme de petite icone") :
    ''' Deezer = ses barres de couleur, mp3 = "MP3", radio = une antenne et ses ondes. Dessinees ici, sans fichier.
    ''' </summary>
    Private Sub DessinerLaSource(g As Graphics, source As String, cadre As Rectangle, cote As Integer, Optional enHaut As Boolean = False)
        Dim s As String = If(source, "").ToLowerInvariant()
        If s <> "deezer" AndAlso s <> "mp3" AndAlso s <> "radio" Then Exit Sub
        Dim d As Single = Math.Max(14.0F, cote * 0.36F)
        Dim marge As Single = Math.Max(2.0F, cote * 0.05F)
        Dim x As Single = cadre.Right - d - marge
        Dim y As Single = If(enHaut, cadre.Top + marge, cadre.Bottom - d - marge)      ' (en haut quand le bas porte le nom de la radio)
        Dim rond As New RectangleF(x, y, d, d)
        Using fond As New SolidBrush(If(s = "mp3", Color.FromArgb(255, 236, 72, 153), Color.FromArgb(225, 18, 18, 22))),
              bord As New Pen(Color.FromArgb(110, 255, 255, 255), Math.Max(1.0F, d * 0.04F))
            g.FillEllipse(fond, rond)
            g.DrawEllipse(bord, rond)
        End Using
        Select Case s
            Case "deezer"
                ' Cinq barres en escalier, aux couleurs de Deezer.
                Dim couleurs As Color() = {Color.FromArgb(64, 177, 73), Color.FromArgb(255, 213, 0), Color.FromArgb(255, 140, 0),
                                           Color.FromArgb(230, 51, 53), Color.FromArgb(160, 60, 200)}
                Dim largeurBarre As Single = d * 0.1F
                Dim ecart As Single = d * 0.035F
                Dim total As Single = 5 * largeurBarre + 4 * ecart
                Dim gauche As Single = x + (d - total) / 2
                Dim bas As Single = y + d * 0.72F
                For i As Integer = 0 To 4
                    Dim h As Single = d * (0.14F + 0.1F * i)
                    Using b As New SolidBrush(couleurs(i))
                        g.FillRectangle(b, gauche + i * (largeurBarre + ecart), bas - h, largeurBarre, h)
                    End Using
                Next
            Case "mp3"
                ' Note de musique blanche sur fond rose, « MP3 » en tout petit dessous (22/10/2026 : modele choisi par Jacob parmi les propositions).
                Using police As New Font("Segoe UI Symbol", d * 0.52F, FontStyle.Bold, GraphicsUnit.Pixel),
                      petite As New Font("Segoe UI", Math.Max(6.6F, d * 0.1265F), FontStyle.Bold, GraphicsUnit.Pixel),
                      centre As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center},
                      fondTexte As New SolidBrush(Color.FromArgb(80, 7, 36))
                    g.DrawString(ChrW(&H266B), police, Brushes.White, New RectangleF(x, y + d * 0.04F, d, d * 0.72F), centre)
                    g.DrawString("MP3", petite, fondTexte, New RectangleF(x, y + d * 0.68F, d, d * 0.2F), centre)
                End Using
            Case "radio"
                Dim cx As Single = x + d / 2
                Dim cy As Single = y + d * 0.68F
                Using blanc As New Pen(Color.White, Math.Max(1.2F, d * 0.07F)) With {.StartCap = Drawing2D.LineCap.Round, .EndCap = Drawing2D.LineCap.Round}
                    g.FillEllipse(Brushes.White, cx - d * 0.06F, cy - d * 0.06F, d * 0.12F, d * 0.12F)
                    For i As Integer = 1 To 3
                        Dim r As Single = d * 0.16F * i
                        g.DrawArc(blanc, cx - r, cy - r, 2 * r, 2 * r, 225.0F, 90.0F)
                    Next
                End Using
        End Select
    End Sub

End Module
