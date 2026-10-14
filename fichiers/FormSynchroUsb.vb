Option Strict On
Option Explicit On

Imports System.Collections.Concurrent
Imports System.Diagnostics

''' <summary>
''' Copie du dossier du programme sur une cle USB, avec le detail de ce qui se passe.
'''
''' La cle se choisit dans la liste (toutes les cles branchees y figurent, celle utilisee la
''' derniere fois est preselectionnee). La copie est faite par robocopy en mode miroir : les
''' fichiers nouveaux ou modifies sont copies, ceux qui n'existent plus sont retires de la cle.
''' Chaque ligne de robocopy s'affiche au fur et a mesure.
''' </summary>
Public Class FormSynchroUsb

    Private _copie As Process
    Private ReadOnly _lignes As New ConcurrentQueue(Of String)
    Private WithEvents TimerAffichage As New Timer With {.Interval = 100}
    Private _chrono As Stopwatch
    ' Les arguments de la copie : la verification finale (VerifierLaCopie) refait la meme comparaison, sans rien copier.
    Private _argumentsDeLaCopie As String = ""

    ''' <summary>Une cle dans la liste deroulante.</summary>
    Private Structure Cle
        Public Nom As String            ' etiquette du volume, ex. "JACOB_1TO"
        Public Racine As String         ' ex. "F:\"
        Public ReadOnly Property Texte As String
            Get
                Return Racine & "  " & If(Nom = "", "(sans nom)", Nom)
            End Get
        End Property
    End Structure

    Private _cles As New List(Of Cle)

    Private Sub FormSynchroUsb_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        LabelSource.Text = "Dossier : " & DossierDuProjet()
        RemplirLesCles()
    End Sub

    ''' <summary>Toutes les cles branchees (lecteurs amovibles), plus les disques externes.</summary>
    Private Sub RemplirLesCles()
        _cles.Clear()
        ComboBoxCles.Items.Clear()

        For Each d As IO.DriveInfo In IO.DriveInfo.GetDrives()
            Try
                If Not d.IsReady Then Continue For
                If d.DriveType <> IO.DriveType.Removable AndAlso d.DriveType <> IO.DriveType.Fixed Then Continue For
                ' Le disque systeme n'est pas une cle : on ne le propose pas.
                If String.Equals(d.RootDirectory.FullName, IO.Path.GetPathRoot(Environment.SystemDirectory), StringComparison.OrdinalIgnoreCase) Then Continue For
                _cles.Add(New Cle With {.Nom = d.VolumeLabel, .Racine = d.RootDirectory.FullName})
            Catch ex As Exception
                ' lecteur non pret : on passe au suivant
            End Try
        Next

        For Each c As Cle In _cles
            ComboBoxCles.Items.Add(c.Texte)
        Next

        ' Celle utilisee la derniere fois (JACOB_1TO au depart), sinon la premiere.
        Dim voulue As Integer = _cles.FindIndex(Function(c) String.Equals(c.Nom, CleUsbNom, StringComparison.OrdinalIgnoreCase))
        If voulue < 0 AndAlso _cles.Count > 0 Then voulue = 0
        If voulue >= 0 Then ComboBoxCles.SelectedIndex = voulue

        If _cles.Count = 0 Then
            LabelDestination.Text = "Aucune clé branchée."
            ButtonSynchroniser.Enabled = False
        Else
            ButtonSynchroniser.Enabled = True
        End If
    End Sub

    Private Sub ComboBoxCles_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxCles.SelectedIndexChanged
        MettreAJourLesTextes()
    End Sub

    ''' <summary>Les etiquettes et le bouton suivent le sens choisi (PC vers cle, ou cle vers ce PC).</summary>
    Private Sub MettreAJourLesTextes()
        If RadioButtonDepuisCle.Checked Then
            LabelDestination.Text = "Source : " & Destination() & "   ->   ce PC"
            ButtonSynchroniser.Text = "Récupérer"
        Else
            LabelDestination.Text = "Destination : " & Destination()
            ButtonSynchroniser.Text = "Synchroniser"
        End If
    End Sub

    Private Sub RadioButtonSens_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButtonVersCle.CheckedChanged, RadioButtonDepuisCle.CheckedChanged
        MettreAJourLesTextes()
    End Sub

    ''' <summary>Dossiers a ne pas copier : .vs, et data\settings (propre a chaque poste) si la case n'est pas cochee.
    ''' Les deux arbres sont cites : un dossier exclu n'est ni copie ni supprime par /MIR.</summary>
    Private Function ExclusionsRobocopy(source As String, cible As String) As String
        Dim xd As String = " /XD "".vs"""
        If Not CheckBoxReglages.Checked Then
            Dim rel As String = IO.Path.GetRelativePath(DossierDuProjet(), DossierReglages)
            xd &= " """ & IO.Path.Combine(source.TrimEnd("\"c), rel) & """ """ & IO.Path.Combine(cible.TrimEnd("\"c), rel) & """"
        End If
        ' Les journaux que Slide (ou Visual Studio) ecrit en permanence (MigrationDiag.log : plusieurs Mo, change a chaque ligne) : ni copies ni verifies, sinon la copie "n'est jamais exacte"
        ' (23/10/2026 : "1 difference : plus recent 5,0 m"). Journaux propres au poste, jamais utiles sur l'autre PC.
        Return xd & " /XF ""MigrationDiag.log"" ""Log.log"""
    End Function

    ''' <summary>Cle -> ce PC : Slide tourne depuis ce dossier (fichiers verrouilles), donc la copie est confiee a une
    ''' fenetre de commandes qui attend la fermeture de Slide, copie, puis attend une touche.</summary>
    Private Sub RecupererDepuisLaCle(source As String, cible As String)
        Dim texte As String = "Récupérer la version de la clé ?" & vbCrLf & vbCrLf &
            "De : " & source & vbCrLf & "Vers : " & cible & vbCrLf & vbCrLf &
            "Les fichiers de ce PC absents de la clé seront supprimés" &
            If(CheckBoxReglages.Checked, ", réglages du poste compris.", " (les réglages du poste sont conservés).") & vbCrLf & vbCrLf &
            "Slide va se fermer, la copie se fait dans une fenêtre noire, puis tu relances Slide."
        If MsgBox(texte, MsgBoxStyle.YesNo Or MsgBoxStyle.Question, Me.Text) <> MsgBoxResult.Yes Then Exit Sub
        Try
            Dim pid As Integer = Process.GetCurrentProcess().Id
            Dim arguments As String = """" & source.TrimEnd("\"c) & """ """ & cible.TrimEnd("\"c) & """ /MIR" &
                                      ExclusionsRobocopy(source, cible) & " /FFT /R:2 /W:2 /NP /NDL"
            Dim lignes As String =
                "@echo off" & vbCrLf & "chcp 65001 >nul" & vbCrLf &
                "echo Attente de la fermeture de Slide..." & vbCrLf & ":att" & vbCrLf &
                "tasklist /FI ""PID eq " & pid & """  2>nul | find """ & pid & """ >nul" & vbCrLf &
                "if not errorlevel 1 (timeout /t 1 /nobreak >nul & goto att)" & vbCrLf &
                "echo." & vbCrLf & "robocopy " & arguments & vbCrLf &
                "set code=%errorlevel%" & vbCrLf & "echo." & vbCrLf & "echo --- Resultat (code %code%) ---" & vbCrLf &
                "if %code% EQU 0 echo Rien a copier : ce PC etait deja identique a la cle." & vbCrLf &
                "if %code% GEQ 16 (echo ERREUR GRAVE : copie impossible, dossier ou cle introuvable, droits insuffisants.) else if %code% GEQ 8 (echo ERREUR : certains fichiers n'ont pas pu etre copies, copie INCOMPLETE.) else if %code% GTR 0 (echo Copie reussie : fichiers copies, et fichiers en trop supprimes de ce PC si code 2 ou 3.)" & vbCrLf &
                "echo Code : 1 = fichiers copies, 2 = fichiers en trop supprimes, 4 = incoherences, 8 = erreurs de copie, 16 = erreur grave." & vbCrLf &
                "if %code% LSS 8 (echo. & echo --- Verification : aucune ligne ci-dessous = copie EXACTE --- & robocopy " & arguments & " /L /NJH /NJS /NP /NDL & echo --- fin de la verification ---)" & vbCrLf &
                "echo Relance Slide." & vbCrLf & "pause" & vbCrLf
            Dim bat As String = IO.Path.Combine(IO.Path.GetTempPath(), "slide_recuperation.bat")
            IO.File.WriteAllText(bat, lignes, New System.Text.UTF8Encoding(False))
            DiagLog("Synchro USB : recuperation " & source & " -> " & cible)
            Process.Start(New ProcessStartInfo("cmd.exe", "/c """ & bat & """") With {.UseShellExecute = True})
            End
        Catch ex As Exception
            MsgBox("Impossible de lancer la récupération : " & ex.Message, MsgBoxStyle.Exclamation, Me.Text)
        End Try
    End Sub

    Private Sub ButtonRelire_Click(sender As Object, e As EventArgs) Handles ButtonRelire.Click
        RemplirLesCles()
    End Sub

    ''' <summary>Dossier vise sur la cle choisie.</summary>
    Private Function Destination() As String
        If ComboBoxCles.SelectedIndex < 0 Then Return ""
        Return IO.Path.Combine(_cles(ComboBoxCles.SelectedIndex).Racine, DossierSurLaCle)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' La copie
    ' ------------------------------------------------------------------------------------------

    Private Sub ButtonSynchroniser_Click(sender As Object, e As EventArgs) Handles ButtonSynchroniser.Click

        Dim depuisLaCle As Boolean = RadioButtonDepuisCle.Checked
        Dim source As String = If(depuisLaCle, Destination(), DossierDuProjet())
        Dim cible As String = If(depuisLaCle, DossierDuProjet(), Destination())
        If source = "" OrElse Not IO.Directory.Exists(source) Then
            MsgBox(If(depuisLaCle, "Aucun dossier Slide sur cette clé.", "Dossier du programme introuvable."), MsgBoxStyle.Exclamation, Me.Text)
            Exit Sub
        End If
        If cible = "" Then Exit Sub

        ' Pas de demande de confirmation : le bouton "Synchroniser" de cette fenetre, qui affiche
        ' deja le dossier et la cle visee, vaut confirmation (demande de Jacob).

        ' La cle choisie devient celle proposee la prochaine fois.
        CleUsbNom = _cles(ComboBoxCles.SelectedIndex).Nom
        My.Settings.CleUsbNom = CleUsbNom
        My.Settings.Save()

        If depuisLaCle Then
            RecupererDepuisLaCle(source, cible)
            Exit Sub
        End If

        TextBoxJournal.Clear()
        Afficher("Copie de " & source)
        Afficher("     vers " & cible)
        Afficher("")

        ' /XD .vs : dossier de travail de Visual Studio, verrouille tant que VS est ouvert et
        ' inutile sur la cle. Pas de /NFL : on veut voir passer les fichiers.
        Dim arguments As String = """" & source.TrimEnd("\"c) & """ """ & cible.TrimEnd("\"c) &
                                  """ /MIR" & ExclusionsRobocopy(source, cible) & " /FFT /R:1 /W:1 /NP /NDL"
        _argumentsDeLaCopie = arguments

        Try
            Dim demarrage As New ProcessStartInfo("robocopy.exe") With {
                .Arguments = arguments,
                .UseShellExecute = False,
                .CreateNoWindow = True,
                .RedirectStandardOutput = True,
                .RedirectStandardError = True}
            Try
                ' robocopy ecrit dans la page de codes de la console, pas en UTF-8.
                demarrage.StandardOutputEncoding = System.Text.Encoding.GetEncoding(Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage)
                demarrage.StandardErrorEncoding = demarrage.StandardOutputEncoding
            Catch ex As Exception
            End Try

            _copie = New Process With {.StartInfo = demarrage, .EnableRaisingEvents = True}
            AddHandler _copie.OutputDataReceived, AddressOf LigneRecue
            AddHandler _copie.ErrorDataReceived, AddressOf LigneRecue

            _chrono = Stopwatch.StartNew()
            _copie.Start()
            _copie.BeginOutputReadLine()
            _copie.BeginErrorReadLine()

            ButtonSynchroniser.Enabled = False
            ButtonArreter.Enabled = True
            ComboBoxCles.Enabled = False
            ButtonRelire.Enabled = False
            LabelEtat.Text = "Copie en cours..."
            TimerAffichage.Start()

        Catch ex As Exception
            DiagLog("Synchro USB : " & ex.Message)
            Afficher("ERREUR : " & ex.Message)
            Terminer(-1)
        End Try
    End Sub

    ''' <summary>Appelee par robocopy depuis un autre fil : on empile, l'affichage suit.</summary>
    Private Sub LigneRecue(expediteur As Object, e As DataReceivedEventArgs)
        If e.Data IsNot Nothing Then _lignes.Enqueue(e.Data)
    End Sub

    Private Sub TimerAffichage_Tick(sender As Object, e As EventArgs) Handles TimerAffichage.Tick
        Dim paquet As New List(Of String)
        Dim ligne As String = Nothing
        Do While _lignes.TryDequeue(ligne)
            If ligne.Trim() <> "" Then paquet.Add(ligne.TrimEnd())
        Loop
        If paquet.Count > 0 Then Afficher(String.Join(Environment.NewLine, paquet))

        If _copie IsNot Nothing AndAlso _copie.HasExited AndAlso _lignes.IsEmpty Then
            TimerAffichage.Stop()
            Terminer(_copie.ExitCode)
        End If
    End Sub

    Private Sub Afficher(texte As String)
        TextBoxJournal.AppendText(texte & Environment.NewLine)
    End Sub

    ''' <summary>Fin de la copie : bilan et remise des boutons.</summary>
    Private Async Sub Terminer(code As Integer)
        Dim minutes As String = If(_chrono Is Nothing, "0", Format(_chrono.Elapsed.TotalMinutes, "0.0"))
        ' robocopy : moins de 8 = tout s'est bien passe (0 = rien a copier, 1 = fichiers copies,
        ' 2 = fichiers supprimes sur la cle, et leurs combinaisons).
        If code >= 8 OrElse code < 0 Then
            LabelEtat.Text = "Terminé avec des erreurs (code " & code & ") en " & minutes & " min"
        Else
            LabelEtat.Text = "Terminé en " & minutes & " minute(s) (code " & code & ")"
        End If
        Afficher("")
        Afficher("--- Résultat (code " & code & ") ---")
        Afficher(ExplicationDuCode(code))
        DiagLog("Synchro USB : " & DossierDuProjet() & " -> " & Destination() & " | code=" & code & " | " & minutes & " min")

        ' Copie EXACTE (22/10/2026 : "la synchronisation de la cle doit etre une copie exacte") : une seconde passe de robocopy compare les deux dossiers sans rien copier et dit s'il
        ' reste la moindre difference (fichier verrouille, nom refuse par la cle, fichier absent...).
        If code >= 0 AndAlso code < 8 AndAlso _argumentsDeLaCopie <> "" Then
            LabelEtat.Text = "Vérification de la copie…"
            Afficher("")
            Afficher("--- Vérification : comparaison des deux dossiers ---")
            Dim differences As List(Of String) = Await VerifierLaCopie(_argumentsDeLaCopie)
            If differences.Count = 0 Then
                Afficher("✔ COPIE EXACTE : aucune différence entre le PC et la clé.")
                LabelEtat.Text = "Copie exacte vérifiée (" & minutes & " min)"
            Else
                Afficher("✘ " & differences.Count & " différence(s) restante(s) :")
                For Each d As String In differences.Take(40)
                    Afficher("   " & d)
                Next
                If differences.Count > 40 Then Afficher("   … et " & (differences.Count - 40) & " autre(s)")
                Afficher("La copie n'est PAS exacte. Ferme Slide et Visual Studio, puis relance la synchronisation.")
                LabelEtat.Text = "Copie NON exacte : " & differences.Count & " différence(s)"
            End If
            DiagLog("Synchro USB : verification, " & differences.Count & " difference(s)")
        End If

        ButtonSynchroniser.Enabled = (ComboBoxCles.SelectedIndex >= 0)
        ButtonArreter.Enabled = False
        ComboBoxCles.Enabled = True
        ButtonRelire.Enabled = True
        _copie = Nothing
    End Sub

    ''' <summary>Compare le PC et la cle comme la copie (memes arguments, sans rien copier ni supprimer : /L) et rend ce qui differe encore, une ligne par difference.</summary>
    Private Shared Async Function VerifierLaCopie(arguments As String) As Task(Of List(Of String))
        Return Await Task.Run(Function() As List(Of String)
                                  Dim differences As New List(Of String)
                                  Try
                                      Dim demarrage As New ProcessStartInfo("robocopy.exe") With {
                                          .Arguments = arguments & " /L /NJH /NJS /NP /NDL",
                                          .UseShellExecute = False,
                                          .CreateNoWindow = True,
                                          .RedirectStandardOutput = True}
                                      Try
                                          demarrage.StandardOutputEncoding = System.Text.Encoding.GetEncoding(Globalization.CultureInfo.CurrentCulture.TextInfo.OEMCodePage)
                                      Catch ex As Exception
                                      End Try
                                      Using p As Process = Process.Start(demarrage)
                                          Dim sortie As String = p.StandardOutput.ReadToEnd()
                                          p.WaitForExit()
                                          For Each ligne As String In sortie.Split({vbCr, vbLf}, StringSplitOptions.RemoveEmptyEntries)
                                              If ligne.Trim() <> "" Then differences.Add(ligne.Trim())
                                          Next
                                      End Using
                                  Catch ex As Exception
                                      differences.Add("Vérification impossible : " & ex.Message)
                                  End Try
                                  Return differences
                              End Function)
    End Function

    ''' <summary>Le code de sortie de robocopy est une somme de bits : on les decrit en clair.</summary>
    Private Shared Function ExplicationDuCode(code As Integer) As String
        If code < 0 Then Return "La copie n'a pas pu démarrer ou a été arrêtée."
        Dim l As New List(Of String)
        If code = 0 Then l.Add("Rien à copier : la destination était déjà identique.")
        If (code And 1) <> 0 Then l.Add("Des fichiers ont été copiés (nouveaux ou modifiés).")
        If (code And 2) <> 0 Then l.Add("Des fichiers ou dossiers en trop ont été trouvés (et supprimés de la destination).")
        If (code And 4) <> 0 Then l.Add("Des incohérences ont été détectées (fichier et dossier de même nom, etc.).")
        If (code And 8) <> 0 Then l.Add("ERREUR : certains fichiers n'ont pas pu être copiés (verrouillés, accès refusé, clé pleine...). Relance après avoir fermé Slide et Visual Studio.")
        If (code And 16) <> 0 Then l.Add("ERREUR GRAVE : copie impossible (dossier ou clé introuvable, droits insuffisants).")
        l.Add(If(code >= 8, "=> Copie INCOMPLÈTE.", "=> Copie réussie."))
        Return String.Join(Environment.NewLine, l)
    End Function

    Private Sub ButtonArreter_Click(sender As Object, e As EventArgs) Handles ButtonArreter.Click
        ArreterLaCopie()
        Afficher("")
        Afficher("--- arrêté ---")
    End Sub

    Private Sub ArreterLaCopie()
        Try
            If _copie IsNot Nothing AndAlso Not _copie.HasExited Then _copie.Kill()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ButtonFermer_Click(sender As Object, e As EventArgs) Handles ButtonFermer.Click
        Me.Close()
    End Sub

    Private Sub FormSynchroUsb_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If _copie IsNot Nothing AndAlso Not _copie.HasExited Then
            If MsgBox("La copie est en cours. L'arrêter ?", MsgBoxStyle.YesNo Or MsgBoxStyle.Question, Me.Text) <> MsgBoxResult.Yes Then
                e.Cancel = True
                Exit Sub
            End If
            ArreterLaCopie()
        End If
        TimerAffichage.Stop()
    End Sub

    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub FormSynchroUsb_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
