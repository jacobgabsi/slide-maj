<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormEpisodes
    Inherits System.Windows.Forms.Form

    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.LabelSerie = New System.Windows.Forms.Label()
        Me.ListBoxEpisodes = New System.Windows.Forms.ListBox()
        Me.ButtonLire = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelSerie
        '
        Me.LabelSerie.AutoEllipsis = True
        Me.LabelSerie.Location = New System.Drawing.Point(12, 12)
        Me.LabelSerie.Name = "LabelSerie"
        Me.LabelSerie.Size = New System.Drawing.Size(616, 52)
        Me.LabelSerie.TabIndex = 0
        Me.LabelSerie.Text = "Série"
        '
        'ListBoxEpisodes
        '
        Me.ListBoxEpisodes.FormattingEnabled = True
        Me.ListBoxEpisodes.HorizontalScrollbar = True
        Me.ListBoxEpisodes.ItemHeight = 20
        Me.ListBoxEpisodes.Location = New System.Drawing.Point(12, 70)
        Me.ListBoxEpisodes.Name = "ListBoxEpisodes"
        Me.ListBoxEpisodes.Size = New System.Drawing.Size(616, 384)
        Me.ListBoxEpisodes.TabIndex = 1
        '
        'ButtonLire
        '
        Me.ButtonLire.Location = New System.Drawing.Point(12, 470)
        Me.ButtonLire.Name = "ButtonLire"
        Me.ButtonLire.Size = New System.Drawing.Size(304, 38)
        Me.ButtonLire.TabIndex = 2
        Me.ButtonLire.Text = "Choisir cet épisode"
        Me.ButtonLire.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(324, 470)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(304, 38)
        Me.ButtonAnnuler.TabIndex = 3
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormEpisodes
        '
        Me.AcceptButton = Me.ButtonLire
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(640, 520)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonLire)
        Me.Controls.Add(Me.ListBoxEpisodes)
        Me.Controls.Add(Me.LabelSerie)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormEpisodes"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Épisodes"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents LabelSerie As Label
    Friend WithEvents ListBoxEpisodes As ListBox
    Friend WithEvents ButtonLire As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
