Option Strict On
Option Explicit On

''' <summary>
''' La molette de la souris agit sur le controle sous le pointeur, meme s'il n'a pas le focus (demande de Jacob,
''' 13/10/2026 : "la molette de la souris doit marcher sur le controle sur lequel elle est positionnee").
'''
''' Windows envoie la molette au controle qui a le FOCUS : sur une fenetre a plusieurs listes (Nflix : groupes,
''' tableau, pitch), il fallait d'abord cliquer dans celle qu'on voulait faire defiler. Ce filtre redirige le
''' message vers le controle place sous le pointeur, sans lui donner le focus.
'''
''' Utilisation : <c>MoletteSousLaSouris.Suivre(Me)</c> au chargement d'une fenetre. Le filtre ne touche que
''' les messages destines a CETTE fenetre. Les listes deroulantes, curseurs et compteurs sont laisses
''' tranquilles : la molette y changerait une valeur par megarde.
''' </summary>
Friend NotInheritable Class MoletteSousLaSouris
    Implements IMessageFilter

    Private Const WM_MOUSEWHEEL As Integer = &H20A
    Private Const WM_MOUSEHWHEEL As Integer = &H20E

    Private Declare Function WindowFromPoint Lib "user32.dll" (ByVal point As Point) As IntPtr
    Private Declare Function SendMessage Lib "user32.dll" Alias "SendMessageW" (ByVal hWnd As IntPtr, ByVal msg As Integer, ByVal wParam As IntPtr, ByVal lParam As IntPtr) As IntPtr

    Private ReadOnly _fenetre As Form
    Private _installe As Boolean

    Private Sub New(fenetre As Form)
        _fenetre = fenetre
    End Sub

    ''' <summary>Active la molette sous le pointeur pour cette fenetre (tant qu'elle est visible).</summary>
    Friend Shared Sub Suivre(fenetre As Form)
        Dim f As New MoletteSousLaSouris(fenetre)
        AddHandler fenetre.VisibleChanged, Sub(sender As Object, e As EventArgs) f.Ajuster()
        AddHandler fenetre.Disposed, Sub(sender As Object, e As EventArgs) f.Retirer()
        f.Ajuster()
    End Sub

    Private Sub Ajuster()
        If _fenetre.Visible Then
            If Not _installe Then
                Application.AddMessageFilter(Me)
                _installe = True
            End If
        Else
            Retirer()
        End If
    End Sub

    Private Sub Retirer()
        If _installe Then
            Application.RemoveMessageFilter(Me)
            _installe = False
        End If
    End Sub

    Private Shared Function ValeurSigne(mot As Long) As Integer
        Dim v As Integer = CInt(mot And &HFFFF)
        Return If(v > 32767, v - 65536, v)
    End Function

    Public Function PreFilterMessage(ByRef m As Message) As Boolean Implements IMessageFilter.PreFilterMessage
        If m.Msg <> WM_MOUSEWHEEL AndAlso m.Msg <> WM_MOUSEHWHEEL Then Return False
        Try
            ' Le message vise-t-il un controle de cette fenetre ?
            Dim vise As Control = Control.FromChildHandle(m.HWnd)
            If vise Is Nothing OrElse vise.FindForm() IsNot _fenetre Then Return False

            Dim lp As Long = m.LParam.ToInt64()
            Dim sous As IntPtr = WindowFromPoint(New Point(ValeurSigne(lp), ValeurSigne(lp >> 16)))
            If sous = IntPtr.Zero OrElse sous = m.HWnd Then Return False
            Dim controle As Control = Control.FromChildHandle(sous)
            If controle Is Nothing OrElse controle.FindForm() IsNot _fenetre OrElse controle Is vise Then Return False
            If TypeOf controle Is ComboBox OrElse TypeOf controle Is TrackBar OrElse TypeOf controle Is NumericUpDown OrElse
               TypeOf controle Is DomainUpDown OrElse TypeOf controle Is ScrollBar Then Return False

            SendMessage(sous, m.Msg, m.WParam, m.LParam)
            Return True
        Catch ex As Exception
            Return False
        End Try
    End Function

End Class
