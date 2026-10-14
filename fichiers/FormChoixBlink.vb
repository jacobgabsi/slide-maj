Option Strict On
Option Explicit On

''' <summary>
''' Choix d'une camera Blink pour une ligne de la minuterie (Jacob, 26/10/2026 : "fonction dans la minuterie Blink avec les memes options qu'un film"). La ligne devient une ligne « IPTV » dont
''' l'adresse est « blink://... » : le reste (PIP, son, coin, taille) est celui d'une chaine. Les controles sont dans le Concepteur.
''' </summary>
Public Class FormChoixBlink

    Private ReadOnly _cameras As List(Of BlinkApi.CameraBlink)

    ''' <summary>La camera choisie (Nothing si annule).</summary>
    Friend Choisie As BlinkApi.CameraBlink

    ''' <summary>Les cameras cochees (bouton « Plusieurs cameras... ») : un ecran de plusieurs cameras Blink ; Nothing si une seule camera a ete choisie.</summary>
    Friend Plusieurs As List(Of BlinkApi.CameraBlink)
    Private _dejaChoisies As List(Of BlinkApi.CameraBlink)

    Friend Sub New(cameras As List(Of BlinkApi.CameraBlink), actuelle As String, Optional dejaChoisies As List(Of BlinkApi.CameraBlink) = Nothing)
        InitializeComponent()
        _cameras = cameras
        _dejaChoisies = dejaChoisies
        For Each c As BlinkApi.CameraBlink In cameras
            ListBoxCameras.Items.Add(c.Nom)
        Next
        Dim i As Integer = cameras.FindIndex(Function(c) c.Nom = actuelle)
        ListBoxCameras.SelectedIndex = If(i >= 0, i, If(cameras.Count > 0, 0, -1))
    End Sub

    Private Sub FormChoixBlink_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

    ''' <summary>Liste a cocher des cameras du compte : l'ecran affichera exactement celles-la.</summary>
    Private Sub ButtonPlusieurs_Click(sender As Object, e As EventArgs) Handles ButtonPlusieurs.Click
        Dim noms As List(Of String) = _cameras.Select(Function(c) c.Nom).ToList()
        Dim coches As New List(Of Integer)
        If _dejaChoisies IsNot Nothing Then
            For i As Integer = 0 To _cameras.Count - 1
                If _dejaChoisies.Any(Function(x) x.Id = _cameras(i).Id AndAlso x.ReseauId = _cameras(i).ReseauId) Then coches.Add(i + 1)
            Next
        End If
        Using f As New FormSelectionCameras(noms, coches)
            If f.ShowDialog(Me) <> DialogResult.OK OrElse f.Choisies.Count < 2 Then Exit Sub
            Plusieurs = f.Choisies.Select(Function(n) _cameras(n - 1)).ToList()
        End Using
        DialogResult = DialogResult.OK
    End Sub

    Private Sub ListBoxCameras_DoubleClick(sender As Object, e As EventArgs) Handles ListBoxCameras.DoubleClick
        If ListBoxCameras.SelectedIndex >= 0 Then DialogResult = DialogResult.OK
    End Sub

    Private Sub FormChoixBlink_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult = DialogResult.OK AndAlso Plusieurs Is Nothing AndAlso ListBoxCameras.SelectedIndex >= 0 Then Choisie = _cameras(ListBoxCameras.SelectedIndex)
    End Sub

End Class
