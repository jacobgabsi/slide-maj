Imports System.Runtime.InteropServices

''' <summary>
''' La barre des taches de Windows reste parfois affichee PAR-DESSUS Slide en plein ecran, au lancement (13/11/2026) : Windows ne la cache que si la fenetre plein ecran est la fenetre ACTIVE ; si une autre
''' fenetre avait pris le premier plan au moment du lancement (Gardien, relance, session a distance...), la barre reste devant et y reste definitivement.
''' Tant que Slide est en plein ecran, il regarde si la barre est vraiment DEVANT lui (toutes les 1,5 s pendant 90 s, puis toutes les 3 s) ; si oui, il se declare « plein ecran » aupres de Windows et se
''' remet au premier plan. 26/11/2026 : le lecteur Deezer CACHE prenait parfois le premier plan au lancement (« la barre ne part plus sauf clic ») : une fenetre de Slide qu'on ne voit pas ne compte plus.
''' Aucun effet quand une fenetre de Slide (options...) est utilisee, quand la souris est sur la barre, ni, apres les 90 premieres secondes, quand un autre programme a ete choisi.
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
    <DllImport("user32.dll")> Private Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef processus As UInteger) As UInteger
    End Function
    <DllImport("user32.dll")> Private Function WindowFromPoint(p As Point) As IntPtr
    End Function
    <DllImport("user32.dll")> Private Function GetAncestor(hWnd As IntPtr, quoi As UInteger) As IntPtr
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

    ' La facon officielle de dire a la barre des taches « cette fenetre occupe tout l'ecran ».
    <ComImport(), Guid("602D4995-B13A-429b-A66E-1935E44F4317"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)>
    Private Interface ITaskbarList2
        Sub HrInit()
        Sub AddTab(hWnd As IntPtr)
        Sub DeleteTab(hWnd As IntPtr)
        Sub ActivateTab(hWnd As IntPtr)
        Sub SetActiveAlt(hWnd As IntPtr)
        Sub MarkFullscreenWindow(hWnd As IntPtr, <MarshalAs(UnmanagedType.Bool)> pleinEcran As Boolean)
    End Interface

    <ComImport(), Guid("56FDF344-FD6D-11d0-958A-006097C9A090")>
    Private Class ListeDeLaBarre
    End Class

    Private WithEvents _minuteur As New Timer With {.Interval = 1500}
    Private _tours As Integer = 0
    Private _dejaNote As Boolean = False
    Private _marquee As IntPtr = IntPtr.Zero          ' fenetre declaree « plein ecran » a la barre des taches

    ''' <summary>A appeler a l'entree en plein ecran (fil de l'interface).</summary>
    Friend Sub SurveillerApresPleinEcran()
        _tours = 0
        _dejaNote = False
        _minuteur.Interval = 1500
        _minuteur.Start()
    End Sub

    ''' <summary>A appeler en quittant le plein ecran (sans effet sinon).</summary>
    Friend Sub FinDuPleinEcran()
        _minuteur.Stop()
        If _marquee = IntPtr.Zero Then Exit Sub
        Declarer(_marquee, False)
        _marquee = IntPtr.Zero
    End Sub

    Private Sub Declarer(fenetre As IntPtr, pleinEcran As Boolean)
        Try
            Dim barre As ITaskbarList2 = CType(CObj(New ListeDeLaBarre()), ITaskbarList2)
            barre.HrInit()
            barre.MarkFullscreenWindow(fenetre, pleinEcran)
            Marshal.ReleaseComObject(barre)
        Catch ex As Exception
            DiagLog("Barre des taches : declaration du plein ecran impossible (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>La barre est-elle vraiment au-dessus (et non cachee derriere Slide) ? Trois points de sa surface, loin du coin gauche ou Slide pose son icone.</summary>
    Private Function EstDevant(barre As IntPtr, r As Rectangle) As Boolean
        For Each part As Double In {0.4, 0.6, 0.8}
            Dim h As IntPtr = WindowFromPoint(New Point(r.Left + CInt(r.Width * part), r.Top + r.Height \ 2))
            If h <> IntPtr.Zero AndAlso GetAncestor(h, 2UI) = barre Then Return True           ' (2 : la fenetre racine)
        Next
        Return False
    End Function

    Private Sub Minuteur_Tick(sender As Object, e As EventArgs) Handles _minuteur.Tick
        Try
            If Not FullScreen Then
                FinDuPleinEcran()
                Exit Sub
            End If
            If _tours < 1000 Then _tours += 1
            If _tours = 60 Then _minuteur.Interval = 3000          ' (60 x 1,5 s = 90 s)
            Dim f As Form = Form3
            If f Is Nothing OrElse f.IsDisposed OrElse Not f.Visible OrElse f.WindowState <> FormWindowState.Maximized Then Exit Sub
            If CalqueWin32.UneFenetreModaleEstOuverte() Then Exit Sub          ' (options... : l'utilisateur travaille dans Slide)
            Dim barre As IntPtr = FindWindow("Shell_TrayWnd", Nothing)
            If barre = IntPtr.Zero OrElse Not IsWindowVisible(barre) Then Exit Sub
            Dim r As RectNatif
            If Not GetWindowRect(barre, r) Then Exit Sub
            Dim rectBarre As New Rectangle(r.Left, r.Top, r.Right - r.Left, r.Bottom - r.Top)
            If Not rectBarre.IntersectsWith(f.Bounds) Then Exit Sub                  ' (la barre est sur un autre ecran)
            If Not EstDevant(barre, rectBarre) Then
                _dejaNote = False
                Exit Sub
            End If
            If rectBarre.Contains(Cursor.Position) Then Exit Sub                     ' (la personne s'en sert)
            ' Une fenetre de Slide visible et utilisee (Deezer montre, apercu...) est laissee ; le lecteur Deezer cache, lui, ne compte pas.
            Dim active As Form = Form.ActiveForm
            Dim lecteurCache As Boolean = TypeOf active Is FormDeezer AndAlso Not DirectCast(active, FormDeezer).EstMontree
            If active IsNot Nothing AndAlso active IsNot f AndAlso Not lecteurCache AndAlso active.FormBorderStyle <> FormBorderStyle.None Then Exit Sub
            Dim premierPlan As IntPtr = GetForegroundWindow()
            Dim processus As UInteger = 0UI
            If premierPlan <> IntPtr.Zero Then GetWindowThreadProcessId(premierPlan, processus)
            Dim chezSlide As Boolean = premierPlan = IntPtr.Zero OrElse processus = CUInt(Environment.ProcessId)
            If _tours >= 60 AndAlso Not chezSlide Then Exit Sub                      ' (un autre programme a ete choisi)
            If Not _dejaNote Then
                _dejaNote = True
                DiagLog("Barre des taches devant Slide en plein ecran (fenetre active : " & premierPlan.ToString() &
                        If(active Is Nothing, "", ", " & active.Name) & ") : Slide repasse au premier plan")
            End If
            If _marquee <> IntPtr.Zero Then Declarer(_marquee, False)
            Declarer(f.Handle, True)
            _marquee = f.Handle
            If premierPlan <> f.Handle Then
                ' (Windows refuse parfois qu'un programme prenne le premier plan : l'appui sur Alt, rendu aussitot, leve ce refus.)
                keybd_event(&H12, 0, 0, UIntPtr.Zero)
                keybd_event(&H12, 0, 2, UIntPtr.Zero)
                SetForegroundWindow(f.Handle)
            End If
        Catch ex As Exception
        End Try
    End Sub

End Module
