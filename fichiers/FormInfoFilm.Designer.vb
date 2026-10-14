<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormInfoFilm
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
        Me.LabelTitre = New System.Windows.Forms.Label()
        Me.PictureBoxAffiche = New System.Windows.Forms.PictureBox()
        Me.TextBoxInfos = New System.Windows.Forms.TextBox()
        Me.ButtonBandeAnnonce = New System.Windows.Forms.Button()
        Me.ButtonFermer = New System.Windows.Forms.Button()
        CType(Me.PictureBoxAffiche, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'LabelTitre
        '
        Me.LabelTitre.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 14.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTitre.Location = New System.Drawing.Point(16, 12)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(968, 40)
        Me.LabelTitre.TabIndex = 0
        Me.LabelTitre.Text = "Titre"
        Me.LabelTitre.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'PictureBoxAffiche
        '
        Me.PictureBoxAffiche.Location = New System.Drawing.Point(16, 60)
        Me.PictureBoxAffiche.Name = "PictureBoxAffiche"
        Me.PictureBoxAffiche.Size = New System.Drawing.Size(300, 450)
        Me.PictureBoxAffiche.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.PictureBoxAffiche.TabIndex = 1
        Me.PictureBoxAffiche.TabStop = False
        '
        'TextBoxInfos
        '
        Me.TextBoxInfos.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBoxInfos.Location = New System.Drawing.Point(332, 60)
        Me.TextBoxInfos.Multiline = True
        Me.TextBoxInfos.Name = "TextBoxInfos"
        Me.TextBoxInfos.ReadOnly = True
        Me.TextBoxInfos.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.TextBoxInfos.Size = New System.Drawing.Size(652, 450)
        Me.TextBoxInfos.TabIndex = 2
        '
        'ButtonBandeAnnonce
        '
        Me.ButtonBandeAnnonce.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonBandeAnnonce.Location = New System.Drawing.Point(332, 524)
        Me.ButtonBandeAnnonce.Name = "ButtonBandeAnnonce"
        Me.ButtonBandeAnnonce.Size = New System.Drawing.Size(220, 40)
        Me.ButtonBandeAnnonce.TabIndex = 3
        Me.ButtonBandeAnnonce.Text = "Bande-annonce"
        Me.ButtonBandeAnnonce.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonFermer.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonFermer.Location = New System.Drawing.Point(864, 524)
        Me.ButtonFermer.Name = "ButtonFermer"
        Me.ButtonFermer.Size = New System.Drawing.Size(120, 40)
        Me.ButtonFermer.TabIndex = 4
        Me.ButtonFermer.Text = "Fermer"
        Me.ButtonFermer.UseVisualStyleBackColor = True
        '
        'FormInfoFilm
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonFermer
        Me.ClientSize = New System.Drawing.Size(1000, 580)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.ButtonBandeAnnonce)
        Me.Controls.Add(Me.TextBoxInfos)
        Me.Controls.Add(Me.PictureBoxAffiche)
        Me.Controls.Add(Me.LabelTitre)
        Me.MinimizeBox = False
        Me.Name = "FormInfoFilm"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Plus d'infos"
        CType(Me.PictureBoxAffiche, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelTitre As Label
    Friend WithEvents PictureBoxAffiche As PictureBox
    Friend WithEvents TextBoxInfos As TextBox
    Friend WithEvents ButtonBandeAnnonce As Button
    Friend WithEvents ButtonFermer As Button
End Class
