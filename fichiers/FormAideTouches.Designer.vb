<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAideTouches
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
        Me.components = New System.ComponentModel.Container()
        Me.LabelTitre = New System.Windows.Forms.Label()
        Me.LabelTouches = New System.Windows.Forms.Label()
        Me.LabelActions = New System.Windows.Forms.Label()
        Me.LabelPied = New System.Windows.Forms.Label()
        Me.MinuteurFermeture = New System.Windows.Forms.Timer(Me.components)
        Me.SuspendLayout()
        '
        'LabelTitre
        '
        Me.LabelTitre.AutoSize = True
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 15.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTitre.ForeColor = System.Drawing.Color.White
        Me.LabelTitre.Location = New System.Drawing.Point(24, 18)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(190, 28)
        Me.LabelTitre.TabIndex = 0
        Me.LabelTitre.Text = "Touches de Slide"
        '
        'LabelTouches
        '
        Me.LabelTouches.AutoSize = True
        Me.LabelTouches.Font = New System.Drawing.Font("Segoe UI Semibold", 11.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTouches.ForeColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(132, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.LabelTouches.Location = New System.Drawing.Point(28, 66)
        Me.LabelTouches.Name = "LabelTouches"
        Me.LabelTouches.Size = New System.Drawing.Size(120, 160)
        Me.LabelTouches.TabIndex = 1
        Me.LabelTouches.Text = "N" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "P" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "M" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "S" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "H" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Espace" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Ctrl + Maj + U" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Échap"
        '
        'LabelActions
        '
        Me.LabelActions.AutoSize = True
        Me.LabelActions.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.LabelActions.ForeColor = System.Drawing.Color.FromArgb(CType(CType(235, Byte), Integer), CType(CType(235, Byte), Integer), CType(CType(240, Byte), Integer))
        Me.LabelActions.Location = New System.Drawing.Point(190, 66)
        Me.LabelActions.Name = "LabelActions"
        Me.LabelActions.Size = New System.Drawing.Size(440, 160)
        Me.LabelActions.TabIndex = 2
        Me.LabelActions.Text = "Suivant (photo, vidéo, musique) ; les PIP restent" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Suivant et ferme les PIP (2e appui : relance ceux du démarrage)" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Réaffiche la pochette de la musique" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Source de musique suivante (MP3, radio, Deezer : celles cochées)" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Affiche ou cache cette aide" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Menu de Slide (comme le clic droit)" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Réglages des mises à jour" & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Quitter Slide"
        '
        'LabelPied
        '
        Me.LabelPied.AutoSize = True
        Me.LabelPied.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Italic)
        Me.LabelPied.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelPied.Location = New System.Drawing.Point(28, 250)
        Me.LabelPied.Name = "LabelPied"
        Me.LabelPied.Size = New System.Drawing.Size(320, 15)
        Me.LabelPied.TabIndex = 3
        Me.LabelPied.Text = "Au démarrage : Ctrl = réglages, Maj = sans la minuterie." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Cliquer ou appuyer sur H pour fermer."
        '
        'MinuteurFermeture
        '
        Me.MinuteurFermeture.Interval = 15000
        '
        'FormAideTouches
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(28, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.ClientSize = New System.Drawing.Size(660, 312)
        Me.Controls.Add(Me.LabelPied)
        Me.Controls.Add(Me.LabelActions)
        Me.Controls.Add(Me.LabelTouches)
        Me.Controls.Add(Me.LabelTitre)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.KeyPreview = True
        Me.Name = "FormAideTouches"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.Manual
        Me.Text = "Touches de Slide"
        Me.TopMost = True
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelTitre As System.Windows.Forms.Label
    Friend WithEvents LabelTouches As System.Windows.Forms.Label
    Friend WithEvents LabelActions As System.Windows.Forms.Label
    Friend WithEvents LabelPied As System.Windows.Forms.Label
    Friend WithEvents MinuteurFermeture As System.Windows.Forms.Timer
End Class
