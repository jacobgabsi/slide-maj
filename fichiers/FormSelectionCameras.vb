Option Strict On
Option Explicit On

''' <summary>
''' Choix des cameras d'un ecran de plusieurs cameras (29/10/2026, Jacob : « la possibilite de choisir les cameras dans la minuterie ») : une liste a cocher des 64 cameras de l'enregistreur. L'ecran affiche
''' les cameras cochees (au moins deux), la grille s'adapte a leur nombre. Les controles sont dans le Concepteur.
''' </summary>
Public Class FormSelectionCameras

    ''' <summary>Les cameras cochees, par ordre croissant (valable si la fenetre est validee).</summary>
    Friend Choisies As New List(Of Integer)

    ''' <summary>Les 64 cameras de l'enregistreur ; <paramref name="actuelles"/> : celles qui sont cochees au depart.</summary>
    Friend Sub New(actuelles As IEnumerable(Of Integer))
        InitializeComponent()
        For c As Integer = 1 To NvrIp.NombreDeCanaux
            CheckedListBoxCameras.Items.Add("Caméra " & c, If(actuelles IsNot Nothing, actuelles.Contains(c), False))
        Next
        MettreAJour()
    End Sub

    ''' <summary>Une liste de cameras par leur nom (cameras Blink) ; <paramref name="cochees"/> : leurs numeros d'ordre, a partir de 1, cochees au depart. <see cref="Choisies"/> rend aussi des numeros d'ordre.</summary>
    Friend Sub New(noms As IList(Of String), cochees As IEnumerable(Of Integer))
        InitializeComponent()
        CheckedListBoxCameras.ColumnWidth = 300
        For i As Integer = 0 To noms.Count - 1
            CheckedListBoxCameras.Items.Add(noms(i), If(cochees IsNot Nothing, cochees.Contains(i + 1), False))
        Next
        MettreAJour()
    End Sub

    Private Sub FormSelectionCameras_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

    Private Sub MettreAJour()
        Dim n As Integer = CheckedListBoxCameras.CheckedItems.Count
        LabelCompte.Text = n & " caméra" & If(n > 1, "s", "") & " cochée" & If(n > 1, "s", "") & If(n < 2, " (il en faut au moins deux)", "")
        ButtonOk.Enabled = n >= 2
    End Sub

    Private Sub CheckedListBoxCameras_ItemCheck(sender As Object, e As ItemCheckEventArgs) Handles CheckedListBoxCameras.ItemCheck
        ' (l'etat de la case change APRES cet evenement : le compte se fait une fois l'evenement termine)
        BeginInvoke(New Action(AddressOf MettreAJour))
    End Sub

    Private Sub ButtonToutCocher_Click(sender As Object, e As EventArgs) Handles ButtonToutCocher.Click
        For i As Integer = 0 To CheckedListBoxCameras.Items.Count - 1
            CheckedListBoxCameras.SetItemChecked(i, True)
        Next
    End Sub

    Private Sub ButtonToutDecocher_Click(sender As Object, e As EventArgs) Handles ButtonToutDecocher.Click
        For i As Integer = 0 To CheckedListBoxCameras.Items.Count - 1
            CheckedListBoxCameras.SetItemChecked(i, False)
        Next
    End Sub

    Private Sub FormSelectionCameras_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult <> DialogResult.OK Then Exit Sub
        Choisies.Clear()
        For i As Integer = 0 To CheckedListBoxCameras.Items.Count - 1
            If CheckedListBoxCameras.GetItemChecked(i) Then Choisies.Add(i + 1)
        Next
    End Sub

End Class
