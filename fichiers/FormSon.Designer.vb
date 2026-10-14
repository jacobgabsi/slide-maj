<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSon
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
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.GroupBoxVideo = New System.Windows.Forms.GroupBox()
        Me.ComboVideo = New System.Windows.Forms.ComboBox()
        Me.GroupBoxMusique = New System.Windows.Forms.GroupBox()
        Me.ComboMusique = New System.Windows.Forms.ComboBox()
        Me.GroupBoxSources = New System.Windows.Forms.GroupBox()
        Me.CheckBoxSources = New System.Windows.Forms.CheckBox()
        Me.CheckBoxMp3 = New System.Windows.Forms.CheckBox()
        Me.CheckBoxRadio = New System.Windows.Forms.CheckBox()
        Me.ComboRadio = New System.Windows.Forms.ComboBox()
        Me.CheckBoxDeezer = New System.Windows.Forms.CheckBox()
        Me.ComboPlaylist = New System.Windows.Forms.ComboBox()
        Me.LabelAidePlaylist = New System.Windows.Forms.Label()
        Me.GroupBoxAlternance = New System.Windows.Forms.GroupBox()
        Me.CheckBoxDuree = New System.Windows.Forms.CheckBox()
        Me.NumericDuree = New System.Windows.Forms.NumericUpDown()
        Me.LabelMinutes = New System.Windows.Forms.Label()
        Me.PanelResume = New System.Windows.Forms.Panel()
        Me.LabelResumeTitre = New System.Windows.Forms.Label()
        Me.LabelResume = New System.Windows.Forms.Label()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.ToolTip1 = New System.Windows.Forms.ToolTip(Me.components)
        Me.GroupBoxVideo.SuspendLayout()
        Me.GroupBoxMusique.SuspendLayout()
        Me.GroupBoxSources.SuspendLayout()
        Me.GroupBoxAlternance.SuspendLayout()
        Me.PanelResume.SuspendLayout()
        CType(Me.NumericDuree, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(16, 12)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(568, 44)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Choisissez ce que cette tâche change dans le son. Ce qui reste sur « Ne pas changer » ou décoché n'est pas touché."
        '
        'GroupBoxVideo
        '
        Me.GroupBoxVideo.Controls.Add(Me.ComboVideo)
        Me.GroupBoxVideo.Location = New System.Drawing.Point(16, 60)
        Me.GroupBoxVideo.Name = "GroupBoxVideo"
        Me.GroupBoxVideo.Size = New System.Drawing.Size(278, 70)
        Me.GroupBoxVideo.TabIndex = 1
        Me.GroupBoxVideo.TabStop = False
        Me.GroupBoxVideo.Text = "Vidéo avec le son"
        '
        'ComboVideo
        '
        Me.ComboVideo.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboVideo.FormattingEnabled = True
        Me.ComboVideo.Items.AddRange(New Object() {"Ne pas changer", "Activé", "Désactivé"})
        Me.ComboVideo.Location = New System.Drawing.Point(16, 28)
        Me.ComboVideo.Name = "ComboVideo"
        Me.ComboVideo.Size = New System.Drawing.Size(246, 28)
        Me.ComboVideo.TabIndex = 0
        '
        'GroupBoxMusique
        '
        Me.GroupBoxMusique.Controls.Add(Me.ComboMusique)
        Me.GroupBoxMusique.Location = New System.Drawing.Point(306, 60)
        Me.GroupBoxMusique.Name = "GroupBoxMusique"
        Me.GroupBoxMusique.Size = New System.Drawing.Size(278, 70)
        Me.GroupBoxMusique.TabIndex = 2
        Me.GroupBoxMusique.TabStop = False
        Me.GroupBoxMusique.Text = "Musique"
        '
        'ComboMusique
        '
        Me.ComboMusique.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboMusique.FormattingEnabled = True
        Me.ComboMusique.Items.AddRange(New Object() {"Ne pas changer", "Activée", "Désactivée"})
        Me.ComboMusique.Location = New System.Drawing.Point(16, 28)
        Me.ComboMusique.Name = "ComboMusique"
        Me.ComboMusique.Size = New System.Drawing.Size(246, 28)
        Me.ComboMusique.TabIndex = 0
        '
        'GroupBoxSources
        '
        Me.GroupBoxSources.Controls.Add(Me.CheckBoxSources)
        Me.GroupBoxSources.Controls.Add(Me.CheckBoxMp3)
        Me.GroupBoxSources.Controls.Add(Me.CheckBoxRadio)
        Me.GroupBoxSources.Controls.Add(Me.ComboRadio)
        Me.GroupBoxSources.Controls.Add(Me.CheckBoxDeezer)
        Me.GroupBoxSources.Controls.Add(Me.ComboPlaylist)
        Me.GroupBoxSources.Controls.Add(Me.LabelAidePlaylist)
        Me.GroupBoxSources.Location = New System.Drawing.Point(16, 140)
        Me.GroupBoxSources.Name = "GroupBoxSources"
        Me.GroupBoxSources.Size = New System.Drawing.Size(568, 196)
        Me.GroupBoxSources.TabIndex = 3
        Me.GroupBoxSources.TabStop = False
        Me.GroupBoxSources.Text = "Sources de la musique"
        '
        'CheckBoxSources
        '
        Me.CheckBoxSources.AutoSize = True
        Me.CheckBoxSources.Location = New System.Drawing.Point(16, 28)
        Me.CheckBoxSources.Name = "CheckBoxSources"
        Me.CheckBoxSources.Size = New System.Drawing.Size(210, 24)
        Me.CheckBoxSources.TabIndex = 0
        Me.CheckBoxSources.Text = "Changer les sources cochées"
        Me.ToolTip1.SetToolTip(Me.CheckBoxSources, "Plusieurs sources cochées : Slide alterne de l'une à l'autre.")
        Me.CheckBoxSources.UseVisualStyleBackColor = True
        '
        'CheckBoxMp3
        '
        Me.CheckBoxMp3.AutoSize = True
        Me.CheckBoxMp3.Location = New System.Drawing.Point(40, 66)
        Me.CheckBoxMp3.Name = "CheckBoxMp3"
        Me.CheckBoxMp3.Size = New System.Drawing.Size(124, 24)
        Me.CheckBoxMp3.TabIndex = 1
        Me.CheckBoxMp3.Text = "Fichiers MP3"
        Me.CheckBoxMp3.UseVisualStyleBackColor = True
        '
        'CheckBoxRadio
        '
        Me.CheckBoxRadio.AutoSize = True
        Me.CheckBoxRadio.Location = New System.Drawing.Point(40, 104)
        Me.CheckBoxRadio.Name = "CheckBoxRadio"
        Me.CheckBoxRadio.Size = New System.Drawing.Size(137, 24)
        Me.CheckBoxRadio.TabIndex = 2
        Me.CheckBoxRadio.Text = "Radio Internet"
        Me.CheckBoxRadio.UseVisualStyleBackColor = True
        '
        'ComboRadio
        '
        Me.ComboRadio.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboRadio.FormattingEnabled = True
        Me.ComboRadio.Location = New System.Drawing.Point(210, 101)
        Me.ComboRadio.Name = "ComboRadio"
        Me.ComboRadio.Size = New System.Drawing.Size(342, 28)
        Me.ComboRadio.TabIndex = 3
        Me.ToolTip1.SetToolTip(Me.ComboRadio, "La station jouée quand la source est la radio (liste du cadre Radio, onglet Audio).")
        '
        'CheckBoxDeezer
        '
        Me.CheckBoxDeezer.AutoSize = True
        Me.CheckBoxDeezer.Location = New System.Drawing.Point(40, 142)
        Me.CheckBoxDeezer.Name = "CheckBoxDeezer"
        Me.CheckBoxDeezer.Size = New System.Drawing.Size(80, 24)
        Me.CheckBoxDeezer.TabIndex = 4
        Me.CheckBoxDeezer.Text = "Deezer"
        Me.CheckBoxDeezer.UseVisualStyleBackColor = True
        '
        'ComboPlaylist
        '
        Me.ComboPlaylist.FormattingEnabled = True
        Me.ComboPlaylist.Items.AddRange(New Object() {"Playlist des réglages", "Flow", "Favoris (coups de cœur)"})
        Me.ComboPlaylist.Location = New System.Drawing.Point(210, 139)
        Me.ComboPlaylist.Name = "ComboPlaylist"
        Me.ComboPlaylist.Size = New System.Drawing.Size(342, 28)
        Me.ComboPlaylist.TabIndex = 5
        Me.ToolTip1.SetToolTip(Me.ComboPlaylist, "La playlist lancée quand la source est Deezer.")
        '
        'LabelAidePlaylist
        '
        Me.LabelAidePlaylist.AutoSize = True
        Me.LabelAidePlaylist.ForeColor = System.Drawing.SystemColors.GrayText
        Me.LabelAidePlaylist.Location = New System.Drawing.Point(210, 169)
        Me.LabelAidePlaylist.Name = "LabelAidePlaylist"
        Me.LabelAidePlaylist.Size = New System.Drawing.Size(300, 20)
        Me.LabelAidePlaylist.TabIndex = 6
        Me.LabelAidePlaylist.Text = "ou collez l'adresse Deezer (Partager > Copier le lien)"
        '
        'GroupBoxAlternance
        '
        Me.GroupBoxAlternance.Controls.Add(Me.CheckBoxDuree)
        Me.GroupBoxAlternance.Controls.Add(Me.NumericDuree)
        Me.GroupBoxAlternance.Controls.Add(Me.LabelMinutes)
        Me.GroupBoxAlternance.Location = New System.Drawing.Point(16, 346)
        Me.GroupBoxAlternance.Name = "GroupBoxAlternance"
        Me.GroupBoxAlternance.Size = New System.Drawing.Size(568, 68)
        Me.GroupBoxAlternance.TabIndex = 4
        Me.GroupBoxAlternance.TabStop = False
        Me.GroupBoxAlternance.Text = "Alternance des sources"
        '
        'CheckBoxDuree
        '
        Me.CheckBoxDuree.AutoSize = True
        Me.CheckBoxDuree.Location = New System.Drawing.Point(16, 28)
        Me.CheckBoxDuree.Name = "CheckBoxDuree"
        Me.CheckBoxDuree.Size = New System.Drawing.Size(190, 24)
        Me.CheckBoxDuree.TabIndex = 0
        Me.CheckBoxDuree.Text = "Alterner toutes les"
        Me.CheckBoxDuree.UseVisualStyleBackColor = True
        '
        'NumericDuree
        '
        Me.NumericDuree.Location = New System.Drawing.Point(210, 26)
        Me.NumericDuree.Maximum = New Decimal(New Integer() {240, 0, 0, 0})
        Me.NumericDuree.Minimum = New Decimal(New Integer() {1, 0, 0, 0})
        Me.NumericDuree.Name = "NumericDuree"
        Me.NumericDuree.Size = New System.Drawing.Size(80, 27)
        Me.NumericDuree.TabIndex = 1
        Me.NumericDuree.Value = New Decimal(New Integer() {15, 0, 0, 0})
        '
        'LabelMinutes
        '
        Me.LabelMinutes.AutoSize = True
        Me.LabelMinutes.Location = New System.Drawing.Point(298, 29)
        Me.LabelMinutes.Name = "LabelMinutes"
        Me.LabelMinutes.Size = New System.Drawing.Size(62, 20)
        Me.LabelMinutes.TabIndex = 2
        Me.LabelMinutes.Text = "minutes"
        '
        'PanelResume
        '
        Me.PanelResume.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.PanelResume.Controls.Add(Me.LabelResume)
        Me.PanelResume.Controls.Add(Me.LabelResumeTitre)
        Me.PanelResume.Location = New System.Drawing.Point(16, 426)
        Me.PanelResume.Name = "PanelResume"
        Me.PanelResume.Size = New System.Drawing.Size(568, 74)
        Me.PanelResume.TabIndex = 5
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
        Me.LabelResume.Size = New System.Drawing.Size(546, 38)
        Me.LabelResume.TabIndex = 1
        Me.LabelResume.Text = "Rien pour l'instant."
        '
        'ButtonOK
        '
        Me.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOK.Location = New System.Drawing.Point(368, 514)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(100, 36)
        Me.ButtonOK.TabIndex = 6
        Me.ButtonOK.Text = "OK"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(484, 514)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(100, 36)
        Me.ButtonAnnuler.TabIndex = 7
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormSon
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(600, 566)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.PanelResume)
        Me.Controls.Add(Me.GroupBoxAlternance)
        Me.Controls.Add(Me.GroupBoxSources)
        Me.Controls.Add(Me.GroupBoxMusique)
        Me.Controls.Add(Me.GroupBoxVideo)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormSon"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Tâche Son"
        Me.GroupBoxVideo.ResumeLayout(False)
        Me.GroupBoxMusique.ResumeLayout(False)
        Me.GroupBoxSources.ResumeLayout(False)
        Me.GroupBoxSources.PerformLayout()
        Me.GroupBoxAlternance.ResumeLayout(False)
        Me.GroupBoxAlternance.PerformLayout()
        Me.PanelResume.ResumeLayout(False)
        Me.PanelResume.PerformLayout()
        CType(Me.NumericDuree, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelAide As Label
    Friend WithEvents GroupBoxVideo As GroupBox
    Friend WithEvents ComboVideo As ComboBox
    Friend WithEvents GroupBoxMusique As GroupBox
    Friend WithEvents ComboMusique As ComboBox
    Friend WithEvents GroupBoxSources As GroupBox
    Friend WithEvents CheckBoxSources As CheckBox
    Friend WithEvents CheckBoxMp3 As CheckBox
    Friend WithEvents CheckBoxRadio As CheckBox
    Friend WithEvents ComboRadio As ComboBox
    Friend WithEvents CheckBoxDeezer As CheckBox
    Friend WithEvents ComboPlaylist As ComboBox
    Friend WithEvents LabelAidePlaylist As Label
    Friend WithEvents GroupBoxAlternance As GroupBox
    Friend WithEvents CheckBoxDuree As CheckBox
    Friend WithEvents NumericDuree As NumericUpDown
    Friend WithEvents LabelMinutes As Label
    Friend WithEvents PanelResume As Panel
    Friend WithEvents LabelResumeTitre As Label
    Friend WithEvents LabelResume As Label
    Friend WithEvents ButtonOK As Button
    Friend WithEvents ButtonAnnuler As Button
    Friend WithEvents ToolTip1 As ToolTip
End Class
