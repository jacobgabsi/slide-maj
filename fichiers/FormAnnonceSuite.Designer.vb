<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAnnonceSuite
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
        Me.LabelDans = New System.Windows.Forms.Label()
        Me.LabelSuite = New System.Windows.Forms.Label()
        Me.PanelBarre = New System.Windows.Forms.Panel()
        Me.SuspendLayout()
        '
        'LabelDans
        '
        Me.LabelDans.AutoSize = True
        Me.LabelDans.Font = New System.Drawing.Font("Segoe UI Semibold", 9.1!, System.Drawing.FontStyle.Bold)
        Me.LabelDans.ForeColor = System.Drawing.Color.White
        Me.LabelDans.Location = New System.Drawing.Point(15, 11)
        Me.LabelDans.Name = "LabelDans"
        Me.LabelDans.Size = New System.Drawing.Size(126, 21)
        Me.LabelDans.TabIndex = 0
        Me.LabelDans.Text = "À suivre dans 30 s"
        '
        'LabelSuite
        '
        Me.LabelSuite.AutoEllipsis = True
        Me.LabelSuite.Font = New System.Drawing.Font("Segoe UI", 8.0!)
        Me.LabelSuite.ForeColor = System.Drawing.Color.FromArgb(CType(CType(205, Byte), Integer), CType(CType(205, Byte), Integer), CType(CType(210, Byte), Integer))
        Me.LabelSuite.Location = New System.Drawing.Point(17, 36)
        Me.LabelSuite.Name = "LabelSuite"
        Me.LabelSuite.Size = New System.Drawing.Size(400, 20)
        Me.LabelSuite.TabIndex = 1
        Me.LabelSuite.Text = "Prochain film"
        '
        'PanelBarre
        '
        Me.PanelBarre.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(28, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.PanelBarre.Location = New System.Drawing.Point(17, 64)
        Me.PanelBarre.Name = "PanelBarre"
        Me.PanelBarre.Size = New System.Drawing.Size(400, 10)
        Me.PanelBarre.TabIndex = 2
        '
        'FormAnnonceSuite
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(28, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(434, 88)
        Me.Controls.Add(Me.PanelBarre)
        Me.Controls.Add(Me.LabelSuite)
        Me.Controls.Add(Me.LabelDans)
        Me.Cursor = System.Windows.Forms.Cursors.SizeAll
        Me.DoubleBuffered = True
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "FormAnnonceSuite"
        Me.Opacity = 0.7R
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Enchaînement"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelDans As System.Windows.Forms.Label
    Friend WithEvents LabelSuite As System.Windows.Forms.Label
    Friend WithEvents PanelBarre As System.Windows.Forms.Panel
End Class
