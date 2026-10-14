Imports System.Runtime.InteropServices

''' <summary>
''' La barre des taches de Windows reste parfois affichee PAR-DESSUS Slide en plein ecran, au lancement (13/11/2026) : Windows ne la cache que si la fenetre plein ecran est la fenetre ACTIVE ; si une autre
''' fenetre avait pris le premier plan au moment du lancement (Gardien, relance, session a distance...), la barre reste devant et y reste definitivement.
''' Apres l'entree en plein ecran, pendant 90 secondes, toutes les 1,5 s : si la barre des taches est visible sur l'ecran de Slide alors que Slide n'est pas la fenetre active, Slide se remet au premier
''' plan (la barre se cache alors d'elle-meme). Aucun effet en mode fenetre, ni quand une fenetre de Slide (options...) est ouverte, ni quand un autre programme a ete choisi apres les 90 secondes.
''' </summary>
Friend Module BarreDesTaches

    <DllImport("user32.dll")> Private Function FindWindow(classe As String, titre As String) As IntPtr
    End Function
    <DllImport("user32.dll")> Private Function IsWindowVisible(hWnd As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll")> Private Function GetWindowRect(hWnd As IntPtr, ByRef r As RectNatif) As Boolean
    End Function
    <DllImport("user32.dll")> Private Function GetForegroundWindow() As IntPtr
    End Function
    <DllImport("user32.dll")> Private Function SetForegroundWindow(hWnd As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll")> Private Sub keybd_event(vk As Byte, scan As Byte, flags As UInteger, extra As UIntPtr)
    End Sub

    <StructLayout(LayoutKind.Sequential)>
    Private Structure RectNatif
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    Private WithEvents _minuteur As New Timer With {.Interval = 1500}
    Private _restant As Integer = 0
    Private _dejaNote As Boolean = False

    ''' <summary>A appeler a l'entree en plein ecran (fil de l'interface).</summary>
    Friend Sub SurveillerApresPleinEcran()
        _restant = 60                  ' (60 x 1,5 s = 90 s)
        _dejaNote = False
        _minuteur.Start()
    End Sub

    Private Sub Minuteur_Tick(sender As Object, e As EventArgs) Handles _minuteur.Tick
        Try
            _restant -= 1
            If _restant <= 0 OrElse Not FullScreen Then
                _minuteur.Stop()
                Exit Sub
            End If
            Dim f As Form = Form3
            If f Is Nothing OrElse f.IsDisposed OrElse Not f.Visible OrElse f.WindowState <> FormWindowState.Maximized Then Exit Sub
            If CalqueWin32.UneFenetreModaleEstOuverte() Then Exit Sub          ' (options... : l'utilisateur travaille dans Slide)
            Dim premierPlan As IntPtr = GetForegroundWindow()
            If premierPlan = f.Handle OrElse (Form.ActiveForm IsNot Nothing) Then Exit Sub          ' (Slide, ou une de ses fenetres, est deja la fenetre active)
            Dim barre As IntPtr = FindWindow("Shell_TrayWnd", Nothing)
            If barre = IntPtr.Zero OrElse Not IsWindowVisible(barre) Then Exit Sub
            Dim r As RectNatif
            If Not GetWindowRect(barre, r) Then Exit Sub
            Dim rectBarre As New Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top)
            If Not rectBarre.IntersectsWith(f.Bounds) Then Exit Sub                  ' (la barre est sur un autre ecran)
            If Not _dejaNote Then
                _dejaNote = True
                DiagLog("Barre des taches visible sur Slide en plein ecran (fenetre active : " & premierPlan.ToString() & ") : Slide repasse au premier plan")
            End If
            ' (Windows refuse parfois qu'un programme prenne le premier plan : l'appui sur Alt, rendu aussitot, leve ce refus.)
            keybd_event(&H12, 0, 0, UIntPtr.Zero)
            keybd_event(&H12, 0, 2, UIntPtr.Zero)
            SetForegroundWindow(f.Handle)
        Catch ex As Exception
        End Try
    End Sub

End Module
