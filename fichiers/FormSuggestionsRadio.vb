Imports System.Threading

''' <summary>
''' Radios proposees sur Internet (annuaire radio-browser.info, voir AnnuaireRadios), a ajouter a la liste (20/10/2026, bouton "+" du cadre
''' Radio). Pays, style et nom filtrent ; les plus ecoutees viennent d'abord ; celles deja dans la liste sont signalees et ne sont pas cochables.
''' "Ecouter la radio choisie" essaie la radio selectionnee (un second clic arrete). "Ajouter les cochees" rend la selection a l'appelant.
''' </summary>
Public Class FormSuggestionsRadio

    Private _noms As New List(Of String)
    Private _adresses As New List(Of String)
    Private _recherche As CancellationTokenSource
    Private _essaiEnCours As Boolean
    Private _enMiseAJour As Boolean

    ''' <summary>Radios cochees, apres "Ajouter les cochees".</summary>
    Friend ReadOnly Choisies As New List(Of AnnuaireRadios.RadioProposee)

    ''' <summary>Montre la fenetre. <paramref name="noms"/> et <paramref name="adresses"/> : la liste actuelle. Rend Vrai si des radios sont choisies
    ''' (dans <paramref name="choisies"/>).</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, noms As IEnumerable(Of String), adresses As IEnumerable(Of String),
                                    choisies As List(Of AnnuaireRadios.RadioProposee)) As Boolean
        Using f As New FormSuggestionsRadio()
            f._noms.AddRange(noms.Select(Function(n) If(n, "").Trim()))
            f._adresses.AddRange(adresses.Select(Function(a) If(a, "").Trim()))
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            choisies.AddRange(f.Choisies)
            Return choisies.Count > 0
        End Using
    End Function

    Private Sub FormSuggestionsRadio_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        For Each p As KeyValuePair(Of String, String) In AnnuaireRadios.Pays
            ComboBoxPays.Items.Add(p.Key)
        Next
        ComboBoxPays.SelectedIndex = 0                                  ' France
        For Each s As KeyValuePair(Of String, String) In AnnuaireRadios.Styles
            ComboBoxStyle.Items.Add(s.Key)
        Next
        ComboBoxStyle.SelectedIndex = 0
        ButtonEcouter.Enabled = False
    End Sub

    Private Sub FormSuggestionsRadio_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        Chercher()                                                      ' les radios les plus ecoutees du pays, tout de suite
    End Sub

    Private Sub ButtonChercher_Click(sender As Object, e As EventArgs) Handles ButtonChercher.Click
        Chercher()
    End Sub

    Private Async Sub Chercher()
        ArreterLEssai()
        If _recherche IsNot Nothing Then _recherche.Cancel()
        _recherche = New CancellationTokenSource()
        Dim jeton As CancellationToken = _recherche.Token
        Dim pays As String = AnnuaireRadios.Pays(Math.Max(0, ComboBoxPays.SelectedIndex)).Value
        Dim style As String = AnnuaireRadios.Styles(Math.Max(0, ComboBoxStyle.SelectedIndex)).Value
        Dim nom As String = TextBoxNom.Text.Trim()
        LabelEtat.Text = "Recherche des radios..."
        ButtonChercher.Enabled = False
        ListViewRadios.Items.Clear()
        ButtonAjouter.Enabled = False
        Try
            Dim trouvees As List(Of AnnuaireRadios.RadioProposee) = Await AnnuaireRadios.Chercher(pays, nom, style, 120, jeton)
            If jeton.IsCancellationRequested OrElse Me.IsDisposed Then Exit Sub
            _enMiseAJour = True
            ListViewRadios.BeginUpdate()
            For Each r As AnnuaireRadios.RadioProposee In trouvees
                Dim deja As Boolean = _noms.Any(Function(n) String.Equals(n, r.Nom, StringComparison.OrdinalIgnoreCase)) OrElse
                                      _adresses.Any(Function(a) String.Equals(a, r.Adresse, StringComparison.OrdinalIgnoreCase))
                Dim ligne As New ListViewItem(r.Nom) With {.Tag = r}
                ligne.SubItems.Add(r.Format)
                ligne.SubItems.Add(r.Pays)
                ligne.SubItems.Add(If(deja, "déjà dans la liste", ""))
                If deja Then ligne.ForeColor = SystemColors.GrayText
                ListViewRadios.Items.Add(ligne)
            Next
            ListViewRadios.EndUpdate()
            _enMiseAJour = False
            LabelEtat.Text = If(trouvees.Count = 0, "Aucune radio trouvée : changez le pays, le style ou le nom.",
                                trouvees.Count & " radio" & If(trouvees.Count > 1, "s", "") & " (les plus écoutées d'abord). Cochez celles à ajouter.")
        Catch ex As OperationCanceledException
            ' une autre recherche a pris le relais
        Catch ex As Exception
            If Not Me.IsDisposed Then LabelEtat.Text = "La recherche a échoué : " & ex.Message
            DiagLog("Radios (annuaire) : " & ex.Message)
        Finally
            If Not Me.IsDisposed Then ButtonChercher.Enabled = True
        End Try
    End Sub

    ' Une radio deja dans la liste ne se coche pas.
    Private Sub ListViewRadios_ItemCheck(sender As Object, e As ItemCheckEventArgs) Handles ListViewRadios.ItemCheck
        If _enMiseAJour Then Exit Sub
        Dim ligne As ListViewItem = ListViewRadios.Items(e.Index)
        If e.NewValue = CheckState.Checked AndAlso ligne.SubItems(3).Text <> "" Then e.NewValue = CheckState.Unchecked
    End Sub

    Private Sub ListViewRadios_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles ListViewRadios.ItemChecked
        If _enMiseAJour Then Exit Sub
        Dim n As Integer = ListViewRadios.CheckedItems.Count
        ButtonAjouter.Enabled = n > 0
        ButtonAjouter.Text = If(n > 1, "Ajouter les " & n & " cochées", "Ajouter la cochée")
        If n = 0 Then ButtonAjouter.Text = "Ajouter les cochées"
    End Sub

    Private Sub ListViewRadios_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListViewRadios.SelectedIndexChanged
        ButtonEcouter.Enabled = ListViewRadios.SelectedItems.Count > 0
        If _essaiEnCours Then ArreterLEssai()
    End Sub

    ''' <summary>"Ecouter la radio choisie" : joue la radio selectionnee (la radio en cours est remplacee) ; un second clic arrete l'essai.</summary>
    Private Sub ButtonEcouter_Click(sender As Object, e As EventArgs) Handles ButtonEcouter.Click
        If _essaiEnCours Then
            ArreterLEssai()
            Exit Sub
        End If
        If ListViewRadios.SelectedItems.Count = 0 Then Exit Sub
        Dim r As AnnuaireRadios.RadioProposee = DirectCast(ListViewRadios.SelectedItems(0).Tag, AnnuaireRadios.RadioProposee)
        Try
            MusiqueVLC.MusiqueJouerRadio(r.Adresse)
            _essaiEnCours = True
            ButtonEcouter.Text = "Arrêter l'écoute"
            LabelEtat.Text = "Écoute : " & r.Nom & " (quelques secondes de chargement)"
            DiagLog("Radios (annuaire) : essai de " & r.Nom & " " & r.Adresse)
        Catch ex As Exception
            LabelEtat.Text = "Cette radio n'a pas pu être lue : " & ex.Message
        End Try
    End Sub

    Private Sub ArreterLEssai()
        If Not _essaiEnCours Then Exit Sub
        _essaiEnCours = False
        ButtonEcouter.Text = "Écouter la radio choisie"
        Try
            MusiqueVLC.MusiqueArreter()
        Catch ex As Exception
        End Try
    End Sub

    ' Double-clic sur une radio : essai.
    Private Sub ListViewRadios_DoubleClick(sender As Object, e As EventArgs) Handles ListViewRadios.DoubleClick
        If ListViewRadios.SelectedItems.Count > 0 AndAlso Not _essaiEnCours Then ButtonEcouter.PerformClick()
    End Sub

    Private Sub ButtonAjouter_Click(sender As Object, e As EventArgs) Handles ButtonAjouter.Click
        Choisies.Clear()
        For Each ligne As ListViewItem In ListViewRadios.CheckedItems
            Choisies.Add(DirectCast(ligne.Tag, AnnuaireRadios.RadioProposee))
        Next
        If Choisies.Count = 0 Then Exit Sub
        DialogResult = DialogResult.OK
    End Sub

    Private Sub FormSuggestionsRadio_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ArreterLEssai()
        If _recherche IsNot Nothing Then _recherche.Cancel()
    End Sub

End Class
