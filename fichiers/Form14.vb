Imports System.IO
Imports Vlc.DotNet.Forms

Public Class Form14


    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Form14DontClose = False
        VlcControl1.Stop()
        retablieTousLesVolumes()
        Form4.BringToFront()
        Me.Close()
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Form14_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TrackBar2.Value = 100
        VlcControl1.Audio.IsMute = False

    End Sub



    Private Sub DataGridView2_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView2.CellContentClick

    End Sub



    Private Sub Button2_Click_1(sender As Object, e As EventArgs) Handles Button2.Click
        Form14DontClose = False
        VlcControl1.Stop()
        VlcControl1.Audio.IsMute = True
        If Form4.DataGridView1.RowCount = 0 Then AjouterUneLigne(1, 1)
        'Else
        'AjouterUneLigne(3, Form4.DataGridView1.CurrentCell.RowIndex)
        'End If



        r = Val(Form4.Label36.Text)
        If r < 0 OrElse r >= Form4.DataGridView1.RowCount Then Me.Close() : Exit Sub        ' (ouverte par le menu Telechargement : pas de ligne a remplir)
        l = Val(Label1.Text)
        ' Le film choisi est celui de la ligne SELECTIONNEE dans la liste (14/10/2026 : "le OK de la fenetre Nflix ne
        ' modifie pas le nom du film"). Label1 ne retient que la derniere ligne LUE par double-clic : un simple clic
        ' puis OK reprenait donc l'ancien choix (ou une ligne sans rapport apres une recherche).
        If DataGridView2.CurrentCell IsNot Nothing AndAlso DataGridView2.CurrentCell.RowIndex >= 0 AndAlso
           DataGridView2.CurrentCell.RowIndex < DataGridView2.RowCount Then l = DataGridView2.CurrentCell.RowIndex
        If l < 0 OrElse l >= DataGridView2.RowCount Then Me.Close() : Exit Sub
        Nom = DataGridView2.Item(1, l).Value
        Title = DataGridView2.Item(0, l).Value
        ' Un autre film : l'ancienne plage (colonne Play) et l'ancienne duree ne sont plus les siennes ; elles sont
        ' recalculees a l'apercu.
        If Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Action").Value) <> Convert.ToString(Title) Then
            Form4.DataGridView1.Rows(r).Cells("Play").Value = ""
            Form4.DataGridView1.Rows(r).Cells("Durée").Value = ""
        End If
        ' La ligne avait un film avec ses reglages (PIP et son coin, boucle...) : proposes au nouveau film, comme pour un fichier.
        Dim reglagesGardes As String = ""
        Try
            Dim ancienneAction As String = Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Action").Value)
            If UCase(Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Type").Value)) = "FILM" AndAlso ancienneAction <> Convert.ToString(Title) Then
                Dim anciens As String = ReglagesConservesDuFilm(Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Argument").Value))
                If anciens <> "" Then
                    Dim volumeLigne As String = Convert.ToString(Form4.DataGridView1.Rows(r).Cells("Volume").Value).Trim()
                    If ReprendreLesReglagesDuDossier(Convert.ToString(Title), ancienneAction, volumeLigne, anciens, True) Then reglagesGardes = anciens
                End If
            End If
        Catch ex As Exception
        End Try
        Form4.DataGridView1.Rows(r).Cells("Action").Value = Title
        'Form4.DataGridView1.Rows(r).Cells("Play").Value = Title
        Form4.DataGridView1.Rows(r).Cells("Argument").Value = "!/Titre:" & Title & "!/Url:" & Nom & "!/Source:" & IptvSources.NomIptv(IptvSources.SelectionIptv) & reglagesGardes
        Form4.DataGridView1.Rows(r).Cells("Action").ToolTipText = Nom
        Form4.DataGridView1.Rows(r).Cells(0).ToolTipText = Form4.DataGridView1.Rows(r).Cells("Argument").Value
        Me.Close()

        '**'
        'Call PreviewFilm(r, Nom, Title)

        'Me.Tag = e.RowIndex
        'Hide()



    End Sub

    Private Sub VlcControl1_Click(sender As Object, e As EventArgs) Handles VlcControl1.Click

    End Sub

    Private Sub VlcControl1_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcControl1.VlcLibDirectoryNeeded

        e.VlcLibDirectory = New DirectoryInfo(VLCpath)

        OptionsDuLecteurVlc(sender)
    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll

        Label8.Text = TimeSpan.FromSeconds(CDbl(TrackBar1.Value)).ToString("hh\:mm\:ss")
        nom = VlcControl1.GetCurrentMedia.Title

        Dim extension As String = UCase(Path.GetExtension(nom))

        If extension = ".TS" Then
            VlcControl1.Position = TrackBar1.Value * 1000 / VlcControl1.Length
        Else
            VlcControl1.Time = TrackBar1.Value * 1000
        End If

    End Sub

    Private Sub TrackBar2_Scroll(sender As Object, e As EventArgs) Handles TrackBar2.Scroll

        'VlcControl1.Audio.Volume = TrackBar2.Value
        Label10.Text = TrackBar2.Value
        VlcControl1.Audio.IsMute = False
        SetVolume(TrackBar2.Value, VlcControl1)
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If VlcControl1.IsPlaying Then
            VlcControl1.Pause()
            Button6.Text = ">"
        Else
            VlcControl1.Play()
            Button6.Text = "II"
        End If
    End Sub
    Private Sub DataGridView2_CellEnter(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView2.CellEnter
        'Exit Sub
        VlcControl1.Stop()
        VlcControl1.Audio.IsMute = True

        rw = e.RowIndex
        Try
            url = DataGridView2.Rows(e.RowIndex).Cells(1).Value
            Title = DataGridView2.Rows(e.RowIndex).Cells(0).Value
            ChannelLogo = DataGridView2.Rows(e.RowIndex).Cells(2).Value
            Try
                ChannelLogo = ChannelLogo.replace("_small", "_big")
            Catch ex As Exception
            End Try

            AfficheLogo(ChannelLogo, e.RowIndex, url, Title)
            If EstUneSerie(e.RowIndex) Then TextBox1.Text = ""        ' (pas d'adresse : ses episodes viennent du serveur)
            DemanderLePitch(e.RowIndex)

        Catch ex As Exception

        End Try

    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs) Handles PictureBox1.Click

        Try
            RowIndex = PictureBox1.Tag
            If RowIndex IsNot Nothing Then

            End If

            If EstUneSerie(CInt(RowIndex)) Then                    ' une serie : on choisit son episode
                ChoisirUnEpisode(CInt(RowIndex))
                Exit Sub
            End If

            Nom = DataGridView2.Rows(RowIndex).Cells(1).Value

            PictureBox1.Visible = False
            VlcControl1.Visible = True
            Application.DoEvents()

            playNFlix(Nom)
        Catch ex As Exception

        End Try



    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs) Handles Timer1.Tick

        If VlcControl1.IsPlaying Then

            Try
                pos1 = VlcControl1.Time / 1000
                Label8.Text = TimeSpan.FromSeconds(CDbl(pos1)).ToString("hh\:mm\:ss")
                pos1 = VlcControl1.Time / 1000
                TrackBar1.Value = pos1
            Catch ex As Exception
            End Try

        End If


    End Sub

    Private Sub Form14_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        e.Cancel = Form14DontClose
    End Sub



    Private Sub Timer2_Tick(sender As Object, e As EventArgs) Handles Timer2.Tick
        Try

            If VlcControl1.IsPlaying Then
                pos1 = VlcControl1.Time / 1000
                pos2 = VlcControl1.Length / 1000
                TrackBar1.Maximum = pos2 '/ 60
                TrackBar1.Value = pos1 '/ 60
                GroupBox2.BringToFront()
                Label9.Text = TimeSpan.FromSeconds(CDbl(pos2)).ToString("hh\:mm\:ss")
                Label8.Text = "00:00:00"
                Label10.Text = TrackBar2.Value
                GroupBox2.Visible = True
                PictureBox1.Visible = False
                VlcControl1.Visible = True
                Timer2.Stop()
                Timer1.Start()
            End If
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Bouton "Rechercher" (remplace le menu du meme nom ; demande de Jacob, 13/10/2026).</summary>
    Private Sub ButtonRechercher_Click(sender As Object, e As EventArgs) Handles ButtonRechercher.Click
        rechercherUnTitre()
    End Sub

    Private Sub Form14_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.Control AndAlso e.KeyCode = Keys.F Then
            e.SuppressKeyPress = True
            rechercherUnTitre()
        End If
    End Sub

    Private Sub ListBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBox1.SelectedIndexChanged

    End Sub

    Private Sub ListBox1_Click(sender As Object, e As EventArgs) Handles ListBox1.Click
        groupe = ListBox1.Items(ListBox1.SelectedIndex).ToString
        TextBox1.Text = ""
        Form4.Cursor = Cursors.WaitCursor
        ChercheGroupe(groupe)
        Form4.Cursor = Cursors.Default
    End Sub

    Private Sub DataGridView2_CellContentDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView2.CellContentDoubleClick
        If e.RowIndex < 0 Then Exit Sub
        If EstUneSerie(e.RowIndex) Then                        ' une serie : on choisit son episode (puis il est lu)
            ChoisirUnEpisode(e.RowIndex)
        Else
            LireLaLigne(e.RowIndex)
        End If
    End Sub

    ''' <summary>Lit la chaine, le film ou l'episode de la ligne : adresse et titre dans la fenetre, puis lecture.</summary>
    Private Sub LireLaLigne(ligne As Integer)
        VlcControl1.Stop()
        url = DataGridView2.Rows(ligne).Cells(1).Value
        Title = DataGridView2.Rows(ligne).Cells(0).Value
        ChannelLogo = DataGridView2.Rows(ligne).Cells(2).Value

        Try
            If Not (String.IsNullOrEmpty(url)) Then
                Clipboard.SetText(url)
            End If
        Catch ex As Exception

        End Try

        PictureBox1.Visible = False
        VlcControl1.Visible = True
        Label1.Text = Str(ligne)
        TextBox1.Text = url
        TextBox2.Text = Title
        playNFlix(url)
        VlcControl1.Audio.IsMute = False
        SetVolume(TrackBar2.Value, VlcControl1)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Films et series d'un compte Xtream (voir IptvSources) : liste "Contenu" au-dessus des chaines.
    ' Une serie n'a pas d'adresse dans le catalogue : on choisit son episode (FormEpisodes), puis la
    ' ligne devient cet episode (titre "Serie S01E02", adresse de l'episode) et il est lu.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Ce que la ligne d'une serie retient pour que son choix puisse etre refait.</summary>
    Private Class SerieChoisie
        Public Numero As String
        Public NomDeLaSerie As String
    End Class

    Private _remplissageContenu As Boolean

    ''' <summary>La liste "Contenu" : visible pour un compte Xtream seulement, sur le genre affiche.</summary>
    Private Sub RemplirLeContenu()
        _remplissageContenu = True
        Try
            ComboBoxContenu.Items.Clear()
            ComboBoxContenu.Items.Add("Chaînes en direct")
            ComboBoxContenu.Items.Add("Films")
            ComboBoxContenu.Items.Add("Séries")
            ComboBoxContenu.SelectedIndex = CInt(IptvSources.GenreAffiche)
        Finally
            _remplissageContenu = False
        End Try
        Dim xtream As Boolean = IptvSources.EstXtream(IptvSources.SelectionIptv)
        ComboBoxContenu.Visible = xtream
        LabelContenuIptv.Visible = xtream
    End Sub

    Private Sub Form14_ContenuIptv(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Not Me.Visible Then Exit Sub
        RemplirLeContenu()
        PositionnerSurLeDernierAjout()
    End Sub

    ''' <summary>S'il existe un groupe "Le dernier ajouté" (ou "Le drenier ajoutee"...), la liste s'y positionne et
    ''' en affiche le contenu (demande de Jacob, 13/10/2026). Sans ce groupe : rien ne change.</summary>
    Private Sub PositionnerSurLeDernierAjout()
        MettreAJourLeTitreDesGroupes()
        Try
            Dim noms As New List(Of String)
            For Each element As Object In ListBox1.Items
                noms.Add(Convert.ToString(element))
            Next
            Dim rang As Integer = RechercheNflix.RangDuDernierAjout(noms)
            If rang < 0 Then Exit Sub
            ListBox1.SelectedIndex = rang                  ' (la liste defile jusqu'a lui)
            groupe = noms(rang)
            TextBox1.Text = ""
            Cursor = Cursors.WaitCursor
            Try
                ChercheGroupe(groupe)
            Finally
                Cursor = DefaultCursor
            End Try
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Titre de la liste des groupes (demande de Jacob, 13/10/2026 : "rajoute un titre au tableau de gauche
    ' pour pouvoir trier") : un clic change l'ordre - celui du fournisseur, A a Z, Z a A.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>"Groupes (717)" + la fleche de l'ordre en cours.</summary>
    Private Sub MettreAJourLeTitreDesGroupes()
        Dim fleche As String = ""
        Select Case RechercheNflix.OrdreDesGroupes
            Case 1 : fleche = "  ▲  A → Z"
            Case 2 : fleche = "  ▼  Z → A"
        End Select
        ButtonTriGroupes.Text = If(RechercheNflix.FiltreDesGroupes IsNot Nothing, "Groupes trouvés (", "Groupes (") & ListBox1.Items.Count & ")" & fleche
    End Sub

    ''' <summary>Remplit la liste des groupes : tous (ordre choisi), ou ceux que la derniere recherche a trouves.
    ''' Le groupe choisi, s'il y est encore, reste selectionne.</summary>
    Friend Sub ReafficherLesGroupes()
        Dim choisi As String = If(ListBox1.SelectedItem Is Nothing, "", Convert.ToString(ListBox1.SelectedItem))
        Dim ordre As List(Of String) = RechercheNflix.GroupesTries(RechercheNflix.Groupes(IptvSources.ListeAffichee()))
        ListBox1.BeginUpdate()
        Try
            ListBox1.Items.Clear()
            ListBox1.Items.AddRange(ordre.ToArray())
        Finally
            ListBox1.EndUpdate()
        End Try
        If choisi <> "" Then
            Dim k As Integer = ListBox1.Items.IndexOf(choisi)
            If k >= 0 Then ListBox1.SelectedIndex = k            ' (la liste defile jusqu'au groupe choisi)
        End If
        MettreAJourLeTitreDesGroupes()
    End Sub

    ''' <summary>Bouton "RAZ recherche" : la recherche est effacee, la liste des groupes revient en entier et la liste de
    ''' droite est videe (15/10/2026). Equivalent d'une recherche vide, sans passer par la saisie.</summary>
    Private Sub ButtonRazRecherche_Click(sender As Object, e As EventArgs) Handles ButtonRazRecherche.Click
        RechercheNflix.DerniereRecherche = ""
        RechercheNflix.FiltreDesGroupes = Nothing
        ReafficherLesGroupes()
        DataGridView2.Rows.Clear()
        Me.Text = "Nflix"
        TextBox1.Text = ""                              ' adresse et titre du dernier element lu
        TextBox2.Text = ""
        TextBoxPitch.Text = ""
        Try
            ListBox1.ClearSelected()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ButtonTriGroupes_Click(sender As Object, e As EventArgs) Handles ButtonTriGroupes.Click
        RechercheNflix.OrdreDesGroupes = (RechercheNflix.OrdreDesGroupes + 1) Mod 3
        ReafficherLesGroupes()
    End Sub

    Private Sub ComboBoxContenu_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxContenu.SelectedIndexChanged
        If _remplissageContenu OrElse ComboBoxContenu.SelectedIndex < 0 Then Exit Sub
        Dim genre As IptvSources.GenreIptv = CType(ComboBoxContenu.SelectedIndex, IptvSources.GenreIptv)
        If genre = IptvSources.GenreAffiche Then Exit Sub
        Dim erreur As String = ""
        Dim ok As Boolean
        Cursor = Cursors.WaitCursor
        Try
            VlcControl1.Stop()
            ok = IptvSources.AssurerLeCatalogue(IptvSources.SelectionIptv, genre, erreur)      ' premier besoin : quelques secondes
        Finally
            Cursor = DefaultCursor
        End Try
        If Not ok Then
            MessageBox.Show(Me, "Le catalogue n'a pas pu être chargé :" & vbCrLf & erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            RemplirLeContenu()                                 ' revient a ce qui est affiche
            Exit Sub
        End If
        IptvSources.GenreAffiche = genre
        DataGridView2.Rows.Clear()
        TextBox1.Text = ""
        TextBox2.Text = ""
        Cursor = Cursors.WaitCursor
        Try
            rechercher()                                       ' les groupes du catalogue
        Finally
            Cursor = DefaultCursor
        End Try
        PositionnerSurLeDernierAjout()
    End Sub

    ''' <summary>La ligne est une serie du catalogue (adresse "slide-serie:..."), ou une serie dont un episode
    ''' a deja ete choisi.</summary>
    Private Function EstUneSerie(ligne As Integer) As Boolean
        If ligne < 0 OrElse ligne >= DataGridView2.Rows.Count Then Return False
        Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
        If TypeOf rangee.Tag Is SerieChoisie Then Return True
        Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
        Return adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Le pitch du film ou de la serie choisi (demande de Jacob, 13/10/2026 : "quand je click sur un film
    ' rajoute le pitch") : demande au serveur du compte en tache de fond (IptvSources.FicheDuTitre),
    ' affichee sous l'apercu. Une ligne choisie vite apres une autre : seule la derniere compte.
    ' ------------------------------------------------------------------------------------------

    Private _pitchDemande As String = ""

    Private Async Sub DemanderLePitch(ligne As Integer)
        Dim cle As String = ""
        Try
            If ligne >= 0 AndAlso ligne < DataGridView2.Rows.Count Then
                Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
                Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
                Dim serie As SerieChoisie = TryCast(rangee.Tag, SerieChoisie)
                If serie IsNot Nothing Then
                    cle = "serie:" & serie.Numero
                ElseIf adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal) Then
                    cle = "serie:" & adresse.Substring(IptvSources.PrefixeSerie.Length)
                ElseIf IptvSources.NumeroDuFilm(adresse) <> "" Then
                    cle = "film:" & IptvSources.NumeroDuFilm(adresse)
                End If
            End If
        Catch ex As Exception
            cle = ""
        End Try
        _pitchDemande = cle
        TextBoxPitch.Text = ""
        If cle = "" Then Exit Sub                      ' une chaine en direct : pas de pitch
        TextBoxPitch.Text = "Chargement de la fiche..."
        Dim source As Integer = IptvSources.SelectionIptv
        Try
            Await Task.Delay(250)                      ' (la ligne peut encore changer : fleches tenues)
            If cle <> _pitchDemande OrElse IsDisposed Then Exit Sub
            Dim fiche As IptvSources.FicheIptv = Await Task.Run(Function() IptvSources.FicheDuTitre(source, cle))
            If cle <> _pitchDemande OrElse IsDisposed Then Exit Sub
            TextBoxPitch.Text = If(fiche Is Nothing, "(la fiche n'a pas pu être chargée)", fiche.EnTexte())
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Demande les episodes de la serie de la ligne au serveur, fait choisir l'un d'eux, et le lit.</summary>
    Private Sub ChoisirUnEpisode(ligne As Integer)
        If ChoisirUnEpisodeSansLire(ligne) Then LireLaLigne(ligne)
    End Sub

    ''' <summary>Demande les episodes de la serie de la ligne au serveur et fait choisir l'un d'eux : la ligne
    ''' devient cet episode. Rend Faux si rien n'est choisi.</summary>
    Private Function ChoisirUnEpisodeSansLire(ligne As Integer) As Boolean
        Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
        Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
        Dim serie As SerieChoisie = TryCast(rangee.Tag, SerieChoisie)
        If adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal) Then
            serie = New SerieChoisie With {.Numero = adresse.Substring(IptvSources.PrefixeSerie.Length),
                                           .NomDeLaSerie = Convert.ToString(rangee.Cells(0).Value)}
            rangee.Tag = serie
        End If
        If serie Is Nothing Then Return False
        VlcControl1.Stop()
        Dim erreur As String = ""
        Dim episodes As List(Of IptvSources.EpisodeIptv)
        Cursor = Cursors.WaitCursor
        Try
            episodes = IptvSources.EpisodesDeLaSerie(IptvSources.SelectionIptv, serie.Numero, erreur)
        Finally
            Cursor = DefaultCursor
        End Try
        If episodes.Count = 0 Then
            MessageBox.Show(Me, erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return False
        End If
        Using fenetre As New FormEpisodes
            fenetre.Initialiser(serie.NomDeLaSerie, episodes, adresse)
            If fenetre.ShowDialog(Me) <> DialogResult.OK Then Return False
            rangee.Cells(0).Value = serie.NomDeLaSerie & " " & fenetre.Choix.Code()
            rangee.Cells(1).Value = fenetre.Choix.AdresseEpisode
        End Using
        Return True
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Bouton "Telecharger" (demande de Jacob, 13/10/2026 : "rajoute un bouton pour telecharger un film
    ' rapidement", puis "rajoute la possibilite de telecharger plusieurs film en simultane") : les films
    ' choisis dans le tableau (Ctrl ou Maj + clic pour en prendre plusieurs) sont mis dans la file des
    ' telechargements (Telechargements) : 3 en meme temps, les autres attendent. Une serie : on choisit
    ' d'abord ses episodes (plusieurs possibles). Un seul film : fenetre "Enregistrer sous" (dossier et
    ' nom) ; plusieurs : on choisit le dossier. La fenetre "Telechargements" montre l'avancement ; un
    ' telechargement interrompu est repris la fois suivante (IptvSources.TelechargerFichier).
    ' ------------------------------------------------------------------------------------------

    Private Const TexteDuTelechargement As String = "Télécharger : les films choisis (Ctrl ou Maj + clic pour plusieurs) sont copiés dans le dossier de votre choix."

    ''' <summary>Les lignes choisies dans le tableau, de haut en bas ; a defaut la ligne courante.</summary>
    Private Function LignesChoisies() As List(Of Integer)
        Dim l As New List(Of Integer)
        For Each r As DataGridViewRow In DataGridView2.SelectedRows
            l.Add(r.Index)
        Next
        If l.Count = 0 AndAlso DataGridView2.CurrentCell IsNot Nothing Then l.Add(DataGridView2.CurrentCell.RowIndex)
        l.Sort()
        Return l
    End Function

    Private Class ATelecharger
        Public Titre As String
        Public Adresse As String
    End Class

    ''' <summary>Demande les episodes d'une serie et en fait choisir un ou plusieurs (sans les lire). Liste vide si rien n'est choisi.</summary>
    Private Function ChoisirDesEpisodes(ligne As Integer, ByRef nomDeLaSerie As String) As List(Of IptvSources.EpisodeIptv)
        nomDeLaSerie = ""
        Dim vide As New List(Of IptvSources.EpisodeIptv)
        Dim rangee As DataGridViewRow = DataGridView2.Rows(ligne)
        Dim adresse As String = Convert.ToString(rangee.Cells(1).Value)
        Dim serie As SerieChoisie = TryCast(rangee.Tag, SerieChoisie)
        If adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal) Then
            serie = New SerieChoisie With {.Numero = adresse.Substring(IptvSources.PrefixeSerie.Length),
                                           .NomDeLaSerie = Convert.ToString(rangee.Cells(0).Value)}
            rangee.Tag = serie
        End If
        If serie Is Nothing Then Return vide
        nomDeLaSerie = serie.NomDeLaSerie
        VlcControl1.Stop()
        Dim erreur As String = ""
        Dim episodes As List(Of IptvSources.EpisodeIptv)
        Cursor = Cursors.WaitCursor
        Try
            episodes = IptvSources.EpisodesDeLaSerie(IptvSources.SelectionIptv, serie.Numero, erreur)
        Finally
            Cursor = DefaultCursor
        End Try
        If episodes.Count = 0 Then
            MessageBox.Show(Me, erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Return vide
        End If
        Using fenetre As New FormEpisodes
            fenetre.Initialiser(serie.NomDeLaSerie, episodes, adresse, True)
            If fenetre.ShowDialog(Me) <> DialogResult.OK Then Return vide
            Return New List(Of IptvSources.EpisodeIptv)(fenetre.Choisis)
        End Using
    End Function

    Private Sub ButtonTelecharger_Click(sender As Object, e As EventArgs) Handles ButtonTelecharger.Click
        If DataGridView2.Rows.Count = 0 Then
            MessageBox.Show(Me, "Choisissez d'abord un ou plusieurs films dans la liste.", "Télécharger", MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Dim aTelecharger As New List(Of ATelecharger)
        Dim ignorees As Integer = 0
        For Each ligne As Integer In LignesChoisies()
            If EstUneSerie(ligne) Then
                Dim nomSerie As String = ""
                For Each ep As IptvSources.EpisodeIptv In ChoisirDesEpisodes(ligne, nomSerie)
                    aTelecharger.Add(New ATelecharger With {.Titre = nomSerie & " " & ep.Code(), .Adresse = ep.AdresseEpisode})
                Next
            Else
                Dim adresse As String = Convert.ToString(DataGridView2.Rows(ligne).Cells(1).Value)
                If IptvSources.EstTelechargeable(adresse) Then
                    aTelecharger.Add(New ATelecharger With {.Titre = Convert.ToString(DataGridView2.Rows(ligne).Cells(0).Value), .Adresse = adresse})
                Else
                    ignorees += 1
                End If
            End If
        Next
        If aTelecharger.Count = 0 Then
            If ignorees > 0 Then
                MessageBox.Show(Me, If(ignorees > 1, "Ces lignes sont des chaînes en direct : elles ne finissent jamais.", "Cette ligne est une chaîne en direct : elle ne finit jamais.") & vbCrLf &
                                "Seuls les films et les épisodes de séries se téléchargent.", "Télécharger", MessageBoxButtons.OK, MessageBoxIcon.Information)
            End If
            Exit Sub
        End If

        ' Ou ? Un seul film : dossier ET nom ; plusieurs : le dossier (les noms viennent des titres).
        Dim dossier As String = My.Settings.Nflix_DossierTelechargement
        If String.IsNullOrWhiteSpace(dossier) OrElse Not Directory.Exists(dossier) Then
            dossier = IptvSources.DossierDesTelechargements()
            Try
                Directory.CreateDirectory(dossier)
            Catch ex As Exception
                dossier = Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            End Try
        End If
        Dim cibleUnique As String = ""
        If aTelecharger.Count = 1 Then
            Dim extension As String = IptvSources.ExtensionDeLAdresse(aTelecharger(0).Adresse)
            If extension = "" Then extension = ".mp4"
            Using choix As New SaveFileDialog
                choix.Title = "Télécharger : choisissez le dossier et le nom du fichier"
                choix.InitialDirectory = dossier
                choix.FileName = IptvSources.NomDeFichier(aTelecharger(0).Titre) & extension
                choix.DefaultExt = extension.TrimStart("."c)
                choix.AddExtension = True
                choix.Filter = "Vidéo (*" & extension & ")|*" & extension & "|Tous les fichiers (*.*)|*.*"
                choix.OverwritePrompt = True
                If choix.ShowDialog(Me) <> DialogResult.OK Then Exit Sub
                cibleUnique = choix.FileName
            End Using
            dossier = Path.GetDirectoryName(cibleUnique)
        Else
            Using choix As New FolderBrowserDialog
                choix.Description = "Dossier des " & aTelecharger.Count & " téléchargements"
                choix.UseDescriptionForTitle = True
                choix.SelectedPath = dossier
                If choix.ShowDialog(Me) <> DialogResult.OK Then Exit Sub
                dossier = choix.SelectedPath
            End Using
        End If
        My.Settings.Nflix_DossierTelechargement = dossier
        Try
            My.Settings.Save()
        Catch ex As Exception
        End Try

        VlcControl1.Stop()                                    ' (l'apercu et les telechargements se partagent le compte)
        Dim ajoutes As Integer = 0, dejaLa As Integer = 0
        Dim pris As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        For Each a As ATelecharger In aTelecharger
            Dim cible As String = cibleUnique
            If cible = "" Then
                Dim extension As String = IptvSources.ExtensionDeLAdresse(a.Adresse)
                If extension = "" Then extension = ".mp4"
                Dim nom As String = IptvSources.NomDeFichier(a.Titre)
                cible = Path.Combine(dossier, nom & extension)
                Dim n As Integer = 2
                Do While pris.Contains(cible)                 ' deux titres de meme nom dans la meme tournee
                    cible = Path.Combine(dossier, nom & " (" & n & ")" & extension)
                    n += 1
                Loop
                If File.Exists(cible) Then
                    dejaLa += 1                                ' deja telecharge : on ne refait pas
                    Continue For
                End If
            End If
            pris.Add(cible)
            If Telechargements.Ajouter(a.Titre, a.Adresse, cible) IsNot Nothing Then ajoutes += 1
        Next
        Dim notes As New List(Of String)
        If dejaLa > 0 Then notes.Add(dejaLa & If(dejaLa > 1, " déjà téléchargés (ignorés)", " déjà téléchargé (ignoré)"))
        If ignorees > 0 Then notes.Add(ignorees & If(ignorees > 1, " chaînes en direct ignorées", " chaîne en direct ignorée"))
        If notes.Count > 0 Then LabelTelechargement.Text = ajoutes & " ajouté(s) à la file ; " & String.Join(" ; ", notes) & "."
        If ajoutes > 0 Then FormTelechargements.Montrer(Me)
    End Sub

    Private Sub ButtonTelechargements_Click(sender As Object, e As EventArgs) Handles ButtonTelechargements.Click
        FormTelechargements.Montrer(Me)
    End Sub

    ''' <summary>La file des telechargements a change : un resume sous le pitch, et la barre d'avancement de l'ensemble.</summary>
    Private Sub Form14_TelechargementsChange()
        If IsDisposed Then Exit Sub
        Dim bilan As String = Telechargements.Bilan()
        LabelTelechargement.Text = If(bilan = "", TexteDuTelechargement, "Téléchargements : " & bilan)
        ButtonTelechargements.Text = If(Telechargements.Actifs() + Telechargements.EnAttente() > 0,
                                        "Téléchargements (" & (Telechargements.Actifs() + Telechargements.EnAttente()) & ")", "Téléchargements")
        Dim avancement As Integer = Telechargements.Avancement()
        If Telechargements.Actifs() = 0 Then
            ProgressBarTelechargement.Visible = False
        ElseIf avancement < 0 Then
            ProgressBarTelechargement.Style = ProgressBarStyle.Marquee
            ProgressBarTelechargement.Visible = True
        Else
            ProgressBarTelechargement.Style = ProgressBarStyle.Continuous
            ProgressBarTelechargement.Value = avancement
            ProgressBarTelechargement.Visible = True
        End If
    End Sub

    Private Sub Form14_SuivreLesTelechargements(sender As Object, e As EventArgs) Handles MyBase.Load
        AddHandler Telechargements.Change, AddressOf Form14_TelechargementsChange
        AddHandler Me.Disposed, Sub() RemoveHandler Telechargements.Change, AddressOf Form14_TelechargementsChange
        Form14_TelechargementsChange()
    End Sub

    ''' <summary>Fenetre fermee ou cachee : la liste gardee en memoire pour la recherche est liberee (voir
    ''' RechercheNflix). Les telechargements en cours continuent.</summary>
    Private Sub Form14_Liberer(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then Exit Sub
        RechercheNflix.Oublier()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Plusieurs IPTV (voir IptvSources) : liste "Source" en haut de la fenetre, visible des qu'il y a
    ' au moins deux IPTV avec une adresse.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>La fenetre s'ouvre au centre de l'ecran (celui des reglages s'ils sont ouverts), a chaque
    ''' affichage : cette fenetre est reutilisee, et rouverte elle reprenait sa derniere place ou celle
    ''' laissee par un autre ecran (demande de Jacob, 13/10/2026 : "centre la fenetre Nflix").</summary>
    Private Sub Form14_Centrer(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Not Me.Visible OrElse Me.WindowState <> FormWindowState.Normal Then Exit Sub
        Try
            Dim reglages As Form = My.Application.OpenForms.OfType(Of Form4)().FirstOrDefault()
            Dim ecran As Screen = If(reglages IsNot Nothing AndAlso reglages.Visible, Screen.FromControl(reglages), Screen.FromControl(Me))
            Dim zone As Rectangle = ecran.WorkingArea
            Me.Location = New Point(zone.Left + Math.Max(0, (zone.Width - Me.Width) \ 2),
                                    zone.Top + Math.Max(0, (zone.Height - Me.Height) \ 2))
        Catch ex As Exception
        End Try
    End Sub

    Private Class ElementSource
        Public Index As Integer
        Public Libelle As String
        Public Overrides Function ToString() As String
            Return Libelle
        End Function
    End Class

    Private _remplissageSources As Boolean

    Private Sub Form14_SourcesIptv(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Not Me.Visible Then Exit Sub
        _remplissageSources = True
        Try
            ComboBoxSource.Items.Clear()
            Dim choix As Integer = -1
            For i As Integer = 0 To IptvSources.NombreIptv - 1
                If IptvSources.UrlIptv(i) <> "" OrElse i = IptvSources.SelectionIptv Then
                    ComboBoxSource.Items.Add(New ElementSource With {.Index = i, .Libelle = IptvSources.NomIptv(i)})
                    If i = IptvSources.SelectionIptv Then choix = ComboBoxSource.Items.Count - 1
                End If
            Next
            If choix >= 0 Then ComboBoxSource.SelectedIndex = choix
        Finally
            _remplissageSources = False
        End Try
        Dim plusieurs As Boolean = ComboBoxSource.Items.Count > 1
        ComboBoxSource.Visible = plusieurs
        LabelSourceIptv.Visible = plusieurs
    End Sub

    Private Sub ComboBoxSource_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxSource.SelectedIndexChanged
        If _remplissageSources Then Exit Sub
        Dim element As ElementSource = TryCast(ComboBoxSource.SelectedItem, ElementSource)
        If element Is Nothing OrElse element.Index = IptvSources.SelectionIptv Then Exit Sub
        Dim erreur As String = ""
        Cursor = Cursors.WaitCursor
        Dim ok As Boolean
        Try
            VlcControl1.Stop()
            ok = IptvSources.Activer(element.Index, erreur)
        Finally
            Cursor = DefaultCursor
        End Try
        If ok Then
            DataGridView2.Rows.Clear()
            TextBox1.Text = ""
            IptvSources.GenreAffiche = IptvSources.GenreIptv.Chaines      ' (les films de la source quittee ne valent plus)
            RemplirLeContenu()
            rechercher()                                  ' les groupes de la nouvelle source
            PositionnerSurLeDernierAjout()
        Else
            MessageBox.Show(Me, erreur, "IPTV", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            _remplissageSources = True
            For k As Integer = 0 To ComboBoxSource.Items.Count - 1
                If DirectCast(ComboBoxSource.Items(k), ElementSource).Index = IptvSources.SelectionIptv Then ComboBoxSource.SelectedIndex = k
            Next
            _remplissageSources = False
        End If
    End Sub

    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form14_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        MoletteSousLaSouris.Suivre(Me)          ' la molette agit sur la liste sous le pointeur
    End Sub

    ''' <summary>Apercu ouvert depuis les reglages : le son reprend (voir SilenceDesReglages.ApercuOuvert).</summary>
    Private Sub Form14_SonDeLApercu(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then SilenceDesReglages.ApercuOuvert(Me, VlcControl1, Function() TrackBar2.Value)
    End Sub


End Class