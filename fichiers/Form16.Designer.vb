<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form16
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

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.  
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()> _
    Private Sub InitializeComponent()
        Me.GroupBox12 = New System.Windows.Forms.GroupBox()
        Me.CheckBox32 = New System.Windows.Forms.CheckBox()
        Me.TextBox28 = New System.Windows.Forms.TextBox()
        Me.Label58 = New System.Windows.Forms.Label()
        Me.TextBox27 = New System.Windows.Forms.TextBox()
        Me.Label57 = New System.Windows.Forms.Label()
        Me.TextBox26 = New System.Windows.Forms.TextBox()
        Me.Label56 = New System.Windows.Forms.Label()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.CheckBox1 = New System.Windows.Forms.CheckBox()
        Me.GroupBox12.SuspendLayout()
        Me.SuspendLayout()
        '
        'GroupBox12
        '
        Me.GroupBox12.Controls.Add(Me.CheckBox1)
        Me.GroupBox12.Controls.Add(Me.CheckBox32)
        Me.GroupBox12.Controls.Add(Me.TextBox28)
        Me.GroupBox12.Controls.Add(Me.Label58)
        Me.GroupBox12.Controls.Add(Me.TextBox27)
        Me.GroupBox12.Controls.Add(Me.Label57)
        Me.GroupBox12.Controls.Add(Me.TextBox26)
        Me.GroupBox12.Controls.Add(Me.Label56)
        Me.GroupBox12.Location = New System.Drawing.Point(12, 28)
        Me.GroupBox12.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.GroupBox12.Name = "GroupBox12"
        Me.GroupBox12.Padding = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.GroupBox12.Size = New System.Drawing.Size(669, 135)
        Me.GroupBox12.TabIndex = 48
        Me.GroupBox12.TabStop = False
        Me.GroupBox12.Text = "RDV (prioritaire sur tous les réglages)"
        '
        'CheckBox32
        '
        Me.CheckBox32.AutoSize = True
        Me.CheckBox32.Location = New System.Drawing.Point(15, 86)
        Me.CheckBox32.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.CheckBox32.Name = "CheckBox32"
        Me.CheckBox32.Size = New System.Drawing.Size(123, 28)
        Me.CheckBox32.TabIndex = 49
        Me.CheckBox32.Text = "En boucle"
        Me.CheckBox32.UseVisualStyleBackColor = True
        '
        'TextBox28
        '
        Me.TextBox28.Location = New System.Drawing.Point(493, 80)
        Me.TextBox28.Name = "TextBox28"
        Me.TextBox28.Size = New System.Drawing.Size(71, 29)
        Me.TextBox28.TabIndex = 48
        '
        'Label58
        '
        Me.Label58.Location = New System.Drawing.Point(361, 49)
        Me.Label58.Name = "Label58"
        Me.Label58.Size = New System.Drawing.Size(85, 28)
        Me.Label58.TabIndex = 47
        Me.Label58.Text = "Date fin"
        '
        'TextBox27
        '
        Me.TextBox27.Location = New System.Drawing.Point(339, 80)
        Me.TextBox27.Name = "TextBox27"
        Me.TextBox27.Size = New System.Drawing.Size(123, 29)
        Me.TextBox27.TabIndex = 46
        '
        'Label57
        '
        Me.Label57.Location = New System.Drawing.Point(211, 50)
        Me.Label57.Name = "Label57"
        Me.Label57.Size = New System.Drawing.Size(105, 28)
        Me.Label57.TabIndex = 45
        Me.Label57.Text = "Date début"
        '
        'TextBox26
        '
        Me.TextBox26.Location = New System.Drawing.Point(205, 80)
        Me.TextBox26.Name = "TextBox26"
        Me.TextBox26.Size = New System.Drawing.Size(123, 29)
        Me.TextBox26.TabIndex = 44
        '
        'Label56
        '
        Me.Label56.Location = New System.Drawing.Point(474, 26)
        Me.Label56.Name = "Label56"
        Me.Label56.Size = New System.Drawing.Size(136, 55)
        Me.Label56.TabIndex = 42
        Me.Label56.Text = "limiter la video à x seconde"
        '
        'Button4
        '
        Me.Button4.Location = New System.Drawing.Point(122, 210)
        Me.Button4.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(210, 42)
        Me.Button4.TabIndex = 50
        Me.Button4.Text = "Annuler"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(348, 210)
        Me.Button3.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(188, 42)
        Me.Button3.TabIndex = 49
        Me.Button3.Text = "OK"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'CheckBox1
        '
        Me.CheckBox1.AutoSize = True
        Me.CheckBox1.Location = New System.Drawing.Point(15, 45)
        Me.CheckBox1.Margin = New System.Windows.Forms.Padding(3, 4, 3, 4)
        Me.CheckBox1.Name = "CheckBox1"
        Me.CheckBox1.Size = New System.Drawing.Size(173, 28)
        Me.CheckBox1.TabIndex = 50
        Me.CheckBox1.Text = "Mode RDV OFF"
        Me.CheckBox1.UseVisualStyleBackColor = True
        '
        'Form16
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(11.0!, 24.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(693, 284)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Button3)
        Me.Controls.Add(Me.GroupBox12)
        Me.Name = "Form16"
        Me.Text = "Mode RDV"
        Me.GroupBox12.ResumeLayout(False)
        Me.GroupBox12.PerformLayout()
        Me.ResumeLayout(False)

    End Sub

    Friend WithEvents GroupBox12 As GroupBox
    Friend WithEvents CheckBox32 As CheckBox
    Friend WithEvents TextBox28 As TextBox
    Friend WithEvents Label58 As Label
    Friend WithEvents TextBox27 As TextBox
    Friend WithEvents Label57 As Label
    Friend WithEvents TextBox26 As TextBox
    Friend WithEvents Label56 As Label
    Friend WithEvents Button4 As Button
    Friend WithEvents Button3 As Button
    Friend WithEvents CheckBox1 As CheckBox
End Class
