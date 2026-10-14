Option Strict On
Option Explicit On

Imports System.Threading

''' <summary>
''' Garde-fous contre l'ecran noir qui reste toute la nuit.
'''
''' Constat du samedi 03/10 : France 2 programmee a 23h26 n'a donne aucune image, puis Slide est
''' reste COMPLETEMENT fige (plus rien dans le journal, plus aucune tache de la minuterie) jusqu'au
''' lendemain 12h55. Le fil de l'interface etait bloque dans un appel a VLC pendant que le tuner
''' TV ne repondait pas.
'''
''' 1. Surveillance : un minuteur de l'interface donne un "battement" chaque seconde ; un fil a
'''    part verifie qu'il bat. Sans battement pendant 3 minutes, Slide est relance (nouveau
'''    processus, l'ancien est ferme) et note l'incident dans data\Blocage.txt.
''' 2. Arret d'un lecteur sans risque de blocage (ArreterSansBloquer).
''' </summary>
Friend Module GardeTV

    Private Const DelaiBlocageMs As Long = 180000          ' 3 minutes sans battement

    Private _battement As Long
    Private _minuteur As System.Windows.Forms.Timer
    Private _surveillance As Thread
    Private _relanceFaite As Integer

    Private Function FichierBlocage() As String
        Return IO.Path.Combine(AppPath, "data", "Blocage.txt")
    End Function

    ''' <summary>A appeler une fois, depuis le fil de l'interface, au demarrage.</summary>
    Public Sub Demarrer()
        If _minuteur IsNot Nothing Then Exit Sub
        Interlocked.Exchange(_battement, Environment.TickCount64)
        _minuteur = New System.Windows.Forms.Timer With {.Interval = 1000}
        AddHandler _minuteur.Tick, Sub() Interlocked.Exchange(_battement, Environment.TickCount64)
        _minuteur.Start()
        _surveillance = New Thread(AddressOf Surveiller) With {.IsBackground = True, .Name = "Surveillance blocage"}
        _surveillance.Start()
    End Sub

    Private Sub Surveiller()
        Dim precedent As Long = Environment.TickCount64
        Do
            Thread.Sleep(5000)
            Dim maintenant As Long = Environment.TickCount64
            ' Ordinateur en veille : le compteur a avance sans que rien ne tourne. Pas un blocage.
            If maintenant - precedent > 30000 Then Interlocked.Exchange(_battement, maintenant)
            precedent = maintenant
            If Debugger.IsAttached Then Continue Do
            Dim ecart As Long = maintenant - Interlocked.Read(_battement)
            If ecart > DelaiBlocageMs Then
                Relancer(ecart)
                Exit Do
            End If
        Loop
    End Sub

    Private Sub Relancer(ecartMs As Long)
        If Interlocked.Exchange(_relanceFaite, 1) = 1 Then Exit Sub
        Dim mode As String = ""
        Try
            mode = Diffusion.Mode
        Catch ex As Exception
        End Try
        Try
            DiagLog("BLOCAGE : Slide fige depuis " & (ecartMs \ 1000) & " s (mode " & mode & ") -> redemarrage automatique")
        Catch ex As Exception
        End Try
        Try
            IO.File.WriteAllText(FichierBlocage(), Now.ToString("o") & vbTab & mode, New Text.UTF8Encoding(False))
        Catch ex As Exception
        End Try
        Try
            Dim exe As String = Environment.ProcessPath
            MarquerLaRelance()
            If Not String.IsNullOrEmpty(exe) Then Process.Start(New ProcessStartInfo(exe) With {.UseShellExecute = True})
        Catch ex As Exception
            Try
                DiagLog("BLOCAGE : relance impossible (" & ex.Message & ")")
            Catch
            End Try
        End Try
        Process.GetCurrentProcess().Kill()
    End Sub

    ''' <summary>Arrete un lecteur VLC dans un autre fil, en attendant <paramref name="attenteMs"/>
    ''' au plus. Un Stop direct peut bloquer indefiniment quand le flux (tuner TV) ne repond plus.
    ''' Renvoie False si l'arret n'a pas abouti dans le temps (le lecteur est alors abandonne).</summary>
    Public Function ArreterSansBloquer(lecteur As Vlc.DotNet.Forms.VlcControl, attenteMs As Integer, quoi As String) As Boolean
        If lecteur Is Nothing Then Return True
        Dim arret As Tasks.Task = Tasks.Task.Run(Sub()
                                                     Try
                                                         lecteur.Stop()
                                                     Catch ex As Exception
                                                     End Try
                                                 End Sub)
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim sta As Boolean = Thread.CurrentThread.GetApartmentState() = ApartmentState.STA
        Do Until arret.IsCompleted OrElse chrono.ElapsedMilliseconds > attenteMs
            If sta Then System.Windows.Forms.Application.DoEvents()
            Thread.Sleep(20)
        Loop
        If Not arret.IsCompleted Then
            DiagLog(quoi & " : le lecteur ne s'arrete pas (" & attenteMs & " ms) -> abandonne, le diaporama continue")
            Return False
        End If
        Return True
    End Function

End Module
