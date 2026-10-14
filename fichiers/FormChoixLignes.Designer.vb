<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormChoixLignes
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
        Me.LabelTitre = New System.Windows.Forms.Label()
        Me.LabelSousTitre = New System.Windows.Forms.Label()
        Me.ListViewLignes = New System.Windows.Forms.ListView()
        Me.ColJour = New System.Windows.Forms.ColumnHeader()
        Me.ColHeure = New System.Windows.Forms.ColumnHeader()
        Me.ColType = New System.Windows.Forms.ColumnHeader()
        Me.ColAction = New System.Windows.Forms.ColumnHeader()
        Me.ButtonTout = New System.Windows.Forms.Button()
        Me.ButtonAucune = New System.Windows.Forms.Button()
        Me.LabelNombre = New System.Windows.Forms.Label()
        Me.PanelMode = New System.Windows.Forms.Panel()
        Me.PanelMarqueMode = New System.Windows.Forms.Panel()
        Me.RadioRemplacer = New System.Windows.Forms.RadioButton()
        Me.RadioAjouter = New System.Windows.Forms.RadioButton()
        Me.ButtonOk = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.PanelMode.SuspendLayout()
        Me.SuspendLayout()
        '
        'LabelTitre
        '
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 17.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTitre.Location = New System.Drawing.Point(28, 22)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(704, 40)
        Me.LabelTitre.TabIndex = 0
        Me.LabelTitre.Text = "Lignes de la minuterie"
        '
        'LabelSousTitre
        '
        Me.LabelSousTitre.Font = New System.Drawing.Font("Segoe UI", 10.5!)
        Me.LabelSousTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelSousTitre.Location = New System.Drawing.Point(28, 64)
        Me.LabelSousTitre.Name = "LabelSousTitre"
        Me.LabelSousTitre.Size = New System.Drawing.Size(704, 28)
        Me.LabelSousTitre.TabIndex = 1
        Me.LabelSousTitre.Text = "Cochez les lignes à garder."
        '
        'ListViewLignes
        '
        Me.ListViewLignes.CheckBoxes = True
        Me.ListViewLignes.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColJour, Me.ColHeure, Me.ColType, Me.ColAction})
        Me.ListViewLignes.FullRowSelect = True
        Me.ListViewLignes.HideSelection = False
        Me.ListViewLignes.Location = New System.Drawing.Point(28, 104)
        Me.ListViewLignes.Name = "ListViewLignes"
        Me.ListViewLignes.Size = New System.Drawing.Size(704, 340)
        Me.ListViewLignes.TabIndex = 2
        Me.ListViewLignes.UseCompatibleStateImageBehavior = False
        Me.ListViewLignes.View = System.Windows.Forms.View.Details
        '
        'ColJour
        '
        Me.ColJour.Text = "Jour"
        Me.ColJour.Width = 130
        '
        'ColHeure
        '
        Me.ColHeure.Text = "Heure"
        Me.ColHeure.Width = 80
        '
        'ColType
        '
        Me.ColType.Text = "Type"
        Me.ColType.Width = 120
        '
        'ColAction
        '
        Me.ColAction.Text = "Action"
        Me.ColAction.Width = 340
        '
        'ButtonTout
        '
        Me.ButtonTout.Location = New System.Drawing.Point(28, 456)
        Me.ButtonTout.Name = "ButtonTout"
        Me.ButtonTout.Size = New System.Drawing.Size(170, 40)
        Me.ButtonTout.TabIndex = 3
        Me.ButtonTout.Text = "Tout cocher"
        Me.ButtonTout.UseVisualStyleBackColor = True
        '
        'ButtonAucune
        '
        Me.ButtonAucune.Location = New System.Drawing.Point(206, 456)
        Me.ButtonAucune.Name = "ButtonAucune"
        Me.ButtonAucune.Size = New System.Drawing.Size(170, 40)
        Me.ButtonAucune.TabIndex = 4
        Me.ButtonAucune.Text = "Aucune"
        Me.ButtonAucune.UseVisualStyleBackColor = True
        '
        'LabelNombre
        '
        Me.LabelNombre.Font = New System.Drawing.Font("Segoe UI Semibold", 10.5!, System.Drawing.FontStyle.Bold)
        Me.LabelNombre.Location = New System.Drawing.Point(390, 462)
        Me.LabelNombre.Name = "LabelNombre"
        Me.LabelNombre.Size = New System.Drawing.Size(342, 30)
        Me.LabelNombre.TabIndex = 5
        Me.LabelNombre.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'PanelMode
        '
        Me.PanelMode.BackColor = System.Drawing.Color.FromArgb(CType(CType(26, Byte), Integer), CType(CType(44, Byte), Integer), CType(CType(70, Byte), Integer))
        Me.PanelMode.Controls.Add(Me.RadioAjouter)
        Me.PanelMode.Controls.Add(Me.RadioRemplacer)
        Me.PanelMode.Controls.Add(Me.PanelMarqueMode)
        Me.PanelMode.Location = New System.Drawing.Point(28, 510)
        Me.PanelMode.Name = "PanelMode"
        Me.PanelMode.Size = New System.Drawing.Size(704, 90)
        Me.PanelMode.TabIndex = 6
        '
        'PanelMarqueMode
        '
        Me.PanelMarqueMode.BackColor = System.Drawing.Color.FromArgb(CType(CType(10, Byte), Integer), CType(CType(132, Byte), Integer), CType(CType(255, Byte), Integer))
        Me.PanelMarqueMode.Dock = System.Windows.Forms.DockStyle.Left
        Me.PanelMarqueMode.Location = New System.Drawing.Point(0, 0)
        Me.PanelMarqueMode.Name = "PanelMarqueMode"
        Me.PanelMarqueMode.Size = New System.Drawing.Size(6, 90)
        Me.PanelMarqueMode.TabIndex = 2
        '
        'RadioRemplacer
        '
        Me.RadioRemplacer.AutoSize = True
        Me.RadioRemplacer.Checked = True
        Me.RadioRemplacer.Font = New System.Drawing.Font("Segoe UI", 10.5!)
        Me.RadioRemplacer.Location = New System.Drawing.Point(28, 12)
        Me.RadioRemplacer.Name = "RadioRemplacer"
        Me.RadioRemplacer.Size = New System.Drawing.Size(420, 27)
        Me.RadioRemplacer.TabIndex = 0
        Me.RadioRemplacer.TabStop = True
        Me.RadioRemplacer.Text = "Remplacer la minuterie actuelle"
        Me.RadioRemplacer.UseVisualStyleBackColor = True
        '
        'RadioAjouter
        '
        Me.RadioAjouter.AutoSize = True
        Me.RadioAjouter.Font = New System.Drawing.Font("Segoe UI", 10.5!)
        Me.RadioAjouter.Location = New System.Drawing.Point(28, 48)
        Me.RadioAjouter.Name = "RadioAjouter"
        Me.RadioAjouter.Size = New System.Drawing.Size(640, 27)
        Me.RadioAjouter.TabIndex = 1
        Me.RadioAjouter.Text = "Ajouter à la minuterie actuelle (sans doubler les lignes identiques)"
        Me.RadioAjouter.UseVisualStyleBackColor = True
        '
        'ButtonOk
        '
        Me.ButtonOk.Font = New System.Drawing.Font("Segoe UI Semibold", 11.0!, System.Drawing.FontStyle.Bold)
        Me.ButtonOk.Location = New System.Drawing.Point(28, 616)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(344, 50)
        Me.ButtonOk.TabIndex = 7
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ButtonAnnuler.Location = New System.Drawing.Point(388, 616)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(344, 50)
        Me.ButtonAnnuler.TabIndex = 8
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormChoixLignes
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(760, 692)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.PanelMode)
        Me.Controls.Add(Me.LabelNombre)
        Me.Controls.Add(Me.ButtonAucune)
        Me.Controls.Add(Me.ButtonTout)
        Me.Controls.Add(Me.ListViewLignes)
        Me.Controls.Add(Me.LabelSousTitre)
        Me.Controls.Add(Me.LabelTitre)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormChoixLignes"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Lignes de la minuterie"
        Me.PanelMode.ResumeLayout(False)
        Me.PanelMode.PerformLayout()
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents LabelTitre As Label
    Friend WithEvents LabelSousTitre As Label
    Friend WithEvents ListViewLignes As ListView
    Friend WithEvents ColJour As ColumnHeader
    Friend WithEvents ColHeure As ColumnHeader
    Friend WithEvents ColType As ColumnHeader
    Friend WithEvents ColAction As ColumnHeader
    Friend WithEvents ButtonTout As Button
    Friend WithEvents ButtonAucune As Button
    Friend WithEvents LabelNombre As Label
    Friend WithEvents PanelMode As Panel
    Friend WithEvents PanelMarqueMode As Panel
    Friend WithEvents RadioRemplacer As RadioButton
    Friend WithEvents RadioAjouter As RadioButton
    Friend WithEvents ButtonOk As Button
    Friend WithEvents ButtonAnnuler As Button
End Class
