Option Strict On
Option Explicit On

''' <summary>
''' Choix d'une minuterie collective a charger (29/10/2026, Jacob) : la liste des fichiers du dossier data\minuteries collectives. Les controles sont dans le Concepteur.
''' </summary>
Public Class FormChoixCollective

    ''' <summary>La minuterie collective choisie (valable si la fenetre est validee).</summary>
    Friend Choisie As MinuteriesEnregistrees.Info

    Friend Sub New(liste As List(Of MinuteriesEnregistrees.Info))
        InitializeComponent()
        For Each i As MinuteriesEnregistrees.Info In liste
            Dim item As New ListViewItem(i.Nom) With {.Tag = i}
            item.SubItems.Add(i.Enregistree.ToString("dd/MM/yyyy HH:mm"))
            item.SubItems.Add(i.NombreDeLignes.ToString())
            ListViewCollectives.Items.Add(item)
        Next
        If ListViewCollectives.Items.Count > 0 Then ListViewCollectives.Items(0).Selected = True
        ButtonOk.Enabled = ListViewCollectives.Items.Count > 0
    End Sub

    Private Sub FormChoixCollective_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        ListViewCollectives.BackColor = StylePro.Surface
        ListViewCollectives.ForeColor = StylePro.Texte
    End Sub

    Private Sub ListViewCollectives_DoubleClick(sender As Object, e As EventArgs) Handles ListViewCollectives.DoubleClick
        If ListViewCollectives.SelectedItems.Count > 0 Then DialogResult = DialogResult.OK
    End Sub

    Private Sub FormChoixCollective_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult = DialogResult.OK AndAlso ListViewCollectives.SelectedItems.Count > 0 Then Choisie = TryCast(ListViewCollectives.SelectedItems(0).Tag, MinuteriesEnregistrees.Info)
    End Sub

End Class
