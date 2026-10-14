Option Strict On
Option Explicit On

''' <summary>
''' L'allure "professionnelle" commune a toutes les fenetres de Slide (demande de Jacob,
''' 09/10/2026 : "arrange toutes les forms au format professionnel") : la meme que celle des
''' fenetres deja refaites (message, verifications, fermeture, telecommande) - fond sombre,
''' texte clair, boutons plats, bouton principal en bleu.
'''
''' Deux etages :
'''  - le mode sombre de .NET (My.Application, ApplyApplicationDefaults) pour tout ce que Windows
'''    dessine lui-meme : barre de titre, onglets, listes deroulantes, barres de defilement, menus ;
'''  - Appliquer(fenetre), appele au chargement de chaque fenetre, pour le reste : fonds, boutons
'''    plats, tableaux, zones de saisie.
''' Les fenetres restent telles quelles dans le Concepteur : rien n'y est change par ce module.
''' Une couleur choisie expres dans le Concepteur ou par le code (bouton noir ou gris de la
''' minuterie, texte colore...) n'est jamais remplacee.
'''
''' Case "Apparence professionnelle (sombre)" de l'onglet Divers (My.Settings.ThemePro) : decochee,
''' Slide reprend l'apparence claire de Windows au lancement suivant.
''' </summary>
Friend Module StylePro

    Friend ReadOnly Fond As Color = Color.FromArgb(28, 28, 30)
    Friend ReadOnly Surface As Color = Color.FromArgb(44, 44, 46)
    Friend ReadOnly Bord As Color = Color.FromArgb(58, 58, 60)
    Friend ReadOnly Texte As Color = Color.FromArgb(245, 245, 247)
    Friend ReadOnly Secondaire As Color = Color.FromArgb(152, 152, 157)
    Friend ReadOnly Accent As Color = Color.FromArgb(10, 132, 255)
    Private ReadOnly _bouton As Color = Color.FromArgb(58, 58, 62)
    ''' <summary>Couleur d'un bouton ordinaire (pour les boutons que le code colore lui-meme).</summary>
    Friend ReadOnly Property BoutonNeutre As Color
        Get
            Return _bouton
        End Get
    End Property
    Private ReadOnly _boutonSurvol As Color = Color.FromArgb(74, 74, 78)
    Private ReadOnly _boutonAppui As Color = Color.FromArgb(84, 84, 88)
    Private ReadOnly _danger As Color = Color.FromArgb(120, 44, 44)

    ''' <summary>Vrai si l'apparence professionnelle est en service (reglage lu au lancement).</summary>
    Friend ReadOnly Property Actif As Boolean
        Get
            Return _actif
        End Get
    End Property
    Private _actif As Boolean = True

    ''' <summary>Au lancement de Slide, avant toute fenetre : retient le reglage.</summary>
    Friend Sub Regler(actif As Boolean)
        _actif = actif
    End Sub

    ''' <summary>Donne a <paramref name="fenetre"/> l'allure commune. Sans effet si le reglage est decoche.</summary>
    Friend Sub Appliquer(fenetre As Form)
        If Not _actif OrElse fenetre Is Nothing Then Exit Sub
        Try
            fenetre.BackColor = Fond
            fenetre.ForeColor = Texte
            Habiller(fenetre, fenetre)
            ' Trace pour comprendre un affichage different d'un PC a l'autre (14/10/2026, "on ne voit pas bien les textes").
            Dim actives As Integer = 0, windows As Integer = 0
            For Each c As CheckBox In ToutesLesCases(fenetre)
                If c.FlatStyle = FlatStyle.System Then windows += 1 Else actives += 1
            Next
            If actives + windows > 0 Then
                DiagLog("Apparence : " & fenetre.Name & " : " & (actives + windows) & " case(s) (" & actives & " a plat, " & windows & " dessinee(s) par Windows) ; Windows " &
                        Environment.OSVersion.Version.ToString() & " ; Slide " & SlideVersion & " ; echelle " & fenetre.DeviceDpi & " ppp")
            End If
        Catch ex As Exception
            DiagLog("Style (" & fenetre.Name & ") : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Vrai si la couleur de fond du controle a ete choisie expres (Concepteur ou code).</summary>
    Private Function FondChoisi(c As Control) As Boolean
        Try
            If Not ComponentModel.TypeDescriptor.GetProperties(c)("BackColor").ShouldSerializeValue(c) Then Return False
            ' Un blanc ou un gris clair pose dans le Concepteur n'est pas un choix : c'etait le fond ordinaire
            ' d'avant (zone de texte blanche, panneau gris...). Les boutons, eux, sont juges a part.
            If TypeOf c Is Button Then Return True
            Dim f As Color = c.BackColor
            Return Not (f.A = 255 AndAlso Math.Abs(CInt(f.R) - f.G) < 12 AndAlso Math.Abs(CInt(f.G) - f.B) < 12 AndAlso f.R >= 150)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function EncreChoisie(c As Control) As Boolean
        Try
            Return ComponentModel.TypeDescriptor.GetProperties(c)("ForeColor").ShouldSerializeValue(c)
        Catch ex As Exception
            Return False
        End Try
    End Function

    Private Function ToutesLesCases(parent As Control) As List(Of CheckBox)
        Dim liste As New List(Of CheckBox)
        For Each c As Control In parent.Controls
            If TypeOf c Is CheckBox AndAlso DirectCast(c, CheckBox).Appearance = Appearance.Normal Then liste.Add(DirectCast(c, CheckBox))
            liste.AddRange(ToutesLesCases(c))
        Next
        Return liste
    End Function

    Private ReadOnly _casesSuivies As New System.Runtime.CompilerServices.ConditionalWeakTable(Of Control, Object)

    ''' <summary>Active : dessinee a plat par .NET (texte clair) ; desactivee : dessinee par Windows (texte grise).</summary>
    Private Sub StyleDeLaCase(b As ButtonBase)
        Dim voulu As FlatStyle = If(b.Enabled, FlatStyle.Flat, FlatStyle.System)
        If b.FlatStyle <> voulu Then b.FlatStyle = voulu
    End Sub

    ''' <summary>Rechange le style d'une case quand elle (ou son groupe) est activee ou desactivee.</summary>
    Private Sub SuivreLActivation(b As ButtonBase)
        Dim dejaLa As Object = Nothing
        If _casesSuivies.TryGetValue(b, dejaLa) Then Exit Sub
        _casesSuivies.Add(b, New Object())
        AddHandler b.EnabledChanged, Sub(expediteur As Object, e As EventArgs) StyleDeLaCase(DirectCast(expediteur, ButtonBase))
    End Sub

    Private Sub Habiller(parent As Control, fenetre As Form)
        For Each c As Control In parent.Controls
            If TypeOf c Is Button Then
                HabillerBouton(DirectCast(c, Button), fenetre)
            ElseIf TypeOf c Is TabControl Then
                For Each page As TabPage In DirectCast(c, TabControl).TabPages
                    page.UseVisualStyleBackColor = False
                    page.BackColor = Fond
                    page.ForeColor = Texte
                    Habiller(page, fenetre)
                Next
            ElseIf TypeOf c Is GroupBox OrElse TypeOf c Is Panel OrElse TypeOf c Is SplitContainer OrElse TypeOf c Is SplitterPanel Then
                If Not FondChoisi(c) Then c.BackColor = Fond
                If Not EncreChoisie(c) Then c.ForeColor = Texte
                Habiller(c, fenetre)
            ElseIf TypeOf c Is CheckBox OrElse TypeOf c Is RadioButton Then
                ' Dessines par Windows : desactives, leur texte est grise proprement (dessines par .NET, il
                ' prenait un relief clair, illisible sur fond sombre).
                Dim bouton As ButtonBase = DirectCast(c, ButtonBase)
                Dim enBouton As Boolean = (TypeOf c Is CheckBox AndAlso DirectCast(c, CheckBox).Appearance = Appearance.Button) OrElse
                                          (TypeOf c Is RadioButton AndAlso DirectCast(c, RadioButton).Appearance = Appearance.Button)
                If TypeOf c Is RadioButton AndAlso Not enBouton Then
                    ' (Un bouton radio dessine par Windows garde un texte noir en mode sombre : il est dessine a plat.)
                    bouton.FlatStyle = FlatStyle.Flat
                    If Not EncreChoisie(c) Then c.ForeColor = Texte
                ElseIf Not enBouton AndAlso bouton.Image Is Nothing AndAlso bouton.BackgroundImage Is Nothing AndAlso Not EncreChoisie(c) AndAlso Not FondChoisi(c) Then
                    ' Case a cocher : texte clair quel que soit le PC (14/10/2026, "dans l'autre PC on ne voit pas bien les
                    ' textes" : dessine par Windows, le texte est gris fonce sur certains PC). Active : dessinee par .NET (sa
                    ' couleur est respectee) ; desactivee : dessinee par Windows, qui la grise proprement.
                    c.ForeColor = Texte
                    StyleDeLaCase(bouton)
                    SuivreLActivation(bouton)
                ElseIf Not EncreChoisie(c) Then
                    c.ForeColor = Texte
                End If
            ElseIf TypeOf c Is Label Then
                If Not EncreChoisie(c) Then c.ForeColor = Texte
                If Not FondChoisi(c) AndAlso c.BackColor <> Color.Transparent Then c.BackColor = If(c.Parent IsNot Nothing, c.Parent.BackColor, Fond)
            ElseIf TypeOf c Is TextBoxBase Then
                If Not FondChoisi(c) Then c.BackColor = Surface
                If Not EncreChoisie(c) Then c.ForeColor = Texte
                If TypeOf c Is TextBox Then DirectCast(c, TextBox).BorderStyle = BorderStyle.FixedSingle
            ElseIf TypeOf c Is ComboBox Then
                Dim liste As ComboBox = DirectCast(c, ComboBox)
                liste.FlatStyle = FlatStyle.Flat
                If Not FondChoisi(c) Then c.BackColor = Surface
                If Not EncreChoisie(c) Then c.ForeColor = Texte
            ElseIf TypeOf c Is ListView Then
                ' (Le quadrillage d'une liste est blanc, impossible a colorer : trop dur sur fond sombre.)
                DirectCast(c, ListView).GridLines = False
                If Not FondChoisi(c) Then c.BackColor = Surface
                If Not EncreChoisie(c) Then c.ForeColor = Texte
            ElseIf TypeOf c Is NumericUpDown OrElse TypeOf c Is ListBox OrElse TypeOf c Is TreeView OrElse TypeOf c Is DateTimePicker Then
                If Not FondChoisi(c) Then c.BackColor = Surface
                If Not EncreChoisie(c) Then c.ForeColor = Texte
            ElseIf TypeOf c Is TrackBar Then
                c.BackColor = If(c.Parent IsNot Nothing, c.Parent.BackColor, Fond)
            ElseIf TypeOf c Is DataGridView Then
                HabillerTableau(DirectCast(c, DataGridView))
            End If
        Next
    End Sub

    ''' <summary>Bouton plat. Le bouton de validation de la fenetre (ou "OK") est en bleu, "Quitter le
    ''' programme" en rouge sombre. Un bouton dont la couleur a ete choisie expres est laisse tel quel.</summary>
    Private Sub HabillerBouton(b As Button, fenetre As Form)
        ' Un gris clair pose dans le Concepteur (Gainsboro, Silver...) n'est pas une couleur "choisie" : c'etait
        ' le bouton ordinaire d'avant. Sans image, il prend l'allure commune (sinon : texte clair sur gris clair).
        Dim fond As Color = b.BackColor
        Dim grisClair As Boolean = Math.Abs(CInt(fond.R) - fond.G) < 12 AndAlso Math.Abs(CInt(fond.G) - fond.B) < 12 AndAlso fond.R >= 150
        Dim sansImage As Boolean = b.Image Is Nothing AndAlso b.BackgroundImage Is Nothing
        If Not b.UseVisualStyleBackColor AndAlso FondChoisi(b) AndAlso Not (grisClair AndAlso sansImage) Then
            ' Couleur choisie expres (bouton a icone, bouton colore par le code) : elle est gardee. Le bouton
            ' devient seulement plat : en mode sombre, un bouton ordinaire n'affiche ni son image de fond ni
            ' sa couleur.
            b.FlatStyle = FlatStyle.Flat
            b.FlatAppearance.BorderSize = 0
            If b.Cursor = Cursors.Default Then b.Cursor = Cursors.Hand
            Exit Sub
        End If
        Dim libelle As String = If(b.Text, "").Trim()
        Dim principal As Boolean = b Is fenetre.AcceptButton OrElse libelle.Equals("OK", StringComparison.OrdinalIgnoreCase)
        Dim quitter As Boolean = libelle.StartsWith("Quitter", StringComparison.OrdinalIgnoreCase)
        b.FlatStyle = FlatStyle.Flat
        b.FlatAppearance.BorderSize = 0
        If principal Then
            b.BackColor = Accent
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(50, 152, 255)
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(0, 104, 214)
            b.ForeColor = Color.White
        ElseIf quitter Then
            b.BackColor = _danger
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(150, 54, 54)
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(170, 60, 60)
            b.ForeColor = Color.White
        Else
            b.BackColor = _bouton
            b.FlatAppearance.MouseOverBackColor = _boutonSurvol
            b.FlatAppearance.MouseDownBackColor = _boutonAppui
            b.ForeColor = Texte
        End If
        b.UseVisualStyleBackColor = False
        If b.Cursor = Cursors.Default Then b.Cursor = Cursors.Hand
    End Sub

    ''' <summary>Tableau : fond sombre, en-tetes un ton au-dessus, lignes separees par un filet discret,
    ''' selection en bleu. Les couleurs posees ligne par ligne par le code restent les siennes.</summary>
    Private Sub HabillerTableau(t As DataGridView)
        Dim enTete As Color = Color.FromArgb(38, 38, 41)
        t.EnableHeadersVisualStyles = False
        t.BackgroundColor = Fond
        t.GridColor = Bord
        t.BorderStyle = BorderStyle.FixedSingle
        For Each style As DataGridViewCellStyle In {t.ColumnHeadersDefaultCellStyle, t.RowHeadersDefaultCellStyle}
            style.BackColor = enTete
            style.ForeColor = Texte
            style.SelectionBackColor = enTete
            style.SelectionForeColor = Texte
        Next
        t.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
        t.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single
        t.DefaultCellStyle.BackColor = Color.FromArgb(34, 34, 36)
        t.DefaultCellStyle.ForeColor = Texte
        t.DefaultCellStyle.SelectionBackColor = Color.FromArgb(10, 96, 190)
        t.DefaultCellStyle.SelectionForeColor = Color.White
        t.RowsDefaultCellStyle.BackColor = Color.Empty
        t.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(39, 39, 42)
        ' Les colonnes dont le style fixait les couleurs claires de Windows suivent le tableau.
        For Each colonne As DataGridViewColumn In t.Columns
            If colonne.HasDefaultCellStyle Then
                Dim s As DataGridViewCellStyle = colonne.DefaultCellStyle
                If s.BackColor = SystemColors.Window OrElse s.BackColor = SystemColors.Control OrElse s.BackColor = Color.White Then s.BackColor = Color.Empty
                If s.ForeColor = SystemColors.ControlText OrElse s.ForeColor = SystemColors.WindowText OrElse s.ForeColor = Color.Black Then s.ForeColor = Color.Empty
                If s.SelectionBackColor = SystemColors.Highlight Then s.SelectionBackColor = Color.Empty
                If s.SelectionForeColor = SystemColors.HighlightText Then s.SelectionForeColor = Color.Empty
            End If
            ' Listes deroulantes, boutons et cases a cocher DANS le tableau : dessines par Windows, ils restent
            ' blancs sur le fond sombre ("les listes fond blanc pas tres beau", Jacob, 09/10/2026). A plat, les
            ' listes prennent les couleurs de leur ligne ; les boutons et les cases a cocher sont dessines par
            ' PeindreLaCase.
            If TypeOf colonne Is DataGridViewComboBoxColumn Then
                DirectCast(colonne, DataGridViewComboBoxColumn).FlatStyle = FlatStyle.Flat
            ElseIf TypeOf colonne Is DataGridViewButtonColumn Then
                DirectCast(colonne, DataGridViewButtonColumn).FlatStyle = FlatStyle.Flat
            End If
        Next
        RemoveHandler t.CellPainting, AddressOf PeindreLaCase
        AddHandler t.CellPainting, AddressOf PeindreLaCase
    End Sub

    ''' <summary>Bouton d'un tableau : la case garde les couleurs de sa ligne et le bouton y est dessine en
    ''' retrait, arrondi (a plat, il remplissait toute la case : la colonne formait un bloc sans lignes).
    ''' Rend l'interieur du bouton, ou l'appelant ecrit son texte.</summary>
    Friend Function PeindreLeBouton(e As DataGridViewCellPaintingEventArgs) As Rectangle
        e.Paint(e.CellBounds, DataGridViewPaintParts.All And Not (DataGridViewPaintParts.ContentForeground Or DataGridViewPaintParts.ContentBackground))
        Dim choisie As Boolean = (e.State And DataGridViewElementStates.Selected) <> 0
        Dim bouton As New Rectangle(e.CellBounds.X + 3, e.CellBounds.Y + 3, e.CellBounds.Width - 7, e.CellBounds.Height - 7)
        If bouton.Width < 8 OrElse bouton.Height < 8 Then Return e.CellBounds
        Dim d As Integer = 2 * Math.Max(2, Math.Min(5, bouton.Height \ 4))
        Dim ancien As Drawing2D.SmoothingMode = e.Graphics.SmoothingMode
        e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
        Using contour As New Drawing2D.GraphicsPath()
            contour.AddArc(bouton.X, bouton.Y, d, d, 180, 90)
            contour.AddArc(bouton.Right - d, bouton.Y, d, d, 270, 90)
            contour.AddArc(bouton.Right - d, bouton.Bottom - d, d, d, 0, 90)
            contour.AddArc(bouton.X, bouton.Bottom - d, d, d, 90, 90)
            contour.CloseFigure()
            ' (Ligne selectionnee : un voile clair sur le bleu de la selection.)
            Using fond As New SolidBrush(If(choisie, Color.FromArgb(56, 255, 255, 255), _bouton))
                e.Graphics.FillPath(fond, contour)
            End Using
        End Using
        e.Graphics.SmoothingMode = ancien
        Return bouton
    End Function

    ''' <summary>Dessin des boutons et des cases a cocher d'un tableau (et mise a plat des listes posees
    ''' case par case).
    ''' Bouton : voir PeindreLeBouton (rien a faire si la fenetre l'a deja dessine elle-meme, comme la
    ''' colonne Play de la minuterie).
    ''' Case a cocher : celle de Windows est un carre blanc sur le fond sombre. Elle est dessinee ici -
    ''' cochee : pastille bleue et coche blanche ; decochee : carre sombre au filet gris.
    ''' Seul le dessin change : la valeur et le clic restent ceux de la case.</summary>
    Private Sub PeindreLaCase(sender As Object, e As DataGridViewCellPaintingEventArgs)
        If e.RowIndex < 0 OrElse e.ColumnIndex < 0 OrElse e.Handled Then Exit Sub
        Dim t As DataGridView = TryCast(sender, DataGridView)
        If t Is Nothing Then Exit Sub
        ' Liste deroulante posee dans UNE case par le code (la liste des chaines d'une ligne TV, dans la
        ' colonne Action, par exemple) : le reglage de la colonne ne la touche pas, elle restait blanche
        ' ("colonne action fond blanc pas jolie", Jacob, 09/10/2026). Mise a plat ici, juste avant son
        ' dessin : elle prend les couleurs de sa ligne.
        Dim liste As DataGridViewComboBoxCell = TryCast(t.Rows(e.RowIndex).Cells(e.ColumnIndex), DataGridViewComboBoxCell)
        If liste IsNot Nothing Then
            If liste.FlatStyle <> FlatStyle.Flat Then liste.FlatStyle = FlatStyle.Flat
            Exit Sub
        End If
        If TypeOf t.Columns(e.ColumnIndex) Is DataGridViewButtonColumn Then
            Try
                Dim interieur As Rectangle = PeindreLeBouton(e)
                Dim libelle As String = CStr(If(e.FormattedValue, ""))
                If libelle <> "" Then
                    TextRenderer.DrawText(e.Graphics, libelle, e.CellStyle.Font, interieur, Texte,
                                          TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis Or
                                          TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine)
                End If
                e.Handled = True
            Catch ex As Exception
                ' un dessin rate ne doit pas empecher l'affichage normal de la case
            End Try
            Exit Sub
        End If
        If Not TypeOf t.Columns(e.ColumnIndex) Is DataGridViewCheckBoxColumn Then Exit Sub
        Try
            Dim valeur As Object = t.Rows(e.RowIndex).Cells(e.ColumnIndex).EditedFormattedValue
            Dim cochee As Boolean = (TypeOf valeur Is Boolean AndAlso CBool(valeur)) OrElse
                                    (TypeOf valeur Is CheckState AndAlso DirectCast(valeur, CheckState) = CheckState.Checked)
            Dim choisie As Boolean = (e.State And DataGridViewElementStates.Selected) <> 0
            e.Paint(e.CellBounds, DataGridViewPaintParts.All And Not (DataGridViewPaintParts.ContentForeground Or DataGridViewPaintParts.ContentBackground))

            Dim cote As Integer = Math.Min(Math.Min(e.CellBounds.Height - 8, e.CellBounds.Width - 6), CInt(Math.Round(e.CellStyle.Font.Height * 0.85)))
            If cote < 8 Then
                e.Handled = True
                Exit Sub
            End If
            Dim zone As New Rectangle(e.CellBounds.X + (e.CellBounds.Width - cote) \ 2, e.CellBounds.Y + (e.CellBounds.Height - cote) \ 2, cote, cote)
            Dim rayon As Integer = Math.Max(2, cote \ 5)
            Dim ancien As Drawing2D.SmoothingMode = e.Graphics.SmoothingMode
            e.Graphics.SmoothingMode = Drawing2D.SmoothingMode.AntiAlias
            Using contour As New Drawing2D.GraphicsPath()
                contour.AddArc(zone.X, zone.Y, 2 * rayon, 2 * rayon, 180, 90)
                contour.AddArc(zone.Right - 2 * rayon, zone.Y, 2 * rayon, 2 * rayon, 270, 90)
                contour.AddArc(zone.Right - 2 * rayon, zone.Bottom - 2 * rayon, 2 * rayon, 2 * rayon, 0, 90)
                contour.AddArc(zone.X, zone.Bottom - 2 * rayon, 2 * rayon, 2 * rayon, 90, 90)
                contour.CloseFigure()
                Using fond As New SolidBrush(If(cochee, Accent, StylePro.Fond)),
                      filet As New Pen(If(choisie, Color.White, If(cochee, Accent, Secondaire)), 1.0F)
                    e.Graphics.FillPath(fond, contour)
                    e.Graphics.DrawPath(filet, contour)
                End Using
            End Using
            If cochee Then
                Using trait As New Pen(Color.White, Math.Max(1.6F, cote / 8.0F))
                    trait.StartCap = Drawing2D.LineCap.Round
                    trait.EndCap = Drawing2D.LineCap.Round
                    trait.LineJoin = Drawing2D.LineJoin.Round
                    e.Graphics.DrawLines(trait, {New PointF(zone.X + cote * 0.25F, zone.Y + cote * 0.53F),
                                                 New PointF(zone.X + cote * 0.43F, zone.Y + cote * 0.7F),
                                                 New PointF(zone.X + cote * 0.76F, zone.Y + cote * 0.32F)})
                End Using
            End If
            e.Graphics.SmoothingMode = ancien
            e.Handled = True
        Catch ex As Exception
            ' un dessin rate ne doit pas empecher l'affichage normal de la case
        End Try
    End Sub

End Module
