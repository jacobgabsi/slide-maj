Option Strict On
Option Explicit On

''' <summary>
''' Fenetre de choix de l'import des parametres (27/10/2026) : tout ce que contient le fichier, sous la forme de la fenetre des Options (onglet > cadre > reglage), a cocher element
''' par element. Pour chaque element : la valeur du fichier, la valeur actuelle de ce PC et s'ils different. Recherche, filtre « ce qui diffère », cases par cadre / onglet.
''' Les lignes de minuterie et les radios se cochent une a une, avec le choix « remplacer » ou « ajouter ». Rien n'est ecrit ici : voir ImportParametres.Appliquer.
''' Les controles et leur disposition sont dans le Concepteur.
''' </summary>
Public Class FormImportParametres

    Private _contenu As ContenuImport
    Private _enMiseAJour As Boolean

    ''' <summary>Montre la fenetre. Rend vrai si l'utilisateur valide, avec ses choix coches dans <paramref name="contenu"/> (ElementImport.Coche) et les deux modes.</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, contenu As ContenuImport, ByRef modeMinuterie As ModeImport, ByRef modeRadios As ModeImport) As Boolean
        Using f As New FormImportParametres()
            f._contenu = contenu
            f.TopMost = True                       ' devant tout : sous Slide en plein ecran ou ses PIP, la fenetre pouvait rester cachee (22/10/2026)
            f.ShowInTaskbar = True
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            modeMinuterie = If(f.RadioButtonMinRemplacer.Checked, ModeImport.Remplacer, ModeImport.Ajouter)
            modeRadios = If(f.RadioButtonRadRemplacer.Checked, ModeImport.Remplacer, ModeImport.Ajouter)
            Return True
        End Using
    End Function

    Private Sub FormImportParametres_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        Dim c As ContenuImport = _contenu
        LabelFichier.Text = "Fichier : " & c.Chemin
        LabelOrigine.Text = "Exporté" & If(c.Poste <> "", " depuis le PC « " & c.Poste & " »", "") & If(c.DateExport <> "", " le " & c.DateExport, "") & If(c.VersionSlide <> "", " par Slide " & c.VersionSlide, "") &
                            "   —   " & c.Elements.Count & " élément(s), dont " & c.Elements.Where(Function(x) x.Differe).Count() & " différent(s) de ce PC" &
                            If(c.NonReconnus.Count > 0, "   —   " & c.NonReconnus.Count & " réglage(s) inconnu(s) de cette version (non importables)", "")
        GroupBoxMinuterie.Enabled = c.LignesMinuterieDuFichier > 0
        GroupBoxMinuterie.Text = "Lignes de la minuterie cochées : que faire des " & c.LignesMinuterieActuelles & " ligne(s) de ce PC ?"
        GroupBoxRadios.Enabled = c.RadiosDuFichier > 0
        GroupBoxRadios.Text = "Radios cochées : que faire des " & c.RadiosActuelles & " radio(s) de ce PC ?"
        LabelAvertissement.Text = "Seuls les éléments cochés sont repris ; tout le reste garde sa valeur actuelle sur ce PC. L'état actuel est sauvegardé avant l'import (dossier data\settings), " &
                                  "et Slide devra être relancé ensuite. Les mots de passe sont rechiffrés pour ce PC et ne sont jamais affichés. Les chemins (dossiers de photos et de films) sont repris tels quels."
        Reconstruire()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Arbre
    ' ------------------------------------------------------------------------------------------

    Private Function Correspond(e As ElementImport, filtre As String) As Boolean
        Return e.Nom.IndexOf(filtre, StringComparison.CurrentCultureIgnoreCase) >= 0 OrElse e.Libelle.IndexOf(filtre, StringComparison.CurrentCultureIgnoreCase) >= 0 OrElse
               e.Onglet.IndexOf(filtre, StringComparison.CurrentCultureIgnoreCase) >= 0 OrElse e.Cadre.IndexOf(filtre, StringComparison.CurrentCultureIgnoreCase) >= 0 OrElse
               (e.Genre <> GenreElement.Secret AndAlso e.ValeurFichier.IndexOf(filtre, StringComparison.CurrentCultureIgnoreCase) >= 0)
    End Function

    ''' <summary>Reconstruit l'arbre selon la recherche et le filtre ; les cases cochees sont gardees (ElementImport.Coche), meme pour ce qui est masque.</summary>
    Private Sub Reconstruire()
        Dim filtre As String = TextBoxRecherche.Text.Trim()
        Dim seulementEcarts As Boolean = CheckBoxDifferences.Checked
        _enMiseAJour = True
        TreeViewElements.BeginUpdate()
        Try
            TreeViewElements.Nodes.Clear()
            Dim onglets As New Dictionary(Of String, TreeNode)
            Dim cadres As New Dictionary(Of String, TreeNode)
            For Each e As ElementImport In _contenu.Elements
                If seulementEcarts AndAlso Not e.Differe Then Continue For
                If filtre <> "" AndAlso Not Correspond(e, filtre) Then Continue For
                Dim noeudOnglet As TreeNode = Nothing
                If Not onglets.TryGetValue(e.Onglet, noeudOnglet) Then
                    noeudOnglet = New TreeNode(e.Onglet) With {.Name = e.Onglet}
                    onglets(e.Onglet) = noeudOnglet
                    TreeViewElements.Nodes.Add(noeudOnglet)
                End If
                Dim parent As TreeNode = noeudOnglet
                If e.Cadre <> "" Then
                    Dim cle As String = e.Onglet & "|" & e.Cadre
                    Dim noeudCadre As TreeNode = Nothing
                    If Not cadres.TryGetValue(cle, noeudCadre) Then
                        noeudCadre = New TreeNode(e.Cadre) With {.Name = e.Cadre}
                        cadres(cle) = noeudCadre
                        noeudOnglet.Nodes.Add(noeudCadre)
                    End If
                    parent = noeudCadre
                End If
                Dim feuille As New TreeNode(If(e.Differe, "● ", "     ") & e.Libelle) With {.Tag = e, .Checked = e.Coche}
                If Not e.Differe Then feuille.ForeColor = Color.Gray
                parent.Nodes.Add(feuille)
            Next
            For Each n As TreeNode In TreeViewElements.Nodes
                RecompterDescendants(n)
            Next
            If filtre <> "" OrElse seulementEcarts Then
                TreeViewElements.ExpandAll()
            Else
                For Each n As TreeNode In TreeViewElements.Nodes
                    n.Expand()
                Next
            End If
            If TreeViewElements.Nodes.Count > 0 Then TreeViewElements.TopNode = TreeViewElements.Nodes(0)
        Finally
            TreeViewElements.EndUpdate()
            _enMiseAJour = False
        End Try
        AfficherLeDetail(TreeViewElements.SelectedNode)
        MettreAJourLeResume()
    End Sub

    ''' <summary>Coche des noeuds parents (tous leurs elements affiches sont coches) et compteur « (coches / total) » de leur titre, de bas en haut.</summary>
    Private Sub RecompterDescendants(noeud As TreeNode)
        If noeud.Tag IsNot Nothing Then Exit Sub
        Dim total As Integer = 0, coches As Integer = 0
        For Each enfant As TreeNode In noeud.Nodes
            If enfant.Tag Is Nothing Then
                RecompterDescendants(enfant)
                Dim t As Integer, k As Integer
                Compter(enfant, t, k)
                total += t : coches += k
            Else
                total += 1
                If enfant.Checked Then coches += 1
            End If
        Next
        noeud.Text = noeud.Name & "  (" & coches & " / " & total & ")"
        noeud.Checked = (total > 0 AndAlso coches = total)
    End Sub

    Private Sub Compter(noeud As TreeNode, ByRef total As Integer, ByRef coches As Integer)
        total = 0 : coches = 0
        For Each enfant As TreeNode In noeud.Nodes
            If enfant.Tag Is Nothing Then
                Dim t As Integer, k As Integer
                Compter(enfant, t, k)
                total += t : coches += k
            Else
                total += 1
                If enfant.Checked Then coches += 1
            End If
        Next
    End Sub

    Private Function Feuilles(noeuds As TreeNodeCollection) As List(Of TreeNode)
        Dim liste As New List(Of TreeNode)
        For Each n As TreeNode In noeuds
            If n.Tag IsNot Nothing Then liste.Add(n) Else liste.AddRange(Feuilles(n.Nodes))
        Next
        Return liste
    End Function

    Private Sub PoserLesCases(valeur As Func(Of ElementImport, Boolean))
        _enMiseAJour = True
        Try
            For Each f As TreeNode In Feuilles(TreeViewElements.Nodes)
                Dim e As ElementImport = DirectCast(f.Tag, ElementImport)
                e.Coche = valeur(e)
                f.Checked = e.Coche
            Next
            For Each n As TreeNode In TreeViewElements.Nodes
                RecompterDescendants(n)
            Next
        Finally
            _enMiseAJour = False
        End Try
        MettreAJourLeResume()
    End Sub

    Private Sub TreeViewElements_AfterCheck(sender As Object, e As TreeViewEventArgs) Handles TreeViewElements.AfterCheck
        If _enMiseAJour Then Exit Sub
        _enMiseAJour = True
        Try
            Dim noeud As TreeNode = e.Node
            If noeud.Tag IsNot Nothing Then
                DirectCast(noeud.Tag, ElementImport).Coche = noeud.Checked
            Else
                For Each f As TreeNode In Feuilles(noeud.Nodes)
                    f.Checked = noeud.Checked
                    DirectCast(f.Tag, ElementImport).Coche = noeud.Checked
                Next
            End If
            Dim racine As TreeNode = noeud
            While racine.Parent IsNot Nothing
                racine = racine.Parent
            End While
            RecompterDescendants(racine)
            ' (un onglet dont on vient de decocher un cadre se recompte aussi ; le noeud clique garde son propre etat)
        Finally
            _enMiseAJour = False
        End Try
        MettreAJourLeResume()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Detail et resume
    ' ------------------------------------------------------------------------------------------

    Private Sub TreeViewElements_AfterSelect(sender As Object, e As TreeViewEventArgs) Handles TreeViewElements.AfterSelect
        AfficherLeDetail(e.Node)
    End Sub

    Private Sub AfficherLeDetail(noeud As TreeNode)
        If noeud Is Nothing Then
            LabelNomDetail.Text = "Sélectionnez un élément dans la liste"
            LabelChemin.Text = "" : TextBoxFichier.Text = "" : TextBoxActuel.Text = "" : LabelEtatDetail.Text = ""
            Exit Sub
        End If
        Dim e As ElementImport = TryCast(noeud.Tag, ElementImport)
        If e Is Nothing Then
            Dim feuilles As List(Of TreeNode) = Me.Feuilles(noeud.Nodes)
            Dim ecarts As Integer = feuilles.Where(Function(f) DirectCast(f.Tag, ElementImport).Differe).Count()
            LabelNomDetail.Text = noeud.Name
            LabelChemin.Text = If(noeud.Parent IsNot Nothing, noeud.Parent.Name & " > " & noeud.Name, "Onglet de la fenêtre des Options")
            TextBoxFichier.Text = "" : TextBoxActuel.Text = ""
            LabelEtatDetail.Text = feuilles.Count & " élément(s) affiché(s), dont " & ecarts & " différent(s) de ce PC. Cochez la case pour tout reprendre d'un coup."
            Exit Sub
        End If
        LabelNomDetail.Text = e.Nom
        LabelChemin.Text = e.Onglet & If(e.Cadre <> "", " > " & e.Cadre, "") & "   (" & Genre(e.Genre) & ")"
        TextBoxFichier.Text = e.ValeurFichier
        TextBoxActuel.Text = e.ValeurActuelle
        LabelEtatDetail.Text = If(e.Differe, "● Différent de ce PC : l'importer change quelque chose.", "Identique à ce PC : l'importer ne change rien.")
    End Sub

    Private Function Genre(g As GenreElement) As String
        Select Case g
            Case GenreElement.Secret : Return "mot de passe / clé secrète"
            Case GenreElement.LigneMinuterie : Return "ligne de la minuterie"
            Case GenreElement.Radio : Return "radio"
            Case GenreElement.MinuterieEnregistree : Return "minuterie enregistrée"
            Case GenreElement.ReglageBlink : Return "réglage Blink"
            Case GenreElement.FichierReglage : Return "fichier de réglages"
            Case Else : Return "réglage"
        End Select
    End Function

    Private Function Cochees(g As GenreElement) As Integer
        Return _contenu.Elements.Where(Function(x) x.Genre = g AndAlso x.Coche).Count()
    End Function

    Private Sub MettreAJourLeResume()
        Dim r As Integer = Cochees(GenreElement.Reglage) + Cochees(GenreElement.ReglageBlink) + Cochees(GenreElement.FichierReglage), s As Integer = Cochees(GenreElement.Secret), l As Integer = Cochees(GenreElement.LigneMinuterie), d As Integer = Cochees(GenreElement.Radio)
        Dim parties As New List(Of String)
        If r > 0 Then parties.Add(r & " réglage(s)")
        If s > 0 Then parties.Add(s & " mot(s) de passe / clé(s)")
        If l > 0 Then parties.Add(l & " ligne(s) de minuterie")
        Dim me2 As Integer = Cochees(GenreElement.MinuterieEnregistree)
        If me2 > 0 Then parties.Add(me2 & " minuterie(s) enregistrée(s)")
        If d > 0 Then parties.Add(d & " radio(s)")
        LabelResume.Text = If(parties.Count = 0, "Rien n'est coché.", "Sélection : " & String.Join(", ", parties) & ".")
        ButtonImporter.Enabled = parties.Count > 0
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Commandes
    ' ------------------------------------------------------------------------------------------

    Private Sub TextBoxRecherche_TextChanged(sender As Object, e As EventArgs) Handles TextBoxRecherche.TextChanged
        Reconstruire()
    End Sub

    Private Sub CheckBoxDifferences_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxDifferences.CheckedChanged
        Reconstruire()
    End Sub

    Private Sub ButtonToutCocher_Click(sender As Object, e As EventArgs) Handles ButtonToutCocher.Click
        PoserLesCases(Function(x) True)
    End Sub

    Private Sub ButtonToutDecocher_Click(sender As Object, e As EventArgs) Handles ButtonToutDecocher.Click
        PoserLesCases(Function(x) False)
    End Sub

    Private Sub ButtonCocherDifferences_Click(sender As Object, e As EventArgs) Handles ButtonCocherDifferences.Click
        PoserLesCases(Function(x) x.Differe)
    End Sub

    ''' <summary>Dernier regard avant d'ecrire : ce qui va etre repris, et ce qui sera remplace.</summary>
    Private Sub ButtonImporter_Click(sender As Object, e As EventArgs) Handles ButtonImporter.Click
        Dim texte As New Text.StringBuilder()
        texte.AppendLine("Importer cette sélection ?").AppendLine().AppendLine(LabelResume.Text)
        Dim l As Integer = Cochees(GenreElement.LigneMinuterie), d As Integer = Cochees(GenreElement.Radio)
        If l > 0 Then
            If RadioButtonMinRemplacer.Checked Then
                texte.AppendLine().AppendLine("⚠ La minuterie de ce PC (" & _contenu.LignesMinuterieActuelles & " ligne(s)) sera ENTIÈREMENT REMPLACÉE par les " & l & " ligne(s) cochée(s).")
            Else
                texte.AppendLine().AppendLine("Les lignes de minuterie cochées s'ajoutent à celles de ce PC (celles qui existent déjà ne sont pas dupliquées).")
            End If
        End If
        If d > 0 Then
            If RadioButtonRadRemplacer.Checked Then
                texte.AppendLine().AppendLine("⚠ La liste de radios de ce PC (" & _contenu.RadiosActuelles & ") sera ENTIÈREMENT REMPLACÉE par les " & d & " radio(s) cochée(s).")
            Else
                texte.AppendLine().AppendLine("Les radios cochées s'ajoutent à la liste de ce PC (sans doublon d'adresse).")
            End If
        End If
        texte.AppendLine().Append("L'état actuel est sauvegardé juste avant (dossier data\settings).")
        If MessageBox.Show(Me, texte.ToString(), "Importer des paramètres", MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then DialogResult = DialogResult.OK
    End Sub

End Class
