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
    ''' <summary>Les appareils montres dans le tableau (ceux du compte choisi, ou de tous), dans l'ordre des lignes.</summary>
    Private _affiches As New List(Of TuyaSlide.AppareilChoisi)
    ''' <summary>Les groupes montres dans la liste (ceux du compte choisi, ou de tous), dans l'ordre.</summary>
    Private _groupesAffiches As New List(Of TuyaSlide.GroupeTuya)
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
        TuyaSlide.FournisseurDAppareils = Function() _appareils.ToList()          ' (tous les appareils et leurs maisons, coches ou non : la liste des maisons de la minuterie)
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
        Afficher()          ' (le tableau suit le compte choisi ; ajout, suppression ou renommage d'un compte)
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
        If _comptes.Count > 1 Then Afficher()          ' (le tableau suit le compte choisi)
    End Sub

    Private _remplissageMaisons As Boolean

    Private Sub ComboBoxMaisons_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBoxMaisons.SelectedIndexChanged
        If _remplissageMaisons OrElse _chargementChamps Then Exit Sub
        Afficher()
    End Sub

    Private Sub CheckBoxTousLesComptes_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxTousLesComptes.CheckedChanged
        If _chargementChamps Then Exit Sub
        Afficher()
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
        ' Les groupes appartiennent au compte : ils le suivent (sans cela, ils restaient attaches a l'ancien nom et disparaissaient de la liste).
        For Each g As TuyaSlide.GroupeTuya In _groupes
            If String.Equals(g.CompteNom, ancien, StringComparison.OrdinalIgnoreCase) Then g.CompteNom = nom
        Next
        _courant.Nom = nom
        _dejaDemandes.Remove(ancien)
        RenommerDansLaMinuterie(ancien, nom)
        AfficherLesComptes(nom)
        Afficher()
        AfficherLesMembres()
        Etat("Compte « " & ancien & " » renommé « " & nom & " » : cliquez sur OK (en bas des réglages) pour l'enregistrer.")
    End Sub

    ''' <summary>Les lignes de minuterie de type Tuya gardent le compte dans leur argument ("!/Compte:Nom") : elles suivent le nouveau nom.</summary>
    Private Shared Sub RenommerDansLaMinuterie(ancien As String, nouveau As String)
        Try
            For Each ligne As DataGridViewRow In Form4.DataGridView1.Rows.Cast(Of DataGridViewRow)().Concat(Form4.DataGridViewTuya.Rows.Cast(Of DataGridViewRow)()).ToList()
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
                    Case "ColMaison" : Return If(a.MaisonNom, "")
                    Case "ColOptions" : Return If(a.Code = "", 1, 0)          ' (les appareils avec un interrupteur d'abord)
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
        Dim triee As List(Of TuyaSlide.AppareilChoisi) = ordonne.ThenBy(Function(a) a.Nom, StringComparer.CurrentCultureIgnoreCase).ThenBy(Function(a) a.CompteNom, StringComparer.CurrentCultureIgnoreCase).ToList()
        ' Les scenarios manuels (lignes violettes) toujours a la fin, quel que soit le tri choisi (30/10/2026).
        Return triee.Where(Function(a) Not TuyaSlide.EstUneScene(a.Code)).Concat(triee.Where(Function(a) TuyaSlide.EstUneScene(a.Code))).ToList()
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
        ' Avec plusieurs comptes, le tableau montre les appareils du compte choisi dans la liste « Compte Tuya : » (28/10/2026 : « changer le compte ne change rien ») ;
        ' la case « tous les comptes » les montre tous. _affiches : les lignes du tableau, dans l'ordre (la ligne n est _affiches(n)).
        Dim tous As Boolean = (_comptes.Count <= 1 OrElse _courant Is Nothing OrElse CheckBoxTousLesComptes.Checked)
        _affiches = If(tous, _appareils.ToList(), _appareils.Where(Function(a) String.Equals(a.CompteNom, _courant.Nom, StringComparison.OrdinalIgnoreCase)).ToList())
        CheckBoxTousLesComptes.Visible = (_comptes.Count > 1)
        ' Maisons Smart Life (« Soisy », « Cannes », « Bureau »...) : la liste propose celles des appareils du compte (ou des comptes) montres ; une maison choisie filtre le tableau.
        Dim maisons As List(Of String) = _affiches.Select(Function(a) a.MaisonNom).Where(Function(m) m <> "").Distinct(StringComparer.CurrentCultureIgnoreCase).OrderBy(Function(m) m, StringComparer.CurrentCultureIgnoreCase).ToList()
        Dim maisonVoulue As String = If(ComboBoxMaisons.SelectedIndex > 0, CStr(ComboBoxMaisons.SelectedItem), "")
        _remplissageMaisons = True
        Try
            ComboBoxMaisons.Items.Clear()
            ComboBoxMaisons.Items.Add("Toutes les maisons")
            ComboBoxMaisons.Items.AddRange(maisons.Cast(Of Object)().ToArray())
            Dim im As Integer = maisons.FindIndex(Function(m) String.Equals(m, maisonVoulue, StringComparison.CurrentCultureIgnoreCase))
            ComboBoxMaisons.SelectedIndex = If(im >= 0, im + 1, 0)
        Finally
            _remplissageMaisons = False
        End Try
        Dim avecMaisons As Boolean = maisons.Count > 0
        LabelMaison.Visible = avecMaisons
        ComboBoxMaisons.Visible = avecMaisons
        ColMaison.Visible = avecMaisons
        If ComboBoxMaisons.SelectedIndex > 0 Then
            Dim choisie As String = CStr(ComboBoxMaisons.SelectedItem)
            _affiches = _affiches.Where(Function(a) String.Equals(a.MaisonNom, choisie, StringComparison.CurrentCultureIgnoreCase)).ToList()
        End If
        _remplissage = True
        Dim haut As Integer = DataGridViewAppareils.FirstDisplayedScrollingRowIndex
        Dim courante As Integer = If(DataGridViewAppareils.CurrentRow IsNot Nothing, DataGridViewAppareils.CurrentRow.Index, -1)
        Try
            ColCompte.Visible = (_comptes.Count > 1 AndAlso tous)
            MontrerLeSens()
            DataGridViewAppareils.Rows.Clear()
            For Each a As TuyaSlide.AppareilChoisi In _affiches
                Dim n As Integer = DataGridViewAppareils.Rows.Add(a.Coche, a.Nom, a.CompteNom, a.MaisonNom, "Options...", "", a.Mesure, "ON", "OFF")
                MettreAJourLEtat(DataGridViewAppareils.Rows(n), a)
            Next
            If courante >= 0 AndAlso courante < DataGridViewAppareils.Rows.Count Then DataGridViewAppareils.CurrentCell = DataGridViewAppareils.Rows(courante).Cells("ColNom")
            If haut >= 0 AndAlso haut < DataGridViewAppareils.Rows.Count Then DataGridViewAppareils.FirstDisplayedScrollingRowIndex = haut
        Finally
            _remplissage = False
        End Try
        AfficherLesGroupes()          ' (les groupes suivent aussi le compte choisi)
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
        If TuyaSlide.EstUneScene(a.Code) Then
            ' Scenario manuel : fond violet pour toute la ligne ; pas d'Options ; un seul bouton « Lancer » (essai) a la place de ON, rien a la place de OFF.
            Dim fond As Color = Color.FromArgb(88, 64, 150)
            ligne.DefaultCellStyle.BackColor = fond
            ligne.DefaultCellStyle.ForeColor = Color.White
            ligne.DefaultCellStyle.SelectionBackColor = Color.FromArgb(124, 98, 196)
            ligne.DefaultCellStyle.SelectionForeColor = Color.White
            cellEtat.Value = "scénario"
            cellEtat.Style.ForeColor = Color.FromArgb(215, 205, 245)
            If Not TypeOf ligne.Cells("ColOptions") Is DataGridViewTextBoxCell Then
                Dim sansOptions As New DataGridViewTextBoxCell With {.Value = ""}
                ligne.Cells("ColOptions") = sansOptions
                sansOptions.ReadOnly = True
            End If
            If Not TypeOf ligne.Cells("ColOff") Is DataGridViewTextBoxCell Then
                Dim sansOff As New DataGridViewTextBoxCell With {.Value = ""}
                ligne.Cells("ColOff") = sansOff
                sansOff.ReadOnly = True
            End If
            If Not TypeOf ligne.Cells("ColOn") Is DataGridViewButtonCell Then
                ligne.Cells("ColOn") = New DataGridViewButtonCell With {.FlatStyle = FlatStyle.Flat}
            End If
            Dim lancer As DataGridViewButtonCell = DirectCast(ligne.Cells("ColOn"), DataGridViewButtonCell)
            lancer.UseColumnTextForButtonValue = False
            lancer.Value = "Lancer"
            lancer.ToolTipText = "Scénario manuel de Smart Life : « Lancer » l'exécute tout de suite."
            lancer.Style.BackColor = Color.FromArgb(46, 160, 67) : lancer.Style.ForeColor = Color.White
            lancer.Style.SelectionBackColor = lancer.Style.BackColor : lancer.Style.SelectionForeColor = Color.White
            Exit Sub
        End If
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
                liste.Add(New TuyaSlide.AppareilChoisi With {.Id = a.Id, .Nom = a.Nom, .Code = lectures(i).Key, .Etat = lectures(i).Value, .EnLigne = a.EnLigne, .Mesure = mesures(i), .CompteNom = compte.Nom, .MaisonNom = a.MaisonNom,
                                                             .Coche = coches.Contains(a.Id) AndAlso lectures(i).Key <> ""})
            Next
            ' Les scenarios manuels de chaque maison (30/10/2026) : ranges avec les appareils, sur un fond different, une seule commande chacun.
            Dim nombreDeScenes As Integer = 0
            Try
                Etat("Scénarios manuels...")
                For Each s As TuyaSlide.Appareil In Await TuyaSlide.ListerLesScenesAsync(compte)
                    liste.Add(New TuyaSlide.AppareilChoisi With {.Id = s.Id, .Nom = s.Nom, .Code = s.Categorie, .Etat = "", .EnLigne = True, .Mesure = "", .CompteNom = compte.Nom, .MaisonNom = s.MaisonNom,
                                                                 .Coche = coches.Contains(s.Id)})
                    nombreDeScenes += 1
                Next
            Catch ex As Exception
                DiagLog("Tuya : scenarios du compte « " & compte.Nom & " » : " & ex.Message)
            End Try
            ' Une maison renommee dans l'application Smart Life : les appareils gardent leur identifiant mais changent de nom de maison ; les groupes (et la maison choisie) suivent le nouveau nom.
            Dim anciennesMaisons As Dictionary(Of String, String) = _appareils.Where(Function(a) String.Equals(a.CompteNom, compte.Nom, StringComparison.OrdinalIgnoreCase) AndAlso a.MaisonNom <> "").
                                                                    GroupBy(Function(a) a.Id).ToDictionary(Function(x) x.Key, Function(x) x.First().MaisonNom)
            Dim renommees As New Dictionary(Of String, String)(StringComparer.CurrentCultureIgnoreCase)
            For Each a As TuyaSlide.AppareilChoisi In liste
                Dim avant As String = Nothing
                If a.MaisonNom <> "" AndAlso anciennesMaisons.TryGetValue(a.Id, avant) AndAlso Not String.Equals(avant, a.MaisonNom, StringComparison.CurrentCultureIgnoreCase) Then renommees(avant) = a.MaisonNom
            Next
            For Each paire As KeyValuePair(Of String, String) In renommees
                For Each g As TuyaSlide.GroupeTuya In _groupes
                    If String.Equals(g.CompteNom, compte.Nom, StringComparison.OrdinalIgnoreCase) AndAlso String.Equals(g.MaisonNom, paire.Key, StringComparison.CurrentCultureIgnoreCase) Then g.MaisonNom = paire.Value
                Next
                DiagLog("Tuya : maison « " & paire.Key & " » renommee « " & paire.Value & " » (compte « " & compte.Nom & " ») : ses groupes suivent")
            Next
            _appareils.RemoveAll(Function(a) String.Equals(a.CompteNom, compte.Nom, StringComparison.OrdinalIgnoreCase))
            _appareils.AddRange(liste)
            TuyaSlide.RattacherLesGroupesAuxMaisons(_groupes, _appareils)          ' (les maisons viennent d'etre apprises : les groupes d'avant s'y rattachent)
            Afficher()
            AfficherLesMembres()
            Etat(compte.Nom & " : " & (liste.Count - nombreDeScenes) & " appareil(s) et " & nombreDeScenes & " scénario(s) manuel(s) trouvé(s).")
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
            If n >= 0 AndAlso n < _affiches.Count Then MettreAJourLEtat(ligne, _affiches(n))
        Next
    End Sub

    Private Async Sub ButtonAppareils_Click(sender As Object, e As EventArgs) Handles ButtonAppareils.Click
        If _courant Is Nothing Then Exit Sub
        ' Tous les comptes renseignes sont actualises d'un coup (28/10/2026, apres « le bouton ne recharge que le compte choisi ») ; sinon, le compte choisi.
        Dim aCharger As List(Of TuyaSlide.Compte) = _comptes.Where(Function(c) c.Renseigne AndAlso c.Uid.Trim() <> "").ToList()
        If aCharger.Count = 0 Then aCharger.Add(_courant)
        For Each c As TuyaSlide.Compte In aCharger
            Await ChargerLesAppareilsDe(c)
        Next
        If aCharger.Count > 1 Then Etat(aCharger.Count & " comptes actualisés : " & _appareils.Count & " appareil(s) au total.")
    End Sub

    ''' <summary>La case "Proposé" est validee tout de suite (sans quitter la ligne).</summary>
    Private Sub DataGridViewAppareils_CurrentCellDirtyStateChanged(sender As Object, e As EventArgs) Handles DataGridViewAppareils.CurrentCellDirtyStateChanged
        If DataGridViewAppareils.IsCurrentCellDirty Then DataGridViewAppareils.CommitEdit(DataGridViewDataErrorContexts.Commit)
    End Sub

    Private Sub DataGridViewAppareils_CellValueChanged(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridViewAppareils.CellValueChanged
        If _remplissage OrElse e.RowIndex < 0 OrElse e.RowIndex >= _affiches.Count Then Exit Sub
        If DataGridViewAppareils.Columns(e.ColumnIndex).Name <> "ColPropose" Then Exit Sub
        Dim a As TuyaSlide.AppareilChoisi = _affiches(e.RowIndex)
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
        If e.RowIndex < 0 OrElse e.RowIndex >= _affiches.Count Then Exit Sub
        Dim colonne As String = DataGridViewAppareils.Columns(e.ColumnIndex).Name
        If colonne = "ColOptions" Then
            Await OuvrirLesOptions(_affiches(e.RowIndex))
            Exit Sub
        End If
        If colonne <> "ColOn" AndAlso colonne <> "ColOff" Then Exit Sub
        If TuyaSlide.EstUneScene(_affiches(e.RowIndex).Code) AndAlso colonne = "ColOff" Then Exit Sub
        Await TesterAppareil(_affiches(e.RowIndex), If(colonne = "ColOn", "true", "false"))
    End Sub

    ''' <summary>Bouton « Options... » d'une ligne : les reglages de l'appareil (curseur de luminosite, mode...), lus chez Tuya puis montres dans une fenetre.</summary>
    Private Async Function OuvrirLesOptions(a As TuyaSlide.AppareilChoisi) As Task
        If _essaiEnCours Then Exit Function
        _essaiEnCours = True
        Dim compte As TuyaSlide.Compte = _comptes.FirstOrDefault(Function(c) String.Equals(c.Nom, a.CompteNom, StringComparison.OrdinalIgnoreCase))
        If compte Is Nothing Then compte = _comptes.FirstOrDefault()
        Dim options As List(Of TuyaSlide.OptionTuya) = Nothing
        Try
            Etat("Lecture des options de « " & a.Nom & " »...")
            Cursor = Cursors.WaitCursor
            If compte Is Nothing OrElse Not compte.Renseigne Then
                Etat("Compte Tuya non renseigné.")
                Exit Function
            End If
            options = Await TuyaSlide.OptionsAsync(compte, a.Id, a.Code)
            Etat("")
        Catch ex As Exception
            Etat("Options de « " & a.Nom & " » : " & ex.Message)
            DiagLog("Tuya : options de « " & a.Nom & " » illisibles : " & ex.Message)
            Exit Function
        Finally
            Cursor = Cursors.Default
            _essaiEnCours = False
        End Try
        FormOptionsTuya.Montrer(FindForm(), a, _comptes, options)
    End Function

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
            If TuyaSlide.EstUneScene(a.Code) Then
                Etat(If(erreur = "", "Scénario « " & a.Nom & " » lancé.", "Échec du scénario « " & a.Nom & " » : " & erreur))
                Exit Try
            End If
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
        For Each a As TuyaSlide.AppareilChoisi In _affiches          ' (seulement ceux du tableau : le compte choisi, ou tous)
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

    ''' <summary>Les groupes sont ceux du compte choisi dans « Compte Tuya : » (tous les comptes si la case « tous les comptes » est cochee) ; un groupe appartient a un compte.</summary>
    Private Function TousLesComptesAffiches() As Boolean
        Return _comptes.Count <= 1 OrElse _courant Is Nothing OrElse CheckBoxTousLesComptes.Checked
    End Function

    ''' <summary>La maison choisie dans la liste « Maison : » ("" : toutes).</summary>
    Private Function MaisonChoisie() As String
        Return If(ComboBoxMaisons.Visible AndAlso ComboBoxMaisons.SelectedIndex > 0, CStr(ComboBoxMaisons.SelectedItem), "")
    End Function

    Private Sub AfficherLesGroupes(Optional selection As String = Nothing)
        Dim voulu As String = If(selection, If(GroupeChoisi() IsNot Nothing, GroupeChoisi().Nom, ""))
        _groupes = _groupes.OrderBy(Function(g) g.Nom, StringComparer.CurrentCultureIgnoreCase).ToList()
        Dim tous As Boolean = TousLesComptesAffiches()
        Dim maison As String = MaisonChoisie()
        _groupesAffiches = _groupes.Where(Function(g) (tous OrElse g.CompteNom = "" OrElse String.Equals(g.CompteNom, _courant.Nom, StringComparison.OrdinalIgnoreCase)) AndAlso
                                                     (maison = "" OrElse g.MaisonNom = "" OrElse String.Equals(g.MaisonNom, maison, StringComparison.CurrentCultureIgnoreCase))).ToList()
        ListBoxGroupes.Items.Clear()
        For Each g As TuyaSlide.GroupeTuya In _groupesAffiches
            ListBoxGroupes.Items.Add(g.Nom & If(tous AndAlso _comptes.Count > 1 AndAlso g.CompteNom <> "", "   - " & g.CompteNom, "") & If(maison = "" AndAlso g.MaisonNom <> "", "   (" & g.MaisonNom & ")", ""))
        Next
        Dim i As Integer = _groupesAffiches.FindIndex(Function(g) String.Equals(g.Nom, voulu, StringComparison.OrdinalIgnoreCase))
        If i >= 0 Then ListBoxGroupes.SelectedIndex = i
        AfficherLesMembres()
    End Sub

    Private Function GroupeChoisi() As TuyaSlide.GroupeTuya
        Dim i As Integer = ListBoxGroupes.SelectedIndex
        If i < 0 OrElse i >= _groupesAffiches.Count Then Return Nothing
        Return _groupesAffiches(i)
    End Function

    ''' <summary>Les appareils (avec interrupteur) a cocher pour le groupe selectionne : ceux de son compte.</summary>
    Private Sub AfficherLesMembres()
        _remplissage = True
        Try
            CheckedListBoxMembres.Items.Clear()
            Dim g As TuyaSlide.GroupeTuya = GroupeChoisi()
            Dim compteDuGroupe As String = If(g IsNot Nothing AndAlso g.CompteNom <> "", g.CompteNom, If(g Is Nothing AndAlso Not TousLesComptesAffiches(), _courant.Nom, ""))
            ' Les appareils proposes sont ceux de la maison du groupe (ou de la maison choisie quand aucun groupe ne l'est).
            Dim maisonDuGroupe As String = If(g IsNot Nothing, g.MaisonNom, MaisonChoisie())
            _membresAffiches = _appareils.Where(Function(a) a.Code <> "" AndAlso Not TuyaSlide.EstUneScene(a.Code) AndAlso (compteDuGroupe = "" OrElse String.Equals(a.CompteNom, compteDuGroupe, StringComparison.OrdinalIgnoreCase)) AndAlso
                                                (maisonDuGroupe = "" OrElse String.Equals(a.MaisonNom, maisonDuGroupe, StringComparison.CurrentCultureIgnoreCase))).ToList()
            For Each a As TuyaSlide.AppareilChoisi In _membresAffiches
                CheckedListBoxMembres.Items.Add(a.Nom & If(_comptes.Count > 1, "   - " & a.CompteNom, "") & If(a.MaisonNom <> "", "   (" & a.MaisonNom & ")", ""), g IsNot Nothing AndAlso g.Ids.Contains(a.Id))
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
        ' Le groupe appartient au compte choisi dans « Compte Tuya : » ; deux comptes peuvent avoir un groupe du meme nom.
        Dim compte As String = If(_courant IsNot Nothing, _courant.Nom, "")
        ' Et a la maison choisie dans la liste « Maison : » (obligatoire quand le compte a des maisons).
        Dim maison As String = MaisonChoisie()
        If maison = "" AndAlso ComboBoxMaisons.Visible AndAlso ComboBoxMaisons.Items.Count > 1 Then
            Etat("Choisissez d'abord une maison dans la liste « Maison : » : un groupe appartient à une maison.")
            Exit Sub
        End If
        If _groupes.Any(Function(g) String.Equals(g.Nom, nom, StringComparison.OrdinalIgnoreCase) AndAlso String.Equals(g.CompteNom, compte, StringComparison.OrdinalIgnoreCase) AndAlso
                                    String.Equals(g.MaisonNom, maison, StringComparison.CurrentCultureIgnoreCase)) Then
            Etat("Le groupe « " & nom & " » existe déjà" & If(maison <> "", " pour la maison « " & maison & " »", If(compte <> "", " pour le compte « " & compte & " »", "")) & ".")
            AfficherLesGroupes(nom)
            Exit Sub
        End If
        _groupes.Add(New TuyaSlide.GroupeTuya With {.Nom = nom, .CompteNom = compte, .MaisonNom = maison})
        AfficherLesGroupes(nom)
        Etat(If(_membresAffiches.Count = 0, "Groupe créé : chargez d'abord vos appareils pour les y mettre.", "Groupe créé : cochez ses appareils."))
    End Sub

    ''' <summary>Renomme le groupe selectionne (aussi par double-clic sur son nom) : ses appareils sont gardes, et les lignes de la minuterie (« Groupe Ancien ON » / « Groupe Ancien OFF ») suivent le
    ''' nouveau nom (Jacob, 26/10/2026 : « possibilite de renommer un nom de groupe Tuya »).</summary>
    Private Sub ButtonRenommerGroupe_Click(sender As Object, e As EventArgs) Handles ButtonRenommerGroupe.Click, ListBoxGroupes.DoubleClick
        Dim g As TuyaSlide.GroupeTuya = GroupeChoisi()
        If g Is Nothing Then
            Etat("Sélectionnez d'abord un groupe dans la liste.")
            Exit Sub
        End If
        Dim ancien As String = g.Nom
        Dim nom As String = Microsoft.VisualBasic.Interaction.InputBox("Nouveau nom du groupe « " & ancien & " » (dans la minuterie : « Groupe " & ancien & " ON »...) :", "Renommer le groupe Tuya", ancien).Trim().Replace("|", " ")
        If nom = "" OrElse nom = ancien Then Exit Sub
        If _groupes.Any(Function(x) x IsNot g AndAlso String.Equals(x.Nom, nom, StringComparison.OrdinalIgnoreCase) AndAlso String.Equals(x.CompteNom, g.CompteNom, StringComparison.OrdinalIgnoreCase) AndAlso String.Equals(x.MaisonNom, g.MaisonNom, StringComparison.CurrentCultureIgnoreCase)) Then
            Etat("Le groupe « " & nom & " » existe déjà.")
            Exit Sub
        End If
        g.Nom = nom
        Dim lignes As Integer = RenommerLeGroupeDansLaMinuterie(ancien, nom, g.CompteNom)
        AfficherLesGroupes(nom)
        Etat("Groupe « " & ancien & " » renommé « " & nom & " »" & If(lignes > 0, " (" & lignes & " ligne(s) de la minuterie mise(s) à jour).", "."))
    End Sub

    ''' <summary>Les lignes de minuterie de type Tuya qui nomment « Groupe Ancien ON / OFF » (seules ou parmi plusieurs actions separees par « ; », avec ou sans nom de compte devant) prennent le nouveau
    ''' nom. Rend le nombre de lignes changees.</summary>
    Private Shared Function RenommerLeGroupeDansLaMinuterie(ancien As String, nouveau As String, compteNom As String) As Integer
        Dim changees As Integer = 0
        Try
            Dim motif As New System.Text.RegularExpressions.Regex("Groupe " & System.Text.RegularExpressions.Regex.Escape(ancien) & "(?= (ON|OFF)\b)", System.Text.RegularExpressions.RegexOptions.IgnoreCase)
            For Each ligne As DataGridViewRow In Form4.DataGridView1.Rows.Cast(Of DataGridViewRow)().Concat(Form4.DataGridViewTuya.Rows.Cast(Of DataGridViewRow)()).ToList()
                If ligne.IsNewRow OrElse Not String.Equals(Convert.ToString(ligne.Cells("Type").FormattedValue), "Tuya", StringComparison.OrdinalIgnoreCase) Then Continue For
                ' Une ligne d'un autre compte (« !/Compte:Autre ») qui porte le meme nom de groupe n'est pas touchee.
                Dim compteLigne As String = Convert.ToString(Arg("Compte", Convert.ToString(ligne.Cells("Argument").Value))).Trim()
                If compteNom <> "" AndAlso compteLigne <> "" AndAlso Not String.Equals(compteLigne, compteNom, StringComparison.OrdinalIgnoreCase) Then Continue For
                Dim action As String = Convert.ToString(ligne.Cells("Action").Value)
                Dim neuve As String = motif.Replace(action, "Groupe " & nouveau.Replace("$", "$$"))
                If neuve = action Then Continue For
                ListeDesActionsDuType(ligne.Index, "Tuya", neuve)                  ' (la liste des actions de la ligne reprend le nouveau nom)
                ligne.Cells("Action").Value = neuve
                changees += 1
            Next
        Catch ex As Exception
            DiagLog("Tuya : renommage du groupe dans la minuterie : " & ex.Message)
        End Try
        Return changees
    End Function

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
            TuyaSlide.FournisseurDAppareils = Nothing
            TuyaSlide.FournisseurActif = Nothing
        End If
    End Sub

    ' Reglages fermes : la minuterie relit les reglages enregistres.
    Private Sub ControlTuya_Disposed(sender As Object, e As EventArgs) Handles Me.Disposed
        TuyaSlide.FournisseurDeCommandes = Nothing
        TuyaSlide.FournisseurDeComptes = Nothing
        TuyaSlide.FournisseurDAppareils = Nothing
        TuyaSlide.FournisseurActif = Nothing
    End Sub

End Class
