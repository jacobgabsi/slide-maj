Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

''' <summary>
''' Message affiche quelques secondes par-dessus le diaporama (Module1.MessageEcran) : aujourd'hui
''' ceux du mode RDV ("Aucune photo ni vidéo sur cette période", etc.).
'''
''' Refait le 06/10 (Jacob : "affichage pro egalement") : c'etait un rectangle gris avec un texte
''' centre. Meme style que la fenetre de fermeture et celle des verifications : carte sombre aux
''' coins arrondis, icone, titre, texte, et une fine barre qui se vide pendant que le message
''' reste a l'ecran. Elle se place au centre de l'image de Slide et ne prend pas le clavier.
''' Les controles sont dans le Concepteur ; l'icone, le bord et la barre sont dessines ici.
''' </summary>
Public Class FormMessage

    Private ReadOnly _bord As Color = Color.FromArgb(58, 58, 60)
    Private ReadOnly _piste As Color = Color.FromArgb(52, 52, 56)
    Private ReadOnly _accent As Color = Color.FromArgb(10, 132, 255)

    Private ReadOnly _chrono As New Stopwatch()
    Private _dureeMs As Integer = 5000

    ''' <summary>Prepare le message : titre, texte, duree d'affichage et place a l'ecran.
    ''' A appeler avant Show().</summary>
    Public Sub Preparer(titre As String, message As String, secondes As Double, Optional devant As Form = Nothing)
        LabelTitre.Text = If(String.IsNullOrWhiteSpace(titre), "Slide", titre)
        Label1.Text = If(message, "")
        _dureeMs = CInt(Math.Max(1, secondes) * 1000)

        ' Hauteur ajustee au texte (une a quatre lignes).
        Dim marge As Integer = Me.ClientSize.Height - Label1.Bottom
        Dim voulu As Size = TextRenderer.MeasureText(Label1.Text, Label1.Font, New Size(Label1.Width, Integer.MaxValue), TextFormatFlags.WordBreak)
        Dim hauteur As Integer = Math.Max(Label1.Font.Height * 2, voulu.Height + 4)
        Me.ClientSize = New Size(Me.ClientSize.Width, Label1.Top + hauteur + marge)

        ' Au centre de l'image de Slide si elle est a l'ecran, sinon de l'ecran.
        Dim zone As Rectangle = Screen.PrimaryScreen.WorkingArea
        Try
            If devant IsNot Nothing AndAlso Not devant.IsDisposed AndAlso devant.Visible AndAlso devant.WindowState <> FormWindowState.Minimized Then
                zone = devant.RectangleToScreen(devant.ClientRectangle)
            End If
        Catch ex As Exception
        End Try
        Me.Location = New Point(zone.Left + (zone.Width - Me.Width) \ 2, zone.Top + (zone.Height - Me.Height) \ 2)
    End Sub

    Private Sub FormMessage_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        ' Devant tout, sans prendre le clavier (jamais par la propriete TopMost, qui active la fenetre).
        CalqueWin32.DefinirPremierPlanSeul(Me, True)
        _chrono.Restart()
        TimerDecompte.Start()
    End Sub

    Private Sub TimerDecompte_Tick(sender As Object, e As EventArgs) Handles TimerDecompte.Tick
        ' Seule la bande de la barre est redessinee.
        Me.Invalidate(New Rectangle(0, Me.ClientSize.Height - 12, Me.ClientSize.Width, 12))
    End Sub

    Private Sub FormMessage_FormClosed(sender As Object, e As FormClosedEventArgs) Handles MyBase.FormClosed
        TimerDecompte.Stop()
    End Sub

    ''' <summary>Le message ne prend pas le clavier : Slide garde ses touches (Echap, N, P...).</summary>
    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    ' ------------------------------------------------------------------------------------------
    ' Dessin : icone "information", bord, barre du temps restant
    ' ------------------------------------------------------------------------------------------

    Private Sub PictureBoxIcone_Paint(sender As Object, e As PaintEventArgs) Handles PictureBoxIcone.Paint
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim zone As New Rectangle(1, 1, PictureBoxIcone.Width - 3, PictureBoxIcone.Height - 3)
        Using pinceau As New SolidBrush(_accent)
            g.FillEllipse(pinceau, zone)
        End Using
        ' Un "i" : un point, puis une barre arrondie.
        Dim l As Single = zone.Width * 0.13F
        Dim cx As Single = zone.Left + zone.Width / 2.0F
        g.FillEllipse(Brushes.White, cx - l * 0.62F, zone.Top + zone.Height * 0.2F, l * 1.24F, l * 1.24F)
        Using blanc As New Pen(Color.White, l) With {.StartCap = LineCap.Round, .EndCap = LineCap.Round}
            g.DrawLine(blanc, cx, zone.Top + zone.Height * 0.46F, cx, zone.Top + zone.Height * 0.76F)
        End Using
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim g As Graphics = e.Graphics
        Using stylo As New Pen(_bord, 1)
            g.DrawRectangle(stylo, 0, 0, Me.ClientSize.Width - 1, Me.ClientSize.Height - 1)
        End Using
        ' Barre fine en bas : pleine au debut, vide quand le message disparait.
        Dim epaisseur As Integer = Math.Max(3, CInt(Math.Round(3 * Me.DeviceDpi / 96.0)))
        Dim piste As New Rectangle(1, Me.ClientSize.Height - epaisseur - 1, Me.ClientSize.Width - 2, epaisseur)
        Using fond As New SolidBrush(_piste)
            g.FillRectangle(fond, piste)
        End Using
        Dim reste As Double = If(_chrono.IsRunning, Math.Max(0.0, 1.0 - _chrono.ElapsedMilliseconds / CDbl(_dureeMs)), 1.0)
        Using plein As New SolidBrush(_accent)
            g.FillRectangle(plein, New Rectangle(piste.Left, piste.Top, CInt(piste.Width * reste), piste.Height))
        End Using
    End Sub

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            If Not Me.DesignMode Then
                cp.ClassStyle = cp.ClassStyle Or CS_DROPSHADOW
                cp.ExStyle = cp.ExStyle Or CalqueWin32.WS_EX_NOACTIVATE Or CalqueWin32.WS_EX_TOOLWINDOW
            End If
            Return cp
        End Get
    End Property

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        If Me.DesignMode Then Exit Sub
        Try
            ' Windows 11 : vrais coins arrondis et lisere dans la couleur du bord (sans effet avant).
            Dim preference As Integer = DWMWCP_ROUND
            DwmSetWindowAttribute(Me.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, preference, 4)
            Dim couleurBord As Integer = _bord.R Or (CInt(_bord.G) << 8) Or (CInt(_bord.B) << 16)
            DwmSetWindowAttribute(Me.Handle, DWMWA_BORDER_COLOR, couleurBord, 4)
        Catch ex As Exception
        End Try
    End Sub

    Private Const CS_DROPSHADOW As Integer = &H20000
    Private Const DWMWA_WINDOW_CORNER_PREFERENCE As Integer = 33
    Private Const DWMWCP_ROUND As Integer = 2
    Private Const DWMWA_BORDER_COLOR As Integer = 34

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attribut As Integer, ByRef valeur As Integer, taille As Integer) As Integer
    End Function

End Class
