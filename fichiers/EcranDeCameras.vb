Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

''' <summary>
''' Ecran de plusieurs cameras de l'enregistreur (29/10/2026, Jacob : « Ecran 4, 8, 16, 32, 64 cameras »). Les cases sont posees DANS la fenetre principale (Form3) : la souris la deplace et la redimensionne comme
''' pour une video, le double clic la met en plein ecran (filtre de messages de Form3, qui traite les PictureBox comme son image).
'''
''' Chaque case est un VLC a part (vlc.exe de data\VLC, --drawable-hwnd = la case), pas un VlcControl : mesure du 29/10/2026, 32 lecteurs VlcControl dans Slide prenaient 11 a 21 coeurs sur 24 (et OK de Form4
''' ne repondait plus), alors que 32 vlc.exe separes en prennent 0,7. Les processus sont lies a Slide par un « job » Windows : ils s'arretent tous avec lui, meme s'il plante.
'''
''' La fenetre que VLC cree dans la case est DESACTIVEE (EnableWindow) : sinon elle prenait les clics (et la capture de la souris : Windows ne pouvait plus deplacer la fenetre) ; desactivee, les clics tombent
''' sur la case, c'est-a-dire sur Form3. Les cases demarrent une par une : l'enregistreur coupe les connexions qui arrivent toutes ensemble.
''' </summary>
Friend Class EcranDeCameras

    Private ReadOnly _hote As Form3
    Private ReadOnly _cases As New List(Of PictureBox)
    Private ReadOnly _adresses As New List(Of String)
    Private ReadOnly _procParCase As New List(Of Process)          ' (le VLC en cours de chaque case)
    Private ReadOnly _aTuer As New List(Of Tuple(Of Process, Long))          ' (anciens VLC remplaces : arretes un peu apres le nouveau, pour que l'image ne disparaisse pas)
    Private ReadOnly _desactivee As New List(Of Boolean)          ' (la fenetre VLC de la case est-elle desactivee ?)
    Private ReadOnly _aRelancer As New List(Of Boolean)           ' (la case a ete recreee par Windows : VLC doit se reposer dedans)
    Private _recreation As Boolean
    Private _canaux As List(Of Integer)                           ' (numero de camera de chaque case)
    Private _agrandie As PictureBox                               ' (Ctrl + double clic : une camera en grand, flux principal, par-dessus la mosaique)
    Private _procAgrandie As Process
    Private _caseAgrandie As Integer = -1                         ' (cameras Blink : la case agrandie par Ctrl + double clic, -1 = aucune)
    Private _indexAgrandi As Integer = -1                         ' (camera de l'enregistreur agrandie par Ctrl + double clic : son numero d'ordre dans l'ecran, -1 = aucune)
    Private WithEvents _suiviAgrandie As New System.Windows.Forms.Timer With {.Interval = 200}
    Private _agrandiePrete As Boolean                              ' (le flux principal de la camera agrandie a sa premiere image : il est passe devant la case)
    Private _vueLe As Long
    Private _noms As List(Of String)                              ' (nom affiche en bas a droite de chaque case)
    Private _fenetreNoms As FormNomsCameras
    Private _imageNoms As Bitmap
    Private _pleinEcranDesNoms As Boolean                          ' (mode plein ecran dans lequel la fenetre des noms a ete posee)
    Private _aDesDonnees As Func(Of Integer, Boolean)              ' (cameras Blink : la case recoit-elle de la video ? Nothing = pas de rangement)
    Private ReadOnly _position As New List(Of Integer)            ' (place de chaque case dans la grille : l'ordre des cameras affichees)
    Private ReadOnly _noir As New List(Of Boolean)                ' (cameras Blink sans image : leur case est masquee et la grille se fait sans elles)
    Private ReadOnly _depart As New List(Of Long)                 ' (heure de demarrage du lecteur de chaque case)
    Private _colonnes As Integer
    Private _lignes As Integer
    Private ReadOnly _nombre As Integer
    Private WithEvents _minuteur As New System.Windows.Forms.Timer With {.Interval = 250}
    Private _tailleChangeeLe As Long
    Private _derniereTaille As Size                ' (taille de la fenetre au dernier rangement des cases)
    Private _ratioDesFlux As Double                ' (format des cases quand les flux ont ete lances)
    Private _fond As PictureBox
    Private _suivante As Integer
    Private _fini As Boolean
    Private _tics As Integer

    ''' <summary>Un ecran des cameras <paramref name="canaux"/> de l'enregistreur (numeros de 1 a 64, dans cet ordre).</summary>
    Friend Sub New(hote As Form3, canaux As List(Of Integer))
        Me.New(hote, canaux.Select(Function(c) NvrIp.AdresseDeLaCase(c)).ToList(), canaux)
    End Sub

    ''' <summary>Un ecran de cameras lues a des adresses quelconques (cameras Blink : les adresses locales des relais).</summary>
    Friend Sub New(hote As Form3, adresses As List(Of String))
        Me.New(hote, adresses, DirectCast(Nothing, List(Of Integer)))
    End Sub

    ''' <summary>Cameras Blink (29/10/2026) : <paramref name="aDesDonnees"/> dit si la camera de la case recoit de la video ; celles qui n'en ont toujours pas 20 s apres leur demarrage (ecran noir) sont rangees
    ''' a la fin de la grille.</summary>
    Friend Sub New(hote As Form3, adresses As List(Of String), aDesDonnees As Func(Of Integer, Boolean))
        Me.New(hote, adresses, DirectCast(Nothing, List(Of Integer)))
        _aDesDonnees = aDesDonnees
        ReclasserLesCases()          ' (les cameras annoncees hors ligne -- adresse vide -- sont a la fin des le depart)
    End Sub

    Private Sub New(hote As Form3, adresses As List(Of String), canaux As List(Of Integer))
        _hote = hote
        Dim nombre As Integer = adresses.Count
        _nombre = nombre
        _canaux = If(canaux IsNot Nothing, canaux.ToList(), Nothing)          ' (Nothing : cameras Blink, qui n'ont pas de numero de canal de l'enregistreur)
        If canaux IsNot Nothing Then _noms = canaux.Select(Function(c) "Caméra " & c).ToList()          ' (l'enregistreur ne donne pas de nom a ses cameras par RTSP : leur numero)
        _hote.EcranCamerasActif = Me
        NvrIp.GrilleDe(nombre, _colonnes, _lignes)
        _colonnes = Math.Max(1, _colonnes)
        _lignes = Math.Max(1, _lignes)
        ' Fond noir derriere les cases : les traits entre elles sont noirs.
        _fond = New PictureBox With {.BackColor = Color.Black, .Dock = DockStyle.Fill}
        _hote.Controls.Add(_fond)
        _fond.BringToFront()
        _hote.DeplacementManuel = True          ' (Form3 suit la souris elle-meme : le deplacement de Windows perdait la souris quand un VLC creait sa fenetre)
        For i As Integer = 0 To nombre - 1
            Dim c As New PictureBox With {.BackColor = Color.Black}
            _hote.Controls.Add(c)
            c.BringToFront()
            Dim h As IntPtr = c.Handle                  ' (la case doit exister avant que VLC s'y pose)
            _cases.Add(c)
            _adresses.Add(adresses(i))
            _position.Add(i)
            _noir.Add(False)
            _depart.Add(0)
            _desactivee.Add(False)
            _aRelancer.Add(False)
            _procParCase.Add(Nothing)
            ' Windows recree les cases (et detruit la fenetre ou VLC dessinait) quand la fenetre principale change de style, au plein ecran : VLC doit alors se reposer dans la case recreee.
            Dim numero As Integer = i
            AddHandler c.HandleCreated, Sub(s As Object, e As EventArgs) _aRelancer(numero) = True
        Next
        Replacer()
        _ratioDesFlux = RatioDeCase()
        AddHandler _hote.ClientSizeChanged, AddressOf HoteRedimensionnee
        AddHandler _hote.LocationChanged, AddressOf HoteDeplacee          ' (la fenetre des noms suit la fenetre principale quand on la deplace)
    End Sub

    Private Sub HoteRedimensionnee(sender As Object, e As EventArgs)
        If _fini Then Exit Sub
        Try
            Replacer()
            ' VLC garde le format de la case a son demarrage : les flux repartent une fois la fenetre vraiment redimensionnee (verifie par le tic de la seconde).
            ' (Pas de minuteur arrete et relance a chaque pixel : cela detruisait et recreait une fenetre Windows a chaque fois, et Slide s'est ferme sur « Erreur lors de la creation d'un handle de fenetre », 29/10/2026.)
            MarquerSiLesFluxDoiventRepartir()
        Catch ex As Exception
            DiagLog("Camera IP : redimensionnement : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Range les cases dans la fenetre principale (1 pixel de noir autour de chacune).</summary>
    Private Sub Replacer()
        ' La grille remplit toute la fenetre, sans bande noire (29/10/2026, Jacob : « il faut eviter les bandes noires et adapter l'affichage ») : on cherche le nombre de colonnes et de lignes dont
        ' les cases sont les plus proches du format 16/9 (celui des cameras) pour la fenetre actuelle ; s'il y a plus de cases que de cameras, les dernieres restent noires (moins d'une ligne).
        Dim l As Integer = Math.Max(1, _hote.ClientSize.Width), h As Integer = Math.Max(1, _hote.ClientSize.Height)
        _derniereTaille = _hote.ClientSize
        ' Cameras Blink non connectees (ecran noir) : leur case disparait et la grille est calculee pour les seules cameras affichees, qui se partagent donc toute la place (29/10/2026).
        Dim affichees As List(Of Integer) = Enumerable.Range(0, _cases.Count).Where(Function(k) Not _noir(k)).ToList()
        Dim nbAffichees As Integer = Math.Max(1, affichees.Count)
        Dim meilleurScore As Double = Double.MaxValue
        For c As Integer = 1 To nbAffichees
            Dim r As Integer = CInt(Math.Ceiling(nbAffichees / CDbl(c)))
            Dim vides As Integer = c * r - nbAffichees
            If vides >= c Then Continue For                     ' (une ligne entiere de trop)
            Dim ratioCase As Double = (l / CDbl(c)) / (h / CDbl(r))
            Dim score As Double = Math.Abs(Math.Log(ratioCase / (16.0 / 9.0))) + 0.02 * vides
            If score < meilleurScore Then
                meilleurScore = score
                _colonnes = c
                _lignes = r
            End If
        Next
        For i As Integer = 0 To _cases.Count - 1
            If _noir(i) Then
                If _cases(i).Visible Then _cases(i).Visible = False
                Continue For
            End If
            Dim place As Integer = affichees.IndexOf(i)
            _position(i) = place
            If Not _cases(i).Visible Then _cases(i).Visible = True
            Dim col As Integer = place Mod _colonnes, lig As Integer = place \ _colonnes
            Dim x0 As Integer = col * l \ _colonnes, x1 As Integer = (col + 1) * l \ _colonnes
            Dim y0 As Integer = lig * h \ _lignes, y1 As Integer = (lig + 1) * h \ _lignes
            _cases(i).SetBounds(x0 + 1, y0 + 1, Math.Max(1, x1 - x0 - 2), Math.Max(1, y1 - y0 - 2))
        Next
        If _agrandie IsNot Nothing Then _agrandie.SetBounds(0, 0, l, h)          ' (la camera agrandie suit la fenetre)
        If _caseAgrandie >= 0 AndAlso _caseAgrandie < _cases.Count Then          ' (cameras Blink : la case agrandie couvre toute la fenetre)
            _cases(_caseAgrandie).SetBounds(0, 0, l, h)
            _cases(_caseAgrandie).BringToFront()
        End If
        MettreAJourLesNoms()          ' (les noms suivent les cases)
    End Sub

    ''' <summary>Les noms des cameras (cameras Blink : leur nom dans l'application), dans l'ordre des cases.</summary>
    Friend Sub DefinirLesNoms(noms As IEnumerable(Of String))
        _noms = noms.ToList()
        MettreAJourLesNoms()
    End Sub

    ''' <summary>Dessine les noms en bas a droite de chaque case visible (ou de la camera agrandie) sur la fenetre calque, qui couvre la zone cliente de Form3. Texte blanc petit, avec une ombre pour rester lisible sur toute image.</summary>
    Private Sub MettreAJourLesNoms()
        Try
            If _fini OrElse _noms Is Nothing OrElse Not _hote.IsHandleCreated Then Exit Sub
            Dim l As Integer = _hote.ClientSize.Width, h As Integer = _hote.ClientSize.Height
            If l < 60 OrElse h < 60 Then Exit Sub
            Dim image As New Bitmap(l, h, Imaging.PixelFormat.Format32bppArgb)
            Using g As Graphics = Graphics.FromImage(image)
                g.TextRenderingHint = Drawing.Text.TextRenderingHint.AntiAlias
                Dim nomDe As Func(Of Integer, String) = Function(k As Integer) If(k >= 0 AndAlso k < _noms.Count, _noms(k), "")
                If _caseAgrandie >= 0 Then
                    DessinerUnNom(g, New Rectangle(0, 0, l, h), nomDe(_caseAgrandie))
                ElseIf _agrandie IsNot Nothing AndAlso _indexAgrandi >= 0 Then
                    DessinerUnNom(g, New Rectangle(0, 0, l, h), nomDe(_indexAgrandi))
                Else
                    For i As Integer = 0 To _cases.Count - 1
                        If _cases(i).Visible Then DessinerUnNom(g, _cases(i).Bounds, nomDe(i))
                    Next
                End If
            End Using
            Dim ancienne As Bitmap = _imageNoms
            _imageNoms = image
            ancienne?.Dispose()
            AppliquerLesNoms()
        Catch ex As Exception
            DiagLog("Camera IP : noms des cameras : " & ex.Message)
        End Try
    End Sub

    Private Shared Sub DessinerUnNom(g As Graphics, zone As Rectangle, nom As String)
        If nom = "" Then Exit Sub
        Dim taille As Single = CSng(Math.Max(10.0, Math.Min(22.0, zone.Height * 0.075)))
        Using police As New Font("Segoe UI Semibold", taille, FontStyle.Regular, GraphicsUnit.Pixel)
            Dim texte As String = nom
            Dim mesure As SizeF = g.MeasureString(texte, police)
            Do While mesure.Width > zone.Width * 0.9F AndAlso texte.Length > 3
                texte = texte.Substring(0, texte.Length - 2).TrimEnd() & "…"
                mesure = g.MeasureString(texte, police)
            Loop
            Dim x As Single = zone.Right - mesure.Width - 6
            Dim y As Single = zone.Bottom - mesure.Height - 3
            Using ombre As New SolidBrush(Color.FromArgb(190, 0, 0, 0)), encre As New SolidBrush(Color.FromArgb(235, 255, 255, 255))
                g.DrawString(texte, police, ombre, x + 1, y + 1)
                g.DrawString(texte, police, encre, x, y)
            End Using
        End Using
    End Sub

    ''' <summary>Pose la fenetre calque sur la zone cliente de Form3 (a sa place actuelle) avec l'image des noms ; au premier plan comme la montre : au-dessus de tout en plein ecran, rattachee a Form3 en fenetre.</summary>
    Private Sub AppliquerLesNoms()
        If _fini OrElse _imageNoms Is Nothing Then Exit Sub
        Try
            If _fenetreNoms Is Nothing OrElse _fenetreNoms.IsDisposed Then
                _fenetreNoms = New FormNomsCameras()
                _fenetreNoms.Show()
            End If
            _fenetreNoms.Bounds = New Rectangle(_hote.PointToScreen(Point.Empty), _imageNoms.Size)
            _pleinEcranDesNoms = FullScreen
            CalqueWin32.DefinirPremierPlan(_fenetreNoms, _hote, FullScreen)
            CalqueWin32.Appliquer(_fenetreNoms, _imageNoms)
            AjusterLePremierPlanDesNoms()
        Catch ex As Exception
            DiagLog("Camera IP : fenetre des noms : " & ex.Message)
        End Try
    End Sub

    ''' <summary>En plein ecran les noms sont au premier plan, sauf quand une fenetre ordinaire de Slide (reglages, boite « Ouvrir ») est active : ils passent alors derriere elle, comme la montre et les PIP.</summary>
    Private Sub AjusterLePremierPlanDesNoms()
        If _fini OrElse _fenetreNoms Is Nothing OrElse _fenetreNoms.IsDisposed Then Exit Sub
        ' Le mode (plein ecran / fenetre) a change depuis que les noms ont ete poses : pendant la bascule, FullScreen n'etait pas encore a jour et les noms restaient au premier plan, au-dessus de tout
        ' (29/10/2026) ; ils sont reposes (premier plan ou rattaches a la fenetre) des que le mode est connu.
        If _pleinEcranDesNoms <> FullScreen Then
            AppliquerLesNoms()
            Exit Sub
        End If
        If Not FullScreen Then Exit Sub
        Try
            If CalqueWin32.SlideAuPremierPlan() AndAlso Not CalqueWin32.ImageDeSlideActive() Then
                CalqueWin32.PlacerAuDessusDeSlide(_fenetreNoms)
            Else
                CalqueWin32.ReaffirmerPremierPlan(_fenetreNoms)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub HoteDeplacee(sender As Object, e As EventArgs)
        If Not _fini Then AppliquerLesNoms()
    End Sub

    ''' <summary>Format (largeur / hauteur) d'une case avec la fenetre et la disposition actuelles.</summary>
    Private Function RatioDeCase() As Double
        Return (Math.Max(1, _hote.ClientSize.Width) / CDbl(_colonnes)) / (Math.Max(1, _hote.ClientSize.Height) / CDbl(_lignes))
    End Function

    ''' <summary>Les flux ne repartent que si le FORMAT des cases a vraiment change depuis leur lancement (plus de 12 %) : agrandir ou reduire la fenetre en gardant ses proportions ne change rien, VLC remet
    ''' l'image a l'echelle de sa case ; un deplacement fait parfois varier la taille d'un pixel (29/10/2026 : relancait les 64 cameras).</summary>
    Private Sub MarquerSiLesFluxDoiventRepartir()
        ' Plus de relance des flux au redimensionnement (29/10/2026) : chaque VLC garde le format 16/9 fixe (--aspect-ratio=16:9), au pire une fine bande noire dans la case. Relancer les 64 flux
        ' effacait les images, et ancien + nouveau lecteur ensemble depassaient le nombre de connexions que l'enregistreur accepte (36 cameras seulement au retour du plein ecran).
    End Sub

    ''' <summary>Le format des cases a change : les flux repartent un par un au nouveau format. L'ancien VLC d'une case n'est arrete que quelques secondes apres le demarrage du nouveau : l'image ne disparait pas.</summary>
    Private Sub Relancer()
        _ratioDesFlux = RatioDeCase()
        _suivante = 0
        _tics = 0
        _minuteur.Interval = 250
    End Sub

    Friend Sub Demarrer()
        InstallerLeCrochet()
        _minuteur.Start()
    End Sub

    ''' <summary>Options de decodage plus legeres quand il y a beaucoup de cases : un seul fil de decodage par case, filtre de lissage coupe, decodage rapide ; a partir de 32 cases les images
    ''' secondaires sont ignorees, a 64 on ne decode plus que les images completes.</summary>
    Private Shared Function OptionsAllegees(nombre As Integer) As String()
        If nombre < 16 Then Return Array.Empty(Of String)()
        Dim o As New List(Of String) From {"--avcodec-threads=1", "--avcodec-skiploopfilter=4", "--avcodec-fast"}
        If nombre >= 24 AndAlso nombre < 48 Then o.Add("--avcodec-skip-frame=1")
        If nombre >= 48 Then o.Add("--avcodec-skip-frame=3")
        Return o.ToArray()
    End Function

    ''' <summary>A chaque tic : la fenetre VLC des cases deja lancees est desactivee des qu'elle existe, puis une nouvelle case demarre.</summary>
    Private Sub _minuteur_Tick(sender As Object, e As EventArgs) Handles _minuteur.Tick
        If _fini Then
            _minuteur.Stop()
            Exit Sub
        End If
        ' Filet de securite : au retour du plein ecran, l'evenement de taille arrivait avant la taille finale et les cases restaient aux positions du plein ecran (capture du 29/10/2026).
        ' Une case qui depasse de la fenetre (grille restee a la taille du plein ecran : capture du 29/10/2026, 36 cameras visibles sur 64) : tout est range a nouveau.
        Dim dehors As Boolean = False
        Dim zoneClient As Size = _hote.ClientSize
        For Each t As PictureBox In _cases
            If t.Right > zoneClient.Width + 2 OrElse t.Bottom > zoneClient.Height + 2 Then
                dehors = True
                Exit For
            End If
        Next
        If dehors OrElse zoneClient <> _derniereTaille Then
            Try
                Replacer()
                DiagLog("Camera IP : cases rangees pour une fenetre de " & zoneClient.Width & "x" & zoneClient.Height & " : " & _colonnes & " colonnes x " & _lignes & " lignes" & If(dehors, " (des cases depassaient)", ""))
            Catch ex As Exception
                DiagLog("Camera IP : rangement des cases : " & ex.Message)
            End Try
            MarquerSiLesFluxDoiventRepartir()
        End If
        If _tailleChangeeLe <> 0 AndAlso Environment.TickCount64 - _tailleChangeeLe > 900 Then
            _tailleChangeeLe = 0
            Relancer()
        End If
        PurgerLesAnciens(False)
        _tics += 1
        If _aDesDonnees IsNot Nothing AndAlso _tics Mod 3 = 0 Then ReclasserLesCases()
        AjusterLePremierPlanDesNoms()
        RectifierLesFenetres()
        ' VLC pose ses fenetres dans la case en plusieurs fois (une fois la connexion faite, parfois plusieurs secondes apres le demarrage), et en recree quand le flux repart : toutes sont desactivees a
        ' chaque tic, pendant toute la duree de l'ecran (EnableWindow sur une fenetre deja desactivee ne coute rien).
        For i As Integer = 0 To Math.Min(_suivante, _cases.Count) - 1
            If _adresses(i) <> "" Then DesactiverLesFenetresDe(_cases(i))
        Next
        If _agrandie IsNot Nothing Then DesactiverLesFenetresDe(_agrandie)
        ' Pas de nouveau lecteur tant qu'un bouton de la souris est enfonce (deplacement ou redimensionnement de la fenetre en cours : le demarrage d'un VLC la ralentissait).
        If Control.MouseButtons <> MouseButtons.None Then Exit Sub
        If _suivante < _cases.Count Then
            Dim premier As Integer = _suivante
            _suivante += 1
            Lancer(premier)
            Exit Sub
        End If
        ' Tout est lance. Les cases recreees par Windows (plein ecran) sont relancees une par une : l'ancien VLC est arrete AVANT le nouveau (jamais plus de connexions que de cameras a l'enregistreur).
        For k As Integer = 0 To _aRelancer.Count - 1
            If _aRelancer(k) Then
                _aRelancer(k) = False
                If Not _recreation Then
                    _recreation = True
                    DiagLog("Camera IP : cases recreees par Windows (plein ecran) : les lecteurs se reposent un par un")
                End If
                Lancer(k)
                Exit Sub
            End If
        Next
        _recreation = False
        ' Plus rien a lancer : le tic passe a une seconde.
        If _minuteur.Interval <> 1000 Then
            _minuteur.Interval = 1000
            Try
                If Not _fini Then _hote.Focus()          ' (Echap et le clavier restent a la fenetre principale)
            Catch ex As Exception
            End Try
        End If
    End Sub

    ''' <summary>Cameras Blink : les cases dont la camera est annoncee hors ligne (adresse vide), ou n'envoie toujours rien 10 s apres le demarrage de son lecteur (ecran noir), sont MASQUEES : la grille est calculee
    ''' pour les seules cameras affichees (elles se partagent la place), dans leur ordre. Une camera qui finit par envoyer de la video revient a sa place.</summary>
    Private Sub ReclasserLesCases()
        Try
            Dim maintenant As Long = Environment.TickCount64
            Dim change As Boolean = False
            Dim nbNoirs As Integer = 0
            For i As Integer = 0 To _cases.Count - 1
                Dim noir As Boolean = (_depart(i) > 0 AndAlso maintenant - _depart(i) > 10000 AndAlso Not _aDesDonnees(i)) OrElse _adresses(i) = ""
                If noir Then nbNoirs += 1
                If _noir(i) <> noir Then
                    _noir(i) = noir
                    change = True
                End If
            Next
            If change Then
                Replacer()
                DiagLog("Camera Blink : " & nbNoirs & " camera(s) sans image retiree(s) de l'ecran, les autres se partagent la place (" & _colonnes & " colonnes x " & _lignes & " lignes)")
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Lance (ou relance) le VLC de la case <paramref name="i"/> ; l'ancien, s'il existe, est arrete d'abord.</summary>
    Private Sub Lancer(i As Integer)
        Dim c As PictureBox = _cases(i)
        Dim adresse As String = _adresses(i)
        Try
            If _procParCase(i) IsNot Nothing Then
                Tuer(New List(Of Process) From {_procParCase(i)})
                _procParCase(i) = Nothing
            End If
            _depart(i) = Environment.TickCount64
            If adresse = "" Then Exit Sub
            Dim psi As New ProcessStartInfo(IO.Path.Combine(VLCpath, "vlc.exe")) With {.UseShellExecute = False, .CreateNoWindow = True, .WorkingDirectory = VLCpath}
            ' vlc.exe n'est pas declare sensible a l'echelle d'affichage (ecran a 154 dpi) : Windows lui donne des tailles virtualisees et, quand il suit sa case apres un changement de taille de la fenetre (plein ecran,
            ' redimensionnement), ses images prenaient 1,6 fois la taille des cases (25 cameras visibles sur 64, 29/10/2026). Meme sensibilite que Slide : les tailles coincident.
            psi.Environment("__COMPAT_LAYER") = "HIGHDPIAWARE"
            ' Un relais Blink local (http://127.0.0.1:...) arrive par a-coups : plus de memoire tampon que pour l'enregistreur.
            Dim cache As String = If(adresse.StartsWith("http://127.0.0.1", StringComparison.OrdinalIgnoreCase), "--network-caching=1500", "--network-caching=500")
            For Each a As String In New String() {"-I", "dummy", "--no-audio", "--rtsp-tcp", cache, "--no-video-title-show", "--no-osd", "--no-mouse-events", "--no-keyboard-events",
                                                  "--no-snapshot-preview", "--drawable-hwnd=" & c.Handle.ToInt64(), "--aspect-ratio=16:9"}
                psi.ArgumentList.Add(a)
            Next
            For Each a As String In OptionsAllegees(_nombre)
                psi.ArgumentList.Add(a)
            Next
            psi.ArgumentList.Add(adresse)
            Dim p As Process = Process.Start(psi)
            If p IsNot Nothing Then
                LierAuJob(p)
                ' Priorite reduite : 64 VLC qui chargent leurs modules en meme temps saturaient le processeur et la fenetre de Slide ne suivait plus la souris (29/10/2026).
                Try
                    p.PriorityClass = ProcessPriorityClass.BelowNormal
                Catch ex As Exception
                End Try
                _procParCase(i) = p
            End If
        Catch ex As Exception
            DiagLog("Camera IP : case " & (i + 1) & " : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Arrete les anciens VLC dont l'heure est venue (ou tous si <paramref name="tous"/>).</summary>
    Private Sub PurgerLesAnciens(tous As Boolean)
        Dim aTuer As New List(Of Process)
        For k As Integer = _aTuer.Count - 1 To 0 Step -1
            If tous OrElse Environment.TickCount64 >= _aTuer(k).Item2 Then
                aTuer.Add(_aTuer(k).Item1)
                _aTuer.RemoveAt(k)
            End If
        Next
        Tuer(aTuer)
    End Sub

    Private Sub ArreterLesLecteurs()
        Dim aTuer As New List(Of Process)
        For i As Integer = 0 To _procParCase.Count - 1
            If _procParCase(i) IsNot Nothing Then aTuer.Add(_procParCase(i))
            _procParCase(i) = Nothing
        Next
        For Each t As Tuple(Of Process, Long) In _aTuer
            aTuer.Add(t.Item1)
        Next
        _aTuer.Clear()
        Tuer(aTuer)
    End Sub

    Private Shared Sub Tuer(aTuer As List(Of Process))
        If aTuer.Count = 0 Then Exit Sub
        Task.Run(Sub()
                     For Each p As Process In aTuer
                         Try
                             p.Kill()
                         Catch ex As Exception
                         End Try
                         Try
                             p.Dispose()
                         Catch ex As Exception
                         End Try
                     Next
                 End Sub)
    End Sub

    ''' <summary>Ctrl + double clic (Form3.ClicSurEcranDeCameras) : agrandit la camera sous <paramref name="pointEcran"/> sur toute la fenetre, avec son flux principal ; un nouveau Ctrl + double clic revient a la mosaique
    ''' (les autres cameras n'ont pas ete arretees : le retour est immediat).</summary>
    Friend Sub AgrandirOuRevenir(pointEcran As Point)
        If _fini Then Exit Sub
        If _agrandie IsNot Nothing OrElse _caseAgrandie >= 0 Then
            RevenirALaMosaique()
            Exit Sub
        End If
        Dim numero As Integer = -1
        For i As Integer = 0 To _cases.Count - 1
            If _cases(i).Visible AndAlso _cases(i).RectangleToScreen(_cases(i).ClientRectangle).Contains(pointEcran) Then
                numero = i
                Exit For
            End If
        Next
        If numero < 0 OrElse _adresses(numero) = "" Then Exit Sub
        If _canaux Is Nothing Then
            ' Cameras Blink (29/10/2026) : pas de flux principal a part (un seul direct par camera) : c'est la CASE qui prend toute la fenetre, avec le meme flux ; les autres restent derriere et le retour est immediat.
            _caseAgrandie = numero
            Replacer()
            _hote.Focus()
            Exit Sub
        End If
        Try
            ' D'abord la CASE prend toute la fenetre, avec son flux leger deja affiche (instantane) ; le lecteur du flux principal demarre derriere et ne passe devant que lorsque sa premiere image est prete :
            ' plus d'ecran noir de plusieurs secondes (29/10/2026).
            _caseAgrandie = numero
            _agrandiePrete = False
            _vueLe = 0
            Replacer()
            Dim grand As New PictureBox With {.BackColor = Color.Black}
            _hote.Controls.Add(grand)
            grand.SetBounds(0, 0, _hote.ClientSize.Width, _hote.ClientSize.Height)
            Dim h As IntPtr = grand.Handle
            _agrandie = grand
            _indexAgrandi = numero
            _suiviAgrandie.Start()
            Dim psi As New ProcessStartInfo(IO.Path.Combine(VLCpath, "vlc.exe")) With {.UseShellExecute = False, .CreateNoWindow = True, .WorkingDirectory = VLCpath}
            psi.Environment("__COMPAT_LAYER") = "HIGHDPIAWARE"
            For Each a As String In New String() {"-I", "dummy", "--no-audio", "--rtsp-tcp", "--network-caching=500", "--no-video-title-show", "--no-osd", "--no-mouse-events", "--no-keyboard-events",
                                                  "--no-snapshot-preview", "--drawable-hwnd=" & h.ToInt64(), NvrIp.Resoudre(NvrIp.AdresseDuCanal(_canaux(numero), 1))}          ' (pas de format force : l'image garde ses proportions, du noir sur les cotes si l'ecran est plus large)
                psi.ArgumentList.Add(a)
            Next
            _procAgrandie = Process.Start(psi)
            If _procAgrandie IsNot Nothing Then LierAuJob(_procAgrandie)
            _hote.Focus()
        Catch ex As Exception
            DiagLog("Camera IP : agrandissement d'une camera : " & ex.Message)
            RevenirALaMosaique()
        End Try
    End Sub

    ''' <summary>Attend la premiere image du flux principal de la camera agrandie (sa fenetre VLC apparait dans l'ecran cache), puis la fait passer devant la case.</summary>
    Private Sub _suiviAgrandie_Tick(sender As Object, e As EventArgs) Handles _suiviAgrandie.Tick
        Try
            If _fini OrElse _agrandie Is Nothing OrElse _agrandiePrete Then
                _suiviAgrandie.Stop()
                Exit Sub
            End If
            DesactiverLesFenetresDe(_agrandie)
            If GetWindow(_agrandie.Handle, 5) <> IntPtr.Zero Then
                If _vueLe = 0 Then _vueLe = Environment.TickCount64
                If Environment.TickCount64 - _vueLe > 700 Then          ' (la fenetre existe : l'image arrive dans la foulee)
                    _agrandie.BringToFront()
                    _agrandiePrete = True
                    _suiviAgrandie.Stop()
                    MettreAJourLesNoms()
                End If
            End If
        Catch ex As Exception
            _suiviAgrandie.Stop()
        End Try
    End Sub

    Private Sub RevenirALaMosaique()
        _suiviAgrandie.Stop()
        If _caseAgrandie >= 0 Then
            _caseAgrandie = -1
            Try
                Replacer()
            Catch ex As Exception
            End Try
        End If
        Dim p As Process = _procAgrandie
        _procAgrandie = Nothing
        _indexAgrandi = -1
        If p IsNot Nothing Then Tuer(New List(Of Process) From {p})
        If _agrandie IsNot Nothing Then
            Try
                _hote.Controls.Remove(_agrandie)
                _agrandie.Dispose()
            Catch ex As Exception
            End Try
            _agrandie = Nothing
        End If
        Try
            _hote.Focus()
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Fin de l'ecran : lecteurs arretes, cases retirees de la fenetre.</summary>
    Friend Sub Terminer()
        If _fini Then Exit Sub
        _fini = True
        _minuteur.Stop()
        _hote.DeplacementManuel = False
        _hote.EcranCamerasActif = Nothing
        RevenirALaMosaique()
        RetirerLeCrochet()
        RemoveHandler _hote.ClientSizeChanged, AddressOf HoteRedimensionnee
        RemoveHandler _hote.LocationChanged, AddressOf HoteDeplacee
        Try
            If _fenetreNoms IsNot Nothing Then _fenetreNoms.Close()
            _fenetreNoms = Nothing
            _imageNoms?.Dispose()
            _imageNoms = Nothing
        Catch ex As Exception
        End Try
        ArreterLesLecteurs()
        For Each c As PictureBox In _cases
            Try
                _hote.Controls.Remove(c)
                c.Dispose()
            Catch ex As Exception
            End Try
        Next
        Try
            _hote.Controls.Remove(_fond)
            _fond.Dispose()
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Fenetres VLC desactivees : la souris traverse jusqu'a la case (WindowFromPoint ignore les fenetres desactivees)
    ' ------------------------------------------------------------------------------------------

    Private Delegate Function ProcedureEnumeration(fenetre As IntPtr, parametre As IntPtr) As Boolean

    <DllImport("user32.dll")>
    Private Shared Function EnumChildWindows(parent As IntPtr, proc As ProcedureEnumeration, parametre As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function EnableWindow(fenetre As IntPtr, active As Boolean) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function WindowFromPoint(p As Point) As IntPtr
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Souris : la fenetre VLC d'une case est desactivee (sinon elle prend les clics et la capture) ; Windows ne rend alors le clic a personne. Un crochet de souris (le temps de l'ecran) le voit quand meme
    ' et le donne a Form3 (ClicSurEcranDeCameras), seulement s'il tombe sur la fenetre principale a un endroit qui n'est pas deja un controle de Slide (cases noires, fond : son filtre s'en charge).
    ' ------------------------------------------------------------------------------------------

    Private Const WH_MOUSE_LL As Integer = 14
    Private Const WM_LBUTTONDOWN As Integer = &H201
    Private Const WM_RBUTTONUP As Integer = &H205
    Private Const GA_ROOT As Integer = 2

    Private Delegate Function ProcedureCrochet(code As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr

    <StructLayout(LayoutKind.Sequential)>
    Private Structure DonneesSouris
        Public Point As Point
        Public Donnees As UInteger
        Public Drapeaux As UInteger
        Public Heure As UInteger
        Public Extra As IntPtr
    End Structure

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowsHookEx(id As Integer, proc As ProcedureCrochet, module_ As IntPtr, fil As UInteger) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function UnhookWindowsHookEx(crochet As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function CallNextHookEx(crochet As IntPtr, code As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetAncestor(fenetre As IntPtr, drapeaux As Integer) As IntPtr
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function GetModuleHandle(nom As String) As IntPtr
    End Function

    Private _crochet As IntPtr = IntPtr.Zero
    Private ReadOnly _procedure As ProcedureCrochet = AddressOf CrochetSouris          ' (garde vivante : sinon le ramasse-miettes la libere)

    Private Sub InstallerLeCrochet()
        Try
            _crochet = SetWindowsHookEx(WH_MOUSE_LL, _procedure, GetModuleHandle(Nothing), 0UI)
            If _crochet = IntPtr.Zero Then DiagLog("Camera IP : crochet de souris impossible (erreur " & Marshal.GetLastWin32Error() & ")")
        Catch ex As Exception
            DiagLog("Camera IP : crochet de souris : " & ex.Message)
        End Try
    End Sub

    Private Sub RetirerLeCrochet()
        Try
            If _crochet <> IntPtr.Zero Then UnhookWindowsHookEx(_crochet)
        Catch ex As Exception
        End Try
        _crochet = IntPtr.Zero
    End Sub

    <DllImport("user32.dll")>
    Private Shared Function GetWindow(fenetre As IntPtr, commande As Integer) As IntPtr
    End Function

    ''' <summary>Le clic tombe-t-il sur une image de camera ? Windows ignore les clics sur la fenetre desactivee de VLC et WindowFromPoint rend alors la case de Slide en dessous : c'est donc une image si la fenetre
    ''' est etrangere, ou si c'est une case qui contient une fenetre VLC. Une case noire (sans VLC) ou le fond ne sont pas concernes : le filtre de Form3 les recoit.</summary>
    Private Function ClicSurUneImage(h As IntPtr) As Boolean
        Dim c As Control = Control.FromHandle(h)
        If c Is Nothing Then Return True
        Dim tuile As PictureBox = TryCast(c, PictureBox)
        If tuile IsNot Nothing AndAlso (tuile Is _agrandie OrElse _cases.Contains(tuile)) Then Return GetWindow(tuile.Handle, 5) <> IntPtr.Zero          ' (5 = GW_CHILD)
        Return False
    End Function

    Private Function CrochetSouris(code As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
        Try
            Dim msg As Integer = wParam.ToInt32()
            If code >= 0 AndAlso Not _fini AndAlso (msg = WM_LBUTTONDOWN OrElse msg = WM_RBUTTONUP) Then
                Dim d As DonneesSouris = Marshal.PtrToStructure(Of DonneesSouris)(lParam)
                Dim h As IntPtr = WindowFromPoint(d.Point)
                ' La fenetre principale, a un endroit qui n'est pas un controle de Slide (donc une fenetre de VLC) : c'est un clic sur une image de camera.
                If h <> IntPtr.Zero AndAlso GetAncestor(h, GA_ROOT) = _hote.Handle AndAlso ClicSurUneImage(h) Then
                    Dim pt As Point = d.Point
                    Dim droit As Boolean = (msg = WM_RBUTTONUP)
                    _hote.BeginInvoke(New Action(Sub()
                                                     Try
                                                         If _fini Then Exit Sub
                                                         If droit Then
                                                             ' Clic droit : le menu habituel de la fenetre (FullScreen, etc.), comme sur une video.
                                                             _hote.ContextMenuStrip1.Show(pt)
                                                         Else
                                                             _hote.ClicSurEcranDeCameras(pt)
                                                         End If
                                                     Catch ex As Exception
                                                         DiagLog("Camera IP : clic sur une camera : " & ex.Message)
                                                     End Try
                                                 End Sub))
                End If
            End If
        Catch ex As Exception
        End Try
        Return CallNextHookEx(_crochet, code, wParam, lParam)
    End Function

    <StructLayout(LayoutKind.Sequential)>
    Private Structure Cadre
        Public Gauche As Integer
        Public Haut As Integer
        Public Droite As Integer
        Public Bas As Integer
    End Structure

    <DllImport("user32.dll")>
    Private Shared Function GetWindowRect(fenetre As IntPtr, ByRef r As Cadre) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetParent(fenetre As IntPtr) As IntPtr
    End Function

    ''' <summary>Le point (ecran) tombe-t-il sur une image de camera, c'est-a-dire une case qui contient une fenetre VLC ? (Form3 : le double clic natif ne doit pas s'ajouter a celui du crochet de souris.)</summary>
    Friend Function ImageSousLePoint(pointEcran As Point) As Boolean
        Try
            For Each c As PictureBox In _cases
                If c.Visible AndAlso c.RectangleToScreen(c.ClientRectangle).Contains(pointEcran) Then Return GetWindow(c.Handle, 5) <> IntPtr.Zero
            Next
            If _agrandie IsNot Nothing AndAlso _agrandie.RectangleToScreen(_agrandie.ClientRectangle).Contains(pointEcran) Then Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetWindowPos(fenetre As IntPtr, apres As IntPtr, x As Integer, y As Integer, l As Integer, h As Integer, drapeaux As UInteger) As Boolean
    End Function

    Private _corrections As Integer

    ''' <summary>Apres le plein ecran, la fenetre reelle d'une case (et celle de VLC dedans) n'avait plus la taille que Slide lui avait donnee : 5 colonnes de 293 pixels au lieu de 8 de 184, 25 cameras visibles
    ''' sur 64 (29/10/2026). Chaque seconde, la taille REELLE (GetWindowRect) est comparee a la taille voulue et remise d'autorite si elle differe ; la fenetre VLC de la case prend celle de la case.</summary>
    Private Sub RectifierLesFenetres()
        Try
            Dim corriges As Integer = 0
            For i As Integer = 0 To _cases.Count - 1
                Dim c As PictureBox = _cases(i)
                Dim voulu As Rectangle = c.Bounds
                Dim r As Cadre
                If c.IsHandleCreated AndAlso GetWindowRect(c.Handle, r) Then
                    If r.Droite - r.Gauche <> voulu.Width OrElse r.Bas - r.Haut <> voulu.Height Then
                        SetWindowPos(c.Handle, IntPtr.Zero, voulu.X, voulu.Y, voulu.Width, voulu.Height, &H14UI)          ' SWP_NOZORDER | SWP_NOACTIVATE
                        corriges += 1
                    End If
                End If
                Dim largeur As Integer = voulu.Width, hauteur As Integer = voulu.Height
                Dim poigneeCase As IntPtr = c.Handle
                EnumChildWindows(c.Handle, Function(h As IntPtr, p As IntPtr) As Boolean
                                               ' Seulement la fenetre posee DIRECTEMENT dans la case, et avec 2 pixels de tolerance : la fenetre interne de VLC a 1 pixel de moins et il la remet
                                               ' aussitot (55 fenetres « corrigees » chaque seconde sans fin, 29/10/2026).
                                               If GetParent(h) <> poigneeCase Then Return True
                                               Dim e As Cadre
                                               If GetWindowRect(h, e) Then
                                                   If Math.Abs(e.Droite - e.Gauche - largeur) > 2 OrElse Math.Abs(e.Bas - e.Haut - hauteur) > 2 Then
                                                       SetWindowPos(h, IntPtr.Zero, 0, 0, largeur, hauteur, &H14UI)
                                                       corriges += 1
                                                   End If
                                               End If
                                               Return True
                                           End Function, IntPtr.Zero)
            Next
            If _agrandie IsNot Nothing Then
                ' La camera agrandie : sa fenetre VLC prend la taille de la fenetre principale.
                Dim gr As PictureBox = _agrandie
                Dim lg As Integer = gr.Width, ht As Integer = gr.Height
                Dim poigneeGrande As IntPtr = gr.Handle
                EnumChildWindows(gr.Handle, Function(h As IntPtr, p As IntPtr) As Boolean
                                                If GetParent(h) <> poigneeGrande Then Return True
                                                Dim e As Cadre
                                                If GetWindowRect(h, e) Then
                                                    If Math.Abs(e.Droite - e.Gauche - lg) > 2 OrElse Math.Abs(e.Bas - e.Haut - ht) > 2 Then
                                                        SetWindowPos(h, IntPtr.Zero, 0, 0, lg, ht, &H14UI)
                                                        corriges += 1
                                                    End If
                                                End If
                                                Return True
                                            End Function, IntPtr.Zero)
            End If
            If corriges > 0 Then
                _corrections += corriges
                DiagLog("Camera IP : " & corriges & " fenetre(s) remise(s) a la taille voulue (total " & _corrections & ")")
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Desactive les fenetres que VLC a creees dans la case ; vrai si au moins une existe (VLC les pose une seconde ou deux apres son demarrage).</summary>
    Private Shared Function DesactiverLesFenetresDe(c As PictureBox) As Boolean
        Dim trouvees As Integer = 0
        Try
            EnumChildWindows(c.Handle, Function(h As IntPtr, p As IntPtr) As Boolean
                                           EnableWindow(h, False)
                                           trouvees += 1
                                           Return True
                                       End Function, IntPtr.Zero)
        Catch ex As Exception
        End Try
        Return trouvees > 0
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Les processus VLC meurent avec Slide (job Windows « tuer a la fermeture »)
    ' ------------------------------------------------------------------------------------------

    <StructLayout(LayoutKind.Sequential)>
    Private Structure LimitesDeBase
        Public TempsParProcessus As Long
        Public TempsDuJob As Long
        Public Drapeaux As UInteger
        Public MemoireMin As UIntPtr
        Public MemoireMax As UIntPtr
        Public ProcessusActifs As UInteger
        Public Affinite As UIntPtr
        Public Priorite As UInteger
        Public Ordonnancement As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure CompteursES
        Public A As ULong
        Public B As ULong
        Public C As ULong
        Public D As ULong
        Public E As ULong
        Public F As ULong
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure LimitesEtendues
        Public De_Base As LimitesDeBase
        Public ES As CompteursES
        Public MemoireProcessus As UIntPtr
        Public MemoireJob As UIntPtr
        Public PicProcessus As UIntPtr
        Public PicJob As UIntPtr
    End Structure

    <DllImport("kernel32.dll", CharSet:=CharSet.Unicode, SetLastError:=True)>
    Private Shared Function CreateJobObject(attributs As IntPtr, nom As String) As IntPtr
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function SetInformationJobObject(job As IntPtr, classe As Integer, info As IntPtr, taille As Integer) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Shared Function AssignProcessToJobObject(job As IntPtr, processus As IntPtr) As Boolean
    End Function

    Private Shared _job As IntPtr = IntPtr.Zero

    Private Shared Sub LierAuJob(p As Process)
        Try
            If _job = IntPtr.Zero Then
                _job = CreateJobObject(IntPtr.Zero, Nothing)
                Dim l As New LimitesEtendues()
                l.De_Base.Drapeaux = &H2000UI                    ' JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE
                Dim taille As Integer = Marshal.SizeOf(Of LimitesEtendues)()
                Dim ptr As IntPtr = Marshal.AllocHGlobal(taille)
                Try
                    Marshal.StructureToPtr(l, ptr, False)
                    SetInformationJobObject(_job, 9, ptr, taille)        ' JobObjectExtendedLimitInformation
                Finally
                    Marshal.FreeHGlobal(ptr)
                End Try
            End If
            AssignProcessToJobObject(_job, p.Handle)
        Catch ex As Exception
            DiagLog("Camera IP : job des lecteurs : " & ex.Message)
        End Try
    End Sub

End Class
