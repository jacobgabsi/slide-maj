<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormDeezer
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
        Me.WebView21 = New Microsoft.Web.WebView2.WinForms.WebView2()
        Me.PanelBas = New System.Windows.Forms.Panel()
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.ButtonRetenir = New System.Windows.Forms.Button()
        Me.ButtonAccueil = New System.Windows.Forms.Button()
        Me.ButtonMasquer = New System.Windows.Forms.Button()
        Me.ButtonCompte = New System.Windows.Forms.Button()
        CType(Me.WebView21, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.PanelBas.SuspendLayout()
        Me.SuspendLayout()
        '
        'WebView21
        '
        Me.WebView21.AllowExternalDrop = False
        Me.WebView21.CreationProperties = Nothing
        Me.WebView21.DefaultBackgroundColor = System.Drawing.Color.FromArgb(CType(CType(18, Byte), Integer), CType(CType(18, Byte), Integer), CType(CType(22, Byte), Integer))
        Me.WebView21.Dock = System.Windows.Forms.DockStyle.Fill
        Me.WebView21.Location = New System.Drawing.Point(0, 0)
        Me.WebView21.Name = "WebView21"
        Me.WebView21.Size = New System.Drawing.Size(1280, 772)
        Me.WebView21.TabIndex = 0
        Me.WebView21.ZoomFactor = 1.0R
        '
        'PanelBas
        '
        Me.PanelBas.BackColor = System.Drawing.Color.FromArgb(CType(CType(28, Byte), Integer), CType(CType(28, Byte), Integer), CType(CType(30, Byte), Integer))
        Me.PanelBas.Controls.Add(Me.LabelEtat)
        Me.PanelBas.Controls.Add(Me.ButtonRetenir)
        Me.PanelBas.Controls.Add(Me.ButtonAccueil)
        Me.PanelBas.Controls.Add(Me.ButtonMasquer)
        Me.PanelBas.Controls.Add(Me.ButtonCompte)
        Me.PanelBas.Dock = System.Windows.Forms.DockStyle.Bottom
        Me.PanelBas.Location = New System.Drawing.Point(0, 772)
        Me.PanelBas.Name = "PanelBas"
        Me.PanelBas.Size = New System.Drawing.Size(1280, 48)
        Me.PanelBas.TabIndex = 1
        '
        'LabelEtat
        '
        Me.LabelEtat.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelEtat.AutoEllipsis = True
        Me.LabelEtat.ForeColor = System.Drawing.Color.FromArgb(CType(CType(230, Byte), Integer), CType(CType(230, Byte), Integer), CType(CType(235, Byte), Integer))
        Me.LabelEtat.Location = New System.Drawing.Point(12, 12)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(424, 26)
        Me.LabelEtat.TabIndex = 0
        Me.LabelEtat.Text = "Deezer dans Slide"
        Me.LabelEtat.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'ButtonRetenir
        '
        Me.ButtonRetenir.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonRetenir.Location = New System.Drawing.Point(632, 8)
        Me.ButtonRetenir.Name = "ButtonRetenir"
        Me.ButtonRetenir.Size = New System.Drawing.Size(260, 32)
        Me.ButtonRetenir.TabIndex = 3
        Me.ButtonRetenir.Text = "Jouer ceci au démarrage"
        Me.ButtonRetenir.UseVisualStyleBackColor = True
        '
        'ButtonAccueil
        '
        Me.ButtonAccueil.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonAccueil.Location = New System.Drawing.Point(904, 8)
        Me.ButtonAccueil.Name = "ButtonAccueil"
        Me.ButtonAccueil.Size = New System.Drawing.Size(170, 32)
        Me.ButtonAccueil.TabIndex = 1
        Me.ButtonAccueil.Text = "Accueil Deezer"
        Me.ButtonAccueil.UseVisualStyleBackColor = True
        '
        'ButtonMasquer
        '
        Me.ButtonMasquer.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonMasquer.Location = New System.Drawing.Point(1086, 8)
        Me.ButtonMasquer.Name = "ButtonMasquer"
        Me.ButtonMasquer.Size = New System.Drawing.Size(182, 32)
        Me.ButtonMasquer.TabIndex = 2
        Me.ButtonMasquer.Text = "Masquer la fenêtre"
        Me.ButtonMasquer.UseVisualStyleBackColor = True
        '
        'ButtonCompte
        '
        Me.ButtonCompte.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonCompte.Location = New System.Drawing.Point(448, 8)
        Me.ButtonCompte.Name = "ButtonCompte"
        Me.ButtonCompte.Size = New System.Drawing.Size(172, 32)
        Me.ButtonCompte.TabIndex = 4
        Me.ButtonCompte.Text = "Changer de compte"
        Me.ButtonCompte.UseVisualStyleBackColor = True
        '
        'FormDeezer
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1280, 820)
        Me.Controls.Add(Me.WebView21)
        Me.Controls.Add(Me.PanelBas)
        Me.MinimizeBox = False
        Me.MinimumSize = New System.Drawing.Size(700, 500)
        Me.Name = "FormDeezer"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Deezer"
        CType(Me.WebView21, System.ComponentModel.ISupportInitialize).EndInit()
        Me.PanelBas.ResumeLayout(False)
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents WebView21 As Microsoft.Web.WebView2.WinForms.WebView2
    Friend WithEvents PanelBas As Panel
    Friend WithEvents LabelEtat As Label
    Friend WithEvents ButtonRetenir As Button
    Friend WithEvents ButtonAccueil As Button
    Friend WithEvents ButtonMasquer As Button
    Friend WithEvents ButtonCompte As Button
End Class
