Option Strict On
Option Explicit On

''' <summary>
''' Options d'un appareil Tuya (Jacob, 26/10/2026 : "remplace la colonne interrupteur par les options possibles, comme la possibilite de changer l'intensite d'une lumiere avec un
''' curseur") : ouverte par le bouton « Options » du tableau des appareils (ControlTuya). Une ligne par option reglable de l'appareil : curseur (luminosite, temperature de couleur,
''' vitesse...), liste (mode...) ou case a cocher. Chaque reglage part tout de suite vers l'appareil. Les lignes sont creees d'apres ce que l'appareil sait faire (TuyaSlide.OptionsAsync) ;
''' la fenetre et ses boutons sont dans le Concepteur.
''' </summary>
Public Class FormOptionsTuya

    Private _appareil As TuyaSlide.AppareilChoisi
    Private _comptes As IList(Of TuyaSlide.Compte)
    Private _enChargement As Boolean

    ''' <summary>Montre la fenetre d'un appareil avec ses options deja lues.</summary>
    Friend Shared Sub Montrer(proprietaire As IWin32Window, appareil As TuyaSlide.AppareilChoisi, comptes As IList(Of TuyaSlide.Compte), options As List(Of TuyaSlide.OptionTuya))
        Using f As New FormOptionsTuya()
            f._appareil = appareil
            f._comptes = comptes
            f.LabelAppareil.Text = appareil.Nom
            f.Remplir(options)
            f.ShowDialog(proprietaire)
        End Using
    End Sub

    ''' <summary>Mode « choix pour une ligne de minuterie » (bouton Option des reglages d'une tache Tuya) : rien ne part vers l'appareil ; OK rend les reglages choisis
    ''' (code, libelle montre, valeur envoyee). Faux si on annule ou si rien n'est choisi.</summary>
    ''' <summary><paramref name="dejaChoisis"/> : les reglages deja choisis pour la tache (« Nom ⚙ Luminosité 50 % {code=valeur} ») : leurs options sont cochees et reglees sur CES valeurs
    ''' (et non sur celles de l'appareil). Chaque option a une case « Dans la tâche » : seules les options cochees sont rendues. Vrai si on valide (la liste peut etre vide : plus aucune option).</summary>
    Friend Shared Function Choisir(proprietaire As IWin32Window, appareil As TuyaSlide.AppareilChoisi, comptes As IList(Of TuyaSlide.Compte), options As List(Of TuyaSlide.OptionTuya),
                                   dejaChoisis As IEnumerable(Of String), ByRef choix As List(Of (Code As String, Libelle As String, Valeur As String))) As Boolean
        Using f As New FormOptionsTuya()
            f._appareil = appareil
            f._comptes = comptes
            f._modeChoix = True
            f.LabelAppareil.Text = appareil.Nom
            f.CheckBoxInstantane.Checked = False
            f.CheckBoxInstantane.Visible = False
            For Each piece As String In If(dejaChoisis, Enumerable.Empty(Of String)())
                Dim a As String = "", l As String = "", k As String = "", v As String = ""
                If Not TuyaSlide.LireCommandeOption(piece, a, l, k, v) Then Continue For
                f._valeursChoisies(k) = (l, v)
                Dim o As TuyaSlide.OptionTuya = options.FirstOrDefault(Function(x) String.Equals(x.Code, k, StringComparison.OrdinalIgnoreCase))
                If o IsNot Nothing Then o.Actuelle = v          ' (le curseur, la liste... montrent ce que la tache applique)
            Next
            f.Remplir(options)
            f.LabelEtat.Text = "Cochez « Dans la tâche » pour chaque réglage que la tâche applique, puis OK (rien n'est envoyé maintenant)."
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            choix = New List(Of (Code As String, Libelle As String, Valeur As String))
            For Each o As TuyaSlide.OptionTuya In options
                Dim caseOption As CheckBox = Nothing
                If Not f._casesChoix.TryGetValue(o.Code, caseOption) OrElse Not caseOption.Checked Then Continue For
                Dim choisi As (Libelle As String, Valeur As String) = Nothing
                If Not f._valeursChoisies.TryGetValue(o.Code, choisi) Then
                    choisi = ValeurActuelle(o)          ' (case cochee sans toucher au reglage : la valeur que l'appareil a maintenant)
                    If choisi.Valeur Is Nothing Then Continue For
                End If
                choix.Add((o.Code, choisi.Libelle, choisi.Valeur))
            Next
            ' Une couleur : l'appareil doit etre en mode « colour » (comme dans la fenetre des options).
            If choix.Any(Function(c) c.Code.StartsWith("colour_data", StringComparison.OrdinalIgnoreCase)) AndAlso Not choix.Any(Function(c) c.Code = "work_mode") Then
                Dim mode As TuyaSlide.OptionTuya = options.FirstOrDefault(Function(x) x.Code = "work_mode" AndAlso x.Valeurs.Contains("colour"))
                If mode IsNot Nothing Then choix.Insert(0, (mode.Code, mode.Nom & " : colour", "colour"))
            End If
            Return True
        End Using
    End Function

    ''' <summary>La valeur actuelle d'une option, comme libelle et valeur envoyee (Nothing dans Valeur si l'appareil ne la donne pas).</summary>
    Private Shared Function ValeurActuelle(o As TuyaSlide.OptionTuya) As (Libelle As String, Valeur As String)
        If String.IsNullOrEmpty(o.Actuelle) Then Return (o.Nom, Nothing)
        Select Case o.Type
            Case "Integer"
                Dim x As Double
                If Not Double.TryParse(o.Actuelle, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, x) Then Return (o.Nom, Nothing)
                Dim pourcent As Integer = If(o.Max > o.Min, CInt(Math.Round((x - o.Min) / (o.Max - o.Min) * 100)), 0)
                Return (o.Nom & " " & pourcent & " %", o.Actuelle)
            Case "Enum" : Return (o.Nom & " : " & o.Actuelle, o.Actuelle)
            Case "Color" : Return (o.Nom, o.Actuelle)
            Case Else : Return (o.Nom & If(String.Equals(o.Actuelle, "true", StringComparison.OrdinalIgnoreCase), " activé", " désactivé"), o.Actuelle.ToLowerInvariant())
        End Select
    End Function

    Private _modeChoix As Boolean
    Private ReadOnly _casesChoix As New Dictionary(Of String, CheckBox)(StringComparer.OrdinalIgnoreCase)
    Private ReadOnly _valeursChoisies As New Dictionary(Of String, (Libelle As String, Valeur As String))(StringComparer.OrdinalIgnoreCase)

    Private Sub FormOptionsTuya_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

    Private _options As New List(Of TuyaSlide.OptionTuya)

    Private Sub Remplir(options As List(Of TuyaSlide.OptionTuya))
        _options = options
        _enChargement = True
        Try
            PanelOptions.SuspendLayout()
            PanelOptions.Controls.Clear()
            If options.Count = 0 Then
                LabelEtat.Text = "Cet appareil n'a pas d'option réglable en dehors d'ON / OFF."
                Exit Sub
            End If
            For Each o As TuyaSlide.OptionTuya In options
                PanelOptions.Controls.Add(UneLigne(o))
            Next
            LabelEtat.Text = "Réglez puis cliquez sur OK (ou cochez « instantanément »)."
        Finally
            PanelOptions.ResumeLayout()
            _enChargement = False
        End Try
    End Sub

    ''' <summary>Une ligne : nom (et valeur) puis le curseur, la liste ou la case.</summary>
    Private Function UneLigne(o As TuyaSlide.OptionTuya) As Control
        Dim largeur As Integer = PanelOptions.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 8
        Dim ligne As New Panel With {.Width = largeur, .Height = 64, .Margin = New Padding(0, 0, 0, 6)}
        Dim titre As New Label With {.AutoSize = True, .Location = New Point(0, 0), .Text = o.Nom}
        ligne.Controls.Add(titre)
        Select Case o.Type
            Case "Integer"
                Dim valeur As Label = New Label With {.AutoSize = True, .Location = New Point(largeur - 70, 0), .Anchor = AnchorStyles.Top Or AnchorStyles.Right}
                Dim curseur As New TrackBar With {.Minimum = 0, .Maximum = 100, .TickFrequency = 10, .Location = New Point(0, 24), .Width = largeur - 4, .Height = 38}
                Dim actuelle As Double
                Dim aUneValeur As Boolean = Double.TryParse(o.Actuelle, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, actuelle)
                curseur.Value = If(aUneValeur, Math.Max(0, Math.Min(100, CInt(Math.Round((actuelle - o.Min) / (o.Max - o.Min) * 100)))), 50)
                valeur.Text = curseur.Value & " %"
                ligne.Controls.Add(valeur)
                ligne.Controls.Add(curseur)
                ' La molette fait defiler la liste d'options, elle ne bouge plus un curseur sans le vouloir (04/11/2026 : des options jamais touchees se retrouvaient dans la tache).
                AddHandler curseur.MouseWheel, Sub(s, e) CType(e, HandledMouseEventArgs).Handled = True
                ' Le reglage part 300 ms apres le dernier mouvement du curseur (pas un envoi a chaque cran) : plus rapide a l'oeil, et sans saturer le service Tuya.
                Dim attente As New System.Windows.Forms.Timer With {.Interval = 300}
                AddHandler attente.Tick, Sub()
                                             attente.Stop()
                                             Dim brut As Double = o.Min + curseur.Value / 100.0 * (o.Max - o.Min)
                                             brut = o.Min + Math.Round((brut - o.Min) / o.Pas) * o.Pas                   ' (un multiple du pas de l'appareil)
                                             Demander(o, CLng(Math.Round(brut)).ToString(Globalization.CultureInfo.InvariantCulture), o.Nom & " " & curseur.Value & " %")
                                         End Sub
                AddHandler curseur.Scroll, Sub()
                                               valeur.Text = curseur.Value & " %"
                                               If Not _enChargement Then
                                                   attente.Stop()
                                                   attente.Start()
                                               End If
                                           End Sub
                AddHandler ligne.Disposed, Sub() attente.Dispose()
            Case "Enum"
                Dim liste As New ComboBox With {.DropDownStyle = ComboBoxStyle.DropDownList, .Location = New Point(0, 26), .Width = Math.Min(300, largeur)}
                For Each v As String In o.Valeurs
                    liste.Items.Add(v)
                Next
                Dim k As Integer = liste.Items.IndexOf(o.Actuelle)
                If k >= 0 Then liste.SelectedIndex = k
                AddHandler liste.SelectedIndexChanged, Sub()
                                                           If Not _enChargement AndAlso liste.SelectedItem IsNot Nothing Then Demander(o, Convert.ToString(liste.SelectedItem), o.Nom & " : " & Convert.ToString(liste.SelectedItem))
                                                       End Sub
                ligne.Controls.Add(liste)
                AddHandler liste.MouseWheel, Sub(s, e) CType(e, HandledMouseEventArgs).Handled = True          ' (la molette ne change pas le choix)
            Case "Color"
                ' Couleur d'une lumiere : echantillon de la couleur actuelle et bouton qui ouvre le selecteur de couleurs de Windows. La couleur part en teinte / saturation / valeur,
                ' dans les echelles de l'appareil ; le mode de la lumiere passe sur « colour » d'abord.
                Dim echantillon As New Panel With {.Location = New Point(0, 26), .Size = New Size(60, 30), .BorderStyle = BorderStyle.FixedSingle, .BackColor = CouleurDeLAppareil(o)}
                Dim bouton As New Button With {.Text = "Choisir la couleur...", .Location = New Point(72, 22), .Size = New Size(240, 38)}
                AddHandler bouton.Click, Sub()
                                             Using d As New ColorDialog With {.Color = echantillon.BackColor, .FullOpen = True}
                                                 If d.ShowDialog(Me) <> DialogResult.OK Then Exit Sub
                                                 echantillon.BackColor = d.Color
                                                 Dim teinte As Double = d.Color.GetHue()
                                                 Dim maxi As Double = Math.Max(d.Color.R, Math.Max(d.Color.G, d.Color.B)) / 255.0
                                                 Dim mini As Double = Math.Min(d.Color.R, Math.Min(d.Color.G, d.Color.B)) / 255.0
                                                 Dim saturation As Double = If(maxi <= 0, 0, (maxi - mini) / maxi)
                                                 Dim json As String = String.Format(Globalization.CultureInfo.InvariantCulture, "{{""h"":{0},""s"":{1},""v"":{2}}}",
                                                                                    CInt(Math.Round(teinte / 360.0 * o.MaxH)), CInt(Math.Round(saturation * o.MaxS)), CInt(Math.Round(maxi * o.MaxV)))
                                                 DemanderCouleur(o, json, o.Nom & " " & ColorTranslator.ToHtml(d.Color))
                                             End Using
                                         End Sub
                ligne.Controls.Add(echantillon)
                ligne.Controls.Add(bouton)
            Case Else                                                   ' Boolean
                Dim case_ As New CheckBox With {.AutoSize = True, .Location = New Point(0, 26), .Text = "Activé", .Checked = String.Equals(o.Actuelle, "true", StringComparison.OrdinalIgnoreCase)}
                AddHandler case_.CheckedChanged, Sub()
                                                     If Not _enChargement Then Demander(o, If(case_.Checked, "true", "false"), o.Nom & If(case_.Checked, " activé", " désactivé"))
                                                 End Sub
                ligne.Controls.Add(case_)
        End Select
        If _modeChoix Then
            ' « Dans la tâche » : cette option fait partie de la tache (cochee d'avance si elle l'etait deja ; toucher au reglage la coche).
            Dim dansLaTache As New CheckBox With {.AutoSize = True, .Text = "Dans la tâche", .Anchor = AnchorStyles.Top Or AnchorStyles.Right, .Checked = _valeursChoisies.ContainsKey(o.Code)}
            dansLaTache.Location = New Point(Math.Max(120, largeur - 400), 0)
            ligne.Controls.Add(dansLaTache)
            _casesChoix(o.Code) = dansLaTache
        End If
        Return ligne
    End Function

    ''' <summary>La couleur actuelle de la lumiere ({"h":..,"s":..,"v":..} dans les echelles de l'appareil), sinon gris.</summary>
    Private Function CouleurDeLAppareil(o As TuyaSlide.OptionTuya) As Color
        Try
            Using doc As System.Text.Json.JsonDocument = System.Text.Json.JsonDocument.Parse(If(o.Actuelle = "", "{}", o.Actuelle))
                Dim r As System.Text.Json.JsonElement = doc.RootElement
                Dim h As System.Text.Json.JsonElement, s As System.Text.Json.JsonElement, v As System.Text.Json.JsonElement
                If r.ValueKind = System.Text.Json.JsonValueKind.Object AndAlso r.TryGetProperty("h", h) AndAlso r.TryGetProperty("s", s) AndAlso r.TryGetProperty("v", v) Then
                    Dim teinte As Double = h.GetDouble() / o.MaxH * 360.0
                    Dim saturation As Double = s.GetDouble() / o.MaxS
                    Dim valeur As Double = v.GetDouble() / o.MaxV
                    Dim c As Double = valeur * saturation
                    Dim x As Double = c * (1 - Math.Abs((teinte / 60.0) Mod 2 - 1))
                    Dim m As Double = valeur - c
                    Dim r1 As Double, g1 As Double, b1 As Double
                    Select Case CInt(Math.Floor(teinte / 60.0)) Mod 6
                        Case 0 : r1 = c : g1 = x : b1 = 0
                        Case 1 : r1 = x : g1 = c : b1 = 0
                        Case 2 : r1 = 0 : g1 = c : b1 = x
                        Case 3 : r1 = 0 : g1 = x : b1 = c
                        Case 4 : r1 = x : g1 = 0 : b1 = c
                        Case Else : r1 = c : g1 = 0 : b1 = x
                    End Select
                    Return Color.FromArgb(CInt(Math.Round((r1 + m) * 255)), CInt(Math.Round((g1 + m) * 255)), CInt(Math.Round((b1 + m) * 255)))
                End If
            End Using
        Catch ex As Exception
        End Try
        Return Color.Gray
    End Function

    ''' <summary>Une couleur est choisie : le mode de la lumiere passe sur « colour » (s'il existe) avant que la couleur ne parte.</summary>
    Private Async Sub DemanderCouleur(o As TuyaSlide.OptionTuya, json As String, libelle As String)
        Dim mode As TuyaSlide.OptionTuya = _options.FirstOrDefault(Function(x) x.Code = "work_mode" AndAlso x.Valeurs.Contains("colour"))
        If mode IsNot Nothing AndAlso Not _modeChoix Then          ' (choix pour une tache : le mode « colour » est ajoute a la validation)
            If CheckBoxInstantane.Checked Then
                Await Envoyer(mode, "colour", mode.Nom & " : colour")
            Else
                Demander(mode, "colour", mode.Nom & " : colour")
            End If
        End If
        Demander(o, json, libelle)
    End Sub

    ' Reglages en attente (case « instantanement » decochee, par defaut) : le dernier de chaque option part avec OK ; Fermer les abandonne.
    Private ReadOnly _enAttente As New List(Of EnAttente)

    Private Class EnAttente
        Friend Opt As TuyaSlide.OptionTuya
        Friend Valeur As String
        Friend Libelle As String
    End Class

    ''' <summary>Un reglage vient d'etre change : il part tout de suite si la case est cochee, sinon il attend OK.</summary>
    Private Sub Demander(o As TuyaSlide.OptionTuya, valeur As String, libelle As String)
        If _modeChoix Then
            ' Choix pour une tache : la valeur est retenue et l'option cochee (rien ne part vers l'appareil).
            _valeursChoisies(o.Code) = (libelle, valeur)
            Dim c As CheckBox = Nothing
            If _casesChoix.TryGetValue(o.Code, c) Then c.Checked = True
            Exit Sub
        End If
        If CheckBoxInstantane.Checked Then
            Dim ignore As Task(Of Boolean) = Envoyer(o, valeur, libelle)
            Exit Sub
        End If
        _enAttente.RemoveAll(Function(x) x.Opt.Code = o.Code)
        _enAttente.Add(New EnAttente With {.Opt = o, .Valeur = valeur, .Libelle = libelle})
        LabelEtat.Text = _enAttente.Count & " réglage(s) en attente : OK pour les appliquer."
    End Sub

    ''' <summary>La case vient d'etre cochee : ce qui attendait part tout de suite.</summary>
    Private Sub CheckBoxInstantane_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxInstantane.CheckedChanged
        If Not CheckBoxInstantane.Checked OrElse _enAttente.Count = 0 Then Exit Sub
        Dim ignore As Task(Of Boolean) = EnvoyerLesReglagesEnAttente()
    End Sub

    ''' <summary>Envoie, dans l'ordre, les reglages en attente. Rend faux si l'un d'eux a echoue (il reste alors en attente).</summary>
    Private Async Function EnvoyerLesReglagesEnAttente() As Task(Of Boolean)
        Dim tous As Boolean = True
        For Each r As EnAttente In _enAttente.ToList()
            If Await Envoyer(r.Opt, r.Valeur, r.Libelle) Then _enAttente.Remove(r) Else tous = False
        Next
        Return tous
    End Function

    Private Async Sub ButtonOk_Click(sender As Object, e As EventArgs) Handles ButtonOk.Click
        If _modeChoix Then DialogResult = DialogResult.OK : Exit Sub          ' (rien n'est envoye : les reglages en attente sont rendus a la ligne de minuterie)
        ButtonOk.Enabled = False
        Try
            If Await EnvoyerLesReglagesEnAttente() Then
                If Not IsDisposed Then DialogResult = DialogResult.OK
            ElseIf Not IsDisposed Then
                ButtonOk.Enabled = True
            End If
        Catch ex As Exception
            If Not IsDisposed Then
                LabelEtat.Text = "Échec : " & ex.Message
                ButtonOk.Enabled = True
            End If
        End Try
    End Sub

    ''' <summary>Envoie le reglage a l'appareil ; le resultat est ecrit en bas de la fenetre. Rend vrai si l'appareil l'a accepte.</summary>
    Private Async Function Envoyer(o As TuyaSlide.OptionTuya, valeur As String, libelle As String) As Task(Of Boolean)
        Try
            LabelEtat.Text = "Envoi : " & libelle & "..."
            Dim cmd As New TuyaSlide.Commande With {.Nom = _appareil.Nom, .AppareilId = _appareil.Id, .AppareilNom = _appareil.Nom, .CompteNom = _appareil.CompteNom, .Code = o.Code, .Valeur = valeur}
            Dim erreur As String = Await TuyaSlide.EnvoyerLaCommandeAsync(_comptes, cmd)
            If IsDisposed Then Return erreur = ""
            LabelEtat.Text = If(erreur = "", "« " & _appareil.Nom & " » : " & libelle & " envoyé.", "Échec : " & erreur)
            If erreur <> "" Then DiagLog("Tuya : option « " & o.Code & " » de « " & _appareil.Nom & " » : " & erreur)
            Return erreur = ""
        Catch ex As Exception
            If Not IsDisposed Then LabelEtat.Text = "Échec : " & ex.Message
            Return False
        End Try
    End Function

End Class