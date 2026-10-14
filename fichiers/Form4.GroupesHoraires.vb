Option Strict On
Option Explicit On

''' <summary>
''' Groupes d'horaires de la minuterie (30/10/2026) : quand DEUX taches (ou plus) a la meme heure se suivent dans un tableau, la premiere colonne (la case Actif) de ces lignes est
''' coloriee, d'une couleur differente pour chaque groupe. Sert a voir d'un coup d'oeil les taches qui partent ensemble. Les trois tableaux (principal, minuterie optionnelle, inactifs)
''' sont concernes ; le recalcul se fait apres chaque ajout, retrait, changement de valeur ou tri.
''' </summary>
Partial Class Form4

    Private Const TachesMiniParGroupe As Integer = 2          ' (3 jusqu'au 30/10/2026)

    ''' <summary>Couleurs moyennes, lisibles sur le theme clair comme sur le theme sombre.</summary>
    Private ReadOnly _couleursDeGroupe As Color() = {
        Color.FromArgb(255, 170, 60), Color.FromArgb(80, 160, 255), Color.FromArgb(100, 200, 120),
        Color.FromArgb(235, 110, 190), Color.FromArgb(170, 140, 255), Color.FromArgb(240, 215, 70),
        Color.FromArgb(70, 200, 200), Color.FromArgb(240, 100, 90)}

    Private _groupesDemandes As Boolean

    Private Sub LignesChangees(sender As Object, e As EventArgs) Handles DataGridView1.RowsAdded, DataGridView1.RowsRemoved, DataGridView1.CellValueChanged, DataGridView1.Sorted,
            DataGridViewTuya.RowsAdded, DataGridViewTuya.RowsRemoved, DataGridViewTuya.CellValueChanged, DataGridViewTuya.Sorted,
            DataGridView2.RowsAdded, DataGridView2.RowsRemoved, DataGridView2.CellValueChanged, DataGridView2.Sorted
        If _groupesDemandes OrElse Not IsHandleCreated Then Exit Sub
        _groupesDemandes = True
        BeginInvoke(Sub()
                        _groupesDemandes = False
                        ColorierLesGroupes()
                    End Sub)
    End Sub

    Private Sub ColorierAffichage(sender As Object, e As EventArgs) Handles Me.Shown
        ColorierLesGroupes()
    End Sub

    ''' <summary>Colonnes : 0 = Actif, 1 = Jour, 2 = Heure (les trois tableaux ont les memes).</summary>
    Friend Sub ColorierLesGroupes()
        Try
            Dim numero As Integer = 0
            For Each grille As DataGridView In {DataGridView1, DataGridViewTuya, DataGridView2}
                If grille Is Nothing OrElse grille.ColumnCount < 3 Then Continue For
                Dim lignes As New List(Of DataGridViewRow)
                For Each ligne As DataGridViewRow In grille.Rows
                    If Not ligne.IsNewRow Then lignes.Add(ligne)
                Next
                Dim debut As Integer = 0
                While debut < lignes.Count
                    Dim cle As String = CleHoraire(lignes(debut))
                    Dim fin As Integer = debut
                    While fin + 1 < lignes.Count AndAlso cle <> "" AndAlso CleHoraire(lignes(fin + 1)) = cle
                        fin += 1
                    End While
                    Dim couleur As Color = Color.Empty
                    If cle <> "" AndAlso fin - debut + 1 >= TachesMiniParGroupe Then
                        couleur = _couleursDeGroupe(numero Mod _couleursDeGroupe.Length)
                        numero += 1
                    End If
                    For k As Integer = debut To fin
                        If lignes(k).Cells(0).Style.BackColor <> Color.Empty Then lignes(k).Cells(0).Style.BackColor = Color.Empty          ' (la case Actif n'est plus coloriee : c'est la marge, a gauche)
                        Dim avant As Object = lignes(k).HeaderCell.Tag
                        If Not (couleur = Color.Empty AndAlso avant Is Nothing) AndAlso Not (TypeOf avant Is Color AndAlso CType(avant, Color) = couleur) Then
                            lignes(k).HeaderCell.Tag = If(couleur = Color.Empty, Nothing, CObj(couleur))
                            grille.InvalidateRow(lignes(k).Index)
                        End If
                    Next
                    debut = fin + 1
                End While
            Next
        Catch ex As Exception
            DiagLog("Minuterie : couleurs des groupes d'horaires impossibles (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>La couleur du groupe est dessinee dans la toute premiere colonne (l'en-tete de ligne, a gauche) : peinte ici, elle ne depend ni du theme ni des styles visuels de Windows.</summary>
    Private Sub PeindreLaMargeDuGroupe(sender As Object, e As DataGridViewRowPostPaintEventArgs) Handles DataGridView1.RowPostPaint, DataGridViewTuya.RowPostPaint, DataGridView2.RowPostPaint
        Try
            Dim grille As DataGridView = DirectCast(sender, DataGridView)
            If e.RowIndex < 0 OrElse e.RowIndex >= grille.Rows.Count OrElse Not grille.RowHeadersVisible Then Exit Sub
            Dim tag As Object = grille.Rows(e.RowIndex).HeaderCell.Tag
            If Not (TypeOf tag Is Color) Then Exit Sub
            Using pinceau As New SolidBrush(CType(tag, Color))
                e.Graphics.FillRectangle(pinceau, e.RowBounds.Left + 2, e.RowBounds.Top + 1, grille.RowHeadersWidth - 4, e.RowBounds.Height - 2)
            End Using
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>Meme jour et meme heure : les taches qui partent ensemble ("" si l'heure est vide).</summary>
    Private Function CleHoraire(ligne As DataGridViewRow) As String
        Dim heure As String = Convert.ToString(ligne.Cells(2).Value)
        If String.IsNullOrWhiteSpace(heure) Then Return ""
        Return Convert.ToString(ligne.Cells(1).Value) & "|" & heure.Trim()
    End Function

End Class
