Imports System.Windows.Forms

''' <summary>
''' Ligne de la minuterie sur laquelle portent l'apercu d'un film (Form6, Form11) et les choix faits dans une liste (Module2).
''' Remplace le Label36 cache de Form4, qui tenait ce numero sous forme de texte (21/10/2026).
''' </summary>
Friend Module LigneMinuterieEnCours
    ''' <summary>Numero de la ligne (-1 : aucune).</summary>
    Friend Index As Integer = -1
End Module

''' <summary>
''' Choix d'un film (ou d'une chaine) dans la fenetre Nflix (Form14) pour une ligne de la minuterie (reecrit le 22/10/2026 :
''' "stocker l'emplacement de la ligne de la minuterie dans un label cache n'est pas professionnel").
'''
''' La ligne a remplir est donnee EXPLICITEMENT a l'ouverture (<see cref="Ouvrir"/>) sous la forme de l'objet ligne lui-meme, et non d'un
''' numero : le tableau de la minuterie est retrie quand on ajoute ou deplace une ligne, un numero retenu a l'ouverture pouvait alors
''' designer une AUTRE ligne (22/10/2026 : le film etait ecrit dans une ligne de chaine TV, dont la case est une liste deroulante
''' qui refusait le titre : rien ne paraissait dans la minuterie). Le OK (<see cref="Valider"/>) ecrit dans cette ligne et la remet en
''' ligne « Film » si elle etait d'un autre type.
''' </summary>
Friend Module ChoixNflix

    ''' <summary>Ligne de la minuterie que le OK de Nflix remplit (Nothing : Nflix est ouverte hors de la minuterie, par exemple pour
    ''' telecharger : il n'y a rien a remplir).</summary>
    Friend LigneCible As DataGridViewRow

    ''' <summary>Ouvre la fenetre Nflix. <paramref name="ligne"/> : la ligne de la minuterie a remplir, ou Nothing.</summary>
    Friend Function Ouvrir(ligne As DataGridViewRow, genre As IptvSources.GenreIptv) As Boolean
        Try
            LigneCible = ligne
            If ligne IsNot Nothing Then LigneMinuterieEnCours.Index = ligne.Index
            IptvSources.GenreAffiche = genre
            If CInt(rechercher()) = 1 Then
                Form14.Show()
                Return True
            End If
        Catch ex As Exception
            DiagLog("Nflix (ouverture) : " & ex.Message)
        End Try
        Return False
    End Function

    ''' <summary>Le OK de Nflix : le film (ou la chaine) selectionne dans <paramref name="liste"/> est ecrit dans la ligne cible de la minuterie.
    ''' Renvoie False, sans rien ecrire, s'il n'y a pas de ligne cible valide ou pas de film selectionne.</summary>
    Friend Function Valider(liste As DataGridView) As Boolean
        Dim grille As DataGridView = Form4.GrilleCourante
        Dim ligne As DataGridViewRow = LigneCible
        If ligne Is Nothing Then
            DiagLog("Nflix OK : aucune ligne de la minuterie a remplir")
            Return False
        End If
        If Not ReferenceEquals(ligne.DataGridView, grille) Then
            DiagLog("Nflix OK : la ligne de la minuterie a ete retiree depuis l'ouverture")
            Return False
        End If

        ' Le film choisi est celui de la ligne SELECTIONNEE dans la liste (un simple clic suffit) ; a defaut la derniere lue par double-clic.
        Dim choisie As Integer = -1
        If liste.CurrentCell IsNot Nothing Then choisie = liste.CurrentCell.RowIndex
        If (choisie < 0 OrElse choisie >= liste.RowCount) AndAlso liste.SelectedRows.Count > 0 Then choisie = liste.SelectedRows(0).Index
        If (choisie < 0 OrElse choisie >= liste.RowCount) Then choisie = CInt(Val(Form14.Label1.Text))
        If choisie < 0 OrElse choisie >= liste.RowCount Then
            DiagLog("Nflix OK : aucune ligne selectionnee dans la liste (" & liste.RowCount & " lignes)")
            Return False
        End If
        Dim titre As String = Convert.ToString(liste.Item(0, choisie).Value)
        Dim adresse As String = Convert.ToString(liste.Item(1, choisie).Value)
        If String.IsNullOrWhiteSpace(titre) OrElse String.IsNullOrWhiteSpace(adresse) Then
            DiagLog("Nflix OK : ligne " & choisie & " sans titre ou sans adresse (titre='" & titre & "')")
            Return False
        End If

        Dim cellules As DataGridViewCellCollection = ligne.Cells
        Dim typeLigne As String = UCase(Convert.ToString(cellules("Type").Value)).Trim()
        Dim ancienneAction As String = If(typeLigne = "FILM" OrElse typeLigne = "IPTV", Convert.ToString(cellules("Action").Value), "")

        ' La case Action doit etre une case de texte : une ligne de chaine TV ou de Sat y a une liste deroulante, qui refuse tout titre
        ' absent de sa liste. Une ligne d'un autre type devient une ligne Film.
        If Not (TypeOf cellules("Action") Is DataGridViewTextBoxCell) Then cellules("Action") = New DataGridViewTextBoxCell()
        ' Chaine de TV (liste des chaines) : ligne « IPTV », sans duree propre ; film ou serie : ligne « Film ».
        Dim estChaine As Boolean = (IptvSources.GenreAffiche = IptvSources.GenreIptv.Chaines)
        cellules("Type").Value = If(estChaine, "IPTV", "Film")
        If Not estChaine AndAlso Not (TypeOf cellules("Play") Is DataGridViewButtonCell) Then cellules("Play") = New DataGridViewButtonCell()

        ' Un autre film : l'ancienne plage (colonne Play) et l'ancienne duree ne sont plus les siennes ; elles sont recalculees a l'apercu.
        If Not estChaine AndAlso ancienneAction <> titre Then
            cellules("Play").Value = ""
            cellules("Durée").Value = ""
        End If

        ' La ligne avait un film avec ses reglages (PIP et son coin, boucle...) : proposes au nouveau film, comme pour un fichier.
        Dim reglagesGardes As String = ""
        Try
            If typeLigne = "FILM" AndAlso ancienneAction <> titre Then
                Dim anciens As String = ReglagesConservesDuFilm(Convert.ToString(cellules("Argument").Value))
                If anciens <> "" Then
                    Dim volumeLigne As String = Convert.ToString(cellules("Volume").Value).Trim()
                    If ReprendreLesReglagesDuDossier(titre, ancienneAction, volumeLigne, anciens, True) Then reglagesGardes = anciens
                End If
            End If
        Catch ex As Exception
            DiagLog("Nflix (reglages du film remplace) : " & ex.Message)
        End Try

        ' Modele d'une ligne « Film » de Nflix : Action = le TITRE ; l'adresse est dans les reglages (voir la memoire du projet).
        cellules("Action").Value = titre
        cellules("Argument").Value = "!/Titre:" & titre & "!/Url:" & adresse & "!/Source:" & IptvSources.NomIptv(IptvSources.SelectionIptv) & reglagesGardes
        cellules("Action").ToolTipText = adresse
        cellules(0).ToolTipText = Convert.ToString(cellules("Argument").Value)
        Try
            grille.CurrentCell = cellules("Action")           ' la ligne remplie est visible, selectionnee
        Catch ex As Exception
        End Try
        LigneCible = Nothing
        Return True
    End Function

End Module
