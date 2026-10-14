<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class PanneauCameraIp
    Inherits System.Windows.Forms.UserControl

    'UserControl remplace la méthode Dispose pour nettoyer la liste des composants.
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
        Me.LabelHote = New System.Windows.Forms.Label()
        Me.TextBoxHote = New System.Windows.Forms.TextBox()
        Me.LabelPort = New System.Windows.Forms.Label()
        Me.NumericPort = New System.Windows.Forms.NumericUpDown()
        Me.LabelIdentifiant = New System.Windows.Forms.Label()
        Me.TextBoxIdentifiant = New System.Windows.Forms.TextBox()
        Me.LabelMotDePasse = New System.Windows.Forms.Label()
        Me.TextBoxMotDePasse = New System.Windows.Forms.TextBox()
        Me.LabelHoteDistant = New System.Windows.Forms.Label()
        Me.TextBoxHoteDistant = New System.Windows.Forms.TextBox()
        Me.ButtonEnregistrer = New System.Windows.Forms.Button()
        Me.ButtonEssai = New System.Windows.Forms.Button()
        Me.LabelEtat = New System.Windows.Forms.Label()
        CType(Me.NumericPort, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'LabelHote
        '
        Me.LabelHote.AutoSize = True
        Me.LabelHote.Location = New System.Drawing.Point(8, 14)
        Me.LabelHote.Name = "LabelHote"
        Me.LabelHote.Size = New System.Drawing.Size(83, 20)
        Me.LabelHote.TabIndex = 0
        Me.LabelHote.Text = "Enregistreur"
        '
        'TextBoxHote
        '
        Me.TextBoxHote.Location = New System.Drawing.Point(110, 10)
        Me.TextBoxHote.Name = "TextBoxHote"
        Me.TextBoxHote.Size = New System.Drawing.Size(210, 27)
        Me.TextBoxHote.TabIndex = 1
        '
        'LabelPort
        '
        Me.LabelPort.AutoSize = True
        Me.LabelPort.Location = New System.Drawing.Point(340, 14)
        Me.LabelPort.Name = "LabelPort"
        Me.LabelPort.Size = New System.Drawing.Size(76, 20)
        Me.LabelPort.TabIndex = 2
        Me.LabelPort.Text = "Port RTSP"
        '
        'NumericPort
        '
        Me.NumericPort.Location = New System.Drawing.Point(424, 10)
        Me.NumericPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        Me.NumericPort.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericPort.Name = "NumericPort"
        Me.NumericPort.Size = New System.Drawing.Size(90, 27)
        Me.NumericPort.TabIndex = 3
        Me.NumericPort.Value = New Decimal(New Integer() {554, 0, 0, 0})
        '
        'LabelIdentifiant
        '
        Me.LabelIdentifiant.AutoSize = True
        Me.LabelIdentifiant.Location = New System.Drawing.Point(540, 14)
        Me.LabelIdentifiant.Name = "LabelIdentifiant"
        Me.LabelIdentifiant.Size = New System.Drawing.Size(79, 20)
        Me.LabelIdentifiant.TabIndex = 4
        Me.LabelIdentifiant.Text = "Identifiant"
        '
        'TextBoxIdentifiant
        '
        Me.TextBoxIdentifiant.Location = New System.Drawing.Point(628, 10)
        Me.TextBoxIdentifiant.Name = "TextBoxIdentifiant"
        Me.TextBoxIdentifiant.Size = New System.Drawing.Size(170, 27)
        Me.TextBoxIdentifiant.TabIndex = 5
        '
        'LabelMotDePasse
        '
        Me.LabelMotDePasse.AutoSize = True
        Me.LabelMotDePasse.Location = New System.Drawing.Point(820, 14)
        Me.LabelMotDePasse.Name = "LabelMotDePasse"
        Me.LabelMotDePasse.Size = New System.Drawing.Size(105, 20)
        Me.LabelMotDePasse.TabIndex = 6
        Me.LabelMotDePasse.Text = "Mot de passe"
        '
        'TextBoxMotDePasse
        '
        Me.TextBoxMotDePasse.Location = New System.Drawing.Point(934, 10)
        Me.TextBoxMotDePasse.Name = "TextBoxMotDePasse"
        Me.TextBoxMotDePasse.Size = New System.Drawing.Size(210, 27)
        Me.TextBoxMotDePasse.TabIndex = 7
        Me.TextBoxMotDePasse.UseSystemPasswordChar = True
        '
        'LabelHoteDistant
        '
        Me.LabelHoteDistant.AutoSize = True
        Me.LabelHoteDistant.Location = New System.Drawing.Point(1162, 14)
        Me.LabelHoteDistant.Name = "LabelHoteDistant"
        Me.LabelHoteDistant.Size = New System.Drawing.Size(95, 20)
        Me.LabelHoteDistant.TabIndex = 11
        Me.LabelHoteDistant.Text = "À distance"
        '
        'TextBoxHoteDistant
        '
        Me.TextBoxHoteDistant.Location = New System.Drawing.Point(1262, 10)
        Me.TextBoxHoteDistant.Name = "TextBoxHoteDistant"
        Me.TextBoxHoteDistant.Size = New System.Drawing.Size(240, 27)
        Me.TextBoxHoteDistant.TabIndex = 12
        '
        'ButtonEnregistrer
        '
        Me.ButtonEnregistrer.Location = New System.Drawing.Point(8, 50)
        Me.ButtonEnregistrer.Name = "ButtonEnregistrer"
        Me.ButtonEnregistrer.Size = New System.Drawing.Size(180, 38)
        Me.ButtonEnregistrer.TabIndex = 8
        Me.ButtonEnregistrer.Text = "Enregistrer"
        Me.ButtonEnregistrer.UseVisualStyleBackColor = True
        '
        'ButtonEssai
        '
        Me.ButtonEssai.Location = New System.Drawing.Point(200, 50)
        Me.ButtonEssai.Name = "ButtonEssai"
        Me.ButtonEssai.Size = New System.Drawing.Size(240, 38)
        Me.ButtonEssai.TabIndex = 9
        Me.ButtonEssai.Text = "Essayer la connexion"
        Me.ButtonEssai.UseVisualStyleBackColor = True
        '
        'LabelEtat
        '
        Me.LabelEtat.Location = New System.Drawing.Point(456, 58)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(1060, 26)
        Me.LabelEtat.TabIndex = 10
        '
        'PanneauCameraIp
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.Controls.Add(Me.TextBoxHoteDistant)
        Me.Controls.Add(Me.LabelHoteDistant)
        Me.Controls.Add(Me.LabelEtat)
        Me.Controls.Add(Me.ButtonEssai)
        Me.Controls.Add(Me.ButtonEnregistrer)
        Me.Controls.Add(Me.TextBoxMotDePasse)
        Me.Controls.Add(Me.LabelMotDePasse)
        Me.Controls.Add(Me.TextBoxIdentifiant)
        Me.Controls.Add(Me.LabelIdentifiant)
        Me.Controls.Add(Me.NumericPort)
        Me.Controls.Add(Me.LabelPort)
        Me.Controls.Add(Me.TextBoxHote)
        Me.Controls.Add(Me.LabelHote)
        Me.Name = "PanneauCameraIp"
        Me.Size = New System.Drawing.Size(1520, 140)
        CType(Me.NumericPort, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelHote As System.Windows.Forms.Label
    Friend WithEvents TextBoxHote As System.Windows.Forms.TextBox
    Friend WithEvents LabelPort As System.Windows.Forms.Label
    Friend WithEvents NumericPort As System.Windows.Forms.NumericUpDown
    Friend WithEvents LabelIdentifiant As System.Windows.Forms.Label
    Friend WithEvents TextBoxIdentifiant As System.Windows.Forms.TextBox
    Friend WithEvents LabelMotDePasse As System.Windows.Forms.Label
    Friend WithEvents TextBoxMotDePasse As System.Windows.Forms.TextBox
    Friend WithEvents LabelHoteDistant As System.Windows.Forms.Label
    Friend WithEvents TextBoxHoteDistant As System.Windows.Forms.TextBox
    Friend WithEvents ButtonEnregistrer As System.Windows.Forms.Button
    Friend WithEvents ButtonEssai As System.Windows.Forms.Button
    Friend WithEvents LabelEtat As System.Windows.Forms.Label
End Class
