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
        ''' <summary>Bouton « Option » d'un appareil Tuya (premiere ligne de l'appareil seulement) et l'appareil qu'il regle.</summary>
        Friend BoutonOptions As Button
        Friend Appareil As TuyaSlide.AppareilChoisi
        ''' <summary>Les reglages d'option choisis pour l'appareil, ecrits dans la tache : « Nom ⚙ Luminosité 50 % {code=valeur} ».</summary>
        Friend Options As New List(Of String)
    End Class

    Private ReadOnly _lignes As New List(Of LigneReglage)
    Private _actuelle As String = ""
    Private _chargement As Boolean = True

    ''' <summary>Montre la fenetre pour le type de tache <paramref name="type"/>. <paramref name="statiques"/> : les actions simples deja proposees par le type.
    ''' Rend Vrai avec la nouvelle action si on valide.</summary>
    ' <summary>Les options (« Nom ⚙ Luminosité 50 % » -> « code=valeur ») retenues par le dernier OK : a ranger dans les reglages caches de la ligne (TuyaSlide.ArgumentAvecOptions).</summary>
    Friend Shared OptionsRetenues As Dictionary(Of String, String) = New Dictionary(Of String, String)

    Private _options As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)          ' texte montre -> code=valeur, de toutes les options connues de la fenetre

    Friend Shared Function Demander(proprietaire As IWin32Window, type As String, statiques As IEnumerable(Of String), actuelle As String, ByRef nouvelle As String, Optional argumentAvant As String = "") As Boolean
        Using f As New FormReglages()
            f.Text = "Réglages de la tâche (" & type & ")"
            f._actuelle = If(actuelle, "")
            For Each kv As KeyValuePair(Of String, String) In TuyaSlide.OptionsDeLArgument(argumentAvant)
                f._options(kv.Key) = kv.Value
            Next
            f.Construire(ReglagesMinuterie.Groupes(type, statiques))
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            nouvelle = f.Ecrire()
            OptionsRetenues = New Dictionary(Of String, String)
            For Each morceau As String In nouvelle.Split(";"c)
                Dim texte As String = morceau.Trim()
                Dim brut As String = Nothing
                If texte <> "" AndAlso f._options.TryGetValue(texte, brut) Then OptionsRetenues(texte) = brut
            Next
            Return nouvelle <> ""
        End Using
    End Function

    Private _groupes As List(Of ReglagesMinuterie.GroupeReglage)

    ''' <summary>Cree les lignes (case, liste, boutons) ; leur DISPOSITION se fait a l'ouverture (<see cref="Disposer"/>), quand la police finale est connue.</summary>
    Private Sub Construire(groupes As List(Of ReglagesMinuterie.GroupeReglage))
        _groupes = groupes
        For Each g As ReglagesMinuterie.GroupeReglage In groupes
            Dim ligne As New LigneReglage With {.Groupe = g}
            ligne.Case_ = New CheckBox With {.Text = g.Libelle}
            PanelListe.Controls.Add(ligne.Case_)
            If g.Valeurs.Count > 0 Then
                ligne.Valeur = New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList}
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
                    ' Bouton « Option » (04/11/2026) : les reglages de l'appareil (luminosite, mode, couleur, 2e commutateur...) dans la tache ; un seul par appareil.
                    Dim appareil As TuyaSlide.AppareilChoisi = AppareilDuGroupe(g.Libelle)
                    If appareil IsNot Nothing AndAlso Not _appareilsAvecBouton.Contains(appareil.Id) Then
                        _appareilsAvecBouton.Add(appareil.Id)
                        ligne.Appareil = appareil
                        ligne.BoutonOptions = New Button With {.Text = "Option", .FlatStyle = FlatStyle.Flat, .Cursor = Cursors.Hand}
                        ligne.BoutonOptions.FlatAppearance.BorderSize = 1
                        PanelListe.Controls.Add(ligne.BoutonOptions)
                        Dim ceBouton As LigneReglage = ligne
                        AddHandler ligne.BoutonOptions.Click, Sub(s, e) ChoisirLesOptions(ceBouton)
                        AddHandler ligne.BoutonOptions.MouseUp, Sub(s, e) If e.Button = MouseButtons.Right Then EffacerLesOptions(ceBouton)
                    End If
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
            ' Un reglage d'option (« Salon ⚙ Luminosité 50 % {...} ») : range sous le bouton Option de son appareil.
            Dim appOption As String = "", libOption As String = "", codeOption As String = "", valOption As String = ""
            If TuyaSlide.EstUneOption(m, appOption) Then
                ' Ancienne forme (« ... {code=valeur} ») : le texte montre est gardé sans les accolades, le code et la valeur passent dans les reglages caches.
                If TuyaSlide.LireCommandeOption(m, appOption, libOption, codeOption, valOption) Then
                    m = appOption & " ⚙ " & libOption
                    _options(m) = codeOption & "=" & valOption
                End If
                Dim porteuse As LigneReglage = _lignes.FirstOrDefault(Function(l) l.BoutonOptions IsNot Nothing AndAlso String.Equals(l.Appareil.Nom, appOption, StringComparison.OrdinalIgnoreCase))
                If porteuse IsNot Nothing Then porteuse.Options.Add(m) : MettreLeBoutonOptions(porteuse)
                Continue For
            End If
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

    End Sub

    ''' <summary>Dispose les lignes d'apres la police REELLE de la fenetre (06/11/2026, autre PC : les lignes, calees sur 34 pixels, se chevauchaient et coupaient les textes
    ''' quand la police est plus grande). Appelee a l'ouverture, apres le theme : hauteur de ligne, largeur des cases et des boutons viennent de la police.</summary>
    Private Sub Disposer()
        Dim zone As Rectangle = Screen.FromControl(Me).WorkingArea
        Dim police As Font = Me.Font
        Dim hauteurTexte As Integer = TextRenderer.MeasureText("Ag", police).Height
        Dim hauteurLigne As Integer = Math.Max(34, hauteurTexte + 16)
        Dim echelle As Double = hauteurLigne / 34.0
        Dim largeurListe As Integer = CInt(262 * echelle)
        Dim largeurOn As Integer = Math.Max(CInt(60 * echelle), TextRenderer.MeasureText("OFF", police).Width + 30)
        Dim largeurOption As Integer = 0
        For Each l As LigneReglage In _lignes
            If l.BoutonOptions IsNot Nothing Then
                largeurOption = Math.Max(largeurOption, TextRenderer.MeasureText("Option (99)", police).Width + 26)
            End If
        Next
        largeurOption = Math.Max(largeurOption, CInt(90 * echelle))
        Dim largeurBoutons As Integer = 2 * largeurOn + 12 + largeurOption + 8          ' (ON, OFF et Option)

        ' Le texte de chaque case est montre EN ENTIER : la case prend la largeur du plus long libelle (dans la limite de 45 % de l'ecran).
        Dim largeurCase As Integer = CInt(200 * echelle)
        For Each g As ReglagesMinuterie.GroupeReglage In _groupes
            largeurCase = Math.Max(largeurCase, CInt(TextRenderer.MeasureText(g.Libelle, police).Width * 1.1) + CInt(SystemInformation.MenuCheckSize.Width * 1.5) + 40)
        Next
        largeurCase = Math.Min(largeurCase, CInt(zone.Width * 0.45))

        Dim largeurValeur As Integer = 0
        For Each g As ReglagesMinuterie.GroupeReglage In _groupes
            If g.EstOnOff Then
                largeurValeur = Math.Max(largeurValeur, largeurBoutons)
            ElseIf g.Valeurs.Count > 0 Then
                largeurValeur = Math.Max(largeurValeur, largeurListe)
            End If
        Next
        Dim largeurColonne As Integer = 8 + largeurCase + If(largeurValeur > 0, 8 + largeurValeur, 0) + 20

        For Each ligne As LigneReglage In _lignes
            ligne.Case_.AutoSize = False
            ligne.Case_.Width = largeurCase
            If ligne.Valeur IsNot Nothing Then ligne.Valeur.Width = largeurListe
            If ligne.BoutonOn IsNot Nothing Then
                ligne.BoutonOn.Size = New Size(largeurOn, hauteurLigne - 4)
                ligne.BoutonOff.Size = New Size(largeurOn, hauteurLigne - 4)
            End If
            If ligne.BoutonOptions IsNot Nothing Then ligne.BoutonOptions.Size = New Size(largeurOption, hauteurLigne - 4)
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
            If ligne.Valeur IsNot Nothing Then ligne.Valeur.Location = New Point(x + largeurCase + 8, y + Math.Max(0, (hauteurLigne - 4 - ligne.Valeur.Height) \ 2))
            If ligne.BoutonOn IsNot Nothing Then
                ligne.BoutonOn.Location = New Point(x + largeurCase + 8, y + 1)
                ligne.BoutonOff.Location = New Point(x + largeurCase + 8 + largeurOn + 4, y + 1)
                If ligne.BoutonOptions IsNot Nothing Then ligne.BoutonOptions.Location = New Point(x + largeurCase + 8 + 2 * largeurOn + 12, y + 1)
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
        CenterToScreen()
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

    Private ReadOnly _appareilsAvecBouton As New HashSet(Of String)

    ''' <summary>L'appareil Tuya d'un groupe de la liste (« Salon » ou « Salon - Commutateur 2 ») ; Nothing si ce n'est pas un appareil (scene, groupe...).</summary>
    Private Shared Function AppareilDuGroupe(libelle As String) As TuyaSlide.AppareilChoisi
        Try
            Return TuyaSlide.AppareilsActuels().Where(Function(a) a.Id <> "" AndAlso a.Code <> "" AndAlso Not TuyaSlide.EstUneScene(a.Code) AndAlso
                                                      (String.Equals(a.Nom, libelle, StringComparison.OrdinalIgnoreCase) OrElse libelle.StartsWith(a.Nom & " - ", StringComparison.OrdinalIgnoreCase))).
                                                  OrderByDescending(Function(a) a.Nom.Length).FirstOrDefault()
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    Private Shared Sub MettreLeBoutonOptions(ligne As LigneReglage)
        If ligne.BoutonOptions Is Nothing Then Exit Sub
        ligne.BoutonOptions.Text = If(ligne.Options.Count > 0, "Option (" & ligne.Options.Count & ")", "Option")
        ligne.BoutonOptions.BackColor = If(ligne.Options.Count > 0, Color.FromArgb(46, 110, 170), SystemColors.Control)
        ligne.BoutonOptions.ForeColor = If(ligne.Options.Count > 0, Color.White, SystemColors.ControlText)
    End Sub

    ''' <summary>Clic droit sur « Option » : retire les reglages d'option choisis pour cet appareil.</summary>
    Private Sub EffacerLesOptions(ligne As LigneReglage)
        If ligne.Options.Count = 0 Then Exit Sub
        ligne.Options.Clear()
        MettreLeBoutonOptions(ligne)
        Mettre_A_Jour(Nothing, EventArgs.Empty)
    End Sub

    ''' <summary>Clic sur « Option » : les reglages de l'appareil sont lus chez Tuya, on choisit lesquels la tache applique (rien n'est envoye maintenant).</summary>
    Private Async Sub ChoisirLesOptions(ligne As LigneReglage)
        If ligne.Appareil Is Nothing Then Exit Sub
        Dim options As List(Of TuyaSlide.OptionTuya) = Nothing
        Dim comptes As List(Of TuyaSlide.Compte) = TuyaSlide.ComptesActuels()
        Try
            Cursor = Cursors.WaitCursor
            ligne.BoutonOptions.Enabled = False
            Dim compte As TuyaSlide.Compte = comptes.FirstOrDefault(Function(c) String.Equals(c.Nom, ligne.Appareil.CompteNom, StringComparison.OrdinalIgnoreCase))
            If compte Is Nothing Then compte = comptes.FirstOrDefault()
            If compte Is Nothing OrElse Not compte.Renseigne Then
                MessageBox.Show(Me, "Compte Tuya non renseigné (onglet Sources, cadre Tuya).", "Option", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            options = Await TuyaSlide.OptionsAsync(compte, ligne.Appareil.Id, ligne.Appareil.Code)
        Catch ex As Exception
            DiagLog("Tuya : options de « " & ligne.Appareil.Nom & " » (minuterie) illisibles : " & ex.Message)
            MessageBox.Show(Me, "Options de « " & ligne.Appareil.Nom & " » illisibles : " & ex.Message, "Option", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Exit Sub
        Finally
            Cursor = Cursors.Default
            If Not IsDisposed Then ligne.BoutonOptions.Enabled = True
        End Try
        If IsDisposed OrElse options Is Nothing Then Exit Sub
        Dim choix As List(Of (Code As String, Libelle As String, Valeur As String)) = Nothing
        ' (la fenetre recoit les choix de la tache avec leur code et leur valeur : « Nom ⚙ Luminosité 50 % {code=valeur} »)
        Dim dejaChoisis As List(Of String) = ligne.Options.Where(Function(p) _options.ContainsKey(p)).Select(Function(p) p & " {" & _options(p) & "}").ToList()
        If Not FormOptionsTuya.Choisir(Me, ligne.Appareil, comptes, options, dejaChoisis, choix) Then Exit Sub
        ' La fenetre montre toutes les options avec leur case « Dans la tâche » : ce qu'elle rend REMPLACE les choix precedents (une case decochee retire l'option de la tache).
        For Each ancienne As String In ligne.Options
            _options.Remove(ancienne)
        Next
        ligne.Options.Clear()
        For Each c In choix
            Dim texte As String = ligne.Appareil.Nom & " ⚙ " & c.Libelle          ' (lisible : le code Tuya et la valeur envoyee restent dans les reglages caches de la ligne)
            ligne.Options.Add(texte)
            _options(texte) = c.Code & "=" & c.Valeur
        Next
        MettreLeBoutonOptions(ligne)
        Mettre_A_Jour(Nothing, EventArgs.Empty)
    End Sub

    Private Function Ecrire() As String
        Dim morceaux As New List(Of String)
        For Each ligne As LigneReglage In _lignes
            If ligne.Options.Count > 0 Then morceaux.AddRange(ligne.Options)
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
        Disposer()
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
