
Module Module4
    Declare Auto Function SetForegroundWindow Lib "User32.dll" (ByVal Hwnd As IntPtr) As Long
    Declare Auto Function FindWindowEx Lib "User32.dll" (ByVal hwndParent As IntPtr, ByVal hwndChildAfter As IntPtr, ByVal lpszClass As String, ByVal lpszWindow As String) As IntPtr
    Declare Auto Function SetWindowPos Lib "User32.dll" (ByVal hWnd As Long, ByVal hWndInsertAfter As Long, ByVal X As Long, ByVal Y As Long, ByVal cx As Long, ByVal cy As Long, ByVal wFlags As Long)
    Const HWND_TOPMOST = -1
    Const HWND_NOTOPMOST = -2
    Const SWP_NOSIZE = &H1
    Const SWP_NOMOVE = &H2
    Const SWP_NOACTIVATE = &H10
    Const SWP_SHOWWINDOW = &H40

    Declare Auto Function ShowWindow Lib "User32.dll" (handle As IntPtr, nCmdShow As Integer) As Boolean
    'Declare Auto Function IsIconic Lib "User32.dll" (handle As IntPtr) As Boolean

    'pour lister toute les fenetres
    Delegate Function EnumWindowDelegate(ByVal hWnd As IntPtr, ByVal Lparam As IntPtr) As Boolean
    Public Callback As EnumWindowDelegate = New EnumWindowDelegate(AddressOf EnumWindowProc)
    Public Callback2 As EnumWindowDelegate = New EnumWindowDelegate(AddressOf EnumWindowProc2)
    Public Declare Function EnumWindows Lib "User32.dll" (ByVal WNDENUMPROC As EnumWindowDelegate, ByVal lparam As IntPtr) As Boolean
    Public Declare Function EnumWindows2 Lib "User32.dll" Alias "EnumWindows" (ByVal WNDENUMPROC As EnumWindowDelegate, ByVal lparam As IntPtr) As Boolean

    ' Titre d'une fenetre : version Unicode avec un StringBuilder dont la capacite est passee
    ' telle quelle. L'ancienne declaration (Auto = GetWindowTextW, dans un Byte() de
    ' 2*longueur+1 octets pour longueur+1 caracteres) ecrivait UN OCTET APRES le tableau, dans
    ' le tas de .NET : corruption silencieuse, puis ExecutionEngineException des heures plus
    ' tard (au passage du ramasse-miettes), sans pile d'appels.
    <System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="GetWindowTextW", CharSet:=System.Runtime.InteropServices.CharSet.Unicode)>
    Private Function GetWindowTextW(hWnd As IntPtr, texte As System.Text.StringBuilder, capacite As Integer) As Integer
    End Function
    <System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="GetWindowTextLengthW")>
    Private Function GetWindowTextLengthW(hWnd As IntPtr) As Integer
    End Function
    Public Declare Function IsWindowVisible Lib "User32.dll" (ByVal hwnd As IntPtr) As Boolean

    ' ------------------------------------------------------------------------------------------
    ' Mise au premier plan (demarrage de Slide)
    ' ------------------------------------------------------------------------------------------
    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Function AttachThreadInput(idAttache As Integer, idA As Integer, attacher As Boolean) As Boolean
    End Function
    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Function GetCurrentThreadId() As Integer
    End Function
    <System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="GetForegroundWindow")>
    Private Function FenetreActive() As IntPtr
    End Function
    <System.Runtime.InteropServices.DllImport("user32.dll", EntryPoint:="SetForegroundWindow")>
    Private Function ActiverFenetre(hWnd As IntPtr) As Boolean
    End Function
    <System.Runtime.InteropServices.DllImport("user32.dll")>
    Private Function BringWindowToTop(hWnd As IntPtr) As Boolean
    End Function

    ''' <summary>
    ''' Met la fenetre devant tous les programmes et lui donne le clavier. Windows interdit
    ''' normalement a un programme de "voler" le premier plan : on s'attache un instant au fil de la
    ''' fenetre active (methode habituelle), puis on active ; un passage eclair en "toujours
    ''' visible" force de toute facon l'ordre d'affichage.
    ''' </summary>
    Sub MettreAuPremierPlan(f As Form)
        Try
            If f Is Nothing OrElse f.IsDisposed Then Exit Sub
            If f.WindowState = FormWindowState.Minimized Then f.WindowState = FormWindowState.Normal
            If Not f.Visible Then f.Show()
            Dim active As IntPtr = FenetreActive()
            Dim filActif As Integer = 0
            If active <> IntPtr.Zero Then filActif = GetWindowThreadProcessId(active, 0)
            Dim monFil As Integer = GetCurrentThreadId()
            Dim attache As Boolean = filActif <> 0 AndAlso filActif <> monFil AndAlso AttachThreadInput(monFil, filActif, True)
            Try
                BringWindowToTop(f.Handle)
                ActiverFenetre(f.Handle)
                f.Activate()
            Finally
                If attache Then AttachThreadInput(monFil, filActif, False)
            End Try
            Dim etaitDevant As Boolean = f.TopMost
            f.TopMost = True
            f.TopMost = etaitDevant
            Application.DoEvents()
        Catch ex As Exception
            DiagLog("Premier plan : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Titre de la fenetre (vide si aucun).</summary>
    Function TitreFenetre(hWnd As IntPtr) As String
        Dim longueur As Integer = GetWindowTextLengthW(hWnd)
        If longueur <= 0 Then Return ""
        Dim texte As New System.Text.StringBuilder(longueur + 1)
        GetWindowTextW(hWnd, texte, texte.Capacity)
        Return texte.ToString()
    End Function

    Public Declare Auto Function FindWindow Lib "user32.dll" (ByVal lpClassName As String, ByVal lpWindowName As String) As Integer

    Declare Auto Function SetParent Lib "user32.dll" (ByVal hWndChild As IntPtr, ByVal hWndNewParent As IntPtr) As Integer
    Declare Auto Function SendMessage Lib "user32.dll" (ByVal hWnd As IntPtr, ByVal Msg As Integer, ByVal wParam As Integer, ByVal lParam As Integer) As Integer
    Public Const WM_SYSCOMMAND As Integer = 274
    Public Const SC_MAXIMIZE As Integer = 61488
    Public Declare Function GetWindowRect Lib "user32" _
                (ByVal hwnd As IntPtr,
                ByRef lpRect As RECT) _
                As Integer
    Public Declare Function GetActiveWindow Lib "user32" Alias "GetActiveWindow" () As IntPtr
    Public Declare Function WindowFromPoint Lib "user32" (ByVal pt As PointAPI) As IntPtr
    Public Declare Function GetForegroundWindow Lib "user32.dll" () As Long
    Public Declare Function IsWindow Lib "user32.dll" (ByVal hWnd As Long) As Long
    Public Declare Function WaitMessage Lib "user32.dll" () As Long

    Public Structure PointAPI

        Public X As Integer
        Public Y As Integer

        Public Sub New(ByVal x As Integer, ByVal y As Integer)
            Me.X = x
            Me.Y = y
        End Sub

    End Structure


    'Public Declare Function SetWindowPos Lib "user32" (ByVal hwnd As Long, ByVal hWndInsertAfter As Long, ByVal x As Long, ByVal y As Long, ByVal cx As Long, ByVal cy As Long, ByVal wFlags As Long) As Long






    ' Method 1
    Sub StartOrShowProcess(ByVal strProcessName As String)
        Try
            Dim handle As IntPtr
            Dim proc As Process() = Process.GetProcessesByName(strProcessName)
            If proc.Count > 0 Then
                For Each procP As Process In proc
                    handle = procP.MainWindowHandle
                    ' Do we have handle and minimized or not minimized?
                    If handle <> 0 Then
                        ShowWindow(handle, 9)
                        SetForegroundWindow(handle)
                    End If

                Next
            Else 'Not running or started...
                Process.Start(strProcessName)
            End If
        Catch ex As Exception
            'Handle your error...
        End Try


    End Sub


    ' ------------------------------------------------------------------------------------------
    ' Plein ecran : les programmes lances APRES LE PASSAGE EN PLEIN ECRAN sont fermes, ceux
    ' d'avant sont gardes.
    '
    ' Avant, Slide retenait les TITRES des fenetres au demarrage et tuait toute fenetre dont le
    ' titre n'y etait pas. Mais un programme deja ouvert change de titre (Outlook et ses messages,
    ' Edge et l'onglet en cours, Deezer et le morceau, AnyDesk...) : il etait tue quand meme, et
    ' des elements de Windows aussi ("Hote de l'experience Windows Shell", "Parametres"...).
    ' On retient maintenant les PROCESSUS (numero + heure de lancement) a l'entree en plein
    ' ecran : une fenetre n'est fermee que si son programme a demarre apres, quel que soit son
    ' titre. En mode fenetre, rien n'est ferme ; au retour en plein ecran, nouvel instantane.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Processus presents a l'entree en plein ecran : numero -> heure de demarrage
    ''' (un numero peut etre reutilise plus tard par un autre programme).</summary>
    Public ProcessusAuDemarrage As New Dictionary(Of Integer, Date)
    Public DemarrageDeSlide As Date = Date.MaxValue     ' en fait : debut du plein ecran
    ''' <summary>Programmes ouverts par la tache "Lancer" : jamais fermes (voir LancerUnProgramme).</summary>
    Public ProcessusLancesParSlide As New HashSet(Of Integer)
    Private _pleinEcranSuivi As Boolean

    ''' <summary>A appeler quand le plein ecran commence ou finit (menu, reglage au demarrage,
    ''' minuterie par securite). L'instantane n'est pris qu'a l'ENTREE en plein ecran.</summary>
    Sub PleinEcranChange(actif As Boolean)
        If actif AndAlso Not _pleinEcranSuivi Then
            RetenirLesProcessusAuDemarrage()
            DiagLog("Plein ecran : " & ProcessusAuDemarrage.Count & " processus gardes, les programmes lances ensuite seront fermes")
        End If
        _pleinEcranSuivi = actif
    End Sub

    ''' <summary>Processus de Windows a ne jamais fermer, meme lances apres Slide.</summary>
    Private ReadOnly ProcessusProteges As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase) From {
        "explorer", "ShellExperienceHost", "StartMenuExperienceHost", "SearchHost", "SearchApp",
        "ApplicationFrameHost", "TextInputHost", "LockApp", "SystemSettings", "ShellHost",
        "dwm", "csrss", "winlogon", "sihost", "ctfmon", "taskmgr", "consent", "LogonUI",
        "SecurityHealthSystray", "smartscreen", "RuntimeBroker", "PeopleExperienceHost",
        "WidgetBoard", "Widgets", "msedgewebview2"}

    ''' <summary>Heure de lancement du processus, ou Nothing si Windows refuse de la donner
    ''' (programme systeme ou administrateur : on ne le touche pas).</summary>
    ''' <remarks>Demandee directement a Windows, SANS exception. Avant : p.StartTime dans un
    ''' Try/Catch. Sur 355 processus, Windows en refuse 192 : 192 exceptions a chaque entree en
    ''' plein ecran. Mesure du 07/10 : 5 ms hors de Visual Studio, mais 4,5 a 5,4 s sous Visual
    ''' Studio (F5), ou chaque exception coute environ 25 ms. Slide restait fige pendant ce temps,
    ''' juste apres avoir agrandi sa fenetre : la video restait petite dans l'ecran noir.</remarks>
    Private Function HeureDeLancement(p As Process) As Date?
        Dim poignee As IntPtr = OpenProcess(PROCESS_QUERY_LIMITED_INFORMATION, False, p.Id)
        If poignee = IntPtr.Zero Then Return Nothing
        Try
            Dim creation, fin, noyau, utilisateur As Long
            If Not GetProcessTimes(poignee, creation, fin, noyau, utilisateur) OrElse creation <= 0 Then Return Nothing
            Return Date.FromFileTime(creation)
        Finally
            CloseHandle(poignee)
        End Try
    End Function

    Private Const PROCESS_QUERY_LIMITED_INFORMATION As Integer = &H1000

    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Function OpenProcess(acces As Integer, heritage As Boolean, pid As Integer) As IntPtr
    End Function

    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Function GetProcessTimes(processus As IntPtr, ByRef creation As Long, ByRef fin As Long, ByRef noyau As Long, ByRef utilisateur As Long) As Boolean
    End Function

    <System.Runtime.InteropServices.DllImport("kernel32.dll")>
    Private Function CloseHandle(poignee As IntPtr) As Boolean
    End Function

    Function EnumWindowProc(ByVal hWnd As IntPtr, ByVal Lparam As IntPtr) As Boolean
        If IsWindowVisible(hWnd) Then
            Dim TheText As String = TitreFenetre(hWnd)
            If TheText <> "" And TheText <> "l" Then
                WindowsList.Add(TheText)
                'Debug.Print(TheText)
            End If
        End If
        Return True
    End Function

    ''' <summary>A l'entree en plein ecran : retient tous les processus en cours.</summary>
    Sub RetenirLesProcessusAuDemarrage()
        ProcessusAuDemarrage.Clear()
        DemarrageDeSlide = Now
        For Each p As Process In Process.GetProcesses()
            Dim heure As Date? = HeureDeLancement(p)
            ProcessusAuDemarrage(p.Id) = If(heure, Date.MinValue)
            p.Dispose()
        Next
    End Sub

    ''' <summary>Vrai si la fenetre appartient a un programme lance apres le passage en plein
    ''' ecran, qu'on peut fermer.</summary>
    Private Function ProgrammeLanceApresSlide(hWnd As IntPtr, ByRef nomProcessus As String) As Boolean
        If Not _pleinEcranSuivi Then Return False                                ' pas d'instantane : rien a fermer
        Dim pid As Integer = 0
        GetWindowThreadProcessId(hWnd, pid)
        If pid = 0 OrElse pid = Environment.ProcessId Then Return False          ' fenetres de Slide
        If ProcessusLancesParSlide.Contains(pid) Then Return False               ' tache "Lancer"
        Try
            Using p As Process = Process.GetProcessById(pid)
                nomProcessus = p.ProcessName
                If ProcessusProteges.Contains(p.ProcessName) Then Return False
                Dim heure As Date? = HeureDeLancement(p)
                If heure Is Nothing Then Return False                             ' inaccessible : on n'y touche pas
                Dim connu As Date
                If ProcessusAuDemarrage.TryGetValue(pid, connu) AndAlso (connu = Date.MinValue OrElse connu = heure.Value) Then Return False
                Return heure.Value > DemarrageDeSlide
            End Using
        Catch ex As Exception
            Return False                                                          ' deja ferme entre-temps
        End Try
    End Function

    Function EnumWindowProc2(ByVal hWnd As IntPtr, ByVal Lparam As IntPtr) As Boolean

        If Form4.Visible Then Return False
        If IsWindowVisible(hWnd) Then
            Dim TheText As String = TitreFenetre(hWnd)
            If TheText <> "" And TheText <> "l" Then
                nom = TheText

                ' Exceptions voulues (lecteurs lances par Slide lui-meme, prise en main a distance...).
                If UCase(nom).contains("SLIDE") Then nom = ""
                If UCase(nom).contains("VOLET TEAMVIEWER") Then nom = ""
                If UCase(nom).contains("PRIME VIDEO") Then nom = ""
                If UCase(nom).contains("NETFLIX") Then nom = ""
                If UCase(nom).contains("DEEZER") Then
                    If Music = True And Music_Deezer = True Then nom = ""
                End If

                Dim nomProcessus As String = ""
                If nom <> "" AndAlso ProgrammeLanceApresSlide(hWnd, nomProcessus) Then
                    SaveLogError(nom & " [" & nomProcessus & "]", "Kill by Slide because its new App")
                    KillProcessByHandle(hWnd)
                End If

            End If
        End If
        Return True
    End Function





End Module
