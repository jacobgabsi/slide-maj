<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormChoixImport
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
        Me.CheckedListBoxChoix = New System.Windows.Forms.CheckedListBox()
        Me.ButtonToutCocher = New System.Windows.Forms.Button()
        Me.ButtonAucun = New System.Windows.Forms.Button()
        Me.LabelAvertissement = New System.Windows.Forms.Label()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelTexte
        '
        Me.LabelTexte.Location = New System.Drawing.Point(16, 14)
        Me.LabelTexte.Name = "LabelTexte"
        Me.LabelTexte.Size = New System.Drawing.Size(620, 80)
        Me.LabelTexte.TabIndex = 0
        Me.LabelTexte.Text = "Texte"
        '
        'CheckedListBoxChoix
        '
        Me.CheckedListBoxChoix.CheckOnClick = True
        Me.CheckedListBoxChoix.FormattingEnabled = True
        Me.CheckedListBoxChoix.Location = New System.Drawing.Point(16, 100)
        Me.CheckedListBoxChoix.Name = "CheckedListBoxChoix"
        Me.CheckedListBoxChoix.Size = New System.Drawing.Size(620, 264)
        Me.CheckedListBoxChoix.TabIndex = 1
        '
        'ButtonToutCocher
        '
        Me.ButtonToutCocher.Location = New System.Drawing.Point(16, 374)
        Me.ButtonToutCocher.Name = "ButtonToutCocher"
        Me.ButtonToutCocher.Size = New System.Drawing.Size(140, 36)
        Me.ButtonToutCocher.TabIndex = 2
        Me.ButtonToutCocher.Text = "Tout cocher"
        Me.ButtonToutCocher.UseVisualStyleBackColor = True
        '
        'ButtonAucun
        '
        Me.ButtonAucun.Location = New System.Drawing.Point(166, 374)
        Me.ButtonAucun.Name = "ButtonAucun"
        Me.ButtonAucun.Size = New System.Drawing.Size(140, 36)
        Me.ButtonAucun.TabIndex = 3
        Me.ButtonAucun.Text = "Aucun"
        Me.ButtonAucun.UseVisualStyleBackColor = True
        '
        'LabelAvertissement
        '
        Me.LabelAvertissement.Location = New System.Drawing.Point(16, 420)
        Me.LabelAvertissement.Name = "LabelAvertissement"
        Me.LabelAvertissement.Size = New System.Drawing.Size(620, 64)
        Me.LabelAvertissement.TabIndex = 4
        Me.LabelAvertissement.Text = "Avertissement"
        '
        'ButtonOK
        '
        Me.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOK.Location = New System.Drawing.Point(376, 494)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(120, 38)
        Me.ButtonOK.TabIndex = 5
        Me.ButtonOK.Text = "Importer"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(516, 494)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(120, 38)
        Me.ButtonAnnuler.TabIndex = 6
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormChoixImport
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(654, 546)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.LabelAvertissement)
        Me.Controls.Add(Me.ButtonAucun)
        Me.Controls.Add(Me.ButtonToutCocher)
        Me.Controls.Add(Me.CheckedListBoxChoix)
        Me.Controls.Add(Me.LabelTexte)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormChoixImport"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Importer les paramètres"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelTexte As System.Windows.Forms.Label
    Friend WithEvents CheckedListBoxChoix As System.Windows.Forms.CheckedListBox
    Friend WithEvents ButtonToutCocher As System.Windows.Forms.Button
    Friend WithEvents ButtonAucun As System.Windows.Forms.Button
    Friend WithEvents LabelAvertissement As System.Windows.Forms.Label
    Friend WithEvents ButtonOK As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
