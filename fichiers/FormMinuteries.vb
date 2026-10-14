Imports Newtonsoft.Json.Linq

''' <summary>
''' Fenetre « Minuteries enregistrees » (bouton « Minuteries... » de l'onglet Minuterie, 28/10/2026) : enregistrer la minuterie actuelle sous un nom (toutes ses lignes ou celles que l'on choisit),
''' charger a la main une minuterie enregistree (toutes ses lignes ou celles que l'on choisit, en remplacant la minuterie actuelle ou en les ajoutant), renommer, supprimer. Voir MinuteriesEnregistrees.
''' Les controles sont dans le Concepteur.
''' </summary>
Public Class FormMinuteries

    Private _liste As New List(Of MinuteriesEnregistrees.Info)

    Public Sub New()
        InitializeComponent()          ' (constructeur sans parametre : requis par le Concepteur)
    End Sub

    Friend Shared Sub Montrer(proprietaire As IWin32Window)
        Using f As New FormMinuteries
            f.ShowDialog(proprietaire)
        End Using
    End Sub

    Private Sub FormMinuteries_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        ListViewMinuteries.BackColor = StylePro.Surface
        ListViewMinuteries.ForeColor = StylePro.Texte
        Actualiser()
    End Sub

    Private Sub Actualiser(Optional selection As String = Nothing)
        _liste = MinuteriesEnregistrees.Lister()
        ListViewMinuteries.Items.Clear()
        For Each i As MinuteriesEnregistrees.Info In _liste
            Dim item As New ListViewItem(i.Nom) With {.Tag = i}
            item.SubItems.Add(i.Enregistree.ToString("dd/MM/yyyy HH:mm"))
            item.SubItems.Add(i.NombreDeLignes.ToString())
            ListViewMinuteries.Items.Add(item)
            If selection IsNot Nothing AndAlso String.Equals(i.Nom, selection, StringComparison.CurrentCultureIgnoreCase) Then item.Selected = True
        Next
        If ListViewMinuteries.SelectedItems.Count = 0 AndAlso ListViewMinuteries.Items.Count > 0 Then ListViewMinuteries.Items(0).Selected = True
        MettreAJourLesBoutons()
    End Sub

    Private Function Choisie() As MinuteriesEnregistrees.Info
        If ListViewMinuteries.SelectedItems.Count = 0 Then Return Nothing
        Return TryCast(ListViewMinuteries.SelectedItems(0).Tag, MinuteriesEnregistrees.Info)
    End Function

    Private Sub MettreAJourLesBoutons()
        Dim a As Boolean = (Choisie() IsNot Nothing)
        ButtonCharger.Enabled = a
        ButtonRenommer.Enabled = a
        ButtonSupprimer.Enabled = a
    End Sub

    Private Sub ListViewMinuteries_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListViewMinuteries.SelectedIndexChanged
        MettreAJourLesBoutons()
    End Sub

    Private Sub ListViewMinuteries_DoubleClick(sender As Object, e As EventArgs) Handles ListViewMinuteries.DoubleClick
        If Choisie() IsNot Nothing Then ButtonCharger.PerformClick()
    End Sub

    ''' <summary>Enregistrer : un nom, puis les lignes de la minuterie actuelle a garder.</summary>
    Private Sub ButtonEnregistrer_Click(sender As Object, e As EventArgs) Handles ButtonEnregistrer.Click
        Dim nom As String = Microsoft.VisualBasic.Interaction.InputBox("Nom de la minuterie (par exemple : Semaine, Vacances, Fêtes).", "Enregistrer la minuterie actuelle", "Minuterie du " & Date.Now.ToString("dd-MM-yyyy")).Trim()
        If nom = "" Then Exit Sub
        If MinuteriesEnregistrees.Existe(nom) AndAlso MessageBox.Show(Me, "« " & nom & " » existe déjà. La remplacer ?", "Minuteries", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        Dim lignes As JArray = MinuteriesEnregistrees.LignesActuelles()
        If lignes.Count = 0 Then
            MessageBox.Show(Me, "La minuterie actuelle n'a aucune ligne.", "Minuteries", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim choisies As JArray = Nothing, remplacer As Boolean
        If Not FormChoixLignes.Demander(Me, "Lignes à enregistrer", "Minuterie « " & nom & " » : cochez les lignes de la minuterie actuelle à garder.", "Enregistrer", lignes, False, choisies, remplacer) Then Exit Sub
        If MinuteriesEnregistrees.Enregistrer(nom, choisies) Then Actualiser(nom)
    End Sub

    ''' <summary>Charger : les lignes de la minuterie choisie a reprendre, et remplacer ou ajouter.</summary>
    Private Sub ButtonCharger_Click(sender As Object, e As EventArgs) Handles ButtonCharger.Click
        Dim info As MinuteriesEnregistrees.Info = Choisie()
        If info Is Nothing Then Exit Sub
        ChargerDepuis(info, False)
    End Sub

    ''' <summary>Enregistrer dans les minuteries collectives (data\minuteries collectives, dossier public publie avec les mises a jour). Les lignes qui contiennent un identifiant ou un mot de passe
    ''' (adresse avec compte, mot de passe, jeton) ne sont jamais enregistrees la : ce dossier part sur un depot public.</summary>
    Private Sub ButtonEnregistrerCollectif_Click(sender As Object, e As EventArgs) Handles ButtonEnregistrerCollectif.Click
        Dim collectif As String = MinuteriesEnregistrees.DossierCollectif()
        Dim nom As String = Microsoft.VisualBasic.Interaction.InputBox("Nom de la minuterie collective (par exemple : Semaine type, Fêtes, Vacances). Elle sera publiée avec les mises à jour de Slide.", "Enregistrer dans les minuteries collectives", "").Trim()
        If nom = "" Then Exit Sub
        If MinuteriesEnregistrees.Existe(nom, collectif) AndAlso MessageBox.Show(Me, "La minuterie collective « " & nom & " » existe déjà. La remplacer ?", "Minuteries collectives", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        Dim lignes As JArray = MinuteriesEnregistrees.LignesActuelles()
        If lignes.Count = 0 Then
            MessageBox.Show(Me, "La minuterie actuelle n'a aucune ligne.", "Minuteries collectives", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim choisies As JArray = Nothing, remplacer As Boolean
        If Not FormChoixLignes.Demander(Me, "Lignes à enregistrer dans les collectives", "Minuterie collective « " & nom & " » : cochez les lignes de la minuterie actuelle à partager.", "Enregistrer", lignes, False, choisies, remplacer) Then Exit Sub
        Dim sures As JArray = MinuteriesEnregistrees.SansSecrets(choisies)
        Dim ecartees As Integer = choisies.Count - sures.Count
        If sures.Count = 0 Then
            MessageBox.Show(Me, "Aucune des lignes choisies ne peut être partagée : elles contiennent toutes un identifiant ou un mot de passe.", "Minuteries collectives", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End If
        If MinuteriesEnregistrees.Enregistrer(nom, sures, collectif) Then
            MessageBox.Show(Me, "« " & nom & " » est enregistrée dans les minuteries collectives (" & sures.Count & " ligne(s))." &
                            If(ecartees > 0, vbCrLf & vbCrLf & ecartees & " ligne(s) n'ont pas été enregistrées car elles contiennent un identifiant ou un mot de passe (adresse avec compte, jeton...) : ce dossier est public.", "") &
                            vbCrLf & vbCrLf & "Elle sera publiée avec la prochaine mise à jour de Slide et apparaîtra sur les autres postes.", "Minuteries collectives", MessageBoxButtons.OK, MessageBoxIcon.Information)
        End If
    End Sub

    ''' <summary>Charger une minuterie collective : on la choisit, puis ses lignes a reprendre (pour les modifier sur ce PC), en remplacant la minuterie actuelle ou en les ajoutant.</summary>
    Private Sub ButtonChargerCollectif_Click(sender As Object, e As EventArgs) Handles ButtonChargerCollectif.Click
        Dim liste As List(Of MinuteriesEnregistrees.Info) = MinuteriesEnregistrees.Lister(MinuteriesEnregistrees.DossierCollectif())
        If liste.Count = 0 Then
            MessageBox.Show(Me, "Aucune minuterie collective pour l'instant." & vbCrLf & vbCrLf & "Dossier : " & MinuteriesEnregistrees.DossierCollectif(), "Minuteries collectives", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim info As MinuteriesEnregistrees.Info
        Using f As New FormChoixCollective(liste.OrderBy(Function(i) i.Nom, StringComparer.CurrentCultureIgnoreCase).ToList())
            If f.ShowDialog(Me) <> DialogResult.OK OrElse f.Choisie Is Nothing Then Exit Sub
            info = f.Choisie
        End Using
        ChargerDepuis(info, True)
    End Sub

    ''' <summary>Les lignes a reprendre de <paramref name="info"/>, puis le chargement (remplacement ou ajout). <paramref name="collective"/> : elle vient du dossier des minuteries collectives.</summary>
    Private Sub ChargerDepuis(info As MinuteriesEnregistrees.Info, collective As Boolean)
        Dim lignes As JArray
        Try
            lignes = MinuteriesEnregistrees.Lignes(info)
        Catch ex As Exception
            MessageBox.Show(Me, "Minuterie illisible : " & ex.Message, "Minuteries", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        End Try
        Dim choisies As JArray = Nothing, remplacer As Boolean
        If Not FormChoixLignes.Demander(Me, "Charger « " & info.Nom & " »" & If(collective, " (collective)", ""), "Cochez les lignes à reprendre. La minuterie actuelle est d'abord enregistrée sous « Avant chargement ... ».", "Charger", lignes, True, choisies, remplacer) Then Exit Sub
        Dim question As String = If(remplacer, "La minuterie actuelle sera REMPLACÉE par ", "Les lignes seront AJOUTÉES à la minuterie actuelle : ") & choisies.Count & " ligne(s) de « " & info.Nom & " ». Continuer ?"
        If MessageBox.Show(Me, question, "Charger une minuterie", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        Cursor = Cursors.WaitCursor
        Try
            If MinuteriesEnregistrees.Charger(choisies, remplacer, If(collective, info.Nom & " (collective)", info.Nom)) Then
                If collective Then Actualiser() Else Actualiser(info.Nom)          ' (la liste de ce PC reçoit « Avant chargement de ... » : elle est relue)
                Dim bilan As String = If(remplacer, choisies.Count & " ligne(s) en remplacement de la minuterie actuelle.",
                                         MinuteriesEnregistrees.DernierAjout & " ligne(s) ajoutée(s) à la minuterie actuelle" &
                                         If(MinuteriesEnregistrees.DejaPresentes > 0, " ; " & MinuteriesEnregistrees.DejaPresentes & " ignorée(s) car déjà présente(s) à l'identique (même jour, heure, type, action).", "."))
                MessageBox.Show(Me, "« " & info.Nom & " » est chargée : " & bilan, "Minuteries", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
        Finally
            Cursor = Cursors.Default
        End Try
    End Sub

    Private Sub ButtonRenommer_Click(sender As Object, e As EventArgs) Handles ButtonRenommer.Click
        Dim info As MinuteriesEnregistrees.Info = Choisie()
        If info Is Nothing Then Exit Sub
        Dim nom As String = Microsoft.VisualBasic.Interaction.InputBox("Nouveau nom de « " & info.Nom & " » :", "Renommer la minuterie", info.Nom).Trim()
        If nom = "" OrElse String.Equals(nom, info.Nom, StringComparison.CurrentCultureIgnoreCase) Then Exit Sub
        If MinuteriesEnregistrees.Existe(nom) Then
            MessageBox.Show(Me, "« " & nom & " » existe déjà.", "Minuteries", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        If MinuteriesEnregistrees.Renommer(info, nom) Then Actualiser(nom)
    End Sub

    Private Sub ButtonSupprimer_Click(sender As Object, e As EventArgs) Handles ButtonSupprimer.Click
        Dim info As MinuteriesEnregistrees.Info = Choisie()
        If info Is Nothing Then Exit Sub
        If MessageBox.Show(Me, "Supprimer « " & info.Nom & " » (" & info.NombreDeLignes & " ligne(s)) ? La minuterie actuelle n'est pas touchée.", "Minuteries", MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        Try
            MinuteriesEnregistrees.Supprimer(info)
        Catch ex As Exception
            MessageBox.Show(Me, "Suppression impossible : " & ex.Message, "Minuteries", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        Actualiser()
    End Sub

End Class
