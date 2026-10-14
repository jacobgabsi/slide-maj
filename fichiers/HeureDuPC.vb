Option Strict On
Option Explicit On

Imports System.Net
Imports System.Net.Http
Imports System.Net.Sockets
Imports System.Runtime.InteropServices
Imports System.Security.Principal

''' <summary>
''' Remise a l'heure du PC au lancement de Slide (remplace le programme externe data\SyncTime.exe).
'''
''' 1. L'heure exacte est demandee a un serveur de temps (NTP, precis a quelques millisecondes),
'''    et a defaut lue dans l'en-tete "Date" de https://google.com, comme le faisait SyncTime.
''' 2. Si l'horloge du PC s'en ecarte de plus de EcartTolere, elle est corrigee.
'''
''' Changer l'heure de Windows exige les droits d'administrateur. Slide ne les a pas : il se
''' relance donc lui-meme en administrateur (demande Windows "Autoriser ?", comme SyncTime avant)
''' avec l'argument /reglerheure. Cette seconde instance ne fait QUE remettre l'heure et se ferme
''' (voir ApplicationEvents) ; la demande n'apparait que si l'heure est vraiment fausse.
''' </summary>
Public Module HeureDuPC

    ''' <summary>Argument de la seconde instance, lancee en administrateur.</summary>
    Public Const ArgumentReglerHeure As String = "/reglerheure"

    ''' <summary>En dessous, on ne touche a rien (l'en-tete HTTP n'est precis qu'a la seconde).</summary>
    Private ReadOnly EcartTolere As TimeSpan = TimeSpan.FromSeconds(30)

    Private ReadOnly ServeursNtp As String() = {"time.windows.com", "pool.ntp.org", "fr.pool.ntp.org"}

    ''' <summary>Au lancement de Slide : verifie l'horloge et la fait corriger si besoin.</summary>
    Public Sub VerifierHeureAuLancement()
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

        DiagLog("Heure du PC : fausse de " & Math.Round(ecart.TotalSeconds) & " s (" & source & ") -> correction")
        If EstAdministrateur() Then
            ReglerHorloge()
        Else
            RelancerEnAdministrateur()
        End If
    End Sub

    ''' <summary>Seconde instance (/reglerheure, en administrateur) : relit l'heure exacte au
    ''' dernier moment - l'utilisateur a pu mettre du temps a accepter - puis corrige.</summary>
    Public Function ReglerHorloge() As Boolean
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

    Private Sub RelancerEnAdministrateur()
        Try
            Dim demarrage As New ProcessStartInfo(Environment.ProcessPath, ArgumentReglerHeure) With {
                .UseShellExecute = True,
                .Verb = "runas",
                .WorkingDirectory = AppPath}
            Using p As Process = Process.Start(demarrage)
                ' Le temps de repondre a Windows ; on ne bloque jamais le diaporama au-dela.
                If p IsNot Nothing AndAlso Not p.WaitForExit(90000) Then
                    DiagLog("Heure du PC : pas de reponse a la demande d'autorisation apres 90 s")
                End If
            End Using
        Catch ex As ComponentModel.Win32Exception When ex.NativeErrorCode = 1223
            DiagLog("Heure du PC : autorisation refusee, heure non corrigee")
        Catch ex As Exception
            DiagLog("Heure du PC : relance en administrateur impossible (" & ex.Message & ")")
        End Try
    End Sub

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

    <DllImport("kernel32.dll", SetLastError:=True)>
    Private Function SetSystemTime(ByRef heure As SYSTEMTIME) As Boolean
    End Function

End Module
