Option Explicit Off
Option Strict Off
Imports System
Imports System.Drawing
Imports System.Windows.Forms
Imports System.Text
Imports System.IO
Imports System.Threading
Imports System.Reflection
Imports Vlc.DotNet.Core
Module Module5


    Public loopBreak As Boolean = False
    Public loopBreakFini As Boolean = False
    Public testPath As String
    Public testFileSize As ULong
    Public testIterations As ULong
    Public testType As String
    Public localStoragePath As String
    Public workerThread As Thread
    Public testIsActive As Boolean = False
    Public FileSize As String = "400"
    Public Loops As String = "2"
    Public DriveLetter
    Public NasSpeedValue As String


    Public Declare Function SendMessageByString Lib "user32.dll" Alias "SendMessageA" (ByVal hwnd As IntPtr, ByVal wMsg As Integer, ByVal wParam As Integer, ByVal lParam As String) As Integer
    Public Const WM_GETTEXT As Integer = &HD
    Public Const WM_GETTEXTLENGTH As Integer = &HE



    'Public Declare Function SetForegroundWindow Lib "user32.dll" (ByVal hwnd As Integer) As Integer
    'Public Declare Auto Function FindWindow Lib "user32.dll" (ByVal lpClassName As String, ByVal lpWindowName As String) As Integer
    Public Declare Function IsIconic Lib "user32.dll" (ByVal hwnd As Integer) As Boolean

    Public Declare Function ShowWindow Lib "user32.dll" (ByVal hwnd As Integer, ByVal nCmdShow As Integer) As Integer
    'Private Declare Function GetWindowRect Lib "user32" Alias "GetWindowRect" (ByVal hwnd As Integer, Byref lpRect As RECT) As Integer
    Public Declare Function PrintWindow Lib "User32.dll" (ByVal hWnd As IntPtr, ByVal hdcBlt As IntPtr, ByVal nFlags As Int32) As Int32

    Public Const SW_RESTORE As Integer = 9
    Public Const SW_SHOW As Integer = 5
    Public Const SW_SHOWMINIMIZED As Integer = 2

    Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure


    Function FocusWindow_Old(ByVal strWindowCaption As String, ByVal strClassName As String)

        'example
        'to show the "Calculator" you can call FocusWindow("Calculator", Nothing) or FocusWindow(Nothing, "CalcFrame")

        Dim hWnd As Integer
        hWnd = FindWindow(strClassName, strWindowCaption)

        If hWnd > 0 Then
            SetForegroundWindow(hWnd)

            If IsIconic(hWnd) Then  'Restore if minimized
                ShowWindow(hWnd, SW_RESTORE)
            Else
                ShowWindow(hWnd, SW_SHOW)
            End If


            Dim hWnd_Rect As RECT
            GetWindowRect(hWnd, hWnd_Rect)

            FocusWindow_Old = hWnd_Rect
        End If

    End Function

    Function FocusWindow(ByVal strWindowCaption As String, ByVal strClassName As String) As (W_RECT As RECT, Handle As Integer)


        Dim hWnd As Integer
        hWnd = FindWindow(strClassName, strWindowCaption)

        If hWnd > 0 Then
            SetForegroundWindow(hWnd)

            If IsIconic(hWnd) Then  'Restore if minimized
                ShowWindow(hWnd, SW_RESTORE)
            Else
                ShowWindow(hWnd, SW_SHOW)
            End If


            Dim hWnd_Rect As RECT
            GetWindowRect(hWnd, hWnd_Rect)

            'FocusWindow = hWnd_Rect

            Return (hWnd_Rect, hWnd)
        End If

    End Function


    Sub UnFocusWindow(ByVal strWindowCaption As String, ByVal strClassName As String)

        'example
        'to show the "Calculator" you can call FocusWindow("Calculator", Nothing) or FocusWindow(Nothing, "CalcFrame")

        Dim hWnd As Integer
        hWnd = FindWindow(strClassName, strWindowCaption)

        If hWnd > 0 Then
            SetForegroundWindow(hWnd)
            ShowWindow(hWnd, SW_SHOWMINIMIZED)

        End If
    End Sub

    Public Function NasSpeed()



        'DriveLetter = "D:\"
        DriveLetter = CheminFilm

        If DriveLetter = "" Then
            Beep()
            MsgBox("Veuillez d'abord clicker sur le bouton source")
            Exit Function
        End If

        If IsFolderWritable(Path.GetDirectoryName(Application.ExecutablePath)) Then
            localStoragePath = Path.GetDirectoryName(Application.ExecutablePath)
        Else ' Write access to program folder denied, falling back to Windows temp folder as defined in the environment variables
            localStoragePath = Path.GetTempPath()
        End If

        loopBreakFini = False
        benchmarkButton(DriveLetter)




    End Function


    Private Function IsFolderWritable(folderPath As String) As Boolean
        Try
            Using fs As FileStream =
              File.Create(Path.Combine(folderPath,
              Path.GetRandomFileName()), 1, FileOptions.DeleteOnClose)
            End Using
            Return True
        Catch
            Return False
        End Try
    End Function


    Private Sub benchmarkButton(DriveLetter)

        If Not testIsActive Then
            Dim checkFileSize As Integer
            Dim checkLoops As Integer

            If (Not String.IsNullOrEmpty(DriveLetter) AndAlso Integer.TryParse(FileSize, checkFileSize) AndAlso Integer.TryParse(Loops, checkLoops)) Then
                If checkFileSize <= 64000 Then
                    Dim newThread As New Thread(New ThreadStart(AddressOf BenchmarkHandler))
                    newThread.Start()
                End If

            End If
        Else
            loopBreak = True

        End If


    End Sub


    Private Sub BenchmarkHandler()
        testIsActive = True
        loopBreak = False
        testPath = DriveLetter

        If Not loopBreak Then ' Run warmup
            testFileSize = 0 ' for warmup run
            testIterations = 1
            testType = "write"
            LaunchTestThread()
        End If
        testFileSize = Convert.ToUInt64(FileSize) * 1000000
        testIterations = Convert.ToUInt64(Loops)
        If Not loopBreak Then ' Run write test
            testType = "write"
            LaunchTestThread()
        End If
        If Not loopBreak Then ' Run read test
            testType = "read"
            LaunchTestThread()
        End If
        testIsActive = False
        loopBreak = False

    End Sub

    Private Sub LaunchTestThread()
        workerThread = New Thread(AddressOf TestPerf)
        workerThread.Start()
        While Not workerThread.IsAlive ' wait for thread activation
        End While
        While workerThread.IsAlive ' wait for thread termination
            Thread.Sleep(500)
        End While
    End Sub

    Private Sub TestPerf()
        If loopBreakFini = True Then Exit Sub
        progvalue = (Val(testIterations)) * 40
        Form15.Show()

        Form15.ProgressBar1.Value = progvalue
        Application.DoEvents()
        Try
            Dim firstPath As String
            Dim secondPath As String
            Dim shortType As String
            Dim iterText As String
            Dim appendIterations As ULong
            Dim totalPerf As Double
            Dim startTime As DateTime
            Dim stopTime As DateTime
            Dim randomText As String = RandomString(100000)
            If testType = "read" Then
                firstPath = testPath
                secondPath = localStoragePath
                shortType = "R"
            Else
                firstPath = localStoragePath
                secondPath = testPath
                shortType = "W"
            End If
            If testIterations = 1 Then
                iterText = "once"
            ElseIf testIterations = 2 Then
                iterText = "twice"
            Else
                iterText = testIterations & " times"
            End If
            If testFileSize = 0 Then

                appendIterations = 100 ' equals a 10MB warmup file.
            Else

                appendIterations = testFileSize \ 100000
                ' Note: dividing integers in VB.Net can produce a decimal number,
                ' so the integer division operator '\' is used instead of '/'
            End If
            totalPerf = 0
            For j As Integer = 1 To testIterations
                Application.DoEvents()
                If File.Exists(firstPath & "\" & j & "test.tmp") Then
                    File.Delete(firstPath & "\" & j & "test.tmp")
                End If
                If File.Exists(secondPath & "\" & j & "test.tmp") Then
                    File.Delete(secondPath & "\" & j & "test.tmp")
                End If

                Dim sWriter As New StreamWriter(firstPath &
                  "\" & j & "test.tmp", True, Encoding.UTF8, 1048576)
                For i As Integer = 1 To appendIterations
                    sWriter.Write(randomText)
                Next
                sWriter.Close()
                startTime = DateTime.Now
                File.Copy(firstPath & "\" & j & "test.tmp",
                  secondPath & "\" & j & "test.tmp")
                stopTime = DateTime.Now
                File.Delete(firstPath & "\" & j & "test.tmp")
                File.Delete(secondPath & "\" & j & "test.tmp")
                Dim interval As TimeSpan = stopTime - startTime
                If testIterations > 1 Then
                    NasSpeedValue = ((testFileSize \ 1000) / interval.TotalMilliseconds).ToString("F2").PadLeft(7) & " MB/sec"
                End If

                totalPerf += (testFileSize \ 1000) / interval.TotalMilliseconds
            Next
            If (testFileSize <> 0) AndAlso (loopBreak = False) Then
                NasSpeedValue = Trim((totalPerf / testIterations).ToString("F2").PadLeft(10) & " MB/sec")
                Form15.Hide()
                Application.DoEvents()
                MsgBox("La vittesse de " & DriveLetter & " est de " & NasSpeedValue)
                loopBreakFini = True
                workerThread.Abort()
            End If
        Catch e As Exception
            'MsgBox("An error occured: " & e.Message & vbCrLf)
            'workerThread.Abort()
            'MsgBox("La vittesse de " & DriveLetter & " est de " & NasSpeedValue)
        End Try


    End Sub

    Private Function RandomString(size As Integer) As String
        Dim builder As New StringBuilder()
        Dim random As New Random()
        Dim ch As Char
        For i As Integer = 0 To size - 1
            ch = Convert.ToChar(Convert.ToInt32(Math.Floor(26 * random.NextDouble() + 65)))
            builder.Append(ch)
        Next
        Return builder.ToString()
    End Function

    Private Function GetNetworkDriveLetters() As String()
        Dim NetworkDriveLetters As New System.Collections.ArrayList()
        Dim allDrives As DriveInfo() = DriveInfo.GetDrives()
        For Each d As DriveInfo In allDrives
            If d.DriveType = DriveType.Network Then
                NetworkDriveLetters.Add(d.Name.Substring(0, 1))
            End If
        Next
        Return TryCast(NetworkDriveLetters.ToArray(GetType(String)), String())
    End Function



End Module
