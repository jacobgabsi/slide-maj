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
        'FormChoixCompteTuya
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(11.0!, 25.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(520, 272)
        Me.KeyPreview = True
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
End Class
