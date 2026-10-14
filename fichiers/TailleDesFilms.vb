''' <summary>
''' La taille du film choisi dans Nflix, sous le bouton « Plus d'infos » (27/11/2026 : « enleve la colonne de la taille du fichier et rajoute dans le cadre rouge la taille,
''' que tu recupereras plus rapidement »). Le catalogue du fournisseur ne la donne pas : elle est demandee au serveur (le premier octet du fichier, comme avant un telechargement)
''' des qu'un titre est choisi, pour ce seul titre, puis gardee en memoire. Films et episodes seulement (une chaine en direct ou une serie sans episode choisi n'a pas de taille).
''' Sans reponse : deux nouveaux essais, puis « ? » (la raison en info-bulle et au journal) ; choisir a nouveau le titre redemande.
''' Rien n'est ecrit sur le disque : les adresses des films contiennent le compte. Fichier partage avec le programme IPTV.
''' </summary>
Friend Module TailleDesFilms

    ' Attente avant chaque essai, en millisecondes. La premiere, tres courte, evite une demande par titre quand on descend la liste au clavier.
    Private ReadOnly Attentes As Integer() = {120, 1000, 3000}

    Private ReadOnly _octets As New Dictionary(Of String, Long)(StringComparer.Ordinal)          ' adresse -> taille connue (fil de l'interface seulement)

    ''' <summary>Montre dans <paramref name="libelle"/> la taille du titre choisi dans <paramref name="grille"/> (adresse du film dans la 2e colonne).</summary>
    Friend Sub Suivre(grille As DataGridView, libelle As Label)
        Dim bulle As New ToolTip()
        libelle.Text = ""
        AddHandler grille.CurrentCellChanged, Sub() Montrer(grille, libelle, bulle)
        ' L'adresse de la ligne choisie change quand un episode d'une serie vient d'etre choisi.
        AddHandler grille.CellValueChanged, Sub(s As Object, e As DataGridViewCellEventArgs)
                                                If e.ColumnIndex = 1 AndAlso grille.CurrentCell IsNot Nothing AndAlso e.RowIndex = grille.CurrentCell.RowIndex Then Montrer(grille, libelle, bulle)
                                            End Sub
        AddHandler grille.Disposed, Sub() bulle.Dispose()
    End Sub

    ' Le libelle garde dans Tag l'adresse qu'il montre : une reponse arrivee apres qu'on a choisi un autre titre n'est pas affichee.
    Private Async Sub Montrer(grille As DataGridView, libelle As Label, bulle As ToolTip)
        Try
            Dim adresse As String = ""
            Dim titre As String = ""
            If grille.CurrentCell IsNot Nothing Then
                Dim ligne As DataGridViewRow = grille.Rows(grille.CurrentCell.RowIndex)
                titre = Convert.ToString(ligne.Cells(0).Value)
                adresse = Convert.ToString(ligne.Cells(1).Value)
            End If
            If String.Equals(TryCast(libelle.Tag, String), adresse) Then Exit Sub          ' (autre case de la meme ligne)
            libelle.Tag = adresse
            bulle.SetToolTip(libelle, "")
            If Not IptvSources.EstTelechargeable(adresse) Then
                libelle.Text = ""
                Exit Sub
            End If
            Dim octets As Long
            If _octets.TryGetValue(adresse, octets) Then
                libelle.Text = EnClair(octets)
                Exit Sub
            End If
            libelle.Text = "Taille : …"
            Dim raison As String = ""
            For Each attente As Integer In Attentes
                Await Task.Delay(attente)
                If libelle.IsDisposed OrElse Not String.Equals(TryCast(libelle.Tag, String), adresse) Then Exit Sub
                Dim chrono As Stopwatch = Stopwatch.StartNew()
                Try
                    octets = Await Task.Run(Function() IptvSources.TailleDuFichier(adresse))
                    If octets > 0 Then
                        _octets(adresse) = octets
                        If chrono.ElapsedMilliseconds >= 2000 Then DiagLog("Nflix : taille de « " & titre & " » reçue en " & chrono.ElapsedMilliseconds & " ms")
                        If Not libelle.IsDisposed AndAlso String.Equals(TryCast(libelle.Tag, String), adresse) Then libelle.Text = EnClair(octets)
                        Exit Sub
                    End If
                    raison = "le serveur ne donne pas la taille"
                Catch ex As OperationCanceledException
                    raison = "pas de réponse du serveur"
                Catch ex As Exception
                    raison = ex.Message
                End Try
            Next
            If libelle.IsDisposed OrElse Not String.Equals(TryCast(libelle.Tag, String), adresse) Then Exit Sub
            libelle.Text = "Taille : ?"
            libelle.Tag = Nothing                    ' choisir a nouveau le titre redemande
            bulle.SetToolTip(libelle, "Taille non donnée par le serveur : " & raison)
            DiagLog("Nflix : taille introuvable pour « " & titre & " » après " & Attentes.Length & " essais (" & raison & ")")          ' (l'adresse n'est jamais notee)
        Catch ex As Exception
        End Try
    End Sub

    ' Comme Windows : 1 Go = 1024 Mo.
    Private Function EnClair(octets As Long) As String
        Return "Taille : " & (octets / 1073741824.0).ToString("0.00") & " Go"
    End Function

End Module
