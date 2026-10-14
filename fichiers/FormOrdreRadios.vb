''' <summary>
''' Ordre des radios (cadre Radio, 20/10/2026 : "affiche plutot une liste ou je pourrai deplacer les radios") : toutes les radios dans une liste ;
''' on glisse une radio a sa place, ou on la choisit et on utilise les boutons (En haut, Monter, Descendre, En bas), ou on trie de A a Z.
''' "Enregistrer" rend le nouvel ordre : les rangs d'origine, dans l'ordre voulu.
''' </summary>
Public Class FormOrdreRadios

    ''' <summary>Une ligne de la liste : le rang d'origine de la radio dans la liste des reglages, et son nom.</summary>
    Private Class Ligne
        Friend Rang As Integer
        Friend Nom As String
        Public Overrides Function ToString() As String
            Return Nom
        End Function
    End Class

    Private _indexGlisse As Integer = -1

    ''' <summary>Montre la fenetre. <paramref name="noms"/> : le nom de chaque radio, dans l'ordre actuel (le rang d'origine est sa place dans cette
    ''' liste). <paramref name="selection"/> : le rang a selectionner. Rend Vrai si l'ordre a change (<paramref name="nouvelOrdre"/> : les rangs
    ''' d'origine, dans le nouvel ordre).</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, noms As IList(Of String), selection As Integer, nouvelOrdre As List(Of Integer)) As Boolean
        Using f As New FormOrdreRadios()
            For i As Integer = 0 To noms.Count - 1
                f.ListBoxRadios.Items.Add(New Ligne With {.Rang = i, .Nom = noms(i)})
            Next
            If selection >= 0 AndAlso selection < f.ListBoxRadios.Items.Count Then f.ListBoxRadios.SelectedIndex = selection
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            Dim ordre As New List(Of Integer)
            For Each element As Object In f.ListBoxRadios.Items
                ordre.Add(DirectCast(element, Ligne).Rang)
            Next
            ' Inchange : rien a enregistrer.
            If ordre.SequenceEqual(Enumerable.Range(0, ordre.Count)) Then Return False
            nouvelOrdre.AddRange(ordre)
            Return True
        End Using
    End Function

    Private Sub FormOrdreRadios_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        MettreAJourLesBoutons()
    End Sub

    Private Sub MettreAJourLesBoutons()
        Dim i As Integer = ListBoxRadios.SelectedIndex
        Dim n As Integer = ListBoxRadios.Items.Count
        ButtonHaut.Enabled = i > 0
        ButtonMonter.Enabled = i > 0
        ButtonDescendre.Enabled = i >= 0 AndAlso i < n - 1
        ButtonBas.Enabled = i >= 0 AndAlso i < n - 1
        ButtonTrier.Enabled = n > 1
    End Sub

    Private Sub ListBoxRadios_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBoxRadios.SelectedIndexChanged
        MettreAJourLesBoutons()
    End Sub

    ''' <summary>Deplace la ligne <paramref name="de"/> a la place <paramref name="vers"/> ; elle reste selectionnee.</summary>
    Private Sub Deplacer(de As Integer, vers As Integer)
        If de < 0 OrElse vers < 0 OrElse de >= ListBoxRadios.Items.Count OrElse vers >= ListBoxRadios.Items.Count OrElse de = vers Then Exit Sub
        Dim element As Object = ListBoxRadios.Items(de)
        ListBoxRadios.Items.RemoveAt(de)
        ListBoxRadios.Items.Insert(vers, element)
        ListBoxRadios.SelectedIndex = vers
    End Sub

    Private Sub ButtonHaut_Click(sender As Object, e As EventArgs) Handles ButtonHaut.Click
        Deplacer(ListBoxRadios.SelectedIndex, 0)
    End Sub

    Private Sub ButtonMonter_Click(sender As Object, e As EventArgs) Handles ButtonMonter.Click
        Deplacer(ListBoxRadios.SelectedIndex, ListBoxRadios.SelectedIndex - 1)
    End Sub

    Private Sub ButtonDescendre_Click(sender As Object, e As EventArgs) Handles ButtonDescendre.Click
        Deplacer(ListBoxRadios.SelectedIndex, ListBoxRadios.SelectedIndex + 1)
    End Sub

    Private Sub ButtonBas_Click(sender As Object, e As EventArgs) Handles ButtonBas.Click
        Deplacer(ListBoxRadios.SelectedIndex, ListBoxRadios.Items.Count - 1)
    End Sub

    ''' <summary>Tri alphabetique (accents et majuscules ignores) ; la radio choisie reste choisie.</summary>
    Private Sub ButtonTrier_Click(sender As Object, e As EventArgs) Handles ButtonTrier.Click
        Dim choisie As Object = ListBoxRadios.SelectedItem
        Dim triees As List(Of Object) = ListBoxRadios.Items.Cast(Of Object)().OrderBy(Function(x) RechercheNflix.Simplifier(DirectCast(x, Ligne).Nom), StringComparer.Ordinal).ToList()
        ListBoxRadios.BeginUpdate()
        ListBoxRadios.Items.Clear()
        For Each x As Object In triees
            ListBoxRadios.Items.Add(x)
        Next
        ListBoxRadios.EndUpdate()
        If choisie IsNot Nothing Then ListBoxRadios.SelectedItem = choisie
    End Sub

    ' Glisser-deposer : une radio attrapee avec la souris est deposee a sa nouvelle place.
    Private Sub ListBoxRadios_MouseDown(sender As Object, e As MouseEventArgs) Handles ListBoxRadios.MouseDown
        _indexGlisse = ListBoxRadios.IndexFromPoint(e.Location)
        If _indexGlisse >= 0 AndAlso e.Button = MouseButtons.Left Then ListBoxRadios.SelectedIndex = _indexGlisse
    End Sub

    Private Sub ListBoxRadios_MouseMove(sender As Object, e As MouseEventArgs) Handles ListBoxRadios.MouseMove
        If e.Button = MouseButtons.Left AndAlso _indexGlisse >= 0 AndAlso ListBoxRadios.IndexFromPoint(e.Location) <> _indexGlisse Then
            ListBoxRadios.DoDragDrop(ListBoxRadios.Items(_indexGlisse), DragDropEffects.Move)
        End If
    End Sub

    Private Sub ListBoxRadios_DragOver(sender As Object, e As DragEventArgs) Handles ListBoxRadios.DragOver
        e.Effect = DragDropEffects.Move
    End Sub

    Private Sub ListBoxRadios_DragDrop(sender As Object, e As DragEventArgs) Handles ListBoxRadios.DragDrop
        Dim point As Point = ListBoxRadios.PointToClient(New Point(e.X, e.Y))
        Dim cible As Integer = ListBoxRadios.IndexFromPoint(point)
        If cible < 0 Then cible = ListBoxRadios.Items.Count - 1          ' (depose sous la derniere radio : tout en bas)
        If _indexGlisse >= 0 Then Deplacer(_indexGlisse, cible)
        _indexGlisse = -1
    End Sub

End Class
