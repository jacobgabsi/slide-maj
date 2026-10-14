<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormMessage
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
        Me.components = New System.ComponentModel.Container()
        Me.PictureBoxIcone = New System.Windows.Forms.PictureBox()
        Me.LabelTitre = New System.Windows.Forms.Label()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TimerDecompte = New System.Windows.Forms.Timer(Me.components)
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.ButtonOptions = New System.Windows.Forms.Button()
        CType(Me.PictureBoxIcone, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'PictureBoxIcone
        '
        Me.PictureBoxIcone.BackColor = System.Drawing.Color.Transparent
        Me.PictureBoxIcone.Location = New System.Drawing.Point(24, 24)
        Me.PictureBoxIcone.Name = "PictureBoxIcone"
        Me.PictureBoxIcone.Size = New System.Drawing.Size(44, 44)
        Me.PictureBoxIcone.TabIndex = 1
        Me.PictureBoxIcone.TabStop = False
        '
        'LabelTitre
        '
        Me.LabelTitre.AutoSize = True
        Me.LabelTitre.BackColor = System.Drawing.Color.Transparent
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point)
        Me.LabelTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(247, Byte), Integer))
        Me.LabelTitre.Location = New System.Drawing.Point(80, 21)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(55, 25)
        Me.LabelTitre.TabIndex = 2
        Me.LabelTitre.Text = "Slide"
        '
        'Label1
        '
        Me.Label1.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Label1.BackColor = System.Drawing.Color.Transparent
        Me.Label1.Font = New System.Drawing.Font("Segoe UI", 10.5!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.Label1.ForeColor = System.Drawing.Color.FromArgb(CType(CType(200, Byte), Integer), CType(CType(200, Byte), Integer), CType(CType(205, Byte), Integer))
        Me.Label1.Location = New System.Drawing.Point(82, 52)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(414, 70)
        Me.Label1.TabIndex = 0
        Me.Label1.Text = "Message"
        '
        'TimerDecompte
        '
        Me.TimerDecompte.Interval = 50
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.BackColor = System.Drawing.Color.FromArgb(CType(CType(58, Byte), Integer), CType(CType(58, Byte), Integer), CType(CType(62, Byte), Integer))
        Me.ButtonAnnuler.FlatAppearance.BorderSize = 0
        Me.ButtonAnnuler.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonAnnuler.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.ButtonAnnuler.ForeColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(247, Byte), Integer))
        Me.ButtonAnnuler.Location = New System.Drawing.Point(396, 110)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(100, 32)
        Me.ButtonAnnuler.TabIndex = 4
        Me.ButtonAnnuler.TabStop = False
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = False
        Me.ButtonAnnuler.Visible = False
        '
        'ButtonOptions
        '
        Me.ButtonOptions.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(132, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.ButtonOptions.FlatAppearance.BorderSize = 0
        Me.ButtonOptions.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonOptions.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.ButtonOptions.ForeColor = System.Drawing.Color.White
        Me.ButtonOptions.Location = New System.Drawing.Point(286, 110)
        Me.ButtonOptions.Name = "ButtonOptions"
        Me.ButtonOptions.Size = New System.Drawing.Size(100, 32)
        Me.ButtonOptions.TabIndex = 3
        Me.ButtonOptions.TabStop = False
        Me.ButtonOptions.Text = "Options"
        Me.ButtonOptions.UseVisualStyleBackColor = False
        Me.ButtonOptions.Visible = False
        '
        'FormMessage
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(28, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(520, 146)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOptions)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.LabelTitre)
        Me.Controls.Add(Me.PictureBoxIcone)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormMessage"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Message"
        CType(Me.PictureBoxIcone, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents Label1 As Label
    Friend WithEvents PictureBoxIcone As PictureBox
    Friend WithEvents LabelTitre As Label
    Friend WithEvents TimerDecompte As Timer
    Friend WithEvents ButtonAnnuler As Button
    Friend WithEvents ButtonOptions As Button
End Class
