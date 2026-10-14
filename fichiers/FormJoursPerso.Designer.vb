<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormJoursPerso
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
        Me.PanelJours = New System.Windows.Forms.Panel()
        Me.PanelMarqueJours = New System.Windows.Forms.Panel()
        Me.CaseLundi = New System.Windows.Forms.CheckBox()
        Me.CaseMardi = New System.Windows.Forms.CheckBox()
        Me.CaseMercredi = New System.Windows.Forms.CheckBox()
        Me.CaseJeudi = New System.Windows.Forms.CheckBox()
        Me.CaseVendredi = New System.Windows.Forms.CheckBox()
        Me.CaseSamedi = New System.Windows.Forms.CheckBox()
        Me.CaseDimanche = New System.Windows.Forms.CheckBox()
        Me.ButtonSemaine = New System.Windows.Forms.Button()
        Me.ButtonWeekEnd = New System.Windows.Forms.Button()
        Me.ButtonTous = New System.Windows.Forms.Button()
        Me.ButtonAucun = New System.Windows.Forms.Button()
        Me.PanelResultat = New System.Windows.Forms.Panel()
        Me.PanelBarreResultat = New System.Windows.Forms.Panel()
        Me.LabelResultatTitre = New System.Windows.Forms.Label()
        Me.LabelResultat = New System.Windows.Forms.Label()
        Me.ButtonOk = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.PanelJours.SuspendLayout()
        Me.PanelResultat.SuspendLayout()
        Me.SuspendLayout()
        '
        'LabelTitre
        '
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 17.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTitre.Location = New System.Drawing.Point(28, 22)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(504, 40)
        Me.LabelTitre.TabIndex = 0
        Me.LabelTitre.Text = "Jours de la tâche"
        '
        'LabelSousTitre
        '
        Me.LabelSousTitre.Font = New System.Drawing.Font("Segoe UI", 10.5!)
        Me.LabelSousTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelSousTitre.Location = New System.Drawing.Point(28, 64)
        Me.LabelSousTitre.Name = "LabelSousTitre"
        Me.LabelSousTitre.Size = New System.Drawing.Size(504, 28)
        Me.LabelSousTitre.TabIndex = 1
        Me.LabelSousTitre.Text = "Cochez les jours de la semaine où elle doit démarrer."
        '
        'PanelJours
        '
        Me.PanelJours.BackColor = System.Drawing.Color.FromArgb(CType(CType(26, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(70, Byte), Integer))
        Me.PanelJours.Controls.Add(Me.ButtonAucun)
        Me.PanelJours.Controls.Add(Me.ButtonTous)
        Me.PanelJours.Controls.Add(Me.ButtonWeekEnd)
        Me.PanelJours.Controls.Add(Me.ButtonSemaine)
        Me.PanelJours.Controls.Add(Me.CaseDimanche)
        Me.PanelJours.Controls.Add(Me.CaseSamedi)
        Me.PanelJours.Controls.Add(Me.CaseVendredi)
        Me.PanelJours.Controls.Add(Me.CaseJeudi)
        Me.PanelJours.Controls.Add(Me.CaseMercredi)
        Me.PanelJours.Controls.Add(Me.CaseMardi)
        Me.PanelJours.Controls.Add(Me.CaseLundi)
        Me.PanelJours.Controls.Add(Me.PanelMarqueJours)
        Me.PanelJours.Location = New System.Drawing.Point(28, 104)
        Me.PanelJours.Name = "PanelJours"
        Me.PanelJours.Size = New System.Drawing.Size(504, 300)
        Me.PanelJours.TabIndex = 2
        '
        'PanelMarqueJours
        '
        Me.PanelMarqueJours.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(132, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.PanelMarqueJours.Dock = System.Windows.Forms.DockStyle.Left
        Me.PanelMarqueJours.Location = New System.Drawing.Point(0, 0)
        Me.PanelMarqueJours.Name = "PanelMarqueJours"
        Me.PanelMarqueJours.Size = New System.Drawing.Size(6, 300)
        Me.PanelMarqueJours.TabIndex = 11
        '
        'CaseLundi
        '
        Me.CaseLundi.AutoSize = True
        Me.CaseLundi.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CaseLundi.Location = New System.Drawing.Point(30, 16)
        Me.CaseLundi.Name = "CaseLundi"
        Me.CaseLundi.Size = New System.Drawing.Size(100, 29)
        Me.CaseLundi.TabIndex = 0
        Me.CaseLundi.Text = "Lundi"
        Me.CaseLundi.UseVisualStyleBackColor = True
        '
        'CaseMardi
        '
        Me.CaseMardi.AutoSize = True
        Me.CaseMardi.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CaseMardi.Location = New System.Drawing.Point(30, 58)
        Me.CaseMardi.Name = "CaseMardi"
        Me.CaseMardi.Size = New System.Drawing.Size(100, 29)
        Me.CaseMardi.TabIndex = 1
        Me.CaseMardi.Text = "Mardi"
        Me.CaseMardi.UseVisualStyleBackColor = True
        '
        'CaseMercredi
        '
        Me.CaseMercredi.AutoSize = True
        Me.CaseMercredi.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CaseMercredi.Location = New System.Drawing.Point(30, 100)
        Me.CaseMercredi.Name = "CaseMercredi"
        Me.CaseMercredi.Size = New System.Drawing.Size(110, 29)
        Me.CaseMercredi.TabIndex = 2
        Me.CaseMercredi.Text = "Mercredi"
        Me.CaseMercredi.UseVisualStyleBackColor = True
        '
        'CaseJeudi
        '
        Me.CaseJeudi.AutoSize = True
        Me.CaseJeudi.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CaseJeudi.Location = New System.Drawing.Point(30, 142)
        Me.CaseJeudi.Name = "CaseJeudi"
        Me.CaseJeudi.Size = New System.Drawing.Size(100, 29)
        Me.CaseJeudi.TabIndex = 3
        Me.CaseJeudi.Text = "Jeudi"
        Me.CaseJeudi.UseVisualStyleBackColor = True
        '
        'CaseVendredi
        '
        Me.CaseVendredi.AutoSize = True
        Me.CaseVendredi.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CaseVendredi.Location = New System.Drawing.Point(30, 184)
        Me.CaseVendredi.Name = "CaseVendredi"
        Me.CaseVendredi.Size = New System.Drawing.Size(110, 29)
        Me.CaseVendredi.TabIndex = 4
        Me.CaseVendredi.Text = "Vendredi"
        Me.CaseVendredi.UseVisualStyleBackColor = True
        '
        'CaseSamedi
        '
        Me.CaseSamedi.AutoSize = True
        Me.CaseSamedi.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CaseSamedi.Location = New System.Drawing.Point(30, 226)
        Me.CaseSamedi.Name = "CaseSamedi"
        Me.CaseSamedi.Size = New System.Drawing.Size(100, 29)
        Me.CaseSamedi.TabIndex = 5
        Me.CaseSamedi.Text = "Samedi"
        Me.CaseSamedi.UseVisualStyleBackColor = True
        '
        'CaseDimanche
        '
        Me.CaseDimanche.AutoSize = True
        Me.CaseDimanche.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.CaseDimanche.Location = New System.Drawing.Point(30, 262)
        Me.CaseDimanche.Name = "CaseDimanche"
        Me.CaseDimanche.Size = New System.Drawing.Size(120, 29)
        Me.CaseDimanche.TabIndex = 6
        Me.CaseDimanche.Text = "Dimanche"
        Me.CaseDimanche.UseVisualStyleBackColor = True
        '
        'ButtonSemaine
        '
        Me.ButtonSemaine.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.ButtonSemaine.Location = New System.Drawing.Point(262, 16)
        Me.ButtonSemaine.Name = "ButtonSemaine"
        Me.ButtonSemaine.Size = New System.Drawing.Size(220, 44)
        Me.ButtonSemaine.TabIndex = 7
        Me.ButtonSemaine.Text = "Lundi au vendredi"
        Me.ButtonSemaine.UseVisualStyleBackColor = True
        '
        'ButtonWeekEnd
        '
        Me.ButtonWeekEnd.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.ButtonWeekEnd.Location = New System.Drawing.Point(262, 70)
        Me.ButtonWeekEnd.Name = "ButtonWeekEnd"
        Me.ButtonWeekEnd.Size = New System.Drawing.Size(220, 44)
        Me.ButtonWeekEnd.TabIndex = 8
        Me.ButtonWeekEnd.Text = "Samedi et dimanche"
        Me.ButtonWeekEnd.UseVisualStyleBackColor = True
        '
        'ButtonTous
        '
        Me.ButtonTous.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.ButtonTous.Location = New System.Drawing.Point(262, 124)
        Me.ButtonTous.Name = "ButtonTous"
        Me.ButtonTous.Size = New System.Drawing.Size(220, 44)
        Me.ButtonTous.TabIndex = 9
        Me.ButtonTous.Text = "Tous les jours"
        Me.ButtonTous.UseVisualStyleBackColor = True
        '
        'ButtonAucun
        '
        Me.ButtonAucun.Font = New System.Drawing.Font("Segoe UI", 10.0!)
        Me.ButtonAucun.Location = New System.Drawing.Point(262, 178)
        Me.ButtonAucun.Name = "ButtonAucun"
        Me.ButtonAucun.Size = New System.Drawing.Size(220, 44)
        Me.ButtonAucun.TabIndex = 10
        Me.ButtonAucun.Text = "Aucun"
        Me.ButtonAucun.UseVisualStyleBackColor = True
        '
        'PanelResultat
        '
        Me.PanelResultat.BackColor = System.Drawing.Color.FromArgb(CType(CType(26, Byte), Integer), CType(CType(54, Byte), Integer), CType(CType(38, Byte), Integer))
        Me.PanelResultat.Controls.Add(Me.LabelResultat)
        Me.PanelResultat.Controls.Add(Me.LabelResultatTitre)
        Me.PanelResultat.Controls.Add(Me.PanelBarreResultat)
        Me.PanelResultat.Location = New System.Drawing.Point(28, 416)
        Me.PanelResultat.Name = "PanelResultat"
        Me.PanelResultat.Size = New System.Drawing.Size(504, 92)
        Me.PanelResultat.TabIndex = 3
        '
        'PanelBarreResultat
        '
        Me.PanelBarreResultat.BackColor = System.Drawing.Color.FromArgb(CType(CType(48, Byte), Integer), CType(CType(209, Byte), Integer), CType(CType(88, Byte), Integer))
        Me.PanelBarreResultat.Dock = System.Windows.Forms.DockStyle.Left
        Me.PanelBarreResultat.Location = New System.Drawing.Point(0, 0)
        Me.PanelBarreResultat.Name = "PanelBarreResultat"
        Me.PanelBarreResultat.Size = New System.Drawing.Size(6, 92)
        Me.PanelBarreResultat.TabIndex = 0
        '
        'LabelResultatTitre
        '
        Me.LabelResultatTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 8.5!, System.Drawing.FontStyle.Bold)
        Me.LabelResultatTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(110, Byte), Integer), CType(CType(224, Byte), Integer), CType(CType(140, Byte), Integer))
        Me.LabelResultatTitre.Location = New System.Drawing.Point(28, 12)
        Me.LabelResultatTitre.Name = "LabelResultatTitre"
        Me.LabelResultatTitre.Size = New System.Drawing.Size(300, 22)
        Me.LabelResultatTitre.TabIndex = 1
        Me.LabelResultatTitre.Text = "LA TÂCHE DÉMARRERA"
        '
        'LabelResultat
        '
        Me.LabelResultat.Font = New System.Drawing.Font("Segoe UI Semibold", 17.0!, System.Drawing.FontStyle.Bold)
        Me.LabelResultat.Location = New System.Drawing.Point(28, 38)
        Me.LabelResultat.Name = "LabelResultat"
        Me.LabelResultat.Size = New System.Drawing.Size(462, 46)
        Me.LabelResultat.TabIndex = 2
        '
        'ButtonOk
        '
        Me.ButtonOk.Font = New System.Drawing.Font("Segoe UI Semibold", 11.0!, System.Drawing.FontStyle.Bold)
        Me.ButtonOk.Location = New System.Drawing.Point(28, 524)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(246, 50)
        Me.ButtonOk.TabIndex = 4
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ButtonAnnuler.Location = New System.Drawing.Point(286, 524)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(246, 50)
        Me.ButtonAnnuler.TabIndex = 5
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormJoursPerso
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(560, 600)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.PanelResultat)
        Me.Controls.Add(Me.PanelJours)
        Me.Controls.Add(Me.LabelSousTitre)
        Me.Controls.Add(Me.LabelTitre)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormJoursPerso"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Jours de la tâche"
        Me.PanelJours.ResumeLayout(False)
        Me.PanelJours.PerformLayout()
        Me.PanelResultat.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents LabelTitre As Label
    Friend WithEvents LabelSousTitre As Label
    Friend WithEvents PanelJours As Panel
    Friend WithEvents PanelMarqueJours As Panel
    Friend WithEvents CaseLundi As CheckBox
    Friend WithEvents CaseMardi As CheckBox
    Friend WithEvents CaseMercredi As CheckBox
    Friend WithEvents CaseJeudi As CheckBox
    Friend WithEvents CaseVendredi As CheckBox
    Friend WithEvents CaseSamedi As CheckBox
    Friend WithEvents CaseDimanche As CheckBox
    Friend WithEvents ButtonSemaine As Button
    Friend WithEvents ButtonWeekEnd As Button
    Friend WithEvents ButtonTous As Button
    Friend WithEvents ButtonAucun As Button
    Friend WithEvents PanelResultat As Panel
    Friend WithEvents PanelBarreResultat As Panel
    Friend WithEvents LabelResultatTitre As Label
    Friend WithEvents LabelResultat As Label
    Friend WithEvents ButtonOk As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
