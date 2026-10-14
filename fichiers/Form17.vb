Public Class Form17
    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Me.Close()
    End Sub

    Private Sub Button21_Click(sender As Object, e As EventArgs) Handles Button21.Click
        Form4.OpenFileDialog1.InitialDirectory = My.Computer.FileSystem.SpecialDirectories.ProgramFiles
        Form4.OpenFileDialog1.Filter = "Programme|*.exe|Tous les Fichiers|*.*"
        Form4.OpenFileDialog1.FileName = ""

        result = Form4.OpenFileDialog1.ShowDialog()
        If result = DialogResult.OK Then
            fileName = Form4.OpenFileDialog1.FileName
        Else
            fileName = ""
        End If
        TextBox15.Text = fileName




    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        If TextBox15.Text = "" Then Exit Sub

        Prg = TextBox15.Text
        Argu = TextBox1.Text
        Valeur = CheckBox1.Checked

        Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Argument").Value = "!/Prg:" & Prg & "!/Argu:" & Argu & "!/Wait:" & Valeur
        Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Action") = New DataGridViewTextBoxCell
        Dim cb As DataGridViewTextBoxCell = DirectCast(Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Action"), DataGridViewTextBoxCell)
        cb.Value = Prg
        Me.Close()


    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form17_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class