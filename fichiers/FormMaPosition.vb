''' <summary>
''' « Ma position » : UNE seule fenetre. Elle cherche la position (localisation de Windows, sinon adresse IP : approximative), montre la ville trouvee ;
''' on peut taper un autre nom puis « Chercher » (base de lieux d'Open-Meteo), et « Utiliser cette ville » l'ajoute aux villes de Slide (voir VillesAjoutees).
''' Choisie : la ville retenue (Nothing si on annule).
''' </summary>
Public Class FormMaPosition

    Friend Choisie As VillesAjoutees.Resultat
    Private _trouvee As VillesAjoutees.Resultat

    Private Sub FormMaPosition_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        LabelEtat.Text = "Recherche de votre position..."
        LabelResultat.Text = ""
        TextBoxVille.Enabled = False
        ButtonChercher.Enabled = False
        ButtonUtiliser.Enabled = False
    End Sub

    Private Async Sub FormMaPosition_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        Dim r As VillesAjoutees.Resultat = Await VillesAjoutees.LocaliserAsync()
        If IsDisposed Then Exit Sub
        TextBoxVille.Enabled = True
        ButtonChercher.Enabled = True
        If r.Ok AndAlso r.Origine = "la localisation de Windows" Then
            LabelEtat.Text = "Votre position a été trouvée par la localisation de Windows. Vous pouvez aussi taper le nom d'une autre ville."
            TextBoxVille.Text = r.Nom
            Montrer(r)
        ElseIf r.Ok Then
            LabelEtat.Text = "Position approximative d'après l'adresse IP (souvent la grande ville voisine). Si ce n'est pas la vôtre, tapez son nom puis « Chercher »."
            TextBoxVille.Text = r.Nom
            Montrer(r)
        Else
            LabelEtat.Text = r.Message & vbCrLf & "Tapez le nom de votre ville puis « Chercher »."
        End If
        ActiveControl = TextBoxVille
        TextBoxVille.SelectAll()
    End Sub

    Private Sub Montrer(r As VillesAjoutees.Resultat)
        _trouvee = r
        LabelResultat.Text = r.Nom & If(r.Detail <> "", vbCrLf & r.Detail, "")
        ButtonUtiliser.Enabled = True
    End Sub

    Private Async Sub ButtonChercher_Click(sender As Object, e As EventArgs) Handles ButtonChercher.Click
        If TextBoxVille.Text.Trim() = "" Then Exit Sub
        ButtonChercher.Enabled = False
        ButtonUtiliser.Enabled = False
        _trouvee = Nothing
        LabelResultat.Text = "Recherche..."
        Dim t As VillesAjoutees.Resultat = Await VillesAjoutees.ChercherParNomAsync(TextBoxVille.Text)
        If IsDisposed Then Exit Sub
        ButtonChercher.Enabled = True
        If t.Ok Then Montrer(t) Else LabelResultat.Text = t.Message
    End Sub

    Private Sub ButtonUtiliser_Click(sender As Object, e As EventArgs) Handles ButtonUtiliser.Click
        If _trouvee Is Nothing Then Exit Sub
        If VillesAjoutees.Ajouter(_trouvee) Then
            Choisie = _trouvee
            DialogResult = DialogResult.OK
            Close()
        Else
            LabelResultat.Text = _trouvee.Message
        End If
    End Sub

End Class
