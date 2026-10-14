<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class PanneauBlinkAuto
    Inherits System.Windows.Forms.UserControl

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
        Me.CheckBoxActif = New System.Windows.Forms.CheckBox()
        Me.LabelIntervalle = New System.Windows.Forms.Label()
        Me.NumericUpDownIntervalle = New System.Windows.Forms.NumericUpDown()
        Me.LabelSecondes = New System.Windows.Forms.Label()
        Me.GroupBoxAffichage = New System.Windows.Forms.GroupBox()
        Me.RadioButtonPip = New System.Windows.Forms.RadioButton()
        Me.RadioButtonPlein = New System.Windows.Forms.RadioButton()
        Me.LabelCoin = New System.Windows.Forms.Label()
        Me.ComboBoxCoin = New System.Windows.Forms.ComboBox()
        Me.LabelTaille = New System.Windows.Forms.Label()
        Me.NumericUpDownTaille = New System.Windows.Forms.NumericUpDown()
        Me.LabelVolume = New System.Windows.Forms.Label()
        Me.NumericUpDownVolume = New System.Windows.Forms.NumericUpDown()
        Me.LabelCameras = New System.Windows.Forms.Label()
        Me.CheckedListBoxCameras = New System.Windows.Forms.CheckedListBox()
        Me.ButtonToutCocher = New System.Windows.Forms.Button()
        Me.ButtonToutDecocher = New System.Windows.Forms.Button()
        Me.LabelEtat = New System.Windows.Forms.Label()
        CType(Me.NumericUpDownIntervalle, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBoxAffichage.SuspendLayout()
        CType(Me.NumericUpDownTaille, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.NumericUpDownVolume, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'CheckBoxActif
        '
        Me.CheckBoxActif.AutoSize = True
        Me.CheckBoxActif.Location = New System.Drawing.Point(16, 16)
        Me.CheckBoxActif.Name = "CheckBoxActif"
        Me.CheckBoxActif.Size = New System.Drawing.Size(520, 24)
        Me.CheckBoxActif.TabIndex = 0
        Me.CheckBoxActif.Text = "Afficher automatiquement chaque nouvel enregistrement"
        Me.CheckBoxActif.UseVisualStyleBackColor = True
        '
        'LabelIntervalle
        '
        Me.LabelIntervalle.AutoSize = True
        Me.LabelIntervalle.Location = New System.Drawing.Point(16, 58)
        Me.LabelIntervalle.Name = "LabelIntervalle"
        Me.LabelIntervalle.Size = New System.Drawing.Size(150, 20)
        Me.LabelIntervalle.TabIndex = 1
        Me.LabelIntervalle.Text = "Vérifier toutes les"
        '
        'NumericUpDownIntervalle
        '
        Me.NumericUpDownIntervalle.Location = New System.Drawing.Point(190, 54)
        Me.NumericUpDownIntervalle.Maximum = New Decimal(New Integer() {600, 0, 0, 0})
        Me.NumericUpDownIntervalle.Minimum = New Decimal(New Integer() {15, 0, 0, 0})
        Me.NumericUpDownIntervalle.Name = "NumericUpDownIntervalle"
        Me.NumericUpDownIntervalle.Size = New System.Drawing.Size(80, 27)
        Me.NumericUpDownIntervalle.TabIndex = 2
        Me.NumericUpDownIntervalle.Value = New Decimal(New Integer() {45, 0, 0, 0})
        '
        'LabelSecondes
        '
        Me.LabelSecondes.AutoSize = True
        Me.LabelSecondes.Location = New System.Drawing.Point(280, 58)
        Me.LabelSecondes.Name = "LabelSecondes"
        Me.LabelSecondes.Size = New System.Drawing.Size(76, 20)
        Me.LabelSecondes.TabIndex = 3
        Me.LabelSecondes.Text = "secondes"
        '
        'GroupBoxAffichage
        '
        Me.GroupBoxAffichage.Controls.Add(Me.NumericUpDownVolume)
        Me.GroupBoxAffichage.Controls.Add(Me.LabelVolume)
        Me.GroupBoxAffichage.Controls.Add(Me.NumericUpDownTaille)
        Me.GroupBoxAffichage.Controls.Add(Me.LabelTaille)
        Me.GroupBoxAffichage.Controls.Add(Me.ComboBoxCoin)
        Me.GroupBoxAffichage.Controls.Add(Me.LabelCoin)
        Me.GroupBoxAffichage.Controls.Add(Me.RadioButtonPlein)
        Me.GroupBoxAffichage.Controls.Add(Me.RadioButtonPip)
        Me.GroupBoxAffichage.Location = New System.Drawing.Point(16, 98)
        Me.GroupBoxAffichage.Name = "GroupBoxAffichage"
        Me.GroupBoxAffichage.Size = New System.Drawing.Size(528, 196)
        Me.GroupBoxAffichage.TabIndex = 4
        Me.GroupBoxAffichage.TabStop = False
        Me.GroupBoxAffichage.Text = "Affichage (le clip est joué en entier)"
        '
        'RadioButtonPip
        '
        Me.RadioButtonPip.AutoSize = True
        Me.RadioButtonPip.Checked = True
        Me.RadioButtonPip.Location = New System.Drawing.Point(16, 32)
        Me.RadioButtonPip.Name = "RadioButtonPip"
        Me.RadioButtonPip.Size = New System.Drawing.Size(80, 24)
        Me.RadioButtonPip.TabIndex = 0
        Me.RadioButtonPip.TabStop = True
        Me.RadioButtonPip.Text = "En PIP"
        Me.RadioButtonPip.UseVisualStyleBackColor = True
        '
        'RadioButtonPlein
        '
        Me.RadioButtonPlein.AutoSize = True
        Me.RadioButtonPlein.Location = New System.Drawing.Point(150, 32)
        Me.RadioButtonPlein.Name = "RadioButtonPlein"
        Me.RadioButtonPlein.Size = New System.Drawing.Size(140, 24)
        Me.RadioButtonPlein.TabIndex = 1
        Me.RadioButtonPlein.Text = "En plein écran"
        Me.RadioButtonPlein.UseVisualStyleBackColor = True
        '
        'LabelCoin
        '
        Me.LabelCoin.AutoSize = True
        Me.LabelCoin.Location = New System.Drawing.Point(16, 76)
        Me.LabelCoin.Name = "LabelCoin"
        Me.LabelCoin.Size = New System.Drawing.Size(86, 20)
        Me.LabelCoin.TabIndex = 2
        Me.LabelCoin.Text = "Emplacement"
        '
        'ComboBoxCoin
        '
        Me.ComboBoxCoin.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxCoin.FormattingEnabled = True
        Me.ComboBoxCoin.Location = New System.Drawing.Point(190, 72)
        Me.ComboBoxCoin.Name = "ComboBoxCoin"
        Me.ComboBoxCoin.Size = New System.Drawing.Size(240, 28)
        Me.ComboBoxCoin.TabIndex = 3
        '
        'LabelTaille
        '
        Me.LabelTaille.AutoSize = True
        Me.LabelTaille.Location = New System.Drawing.Point(16, 116)
        Me.LabelTaille.Name = "LabelTaille"
        Me.LabelTaille.Size = New System.Drawing.Size(130, 20)
        Me.LabelTaille.TabIndex = 4
        Me.LabelTaille.Text = "Taille du PIP (%)"
        '
        'NumericUpDownTaille
        '
        Me.NumericUpDownTaille.Location = New System.Drawing.Point(190, 112)
        Me.NumericUpDownTaille.Minimum = New Decimal(New Integer() {5, 0, 0, 0})
        Me.NumericUpDownTaille.Name = "NumericUpDownTaille"
        Me.NumericUpDownTaille.Size = New System.Drawing.Size(80, 27)
        Me.NumericUpDownTaille.TabIndex = 5
        Me.NumericUpDownTaille.Value = New Decimal(New Integer() {30, 0, 0, 0})
        '
        'LabelVolume
        '
        Me.LabelVolume.AutoSize = True
        Me.LabelVolume.Location = New System.Drawing.Point(16, 156)
        Me.LabelVolume.Name = "LabelVolume"
        Me.LabelVolume.Size = New System.Drawing.Size(150, 20)
        Me.LabelVolume.TabIndex = 6
        Me.LabelVolume.Text = "Volume (0 = muet)"
        '
        'NumericUpDownVolume
        '
        Me.NumericUpDownVolume.Location = New System.Drawing.Point(190, 152)
        Me.NumericUpDownVolume.Name = "NumericUpDownVolume"
        Me.NumericUpDownVolume.Size = New System.Drawing.Size(80, 27)
        Me.NumericUpDownVolume.TabIndex = 7
        '
        'LabelCameras
        '
        Me.LabelCameras.AutoSize = True
        Me.LabelCameras.Location = New System.Drawing.Point(16, 308)
        Me.LabelCameras.Name = "LabelCameras"
        Me.LabelCameras.Size = New System.Drawing.Size(400, 20)
        Me.LabelCameras.TabIndex = 5
        Me.LabelCameras.Text = "Caméras à afficher :"
        '
        'CheckedListBoxCameras
        '
        Me.CheckedListBoxCameras.CheckOnClick = True
        Me.CheckedListBoxCameras.FormattingEnabled = True
        Me.CheckedListBoxCameras.Location = New System.Drawing.Point(16, 334)
        Me.CheckedListBoxCameras.Name = "CheckedListBoxCameras"
        Me.CheckedListBoxCameras.Size = New System.Drawing.Size(528, 114)
        Me.CheckedListBoxCameras.TabIndex = 6
        '
        'ButtonToutCocher
        '
        Me.ButtonToutCocher.Location = New System.Drawing.Point(16, 458)
        Me.ButtonToutCocher.Name = "ButtonToutCocher"
        Me.ButtonToutCocher.Size = New System.Drawing.Size(150, 34)
        Me.ButtonToutCocher.TabIndex = 9
        Me.ButtonToutCocher.Text = "Tout cocher"
        Me.ButtonToutCocher.UseVisualStyleBackColor = True
        '
        'ButtonToutDecocher
        '
        Me.ButtonToutDecocher.Location = New System.Drawing.Point(176, 458)
        Me.ButtonToutDecocher.Name = "ButtonToutDecocher"
        Me.ButtonToutDecocher.Size = New System.Drawing.Size(150, 34)
        Me.ButtonToutDecocher.TabIndex = 10
        Me.ButtonToutDecocher.Text = "Tout décocher"
        Me.ButtonToutDecocher.UseVisualStyleBackColor = True
        '
        'LabelEtat
        '
        Me.LabelEtat.Location = New System.Drawing.Point(16, 510)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(528, 44)
        Me.LabelEtat.TabIndex = 8
        '
        'PanneauBlinkAuto
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Size = New System.Drawing.Size(560, 618)
        Me.Controls.Add(Me.ButtonToutDecocher)
        Me.Controls.Add(Me.ButtonToutCocher)
        Me.Controls.Add(Me.LabelEtat)
        Me.Controls.Add(Me.CheckedListBoxCameras)
        Me.Controls.Add(Me.LabelCameras)
        Me.Controls.Add(Me.GroupBoxAffichage)
        Me.Controls.Add(Me.LabelSecondes)
        Me.Controls.Add(Me.NumericUpDownIntervalle)
        Me.Controls.Add(Me.LabelIntervalle)
        Me.Controls.Add(Me.CheckBoxActif)
        Me.Name = "PanneauBlinkAuto"
        CType(Me.NumericUpDownIntervalle, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBoxAffichage.ResumeLayout(False)
        Me.GroupBoxAffichage.PerformLayout()
        CType(Me.NumericUpDownTaille, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.NumericUpDownVolume, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents CheckBoxActif As System.Windows.Forms.CheckBox
    Friend WithEvents LabelIntervalle As System.Windows.Forms.Label
    Friend WithEvents NumericUpDownIntervalle As System.Windows.Forms.NumericUpDown
    Friend WithEvents LabelSecondes As System.Windows.Forms.Label
    Friend WithEvents GroupBoxAffichage As System.Windows.Forms.GroupBox
    Friend WithEvents RadioButtonPip As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButtonPlein As System.Windows.Forms.RadioButton
    Friend WithEvents LabelCoin As System.Windows.Forms.Label
    Friend WithEvents ComboBoxCoin As System.Windows.Forms.ComboBox
    Friend WithEvents LabelTaille As System.Windows.Forms.Label
    Friend WithEvents NumericUpDownTaille As System.Windows.Forms.NumericUpDown
    Friend WithEvents LabelVolume As System.Windows.Forms.Label
    Friend WithEvents NumericUpDownVolume As System.Windows.Forms.NumericUpDown
    Friend WithEvents LabelCameras As System.Windows.Forms.Label
    Friend WithEvents CheckedListBoxCameras As System.Windows.Forms.CheckedListBox
    Friend WithEvents LabelEtat As System.Windows.Forms.Label
    Friend WithEvents ButtonToutCocher As System.Windows.Forms.Button
    Friend WithEvents ButtonToutDecocher As System.Windows.Forms.Button
End Class
