<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class ControlTuya
    Inherits System.Windows.Forms.UserControl

    'UserControl remplace la méthode Dispose pour nettoyer la liste des composants.
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
        Me.components = New System.ComponentModel.Container()
        Me.GroupBoxTuya = New System.Windows.Forms.GroupBox()
        Me.CheckBoxActif = New System.Windows.Forms.CheckBox()
        Me.LabelCompte = New System.Windows.Forms.Label()
        Me.ComboBoxComptes = New System.Windows.Forms.ComboBox()
        Me.ButtonNouveauCompte = New System.Windows.Forms.Button()
        Me.ButtonRenommerCompte = New System.Windows.Forms.Button()
        Me.ButtonSupprimerCompte = New System.Windows.Forms.Button()
        Me.LabelClientId = New System.Windows.Forms.Label()
        Me.TextBoxClientId = New System.Windows.Forms.TextBox()
        Me.LabelSecret = New System.Windows.Forms.Label()
        Me.TextBoxSecret = New System.Windows.Forms.TextBox()
        Me.LabelUid = New System.Windows.Forms.Label()
        Me.TextBoxUid = New System.Windows.Forms.TextBox()
        Me.LabelRegion = New System.Windows.Forms.Label()
        Me.ComboBoxRegion = New System.Windows.Forms.ComboBox()
        Me.ButtonAppareils = New System.Windows.Forms.Button()
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.ButtonAide = New System.Windows.Forms.Button()
        Me.LabelAppareils = New System.Windows.Forms.Label()
        Me.DataGridViewAppareils = New System.Windows.Forms.DataGridView()
        Me.ColPropose = New System.Windows.Forms.DataGridViewCheckBoxColumn()
        Me.ColNom = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColCompte = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColCode = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColEtat = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColMesure = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColOn = New System.Windows.Forms.DataGridViewButtonColumn()
        Me.ColOff = New System.Windows.Forms.DataGridViewButtonColumn()
        Me.ButtonToutCocher = New System.Windows.Forms.Button()
        Me.ButtonToutDecocher = New System.Windows.Forms.Button()
        Me.LabelGroupes = New System.Windows.Forms.Label()
        Me.ListBoxGroupes = New System.Windows.Forms.ListBox()
        Me.LabelMembres = New System.Windows.Forms.Label()
        Me.CheckedListBoxMembres = New System.Windows.Forms.CheckedListBox()
        Me.ButtonNouveauGroupe = New System.Windows.Forms.Button()
        Me.ButtonSupprimerGroupe = New System.Windows.Forms.Button()
        Me.ButtonTestGroupeOn = New System.Windows.Forms.Button()
        Me.ButtonTestGroupeOff = New System.Windows.Forms.Button()
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.GroupBoxTuya.SuspendLayout()
        Me.SuspendLayout()
        CType(Me.DataGridViewAppareils, System.ComponentModel.ISupportInitialize).BeginInit()
        '
        'GroupBoxTuya
        '
        Me.GroupBoxTuya.Controls.Add(Me.CheckBoxActif)
        Me.GroupBoxTuya.Controls.Add(Me.LabelAide)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonSupprimerGroupe)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonTestGroupeOn)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonTestGroupeOff)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonNouveauGroupe)
        Me.GroupBoxTuya.Controls.Add(Me.CheckedListBoxMembres)
        Me.GroupBoxTuya.Controls.Add(Me.LabelMembres)
        Me.GroupBoxTuya.Controls.Add(Me.ListBoxGroupes)
        Me.GroupBoxTuya.Controls.Add(Me.LabelGroupes)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonToutCocher)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonToutDecocher)
        Me.GroupBoxTuya.Controls.Add(Me.DataGridViewAppareils)
        Me.GroupBoxTuya.Controls.Add(Me.LabelAppareils)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonAide)
        Me.GroupBoxTuya.Controls.Add(Me.LabelEtat)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonAppareils)
        Me.GroupBoxTuya.Controls.Add(Me.ComboBoxRegion)
        Me.GroupBoxTuya.Controls.Add(Me.LabelRegion)
        Me.GroupBoxTuya.Controls.Add(Me.TextBoxUid)
        Me.GroupBoxTuya.Controls.Add(Me.LabelUid)
        Me.GroupBoxTuya.Controls.Add(Me.TextBoxSecret)
        Me.GroupBoxTuya.Controls.Add(Me.LabelSecret)
        Me.GroupBoxTuya.Controls.Add(Me.TextBoxClientId)
        Me.GroupBoxTuya.Controls.Add(Me.LabelClientId)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonRenommerCompte)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonSupprimerCompte)
        Me.GroupBoxTuya.Controls.Add(Me.ButtonNouveauCompte)
        Me.GroupBoxTuya.Controls.Add(Me.ComboBoxComptes)
        Me.GroupBoxTuya.Controls.Add(Me.LabelCompte)
        Me.GroupBoxTuya.Dock = System.Windows.Forms.DockStyle.Fill
        Me.GroupBoxTuya.Location = New System.Drawing.Point(0, 0)
        Me.GroupBoxTuya.Name = "GroupBoxTuya"
        Me.GroupBoxTuya.Size = New System.Drawing.Size(780, 868)
        Me.GroupBoxTuya.TabIndex = 0
        Me.GroupBoxTuya.TabStop = False
        Me.GroupBoxTuya.Text = "Tuya / Smart Life (commandes de la minuterie, type Tuya)"
        '
        'CheckBoxActif
        '
        Me.CheckBoxActif.AutoSize = True
        Me.CheckBoxActif.Location = New System.Drawing.Point(18, 34)
        Me.CheckBoxActif.Name = "CheckBoxActif"
        Me.CheckBoxActif.Size = New System.Drawing.Size(560, 29)
        Me.CheckBoxActif.TabIndex = 26
        Me.CheckBoxActif.Text = "Utiliser Tuya (décoché : aucun appareil n'est téléchargé au démarrage)"
        Me.CheckBoxActif.UseVisualStyleBackColor = True
        '
        'LabelCompte
        '
        Me.LabelCompte.AutoSize = True
        Me.LabelCompte.Location = New System.Drawing.Point(18, 74)
        Me.LabelCompte.Name = "LabelCompte"
        Me.LabelCompte.Size = New System.Drawing.Size(120, 25)
        Me.LabelCompte.TabIndex = 0
        Me.LabelCompte.Text = "Compte Tuya :"
        '
        'ComboBoxComptes
        '
        Me.ComboBoxComptes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxComptes.FormattingEnabled = True
        Me.ComboBoxComptes.Location = New System.Drawing.Point(300, 71)
        Me.ComboBoxComptes.Name = "ComboBoxComptes"
        Me.ComboBoxComptes.Size = New System.Drawing.Size(160, 33)
        Me.ComboBoxComptes.TabIndex = 1
        '
        'ButtonNouveauCompte
        '
        Me.ButtonNouveauCompte.Location = New System.Drawing.Point(468, 69)
        Me.ButtonNouveauCompte.Name = "ButtonNouveauCompte"
        Me.ButtonNouveauCompte.Size = New System.Drawing.Size(100, 38)
        Me.ButtonNouveauCompte.TabIndex = 2
        Me.ButtonNouveauCompte.Text = "Nouveau..."
        Me.ButtonNouveauCompte.UseVisualStyleBackColor = True
        '
        'ButtonRenommerCompte
        '
        Me.ButtonRenommerCompte.Location = New System.Drawing.Point(576, 69)
        Me.ButtonRenommerCompte.Name = "ButtonRenommerCompte"
        Me.ButtonRenommerCompte.Size = New System.Drawing.Size(100, 38)
        Me.ButtonRenommerCompte.TabIndex = 27
        Me.ButtonRenommerCompte.Text = "Renommer"
        Me.ButtonRenommerCompte.UseVisualStyleBackColor = True
        '
        'ButtonSupprimerCompte
        '
        Me.ButtonSupprimerCompte.Location = New System.Drawing.Point(684, 69)
        Me.ButtonSupprimerCompte.Name = "ButtonSupprimerCompte"
        Me.ButtonSupprimerCompte.Size = New System.Drawing.Size(78, 38)
        Me.ButtonSupprimerCompte.TabIndex = 3
        Me.ButtonSupprimerCompte.Text = "Suppr."
        Me.ButtonSupprimerCompte.UseVisualStyleBackColor = True
        '
        'LabelClientId
        '
        Me.LabelClientId.AutoSize = True
        Me.LabelClientId.Location = New System.Drawing.Point(18, 120)
        Me.LabelClientId.Name = "LabelClientId"
        Me.LabelClientId.Size = New System.Drawing.Size(220, 25)
        Me.LabelClientId.TabIndex = 4
        Me.LabelClientId.Text = "Identifiant client (Client ID) :"
        '
        'TextBoxClientId
        '
        Me.TextBoxClientId.Location = New System.Drawing.Point(300, 117)
        Me.TextBoxClientId.Name = "TextBoxClientId"
        Me.TextBoxClientId.Size = New System.Drawing.Size(462, 31)
        Me.TextBoxClientId.TabIndex = 5
        '
        'LabelSecret
        '
        Me.LabelSecret.AutoSize = True
        Me.LabelSecret.Location = New System.Drawing.Point(18, 166)
        Me.LabelSecret.Name = "LabelSecret"
        Me.LabelSecret.Size = New System.Drawing.Size(240, 25)
        Me.LabelSecret.TabIndex = 6
        Me.LabelSecret.Text = "Clé secrète (Client Secret) :"
        '
        'TextBoxSecret
        '
        Me.TextBoxSecret.Location = New System.Drawing.Point(300, 163)
        Me.TextBoxSecret.Name = "TextBoxSecret"
        Me.TextBoxSecret.Size = New System.Drawing.Size(462, 31)
        Me.TextBoxSecret.TabIndex = 7
        Me.TextBoxSecret.UseSystemPasswordChar = True
        '
        'LabelUid
        '
        Me.LabelUid.AutoSize = True
        Me.LabelUid.Location = New System.Drawing.Point(18, 212)
        Me.LabelUid.Name = "LabelUid"
        Me.LabelUid.Size = New System.Drawing.Size(250, 25)
        Me.LabelUid.TabIndex = 8
        Me.LabelUid.Text = "Identifiant du compte (UID) :"
        '
        'TextBoxUid
        '
        Me.TextBoxUid.Location = New System.Drawing.Point(300, 209)
        Me.TextBoxUid.Name = "TextBoxUid"
        Me.TextBoxUid.Size = New System.Drawing.Size(462, 31)
        Me.TextBoxUid.TabIndex = 9
        '
        'LabelRegion
        '
        Me.LabelRegion.AutoSize = True
        Me.LabelRegion.Location = New System.Drawing.Point(18, 258)
        Me.LabelRegion.Name = "LabelRegion"
        Me.LabelRegion.Size = New System.Drawing.Size(180, 25)
        Me.LabelRegion.TabIndex = 10
        Me.LabelRegion.Text = "Centre de données :"
        '
        'ComboBoxRegion
        '
        Me.ComboBoxRegion.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxRegion.FormattingEnabled = True
        Me.ComboBoxRegion.Location = New System.Drawing.Point(300, 255)
        Me.ComboBoxRegion.Name = "ComboBoxRegion"
        Me.ComboBoxRegion.Size = New System.Drawing.Size(300, 33)
        Me.ComboBoxRegion.TabIndex = 11
        '
        'ButtonAppareils
        '
        Me.ButtonAppareils.Location = New System.Drawing.Point(18, 306)
        Me.ButtonAppareils.Name = "ButtonAppareils"
        Me.ButtonAppareils.Size = New System.Drawing.Size(290, 42)
        Me.ButtonAppareils.TabIndex = 12
        Me.ButtonAppareils.Text = "Charger/Actualiser appareils"
        Me.ButtonAppareils.UseVisualStyleBackColor = True
        '
        'LabelEtat
        '
        Me.LabelEtat.Location = New System.Drawing.Point(316, 306)
        Me.LabelEtat.AutoEllipsis = True
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(306, 42)
        Me.LabelEtat.TabIndex = 13
        Me.LabelEtat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'ButtonAide
        '
        Me.ButtonAide.Location = New System.Drawing.Point(630, 306)
        Me.ButtonAide.Name = "ButtonAide"
        Me.ButtonAide.Size = New System.Drawing.Size(132, 42)
        Me.ButtonAide.TabIndex = 14
        Me.ButtonAide.Text = "Aide ?"
        Me.ButtonAide.UseVisualStyleBackColor = True
        '
        'LabelAppareils
        '
        Me.LabelAppareils.AutoSize = True
        Me.LabelAppareils.Location = New System.Drawing.Point(18, 366)
        Me.LabelAppareils.Name = "LabelAppareils"
        Me.LabelAppareils.Size = New System.Drawing.Size(660, 25)
        Me.LabelAppareils.TabIndex = 15
        Me.LabelAppareils.Text = "Appareils proposés (cochez) :"
        '
        'DataGridViewAppareils
        '
        Me.DataGridViewAppareils.AllowUserToAddRows = False
        Me.DataGridViewAppareils.AllowUserToDeleteRows = False
        Me.DataGridViewAppareils.AllowUserToResizeRows = False
        Me.DataGridViewAppareils.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill
        Me.DataGridViewAppareils.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridViewAppareils.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ColPropose, Me.ColNom, Me.ColCompte, Me.ColCode, Me.ColEtat, Me.ColMesure, Me.ColOn, Me.ColOff})
        Me.DataGridViewAppareils.Location = New System.Drawing.Point(18, 400)
        Me.DataGridViewAppareils.MultiSelect = False
        Me.DataGridViewAppareils.Name = "DataGridViewAppareils"
        Me.DataGridViewAppareils.RowHeadersVisible = False
        Me.DataGridViewAppareils.RowHeadersWidth = 62
        Me.DataGridViewAppareils.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.DataGridViewAppareils.Size = New System.Drawing.Size(744, 196)
        Me.DataGridViewAppareils.TabIndex = 16
        '
        'ColPropose
        '
        Me.ColPropose.HeaderText = "Proposé"
        Me.ColPropose.MinimumWidth = 8
        Me.ColPropose.Name = "ColPropose"
        Me.ColPropose.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic
        Me.ColPropose.ToolTipText = "Coché : l'appareil est proposé dans la minuterie (Nom ON / Nom OFF)"
        Me.ColPropose.FillWeight = 45.0!
        '
        'ColNom
        '
        Me.ColNom.HeaderText = "Appareil"
        Me.ColNom.MinimumWidth = 8
        Me.ColNom.Name = "ColNom"
        Me.ColNom.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic
        Me.ColNom.ReadOnly = True
        Me.ColNom.FillWeight = 190.0!
        '
        'ColCompte
        '
        Me.ColCompte.HeaderText = "Compte"
        Me.ColCompte.MinimumWidth = 8
        Me.ColCompte.Name = "ColCompte"
        Me.ColCompte.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic
        Me.ColCompte.ReadOnly = True
        Me.ColCompte.FillWeight = 80.0!
        '
        'ColCode
        '
        Me.ColCode.HeaderText = "Interrupteur"
        Me.ColCode.MinimumWidth = 8
        Me.ColCode.Name = "ColCode"
        Me.ColCode.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic
        Me.ColCode.ReadOnly = True
        Me.ColCode.FillWeight = 90.0!
        '
        'ColEtat
        '
        Me.ColEtat.FillWeight = 80.0!
        Me.ColEtat.HeaderText = "État"
        Me.ColEtat.MinimumWidth = 8
        Me.ColEtat.Name = "ColEtat"
        Me.ColEtat.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic
        Me.ColEtat.ReadOnly = True
        '
        'ColMesure
        '
        Me.ColMesure.HeaderText = "Mesure"
        Me.ColMesure.MinimumWidth = 8
        Me.ColMesure.Name = "ColMesure"
        Me.ColMesure.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic
        Me.ColMesure.ReadOnly = True
        Me.ColMesure.FillWeight = 120.0!
        '
        'ColOn
        '
        Me.ColOn.HeaderText = "Test"
        Me.ColOn.MinimumWidth = 8
        Me.ColOn.Name = "ColOn"
        Me.ColOn.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic
        Me.ColOn.Text = "ON"
        Me.ColOn.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ColOn.UseColumnTextForButtonValue = True
        Me.ColOn.FillWeight = 50.0!
        '
        'ColOff
        '
        Me.ColOff.HeaderText = ""
        Me.ColOff.MinimumWidth = 8
        Me.ColOff.Name = "ColOff"
        Me.ColOff.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.Programmatic
        Me.ColOff.Text = "OFF"
        Me.ColOff.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ColOff.UseColumnTextForButtonValue = True
        Me.ColOff.FillWeight = 50.0!
        '
        'ButtonToutCocher
        '
        Me.ButtonToutCocher.Location = New System.Drawing.Point(470, 358)
        Me.ButtonToutCocher.Name = "ButtonToutCocher"
        Me.ButtonToutCocher.Size = New System.Drawing.Size(150, 38)
        Me.ButtonToutCocher.TabIndex = 30
        Me.ButtonToutCocher.Text = "Tout cocher"
        Me.ButtonToutCocher.UseVisualStyleBackColor = True
        '
        'ButtonToutDecocher
        '
        Me.ButtonToutDecocher.Location = New System.Drawing.Point(630, 358)
        Me.ButtonToutDecocher.Name = "ButtonToutDecocher"
        Me.ButtonToutDecocher.Size = New System.Drawing.Size(132, 38)
        Me.ButtonToutDecocher.TabIndex = 31
        Me.ButtonToutDecocher.Text = "Aucun"
        Me.ButtonToutDecocher.UseVisualStyleBackColor = True
        '
        'LabelGroupes
        '
        Me.LabelGroupes.AutoSize = True
        Me.LabelGroupes.Location = New System.Drawing.Point(18, 602)
        Me.LabelGroupes.Name = "LabelGroupes"
        Me.LabelGroupes.Size = New System.Drawing.Size(300, 25)
        Me.LabelGroupes.TabIndex = 19
        Me.LabelGroupes.Text = "Groupes (agissent sur tous leurs appareils) :"
        '
        'ListBoxGroupes
        '
        Me.ListBoxGroupes.FormattingEnabled = True
        Me.ListBoxGroupes.ItemHeight = 25
        Me.ListBoxGroupes.Location = New System.Drawing.Point(18, 632)
        Me.ListBoxGroupes.Name = "ListBoxGroupes"
        Me.ListBoxGroupes.Size = New System.Drawing.Size(300, 136)
        Me.ListBoxGroupes.TabIndex = 20
        '
        'LabelMembres
        '
        Me.LabelMembres.AutoSize = True
        Me.LabelMembres.Location = New System.Drawing.Point(334, 602)
        Me.LabelMembres.Name = "LabelMembres"
        Me.LabelMembres.Size = New System.Drawing.Size(300, 25)
        Me.LabelMembres.TabIndex = 21
        Me.LabelMembres.Text = "Appareils du groupe sélectionné :"
        '
        'CheckedListBoxMembres
        '
        Me.CheckedListBoxMembres.CheckOnClick = True
        Me.CheckedListBoxMembres.FormattingEnabled = True
        Me.CheckedListBoxMembres.Location = New System.Drawing.Point(334, 632)
        Me.CheckedListBoxMembres.Name = "CheckedListBoxMembres"
        Me.CheckedListBoxMembres.Size = New System.Drawing.Size(428, 136)
        Me.CheckedListBoxMembres.TabIndex = 22
        '
        'ButtonNouveauGroupe
        '
        Me.ButtonNouveauGroupe.Location = New System.Drawing.Point(18, 776)
        Me.ButtonNouveauGroupe.Name = "ButtonNouveauGroupe"
        Me.ButtonNouveauGroupe.Size = New System.Drawing.Size(145, 42)
        Me.ButtonNouveauGroupe.TabIndex = 23
        Me.ButtonNouveauGroupe.Text = "Nouveau..."
        Me.ButtonNouveauGroupe.UseVisualStyleBackColor = True
        '
        'ButtonSupprimerGroupe
        '
        Me.ButtonSupprimerGroupe.Location = New System.Drawing.Point(173, 776)
        Me.ButtonSupprimerGroupe.Name = "ButtonSupprimerGroupe"
        Me.ButtonSupprimerGroupe.Size = New System.Drawing.Size(145, 42)
        Me.ButtonSupprimerGroupe.TabIndex = 24
        Me.ButtonSupprimerGroupe.Text = "Supprimer"
        Me.ButtonSupprimerGroupe.UseVisualStyleBackColor = True
        '
        'ButtonTestGroupeOn
        '
        Me.ButtonTestGroupeOn.Location = New System.Drawing.Point(334, 776)
        Me.ButtonTestGroupeOn.Name = "ButtonTestGroupeOn"
        Me.ButtonTestGroupeOn.Size = New System.Drawing.Size(214, 42)
        Me.ButtonTestGroupeOn.TabIndex = 28
        Me.ButtonTestGroupeOn.Text = "Tester ON (groupe)"
        Me.ButtonTestGroupeOn.UseVisualStyleBackColor = True
        '
        'ButtonTestGroupeOff
        '
        Me.ButtonTestGroupeOff.Location = New System.Drawing.Point(556, 776)
        Me.ButtonTestGroupeOff.Name = "ButtonTestGroupeOff"
        Me.ButtonTestGroupeOff.Size = New System.Drawing.Size(206, 42)
        Me.ButtonTestGroupeOff.TabIndex = 29
        Me.ButtonTestGroupeOff.Text = "Tester OFF (groupe)"
        Me.ButtonTestGroupeOff.UseVisualStyleBackColor = True
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(18, 830)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(744, 36)
        Me.LabelAide.TabIndex = 25
        Me.LabelAide.Text = "Identifiants : plateforme Tuya IoT, projet cloud (Client ID, Secret) ; UID : Appareils, Lier un compte application."
        '
        'ControlTuya
        '
        Me.Controls.Add(Me.GroupBoxTuya)
        Me.Name = "ControlTuya"
        Me.Size = New System.Drawing.Size(780, 868)
CType(Me.DataGridViewAppareils, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBoxTuya.ResumeLayout(False)
        Me.GroupBoxTuya.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents GroupBoxTuya As System.Windows.Forms.GroupBox
    Friend WithEvents CheckBoxActif As System.Windows.Forms.CheckBox
    Friend WithEvents LabelCompte As System.Windows.Forms.Label
    Friend WithEvents ComboBoxComptes As System.Windows.Forms.ComboBox
    Friend WithEvents ButtonNouveauCompte As System.Windows.Forms.Button
    Friend WithEvents ButtonSupprimerCompte As System.Windows.Forms.Button
    Friend WithEvents ButtonRenommerCompte As System.Windows.Forms.Button
    Friend WithEvents LabelClientId As System.Windows.Forms.Label
    Friend WithEvents TextBoxClientId As System.Windows.Forms.TextBox
    Friend WithEvents LabelSecret As System.Windows.Forms.Label
    Friend WithEvents TextBoxSecret As System.Windows.Forms.TextBox
    Friend WithEvents LabelUid As System.Windows.Forms.Label
    Friend WithEvents TextBoxUid As System.Windows.Forms.TextBox
    Friend WithEvents LabelRegion As System.Windows.Forms.Label
    Friend WithEvents ComboBoxRegion As System.Windows.Forms.ComboBox
    Friend WithEvents ButtonAppareils As System.Windows.Forms.Button
    Friend WithEvents LabelEtat As System.Windows.Forms.Label
    Friend WithEvents ButtonAide As System.Windows.Forms.Button
    Friend WithEvents LabelAppareils As System.Windows.Forms.Label
    Friend WithEvents DataGridViewAppareils As System.Windows.Forms.DataGridView
    Friend WithEvents ColPropose As System.Windows.Forms.DataGridViewCheckBoxColumn
    Friend WithEvents ColNom As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColCompte As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColCode As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColEtat As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColMesure As System.Windows.Forms.DataGridViewTextBoxColumn
    Friend WithEvents ColOn As System.Windows.Forms.DataGridViewButtonColumn
    Friend WithEvents ColOff As System.Windows.Forms.DataGridViewButtonColumn
    Friend WithEvents ButtonToutCocher As System.Windows.Forms.Button
    Friend WithEvents ButtonToutDecocher As System.Windows.Forms.Button
    Friend WithEvents LabelGroupes As System.Windows.Forms.Label
    Friend WithEvents ListBoxGroupes As System.Windows.Forms.ListBox
    Friend WithEvents LabelMembres As System.Windows.Forms.Label
    Friend WithEvents CheckedListBoxMembres As System.Windows.Forms.CheckedListBox
    Friend WithEvents ButtonNouveauGroupe As System.Windows.Forms.Button
    Friend WithEvents ButtonSupprimerGroupe As System.Windows.Forms.Button
    Friend WithEvents ButtonTestGroupeOn As System.Windows.Forms.Button
    Friend WithEvents ButtonTestGroupeOff As System.Windows.Forms.Button
    Friend WithEvents LabelAide As System.Windows.Forms.Label
    Friend WithEvents ToolTip1 As System.Windows.Forms.ToolTip
End Class
