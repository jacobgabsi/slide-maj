Option Strict On
Option Explicit On

Imports System.IO

''' <summary>
''' Gardien de Slide (30/10/2026) : relance Slide s'il disparait SANS s'etre ferme normalement (plantage natif, 0xC0000005 dans
''' coreclr.dll sur l'autre PC a 04:01 : le programme etait mort sans une ligne dans le journal et l'ecran est reste vide
''' jusqu'au matin).
'''
''' Principe : au lancement, Slide pose une marque "GardienMarche_(numero du processus).txt" et demarre un petit script
''' cache (cmd) qui surveille son numero. A chaque fermeture NORMALE (PROCESS_EXIT, relance de nuit, mise a jour, Quitter) la
''' marque est retiree : le script ne fait rien. Si le processus a disparu et que la marque est encore la, c'est un arret
''' anormal : le script relance Slide (3 fois au plus tant qu'il ne tient pas 10 minutes, pour ne pas boucler).
''' Un arret par le Gestionnaire des taches compte comme un arret anormal (Slide repart).
'''
''' Reglage PAR POSTE (Reglages, onglet Divers) : fichier data\settings\GardienSlide.txt present = actif. Inactif par
''' defaut : sur le poste de developpement, Slide est arrete avant chaque compilation et ne doit pas repartir.
''' </summary>
Friend Module GardienSlide

    Private Const MinutesDeStabilite As Double = 10
    Private _lance As Boolean

    Private Function FichierOption() As String
        Return Path.Combine(DossierReglages, "GardienSlide.txt")
    End Function

    Private Function FichierMarque() As String
        Return Path.Combine(DossierReglages, "GardienMarche_" & Environment.ProcessId & ".txt")
    End Function

    Private Function FichierPlantages() As String
        Return Path.Combine(DossierReglages, "GardienPlantages.txt")
    End Function

    Private Function FichierScript() As String
        Return Path.Combine(DossierReglages, "GardienSlide.bat")
    End Function

    Public Function EstActif() As Boolean
        Return File.Exists(FichierOption())
    End Function

    ''' <summary>Coche / decoche le reglage. Coche en cours de route : le gardien demarre tout de suite.</summary>
    Public Sub FixerActif(actif As Boolean)
        Try
            If actif Then
                File.WriteAllText(FichierOption(), "Relancer Slide s'il s'arrete anormalement (GardienSlide)")
                Demarrer()
            ElseIf File.Exists(FichierOption()) Then
                File.Delete(FichierOption())
            End If
        Catch ex As Exception
            DiagLog("Gardien : reglage impossible : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Au lancement (depuis RelanceDeNuit.Demarrer, fil de l'interface).</summary>
    Public Sub Demarrer()
        If _lance OrElse Not EstActif() Then Exit Sub
        Try
            NettoyerLesMarquesPerimees()
            If File.Exists(FichierPlantages()) Then
                Dim lignes As String() = File.ReadAllLines(FichierPlantages())
                If lignes.Length > 0 Then DiagLog("Gardien : Slide a ete relance " & lignes.Length & " fois apres un arret anormal (derniere : " & lignes(lignes.Length - 1).Trim() & ")")
            End If

            File.WriteAllText(FichierMarque(), Now.ToString("o"))
            File.WriteAllText(FichierScript(), Script(), System.Text.Encoding.ASCII)
            Dim exe As String = Environment.ProcessPath
            If String.IsNullOrEmpty(exe) Then Exit Sub
            Dim p As New ProcessStartInfo("cmd.exe") With {.UseShellExecute = False, .CreateNoWindow = True, .WindowStyle = ProcessWindowStyle.Hidden}
            p.Arguments = "/c """ & Guillemets(FichierScript()) & " " & Environment.ProcessId & " " & Guillemets(FichierMarque()) & " " &
                          Guillemets(FichierOption()) & " " & Guillemets(FichierPlantages()) & " " & Guillemets(exe) & """"
            Using Process.Start(p)
            End Using
            _lance = True
            DiagLog("Gardien : actif (Slide sera relance s'il s'arrete anormalement)")
        Catch ex As Exception
            DiagLog("Gardien : demarrage impossible : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Fermeture NORMALE : la marque est retiree, le script ne relancera pas.</summary>
    Public Sub Arreter()
        Try
            If File.Exists(FichierMarque()) Then File.Delete(FichierMarque())
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Slide tient depuis assez longtemps : le compte des relances apres arret est remis a zero.</summary>
    Public Sub OublierLesPlantagesSiStable(minutesDeMarche As Double)
        If minutesDeMarche < MinutesDeStabilite Then Exit Sub
        Try
            If File.Exists(FichierPlantages()) Then File.Delete(FichierPlantages())
        Catch ex As Exception
        End Try
    End Sub

    Private Function Guillemets(texte As String) As String
        Return """" & texte & """"
    End Function

    ''' <summary>Marques laissees par des processus qui n'existent plus (arret de Windows...) : sans objet.</summary>
    Private Sub NettoyerLesMarquesPerimees()
        For Each f As String In Directory.GetFiles(DossierReglages, "GardienMarche_*.txt")
            Try
                Dim numero As Integer
                Dim nom As String = Path.GetFileNameWithoutExtension(f)
                If Integer.TryParse(nom.Substring("GardienMarche_".Length), numero) AndAlso numero <> Environment.ProcessId Then
                    Dim vivant As Boolean = False
                    Try
                        Using Process.GetProcessById(numero)
                            vivant = True
                        End Using
                    Catch ex As ArgumentException
                    End Try
                    If Not vivant Then File.Delete(f)
                End If
            Catch ex As Exception
            End Try
        Next
    End Sub

    Private Function Script() As String
        Dim l As New List(Of String) From {
            "@echo off",
            "rem Gardien de Slide (cree par Slide a chaque lancement) : %1 numero du processus, %2 marque, %3 reglage, %4 relances, %5 slide.exe",
            ":attente",
            "ping -n 16 127.0.0.1 >nul",
            "tasklist /FI ""PID eq %~1"" /FI ""IMAGENAME eq slide.exe"" /NH 2>nul | find /i ""slide.exe"" >nul",
            "if not errorlevel 1 goto attente",
            "if not exist ""%~2"" goto fin",
            "del ""%~2"" >nul 2>&1",
            "if not exist ""%~3"" goto fin",
            "set n=0",
            "if exist ""%~4"" for /f %%c in ('find /c /v """" ^< ""%~4""') do set n=%%c",
            "if %n% GEQ 3 goto fin",
            "echo %date% %time% >> ""%~4""",
            "ping -n 6 127.0.0.1 >nul",
            "start """" /D ""%~dp5"" ""%~5""",
            ":fin"}
        Return String.Join(vbCrLf, l) & vbCrLf
    End Function

    ''' <summary>Lance un programme dans quelques secondes, sans fenetre : l'ancien Slide a le temps de se fermer et de rendre sa
    ''' memoire, et le disque de finir d'ecrire (mise a jour), avant que le nouveau charge .NET (relance de nuit, autre PC : 4 Go, disque dur).</summary>
    Public Sub LancerPlusTard(exe As String, secondes As Integer)
        Dim p As New ProcessStartInfo("cmd.exe") With {.UseShellExecute = False, .CreateNoWindow = True, .WindowStyle = ProcessWindowStyle.Hidden}
        p.Arguments = "/c ping -n " & (secondes + 1) & " 127.0.0.1 >nul & start """" /D " & Guillemets(Path.GetDirectoryName(exe)) & " " & Guillemets(exe)
        Using Process.Start(p)
        End Using
    End Sub

End Module
