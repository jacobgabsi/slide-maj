<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormImportParametres
    Inherits System.Windows.Forms.Form

    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
    <System.Diagnostics.DebuggerNonUserCode()>
    Protected Overrides Sub Dispose(ByVal disposing As Boolean)
        Try
            If disposing AndAlso components IsNot Nothing Then
                components.Dispose()
            End If
        Finally
            MyBase.Dispose(disposing)
        End Try
    End Sub

    Private components As System.ComponentModel.IContainer

    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.GroupBoxFichier = New System.Windows.Forms.GroupBox()
        Me.LabelOrigine = New System.Windows.Forms.Label()
        Me.LabelFichier = New System.Windows.Forms.Label()
        Me.LabelConsigne = New System.Windows.Forms.Label()
        Me.LabelRecherche = New System.Windows.Forms.Label()
        Me.TextBoxRecherche = New System.Windows.Forms.TextBox()
        Me.CheckBoxDifferences = New System.Windows.Forms.CheckBox()
        Me.ButtonToutCocher = New System.Windows.Forms.Button()
        Me.ButtonToutDecocher = New System.Windows.Forms.Button()
        Me.ButtonCocherDifferences = New System.Windows.Forms.Button()
        Me.TreeViewElements = New System.Windows.Forms.TreeView()
        Me.GroupBoxDetail = New System.Windows.Forms.GroupBox()
        Me.LabelNomDetail = New System.Windows.Forms.Label()
        Me.LabelChemin = New System.Windows.Forms.Label()
        Me.LabelFichierTitre = New System.Windows.Forms.Label()
        Me.TextBoxFichier = New System.Windows.Forms.TextBox()
        Me.LabelActuelTitre = New System.Windows.Forms.Label()
        Me.TextBoxActuel = New System.Windows.Forms.TextBox()
        Me.LabelEtatDetail = New System.Windows.Forms.Label()
        Me.GroupBoxMinuterie = New System.Windows.Forms.GroupBox()
        Me.RadioButtonMinRemplacer = New System.Windows.Forms.RadioButton()
        Me.RadioButtonMinAjouter = New System.Windows.Forms.RadioButton()
        Me.GroupBoxRadios = New System.Windows.Forms.GroupBox()
        Me.RadioButtonRadRemplacer = New System.Windows.Forms.RadioButton()
        Me.RadioButtonRadAjouter = New System.Windows.Forms.RadioButton()
        Me.LabelResume = New System.Windows.Forms.Label()
        Me.LabelAvertissement = New System.Windows.Forms.Label()
        Me.ButtonImporter = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.GroupBoxFichier.SuspendLayout()
        Me.GroupBoxDetail.SuspendLayout()
        Me.GroupBoxMinuterie.SuspendLayout()
        Me.GroupBoxRadios.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBoxFichier
        '
        Me.GroupBoxFichier.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBoxFichier.Controls.Add(Me.LabelConsigne)
        Me.GroupBoxFichier.Controls.Add(Me.LabelOrigine)
        Me.GroupBoxFichier.Controls.Add(Me.LabelFichier)
        Me.GroupBoxFichier.Location = New System.Drawing.Point(16, 10)
        Me.GroupBoxFichier.Name = "GroupBoxFichier"
        Me.GroupBoxFichier.Size = New System.Drawing.Size(1208, 112)
        Me.GroupBoxFichier.TabIndex = 0
        Me.GroupBoxFichier.TabStop = False
        Me.GroupBoxFichier.Text = "Fichier à importer"
        '
        'LabelFichier
        '
        Me.LabelFichier.AutoEllipsis = True
        Me.LabelFichier.Location = New System.Drawing.Point(14, 28)
        Me.LabelFichier.Name = "LabelFichier"
        Me.LabelFichier.Size = New System.Drawing.Size(1180, 26)
        Me.LabelFichier.TabIndex = 0
        Me.LabelFichier.Text = "Fichier"
        '
        'LabelOrigine
        '
        Me.LabelOrigine.AutoEllipsis = True
        Me.LabelOrigine.Location = New System.Drawing.Point(14, 54)
        Me.LabelOrigine.Name = "LabelOrigine"
        Me.LabelOrigine.Size = New System.Drawing.Size(1180, 26)
        Me.LabelOrigine.TabIndex = 1
        Me.LabelOrigine.Text = "Origine"
        '
        'LabelConsigne
        '
        Me.LabelConsigne.AutoEllipsis = True
        Me.LabelConsigne.Location = New System.Drawing.Point(14, 80)
        Me.LabelConsigne.Name = "LabelConsigne"
        Me.LabelConsigne.Size = New System.Drawing.Size(1180, 26)
        Me.LabelConsigne.TabIndex = 2
        Me.LabelConsigne.Text = "Cochez ce que vous voulez reprendre. Un élément marqué ● diffère de ce PC ; un élément grisé est identique (le reprendre ne change rien)."
        '
        'LabelRecherche
        '
        Me.LabelRecherche.AutoSize = True
        Me.LabelRecherche.Location = New System.Drawing.Point(16, 138)
        Me.LabelRecherche.Name = "LabelRecherche"
        Me.LabelRecherche.Size = New System.Drawing.Size(104, 20)
        Me.LabelRecherche.TabIndex = 1
        Me.LabelRecherche.Text = "Rechercher :"
        '
        'TextBoxRecherche
        '
        Me.TextBoxRecherche.Location = New System.Drawing.Point(126, 134)
        Me.TextBoxRecherche.Name = "TextBoxRecherche"
        Me.TextBoxRecherche.Size = New System.Drawing.Size(260, 27)
        Me.TextBoxRecherche.TabIndex = 2
        '
        'CheckBoxDifferences
        '
        Me.CheckBoxDifferences.AutoSize = True
        Me.CheckBoxDifferences.Location = New System.Drawing.Point(404, 136)
        Me.CheckBoxDifferences.Name = "CheckBoxDifferences"
        Me.CheckBoxDifferences.Size = New System.Drawing.Size(360, 24)
        Me.CheckBoxDifferences.TabIndex = 3
        Me.CheckBoxDifferences.Text = "Afficher seulement ce qui diffère de ce PC"
        Me.CheckBoxDifferences.UseVisualStyleBackColor = True
        '
        'ButtonToutCocher
        '
        Me.ButtonToutCocher.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonToutCocher.Location = New System.Drawing.Point(788, 130)
        Me.ButtonToutCocher.Name = "ButtonToutCocher"
        Me.ButtonToutCocher.Size = New System.Drawing.Size(130, 36)
        Me.ButtonToutCocher.TabIndex = 4
        Me.ButtonToutCocher.Text = "Tout cocher"
        Me.ButtonToutCocher.UseVisualStyleBackColor = True
        '
        'ButtonToutDecocher
        '
        Me.ButtonToutDecocher.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonToutDecocher.Location = New System.Drawing.Point(924, 130)
        Me.ButtonToutDecocher.Name = "ButtonToutDecocher"
        Me.ButtonToutDecocher.Size = New System.Drawing.Size(140, 36)
        Me.ButtonToutDecocher.TabIndex = 5
        Me.ButtonToutDecocher.Text = "Tout décocher"
        Me.ButtonToutDecocher.UseVisualStyleBackColor = True
        '
        'ButtonCocherDifferences
        '
        Me.ButtonCocherDifferences.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonCocherDifferences.Location = New System.Drawing.Point(1070, 130)
        Me.ButtonCocherDifferences.Name = "ButtonCocherDifferences"
        Me.ButtonCocherDifferences.Size = New System.Drawing.Size(154, 36)
        Me.ButtonCocherDifferences.TabIndex = 6
        Me.ButtonCocherDifferences.Text = "Cocher les écarts"
        Me.ButtonCocherDifferences.UseVisualStyleBackColor = True
        '
        'TreeViewElements
        '
        Me.TreeViewElements.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.TreeViewElements.CheckBoxes = True
        Me.TreeViewElements.HideSelection = False
        Me.TreeViewElements.Location = New System.Drawing.Point(16, 176)
        Me.TreeViewElements.Name = "TreeViewElements"
        Me.TreeViewElements.Size = New System.Drawing.Size(716, 478)
        Me.TreeViewElements.TabIndex = 7
        '
        'GroupBoxDetail
        '
        Me.GroupBoxDetail.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBoxDetail.Controls.Add(Me.LabelEtatDetail)
        Me.GroupBoxDetail.Controls.Add(Me.TextBoxActuel)
        Me.GroupBoxDetail.Controls.Add(Me.LabelActuelTitre)
        Me.GroupBoxDetail.Controls.Add(Me.TextBoxFichier)
        Me.GroupBoxDetail.Controls.Add(Me.LabelFichierTitre)
        Me.GroupBoxDetail.Controls.Add(Me.LabelChemin)
        Me.GroupBoxDetail.Controls.Add(Me.LabelNomDetail)
        Me.GroupBoxDetail.Location = New System.Drawing.Point(744, 172)
        Me.GroupBoxDetail.Name = "GroupBoxDetail"
        Me.GroupBoxDetail.Size = New System.Drawing.Size(480, 306)
        Me.GroupBoxDetail.TabIndex = 8
        Me.GroupBoxDetail.TabStop = False
        Me.GroupBoxDetail.Text = "Détail de l'élément sélectionné"
        '
        'LabelNomDetail
        '
        Me.LabelNomDetail.AutoEllipsis = True
        Me.LabelNomDetail.Font = New System.Drawing.Font("Segoe UI Semibold", 10.0!, System.Drawing.FontStyle.Bold)
        Me.LabelNomDetail.Location = New System.Drawing.Point(14, 28)
        Me.LabelNomDetail.Name = "LabelNomDetail"
        Me.LabelNomDetail.Size = New System.Drawing.Size(452, 26)
        Me.LabelNomDetail.TabIndex = 0
        Me.LabelNomDetail.Text = "Sélectionnez un élément dans la liste"
        '
        'LabelChemin
        '
        Me.LabelChemin.AutoEllipsis = True
        Me.LabelChemin.Location = New System.Drawing.Point(14, 54)
        Me.LabelChemin.Name = "LabelChemin"
        Me.LabelChemin.Size = New System.Drawing.Size(452, 24)
        Me.LabelChemin.TabIndex = 1
        '
        'LabelFichierTitre
        '
        Me.LabelFichierTitre.AutoSize = True
        Me.LabelFichierTitre.Location = New System.Drawing.Point(14, 84)
        Me.LabelFichierTitre.Name = "LabelFichierTitre"
        Me.LabelFichierTitre.Size = New System.Drawing.Size(180, 20)
        Me.LabelFichierTitre.TabIndex = 2
        Me.LabelFichierTitre.Text = "Valeur dans le fichier :"
        '
        'TextBoxFichier
        '
        Me.TextBoxFichier.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBoxFichier.Location = New System.Drawing.Point(14, 108)
        Me.TextBoxFichier.Multiline = True
        Me.TextBoxFichier.Name = "TextBoxFichier"
        Me.TextBoxFichier.ReadOnly = True
        Me.TextBoxFichier.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.TextBoxFichier.Size = New System.Drawing.Size(452, 74)
        Me.TextBoxFichier.TabIndex = 3
        '
        'LabelActuelTitre
        '
        Me.LabelActuelTitre.AutoSize = True
        Me.LabelActuelTitre.Location = New System.Drawing.Point(14, 190)
        Me.LabelActuelTitre.Name = "LabelActuelTitre"
        Me.LabelActuelTitre.Size = New System.Drawing.Size(196, 20)
        Me.LabelActuelTitre.TabIndex = 4
        Me.LabelActuelTitre.Text = "Valeur actuelle sur ce PC :"
        '
        'TextBoxActuel
        '
        Me.TextBoxActuel.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBoxActuel.Location = New System.Drawing.Point(14, 214)
        Me.TextBoxActuel.Multiline = True
        Me.TextBoxActuel.Name = "TextBoxActuel"
        Me.TextBoxActuel.ReadOnly = True
        Me.TextBoxActuel.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.TextBoxActuel.Size = New System.Drawing.Size(452, 50)
        Me.TextBoxActuel.TabIndex = 5
        '
        'LabelEtatDetail
        '
        Me.LabelEtatDetail.AutoEllipsis = True
        Me.LabelEtatDetail.Location = New System.Drawing.Point(14, 270)
        Me.LabelEtatDetail.Name = "LabelEtatDetail"
        Me.LabelEtatDetail.Size = New System.Drawing.Size(452, 26)
        Me.LabelEtatDetail.TabIndex = 6
        '
        'GroupBoxMinuterie
        '
        Me.GroupBoxMinuterie.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBoxMinuterie.Controls.Add(Me.RadioButtonMinAjouter)
        Me.GroupBoxMinuterie.Controls.Add(Me.RadioButtonMinRemplacer)
        Me.GroupBoxMinuterie.Location = New System.Drawing.Point(744, 486)
        Me.GroupBoxMinuterie.Name = "GroupBoxMinuterie"
        Me.GroupBoxMinuterie.Size = New System.Drawing.Size(480, 82)
        Me.GroupBoxMinuterie.TabIndex = 9
        Me.GroupBoxMinuterie.TabStop = False
        Me.GroupBoxMinuterie.Text = "Lignes de la minuterie cochées : que faire de celles de ce PC ?"
        '
        'RadioButtonMinRemplacer
        '
        Me.RadioButtonMinRemplacer.AutoSize = True
        Me.RadioButtonMinRemplacer.Location = New System.Drawing.Point(14, 26)
        Me.RadioButtonMinRemplacer.Name = "RadioButtonMinRemplacer"
        Me.RadioButtonMinRemplacer.Size = New System.Drawing.Size(420, 24)
        Me.RadioButtonMinRemplacer.TabIndex = 0
        Me.RadioButtonMinRemplacer.Text = "Remplacer toute la minuterie de ce PC par les lignes cochées"
        Me.RadioButtonMinRemplacer.UseVisualStyleBackColor = True
        '
        'RadioButtonMinAjouter
        '
        Me.RadioButtonMinAjouter.AutoSize = True
        Me.RadioButtonMinAjouter.Checked = True
        Me.RadioButtonMinAjouter.Location = New System.Drawing.Point(14, 52)
        Me.RadioButtonMinAjouter.Name = "RadioButtonMinAjouter"
        Me.RadioButtonMinAjouter.Size = New System.Drawing.Size(420, 24)
        Me.RadioButtonMinAjouter.TabIndex = 1
        Me.RadioButtonMinAjouter.TabStop = True
        Me.RadioButtonMinAjouter.Text = "Ajouter les lignes cochées à celles de ce PC (sans doublon)"
        Me.RadioButtonMinAjouter.UseVisualStyleBackColor = True
        '
        'GroupBoxRadios
        '
        Me.GroupBoxRadios.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBoxRadios.Controls.Add(Me.RadioButtonRadAjouter)
        Me.GroupBoxRadios.Controls.Add(Me.RadioButtonRadRemplacer)
        Me.GroupBoxRadios.Location = New System.Drawing.Point(744, 574)
        Me.GroupBoxRadios.Name = "GroupBoxRadios"
        Me.GroupBoxRadios.Size = New System.Drawing.Size(480, 82)
        Me.GroupBoxRadios.TabIndex = 10
        Me.GroupBoxRadios.TabStop = False
        Me.GroupBoxRadios.Text = "Radios cochées : que faire de la liste de ce PC ?"
        '
        'RadioButtonRadRemplacer
        '
        Me.RadioButtonRadRemplacer.AutoSize = True
        Me.RadioButtonRadRemplacer.Location = New System.Drawing.Point(14, 26)
        Me.RadioButtonRadRemplacer.Name = "RadioButtonRadRemplacer"
        Me.RadioButtonRadRemplacer.Size = New System.Drawing.Size(420, 24)
        Me.RadioButtonRadRemplacer.TabIndex = 0
        Me.RadioButtonRadRemplacer.Text = "Remplacer toute la liste de ce PC par les radios cochées"
        Me.RadioButtonRadRemplacer.UseVisualStyleBackColor = True
        '
        'RadioButtonRadAjouter
        '
        Me.RadioButtonRadAjouter.AutoSize = True
        Me.RadioButtonRadAjouter.Checked = True
        Me.RadioButtonRadAjouter.Location = New System.Drawing.Point(14, 52)
        Me.RadioButtonRadAjouter.Name = "RadioButtonRadAjouter"
        Me.RadioButtonRadAjouter.Size = New System.Drawing.Size(420, 24)
        Me.RadioButtonRadAjouter.TabIndex = 1
        Me.RadioButtonRadAjouter.TabStop = True
        Me.RadioButtonRadAjouter.Text = "Ajouter les radios cochées à la liste de ce PC (sans doublon)"
        Me.RadioButtonRadAjouter.UseVisualStyleBackColor = True
        '
        'LabelResume
        '
        Me.LabelResume.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelResume.AutoEllipsis = True
        Me.LabelResume.Font = New System.Drawing.Font("Segoe UI Semibold", 10.0!, System.Drawing.FontStyle.Bold)
        Me.LabelResume.Location = New System.Drawing.Point(16, 666)
        Me.LabelResume.Name = "LabelResume"
        Me.LabelResume.Size = New System.Drawing.Size(1208, 28)
        Me.LabelResume.TabIndex = 11
        Me.LabelResume.Text = "Sélection"
        '
        'LabelAvertissement
        '
        Me.LabelAvertissement.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelAvertissement.Location = New System.Drawing.Point(16, 696)
        Me.LabelAvertissement.Name = "LabelAvertissement"
        Me.LabelAvertissement.Size = New System.Drawing.Size(1208, 50)
        Me.LabelAvertissement.TabIndex = 12
        Me.LabelAvertissement.Text = "Avertissement"
        '
        'ButtonImporter
        '
        Me.ButtonImporter.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonImporter.Location = New System.Drawing.Point(1040, 756)
        Me.ButtonImporter.Name = "ButtonImporter"
        Me.ButtonImporter.Size = New System.Drawing.Size(88, 40)
        Me.ButtonImporter.TabIndex = 13
        Me.ButtonImporter.Text = "Importer"
        Me.ButtonImporter.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(1134, 756)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(90, 40)
        Me.ButtonAnnuler.TabIndex = 14
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormImportParametres
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(1240, 812)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonImporter)
        Me.Controls.Add(Me.LabelAvertissement)
        Me.Controls.Add(Me.LabelResume)
        Me.Controls.Add(Me.GroupBoxRadios)
        Me.Controls.Add(Me.GroupBoxMinuterie)
        Me.Controls.Add(Me.GroupBoxDetail)
        Me.Controls.Add(Me.TreeViewElements)
        Me.Controls.Add(Me.ButtonCocherDifferences)
        Me.Controls.Add(Me.ButtonToutDecocher)
        Me.Controls.Add(Me.ButtonToutCocher)
        Me.Controls.Add(Me.CheckBoxDifferences)
        Me.Controls.Add(Me.TextBoxRecherche)
        Me.Controls.Add(Me.LabelRecherche)
        Me.Controls.Add(Me.GroupBoxFichier)
        Me.MinimumSize = New System.Drawing.Size(1000, 700)
        Me.Name = "FormImportParametres"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Importer des paramètres"
        Me.GroupBoxFichier.ResumeLayout(False)
        Me.GroupBoxDetail.ResumeLayout(False)
        Me.GroupBoxDetail.PerformLayout()
        Me.GroupBoxMinuterie.ResumeLayout(False)
        Me.GroupBoxMinuterie.PerformLayout()
        Me.GroupBoxRadios.ResumeLayout(False)
        Me.GroupBoxRadios.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents GroupBoxFichier As System.Windows.Forms.GroupBox
    Friend WithEvents LabelOrigine As System.Windows.Forms.Label
    Friend WithEvents LabelFichier As System.Windows.Forms.Label
    Friend WithEvents LabelConsigne As System.Windows.Forms.Label
    Friend WithEvents LabelRecherche As System.Windows.Forms.Label
    Friend WithEvents TextBoxRecherche As System.Windows.Forms.TextBox
    Friend WithEvents CheckBoxDifferences As System.Windows.Forms.CheckBox
    Friend WithEvents ButtonToutCocher As System.Windows.Forms.Button
    Friend WithEvents ButtonToutDecocher As System.Windows.Forms.Button
    Friend WithEvents ButtonCocherDifferences As System.Windows.Forms.Button
    Friend WithEvents TreeViewElements As System.Windows.Forms.TreeView
    Friend WithEvents GroupBoxDetail As System.Windows.Forms.GroupBox
    Friend WithEvents LabelNomDetail As System.Windows.Forms.Label
    Friend WithEvents LabelChemin As System.Windows.Forms.Label
    Friend WithEvents LabelFichierTitre As System.Windows.Forms.Label
    Friend WithEvents TextBoxFichier As System.Windows.Forms.TextBox
    Friend WithEvents LabelActuelTitre As System.Windows.Forms.Label
    Friend WithEvents TextBoxActuel As System.Windows.Forms.TextBox
    Friend WithEvents LabelEtatDetail As System.Windows.Forms.Label
    Friend WithEvents GroupBoxMinuterie As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButtonMinRemplacer As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButtonMinAjouter As System.Windows.Forms.RadioButton
    Friend WithEvents GroupBoxRadios As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButtonRadRemplacer As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButtonRadAjouter As System.Windows.Forms.RadioButton
    Friend WithEvents LabelResume As System.Windows.Forms.Label
    Friend WithEvents LabelAvertissement As System.Windows.Forms.Label
    Friend WithEvents ButtonImporter As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
