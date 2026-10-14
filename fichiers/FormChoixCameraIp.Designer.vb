<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormChoixCameraIp
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
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.ListBoxCameras = New System.Windows.Forms.ListBox()
        Me.LabelEcran = New System.Windows.Forms.Label()
        Me.ComboBoxEcran = New System.Windows.Forms.ComboBox()
        Me.CheckBoxLeger = New System.Windows.Forms.CheckBox()
        Me.ButtonSelection = New System.Windows.Forms.Button()
        Me.ButtonOk = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(16, 12)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(468, 44)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Choisissez la caméra de l'enregistreur à diffuser (comme une chaîne : PIP ou plein écran, avec ou sans son)."
        '
        'ListBoxCameras
        '
        Me.ListBoxCameras.FormattingEnabled = True
        Me.ListBoxCameras.ItemHeight = 20
        Me.ListBoxCameras.Location = New System.Drawing.Point(16, 62)
        Me.ListBoxCameras.Name = "ListBoxCameras"
        Me.ListBoxCameras.Size = New System.Drawing.Size(468, 284)
        Me.ListBoxCameras.TabIndex = 1
        '
        'LabelEcran
        '
        Me.LabelEcran.AutoSize = True
        Me.LabelEcran.Location = New System.Drawing.Point(16, 362)
        Me.LabelEcran.Name = "LabelEcran"
        Me.LabelEcran.Size = New System.Drawing.Size(144, 20)
        Me.LabelEcran.TabIndex = 5
        Me.LabelEcran.Text = "Écran (1ère caméra) :"
        '
        'ComboBoxEcran
        '
        Me.ComboBoxEcran.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxEcran.FormattingEnabled = True
        Me.ComboBoxEcran.Location = New System.Drawing.Point(220, 358)
        Me.ComboBoxEcran.Name = "ComboBoxEcran"
        Me.ComboBoxEcran.Size = New System.Drawing.Size(264, 28)
        Me.ComboBoxEcran.TabIndex = 2
        '
        'CheckBoxLeger
        '
        Me.CheckBoxLeger.AutoSize = True
        Me.CheckBoxLeger.Location = New System.Drawing.Point(16, 398)
        Me.CheckBoxLeger.Name = "CheckBoxLeger"
        Me.CheckBoxLeger.Size = New System.Drawing.Size(334, 24)
        Me.CheckBoxLeger.TabIndex = 3
        Me.CheckBoxLeger.Text = "Flux léger (conseillé pour un PIP ou plusieurs caméras)"
        Me.CheckBoxLeger.UseVisualStyleBackColor = True
        '
        'ButtonSelection
        '
        Me.ButtonSelection.Location = New System.Drawing.Point(16, 440)
        Me.ButtonSelection.Name = "ButtonSelection"
        Me.ButtonSelection.Size = New System.Drawing.Size(222, 38)
        Me.ButtonSelection.TabIndex = 7
        Me.ButtonSelection.Text = "Plusieurs caméras..."
        Me.ButtonSelection.UseVisualStyleBackColor = True
        '
        'ButtonOk
        '
        Me.ButtonOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOk.Location = New System.Drawing.Point(254, 440)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(110, 38)
        Me.ButtonOk.TabIndex = 4
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(374, 440)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(110, 38)
        Me.ButtonAnnuler.TabIndex = 6
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormChoixCameraIp
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(500, 494)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonSelection)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.CheckBoxLeger)
        Me.Controls.Add(Me.ComboBoxEcran)
        Me.Controls.Add(Me.LabelEcran)
        Me.Controls.Add(Me.ListBoxCameras)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormChoixCameraIp"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Caméra IP"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAide As System.Windows.Forms.Label
    Friend WithEvents ListBoxCameras As System.Windows.Forms.ListBox
    Friend WithEvents LabelEcran As System.Windows.Forms.Label
    Friend WithEvents ComboBoxEcran As System.Windows.Forms.ComboBox
    Friend WithEvents CheckBoxLeger As System.Windows.Forms.CheckBox
    Friend WithEvents ButtonSelection As System.Windows.Forms.Button
    Friend WithEvents ButtonOk As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
