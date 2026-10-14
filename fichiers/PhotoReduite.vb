Option Strict On
Option Explicit On

Imports System.IO
Imports System.Windows.Media
Imports System.Windows.Media.Imaging

''' <summary>
''' Lecture rapide d'une photo, deja reduite a la taille voulue et remise a l'endroit (08/11/2026).
'''
''' Le clic droit de l'autre PC (4K) mettait jusqu'a 4,7 s a se DESSINER (« Menu : premier dessin 4700 ms apres le debut de l'ouverture ») quand un album se preparait : lire en GDI+ des
''' photos de 12 a 24 millions de points, puis les reduire en bicubique, occupe GDI+ (verrouille pour tout le processus) et fait attendre le dessin du menu, meme dans une tache de fond.
''' Ici la lecture passe par WIC (Windows Imaging Component, comme les photos animees) : le JPEG est decode DIRECTEMENT a la taille voulue (reduction a la lecture, sans GDI+),
''' ce qui est de 5 a 10 fois plus rapide et ne bloque pas le dessin des fenetres. En cas d'echec (format que WIC ne lit pas), l'ancienne lecture GDI+ prend le relais.
''' </summary>
Friend Module PhotoReduite

    ''' <summary>La photo reduite (jamais agrandie) pour tenir dans <paramref name="maxL"/> x <paramref name="maxH"/>, au format 32 bits premultiplie, remise a l'endroit si <paramref name="tourner"/>
    ''' (<paramref name="orientation"/> : code EXIF lu). Nothing si WIC ne sait pas la lire : l'appelant retombe alors sur GDI+.</summary>
    Friend Function Lire(nom As String, maxL As Integer, maxH As Integer, tourner As Boolean, ByRef orientation As Integer) As System.Drawing.Bitmap
        orientation = 0
        Try
            Using flux As New FileStream(nom, FileMode.Open, FileAccess.Read, FileShare.ReadWrite)
                Dim largeur As Integer, hauteur As Integer
                Dim decodeur As BitmapDecoder = BitmapDecoder.Create(flux, BitmapCreateOptions.DelayCreation Or BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None)
                Dim cadre As BitmapFrame = decodeur.Frames(0)
                largeur = cadre.PixelWidth
                hauteur = cadre.PixelHeight
                If tourner Then
                    Try
                        Dim meta As BitmapMetadata = TryCast(cadre.Metadata, BitmapMetadata)
                        If meta IsNot Nothing Then
                            Dim o As Object = meta.GetQuery("/app1/ifd/{ushort=274}")
                            If o IsNot Nothing Then orientation = Convert.ToInt32(o)
                        End If
                    Catch ex As Exception
                        orientation = 0
                    End Try
                End If
                If largeur < 1 OrElse hauteur < 1 Then Return Nothing

                ' Dimensions apres rotation (quart de tour : largeur et hauteur echangees) ; echelle jamais superieure a 1.
                Dim quartDeTour As Boolean = (orientation = 5 OrElse orientation = 6 OrElse orientation = 8)
                Dim rl As Integer = If(quartDeTour, hauteur, largeur)
                Dim rh As Integer = If(quartDeTour, largeur, hauteur)
                Dim echelle As Double = Math.Min(1.0, Math.Min(maxL / CDbl(rl), maxH / CDbl(rh)))

                flux.Position = 0
                Dim image As New BitmapImage()
                image.BeginInit()
                image.CacheOption = BitmapCacheOption.OnLoad
                image.CreateOptions = BitmapCreateOptions.IgnoreColorProfile
                If echelle < 1.0 Then image.DecodePixelWidth = Math.Max(1, CInt(largeur * echelle))
                image.StreamSource = flux
                image.EndInit()
                image.Freeze()

                Dim source As BitmapSource = image
                Select Case orientation
                    Case 3
                        source = New TransformedBitmap(source, New RotateTransform(180))
                    Case 4
                        Dim groupe As New TransformGroup()
                        groupe.Children.Add(New RotateTransform(180))
                        groupe.Children.Add(New ScaleTransform(-1, 1))
                        source = New TransformedBitmap(source, groupe)
                    Case 5, 6
                        source = New TransformedBitmap(source, New RotateTransform(90))
                    Case 8
                        source = New TransformedBitmap(source, New RotateTransform(270))
                End Select
                Dim converti As New FormatConvertedBitmap(source, PixelFormats.Pbgra32, Nothing, 0)
                converti.Freeze()

                Dim w As Integer = converti.PixelWidth
                Dim h As Integer = converti.PixelHeight
                Dim bitmap As New System.Drawing.Bitmap(w, h, System.Drawing.Imaging.PixelFormat.Format32bppPArgb)
                Dim zone As New System.Drawing.Rectangle(0, 0, w, h)
                Dim donnees As System.Drawing.Imaging.BitmapData = bitmap.LockBits(zone, System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb)
                Try
                    converti.CopyPixels(System.Windows.Int32Rect.Empty, donnees.Scan0, donnees.Stride * h, donnees.Stride)
                Finally
                    bitmap.UnlockBits(donnees)
                End Try
                Return bitmap
            End Using
        Catch ex As Exception
            DiagLog("Photo : lecture rapide impossible (" & ex.GetType().Name & " : " & ex.Message & ") : " & Path.GetFileName(nom) & " ; lecture classique")
            orientation = 0
            Return Nothing
        End Try
    End Function

End Module
