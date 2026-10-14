<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormVerifierRadios
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
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.ProgressBarVerif = New System.Windows.Forms.ProgressBar()
        Me.LabelListe = New System.Windows.Forms.Label()
        Me.CheckedListBoxPannes = New System.Windows.Forms.CheckedListBox()
        Me.ButtonToutCocher = New System.Windows.Forms.Button()
        Me.ButtonToutDecocher = New System.Windows.Forms.Button()
        Me.ButtonSupprimer = New System.Windows.Forms.Button()
        Me.ButtonFermer = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelEtat
        '
        Me.LabelEtat.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelEtat.Location = New System.Drawing.Point(16, 14)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(728, 28)
        Me.LabelEtat.TabIndex = 0
        Me.LabelEtat.Text = "Vérification des radios..."
        '
        'ProgressBarVerif
        '
        Me.ProgressBarVerif.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ProgressBarVerif.Location = New System.Drawing.Point(16, 46)
        Me.ProgressBarVerif.Name = "ProgressBarVerif"
        Me.ProgressBarVerif.Size = New System.Drawing.Size(728, 22)
        Me.ProgressBarVerif.TabIndex = 1
        '
        'LabelListe
        '
        Me.LabelListe.Anchor = CType(((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelListe.Location = New System.Drawing.Point(16, 80)
        Me.LabelListe.Name = "LabelListe"
        Me.LabelListe.Size = New System.Drawing.Size(728, 28)
        Me.LabelListe.TabIndex = 2
        Me.LabelListe.Text = "Radios qui ne répondent pas (cochées : elles seront supprimées) :"
        '
        'CheckedListBoxPannes
        '
        Me.CheckedListBoxPannes.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) Or System.Windows.Forms.AnchorStyles.Left) Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.CheckedListBoxPannes.CheckOnClick = True
        Me.CheckedListBoxPannes.FormattingEnabled = True
        Me.CheckedListBoxPannes.HorizontalScrollbar = True
        Me.CheckedListBoxPannes.Location = New System.Drawing.Point(16, 112)
        Me.CheckedListBoxPannes.Name = "CheckedListBoxPannes"
        Me.CheckedListBoxPannes.Size = New System.Drawing.Size(728, 284)
        Me.CheckedListBoxPannes.TabIndex = 3
        '
        'ButtonToutCocher
        '
        Me.ButtonToutCocher.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonToutCocher.Location = New System.Drawing.Point(16, 410)
        Me.ButtonToutCocher.Name = "ButtonToutCocher"
        Me.ButtonToutCocher.Size = New System.Drawing.Size(130, 38)
        Me.ButtonToutCocher.TabIndex = 4
        Me.ButtonToutCocher.Text = "Tout cocher"
        Me.ButtonToutCocher.UseVisualStyleBackColor = True
        '
        'ButtonToutDecocher
        '
        Me.ButtonToutDecocher.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonToutDecocher.Location = New System.Drawing.Point(154, 410)
        Me.ButtonToutDecocher.Name = "ButtonToutDecocher"
        Me.ButtonToutDecocher.Size = New System.Drawing.Size(140, 38)
        Me.ButtonToutDecocher.TabIndex = 5
        Me.ButtonToutDecocher.Text = "Tout décocher"
        Me.ButtonToutDecocher.UseVisualStyleBackColor = True
        '
        'ButtonSupprimer
        '
        Me.ButtonSupprimer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonSupprimer.Enabled = False
        Me.ButtonSupprimer.Location = New System.Drawing.Point(398, 410)
        Me.ButtonSupprimer.Name = "ButtonSupprimer"
        Me.ButtonSupprimer.Size = New System.Drawing.Size(220, 38)
        Me.ButtonSupprimer.TabIndex = 6
        Me.ButtonSupprimer.Text = "Supprimer les cochées"
        Me.ButtonSupprimer.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonFermer.Location = New System.Drawing.Point(626, 410)
        Me.ButtonFermer.Name = "ButtonFermer"
        Me.ButtonFermer.Size = New System.Drawing.Size(118, 38)
        Me.ButtonFermer.TabIndex = 7
        Me.ButtonFermer.Text = "Arrêter"
        Me.ButtonFermer.UseVisualStyleBackColor = True
        '
        'FormVerifierRadios
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonFermer
        Me.ClientSize = New System.Drawing.Size(760, 464)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.ButtonSupprimer)
        Me.Controls.Add(Me.ButtonToutDecocher)
        Me.Controls.Add(Me.ButtonToutCocher)
        Me.Controls.Add(Me.CheckedListBoxPannes)
        Me.Controls.Add(Me.LabelListe)
        Me.Controls.Add(Me.ProgressBarVerif)
        Me.Controls.Add(Me.LabelEtat)
        Me.MinimumSize = New System.Drawing.Size(640, 400)
        Me.Name = "FormVerifierRadios"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Vérifier les radios"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelEtat As Label
    Friend WithEvents ProgressBarVerif As ProgressBar
    Friend WithEvents LabelListe As Label
    Friend WithEvents CheckedListBoxPannes As CheckedListBox
    Friend WithEvents ButtonToutCocher As Button
    Friend WithEvents ButtonToutDecocher As Button
    Friend WithEvents ButtonSupprimer As Button
    Friend WithEvents ButtonFermer As Button
End Class
