<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormTnt
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
        Me.LabelListe = New System.Windows.Forms.Label()
        Me.ComboListe = New System.Windows.Forms.ComboBox()
        Me.ButtonOuvrir = New System.Windows.Forms.Button()
        Me.ButtonRenommer = New System.Windows.Forms.Button()
        Me.ButtonScanner = New System.Windows.Forms.Button()
        Me.ButtonArreter = New System.Windows.Forms.Button()
        Me.ProgressBarScan = New System.Windows.Forms.ProgressBar()
        Me.LabelPourcentage = New System.Windows.Forms.Label()
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.GrilleChaines = New System.Windows.Forms.DataGridView()
        Me.ColonneNumero = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneNom = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneOfficiel = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneFrequence = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneProgramme = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ButtonMonter = New System.Windows.Forms.Button()
        Me.ButtonDescendre = New System.Windows.Forms.Button()
        Me.ButtonSupprimer = New System.Windows.Forms.Button()
        Me.ButtonNumeroter = New System.Windows.Forms.Button()
        Me.ButtonReinitCle = New System.Windows.Forms.Button()
        Me.ButtonEnregistrer = New System.Windows.Forms.Button()
        Me.CheckBoxUtiliser = New System.Windows.Forms.CheckBox()
        Me.ButtonFermer = New System.Windows.Forms.Button()
        Me.VlcScan = New Vlc.DotNet.Forms.VlcControl()
        CType(Me.GrilleChaines, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.VlcScan, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'LabelListe
        '
        Me.LabelListe.AutoSize = True
        Me.LabelListe.Location = New System.Drawing.Point(12, 22)
        Me.LabelListe.Name = "LabelListe"
        Me.LabelListe.Size = New System.Drawing.Size(230, 25)
        Me.LabelListe.TabIndex = 0
        Me.LabelListe.Text = "Nom de la liste (ville) :"
        '
        'ComboListe
        '
        Me.ComboListe.Location = New System.Drawing.Point(260, 18)
        Me.ComboListe.Name = "ComboListe"
        Me.ComboListe.Size = New System.Drawing.Size(420, 33)
        Me.ComboListe.TabIndex = 1
        '
        'ButtonOuvrir
        '
        Me.ButtonOuvrir.Location = New System.Drawing.Point(692, 14)
        Me.ButtonOuvrir.Name = "ButtonOuvrir"
        Me.ButtonOuvrir.Size = New System.Drawing.Size(150, 40)
        Me.ButtonOuvrir.TabIndex = 2
        Me.ButtonOuvrir.Text = "Ouvrir"
        Me.ButtonOuvrir.UseVisualStyleBackColor = True
        '
        'ButtonRenommer
        '
        Me.ButtonRenommer.Location = New System.Drawing.Point(850, 14)
        Me.ButtonRenommer.Name = "ButtonRenommer"
        Me.ButtonRenommer.Size = New System.Drawing.Size(180, 40)
        Me.ButtonRenommer.TabIndex = 3
        Me.ButtonRenommer.Text = "Renommer"
        Me.ButtonRenommer.UseVisualStyleBackColor = True
        '
        'ButtonScanner
        '
        Me.ButtonScanner.Location = New System.Drawing.Point(1040, 14)
        Me.ButtonScanner.Name = "ButtonScanner"
        Me.ButtonScanner.Size = New System.Drawing.Size(210, 40)
        Me.ButtonScanner.TabIndex = 4
        Me.ButtonScanner.Text = "Scanner la TNT"
        Me.ButtonScanner.UseVisualStyleBackColor = True
        '
        'ButtonArreter
        '
        Me.ButtonArreter.Enabled = False
        Me.ButtonArreter.Location = New System.Drawing.Point(1258, 14)
        Me.ButtonArreter.Name = "ButtonArreter"
        Me.ButtonArreter.Size = New System.Drawing.Size(130, 40)
        Me.ButtonArreter.TabIndex = 5
        Me.ButtonArreter.Text = "Arrêter"
        Me.ButtonArreter.UseVisualStyleBackColor = True
        '
        'ProgressBarScan
        '
        Me.ProgressBarScan.Location = New System.Drawing.Point(12, 68)
        Me.ProgressBarScan.Name = "ProgressBarScan"
        Me.ProgressBarScan.Size = New System.Drawing.Size(1290, 22)
        Me.ProgressBarScan.TabIndex = 6
        '
        'LabelPourcentage
        '
        Me.LabelPourcentage.Font = New System.Drawing.Font("Segoe UI", 10.0!, System.Drawing.FontStyle.Bold)
        Me.LabelPourcentage.Location = New System.Drawing.Point(1308, 65)
        Me.LabelPourcentage.Name = "LabelPourcentage"
        Me.LabelPourcentage.Size = New System.Drawing.Size(80, 28)
        Me.LabelPourcentage.TabIndex = 19
        Me.LabelPourcentage.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'LabelEtat
        '
        Me.LabelEtat.AutoEllipsis = True
        Me.LabelEtat.Location = New System.Drawing.Point(12, 96)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(1376, 28)
        Me.LabelEtat.TabIndex = 7
        Me.LabelEtat.Text = "Choisissez une liste, ou lancez le scan (la TV de Slide est coupée pendant le scan)."
        Me.LabelEtat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'GrilleChaines
        '
        Me.GrilleChaines.AllowUserToAddRows = False
        Me.GrilleChaines.AllowUserToDeleteRows = False
        Me.GrilleChaines.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GrilleChaines.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.GrilleChaines.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ColonneNumero, Me.ColonneNom, Me.ColonneOfficiel, Me.ColonneFrequence, Me.ColonneProgramme})
        Me.GrilleChaines.Location = New System.Drawing.Point(12, 132)
        Me.GrilleChaines.MultiSelect = False
        Me.GrilleChaines.Name = "GrilleChaines"
        Me.GrilleChaines.RowHeadersVisible = False
        Me.GrilleChaines.RowHeadersWidth = 62
        Me.GrilleChaines.RowTemplate.Height = 33
        Me.GrilleChaines.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.GrilleChaines.Size = New System.Drawing.Size(1086, 520)
        Me.GrilleChaines.TabIndex = 8
        '
        'ColonneNumero
        '
        Me.ColonneNumero.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonneNumero.HeaderText = "N° (Slide)"
        Me.ColonneNumero.MinimumWidth = 8
        Me.ColonneNumero.Name = "ColonneNumero"
        Me.ColonneNumero.Width = 130
        '
        'ColonneNom
        '
        Me.ColonneNom.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonneNom.HeaderText = "Chaîne"
        Me.ColonneNom.MinimumWidth = 150
        Me.ColonneNom.Name = "ColonneNom"
        Me.ColonneNom.Width = 330
        '
        'ColonneOfficiel
        '
        Me.ColonneOfficiel.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonneOfficiel.HeaderText = "N° officiel"
        Me.ColonneOfficiel.MinimumWidth = 8
        Me.ColonneOfficiel.Name = "ColonneOfficiel"
        Me.ColonneOfficiel.ReadOnly = True
        Me.ColonneOfficiel.Width = 140
        '
        'ColonneFrequence
        '
        Me.ColonneFrequence.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonneFrequence.HeaderText = "Fréquence"
        Me.ColonneFrequence.MinimumWidth = 8
        Me.ColonneFrequence.Name = "ColonneFrequence"
        Me.ColonneFrequence.ReadOnly = True
        Me.ColonneFrequence.Width = 270
        '
        'ColonneProgramme
        '
        Me.ColonneProgramme.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.ColonneProgramme.HeaderText = "Programme"
        Me.ColonneProgramme.MinimumWidth = 120
        Me.ColonneProgramme.Name = "ColonneProgramme"
        Me.ColonneProgramme.ReadOnly = True
        '
        'ButtonMonter
        '
        Me.ButtonMonter.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonMonter.Location = New System.Drawing.Point(1110, 132)
        Me.ButtonMonter.Name = "ButtonMonter"
        Me.ButtonMonter.Size = New System.Drawing.Size(278, 44)
        Me.ButtonMonter.TabIndex = 9
        Me.ButtonMonter.Text = "▲ Monter"
        Me.ButtonMonter.UseVisualStyleBackColor = True
        '
        'ButtonDescendre
        '
        Me.ButtonDescendre.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDescendre.Location = New System.Drawing.Point(1110, 184)
        Me.ButtonDescendre.Name = "ButtonDescendre"
        Me.ButtonDescendre.Size = New System.Drawing.Size(278, 44)
        Me.ButtonDescendre.TabIndex = 10
        Me.ButtonDescendre.Text = "▼ Descendre"
        Me.ButtonDescendre.UseVisualStyleBackColor = True
        '
        'ButtonSupprimer
        '
        Me.ButtonSupprimer.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonSupprimer.Location = New System.Drawing.Point(1110, 252)
        Me.ButtonSupprimer.Name = "ButtonSupprimer"
        Me.ButtonSupprimer.Size = New System.Drawing.Size(278, 44)
        Me.ButtonSupprimer.TabIndex = 11
        Me.ButtonSupprimer.Text = "Supprimer la chaîne"
        Me.ButtonSupprimer.UseVisualStyleBackColor = True
        '
        'ButtonNumeroter
        '
        Me.ButtonNumeroter.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonNumeroter.Location = New System.Drawing.Point(1110, 320)
        Me.ButtonNumeroter.Name = "ButtonNumeroter"
        Me.ButtonNumeroter.Size = New System.Drawing.Size(278, 76)
        Me.ButtonNumeroter.TabIndex = 12
        Me.ButtonNumeroter.Text = "Numéroter comme le pays (N° officiel)"
        Me.ButtonNumeroter.UseVisualStyleBackColor = True
        '
        'ButtonReinitCle
        '
        Me.ButtonReinitCle.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonReinitCle.Location = New System.Drawing.Point(1110, 432)
        Me.ButtonReinitCle.Name = "ButtonReinitCle"
        Me.ButtonReinitCle.Size = New System.Drawing.Size(278, 76)
        Me.ButtonReinitCle.TabIndex = 17
        Me.ButtonReinitCle.Text = "Réinitialiser la clé TNT (active la clé et relance Slide)"
        Me.ButtonReinitCle.UseVisualStyleBackColor = True
        '
        'ButtonEnregistrer
        '
        Me.ButtonEnregistrer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonEnregistrer.Location = New System.Drawing.Point(12, 668)
        Me.ButtonEnregistrer.Name = "ButtonEnregistrer"
        Me.ButtonEnregistrer.Size = New System.Drawing.Size(300, 48)
        Me.ButtonEnregistrer.TabIndex = 13
        Me.ButtonEnregistrer.Text = "Enregistrer la liste"
        Me.ButtonEnregistrer.UseVisualStyleBackColor = True
        '
        'CheckBoxUtiliser
        '
        Me.CheckBoxUtiliser.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.CheckBoxUtiliser.AutoSize = True
        Me.CheckBoxUtiliser.Location = New System.Drawing.Point(332, 678)
        Me.CheckBoxUtiliser.Name = "CheckBoxUtiliser"
        Me.CheckBoxUtiliser.Size = New System.Drawing.Size(470, 29)
        Me.CheckBoxUtiliser.TabIndex = 14
        Me.CheckBoxUtiliser.Text = "Utiliser cette liste pour la TV de Slide"
        Me.CheckBoxUtiliser.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonFermer.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonFermer.Location = New System.Drawing.Point(1198, 668)
        Me.ButtonFermer.Name = "ButtonFermer"
        Me.ButtonFermer.Size = New System.Drawing.Size(190, 48)
        Me.ButtonFermer.TabIndex = 15
        Me.ButtonFermer.Text = "Fermer"
        Me.ButtonFermer.UseVisualStyleBackColor = True
        '
        'VlcScan
        '
        Me.VlcScan.BackColor = System.Drawing.Color.Black
        Me.VlcScan.Location = New System.Drawing.Point(12, 600)
        Me.VlcScan.Name = "VlcScan"
        Me.VlcScan.Size = New System.Drawing.Size(16, 16)
        Me.VlcScan.Spu = -1
        Me.VlcScan.TabIndex = 18
        Me.VlcScan.Text = "VlcScan"
        Me.VlcScan.VlcLibDirectory = Nothing
        Me.VlcScan.VlcMediaplayerOptions = Nothing
        Me.VlcScan.Visible = False
        '
        'FormTnt
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonFermer
        Me.ClientSize = New System.Drawing.Size(1400, 736)
        Me.Controls.Add(Me.VlcScan)
        Me.Controls.Add(Me.LabelPourcentage)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.CheckBoxUtiliser)
        Me.Controls.Add(Me.ButtonEnregistrer)
        Me.Controls.Add(Me.ButtonReinitCle)
        Me.Controls.Add(Me.ButtonNumeroter)
        Me.Controls.Add(Me.ButtonSupprimer)
        Me.Controls.Add(Me.ButtonDescendre)
        Me.Controls.Add(Me.ButtonMonter)
        Me.Controls.Add(Me.GrilleChaines)
        Me.Controls.Add(Me.LabelEtat)
        Me.Controls.Add(Me.ProgressBarScan)
        Me.Controls.Add(Me.ButtonArreter)
        Me.Controls.Add(Me.ButtonScanner)
        Me.Controls.Add(Me.ButtonRenommer)
        Me.Controls.Add(Me.ButtonOuvrir)
        Me.Controls.Add(Me.ComboListe)
        Me.Controls.Add(Me.LabelListe)
        Me.MinimumSize = New System.Drawing.Size(1300, 600)
        Me.Name = "FormTnt"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "TNT : scanner et organiser les chaînes"
        CType(Me.GrilleChaines, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.VlcScan, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents LabelListe As Label
    Friend WithEvents ComboListe As ComboBox
    Friend WithEvents ButtonOuvrir As Button
    Friend WithEvents ButtonRenommer As Button
    Friend WithEvents ButtonScanner As Button
    Friend WithEvents ButtonArreter As Button
    Friend WithEvents ProgressBarScan As ProgressBar
    Friend WithEvents LabelPourcentage As Label
    Friend WithEvents LabelEtat As Label
    Friend WithEvents GrilleChaines As DataGridView
    Friend WithEvents ColonneNumero As DataGridViewTextBoxColumn
    Friend WithEvents ColonneNom As DataGridViewTextBoxColumn
    Friend WithEvents ColonneOfficiel As DataGridViewTextBoxColumn
    Friend WithEvents ColonneFrequence As DataGridViewTextBoxColumn
    Friend WithEvents ColonneProgramme As DataGridViewTextBoxColumn
    Friend WithEvents ButtonMonter As Button
    Friend WithEvents ButtonDescendre As Button
    Friend WithEvents ButtonSupprimer As Button
    Friend WithEvents ButtonNumeroter As Button
    Friend WithEvents ButtonReinitCle As Button
    Friend WithEvents ButtonEnregistrer As Button
    Friend WithEvents CheckBoxUtiliser As CheckBox
    Friend WithEvents ButtonFermer As Button
    Friend WithEvents VlcScan As Vlc.DotNet.Forms.VlcControl
End Class
