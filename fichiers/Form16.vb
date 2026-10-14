Imports System.Windows.Controls
Imports System.Windows.Forms.VisualStyles.VisualStyleElement
Imports System.Windows.Forms.VisualStyles.VisualStyleElement.Button

Public Class Form16
    Private Sub Form16_Load(sender As Object, e As EventArgs) Handles MyBase.Load



        lg = Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Action").Value

        If UCase(lg) = "OFF" Then CheckBox1.Checked = True : Exit Sub
        If lg = "" Then
            If DateVideoMinimum < DatePhotoMinimum Then DateMini = DateVideoMinimum Else DateMini = DatePhotoMinimum
            If DateVideoMaximum > DatePhotoMaximum Then DateMaxi = DateVideoMaximum Else DateMaxi = DatePhotoMaximum
            Dim dt1 As Date = DateMini
            TextBox26.Text = Format(dt1, "dd/MM/yyyy")
            Dim dt2 As Date = DateMaxi
            TextBox27.Text = Format(dt2, "dd/MM/yyyy")
        Else

            TextBox26.Text = Mid(lg, 4, 10)
            TextBox27.Text = Mid(lg, 18, 10)

            eb1 = InStr(lg, "EnBoucle:")
            If eb1 > 0 Then
                eb2 = Mid(lg, eb1 + 9)

                CheckBox32.Checked = eb2
            End If

            eb3 = InStr(lg, "Lim.Video:")
            If eb3 > 0 Then
                eb4 = Mid(lg, eb3 + 10, eb1 - eb3 - 10)
                TextBox28.Text = eb4
            End If







        End If
    End Sub

    Private Sub Button3_Click(sender As Object, e As EventArgs) Handles Button3.Click

        Dim dt1 As Date
        Dim dt2 As Date


        If CheckBox1.Checked = False Then
            If (IsDate(TextBox26.Text) = False Or IsDate(TextBox27.Text) = False) Then
                MsgBox("Attention modeRDV date invalide " & vbCrLf & "Veuillez resaisir la date ou desactiver ce mode")
                Exit Sub
            End If

            ModeRdvDateDebut0 = TextBox26.Text
            ModeRdvDateFin0 = TextBox27.Text
            If TextBox28.Text = "" Then TextBox28.Text = "0"


            dt1 = ModeRdvDateDebut0
            dt2 = ModeRdvDateFin0

            ' Dates remises dans l'ordre et ecrites sur 10 caracteres (jj/mm/aaaa) : la fenetre les
            ' relit a position fixe dans le texte de la colonne Action ; une date saisie "1/1/2020"
            ' decalait tout a la reouverture.
            If dt2 < dt1 Then
                Dim echange As Date = dt1 : dt1 = dt2 : dt2 = echange
            End If
            TextBox26.Text = Format(dt1, "dd/MM/yyyy")
            TextBox27.Text = Format(dt2, "dd/MM/yyyy")
            If Val(TextBox28.Text) < 0 Then TextBox28.Text = "0"
            TextBox28.Text = CInt(Val(TextBox28.Text)).ToString()

            argu = "du:" & TextBox26.Text & " au " & TextBox27.Text & " Lim.Video:" & TextBox28.Text & " EnBoucle:" & CheckBox32.Checked
            TextBox26.Text = Replace(TextBox26.Text, "/", "-")
            TextBox27.Text = Replace(TextBox27.Text, "/", "-")

        Else
            argu = "OFF"
        End If


        Valeur = Not CheckBox1.Checked
        Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Argument").Value = "!/ModeRdv:" & Valeur & "!/ModeRdvDateDebut:" & TextBox26.Text & "!/ModeRdvDateFin:" & TextBox27.Text & "!/ModeRdvLimitVideo:" & TextBox28.Text & "!/ModeRdvEnBoucle:" & CheckBox32.Checked
        ' (ecrits au format actuel "!/..." : a l'ancien format "/...", la minuterie ne lisait rien
        '  tant que Slide n'avait pas ete relance, et le RDV programme ne demarrait pas)

        Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Action") = New DataGridViewTextBoxCell
        Dim cb As DataGridViewTextBoxCell = DirectCast(Form4.GrilleCourante.Rows(Form4.GrilleCourante.CurrentCell.RowIndex).Cells("Action"), DataGridViewTextBoxCell)
        cb.Value = argu
        Me.Close()
    End Sub
    Private Sub Button4_Click(sender As Object, e As EventArgs) Handles Button4.Click
        Me.Close()
    End Sub
    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub Form16_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class