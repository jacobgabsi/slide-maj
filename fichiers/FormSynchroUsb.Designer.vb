<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormSynchroUsb
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
        Me.LabelSource = New System.Windows.Forms.Label()
        Me.LabelCle = New System.Windows.Forms.Label()
        Me.ComboBoxCles = New System.Windows.Forms.ComboBox()
        Me.ButtonRelire = New System.Windows.Forms.Button()
        Me.LabelDestination = New System.Windows.Forms.Label()
        Me.TextBoxJournal = New System.Windows.Forms.TextBox()
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.ButtonSynchroniser = New System.Windows.Forms.Button()
        Me.ButtonArreter = New System.Windows.Forms.Button()
        Me.ButtonFermer = New System.Windows.Forms.Button()
        Me.RadioButtonVersCle = New System.Windows.Forms.RadioButton()
        Me.RadioButtonDepuisCle = New System.Windows.Forms.RadioButton()
        Me.CheckBoxReglages = New System.Windows.Forms.CheckBox()
        Me.SuspendLayout()
        '
        'LabelSource
        '
        Me.LabelSource.AutoSize = True
        Me.LabelSource.Location = New System.Drawing.Point(16, 18)
        Me.LabelSource.Name = "LabelSource"
        Me.LabelSource.Size = New System.Drawing.Size(60, 20)
        Me.LabelSource.TabIndex = 0
        Me.LabelSource.Text = "Dossier"
        '
        'LabelCle
        '
        Me.LabelCle.AutoSize = True
        Me.LabelCle.Location = New System.Drawing.Point(16, 54)
        Me.LabelCle.Name = "LabelCle"
        Me.LabelCle.Size = New System.Drawing.Size(30, 20)
        Me.LabelCle.TabIndex = 1
        Me.LabelCle.Text = "Clé"
        '
        'ComboBoxCles
        '
        Me.ComboBoxCles.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxCles.Location = New System.Drawing.Point(120, 50)
        Me.ComboBoxCles.Name = "ComboBoxCles"
        Me.ComboBoxCles.Size = New System.Drawing.Size(460, 28)
        Me.ComboBoxCles.TabIndex = 2
        '
        'ButtonRelire
        '
        Me.ButtonRelire.Location = New System.Drawing.Point(592, 49)
        Me.ButtonRelire.Name = "ButtonRelire"
        Me.ButtonRelire.Size = New System.Drawing.Size(120, 30)
        Me.ButtonRelire.TabIndex = 3
        Me.ButtonRelire.Text = "Relire les clés"
        Me.ButtonRelire.UseVisualStyleBackColor = True
        '
        'LabelDestination
        '
        Me.LabelDestination.AutoSize = True
        Me.LabelDestination.Location = New System.Drawing.Point(16, 90)
        Me.LabelDestination.Name = "LabelDestination"
        Me.LabelDestination.Size = New System.Drawing.Size(90, 20)
        Me.LabelDestination.TabIndex = 4
        Me.LabelDestination.Text = "Destination"
        '
        'TextBoxJournal
        '
        Me.TextBoxJournal.BackColor = System.Drawing.Color.White
        Me.TextBoxJournal.Font = New System.Drawing.Font("Consolas", 9.0!)
        Me.TextBoxJournal.Location = New System.Drawing.Point(16, 162)
        Me.TextBoxJournal.Multiline = True
        Me.TextBoxJournal.Name = "TextBoxJournal"
        Me.TextBoxJournal.ReadOnly = True
        Me.TextBoxJournal.ScrollBars = System.Windows.Forms.ScrollBars.Both
        Me.TextBoxJournal.Size = New System.Drawing.Size(696, 322)
        Me.TextBoxJournal.TabIndex = 5
        Me.TextBoxJournal.WordWrap = False
        '
        'RadioButtonVersCle
        '
        Me.RadioButtonVersCle.AutoSize = True
        Me.RadioButtonVersCle.Checked = True
        Me.RadioButtonVersCle.Location = New System.Drawing.Point(16, 124)
        Me.RadioButtonVersCle.Name = "RadioButtonVersCle"
        Me.RadioButtonVersCle.Size = New System.Drawing.Size(150, 24)
        Me.RadioButtonVersCle.TabIndex = 10
        Me.RadioButtonVersCle.TabStop = True
        Me.RadioButtonVersCle.Text = "PC  ->  clé"
        Me.RadioButtonVersCle.UseVisualStyleBackColor = True
        '
        'RadioButtonDepuisCle
        '
        Me.RadioButtonDepuisCle.AutoSize = True
        Me.RadioButtonDepuisCle.Location = New System.Drawing.Point(180, 124)
        Me.RadioButtonDepuisCle.Name = "RadioButtonDepuisCle"
        Me.RadioButtonDepuisCle.Size = New System.Drawing.Size(170, 24)
        Me.RadioButtonDepuisCle.TabIndex = 11
        Me.RadioButtonDepuisCle.Text = "clé  ->  ce PC (récupérer)"
        Me.RadioButtonDepuisCle.UseVisualStyleBackColor = True
        '
        'CheckBoxReglages
        '
        Me.CheckBoxReglages.AutoSize = True
        Me.CheckBoxReglages.Location = New System.Drawing.Point(400, 124)
        Me.CheckBoxReglages.Name = "CheckBoxReglages"
        Me.CheckBoxReglages.Size = New System.Drawing.Size(300, 24)
        Me.CheckBoxReglages.TabIndex = 12
        Me.CheckBoxReglages.Text = "Avec les réglages du poste (data\settings)"
        Me.CheckBoxReglages.UseVisualStyleBackColor = True
        '
        'LabelEtat
        '
        Me.LabelEtat.AutoSize = True
        Me.LabelEtat.Location = New System.Drawing.Point(16, 496)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(0, 20)
        Me.LabelEtat.TabIndex = 6
        '
        'ButtonSynchroniser
        '
        Me.ButtonSynchroniser.Location = New System.Drawing.Point(16, 530)
        Me.ButtonSynchroniser.Name = "ButtonSynchroniser"
        Me.ButtonSynchroniser.Size = New System.Drawing.Size(160, 38)
        Me.ButtonSynchroniser.TabIndex = 7
        Me.ButtonSynchroniser.Text = "Synchroniser"
        Me.ButtonSynchroniser.UseVisualStyleBackColor = True
        '
        'ButtonArreter
        '
        Me.ButtonArreter.Enabled = False
        Me.ButtonArreter.Location = New System.Drawing.Point(192, 530)
        Me.ButtonArreter.Name = "ButtonArreter"
        Me.ButtonArreter.Size = New System.Drawing.Size(120, 38)
        Me.ButtonArreter.TabIndex = 8
        Me.ButtonArreter.Text = "Arrêter"
        Me.ButtonArreter.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.Location = New System.Drawing.Point(592, 530)
        Me.ButtonFermer.Name = "ButtonFermer"
        Me.ButtonFermer.Size = New System.Drawing.Size(120, 38)
        Me.ButtonFermer.TabIndex = 9
        Me.ButtonFermer.Text = "Fermer"
        Me.ButtonFermer.UseVisualStyleBackColor = True
        '
        'FormSynchroUsb
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(728, 584)
        Me.Controls.Add(Me.CheckBoxReglages)
        Me.Controls.Add(Me.RadioButtonDepuisCle)
        Me.Controls.Add(Me.RadioButtonVersCle)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.ButtonArreter)
        Me.Controls.Add(Me.ButtonSynchroniser)
        Me.Controls.Add(Me.LabelEtat)
        Me.Controls.Add(Me.TextBoxJournal)
        Me.Controls.Add(Me.LabelDestination)
        Me.Controls.Add(Me.ButtonRelire)
        Me.Controls.Add(Me.ComboBoxCles)
        Me.Controls.Add(Me.LabelCle)
        Me.Controls.Add(Me.LabelSource)
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormSynchroUsb"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Synchroniser sur la clé USB"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelSource As Label
    Friend WithEvents LabelCle As Label
    Friend WithEvents ComboBoxCles As ComboBox
    Friend WithEvents ButtonRelire As Button
    Friend WithEvents LabelDestination As Label
    Friend WithEvents TextBoxJournal As TextBox
    Friend WithEvents LabelEtat As Label
    Friend WithEvents ButtonSynchroniser As Button
    Friend WithEvents ButtonArreter As Button
    Friend WithEvents ButtonFermer As Button
    Friend WithEvents RadioButtonVersCle As RadioButton
    Friend WithEvents RadioButtonDepuisCle As RadioButton
    Friend WithEvents CheckBoxReglages As CheckBox
End Class
