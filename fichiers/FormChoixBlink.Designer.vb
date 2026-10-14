<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormChoixBlink
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
        Me.ButtonPlusieurs = New System.Windows.Forms.Button()
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
        Me.LabelAide.Text = "Choisissez la caméra Blink à diffuser (comme une chaîne : PIP ou plein écran, avec ou sans son)."
        '
        'ListBoxCameras
        '
        Me.ListBoxCameras.FormattingEnabled = True
        Me.ListBoxCameras.ItemHeight = 20
        Me.ListBoxCameras.Location = New System.Drawing.Point(16, 62)
        Me.ListBoxCameras.Name = "ListBoxCameras"
        Me.ListBoxCameras.Size = New System.Drawing.Size(468, 304)
        Me.ListBoxCameras.TabIndex = 1
        '
        'ButtonPlusieurs
        '
        Me.ButtonPlusieurs.Location = New System.Drawing.Point(16, 380)
        Me.ButtonPlusieurs.Name = "ButtonPlusieurs"
        Me.ButtonPlusieurs.Size = New System.Drawing.Size(222, 38)
        Me.ButtonPlusieurs.TabIndex = 4
        Me.ButtonPlusieurs.Text = "Plusieurs caméras..."
        Me.ButtonPlusieurs.UseVisualStyleBackColor = True
        '
        'ButtonOk
        '
        Me.ButtonOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOk.Location = New System.Drawing.Point(254, 380)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(110, 38)
        Me.ButtonOk.TabIndex = 2
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(374, 380)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(110, 38)
        Me.ButtonAnnuler.TabIndex = 3
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormChoixBlink
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(500, 434)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonPlusieurs)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.ListBoxCameras)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormChoixBlink"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Caméra Blink"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelAide As System.Windows.Forms.Label
    Friend WithEvents ListBoxCameras As System.Windows.Forms.ListBox
    Friend WithEvents ButtonPlusieurs As System.Windows.Forms.Button
    Friend WithEvents ButtonOk As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
