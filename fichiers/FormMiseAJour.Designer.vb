<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormMiseAJour
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
        Me.LabelVersion = New System.Windows.Forms.Label()
        Me.GroupBoxSource = New System.Windows.Forms.GroupBox()
        Me.LabelDossier = New System.Windows.Forms.Label()
        Me.TextBoxDossier = New System.Windows.Forms.TextBox()
        Me.ButtonParcourir = New System.Windows.Forms.Button()
        Me.LabelAdresse = New System.Windows.Forms.Label()
        Me.TextBoxAdresse = New System.Windows.Forms.TextBox()
        Me.LabelPaquet = New System.Windows.Forms.Label()
        Me.TextBoxPaquet = New System.Windows.Forms.TextBox()
        Me.LabelMode = New System.Windows.Forms.Label()
        Me.ComboBoxMode = New System.Windows.Forms.ComboBox()
        Me.ProgressBarMaj = New System.Windows.Forms.ProgressBar()
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.ButtonVerifier = New System.Windows.Forms.Button()
        Me.ButtonInstaller = New System.Windows.Forms.Button()
        Me.ButtonPublier = New System.Windows.Forms.Button()
        Me.ButtonEnregistrer = New System.Windows.Forms.Button()
        Me.GroupBoxSource.SuspendLayout()
        Me.SuspendLayout()
        '
        'LabelVersion
        '
        Me.LabelVersion.Location = New System.Drawing.Point(12, 12)
        Me.LabelVersion.Name = "LabelVersion"
        Me.LabelVersion.Size = New System.Drawing.Size(796, 28)
        Me.LabelVersion.TabIndex = 0
        Me.LabelVersion.Text = "Slide"
        '
        'GroupBoxSource
        '
        Me.GroupBoxSource.Controls.Add(Me.TextBoxPaquet)
        Me.GroupBoxSource.Controls.Add(Me.LabelPaquet)
        Me.GroupBoxSource.Controls.Add(Me.TextBoxAdresse)
        Me.GroupBoxSource.Controls.Add(Me.LabelAdresse)
        Me.GroupBoxSource.Controls.Add(Me.ButtonParcourir)
        Me.GroupBoxSource.Controls.Add(Me.TextBoxDossier)
        Me.GroupBoxSource.Controls.Add(Me.LabelDossier)
        Me.GroupBoxSource.Location = New System.Drawing.Point(12, 48)
        Me.GroupBoxSource.Name = "GroupBoxSource"
        Me.GroupBoxSource.Size = New System.Drawing.Size(796, 246)
        Me.GroupBoxSource.TabIndex = 1
        Me.GroupBoxSource.TabStop = False
        Me.GroupBoxSource.Text = "Où se trouvent les versions publiées (le dossier est essayé d'abord, puis le web)"
        '
        'LabelDossier
        '
        Me.LabelDossier.AutoSize = True
        Me.LabelDossier.Location = New System.Drawing.Point(16, 32)
        Me.LabelDossier.Name = "LabelDossier"
        Me.LabelDossier.Size = New System.Drawing.Size(329, 20)
        Me.LabelDossier.TabIndex = 0
        Me.LabelDossier.Text = "Dossier (réseau local, ou dossier synchronisé) :"
        '
        'TextBoxDossier
        '
        Me.TextBoxDossier.Location = New System.Drawing.Point(16, 58)
        Me.TextBoxDossier.Name = "TextBoxDossier"
        Me.TextBoxDossier.PlaceholderText = "ex. V:\VBstudio.Net10\Slide\Publication"
        Me.TextBoxDossier.Size = New System.Drawing.Size(640, 27)
        Me.TextBoxDossier.TabIndex = 1
        '
        'ButtonParcourir
        '
        Me.ButtonParcourir.Location = New System.Drawing.Point(666, 56)
        Me.ButtonParcourir.Name = "ButtonParcourir"
        Me.ButtonParcourir.Size = New System.Drawing.Size(114, 31)
        Me.ButtonParcourir.TabIndex = 2
        Me.ButtonParcourir.Text = "Parcourir..."
        Me.ButtonParcourir.UseVisualStyleBackColor = True
        '
        'LabelAdresse
        '
        Me.LabelAdresse.AutoSize = True
        Me.LabelAdresse.Location = New System.Drawing.Point(16, 100)
        Me.LabelAdresse.Name = "LabelAdresse"
        Me.LabelAdresse.Size = New System.Drawing.Size(254, 20)
        Me.LabelAdresse.TabIndex = 3
        Me.LabelAdresse.Text = "Adresse web de manifeste.json (depot GitHub) :"
        '
        'TextBoxAdresse
        '
        Me.TextBoxAdresse.Location = New System.Drawing.Point(16, 126)
        Me.TextBoxAdresse.Name = "TextBoxAdresse"
        Me.TextBoxAdresse.PlaceholderText = "ex. http://100.x.y.z:5080/slide-maj/manifeste.json"
        Me.TextBoxAdresse.Size = New System.Drawing.Size(764, 27)
        Me.TextBoxAdresse.TabIndex = 4
        '
        'LabelPaquet
        '
        Me.LabelPaquet.AutoSize = True
        Me.LabelPaquet.Location = New System.Drawing.Point(16, 168)
        Me.LabelPaquet.Name = "LabelPaquet"
        Me.LabelPaquet.Size = New System.Drawing.Size(560, 20)
        Me.LabelPaquet.TabIndex = 5
        Me.LabelPaquet.Text = "Adresse web du paquet (seulement si elle n'est pas à côté de version.json) :"
        '
        'TextBoxPaquet
        '
        Me.TextBoxPaquet.Location = New System.Drawing.Point(16, 194)
        Me.TextBoxPaquet.Name = "TextBoxPaquet"
        Me.TextBoxPaquet.Size = New System.Drawing.Size(764, 27)
        Me.TextBoxPaquet.TabIndex = 6
        '
        'LabelMode
        '
        Me.LabelMode.AutoSize = True
        Me.LabelMode.Location = New System.Drawing.Point(12, 314)
        Me.LabelMode.Name = "LabelMode"
        Me.LabelMode.Size = New System.Drawing.Size(336, 20)
        Me.LabelMode.TabIndex = 2
        Me.LabelMode.Text = "Quand une version plus récente est publiée :"
        '
        'ComboBoxMode
        '
        Me.ComboBoxMode.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxMode.FormattingEnabled = True
        Me.ComboBoxMode.Location = New System.Drawing.Point(358, 310)
        Me.ComboBoxMode.Name = "ComboBoxMode"
        Me.ComboBoxMode.Size = New System.Drawing.Size(450, 28)
        Me.ComboBoxMode.TabIndex = 3
        '
        'ProgressBarMaj
        '
        Me.ProgressBarMaj.Location = New System.Drawing.Point(12, 352)
        Me.ProgressBarMaj.Name = "ProgressBarMaj"
        Me.ProgressBarMaj.Size = New System.Drawing.Size(796, 20)
        Me.ProgressBarMaj.TabIndex = 4
        Me.ProgressBarMaj.Visible = False
        '
        'LabelEtat
        '
        Me.LabelEtat.Location = New System.Drawing.Point(12, 380)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(796, 62)
        Me.LabelEtat.TabIndex = 5
        '
        'ButtonVerifier
        '
        Me.ButtonVerifier.Location = New System.Drawing.Point(12, 454)
        Me.ButtonVerifier.Name = "ButtonVerifier"
        Me.ButtonVerifier.Size = New System.Drawing.Size(190, 40)
        Me.ButtonVerifier.TabIndex = 6
        Me.ButtonVerifier.Text = "Vérifier maintenant"
        Me.ButtonVerifier.UseVisualStyleBackColor = True
        '
        'ButtonInstaller
        '
        Me.ButtonInstaller.Location = New System.Drawing.Point(210, 454)
        Me.ButtonInstaller.Name = "ButtonInstaller"
        Me.ButtonInstaller.Size = New System.Drawing.Size(190, 40)
        Me.ButtonInstaller.TabIndex = 7
        Me.ButtonInstaller.Text = "Installer maintenant"
        Me.ButtonInstaller.UseVisualStyleBackColor = True
        Me.ButtonInstaller.Visible = False
        '
        'ButtonPublier
        '
        Me.ButtonPublier.Location = New System.Drawing.Point(408, 454)
        Me.ButtonPublier.Name = "ButtonPublier"
        Me.ButtonPublier.Size = New System.Drawing.Size(190, 40)
        Me.ButtonPublier.TabIndex = 8
        Me.ButtonPublier.Text = "Publier cette version"
        Me.ButtonPublier.UseVisualStyleBackColor = True
        Me.ButtonPublier.Visible = False
        '
        'ButtonEnregistrer
        '
        Me.ButtonEnregistrer.Location = New System.Drawing.Point(618, 454)
        Me.ButtonEnregistrer.Name = "ButtonEnregistrer"
        Me.ButtonEnregistrer.Size = New System.Drawing.Size(190, 40)
        Me.ButtonEnregistrer.TabIndex = 9
        Me.ButtonEnregistrer.Text = "Enregistrer et fermer"
        Me.ButtonEnregistrer.UseVisualStyleBackColor = True
        '
        'FormMiseAJour
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(820, 508)
        Me.Controls.Add(Me.ButtonEnregistrer)
        Me.Controls.Add(Me.ButtonPublier)
        Me.Controls.Add(Me.ButtonInstaller)
        Me.Controls.Add(Me.ButtonVerifier)
        Me.Controls.Add(Me.LabelEtat)
        Me.Controls.Add(Me.ProgressBarMaj)
        Me.Controls.Add(Me.ComboBoxMode)
        Me.Controls.Add(Me.LabelMode)
        Me.Controls.Add(Me.GroupBoxSource)
        Me.Controls.Add(Me.LabelVersion)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormMiseAJour"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Mises à jour de Slide"
        Me.GroupBoxSource.ResumeLayout(False)
        Me.GroupBoxSource.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents LabelVersion As Label
    Friend WithEvents GroupBoxSource As GroupBox
    Friend WithEvents LabelDossier As Label
    Friend WithEvents TextBoxDossier As TextBox
    Friend WithEvents ButtonParcourir As Button
    Friend WithEvents LabelAdresse As Label
    Friend WithEvents TextBoxAdresse As TextBox
    Friend WithEvents LabelPaquet As Label
    Friend WithEvents TextBoxPaquet As TextBox
    Friend WithEvents LabelMode As Label
    Friend WithEvents ComboBoxMode As ComboBox
    Friend WithEvents ProgressBarMaj As ProgressBar
    Friend WithEvents LabelEtat As Label
    Friend WithEvents ButtonVerifier As Button
    Friend WithEvents ButtonInstaller As Button
    Friend WithEvents ButtonPublier As Button
    Friend WithEvents ButtonEnregistrer As Button
End Class
