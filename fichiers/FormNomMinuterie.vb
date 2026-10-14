Option Strict On
Option Explicit On

''' <summary>
''' Nom d'une minuterie a enregistrer (05/11/2026, Jacob) : la liste des minuteries deja presentes dans le repertoire vise
''' (personnel ou collectif) ; le nom se tape ou se prend dans la liste (pour en remplacer une). Les controles sont dans le Concepteur.
''' </summary>
Public Class FormNomMinuterie

    ''' <summary>Le nom choisi (valable si la fenetre est validee).</summary>
    Friend Nom As String = ""

    Friend Sub New(titre As String, explication As String, nomPropose As String, existantes As List(Of MinuteriesEnregistrees.Info))
        InitializeComponent()
        Text = titre
        LabelAide.Text = explication
        TextBoxNom.Text = nomPropose
        For Each i As MinuteriesEnregistrees.Info In existantes.OrderBy(Function(x) x.Nom, StringComparer.CurrentCultureIgnoreCase)
            Dim item As New ListViewItem(i.Nom)
            item.SubItems.Add(i.Enregistree.ToString("dd/MM/yyyy HH:mm"))
            item.SubItems.Add(i.NombreDeLignes.ToString())
            ListViewExistantes.Items.Add(item)
        Next
        LabelExistantes.Text = If(ListViewExistantes.Items.Count = 0, "Aucune minuterie dans ce répertoire pour l'instant.", "Déjà dans ce répertoire (un clic reprend le nom pour la remplacer) :")
    End Sub

    Private Sub FormNomMinuterie_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        ListViewExistantes.BackColor = StylePro.Surface
        ListViewExistantes.ForeColor = StylePro.Texte
        ActiveControl = TextBoxNom
        TextBoxNom.SelectAll()
    End Sub

    Private Sub ListViewExistantes_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListViewExistantes.SelectedIndexChanged
        If ListViewExistantes.SelectedItems.Count > 0 Then TextBoxNom.Text = ListViewExistantes.SelectedItems(0).Text
    End Sub

    Private Sub ListViewExistantes_DoubleClick(sender As Object, e As EventArgs) Handles ListViewExistantes.DoubleClick
        If ListViewExistantes.SelectedItems.Count > 0 Then ButtonOk.PerformClick()
    End Sub

    Private Sub FormNomMinuterie_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult = DialogResult.OK Then Nom = TextBoxNom.Text.Trim()
    End Sub

    ''' <summary>Ouvre la fenetre ; rend le nom choisi, ou "" si annule ou vide.</summary>
    Friend Shared Function Demander(parent As IWin32Window, titre As String, explication As String, nomPropose As String, existantes As List(Of MinuteriesEnregistrees.Info)) As String
        Using f As New FormNomMinuterie(titre, explication, nomPropose, existantes)
            If f.ShowDialog(parent) <> DialogResult.OK Then Return ""
            Return f.Nom
        End Using
    End Function

End Class
