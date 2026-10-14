Option Strict On
Option Explicit On

''' <summary>
''' Choix de la camera de l'enregistreur (1 a 64) quand on choisit le type « Caméra IP » dans la minuterie (Jacob, 29/10/2026 : « il faut que d'abord je choisisse une camera avant d'afficher la camera 1 »),
''' ou d'un ecran de 4, 8, 16, 32 ou 64 cameras (« Ecran 4, 8, 16, 32, 64 cameras »). Les controles sont dans le Concepteur.
''' </summary>
Public Class FormChoixCameraIp

    ''' <summary>Canal et flux choisis (valables si la fenetre est validee).</summary>
    Friend Canal As Integer = 1
    Friend Flux As Integer = 1
    ''' <summary>Nombre de cameras a l'ecran : 1 = une seule camera (Canal, Flux) ; sinon un ecran qui commence a la camera Canal.</summary>
    Friend Nombre As Integer = 1
    ''' <summary>Cameras choisies une a une (bouton « Plusieurs cameras... ») ; Nothing si l'ecran est une camera seule ou une suite de cameras.</summary>
    Friend Selection As List(Of Integer)
    Private _selectionActuelle As List(Of Integer)

    Friend Sub New(canalActuel As Integer, fluxActuel As Integer, nombreActuel As Integer, Optional selectionActuelle As List(Of Integer) = Nothing)
        InitializeComponent()
        _selectionActuelle = selectionActuelle
        For c As Integer = 1 To NvrIp.NombreDeCanaux
            ListBoxCameras.Items.Add("Caméra " & c)
        Next
        For Each nb As Integer In NvrIp.EcransPossibles
            ComboBoxEcran.Items.Add(If(nb = 1, "1 caméra", nb & " caméras"))
        Next
        ComboBoxEcran.SelectedIndex = Math.Max(0, Array.IndexOf(NvrIp.EcransPossibles, nombreActuel))
        ListBoxCameras.SelectedIndex = Math.Max(0, Math.Min(NvrIp.NombreDeCanaux, canalActuel) - 1)
        CheckBoxLeger.Checked = (fluxActuel = 2)
        MettreAJour()
    End Sub

    Private Sub FormChoixCameraIp_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

    Private Function NombreChoisi() As Integer
        Return If(ComboBoxEcran.SelectedIndex >= 0, NvrIp.EcransPossibles(ComboBoxEcran.SelectedIndex), 1)
    End Function

    ''' <summary>Plusieurs cameras : l'ecran joue toujours les flux legers (la case est inutile) ; la liste designe la camera de la case 1 de l'ecran.</summary>
    Private Sub MettreAJour()
        Dim plusieurs As Boolean = NombreChoisi() > 1
        CheckBoxLeger.Enabled = Not plusieurs
        LabelEcran.Text = If(plusieurs, "Écran (1ère caméra) :", "Écran :")
    End Sub

    Private Sub ComboBoxEcran_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxEcran.SelectedIndexChanged
        MettreAJour()
    End Sub

    ''' <summary>Liste a cocher des cameras : l'ecran affichera exactement celles-la.</summary>
    Private Sub ButtonSelection_Click(sender As Object, e As EventArgs) Handles ButtonSelection.Click
        Using f As New FormSelectionCameras(_selectionActuelle)
            If f.ShowDialog(Me) <> DialogResult.OK OrElse f.Choisies.Count < 2 Then Exit Sub
            Selection = f.Choisies.ToList()
        End Using
        DialogResult = DialogResult.OK
    End Sub

    Private Sub ListBoxCameras_DoubleClick(sender As Object, e As EventArgs) Handles ListBoxCameras.DoubleClick
        If ListBoxCameras.SelectedIndex >= 0 Then DialogResult = DialogResult.OK
    End Sub

    Private Sub FormChoixCameraIp_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult <> DialogResult.OK Then Exit Sub
        If Selection IsNot Nothing Then
            Nombre = Selection.Count
            Exit Sub
        End If
        If ListBoxCameras.SelectedIndex < 0 Then Exit Sub
        Nombre = NombreChoisi()
        Canal = ListBoxCameras.SelectedIndex + 1
        If Nombre > 1 Then Canal = ((Canal - 1) \ Nombre) * Nombre + 1          ' (l'ecran commence au debut de sa page : 1, 17, 33, 49 pour 16 cameras)
        Flux = If(CheckBoxLeger.Checked, 2, 1)
    End Sub

End Class
