Option Strict On
Option Explicit On

''' <summary>
''' Legende d'un clip Blink affiche tout seul (27/10/2026, Jacob : « le message sur la video, pas un ecran noir avant ») : un bandeau sombre translucide avec le nom de la camera et l'heure,
''' pose en haut a gauche de l'image (plein ecran ou PIP) quelques secondes. Fenetre a part, sans bordure et sans prendre le clavier : un controle pose sur VLC serait masque par la video.
''' Les controles sont dans le Concepteur.
''' </summary>
Public Class FormLegendeClip

    Private _zone As Rectangle
    Private _suivi As Func(Of Rectangle)
    Private _heureAGauche As Boolean

    ''' <summary>Pose la legende en bas, au centre de <paramref name="zone"/> (ecran) et la retire seule apres <paramref name="secondes"/>. <paramref name="suivi"/> : rend la zone actuelle de l'image
    ''' (le PIP peut etre agrandi par double-clic, deplace ou redimensionne) ; la legende s'y recale.</summary>
    Friend Sub Montrer(texte As String, zone As Rectangle, secondes As Double, Optional suivi As Func(Of Rectangle) = Nothing, Optional heure As String = "", Optional heureAGauche As Boolean = False)
        _heureAGauche = heureAGauche          ' (camera IP : le numero de la camera se pose en haut a gauche, l'heure des clips Blink en haut a droite)
        LabelTexte.Text = texte
        LabelHeure.Text = heure
        LabelHeure.Visible = (heure <> "")
        _suivi = suivi
        Placer(zone)
        TimerFin.Interval = CInt(Math.Max(1, secondes) * 1000)
        TimerFin.Start()
        If suivi IsNot Nothing Then TimerSuivi.Start()
        Show()
        CalqueWin32.DefinirPremierPlanSeul(Me, True)
        ' Le PIP se remet au premier plan toutes les 0,5 s et recouvrait la legende (27/10/2026) : chaque fois, elle est remise juste apres lui (CalqueWin32), sans clignoter.
        CalqueWin32.ProtegerLaLegende(Me.Handle)
    End Sub

    Protected Overrides Sub OnFormClosed(e As FormClosedEventArgs)
        CalqueWin32.ProtegerLaLegende(IntPtr.Zero)
        MyBase.OnFormClosed(e)
    End Sub

    ''' <summary>Taille du texte et place de la legende pour une image a <paramref name="zone"/>.</summary>
    Private Sub Placer(zone As Rectangle)
        _zone = zone
        ' Texte plus petit dans un petit PIP (28 pt en plein ecran au plus), et jamais plus large que l'image : la taille baisse jusqu'a ce qu'il tienne.
        Dim points As Single = CSng(Math.Min(28.0, Math.Max(6.0, zone.Width / 30.0)))
        LabelTexte.Font = New Font(LabelTexte.Font.FontFamily, points, FontStyle.Bold)
        Do While points > 6 AndAlso LabelTexte.PreferredWidth + 20 > zone.Width
            points -= 1
            LabelTexte.Font = New Font(LabelTexte.Font.FontFamily, points, FontStyle.Bold)
        Loop
        ' La fenetre couvre toute l'image (fond transparent, les clics passent) : le message reste centre tout en bas, comme avant, et l'heure
        ' (deux fois plus petite que le message) se pose en haut a gauche.
        Bounds = zone
        LabelHeure.Font = New Font(LabelHeure.Font.FontFamily, Math.Max(5.0F, points / 2.0F), FontStyle.Bold)
        LabelHeure.Location = If(_heureAGauche, New Point(8, 6), New Point(Math.Max(0, zone.Width - LabelHeure.PreferredWidth - 6), 4))          ' (haut a droite ; a gauche pour le numero d'une camera IP)
        LabelTexte.Location = New Point(Math.Max(0, (zone.Width - LabelTexte.PreferredWidth) \ 2), zone.Height - LabelTexte.PreferredHeight - 4)
    End Sub

    Private Sub TimerSuivi_Tick(sender As Object, e As EventArgs) Handles TimerSuivi.Tick
        Try
            Dim z As Rectangle = _suivi()
            ' (a 3 pixels pres : le PIP bouge d'un pixel sans raison, et la legende se redessinait en clignotant)
            If Not z.IsEmpty AndAlso (Math.Abs(z.X - _zone.X) > 3 OrElse Math.Abs(z.Y - _zone.Y) > 3 OrElse Math.Abs(z.Width - _zone.Width) > 3 OrElse Math.Abs(z.Height - _zone.Height) > 3) Then
                Placer(z)
                CalqueWin32.DefinirPremierPlanSeul(Me, True)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub TimerFin_Tick(sender As Object, e As EventArgs) Handles TimerFin.Tick
        TimerFin.Stop()
        Close()
    End Sub

    ''' <summary>La legende ne prend pas le clavier : Slide garde ses touches.</summary>
    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            If Not DesignMode Then cp.ExStyle = cp.ExStyle Or CalqueWin32.WS_EX_NOACTIVATE Or CalqueWin32.WS_EX_TOOLWINDOW
            Return cp
        End Get
    End Property

End Class
