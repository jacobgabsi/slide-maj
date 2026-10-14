<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormCoherence
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
        Me.PictureBoxIcone = New System.Windows.Forms.PictureBox()
        Me.LabelTitre = New System.Windows.Forms.Label()
        Me.LabelSousTitre = New System.Windows.Forms.Label()
        Me.ListBoxProblemes = New System.Windows.Forms.ListBox()
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.ButtonEnregistrer = New System.Windows.Forms.Button()
        Me.ButtonCorriger = New System.Windows.Forms.Button()
        CType(Me.PictureBoxIcone, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'PictureBoxIcone
        '
        Me.PictureBoxIcone.BackColor = System.Drawing.Color.Transparent
        Me.PictureBoxIcone.Location = New System.Drawing.Point(24, 22)
        Me.PictureBoxIcone.Name = "PictureBoxIcone"
        Me.PictureBoxIcone.Size = New System.Drawing.Size(44, 44)
        Me.PictureBoxIcone.TabIndex = 0
        Me.PictureBoxIcone.TabStop = False
        '
        'LabelTitre
        '
        Me.LabelTitre.AutoSize = True
        Me.LabelTitre.BackColor = System.Drawing.Color.Transparent
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 14.25!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point)
        Me.LabelTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(247, Byte), Integer))
        Me.LabelTitre.Location = New System.Drawing.Point(80, 19)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(243, 25)
        Me.LabelTitre.TabIndex = 1
        Me.LabelTitre.Text = "Minuterie : incohérences"
        '
        'LabelSousTitre
        '
        Me.LabelSousTitre.AutoSize = True
        Me.LabelSousTitre.BackColor = System.Drawing.Color.Transparent
        Me.LabelSousTitre.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.LabelSousTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelSousTitre.Location = New System.Drawing.Point(82, 48)
        Me.LabelSousTitre.Name = "LabelSousTitre"
        Me.LabelSousTitre.Size = New System.Drawing.Size(300, 17)
        Me.LabelSousTitre.TabIndex = 2
        Me.LabelSousTitre.Text = "À vérifier avant d'enregistrer les réglages."
        '
        'ListBoxProblemes
        '
        Me.ListBoxProblemes.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ListBoxProblemes.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(28, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.ListBoxProblemes.BorderStyle = System.Windows.Forms.BorderStyle.None
        Me.ListBoxProblemes.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable
        Me.ListBoxProblemes.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.ListBoxProblemes.ForeColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(247, Byte), Integer))
        Me.ListBoxProblemes.FormattingEnabled = True
        Me.ListBoxProblemes.IntegralHeight = False
        Me.ListBoxProblemes.Location = New System.Drawing.Point(24, 86)
        Me.ListBoxProblemes.Name = "ListBoxProblemes"
        Me.ListBoxProblemes.Size = New System.Drawing.Size(592, 250)
        Me.ListBoxProblemes.TabIndex = 0
        '
        'LabelAide
        '
        Me.LabelAide.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.LabelAide.AutoSize = True
        Me.LabelAide.BackColor = System.Drawing.Color.Transparent
        Me.LabelAide.Font = New System.Drawing.Font("Segoe UI", 9.0!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.LabelAide.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelAide.Location = New System.Drawing.Point(24, 366)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(230, 15)
        Me.LabelAide.TabIndex = 3
        Me.LabelAide.Text = "Double-clic : aller aux lignes à corriger"
        '
        'ButtonEnregistrer
        '
        Me.ButtonEnregistrer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonEnregistrer.BackColor = System.Drawing.Color.FromArgb(CType(CType(58, Byte), Integer), CType(CType(58, Byte), Integer), CType(CType(62, Byte), Integer))
        Me.ButtonEnregistrer.FlatAppearance.BorderSize = 0
        Me.ButtonEnregistrer.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(CType(CType(84, Byte), Integer), CType(CType(84, Byte), Integer), CType(CType(90, Byte), Integer))
        Me.ButtonEnregistrer.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(74, Byte), Integer), CType(CType(74, Byte), Integer), CType(CType(80, Byte), Integer))
        Me.ButtonEnregistrer.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonEnregistrer.Font = New System.Drawing.Font("Segoe UI", 9.75!, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point)
        Me.ButtonEnregistrer.ForeColor = System.Drawing.Color.FromArgb(CType(CType(245, Byte), Integer), CType(CType(245, Byte), Integer), CType(CType(247, Byte), Integer))
        Me.ButtonEnregistrer.Location = New System.Drawing.Point(296, 356)
        Me.ButtonEnregistrer.Name = "ButtonEnregistrer"
        Me.ButtonEnregistrer.Size = New System.Drawing.Size(186, 36)
        Me.ButtonEnregistrer.TabIndex = 2
        Me.ButtonEnregistrer.Text = "&Enregistrer quand même"
        Me.ButtonEnregistrer.UseVisualStyleBackColor = False
        '
        'ButtonCorriger
        '
        Me.ButtonCorriger.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonCorriger.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(132, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.ButtonCorriger.FlatAppearance.BorderSize = 0
        Me.ButtonCorriger.FlatAppearance.MouseDownBackColor = System.Drawing.Color.FromArgb(CType(CType(0, Byte), Integer), CType(CType(104, Byte), Integer), CType(CType(214, Byte), Integer))
        Me.ButtonCorriger.FlatAppearance.MouseOverBackColor = System.Drawing.Color.FromArgb(CType(CType(50, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.ButtonCorriger.FlatStyle = System.Windows.Forms.FlatStyle.Flat
        Me.ButtonCorriger.Font = New System.Drawing.Font("Segoe UI Semibold", 9.75!, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point)
        Me.ButtonCorriger.ForeColor = System.Drawing.Color.White
        Me.ButtonCorriger.Location = New System.Drawing.Point(492, 356)
        Me.ButtonCorriger.Name = "ButtonCorriger"
        Me.ButtonCorriger.Size = New System.Drawing.Size(124, 36)
        Me.ButtonCorriger.TabIndex = 1
        Me.ButtonCorriger.Text = "&Corriger"
        Me.ButtonCorriger.UseVisualStyleBackColor = False
        '
        'FormCoherence
        '
        Me.AcceptButton = Me.ButtonCorriger
        Me.AutoScaleDimensions = New System.Drawing.SizeF(96.0!, 96.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi
        Me.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(28, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.CancelButton = Me.ButtonCorriger
        Me.ClientSize = New System.Drawing.Size(640, 414)
        Me.Controls.Add(Me.ButtonCorriger)
        Me.Controls.Add(Me.ButtonEnregistrer)
        Me.Controls.Add(Me.LabelAide)
        Me.Controls.Add(Me.ListBoxProblemes)
        Me.Controls.Add(Me.LabelSousTitre)
        Me.Controls.Add(Me.LabelTitre)
        Me.Controls.Add(Me.PictureBoxIcone)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None
        Me.KeyPreview = True
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormCoherence"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Minuterie"
        CType(Me.PictureBoxIcone, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents PictureBoxIcone As PictureBox
    Friend WithEvents LabelTitre As Label
    Friend WithEvents LabelSousTitre As Label
    Friend WithEvents ListBoxProblemes As ListBox
    Friend WithEvents LabelAide As Label
    Friend WithEvents ButtonEnregistrer As Button
    Friend WithEvents ButtonCorriger As Button
End Class
