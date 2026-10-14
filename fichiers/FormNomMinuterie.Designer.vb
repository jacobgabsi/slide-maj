<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormNomMinuterie
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
        Me.LabelExistantes = New System.Windows.Forms.Label()
        Me.ListViewExistantes = New System.Windows.Forms.ListView()
        Me.ColNom = New System.Windows.Forms.ColumnHeader()
        Me.ColDate = New System.Windows.Forms.ColumnHeader()
        Me.ColLignes = New System.Windows.Forms.ColumnHeader()
        Me.LabelNom = New System.Windows.Forms.Label()
        Me.TextBoxNom = New System.Windows.Forms.TextBox()
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
        Me.LabelAide.Text = "Nom de la minuterie."
        '
        'LabelExistantes
        '
        Me.LabelExistantes.AutoSize = True
        Me.LabelExistantes.Location = New System.Drawing.Point(20, 74)
        Me.LabelExistantes.Name = "LabelExistantes"
        Me.LabelExistantes.Size = New System.Drawing.Size(200, 20)
        Me.LabelExistantes.TabIndex = 1
        Me.LabelExistantes.Text = "Déjà dans ce répertoire :"
        '
        'ListViewExistantes
        '
        Me.ListViewExistantes.Columns.AddRange(New System.Windows.Forms.ColumnHeader() {Me.ColNom, Me.ColDate, Me.ColLignes})
        Me.ListViewExistantes.FullRowSelect = True
        Me.ListViewExistantes.HideSelection = False
        Me.ListViewExistantes.Location = New System.Drawing.Point(20, 100)
        Me.ListViewExistantes.MultiSelect = False
        Me.ListViewExistantes.Name = "ListViewExistantes"
        Me.ListViewExistantes.Size = New System.Drawing.Size(660, 220)
        Me.ListViewExistantes.TabIndex = 2
        Me.ListViewExistantes.UseCompatibleStateImageBehavior = False
        Me.ListViewExistantes.View = System.Windows.Forms.View.Details
        '
        'ColNom
        '
        Me.ColNom.Text = "Minuterie"
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
        'LabelNom
        '
        Me.LabelNom.AutoSize = True
        Me.LabelNom.Location = New System.Drawing.Point(20, 340)
        Me.LabelNom.Name = "LabelNom"
        Me.LabelNom.Size = New System.Drawing.Size(48, 20)
        Me.LabelNom.TabIndex = 3
        Me.LabelNom.Text = "Nom :"
        '
        'TextBoxNom
        '
        Me.TextBoxNom.Location = New System.Drawing.Point(80, 336)
        Me.TextBoxNom.Name = "TextBoxNom"
        Me.TextBoxNom.Size = New System.Drawing.Size(600, 27)
        Me.TextBoxNom.TabIndex = 4
        '
        'ButtonOk
        '
        Me.ButtonOk.DialogResult = System.Windows.Forms.DialogResult.OK
        Me.ButtonOk.Location = New System.Drawing.Point(440, 390)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(116, 42)
        Me.ButtonOk.TabIndex = 5
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonAnnuler
        '
        Me.ButtonAnnuler.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonAnnuler.Location = New System.Drawing.Point(564, 390)
        Me.ButtonAnnuler.Name = "ButtonAnnuler"
        Me.ButtonAnnuler.Size = New System.Drawing.Size(116, 42)
        Me.ButtonAnnuler.TabIndex = 6
        Me.ButtonAnnuler.Text = "Annuler"
        Me.ButtonAnnuler.UseVisualStyleBackColor = True
        '
        'FormNomMinuterie
        '
        Me.AcceptButton = Me.ButtonOk
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.CancelButton = Me.ButtonAnnuler
        Me.ClientSize = New System.Drawing.Size(700, 450)
        Me.Controls.Add(Me.ButtonAnnuler)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.TextBoxNom)
        Me.Controls.Add(Me.LabelNom)
        Me.Controls.Add(Me.ListViewExistantes)
        Me.Controls.Add(Me.LabelExistantes)
        Me.Controls.Add(Me.LabelAide)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormNomMinuterie"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Enregistrer la minuterie"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAide As System.Windows.Forms.Label
    Friend WithEvents LabelExistantes As System.Windows.Forms.Label
    Friend WithEvents ListViewExistantes As System.Windows.Forms.ListView
    Friend WithEvents ColNom As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColDate As System.Windows.Forms.ColumnHeader
    Friend WithEvents ColLignes As System.Windows.Forms.ColumnHeader
    Friend WithEvents LabelNom As System.Windows.Forms.Label
    Friend WithEvents TextBoxNom As System.Windows.Forms.TextBox
    Friend WithEvents ButtonOk As System.Windows.Forms.Button
    Friend WithEvents ButtonAnnuler As System.Windows.Forms.Button
End Class
