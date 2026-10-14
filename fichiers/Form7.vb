Imports System.ComponentModel
Imports Vlc.DotNet.Forms
Imports System.IO

Public Class Form7
    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        Dim cb As DataGridViewTextBoxCell = DirectCast(Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Action"), DataGridViewTextBoxCell)
        cb.Value = ComboBox2.Text

        ' Dim cb As DataGridViewComboBoxCell = DirectCast(Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Action"), DataGridViewComboBoxCell)
        'cb.Value = ComboBox2.Text


        Dim cb2 As DataGridViewComboBoxCell = DirectCast(Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Volume"), DataGridViewComboBoxCell)
        cb2.Value = ComboBox1.Text

        Me.VlcControl1.Stop()
        Application.DoEvents()
        Timer1.Enabled = False
        Me.Close()
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Me.VlcControl1.Stop()
        Application.DoEvents()
        Timer1.Enabled = False
        Me.Close()
    End Sub

    Private Sub TextBox1_TextChanged(sender As Object, e As EventArgs)

    End Sub

    Private Sub Timer1_Tick(sender As Object, e As EventArgs)

    End Sub

    Private Sub TextBox1_MouseClick(sender As Object, e As MouseEventArgs)


    End Sub






    Private Sub AxWindowsMediaPlayer1_Enter(sender As Object, e As EventArgs)

    End Sub

    Private Sub Form7_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Timer1.Enabled = True


    End Sub

    Private Sub Timer1_Tick_1(sender As Object, e As EventArgs) Handles Timer1.Tick



        vol = Val(Me.ComboBox1.Text)
        If vol <> GetVol() Then
            SetVol(vol)

        End If

    End Sub





    Private Sub Form7_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        FlagStop = False

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

    Private Sub ComboBox2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox2.SelectedIndexChanged

    End Sub

    Private Sub ComboBox2_SelectionChangeCommitted(sender As Object, e As EventArgs) Handles ComboBox2.SelectionChangeCommitted

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

    Private Sub ComboBox1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ComboBox1.SelectedIndexChanged


    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form7_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class

