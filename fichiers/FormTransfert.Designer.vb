<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormTransfert
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

    'Requise par le Concepteur Windows Form
    Private components As System.ComponentModel.IContainer

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.LabelTexte = New System.Windows.Forms.Label()
        Me.CheckBoxMinuterie = New System.Windows.Forms.CheckBox()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelTexte
        '
        Me.LabelTexte.Location = New System.Drawing.Point(16, 14)
        Me.LabelTexte.Name = "LabelTexte"
        Me.LabelTexte.Size = New System.Drawing.Size(560, 90)
        Me.LabelTexte.TabIndex = 0
        Me.LabelTexte.Text = "Texte"
        '
        'CheckBoxMinuterie
        '
        Me.CheckBoxMinuterie.AutoSize = True
        Me.CheckBoxMinuterie.Checked = True
        Me.CheckBoxMinuterie.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBoxMinuterie.Location = New System.Drawing.Point(16, 116)
        Me.CheckBoxMinuterie.Name = "CheckBoxMinuterie"
        Me.CheckBoxMinuterie.Size = New System.Drawing.Size(210, 24)
        Me.CheckBoxMinuterie.TabIndex = 1
        Me.CheckBoxMinuterie.Text = "Avec la minuterie"
        Me.CheckBoxMinuterie.UseVisualStyleBackColor = True
        '
        'ButtonOK
        '
        Me.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOK.Location = New System.Drawing.Point(316, 160)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(120, 36)
        Me.ButtonOK.TabIndex = 2
        Me.ButtonOK.Text = "Continuer"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(456, 160)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(120, 36)
        Me.ButtonAnnuler.TabIndex = 3
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormTransfert
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(594, 212)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.CheckBoxMinuterie)
        Me.Controls.Add(Me.LabelTexte)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormTransfert"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Paramètres"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelTexte As Label
    Friend WithEvents CheckBoxMinuterie As CheckBox
    Friend WithEvents ButtonOK As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
