Option Strict On
Option Explicit On

''' <summary>
''' Une fenetre ou un programme EXTERNE prend la main (Nflix, programme lance par la minuterie avec "Attend la fin avant de continuer") :
''' le son de Slide est coupe (musique en pause, lecteurs video coupes : meme silence que l'ouverture des reglages, voir
''' SilenceDesReglages) et le diaporama est mis en pause (la pause du menu "Pause" du clic droit : FlagStop, la video en cours est arretee
''' par la boucle du diaporama elle-meme). <see cref="Rendre"/> defait seulement ce que cet objet a fait ; il est sans effet la deuxieme fois.
'''
''' Pas d'arret explicite du lecteur video : la boucle video (Form1) croirait la video terminee, passerait a la suite et relancerait la musique
''' (constate dans le journal le 19/10/2026).
''' </summary>
Friend NotInheritable Class PauseExterne

    Private _silence As Boolean
    Private _pause As Boolean
    Private _rendu As Boolean
    Private ReadOnly _quoi As String

    ''' <summary>Coupe le son de Slide et met le diaporama en pause (ce qui ne l'est pas deja).</summary>
    Friend Sub New(quoi As String)
        _quoi = quoi
        Try
            If Not SilenceDesReglages.Actif Then
                SilenceDesReglages.Couper()
                _silence = True
            End If
            If Not Form3.PauseToolStripMenuItem.Checked Then
                FlagStop = True
                Form3.PauseToolStripMenuItem.Checked = True
                _pause = True
            End If
            DiagLog(quoi & " : son de Slide coupe" & If(_silence, "", " (deja par les reglages)") & ", diaporama en pause" & If(_pause, "", " (deja)"))
        Catch ex As Exception
            DiagLog(quoi & " (silence et pause) : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Rend le son et leve la pause, si c'est cet objet qui les avait mis.</summary>
    Friend Sub Rendre()
        If _rendu Then Exit Sub
        _rendu = True
        Try
            If _silence Then SilenceDesReglages.Rendre()
            If _pause Then
                FlagStop = False
                Form3.PauseToolStripMenuItem.Checked = False
            End If
            DiagLog(_quoi & " termine : son rendu et diaporama relance")
        Catch ex As Exception
            DiagLog(_quoi & " (retour au diaporama) : " & ex.Message)
        End Try
    End Sub

End Class
