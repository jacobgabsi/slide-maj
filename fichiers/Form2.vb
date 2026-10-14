Imports Vlc.DotNet.Forms
Imports System.IO

Public Class Form2

    Private Sub DataGridView2_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView2.CellContentClick
        Me.PictureBox1.Visible = False
        Me.VlcControl1.Visible = True
        Me.Button1.Visible = False
        Me.Button2.Visible = True
        L = Me.DataGridView2.CurrentRow.Index
        nom = Me.DataGridView2.Rows(L).Cells(3).Value
        Form3.VlcControl1.Stop()
        ext = "file:///"
        Me.VlcControl1.SetMedia(New Uri(ext & nom))
        CouperLeSonAvantDemarrage(Me.VlcControl1)
        Me.VlcControl1.Play()
        SetVolume(100, Me.VlcControl1)
        Me.HScrollBar2.Value = L
        'Form13.Show()
        'Form13.Top = Me.Button2.Top - Form13.ClientSize.Height
        'Form13.Left = Me.Button2.Left
        'Form13.BringToFront()
    End Sub

    Private Sub DataGridView1_CellContentClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentClick

        Me.Button1.Visible = True
        Me.Button2.Visible = False

        Me.PictureBox1.Visible = True
        Me.VlcControl1.Visible = False

        Me.VlcControl1.Pause()

        L = Me.DataGridView1.CurrentRow.Index
        C = e.ColumnIndex

        nom = Me.DataGridView1.Rows(L).Cells(3).Value

        Try
            Me.PictureBox1.Image = Image.FromFile(nom)
            Me.PictureBox1.Refresh()
        Catch ex As Exception
            'MsgBox(ex.Message)

            Dim myGraphics As Graphics = Me.PictureBox1.CreateGraphics
            myGraphics.FillRectangle(Brushes.Gray, 0, 0, Me.PictureBox1.Width, Me.PictureBox1.Height)
            myGraphics.DrawString(ex.Message, New Font("Arial", 12), Brushes.Black, 0, 0)


        End Try




        Me.HScrollBar1.Value = L
        ' a = MessageBox.Show(Me.DataGridView1.Rows(L).Cells(3).Value & " Exifdate: " & exif(Me.DataGridView1.Rows(L).Cells(3).Value) & " SystemDate: " & Me.DataGridView1.Rows(L).Cells(2).Value)
    End Sub
    Private Sub Form2_KeyDown(sender As Object, e As KeyEventArgs) Handles Me.KeyDown
        If e.KeyCode = Keys.Escape Then Me.Hide()
    End Sub
    Private Sub Form2_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.KeyPreview = True
        Me.Width = DataGridView2.Left + DataGridView2.Width + 20


    End Sub
    Protected Overrides Sub OnFormClosing(ByVal e As System.Windows.Forms.FormClosingEventArgs)
        Me.Hide()
        Me.VlcControl1.Stop()
        e.Cancel = True
        MyBase.OnFormClosing(e)
    End Sub

    Private Sub HScrollBar1_Scroll(sender As Object, e As ScrollEventArgs) Handles HScrollBar1.Scroll

        Me.PictureBox1.Visible = True
        Me.Button1.Visible = True
        Me.Button2.Visible = False
        Me.VlcControl1.Visible = False

        L = Me.HScrollBar1.Value
        Me.VlcControl1.Stop()
        nom = Me.DataGridView1.Rows(L).Cells(3).Value

        Try
            Me.PictureBox1.Image = Image.FromFile(nom)
        Catch ex As Exception
            Dim myGraphics As Graphics = Me.PictureBox1.CreateGraphics
            myGraphics.FillRectangle(Brushes.Gray, 0, 0, Me.PictureBox1.Width, Me.PictureBox1.Height)
            myGraphics.DrawString(ex.Message, New Font("Arial", 12), Brushes.Black, 0, 0)
        End Try

        Me.PictureBox1.Refresh()
        Me.DataGridView1.CurrentCell = Me.DataGridView1.Item(1, Me.HScrollBar1.Value)
    End Sub

    Private Sub HScrollBar2_Scroll(sender As Object, e As ScrollEventArgs) Handles HScrollBar2.Scroll

        Me.PictureBox1.Visible = False
        Me.VlcControl1.Visible = True
        Me.Button1.Visible = False
        Me.Button2.Visible = True
        L = Me.HScrollBar2.Value
        nom = Me.DataGridView2.Rows(L).Cells(3).Value

        ext = "file:///"
        Me.VlcControl1.SetMedia(New Uri(ext & nom))
        CouperLeSonAvantDemarrage(Me.VlcControl1)
        Me.VlcControl1.Play()
        Me.DataGridView2.CurrentCell = Me.DataGridView2.Item(1, Me.HScrollBar2.Value)
        SetVolume(100, Me.VlcControl1)          ' volume voulu, et son retabli

    End Sub

    Private Sub BackgroundWorker1_DoWork(sender As Object, e As System.ComponentModel.DoWorkEventArgs)

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        L = Me.DataGridView1.CurrentRow.Index
        NumPhoto = L
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        L = Me.DataGridView2.CurrentRow.Index
        NumVideo = L
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        NumPhoto = 0
        CaseAcocherVide(1)


    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        NumVideo = 0
        CaseAcocherVide(2)

    End Sub


    Private Sub Button5_Click(sender As Object, e As EventArgs)
        Dim colType As Type = Me.DataGridView1.Columns(0).GetType
        'MessageBox.Show(colType.ToString)
        MessageBox.Show(Me.DataGridView1.Columns(0).GetType.ToString)
        'Debug.Print(Me.DataGridView1.Columns(0).GetType.ToString)
    End Sub

    Private Sub DataGridView1_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles DataGridView1.ColumnHeaderMouseClick
        NumPhoto = 0
        CaseAcocherVide(1)
    End Sub
    Private Sub DataGridView2_ColumnHeaderMouseClick(sender As Object, e As DataGridViewCellMouseEventArgs) Handles DataGridView2.ColumnHeaderMouseClick
        NumVideo = 0
        CaseAcocherVide(2)
    End Sub

    Private Sub Button5_Click_1(sender As Object, e As EventArgs)

        PictureBox1.Image.RotateFlip(RotateFlipType.Rotate180FlipNone)
        PictureBox1.Refresh()
    End Sub



    Private Sub Button6_Click(sender As Object, e As EventArgs)

        PictureBox1.Image.RotateFlip(RotateFlipType.Rotate270FlipNone)
        PictureBox1.Refresh()
    End Sub

    Private Sub Button7_Click(sender As Object, e As EventArgs)

        PictureBox1.Image.RotateFlip(RotateFlipType.Rotate90FlipNone)
        PictureBox1.Refresh()
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click
        If NumPhoto > 1 Then Me.DataGridView1.FirstDisplayedScrollingRowIndex = NumPhoto

    End Sub

    Private Sub Label2_Click(sender As Object, e As EventArgs) Handles Label2.Click
        If NumVideo > 1 Then Me.DataGridView2.FirstDisplayedScrollingRowIndex = NumVideo
    End Sub

    Private Sub PictureBox1_Click(sender As Object, e As EventArgs)

    End Sub

    Private Sub VlcControl1_Click(sender As Object, e As EventArgs)

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

    Private Sub Button5_Click_2(sender As Object, e As EventArgs)
        SaveRestoreLeSlide(1)
    End Sub

    Private Sub Button6_Click_1(sender As Object, e As EventArgs)
        SaveRestoreLeSlide(2)
    End Sub

    Private Sub Button5_Click_3(sender As Object, e As EventArgs) Handles Button5.Click
        NombreDePhotoChecked = 0
        NombreDeVideoChecked = 0
        msg = "Stat non disponible"

        nb = DataGridView1.RowCount - 1
        For a = 0 To nb
            If DataGridView1.Rows(a).Cells(0).Value <> 0 Then
                NombreDePhotoChecked += 1
            End If
        Next

        If (nb) > 0 Then msg = "Photo deja diffusés = " & NombreDePhotoChecked & "/" & nb & "  ( " & Format(NombreDePhotoChecked / nb, "0 %") & " )" & vbCrLf

        nb2 = DataGridView2.RowCount - 1
        For a = 0 To nb2

            If DataGridView2.Rows(a).Cells(0).Value <> 0 Then
                NombreDeVideoChecked += 1
            End If
        Next

        If (nb2) > 0 Then msg += "video deja diffusés = " & NombreDeVideoChecked & "/" & nb2 & "  ( " & Format(NombreDeVideoChecked / nb2, "0 %") & " )"

        MsgBox(msg)
    End Sub

    Private Sub Button6_Click_2(sender As Object, e As EventArgs) Handles Button6.Click
        Call ChangeDatebyExifNow
    End Sub

    Private Sub DataGridView1_CellContentDoubleClick(sender As Object, e As DataGridViewCellEventArgs) Handles DataGridView1.CellContentDoubleClick

    End Sub





    Private Sub DataGridView1_MouseDown(sender As Object, e As MouseEventArgs) Handles DataGridView1.MouseDown

        If ModeModifExif = False Then Exit Sub

        Try


            Dim hit As DataGridView.HitTestInfo = DataGridView1.HitTest(e.X, e.Y)
            c = hit.ColumnIndex
            If c <> 4 Then Exit Sub

            l = hit.RowIndex

            nom = Me.DataGridView1.Rows(l).Cells(3).Value
            DirSource = Path.GetDirectoryName(nom)
            For h = Len(DirSource) To 1 Step -1
                If Mid(DirSource, h, 1) = "\" Then trouve = h : Exit For
            Next

            Dt0 = Strings.Mid(DirSource, trouve + 1)

            Dim dt As Date

            Try
                dt = DateSerial(Strings.Left(Dt0, 4), Strings.Right(Dt0, 2), 15)
                'Dt_OK = dt.ToString("dd/MM/yyyy")
                Dt_OK = dt.ToString
            Catch ex As Exception
                dt = Nothing
                Dt_OK = Nothing
            End Try




            If e.Button = MouseButtons.Right Then

                r = InputBox("Changer Date", "Changer Date par ?", Dt_OK)

                If IsDate(r) Then
                    dt = r
                    Me.DataGridView1.Rows(l).Cells(c).Value = dt.ToString
                Else
                    Me.DataGridView1.Rows(l).Cells(c).Value = ""

                End If
            Else
                If IsDate(Dt_OK) Then Me.DataGridView1.Rows(l).Cells(4).Value = Dt_OK

            End If
        Catch ex As Exception

        End Try



    End Sub

    Private Sub HScrollBar1_StyleChanged(sender As Object, e As EventArgs) Handles HScrollBar1.StyleChanged

    End Sub

    Private Sub DataGridView2_MouseDown(sender As Object, e As MouseEventArgs) Handles DataGridView2.MouseDown

        If ModeModifExif = False Then Exit Sub


        Try

            Dim hit As DataGridView.HitTestInfo = DataGridView2.HitTest(e.X, e.Y)
        c = hit.ColumnIndex
        If c <> 4 Then Exit Sub

        l = hit.RowIndex

        nom = Me.DataGridView2.Rows(l).Cells(3).Value
        DirSource = Path.GetDirectoryName(nom)
        For h = Len(DirSource) To 1 Step -1
            If Mid(DirSource, h, 1) = "\" Then trouve = h : Exit For
        Next

        Dt0 = Strings.Mid(DirSource, trouve + 1)

        Dim dt As Date
        dt = DateSerial(Strings.Left(Dt0, 4), Strings.Right(Dt0, 2), 15)
        'Dt_OK = dt.ToString("dd/MM/yyyy")
        Dt_OK = dt.ToString



        If e.Button = MouseButtons.Right Then

            r = InputBox("Changer Date", "Changer Date par ?", Dt_OK)

            If IsDate(r) Then
                dt = r
                Me.DataGridView2.Rows(l).Cells(c).Value = dt.ToString
            Else
                Me.DataGridView2.Rows(l).Cells(c).Value = ""

            End If
        Else

            Me.DataGridView2.Rows(l).Cells(4).Value = Dt_OK

        End If


        Catch ex As Exception

        End Try



    End Sub

    Private Sub Button7_Click_1(sender As Object, e As EventArgs)

    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form2_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
