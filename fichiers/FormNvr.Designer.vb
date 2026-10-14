<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormNvr
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
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.LabelHote = New System.Windows.Forms.Label()
        Me.TextBoxHote = New System.Windows.Forms.TextBox()
        Me.LabelPort = New System.Windows.Forms.Label()
        Me.NumericPort = New System.Windows.Forms.NumericUpDown()
        Me.LabelIdentifiant = New System.Windows.Forms.Label()
        Me.TextBoxIdentifiant = New System.Windows.Forms.TextBox()
        Me.LabelMotDePasse = New System.Windows.Forms.Label()
        Me.TextBoxMotDePasse = New System.Windows.Forms.TextBox()
        Me.ButtonOk = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        CType(Me.NumericPort, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(16, 12)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(468, 86)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Enregistreur de caméras IP (NVR, ex. Herospeed) : adresse, port RTSP (554 en général) et compte en lecture seule. Le mot de passe est chiffré pour ce poste et n'est jamais écrit dans les minuteries."
        '
        'LabelHote
        '
        Me.LabelHote.Location = New System.Drawing.Point(16, 112)
        Me.LabelHote.Name = "LabelHote"
        Me.LabelHote.Size = New System.Drawing.Size(150, 26)
        Me.LabelHote.TabIndex = 1
        Me.LabelHote.Text = "Adresse (IP)"
        '
        'TextBoxHote
        '
        Me.TextBoxHote.Location = New System.Drawing.Point(180, 108)
        Me.TextBoxHote.Name = "TextBoxHote"
        Me.TextBoxHote.Size = New System.Drawing.Size(304, 27)
        Me.TextBoxHote.TabIndex = 2
        '
        'LabelPort
        '
        Me.LabelPort.Location = New System.Drawing.Point(16, 152)
        Me.LabelPort.Name = "LabelPort"
        Me.LabelPort.Size = New System.Drawing.Size(150, 26)
        Me.LabelPort.TabIndex = 3
        Me.LabelPort.Text = "Port RTSP"
        '
        'NumericPort
        '
        Me.NumericPort.Location = New System.Drawing.Point(180, 148)
        Me.NumericPort.Maximum = New Decimal(New Integer() {65535, 0, 0, 0})
        Me.NumericPort.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericPort.Name = "NumericPort"
        Me.NumericPort.Size = New System.Drawing.Size(120, 27)
        Me.NumericPort.TabIndex = 4
        Me.NumericPort.Value = New Decimal(New Integer() {554, 0, 0, 0})
        '
        'LabelIdentifiant
        '
        Me.LabelIdentifiant.Location = New System.Drawing.Point(16, 192)
        Me.LabelIdentifiant.Name = "LabelIdentifiant"
        Me.LabelIdentifiant.Size = New System.Drawing.Size(150, 26)
        Me.LabelIdentifiant.TabIndex = 5
        Me.LabelIdentifiant.Text = "Identifiant"
        '
        'TextBoxIdentifiant
        '
        Me.TextBoxIdentifiant.Location = New System.Drawing.Point(180, 188)
        Me.TextBoxIdentifiant.Name = "TextBoxIdentifiant"
        Me.TextBoxIdentifiant.Size = New System.Drawing.Size(304, 27)
        Me.TextBoxIdentifiant.TabIndex = 6
        '
        'LabelMotDePasse
        '
        Me.LabelMotDePasse.Location = New System.Drawing.Point(16, 232)
        Me.LabelMotDePasse.Name = "LabelMotDePasse"
        Me.LabelMotDePasse.Size = New System.Drawing.Size(150, 26)
        Me.LabelMotDePasse.TabIndex = 7
        Me.LabelMotDePasse.Text = "Mot de passe"
        '
        'TextBoxMotDePasse
        '
        Me.TextBoxMotDePasse.Location = New System.Drawing.Point(180, 228)
        Me.TextBoxMotDePasse.Name = "TextBoxMotDePasse"
        Me.TextBoxMotDePasse.Size = New System.Drawing.Size(304, 27)
        Me.TextBoxMotDePasse.TabIndex = 8
        Me.TextBoxMotDePasse.UseSystemPasswordChar = True
        '
        'ButtonOk
        '
        Me.ButtonOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOk.Location = New System.Drawing.Point(254, 284)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(110, 38)
        Me.ButtonOk.TabIndex = 9
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(374, 284)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(110, 38)
        Me.ButtonAnnuler.TabIndex = 10
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormNvr
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(500, 338)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.TextBoxMotDePasse)
        Me.Controls.Add(Me.LabelMotDePasse)
        Me.Controls.Add(Me.TextBoxIdentifiant)
        Me.Controls.Add(Me.LabelIdentifiant)
        Me.Controls.Add(Me.NumericPort)
        Me.Controls.Add(Me.LabelPort)
        Me.Controls.Add(Me.TextBoxHote)
        Me.Controls.Add(Me.LabelHote)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormNvr"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Caméras IP (enregistreur)"
        CType(Me.NumericPort, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAide As System.Windows.Forms.Label
    Friend WithEvents LabelHote As System.Windows.Forms.Label
    Friend WithEvents TextBoxHote As System.Windows.Forms.TextBox
    Friend WithEvents LabelPort As System.Windows.Forms.Label
    Friend WithEvents NumericPort As System.Windows.Forms.NumericUpDown
    Friend WithEvents LabelIdentifiant As System.Windows.Forms.Label
    Friend WithEvents TextBoxIdentifiant As System.Windows.Forms.TextBox
    Friend WithEvents LabelMotDePasse As System.Windows.Forms.Label
    Friend WithEvents TextBoxMotDePasse As System.Windows.Forms.TextBox
    Friend WithEvents ButtonOk As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
