<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormYomTov
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
        Me.LabelVille = New System.Windows.Forms.Label()
        Me.ComboVille = New System.Windows.Forms.ComboBox()
        Me.ListViewFetes = New System.Windows.Forms.ListView()
        Me.ColonneFete = New System.Windows.Forms.ColumnHeader()
        Me.ColonneEntree = New System.Windows.Forms.ColumnHeader()
        Me.ColonneSortie = New System.Windows.Forms.ColumnHeader()
        Me.GroupBoxManuel = New System.Windows.Forms.GroupBox()
        Me.LabelNom = New System.Windows.Forms.Label()
        Me.TextBoxNom = New System.Windows.Forms.TextBox()
        Me.LabelEntree = New System.Windows.Forms.Label()
        Me.DateEntree = New System.Windows.Forms.DateTimePicker()
        Me.LabelSortie = New System.Windows.Forms.Label()
        Me.DateSortie = New System.Windows.Forms.DateTimePicker()
        Me.ButtonAjouterManuel = New System.Windows.Forms.Button()
        Me.CheckBoxEntree = New System.Windows.Forms.CheckBox()
        Me.CheckBoxSortie = New System.Windows.Forms.CheckBox()
        Me.ButtonVide = New System.Windows.Forms.Button()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.GroupBoxManuel.SuspendLayout()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(16, 12)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(688, 44)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Cochez les fêtes à ajouter à la minuterie (type « Yom Tov »). Les dates (un ou deux jours) et les horaires (zmanim) dépendent de la localité choisie ; vous pouvez aussi saisir une date à la main."
        '
        'LabelVille
        '
        Me.LabelVille.AutoSize = True
        Me.LabelVille.Location = New System.Drawing.Point(16, 62)
        Me.LabelVille.Name = "LabelVille"
        Me.LabelVille.Size = New System.Drawing.Size(86, 20)
        Me.LabelVille.TabIndex = 7
        Me.LabelVille.Text = "Localité :"
        '
        'ComboVille
        '
        Me.ComboVille.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboVille.FormattingEnabled = True
        Me.ComboVille.Location = New System.Drawing.Point(110, 58)
        Me.ComboVille.Name = "ComboVille"
        Me.ComboVille.Size = New System.Drawing.Size(300, 28)
        Me.ComboVille.TabIndex = 8
        '
        'ListViewFetes
        '
        Me.ListViewFetes.CheckBoxes = True
        Me.ListViewFetes.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColonneFete, Me.ColonneEntree, Me.ColonneSortie})
        Me.ListViewFetes.FullRowSelect = True
        Me.ListViewFetes.HideSelection = False
        Me.ListViewFetes.Location = New System.Drawing.Point(16, 100)
        Me.ListViewFetes.Name = "ListViewFetes"
        Me.ListViewFetes.Size = New System.Drawing.Size(688, 224)
        Me.ListViewFetes.TabIndex = 1
        Me.ListViewFetes.UseCompatibleStateImageBehavior = False
        Me.ListViewFetes.View = System.Windows.Forms.View.Details
        '
        'ColonneFete
        '
        Me.ColonneFete.Text = "Fête"
        Me.ColonneFete.Width = 240
        '
        'ColonneEntree
        '
        Me.ColonneEntree.Text = "Entrée"
        Me.ColonneEntree.Width = 230
        '
        'ColonneSortie
        '
        Me.ColonneSortie.Text = "Sortie"
        Me.ColonneSortie.Width = 210
        '
        'GroupBoxManuel
        '
        Me.GroupBoxManuel.Controls.Add(Me.LabelNom)
        Me.GroupBoxManuel.Controls.Add(Me.TextBoxNom)
        Me.GroupBoxManuel.Controls.Add(Me.LabelEntree)
        Me.GroupBoxManuel.Controls.Add(Me.DateEntree)
        Me.GroupBoxManuel.Controls.Add(Me.LabelSortie)
        Me.GroupBoxManuel.Controls.Add(Me.DateSortie)
        Me.GroupBoxManuel.Controls.Add(Me.ButtonAjouterManuel)
        Me.GroupBoxManuel.Location = New System.Drawing.Point(16, 334)
        Me.GroupBoxManuel.Name = "GroupBoxManuel"
        Me.GroupBoxManuel.Size = New System.Drawing.Size(688, 124)
        Me.GroupBoxManuel.TabIndex = 2
        Me.GroupBoxManuel.TabStop = False
        Me.GroupBoxManuel.Text = "Date saisie à la main"
        '
        'LabelNom
        '
        Me.LabelNom.AutoSize = True
        Me.LabelNom.Location = New System.Drawing.Point(16, 34)
        Me.LabelNom.Name = "LabelNom"
        Me.LabelNom.Size = New System.Drawing.Size(45, 20)
        Me.LabelNom.TabIndex = 0
        Me.LabelNom.Text = "Nom :"
        '
        'TextBoxNom
        '
        Me.TextBoxNom.Location = New System.Drawing.Point(150, 31)
        Me.TextBoxNom.Name = "TextBoxNom"
        Me.TextBoxNom.Size = New System.Drawing.Size(250, 27)
        Me.TextBoxNom.TabIndex = 1
        Me.TextBoxNom.Text = "Fête"
        '
        'LabelEntree
        '
        Me.LabelEntree.AutoSize = True
        Me.LabelEntree.Location = New System.Drawing.Point(16, 80)
        Me.LabelEntree.Name = "LabelEntree"
        Me.LabelEntree.Size = New System.Drawing.Size(124, 20)
        Me.LabelEntree.TabIndex = 2
        Me.LabelEntree.Text = "Jour d'entrée :"
        '
        'DateEntree
        '
        Me.DateEntree.Format = System.Windows.Forms.DateTimePickerFormat.Long
        Me.DateEntree.Location = New System.Drawing.Point(150, 76)
        Me.DateEntree.Name = "DateEntree"
        Me.DateEntree.Size = New System.Drawing.Size(230, 27)
        Me.DateEntree.TabIndex = 3
        '
        'LabelSortie
        '
        Me.LabelSortie.AutoSize = True
        Me.LabelSortie.Location = New System.Drawing.Point(396, 80)
        Me.LabelSortie.Name = "LabelSortie"
        Me.LabelSortie.Size = New System.Drawing.Size(116, 20)
        Me.LabelSortie.TabIndex = 4
        Me.LabelSortie.Text = "Jour de sortie :"
        '
        'DateSortie
        '
        Me.DateSortie.Format = System.Windows.Forms.DateTimePickerFormat.Long
        Me.DateSortie.Location = New System.Drawing.Point(520, 76)
        Me.DateSortie.Name = "DateSortie"
        Me.DateSortie.Size = New System.Drawing.Size(150, 27)
        Me.DateSortie.TabIndex = 5
        '
        'ButtonAjouterManuel
        '
        Me.ButtonAjouterManuel.Location = New System.Drawing.Point(420, 28)
        Me.ButtonAjouterManuel.Name = "ButtonAjouterManuel"
        Me.ButtonAjouterManuel.Size = New System.Drawing.Size(250, 34)
        Me.ButtonAjouterManuel.TabIndex = 6
        Me.ButtonAjouterManuel.Text = "Ajouter à la liste"
        Me.ButtonAjouterManuel.UseVisualStyleBackColor = True
        '
        'CheckBoxEntree
        '
        Me.CheckBoxEntree.AutoSize = True
        Me.CheckBoxEntree.Checked = True
        Me.CheckBoxEntree.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBoxEntree.Location = New System.Drawing.Point(16, 474)
        Me.CheckBoxEntree.Name = "CheckBoxEntree"
        Me.CheckBoxEntree.Size = New System.Drawing.Size(150, 24)
        Me.CheckBoxEntree.TabIndex = 3
        Me.CheckBoxEntree.Text = "Insérer l'entrée"
        Me.CheckBoxEntree.UseVisualStyleBackColor = True
        '
        'CheckBoxSortie
        '
        Me.CheckBoxSortie.AutoSize = True
        Me.CheckBoxSortie.Checked = True
        Me.CheckBoxSortie.CheckState = System.Windows.Forms.CheckState.Checked
        Me.CheckBoxSortie.Location = New System.Drawing.Point(200, 474)
        Me.CheckBoxSortie.Name = "CheckBoxSortie"
        Me.CheckBoxSortie.Size = New System.Drawing.Size(142, 24)
        Me.CheckBoxSortie.TabIndex = 4
        Me.CheckBoxSortie.Text = "Insérer la sortie"
        Me.CheckBoxSortie.UseVisualStyleBackColor = True
        '
        'ButtonVide
        '
        Me.ButtonVide.Location = New System.Drawing.Point(16, 506)
        Me.ButtonVide.Name = "ButtonVide"
        Me.ButtonVide.Size = New System.Drawing.Size(340, 36)
        Me.ButtonVide.TabIndex = 7
        Me.ButtonVide.Text = "Insérer 2 lignes à régler à la main"
        Me.ButtonVide.UseVisualStyleBackColor = True
        '
        'ButtonOK
        '
        Me.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOK.Location = New System.Drawing.Point(484, 506)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(100, 36)
        Me.ButtonOK.TabIndex = 5
        Me.ButtonOK.Text = "Insérer"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(600, 506)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(100, 36)
        Me.ButtonAnnuler.TabIndex = 6
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormYomTov
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(720, 556)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonVide)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.CheckBoxSortie)
        Me.Controls.Add(Me.CheckBoxEntree)
        Me.Controls.Add(Me.GroupBoxManuel)
        Me.Controls.Add(Me.ListViewFetes)
        Me.Controls.Add(Me.ComboVille)
        Me.Controls.Add(Me.LabelVille)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormYomTov"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Fêtes juives (Yom Tov)"
        Me.GroupBoxManuel.ResumeLayout(False)
        Me.GroupBoxManuel.PerformLayout()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAide As Label
    Friend WithEvents LabelVille As Label
    Friend WithEvents ComboVille As ComboBox
    Friend WithEvents ListViewFetes As ListView
    Friend WithEvents ColonneFete As ColumnHeader
    Friend WithEvents ColonneEntree As ColumnHeader
    Friend WithEvents ColonneSortie As ColumnHeader
    Friend WithEvents GroupBoxManuel As GroupBox
    Friend WithEvents LabelNom As Label
    Friend WithEvents TextBoxNom As TextBox
    Friend WithEvents LabelEntree As Label
    Friend WithEvents DateEntree As DateTimePicker
    Friend WithEvents LabelSortie As Label
    Friend WithEvents DateSortie As DateTimePicker
    Friend WithEvents ButtonAjouterManuel As Button
    Friend WithEvents CheckBoxEntree As CheckBox
    Friend WithEvents CheckBoxSortie As CheckBox
    Friend WithEvents ButtonVide As Button
    Friend WithEvents ButtonOK As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
