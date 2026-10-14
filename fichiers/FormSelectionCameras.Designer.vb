<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSelectionCameras
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
        Me.CheckedListBoxCameras = New System.Windows.Forms.CheckedListBox()
        Me.ButtonToutCocher = New System.Windows.Forms.Button()
        Me.ButtonToutDecocher = New System.Windows.Forms.Button()
        Me.LabelCompte = New System.Windows.Forms.Label()
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
        Me.LabelAide.Text = "Cochez les caméras à afficher ensemble (au moins deux). La grille s'adapte à leur nombre."
        '
        'CheckedListBoxCameras
        '
        Me.CheckedListBoxCameras.CheckOnClick = True
        Me.CheckedListBoxCameras.FormattingEnabled = True
        Me.CheckedListBoxCameras.Location = New System.Drawing.Point(16, 62)
        Me.CheckedListBoxCameras.MultiColumn = True
        Me.CheckedListBoxCameras.Name = "CheckedListBoxCameras"
        Me.CheckedListBoxCameras.ColumnWidth = 150
        Me.CheckedListBoxCameras.Size = New System.Drawing.Size(628, 364)
        Me.CheckedListBoxCameras.TabIndex = 1
        '
        'ButtonToutCocher
        '
        Me.ButtonToutCocher.Location = New System.Drawing.Point(16, 440)
        Me.ButtonToutCocher.Name = "ButtonToutCocher"
        Me.ButtonToutCocher.Size = New System.Drawing.Size(140, 38)
        Me.ButtonToutCocher.TabIndex = 2
        Me.ButtonToutCocher.Text = "Tout cocher"
        Me.ButtonToutCocher.UseVisualStyleBackColor = True
        '
        'ButtonToutDecocher
        '
        Me.ButtonToutDecocher.Location = New System.Drawing.Point(166, 440)
        Me.ButtonToutDecocher.Name = "ButtonToutDecocher"
        Me.ButtonToutDecocher.Size = New System.Drawing.Size(150, 38)
        Me.ButtonToutDecocher.TabIndex = 3
        Me.ButtonToutDecocher.Text = "Tout décocher"
        Me.ButtonToutDecocher.UseVisualStyleBackColor = True
        '
        'LabelCompte
        '
        Me.LabelCompte.Location = New System.Drawing.Point(16, 490)
        Me.LabelCompte.Name = "LabelCompte"
        Me.LabelCompte.Size = New System.Drawing.Size(400, 26)
        Me.LabelCompte.TabIndex = 4
        '
        'ButtonOk
        '
        Me.ButtonOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOk.Location = New System.Drawing.Point(414, 484)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(110, 38)
        Me.ButtonOk.TabIndex = 5
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(534, 484)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(110, 38)
        Me.ButtonAnnuler.TabIndex = 6
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormSelectionCameras
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(660, 538)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.LabelCompte)
        Me.Controls.Add(Me.ButtonToutDecocher)
        Me.Controls.Add(Me.ButtonToutCocher)
        Me.Controls.Add(Me.CheckedListBoxCameras)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormSelectionCameras"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Caméras de l'écran"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelAide As System.Windows.Forms.Label
    Friend WithEvents CheckedListBoxCameras As System.Windows.Forms.CheckedListBox
    Friend WithEvents ButtonToutCocher As System.Windows.Forms.Button
    Friend WithEvents ButtonToutDecocher As System.Windows.Forms.Button
    Friend WithEvents LabelCompte As System.Windows.Forms.Label
    Friend WithEvents ButtonOk As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
