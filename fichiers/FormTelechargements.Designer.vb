<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormTelechargements
    Inherits System.Windows.Forms.Form

    'Form remplace la méthode Dispose pour nettoyer la liste des composants.
    <System.Diagnostics.DebuggerNonUserCode()> _
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
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.components = New System.ComponentModel.Container()
        Me.GrilleTelechargements = New System.Windows.Forms.DataGridView()
        Me.ColonneTitre = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneEtat = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneAvancement = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneVitesse = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ButtonAnnulerTelechargement = New System.Windows.Forms.Button()
        Me.ButtonReprendre = New System.Windows.Forms.Button()
        Me.ButtonOuvrirLeDossier = New System.Windows.Forms.Button()
        Me.ButtonRetirerTermines = New System.Windows.Forms.Button()
        Me.ButtonFermer = New System.Windows.Forms.Button()
        Me.LabelBilan = New System.Windows.Forms.Label()
        Me.TimerRafraichir = New System.Windows.Forms.Timer(Me.components)
        CType(Me.GrilleTelechargements, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'GrilleTelechargements
        '
        Me.GrilleTelechargements.AllowUserToAddRows = False
        Me.GrilleTelechargements.AllowUserToDeleteRows = False
        Me.GrilleTelechargements.Anchor = CType((((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom) _
            Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GrilleTelechargements.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.GrilleTelechargements.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ColonneTitre, Me.ColonneEtat, Me.ColonneAvancement, Me.ColonneVitesse})
        Me.GrilleTelechargements.Location = New System.Drawing.Point(12, 12)
        Me.GrilleTelechargements.MultiSelect = True
        Me.GrilleTelechargements.Name = "GrilleTelechargements"
        Me.GrilleTelechargements.ReadOnly = True
        Me.GrilleTelechargements.RowHeadersVisible = False
        Me.GrilleTelechargements.RowHeadersWidth = 62
        Me.GrilleTelechargements.RowTemplate.Height = 33
        Me.GrilleTelechargements.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.GrilleTelechargements.Size = New System.Drawing.Size(1036, 348)
        Me.GrilleTelechargements.TabIndex = 0
        '
        'ColonneTitre
        '
        Me.ColonneTitre.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.ColonneTitre.HeaderText = "Film"
        Me.ColonneTitre.MinimumWidth = 8
        Me.ColonneTitre.Name = "ColonneTitre"
        Me.ColonneTitre.ReadOnly = True
        '
        'ColonneEtat
        '
        Me.ColonneEtat.HeaderText = "État"
        Me.ColonneEtat.MinimumWidth = 8
        Me.ColonneEtat.Name = "ColonneEtat"
        Me.ColonneEtat.ReadOnly = True
        Me.ColonneEtat.Width = 190
        '
        'ColonneAvancement
        '
        Me.ColonneAvancement.HeaderText = "Avancement"
        Me.ColonneAvancement.MinimumWidth = 8
        Me.ColonneAvancement.Name = "ColonneAvancement"
        Me.ColonneAvancement.ReadOnly = True
        Me.ColonneAvancement.Width = 260
        '
        'ColonneVitesse
        '
        Me.ColonneVitesse.HeaderText = "Vitesse"
        Me.ColonneVitesse.MinimumWidth = 8
        Me.ColonneVitesse.Name = "ColonneVitesse"
        Me.ColonneVitesse.ReadOnly = True
        Me.ColonneVitesse.Width = 120
        '
        'ButtonAnnulerTelechargement
        '
        Me.ButtonAnnulerTelechargement.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonAnnulerTelechargement.Location = New System.Drawing.Point(12, 408)
        Me.ButtonAnnulerTelechargement.Name = "ButtonAnnulerTelechargement"
        Me.ButtonAnnulerTelechargement.Size = New System.Drawing.Size(180, 40)
        Me.ButtonAnnulerTelechargement.TabIndex = 1
        Me.ButtonAnnulerTelechargement.Text = "Annuler"
        Me.ButtonAnnulerTelechargement.UseVisualStyleBackColor = True
        '
        'ButtonReprendre
        '
        Me.ButtonReprendre.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonReprendre.Location = New System.Drawing.Point(200, 408)
        Me.ButtonReprendre.Name = "ButtonReprendre"
        Me.ButtonReprendre.Size = New System.Drawing.Size(180, 40)
        Me.ButtonReprendre.TabIndex = 2
        Me.ButtonReprendre.Text = "Reprendre"
        Me.ButtonReprendre.UseVisualStyleBackColor = True
        '
        'ButtonOuvrirLeDossier
        '
        Me.ButtonOuvrirLeDossier.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonOuvrirLeDossier.Location = New System.Drawing.Point(388, 408)
        Me.ButtonOuvrirLeDossier.Name = "ButtonOuvrirLeDossier"
        Me.ButtonOuvrirLeDossier.Size = New System.Drawing.Size(200, 40)
        Me.ButtonOuvrirLeDossier.TabIndex = 3
        Me.ButtonOuvrirLeDossier.Text = "Ouvrir le dossier"
        Me.ButtonOuvrirLeDossier.UseVisualStyleBackColor = True
        '
        'ButtonRetirerTermines
        '
        Me.ButtonRetirerTermines.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonRetirerTermines.Location = New System.Drawing.Point(596, 408)
        Me.ButtonRetirerTermines.Name = "ButtonRetirerTermines"
        Me.ButtonRetirerTermines.Size = New System.Drawing.Size(240, 40)
        Me.ButtonRetirerTermines.TabIndex = 4
        Me.ButtonRetirerTermines.Text = "Retirer les terminés"
        Me.ButtonRetirerTermines.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonFermer.Location = New System.Drawing.Point(868, 408)
        Me.ButtonFermer.Name = "ButtonFermer"
        Me.ButtonFermer.Size = New System.Drawing.Size(180, 40)
        Me.ButtonFermer.TabIndex = 5
        Me.ButtonFermer.Text = "Fermer"
        Me.ButtonFermer.UseVisualStyleBackColor = True
        '
        'LabelBilan
        '
        Me.LabelBilan.Anchor = CType(((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left) _
            Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelBilan.AutoEllipsis = True
        Me.LabelBilan.Location = New System.Drawing.Point(12, 368)
        Me.LabelBilan.Name = "LabelBilan"
        Me.LabelBilan.Size = New System.Drawing.Size(1036, 28)
        Me.LabelBilan.TabIndex = 6
        Me.LabelBilan.Text = "Aucun téléchargement."
        Me.LabelBilan.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'TimerRafraichir
        '
        Me.TimerRafraichir.Interval = 700
        '
        'FormTelechargements
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1060, 460)
        Me.Controls.Add(Me.LabelBilan)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.ButtonRetirerTermines)
        Me.Controls.Add(Me.ButtonOuvrirLeDossier)
        Me.Controls.Add(Me.ButtonReprendre)
        Me.Controls.Add(Me.ButtonAnnulerTelechargement)
        Me.Controls.Add(Me.GrilleTelechargements)
        Me.MinimumSize = New System.Drawing.Size(900, 400)
        Me.Name = "FormTelechargements"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Téléchargements"
        CType(Me.GrilleTelechargements, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents GrilleTelechargements As DataGridView
    Friend WithEvents ColonneTitre As DataGridViewTextBoxColumn
    Friend WithEvents ColonneEtat As DataGridViewTextBoxColumn
    Friend WithEvents ColonneAvancement As DataGridViewTextBoxColumn
    Friend WithEvents ColonneVitesse As DataGridViewTextBoxColumn
    Friend WithEvents ButtonAnnulerTelechargement As Button
    Friend WithEvents ButtonReprendre As Button
    Friend WithEvents ButtonOuvrirLeDossier As Button
    Friend WithEvents ButtonRetirerTermines As Button
    Friend WithEvents ButtonFermer As Button
    Friend WithEvents LabelBilan As Label
    Friend WithEvents TimerRafraichir As Timer
End Class
