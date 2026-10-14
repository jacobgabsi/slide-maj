Option Strict On
Option Explicit On

Imports System.Net
Imports System.Net.Http
Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Imports System.Security.Principal
Imports Microsoft.Win32

''' <summary>
''' Remise a l'heure du PC au lancement de Slide (remplace le programme externe data\SyncTime.exe).
'''
''' 1. L'heure exacte est demandee a un serveur de temps (NTP, precis a quelques millisecondes),
'''    et a defaut lue dans l'en-tete "Date" de https://google.com, comme le faisait SyncTime.
''' 2. Si l'horloge du PC s'en ecarte de plus de EcartTolere, elle est corrigee.
'''
''' Changer l'heure de Windows exige les droits d'administrateur. Depuis le 26/11/2026 Slide ne les
''' demande plus a chaque heure fausse : au premier lancement sur un PC, il demande UNE autorisation
''' pour poser une tache Windows qui remet ensuite l'heure toute seule (au demarrage puis chaque
''' heure). La demande n'est faite que si quelqu'un est devant le PC (jamais a la relance de nuit).
''' Refusee, elle revient au lancement au plus tot une semaine apres, et a chaque ouverture des
''' options tant que la tache n'est pas en place. Slide ne corrige l'horloge lui-meme que s'il est
''' deja administrateur ; sinon il note l'ecart dans le journal.
''' </summary>
Public Module HeureDuPC

    ''' <summary>En dessous, on ne touche a rien (l'en-tete HTTP n'est precis qu'a la seconde).</summary>
    Private ReadOnly EcartTolere As TimeSpan = TimeSpan.FromSeconds(30)

    Private ReadOnly ServeursNtp As String() = {"time.windows.com", "pool.ntp.org", "fr.pool.ntp.org"}

    ''' <summary>Au lancement de Slide : pose la tache Windows si elle manque (premier lancement),
    ''' puis verifie l'horloge.</summary>
    Public Sub VerifierHeureAuLancement()
        DemanderHeureAutomatique()
        Dim source As String = ""
        Dim exacte As DateTime? = HeureExacteUtc(source)
        If Not exacte.HasValue Then
            DiagLog("Heure du PC : pas de reference (ni NTP ni Internet), heure laissee telle quelle")
            Exit Sub
        End If

        Dim ecart As TimeSpan = exacte.Value - DateTime.UtcNow
        If ecart.Duration() <= EcartTolere Then
            DiagLog("Heure du PC : juste (ecart " & Math.Round(ecart.TotalSeconds, 1) & " s, " & source & ")")
            Exit Sub
        End If

        If EstAdministrateur() Then
            DiagLog("Heure du PC : fausse de " & Math.Round(ecart.TotalSeconds) & " s (" & source & ") -> correction")
            ReglerHorloge()
        Else
            DiagLog("Heure du PC : fausse de " & Math.Round(ecart.TotalSeconds) & " s (" & source & ") -> non corrigee par Slide ; " &
                    If(HeureAutomatiqueEnPlace(), "la tache Windows « " & NomTache & " » s'en charge", "tache Windows pas (encore) autorisee sur ce PC"))
        End If
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Heure automatique : une tache Windows, posee une seule fois avec l'autorisation
    ' ------------------------------------------------------------------------------------------

    Private Const NomTache As String = "Slide - Heure du PC"

    ''' <summary>La tache a-t-elle ete posee sur ce PC ? (Marque lisible sans droits : la tache
    ''' elle-meme, qui appartient au compte Systeme, ne l'est pas forcement.)</summary>
    Private Function HeureAutomatiqueEnPlace() As Boolean
        Try
            Using racine As RegistryKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64)
                Using cle As RegistryKey = racine.OpenSubKey("SOFTWARE\Slide")
                    Return cle IsNot Nothing AndAlso cle.GetValue("HeureAutomatique") IsNot Nothing
                End Using
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' Par PC et par utilisateur Windows (le registre n'est ni copie par les mises a jour ni exporte avec les reglages).
    Private Const CleDemande As String = "SOFTWARE\Slide"
    Private Const ValeurDemande As String = "HeureAutomatiqueDemandee"

    ''' <summary>Premier lancement sur ce PC : demande l'autorisation sans retarder le demarrage.
    ''' Rien n'est note tant que personne n'a repondu : la demande revient au lancement suivant.</summary>
    Private Sub DemanderHeureAutomatique()
        Try
            If HeureAutomatiqueEnPlace() Then Exit Sub
            Using cle As RegistryKey = Registry.CurrentUser.OpenSubKey(CleDemande)
                Dim quand As Date
                If cle IsNot Nothing AndAlso Date.TryParseExact(TryCast(cle.GetValue(ValeurDemande), String), "yyyy-MM-dd",
                        Globalization.CultureInfo.InvariantCulture, Globalization.DateTimeStyles.None, quand) AndAlso
                        Math.Abs((Date.Today - quand).TotalDays) < 7 Then Exit Sub      ' (Abs : l'horloge peut etre fausse)
            End Using
            If Not QuelquUnEstLa() Then
                DiagLog("Heure du PC : personne devant le PC, l'autorisation sera demandee a un prochain lancement")
                Exit Sub
            End If
            DemanderQuandSlideEstDevant()
        Catch ex As Exception
            DiagLog("Heure du PC : demande d'autorisation impossible (" & ex.Message & ")")
        End Try
    End Sub

    Private _attente As System.Windows.Forms.Timer
    Private _secondes As Integer

    ''' <summary>Au lancement, la demande attend (8 s au moins) qu'une fenetre de Slide soit au premier plan : demandee trop tot, Windows
    ''' ne montrait pas la sienne, il la faisait seulement clignoter dans la barre des taches (journal du 26/11/2026 : trois demandes sans
    ''' reponse). Appelee depuis le fil de l'interface.</summary>
    Private Sub DemanderQuandSlideEstDevant()
        _secondes = 0
        _attente = New System.Windows.Forms.Timer With {.Interval = 1000}
        AddHandler _attente.Tick,
            Sub()
                _secondes += 1
                Dim devant As IntPtr = GetForegroundWindow()
                Dim processus As UInteger = 0UI
                If devant <> IntPtr.Zero Then GetWindowThreadProcessId(devant, processus)
                If _secondes >= 8 AndAlso processus = CUInt(Environment.ProcessId) Then
                    _attente.Stop()
                    LancerLaDemande("au lancement", devant)
                ElseIf _secondes >= 180 Then
                    _attente.Stop()
                    DiagLog("Heure du PC : Slide n'est pas au premier plan, l'autorisation sera demandee plus tard (ou a l'ouverture des options)")
                End If
            End Sub
        _attente.Start()
    End Sub

    ''' <summary>A chaque ouverture des options, tant que la tache n'est pas en place : la personne est
    ''' devant le PC, on demande sans delai d'une semaine.</summary>
    Public Sub DemanderHeureAutomatiqueAuxOptions(fenetre As IntPtr)
        If Not HeureAutomatiqueEnPlace() Then LancerLaDemande("a l'ouverture des options", fenetre)
    End Sub

    Private DemandeEnCours As Integer        ' une seule fenetre d'autorisation a la fois

    ''' <summary><paramref name="fenetre"/> : la fenetre de Slide au premier plan, a laquelle Windows rattache sa demande (elle s'affiche alors devant).</summary>
    Private Sub LancerLaDemande(quand As String, fenetre As IntPtr)
        If Threading.Interlocked.Exchange(DemandeEnCours, 1) = 1 Then Exit Sub
        DiagLog("Heure du PC : demande de l'autorisation pour la tache Windows (" & quand & ")")
        Threading.Tasks.Task.Run(
            Sub()
                Try
                    Dim chrono As Stopwatch = Stopwatch.StartNew()
                    ' Windows referme seul sa fenetre au bout de 2 minutes : ce n'est pas un refus.
                    If Not InstallerHeureAutomatique(fenetre) AndAlso chrono.Elapsed.TotalSeconds < 100 Then NoterLaDemande()
                Finally
                    Threading.Interlocked.Exchange(DemandeEnCours, 0)
                End Try
            End Sub)
    End Sub

    Private Sub NoterLaDemande()
        Try
            Using cle As RegistryKey = Registry.CurrentUser.CreateSubKey(CleDemande)
                cle.SetValue(ValeurDemande, Date.Today.ToString("yyyy-MM-dd", Globalization.CultureInfo.InvariantCulture))
            End Using
        Catch ex As Exception
            DiagLog("Heure du PC : refus non note (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Clavier ou souris utilises depuis moins de 3 minutes.</summary>
    Private Function QuelquUnEstLa() As Boolean
        Dim info As New LASTINPUTINFO With {.cbSize = 8UI}
        If Not GetLastInputInfo(info) Then Return False
        Dim inactif As Long = ((Environment.TickCount64 And &HFFFFFFFFL) - CLng(info.dwTime)) And &HFFFFFFFFL
        Return inactif < 180000L
    End Function

    ''' <summary>Regle Windows pour qu'il garde l'heure juste tout seul (une demande d'autorisation) :
    ''' service de temps en demarrage automatique, correction acceptee quel que soit l'ecart, et tache
    ''' du compte Systeme au demarrage puis chaque heure. La tache ne lance que des programmes de
    ''' Windows (jamais Slide). Le script passe par la ligne de commande : aucun fichier qu'un autre
    ''' programme pourrait modifier avant son execution. Vrai si tout est en place (le detail va au journal).</summary>
    Private Function InstallerHeureAutomatique(fenetre As IntPtr) As Boolean
        ' Apostrophes seulement : le script entier est entre guillemets sur la ligne de commande.
        ' Le code de sortie donne l'etape qui a echoue (voir EtapeDuReglage).
        Dim script As String() = {
            "$ErrorActionPreference = 'Stop'",
            "$etape = 1",
            "try {",
            "$sys = $env:SystemRoot + '\System32\'",
            "$conf = 'HKLM\SYSTEM\CurrentControlSet\Services\W32Time\Config'",
            "Set-Service -Name w32time -StartupType Automatic",
            "Start-Service -Name w32time",
            "$etape = 2",
            "& ($sys + 'reg.exe') add $conf /v MaxPosPhaseCorrection /t REG_DWORD /d 4294967295 /f",
            "& ($sys + 'reg.exe') add $conf /v MaxNegPhaseCorrection /t REG_DWORD /d 4294967295 /f",
            "if ((Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Services\W32Time\Parameters').Type -eq 'NoSync') { & ($sys + 'w32tm.exe') /config '/manualpeerlist:time.windows.com,0x9' /syncfromflags:manual }",
            "& ($sys + 'w32tm.exe') /config /update",
            "$etape = 3",
            "$action = New-ScheduledTaskAction -Execute ($sys + 'cmd.exe') -Argument ('/c ' + $sys + 'net.exe start w32time & ' + $sys + 'w32tm.exe /resync /force')",
            "$demarrage = New-ScheduledTaskTrigger -AtStartup",
            "$demarrage.Delay = 'PT1M'",
            "$chaqueHeure = New-ScheduledTaskTrigger -Once -At '2020-01-01T00:00:00' -RepetitionInterval (New-TimeSpan -Hours 1)",
            "$reglages = New-ScheduledTaskSettingsSet -StartWhenAvailable -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries -ExecutionTimeLimit (New-TimeSpan -Minutes 5)",
            "Register-ScheduledTask -TaskName '" & NomTache & "' -Action $action -Trigger $demarrage, $chaqueHeure -Settings $reglages -User 'SYSTEM' -RunLevel Highest -Force | Out-Null",
            "$etape = 4",
            "& ($sys + 'reg.exe') add 'HKLM\SOFTWARE\Slide' /v HeureAutomatique /t REG_DWORD /d 1 /f /reg:64",
            "& ($sys + 'w32tm.exe') /resync /force",
            "exit 0",
            "} catch { exit $etape }"}
        Try
            Dim powershell As String = IO.Path.Combine(Environment.SystemDirectory, "WindowsPowerShell\v1.0\powershell.exe")
            Dim psi As New ProcessStartInfo(powershell, "-NoProfile -NonInteractive -Command """ & String.Join("; ", script) & """") With {
                .UseShellExecute = True, .Verb = "runas", .WindowStyle = ProcessWindowStyle.Hidden,
                .ErrorDialog = True, .ErrorDialogParentHandle = fenetre}
            Using p As Process = Process.Start(psi)
                DiagLog("Heure du PC : autorisation accordee, reglage en cours")
                If p Is Nothing OrElse Not p.WaitForExit(90000) Then
                    DiagLog("Heure du PC : le reglage de la tache Windows ne s'est pas termine")
                    Return False
                End If
                If p.ExitCode <> 0 Then
                    DiagLog("Heure du PC : tache Windows non posee (code " & p.ExitCode & " : " & EtapeDuReglage(p.ExitCode) & ")")
                    Return False
                End If
            End Using
        Catch ex As ComponentModel.Win32Exception When ex.NativeErrorCode = 1223
            DiagLog("Heure du PC : autorisation refusee ou sans reponse, tache Windows non posee")
            Return False
        Catch ex As Exception
            DiagLog("Heure du PC : tache Windows non posee (" & ex.Message & ")")
            Return False
        End Try
        DiagLog("Heure du PC : tache Windows « " & NomTache & " » posee (au demarrage et chaque heure)")
        Return True
    End Function

    Private Function EtapeDuReglage(code As Integer) As String
        Select Case code
            Case 1 : Return "service de l'heure"
            Case 2 : Return "reglage de l'heure de Windows"
            Case 3 : Return "creation de la tache planifiee"
            Case 4 : Return "enregistrement final"
            Case Else : Return "inconnue"
        End Select
    End Function

    <DllImport("user32.dll")>
    Private Function GetForegroundWindow() As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Function GetWindowThreadProcessId(fenetre As IntPtr, ByRef processus As UInteger) As UInteger
    End Function

    ''' <summary>Slide deja administrateur : relit l'heure exacte au dernier moment puis corrige.</summary>
    Private Function ReglerHorloge() As Boolean
        Dim source As String = ""
        Dim exacte As DateTime? = HeureExacteUtc(source)
        If Not exacte.HasValue Then
            DiagLog("Heure du PC : pas de reference au moment de corriger")
            Return False
        End If
        Dim t As DateTime = exacte.Value
        Dim st As New SYSTEMTIME With {
            .wYear = CUShort(t.Year), .wMonth = CUShort(t.Month), .wDay = CUShort(t.Day),
            .wDayOfWeek = CUShort(t.DayOfWeek), .wHour = CUShort(t.Hour), .wMinute = CUShort(t.Minute),
            .wSecond = CUShort(t.Second), .wMilliseconds = CUShort(t.Millisecond)}
        ' SetSystemTime attend l'heure UTC ; il active lui-meme le privilege "changer l'heure".
        If SetSystemTime(st) Then
            DiagLog("Heure du PC : corrigee (" & source & ")")
            Return True
        End If
        DiagLog("Heure du PC : correction refusee par Windows (erreur " & Marshal.GetLastWin32Error() & ")")
        Return False
    End Function

    Private Function EstAdministrateur() As Boolean
        Try
            Using identite As WindowsIdentity = WindowsIdentity.GetCurrent()
                Return New WindowsPrincipal(identite).IsInRole(WindowsBuiltInRole.Administrator)
            End Using
        Catch ex As Exception
            Return False
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Heure exacte
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Heure exacte en UTC, ou Nothing sans reseau. <paramref name="source"/> dit d'ou
    ''' elle vient (pour le journal).</summary>
    Public Function HeureExacteUtc(ByRef source As String) As DateTime?
        For Each serveur As String In ServeursNtp
            Dim t As DateTime? = HeureNtp(serveur)
            If t.HasValue Then source = "NTP " & serveur : Return t
        Next
        Dim h As DateTime? = HeureHttp()
        If h.HasValue Then source = "en-tete HTTP de google.com" : Return h
        Return Nothing
    End Function

    ''' <summary>Requete SNTP (RFC 4330) : 48 octets, reponse avec l'heure d'emission du serveur,
    ''' corrigee de la moitie du temps d'aller-retour.</summary>
    Private Function HeureNtp(serveur As String) As DateTime?
        Try
            Dim paquet(47) As Byte
            paquet(0) = &H1B                       ' pas d'alerte, version 3, mode client
            Using udp As New UdpClient()
                udp.Client.ReceiveTimeout = 2000
                udp.Connect(serveur, 123)
                Dim chrono As Stopwatch = Stopwatch.StartNew()
                udp.Send(paquet, paquet.Length)
                Dim distant As IPEndPoint = Nothing
                Dim reponse As Byte() = udp.Receive(distant)
                chrono.Stop()
                If reponse.Length < 48 Then Return Nothing
                ' Heure d'emission : octets 40 a 47, secondes et fraction depuis le 1er janvier 1900.
                Dim secondes As ULong = LireGrandBoutiste(reponse, 40)
                Dim fraction As ULong = LireGrandBoutiste(reponse, 44)
                If secondes = 0UL Then Return Nothing
                Dim ms As Double = secondes * 1000.0 + fraction * 1000.0 / 4294967296.0
                Dim t As DateTime = New DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMilliseconds(ms)
                Return t.AddMilliseconds(chrono.Elapsed.TotalMilliseconds / 2)
            End Using
        Catch ex As Exception
            Return Nothing                         ' serveur injoignable, port 123 bloque...
        End Try
    End Function

    Private Function LireGrandBoutiste(octets As Byte(), debut As Integer) As ULong
        Return (CULng(octets(debut)) << 24) Or (CULng(octets(debut + 1)) << 16) Or
               (CULng(octets(debut + 2)) << 8) Or CULng(octets(debut + 3))
    End Function

    ''' <summary>Comme l'ancien SyncTime : l'en-tete "Date" d'une reponse de google.com (precis a la
    ''' seconde).</summary>
    Private Function HeureHttp() As DateTime?
        Try
            Using client As New HttpClient With {.Timeout = TimeSpan.FromSeconds(5)}
                Using reponse As HttpResponseMessage =
                    client.GetAsync("https://google.com", HttpCompletionOption.ResponseHeadersRead).Result
                    If reponse.Headers.Date.HasValue Then Return reponse.Headers.Date.Value.UtcDateTime
                End Using
            End Using
        Catch ex As Exception
        End Try
        Return Nothing
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Windows
    ' ------------------------------------------------------------------------------------------

    <StructLayout(LayoutKind.Sequential)>
    Private Structure SYSTEMTIME
        Public wYear As UShort
        Public wMonth As UShort
        Public wDayOfWeek As UShort
        Public wDay As UShort
        Public wHour As UShort
        Public wMinute As UShort
        Public wSecond As UShort
        Public wMilliseconds As UShort
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure LASTINPUTINFO
        Public cbSize As UInteger
        Public dwTime As UInteger
    End Structure

    <DllImport("user32.dll")>
    Private Function GetLastInputInfo(ByRef info As LASTINPUTINFO) As Boolean
    End Function

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function SetSystemTime(ByRef heure As SYSTEMTIME) As Boolean
    End Function

End Module
