''' <summary>
''' Fenetre de choix multiples d'une tache de la minuterie (20/10/2026 : "oui") pour les types Photo Mode, Video Mode, Autres et Tuya : on coche les
''' reglages que la tache change et on choisit leur valeur. La case Action de la ligne recoit le resume, par exemple
''' "Animer les photos : Oui ; Durée d'affichage d'une photo : 10 s" (morceaux separes par " ; "). La liste des reglages vient de
''' ReglagesMinuterie ; les lignes (case + liste de valeurs) sont creees a l'ouverture, d'apres cette liste.
'''
''' 30/10/2026 : les lignes se rangent en COLONNES (plus de defilement tant que la liste tient dans l'ecran), la fenetre grandit pour les montrer, et une paire « X ON / X OFF » d'un
''' appareil Tuya est une ligne avec deux boutons ON et OFF (un clic ; reclic = decocher). Les listes de valeurs ne sont plus grisees tant que la case n'est pas cochee : choisir une valeur coche la case.
''' </summary>
Public Class FormReglages

    Private Class LigneReglage
        Friend Groupe As ReglagesMinuterie.GroupeReglage
        Friend Case_ As CheckBox
        Friend Valeur As ComboBox          ' (paire ON / OFF : cache, il garde seulement le choix)
        Friend BoutonOn As CheckBox
        Friend BoutonOff As CheckBox
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
        Const largeurListe As Integer = 262
        Const largeurBoutons As Integer = 128
        Dim zone As Rectangle = Screen.FromControl(Me).WorkingArea

        ' Le texte de chaque case est montre EN ENTIER (26/10/2026) : la case prend la largeur du plus long libelle (dans la limite d'un tiers de l'ecran).
        Dim largeurCase As Integer = 200
        For Each g As ReglagesMinuterie.GroupeReglage In groupes
            ' (x 1,4 : le theme applique a l'ouverture (StylePro) agrandit la police, le texte mesure ici avant serait tronque ; 30/10/2026)
            largeurCase = Math.Max(largeurCase, CInt(TextRenderer.MeasureText(g.Libelle, Me.Font).Width * 1.4) + CInt(SystemInformation.MenuCheckSize.Width * 1.5) + 40)
        Next
        largeurCase = Math.Min(largeurCase, CInt(zone.Width * 0.45))

        Dim largeurValeur As Integer = 0
        For Each g As ReglagesMinuterie.GroupeReglage In groupes
            If g.EstOnOff Then
                largeurValeur = Math.Max(largeurValeur, largeurBoutons)
            ElseIf g.Valeurs.Count > 0 Then
                largeurValeur = Math.Max(largeurValeur, largeurListe)
            End If
        Next
        Dim largeurColonne As Integer = 8 + largeurCase + If(largeurValeur > 0, 8 + largeurValeur, 0) + 20

        For Each g As ReglagesMinuterie.GroupeReglage In groupes
            Dim ligne As New LigneReglage With {.Groupe = g}
            ligne.Case_ = New CheckBox With {.Text = g.Libelle, .Width = largeurCase}
            PanelListe.Controls.Add(ligne.Case_)
            If g.Valeurs.Count > 0 Then
                ligne.Valeur = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Width = largeurListe}
                For Each v As String In g.Valeurs
                    ligne.Valeur.Items.Add(v.Substring(g.Libelle.Length).TrimStart(" "c, ":"c))        ' (la valeur seule : apres " : ", ou apres l'espace de « Applique Salon ON »)
                Next
                ligne.Valeur.SelectedIndex = 0
                PanelListe.Controls.Add(ligne.Valeur)
                Dim cetteLigne As LigneReglage = ligne
                If g.EstOnOff Then
                    ligne.Valeur.Visible = False
                    ligne.BoutonOn = NouveauBouton("ON", Color.FromArgb(46, 140, 70))
                    ligne.BoutonOff = NouveauBouton("OFF", Color.FromArgb(190, 70, 60))
                    PanelListe.Controls.Add(ligne.BoutonOn)
                    PanelListe.Controls.Add(ligne.BoutonOff)
                    AddHandler ligne.BoutonOn.Click, Sub(s, e) ChoisirOnOff(cetteLigne, 0)
                    AddHandler ligne.BoutonOff.Click, Sub(s, e) ChoisirOnOff(cetteLigne, 1)
                    AddHandler ligne.Case_.CheckedChanged, Sub(s, e)
                                                               MettreLesBoutons(cetteLigne)
                                                               Mettre_A_Jour(s, e)
                                                           End Sub
                Else
                    ' Choisir une valeur coche la case (la liste n'est plus grisee tant qu'elle n'est pas cochee).
                    AddHandler ligne.Valeur.SelectedIndexChanged, Sub(s, e)
                                                                      If _chargement Then Exit Sub
                                                                      cetteLigne.Case_.Checked = True
                                                                      Mettre_A_Jour(s, e)
                                                                  End Sub
                    AddHandler ligne.Case_.CheckedChanged, AddressOf Mettre_A_Jour
                End If
            Else
                AddHandler ligne.Case_.CheckedChanged, AddressOf Mettre_A_Jour
            End If
            _lignes.Add(ligne)
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
                        ligne.Valeur.SelectedIndex = rang
                        ligne.Case_.Checked = True
                        Exit For
                    End If
                End If
            Next
        Next

        ' Rangement en colonnes : aussi peu de colonnes que la hauteur de l'ecran le permet, la fenetre grandit pour tout montrer.
        Dim fixe As Integer = Me.ClientSize.Height - PanelListe.Height          ' (aide, resume et boutons)
        Dim hauteurMax As Integer = Math.Max(300, CInt(zone.Height * 0.88) - 60 - fixe)
        Dim lignesMax As Integer = Math.Max(8, (hauteurMax - 12) \ hauteurLigne)
        Dim n As Integer = Math.Max(1, _lignes.Count)
        Dim colonnesMax As Integer = Math.Max(1, (CInt(zone.Width * 0.95) - 50) \ largeurColonne)
        Dim colonnes As Integer = Math.Min(colonnesMax, Math.Max(1, CInt(Math.Ceiling(n / lignesMax))))
        Dim parColonne As Integer = CInt(Math.Ceiling(n / colonnes))
        For i As Integer = 0 To _lignes.Count - 1
            Dim ligne As LigneReglage = _lignes(i)
            Dim x As Integer = (i \ parColonne) * largeurColonne + 8
            Dim y As Integer = 6 + (i Mod parColonne) * hauteurLigne
            ligne.Case_.AutoSize = False
            ligne.Case_.Height = hauteurLigne - 6          ' (assez haut pour le theme : les queues des lettres - g, p, y - etaient coupees en bas)
            ligne.Case_.TextAlign = ContentAlignment.MiddleLeft
            ligne.Case_.Location = New Point(x, y + 2)
            If ligne.Valeur IsNot Nothing Then ligne.Valeur.Location = New Point(x + largeurCase + 8, y)
            If ligne.BoutonOn IsNot Nothing Then
                ligne.BoutonOn.Location = New Point(x + largeurCase + 8, y + 1)
                ligne.BoutonOff.Location = New Point(x + largeurCase + 8 + 64, y + 1)
                MettreLesBoutons(ligne)
            End If
        Next
        Dim largeurVoulue As Integer = colonnes * largeurColonne + 8
        If largeurVoulue > PanelListe.Width Then ElargirLaFenetre(largeurVoulue - PanelListe.Width)
        Dim hauteurVoulue As Integer = Math.Min(hauteurMax, Math.Max(PanelListe.Height, parColonne * hauteurLigne + 16))
        Dim dh As Integer = hauteurVoulue - PanelListe.Height
        If dh <> 0 Then
            Me.ClientSize = New Size(Me.ClientSize.Width, Me.ClientSize.Height + dh)
            PanelListe.Height += dh
            PanelResume.Top += dh
            ButtonOK.Top += dh
            ButtonAnnuler.Top += dh
        End If
        ' Le resume (« Ce que la tâche fera... ») peut etre long : une ligne de plus, sinon la fin etait coupee.
        Const supplement As Integer = 34
        Me.ClientSize = New Size(Me.ClientSize.Width, Me.ClientSize.Height + supplement)
        PanelResume.Height += supplement
        LabelResume.Height += supplement
        ButtonOK.Top += supplement
        ButtonAnnuler.Top += supplement
        Me.StartPosition = FormStartPosition.CenterScreen
    End Sub

    ''' <summary>Un bouton ON ou OFF : un clic le met, un autre clic le retire ; colore quand il est choisi.</summary>
    Private Shared Function NouveauBouton(texte As String, couleurChoisi As Color) As CheckBox
        Dim b As New CheckBox With {.Text = texte, .Appearance = Appearance.Button, .TextAlign = ContentAlignment.MiddleCenter, .Width = 60, .Height = 30, .FlatStyle = FlatStyle.Flat}
        b.FlatAppearance.CheckedBackColor = couleurChoisi
        b.FlatAppearance.BorderSize = 1
        Return b
    End Function

    ''' <summary>Clic sur ON (0) ou OFF (1) : choisit ce cote et coche la ligne ; un second clic sur le meme cote la decoche.</summary>
    Private Sub ChoisirOnOff(ligne As LigneReglage, rang As Integer)
        If ligne.Case_.Checked AndAlso ligne.Valeur.SelectedIndex = rang Then
            ligne.Case_.Checked = False
        Else
            ligne.Valeur.SelectedIndex = rang
            ligne.Case_.Checked = True
        End If
        MettreLesBoutons(ligne)
        Mettre_A_Jour(Nothing, EventArgs.Empty)
    End Sub

    Private Sub MettreLesBoutons(ligne As LigneReglage)
        If ligne.BoutonOn Is Nothing Then Exit Sub
        ligne.BoutonOn.Checked = ligne.Case_.Checked AndAlso ligne.Valeur.SelectedIndex = 0
        ligne.BoutonOff.Checked = ligne.Case_.Checked AndAlso ligne.Valeur.SelectedIndex = 1
    End Sub

    ''' <summary>Elargit la fenetre de <paramref name="delta"/> pixels : la liste, le resume et le texte d'aide prennent la largeur, les boutons suivent le bord droit.</summary>
    Private Sub ElargirLaFenetre(delta As Integer)
        Me.ClientSize = New Size(Me.ClientSize.Width + delta, Me.ClientSize.Height)
        PanelListe.Width += delta
        PanelResume.Width += delta
        LabelAide.Width += delta
        LabelResume.Width += delta
        ButtonOK.Left += delta
        ButtonAnnuler.Left += delta
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
        For Each ligne As LigneReglage In _lignes
            MettreLesBoutons(ligne)
        Next
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
