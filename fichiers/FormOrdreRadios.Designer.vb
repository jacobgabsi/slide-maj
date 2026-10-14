<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormOrdreRadios
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
        Me.ListBoxRadios = New System.Windows.Forms.ListBox()
        Me.ButtonHaut = New System.Windows.Forms.Button()
        Me.ButtonMonter = New System.Windows.Forms.Button()
        Me.ButtonDescendre = New System.Windows.Forms.Button()
        Me.ButtonBas = New System.Windows.Forms.Button()
        Me.ButtonTrier = New System.Windows.Forms.Button()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelAide.Location = New System.Drawing.Point(16, 14)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(588, 50)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Glissez une radio à sa place, ou choisissez-la puis utilisez les flèches. La touche « radio suivante » suit cet ordre."
        '
        'ListBoxRadios
        '
        Me.ListBoxRadios.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListBoxRadios.AllowDrop = True
        Me.ListBoxRadios.FormattingEnabled = True
        Me.ListBoxRadios.IntegralHeight = False
        Me.ListBoxRadios.ItemHeight = 20
        Me.ListBoxRadios.Location = New System.Drawing.Point(16, 68)
        Me.ListBoxRadios.Name = "ListBoxRadios"
        Me.ListBoxRadios.Size = New System.Drawing.Size(448, 400)
        Me.ListBoxRadios.TabIndex = 1
        '
        'ButtonHaut
        '
        Me.ButtonHaut.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonHaut.Location = New System.Drawing.Point(480, 68)
        Me.ButtonHaut.Name = "ButtonHaut"
        Me.ButtonHaut.Size = New System.Drawing.Size(124, 40)
        Me.ButtonHaut.TabIndex = 2
        Me.ButtonHaut.Text = "⤒  En haut"
        Me.ButtonHaut.UseVisualStyleBackColor = True
        '
        'ButtonMonter
        '
        Me.ButtonMonter.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonMonter.Location = New System.Drawing.Point(480, 116)
        Me.ButtonMonter.Name = "ButtonMonter"
        Me.ButtonMonter.Size = New System.Drawing.Size(124, 40)
        Me.ButtonMonter.TabIndex = 3
        Me.ButtonMonter.Text = "▲  Monter"
        Me.ButtonMonter.UseVisualStyleBackColor = True
        '
        'ButtonDescendre
        '
        Me.ButtonDescendre.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonDescendre.Location = New System.Drawing.Point(480, 164)
        Me.ButtonDescendre.Name = "ButtonDescendre"
        Me.ButtonDescendre.Size = New System.Drawing.Size(124, 40)
        Me.ButtonDescendre.TabIndex = 4
        Me.ButtonDescendre.Text = "▼  Descendre"
        Me.ButtonDescendre.UseVisualStyleBackColor = True
        '
        'ButtonBas
        '
        Me.ButtonBas.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonBas.Location = New System.Drawing.Point(480, 212)
        Me.ButtonBas.Name = "ButtonBas"
        Me.ButtonBas.Size = New System.Drawing.Size(124, 40)
        Me.ButtonBas.TabIndex = 5
        Me.ButtonBas.Text = "⤓  En bas"
        Me.ButtonBas.UseVisualStyleBackColor = True
        '
        'ButtonTrier
        '
        Me.ButtonTrier.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonTrier.Location = New System.Drawing.Point(480, 276)
        Me.ButtonTrier.Name = "ButtonTrier"
        Me.ButtonTrier.Size = New System.Drawing.Size(124, 40)
        Me.ButtonTrier.TabIndex = 6
        Me.ButtonTrier.Text = "Trier A → Z"
        Me.ButtonTrier.UseVisualStyleBackColor = True
        '
        'ButtonOK
        '
        Me.ButtonOK.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonOK.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOK.Location = New System.Drawing.Point(386, 484)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(120, 40)
        Me.ButtonOK.TabIndex = 7
        Me.ButtonOK.Text = "Enregistrer"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(514, 484)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(90, 40)
        Me.ButtonAnnuler.TabIndex = 8
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormOrdreRadios
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(620, 540)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.ButtonTrier)
        Me.Controls.Add(Me.ButtonBas)
        Me.Controls.Add(Me.ButtonDescendre)
        Me.Controls.Add(Me.ButtonMonter)
        Me.Controls.Add(Me.ButtonHaut)
        Me.Controls.Add(Me.ListBoxRadios)
        Me.Controls.Add(Me.LabelAide)
        Me.MinimumSize = New System.Drawing.Size(560, 400)
        Me.Name = "FormOrdreRadios"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Ordre des radios"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelAide As Label
    Friend WithEvents ListBoxRadios As ListBox
    Friend WithEvents ButtonHaut As Button
    Friend WithEvents ButtonMonter As Button
    Friend WithEvents ButtonDescendre As Button
    Friend WithEvents ButtonBas As Button
    Friend WithEvents ButtonTrier As Button
    Friend WithEvents ButtonOK As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
