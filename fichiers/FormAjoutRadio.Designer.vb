<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAjoutRadio
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
        Me.LabelNom = New System.Windows.Forms.Label()
        Me.TextBoxNom = New System.Windows.Forms.TextBox()
        Me.LabelAdresse = New System.Windows.Forms.Label()
        Me.TextBoxAdresse = New System.Windows.Forms.TextBox()
        Me.ButtonEcouter = New System.Windows.Forms.Button()
        Me.ButtonSuggestions = New System.Windows.Forms.Button()
        Me.LabelMessage = New System.Windows.Forms.Label()
        Me.ButtonAjouter = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(16, 14)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(628, 50)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Ajoutez une radio à la liste : donnez-lui un nom et l'adresse de son flux (http://… ; fichiers .mp3, .aac, .m3u ou .pls)." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "« Écouter » permet de l'essayer avant de l'ajouter."
        '
        'LabelNom
        '
        Me.LabelNom.Location = New System.Drawing.Point(16, 80)
        Me.LabelNom.Name = "LabelNom"
        Me.LabelNom.Size = New System.Drawing.Size(90, 28)
        Me.LabelNom.TabIndex = 1
        Me.LabelNom.Text = "Nom :"
        Me.LabelNom.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'TextBoxNom
        '
        Me.TextBoxNom.Location = New System.Drawing.Point(112, 78)
        Me.TextBoxNom.MaxLength = 48
        Me.TextBoxNom.Name = "TextBoxNom"
        Me.TextBoxNom.Size = New System.Drawing.Size(532, 27)
        Me.TextBoxNom.TabIndex = 2
        '
        'LabelAdresse
        '
        Me.LabelAdresse.Location = New System.Drawing.Point(16, 124)
        Me.LabelAdresse.Name = "LabelAdresse"
        Me.LabelAdresse.Size = New System.Drawing.Size(90, 28)
        Me.LabelAdresse.TabIndex = 3
        Me.LabelAdresse.Text = "Adresse :"
        Me.LabelAdresse.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'TextBoxAdresse
        '
        Me.TextBoxAdresse.Location = New System.Drawing.Point(112, 122)
        Me.TextBoxAdresse.Name = "TextBoxAdresse"
        Me.TextBoxAdresse.Size = New System.Drawing.Size(532, 27)
        Me.TextBoxAdresse.TabIndex = 4
        '
        'ButtonEcouter
        '
        Me.ButtonEcouter.Location = New System.Drawing.Point(112, 166)
        Me.ButtonEcouter.Name = "ButtonEcouter"
        Me.ButtonEcouter.Size = New System.Drawing.Size(180, 38)
        Me.ButtonEcouter.TabIndex = 5
        Me.ButtonEcouter.Text = "Écouter"
        Me.ButtonEcouter.UseVisualStyleBackColor = True
        '
        'ButtonSuggestions
        '
        Me.ButtonSuggestions.Location = New System.Drawing.Point(300, 166)
        Me.ButtonSuggestions.Name = "ButtonSuggestions"
        Me.ButtonSuggestions.Size = New System.Drawing.Size(344, 38)
        Me.ButtonSuggestions.TabIndex = 9
        Me.ButtonSuggestions.Text = "Choisir parmi des radios sur Internet..."
        Me.ButtonSuggestions.UseVisualStyleBackColor = True
        '
        'LabelMessage
        '
        Me.LabelMessage.ForeColor = System.Drawing.Color.Firebrick
        Me.LabelMessage.Location = New System.Drawing.Point(16, 216)
        Me.LabelMessage.Name = "LabelMessage"
        Me.LabelMessage.Size = New System.Drawing.Size(628, 50)
        Me.LabelMessage.TabIndex = 6
        '
        'ButtonAjouter
        '
        Me.ButtonAjouter.Location = New System.Drawing.Point(384, 282)
        Me.ButtonAjouter.Name = "ButtonAjouter"
        Me.ButtonAjouter.Size = New System.Drawing.Size(130, 40)
        Me.ButtonAjouter.TabIndex = 7
        Me.ButtonAjouter.Text = "Ajouter"
        Me.ButtonAjouter.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(524, 282)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(120, 40)
        Me.ButtonAnnuler.TabIndex = 8
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormAjoutRadio
        '
        Me.AcceptButton = Me.ButtonAjouter
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(660, 338)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonAjouter)
        Me.Controls.Add(Me.LabelMessage)
        Me.Controls.Add(Me.ButtonSuggestions)
        Me.Controls.Add(Me.ButtonEcouter)
        Me.Controls.Add(Me.TextBoxAdresse)
        Me.Controls.Add(Me.LabelAdresse)
        Me.Controls.Add(Me.TextBoxNom)
        Me.Controls.Add(Me.LabelNom)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormAjoutRadio"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Ajouter une radio"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAide As Label
    Friend WithEvents LabelNom As Label
    Friend WithEvents TextBoxNom As TextBox
    Friend WithEvents LabelAdresse As Label
    Friend WithEvents TextBoxAdresse As TextBox
    Friend WithEvents ButtonEcouter As Button
    Friend WithEvents ButtonSuggestions As Button
    Friend WithEvents LabelMessage As Label
    Friend WithEvents ButtonAjouter As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
