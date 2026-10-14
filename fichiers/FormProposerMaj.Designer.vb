<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormProposerMaj
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
        Me.LabelTitre = New System.Windows.Forms.Label()
        Me.LabelDetail = New System.Windows.Forms.Label()
        Me.LabelCompte = New System.Windows.Forms.Label()
        Me.ProgressBarMaj = New System.Windows.Forms.ProgressBar()
        Me.ButtonMaj = New System.Windows.Forms.Button()
        Me.ButtonPlusTard = New System.Windows.Forms.Button()
        Me.TimerCompte = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'LabelTitre
        '
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI", 13.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTitre.Location = New System.Drawing.Point(20, 18)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(600, 36)
        Me.LabelTitre.TabIndex = 0
        Me.LabelTitre.Text = "Nouvelle version de Slide"
        '
        'LabelDetail
        '
        Me.LabelDetail.Location = New System.Drawing.Point(20, 64)
        Me.LabelDetail.Name = "LabelDetail"
        Me.LabelDetail.Size = New System.Drawing.Size(600, 110)
        Me.LabelDetail.TabIndex = 1
        '
        'LabelCompte
        '
        Me.LabelCompte.Location = New System.Drawing.Point(20, 182)
        Me.LabelCompte.Name = "LabelCompte"
        Me.LabelCompte.Size = New System.Drawing.Size(600, 28)
        Me.LabelCompte.TabIndex = 2
        '
        'ProgressBarMaj
        '
        Me.ProgressBarMaj.Location = New System.Drawing.Point(20, 214)
        Me.ProgressBarMaj.Name = "ProgressBarMaj"
        Me.ProgressBarMaj.Size = New System.Drawing.Size(600, 20)
        Me.ProgressBarMaj.TabIndex = 3
        Me.ProgressBarMaj.Visible = False
        '
        'ButtonMaj
        '
        Me.ButtonMaj.Location = New System.Drawing.Point(20, 250)
        Me.ButtonMaj.Name = "ButtonMaj"
        Me.ButtonMaj.Size = New System.Drawing.Size(300, 44)
        Me.ButtonMaj.TabIndex = 4
        Me.ButtonMaj.Text = "Mettre à jour maintenant"
        Me.ButtonMaj.UseVisualStyleBackColor = True
        '
        'ButtonPlusTard
        '
        Me.ButtonPlusTard.Location = New System.Drawing.Point(330, 250)
        Me.ButtonPlusTard.Name = "ButtonPlusTard"
        Me.ButtonPlusTard.Size = New System.Drawing.Size(290, 44)
        Me.ButtonPlusTard.TabIndex = 5
        Me.ButtonPlusTard.Text = "Plus tard"
        Me.ButtonPlusTard.UseVisualStyleBackColor = True
        '
        'TimerCompte
        '
        Me.TimerCompte.Interval = 1000
        '
        'FormProposerMaj
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(640, 316)
        Me.Controls.Add(Me.ButtonPlusTard)
        Me.Controls.Add(Me.ButtonMaj)
        Me.Controls.Add(Me.ProgressBarMaj)
        Me.Controls.Add(Me.LabelCompte)
        Me.Controls.Add(Me.LabelDetail)
        Me.Controls.Add(Me.LabelTitre)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormProposerMaj"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Mise à jour de Slide"
        Me.TopMost = True
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents LabelTitre As Label
    Friend WithEvents LabelDetail As Label
    Friend WithEvents LabelCompte As Label
    Friend WithEvents ProgressBarMaj As ProgressBar
    Friend WithEvents ButtonMaj As Button
    Friend WithEvents ButtonPlusTard As Button
    Friend WithEvents TimerCompte As Timer
End Class
