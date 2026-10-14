Imports System.IO

''' <summary>
''' La liste des telechargements de Nflix (voir Telechargements) : ceux qui sont en cours, en attente, termines ou
''' echoues, avec leur avancement et leur vitesse. Demande de Jacob, 13/10/2026 : "rajoute la possibilite de
''' telecharger plusieurs film en simultane". Une seule fenetre, qui se cache a la fermeture.
''' </summary>
Public Class FormTelechargements

    Private Shared _instance As FormTelechargements

    ''' <summary>Montre la fenetre (la cree au besoin).</summary>
    Friend Shared Sub Montrer(proprietaire As Form)
        If _instance Is Nothing OrElse _instance.IsDisposed Then _instance = New FormTelechargements()
        If Not _instance.Visible Then
            If proprietaire IsNot Nothing Then _instance.Show(proprietaire) Else _instance.Show()
        End If
        _instance.WindowState = FormWindowState.Normal
        _instance.BringToFront()
        _instance.Rafraichir()
    End Sub

    Private Function EtatEnTexte(t As TelechargementIptv) As String
        Select Case t.Etat
            Case EtatTelechargement.Attente : Return If(t.Message <> "", t.Message, "En attente")
            Case EtatTelechargement.EnCours : Return "En cours"
            Case EtatTelechargement.Termine : Return "Terminé"
            Case EtatTelechargement.Annule : Return "Annulé (reprise possible)"
            Case EtatTelechargement.Echec : Return "Échec : " & t.Message
        End Select
        Return ""
    End Function

    Private Function AvancementEnTexte(t As TelechargementIptv) As String
        If t.Etat = EtatTelechargement.Attente AndAlso t.Recu = 0 Then Return ""
        If t.Total > 0 Then
            Return Telechargements.TailleLisible(t.Recu) & " / " & Telechargements.TailleLisible(t.Total) & "  (" & CInt(Math.Min(100L, t.Recu * 100L \ t.Total)) & " %)"
        End If
        Return If(t.Recu > 0, Telechargements.TailleLisible(t.Recu), "")
    End Function

    ''' <summary>Remet la grille a jour (les lignes gardent leur place, la selection aussi).</summary>
    Friend Sub Rafraichir()
        If IsDisposed Then Exit Sub
        Dim liste As List(Of TelechargementIptv) = Telechargements.Liste
        GrilleTelechargements.SuspendLayout()
        Try
            Do While GrilleTelechargements.Rows.Count > liste.Count
                GrilleTelechargements.Rows.RemoveAt(GrilleTelechargements.Rows.Count - 1)
            Loop
            Do While GrilleTelechargements.Rows.Count < liste.Count
                GrilleTelechargements.Rows.Add("", "", "", "")
            Loop
            For i As Integer = 0 To liste.Count - 1
                Dim t As TelechargementIptv = liste(i)
                Dim r As DataGridViewRow = GrilleTelechargements.Rows(i)
                r.Tag = t
                r.Cells(0).Value = t.Titre
                r.Cells(1).Value = EtatEnTexte(t)
                r.Cells(2).Value = AvancementEnTexte(t)
                r.Cells(3).Value = If(t.Etat = EtatTelechargement.EnCours AndAlso t.Vitesse > 0, (t.Vitesse / 1048576.0).ToString("0.0") & " Mo/s", "")
            Next
        Finally
            GrilleTelechargements.ResumeLayout()
        End Try
        Dim bilan As String = Telechargements.Bilan()
        LabelBilan.Text = If(bilan = "", If(liste.Count = 0, "Aucun téléchargement.", "Rien en cours."), "Téléchargements : " & bilan) &
                          "     (" & Telechargements.MaxSimultanes & " au plus en même temps)"
    End Sub

    Private Function SelectionnesEtLeurDossier() As List(Of TelechargementIptv)
        Dim l As New List(Of TelechargementIptv)
        For Each r As DataGridViewRow In GrilleTelechargements.SelectedRows
            Dim t As TelechargementIptv = TryCast(r.Tag, TelechargementIptv)
            If t IsNot Nothing Then l.Add(t)
        Next
        Return l
    End Function

    Private Sub FormTelechargements_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        MoletteSousLaSouris.Suivre(Me)
        AddHandler Telechargements.Change, AddressOf QuandCaChange
        TimerRafraichir.Start()
        Rafraichir()
    End Sub

    Private Sub QuandCaChange()
        If Visible Then Rafraichir()
    End Sub

    Private Sub TimerRafraichir_Tick(sender As Object, e As EventArgs) Handles TimerRafraichir.Tick
        If Visible Then Rafraichir()
    End Sub

    Private Sub FormTelechargements_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True                        ' les telechargements continuent : la fenetre se cache seulement
            Hide()
        Else
            RemoveHandler Telechargements.Change, AddressOf QuandCaChange
        End If
    End Sub

    Private Sub ButtonAnnulerTelechargement_Click(sender As Object, e As EventArgs) Handles ButtonAnnulerTelechargement.Click
        For Each t As TelechargementIptv In SelectionnesEtLeurDossier()
            Telechargements.Annuler(t)
        Next
    End Sub

    Private Sub ButtonReprendre_Click(sender As Object, e As EventArgs) Handles ButtonReprendre.Click
        For Each t As TelechargementIptv In SelectionnesEtLeurDossier()
            Telechargements.Reprendre(t)
        Next
    End Sub

    Private Sub ButtonOuvrirLeDossier_Click(sender As Object, e As EventArgs) Handles ButtonOuvrirLeDossier.Click
        Dim choisis As List(Of TelechargementIptv) = SelectionnesEtLeurDossier()
        Dim t As TelechargementIptv = If(choisis.Count > 0, choisis(0), Telechargements.Liste.LastOrDefault())
        If t Is Nothing Then Exit Sub
        Try
            If File.Exists(t.Cible) Then
                Process.Start("explorer.exe", "/select,""" & t.Cible & """")
            ElseIf Directory.Exists(Path.GetDirectoryName(t.Cible)) Then
                Process.Start("explorer.exe", """" & Path.GetDirectoryName(t.Cible) & """")
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ButtonRetirerTermines_Click(sender As Object, e As EventArgs) Handles ButtonRetirerTermines.Click
        Telechargements.RetirerTermines()
    End Sub

    Private Sub ButtonFermer_Click(sender As Object, e As EventArgs) Handles ButtonFermer.Click
        Hide()
    End Sub

End Class
