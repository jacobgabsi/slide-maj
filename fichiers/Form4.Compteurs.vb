Option Strict On
Option Explicit On

''' <summary>
''' Les ronds de comptage (04/11/2026, Jacob : « en haut a droite de chaque tableau un rond avec le nombre de lignes remplies »). Un rond par tableau de la minuterie
''' (BadgeLignesPrincipal, BadgeLignesOptionnelle, BadgeLignesInactifs, poses dans le Concepteur). Une ligne est « remplie » quand elle a un Type ou une Action.
''' Le comptage est refait 150 ms apres le dernier changement (le chargement des tableaux fait des centaines de changements).
''' </summary>
Partial Class Form4

    Private WithEvents _minuteurCompteurs As New System.Windows.Forms.Timer With {.Interval = 150}

    Private Sub CompteursDemarrer(sender As Object, e As EventArgs) Handles MyBase.Shown
        ' Au premier plan : les barres de titre des tableaux (Minuterie optionnelle, Inactif) les recouvriraient.
        BadgeLignesPrincipal.BringToFront() : BadgeLignesOptionnelle.BringToFront() : BadgeLignesInactifs.BringToFront()
        PlacerLesRonds()
        MettreAJourLesCompteurs()
    End Sub

    ''' <summary>Les ronds suivent leur tableau (taille ou position changees avec les barres de separation) : le principal au-dessus de son tableau (le numero de version est decale a gauche pour lui faire de la place),
    ''' les deux autres a droite de la barre de titre qui est juste au-dessus de leur tableau (« Minuterie optionnelle », « Inactif »).</summary>
    Private Sub PlacerLesRonds()
        Try
            BadgeLignesPrincipal.Location = New Point(DataGridView1.Right - BadgeLignesPrincipal.Width - 2, DataGridView1.Top - BadgeLignesPrincipal.Height - 2)
            BadgeLignesOptionnelle.Location = New Point(DataGridViewTuya.Right - BadgeLignesOptionnelle.Width - 2, DataGridViewTuya.Top - BadgeLignesOptionnelle.Height - 2)
            BadgeLignesInactifs.Location = New Point(DataGridView2.Right - BadgeLignesInactifs.Width - 2, DataGridView2.Top - BadgeLignesInactifs.Height - 2)
        Catch ex As Exception
        End Try
    End Sub

    Private Sub CompteursReplacer(sender As Object, e As EventArgs) Handles DataGridView1.SizeChanged, DataGridView1.LocationChanged, DataGridViewTuya.SizeChanged, DataGridViewTuya.LocationChanged,
        DataGridView2.SizeChanged, DataGridView2.LocationChanged
        PlacerLesRonds()
    End Sub

    Private Sub CompteursChangement(sender As Object, e As EventArgs) Handles DataGridView1.RowsAdded, DataGridView1.RowsRemoved, DataGridView1.CellValueChanged,
        DataGridViewTuya.RowsAdded, DataGridViewTuya.RowsRemoved, DataGridViewTuya.CellValueChanged, DataGridView2.RowsAdded, DataGridView2.RowsRemoved, DataGridView2.CellValueChanged
        _minuteurCompteurs.Stop()
        _minuteurCompteurs.Start()
    End Sub

    Private Sub MinuteurCompteurs_Tick(sender As Object, e As EventArgs) Handles _minuteurCompteurs.Tick
        _minuteurCompteurs.Stop()
        MettreAJourLesCompteurs()
    End Sub

    Private Sub MettreAJourLesCompteurs()
        Try
            ' Tableau vide : pas de rond (04/11/2026, Jacob : « si 0 pas besoin »).
            For Each paire As (badge As BadgeNombre, grille As DataGridView) In {(BadgeLignesPrincipal, DataGridView1), (BadgeLignesOptionnelle, DataGridViewTuya), (BadgeLignesInactifs, DataGridView2)}
                Dim n As Integer = LignesRemplies(paire.grille)
                paire.badge.Nombre = n
                paire.badge.Visible = n > 0
            Next
        Catch ex As Exception
            DiagLog("Compteurs de lignes : " & ex.Message)
        End Try
    End Sub

    Private Shared Function LignesRemplies(grille As DataGridView) As Integer
        Dim n As Integer = 0
        Dim suffixe As String = If(grille.Columns.Contains("Type2"), "2", "")
        For Each ligne As DataGridViewRow In grille.Rows
            If ligne.IsNewRow Then Continue For
            Dim type As String = Convert.ToString(ligne.Cells("Type" & suffixe).Value)
            Dim action As String = Convert.ToString(ligne.Cells("Action" & suffixe).Value)
            If type <> "" OrElse action <> "" Then n += 1
        Next
        Return n
    End Function

End Class
