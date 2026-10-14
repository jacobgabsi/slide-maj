<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSuggestionsRadio
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
        Me.LabelPays = New System.Windows.Forms.Label()
        Me.ComboBoxPays = New System.Windows.Forms.ComboBox()
        Me.LabelStyle = New System.Windows.Forms.Label()
        Me.ComboBoxStyle = New System.Windows.Forms.ComboBox()
        Me.LabelNom = New System.Windows.Forms.Label()
        Me.TextBoxNom = New System.Windows.Forms.TextBox()
        Me.ButtonChercher = New System.Windows.Forms.Button()
        Me.ListViewRadios = New System.Windows.Forms.ListView()
        Me.ColonneRadio = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColonneFormat = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColonnePays = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.ColonneEtat = CType(New System.Windows.Forms.ColumnHeader(), System.Windows.Forms.ColumnHeader)
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.ButtonEcouter = New System.Windows.Forms.Button()
        Me.ButtonAjouter = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelPays
        '
        Me.LabelPays.Location = New System.Drawing.Point(16, 18)
        Me.LabelPays.Name = "LabelPays"
        Me.LabelPays.Size = New System.Drawing.Size(60, 28)
        Me.LabelPays.TabIndex = 0
        Me.LabelPays.Text = "Pays :"
        Me.LabelPays.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'ComboBoxPays
        '
        Me.ComboBoxPays.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxPays.FormattingEnabled = True
        Me.ComboBoxPays.Location = New System.Drawing.Point(80, 16)
        Me.ComboBoxPays.Name = "ComboBoxPays"
        Me.ComboBoxPays.Size = New System.Drawing.Size(170, 28)
        Me.ComboBoxPays.TabIndex = 1
        '
        'LabelStyle
        '
        Me.LabelStyle.Location = New System.Drawing.Point(266, 18)
        Me.LabelStyle.Name = "LabelStyle"
        Me.LabelStyle.Size = New System.Drawing.Size(60, 28)
        Me.LabelStyle.TabIndex = 2
        Me.LabelStyle.Text = "Style :"
        Me.LabelStyle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'ComboBoxStyle
        '
        Me.ComboBoxStyle.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxStyle.FormattingEnabled = True
        Me.ComboBoxStyle.Location = New System.Drawing.Point(330, 16)
        Me.ComboBoxStyle.Name = "ComboBoxStyle"
        Me.ComboBoxStyle.Size = New System.Drawing.Size(210, 28)
        Me.ComboBoxStyle.TabIndex = 3
        '
        'LabelNom
        '
        Me.LabelNom.Location = New System.Drawing.Point(556, 18)
        Me.LabelNom.Name = "LabelNom"
        Me.LabelNom.Size = New System.Drawing.Size(60, 28)
        Me.LabelNom.TabIndex = 4
        Me.LabelNom.Text = "Nom :"
        Me.LabelNom.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'TextBoxNom
        '
        Me.TextBoxNom.Location = New System.Drawing.Point(620, 16)
        Me.TextBoxNom.Name = "TextBoxNom"
        Me.TextBoxNom.Size = New System.Drawing.Size(200, 27)
        Me.TextBoxNom.TabIndex = 5
        '
        'ButtonChercher
        '
        Me.ButtonChercher.Location = New System.Drawing.Point(834, 12)
        Me.ButtonChercher.Name = "ButtonChercher"
        Me.ButtonChercher.Size = New System.Drawing.Size(120, 36)
        Me.ButtonChercher.TabIndex = 6
        Me.ButtonChercher.Text = "Chercher"
        Me.ButtonChercher.UseVisualStyleBackColor = True
        '
        'ListViewRadios
        '
        Me.ListViewRadios.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListViewRadios.CheckBoxes = True
        Me.ListViewRadios.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColonneRadio, Me.ColonneFormat, Me.ColonnePays, Me.ColonneEtat})
        Me.ListViewRadios.FullRowSelect = True
        Me.ListViewRadios.HideSelection = False
        Me.ListViewRadios.Location = New System.Drawing.Point(16, 60)
        Me.ListViewRadios.MultiSelect = False
        Me.ListViewRadios.Name = "ListViewRadios"
        Me.ListViewRadios.Size = New System.Drawing.Size(938, 380)
        Me.ListViewRadios.TabIndex = 7
        Me.ListViewRadios.UseCompatibleStateImageBehavior = False
        Me.ListViewRadios.View = System.Windows.Forms.View.Details
        '
        'ColonneRadio
        '
        Me.ColonneRadio.Text = "Radio"
        Me.ColonneRadio.Width = 430
        '
        'ColonneFormat
        '
        Me.ColonneFormat.Text = "Format"
        Me.ColonneFormat.Width = 150
        '
        'ColonnePays
        '
        Me.ColonnePays.Text = "Pays"
        Me.ColonnePays.Width = 70
        '
        'ColonneEtat
        '
        Me.ColonneEtat.Text = ""
        Me.ColonneEtat.Width = 240
        '
        'LabelEtat
        '
        Me.LabelEtat.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelEtat.Location = New System.Drawing.Point(16, 450)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(938, 28)
        Me.LabelEtat.TabIndex = 8
        '
        'ButtonEcouter
        '
        Me.ButtonEcouter.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonEcouter.Location = New System.Drawing.Point(16, 486)
        Me.ButtonEcouter.Name = "ButtonEcouter"
        Me.ButtonEcouter.Size = New System.Drawing.Size(230, 40)
        Me.ButtonEcouter.TabIndex = 9
        Me.ButtonEcouter.Text = "Écouter la radio choisie"
        Me.ButtonEcouter.UseVisualStyleBackColor = True
        '
        'ButtonAjouter
        '
        Me.ButtonAjouter.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonAjouter.Enabled = False
        Me.ButtonAjouter.Location = New System.Drawing.Point(654, 486)
        Me.ButtonAjouter.Name = "ButtonAjouter"
        Me.ButtonAjouter.Size = New System.Drawing.Size(190, 40)
        Me.ButtonAjouter.TabIndex = 10
        Me.ButtonAjouter.Text = "Ajouter les cochées"
        Me.ButtonAjouter.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(854, 486)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(100, 40)
        Me.ButtonAnnuler.TabIndex = 11
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormSuggestionsRadio
        '
        Me.AcceptButton = Me.ButtonChercher
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(970, 540)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonAjouter)
        Me.Controls.Add(Me.ButtonEcouter)
        Me.Controls.Add(Me.LabelEtat)
        Me.Controls.Add(Me.ListViewRadios)
        Me.Controls.Add(Me.ButtonChercher)
        Me.Controls.Add(Me.TextBoxNom)
        Me.Controls.Add(Me.LabelNom)
        Me.Controls.Add(Me.ComboBoxStyle)
        Me.Controls.Add(Me.LabelStyle)
        Me.Controls.Add(Me.ComboBoxPays)
        Me.Controls.Add(Me.LabelPays)
        Me.MinimumSize = New System.Drawing.Size(800, 420)
        Me.Name = "FormSuggestionsRadio"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Radios sur Internet"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelPays As Label
    Friend WithEvents ComboBoxPays As ComboBox
    Friend WithEvents LabelStyle As Label
    Friend WithEvents ComboBoxStyle As ComboBox
    Friend WithEvents LabelNom As Label
    Friend WithEvents TextBoxNom As TextBox
    Friend WithEvents ButtonChercher As Button
    Friend WithEvents ListViewRadios As ListView
    Friend WithEvents ColonneRadio As ColumnHeader
    Friend WithEvents ColonneFormat As ColumnHeader
    Friend WithEvents ColonnePays As ColumnHeader
    Friend WithEvents ColonneEtat As ColumnHeader
    Friend WithEvents LabelEtat As Label
    Friend WithEvents ButtonEcouter As Button
    Friend WithEvents ButtonAjouter As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
