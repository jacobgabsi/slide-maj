Public Class Form5

    Private Sub Form5_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        DateTimePicker1.Format = DateTimePickerFormat.Custom
        DateTimePicker1.CustomFormat = "ddd dd MMM yyyy --- HH:mm tt"

        DateTimePicker2.Format = DateTimePickerFormat.Custom
        DateTimePicker2.CustomFormat = "ddd dd MMM yyyy --- HH:mm tt"

        'Me.DateTimePicker1.Value = Now
        'Me.DateTimePicker2.Value = Now
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click




        EcranNoirDebut = DateTimePicker1.Value
        seconde = Second(EcranNoirDebut)
        EcranNoirDebut = EcranNoirDebut.AddSeconds(-seconde)

        EcranNoirFin = DateTimePicker2.Value
        seconde = Second(EcranNoirFin)
        EcranNoirFin = EcranNoirFin.AddSeconds(-seconde)



        Form4.DataGridView1.Rows(Form4.DataGridView1.CurrentCell.RowIndex).Cells("Action") = New DataGridViewTextBoxCell
        Dim cb As DataGridViewTextBoxCell = DirectCast(Form4.DataGridView1.Rows(Form4.DataGridView1.CurrentCell.RowIndex).Cells("Action"), DataGridViewTextBoxCell)


        cb.Value = "Jusquau - " & EcranNoirFin.ToLongDateString & " " & EcranNoirFin.ToShortTimeString

        If CheckBox1.Checked = True Then tx = "Timer ON" Else tx = "Timer OFF"
        Form4.DataGridView1.Rows(Form4.DataGridView1.CurrentCell.RowIndex).Cells("Play").Value = tx
        ' Aquarium (video de data\Aquarium en boucle) au lieu de l'ecran noir : voir EcranNoir.
        Form4.DataGridView1.Rows(Form4.DataGridView1.CurrentCell.RowIndex).Cells("Argument").Value =
            If(CheckBoxAquarium.Checked, "!/Aquarium:True", "")
        'Dim timeDif As Long = DateDiff(DateInterval.Minute, EcranNoirDebut, EcranNoirFin)

        Dim Seconds As Integer = DateDiff(DateInterval.Second, EcranNoirDebut, EcranNoirFin)
        Dim d As Integer = TimeSpan.FromSeconds(CDbl(Seconds)).Days
        Dim h As Integer = TimeSpan.FromSeconds(CDbl(Seconds)).Hours
        Dim m As Integer = TimeSpan.FromSeconds(CDbl(Seconds)).Minutes
        Dim s As Integer = TimeSpan.FromSeconds(CDbl(Seconds)).Seconds
        'MsgBox(d & " Days " & h & " Hours " & m & " Minutes " & s & " Seconds")


        Form4.DataGridView1.Rows(Form4.DataGridView1.CurrentCell.RowIndex).Cells("Durée").Value = d & "J" & h & "h" & m & "m"


        'Debug.Print(EcranNoirFin)
        Me.Close()


    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()

    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form5_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class