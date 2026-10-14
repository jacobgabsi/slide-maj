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
    Public Sub LancerPlusTard(exe As String, secondes As Integer, Optional avecFenetre As Boolean = False, Optional texte As String = "Slide va redémarrer dans un instant.")
        If avecFenetre Then
            Try
                LancerPlusTardAvecFenetre(exe, secondes, texte)
                Exit Sub
            Catch ex As Exception
                DiagLog("Relance : fenetre d'attente impossible (" & ex.Message & ") -> relance sans fenetre")
            End Try
        End If
        Dim p As New ProcessStartInfo("cmd.exe") With {.UseShellExecute = False, .CreateNoWindow = True, .WindowStyle = ProcessWindowStyle.Hidden}
        p.Arguments = "/c ping -n " & (secondes + 1) & " 127.0.0.1 >nul & start """" /D " & Guillemets(Path.GetDirectoryName(exe)) & " " & Guillemets(exe)
        Using Process.Start(p)
        End Using
    End Sub

    ''' <summary>Pendant l'attente, une petite fenetre sombre explique que Slide va se relancer (le programme est ferme : la fenetre est donc portee par un
    ''' processus PowerShell a part, qui lance Slide a la fin du decompte). Demande de Jacob, 01/11/2026.</summary>
    Private Sub LancerPlusTardAvecFenetre(exe As String, secondes As Integer, texte As String)
        Dim dossier As String = Path.GetDirectoryName(exe)
        Dim q As Func(Of String, String) = Function(s) "'" & s.Replace("'", "''") & "'"
        Dim script As String =
            "Add-Type -AssemblyName System.Windows.Forms,System.Drawing" & vbLf &
            "[System.Windows.Forms.Application]::EnableVisualStyles()" & vbLf &
            "$exe = " & q(exe) & "; $dossier = " & q(dossier) & vbLf &
            "$fin = [DateTime]::Now.AddSeconds(" & Math.Max(1, secondes - 1) & ")" & vbLf &
            "$f = New-Object System.Windows.Forms.Form" & vbLf &
            "$f.FormBorderStyle = 'None'; $f.StartPosition = 'CenterScreen'; $f.TopMost = $true; $f.ShowInTaskbar = $false" & vbLf &
            "$f.ClientSize = New-Object System.Drawing.Size(560, 205); $f.BackColor = [System.Drawing.Color]::FromArgb(28, 28, 30)" & vbLf &
            "[void]$f.GetType().GetProperty('DoubleBuffered', [System.Reflection.BindingFlags]'Instance,NonPublic').SetValue($f, $true)" & vbLf &
            "[void]$f.GetType().GetMethod('SetStyle', [System.Reflection.BindingFlags]'Instance,NonPublic').Invoke($f, @([System.Windows.Forms.ControlStyles]'AllPaintingInWmPaint,UserPaint,OptimizedDoubleBuffer', $true))" & vbLf &
            "try { Add-Type -Namespace Slide -Name Dwm -MemberDefinition '[System.Runtime.InteropServices.DllImport(""dwmapi.dll"")] public static extern int DwmSetWindowAttribute(System.IntPtr h, int a, ref int v, int s);' } catch { }" & vbLf &
            "$total = [Math]::Max(1, " & Math.Max(1, secondes - 1) & ")" & vbLf &
            "$icone = $null; try { $icone = [System.Drawing.Icon]::ExtractAssociatedIcon($exe) } catch { }" & vbLf &
            "$ftitre = New-Object System.Drawing.Font('Segoe UI Semibold', 15); $ftexte = New-Object System.Drawing.Font('Segoe UI', 10); $fpetit = New-Object System.Drawing.Font('Segoe UI', 9)" & vbLf &
            "$blanc = [System.Drawing.Color]::FromArgb(245, 245, 247); $gris = [System.Drawing.Color]::FromArgb(152, 152, 157); $accent = [System.Drawing.Color]::FromArgb(10, 132, 255)" & vbLf &
            "$f.Add_Paint({ param($s, $e)" & vbLf &
            "  $g = $e.Graphics; $g.Clear($f.BackColor); $g.SmoothingMode = 'AntiAlias'; $g.TextRenderingHint = 'ClearTypeGridFit'" & vbLf &
            "  $g.DrawRectangle((New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(58, 58, 60), 1)), 0, 0, $f.ClientSize.Width - 1, $f.ClientSize.Height - 1)" & vbLf &
            "  if ($icone) { $g.DrawIcon($icone, (New-Object System.Drawing.Rectangle(24, 24, 48, 48))) }" & vbLf &
            "  [System.Windows.Forms.TextRenderer]::DrawText($g, 'Slide va se relancer', $ftitre, (New-Object System.Drawing.Point(88, 22)), $blanc)" & vbLf &
            "  [System.Windows.Forms.TextRenderer]::DrawText($g, $script:reste, $fpetit, (New-Object System.Drawing.Point(90, 54)), $gris)" & vbLf &
            "  $zone = New-Object System.Drawing.Rectangle(24, 88, 512, 66)" & vbLf &
            "  [System.Windows.Forms.TextRenderer]::DrawText($g, " & q(texte) & ", $ftexte, $zone, $blanc, [System.Windows.Forms.TextFormatFlags]::WordBreak)" & vbLf &
            "  $piste = New-Object System.Drawing.Rectangle(24, 168, 450, 10)" & vbLf &
            "  $g.FillRectangle((New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(52, 52, 56))), $piste)" & vbLf &
            "  $part = 1.0 - [Math]::Max(0.0, [Math]::Min(1.0, ($fin - [DateTime]::Now).TotalSeconds / $total))" & vbLf &
            "  $g.FillRectangle((New-Object System.Drawing.SolidBrush($accent)), (New-Object System.Drawing.Rectangle($piste.Left, $piste.Top, [int]($piste.Width * $part), $piste.Height)))" & vbLf &
            "  [System.Windows.Forms.TextRenderer]::DrawText($g, ([int][Math]::Round($part * 100)).ToString() + ' %', $fpetit, (New-Object System.Drawing.Point(486, 163)), $gris)" & vbLf &
            "})" & vbLf &
            "$script:reste = ''" & vbLf &
            "$f.Add_HandleCreated({ try { $arrondi = 2; [void][Slide.Dwm]::DwmSetWindowAttribute($f.Handle, 33, [ref]$arrondi, 4); $bord = 58 + (58 -shl 8) + (60 -shl 16); [void][Slide.Dwm]::DwmSetWindowAttribute($f.Handle, 34, [ref]$bord, 4) } catch { } })" & vbLf &
            "$tm = New-Object System.Windows.Forms.Timer; $tm.Interval = 100" & vbLf &
            "$tm.Add_Tick({ $r = [Math]::Ceiling(($fin - [DateTime]::Now).TotalSeconds); if ($r -le 0) { $tm.Stop(); Start-Process -FilePath $exe -WorkingDirectory $dossier; $f.Close() } else { if ($script:reste -ne ('Relance dans ' + $r + ' s')) { $script:reste = 'Relance dans ' + $r + ' s'; $f.Invalidate((New-Object System.Drawing.Rectangle(88, 50, 440, 24))) }; $f.Invalidate((New-Object System.Drawing.Rectangle(20, 160, 530, 26))) } })" & vbLf &
            "$tm.Start(); [void]$f.ShowDialog()" & vbLf
        Dim codage As String = Convert.ToBase64String(System.Text.Encoding.Unicode.GetBytes(script))
        Dim p As New ProcessStartInfo("powershell.exe") With {.UseShellExecute = False, .CreateNoWindow = True, .WindowStyle = ProcessWindowStyle.Hidden}
        p.Arguments = "-NoProfile -NonInteractive -WindowStyle Hidden -ExecutionPolicy Bypass -EncodedCommand " & codage
        Using Process.Start(p)
        End Using
    End Sub

End Module
