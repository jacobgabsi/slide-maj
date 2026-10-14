''' <summary>
''' Fenetre de choix multiples d'une tache de la minuterie (20/10/2026 : "oui") pour les types Photo Mode, Video Mode et Autres : on coche les
''' reglages que la tache change et on choisit leur valeur. La case Action de la ligne recoit le resume, par exemple
''' "Animer les photos : Oui ; Durée d'affichage d'une photo : 10 s" (morceaux separes par " ; "). La liste des reglages vient de
''' ReglagesMinuterie ; les lignes (case + liste de valeurs) sont creees a l'ouverture, d'apres cette liste.
''' </summary>
Public Class FormReglages

    Private Class LigneReglage
        Friend Groupe As ReglagesMinuterie.GroupeReglage
        Friend Case_ As CheckBox
        Friend Valeur As ComboBox
    End Class

    Private ReadOnly _lignes As New List(Of LigneReglage)
    Private _actuelle As String = ""
    Private _chargement As Boolean = True

    ''' <summary>Montre la fenetre pour le type de tache <paramref name="type"/>. <paramref name="statiques"/> : les actions simples deja proposees par le type.
    ''' Rend Vrai avec la nouvelle action si on valide.</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, type As String, statiques As IEnumerable(Of String), actuelle As String, ByRef nouvelle As String) As Boolean
        Using f As New FormReglages()
            f.Text = "Réglages de la tâche (" & type & ")"
            f._actuelle = If(actuelle, "")
            f.Construire(ReglagesMinuterie.Groupes(type, statiques))
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            nouvelle = f.Ecrire()
            Return nouvelle <> ""
        End Using
    End Function

    Private Sub Construire(groupes As List(Of ReglagesMinuterie.GroupeReglage))
        Const hauteurLigne As Integer = 34
        Dim y As Integer = 6
        For Each g As ReglagesMinuterie.GroupeReglage In groupes
            Dim ligne As New LigneReglage With {.Groupe = g}
            ligne.Case_ = New CheckBox With {.Text = g.Libelle, .Left = 8, .Top = y + 4, .Width = 300, .AutoEllipsis = True}
            PanelListe.Controls.Add(ligne.Case_)
            If g.Valeurs.Count > 0 Then
                ligne.Valeur = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Left = 316, .Top = y, .Width = 262}
                For Each v As String In g.Valeurs
                    ligne.Valeur.Items.Add(v.Substring(g.Libelle.Length + 3))        ' (la valeur seule : apres " : ")
                Next
                ligne.Valeur.SelectedIndex = 0
                PanelListe.Controls.Add(ligne.Valeur)
                AddHandler ligne.Valeur.SelectedIndexChanged, AddressOf Mettre_A_Jour
                AddHandler ligne.Case_.CheckedChanged, Sub(s, e)
                                                           ligne.Valeur.Enabled = ligne.Case_.Checked
                                                           Mettre_A_Jour(s, e)
                                                       End Sub
                ligne.Valeur.Enabled = False
            Else
                AddHandler ligne.Case_.CheckedChanged, AddressOf Mettre_A_Jour
            End If
            _lignes.Add(ligne)
            y += hauteurLigne
        Next
        ' Les reglages deja ecrits dans la ligne sont coches, avec leur valeur.
        For Each morceau As String In _actuelle.Split(";"c)
            Dim m As String = morceau.Trim()
            If m = "" Then Continue For
            For Each ligne As LigneReglage In _lignes
                If ligne.Groupe.Valeurs.Count = 0 Then
                    If String.Equals(ligne.Groupe.Libelle, m, StringComparison.OrdinalIgnoreCase) Then ligne.Case_.Checked = True : Exit For
                Else
                    Dim rang As Integer = ligne.Groupe.Valeurs.FindIndex(Function(v) String.Equals(v, m, StringComparison.OrdinalIgnoreCase))
                    If rang >= 0 Then
                        ligne.Case_.Checked = True
                        ligne.Valeur.SelectedIndex = rang
                        Exit For
                    End If
                End If
            Next
        Next
    End Sub

    Private Function Ecrire() As String
        Dim morceaux As New List(Of String)
        For Each ligne As LigneReglage In _lignes
            If Not ligne.Case_.Checked Then Continue For
            If ligne.Groupe.Valeurs.Count = 0 Then
                morceaux.Add(ligne.Groupe.Libelle)
            ElseIf ligne.Valeur.SelectedIndex >= 0 Then
                morceaux.Add(ligne.Groupe.Valeurs(ligne.Valeur.SelectedIndex))
            End If
        Next
        Return String.Join(FormSon.Separateur, morceaux)
    End Function

    Private Sub Mettre_A_Jour(sender As Object, e As EventArgs)
        If _chargement Then Exit Sub
        Dim apercu As String = Ecrire()
        LabelResume.Text = If(apercu = "", "Rien pour l'instant : cochez au moins un réglage.", apercu.Replace(FormSon.Separateur, "  •  "))
    End Sub

    Private Sub FormReglages_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        _chargement = False
        Mettre_A_Jour(Nothing, EventArgs.Empty)
    End Sub

    Private Sub FormReglages_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult <> DialogResult.OK Then Exit Sub
        If Ecrire() = "" Then
            MessageBox.Show(Me, "Rien n'est à changer : cochez au moins un réglage.", "Réglages de la tâche", MessageBoxButtons.OK, MessageBoxIcon.Information)
            e.Cancel = True
        End If
    End Sub

End Class
