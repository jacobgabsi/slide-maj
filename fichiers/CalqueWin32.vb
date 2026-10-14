Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices

''' <summary>
''' Fenetres "calques" (WS_EX_LAYERED) : affichage d'une image avec transparence par pixel, pour
''' poser du texte ou une montre sur les photos ET les videos sans rectangle de fond. Partage par
''' FormHorloge (montre) et FormLegende (lieu et date en bas a gauche).
''' La fenetre doit avoir le style WS_EX_LAYERED (CreateParams) et etre affichee.
''' </summary>
Module CalqueWin32

    Public Const WS_EX_LAYERED As Integer = &H80000
    Public Const WS_EX_TRANSPARENT As Integer = &H20       ' les clics traversent
    Public Const WS_EX_TOOLWINDOW As Integer = &H80        ' pas de bouton dans la barre des taches
    Public Const WS_EX_NOACTIVATE As Integer = &H8000000   ' ne prend jamais le focus

    ''' <summary>Plein ecran : fenetre independante au premier plan. Mode fenetre : rattachee a
    ''' <paramref name="reference"/> (la suit, disparait avec elle). N'affecte Owner et le premier
    ''' plan QUE s'ils changent : les reaffecter a chaque photo (meme valeur) fermait le menu du
    ''' clic droit de Form3 des l'image suivante.
    ''' Le premier plan est pose par SetWindowPos SANS activation, jamais par la propriete TopMost :
    ''' une Form TopMost prend le focus en s'affichant (ShowWithoutActivation ignore) et Form3 ne
    ''' recevait plus les touches du clavier en plein ecran. Les Forms calques ont donc
    ''' TopMost = False dans le concepteur.</summary>
    Public Sub DefinirPremierPlan(fenetre As Form, reference As Form, premierPlan As Boolean)
        Dim proprietaire As Form = If(premierPlan, Nothing, reference)
        If Not ReferenceEquals(fenetre.Owner, proprietaire) Then fenetre.Owner = proprietaire
        Dim estPremierPlan As Boolean = (GetWindowLong(fenetre.Handle, GWL_EXSTYLE) And WS_EX_TOPMOST) <> 0
        If estPremierPlan <> premierPlan Then
            SetWindowPos(fenetre.Handle, If(premierPlan, HWND_TOPMOST, HWND_NOTOPMOST), 0, 0, 0, 0,
                         SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        End If
    End Sub

    ''' <summary>Opacite du lieu/date dans les bandes noires : discrets, l'image reste au premier
    ''' plan (1 = opaque).</summary>
    Public Const OpaciteDiscrete As Single = 0.45F
    ''' <summary>Opacite de la montre : attenuee elle aussi, mais moins (a 0,45 on ne la voyait
    ''' plus assez).</summary>
    Public Const OpaciteMontre As Single = 0.75F

    ''' <summary>Multiplie la transparence de chaque pixel par <paramref name="facteur"/>. Un pixel
    ''' visible, meme a peine, garde au moins alpha = 1 : la montre recoit la souris grace a un
    ''' disque d'alpha 1 (un pixel d'alpha 0 laisse passer les clics).</summary>
    Public Sub Attenuer(image As Bitmap, facteur As Single)
        If facteur >= 1 Then Exit Sub
        Dim zone As New Rectangle(0, 0, image.Width, image.Height)
        Dim donnees As Imaging.BitmapData = image.LockBits(zone, Imaging.ImageLockMode.ReadWrite, Imaging.PixelFormat.Format32bppArgb)
        Try
            Dim octets(donnees.Stride * donnees.Height - 1) As Byte
            Marshal.Copy(donnees.Scan0, octets, 0, octets.Length)
            For i As Integer = 3 To octets.Length - 1 Step 4
                Dim a As Byte = octets(i)
                If a > 1 Then octets(i) = CByte(Math.Max(1, CInt(a * facteur)))
            Next
            Marshal.Copy(octets, 0, donnees.Scan0, octets.Length)
        Finally
            image.UnlockBits(donnees)
        End Try
    End Sub

    ''' <summary>Envoie l'image (ARGB) a la fenetre calque, a la position actuelle de la fenetre.</summary>
    Public Sub Appliquer(fenetre As Form, image As Bitmap)
        Dim ecranDC As IntPtr = GetDC(IntPtr.Zero)
        Dim memoireDC As IntPtr = CreateCompatibleDC(ecranDC)
        Dim hBitmap As IntPtr = image.GetHbitmap(Color.FromArgb(0))   ' alpha premultiplie
        Dim ancien As IntPtr = SelectObject(memoireDC, hBitmap)
        Try
            Dim taille As New TailleNative With {.Largeur = image.Width, .Hauteur = image.Height}
            Dim source As New PointNatif With {.X = 0, .Y = 0}
            Dim destination As New PointNatif With {.X = fenetre.Left, .Y = fenetre.Top}
            Dim melange As New Melange With {.BlendOp = 0, .BlendFlags = 0,
                                             .SourceConstantAlpha = 255, .AlphaFormat = AC_SRC_ALPHA}
            UpdateLayeredWindow(fenetre.Handle, ecranDC, destination, taille, memoireDC, source, 0, melange, ULW_ALPHA)
        Finally
            SelectObject(memoireDC, ancien)
            DeleteObject(hBitmap)
            DeleteDC(memoireDC)
            ReleaseDC(IntPtr.Zero, ecranDC)
        End Try
    End Sub

    ''' <summary>Replace la fenetre au sommet des fenetres "premier plan", sans l'activer.</summary>
    Public Sub ReaffirmerPremierPlan(fenetre As Form)
        SetWindowPos(fenetre.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        RemettreLaFenetreProtegee(fenetre)          ' (la legende d'un clip Blink ne doit pas rester sous l'horloge, la legende ou la bande : elle clignotait)
    End Sub

    ''' <summary>Met la fenetre au premier plan ou l'en retire, sans l'activer ni toucher a son
    ''' proprietaire (contrairement a DefinirPremierPlan).</summary>
    Public Sub DefinirPremierPlanSeul(fenetre As Form, premierPlan As Boolean)
        SetWindowPos(fenetre.Handle, If(premierPlan, HWND_TOPMOST, HWND_NOTOPMOST), 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        ' Mesure du 01/10 (2 PIP au BOOT) : pour une des fenetres, Windows repondait "fait" mais
        ' ne posait PAS le premier plan (il la croyait deja devant) ; elle restait derriere Slide.
        ' Retirer puis remettre le premier plan, lui, fonctionne.
        If premierPlan AndAlso (GetWindowLong(fenetre.Handle, GWL_EXSTYLE) And WS_EX_TOPMOST) = 0 Then
            SetWindowPos(fenetre.Handle, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
            SetWindowPos(fenetre.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        End If
        RemettreLaFenetreProtegee(fenetre)
    End Sub

    ''' <summary>Range la fenetre juste derriere <paramref name="devant"/>, sans l'activer. Si elle
    ''' etait au premier plan, elle ne l'est plus (Windows le retire a une fenetre placee derriere
    ''' une fenetre normale).</summary>
    Public Sub PlacerDerriere(fenetre As Form, devant As IntPtr)
        SetWindowPos(fenetre.Handle, devant, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        RemettreLaFenetreProtegee(fenetre)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Fenetre protegee : la normalisation du volume (fenetre ou pastille) reste AU-DESSUS des PIP.
    ' Les PIP se remettent devant toutes les 2 s ; a chaque fois, la fenetre protegee reprend sa
    ' place juste apres (au sommet, ou juste derriere la fenetre sous laquelle elle se range).
    ' ------------------------------------------------------------------------------------------

    ' ------------------------------------------------------------------------------------------
    ' Slide au premier plan ou non. Les PIP et la pastille de la normalisation ne sont "au-dessus
    ' de tout" QUE si Slide est le programme actif. Si un autre programme passe devant Slide, ils
    ' passent derriere lui avec Slide : ils se rangent juste au-dessus de la fenetre de Slide.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Fenetre principale de Slide (Form3), notee sur le thread de l'interface pour les
    ''' PIP qui vivent dans d'autres threads.</summary>
    Public Property FenetreDeSlide As IntPtr = IntPtr.Zero

    Private ReadOnly _processus As UInteger = CUInt(Process.GetCurrentProcess().Id)

    ''' <summary>Vrai si le programme actif (fenetre au premier plan) est Slide.</summary>
    Public Function SlideAuPremierPlan() As Boolean
        Dim active As IntPtr = GetForegroundWindow()
        If active = IntPtr.Zero Then Return True
        Dim pid As UInteger
        GetWindowThreadProcessId(active, pid)
        Return pid = _processus
    End Function

    ''' <summary>Vrai si la fenetre active est l'IMAGE de Slide (sa fenetre principale). Les PIP ne
    ''' sont "au-dessus de tout" que dans ce cas. Des qu'une AUTRE fenetre est active - un autre
    ''' programme, mais aussi une fenetre de Slide : reglages, choix d'un film, boite "Ouvrir",
    ''' message - ils se rangent juste au-dessus de l'image, donc derriere cette fenetre.
    ''' (06/10 : le PIP restait devant la boite "Ouvrir" et devant l'Explorateur de fichiers.)</summary>
    Public Function ImageDeSlideActive() As Boolean
        Dim active As IntPtr = GetForegroundWindow()
        If active = IntPtr.Zero Then Return True
        Dim reference As IntPtr = FenetreDeSlide
        ' Fenetre principale pas encore affichee (demarrage) : comme avant, d'apres le programme actif.
        If reference = IntPtr.Zero OrElse Not IsWindowVisible(reference) Then Return SlideAuPremierPlan()
        If active = reference Then Return True
        ' Les petites fenetres posees sur l'image (PIP, montre, legende) ne prennent jamais le
        ' clavier ; si l'une d'elles etait tout de meme active, c'est comme l'image.
        Dim pid As UInteger
        GetWindowThreadProcessId(active, pid)
        Return pid = _processus AndAlso (GetWindowLong(active, GWL_EXSTYLE) And WS_EX_NOACTIVATE) <> 0
    End Function

    ''' <summary>Range <paramref name="fenetre"/> juste au-dessus de la fenetre de Slide (et des
    ''' autres petites fenetres de Slide deja posees dessus), sans premier plan : un programme
    ''' passe devant Slide la recouvre aussi. Ne fait rien si elle y est deja.</summary>
    Public Sub PlacerAuDessusDeSlide(fenetre As Form)
        Dim reference As IntPtr = FenetreDeSlide
        If reference = IntPtr.Zero Then Exit Sub
        Dim h As IntPtr = fenetre.Handle
        Dim estPremierPlan As Boolean = (GetWindowLong(h, GWL_EXSTYLE) And WS_EX_TOPMOST) <> 0
        If Not estPremierPlan AndAlso DansLeGroupeDeSlide(h, reference) Then
            ' Deja range avec Slide. Reste un cas (09/10, "le PIP est de nouveau au-dessus de form4") :
            ' un PIP qui DEMARRE alors que les reglages sont ouverts (tache de la minuterie, bouton
            ' Play du tableau, film suivant). Windows pose toute nouvelle fenetre en haut de la pile :
            ' il se trouvait devant les reglages, sans premier plan, et etait juge "a sa place".
            PasserDerriereLesFenetresDeSlide(h, reference)
            Exit Sub
        End If
        Dim dessus As IntPtr = GetWindow(reference, GW_HWNDPREV)
        If dessus = IntPtr.Zero OrElse (GetWindowLong(dessus, GWL_EXSTYLE) And WS_EX_TOPMOST) <> 0 Then
            ' Rien de "normal" au-dessus de Slide : en tete des fenetres normales.
            SetWindowPos(h, HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        Else
            ' Juste sous la fenetre qui est au-dessus de Slide (Windows lui retire le premier plan).
            SetWindowPos(h, dessus, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        End If
        ' Windows REFUSE de ranger une fenetre derriere celle d'un programme systeme (erreur 5,
        ' "acces refuse" : services du constructeur, pilote graphique, saisie clavier, DWM... leurs
        ' fenetres invisibles se glissent n'importe ou dans l'empilement). Mesure du 06/10 : 10
        ' fenetres sur 446 dans ce cas. Si c'est l'une d'elles qui se trouve juste au-dessus de
        ' Slide, le PIP gardait son premier plan et restait DEVANT l'autre programme (Explorateur
        ' de fichiers...), indefiniment. On verifie donc le resultat ; s'il n'est pas bon, on se
        ' range par rapport a la fenetre de Slide elle-meme, ce que Windows accepte toujours.
        If (GetWindowLong(h, GWL_EXSTYLE) And WS_EX_TOPMOST) <> 0 OrElse Not DansLeGroupeDeSlide(h, reference) Then
            NoterVoisineRefusee(dessus)
            PlacerParRapportASlide(h, reference)
        End If
        ' Range "en tete des fenetres normales" : pas devant les reglages pour autant.
        PasserDerriereLesFenetresDeSlide(h, reference)
        ' La pastille de la normalisation reste au-dessus du PIP qu'on vient de ranger.
        Dim protegee As IntPtr = _protegee
        If protegee <> IntPtr.Zero AndAlso protegee <> h AndAlso _protegeeDerriere = IntPtr.Zero Then
            Dim auDessusDuPip As IntPtr = GetWindow(h, GW_HWNDPREV)
            If auDessusDuPip <> protegee Then
                Dim apres As IntPtr = If(auDessusDuPip = IntPtr.Zero OrElse (GetWindowLong(auDessusDuPip, GWL_EXSTYLE) And WS_EX_TOPMOST) <> 0,
                                         HWND_NOTOPMOST, auDessusDuPip)
                SetWindowPos(protegee, apres, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE Or SWP_ASYNCWINDOWPOS)
            End If
        End If
    End Sub

    ''' <summary>Vrai si <paramref name="h"/> est AU-DESSUS de l'image de Slide dans la pile de Windows (devant elle ou sur un premier plan).
    ''' Faux : il est passe DERRIERE l'image (il est visible pour Windows, mais cache a l'ecran).</summary>
    Public Function EstAuDessusDeLImage(h As IntPtr) As Boolean
        Dim reference As IntPtr = FenetreDeSlide
        If reference = IntPtr.Zero OrElse h = IntPtr.Zero Then Return True
        Dim w As IntPtr = GetWindow(reference, GW_HWNDPREV)
        Dim n As Integer = 0
        Do While w <> IntPtr.Zero AndAlso n < 3000
            If w = h Then Return True
            w = GetWindow(w, GW_HWNDPREV)
            n += 1
        Loop
        Return False
    End Function

    ''' <summary>Si <paramref name="h"/> (un PIP, la pastille) se trouve DEVANT une fenetre ordinaire de
    ''' Slide posee sur l'image - reglages, choix d'un film, boite "Ouvrir", message... -, le range
    ''' juste derriere la plus basse d'entre elles. Il reste au-dessus de l'image et de ce qui est pose
    ''' dessus (legende, montre, autres PIP). Sans effet s'il n'y en a pas.</summary>
    Private Sub PasserDerriereLesFenetresDeSlide(h As IntPtr, reference As IntPtr)
        Dim ordinaire As IntPtr = OrdinaireEntre(h, reference)
        If ordinaire <> IntPtr.Zero Then SetWindowPos(h, ordinaire, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
    End Sub

    ''' <summary>La plus basse fenetre ORDINAIRE de Slide situee entre <paramref name="h"/> et l'image
    ''' dans la pile de Windows (zero s'il n'y en a pas, ou si h n'est pas au-dessus de l'image).
    ''' Ordinaire : visible, pas reduite, et qui peut prendre le clavier. Les petites fenetres posees
    ''' sur l'image (PIP, legende, montre, texte des bandes, videos animees, message) ont toutes le
    ''' style WS_EX_NOACTIVATE : elles ne comptent pas. (Le proprietaire ne les distingue pas : les
    ''' reglages, ouverts par ShowDialog, appartiennent eux aussi a la fenetre de l'image.)</summary>
    Private Function OrdinaireEntre(h As IntPtr, reference As IntPtr) As IntPtr
        Dim plusBasse As IntPtr = IntPtr.Zero
        Dim w As IntPtr = GetWindow(h, GW_HWNDNEXT)
        For i As Integer = 1 To 400
            If w = IntPtr.Zero Then Return IntPtr.Zero
            If w = reference Then Return plusBasse
            If IsWindowVisible(w) AndAlso Not IsIconic(w) AndAlso (GetWindowLong(w, GWL_EXSTYLE) And WS_EX_NOACTIVATE) = 0 Then
                Dim pid As UInteger
                GetWindowThreadProcessId(w, pid)
                If pid = _processus Then plusBasse = w
            End If
            w = GetWindow(w, GW_HWNDNEXT)
        Next
        Return IntPtr.Zero
    End Function

    ''' <summary>Range <paramref name="h"/> juste au-dessus de la fenetre de Slide sans se servir
    ''' d'aucune autre fenetre : d'abord juste SOUS Slide (elle y perd son premier plan a coup
    ''' sur), puis Slide passe juste sous elle. Le second deplacement concerne la fenetre de
    ''' Slide, qui appartient au thread de l'interface : il est demande sans attendre.</summary>
    Private Sub PlacerParRapportASlide(h As IntPtr, reference As IntPtr)
        SetWindowPos(h, reference, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
        SetWindowPos(reference, h, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE Or SWP_ASYNCWINDOWPOS)
    End Sub

    ''' <summary>
    ''' Range <paramref name="fenetre"/> IMMEDIATEMENT au-dessus de <paramref name="reference"/> :
    ''' sous tout ce qui est deja pose dessus, y compris les fenetres qui lui sont attachees (texte
    ''' des bandes, legende, montre, quand elles ne sont pas au premier plan). A appeler depuis le
    ''' fil de l'interface. Sert aux fenetres des videos animees (FormTransitionVideo, VideoVivante).
    '''
    ''' Pourquoi (07/10, signale par Jacob : "utiliser les bandes" ne marchait plus avec le fond
    ''' flou) : ces fenetres etaient rangees en passant d'abord SOUS la fenetre de Slide, puis en
    ''' faisant passer Slide sous elles. Or deplacer Slide dans la pile entraine les fenetres qui
    ''' lui sont attachees : elles se retrouvaient juste au-dessus de Slide, donc SOUS le fond flou
    ''' (verifie au banc : "fond > legende > texte > Slide"). On se range donc directement sous la
    ''' fenetre qui se trouve juste au-dessus de Slide, sans toucher a Slide. Si Windows refuse
    ''' (voisine d'un programme systeme, voir PlacerAuDessusDeSlide), l'ancienne methode, puis on
    ''' repasse sous la plus basse des fenetres attachees.
    ''' </summary>
    Public Sub PlacerJusteAuDessus(fenetre As IntPtr, reference As IntPtr)
        Const SansBouger As UInteger = SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE
        ' La premiere fenetre VISIBLE au-dessus de la reference. Les fenetres invisibles ne comptent
        ' pas : il y en a partout dans la pile (saisie clavier, services...), et se ranger sous
        ' l'une d'elles echoue ou ne veut rien dire. (Essai du 07/10 : une fenetre invisible
        ' intercalee faisait echouer ce rangement ; l'ancienne methode, prise en secours, faisait
        ' alors repasser l'ancienne image d'une transition devant la legende.)
        Dim dessus As IntPtr = VisibleAuDessus(reference)
        If dessus = fenetre Then Exit Sub
        If dessus <> IntPtr.Zero Then SetWindowPos(fenetre, dessus, 0, 0, 0, 0, SansBouger)
        If VisibleAuDessus(reference) = fenetre Then Exit Sub
        ' Refuse, ou rien au-dessus de la reference.
        SetWindowPos(fenetre, reference, 0, 0, 0, 0, SansBouger)
        SetWindowPos(reference, fenetre, 0, 0, 0, 0, SansBouger)
        ' Entre la fenetre et la reference : les fenetres attachees a celle-ci, que Windows vient
        ' de glisser la. La fenetre repasse sous la plus basse.
        Dim plusBasse As IntPtr = IntPtr.Zero
        Dim h As IntPtr = GetWindow(fenetre, GW_HWNDNEXT)
        Dim garde As Integer = 0
        Do While h <> IntPtr.Zero AndAlso h <> reference AndAlso garde < 200
            plusBasse = h
            h = GetWindow(h, GW_HWNDNEXT)
            garde += 1
        Loop
        If h = reference AndAlso plusBasse <> IntPtr.Zero Then SetWindowPos(fenetre, plusBasse, 0, 0, 0, 0, SansBouger)
    End Sub

    ''' <summary>Premiere fenetre visible au-dessus de <paramref name="h"/> dans la pile de Windows
    ''' (zero s'il n'y en a pas).</summary>
    Private Function VisibleAuDessus(h As IntPtr) As IntPtr
        Dim w As IntPtr = GetWindow(h, GW_HWNDPREV)
        Dim garde As Integer = 0
        Do While w <> IntPtr.Zero AndAlso Not IsWindowVisible(w) AndAlso garde < 2000
            w = GetWindow(w, GW_HWNDPREV)
            garde += 1
        Loop
        Return w
    End Function

    Private ReadOnly _voisinesRefusees As New HashSet(Of String)

    ''' <summary>Trace, une fois par sorte de fenetre : derriere quoi Windows a refuse de ranger.</summary>
    Private Sub NoterVoisineRefusee(voisine As IntPtr)
        Try
            Dim classe As New System.Text.StringBuilder(128)
            If voisine <> IntPtr.Zero Then GetClassNameW(voisine, classe, classe.Capacity)
            Dim pid As UInteger
            If voisine <> IntPtr.Zero Then GetWindowThreadProcessId(voisine, pid)
            Dim programme As String = "?"
            Try
                If pid <> 0 Then programme = Process.GetProcessById(CInt(pid)).ProcessName
            Catch ex As Exception
            End Try
            Dim quoi As String = classe.ToString() & " [" & programme & "]"
            SyncLock _voisinesRefusees
                If Not _voisinesRefusees.Add(quoi) Then Exit Sub
            End SyncLock
            DiagLog("PIP : Windows refuse de le ranger derriere " & quoi & " -> range par rapport a la fenetre de Slide")
        Catch ex As Exception
        End Try
    End Sub

    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Function GetClassNameW(hWnd As IntPtr, texte As System.Text.StringBuilder, capacite As Integer) As Integer
    End Function

    ''' <summary>Vrai si la fenetre <paramref name="h"/> est visible ET au-dessus de la fenetre de
    ''' Slide dans l'empilement. Les PIP et la pastille se rangent derriere les reglages pour ne
    ''' pas les couvrir ; mais si les reglages sont eux-memes passes DERRIERE l'image (film lance
    ''' depuis la minuterie, reglages restes ouverts), se ranger derriere eux les cachait sous
    ''' l'image : "le PIP a disparu au retour du film".</summary>
    Public Function EstDevantSlide(h As IntPtr) As Boolean
        Dim reference As IntPtr = FenetreDeSlide
        If h = IntPtr.Zero OrElse reference = IntPtr.Zero OrElse Not IsWindowVisible(h) OrElse IsIconic(h) Then Return False
        Dim w As IntPtr = GetWindow(h, GW_HWNDNEXT)
        For i As Integer = 1 To 2000
            If w = IntPtr.Zero Then Return False
            If w = reference Then Return True
            w = GetWindow(w, GW_HWNDNEXT)
        Next
        Return False
    End Function

    <DllImport("user32.dll")> Private Function IsIconic(hWnd As IntPtr) As Boolean
    End Function

    ''' <summary>Vrai si, en descendant depuis <paramref name="h"/>, on atteint la fenetre de Slide
    ''' sans croiser de fenetre visible d'un autre programme.</summary>
    Private Function DansLeGroupeDeSlide(h As IntPtr, reference As IntPtr) As Boolean
        Dim w As IntPtr = GetWindow(h, GW_HWNDNEXT)
        For i As Integer = 1 To 200
            If w = IntPtr.Zero Then Return False
            If w = reference Then Return True
            If IsWindowVisible(w) Then
                Dim pid As UInteger
                GetWindowThreadProcessId(w, pid)
                If pid <> _processus Then Return False
            End If
            w = GetWindow(w, GW_HWNDNEXT)
        Next
        Return False
    End Function

    Private Const GW_HWNDNEXT As UInteger = 2
    Private Const GW_HWNDPREV As UInteger = 3
    <DllImport("user32.dll")> Private Function GetForegroundWindow() As IntPtr
    End Function
    <DllImport("user32.dll")> Private Function GetWindow(hWnd As IntPtr, uCmd As UInteger) As IntPtr
    End Function
    <DllImport("user32.dll")> Private Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef processId As UInteger) As UInteger
    End Function
    <DllImport("user32.dll")> Private Function IsWindowVisible(hWnd As IntPtr) As Boolean
    End Function

    Private _protegee As IntPtr = IntPtr.Zero
    Private _protegeeDerriere As IntPtr = IntPtr.Zero      ' Zero : au sommet du premier plan

    ''' <summary>Declare la fenetre a garder au-dessus des PIP (Zero pour aucune), et sa place :
    ''' au sommet (<paramref name="derriere"/> = Zero) ou juste derriere une fenetre.</summary>
    Public Sub ProtegerLaFenetre(poignee As IntPtr, derriere As IntPtr)
        _protegee = poignee
        _protegeeDerriere = derriere
    End Sub

    Private _legende As IntPtr = IntPtr.Zero

    ''' <summary>Legende d'un clip Blink : remise au sommet juste apres chaque PIP qui se remet devant (sinon il la recouvre, ou elle clignote si elle se remet devant elle-meme). Zero : aucune.</summary>
    Public Sub ProtegerLaLegende(poignee As IntPtr)
        _legende = poignee
    End Sub

    Private Sub RemettreLaFenetreProtegee(deplacee As Form)
        Dim legende As IntPtr = _legende
        ' Deja tout en haut (aucune fenetre au-dessus) : on n'y touche pas, remettre une fenetre au sommet toutes les 0,5 s la faisait clignoter.
        If legende <> IntPtr.Zero AndAlso legende <> deplacee.Handle AndAlso Not TypeOf deplacee Is FormProgression AndAlso GetWindow(legende, GW_HWNDPREV) <> IntPtr.Zero Then
            SetWindowPos(legende, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE Or SWP_ASYNCWINDOWPOS)
        End If
        Dim protegee As IntPtr = _protegee
        If protegee = IntPtr.Zero OrElse TypeOf deplacee Is FormProgression Then Exit Sub
        ' Slide n'est pas le programme actif : la pastille reste rangee avec Slide (pas au sommet).
        If _protegeeDerriere = IntPtr.Zero AndAlso Not SlideAuPremierPlan() Then Exit Sub
        ' Asynchrone : la fenetre protegee appartient au thread de l'interface, les PIP a leur
        ' propre thread ; on ne l'attend pas.
        SetWindowPos(protegee, If(_protegeeDerriere = IntPtr.Zero, HWND_TOPMOST, _protegeeDerriere), 0, 0, 0, 0,
                     SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE Or SWP_ASYNCWINDOWPOS)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Win32
    ' ------------------------------------------------------------------------------------------

    <StructLayout(LayoutKind.Sequential)>
    Private Structure PointNatif
        Public X As Integer
        Public Y As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure TailleNative
        Public Largeur As Integer
        Public Hauteur As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential, Pack:=1)>
    Private Structure Melange
        Public BlendOp As Byte
        Public BlendFlags As Byte
        Public SourceConstantAlpha As Byte
        Public AlphaFormat As Byte
    End Structure

    Private Const ULW_ALPHA As Integer = 2
    Private Const AC_SRC_ALPHA As Byte = 1
    Private ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private ReadOnly HWND_NOTOPMOST As New IntPtr(-2)
    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_TOPMOST As Integer = &H8
    Private Const SWP_NOSIZE As UInteger = &H1
    Private Const SWP_NOMOVE As UInteger = &H2
    Private Const SWP_NOACTIVATE As UInteger = &H10
    Private Const SWP_ASYNCWINDOWPOS As UInteger = &H4000

    <DllImport("user32.dll", SetLastError:=True)>
    Private Function UpdateLayeredWindow(hwnd As IntPtr, hdcDst As IntPtr, ByRef pptDst As PointNatif,
                                         ByRef psize As TailleNative, hdcSrc As IntPtr, ByRef pptSrc As PointNatif,
                                         crKey As Integer, ByRef pblend As Melange, dwFlags As Integer) As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, x As Integer, y As Integer,
                                  cx As Integer, cy As Integer, uFlags As UInteger) As Boolean
    End Function
    <DllImport("user32.dll")> Private Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function
    <DllImport("user32.dll")> Private Function GetDC(hWnd As IntPtr) As IntPtr
    End Function
    <DllImport("user32.dll")> Private Function ReleaseDC(hWnd As IntPtr, hDC As IntPtr) As Integer
    End Function
    <DllImport("gdi32.dll")> Private Function CreateCompatibleDC(hDC As IntPtr) As IntPtr
    End Function
    <DllImport("gdi32.dll")> Private Function DeleteDC(hdc As IntPtr) As Boolean
    End Function
    <DllImport("gdi32.dll")> Private Function SelectObject(hDC As IntPtr, hObject As IntPtr) As IntPtr
    End Function
    <DllImport("gdi32.dll")> Private Function DeleteObject(hObject As IntPtr) As Boolean
    End Function

End Module
