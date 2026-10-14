''' <summary>
''' Fetes juives dans la minuterie (20/10/2026) : la liste des fetes a venir (calendrier hebraique de la bibliotheque Zmanim, 13 mois) avec une case
''' chacune, et une date saisie a la main. La LOCALITE se choisit en haut : elle fixe les horaires (zmanim de la ville) et la duree des fetes (un jour
''' en Israel, deux ailleurs). "Inserer" ajoute a la minuterie l'entree et/ou la sortie des fetes cochees (YomTov.Inserer), aux horaires de leur jour.
''' Les annonces elles-memes sont jouees par Module1.Minuterie (type "Yom Tov", voir YomTov.Annoncer).
''' </summary>
Public Class FormYomTov

    Private _preCochees As List(Of String) = New List(Of String)
    Private ReadOnly _manuelles As New List(Of YomTov.Fete)
    Private _chargement As Boolean = True

    ''' <summary>Montre la fenetre ; si on valide, insere les annonces dans la minuterie et montre le compte rendu. <paramref name="preCochees"/> :
    ''' les fetes (YomTov.Fete.Cle) deja cochees (rien d'autre n'est coche d'avance). <paramref name="ligneLibre"/> : une ligne de la minuterie que la
    ''' premiere annonce remplit (-1 : aucune). Rend Vrai si la fenetre a ete validee.</summary>
    Friend Shared Function Choisir(proprietaire As IWin32Window, preCochees As List(Of String), ligneLibre As Integer) As Boolean
        Using f As New FormYomTov()
            f._preCochees = If(preCochees, New List(Of String))
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            ' Pas de fete saisie : deux lignes (entree, sortie) inserees telles quelles, a regler a la main (jour, heure, date...).
            If f._lignesAReglerALaMain Then
                MessageBox.Show(proprietaire, YomTov.InsererLignesAReglerALaMain(f.CheckBoxEntree.Checked, f.CheckBoxSortie.Checked, ligneLibre), "Fêtes juives", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Return True
            End If
            Dim choisies As List(Of YomTov.Fete) = f.FetesChoisies()
            If choisies.Count = 0 Then Return False
            Dim bilan As String = YomTov.Inserer(choisies, f.CheckBoxEntree.Checked, f.CheckBoxSortie.Checked, ligneLibre, f.VilleChoisie())
            MessageBox.Show(proprietaire, bilan, "Fêtes juives", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return True
        End Using
    End Function

    Private Function VilleChoisie() As String
        Return Convert.ToString(ComboVille.SelectedItem)
    End Function

    Private _lignesAReglerALaMain As Boolean = False

    ''' <summary>"Insérer 2 lignes à régler à la main" : aucune fête n'est saisie ; une ligne d'entrée et une ligne de sortie sont ajoutées à la minuterie.</summary>
    Private Sub ButtonVide_Click(sender As Object, e As EventArgs) Handles ButtonVide.Click
        If Not CheckBoxEntree.Checked AndAlso Not CheckBoxSortie.Checked Then
            MessageBox.Show(Me, "Cochez « Insérer l'entrée », « Insérer la sortie », ou les deux.", "Fêtes juives", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        _lignesAReglerALaMain = True
        DialogResult = DialogResult.OK
    End Sub

    ''' <summary>Les fetes cochees ; sans case cochee, celles qui sont selectionnees dans la liste.</summary>
    Private Function FetesChoisies() As List(Of YomTov.Fete)
        Dim choisies As New List(Of YomTov.Fete)
        For Each ligne As ListViewItem In ListViewFetes.Items
            If ligne.Checked Then choisies.Add(DirectCast(ligne.Tag, YomTov.Fete))
        Next
        If choisies.Count = 0 Then
            For Each ligne As ListViewItem In ListViewFetes.SelectedItems
                choisies.Add(DirectCast(ligne.Tag, YomTov.Fete))
            Next
        End If
        Return choisies
    End Function

    ''' <summary>"Inserer" sans rien de choisi (aucune fete cochee, ni entree ni sortie) : la fenetre dit quoi faire et reste ouverte.</summary>
    Private Sub FormYomTov_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult <> DialogResult.OK OrElse _lignesAReglerALaMain Then Exit Sub
        If FetesChoisies().Count = 0 Then
            MessageBox.Show(Me, "Cochez au moins une fête dans la liste (ou ajoutez une date à la main), puis cliquez sur « Insérer ».", "Fêtes juives", MessageBoxButtons.OK, MessageBoxIcon.Information)
            e.Cancel = True
        ElseIf Not CheckBoxEntree.Checked AndAlso Not CheckBoxSortie.Checked Then
            MessageBox.Show(Me, "Cochez « Insérer l'entrée », « Insérer la sortie », ou les deux.", "Fêtes juives", MessageBoxButtons.OK, MessageBoxIcon.Information)
            e.Cancel = True
        End If
    End Sub

    Private Sub FormYomTov_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        DateEntree.Value = Date.Today
        DateSortie.Value = Date.Today.AddDays(1)
        ' Localites : celles des reglages (liste "Ville" du cadre Sabbat) ; par defaut la ville affichee dans les reglages.
        For Each ville As String In YomTov.Villes()
            ComboVille.Items.Add(ville)
        Next
        Dim voulue As String = YomTov.VilleDesReglages()
        Dim place As Integer = ComboVille.FindStringExact(voulue)
        If place >= 0 Then ComboVille.SelectedIndex = place Else If ComboVille.Items.Count > 0 Then ComboVille.SelectedIndex = 0
        _chargement = False
        Remplir()
    End Sub

    ''' <summary>La liste : les fetes calculees pour la localite choisie, puis celles saisies a la main. Les cases cochees sont gardees.</summary>
    Private Sub Remplir()
        Dim cochees As New HashSet(Of String)(_preCochees)
        For Each ligne As ListViewItem In ListViewFetes.Items
            If ligne.Checked Then cochees.Add(DirectCast(ligne.Tag, YomTov.Fete).Cle) Else cochees.Remove(DirectCast(ligne.Tag, YomTov.Fete).Cle)
        Next
        _preCochees.Clear()
        ListViewFetes.Items.Clear()
        Try
            For Each fete As YomTov.Fete In YomTov.Calculer(Date.Today, Date.Today.AddMonths(13), VilleChoisie())
                If fete.Sortie < Date.Today Then Continue For
                Ajouter(fete, cochees.Contains(fete.Cle))
            Next
        Catch ex As Exception
            MessageBox.Show(Me, "Les fêtes n'ont pas pu être calculées : " & ex.Message, "Fêtes juives", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
        For Each fete As YomTov.Fete In _manuelles
            Ajouter(fete, True)
        Next
    End Sub

    Private Sub Ajouter(fete As YomTov.Fete, cochee As Boolean)
        Dim ville As String = VilleChoisie()
        Dim entree As String = fete.Entree.ToString("dddd d MMMM yyyy") & If(fete.EntreeAnnoncee, " à " & YomTov.HeureEntree(fete, ville), " (Chabbat Chalom)")
        Dim sortie As String = fete.Sortie.ToString("dddd d MMMM yyyy") & If(fete.SortieAnnoncee, " à " & YomTov.HeureSortie(fete, ville), " (déjà annoncée)")
        Dim ligne As New ListViewItem(fete.Nom) With {.Tag = fete, .Checked = cochee}
        ligne.SubItems.Add(entree)
        ligne.SubItems.Add(sortie)
        ListViewFetes.Items.Add(ligne)
    End Sub

    Private Sub ComboVille_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboVille.SelectedIndexChanged
        If _chargement Then Exit Sub
        Remplir()
    End Sub

    Private Sub ButtonAjouterManuel_Click(sender As Object, e As EventArgs) Handles ButtonAjouterManuel.Click
        If DateSortie.Value.Date < DateEntree.Value.Date Then
            MessageBox.Show(Me, "Le jour de sortie ne peut pas être avant le jour d'entrée.", "Fêtes juives", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Exit Sub
        End If
        Dim nom As String = TextBoxNom.Text.Trim()
        If nom = "" Then nom = "Fête"
        Dim fete As New YomTov.Fete With {.Nom = nom, .Entree = DateEntree.Value.Date, .Sortie = DateSortie.Value.Date}
        _manuelles.Add(fete)
        Ajouter(fete, True)
        ListViewFetes.EnsureVisible(ListViewFetes.Items.Count - 1)
    End Sub

End Class
