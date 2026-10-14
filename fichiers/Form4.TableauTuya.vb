Option Strict On
Option Explicit On

''' <summary>
''' Minuterie optionnelle (29/10/2026) : un second tableau des taches, JUMEAU du tableau principal (memes colonnes, memes menus, memes fonctions : DataGridViewTuya). Voir TableauTuya.vb.
'''
''' Tout le code des tableaux de la minuterie ecrit pour DataGridView1 travaille sur « le tableau courant » (GrilleCourante) : celui dont vient l'evenement, ou celui sur lequel on a cliqué en dernier
''' (menus, boutons + et -, fenetres de choix...). Chaque ligne reste dans son tableau : rien n'est cache ni deplace. L'interrupteur ON/OFF coupe les lignes de ce tableau dans la minuterie.
''' Les controles sont dans le Concepteur.
''' </summary>
Partial Class Form4

    Private _grilleCourante As DataGridView

    ''' <summary>Le tableau de la minuterie sur lequel on travaille : le principal (DataGridView1) ou la minuterie optionnelle (DataGridViewTuya).</summary>
    Friend Property GrilleCourante As DataGridView
        Get
            Return If(_grilleCourante, DataGridView1)
        End Get
        Set(valeur As DataGridView)
            _grilleCourante = valeur
        End Set
    End Property

    Private Sub GrilleActivee(sender As Object, e As EventArgs) Handles DataGridView1.Enter, DataGridViewTuya.Enter
        GrilleCourante = DirectCast(sender, DataGridView)
    End Sub

    Private Sub GrilleTouchee(sender As Object, e As MouseEventArgs) Handles DataGridView1.MouseDown, DataGridViewTuya.MouseDown
        GrilleCourante = DirectCast(sender, DataGridView)
    End Sub

    ''' <summary>Vrai tant que la fenetre n'est pas initialisee : la case ON/OFF est cochee par le Concepteur a la creation de la fenetre, ce qui ne doit pas ecraser le reglage enregistre (il revenait toujours a ON).</summary>
    Private _tuyaInitialisation As Boolean = True

    ''' <summary>Largeurs des colonnes, listes (jours, types, volumes), hauteur de ligne et interrupteur ON/OFF, a l'ouverture de la fenetre.</summary>
    Private Sub InitialiserLeTableauTuya()
        Dim lt As Double = DataGridViewTuya.Width - DataGridViewTuya.RowHeadersWidth - DataGridViewTuya.ColumnHeadersHeight
        DataGridViewTuya.Columns("Actif").Width = CInt(lt * 30 / 784.5)
        DataGridViewTuya.Columns("Jour").Width = CInt(lt * 80 / 784.5)
        DataGridViewTuya.Columns("Heure").Width = CInt(lt * 50 / 784.5)
        DataGridViewTuya.Columns("Type").Width = CInt(lt * 88 / 784.5)
        DataGridViewTuya.Columns("Action").Width = CInt(lt * 282.5 / 784.5)
        DataGridViewTuya.Columns("Play").Width = CInt(lt * 160 / 784.5)
        DataGridViewTuya.Columns("Durée").Width = CInt(lt * 50 / 784.5)
        DataGridViewTuya.Columns("Volume").Width = CInt(lt * 45 / 784.5)

        For Each nom As String In {"Jour", "Type", "Volume"}
            Dim source As DataGridViewComboBoxColumn = DirectCast(DataGridView1.Columns(nom), DataGridViewComboBoxColumn)
            Dim cible As DataGridViewComboBoxColumn = DirectCast(DataGridViewTuya.Columns(nom), DataGridViewComboBoxColumn)
            cible.Items.Clear()
            For Each element As Object In source.Items
                cible.Items.Add(element)
            Next
        Next
        DirectCast(DataGridViewTuya.Columns("Volume"), DataGridViewComboBoxColumn).ToolTipText = DirectCast(DataGridView1.Columns("Volume"), DataGridViewComboBoxColumn).ToolTipText
        AjusteHauteurDesLignes(DataGridViewTuya)

        _tuyaInitialisation = True
        CheckBoxTuyaMarche.Checked = TableauTuya.EstEnMarche()
        CheckBoxPrincipaleMarche.Checked = TableauTuya.PrincipaleEnMarche()
        LabelMinuteriePrincipale.Text = If(CheckBoxPrincipaleMarche.Checked, "Minuterie principale", "Minuterie principale (OFF)")
        LabelTableauTuya.Text = If(CheckBoxTuyaMarche.Checked, "Minuterie optionnelle", "Minuterie optionnelle (OFF)")
        _tuyaInitialisation = False
        LirePartsDesTableaux()
        SynchroniserLesLargeursInactifs()
    End Sub

    ''' <summary>Parts des trois tableaux, propres a chaque poste (data\settings\HauteursTableauxMinuterie.txt, "0.4;0.4", point decimal), exportees avec les reglages.</summary>
    Private Shared Function FichierPartsTableaux() As String
        Return IO.Path.Combine(Convert.ToString(AppPath), "data", "settings", "HauteursTableauxMinuterie.txt")
    End Function

    Private Sub LirePartsDesTableaux()
        Try
            If Not IO.File.Exists(FichierPartsTableaux()) Then Exit Sub
            Dim morceaux As String() = IO.File.ReadAllText(FichierPartsTableaux()).Trim().Split(";"c)
            Dim p1, p2 As Double
            If morceaux.Length = 2 AndAlso Double.TryParse(morceaux(0), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, p1) _
               AndAlso Double.TryParse(morceaux(1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, p2) _
               AndAlso p1 > 0.05 AndAlso p2 > 0.05 AndAlso p1 + p2 < 0.95 Then
                _partTableau1 = p1
                _partTableau2 = p2
            End If
        Catch ex As Exception
            DiagLog("Form4 : lecture des hauteurs des tableaux impossible (" & ex.Message & ")")
        End Try
    End Sub

    Private Sub EcrirePartsDesTableaux()
        Try
            IO.File.WriteAllText(FichierPartsTableaux(), _partTableau1.ToString("0.####", Globalization.CultureInfo.InvariantCulture) & ";" & _partTableau2.ToString("0.####", Globalization.CultureInfo.InvariantCulture))
        Catch ex As Exception
            DiagLog("Form4 : ecriture des hauteurs des tableaux impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>Le tableau des inactifs reprend EXACTEMENT les largeurs de colonne du tableau principal (30/10/2026), au chargement et quand une colonne du principal est elargie a la souris.</summary>
    Private Sub SynchroniserLesLargeursInactifs()
        Try
            For Each suivant As DataGridView In {DataGridViewTuya, DataGridView2}
                If suivant.RowHeadersWidth <> DataGridView1.RowHeadersWidth Then suivant.RowHeadersWidth = DataGridView1.RowHeadersWidth          ' (la marge a gauche : toute premiere colonne)
                For c As Integer = 0 To Math.Min(7, Math.Min(DataGridView1.ColumnCount, suivant.ColumnCount) - 1)
                    If suivant.Columns(c).Width <> DataGridView1.Columns(c).Width Then suivant.Columns(c).Width = DataGridView1.Columns(c).Width
                Next
            Next
        Catch ex As Exception
            DiagLog("Form4 : largeurs des colonnes des inactifs impossibles (" & ex.Message & ")")
        End Try
    End Sub

    Private Sub LargeurDuPrincipalChangee(sender As Object, e As DataGridViewColumnEventArgs) Handles DataGridView1.ColumnWidthChanged
        If Not _tuyaInitialisation Then SynchroniserLesLargeursInactifs()
    End Sub

    Private Sub MargeDuPrincipalChangee(sender As Object, e As EventArgs) Handles DataGridView1.RowHeadersWidthChanged
        If Not _tuyaInitialisation Then SynchroniserLesLargeursInactifs()
    End Sub

    ''' <summary>La fenetre ne depasse jamais la zone utile de l'ecran (sinon les boutons OK / Annuler / Quitter, en bas, sont hors de l'ecran) : elle est raccourcie de ce qui depasse,
    ''' les boutons suivent le bas (ancrages) et les trois tableaux de la minuterie se partagent la hauteur restante.</summary>
    Private Sub AjusterAuxEcran(sender As Object, e As EventArgs) Handles Me.Shown
        Try
            Dim ecran As Rectangle = Screen.FromControl(Me).WorkingArea
            Dim exces As Integer = Me.Bottom - ecran.Bottom
            If exces > 0 Then
                Me.ClientSize = New Size(Me.ClientSize.Width, Me.ClientSize.Height - exces)
                Me.Top = Math.Max(ecran.Top, ecran.Top + (ecran.Height - Me.Height) \ 2)
                DiagLog("Form4 : fenetre raccourcie de " & exces & " px pour tenir dans l'ecran (zone utile " & ecran.Height & " px)")
            End If
            PlacerLesBoutonsDuBas()
            RedisposerLaMinuterie()
        Catch ex As Exception
            DiagLog("Form4 : ajustement a l'ecran impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>OK, Annuler, Quitter... doivent toujours etre DANS la fenetre : sur un petit ecran (autre PC, 30/10/2026) la fenetre est raccourcie au chargement
    ''' mais les boutons gardaient leur place d'origine (bas du bouton Quitter a 921 pour une fenetre de 789) : ils sortaient de la fenetre. Ils sont donc replaces
    ''' le long du bas, et les onglets s'arretent au-dessus d'eux.</summary>
    Private Sub PlacerLesBoutonsDuBas()
        Dim boutons As Button() = {Button3, Button4, Button20, Button24}
        Dim marge As Integer = CInt(Math.Round(8 * DeviceDpi / 96.0))
        Dim ecart As Integer = CInt(Math.Round(6 * DeviceDpi / 96.0))
        Dim bas As Integer = boutons.Max(Function(b) b.Bottom)
        Dim cible As Integer = ClientSize.Height - marge
        If bas > cible Then
            Dim decalage As Integer = cible - bas
            For Each b As Button In boutons
                b.Top += decalage
            Next
            DiagLog("Form4 : boutons du bas remontes de " & -decalage & " px (fenetre " & ClientSize.Height & " px)")
        End If
        ' Idem a droite (autre PC, 02/11/2026 : fenetre de 1018 px de large, OK et Annuler restes a leur place d'origine, hors de la fenetre).
        Dim droits As Button() = {Button3, Button4}
        Dim ecartDroit As Integer = ClientSize.Width - marge - droits.Max(Function(b) b.Right)
        If ecartDroit < 0 Then
            For Each b As Button In droits
                b.Left += ecartDroit
            Next
            DiagLog("Form4 : boutons OK / Annuler ramenes de " & -ecartDroit & " px vers la gauche (fenetre " & ClientSize.Width & " px de large)")
        End If
        Dim hautDesBoutons As Integer = boutons.Min(Function(b) b.Top)
        If TabControl1.Bottom > hautDesBoutons - ecart Then TabControl1.Height = Math.Max(300, hautDesBoutons - ecart - TabControl1.Top)
    End Sub

    Private Sub TabPageMinuterie_SizeChanged(sender As Object, e As EventArgs) Handles TabPage5.SizeChanged
        RedisposerLaMinuterie()
    End Sub

    ' Parts de la hauteur de chaque tableau (principal, minuterie optionnelle) ; les inactifs prennent le reste. Modifiees en tirant les poignees.
    Private _partTableau1 As Double = 0.4
    Private _partTableau2 As Double = 0.4
    Private _hauteurDisponible As Integer
    Private _barreEnCours As Panel

    ''' <summary>Les trois tableaux de la minuterie (par defaut 40 %, 40 %, 20 %) se partagent la hauteur de l'onglet, titres compris. Les poignees entre eux changent les parts.</summary>
    Private Sub RedisposerLaMinuterie()
        Try
            If DataGridView1 Is Nothing OrElse DataGridViewTuya Is Nothing OrElse DataGridView2 Is Nothing Then Exit Sub
            Dim marge As Integer = CInt(Math.Round(12 * DeviceDpi / 96.0))
            Dim ecart As Integer = CInt(Math.Round(6 * DeviceDpi / 96.0))
            Dim titre As Integer = Math.Max(LabelTableauTuya.Height, Math.Max(CheckBoxTuyaMarche.Height, Label13.Height))
            Dim disponible As Integer = TabPage5.ClientSize.Height - marge - DataGridView1.Top - 2 * (titre + 2 * ecart)
            If disponible < 150 Then Exit Sub
            _hauteurDisponible = disponible
            Dim h1 As Integer = Math.Max(HauteurMiniTableau, CInt(disponible * _partTableau1))
            Dim h2 As Integer = Math.Max(HauteurMiniTableau, CInt(disponible * _partTableau2))
            LabelMinuteriePrincipale.Top = DataGridView1.Top - titre - ecart
            CheckBoxPrincipaleMarche.Top = LabelMinuteriePrincipale.Top - CInt(Math.Round(2 * DeviceDpi / 96.0))
            CheckBoxPrincipaleMarche.Left = CheckBoxTuyaMarche.Left
            DataGridView1.Height = h1
            LabelTableauTuya.Top = DataGridView1.Bottom + ecart
            CheckBoxTuyaMarche.Top = LabelTableauTuya.Top - CInt(Math.Round(2 * DeviceDpi / 96.0))
            DataGridViewTuya.Top = LabelTableauTuya.Top + titre + ecart
            DataGridViewTuya.Height = h2
            Label13.Top = DataGridViewTuya.Bottom + ecart
            DataGridView2.Top = Label13.Top + titre + ecart
            DataGridView2.Height = Math.Max(60, TabPage5.ClientSize.Height - marge - DataGridView2.Top)
            PlacerLesPoignees(ecart)
            ' Pendant le glissement, l'ancien emplacement des tableaux et des titres n'etait pas repeint (traces) : on force le rafraichissement.
            If _barreEnCours IsNot Nothing Then TabPage5.Invalidate(True) : TabPage5.Update()
        Catch ex As Exception
            DiagLog("Form4 : disposition des tableaux de la minuterie impossible (" & ex.Message & ")")
        End Try
    End Sub

    Private ReadOnly Property HauteurMiniTableau As Integer
        Get
            Return CInt(Math.Round(60 * DeviceDpi / 96.0))
        End Get
    End Property

    ''' <summary>Les poignees occupent l'espace entre le bas d'un tableau et le titre du suivant.</summary>
    Private Sub PlacerLesPoignees(ecart As Integer)
        BarreTableauA.SetBounds(DataGridView1.Left, DataGridView1.Bottom, DataGridView1.Width, ecart)
        BarreTableauB.SetBounds(DataGridViewTuya.Left, DataGridViewTuya.Bottom, DataGridViewTuya.Width, ecart)
        BarreTableauA.BringToFront()
        BarreTableauB.BringToFront()
    End Sub

    Private Sub BarreTableau_MouseDown(sender As Object, e As MouseEventArgs) Handles BarreTableauA.MouseDown, BarreTableauB.MouseDown
        If e.Button = MouseButtons.Left Then
            _barreEnCours = DirectCast(sender, Panel)
            _barreEnCours.Capture = True
        End If
    End Sub

    ''' <summary>Poignee A : le tableau principal grandit et les deux autres se reduisent (ou l'inverse), en gardant leur proportion.
    ''' Poignee B : la minuterie optionnelle grandit aux depens des inactifs (ou l'inverse).</summary>
    Private Sub BarreTableau_MouseMove(sender As Object, e As MouseEventArgs) Handles BarreTableauA.MouseMove, BarreTableauB.MouseMove
        If _barreEnCours Is Nothing OrElse _hauteurDisponible < 150 Then Exit Sub
        Dim mini As Double = HauteurMiniTableau / CDbl(_hauteurDisponible)
        Dim y As Integer = TabPage5.PointToClient(Cursor.Position).Y
        If _barreEnCours Is BarreTableauA Then
            Dim p1 As Double = Math.Min(1 - 2 * mini, Math.Max(mini, (y - DataGridView1.Top) / CDbl(_hauteurDisponible)))
            Dim p3 As Double = Math.Max(mini, 1 - _partTableau1 - _partTableau2)
            Dim autres As Double = _partTableau2 + p3
            _partTableau2 = (1 - p1) * _partTableau2 / autres
            _partTableau1 = p1
        Else
            Dim p2 As Double = Math.Min(1 - _partTableau1 - mini, Math.Max(mini, (y - DataGridViewTuya.Top) / CDbl(_hauteurDisponible)))
            _partTableau2 = p2
        End If
        RedisposerLaMinuterie()
    End Sub

    Private Sub BarreTableau_MouseUp(sender As Object, e As MouseEventArgs) Handles BarreTableauA.MouseUp, BarreTableauB.MouseUp
        If _barreEnCours IsNot Nothing Then
            _barreEnCours.Capture = False
            EcrirePartsDesTableaux()
        End If
        _barreEnCours = Nothing
    End Sub

    ''' <summary>L'interrupteur ON/OFF de la minuterie principale : OFF, la minuterie tourne sans les lignes du tableau principal (voir TableauTuya.Ignoree).</summary>
    Private Sub CheckBoxPrincipaleMarche_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxPrincipaleMarche.CheckedChanged
        If _tuyaInitialisation Then Exit Sub
        TableauTuya.FixerMarchePrincipale(CheckBoxPrincipaleMarche.Checked)
        LabelMinuteriePrincipale.Text = If(CheckBoxPrincipaleMarche.Checked, "Minuterie principale", "Minuterie principale (OFF)")
    End Sub

    ''' <summary>L'interrupteur ON/OFF de la minuterie optionnelle : OFF, la minuterie tourne sans ses lignes (voir Module1.Minuterie).</summary>
    Private Sub CheckBoxTuyaMarche_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBoxTuyaMarche.CheckedChanged
        If _tuyaInitialisation Then Exit Sub
        TableauTuya.FixerMarche(CheckBoxTuyaMarche.Checked)
        LabelTableauTuya.Text = If(CheckBoxTuyaMarche.Checked, "Minuterie optionnelle", "Minuterie optionnelle (OFF)")
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Copier / coller entre les trois tableaux
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Le tampon du copier-coller vient du tableau de la minuterie optionnelle (ses lignes gardent leur marque si on les colle parmi les inactives).</summary>
    Private _tamponDeLaMinuterieOptionnelle As Boolean

    Private Sub CopierInactifToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CopierInactifToolStripMenuItem.Click
        Dim lignes As New List(Of DataGridViewRow)
        For Each ligne As DataGridViewRow In DataGridView2.SelectedRows
            If Not ligne.IsNewRow Then lignes.Add(ligne)
        Next
        If lignes.Count = 0 AndAlso _ligneInactiveDuMenu >= 0 AndAlso _ligneInactiveDuMenu < DataGridView2.Rows.Count Then lignes.Add(DataGridView2.Rows(_ligneInactiveDuMenu))
        lignes.Sort(Function(a, b) a.Index.CompareTo(b.Index))
        If lignes.Count = 0 Then Beep() : Exit Sub
        Dim copies As New List(Of Object())
        Dim infobulles As New List(Of String())
        For Each source As DataGridViewRow In lignes
            Dim valeurs(source.Cells.Count - 1) As Object
            Dim bulles(source.Cells.Count - 1) As String
            For c As Integer = 0 To source.Cells.Count - 1
                valeurs(c) = source.Cells(c).Value
                bulles(c) = source.Cells(c).ToolTipText
            Next
            copies.Add(valeurs)
            infobulles.Add(bulles)
        Next
        _lignesCopiees = copies
        _infobullesCopiees = infobulles
        _tamponDeLaMinuterieOptionnelle = TableauTuya.EstDuTableau("", copies(0)(copies(0).Length - 1))       ' (une ligne inactive qui vient de la minuterie optionnelle en porte la marque)
    End Sub

    Private Sub CollerInactifToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles CollerInactifToolStripMenuItem.Click
        Try
            If _lignesCopiees.Count = 0 Then Beep() : Exit Sub
            For i As Integer = 0 To _lignesCopiees.Count - 1
                If _lignesCopiees(i).Length <> DataGridView2.ColumnCount Then Continue For
                Dim valeurs As Object() = CType(_lignesCopiees(i).Clone(), Object())
                valeurs(0) = False
                Dim dernier As Integer = valeurs.Length - 1
                valeurs(dernier) = TableauTuya.ArgumentAvecMarque(Convert.ToString(valeurs(dernier)), _tamponDeLaMinuterieOptionnelle)
                DataGridView2.Rows.Add(valeurs)
            Next
            DataGridView2.Sort(DataGridView2.Columns("tri2"), System.ComponentModel.ListSortDirection.Ascending)
        Catch ex As Exception
            DiagLog("Minuterie, coller parmi les inactives : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Les reglages d'une ligne qui passe parmi les inactives : ils gardent d'ou elle vient (la marque de la minuterie optionnelle, ou « Principale » pour une ligne Tuya du tableau principal).</summary>
    Private Shared Function ArgumentPourLInactif(ligne As DataGridViewRow, grille As DataGridView) As String
        Dim typeTuya As Boolean = String.Equals(Convert.ToString(ligne.Cells("Type").Value).Trim(), TuyaSlide.TypeMinuterie, StringComparison.OrdinalIgnoreCase)
        Return TableauTuya.ArgumentAvecMarque(Convert.ToString(ligne.Cells("Argument").FormattedValue), grille.Name = "DataGridViewTuya", typeTuya)
    End Function

    ''' <summary>Le tableau ou une ligne inactive revient quand on la reactive : la minuterie optionnelle si elle en vient, sinon le tableau principal.</summary>
    Private Function GrilleDeLaLigneInactive(ligne As DataGridViewRow) As DataGridView
        Return If(TableauTuya.EstDuTableau(ligne.Cells("Type2").Value, ligne.Cells("Argument2").Value), DataGridViewTuya, DataGridView1)
    End Function

    ''' <summary>Les reglages d'une ligne copiee, sans la marque de la minuterie optionnelle (la ligne prend celle du tableau ou elle est collee).</summary>
    Private Shared Function ArgumentCollable(valeur As Object) As String
        Return TableauTuya.ArgumentAvecMarque(Convert.ToString(valeur), False)
    End Function

End Class
