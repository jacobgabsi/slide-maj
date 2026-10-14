<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormAideTuya
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
        Me.RichTextBoxAide = New System.Windows.Forms.RichTextBox()
        Me.ButtonPlateforme = New System.Windows.Forms.Button()
        Me.ButtonFermer = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'RichTextBoxAide
        '
        Me.RichTextBoxAide.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.RichTextBoxAide.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.RichTextBoxAide.Location = New System.Drawing.Point(16, 16)
        Me.RichTextBoxAide.Name = "RichTextBoxAide"
        Me.RichTextBoxAide.ReadOnly = True
        Me.RichTextBoxAide.Size = New System.Drawing.Size(948, 620)
        Me.RichTextBoxAide.TabIndex = 0
        Me.RichTextBoxAide.Text = ""
        '
        'ButtonPlateforme
        '
        Me.ButtonPlateforme.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonPlateforme.Location = New System.Drawing.Point(16, 650)
        Me.ButtonPlateforme.Name = "ButtonPlateforme"
        Me.ButtonPlateforme.Size = New System.Drawing.Size(360, 42)
        Me.ButtonPlateforme.TabIndex = 1
        Me.ButtonPlateforme.Text = "Ouvrir la plateforme Tuya IoT (iot.tuya.com)"
        Me.ButtonPlateforme.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonFermer.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonFermer.Location = New System.Drawing.Point(824, 650)
        Me.ButtonFermer.Name = "ButtonFermer"
        Me.ButtonFermer.Size = New System.Drawing.Size(140, 42)
        Me.ButtonFermer.TabIndex = 2
        Me.ButtonFermer.Text = "Fermer"
        Me.ButtonFermer.UseVisualStyleBackColor = True
        '
        'FormAideTuya
        '
        Me.AcceptButton = Me.ButtonFermer
        Me.AutoScaleDimensions = New System.Drawing.SizeF(11.0!, 25.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonFermer
        Me.ClientSize = New System.Drawing.Size(980, 708)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.ButtonPlateforme)
        Me.Controls.Add(Me.RichTextBoxAide)
        Me.MinimizeBox = False
        Me.Name = "FormAideTuya"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Aide Tuya : brancher Slide sur vos appareils Smart Life"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents RichTextBoxAide As System.Windows.Forms.RichTextBox
    Friend WithEvents ButtonPlateforme As System.Windows.Forms.Button
    Friend WithEvents ButtonFermer As System.Windows.Forms.Button
End Class
