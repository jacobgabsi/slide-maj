''' <summary>
''' Les commandes « Musique.* » de la telecommande (04/11/2026, demande de Jacob : « renommer tous mes Deezer en Musique pour que ca marche avec toutes les sources »).
''' Lecture, pause, morceau suivant et precedent agissent sur la source de musique EN COURS : Deezer integre a Slide, fichiers MP3, radio. Les commandes propres a Deezer
''' (Favo, Favo_Non, Loved, Flow, SongNameByMail, Lance, Kill) gardent leur effet : « Musique.X » est traitee comme « Deezer.X ». Les anciens noms « Deezer.* » restent compris
''' (raccourcis, autres postes, telecommandes pas encore mises a jour).
''' </summary>
Friend Module CommandesMusique

    ''' <summary>Execute <paramref name="instruction"/> (en majuscules, commence par « MUSIQUE. »).</summary>
    Friend Sub Traiter(instruction As String)
        Dim commande As String = instruction.Substring("MUSIQUE.".Length)
        Select Case True
            Case commande.StartsWith("PRECEDENT") OrElse commande.StartsWith("PREV")
                If MusiqueActive("Precedent") Then MusiqueVLC.MusiquePrecedent()
            Case commande.StartsWith("NEXT") OrElse commande.StartsWith("SUIVANT")
                If MusiqueActive("Suivant") Then Suivante()
            Case commande.StartsWith("PLAY")
                If MusiqueActive("Play") Then
                    If MusiqueDeezer.Actif Then MusiqueDeezer.LectureSurDemande() Else MusiqueVLC.MusiqueJouer()
                End If
            Case commande.StartsWith("STOP")
                If MusiqueActive("Stop") Then
                    If MusiqueDeezer.Actif Then MusiqueDeezer.PauseSurDemande() Else MusiqueVLC.MusiquePause()
                End If
            Case commande.StartsWith("PAUSE")                 ' (PausePlay et Pause : la bascule, comme pour Deezer)
                If MusiqueActive("Pause") Then
                    If MusiqueDeezer.Actif Then
                        MusiqueDeezer.Basculer()
                    ElseIf MusiqueVLC.MusiqueEnCours() Then
                        MusiqueVLC.MusiquePause()
                    Else
                        MusiqueVLC.MusiqueJouer()
                    End If
                End If
            Case Else
                ' Favo, Favo_Non, Loved, Flow, SongNameByMail, Lance, Kill : propres a Deezer.
                GestionControlAdistanceSub2(Nothing, Nothing, "Deezer." & commande)
        End Select
    End Sub

    Private Function MusiqueActive(quoi As String) As Boolean
        If Music Then Return True
        DiagLog("Musique." & quoi & " sans effet (la musique est coupee)")
        Return False
    End Function

    ''' <summary>Morceau (ou station) suivant de la source en cours, sans toucher au diaporama (comme la touche N pour la musique).</summary>
    Private Sub Suivante()
        If MusiqueDeezer.Actif Then
            DiagLog("Musique.Next : morceau suivant de Deezer")
            MusiqueDeezer.Suivant()
        ElseIf Music_Deezer Then
            DiagLog("Musique.Next : morceau suivant de l'application Deezer")
            GestionControlAdistanceSub2(Nothing, Nothing, "Deezer.Next")
        ElseIf Music_Radio Then
            Dim station As String = MusiqueVLC.MusiqueRadioSuivante()
            If station <> "" Then DiagLog("Musique.Next : radio suivante (" & station & ")")
        Else
            DiagLog("Musique.Next : fichier de musique suivant")
            MusiqueVLC.MusiqueSuivant()
        End If
    End Sub

End Module
