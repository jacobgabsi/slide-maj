<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormChoixCompteTuya
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
        Me.LabelTexte = New System.Windows.Forms.Label()
        Me.ListBoxComptes = New System.Windows.Forms.ListBox()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelTexte
        '
        Me.LabelTexte.Location = New System.Drawing.Point(16, 14)
        Me.LabelTexte.Name = "LabelTexte"
        Me.LabelTexte.Size = New System.Drawing.Size(488, 56)
        Me.LabelTexte.TabIndex = 0
        Me.LabelTexte.Text = "Tuya : de quelle maison est l'appareil de cette ligne ?"
        '
        'ListBoxComptes
        '
        Me.ListBoxComptes.FormattingEnabled = True
        Me.ListBoxComptes.ItemHeight = 25
        Me.ListBoxComptes.Location = New System.Drawing.Point(16, 76)
        Me.ListBoxComptes.Name = "ListBoxComptes"
        Me.ListBoxComptes.Size = New System.Drawing.Size(488, 179)
        Me.ListBoxComptes.TabIndex = 1
        '
        'ButtonOK
        '
        Me.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOK.Location = New System.Drawing.Point(244, 272)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(120, 40)
        Me.ButtonOK.TabIndex = 2
        Me.ButtonOK.Text = "OK"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(384, 272)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(120, 40)
        Me.ButtonAnnuler.TabIndex = 3
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormChoixCompteTuya
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(11.0!, 25.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(520, 326)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.ListBoxComptes)
        Me.Controls.Add(Me.LabelTexte)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormChoixCompteTuya"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Maison Tuya de la ligne"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelTexte As System.Windows.Forms.Label
    Friend WithEvents ListBoxComptes As System.Windows.Forms.ListBox
    Friend WithEvents ButtonOK As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
