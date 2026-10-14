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
        Me.ColonnePourcent = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneVitesse = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneFin = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ButtonAnnulerTelechargement = New System.Windows.Forms.Button()
        Me.ButtonReprendre = New System.Windows.Forms.Button()
        Me.ButtonPause = New System.Windows.Forms.Button()
        Me.ButtonToutAnnuler = New System.Windows.Forms.Button()
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
        Me.GrilleTelechargements.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.ColonneTitre, Me.ColonneEtat, Me.ColonneAvancement, Me.ColonnePourcent, Me.ColonneVitesse, Me.ColonneFin})
        Me.GrilleTelechargements.Location = New System.Drawing.Point(12, 12)
        Me.GrilleTelechargements.MultiSelect = True
        Me.GrilleTelechargements.Name = "GrilleTelechargements"
        Me.GrilleTelechargements.ReadOnly = True
        Me.GrilleTelechargements.RowHeadersVisible = False
        Me.GrilleTelechargements.RowHeadersWidth = 62
        Me.GrilleTelechargements.RowTemplate.Height = 33
        Me.GrilleTelechargements.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.GrilleTelechargements.Size = New System.Drawing.Size(1376, 348)
        Me.GrilleTelechargements.TabIndex = 0
        '
        'ColonneTitre
        '
        Me.ColonneTitre.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.ColonneTitre.HeaderText = "Film"
        Me.ColonneTitre.MinimumWidth = 150
        Me.ColonneTitre.DefaultCellStyle = New System.Windows.Forms.DataGridViewCellStyle() With {.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft, .Padding = New System.Windows.Forms.Padding(6, 0, 10, 0)}
        Me.ColonneTitre.Name = "ColonneTitre"
        Me.ColonneTitre.ReadOnly = True
        '
        'ColonneEtat
        '
        Me.ColonneEtat.HeaderText = "État"
        Me.ColonneEtat.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonneEtat.MinimumWidth = 8
        Me.ColonneEtat.Name = "ColonneEtat"
        Me.ColonneEtat.ReadOnly = True
        Me.ColonneEtat.Resizable = System.Windows.Forms.DataGridViewTriState.True
        Me.ColonneEtat.DefaultCellStyle = New System.Windows.Forms.DataGridViewCellStyle() With {.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft, .Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)}
        Me.ColonneEtat.Width = 210
        '
        'ColonneAvancement
        '
        Me.ColonneAvancement.HeaderText = "Avancement"
        Me.ColonneAvancement.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonneAvancement.MinimumWidth = 8
        Me.ColonneAvancement.Name = "ColonneAvancement"
        Me.ColonneAvancement.ReadOnly = True
        Me.ColonneAvancement.DefaultCellStyle = New System.Windows.Forms.DataGridViewCellStyle() With {.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight, .Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)}
        Me.ColonneAvancement.HeaderCell.Style.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
        Me.ColonneAvancement.Width = 250
        '
        'ColonnePourcent
        '
        Me.ColonnePourcent.HeaderText = "%"
        Me.ColonnePourcent.MinimumWidth = 8
        Me.ColonnePourcent.Name = "ColonnePourcent"
        Me.ColonnePourcent.ReadOnly = True
        Me.ColonnePourcent.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonnePourcent.Width = 90
        Me.ColonnePourcent.DefaultCellStyle = New System.Windows.Forms.DataGridViewCellStyle() With {.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight, .Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)}
        Me.ColonnePourcent.HeaderCell.Style.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
        '
        'ColonneVitesse
        '
        Me.ColonneVitesse.HeaderText = "Vitesse"
        Me.ColonneVitesse.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonneVitesse.MinimumWidth = 8
        Me.ColonneVitesse.Name = "ColonneVitesse"
        Me.ColonneVitesse.ReadOnly = True
        Me.ColonneVitesse.DefaultCellStyle = New System.Windows.Forms.DataGridViewCellStyle() With {.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight, .Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)}
        Me.ColonneVitesse.HeaderCell.Style.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleRight
        Me.ColonneVitesse.Width = 120
        '
        'ColonneFin
        '
        Me.ColonneFin.HeaderText = "Reste / fin estimée"
        Me.ColonneFin.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.None
        Me.ColonneFin.MinimumWidth = 8
        Me.ColonneFin.Name = "ColonneFin"
        Me.ColonneFin.ReadOnly = True
        Me.ColonneFin.DefaultCellStyle = New System.Windows.Forms.DataGridViewCellStyle() With {.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft, .Padding = New System.Windows.Forms.Padding(10, 0, 10, 0)}
        Me.ColonneFin.Width = 250
        '
        'ButtonAnnulerTelechargement
        '
        Me.ButtonAnnulerTelechargement.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonAnnulerTelechargement.Location = New System.Drawing.Point(12, 408)
        Me.ButtonAnnulerTelechargement.Name = "ButtonAnnulerTelechargement"
        Me.ButtonAnnulerTelechargement.Size = New System.Drawing.Size(150, 40)
        Me.ButtonAnnulerTelechargement.TabIndex = 1
        Me.ButtonAnnulerTelechargement.Text = "Annuler"
        Me.ButtonAnnulerTelechargement.UseVisualStyleBackColor = True
        '
        'ButtonToutAnnuler
        '
        Me.ButtonToutAnnuler.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonToutAnnuler.Location = New System.Drawing.Point(170, 408)
        Me.ButtonToutAnnuler.Name = "ButtonToutAnnuler"
        Me.ButtonToutAnnuler.Size = New System.Drawing.Size(180, 40)
        Me.ButtonToutAnnuler.TabIndex = 7
        Me.ButtonToutAnnuler.Text = "Tout annuler"
        Me.ButtonToutAnnuler.UseVisualStyleBackColor = True
        '
        'ButtonPause
        '
        Me.ButtonPause.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonPause.Location = New System.Drawing.Point(358, 408)
        Me.ButtonPause.Name = "ButtonPause"
        Me.ButtonPause.Size = New System.Drawing.Size(150, 40)
        Me.ButtonPause.TabIndex = 8
        Me.ButtonPause.Text = "Pause"
        Me.ButtonPause.UseVisualStyleBackColor = True
        '
        'ButtonReprendre
        '
        Me.ButtonReprendre.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonReprendre.Location = New System.Drawing.Point(516, 408)
        Me.ButtonReprendre.Name = "ButtonReprendre"
        Me.ButtonReprendre.Size = New System.Drawing.Size(150, 40)
        Me.ButtonReprendre.TabIndex = 2
        Me.ButtonReprendre.Text = "Reprendre"
        Me.ButtonReprendre.UseVisualStyleBackColor = True
        '
        'ButtonOuvrirLeDossier
        '
        Me.ButtonOuvrirLeDossier.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonOuvrirLeDossier.Location = New System.Drawing.Point(674, 408)
        Me.ButtonOuvrirLeDossier.Name = "ButtonOuvrirLeDossier"
        Me.ButtonOuvrirLeDossier.Size = New System.Drawing.Size(200, 40)
        Me.ButtonOuvrirLeDossier.TabIndex = 3
        Me.ButtonOuvrirLeDossier.Text = "Ouvrir le dossier"
        Me.ButtonOuvrirLeDossier.UseVisualStyleBackColor = True
        '
        'ButtonRetirerTermines
        '
        Me.ButtonRetirerTermines.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonRetirerTermines.Location = New System.Drawing.Point(882, 408)
        Me.ButtonRetirerTermines.Name = "ButtonRetirerTermines"
        Me.ButtonRetirerTermines.Size = New System.Drawing.Size(240, 40)
        Me.ButtonRetirerTermines.TabIndex = 4
        Me.ButtonRetirerTermines.Text = "Retirer les terminés"
        Me.ButtonRetirerTermines.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonFermer.Location = New System.Drawing.Point(1208, 408)
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
        Me.LabelBilan.Size = New System.Drawing.Size(1376, 28)
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
        Me.ClientSize = New System.Drawing.Size(1400, 460)
        Me.Controls.Add(Me.LabelBilan)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.ButtonRetirerTermines)
        Me.Controls.Add(Me.ButtonOuvrirLeDossier)
        Me.Controls.Add(Me.ButtonReprendre)
        Me.Controls.Add(Me.ButtonPause)
        Me.Controls.Add(Me.ButtonToutAnnuler)
        Me.Controls.Add(Me.ButtonAnnulerTelechargement)
        Me.Controls.Add(Me.GrilleTelechargements)
        Me.MinimumSize = New System.Drawing.Size(1150, 400)
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
    Friend WithEvents ColonnePourcent As DataGridViewTextBoxColumn
    Friend WithEvents ColonneVitesse As DataGridViewTextBoxColumn
    Friend WithEvents ColonneFin As DataGridViewTextBoxColumn
    Friend WithEvents ButtonAnnulerTelechargement As Button
    Friend WithEvents ButtonReprendre As Button
    Friend WithEvents ButtonPause As Button
    Friend WithEvents ButtonToutAnnuler As Button
    Friend WithEvents ButtonOuvrirLeDossier As Button
    Friend WithEvents ButtonRetirerTermines As Button
    Friend WithEvents ButtonFermer As Button
    Friend WithEvents LabelBilan As Label
    Friend WithEvents TimerRafraichir As Timer
End Class
