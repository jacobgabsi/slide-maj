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

    Private Sub MajBoutons()
        Dim a As Boolean = ListViewCollectives.SelectedItems.Count > 0
        ButtonOk.Enabled = a
        ButtonRenommer.Enabled = a
        ButtonSupprimer.Enabled = a
    End Sub

    Private Sub ListViewCollectives_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListViewCollectives.SelectedIndexChanged
        MajBoutons()
    End Sub

    Private Async Sub ButtonSupprimer_Click(sender As Object, e As EventArgs) Handles ButtonSupprimer.Click
        If ListViewCollectives.SelectedItems.Count = 0 Then Exit Sub
        Dim item As ListViewItem = ListViewCollectives.SelectedItems(0)
        Dim info As MinuteriesEnregistrees.Info = DirectCast(item.Tag, MinuteriesEnregistrees.Info)
        If MessageBox.Show(Me, "Supprimer la minuterie collective « " & info.Nom & " » (" & info.NombreDeLignes & " ligne(s)) sur TOUS les postes ?" & vbCrLf & vbCrLf & "Votre minuterie actuelle n'est pas touchée. Un poste éteint la supprimera à son prochain démarrage.", "Minuteries collectives", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        Enabled = False
        Try
            Dim diffusee As Boolean = Await MinuteriesEnregistrees.SupprimerCollective(info)
            ListViewCollectives.Items.Remove(item)
            If Not diffusee Then MessageBox.Show(Me, "Supprimée sur ce PC, mais la suppression n'a pas pu être envoyée aux autres postes (réseau). Elle sera envoyée par le poste de distribution dès qu'il la connaîtra.", "Minuteries collectives", MessageBoxButtons.OK, MessageBoxIcon.Information)
        Catch ex As Exception
            MessageBox.Show(Me, "Suppression impossible : " & ex.Message, "Minuteries collectives", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Finally
            Enabled = True
            MajBoutons()
        End Try
    End Sub

    Private Async Sub ButtonRenommer_Click(sender As Object, e As EventArgs) Handles ButtonRenommer.Click
        If ListViewCollectives.SelectedItems.Count = 0 Then Exit Sub
        Dim item As ListViewItem = ListViewCollectives.SelectedItems(0)
        Dim info As MinuteriesEnregistrees.Info = DirectCast(item.Tag, MinuteriesEnregistrees.Info)
        Dim nom As String = Microsoft.VisualBasic.Interaction.InputBox("Nouveau nom de la minuterie collective « " & info.Nom & " » (renommée sur tous les postes) :", "Renommer la minuterie collective", info.Nom).Trim()
        If nom = "" OrElse String.Equals(nom, info.Nom, StringComparison.CurrentCultureIgnoreCase) Then Exit Sub
        If MinuteriesEnregistrees.Existe(nom, MinuteriesEnregistrees.DossierCollectif()) Then
            MessageBox.Show(Me, "« " & nom & " » existe déjà.", "Minuteries collectives", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Enabled = False
        Try
            If Await MinuteriesEnregistrees.RenommerCollective(info, nom) Then
                Dim nouvelle As MinuteriesEnregistrees.Info = MinuteriesEnregistrees.Lister(MinuteriesEnregistrees.DossierCollectif()).FirstOrDefault(Function(i) String.Equals(i.Nom, nom, StringComparison.CurrentCultureIgnoreCase))
                If nouvelle IsNot Nothing Then
                    item.Text = nouvelle.Nom
                    item.SubItems(1).Text = nouvelle.Enregistree.ToString("dd/MM/yyyy HH:mm")
                    item.Tag = nouvelle
                End If
            End If
        Catch ex As Exception
            MessageBox.Show(Me, "Renommage impossible : " & ex.Message, "Minuteries collectives", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        Finally
            Enabled = True
            MajBoutons()
        End Try
    End Sub

    Private Sub FormChoixCollective_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult = DialogResult.OK AndAlso ListViewCollectives.SelectedItems.Count > 0 Then Choisie = TryCast(ListViewCollectives.SelectedItems(0).Tag, MinuteriesEnregistrees.Info)
    End Sub

End Class
