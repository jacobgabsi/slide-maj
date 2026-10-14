<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormMaPosition
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
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.LabelVille = New System.Windows.Forms.Label()
        Me.TextBoxVille = New System.Windows.Forms.TextBox()
        Me.ButtonChercher = New System.Windows.Forms.Button()
        Me.LabelResultat = New System.Windows.Forms.Label()
        Me.ButtonUtiliser = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelEtat
        '
        Me.LabelEtat.Location = New System.Drawing.Point(20, 16)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(600, 62)
        Me.LabelEtat.TabIndex = 0
        Me.LabelEtat.Text = "Recherche de votre position..."
        '
        'LabelVille
        '
        Me.LabelVille.AutoSize = True
        Me.LabelVille.Location = New System.Drawing.Point(20, 94)
        Me.LabelVille.Name = "LabelVille"
        Me.LabelVille.Size = New System.Drawing.Size(48, 20)
        Me.LabelVille.TabIndex = 1
        Me.LabelVille.Text = "Ville :"
        '
        'TextBoxVille
        '
        Me.TextBoxVille.Location = New System.Drawing.Point(90, 90)
        Me.TextBoxVille.Name = "TextBoxVille"
        Me.TextBoxVille.Size = New System.Drawing.Size(410, 27)
        Me.TextBoxVille.TabIndex = 2
        '
        'ButtonChercher
        '
        Me.ButtonChercher.Location = New System.Drawing.Point(515, 86)
        Me.ButtonChercher.Name = "ButtonChercher"
        Me.ButtonChercher.Size = New System.Drawing.Size(105, 36)
        Me.ButtonChercher.TabIndex = 3
        Me.ButtonChercher.Text = "Chercher"
        Me.ButtonChercher.UseVisualStyleBackColor = True
        '
        'LabelResultat
        '
        Me.LabelResultat.Font = New System.Drawing.Font("Segoe UI", 11.0!, System.Drawing.FontStyle.Bold)
        Me.LabelResultat.Location = New System.Drawing.Point(20, 140)
        Me.LabelResultat.Name = "LabelResultat"
        Me.LabelResultat.Size = New System.Drawing.Size(600, 74)
        Me.LabelResultat.TabIndex = 4
        '
        'ButtonUtiliser
        '
        Me.ButtonUtiliser.Enabled = False
        Me.ButtonUtiliser.Location = New System.Drawing.Point(290, 232)
        Me.ButtonUtiliser.Name = "ButtonUtiliser"
        Me.ButtonUtiliser.Size = New System.Drawing.Size(210, 44)
        Me.ButtonUtiliser.TabIndex = 5
        Me.ButtonUtiliser.Text = "Utiliser cette ville"
        Me.ButtonUtiliser.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(515, 232)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(105, 44)
        Me.ButtonAnnuler.TabIndex = 6
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormMaPosition
        '
        Me.AcceptButton = Me.ButtonChercher
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(640, 300)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonUtiliser)
        Me.Controls.Add(Me.LabelResultat)
        Me.Controls.Add(Me.ButtonChercher)
        Me.Controls.Add(Me.TextBoxVille)
        Me.Controls.Add(Me.LabelVille)
        Me.Controls.Add(Me.LabelEtat)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormMaPosition"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Ma position"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelEtat As System.Windows.Forms.Label
    Friend WithEvents LabelVille As System.Windows.Forms.Label
    Friend WithEvents TextBoxVille As System.Windows.Forms.TextBox
    Friend WithEvents ButtonChercher As System.Windows.Forms.Button
    Friend WithEvents LabelResultat As System.Windows.Forms.Label
    Friend WithEvents ButtonUtiliser As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
