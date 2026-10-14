<Global.Microsoft.VisualBasic.CompilerServices.DesignerGenerated()> _
Partial Class Form14
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
        Me.components = New System.ComponentModel.Container()
        Me.DataGridView2 = New System.Windows.Forms.DataGridView()
        Me.Button1 = New System.Windows.Forms.Button()
        Me.PictureBox1 = New System.Windows.Forms.PictureBox()
        Me.VlcControl1 = New Vlc.DotNet.Forms.VlcControl()
        Me.GroupBox2 = New System.Windows.Forms.GroupBox()
        Me.Button6 = New System.Windows.Forms.Button()
        Me.TrackBar1 = New System.Windows.Forms.TrackBar()
        Me.Label8 = New System.Windows.Forms.Label()
        Me.Label9 = New System.Windows.Forms.Label()
        Me.Label10 = New System.Windows.Forms.Label()
        Me.TrackBar2 = New System.Windows.Forms.TrackBar()
        Me.Button2 = New System.Windows.Forms.Button()
        Me.Timer1 = New System.Windows.Forms.Timer(Me.components)
        Me.Timer2 = New System.Windows.Forms.Timer(Me.components)
        Me.ListBox1 = New System.Windows.Forms.ListBox()
        Me.LabelSourceIptv = New System.Windows.Forms.Label()
        Me.ComboBoxSource = New System.Windows.Forms.ComboBox()
        Me.LabelContenuIptv = New System.Windows.Forms.Label()
        Me.ComboBoxContenu = New System.Windows.Forms.ComboBox()
        Me.ButtonTelecharger = New System.Windows.Forms.Button()
        Me.ButtonTelechargements = New System.Windows.Forms.Button()
        Me.TextBoxPitch = New System.Windows.Forms.TextBox()
        Me.ButtonRechercher = New System.Windows.Forms.Button()
        Me.ButtonRazRecherche = New System.Windows.Forms.Button()
        Me.ButtonTriGroupes = New System.Windows.Forms.Button()
        Me.LabelTelechargement = New System.Windows.Forms.Label()
        Me.ProgressBarTelechargement = New System.Windows.Forms.ProgressBar()
        Me.Label1 = New System.Windows.Forms.Label()
        Me.TextBox1 = New System.Windows.Forms.TextBox()
        Me.TextBox2 = New System.Windows.Forms.TextBox()
        Me.DataGridViewTextBoxColumn1 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DataGridViewTextBoxColumn2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DataGridViewTextBoxColumn3 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Titre = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column1 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.Column2 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.DataGridViewTextBoxColumn4 = New System.Windows.Forms.DataGridViewTextBoxColumn()
        Me.ColonneContenu = New System.Windows.Forms.DataGridViewTextBoxColumn()
        CType(Me.DataGridView2, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.VlcControl1, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.GroupBox2.SuspendLayout()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).BeginInit()
        CType(Me.TrackBar2, System.ComponentModel.ISupportInitialize).BeginInit()
        Me.SuspendLayout()
        '
        'DataGridView2
        '
        Me.DataGridView2.AllowUserToAddRows = False
        Me.DataGridView2.AllowUserToDeleteRows = False
        Me.DataGridView2.AllowUserToOrderColumns = True
        Me.DataGridView2.AutoSizeRowsMode = System.Windows.Forms.DataGridViewAutoSizeRowsMode.AllCells
        Me.DataGridView2.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize
        Me.DataGridView2.Columns.AddRange(New System.Windows.Forms.DataGridViewColumn() {Me.Titre, Me.Column1, Me.Column2, Me.ColonneContenu})
        Me.DataGridView2.Location = New System.Drawing.Point(404, 50)
        Me.DataGridView2.Margin = New System.Windows.Forms.Padding(2)
        Me.DataGridView2.MultiSelect = True
        Me.DataGridView2.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.DataGridView2.Name = "DataGridView2"
        Me.DataGridView2.ReadOnly = True
        Me.DataGridView2.RowHeadersVisible = False
        Me.DataGridView2.RowHeadersWidth = 82
        Me.DataGridView2.RowTemplate.Height = 33
        Me.DataGridView2.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect
        Me.DataGridView2.Size = New System.Drawing.Size(580, 674)
        Me.DataGridView2.TabIndex = 1
        '
        'Button1
        '
        Me.Button1.Location = New System.Drawing.Point(1368, 648)
        Me.Button1.Margin = New System.Windows.Forms.Padding(2)
        Me.Button1.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button1.Name = "Button1"
        Me.Button1.Size = New System.Drawing.Size(150, 38)
        Me.Button1.TabIndex = 2
        Me.Button1.Text = "Annuler"
        Me.Button1.UseVisualStyleBackColor = True
        '
        'PictureBox1
        '
        Me.PictureBox1.Location = New System.Drawing.Point(996, 10)
        Me.PictureBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.PictureBox1.Name = "PictureBox1"
        Me.PictureBox1.Size = New System.Drawing.Size(732, 360)
        Me.PictureBox1.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom
        Me.PictureBox1.TabIndex = 3
        Me.PictureBox1.TabStop = False
        '
        'VlcControl1
        '
        Me.VlcControl1.BackColor = System.Drawing.Color.Black
        Me.VlcControl1.ImeMode = System.Windows.Forms.ImeMode.[On]
        Me.VlcControl1.Location = New System.Drawing.Point(996, 10)
        Me.VlcControl1.Margin = New System.Windows.Forms.Padding(2)
        Me.VlcControl1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.VlcControl1.Name = "VlcControl1"
        Me.VlcControl1.Size = New System.Drawing.Size(732, 360)
        Me.VlcControl1.Spu = -1
        Me.VlcControl1.TabIndex = 13
        Me.VlcControl1.Text = "VlcControl1"
        Me.VlcControl1.Visible = False
        Me.VlcControl1.VlcLibDirectory = Nothing
        Me.VlcControl1.VlcMediaplayerOptions = Nothing
        '
        'GroupBox2
        '
        Me.GroupBox2.BackColor = System.Drawing.Color.LightGray
        Me.GroupBox2.Controls.Add(Me.Button6)
        Me.GroupBox2.Controls.Add(Me.TrackBar1)
        Me.GroupBox2.Controls.Add(Me.Label8)
        Me.GroupBox2.Controls.Add(Me.Label9)
        Me.GroupBox2.Controls.Add(Me.Label10)
        Me.GroupBox2.Controls.Add(Me.TrackBar2)
        Me.GroupBox2.Location = New System.Drawing.Point(996, 376)
        Me.GroupBox2.Margin = New System.Windows.Forms.Padding(2)
        Me.GroupBox2.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.GroupBox2.Name = "GroupBox2"
        Me.GroupBox2.Padding = New System.Windows.Forms.Padding(2)
        Me.GroupBox2.Size = New System.Drawing.Size(732, 32)
        Me.GroupBox2.TabIndex = 32
        Me.GroupBox2.TabStop = False
        Me.GroupBox2.Visible = False
        '
        'Button6
        '
        Me.Button6.Location = New System.Drawing.Point(0, 0)
        Me.Button6.Margin = New System.Windows.Forms.Padding(2)
        Me.Button6.Name = "Button6"
        Me.Button6.Size = New System.Drawing.Size(52, 31)
        Me.Button6.TabIndex = 1
        Me.Button6.Text = "II"
        Me.Button6.UseVisualStyleBackColor = True
        '
        'TrackBar1
        '
        Me.TrackBar1.AutoSize = False
        Me.TrackBar1.LargeChange = 60
        Me.TrackBar1.Location = New System.Drawing.Point(143, 2)
        Me.TrackBar1.Maximum = 100
        Me.TrackBar1.Name = "TrackBar1"
        Me.TrackBar1.Size = New System.Drawing.Size(323, 29)
        Me.TrackBar1.TabIndex = 26
        Me.TrackBar1.TickFrequency = 10
        '
        'Label8
        '
        Me.Label8.AutoSize = True
        Me.Label8.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label8.Location = New System.Drawing.Point(57, 3)
        Me.Label8.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label8.Name = "Label8"
        Me.Label8.Size = New System.Drawing.Size(75, 22)
        Me.Label8.TabIndex = 25
        Me.Label8.Text = "00:00:00"
        Me.Label8.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'Label9
        '
        Me.Label9.AutoSize = True
        Me.Label9.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label9.Location = New System.Drawing.Point(472, 6)
        Me.Label9.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label9.Name = "Label9"
        Me.Label9.Size = New System.Drawing.Size(75, 22)
        Me.Label9.TabIndex = 24
        Me.Label9.Text = "00:00:00"
        Me.Label9.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'Label10
        '
        Me.Label10.AutoSize = True
        Me.Label10.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle
        Me.Label10.Location = New System.Drawing.Point(692, 5)
        Me.Label10.Margin = New System.Windows.Forms.Padding(2, 0, 2, 0)
        Me.Label10.Name = "Label10"
        Me.Label10.Size = New System.Drawing.Size(38, 22)
        Me.Label10.TabIndex = 23
        Me.Label10.Text = "100"
        Me.Label10.TextAlign = System.Drawing.ContentAlignment.MiddleRight
        '
        'TrackBar2
        '
        Me.TrackBar2.AutoSize = False
        Me.TrackBar2.LargeChange = 1
        Me.TrackBar2.Location = New System.Drawing.Point(552, 3)
        Me.TrackBar2.Maximum = 100
        Me.TrackBar2.Name = "TrackBar2"
        Me.TrackBar2.Size = New System.Drawing.Size(133, 29)
        Me.TrackBar2.TabIndex = 22
        Me.TrackBar2.TickFrequency = 10
        '
        'Button2
        '
        Me.Button2.Location = New System.Drawing.Point(1524, 648)
        Me.Button2.Margin = New System.Windows.Forms.Padding(2)
        Me.Button2.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.Button2.Name = "Button2"
        Me.Button2.Size = New System.Drawing.Size(204, 38)
        Me.Button2.TabIndex = 33
        Me.Button2.Text = "OK"
        Me.Button2.UseVisualStyleBackColor = True
        '
        'Timer1
        '
        Me.Timer1.Interval = 1000
        '
        'Timer2
        '
        Me.Timer2.Interval = 1000
        '
        'LabelSourceIptv
        '
        Me.LabelSourceIptv.AutoSize = True
        Me.LabelSourceIptv.Location = New System.Drawing.Point(12, 16)
        Me.LabelSourceIptv.Name = "LabelSourceIptv"
        Me.LabelSourceIptv.Size = New System.Drawing.Size(60, 20)
        Me.LabelSourceIptv.TabIndex = 40
        Me.LabelSourceIptv.Text = "Source :"
        '
        'ComboBoxSource
        '
        Me.ComboBoxSource.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxSource.FormattingEnabled = True
        Me.ComboBoxSource.Location = New System.Drawing.Point(84, 10)
        Me.ComboBoxSource.Name = "ComboBoxSource"
        Me.ComboBoxSource.Size = New System.Drawing.Size(308, 28)
        Me.ComboBoxSource.TabIndex = 41
        '
        'LabelContenuIptv
        '
        Me.LabelContenuIptv.AutoSize = True
        Me.LabelContenuIptv.Location = New System.Drawing.Point(404, 16)
        Me.LabelContenuIptv.Name = "LabelContenuIptv"
        Me.LabelContenuIptv.Size = New System.Drawing.Size(74, 20)
        Me.LabelContenuIptv.TabIndex = 42
        Me.LabelContenuIptv.Text = "Contenu :"
        Me.LabelContenuIptv.Visible = False
        '
        'ComboBoxContenu
        '
        Me.ComboBoxContenu.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList
        Me.ComboBoxContenu.FormattingEnabled = True
        Me.ComboBoxContenu.Location = New System.Drawing.Point(486, 10)
        Me.ComboBoxContenu.Name = "ComboBoxContenu"
        Me.ComboBoxContenu.Size = New System.Drawing.Size(200, 28)
        Me.ComboBoxContenu.TabIndex = 43
        Me.ComboBoxContenu.Visible = False
        '
        'ButtonTelecharger
        '
        Me.ButtonTelecharger.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonTelecharger.Location = New System.Drawing.Point(996, 648)
        Me.ButtonTelecharger.Margin = New System.Windows.Forms.Padding(2)
        Me.ButtonTelecharger.Name = "ButtonTelecharger"
        Me.ButtonTelecharger.Size = New System.Drawing.Size(170, 38)
        Me.ButtonTelecharger.TabIndex = 44
        Me.ButtonTelecharger.Text = "Télécharger"
        Me.ButtonTelecharger.UseVisualStyleBackColor = True
        '
        'ButtonTelechargements
        '
        Me.ButtonTelechargements.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ButtonTelechargements.Location = New System.Drawing.Point(1172, 648)
        Me.ButtonTelechargements.Margin = New System.Windows.Forms.Padding(2)
        Me.ButtonTelechargements.Name = "ButtonTelechargements"
        Me.ButtonTelechargements.Size = New System.Drawing.Size(190, 38)
        Me.ButtonTelechargements.TabIndex = 50
        Me.ButtonTelechargements.Text = "Téléchargements"
        Me.ButtonTelechargements.UseVisualStyleBackColor = True
        '
        'LabelTelechargement
        '
        Me.LabelTelechargement.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.LabelTelechargement.AutoEllipsis = True
        Me.LabelTelechargement.Location = New System.Drawing.Point(996, 578)
        Me.LabelTelechargement.Name = "LabelTelechargement"
        Me.LabelTelechargement.Size = New System.Drawing.Size(732, 28)
        Me.LabelTelechargement.TabIndex = 45
        Me.LabelTelechargement.Text = "Télécharger : le film ou l'épisode choisi est copié dans le dossier de votre choix."
        Me.LabelTelechargement.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        '
        'ProgressBarTelechargement
        '
        Me.ProgressBarTelechargement.Anchor = CType((System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.ProgressBarTelechargement.Location = New System.Drawing.Point(996, 610)
        Me.ProgressBarTelechargement.Name = "ProgressBarTelechargement"
        Me.ProgressBarTelechargement.Size = New System.Drawing.Size(732, 20)
        Me.ProgressBarTelechargement.TabIndex = 46
        Me.ProgressBarTelechargement.Visible = False
        '
        'TextBoxPitch
        '
        Me.TextBoxPitch.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBoxPitch.Location = New System.Drawing.Point(996, 416)
        Me.TextBoxPitch.Multiline = True
        Me.TextBoxPitch.Name = "TextBoxPitch"
        Me.TextBoxPitch.ReadOnly = True
        Me.TextBoxPitch.ScrollBars = System.Windows.Forms.ScrollBars.Vertical
        Me.TextBoxPitch.Size = New System.Drawing.Size(732, 120)
        Me.TextBoxPitch.TabIndex = 48
        '
        'ButtonRechercher
        '
        Me.ButtonRechercher.Location = New System.Drawing.Point(692, 8)
        Me.ButtonRechercher.Margin = New System.Windows.Forms.Padding(2)
        Me.ButtonRechercher.Name = "ButtonRechercher"
        Me.ButtonRechercher.Size = New System.Drawing.Size(165, 32)
        Me.ButtonRechercher.TabIndex = 49
        Me.ButtonRechercher.Text = "Rechercher  (Ctrl+F)"
        Me.ButtonRechercher.UseVisualStyleBackColor = True
        '
        'ButtonRazRecherche
        '
        Me.ButtonRazRecherche.Location = New System.Drawing.Point(861, 8)
        Me.ButtonRazRecherche.Margin = New System.Windows.Forms.Padding(2)
        Me.ButtonRazRecherche.Name = "ButtonRazRecherche"
        Me.ButtonRazRecherche.Size = New System.Drawing.Size(123, 32)
        Me.ButtonRazRecherche.TabIndex = 50
        Me.ButtonRazRecherche.Text = "RAZ recherche"
        Me.ButtonRazRecherche.UseVisualStyleBackColor = True
        '
        'ButtonTriGroupes
        '
        Me.ButtonTriGroupes.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ButtonTriGroupes.Location = New System.Drawing.Point(12, 50)
        Me.ButtonTriGroupes.Margin = New System.Windows.Forms.Padding(2)
        Me.ButtonTriGroupes.Name = "ButtonTriGroupes"
        Me.ButtonTriGroupes.Padding = New System.Windows.Forms.Padding(6, 0, 0, 0)
        Me.ButtonTriGroupes.Size = New System.Drawing.Size(380, 30)
        Me.ButtonTriGroupes.TabIndex = 47
        Me.ButtonTriGroupes.Text = "Groupes"
        Me.ButtonTriGroupes.TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        Me.ButtonTriGroupes.UseVisualStyleBackColor = True
        '
        'ListBox1
        '
        Me.ListBox1.FormattingEnabled = True
        Me.ListBox1.ItemHeight = 20
        Me.ListBox1.Location = New System.Drawing.Point(12, 80)
        Me.ListBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Bottom Or System.Windows.Forms.AnchorStyles.Left), System.Windows.Forms.AnchorStyles)
        Me.ListBox1.Name = "ListBox1"
        Me.ListBox1.Size = New System.Drawing.Size(380, 644)
        Me.ListBox1.TabIndex = 36
        '
        'Label1
        '
        Me.Label1.AutoSize = True
        Me.Label1.Location = New System.Drawing.Point(1632, 708)
        Me.Label1.Name = "Label1"
        Me.Label1.Size = New System.Drawing.Size(59, 20)
        Me.Label1.TabIndex = 37
        Me.Label1.Text = "Label1"
        Me.Label1.Visible = False
        '
        'TextBox1
        '
        Me.TextBox1.Location = New System.Drawing.Point(996, 544)
        Me.TextBox1.Margin = New System.Windows.Forms.Padding(5)
        Me.TextBox1.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox1.Name = "TextBox1"
        Me.TextBox1.Size = New System.Drawing.Size(364, 27)
        Me.TextBox1.TabIndex = 38
        Me.TextBox1.TextAlign = System.Windows.Forms.HorizontalAlignment.Right
        '
        'TextBox2
        '
        Me.TextBox2.Location = New System.Drawing.Point(1368, 544)
        Me.TextBox2.Margin = New System.Windows.Forms.Padding(5)
        Me.TextBox2.Anchor = CType((System.Windows.Forms.AnchorStyles.Top Or System.Windows.Forms.AnchorStyles.Right), System.Windows.Forms.AnchorStyles)
        Me.TextBox2.Name = "TextBox2"
        Me.TextBox2.Size = New System.Drawing.Size(360, 27)
        Me.TextBox2.TabIndex = 39
        '
        'DataGridViewTextBoxColumn1
        '
        Me.DataGridViewTextBoxColumn1.HeaderText = "Column1"
        Me.DataGridViewTextBoxColumn1.MinimumWidth = 10
        Me.DataGridViewTextBoxColumn1.Name = "DataGridViewTextBoxColumn1"
        Me.DataGridViewTextBoxColumn1.ReadOnly = True
        Me.DataGridViewTextBoxColumn1.Width = 400
        '
        'DataGridViewTextBoxColumn2
        '
        Me.DataGridViewTextBoxColumn2.HeaderText = "Column1"
        Me.DataGridViewTextBoxColumn2.MinimumWidth = 10
        Me.DataGridViewTextBoxColumn2.Name = "DataGridViewTextBoxColumn2"
        Me.DataGridViewTextBoxColumn2.ReadOnly = True
        Me.DataGridViewTextBoxColumn2.Width = 400
        '
        'DataGridViewTextBoxColumn3
        '
        Me.DataGridViewTextBoxColumn3.HeaderText = "url"
        Me.DataGridViewTextBoxColumn3.MinimumWidth = 10
        Me.DataGridViewTextBoxColumn3.Name = "DataGridViewTextBoxColumn3"
        Me.DataGridViewTextBoxColumn3.ReadOnly = True
        Me.DataGridViewTextBoxColumn3.Visible = False
        Me.DataGridViewTextBoxColumn3.Width = 200
        '
        'Titre
        '
        Me.Titre.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill
        Me.Titre.HeaderText = "Titre"
        Me.Titre.MinimumWidth = 10
        Me.Titre.Name = "Titre"
        Me.Titre.ReadOnly = True
        Me.Titre.Width = 400
        '
        'Column1
        '
        Me.Column1.HeaderText = "url"
        Me.Column1.MinimumWidth = 10
        Me.Column1.Name = "Column1"
        Me.Column1.ReadOnly = True
        Me.Column1.Visible = False
        Me.Column1.Width = 200
        '
        'Column2
        '
        Me.Column2.HeaderText = "logo"
        Me.Column2.MinimumWidth = 8
        Me.Column2.Name = "Column2"
        Me.Column2.ReadOnly = True
        Me.Column2.Visible = False
        Me.Column2.Width = 160
        '
        'ColonneContenu
        '
        Me.ColonneContenu.HeaderText = "Contenu"
        Me.ColonneContenu.MinimumWidth = 10
        Me.ColonneContenu.Name = "ColonneContenu"
        Me.ColonneContenu.ReadOnly = True
        Me.ColonneContenu.Width = 90
        '
        'DataGridViewTextBoxColumn4
        '
        Me.DataGridViewTextBoxColumn4.HeaderText = "logo"
        Me.DataGridViewTextBoxColumn4.MinimumWidth = 8
        Me.DataGridViewTextBoxColumn4.Name = "DataGridViewTextBoxColumn4"
        Me.DataGridViewTextBoxColumn4.Visible = False
        Me.DataGridViewTextBoxColumn4.Width = 160
        '
        'Form14
        '
        Me.AutoScaleDimensions = New System.Drawing.SizeF(10.0!, 20.0!)
        Me.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font
        Me.ClientSize = New System.Drawing.Size(1738, 702)
        Me.KeyPreview = True
        Me.Controls.Add(Me.TextBox2)
        Me.Controls.Add(Me.TextBox1)
        Me.Controls.Add(Me.Label1)
        Me.Controls.Add(Me.ButtonTriGroupes)
        Me.Controls.Add(Me.ButtonRazRecherche)
        Me.Controls.Add(Me.ButtonRechercher)
        Me.Controls.Add(Me.TextBoxPitch)
        Me.Controls.Add(Me.ProgressBarTelechargement)
        Me.Controls.Add(Me.LabelTelechargement)
        Me.Controls.Add(Me.ButtonTelechargements)
        Me.Controls.Add(Me.ButtonTelecharger)
        Me.Controls.Add(Me.ComboBoxContenu)
        Me.Controls.Add(Me.LabelContenuIptv)
        Me.Controls.Add(Me.ComboBoxSource)
        Me.Controls.Add(Me.LabelSourceIptv)
        Me.Controls.Add(Me.ListBox1)
        Me.Controls.Add(Me.Button2)
        Me.Controls.Add(Me.GroupBox2)
        Me.Controls.Add(Me.VlcControl1)
        Me.Controls.Add(Me.PictureBox1)
        Me.Controls.Add(Me.Button1)
        Me.Controls.Add(Me.DataGridView2)
        Me.MinimumSize = New System.Drawing.Size(1500, 700)
        Me.Margin = New System.Windows.Forms.Padding(2)
        Me.Name = "Form14"
        Me.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen
        Me.Text = "Nflix"
        CType(Me.DataGridView2, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.PictureBox1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.VlcControl1, System.ComponentModel.ISupportInitialize).EndInit()
        Me.GroupBox2.ResumeLayout(False)
        Me.GroupBox2.PerformLayout()
        CType(Me.TrackBar1, System.ComponentModel.ISupportInitialize).EndInit()
        CType(Me.TrackBar2, System.ComponentModel.ISupportInitialize).EndInit()
        Me.ResumeLayout(False)
        Me.PerformLayout()

    End Sub
    Friend WithEvents DataGridView2 As DataGridView
    Friend WithEvents Button1 As Button
    Friend WithEvents DataGridViewTextBoxColumn1 As DataGridViewTextBoxColumn
    Friend WithEvents DataGridViewTextBoxColumn2 As DataGridViewTextBoxColumn
    Friend WithEvents DataGridViewTextBoxColumn3 As DataGridViewTextBoxColumn
    Friend WithEvents PictureBox1 As PictureBox
    Friend WithEvents VlcControl1 As Vlc.DotNet.Forms.VlcControl
    Friend WithEvents GroupBox2 As GroupBox
    Friend WithEvents Button6 As Button
    Friend WithEvents TrackBar1 As TrackBar
    Friend WithEvents Label8 As Label
    Friend WithEvents Label9 As Label
    Friend WithEvents Label10 As Label
    Friend WithEvents TrackBar2 As TrackBar
    Friend WithEvents Titre As DataGridViewTextBoxColumn
    Friend WithEvents Column1 As DataGridViewTextBoxColumn
    Friend WithEvents Column2 As DataGridViewTextBoxColumn
    Friend WithEvents ColonneContenu As DataGridViewTextBoxColumn
    Friend WithEvents Button2 As Button
    Friend WithEvents DataGridViewTextBoxColumn4 As DataGridViewTextBoxColumn
    Friend WithEvents Timer1 As Timer
    Friend WithEvents Timer2 As Timer
    Friend WithEvents ListBox1 As ListBox
    Friend WithEvents LabelSourceIptv As Label
    Friend WithEvents ComboBoxSource As ComboBox
    Friend WithEvents LabelContenuIptv As Label
    Friend WithEvents ComboBoxContenu As ComboBox
    Friend WithEvents ButtonTelecharger As Button
    Friend WithEvents ButtonTelechargements As Button
    Friend WithEvents TextBoxPitch As TextBox
    Friend WithEvents ButtonRechercher As Button
    Friend WithEvents ButtonRazRecherche As System.Windows.Forms.Button
    Friend WithEvents ButtonTriGroupes As Button
    Friend WithEvents LabelTelechargement As Label
    Friend WithEvents ProgressBarTelechargement As ProgressBar
    Friend WithEvents Label1 As Label
    Friend WithEvents TextBox1 As TextBox
    Friend WithEvents TextBox2 As TextBox
End Class
