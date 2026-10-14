<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class PanneauBlinkCompte
    Inherits System.Windows.Forms.UserControl

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
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.LabelEmail = New System.Windows.Forms.Label()
        Me.TextBoxEmail = New System.Windows.Forms.TextBox()
        Me.LabelMotDePasse = New System.Windows.Forms.Label()
        Me.TextBoxMotDePasse = New System.Windows.Forms.TextBox()
        Me.ButtonConnexion = New System.Windows.Forms.Button()
        Me.LabelCode = New System.Windows.Forms.Label()
        Me.TextBoxCode = New System.Windows.Forms.TextBox()
        Me.ButtonCode = New System.Windows.Forms.Button()
        Me.LabelCameras = New System.Windows.Forms.Label()
        Me.ListBoxCameras = New System.Windows.Forms.ListBox()
        Me.ButtonActualiser = New System.Windows.Forms.Button()
        Me.ButtonDeconnecter = New System.Windows.Forms.Button()
        Me.ButtonEssai = New System.Windows.Forms.Button()
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(16, 12)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(588, 66)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Compte Blink (Amazon) pour afficher une caméra en PIP. L'adresse et le mot de passe sont enregistrés dans data\settings, chiffrés pour votre session Windows sur ce PC (illisibles ailleurs)."
        '
        'LabelEmail
        '
        Me.LabelEmail.AutoSize = True
        Me.LabelEmail.Location = New System.Drawing.Point(16, 90)
        Me.LabelEmail.Name = "LabelEmail"
        Me.LabelEmail.Size = New System.Drawing.Size(66, 20)
        Me.LabelEmail.TabIndex = 1
        Me.LabelEmail.Text = "Adresse"
        '
        'TextBoxEmail
        '
        Me.TextBoxEmail.Location = New System.Drawing.Point(150, 86)
        Me.TextBoxEmail.Name = "TextBoxEmail"
        Me.TextBoxEmail.Size = New System.Drawing.Size(454, 27)
        Me.TextBoxEmail.TabIndex = 2
        '
        'LabelMotDePasse
        '
        Me.LabelMotDePasse.AutoSize = True
        Me.LabelMotDePasse.Location = New System.Drawing.Point(16, 126)
        Me.LabelMotDePasse.Name = "LabelMotDePasse"
        Me.LabelMotDePasse.Size = New System.Drawing.Size(105, 20)
        Me.LabelMotDePasse.TabIndex = 3
        Me.LabelMotDePasse.Text = "Mot de passe"
        '
        'TextBoxMotDePasse
        '
        Me.TextBoxMotDePasse.Location = New System.Drawing.Point(150, 122)
        Me.TextBoxMotDePasse.Name = "TextBoxMotDePasse"
        Me.TextBoxMotDePasse.Size = New System.Drawing.Size(454, 27)
        Me.TextBoxMotDePasse.TabIndex = 4
        Me.TextBoxMotDePasse.UseSystemPasswordChar = True
        '
        'ButtonConnexion
        '
        Me.ButtonConnexion.Location = New System.Drawing.Point(150, 160)
        Me.ButtonConnexion.Name = "ButtonConnexion"
        Me.ButtonConnexion.Size = New System.Drawing.Size(200, 38)
        Me.ButtonConnexion.TabIndex = 5
        Me.ButtonConnexion.Text = "Se connecter"
        Me.ButtonConnexion.UseVisualStyleBackColor = True
        '
        'LabelCode
        '
        Me.LabelCode.AutoSize = True
        Me.LabelCode.Location = New System.Drawing.Point(16, 214)
        Me.LabelCode.Name = "LabelCode"
        Me.LabelCode.Size = New System.Drawing.Size(122, 20)
        Me.LabelCode.TabIndex = 6
        Me.LabelCode.Text = "Code de contrôle"
        Me.LabelCode.Visible = False
        '
        'TextBoxCode
        '
        Me.TextBoxCode.Location = New System.Drawing.Point(150, 210)
        Me.TextBoxCode.Name = "TextBoxCode"
        Me.TextBoxCode.Size = New System.Drawing.Size(200, 27)
        Me.TextBoxCode.TabIndex = 7
        Me.TextBoxCode.Visible = False
        '
        'ButtonCode
        '
        Me.ButtonCode.Location = New System.Drawing.Point(366, 206)
        Me.ButtonCode.Name = "ButtonCode"
        Me.ButtonCode.Size = New System.Drawing.Size(238, 38)
        Me.ButtonCode.TabIndex = 8
        Me.ButtonCode.Text = "Valider le code"
        Me.ButtonCode.UseVisualStyleBackColor = True
        Me.ButtonCode.Visible = False
        '
        'LabelCameras
        '
        Me.LabelCameras.AutoSize = True
        Me.LabelCameras.Location = New System.Drawing.Point(16, 262)
        Me.LabelCameras.Name = "LabelCameras"
        Me.LabelCameras.Size = New System.Drawing.Size(155, 20)
        Me.LabelCameras.TabIndex = 9
        Me.LabelCameras.Text = "Caméras du compte :"
        '
        'ListBoxCameras
        '
        Me.ListBoxCameras.FormattingEnabled = True
        Me.ListBoxCameras.ItemHeight = 20
        Me.ListBoxCameras.Location = New System.Drawing.Point(16, 288)
        Me.ListBoxCameras.Name = "ListBoxCameras"
        Me.ListBoxCameras.Size = New System.Drawing.Size(588, 124)
        Me.ListBoxCameras.TabIndex = 10
        '
        'ButtonActualiser
        '
        Me.ButtonActualiser.Location = New System.Drawing.Point(16, 424)
        Me.ButtonActualiser.Name = "ButtonActualiser"
        Me.ButtonActualiser.Size = New System.Drawing.Size(160, 38)
        Me.ButtonActualiser.TabIndex = 11
        Me.ButtonActualiser.Text = "Actualiser"
        Me.ButtonActualiser.UseVisualStyleBackColor = True
        '
        'ButtonDeconnecter
        '
        Me.ButtonDeconnecter.Location = New System.Drawing.Point(188, 424)
        Me.ButtonDeconnecter.Name = "ButtonDeconnecter"
        Me.ButtonDeconnecter.Size = New System.Drawing.Size(160, 38)
        Me.ButtonDeconnecter.TabIndex = 12
        Me.ButtonDeconnecter.Text = "Se déconnecter"
        Me.ButtonDeconnecter.UseVisualStyleBackColor = True
        '
        'LabelEtat
        '
        '
        'ButtonEssai
        '
        Me.ButtonEssai.Location = New System.Drawing.Point(360, 424)
        Me.ButtonEssai.Name = "ButtonEssai"
        Me.ButtonEssai.Size = New System.Drawing.Size(244, 38)
        Me.ButtonEssai.TabIndex = 15
        Me.ButtonEssai.Text = "Essayer avec le dernier clip"
        Me.ButtonEssai.UseVisualStyleBackColor = True
        '
        'LabelEtat
        '
        Me.LabelEtat.Location = New System.Drawing.Point(16, 474)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(588, 44)
        Me.LabelEtat.TabIndex = 13
        '
        'PanneauBlinkCompte
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Size = New System.Drawing.Size(622, 580)
        Me.Controls.Add(Me.LabelEtat)
        Me.Controls.Add(Me.ButtonEssai)
        Me.Controls.Add(Me.ButtonDeconnecter)
        Me.Controls.Add(Me.ButtonActualiser)
        Me.Controls.Add(Me.ListBoxCameras)
        Me.Controls.Add(Me.LabelCameras)
        Me.Controls.Add(Me.ButtonCode)
        Me.Controls.Add(Me.TextBoxCode)
        Me.Controls.Add(Me.LabelCode)
        Me.Controls.Add(Me.ButtonConnexion)
        Me.Controls.Add(Me.TextBoxMotDePasse)
        Me.Controls.Add(Me.LabelMotDePasse)
        Me.Controls.Add(Me.TextBoxEmail)
        Me.Controls.Add(Me.LabelEmail)
        Me.Controls.Add(Me.LabelAide)
        Me.Name = "PanneauBlinkCompte"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAide As System.Windows.Forms.Label
    Friend WithEvents LabelEmail As System.Windows.Forms.Label
    Friend WithEvents TextBoxEmail As System.Windows.Forms.TextBox
    Friend WithEvents LabelMotDePasse As System.Windows.Forms.Label
    Friend WithEvents TextBoxMotDePasse As System.Windows.Forms.TextBox
    Friend WithEvents ButtonConnexion As System.Windows.Forms.Button
    Friend WithEvents LabelCode As System.Windows.Forms.Label
    Friend WithEvents TextBoxCode As System.Windows.Forms.TextBox
    Friend WithEvents ButtonCode As System.Windows.Forms.Button
    Friend WithEvents LabelCameras As System.Windows.Forms.Label
    Friend WithEvents ListBoxCameras As System.Windows.Forms.ListBox
    Friend WithEvents ButtonActualiser As System.Windows.Forms.Button
    Friend WithEvents ButtonDeconnecter As System.Windows.Forms.Button
    Friend WithEvents LabelEtat As System.Windows.Forms.Label
    Friend WithEvents ButtonEssai As System.Windows.Forms.Button
End Class
