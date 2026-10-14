''' <summary>
''' Aide des touches de Slide, a l'ecran (demande de Jacob, 15/10/2026 : "si j'appuie sur 'H' affiche un recap des
''' touches a l'ecran"). Touche H dans la fenetre de Slide ; elle se referme en cliquant dessus, en appuyant de nouveau
''' sur H, ou toute seule apres 15 secondes. Elle ne prend pas le clavier : Slide continue de recevoir les touches.
''' Les textes sont dans le Concepteur (LabelTouches et LabelActions : une ligne par touche, dans le meme ordre).
''' </summary>
Public Class FormAideTouches

    Private Shared _instance As FormAideTouches

    ''' <summary>Vrai tant que l'aide est a l'ecran.</summary>
    Friend Shared ReadOnly Property Visible_ As Boolean
        Get
            Return _instance IsNot Nothing AndAlso Not _instance.IsDisposed AndAlso _instance.Visible
        End Get
    End Property

    ''' <summary>H : montre l'aide, ou la cache si elle est deja la.</summary>
    Friend Shared Sub Basculer(principale As Form)
        If Visible_ Then
            Cacher()
            Exit Sub
        End If
        If _instance Is Nothing OrElse _instance.IsDisposed Then _instance = New FormAideTouches()
        _instance.PlacerSur(principale)
        _instance.TopMost = principale IsNot Nothing AndAlso principale.TopMost          ' (Nflix en plein ecran est au premier plan : l'aide doit l'etre aussi)
        _instance.Show()
        _instance.MinuteurFermeture.Stop()
        _instance.MinuteurFermeture.Start()
    End Sub

    Friend Shared Sub Cacher()
        If Visible_ Then
            _instance.MinuteurFermeture.Stop()
            _instance.Hide()
        End If
    End Sub

    ''' <summary>Au centre de l'image de Slide, ou de l'ecran si elle n'est pas visible.</summary>
    Private Sub PlacerSur(principale As Form)
        Dim zone As Rectangle = Screen.PrimaryScreen.WorkingArea
        Try
            If principale IsNot Nothing AndAlso Not principale.IsDisposed AndAlso principale.Visible Then
                zone = principale.RectangleToScreen(principale.ClientRectangle)
            End If
        Catch ex As Exception
        End Try
        Me.Location = New Point(zone.Left + Math.Max(0, (zone.Width - Me.Width) \ 2), zone.Top + Math.Max(0, (zone.Height - Me.Height) \ 2))
    End Sub

    ''' <summary>La fenetre ne prend pas le focus : le clavier reste a Slide (N, P, M, H continuent de marcher).</summary>
    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    Private Sub FormAideTouches_Click(sender As Object, e As EventArgs) Handles MyBase.Click, LabelTitre.Click, LabelTouches.Click, LabelActions.Click, LabelPied.Click
        Cacher()
    End Sub

    Private Sub MinuteurFermeture_Tick(sender As Object, e As EventArgs) Handles MinuteurFermeture.Tick
        Cacher()
    End Sub

    Private Sub FormAideTouches_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        Cacher()
    End Sub

End Class
