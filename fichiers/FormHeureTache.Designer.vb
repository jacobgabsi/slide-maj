<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormHeureTache
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
        Me.LabelTitre = New System.Windows.Forms.Label()
        Me.LabelSousTitre = New System.Windows.Forms.Label()
        Me.PanelFixe = New System.Windows.Forms.Panel()
        Me.PanelMarqueFixe = New System.Windows.Forms.Panel()
        Me.RadioFixe = New System.Windows.Forms.RadioButton()
        Me.TimeFixe = New ChampHeure()
        Me.PanelCalcule = New System.Windows.Forms.Panel()
        Me.PanelMarqueCalcule = New System.Windows.Forms.Panel()
        Me.RadioShabbat = New System.Windows.Forms.RadioButton()
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.ListeRepere = New System.Windows.Forms.ListBox()
        Me.LabelDecalage = New System.Windows.Forms.Label()
        Me.NumericDecalage = New System.Windows.Forms.NumericUpDown()
        Me.LabelUnite = New System.Windows.Forms.Label()
        Me.CheckPlusTard = New System.Windows.Forms.CheckBox()
        Me.TimePlusTard = New ChampHeure()
        Me.CheckPlusTot = New System.Windows.Forms.CheckBox()
        Me.TimePlusTot = New ChampHeure()
        Me.PanelApercu = New System.Windows.Forms.Panel()
        Me.PanelBarre = New System.Windows.Forms.Panel()
        Me.LabelApercuTitre = New System.Windows.Forms.Label()
        Me.LabelApercu = New System.Windows.Forms.Label()
        Me.LabelApercuDetail = New System.Windows.Forms.Label()
        Me.ButtonOk = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.PanelFixe.SuspendLayout()
        Me.PanelCalcule.SuspendLayout()
        CType(Me.NumericDecalage, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.PanelApercu.SuspendLayout()
        Me.SuspendLayout()
        '
        'LabelTitre
        '
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 17.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTitre.Location = New System.Drawing.Point(28, 22)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(624, 40)
        Me.LabelTitre.TabIndex = 0
        Me.LabelTitre.Text = "Heure de la tâche"
        '
        'LabelSousTitre
        '
        Me.LabelSousTitre.Font = New System.Drawing.Font("Segoe UI", 10.5!)
        Me.LabelSousTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelSousTitre.Location = New System.Drawing.Point(28, 64)
        Me.LabelSousTitre.Name = "LabelSousTitre"
        Me.LabelSousTitre.Size = New System.Drawing.Size(624, 28)
        Me.LabelSousTitre.TabIndex = 1
        Me.LabelSousTitre.Text = "Choisissez quand la tâche doit démarrer."
        '
        'PanelFixe
        '
        Me.PanelFixe.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(46, Byte), Integer))
        Me.PanelFixe.Controls.Add(Me.TimeFixe)
        Me.PanelFixe.Controls.Add(Me.RadioFixe)
        Me.PanelFixe.Controls.Add(Me.PanelMarqueFixe)
        Me.PanelFixe.Location = New System.Drawing.Point(28, 108)
        Me.PanelFixe.Name = "PanelFixe"
        Me.PanelFixe.Size = New System.Drawing.Size(624, 80)
        Me.PanelFixe.TabIndex = 2
        '
        'PanelMarqueFixe
        '
        Me.PanelMarqueFixe.BackColor = System.Drawing.Color.FromArgb(CType(CType(255, Byte), Integer), CType(CType(159, Byte), Integer), CType(CType(10, Byte), Integer))
        Me.PanelMarqueFixe.Dock = System.Windows.Forms.DockStyle.Left
        Me.PanelMarqueFixe.Location = New System.Drawing.Point(0, 0)
        Me.PanelMarqueFixe.Name = "PanelMarqueFixe"
        Me.PanelMarqueFixe.Size = New System.Drawing.Size(6, 80)
        Me.PanelMarqueFixe.TabIndex = 2
        '
        'RadioFixe
        '
        Me.RadioFixe.AutoSize = True
        Me.RadioFixe.Checked = True
        Me.RadioFixe.Font = New System.Drawing.Font("Segoe UI Semibold", 11.0!, System.Drawing.FontStyle.Bold)
        Me.RadioFixe.Location = New System.Drawing.Point(26, 26)
        Me.RadioFixe.Name = "RadioFixe"
        Me.RadioFixe.Size = New System.Drawing.Size(130, 29)
        Me.RadioFixe.TabIndex = 0
        Me.RadioFixe.TabStop = True
        Me.RadioFixe.Text = "Heure fixe"
        Me.RadioFixe.UseVisualStyleBackColor = True
        '
        'TimeFixe
        '
        Me.TimeFixe.Font = New System.Drawing.Font("Segoe UI Semibold", 17.0!, System.Drawing.FontStyle.Bold)
        Me.TimeFixe.Location = New System.Drawing.Point(468, 16)
        Me.TimeFixe.Name = "TimeFixe"
        Me.TimeFixe.Size = New System.Drawing.Size(136, 45)
        Me.TimeFixe.TabIndex = 1
        '
        'PanelCalcule
        '
        Me.PanelCalcule.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(46, Byte), Integer))
        Me.PanelCalcule.Controls.Add(Me.TimePlusTot)
        Me.PanelCalcule.Controls.Add(Me.CheckPlusTot)
        Me.PanelCalcule.Controls.Add(Me.TimePlusTard)
        Me.PanelCalcule.Controls.Add(Me.CheckPlusTard)
        Me.PanelCalcule.Controls.Add(Me.LabelUnite)
        Me.PanelCalcule.Controls.Add(Me.NumericDecalage)
        Me.PanelCalcule.Controls.Add(Me.LabelDecalage)
        Me.PanelCalcule.Controls.Add(Me.ListeRepere)
        Me.PanelCalcule.Controls.Add(Me.LabelAide)
        Me.PanelCalcule.Controls.Add(Me.RadioShabbat)
        Me.PanelCalcule.Controls.Add(Me.PanelMarqueCalcule)
        Me.PanelCalcule.Location = New System.Drawing.Point(28, 200)
        Me.PanelCalcule.Name = "PanelCalcule"
        Me.PanelCalcule.Size = New System.Drawing.Size(624, 404)
        Me.PanelCalcule.TabIndex = 3
        '
        'PanelMarqueCalcule
        '
        Me.PanelMarqueCalcule.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(132, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.PanelMarqueCalcule.Dock = System.Windows.Forms.DockStyle.Left
        Me.PanelMarqueCalcule.Location = New System.Drawing.Point(0, 0)
        Me.PanelMarqueCalcule.Name = "PanelMarqueCalcule"
        Me.PanelMarqueCalcule.Size = New System.Drawing.Size(6, 404)
        Me.PanelMarqueCalcule.TabIndex = 6
        '
        'RadioShabbat
        '
        Me.RadioShabbat.AutoSize = True
        Me.RadioShabbat.Font = New System.Drawing.Font("Segoe UI Semibold", 11.0!, System.Drawing.FontStyle.Bold)
        Me.RadioShabbat.Location = New System.Drawing.Point(26, 16)
        Me.RadioShabbat.Name = "RadioShabbat"
        Me.RadioShabbat.Size = New System.Drawing.Size(330, 29)
        Me.RadioShabbat.TabIndex = 0
        Me.RadioShabbat.Text = "Heure calculée automatiquement"
        Me.RadioShabbat.UseVisualStyleBackColor = True
        '
        'LabelAide
        '
        Me.LabelAide.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.LabelAide.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelAide.Location = New System.Drawing.Point(48, 50)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(556, 26)
        Me.LabelAide.TabIndex = 1
        Me.LabelAide.Text = "Suit Chabbat ou le soleil et se met à jour toute seule."
        '
        'ListeRepere
        '
        Me.ListeRepere.BackColor = System.Drawing.Color.FromArgb(CType(CType(36, Byte), Integer), CType(CType(36, Byte), Integer), CType(CType(38, Byte), Integer))
        Me.ListeRepere.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.ListeRepere.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed
        Me.ListeRepere.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ListeRepere.FormattingEnabled = True
        Me.ListeRepere.IntegralHeight = False
        Me.ListeRepere.ItemHeight = 34
        Me.ListeRepere.Location = New System.Drawing.Point(48, 78)
        Me.ListeRepere.Name = "ListeRepere"
        Me.ListeRepere.Size = New System.Drawing.Size(556, 172)
        Me.ListeRepere.TabIndex = 2
        '
        'LabelDecalage
        '
        Me.LabelDecalage.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.LabelDecalage.Location = New System.Drawing.Point(48, 266)
        Me.LabelDecalage.Name = "LabelDecalage"
        Me.LabelDecalage.Size = New System.Drawing.Size(110, 30)
        Me.LabelDecalage.TabIndex = 3
        Me.LabelDecalage.Text = "Décalage"
        '
        'NumericDecalage
        '
        Me.NumericDecalage.Font = New System.Drawing.Font("Segoe UI Semibold", 12.0!, System.Drawing.FontStyle.Bold)
        Me.NumericDecalage.Location = New System.Drawing.Point(164, 262)
        Me.NumericDecalage.Maximum = New Decimal(New Integer() {600, 0, 0, 0})
        Me.NumericDecalage.Minimum = New Decimal(New Integer() {600, 0, 0, -2147483648})
        Me.NumericDecalage.Name = "NumericDecalage"
        Me.NumericDecalage.Size = New System.Drawing.Size(110, 34)
        Me.NumericDecalage.TabIndex = 4
        Me.NumericDecalage.TextAlign = System.Windows.Forms.HorizontalAlignment.Center
        '
        'LabelUnite
        '
        Me.LabelUnite.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.LabelUnite.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelUnite.Location = New System.Drawing.Point(288, 268)
        Me.LabelUnite.Name = "LabelUnite"
        Me.LabelUnite.Size = New System.Drawing.Size(316, 28)
        Me.LabelUnite.TabIndex = 5
        Me.LabelUnite.Text = "minutes  (− avant, + après)"
        '
        'CheckPlusTard
        '
        Me.CheckPlusTard.AutoSize = True
        Me.CheckPlusTard.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CheckPlusTard.Location = New System.Drawing.Point(48, 312)
        Me.CheckPlusTard.Name = "CheckPlusTard"
        Me.CheckPlusTard.Size = New System.Drawing.Size(270, 29)
        Me.CheckPlusTard.TabIndex = 6
        Me.CheckPlusTard.Text = "Pas plus tard que (heure par défaut)"
        Me.CheckPlusTard.UseVisualStyleBackColor = True
        '
        'TimePlusTard
        '
        Me.TimePlusTard.Font = New System.Drawing.Font("Segoe UI Semibold", 12.0!, System.Drawing.FontStyle.Bold)
        Me.TimePlusTard.Location = New System.Drawing.Point(468, 306)
        Me.TimePlusTard.Name = "TimePlusTard"
        Me.TimePlusTard.Size = New System.Drawing.Size(136, 34)
        Me.TimePlusTard.TabIndex = 7
        '
        'CheckPlusTot
        '
        Me.CheckPlusTot.AutoSize = True
        Me.CheckPlusTot.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CheckPlusTot.Location = New System.Drawing.Point(48, 356)
        Me.CheckPlusTot.Name = "CheckPlusTot"
        Me.CheckPlusTot.Size = New System.Drawing.Size(270, 29)
        Me.CheckPlusTot.TabIndex = 8
        Me.CheckPlusTot.Text = "Pas plus tôt que"
        Me.CheckPlusTot.UseVisualStyleBackColor = True
        '
        'TimePlusTot
        '
        Me.TimePlusTot.Font = New System.Drawing.Font("Segoe UI Semibold", 12.0!, System.Drawing.FontStyle.Bold)
        Me.TimePlusTot.Location = New System.Drawing.Point(468, 350)
        Me.TimePlusTot.Name = "TimePlusTot"
        Me.TimePlusTot.Size = New System.Drawing.Size(136, 34)
        Me.TimePlusTot.TabIndex = 9
        '
        'PanelApercu
        '
        Me.PanelApercu.BackColor = System.Drawing.Color.FromArgb(CType(CType(44, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(46, Byte), Integer))
        Me.PanelApercu.Controls.Add(Me.LabelApercuDetail)
        Me.PanelApercu.Controls.Add(Me.LabelApercu)
        Me.PanelApercu.Controls.Add(Me.LabelApercuTitre)
        Me.PanelApercu.Controls.Add(Me.PanelBarre)
        Me.PanelApercu.Location = New System.Drawing.Point(28, 596)
        Me.PanelApercu.Name = "PanelApercu"
        Me.PanelApercu.Size = New System.Drawing.Size(624, 120)
        Me.PanelApercu.TabIndex = 4
        '
        'PanelBarre
        '
        Me.PanelBarre.BackColor = System.Drawing.Color.FromArgb(CType(CType(48, Byte), Integer), CType(CType(209, Byte), Integer), CType(CType(88, Byte), Integer))
        Me.PanelBarre.Dock = System.Windows.Forms.DockStyle.Left
        Me.PanelBarre.Location = New System.Drawing.Point(0, 0)
        Me.PanelBarre.Name = "PanelBarre"
        Me.PanelBarre.Size = New System.Drawing.Size(6, 120)
        Me.PanelBarre.TabIndex = 0
        '
        'LabelApercuTitre
        '
        Me.LabelApercuTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 8.5!, System.Drawing.FontStyle.Bold)
        Me.LabelApercuTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(110, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(140, Byte), Integer))
        Me.LabelApercuTitre.Location = New System.Drawing.Point(28, 12)
        Me.LabelApercuTitre.Name = "LabelApercuTitre"
        Me.LabelApercuTitre.Size = New System.Drawing.Size(300, 22)
        Me.LabelApercuTitre.TabIndex = 1
        Me.LabelApercuTitre.Text = "LA TÂCHE DÉMARRERA"
        '
        'LabelApercu
        '
        Me.LabelApercu.Font = New System.Drawing.Font("Segoe UI Semibold", 20.0!, System.Drawing.FontStyle.Bold)
        Me.LabelApercu.Location = New System.Drawing.Point(28, 36)
        Me.LabelApercu.Name = "LabelApercu"
        Me.LabelApercu.Size = New System.Drawing.Size(582, 48)
        Me.LabelApercu.TabIndex = 2
        '
        'LabelApercuDetail
        '
        Me.LabelApercuDetail.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.LabelApercuDetail.ForeColor = System.Drawing.Color.FromArgb(CType(CType(170, Byte), Integer), CType(CType(205, Byte), Integer), CType(CType(180, Byte), Integer))
        Me.LabelApercuDetail.Location = New System.Drawing.Point(28, 86)
        Me.LabelApercuDetail.Name = "LabelApercuDetail"
        Me.LabelApercuDetail.Size = New System.Drawing.Size(582, 28)
        Me.LabelApercuDetail.TabIndex = 3
        '
        'ButtonOk
        '
        Me.ButtonOk.Font = New System.Drawing.Font("Segoe UI Semibold", 11.0!, System.Drawing.FontStyle.Bold)
        Me.ButtonOk.Location = New System.Drawing.Point(28, 732)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(306, 50)
        Me.ButtonOk.TabIndex = 5
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ButtonAnnuler.Location = New System.Drawing.Point(346, 732)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(306, 50)
        Me.ButtonAnnuler.TabIndex = 6
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormHeureTache
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(680, 798)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.PanelApercu)
        Me.Controls.Add(Me.PanelCalcule)
        Me.Controls.Add(Me.PanelFixe)
        Me.Controls.Add(Me.LabelSousTitre)
        Me.Controls.Add(Me.LabelTitre)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormHeureTache"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Heure de la tâche"
        Me.PanelFixe.ResumeLayout(False)
        Me.PanelFixe.PerformLayout()
        Me.PanelCalcule.ResumeLayout(False)
        Me.PanelCalcule.PerformLayout()
        CType(Me.NumericDecalage, System.ComponentModel.ISupportInitialize).EndInit()
        Me.PanelApercu.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents LabelTitre As Label
    Friend WithEvents LabelSousTitre As Label
    Friend WithEvents PanelFixe As Panel
    Friend WithEvents PanelMarqueFixe As Panel
    Friend WithEvents RadioFixe As RadioButton
    Friend WithEvents TimeFixe As ChampHeure
    Friend WithEvents PanelCalcule As Panel
    Friend WithEvents PanelMarqueCalcule As Panel
    Friend WithEvents RadioShabbat As RadioButton
    Friend WithEvents LabelAide As Label
    Friend WithEvents ListeRepere As ListBox
    Friend WithEvents LabelDecalage As Label
    Friend WithEvents NumericDecalage As NumericUpDown
    Friend WithEvents LabelUnite As Label
    Friend WithEvents CheckPlusTard As CheckBox
    Friend WithEvents TimePlusTard As ChampHeure
    Friend WithEvents CheckPlusTot As CheckBox
    Friend WithEvents TimePlusTot As ChampHeure
    Friend WithEvents PanelApercu As Panel
    Friend WithEvents PanelBarre As Panel
    Friend WithEvents LabelApercuTitre As Label
    Friend WithEvents LabelApercu As Label
    Friend WithEvents LabelApercuDetail As Label
    Friend WithEvents ButtonOk As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
