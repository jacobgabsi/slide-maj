<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class FormMinuteries
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
        Me.ListViewMinuteries = New System.Windows.Forms.ListView()
        Me.ColNom = New System.Windows.Forms.ColumnHeader()
        Me.ColDate = New System.Windows.Forms.ColumnHeader()
        Me.ColLignes = New System.Windows.Forms.ColumnHeader()
        Me.ButtonEnregistrer = New System.Windows.Forms.Button()
        Me.ButtonCharger = New System.Windows.Forms.Button()
        Me.ButtonRenommer = New System.Windows.Forms.Button()
        Me.ButtonSupprimer = New System.Windows.Forms.Button()
        Me.ButtonEnregistrerCollectif = New System.Windows.Forms.Button()
        Me.ButtonChargerCollectif = New System.Windows.Forms.Button()
        Me.ButtonFermer = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelTitre
        '
        Me.LabelTitre.Font = New System.Drawing.Font("Segoe UI Semibold", 17.0!, System.Drawing.FontStyle.Bold)
        Me.LabelTitre.Location = New System.Drawing.Point(28, 22)
        Me.LabelTitre.Name = "LabelTitre"
        Me.LabelTitre.Size = New System.Drawing.Size(704, 40)
        Me.LabelTitre.TabIndex = 0
        Me.LabelTitre.Text = "Minuteries enregistrées"
        '
        'LabelSousTitre
        '
        Me.LabelSousTitre.Font = New System.Drawing.Font("Segoe UI", 10.5!)
        Me.LabelSousTitre.ForeColor = System.Drawing.Color.FromArgb(CType(CType(152, Byte), Integer), CType(CType(152, Byte), Integer), CType(CType(157, Byte), Integer))
        Me.LabelSousTitre.Location = New System.Drawing.Point(28, 64)
        Me.LabelSousTitre.Name = "LabelSousTitre"
        Me.LabelSousTitre.Size = New System.Drawing.Size(704, 28)
        Me.LabelSousTitre.TabIndex = 1
        Me.LabelSousTitre.Text = "Enregistrez la minuterie actuelle, puis chargez-en une autre quand vous voulez."
        '
        'ListViewMinuteries
        '
        Me.ListViewMinuteries.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColNom, Me.ColDate, Me.ColLignes})
        Me.ListViewMinuteries.FullRowSelect = True
        Me.ListViewMinuteries.HideSelection = False
        Me.ListViewMinuteries.Location = New System.Drawing.Point(28, 104)
        Me.ListViewMinuteries.MultiSelect = False
        Me.ListViewMinuteries.Name = "ListViewMinuteries"
        Me.ListViewMinuteries.Size = New System.Drawing.Size(704, 300)
        Me.ListViewMinuteries.TabIndex = 2
        Me.ListViewMinuteries.UseCompatibleStateImageBehavior = False
        Me.ListViewMinuteries.View = System.Windows.Forms.View.Details
        '
        'ColNom
        '
        Me.ColNom.Text = "Minuterie"
        Me.ColNom.Width = 360
        '
        'ColDate
        '
        Me.ColDate.Text = "Enregistrée le"
        Me.ColDate.Width = 210
        '
        'ColLignes
        '
        Me.ColLignes.Text = "Lignes"
        Me.ColLignes.Width = 100
        '
        'ButtonEnregistrer
        '
        Me.ButtonEnregistrer.Font = New System.Drawing.Font("Segoe UI Semibold", 11.0!, System.Drawing.FontStyle.Bold)
        Me.ButtonEnregistrer.Location = New System.Drawing.Point(28, 418)
        Me.ButtonEnregistrer.Name = "ButtonEnregistrer"
        Me.ButtonEnregistrer.Size = New System.Drawing.Size(704, 50)
        Me.ButtonEnregistrer.TabIndex = 3
        Me.ButtonEnregistrer.Text = "Enregistrer la minuterie actuelle..."
        Me.ButtonEnregistrer.UseVisualStyleBackColor = True
        '
        'ButtonCharger
        '
        Me.ButtonCharger.Font = New System.Drawing.Font("Segoe UI Semibold", 11.0!, System.Drawing.FontStyle.Bold)
        Me.ButtonCharger.Location = New System.Drawing.Point(28, 478)
        Me.ButtonCharger.Name = "ButtonCharger"
        Me.ButtonCharger.Size = New System.Drawing.Size(224, 50)
        Me.ButtonCharger.TabIndex = 4
        Me.ButtonCharger.Text = "Charger..."
        Me.ButtonCharger.UseVisualStyleBackColor = True
        '
        'ButtonRenommer
        '
        Me.ButtonRenommer.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ButtonRenommer.Location = New System.Drawing.Point(268, 478)
        Me.ButtonRenommer.Name = "ButtonRenommer"
        Me.ButtonRenommer.Size = New System.Drawing.Size(224, 50)
        Me.ButtonRenommer.TabIndex = 5
        Me.ButtonRenommer.Text = "Renommer..."
        Me.ButtonRenommer.UseVisualStyleBackColor = True
        '
        'ButtonSupprimer
        '
        Me.ButtonSupprimer.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ButtonSupprimer.Location = New System.Drawing.Point(508, 478)
        Me.ButtonSupprimer.Name = "ButtonSupprimer"
        Me.ButtonSupprimer.Size = New System.Drawing.Size(224, 50)
        Me.ButtonSupprimer.TabIndex = 6
        Me.ButtonSupprimer.Text = "Supprimer"
        Me.ButtonSupprimer.UseVisualStyleBackColor = True
        '
        'ButtonEnregistrerCollectif
        '
        Me.ButtonEnregistrerCollectif.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ButtonEnregistrerCollectif.Location = New System.Drawing.Point(28, 540)
        Me.ButtonEnregistrerCollectif.Name = "ButtonEnregistrerCollectif"
        Me.ButtonEnregistrerCollectif.Size = New System.Drawing.Size(344, 50)
        Me.ButtonEnregistrerCollectif.TabIndex = 8
        Me.ButtonEnregistrerCollectif.Text = "Enregistrer dans les collectives..."
        Me.ButtonEnregistrerCollectif.UseVisualStyleBackColor = True
        '
        'ButtonChargerCollectif
        '
        Me.ButtonChargerCollectif.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ButtonChargerCollectif.Location = New System.Drawing.Point(388, 540)
        Me.ButtonChargerCollectif.Name = "ButtonChargerCollectif"
        Me.ButtonChargerCollectif.Size = New System.Drawing.Size(344, 50)
        Me.ButtonChargerCollectif.TabIndex = 9
        Me.ButtonChargerCollectif.Text = "Charger une collective..."
        Me.ButtonChargerCollectif.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonFermer.Font = New System.Drawing.Font("Segoe UI", 11.0!)
        Me.ButtonFermer.Location = New System.Drawing.Point(28, 602)
        Me.ButtonFermer.Name = "ButtonFermer"
        Me.ButtonFermer.Size = New System.Drawing.Size(704, 50)
        Me.ButtonFermer.TabIndex = 7
        Me.ButtonFermer.Text = "Fermer"
        Me.ButtonFermer.UseVisualStyleBackColor = True
        '
        'FormMinuteries
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonFermer
        Me.ClientSize = New System.Drawing.Size(760, 678)
        Me.Controls.Add(Me.ButtonChargerCollectif)
        Me.Controls.Add(Me.ButtonEnregistrerCollectif)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.ButtonSupprimer)
        Me.Controls.Add(Me.ButtonRenommer)
        Me.Controls.Add(Me.ButtonCharger)
        Me.Controls.Add(Me.ButtonEnregistrer)
        Me.Controls.Add(Me.ListViewMinuteries)
        Me.Controls.Add(Me.LabelSousTitre)
        Me.Controls.Add(Me.LabelTitre)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormMinuteries"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Minuteries enregistrées"
        Me.ResumeLayout(False)

    End Sub
    Friend WithEvents LabelTitre As Label
    Friend WithEvents LabelSousTitre As Label
    Friend WithEvents ListViewMinuteries As ListView
    Friend WithEvents ColNom As ColumnHeader
    Friend WithEvents ColDate As ColumnHeader
    Friend WithEvents ColLignes As ColumnHeader
    Friend WithEvents ButtonEnregistrer As Button
    Friend WithEvents ButtonCharger As Button
    Friend WithEvents ButtonRenommer As Button
    Friend WithEvents ButtonSupprimer As Button
    Friend WithEvents ButtonEnregistrerCollectif As Button
    Friend WithEvents ButtonChargerCollectif As Button
    Friend WithEvents ButtonFermer As Button
End Class
