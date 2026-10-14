<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormChoixHasard
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
        Me.LabelListe1 = New System.Windows.Forms.Label()
        Me.CheckedListBox1 = New System.Windows.Forms.CheckedListBox()
        Me.ButtonTout1 = New System.Windows.Forms.Button()
        Me.ButtonRien1 = New System.Windows.Forms.Button()
        Me.LabelListe2 = New System.Windows.Forms.Label()
        Me.CheckedListBox2 = New System.Windows.Forms.CheckedListBox()
        Me.ButtonTout2 = New System.Windows.Forms.Button()
        Me.ButtonRien2 = New System.Windows.Forms.Button()
        Me.ButtonOK = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(20, 14)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(640, 46)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Animations tirées au sort quand la liste est sur « Au hasard »." & Global.Microsoft.VisualBasic.ChrW(13) & Global.Microsoft.VisualBasic.ChrW(10) & "Décochez celles dont vous ne voulez pas."
        '
        'LabelListe1
        '
        Me.LabelListe1.AutoSize = True
        Me.LabelListe1.Location = New System.Drawing.Point(20, 72)
        Me.LabelListe1.Name = "LabelListe1"
        Me.LabelListe1.Size = New System.Drawing.Size(90, 20)
        Me.LabelListe1.TabIndex = 1
        Me.LabelListe1.Text = "Arrivées :"
        '
        'CheckedListBox1
        '
        Me.CheckedListBox1.CheckOnClick = True
        Me.CheckedListBox1.FormattingEnabled = True
        Me.CheckedListBox1.IntegralHeight = False
        Me.CheckedListBox1.Location = New System.Drawing.Point(20, 98)
        Me.CheckedListBox1.Name = "CheckedListBox1"
        Me.CheckedListBox1.Size = New System.Drawing.Size(310, 380)
        Me.CheckedListBox1.TabIndex = 2
        '
        'ButtonTout1
        '
        Me.ButtonTout1.Location = New System.Drawing.Point(20, 486)
        Me.ButtonTout1.Name = "ButtonTout1"
        Me.ButtonTout1.Size = New System.Drawing.Size(150, 32)
        Me.ButtonTout1.TabIndex = 3
        Me.ButtonTout1.Text = "Tout cocher"
        Me.ButtonTout1.UseVisualStyleBackColor = True
        '
        'ButtonRien1
        '
        Me.ButtonRien1.Location = New System.Drawing.Point(180, 486)
        Me.ButtonRien1.Name = "ButtonRien1"
        Me.ButtonRien1.Size = New System.Drawing.Size(150, 32)
        Me.ButtonRien1.TabIndex = 4
        Me.ButtonRien1.Text = "Tout décocher"
        Me.ButtonRien1.UseVisualStyleBackColor = True
        '
        'LabelListe2
        '
        Me.LabelListe2.AutoSize = True
        Me.LabelListe2.Location = New System.Drawing.Point(350, 72)
        Me.LabelListe2.Name = "LabelListe2"
        Me.LabelListe2.Size = New System.Drawing.Size(120, 20)
        Me.LabelListe2.TabIndex = 5
        Me.LabelListe2.Text = "Mouvements :"
        '
        'CheckedListBox2
        '
        Me.CheckedListBox2.CheckOnClick = True
        Me.CheckedListBox2.FormattingEnabled = True
        Me.CheckedListBox2.IntegralHeight = False
        Me.CheckedListBox2.Location = New System.Drawing.Point(350, 98)
        Me.CheckedListBox2.Name = "CheckedListBox2"
        Me.CheckedListBox2.Size = New System.Drawing.Size(310, 380)
        Me.CheckedListBox2.TabIndex = 6
        '
        'ButtonTout2
        '
        Me.ButtonTout2.Location = New System.Drawing.Point(350, 486)
        Me.ButtonTout2.Name = "ButtonTout2"
        Me.ButtonTout2.Size = New System.Drawing.Size(150, 32)
        Me.ButtonTout2.TabIndex = 7
        Me.ButtonTout2.Text = "Tout cocher"
        Me.ButtonTout2.UseVisualStyleBackColor = True
        '
        'ButtonRien2
        '
        Me.ButtonRien2.Location = New System.Drawing.Point(510, 486)
        Me.ButtonRien2.Name = "ButtonRien2"
        Me.ButtonRien2.Size = New System.Drawing.Size(150, 32)
        Me.ButtonRien2.TabIndex = 8
        Me.ButtonRien2.Text = "Tout décocher"
        Me.ButtonRien2.UseVisualStyleBackColor = True
        '
        'ButtonOK
        '
        Me.ButtonOK.Location = New System.Drawing.Point(350, 534)
        Me.ButtonOK.Name = "ButtonOK"
        Me.ButtonOK.Size = New System.Drawing.Size(150, 35)
        Me.ButtonOK.TabIndex = 9
        Me.ButtonOK.Text = "OK"
        Me.ButtonOK.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(510, 534)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(150, 35)
        Me.ButtonAnnuler.TabIndex = 10
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormChoixHasard
        '
        Me.AcceptButton = Me.ButtonOK
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(680, 586)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOK)
        Me.Controls.Add(Me.ButtonRien2)
        Me.Controls.Add(Me.ButtonTout2)
        Me.Controls.Add(Me.CheckedListBox2)
        Me.Controls.Add(Me.LabelListe2)
        Me.Controls.Add(Me.ButtonRien1)
        Me.Controls.Add(Me.ButtonTout1)
        Me.Controls.Add(Me.CheckedListBox1)
        Me.Controls.Add(Me.LabelListe1)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormChoixHasard"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Animations du hasard"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAide As Label
    Friend WithEvents LabelListe1 As Label
    Friend WithEvents CheckedListBox1 As CheckedListBox
    Friend WithEvents ButtonTout1 As Button
    Friend WithEvents ButtonRien1 As Button
    Friend WithEvents LabelListe2 As Label
    Friend WithEvents CheckedListBox2 As CheckedListBox
    Friend WithEvents ButtonTout2 As Button
    Friend WithEvents ButtonRien2 As Button
    Friend WithEvents ButtonOK As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
