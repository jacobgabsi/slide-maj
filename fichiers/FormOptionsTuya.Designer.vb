<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class FormOptionsTuya
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
        Me.LabelAppareil = New System.Windows.Forms.Label()
        Me.PanelOptions = New System.Windows.Forms.FlowLayoutPanel()
        Me.LabelEtat = New System.Windows.Forms.Label()
        Me.ButtonFermer = New System.Windows.Forms.Button()
        Me.ButtonOk = New System.Windows.Forms.Button()
        Me.CheckBoxInstantane = New System.Windows.Forms.CheckBox()
        Me.SuspendLayout()
        '
        'LabelAppareil
        '
        Me.LabelAppareil.AutoSize = True
        Me.LabelAppareil.Font = New System.Drawing.Font("Segoe UI Semibold", 10.0!, System.Drawing.FontStyle.Bold)
        Me.LabelAppareil.Location = New System.Drawing.Point(16, 14)
        Me.LabelAppareil.Name = "LabelAppareil"
        Me.LabelAppareil.Size = New System.Drawing.Size(80, 23)
        Me.LabelAppareil.TabIndex = 0
        Me.LabelAppareil.Text = "Appareil"
        '
        'PanelOptions
        '
        Me.PanelOptions.AutoScroll = True
        Me.PanelOptions.FlowDirection = System.Windows.Forms.FlowDirection.TopDown
        Me.PanelOptions.Location = New System.Drawing.Point(16, 52)
        Me.PanelOptions.Name = "PanelOptions"
        Me.PanelOptions.Size = New System.Drawing.Size(560, 300)
        Me.PanelOptions.TabIndex = 1
        Me.PanelOptions.WrapContents = False
        '
        'LabelEtat
        '
        Me.LabelEtat.AutoEllipsis = True
        Me.LabelEtat.Location = New System.Drawing.Point(16, 398)
        Me.LabelEtat.Name = "LabelEtat"
        Me.LabelEtat.Size = New System.Drawing.Size(300, 32)
        Me.LabelEtat.TabIndex = 2
        '
        'CheckBoxInstantane
        '
        Me.CheckBoxInstantane.AutoSize = True
        Me.CheckBoxInstantane.Location = New System.Drawing.Point(16, 360)
        Me.CheckBoxInstantane.Name = "CheckBoxInstantane"
        Me.CheckBoxInstantane.Size = New System.Drawing.Size(400, 24)
        Me.CheckBoxInstantane.TabIndex = 5
        Me.CheckBoxInstantane.Text = "Appliquer les réglages instantanément"
        Me.CheckBoxInstantane.UseVisualStyleBackColor = True
        '
        'ButtonOk
        '
        Me.ButtonOk.Location = New System.Drawing.Point(328, 394)
        Me.ButtonOk.Name = "ButtonOk"
        Me.ButtonOk.Size = New System.Drawing.Size(120, 36)
        Me.ButtonOk.TabIndex = 4
        Me.ButtonOk.Text = "OK"
        Me.ButtonOk.UseVisualStyleBackColor = True
        '
        'ButtonFermer
        '
        Me.ButtonFermer.DialogResult = System.Windows.Forms.DialogResult.Cancel
        Me.ButtonFermer.Location = New System.Drawing.Point(456, 394)
        Me.ButtonFermer.Name = "ButtonFermer"
        Me.ButtonFermer.Size = New System.Drawing.Size(120, 36)
        Me.ButtonFermer.TabIndex = 3
        Me.ButtonFermer.Text = "Fermer"
        Me.ButtonFermer.UseVisualStyleBackColor = True
        '
        'FormOptionsTuya
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(9.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.AcceptButton = Me.ButtonOk
        Me.CancelButton = Me.ButtonFermer
        Me.ClientSize = New System.Drawing.Size(594, 446)
        Me.Controls.Add(Me.CheckBoxInstantane)
        Me.Controls.Add(Me.ButtonOk)
        Me.Controls.Add(Me.ButtonFermer)
        Me.Controls.Add(Me.LabelEtat)
        Me.Controls.Add(Me.PanelOptions)
        Me.Controls.Add(Me.LabelAppareil)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog
        Me.MaximizeBox = False
        Me.MinimizeBox = False
        Me.Name = "FormOptionsTuya"
        Me.ShowInTaskbar = False
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent
        Me.Text = "Options de l'appareil Tuya"
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub

    Friend WithEvents LabelAppareil As System.Windows.Forms.Label
    Friend WithEvents PanelOptions As System.Windows.Forms.FlowLayoutPanel
    Friend WithEvents LabelEtat As System.Windows.Forms.Label
    Friend WithEvents ButtonFermer As System.Windows.Forms.Button
    Friend WithEvents ButtonOk As System.Windows.Forms.Button
    Friend WithEvents CheckBoxInstantane As System.Windows.Forms.CheckBox
End Class
