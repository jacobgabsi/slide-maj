<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormRechercheNflix
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
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.TextBoxTexte = New System.Windows.Forms.TextBox()
        Me.CheckBoxGroupe = New System.Windows.Forms.CheckBox()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(16, 14)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(560, 46)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Nom ou groupe à chercher (plusieurs mots : tous doivent y être)." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Laisser vide pour tout réafficher."
        '
        'TextBoxTexte
        '
        Me.TextBoxTexte.Location = New System.Drawing.Point(16, 68)
        Me.TextBoxTexte.Name = "TextBoxTexte"
        Me.TextBoxTexte.Size = New System.Drawing.Size(560, 27)
        Me.TextBoxTexte.TabIndex = 1
        '
        'CheckBoxGroupe
        '
        Me.CheckBoxGroupe.AutoSize = True
        Me.CheckBoxGroupe.Location = New System.Drawing.Point(16, 108)
        Me.CheckBoxGroupe.Name = "CheckBoxGroupe"
        Me.CheckBoxGroupe.Size = New System.Drawing.Size(178, 24)
        Me.CheckBoxGroupe.TabIndex = 2
        Me.CheckBoxGroupe.Text = "Dans ce groupe seulement"
        Me.CheckBoxGroupe.UseVisualStyleBackColor = True
        '
        'ButtonOK
        '
        Me.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOK.Location = New System.Drawing.Point(316, 150)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(120, 36)
        Me.ButtonOK.TabIndex = 3
        Me.ButtonOK.Text = "Rechercher"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(456, 150)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(120, 36)
        Me.ButtonAnnuler.TabIndex = 4
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormRechercheNflix
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(594, 202)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.CheckBoxGroupe)
        Me.Controls.Add(Me.TextBoxTexte)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormRechercheNflix"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Rechercher"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAide As Label
    Friend WithEvents TextBoxTexte As TextBox
    Friend WithEvents CheckBoxGroupe As CheckBox
    Friend WithEvents ButtonOK As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
