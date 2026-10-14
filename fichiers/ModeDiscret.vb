Imports System.IO

''' <summary>
''' Mode discret (03/11/2026, demande de Jacob : « un mode sans photo sans video, Slide etant cache dans la barre des taches pres de
''' l'horloge Windows »). Plus de photo, plus de video, plus de PIP : la fenetre de Slide est cachee, seule une icone reste dans la zone de
''' notification. La MUSIQUE et la MINUTERIE continuent (taches, Deezer, radio, telecommande).
'''
''' Principe : le diaporama (Form1) passe par <see cref="Attendre"/> avant chaque photo et chaque video ; tant que le mode est actif il y
''' reste (aucun media n'est charge ni decode). L'etat est garde dans data\settings\ModeDiscret.txt : apres une relance (nuit, mise a jour,
''' redemarrage du PC) Slide revient tout seul en mode discret. Ce fichier est propre au poste : il n'est pas dans l'export des reglages.
''' Entree : menu du clic droit, commande « Slide.Discret » de la telecommande. Sortie : double clic sur l'icone, ou son menu.
''' </summary>
Friend Module ModeDiscret

    Private _actif As Boolean
    Private _lu As Boolean
    Private _icone As NotifyIcon

    Private Function FichierMarque() As String
        Return Path.Combine(DossierReglages, "ModeDiscret.txt")
    End Function

    ''' <summary>Vrai tant que Slide est en mode discret.</summary>
    Friend ReadOnly Property Actif As Boolean
        Get
            If Not _lu Then
                _lu = True
                Try
                    _actif = File.Exists(FichierMarque())
                Catch ex As Exception
                End Try
            End If
            Return _actif
        End Get
    End Property

    ''' <summary>Entre dans le mode discret (ou le quitte). Le diaporama s'en apercoit tout de suite : la photo ou la video en cours est abandonnee.</summary>
    Friend Sub Definir(voulu As Boolean)
        ' (Le parametre ne s'appelle PAS « actif » : VB ne distingue pas les majuscules, il masquait la propriete Actif et la procedure sortait toujours ici.)
        If Actif = voulu Then Exit Sub
        _actif = voulu
        If voulu Then _cache = True
        Try
            If voulu Then File.WriteAllText(FichierMarque(), Now.ToString("s")) Else File.Delete(FichierMarque())
        Catch ex As Exception
            DiagLog("Mode discret : etat non enregistre (" & ex.Message & ")")
        End Try
        DiagLog("Mode discret : " & If(voulu, "active (musique seule, Slide cache pres de l'horloge)", "quitte (le diaporama reprend)"))
        If voulu Then
            ' (la photo ou la video en cours s'arrete, comme avec la touche N). Les PIP, eux, restent : un PIP ordinaire (ceux du BOOT) s'arrete sur FlagExit,
            ' sauf demande contraire (03/11/2026 : « le pip ne marche pas au boot » en mode discret : il s'arretait a l'entree dans le mode).
            EpargnerLesPipAuProchainSignal()
            FlagExit = True
        End If
    End Sub

    Friend Sub Basculer()
        Definir(Not Actif)
    End Sub

    ''' <summary>Appelee par le diaporama avant chaque photo et chaque video : cache Slide et attend la fin du mode discret.</summary>
    Friend Sub Attendre()
        If Not Actif Then Exit Sub
        Try
            MontrerLIcone()
        Catch ex As Exception
            DiagLog("Mode discret : " & ex.Message)
        End Try
        FlagExit = False
        Do While Actif
            Application.DoEvents()
            ' La minuterie marche a 100 % (demande de Jacob, 03/11/2026) : tant qu'une tache montre quelque chose (film, chaine, PIP,
            ' message), Slide est affiche comme d'habitude ; des qu'elle est finie il redevient invisible.
            Dim voulu As Boolean = QuelqueChoseAMontrer()
            If voulu AndAlso _cache Then
                Montrer()
            ElseIf Not voulu AndAlso Not _cache Then
                Cacher()
            ElseIf Not voulu Then
                CacherLesFenetres()                      ' filet de securite : toute fenetre de Slide qui s'afficherait sans raison est recachee
            End If
            Threading.Thread.Sleep(100)
        Loop
        Montrer()
        Try
            If _icone IsNot Nothing Then _icone.Visible = False
        Catch ex As Exception
        End Try
        FlagExit = False
    End Sub

    ''' <summary>Vrai quand Slide est cache : mode discret ET rien a montrer pour l'instant.</summary>
    Friend ReadOnly Property Cache As Boolean
        Get
            Return Actif AndAlso _cache
        End Get
    End Property

    ' (Vrai des l'entree dans le mode : au demarrage, rien ne doit apparaitre avant la premiere tache.)
    Private _cache As Boolean = True
    Private _montrerJusqua As Long

    ''' <summary>A appeler au debut d'une tache de la minuterie qui affiche quelque chose (film, chaine, message) : en mode discret, Slide
    ''' s'affiche tout de suite, comme d'habitude, puis se recache quand la tache est finie. <paramref name="secondes"/> : delai avant que
    ''' la tache ne soit comptee comme « en cours » (TaskIsPlaying, PIP).</summary>
    Friend Sub Reveiller(Optional secondes As Double = 8, Optional pip As Boolean = False)
        If Not Actif Then Exit Sub
        If pip Then Exit Sub                             ' (un PIP s'affiche tout seul, SANS la fenetre de Slide : voir QuelqueChoseAMontrer)
        _montrerJusqua = Math.Max(_montrerJusqua, Environment.TickCount64 + CLng(secondes * 1000))
        If _cache Then
            Try
                If Form3.InvokeRequired Then Form3.BeginInvoke(New Action(AddressOf Montrer)) Else Montrer()
            Catch ex As Exception
            End Try
        End If
    End Sub

    ''' <summary>Zone dans laquelle les PIP se placent et se deplacent : l'image de la fenetre de Slide, et en mode discret fenetre cachee TOUT L'ECRAN
    ''' (demande de Jacob, 03/11/2026 : « afficher le pip n'importe ou sur l'ecran »). Appelee aussi depuis le fil d'un PIP.</summary>
    Friend Function ZoneDesPip(principale As Form) As Rectangle
        If principale Is Nothing Then Return Rectangle.Empty
        If Actif AndAlso (Not principale.Visible OrElse _cache) Then Return Screen.FromRectangle(principale.Bounds).Bounds
        Return principale.RectangleToScreen(principale.ClientRectangle)
    End Function

    ''' <summary>Vrai quand la fenetre de Slide doit etre affichee (tache plein ecran en cours). Les PIP ne comptent pas.</summary>
    Private Function QuelqueChoseAMontrer() As Boolean
        Try
            If Environment.TickCount64 < _montrerJusqua Then Return True
            ' Un PIP (fenetre a part, dans son propre fil) s'affiche SEUL, sans la fenetre de Slide ; la fenetre de Slide ne revient
            ' que pour une tache plein ecran (film, chaine, message) : demande de Jacob, 03/11/2026.
            If TaskIsPlaying AndAlso PipsActifs() = 0 Then Return True
        Catch ex As Exception
        End Try
        Return False
    End Function

    Private Sub Montrer()
        If Not _cache AndAlso Not Form3.IsDisposed AndAlso Form3.Visible Then Exit Sub
        _cache = False
        RemontrerLesFenetres()
        Try
            Form3.Show()
            MettreAuPremierPlan(Form3)
            AfficheMontre()
            If Actif Then DiagLog("Mode discret : Slide affiche (tache de la minuterie)")
        Catch ex As Exception
            DiagLog("Mode discret : fenetre non reaffichee (" & ex.Message & ")")
        End Try
    End Sub

    Private Sub Cacher()
        _cache = True
        Try
            Form3.Hide()
            AfficheMontre()                              ' (Cache : la montre se masque)
            DiagLog("Mode discret : tache finie, Slide de nouveau cache")
        Catch ex As Exception
            DiagLog("Mode discret : fenetre non cachee (" & ex.Message & ")")
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Filet de securite : en mode discret RIEN ne doit apparaitre a l'ecran (demande de Jacob, 03/11/2026). Les calques
    ' (annonces, pochette, horloge, legende) se cachent deja avec la fenetre principale ; ici on recache tout le reste
    ' (fenetre d'etat, mise a jour, telechargements...) a chaque tour de la boucle d'attente.
    ' ------------------------------------------------------------------------------------------

    Private ReadOnly _cachees As New List(Of IntPtr)
    Private ReadOnly _signalees As New HashSet(Of String)

    Private Delegate Function EnumFenetres(hwnd As IntPtr, lParam As IntPtr) As Boolean
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function EnumWindows(rappel As EnumFenetres, lParam As IntPtr) As Boolean
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function GetWindowThreadProcessId(hwnd As IntPtr, ByRef pid As UInteger) As UInteger
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function IsWindowVisible(hwnd As IntPtr) As Boolean
    End Function
    <Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Function GetCurrentThreadId() As UInteger
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function IsWindow(hwnd As IntPtr) As Boolean
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function GetWindowRect(hwnd As IntPtr, ByRef r As Rectangle) As Boolean
    End Function
    <Runtime.InteropServices.DllImport("user32.dll", CharSet:=Runtime.InteropServices.CharSet.Unicode)>
    Private Function GetClassName(hwnd As IntPtr, texte As Text.StringBuilder, max As Integer) As Integer
    End Function
    <Runtime.InteropServices.DllImport("user32.dll")>
    Private Function ShowWindow(hwnd As IntPtr, commande As Integer) As Boolean
    End Function

    ''' <summary>Les fenetres de CE programme visibles a l'ecran (hors menu de l'icone, infobulles et fenetres deja hors ecran) sont cachees.</summary>
    Private Sub CacherLesFenetres()
        Try
            If _icone IsNot Nothing AndAlso _icone.ContextMenuStrip IsNot Nothing AndAlso _icone.ContextMenuStrip.Visible Then Exit Sub
            Dim monPid As UInteger = CUInt(Environment.ProcessId)
            Dim monFil As UInteger = GetCurrentThreadId()           ' (les PIP ont chacun leur fil et leur fenetre : on n'y touche pas)
            Dim ecrans As Rectangle() = Screen.AllScreens.Select(Function(s) s.Bounds).ToArray()
            Dim aCacher As New List(Of IntPtr)
            EnumWindows(Function(h, p)
                            Dim pid As UInteger = 0
                            Dim fil As UInteger = GetWindowThreadProcessId(h, pid)
                            If pid <> monPid OrElse fil <> monFil OrElse Not IsWindowVisible(h) Then Return True
                            Dim r As Rectangle
                            If Not GetWindowRect(h, r) Then Return True
                            Dim rect As Rectangle = Rectangle.FromLTRB(r.Left, r.Top, r.Width, r.Height)       ' (GetWindowRect : Left, Top, Right, Bottom)
                            If rect.Width <= 1 OrElse rect.Height <= 1 Then Return True
                            If Not ecrans.Any(Function(e) e.IntersectsWith(rect)) Then Return True          ' (deja hors ecran : lecteur Deezer, fenetres techniques)
                            Dim classe As New Text.StringBuilder(64)
                            GetClassName(h, classe, 64)
                            Dim nomClasse As String = classe.ToString()
                            If nomClasse.StartsWith("tooltips_class") OrElse nomClasse = "#32768" OrElse nomClasse.Contains("IME") Then Return True
                            aCacher.Add(h)
                            Return True
                        End Function, IntPtr.Zero)
            For Each h As IntPtr In aCacher
                Dim c As Control = Control.FromHandle(h)
                Dim f As Form = TryCast(c, Form)
                Dim nom As String = If(f IsNot Nothing, f.GetType().Name & " """ & f.Text & """", "fenetre " & h.ToString())
                If _signalees.Add(nom) Then DiagLog("Mode discret : fenetre cachee -> " & nom)
                If f IsNot Nothing AndAlso Not f.Modal Then
                    f.Hide()
                Else
                    ShowWindow(h, 0)                       ' SW_HIDE (fenetre modale ou hors WinForms : on ne la ferme pas, on la cache)
                    If Not _cachees.Contains(h) Then _cachees.Add(h)
                End If
            Next
        Catch ex As Exception
            DiagLog("Mode discret : filet : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Fin du mode : les fenetres cachees d'office par Win32 (modales surtout) reviennent.</summary>
    Private Sub RemontrerLesFenetres()
        For Each h As IntPtr In _cachees
            Try
                If IsWindow(h) Then ShowWindow(h, 8)       ' SW_SHOWNA
            Catch ex As Exception
            End Try
        Next
        _cachees.Clear()
    End Sub

    Private Sub MontrerLIcone()
        If _icone Is Nothing Then
            Dim menu As New ContextMenuStrip()
            menu.Items.Add("Réafficher Slide (photos et vidéos)", Nothing, Sub(s, e) Definir(False))
            menu.Items.Add("Ouvrir les options", Nothing, Sub(s, e) OuvrirLesOptions())
            menu.Items.Add("-")
            menu.Items.Add("Quitter Slide", Nothing, Sub(s, e) Quitter())
            _icone = New NotifyIcon With {
                .Icon = If(Form3.Icon, SystemIcons.Application),
                .Text = "Slide : mode discret (musique seule)",
                .ContextMenuStrip = menu}
            AddHandler _icone.DoubleClick, Sub(s, e) Definir(False)
            AddHandler Application.ApplicationExit, Sub(s, e) EffacerLIcone()
        End If
        _icone.Visible = True
    End Sub

    ''' <summary>« Ouvrir les options » du menu de l'icone : meme fenetre que Option du menu de Slide (centree sur l'ecran), sans reafficher Slide.</summary>
    Friend Sub OuvrirLesOptions()
        Try
            If Form4.Visible Then Form4.Activate() : Exit Sub
            Form3.OptionToolStripMenuItem.PerformClick()
        Catch ex As Exception
            DiagLog("Mode discret : options : " & ex.Message)
        End Try
    End Sub

    Private Sub EffacerLIcone()
        Try
            If _icone IsNot Nothing Then _icone.Visible = False : _icone.Dispose() : _icone = Nothing
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>« Quitter Slide » du menu de l'icone : meme fermeture que le menu « Fin » (la fenetre est reaffichee pour la fenetre de fermeture). L'etat
    ''' « discret » est garde : Slide, relance, revient discret.</summary>
    Private Sub Quitter()
        EffacerLIcone()
        Try
            Form3.Show()
            Form3.EndToolStripMenuItem.PerformClick()
        Catch ex As Exception
            DiagLog("Mode discret : fermeture : " & ex.Message)
        End Try
        End
    End Sub

End Module
