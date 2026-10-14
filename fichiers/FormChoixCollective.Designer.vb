<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormChoixCollective
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
        Me.LabelAide = New System.Windows.Forms.Label()
        Me.ListViewCollectives = New System.Windows.Forms.ListView()
        Me.ColNom = New System.Windows.Forms.ColumnHeader()
        Me.ColDate = New System.Windows.Forms.ColumnHeader()
        Me.ColLignes = New System.Windows.Forms.ColumnHeader()
        Me.ButtonOk = New System.Windows.Forms.Button()
        Me.ButtonAnnuler = New System.Windows.Forms.Button()
        Me.SuspendLayout()
        '
        'LabelAide
        '
        Me.LabelAide.Location = New System.Drawing.Point(20, 14)
        Me.LabelAide.Name = "LabelAide"
        Me.LabelAide.Size = New System.Drawing.Size(660, 52)
        Me.LabelAide.TabIndex = 0
        Me.LabelAide.Text = "Choisissez la minuterie collective à charger. Vous choisirez ensuite les lignes à reprendre, et si elles remplacent votre minuterie ou s'y ajoutent."
        '
        'ListViewCollectives
        '
        Me.ListViewCollectives.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColNom, Me.ColDate, Me.ColLignes})
        Me.ListViewCollectives.FullRowSelect = True
        Me.ListViewCollectives.HideSelection = False
        Me.ListViewCollectives.Location = New System.Drawing.Point(20, 74)
        Me.ListViewCollectives.MultiSelect = False
        Me.ListViewCollectives.Name = "ListViewCollectives"
        Me.ListViewCollectives.Size = New System.Drawing.Size(660, 300)
        Me.ListViewCollectives.TabIndex = 1
        Me.ListViewCollectives.UseCompatibleStateImageBehavior = False
        Me.ListViewCollectives.View = System.Windows.Forms.View.Details
        '
        'ColNom
        '
        Me.ColNom.Text = "Minuterie collective"
        Me.ColNom.Width = 330
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
        'ButtonOk
        '
        Me.ButtonOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOk.Location = New System.Drawing.Point(440, 390)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(116, 42)
        Me.ButtonOk.TabIndex = 2
        Me.ButtonOk.Text = "Choisir"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(564, 390)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(116, 42)
        Me.ButtonAnnuler.TabIndex = 3
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormChoixCollective
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(700, 450)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.ListViewCollectives)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormChoixCollective"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Minuteries collectives"
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents LabelAide As System.Windows.Forms.Label
    Friend WithEvents ListViewCollectives As System.Windows.Forms.ListView
    Friend WithEvents ColNom As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColDate As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColLignes As System.Windows.Forms.ColumnHeader
    Friend WithEvents ButtonOk As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
