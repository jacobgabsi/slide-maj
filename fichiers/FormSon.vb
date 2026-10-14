''' <summary>
''' Tache "Son" de la minuterie (20/10/2026 : "pouvoir selectionner plusieurs choix") : une seule ligne peut changer plusieurs reglages du
''' cadre Son de l'onglet Audio (video avec le son, musique, sources cochees, duree de l'alternance). La case Action de la ligne recoit le
''' resume, par exemple "Musique ON ; Sources : MP3 + Radio ; Alterner toutes les 10 min" (les morceaux sont separes par " ; ").
''' </summary>
Public Class FormSon

    Friend Const Separateur As String = " ; "

    ''' <summary>Montre la fenetre sur l'action actuelle de la ligne ("" : rien de choisi). Rend Vrai avec la nouvelle action si on valide.</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, actuelle As String, ByRef nouvelle As String) As Boolean
        Using f As New FormSon()
            f.Lire(actuelle)
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            nouvelle = f.Ecrire()
            Return nouvelle <> ""
        End Using
    End Function

    Private Sub FormSon_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        ' Les radios de la liste (cadre Radio, onglet Audio) ; par defaut celle des reglages.
        Dim noms As List(Of String) = NomsDesRadios()
        ComboRadio.Items.AddRange(noms.ToArray())
        If _radioLue <> "" AndAlso Not ComboRadio.Items.Contains(_radioLue) Then ComboRadio.Items.Add(_radioLue)
        Dim voulue As String = _radioLue
        If voulue = "" Then
            For i As Integer = 0 To UBound(Radio, 1)
                If If(Radio(i, 1), "").Trim().Equals(If(RadioUrl, "").Trim(), StringComparison.OrdinalIgnoreCase) Then voulue = If(Radio(i, 0), "").Trim() : Exit For
            Next
        End If
        Dim place As Integer = ComboRadio.FindStringExact(voulue)
        ComboRadio.SelectedIndex = If(place >= 0, place, If(ComboRadio.Items.Count > 0, 0, -1))
        ' Playlist de Deezer : ce qui est ecrit dans la ligne, sinon "Playlist des reglages".
        Select Case LCase(_playlistLue)
            Case "" : ComboPlaylist.SelectedIndex = 0
            Case "flow" : ComboPlaylist.SelectedIndex = 1
            Case MusiqueDeezer.Favoris : ComboPlaylist.SelectedIndex = 2
            Case Else : ComboPlaylist.Text = _playlistLue
        End Select
        MettreAJourLesCases()
        _chargement = False
    End Sub

    Private _radioLue As String = ""
    Private _playlistLue As String = ""

    Private Sub Lire(actuelle As String)
        ComboVideo.SelectedIndex = 0
        ComboMusique.SelectedIndex = 0
        For Each morceau As String In If(actuelle, "").Split(";"c)
            Dim a As String = UCase(morceau.Trim())
            Select Case a
                Case "VIDEO AVEC SON ON" : ComboVideo.SelectedIndex = 1
                Case "VIDEO AVEC SON OFF" : ComboVideo.SelectedIndex = 2
                Case "MUSIQUE ON" : ComboMusique.SelectedIndex = 1
                Case "MUSIQUE OFF" : ComboMusique.SelectedIndex = 2
                Case Else
                    If a.StartsWith("DEEZER :") Then
                        _playlistLue = morceau.Trim().Substring("Deezer :".Length).Trim()
                    ElseIf a.StartsWith("RADIO :") Then
                        _radioLue = morceau.Trim().Substring("Radio :".Length).Trim()
                    ElseIf a.StartsWith("SOURCES :") Then
                        CheckBoxSources.Checked = True
                        CheckBoxMp3.Checked = a.Contains("MP3")
                        CheckBoxRadio.Checked = a.Contains("RADIO")
                        CheckBoxDeezer.Checked = a.Contains("DEEZER")
                    ElseIf a.StartsWith("ALTERNER TOUTES LES ") Then
                        CheckBoxDuree.Checked = True
                        NumericDuree.Value = Math.Max(NumericDuree.Minimum, Math.Min(NumericDuree.Maximum, CInt(Val(a.Substring("ALTERNER TOUTES LES ".Length)))))
                    End If
            End Select
        Next
    End Sub

    ''' <summary>L'action ecrite d'apres la fenetre ("" s'il n'y a rien a changer).</summary>
    Private Function Ecrire() As String
        Dim morceaux As New List(Of String)
        If ComboVideo.SelectedIndex = 1 Then morceaux.Add("Video avec son ON")
        If ComboVideo.SelectedIndex = 2 Then morceaux.Add("Video avec son OFF")
        If ComboMusique.SelectedIndex = 1 Then morceaux.Add("Musique ON")
        If ComboMusique.SelectedIndex = 2 Then morceaux.Add("Musique OFF")
        If CheckBoxSources.Checked Then
            Dim sources As New List(Of String)
            If CheckBoxMp3.Checked Then sources.Add("MP3")
            If CheckBoxRadio.Checked Then sources.Add("Radio")
            If CheckBoxDeezer.Checked Then sources.Add("Deezer")
            If sources.Count > 0 Then morceaux.Add("Sources : " & String.Join(" + ", sources))
            ' Radio cochee : la station a jouer (sinon celle des reglages).
            If CheckBoxRadio.Checked AndAlso ComboRadio.SelectedItem IsNot Nothing Then morceaux.Add("Radio : " & Convert.ToString(ComboRadio.SelectedItem))
            ' Deezer coche : la playlist a lancer (vide : celle des reglages).
            If CheckBoxDeezer.Checked Then
                Dim playlist As String = PlaylistChoisie()
                If playlist <> "" Then morceaux.Add("Deezer : " & playlist)
            End If
        End If
        If CheckBoxDuree.Checked Then morceaux.Add("Alterner toutes les " & CInt(NumericDuree.Value) & " min")
        Return String.Join(Separateur, morceaux)
    End Function

    ''' <summary>La playlist ecrite pour Deezer : "flow", "favoris" ou l'adresse Deezer ; "" pour celle des reglages (ou une adresse invalide).</summary>
    Private Function PlaylistChoisie() As String
        Dim texte As String = ComboPlaylist.Text.Trim()
        If texte = "" OrElse ComboPlaylist.SelectedIndex = 0 Then Return ""
        If ComboPlaylist.SelectedIndex = 1 Then Return "flow"
        If ComboPlaylist.SelectedIndex = 2 Then Return MusiqueDeezer.Favoris
        Return MusiqueDeezer.AdresseDeezer(texte)
    End Function

    Private Sub MettreAJourLesCases() Handles CheckBoxSources.CheckedChanged, CheckBoxDuree.CheckedChanged, CheckBoxRadio.CheckedChanged,
            CheckBoxDeezer.CheckedChanged, CheckBoxMp3.CheckedChanged, ComboVideo.SelectedIndexChanged, ComboMusique.SelectedIndexChanged,
            ComboRadio.SelectedIndexChanged, ComboPlaylist.SelectedIndexChanged, ComboPlaylist.TextChanged, NumericDuree.ValueChanged
        CheckBoxMp3.Enabled = CheckBoxSources.Checked
        CheckBoxRadio.Enabled = CheckBoxSources.Checked
        CheckBoxDeezer.Enabled = CheckBoxSources.Checked
        ComboRadio.Enabled = CheckBoxSources.Checked AndAlso CheckBoxRadio.Checked
        ComboPlaylist.Enabled = CheckBoxSources.Checked AndAlso CheckBoxDeezer.Checked
        LabelAidePlaylist.Enabled = ComboPlaylist.Enabled
        NumericDuree.Enabled = CheckBoxDuree.Checked
        ' Resume en direct de ce que la tache fera.
        Dim apercu As String = Ecrire()
        LabelResume.Text = If(apercu = "", "Rien pour l'instant : choisissez au moins un réglage.", apercu.Replace(Separateur, "  •  "))
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Deux sources ou plus : Slide alterne forcement (voir AlternanceMusique). La case "Alterner" se coche donc toute seule des que
    ' deux sources sont cochees (20/10/2026) ; la decocher demande laquelle des sources decocher.
    ' ------------------------------------------------------------------------------------------

    Private _chargement As Boolean = True       ' pas de controle pendant que la fenetre se remplit
    Private _controle As Boolean = False        ' les cases changees par ces controles ne les relancent pas
    Private _autoCoche As Boolean = False       ' "Alterner" a ete cochee automatiquement (elle se decoche s'il ne reste qu'une source)

    Private Function SourcesCochees() As List(Of CheckBox)
        Dim liste As New List(Of CheckBox)
        If Not CheckBoxSources.Checked Then Return liste
        For Each c As CheckBox In {CheckBoxMp3, CheckBoxRadio, CheckBoxDeezer}
            If c.Checked Then liste.Add(c)
        Next
        Return liste
    End Function

    Private Sub DefinirSansControle(c As CheckBox, valeur As Boolean)
        _controle = True
        Try
            c.Checked = valeur
        Finally
            _controle = False
        End Try
    End Sub

    Private Sub SourcesChangees(sender As Object, e As EventArgs) Handles CheckBoxSources.CheckedChanged, CheckBoxMp3.CheckedChanged,
            CheckBoxRadio.CheckedChanged, CheckBoxDeezer.CheckedChanged
        If _chargement OrElse _controle Then Exit Sub
        Dim n As Integer = SourcesCochees().Count
        If n >= 2 AndAlso Not CheckBoxDuree.Checked Then
            DefinirSansControle(CheckBoxDuree, True)
            _autoCoche = True
        ElseIf n < 2 AndAlso _autoCoche AndAlso CheckBoxDuree.Checked Then
            DefinirSansControle(CheckBoxDuree, False)
            _autoCoche = False
        End If
        MettreAJourLesCases()
    End Sub

    Private Sub AlternerChange(sender As Object, e As EventArgs) Handles CheckBoxDuree.CheckedChanged
        If _chargement OrElse _controle Then Exit Sub
        _autoCoche = False
        If Not CheckBoxDuree.Checked Then ChoisirQuoiDecocher()
        MettreAJourLesCases()
    End Sub

    ''' <summary>"Alterner" est decochee alors que deux sources ou plus le sont : demande laquelle decocher, jusqu'a ce qu'il n'en reste qu'une.
    ''' Annuler : l'alternance est gardee ("Alterner" recochee). Rend Vrai si l'alternance n'a plus lieu d'etre.</summary>
    Private Function ChoisirQuoiDecocher() As Boolean
        Do While SourcesCochees().Count >= 2
            Dim page As New TaskDialogPage() With {
                .Caption = "Tâche Son",
                .Heading = "Plusieurs sources : Slide alterne",
                .Text = "Avec " & SourcesCochees().Count & " sources cochées, la musique passe de l'une à l'autre." & vbCrLf &
                        "Pour ne pas alterner, laquelle voulez-vous décocher ?",
                .Icon = TaskDialogIcon.Information,
                .AllowCancel = True}
            Dim boutons As New Dictionary(Of TaskDialogButton, CheckBox)
            For Each c As CheckBox In SourcesCochees()
                Dim b As New TaskDialogCommandLinkButton("Décocher " & c.Text)
                boutons(b) = c
                page.Buttons.Add(b)
            Next
            Dim garder As New TaskDialogCommandLinkButton("Garder l'alternance")
            page.Buttons.Add(garder)
            Dim choisi As TaskDialogButton = TaskDialog.ShowDialog(Me, page)
            Dim source As CheckBox = Nothing
            If choisi Is Nothing OrElse choisi Is garder OrElse Not boutons.TryGetValue(choisi, source) Then
                DefinirSansControle(CheckBoxDuree, True)
                Return False
            End If
            DefinirSansControle(source, False)
        Loop
        Return True
    End Function

    Private Sub FormSon_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult <> DialogResult.OK Then Exit Sub
        If SourcesCochees().Count >= 2 AndAlso Not CheckBoxDuree.Checked Then
            If Not ChoisirQuoiDecocher() Then
                MettreAJourLesCases()
                e.Cancel = True
                Exit Sub
            End If
        End If
        If CheckBoxSources.Checked AndAlso CheckBoxDeezer.Checked AndAlso ComboPlaylist.SelectedIndex < 0 AndAlso ComboPlaylist.Text.Trim() <> "" AndAlso PlaylistChoisie() = "" Then
            MessageBox.Show(Me, "Ce n'est pas une adresse de Deezer :" & vbCrLf & ComboPlaylist.Text, "Tâche Son", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            e.Cancel = True
            Exit Sub
        End If
        If Ecrire() = "" Then
            MessageBox.Show(Me, "Rien n'est à changer : choisissez au moins un réglage (ou cochez au moins une source).", "Tâche Son", MessageBoxButtons.OK, MessageBoxIcon.Information)
            e.Cancel = True
        End If
    End Sub

End Class
