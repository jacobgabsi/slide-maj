<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormReglages
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

    'Requise par le Concepteur Windows Form
    Private components As System.ComponentModel.IContainer

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.PanelListe = New System.Windows.Forms.Panel()
        Me.PanelResume = New System.Windows.Forms.Panel()
        Me.LabelResumeTitre = New System.Windows.Forms.Label()
        Me.LabelResume = New System.Windows.Forms.Label()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.PanelResume.SuspendLayout()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(16, 12)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(608, 44)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Cochez les réglages que cette tâche change, puis choisissez leur valeur. Ce qui n'est pas coché n'est pas touché."
        '
        'PanelListe
        '
        Me.PanelListe.AutoScroll = True
        Me.PanelListe.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PanelListe.Location = New System.Drawing.Point(16, 60)
        Me.PanelListe.Name = "PanelListe"
        Me.PanelListe.Size = New System.Drawing.Size(608, 380)
        Me.PanelListe.TabIndex = 1
        '
        'PanelResume
        '
        Me.PanelResume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PanelResume.Controls.Add(Me.LabelResume)
        Me.PanelResume.Controls.Add(Me.LabelResumeTitre)
        Me.PanelResume.Location = New System.Drawing.Point(16, 450)
        Me.PanelResume.Name = "PanelResume"
        Me.PanelResume.Size = New System.Drawing.Size(608, 74)
        Me.PanelResume.TabIndex = 2
        '
        'LabelResumeTitre
        '
        Me.LabelResumeTitre.AutoSize = True
        Me.LabelResumeTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 9.0!, System.Drawing.FontStyle.Bold)
        Me.LabelResumeTitre.Location = New System.Drawing.Point(10, 8)
        Me.LabelResumeTitre.Name = "LabelResumeTitre"
        Me.LabelResumeTitre.Size = New System.Drawing.Size(210, 20)
        Me.LabelResumeTitre.TabIndex = 0
        Me.LabelResumeTitre.Text = "Ce que la tâche fera à l'heure dite"
        '
        'LabelResume
        '
        Me.LabelResume.Location = New System.Drawing.Point(10, 32)
        Me.LabelResume.Name = "LabelResume"
        Me.LabelResume.Size = New System.Drawing.Size(586, 38)
        Me.LabelResume.TabIndex = 1
        Me.LabelResume.Text = "Rien pour l'instant."
        '
        'ButtonOK
        '
        Me.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOK.Location = New System.Drawing.Point(408, 538)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(100, 36)
        Me.ButtonOK.TabIndex = 3
        Me.ButtonOK.Text = "OK"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(524, 538)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(100, 36)
        Me.ButtonAnnuler.TabIndex = 4
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormReglages
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(640, 590)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.PanelResume)
        Me.Controls.Add(Me.PanelListe)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormReglages"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Réglages de la tâche"
        Me.PanelResume.ResumeLayout(False)
        Me.PanelResume.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelAide As Label
    Friend WithEvents PanelListe As Panel
    Friend WithEvents PanelResume As Panel
    Friend WithEvents LabelResumeTitre As Label
    Friend WithEvents LabelResume As Label
    Friend WithEvents ButtonOK As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
