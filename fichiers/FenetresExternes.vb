Option Strict On
Option Explicit On

Imports System.Runtime.InteropServices
Imports System.Text

''' <summary>
''' Les fenetres visibles du bureau, pour la tache "Lancer" avec "Attend la fin avant de continuer" (20/10/2026). Un programme lance
''' n'est parfois qu'un LANCEUR : calc.exe (Windows 11) demarre la Calculatrice du Store (CalculatorApp, dont la fenetre appartient a
''' ApplicationFrameHost) puis se ferme au bout d'une seconde ; le suivre par son processus ou par son nom ne marche pas. On retient
''' donc les fenetres qui APPARAISSENT apres le lancement (hors celles de Slide) et la tache attend qu'elles soient toutes fermees.
''' </summary>
Friend Module FenetresExternes

    <DllImport("user32.dll")>
    Private Function EnumWindows(rappel As EnumWindowsProc, lParam As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Function IsWindowVisible(hWnd As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Function IsWindow(hWnd As IntPtr) As Boolean
    End Function
    <DllImport("user32.dll", CharSet:=CharSet.Unicode)>
    Private Function GetWindowTextLength(hWnd As IntPtr) As Integer
    End Function
    <DllImport("user32.dll")>
    Private Function GetWindow(hWnd As IntPtr, commande As UInteger) As IntPtr
    End Function
    <DllImport("user32.dll", EntryPoint:="GetWindowLongPtrW")>
    Private Function GetWindowLongPtr(hWnd As IntPtr, nIndex As Integer) As IntPtr
    End Function
    <DllImport("user32.dll")>
    Private Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef processId As UInteger) As UInteger
    End Function
    <DllImport("dwmapi.dll")>
    Private Function DwmGetWindowAttribute(hWnd As IntPtr, attribut As Integer, ByRef valeur As Integer, taille As Integer) As Integer
    End Function

    Private Delegate Function EnumWindowsProc(hWnd As IntPtr, lParam As IntPtr) As Boolean

    Private Const GW_OWNER As UInteger = 4UI
    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_TOOLWINDOW As Long = &H80L
    Private Const DWMWA_CLOAKED As Integer = 14

    ''' <summary>Une vraie fenetre de programme : visible, avec un titre, sans proprietaire, ni fenetre outil, ni fenetre cachee par Windows
    ''' (les applications du Store suspendues en ont de "cloaked").</summary>
    Private Function EstUneFenetreDeProgramme(h As IntPtr) As Boolean
        If Not IsWindowVisible(h) Then Return False
        If GetWindowTextLength(h) = 0 Then Return False
        If GetWindow(h, GW_OWNER) <> IntPtr.Zero Then Return False
        If (GetWindowLongPtr(h, GWL_EXSTYLE).ToInt64() And WS_EX_TOOLWINDOW) <> 0 Then Return False
        Dim cache As Integer = 0
        If DwmGetWindowAttribute(h, DWMWA_CLOAKED, cache, 4) = 0 AndAlso cache <> 0 Then Return False
        Return True
    End Function

    ''' <summary>Les fenetres de programme visibles en ce moment (a prendre juste avant le lancement).</summary>
    Friend Function Visibles() As HashSet(Of IntPtr)
        Dim liste As New HashSet(Of IntPtr)
        EnumWindows(Function(h As IntPtr, p As IntPtr) As Boolean
                        If EstUneFenetreDeProgramme(h) Then liste.Add(h)
                        Return True
                    End Function, IntPtr.Zero)
        Return liste
    End Function

    ''' <summary>Les fenetres de programme apparues depuis <paramref name="avant"/>, hors celles de Slide, avec le processus qui les possede.</summary>
    Friend Function Nouvelles(avant As HashSet(Of IntPtr)) As List(Of KeyValuePair(Of IntPtr, Integer))
        Dim trouvees As New List(Of KeyValuePair(Of IntPtr, Integer))
        Dim moi As Integer = Environment.ProcessId
        For Each h As IntPtr In Visibles()
            If avant.Contains(h) Then Continue For
            Dim pid As UInteger = 0
            GetWindowThreadProcessId(h, pid)
            If CInt(pid) = moi Then Continue For
            trouvees.Add(New KeyValuePair(Of IntPtr, Integer)(h, CInt(pid)))
        Next
        Return trouvees
    End Function

    ''' <summary>La fenetre existe encore (elle n'a pas ete fermee).</summary>
    Friend Function Existe(h As IntPtr) As Boolean
        Return IsWindow(h)
    End Function

End Module
