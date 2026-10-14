Imports System.Data.OleDb
Imports ADOX

Public Class Form10
    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        Me.Close()

    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click

        'Call MAJCommentaires()

        My.Settings.Comment = RichTextBox1.Text
        My.Settings.Save()

        Me.Close()





    End Sub

    Private Sub Form10_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        RichTextBox1.Text = My.Settings.Comment

        Exit Sub

        Dim mycommand As OleDbCommand
        Dim myconnection As OleDbConnection
        Dim Sql As String
        Dim cat As New Catalog()
        Dim table As New DataTable

        ' con.ConnectionString = "Provider=Microsoft.ACE.OLEDB.12.0; Data Source=" & Environment.CurrentDirectory & "\\" & "databasedic.accdb"
        myconnection = New OleDbConnection(OLEDBPROVIDER & DatabasePath)
        myconnection.Open()

        tablename = "Commentaires"

        Sql = "Select * from Commentaires"
        mycommand = New OleDb.OleDbCommand(Sql, myconnection)

        cmt = ""
        Try
            mycommand.ExecuteNonQuery()
            reader1 = mycommand.ExecuteReader()
            cmt = ""

            Try

                While reader1.Read()
                    cmt = cmt & reader1("comments")


                End While

            Catch ex As Exception
                MsgBox(ex.Message)
                End Try



        Catch ex2 As Exception

        End Try




        Me.RichTextBox1.Text = cmt

        myconnection.Close()
    End Sub

    Private Sub RichTextBox1_TextChanged(sender As Object, e As EventArgs) Handles RichTextBox1.TextChanged

    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form10_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class