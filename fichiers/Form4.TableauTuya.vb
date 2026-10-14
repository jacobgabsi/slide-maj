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
        LabelTableauTuya.Text = If(CheckBoxTuyaMarche.Checked, "Minuterie optionnelle", "Minuterie optionnelle (OFF)")
        _tuyaInitialisation = False
        SynchroniserLesLargeursInactifs()
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
        Dim hautDesBoutons As Integer = boutons.Min(Function(b) b.Top)
        If TabControl1.Bottom > hautDesBoutons - ecart Then TabControl1.Height = Math.Max(300, hautDesBoutons - ecart - TabControl1.Top)
    End Sub

    Private Sub TabPageMinuterie_SizeChanged(sender As Object, e As EventArgs) Handles TabPage5.SizeChanged
        RedisposerLaMinuterie()
    End Sub

    ''' <summary>Les trois tableaux de la minuterie (principal 40 %, minuterie optionnelle 40 %, inactifs 20 %) se partagent la hauteur de l'onglet, titres compris.</summary>
    Private Sub RedisposerLaMinuterie()
        Try
            If DataGridView1 Is Nothing OrElse DataGridViewTuya Is Nothing OrElse DataGridView2 Is Nothing Then Exit Sub
            Dim marge As Integer = CInt(Math.Round(12 * DeviceDpi / 96.0))
            Dim ecart As Integer = CInt(Math.Round(6 * DeviceDpi / 96.0))
            Dim titre As Integer = Math.Max(LabelTableauTuya.Height, Math.Max(CheckBoxTuyaMarche.Height, Label13.Height))
            Dim disponible As Integer = TabPage5.ClientSize.Height - marge - DataGridView1.Top - 2 * (titre + 2 * ecart)
            If disponible < 150 Then Exit Sub
            DataGridView1.Height = CInt(disponible * 0.4)
            LabelTableauTuya.Top = DataGridView1.Bottom + ecart
            CheckBoxTuyaMarche.Top = LabelTableauTuya.Top - CInt(Math.Round(2 * DeviceDpi / 96.0))
            DataGridViewTuya.Top = LabelTableauTuya.Top + titre + ecart
            DataGridViewTuya.Height = CInt(disponible * 0.4)
            Label13.Top = DataGridViewTuya.Bottom + ecart
            DataGridView2.Top = Label13.Top + titre + ecart
            DataGridView2.Height = Math.Max(60, TabPage5.ClientSize.Height - marge - DataGridView2.Top)
        Catch ex As Exception
            DiagLog("Form4 : disposition des tableaux de la minuterie impossible (" & ex.Message & ")")
        End Try
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
