Imports Vlc.DotNet.Forms
Imports System.IO

Public Class Form11
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

    Private Sub Form11_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        ApercuTvOuvert = True
    End Sub

    ' A la fermeture, le lecteur de l'apercu est arrete explicitement : il n'y a qu'un tuner TV,
    ' et tant que l'apercu le tenait, le PIP TV relance ensuite restait un rectangle noir.
    ' (Stop dans un autre thread, 2 s au plus : un Stop direct peut bloquer la fenetre.)
    Private Sub Form11_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        Try
            Dim lecteur As VlcControl = VlcControl1
            Dim arret = Threading.Tasks.Task.Run(Sub()
                                                     Try
                                                         lecteur.Stop()
                                                     Catch ex As Exception
                                                     End Try
                                                 End Sub)
            If Not arret.Wait(2000) Then DiagLog("Apercu : arret du lecteur plus long que 2 s a la fermeture")
        Catch ex As Exception
        End Try
    End Sub

    Private Sub Form11_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        ApercuTvOuvert = False
    End Sub

    Private Sub Form11_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then
            Me.Close()

        End If
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub TrackBar2_Scroll(sender As Object, e As EventArgs) Handles TrackBar2.Scroll
        Label1.Text = TrackBar2.Value
        SetVolume(TrackBar2.Value, VlcControl1)
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        If VlcControl1.Audio.IsMute = False Then
            VlcControl1.Audio.IsMute = True
            Button3.Text = "Vo"
        Else
            VlcControl1.Audio.IsMute = False
            Button3.Text = "Vm"
        End If

    End Sub

    Private Sub Panel1_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click

        ' PIP avec du son : confirmation avant de valider (16/10/2026, comme l'apercu des films : Form6).
        If CheckBox4.Checked AndAlso TrackBar2.Value <> 0 Then
            Dim question As String = "Cette source sera jouée en PIP avec un volume de " & TrackBar2.Value & " % : elle sera audible." & vbCrLf & vbCrLf & "Valider quand même ?"
            If MessageBox.Show(Me, question, "PIP avec du son", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) <> DialogResult.Yes Then Exit Sub
        End If

        idx = clickedCell.RowIndex
        'idx = 1
        If idx = -1 Then idx = LigneMinuterieEnCours.Index
        Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.DataGridView1.Rows(idx).Cells("Volume"), DataGridViewComboBoxCell)
        cb.Value = TrackBar2.Value.ToString
        argument = Form4.DataGridView1.Rows(idx).Cells("Argument").Value
        Titre = Arg("titre", argument)
        NomUrl = Arg("Url", argument)

        PipPosition = "Bdroite"
        For Each Ctrl In GroupBox3.Controls
            If Ctrl.checked Then PipPosition = Ctrl.text : Exit For
        Next

        'pip = CheckBox4.Checked
        'If pip = True Then txt = "Pip " & Strings.Left(PipPosition, 2) Else txt = ""
        'Form4.DataGridView1.Rows(idx).Cells("Durée").Value = txt

        If Form4.DataGridView1.Rows(idx).Cells("Type").Value = "IPTV" Then
            Form4.DataGridView1.Rows(idx).Cells("Argument").Value = "!/Titre:" & Titre & "!/Url:" & NomUrl & If(Arg("Source", argument) <> "", "!/Source:" & Arg("Source", argument), "") & "!/Pip:" & CheckBox4.Checked & "!/PipSize:" & Format(TrackBar3.Value / 100, "0.00") & "!/PipPosition:" & PipPosition & "!/PipPersistant:" & CheckBoxPipPersistant.Checked
        Else
            Form4.DataGridView1.Rows(idx).Cells("Argument").Value = "!/Pip:" & CheckBox4.Checked & "!/PipSize:" & Format(TrackBar3.Value / 100, "0.00") & "!/PipPosition:" & PipPosition & "!/PipPersistant:" & CheckBoxPipPersistant.Checked
        End If



        ' Chaine enchainee a la suite d'un film : le reglage "Suite" est garde (Module1.JouerLaSuiteDeLaSerie).
        If CheckBoxSuite.Checked Then Form4.DataGridView1.Rows(idx).Cells("Argument").Value = Convert.ToString(Form4.DataGridView1.Rows(idx).Cells("Argument").Value) & "!/Suite:True"

        If CheckBox4.Checked = True Then
            Form4.DataGridView1.Rows(idx).Cells("Play").Value = "Pip:" & Format(TrackBar3.Value / 100, "0%") & " " & PipPosition
        Else
            Form4.DataGridView1.Rows(idx).Cells("Play").Value = ""
        End If
        FlagExit = True
        Me.Visible = False
        Me.Close()
        retablieTousLesVolumes()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Me.Close()
    End Sub

    Private Sub TrackBar3_Scroll(sender As Object, e As EventArgs) Handles TrackBar3.Scroll
        Label11.Text = TrackBar3.Value

    End Sub

    Private Sub CheckBox4_CheckedChanged(sender As Object, e As EventArgs) Handles CheckBox4.CheckedChanged
        ' "Rester affiche" n'a de sens que pour un PIP (demande de Jacob, 06/10) : PIP decoche, la
        ' case est decochee et grisee ; PIP recoche, elle redevient disponible (decochee).
        If Not CheckBox4.Checked Then CheckBoxPipPersistant.Checked = False
        CheckBoxPipPersistant.Enabled = CheckBox4.Checked
        ' Un PIP ne s'enchaine pas (Module1.JouerLaSuiteDeLaSerie) : la case "Enchainer a la suite" est decochee et grisee quand PIP est coche.
        If CheckBox4.Checked Then CheckBoxSuite.Checked = False
        CheckBoxSuite.Enabled = Not CheckBox4.Checked
        If CheckBox4.Checked = True Then
            TrackBar2.Value = 0
            Label1.Text = 0
            SetVolume(TrackBar2.Value, VlcControl1)
        End If
    End Sub



    Private Sub RadioButton5_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButton5.CheckedChanged

    End Sub




    Private Sub RadioButton6_Click(sender As Object, e As EventArgs) Handles RadioButton6.Click
        CheckBox4.Checked = RadioButton6.Checked
    End Sub

    Private Sub RadioButton5_Click(sender As Object, e As EventArgs) Handles RadioButton5.Click
        CheckBox4.Checked = RadioButton5.Checked
    End Sub

    Private Sub RadioButton3_Click(sender As Object, e As EventArgs) Handles RadioButton3.Click
        CheckBox4.Checked = RadioButton3.Checked
    End Sub

    Private Sub RadioButton4_Click(sender As Object, e As EventArgs) Handles RadioButton4.Click
        CheckBox4.Checked = RadioButton4.Checked
    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form11_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

    ''' <summary>Apercu ouvert depuis les reglages : le son reprend (voir SilenceDesReglages.ApercuOuvert).</summary>
    Private Sub Form11_SonDeLApercu(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Me.Visible Then SilenceDesReglages.ApercuOuvert(Me, VlcControl1, Function() TrackBar2.Value)
    End Sub


End Class