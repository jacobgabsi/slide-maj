''' <summary>
''' Choix d'un episode d'une serie (compte Xtream), ouvert par la fenetre Nflix (Form14) quand on choisit
''' une serie du catalogue : les episodes d'une serie ne sont connus qu'une fois la serie demandee au
''' serveur (voir IptvSources.EpisodesDeLaSerie). Demande de Jacob, 13/10/2026 : "ajoute aussi les films
''' et series VOD".
'''
''' Pour un TELECHARGEMENT, plusieurs episodes se choisissent d'un coup (Ctrl ou Maj + clic) : voir
''' <paramref name="multiple"/> de Initialiser et <see cref="Choisis"/>.
''' </summary>
Public Class FormEpisodes

    Private _choix As IptvSources.EpisodeIptv
    Private ReadOnly _choisis As New List(Of IptvSources.EpisodeIptv)

    ''' <summary>L'episode choisi (apres OK) ; en choix multiple : le premier.</summary>
    Friend ReadOnly Property Choix As IptvSources.EpisodeIptv
        Get
            Return _choix
        End Get
    End Property

    ''' <summary>Les episodes choisis (apres OK), dans l'ordre de la liste.</summary>
    Friend ReadOnly Property Choisis As List(Of IptvSources.EpisodeIptv)
        Get
            Return _choisis
        End Get
    End Property

    ''' <summary>Remplit la fenetre : l'episode dont l'adresse est <paramref name="adresseChoisie"/> (choisi
    ''' la fois d'avant) est selectionne, sinon le premier. <paramref name="multiple"/> : plusieurs episodes
    ''' peuvent etre choisis (telechargement).</summary>
    Friend Sub Initialiser(nomDeLaSerie As String, episodes As List(Of IptvSources.EpisodeIptv), adresseChoisie As String,
                           Optional multiple As Boolean = False)
        LabelSerie.Text = nomDeLaSerie & vbCrLf & episodes.Count & If(episodes.Count > 1, " épisodes", " épisode") &
                          If(multiple, "  -  Maj + flèches (ou Maj + clic) pour en choisir plusieurs, Ctrl + A pour tous", "  -  Ctrl ou Maj + clic : plusieurs épisodes à télécharger")
        ' Comme dans Windows (Ctrl ou Maj + clic) : un seul episode = on le lit ; plusieurs = on les telecharge.
        ListBoxEpisodes.SelectionMode = SelectionMode.MultiExtended
        If multiple Then
            ButtonLire.Text = "Télécharger les épisodes choisis"
            Text = "Épisodes à télécharger"
        End If
        _libelleLire = ButtonLire.Text
        ListBoxEpisodes.BeginUpdate()
        ListBoxEpisodes.Items.Clear()
        Dim rang As Integer = 0
        For Each ep As IptvSources.EpisodeIptv In episodes
            Dim k As Integer = ListBoxEpisodes.Items.Add(ep)
            If ep.AdresseEpisode = adresseChoisie Then rang = k
        Next
        ListBoxEpisodes.EndUpdate()
        If ListBoxEpisodes.Items.Count > 0 Then ListBoxEpisodes.SelectedIndex = rang
        ' La fenetre s'elargit pour montrer les titres en entier (jusqu'a la largeur de l'ecran) ; elle se redimensionne aussi a la main.
        Try
            Dim largeurMax As Integer = 0
            For Each ep As IptvSources.EpisodeIptv In episodes
                largeurMax = Math.Max(largeurMax, TextRenderer.MeasureText(ep.ToString(), ListBoxEpisodes.Font).Width)
            Next
            Dim manque As Integer = largeurMax + SystemInformation.VerticalScrollBarWidth + 16 - ListBoxEpisodes.ClientSize.Width
            If manque > 0 Then
                Dim ecran As Rectangle = Screen.FromControl(Me).WorkingArea
                ClientSize = New Size(Math.Min(ClientSize.Width + manque, ecran.Width - 40), ClientSize.Height)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Private _libelleLire As String = ""

    ''' <summary>Plusieurs episodes choisis : le bouton annonce le telechargement (hors du mode « telecharger », ou il le dit deja).</summary>
    Private Sub ListBoxEpisodes_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBoxEpisodes.SelectedIndexChanged
        If _libelleLire = "" Then Exit Sub
        Dim n As Integer = ListBoxEpisodes.SelectedItems.Count
        ButtonLire.Text = If(n > 1 AndAlso Text = "Épisodes", "Télécharger les " & n & " épisodes choisis", _libelleLire)
    End Sub

    Private Sub FormEpisodes_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        MoletteSousLaSouris.Suivre(Me)
    End Sub

    ''' <summary>Le clavier est a la liste des episodes des l'ouverture : Maj + fleche haut / bas etend la selection comme dans l'Explorateur de Windows.</summary>
    Private Sub FormEpisodes_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        ListBoxEpisodes.Select()
    End Sub

    ''' <summary>Ctrl + A : tous les episodes (choix multiple, telechargement). Maj + fleches / Maj + Fin / Maj + Debut / Maj + Page sont ceux de la liste elle-meme (selection etendue).</summary>
    Private Sub ListBoxEpisodes_KeyDown(sender As Object, e As KeyEventArgs) Handles ListBoxEpisodes.KeyDown
        If e.Control AndAlso e.KeyCode = Keys.A AndAlso ListBoxEpisodes.SelectionMode = SelectionMode.MultiExtended Then
            e.SuppressKeyPress = True : e.Handled = True
            ListBoxEpisodes.BeginUpdate()
            For i As Integer = 0 To ListBoxEpisodes.Items.Count - 1
                ListBoxEpisodes.SetSelected(i, True)
            Next
            ListBoxEpisodes.EndUpdate()
        End If
    End Sub

    Private Sub ButtonLire_Click(sender As Object, e As EventArgs) Handles ButtonLire.Click
        _choisis.Clear()
        For Each element As Object In ListBoxEpisodes.SelectedItems
            Dim ep As IptvSources.EpisodeIptv = TryCast(element, IptvSources.EpisodeIptv)
            If ep IsNot Nothing Then _choisis.Add(ep)
        Next
        If _choisis.Count = 0 Then Exit Sub
        _choix = _choisis(0)
        DialogResult = DialogResult.OK
    End Sub

    Private Sub ListBoxEpisodes_DoubleClick(sender As Object, e As EventArgs) Handles ListBoxEpisodes.DoubleClick
        If ListBoxEpisodes.SelectedItem IsNot Nothing Then ButtonLire.PerformClick()
    End Sub

End Class
