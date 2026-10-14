Imports System.Text

Namespace My

    Partial Friend Class MyApplication

        ''' <summary>Avant la premiere fenetre : l'apparence professionnelle (sombre) de Slide, selon la case de
        ''' l'onglet Divers. Le mode sombre de .NET s'occupe de ce que Windows dessine (barre de titre, onglets,
        ''' listes, barres de defilement, menus) ; StylePro.Appliquer habille le reste a l'ouverture des fenetres.</summary>
        Private Sub MyApplication_ApplyApplicationDefaults(sender As Object,
                                                           e As ApplicationServices.ApplyApplicationDefaultsEventArgs) Handles Me.ApplyApplicationDefaults
            Dim sombre As Boolean = True
            Try
                sombre = My.Settings.ThemePro
            Catch ex As Exception
            End Try
            StylePro.Regler(sombre)
            e.ColorMode = If(sombre, SystemColorMode.Dark, SystemColorMode.Classic)
        End Sub

        Private Sub MyApplication_Startup(sender As Object,
                                          e As ApplicationServices.StartupEventArgs) Handles Me.Startup

            ' .NET moderne n'embarque que l'UTF-8, l'ASCII et l'UTF-16 : la page de codes ANSI
            ' Windows-1252, utilisee implicitement par les E/S fichiers VB heritees (FileOpen,
            ' LineInput, PrintLine, My.Computer.FileSystem...), n'existe plus sans ce fournisseur.
            ' Sans cet appel : "No data is available for encoding 1252" des la lecture des .m3u/.txt.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)

            ' Instance relancee en administrateur par HeureDuPC, uniquement pour remettre l'horloge
            ' du PC a l'heure : on la corrige et on s'arrete la, sans ouvrir aucune fenetre.
            If e.CommandLine.Any(Function(a) String.Equals(a, ArgumentReglerHeure, StringComparison.OrdinalIgnoreCase)) Then
                ReglerHorloge()
                e.Cancel = True
                Return
            End If

            Diag("--- demarrage du processus ---")
            ' Temps passe AVANT cette premiere ligne : chargement de .NET et de Slide depuis le disque
            ' (lecteur reseau). Le chronometre du demarrage ("CHRONO") ne commence qu'apres.
            Try
                Using moi As Process = Process.GetCurrentProcess()
                    Diag("Lancement : " & CLng((Date.Now - moi.StartTime).TotalMilliseconds) & " ms entre le lancement du programme et sa premiere ligne (chargement de .NET et de Slide)")
                End Using
            Catch ex As Exception
            End Try

            ' --- Diagnostic de migration (temporaire) : tracer TOUTE sortie du processus. ---
            Diag("STARTUP")
            AddHandler AppDomain.CurrentDomain.UnhandledException,
                Sub(s, ex) Diag("APPDOMAIN_UNHANDLED : " & DescribeError(TryCast(ex.ExceptionObject, Exception)))
            AddHandler AppDomain.CurrentDomain.ProcessExit,
                Sub(s, ev) Diag("PROCESS_EXIT")
            AddHandler Global.System.Windows.Forms.Application.ApplicationExit,
                Sub(s, ev) Diag("APPLICATION_EXIT" & vbCrLf & "    pile : " & Environment.StackTrace)
            AddHandler Global.System.Windows.Forms.Application.ThreadExit,
                Sub(s, ev) Diag("THREAD_EXIT (thread " & Environment.CurrentManagedThreadId & ")")

        End Sub

        Private Sub MyApplication_UnhandledException(sender As Object,
                                                     e As ApplicationServices.UnhandledExceptionEventArgs) Handles Me.UnhandledException
            Diag("MY_UNHANDLED (ExitApplication=" & e.ExitApplication & ") : " & DescribeError(e.Exception))
        End Sub

        Private Sub MyApplication_Shutdown(sender As Object, e As EventArgs) Handles Me.Shutdown
            Diag("SHUTDOWN")
        End Sub

        Private Shared Function DescribeError(ex As Exception) As String
            If ex Is Nothing Then Return "(exception Nothing)"
            Dim sb As New StringBuilder()
            Dim cur As Exception = ex
            Do While cur IsNot Nothing
                sb.AppendLine(cur.GetType().FullName & " : " & cur.Message)
                sb.AppendLine(cur.StackTrace)
                cur = cur.InnerException
                If cur IsNot Nothing Then sb.AppendLine("  --- inner ---")
            Loop
            Return sb.ToString()
        End Function

        Private Shared Sub Diag(message As String)
            Try
                Dim dir As String = IO.Path.Combine(
                    Global.System.Windows.Forms.Application.StartupPath.TrimEnd(IO.Path.DirectorySeparatorChar), "data", "settings")
                IO.Directory.CreateDirectory(dir)
                IO.File.AppendAllText(IO.Path.Combine(dir, "MigrationDiag.log"),
                                      Date.Now.ToString("dd/MM/yyyy HH:mm:ss.fff") & " ; " & message & vbCrLf)
            Catch
                ' le diagnostic ne doit jamais faire tomber l'application
            End Try
        End Sub

    End Class

End Namespace
