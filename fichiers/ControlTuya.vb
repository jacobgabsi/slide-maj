''' <summary>
''' Cadre Tuya de l'onglet Sources (Form4) : un ou plusieurs comptes Tuya / Smart Life (chacun avec ses identifiants), la liste de leurs appareils, a
''' cocher, et des groupes. Chaque appareil coche (qui a un interrupteur) donne deux commandes a la minuterie, "Nom ON" et "Nom OFF" (precedes du nom du
''' compte quand il y a plusieurs comptes) ; chaque groupe donne "Groupe Nom ON" et "Groupe Nom OFF" (type de tache "Tuya", voir TuyaSlide).
''' Les controles sont dans le Concepteur ; Form4 appelle ChargerLesReglages a l'ouverture et EnregistrerLesReglages au OK.
''' </summary>
Public Class ControlTuya

    ''' <summary>Les comptes de la liste deroulante, dans l'ordre.</summary>
    Private _comptes As New List(Of TuyaSlide.Compte)
    ''' <summary>Le compte dont les champs sont affiches.</summary>
    Private _courant As TuyaSlide.Compte
    Private _chargementChamps As Boolean

    ''' <summary>Les appareils de TOUS les comptes, dans l'ordre des cases.</summary>
    Private _appareils As New List(Of TuyaSlide.AppareilChoisi)
    Private _remplissage As Boolean

    ''' <summary>Les groupes (un groupe = des appareils, de n'importe quel compte ; "Groupe Nom ON" / "Groupe Nom OFF" dans la minuterie agissent sur tous).</summary>
    Private _groupes As New List(Of TuyaSlide.GroupeTuya)
    ''' <summary>Les appareils proposes dans la liste des membres (ceux qui ont un interrupteur), dans l'ordre des cases.</summary>
    Private _membresAffiches As New List(Of TuyaSlide.AppareilChoisi)

    Public Sub New()
        InitializeComponent()
        For Each r As TuyaSlide.Region In TuyaSlide.Regions
            ComboBoxRegion.Items.Add(r.Libelle)
        Next
        ComboBoxRegion.SelectedIndex = 0
        Etat("")
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Reglages
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Le message sous les boutons : une ligne, coupee par "..." si elle est trop longue (le texte entier est dans l'infobulle).</summary>
    Private Sub Etat(texte As String)
        LabelEtat.Text = texte
        ToolTip1.SetToolTip(LabelEtat, texte)
    End Sub

    Friend Sub ChargerLesReglages()
        TuyaSlide.FournisseurDeCommandes = AddressOf CommandesCochees          ' la minuterie voit les cases en cours de saisie
        TuyaSlide.FournisseurDeComptes = AddressOf ComptesSaisis
        TuyaSlide.FournisseurActif = AddressOf EstActif
        Dim fenetre As Form = FindForm()
        If fenetre IsNot Nothing AndAlso fenetre IsNot _fenetreSuivie Then
            If _fenetreSuivie IsNot Nothing Then RemoveHandler _fenetreSuivie.VisibleChanged, AddressOf FenetreVisible
            _fenetreSuivie = fenetre
            AddHandler fenetre.VisibleChanged, AddressOf FenetreVisible
        End If
        _chargementChamps = True
        CheckBoxActif.Checked = My.Settings.Tuya_Actif
        _chargementChamps = False
        _comptes = TuyaSlide.ComptesEnregistres()
        If _comptes.Count = 0 Then _comptes.Add(New TuyaSlide.Compte With {.Nom = TuyaSlide.NomDuPremierCompte})
        _appareils = TuyaSlide.AppareilsEnregistres()
        _groupes = TuyaSlide.GroupesEnregistres()
        AfficherLesComptes(Nothing)
        Afficher()
        AfficherLesGroupes()
        Etat(If(_comptes.Any(Function(c) c.Renseigne), "Compte renseigné.", "Compte non renseigné."))
        AppliquerLActivation()
        DemanderLeChargementAutomatique()
    End Sub

    Private Function EstActif() As Boolean
        Return CheckBoxActif.Checked
    End Function

    ''' <summary>Tuya non utilise : tout le cadre est grise (sauf la case et l'aide), et rien n'est telecharge.</summary>
    Private Sub AppliquerLActivation()
        For Each c As Control In GroupBoxTuya.Controls
            If c Is CheckBoxActif OrElse c Is ButtonAide Then Continue For
            c.Enabled = CheckBoxActif.Checked
        Next
        If CheckBoxActif.Checked Then
            ButtonSupprimerCompte.Enabled = (_courant IsNot Nothing)
            ButtonRenommerCompte.Enabled = ButtonSupprimerCompte.Enabled
            CheckedListBoxMembres.Enabled = (GroupeChoisi() IsNot Nothing)
        End If
    End Sub

    Private Sub CheckBoxActif_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxActif.CheckedChanged
        If _chargementChamps Then Exit Sub
        AppliquerLActivation()
        DemanderLeChargementAutomatique()
    End Sub

    ''' <summary>Comptes de la session dont les appareils ont deja ete demandes : l'ouverture des reglages ne les redemande pas (le bouton le fait a la main).</summary>
    Private Shared ReadOnly _dejaDemandes As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>Seulement si Tuya est utilise : les comptes renseignes qui n'ont pas encore d'appareils les chargent tout seuls, une fois par session.</summary>
    Private Sub DemanderLeChargementAutomatique()
        If Not CheckBoxActif.Checked Then Exit Sub
        If Not _comptes.Any(Function(c) c.Renseigne AndAlso c.Uid <> "" AndAlso Not _appareils.Any(Function(a) a.CompteNom = c.Nom) AndAlso Not _dejaDemandes.Contains(c.Nom)) Then Exit Sub
        If IsHandleCreated Then
            BeginInvoke(New Action(AddressOf ChargerLesComptesSansAppareils))
        Else
            AddHandler HandleCreated, Sub(s, e) BeginInvoke(New Action(AddressOf ChargerLesComptesSansAppareils))
        End If
    End Sub

    Private Async Sub ChargerLesComptesSansAppareils()
        For Each c As TuyaSlide.Compte In _comptes.ToList()
            If IsDisposed Then Exit Sub
            If CheckBoxActif.Checked AndAlso c.Renseigne AndAlso c.Uid <> "" AndAlso Not _appareils.Any(Function(a) a.CompteNom = c.Nom) AndAlso _dejaDemandes.Add(c.Nom) Then Await ChargerLesAppareilsDe(c)
        Next
    End Sub

    Private Function ComptesSaisis() As List(Of TuyaSlide.Compte)
        Return _comptes.ToList()
    End Function

    Private Function CommandesCochees() As List(Of TuyaSlide.Commande)
        Return TuyaSlide.CommandesDe(_appareils, _groupes, False)
    End Function

    ''' <summary>Compte et appareils enregistres (OK des reglages) : seuls les comptes qui ont des identifiants sont gardes.</summary>
    Friend Sub EnregistrerLesReglages()
        TuyaSlide.EnregistrerLesComptes(_comptes.Where(Function(c) c.ClientId.Trim() <> "" OrElse c.Secret.Trim() <> "" OrElse c.Uid.Trim() <> ""))
        My.Settings.Tuya_Actif = CheckBoxActif.Checked
        My.Settings.Tuya_Appareils = TuyaSlide.SerialiserLesAppareils(_appareils)
        My.Settings.Tuya_Groupes = TuyaSlide.SerialiserLesGroupes(_groupes)
        My.Settings.Save()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Comptes
    ' ------------------------------------------------------------------------------------------

    Private Sub AfficherLesComptes(selection As String)
        Dim voulu As String = If(selection, If(_courant IsNot Nothing, _courant.Nom, ""))
        _chargementChamps = True
        Try
            ComboBoxComptes.Items.Clear()
            For Each c As TuyaSlide.Compte In _comptes
                ComboBoxComptes.Items.Add(c.Nom)
            Next
            Dim i As Integer = _comptes.FindIndex(Function(c) String.Equals(c.Nom, voulu, StringComparison.OrdinalIgnoreCase))
            ComboBoxComptes.SelectedIndex = If(i >= 0, i, 0)
        Finally
            _chargementChamps = False
        End Try
        MontrerLesChamps()
    End Sub

    ''' <summary>Les champs affichent le compte choisi dans la liste deroulante.</summary>
    Private Sub MontrerLesChamps()
        Dim i As Integer = ComboBoxComptes.SelectedIndex
        _courant = If(i >= 0 AndAlso i < _comptes.Count, _comptes(i), Nothing)
        _chargementChamps = True
        Try
            If _courant Is Nothing Then
                TextBoxClientId.Text = "" : TextBoxSecret.Text = "" : TextBoxUid.Text = ""
            Else
                TextBoxClientId.Text = _courant.ClientId
                TextBoxSecret.Text = _courant.Secret
                TextBoxUid.Text = _courant.Uid
                Dim r As Integer = Array.FindIndex(TuyaSlide.Regions, Function(x) String.Equals(x.Code, _courant.RegionCode, StringComparison.OrdinalIgnoreCase))
                ComboBoxRegion.SelectedIndex = Math.Max(0, r)
            End If
        Finally
            _chargementChamps = False
        End Try
        ButtonSupprimerCompte.Enabled = (_courant IsNot Nothing) AndAlso CheckBoxActif.Checked
        ButtonRenommerCompte.Enabled = ButtonSupprimerCompte.Enabled
    End Sub

    Private Sub ComboBoxComptes_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxComptes.SelectedIndexChanged
        If _chargementChamps Then Exit Sub
        MontrerLesChamps()
    End Sub

    Private Sub ChampsCompte_Changes(sender As Object, e As EventArgs) Handles TextBoxClientId.TextChanged, TextBoxSecret.TextChanged, TextBoxUid.TextChanged, ComboBoxRegion.SelectedIndexChanged
        If _chargementChamps OrElse _courant Is Nothing Then Exit Sub
        _courant.ClientId = TextBoxClientId.Text.Trim()
        _courant.Secret = TextBoxSecret.Text.Trim()
        _courant.Uid = TextBoxUid.Text.Trim()
        _courant.RegionCode = TuyaSlide.Regions(Math.Max(0, ComboBoxRegion.SelectedIndex)).Code
    End Sub

    Private Sub ButtonNouveauCompte_Click(sender As Object, e As EventArgs) Handles ButtonNouveauCompte.Click
        Dim nom As String = Microsoft.VisualBasic.Interaction.InputBox("Nom du compte (par exemple : Maison, Bureau). Avec plusieurs comptes, la minuterie demande de quel compte est chaque ligne Tuya.", "Nouveau compte Tuya", "").Trim().Replace("|", " ")
        If nom = "" Then Exit Sub
        If _comptes.Any(Function(c) String.Equals(c.Nom, nom, StringComparison.OrdinalIgnoreCase)) Then
            Etat("Le compte « " & nom & " » existe déjà.")
            AfficherLesComptes(nom)
            Exit Sub
        End If
        _comptes.Add(New TuyaSlide.Compte With {.Nom = nom})
        AfficherLesComptes(nom)
        AfficherLesMembres()
        Etat("Compte « " & nom & " » créé : saisissez ses identifiants, puis chargez ses appareils.")
    End Sub

    ''' <summary>Change le nom du compte : ses appareils et les lignes de minuterie qui l'utilisent le suivent.</summary>
    Private Sub ButtonRenommerCompte_Click(sender As Object, e As EventArgs) Handles ButtonRenommerCompte.Click
        If _courant Is Nothing Then Exit Sub
        Dim ancien As String = _courant.Nom
        Dim nom As String = Microsoft.VisualBasic.Interaction.InputBox("Nouveau nom du compte « " & ancien & " » :", "Renommer le compte Tuya", ancien).Trim().Replace("|", " ")
        If nom = "" OrElse nom = ancien Then Exit Sub
        If _comptes.Any(Function(c) c IsNot _courant AndAlso String.Equals(c.Nom, nom, StringComparison.OrdinalIgnoreCase)) Then
            Etat("Le compte « " & nom & " » existe déjà.")
            Exit Sub
        End If
        For Each a As TuyaSlide.AppareilChoisi In _appareils
            If String.Equals(a.CompteNom, ancien, StringComparison.OrdinalIgnoreCase) Then a.CompteNom = nom
        Next
        _courant.Nom = nom
        _dejaDemandes.Remove(ancien)
        RenommerDansLaMinuterie(ancien, nom)
        AfficherLesComptes(nom)
        Afficher()
        AfficherLesMembres()
        Etat("Compte « " & ancien & " » renommé « " & nom & " ».")
    End Sub

    ''' <summary>Les lignes de minuterie de type Tuya gardent le compte dans leur argument ("!/Compte:Nom") : elles suivent le nouveau nom.</summary>
    Private Shared Sub RenommerDansLaMinuterie(ancien As String, nouveau As String)
        Try
            For Each ligne As DataGridViewRow In Form4.DataGridView1.Rows
                If ligne.IsNewRow OrElse Not String.Equals(Convert.ToString(ligne.Cells("Type").FormattedValue), "Tuya", StringComparison.OrdinalIgnoreCase) Then Continue For
                Dim argument As String = CStr(If(ligne.Cells("Argument").Value, ""))
                If argument.Contains("!/Compte:" & ancien) Then ligne.Cells("Argument").Value = argument.Replace("!/Compte:" & ancien, "!/Compte:" & nouveau)
            Next
        Catch ex As Exception
            DiagLog("Tuya : renommage du compte dans la minuterie : " & ex.Message)
        End Try
    End Sub

    Private Sub ButtonSupprimerCompte_Click(sender As Object, e As EventArgs) Handles ButtonSupprimerCompte.Click
        If _courant Is Nothing Then Exit Sub
        Dim c As TuyaSlide.Compte = _courant
        If MessageBox.Show(Me, "Supprimer le compte « " & c.Nom & " » et ses appareils ? Les lignes de la minuterie qui les utilisent ne feront plus rien.", "Compte Tuya",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        _appareils.RemoveAll(Function(a) String.Equals(a.CompteNom, c.Nom, StringComparison.OrdinalIgnoreCase))
        _comptes.Remove(c)
        _courant = Nothing
        If _comptes.Count = 0 Then _comptes.Add(New TuyaSlide.Compte With {.Nom = TuyaSlide.NomDuPremierCompte})
        AfficherLesComptes("")
        Afficher()
        AfficherLesMembres()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Appareils
    ' ------------------------------------------------------------------------------------------


    ' ------------------------------------------------------------------------------------------
    ' Tri : un clic sur l'en-tete d'une colonne trie le tableau (un second clic inverse l'ordre ; une fleche montre la colonne triee)
    ' ------------------------------------------------------------------------------------------

    Private _triColonne As String = "ColNom"
    Private _triAscendant As Boolean = True

    ''' <summary>Le premier nombre d'un texte ("18,7 °C - 59 %" donne 18,7) ; NaN s'il n'y en a pas.</summary>
    Private Shared Function PremierNombre(texte As String) As Double
        Dim m As System.Text.RegularExpressions.Match = System.Text.RegularExpressions.Regex.Match(If(texte, ""), "-?\d+([.,]\d+)?")
        If Not m.Success Then Return Double.NaN
        Return Double.Parse(m.Value.Replace(","c, "."c), Globalization.CultureInfo.InvariantCulture)
    End Function

    ''' <summary>Rang d'un appareil pour le tri des colonnes de test : colonne ON, les VERTS (allumes) d'abord ; colonne OFF, les ROUGES (eteints) d'abord. Puis l'autre etat, puis les hors ligne,
    ''' puis l'etat inconnu, puis ceux sans interrupteur.</summary>
    Private Shared Function RangDEtat(a As TuyaSlide.AppareilChoisi, onD As Boolean) As Integer
        If a.Code = "" Then Return 4
        If Not a.EnLigne Then Return 2
        If a.Etat = "" Then Return 3
        If onD Then Return If(a.Etat = "ON", 0, 1)
        Return If(a.Etat = "OFF", 0, 1)
    End Function

    Private Function Trier(liste As IEnumerable(Of TuyaSlide.AppareilChoisi)) As List(Of TuyaSlide.AppareilChoisi)
        Dim cle As Func(Of TuyaSlide.AppareilChoisi, Object) =
            Function(a As TuyaSlide.AppareilChoisi) As Object
                Select Case _triColonne
                    Case "ColPropose" : Return If(a.Coche, 0, 1)
                    Case "ColCompte" : Return If(a.CompteNom, "")
                    Case "ColCode" : Return If(a.Code = "", "~", a.Code)
                    Case "ColEtat" : Return If(a.EnLigne, 0, 1)
                    Case "ColMesure"
                        Dim n As Double = PremierNombre(a.Mesure)
                        Return If(Double.IsNaN(n), Double.MaxValue, n)
                    Case "ColOn" : Return RangDEtat(a, True)
                    Case "ColOff" : Return RangDEtat(a, False)
                    Case Else : Return If(a.Nom, "")
                End Select
            End Function
        Dim ordonne As IOrderedEnumerable(Of TuyaSlide.AppareilChoisi) = If(_triAscendant, liste.OrderBy(cle), liste.OrderByDescending(cle))
        Return ordonne.ThenBy(Function(a) a.Nom, StringComparer.CurrentCultureIgnoreCase).ThenBy(Function(a) a.CompteNom, StringComparer.CurrentCultureIgnoreCase).ToList()
    End Function

    Private Sub MontrerLeSens()
        For Each c As DataGridViewColumn In DataGridViewAppareils.Columns
            c.HeaderCell.SortGlyphDirection = If(c.Name = _triColonne, If(_triAscendant, SortOrder.Ascending, SortOrder.Descending), SortOrder.None)
        Next
    End Sub

    Private Sub DataGridViewAppareils_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles DataGridViewAppareils.ColumnHeaderMouseClick
        If e.Button <> MouseButtons.Left OrElse e.ColumnIndex < 0 Then Exit Sub
        Dim colonne As String = DataGridViewAppareils.Columns(e.ColumnIndex).Name
        If colonne = _triColonne Then
            _triAscendant = Not _triAscendant
        Else
            _triColonne = colonne
            _triAscendant = True
        End If
        Afficher()
    End Sub

    ''' <summary>Le tableau des appareils d'apres _appareils (Proposé = appareil propose dans la minuterie ; État = ON / OFF lu sur l'appareil ; boutons ON et OFF = essai), trie par nom.</summary>
    Private Sub Afficher()
        _appareils = Trier(_appareils)          ' (selon la colonne cliquee ; par defaut, par nom)
        _remplissage = True
        Dim haut As Integer = DataGridViewAppareils.FirstDisplayedScrollingRowIndex
        Dim courante As Integer = If(DataGridViewAppareils.CurrentRow IsNot Nothing, DataGridViewAppareils.CurrentRow.Index, -1)
        Try
            ColCompte.Visible = (_comptes.Count > 1)
            MontrerLeSens()
            DataGridViewAppareils.Rows.Clear()
            For Each a As TuyaSlide.AppareilChoisi In _appareils
                Dim n As Integer = DataGridViewAppareils.Rows.Add(a.Coche, a.Nom, a.CompteNom, If(a.Code = "", "(aucun)", a.Code), "", a.Mesure, "ON", "OFF")
                MettreAJourLEtat(DataGridViewAppareils.Rows(n), a)
            Next
            If courante >= 0 AndAlso courante < DataGridViewAppareils.Rows.Count Then DataGridViewAppareils.CurrentCell = DataGridViewAppareils.Rows(courante).Cells("ColNom")
            If haut >= 0 AndAlso haut < DataGridViewAppareils.Rows.Count Then DataGridViewAppareils.FirstDisplayedScrollingRowIndex = haut
        Finally
            _remplissage = False
        End Try
    End Sub

    ''' <summary>
    ''' Les boutons ON et OFF d'une ligne montrent l'etat de l'appareil par leur fond : ON vert quand il est allume, OFF rouge quand il est eteint (l'autre bouton reste neutre).
    ''' Hors ligne : memes couleurs, ternes, avec le dernier etat connu en infobulle. Etat pas encore lu : les deux restent neutres. Sans interrupteur (capteur, passerelle,
    ''' camera, telecommande) : pas de bouton (cases vides).
    ''' </summary>
    Private Sub MettreAJourLEtat(ligne As DataGridViewRow, a As TuyaSlide.AppareilChoisi)
        Dim grille As DataGridView = ligne.DataGridView
        ' Colonne État : "hors ligne" (gris) seulement quand Tuya ne joint pas l'appareil ; ON / OFF sont deja montres par la couleur des boutons.
        Dim cellEtat As DataGridViewCell = ligne.Cells("ColEtat")
        cellEtat.Value = If(a.EnLigne, "", "hors ligne")
        cellEtat.Style.ForeColor = Color.FromArgb(150, 150, 150)
        cellEtat.Style.Font = New Font(grille.Font, FontStyle.Bold)
        If a.Code = "" Then
            ' Pas d'interrupteur (capteur, passerelle, camera, telecommande) : les boutons ON et OFF sont effaces (cases vides, sans bouton) ; la mesure, s'il y en a une, est dans la colonne Mesure.
            For Each nom As String In {"ColOn", "ColOff"}
                If TypeOf ligne.Cells(nom) Is DataGridViewButtonCell Then
                    Dim vide As New DataGridViewTextBoxCell With {.Value = ""}
                    ligne.Cells(nom) = vide
                    vide.ReadOnly = True          ' (apres l'avoir mise dans la ligne : impossible avant)
                End If
            Next
            Exit Sub
        End If
        ' Avec interrupteur : de vrais boutons (une case videe plus haut est remise en bouton).
        For Each nom As String In {"ColOn", "ColOff"}
            If Not TypeOf ligne.Cells(nom) Is DataGridViewButtonCell Then
                ligne.Cells(nom) = New DataGridViewButtonCell With {.FlatStyle = FlatStyle.Flat, .UseColumnTextForButtonValue = True}
            End If
        Next
        Dim on_ As DataGridViewButtonCell = DirectCast(ligne.Cells("ColOn"), DataGridViewButtonCell)
        Dim off_ As DataGridViewButtonCell = DirectCast(ligne.Cells("ColOff"), DataGridViewButtonCell)
        Dim defaut As Color = grille.DefaultCellStyle.BackColor
        Dim texteDefaut As Color = grille.DefaultCellStyle.ForeColor
        Dim vert As Color = If(a.EnLigne, Color.FromArgb(46, 160, 67), Color.FromArgb(78, 112, 88))
        Dim rouge As Color = If(a.EnLigne, Color.FromArgb(200, 55, 55), Color.FromArgb(125, 80, 80))
        Dim blanc As Color = If(a.EnLigne, Color.White, Color.FromArgb(200, 200, 200))
        on_.UseColumnTextForButtonValue = True : off_.UseColumnTextForButtonValue = True
        If Not a.EnLigne Then
            ' Hors ligne : ON et OFF en blanc sur fond noir (sans couleur d'etat) ; le dernier etat connu est dans l'infobulle.
            For Each c As DataGridViewButtonCell In {on_, off_}
                c.Style.BackColor = Color.Black
                c.Style.ForeColor = Color.White
                c.Style.SelectionBackColor = Color.Black
                c.Style.SelectionForeColor = Color.White
                c.ToolTipText = "Hors ligne : Tuya ne joint pas cet appareil" & If(a.Etat <> "", " (dernier état connu : " & a.Etat & ")", "") & "."
            Next
            Exit Sub
        End If
        Dim infobulle As String
        If Not a.EnLigne Then
            infobulle = "Hors ligne : Tuya ne joint pas cet appareil" & If(a.Etat <> "", " (dernier état connu : " & a.Etat & ")", "") & "."
        ElseIf a.Etat = "" Then
            infobulle = "État pas encore lu : « Charger/Actualiser appareils »."
        Else
            infobulle = "Appareil " & If(a.Etat = "ON", "allumé (ON)", "éteint (OFF)") & ". Fond vert : allumé ; fond rouge : éteint."
        End If
        on_.ToolTipText = infobulle : off_.ToolTipText = infobulle
        on_.Style.BackColor = If(a.Etat = "ON", vert, defaut)
        on_.Style.ForeColor = If(a.Etat = "ON", blanc, texteDefaut)
        on_.Style.SelectionBackColor = on_.Style.BackColor
        off_.Style.BackColor = If(a.Etat = "OFF", rouge, defaut)
        off_.Style.ForeColor = If(a.Etat = "OFF", blanc, texteDefaut)
        off_.Style.SelectionBackColor = off_.Style.BackColor
        on_.Style.SelectionForeColor = on_.Style.ForeColor : off_.Style.SelectionForeColor = off_.Style.ForeColor
    End Sub

    ''' <summary>Les appareils d'UN compte : la liste de ce compte est refaite d'apres Tuya (le code de l'interrupteur de chacun est lu), ses cases deja cochees restent cochees.</summary>
    Private Async Function ChargerLesAppareilsDe(compte As TuyaSlide.Compte) As Task
        ButtonAppareils.Enabled = False
        Etat("Connexion...")
        Try
            Dim trouves As List(Of TuyaSlide.Appareil) = Await TuyaSlide.ListerLesAppareilsAsync(compte)
            Dim coches As New HashSet(Of String)(_appareils.Where(Function(a) a.Coche AndAlso String.Equals(a.CompteNom, compte.Nom, StringComparison.OrdinalIgnoreCase)).Select(Function(a) a.Id))
            ' Le code et l'etat (ON / OFF) de l'interrupteur de chaque appareil : lus six par six pour aller vite.
            Dim lectures(trouves.Count - 1) As KeyValuePair(Of String, String)
            Dim mesures(trouves.Count - 1) As String
            Dim fait As Integer = 0
            Dim feu As New Threading.SemaphoreSlim(6)
            Dim taches As New List(Of Task)
            For i As Integer = 0 To trouves.Count - 1
                Dim rang As Integer = i
                taches.Add(LireUnInterrupteur(compte, trouves(rang).Id, feu,
                                              Sub(r, m)
                                                  lectures(rang) = r
                                                  mesures(rang) = m
                                                  fait += 1
                                                  Etat("Appareil " & fait & " / " & trouves.Count)
                                              End Sub))
            Next
            Await Task.WhenAll(taches)
            Dim liste As New List(Of TuyaSlide.AppareilChoisi)
            For i As Integer = 0 To trouves.Count - 1
                Dim a As TuyaSlide.Appareil = trouves(i)
                liste.Add(New TuyaSlide.AppareilChoisi With {.Id = a.Id, .Nom = a.Nom, .Code = lectures(i).Key, .Etat = lectures(i).Value, .EnLigne = a.EnLigne, .Mesure = mesures(i), .CompteNom = compte.Nom,
                                                             .Coche = coches.Contains(a.Id) AndAlso lectures(i).Key <> ""})
            Next
            _appareils.RemoveAll(Function(a) String.Equals(a.CompteNom, compte.Nom, StringComparison.OrdinalIgnoreCase))
            _appareils.AddRange(liste)
            Afficher()
            AfficherLesMembres()
            Etat(compte.Nom & " : " & liste.Count & " appareil(s) trouvé(s).")
        Catch ex As Exception
            Etat(compte.Nom & " - échec : " & ex.Message)
        Finally
            ButtonAppareils.Enabled = True
        End Try
    End Function

    ''' <summary>Lit le code et l'etat de l'interrupteur d'un appareil ; sans interrupteur (capteur...), sa mesure (temperature, humidite). Hors ligne : ce que Tuya garde de lui.</summary>
    Private Shared Async Function LireUnInterrupteur(compte As TuyaSlide.Compte, id As String, feu As Threading.SemaphoreSlim, fini As Action(Of KeyValuePair(Of String, String), String)) As Task
        Await feu.WaitAsync()
        Dim resultat As New KeyValuePair(Of String, String)("", "")
        Dim mesure As String = ""
        Try
            resultat = Await TuyaSlide.InterrupteurAsync(compte, id)
            If resultat.Key = "" Then mesure = Await TuyaSlide.MesureAsync(compte, id)
        Catch ex As Exception
            ' (appareil qui ne repond pas : ni interrupteur ni mesure ; relu au prochain chargement)
        Finally
            feu.Release()
        End Try
        fini(resultat, mesure)
    End Function

    ''' <summary>Met a jour la colonne État du tableau (apres un essai ON / OFF), sans rien redessiner d'autre.</summary>
    Private Sub RafraichirLaListe()
        For Each ligne As DataGridViewRow In DataGridViewAppareils.Rows
            Dim n As Integer = ligne.Index
            If n >= 0 AndAlso n < _appareils.Count Then MettreAJourLEtat(ligne, _appareils(n))
        Next
    End Sub

    Private Async Sub ButtonAppareils_Click(sender As Object, e As EventArgs) Handles ButtonAppareils.Click
        If _courant Is Nothing Then Exit Sub
        Await ChargerLesAppareilsDe(_courant)
    End Sub

    ''' <summary>La case "Proposé" est validee tout de suite (sans quitter la ligne).</summary>
    Private Sub DataGridViewAppareils_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles DataGridViewAppareils.CurrentCellDirtyStateChanged
        If DataGridViewAppareils.IsCurrentCellDirty Then DataGridViewAppareils.CommitEdit(DataGridViewDataErrorContexts.Commit)
    End Sub

    Private Sub DataGridViewAppareils_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewAppareils.CellValueChanged
        If _remplissage OrElse e.RowIndex < 0 OrElse e.RowIndex >= _appareils.Count Then Exit Sub
        If DataGridViewAppareils.Columns(e.ColumnIndex).Name <> "ColPropose" Then Exit Sub
        Dim a As TuyaSlide.AppareilChoisi = _appareils(e.RowIndex)
        Dim coche As Boolean = Convert.ToBoolean(DataGridViewAppareils.Rows(e.RowIndex).Cells("ColPropose").Value)
        If coche AndAlso a.Code = "" Then
            _remplissage = True
            DataGridViewAppareils.Rows(e.RowIndex).Cells("ColPropose").Value = False          ' sans interrupteur, ON / OFF n'aurait rien a commander
            _remplissage = False
            Etat("« " & a.Nom & " » n'a pas d'interrupteur lu (rechargez les appareils, ou il est hors ligne).")
            Exit Sub
        End If
        a.Coche = coche
    End Sub

    ''' <summary>Les boutons ON et OFF d'une ligne : essai immediat sur cet appareil.</summary>
    Private Async Sub DataGridViewAppareils_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewAppareils.CellContentClick
        If e.RowIndex < 0 OrElse e.RowIndex >= _appareils.Count Then Exit Sub
        Dim colonne As String = DataGridViewAppareils.Columns(e.ColumnIndex).Name
        If colonne <> "ColOn" AndAlso colonne <> "ColOff" Then Exit Sub
        Await TesterAppareil(_appareils(e.RowIndex), If(colonne = "ColOn", "true", "false"))
    End Sub

    Private _essaiEnCours As Boolean

    Private Async Function TesterAppareil(a As TuyaSlide.AppareilChoisi, valeur As String) As Task
        If _essaiEnCours Then Exit Function
        If a.Code = "" Then
            Etat("« " & a.Nom & " » n'a pas d'interrupteur lu.")
            Exit Function
        End If
        _essaiEnCours = True
        Etat("Envoi à « " & a.Nom & " »...")
        Try
            Dim cmd As New TuyaSlide.Commande With {.Nom = a.Nom, .AppareilId = a.Id, .AppareilNom = a.Nom, .CompteNom = a.CompteNom, .Code = a.Code, .Valeur = valeur}
            Dim erreur As String = Await TuyaSlide.EnvoyerLaCommandeAsync(_comptes, cmd)
            Etat(If(erreur = "", "« " & a.Nom & " " & If(valeur = "true", "ON", "OFF") & " » envoyée.", "Échec : " & erreur))
            If erreur = "" Then
                a.Etat = If(valeur = "true", "ON", "OFF") : a.EnLigne = True
                RafraichirLaListe()
            End If
        Finally
            _essaiEnCours = False
        End Try
    End Function

    ''' <summary>Coche (ou decoche) tous les appareils qui ont un interrupteur : ceux-la sont alors proposes dans la minuterie.</summary>
    Private Sub CocherTout(coche As Boolean)
        For Each a As TuyaSlide.AppareilChoisi In _appareils
            a.Coche = coche AndAlso a.Code <> ""
        Next
        Afficher()
        Etat(If(coche, _appareils.Where(Function(a) a.Coche).Count() & " appareil(s) coché(s).", "Aucun appareil coché."))
    End Sub

    Private Sub ButtonToutCocher_Click(sender As Object, e As EventArgs) Handles ButtonToutCocher.Click
        CocherTout(True)
    End Sub

    Private Sub ButtonToutDecocher_Click(sender As Object, e As EventArgs) Handles ButtonToutDecocher.Click
        CocherTout(False)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Groupes
    ' ------------------------------------------------------------------------------------------

    Private Sub AfficherLesGroupes(Optional selection As String = Nothing)
        Dim voulu As String = If(selection, If(ListBoxGroupes.SelectedItem IsNot Nothing, CStr(ListBoxGroupes.SelectedItem), ""))
        _groupes = _groupes.OrderBy(Function(g) g.Nom, StringComparer.CurrentCultureIgnoreCase).ToList()
        ListBoxGroupes.Items.Clear()
        For Each g As TuyaSlide.GroupeTuya In _groupes
            ListBoxGroupes.Items.Add(g.Nom)
        Next
        Dim i As Integer = _groupes.FindIndex(Function(g) String.Equals(g.Nom, voulu, StringComparison.OrdinalIgnoreCase))
        If i >= 0 Then ListBoxGroupes.SelectedIndex = i
        AfficherLesMembres()
    End Sub

    Private Function GroupeChoisi() As TuyaSlide.GroupeTuya
        Dim i As Integer = ListBoxGroupes.SelectedIndex
        If i < 0 OrElse i >= _groupes.Count Then Return Nothing
        Return _groupes(i)
    End Function

    ''' <summary>Les appareils (avec interrupteur) a cocher pour le groupe selectionne.</summary>
    Private Sub AfficherLesMembres()
        _remplissage = True
        Try
            CheckedListBoxMembres.Items.Clear()
            _membresAffiches = _appareils.Where(Function(a) a.Code <> "").ToList()
            Dim g As TuyaSlide.GroupeTuya = GroupeChoisi()
            For Each a As TuyaSlide.AppareilChoisi In _membresAffiches
                CheckedListBoxMembres.Items.Add(a.Nom & If(_comptes.Count > 1, "   - " & a.CompteNom, ""), g IsNot Nothing AndAlso g.Ids.Contains(a.Id))
            Next
            CheckedListBoxMembres.Enabled = (g IsNot Nothing)
            LabelMembres.Text = If(g Is Nothing, "Choisissez ou créez un groupe :", "Appareils du groupe « " & g.Nom & " » :")
        Finally
            _remplissage = False
        End Try
    End Sub

    Private Sub ListBoxGroupes_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListBoxGroupes.SelectedIndexChanged
        AfficherLesMembres()
    End Sub

    Private Sub CheckedListBoxMembres_ItemCheck(sender As Object, e As ItemCheckEventArgs) Handles CheckedListBoxMembres.ItemCheck
        If _remplissage OrElse e.Index < 0 OrElse e.Index >= _membresAffiches.Count Then Exit Sub
        Dim g As TuyaSlide.GroupeTuya = GroupeChoisi()
        If g Is Nothing Then Exit Sub
        Dim id As String = _membresAffiches(e.Index).Id
        If e.NewValue = CheckState.Checked Then
            If Not g.Ids.Contains(id) Then g.Ids.Add(id)
        Else
            g.Ids.Remove(id)
        End If
    End Sub

    Private Sub ButtonNouveauGroupe_Click(sender As Object, e As EventArgs) Handles ButtonNouveauGroupe.Click
        Dim nom As String = Microsoft.VisualBasic.Interaction.InputBox("Nom du groupe (par exemple : Salon). Dans la minuterie : « Groupe Salon ON » et « Groupe Salon OFF ».", "Nouveau groupe Tuya", "").Trim().Replace("|", " ")
        If nom = "" Then Exit Sub
        If _groupes.Any(Function(g) String.Equals(g.Nom, nom, StringComparison.OrdinalIgnoreCase)) Then
            Etat("Le groupe « " & nom & " » existe déjà.")
            AfficherLesGroupes(nom)
            Exit Sub
        End If
        _groupes.Add(New TuyaSlide.GroupeTuya With {.Nom = nom})
        AfficherLesGroupes(nom)
        Etat(If(_membresAffiches.Count = 0, "Groupe créé : chargez d'abord vos appareils pour les y mettre.", "Groupe créé : cochez ses appareils."))
    End Sub

    Private Sub ButtonSupprimerGroupe_Click(sender As Object, e As EventArgs) Handles ButtonSupprimerGroupe.Click
        Dim g As TuyaSlide.GroupeTuya = GroupeChoisi()
        If g Is Nothing Then Exit Sub
        If MessageBox.Show(Me, "Supprimer le groupe « " & g.Nom & " » ? Les lignes de la minuterie qui l'utilisent ne feront plus rien.", "Groupe Tuya",
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) <> DialogResult.Yes Then Exit Sub
        _groupes.Remove(g)
        AfficherLesGroupes("")
    End Sub

    ''' <summary>Essai du groupe selectionne : tous ses appareils (de tous les comptes) recoivent ON ou OFF, comme la commande "Groupe Nom ON / OFF" de la minuterie.</summary>
    Private Async Function TesterLeGroupe(valeur As String) As Task
        Dim g As TuyaSlide.GroupeTuya = GroupeChoisi()
        If g Is Nothing Then
            Etat("Sélectionnez un groupe dans la liste.")
            Exit Function
        End If
        Dim nom As String = "Groupe " & g.Nom & If(valeur = "true", " ON", " OFF")
        Dim cmd As TuyaSlide.Commande = TuyaSlide.CommandesDe(_appareils, {g}, False).FirstOrDefault(Function(c) c.Membres IsNot Nothing AndAlso String.Equals(c.Nom, nom, StringComparison.OrdinalIgnoreCase))
        If cmd Is Nothing Then
            Etat("Le groupe « " & g.Nom & " » n'a aucun appareil avec interrupteur : cochez-en à droite.")
            Exit Function
        End If
        ButtonTestGroupeOn.Enabled = False : ButtonTestGroupeOff.Enabled = False
        Etat("Envoi au groupe « " & g.Nom & " » (" & cmd.Membres.Count & " appareil(s))...")
        Try
            Dim erreur As String = Await TuyaSlide.EnvoyerLaCommandeAsync(_comptes, cmd)
            Etat(If(erreur = "", "« " & nom & " » envoyée.", "Échec : " & erreur))
            If erreur = "" Then
                Dim etatVoulu As String = If(valeur = "true", "ON", "OFF")
                For Each m As TuyaSlide.Commande In cmd.Membres
                    Dim cible As TuyaSlide.AppareilChoisi = _appareils.FirstOrDefault(Function(x) x.Id = m.AppareilId)
                    If cible IsNot Nothing Then cible.Etat = etatVoulu
                Next
                RafraichirLaListe()
            End If
        Finally
            ButtonTestGroupeOn.Enabled = True : ButtonTestGroupeOff.Enabled = True
        End Try
    End Function

    Private Async Sub ButtonTestGroupeOn_Click(sender As Object, e As EventArgs) Handles ButtonTestGroupeOn.Click
        Await TesterLeGroupe("true")
    End Sub

    Private Async Sub ButtonTestGroupeOff_Click(sender As Object, e As EventArgs) Handles ButtonTestGroupeOff.Click
        Await TesterLeGroupe("false")
    End Sub

    Private Sub ButtonAide_Click(sender As Object, e As EventArgs) Handles ButtonAide.Click
        FormAideTuya.Montrer(FindForm())
    End Sub

    Private _fenetreSuivie As Form

    ' Fenetre des reglages fermee ou annulee : la minuterie relit les reglages enregistres (et pas une saisie abandonnee).
    Private Sub FenetreVisible(sender As Object, e As EventArgs)
        If _fenetreSuivie IsNot Nothing AndAlso Not _fenetreSuivie.Visible Then
            TuyaSlide.FournisseurDeCommandes = Nothing
            TuyaSlide.FournisseurDeComptes = Nothing
            TuyaSlide.FournisseurActif = Nothing
        End If
    End Sub

    ' Reglages fermes : la minuterie relit les reglages enregistres.
    Private Sub ControlTuya_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
        TuyaSlide.FournisseurDeCommandes = Nothing
        TuyaSlide.FournisseurDeComptes = Nothing
        TuyaSlide.FournisseurActif = Nothing
    End Sub

End Class
