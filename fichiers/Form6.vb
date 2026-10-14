Imports System.ComponentModel
Imports Vlc.DotNet.Forms
Imports System.IO

Public Class Form6
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        If Form14.Visible = True Then Form14.Hide()
        If IsDate(MaskedTextBox1.Text) = False Or IsDate(MaskedTextBox2.Text) = False Then Console.Beep() : Exit Sub


        TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)

        If IsDate(TextBox1.Text) = False Then MsgBox("Erreur de durrée", vbOKOnly) : Exit Sub
        MyDuration = TimeSpan.ParseExact(TextBox1.Text, "c", Nothing).TotalSeconds
        If MyDuration < 60 Then MsgBox("Erreur la durrée doit etre de 1 mn minimum", vbOKOnly) : Exit Sub

        ' PIP avec du son (16/10/2026) : confirmation avant de valider, pour ne pas laisser un PIP audible par inadvertance.
        If CheckBox4.Checked AndAlso Val(ComboBox1.Text) <> 0 Then
            Dim question As String = "Ce film sera joué en PIP avec un volume de " & Val(ComboBox1.Text) & " % : il sera audible." & vbCrLf & vbCrLf & "Valider quand même ?"
            If MessageBox.Show(Me, question, "PIP avec du son", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Exit Sub
        End If

        idx = -1
        Try
            idx = clickedCell.RowIndex
        Catch ex As Exception

        End Try

        If idx = -1 Then idx = Val(Form4.Label36.Text)
        Form4.DataGridView1.Rows(idx).Cells("Play") = New DataGridViewButtonCell
        Dim cb2 As DataGridViewButtonCell = DirectCast(Form4.DataGridView1.Rows(idx).Cells("Play"), DataGridViewButtonCell)


        'Try


        mTXT2 = Format(DateAdd("s", Me.VlcControl1.Time / 1000, "00:00:00"), "HH:mm:ss")

        Lancement = "INT"

        AudioLanguage = ""
        If ComboBox2.Text <> "" Then AudioLanguage = ComboBox2.SelectedIndex
        SubTitle = ""
        If ComboBox3.Text <> "" Then SubTitle = ComboBox3.SelectedIndex



        ' Film ENTIER (du debut jusqu'a sa fin) : la case Play ne montre que sa duree ("00:09:04"),
        ' comme au moment ou l'on choisit le film ; une plage "debut - fin" n'est ecrite que si l'on
        ' a vraiment coupe le film. Avant, la fin etait comparee a la POSITION DE LECTURE du moment
        ' (mTXT2) et non a la longueur du film : presque toujours differente, donc le film entier
        ' etait note "00:00:00 - 00:09:04" apres un OK, et "00:09:04" sinon (affichage "bizarre").
        Dim filmEntier As Boolean = False
        Try
            Dim longueur As Double = Me.VlcControl1.Length / 1000.0
            Dim finChoisie As Double = TimeSpan.ParseExact(MaskedTextBox2.Text, "c", Nothing).TotalSeconds
            filmEntier = MaskedTextBox1.Text = "00:00:00" AndAlso longueur > 0 AndAlso finChoisie >= longueur - 1.5
        Catch ex As Exception
        End Try

        If filmEntier OrElse (MaskedTextBox1.Text = "00:00:00" And Me.MaskedTextBox2.Text = mTXT2) Then
            cb2.Value = MaskedTextBox2.Text
        Else
            cb2.Value = MaskedTextBox1.Text + " - " + MaskedTextBox2.Text
        End If


        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(idx).Cells("Volume"), DataGridViewComboBoxCell)
        cb.Value = ComboBox1.Text

        Form4.DataGridView1.Rows(idx).Cells("Durée").Value = TextBox1.Text

        PipPosition = "Bdroite"
        For Each Ctrl In GroupBox3.Controls
            If Ctrl.checked Then PipPosition = Ctrl.text : Exit For
        Next

        ' Film IPTV choisi dans Nflix : son adresse ("Url") et sa source sont rangees dans les reglages de la tache.
        ' Les reecrire ici sans les garder faisait perdre l'adresse du film : PLAY bipait et la tache etait ignoree
        ' ("Film introuvable"), vu par Jacob le 14/10/2026.
        Dim reglagesAvant As String = Convert.ToString(Form4.DataGridView1.Rows(idx).Cells("Argument").Value)
        Dim urlIptv As String = Convert.ToString(Arg("Url", reglagesAvant))
        Dim sourceIptv As String = Convert.ToString(Arg("Source", reglagesAvant))
        ' Reglages de la tache. Le champ de la base ne prend que 255 caracteres (voir CoherenceMinuterie) et un film
        ' IPTV y ajoute son adresse (!/Url:), souvent longue : les reglages vides ou "False" ne sont donc pas ecrits
        ' (Arg rend la meme chose pour une cle absente).
        Dim reglagesTache As New System.Text.StringBuilder()
        reglagesTache.Append("!/Titre:").Append(TextBox2.Text)
        If Convert.ToString(AudioLanguage) <> "" Then reglagesTache.Append("!/AudioLanguage:").Append(Convert.ToString(AudioLanguage))
        If Convert.ToString(SubTitle) <> "" Then reglagesTache.Append("!/SubTitle:").Append(Convert.ToString(SubTitle))
        If TextBox3.Text <> "" Then reglagesTache.Append("!/Pub:").Append(TextBox3.Text)
        If CheckBox2.Checked Then reglagesTache.Append("!/ModeHTTP:True")
        reglagesTache.Append("!/Lancement:").Append(Convert.ToString(Lancement))
        reglagesTache.Append("!/Pip:").Append(CheckBox4.Checked)
        reglagesTache.Append("!/PipSize:").Append(Format(TrackBar3.Value / 100, "0.00"))
        reglagesTache.Append("!/PipPosition:").Append(PipPosition)
        reglagesTache.Append("!/Boucle:").Append(CheckBoxBoucle.Checked)
        If CheckBoxPipPersistant.Checked Then reglagesTache.Append("!/PipPersistant:True")
        Form4.DataGridView1.Rows(idx).Cells("Argument").Value = reglagesTache.ToString()
        If urlIptv <> "" Then Form4.DataGridView1.Rows(idx).Cells("Argument").Value = Convert.ToString(Form4.DataGridView1.Rows(idx).Cells("Argument").Value) & "!/Url:" & urlIptv & If(sourceIptv <> "", "!/Source:" & sourceIptv, "")
        Form4.DataGridView1.Rows(idx).Cells("Play").ToolTipText = TextBox2.Text
        Form4.DataGridView1.Rows(idx).Cells(0).ToolTipText = Form4.DataGridView1.Rows(idx).Cells("Argument").Value
        Nom = Form4.DataGridView1.Rows(Val(Form4.Label36.Text)).Cells("Action").Value
        'Nom = VlcControl1.GetCurrentMedia.Mrl
        'Nom = Propre(Nom)
        Dim cb3 As DataGridViewTextBoxCell = DirectCast(Form4.DataGridView1.Rows(idx).Cells("Action"), DataGridViewTextBoxCell)
        cb3.Value = Nom
        Form4.DataGridView1.CurrentCell = Form4.DataGridView1.Rows(idx).Cells("Play")
        Me.Close()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        retablieTousLesVolumes()
        Me.Close()
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs)

    End Sub


    Private Sub Timer1_Tick_1(sender As Object, e As EventArgs) Handles Timer1.Tick

        vol = Me.TrackBar2.Value
        Label10.Text = vol.ToString
        pos1 = VlcControl1.Time / 1000
            Label8.Text = TimeSpan.FromSeconds(CDbl(pos1)).ToString("hh\:mm\:ss")


        If vol <> Val(Me.ComboBox1.Text) Then Me.ComboBox1.Text = vol.ToString
    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click


        Me.MaskedTextBox1.Text = Format(DateAdd("s", Me.VlcControl1.Time / 1000, "00:00:00"), "HH:mm:ss")


        Try
            TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
        Catch ex As Exception

        End Try

    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click


        Me.MaskedTextBox2.Text = Format(DateAdd("s", Me.VlcControl1.Time / 1000, "00:00:00"), "HH:mm:ss")


        Try
            TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
        Catch ex As Exception

        End Try

    End Sub

    Private Sub MaskedTextBox1_MaskInputRejected(sender As Object, e As MaskInputRejectedEventArgs) Handles MaskedTextBox1.MaskInputRejected

    End Sub

    Private Sub MaskedTextBox1_KeyDown(sender As Object, e As KeyEventArgs) Handles MaskedTextBox1.KeyDown
        If IsDate(Me.MaskedTextBox1.Text) = False Then Exit Sub

        If e.KeyCode = Keys.Enter Then

            If Me.VlcControl1.Visible = True Then
                Dim Minute = TimeSpan.ParseExact(Me.MaskedTextBox1.Text, "c", Nothing).TotalMinutes
                Me.VlcControl1.Time = Minute * 60 * 1000

            End If

            TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
        End If


    End Sub

    Private Sub MaskedTextBox2_MaskInputRejected(sender As Object, e As MaskInputRejectedEventArgs) Handles MaskedTextBox2.MaskInputRejected

    End Sub

    Private Sub MaskedTextBox2_KeyDown(sender As Object, e As KeyEventArgs) Handles MaskedTextBox2.KeyDown

        If IsDate(Me.MaskedTextBox2.Text) = False Then Exit Sub
        If e.KeyCode = Keys.Enter Then


            Dim Minute = TimeSpan.ParseExact(Me.MaskedTextBox2.Text, "c", Nothing).TotalMinutes
                Me.VlcControl1.Time = Minute * 60 * 1000

            TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
        End If



    End Sub

    Private Sub MaskedTextBox4_KeyDown(sender As Object, e As KeyEventArgs) Handles MaskedTextBox4.KeyDown

        If IsDate(Me.MaskedTextBox4.Text) = False Then Exit Sub

        If e.KeyCode = Keys.Enter Then
            Dim Minute = TimeSpan.ParseExact(Me.MaskedTextBox4.Text, "c", Nothing).TotalMinutes
            Me.VlcControl1.Time = Minute * 60 * 1000



        End If
    End Sub
    Private Sub MaskedTextBox3_KeyDown(sender As Object, e As KeyEventArgs) Handles MaskedTextBox3.KeyDown

        If IsDate(Me.MaskedTextBox3.Text) = False Then Exit Sub

        If e.KeyCode = Keys.Enter Then
            Dim Minute = TimeSpan.ParseExact(Me.MaskedTextBox3.Text, "c", Nothing).TotalMinutes
            Me.VlcControl1.Time = Minute * 60 * 1000

        End If
    End Sub

    Private Sub MaskedTextBox1_LostFocus(sender As Object, e As EventArgs) Handles MaskedTextBox1.LostFocus
        On Error Resume Next
        TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)


    End Sub

    Private Sub Label4_Click(sender As Object, e As EventArgs) Handles Label4.Click
        TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
    End Sub


    Private Sub TextBox1_TextChanged_1(sender As Object, e As EventArgs) Handles TextBox1.TextChanged

    End Sub

    Private Sub TextBox1_Click(sender As Object, e As EventArgs) Handles TextBox1.Click
        On Error Resume Next
        TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
    End Sub

    Private Sub MaskedTextBox2_LostFocus(sender As Object, e As EventArgs) Handles MaskedTextBox2.LostFocus
        On Error Resume Next
        TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        MaskedTextBox1.Text = "00:00:00"
        MaskedTextBox2.Text = TimeSpan.FromSeconds(CDbl(Me.VlcControl1.Length / 1000)).ToString("hh\:mm\:ss")

        If IsDate(MaskedTextBox2.Text) = False Then
            Nom = Form4.DataGridView1.Rows(Val(Form4.Label36.Text)).Cells("Action").Value
            'Nom = Me.VlcControl1.GetCurrentMedia.Mrl
            'Nom = Propre(Nom)
            MaskedTextBox2.Text = GetDuration(Nom)
        End If

        If IsDate(MaskedTextBox2.Text) = False Then MaskedTextBox2.Text = "23:59:59"
        TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
    End Sub

    Private Sub Form6_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        FlagStop = False

        ' La fenetre disparait et le film se tait tout de suite ; l'arret du lecteur (film lu sur
        ' le reseau) se fait dans un autre fil, 2 s au plus, au lieu de retenir la fermeture.
        Me.Visible = False
        GardeTV.ArreterSansBloquer(VlcControl1, 2000, "Apercu du film")

        ' Reglages (Form4) encore ouverts : le silence tient, le son revient avec leur fermeture.
        If SilenceDesReglages.Actif Then Exit Sub

        If Diffusion.mode = "PHOTO" And Music = True Then
            MusiqueVLC.MusiqueJouer()

        Else
            If VideoWithAudio = False Then
                Form3.VlcControl1.Audio.IsMute = True
                If Music = True Then MusiqueVLC.MusiqueJouer()
            Else
                Form3.VlcControl1.Audio.IsMute = False
                MusiqueVLC.MusiquePause()
            End If

        End If



    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs)



    End Sub

    Private Sub TextBox3_TextChanged(sender As Object, e As EventArgs) Handles TextBox3.TextChanged

    End Sub

    Private Sub Button2_Click_1(sender As Object, e As EventArgs) Handles Button2.Click
        Me.TextBox3.Text += "*" & Me.MaskedTextBox4.Text & "-" & Me.MaskedTextBox3.Text
        TextBox1.Text = CalculDiffHeureAvePub(MaskedTextBox1.Text, MaskedTextBox2.Text)
    End Sub

    Private Sub Label7_Click(sender As Object, e As EventArgs) Handles Label7.Click


        Me.MaskedTextBox4.Text = Format(DateAdd("s", Me.VlcControl1.Time / 1000, "00:00:00"), "HH:mm:ss")

    End Sub

    Private Sub Label6_Click(sender As Object, e As EventArgs) Handles Label6.Click


        Me.MaskedTextBox3.Text = Format(DateAdd("s", Me.VlcControl1.Time / 1000, "00:00:00"), "HH:mm:ss")

    End Sub

    Private Sub Form6_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        dejaPasséParLa = 1
        Me.Height = Button3.Top + Button3.Height + 10
        rebors = Me.Height - Me.ClientSize.Height
        Me.Height = Me.Height + rebors
    End Sub

    Private Sub CheckBox1_Click(sender As Object, e As EventArgs)



    End Sub

    Private Sub VlcControl1_Click(sender As Object, e As EventArgs) Handles VlcControl1.Click

    End Sub

    Private Sub VlcControl1_VlcLibDirectoryNeeded(sender As Object, e As VlcLibDirectoryNeededEventArgs) Handles VlcControl1.VlcLibDirectoryNeeded
        e.VlcLibDirectory = New DirectoryInfo(VLCpath)
        OptionsDuLecteurVlc(sender)

        'Dim aP As String
        'aP = Path.Combine(Environment.GetEnvironmentVariable("ProgramFiles"), "VideoLAN\VLC")

        'If Not File.Exists(Path.Combine(aP, "libvlc.dll")) Then
        'e.VlcLibDirectory = New DirectoryInfo(AppPath & "\data\VLC")
        'Else
        'e.VlcLibDirectory = New DirectoryInfo(aP)
        'End If

    End Sub

    Private Sub TrackBar2_Scroll(sender As Object, e As EventArgs) Handles TrackBar2.Scroll
        ComboBox1.Text = TrackBar2.Value
        SetVolume(TrackBar2.Value, VlcControl1)
    End Sub

    Private Sub TrackBar1_Scroll(sender As Object, e As EventArgs) Handles TrackBar1.Scroll


        Label8.Text = TimeSpan.FromSeconds(CDbl(TrackBar1.Value)).ToString("hh\:mm\:ss")
        nom = VlcControl1.GetCurrentMedia.Title

        Dim extension As String = UCase(Path.GetExtension(Nom))

        If extension = ".TS" Then
            VlcControl1.Position = TrackBar1.Value * 1000 / VlcControl1.Length
        Else
            VlcControl1.Time = TrackBar1.Value * 1000
        End If
    End Sub

    Private Sub Button6_Click(sender As Object, e As EventArgs) Handles Button6.Click
        If VlcControl1.IsPlaying Then
            VlcControl1.Pause()
            Button6.Text = ">"
        Else
            VlcControl1.Play()
            Button6.Text = "II"
        End If
    End Sub

    Private Sub Button5_Click(sender As Object, e As EventArgs) Handles Button5.Click
        If VlcControl1.Audio.IsMute = False Then
            VlcControl1.Audio.IsMute = True
            Button5.Text = "Vo"
        Else
            VlcControl1.Audio.IsMute = False
            Button5.Text = "Vm"
        End If
    End Sub

    Private Sub TrackBar3_Scroll(sender As Object, e As EventArgs) Handles TrackBar3.Scroll
        Label11.Text = TrackBar3.Value
        CheckBox4.Checked = True
    End Sub

    Private Sub CheckBox1_CheckedChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged
        ' "Lire en boucle" suit la case PIP : en standard, un film en PIP boucle, un film en plein
        ' ecran non (la case reste modifiable ensuite).
        CheckBoxBoucle.Checked = CheckBox4.Checked
        ' "Rester affiche" n'a de sens que pour un PIP (demande de Jacob, 06/10) : PIP decoche, la
        ' case est decochee et grisee ; PIP recoche, elle redevient disponible (decochee).
        If Not CheckBox4.Checked Then CheckBoxPipPersistant.Checked = False
        CheckBoxPipPersistant.Enabled = CheckBox4.Checked
        If CheckBox4.Checked = True Then
            TrackBar2.Value = 0
            ComboBox1.Text = TrackBar2.Value
            SetVolume(TrackBar2.Value, VlcControl1)
        End If
    End Sub

    ' Changer de piste audio redemarre decodeur et sortie audio : l'horloge de lecture saute et VLC
    ' jette les images "en retard" (saccade). On ne change donc que si la piste est differente.
    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged
        Try
            If ComboBox2.SelectedIndex < 0 Then Exit Sub
            Dim voulue = Me.VlcControl1.Audio.Tracks.All.ElementAt(ComboBox2.SelectedIndex)
            Dim actuelle = Me.VlcControl1.Audio.Tracks.Current
            If actuelle IsNot Nothing AndAlso actuelle.ID = voulue.ID Then Exit Sub
            Me.VlcControl1.Audio.Tracks.Current = voulue
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ComboBox3_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox3.SelectedIndexChanged
        Try
            If ComboBox3.SelectedIndex < 0 Then Exit Sub
            Dim voulu = Me.VlcControl1.SubTitles.All.ElementAt(ComboBox3.SelectedIndex)
            Dim actuel = Me.VlcControl1.SubTitles.Current
            If actuel IsNot Nothing AndAlso actuel.ID = voulu.ID Then Exit Sub
            Me.VlcControl1.SubTitles.Current = voulu
        Catch ex As Exception
        End Try
    End Sub

    Private Sub Label8_Click(sender As Object, e As EventArgs) Handles Label8.Click

    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form6_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

    ''' <summary>Apercu ouvert depuis les reglages : le son reprend (voir SilenceDesReglages.ApercuOuvert).</summary>
    Private Sub Form6_SonDeLApercu(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then SilenceDesReglages.ApercuOuvert(Me, VlcControl1, Function() TrackBar2.Value)
    End Sub


End Class

