Imports System.IO


Public Class Form8
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        p = InStr(TextBox3.Text, ".")
        If p = 0 Then
            TextBox3.Text = "*.*"
        Else
            TextBox3.Text = "*." & Mid(TextBox3.Text, p + 1)
        End If

        Dim cb As DataGridViewTextBoxCell = DirectCast(Form4.DataGridView1.Rows(clickedCell.rowindex).Cells("Action"), DataGridViewTextBoxCell)
        cb.Value = Trim(TextBox1.Text) + "\#>" & UCase(TextBox2.Text) & UCase(TextBox3.Text)
        Dim cb2 As DataGridViewButtonCell = DirectCast(Form4.DataGridView1.Rows(clickedCell.rowindex).Cells("Play"), DataGridViewButtonCell)
        cb2.Value = "23:59:59"
        Dim cb3 As DataGridViewTextBoxCell = DirectCast(Form4.DataGridView1.Rows(clickedCell.rowindex).Cells("Durée"), DataGridViewTextBoxCell)
        cb3.Value = "23:59:59"

        If InStr(UCase(TextBox1.Text), recepteurSat) > 0 Then Form4.DataGridView1.Rows(clickedCell.rowindex).Cells("Volume").Value = Format(VolumeDreamboxParDefault)

        Form4.DataGridView1.ClearSelection()
        Me.Close()
    End Sub

    Private Sub Button8_Click(sender As Object, e As EventArgs) Handles Button8.Click
        TextBox1.Text = DossierDivxParDefault
        TextBox3.Text = "*.*"
    End Sub

    Private Sub Button9_Click(sender As Object, e As EventArgs) Handles Button9.Click
        TextBox1.Text = DossierDreamboxParDefault
        TextBox3.Text = "*.TS"

    End Sub

    Private Sub Button13_Click(sender As Object, e As EventArgs) Handles Button13.Click
        TextBox1.Text = DossierFreeboxParDefault
        TextBox3.Text = "*.*"
    End Sub

    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        FolderBrowserDialog1.ShowDialog()
        folderName = FolderBrowserDialog1.SelectedPath
        TextBox1.Text = folderName

    End Sub

    Private Sub Form8_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        TextBox3.Text = "*.*"

    End Sub



    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click
        p = InStr(TextBox3.Text, ".")
        If p = 0 Then
            TextBox3.Text = "*.*"
        Else
            TextBox3.Text = "*." & Mid(TextBox3.Text, p + 1)
        End If

        TextBox4.Text = ReplayCherche(TextBox1.Text, TextBox2.Text, TextBox3.Text)
    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form8_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class