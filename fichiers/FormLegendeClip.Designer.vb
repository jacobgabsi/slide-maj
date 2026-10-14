<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormLegendeClip
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
        Me.components = New System.ComponentModel.Container()
        Me.LabelTexte = New System.Windows.Forms.Label()
        Me.LabelHeure = New System.Windows.Forms.Label()
        Me.TimerFin = New System.Windows.Forms.Timer(Me.components)
        Me.TimerSuivi = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'LabelTexte
        '
        Me.LabelTexte.AutoSize = True
        Me.LabelTexte.Font = New System.Drawing.Font("Segoe UI Semibold", 14.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTexte.ForeColor = System.Drawing.Color.White
        Me.LabelTexte.Location = New System.Drawing.Point(10, 6)
        Me.LabelTexte.Name = "LabelTexte"
        Me.LabelTexte.Size = New System.Drawing.Size(120, 32)
        Me.LabelTexte.TabIndex = 0
        Me.LabelTexte.Text = "Caméra"
        '
        'LabelHeure
        '
        Me.LabelHeure.AutoSize = True
        Me.LabelHeure.Font = New System.Drawing.Font("Segoe UI Semibold", 7.0!, System.Drawing.FontStyle.Bold)
        Me.LabelHeure.ForeColor = System.Drawing.Color.White
        Me.LabelHeure.Location = New System.Drawing.Point(6, 4)
        Me.LabelHeure.Name = "LabelHeure"
        Me.LabelHeure.Size = New System.Drawing.Size(40, 16)
        Me.LabelHeure.TabIndex = 1
        Me.LabelHeure.Text = "00:00"
        '
        'TimerFin
        '
        Me.TimerFin.Interval = 6000
        '
        'TimerSuivi
        '
        Me.TimerSuivi.Interval = 300
        '
        'FormLegendeClip
        '
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(8, Byte), Integer), CType(CType(8, Byte), Integer), CType(CType(8, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(200, 44)
        Me.Controls.Add(Me.LabelHeure)
        Me.Controls.Add(Me.LabelTexte)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.Name = "FormLegendeClip"
        Me.Opacity = 0.92R
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "FormLegendeClip"
        Me.TransparencyKey = System.Drawing.Color.FromArgb(CType(CType(8, Byte), Integer), CType(CType(8, Byte), Integer), CType(CType(8, Byte), Integer))
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelTexte As System.Windows.Forms.Label
    Friend WithEvents LabelHeure As System.Windows.Forms.Label
    Friend WithEvents TimerFin As System.Windows.Forms.Timer
    Friend WithEvents TimerSuivi As System.Windows.Forms.Timer
End Class
