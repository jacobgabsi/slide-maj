''' <summary>
''' "Plus d'infos" d'un film ou d'une serie de Nflix (19/10/2026) : l'affiche, toutes les rubriques de la fiche du serveur
''' (titre original, genre, annee, duree, note, pays, age, realisateur, acteurs, image, son, debit, date d'ajout), le resume
''' en entier et la bande-annonce (ouverte dans le navigateur).
''' </summary>
Public Class FormInfoFilm

    Private _bandeAnnonce As String = ""

    ''' <summary>Montre la fiche. L'affiche est chargee en tache de fond, la fenetre s'ouvre tout de suite.</summary>
    Friend Shared Sub Montrer(proprietaire As IWin32Window, titre As String, fiche As IptvSources.FicheIptv)
        Using f As New FormInfoFilm()
            f.LabelTitre.Text = titre
            f.Text = "Plus d'infos - " & titre
            f.TextBoxInfos.Text = fiche.EnTexteComplet()
            f.TextBoxInfos.SelectionStart = 0
            f._bandeAnnonce = AdresseBandeAnnonce(fiche.BandeAnnonce)
            f.ButtonBandeAnnonce.Enabled = f._bandeAnnonce <> ""
            f.ChargerLAffiche(fiche.Affiche)
            f.ChargerLesNotes(titre, fiche)
            f.ShowDialog(proprietaire)
        End Using
    End Sub

    ''' <summary>Le serveur donne souvent seulement le code de la video YouTube : adresse complete dans ce cas ; "" si rien d'utilisable.</summary>
    Private Shared Function AdresseBandeAnnonce(brut As String) As String
        Dim b As String = If(brut, "").Trim()
        If b = "" Then Return ""
        If b.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse b.StartsWith("https://", StringComparison.OrdinalIgnoreCase) Then Return b
        If System.Text.RegularExpressions.Regex.IsMatch(b, "^[A-Za-z0-9_-]{6,20}$") Then Return "https://www.youtube.com/watch?v=" & b
        Return ""
    End Function

    ''' <summary>Les notes de la concurrence (OMDb : IMDb, Rotten Tomatoes, Metacritic), placees en tete de la fiche des qu'elles arrivent.
    ''' La note du serveur (TMDB le plus souvent) est rappelee pour les comparer.</summary>
    Private Async Sub ChargerLesNotes(titre As String, fiche As IptvSources.FicheIptv)
        Dim fiche_texte As String = fiche.EnTexteComplet()
        Dim serveur As String = If(fiche.Note <> "", "Note du serveur : " & fiche.Note & " / 10", "Note du serveur : aucune")
        Dim cle As String = NotesOmdb.CleDAcces()
        If cle <> "" Then
            TextBoxInfos.Text = serveur & vbCrLf & "Autres notes : chargement..." & vbCrLf & vbCrLf & fiche_texte
        End If
        Try
            Dim notes As String = Await Task.Run(Function() NotesOmdb.Chercher(cle, fiche.TitreOriginal, titre, fiche.Annee))
            If IsDisposed Then Exit Sub
            TextBoxInfos.Text = serveur & vbCrLf & notes & vbCrLf & vbCrLf & fiche_texte
            TextBoxInfos.SelectionStart = 0
        Catch ex As Exception
        End Try
    End Sub

    Private Async Sub ChargerLAffiche(adresse As String)
        If String.IsNullOrWhiteSpace(adresse) OrElse Not adresse.StartsWith("http", StringComparison.OrdinalIgnoreCase) Then Exit Sub
        Try
            Dim octets As Byte() = Await Task.Run(Async Function()
                                                      Using client As New Net.Http.HttpClient With {.Timeout = TimeSpan.FromSeconds(15)}
                                                          Return Await client.GetByteArrayAsync(adresse.Replace(" ", "%20"))
                                                      End Using
                                                  End Function)
            If IsDisposed Then Exit Sub
            Using flux As New IO.MemoryStream(octets)
                Using image As Image = Image.FromStream(flux)
                    PictureBoxAffiche.Image = New Bitmap(image)
                End Using
            End Using
        Catch ex As Exception
            ' pas d'affiche : le cadre reste vide
        End Try
    End Sub

    Private Sub ButtonBandeAnnonce_Click(sender As Object, e As EventArgs) Handles ButtonBandeAnnonce.Click
        Try
            Process.Start(New ProcessStartInfo(_bandeAnnonce) With {.UseShellExecute = True})
        Catch ex As Exception
            MessageBox.Show(Me, "La bande-annonce n'a pas pu être ouverte : " & ex.Message, "Plus d'infos", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub FormInfoFilm_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
