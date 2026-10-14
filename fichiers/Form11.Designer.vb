<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()>
Partial Class Form11
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

    'REMARQUE : la procédure suivante est requise par le Concepteur Windows Form
    'Elle peut être modifiée à l'aide du Concepteur Windows Form.  
    'Ne la modifiez pas à l'aide de l'éditeur de code.
    <System.Diagnostics.DebuggerStepThrough()>
    Private Sub InitializeComponent()
        Me.GroupBox1 = New System.Windows.Forms.GroupBox()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TrackBar2 = New System.Windows.Forms.TrackBar()
        Me.Button3 = New System.Windows.Forms.Button()
        Me.GroupBox3 = New System.Windows.Forms.GroupBox()
        Me.CheckBoxSuite = New System.Windows.Forms.CheckBox()
        Me.RadioButton6 = New System.Windows.Forms.RadioButton()
        Me.RadioButton5 = New System.Windows.Forms.RadioButton()
        Me.RadioButton4 = New System.Windows.Forms.RadioButton()
        Me.RadioButton3 = New System.Windows.Forms.RadioButton()
        Me.RadioButton7 = New System.Windows.Forms.RadioButton()
        Me.RadioButton8 = New System.Windows.Forms.RadioButton()
        Me.Label11 = New System.Windows.Forms.Label()
        Me.TrackBar3 = New System.Windows.Forms.TrackBar()
        Me.CheckBox4 = New System.Windows.Forms.CheckBox()
        Me.CheckBoxPipPersistant = New System.Windows.Forms.CheckBox()
        Me.Button4 = New System.Windows.Forms.Button()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.VlcControl1 = New Vlc.DotNet.Forms.VlcControl()
        Me.GroupBox1.SuspendLayout()
        CType(Me.TrackBar2, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox3.SuspendLayout()
        CType(Me.TrackBar3, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.VlcControl1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'GroupBox1
        '
        Me.GroupBox1.BackColor = System.Drawing.Color.LightGray
        Me.GroupBox1.Controls.Add(Me.Label1)
        Me.GroupBox1.Controls.Add(Me.TrackBar2)
        Me.GroupBox1.Controls.Add(Me.Button3)
        Me.GroupBox1.Location = New System.Drawing.Point(13, 227)
        Me.GroupBox1.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.GroupBox1.Name = "GroupBox1"
        Me.GroupBox1.Padding = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.GroupBox1.Size = New System.Drawing.Size(178, 20)
        Me.GroupBox1.TabIndex = 14
        Me.GroupBox1.TabStop = False
        Me.GroupBox1.Visible = False
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label1.Location = New System.Drawing.Point(143, 2)
        Me.Label1.Margin = New System.Windows.Forms.Padding(1, 0, 1, 0)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(27, 15)
        Me.Label1.TabIndex = 23
        Me.Label1.Text = "100"
        Me.Label1.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'TrackBar2
        '
        Me.TrackBar2.AutoSize = False
        Me.TrackBar2.LargeChange = 1
        Me.TrackBar2.Location = New System.Drawing.Point(32, 1)
        Me.TrackBar2.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.TrackBar2.Maximum = 100
        Me.TrackBar2.Name = "TrackBar2"
        Me.TrackBar2.Size = New System.Drawing.Size(109, 19)
        Me.TrackBar2.TabIndex = 22
        Me.TrackBar2.TickFrequency = 10
        '
        'Button3
        '
        Me.Button3.Location = New System.Drawing.Point(3, 0)
        Me.Button3.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.Button3.Name = "Button3"
        Me.Button3.Size = New System.Drawing.Size(31, 20)
        Me.Button3.TabIndex = 2
        Me.Button3.Text = "Vm"
        Me.Button3.UseVisualStyleBackColor = True
        '
        'CheckBoxSuite
        '
        Me.CheckBoxSuite.AutoSize = True
        Me.CheckBoxSuite.Location = New System.Drawing.Point(196, 247)
        Me.CheckBoxSuite.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.CheckBoxSuite.Name = "CheckBoxSuite"
        Me.CheckBoxSuite.Size = New System.Drawing.Size(120, 17)
        Me.CheckBoxSuite.TabIndex = 60
        Me.CheckBoxSuite.Text = "Enchaîner à la suite"
        Me.CheckBoxSuite.UseVisualStyleBackColor = True
        '
        'GroupBox3
        '
        Me.GroupBox3.Controls.Add(Me.RadioButton6)
        Me.GroupBox3.Controls.Add(Me.RadioButton5)
        Me.GroupBox3.Controls.Add(Me.RadioButton4)
        Me.GroupBox3.Controls.Add(Me.RadioButton3)
        Me.GroupBox3.Controls.Add(Me.RadioButton7)
        Me.GroupBox3.Controls.Add(Me.RadioButton8)
        Me.GroupBox3.Location = New System.Drawing.Point(16, 266)
        Me.GroupBox3.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.GroupBox3.Name = "GroupBox3"
        Me.GroupBox3.Padding = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.GroupBox3.Size = New System.Drawing.Size(159, 73)
        Me.GroupBox3.TabIndex = 36
        Me.GroupBox3.TabStop = False
        Me.GroupBox3.Text = "Pip Position"
        '
        'RadioButton6
        '
        Me.RadioButton6.AutoSize = True
        Me.RadioButton6.Checked = True
        Me.RadioButton6.Location = New System.Drawing.Point(88, 52)
        Me.RadioButton6.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.RadioButton6.Name = "RadioButton6"
        Me.RadioButton6.Size = New System.Drawing.Size(58, 17)
        Me.RadioButton6.TabIndex = 3
        Me.RadioButton6.TabStop = True
        Me.RadioButton6.Text = "Bdroite"
        Me.RadioButton6.UseVisualStyleBackColor = True
        '
        'RadioButton5
        '
        Me.RadioButton5.AutoSize = True
        Me.RadioButton5.Location = New System.Drawing.Point(88, 16)
        Me.RadioButton5.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.RadioButton5.Name = "RadioButton5"
        Me.RadioButton5.Size = New System.Drawing.Size(59, 17)
        Me.RadioButton5.TabIndex = 2
        Me.RadioButton5.Text = "Hdroite"
        Me.RadioButton5.UseVisualStyleBackColor = True
        '
        'RadioButton4
        '
        Me.RadioButton4.AutoSize = True
        Me.RadioButton4.Location = New System.Drawing.Point(11, 52)
        Me.RadioButton4.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.RadioButton4.Name = "RadioButton4"
        Me.RadioButton4.Size = New System.Drawing.Size(68, 17)
        Me.RadioButton4.TabIndex = 1
        Me.RadioButton4.Text = "Bgauche"
        Me.RadioButton4.UseVisualStyleBackColor = True
        '
        'RadioButton7
        '
        Me.RadioButton7.AutoSize = True
        Me.RadioButton7.Location = New System.Drawing.Point(11, 34)
        Me.RadioButton7.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.RadioButton7.Name = "RadioButton7"
        Me.RadioButton7.Size = New System.Drawing.Size(68, 17)
        Me.RadioButton7.TabIndex = 4
        Me.RadioButton7.Text = "Mgauche"
        Me.RadioButton7.UseVisualStyleBackColor = True
        '
        'RadioButton8
        '
        Me.RadioButton8.AutoSize = True
        Me.RadioButton8.Location = New System.Drawing.Point(88, 34)
        Me.RadioButton8.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.RadioButton8.Name = "RadioButton8"
        Me.RadioButton8.Size = New System.Drawing.Size(59, 17)
        Me.RadioButton8.TabIndex = 5
        Me.RadioButton8.Text = "Mdroite"
        Me.RadioButton8.UseVisualStyleBackColor = True
        '
        'RadioButton3
        '
        Me.RadioButton3.AutoSize = True
        Me.RadioButton3.Location = New System.Drawing.Point(11, 16)
        Me.RadioButton3.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.RadioButton3.Name = "RadioButton3"
        Me.RadioButton3.Size = New System.Drawing.Size(69, 17)
        Me.RadioButton3.TabIndex = 0
        Me.RadioButton3.Text = "Hgauche"
        Me.RadioButton3.UseVisualStyleBackColor = True
        '
        'Label11
        '
        Me.Label11.AutoSize = True
        Me.Label11.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label11.Location = New System.Drawing.Point(331, 229)
        Me.Label11.Margin = New System.Windows.Forms.Padding(1, 0, 1, 0)
        Me.Label11.Name = "Label11"
        Me.Label11.Size = New System.Drawing.Size(27, 15)
        Me.Label11.TabIndex = 33
        Me.Label11.Text = "100"
        Me.Label11.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'TrackBar3
        '
        Me.TrackBar3.AutoSize = False
        Me.TrackBar3.LargeChange = 1
        Me.TrackBar3.Location = New System.Drawing.Point(233, 229)
        Me.TrackBar3.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.TrackBar3.Maximum = 100
        Me.TrackBar3.Name = "TrackBar3"
        Me.TrackBar3.Size = New System.Drawing.Size(100, 19)
        Me.TrackBar3.TabIndex = 34
        Me.TrackBar3.TickFrequency = 10
        Me.TrackBar3.Value = 15
        '
        'CheckBox4
        '
        Me.CheckBox4.AutoSize = True
        Me.CheckBox4.Location = New System.Drawing.Point(196, 229)
        Me.CheckBox4.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.CheckBox4.Name = "CheckBox4"
        Me.CheckBox4.Size = New System.Drawing.Size(41, 17)
        Me.CheckBox4.TabIndex = 35
        Me.CheckBox4.Text = "Pip"
        Me.CheckBox4.UseVisualStyleBackColor = True
        '
        'CheckBoxPipPersistant
        '
        Me.CheckBoxPipPersistant.AutoSize = True
        Me.CheckBoxPipPersistant.Location = New System.Drawing.Point(196, 266)
        Me.CheckBoxPipPersistant.Margin = New System.Windows.Forms.Padding(2, 2, 2, 2)
        Me.CheckBoxPipPersistant.Name = "CheckBoxPipPersistant"
        Me.CheckBoxPipPersistant.Size = New System.Drawing.Size(98, 17)
        Me.CheckBoxPipPersistant.TabIndex = 40
        Me.CheckBoxPipPersistant.Text = "Rester affiché"
        Me.CheckBoxPipPersistant.UseVisualStyleBackColor = True
        '
        'Button4
        '
        Me.Button4.Location = New System.Drawing.Point(183, 296)
        Me.Button4.Margin = New System.Windows.Forms.Padding(2, 1, 2, 1)
        Me.Button4.Name = "Button4"
        Me.Button4.Size = New System.Drawing.Size(76, 23)
        Me.Button4.TabIndex = 38
        Me.Button4.Text = "Annuler"
        Me.Button4.UseVisualStyleBackColor = True
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(277, 298)
        Me.Button2.Margin = New System.Windows.Forms.Padding(2, 1, 2, 1)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(76, 23)
        Me.Button2.TabIndex = 37
        Me.Button2.Text = "OK"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'VlcControl1
        '
        Me.VlcControl1.BackColor = System.Drawing.Color.Black
        Me.VlcControl1.ImeMode = System.Windows.Forms.ImeMode.NoControl
        Me.VlcControl1.Location = New System.Drawing.Point(13, 7)
        Me.VlcControl1.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.VlcControl1.Name = "VlcControl1"
        Me.VlcControl1.Size = New System.Drawing.Size(341, 208)
        Me.VlcControl1.Spu = -1
        Me.VlcControl1.TabIndex = 0
        Me.VlcControl1.Text = "VlcControl1"
        Me.VlcControl1.VlcLibDirectory = Nothing
        Me.VlcControl1.VlcMediaplayerOptions = Nothing
        '
        'Form11
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(6.0!, 13.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(371, 372)
        Me.Controls.Add(Me.VlcControl1)
        Me.Controls.Add(Me.Button4)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.CheckBoxSuite)
        Me.Controls.Add(Me.GroupBox3)
        Me.Controls.Add(Me.Label11)
        Me.Controls.Add(Me.TrackBar3)
        Me.Controls.Add(Me.CheckBoxPipPersistant)
        Me.Controls.Add(Me.CheckBox4)
        Me.Controls.Add(Me.GroupBox1)
        Me.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle
        Me.KeyPreview = True
        Me.Margin = New System.Windows.Forms.Padding(1, 1, 1, 1)
        Me.Name = "Form11"
        Me.Text = "PreviewTV"
        Me.GroupBox1.ResumeLayout(False)
        Me.GroupBox1.PerformLayout()
        CType(Me.TrackBar2, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox3.ResumeLayout(False)
        Me.GroupBox3.PerformLayout()
        CType(Me.TrackBar3, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.VlcControl1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents GroupBox1 As GroupBox
    Friend WithEvents Label1 As Label
    Friend WithEvents TrackBar2 As TrackBar
    Friend WithEvents Button3 As Button
    Friend WithEvents GroupBox3 As GroupBox
    Friend WithEvents CheckBoxSuite As System.Windows.Forms.CheckBox
    Friend WithEvents RadioButton6 As RadioButton
    Friend WithEvents RadioButton5 As RadioButton
    Friend WithEvents RadioButton4 As RadioButton
    Friend WithEvents RadioButton3 As RadioButton
    Friend WithEvents RadioButton7 As System.Windows.Forms.RadioButton
    Friend WithEvents RadioButton8 As System.Windows.Forms.RadioButton
    Friend WithEvents Label11 As Label
    Friend WithEvents TrackBar3 As TrackBar
    Friend WithEvents CheckBox4 As CheckBox
    Friend WithEvents CheckBoxPipPersistant As CheckBox
    Friend WithEvents Button4 As Button
    Friend WithEvents Button2 As Button
    Friend WithEvents VlcControl1 As Vlc.DotNet.Forms.VlcControl
End Class
