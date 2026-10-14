Imports System.Runtime.InteropServices
Imports System.ServiceProcess
Imports System.Net.Mail



Module Module3
    '' simulation  mouse click
    Public Declare Sub mouse_event Lib "user32" (ByVal dwFlags As Integer,
      ByVal dx As Integer, ByVal dy As Integer, ByVal cButtons As Integer,
      ByVal dwExtraInfo As Integer)
    ''
    ''' <summary>
    '''  pour dimensionner une fenetre externe

    Private Const SW_SHOWMINIMIZED As Int32 = 2

    <DllImport("user32.dll", EntryPoint:="ShowWindow",
     CharSet:=CharSet.Auto)>
    Public Function ShowWindow(ByVal hwnd As Int32,
     ByVal nCmdShow As Int32) As Int32
    End Function   'ShowWindow API
    <DllImport("user32.dll", EntryPoint:="MoveWindow",
   CharSet:=CharSet.Auto)>
    Public Function MoveWindow(ByVal hWnd As IntPtr,
   ByVal X As Int32,
   ByVal Y As Int32,
   ByVal nWidth As Int32,
   ByVal nHeight As Int32,
   ByVal bRepaint As Boolean
   ) As Boolean
    End Function




    Private Const OrientationId As Integer = &H112
    Public Enum ExifOrientations As Byte
        Unknown = 0
        TopLeft = 1
        TopRight = 2
        BottomRight = 3
        BottomLeft = 4
        LeftTop = 5
        RightTop = 6
        RightBottom = 7
        LeftBottom = 8
    End Enum



    Public Function ImageOrientation(ByVal img As Image) As ExifOrientations
        ' Get the index of the orientation property.
        Dim orientation_index As Integer =
            Array.IndexOf(img.PropertyIdList, OrientationId)

        ' If there is no such property, return Unknown.
        If (orientation_index < 0) Then Return _
            ExifOrientations.Unknown

        ' Return the orientation value.
        Return DirectCast(img.GetPropertyItem(OrientationId).Value(0),
            ExifOrientations)
    End Function

    Public Function OrientationImage(ByVal orientation As _
    ExifOrientations) As Image
        Const size As Integer = 64
        Dim bm As New Bitmap(size, size)
        Using gr As Graphics = Graphics.FromImage(bm)
            gr.Clear(Color.White)
            gr.TextRenderingHint =
                System.Drawing.Text.TextRenderingHint.AntiAliasGridFit

            ' Orient the result.
            Select Case (orientation)
                Case ExifOrientations.TopLeft
                Case ExifOrientations.TopRight
                    gr.ScaleTransform(-1, 1)
                Case ExifOrientations.BottomRight
                    gr.RotateTransform(180)
                Case ExifOrientations.BottomLeft
                    gr.ScaleTransform(1, -1)
                Case ExifOrientations.LeftTop
                    gr.RotateTransform(90)
                    'gr.ScaleTransform(-1, 1, MatrixOrder.Append)
                Case ExifOrientations.RightTop
                    gr.RotateTransform(-90)
                Case ExifOrientations.RightBottom
                    gr.RotateTransform(90)
                    'gr.ScaleTransform(1, -1, MatrixOrder.Append)
                Case ExifOrientations.LeftBottom
                    gr.RotateTransform(90)
            End Select

            ' Translate the result to the center of the bitmap.
            'gr.TranslateTransform(size / 2, size / 2, MatrixOrder.Append)
            Using string_format As New StringFormat()
                string_format.LineAlignment =
                    StringAlignment.Center
                string_format.Alignment = StringAlignment.Center
                Using the_font As New Font("Times New Roman",
                    40, GraphicsUnit.Point)
                    If (orientation = ExifOrientations.Unknown) _
                        Then
                        gr.DrawString("?", the_font,
                            Brushes.Black, 0, 0, string_format)
                    Else
                        gr.DrawString("F", the_font,
                            Brushes.Black, 0, 0, string_format)
                    End If
                End Using
            End Using
        End Using

        Return bm
    End Function


    '''''''''''''''''''
    'Private Sub btnOpen_Click(ByVal sender As System.Object, _
    'ByVal e As System.EventArgs) Handles btnOpen.Click
    ' Open the file.
    'Dim bm As New Bitmap(txtFile.Text)
    '    picOriginal.Image = bm

    ' Get the PropertyItems property from image.
    'Dim Orientation As ExifOrientations = _
    '    ImageOrientation(bm)
    '    lblOrientation.Text = Orientation.ToString()
    '    picOrientation.Image = OrientationImage(Orientation)
    'End Sub



    '''''''''''''''''
    Function RotateImage(b As Bitmap, angle As Single) As Bitmap
        'create a new empty bitmap to hold rotated image
        Dim returnBitmap As New Bitmap(b.Width, b.Height)
        'make a graphics object from the empty bitmap
        Dim g As Graphics = Graphics.FromImage(returnBitmap)
        'move rotation point to center of image
        g.TranslateTransform(CSng(b.Width) / 2, CSng(b.Height) / 2)
        'rotate
        g.RotateTransform(angle)
        'move image back
        g.TranslateTransform(-CSng(b.Width) / 2, -CSng(b.Height) / 2)
        'draw passed in image onto graphics object
        g.DrawImage(b, New Point(0, 0))
        Return returnBitmap
    End Function


    Sub WindowsUpdate(x)

        Dim sc As New ServiceController("wuauserv")
        If x = "OFF" Then


            'MsgBox("The service status is currently set to " & sc.Status.ToString)
            Try
                sc.Stop()
            Catch ex As Exception

            End Try

        End If

        If x = "ON" Then
            'MsgBox("The service status is currently set to " & sc.Status.ToString)
            Try
                sc.Start()
            Catch ex As Exception

            End Try

        End If

        Exit Sub
        If sc.Status.Equals(ServiceControllerStatus.Stopped) Or sc.Status.Equals(ServiceControllerStatus.StopPending) Then
            ' Start the service if the current status is stopped.
            Console.WriteLine("Starting the Telnet service...")
            sc.Start()
        Else
            ' Stop the service if its status is not set to "Stopped".
            Console.WriteLine("Stopping the Telnet service...")
            sc.Stop()
        End If

        ' Refresh and display the current service status.
        sc.Refresh()
        Console.WriteLine("The Telnet service status is now set to {0}.", sc.Status)


    End Sub



End Module
