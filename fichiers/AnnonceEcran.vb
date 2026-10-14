Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Drawing.Imaging

''' <summary>
''' Annonce en TRES GROS au centre de l'image de Slide (demande de Jacob, 08/10/2026 : "affiche en
''' tres gros, centre a l'ecran, les commandes Deezer quand elles arrivent"). Elle reste deux
''' secondes et demie, ne prend pas le clavier et laisse passer les clics.
'''
''' Une seconde ligne, plus petite, peut dire le resultat (morceau ajoute aux Coups de coeur...).
''' </summary>
Friend Module AnnonceEcran

    Private Const DureeMs As Integer = 2500

    Private _calque As FormCalqueImage
    Private WithEvents _minuteur As New Timer With {.Interval = 200}
    Private _texte As String = ""
    Private _detail As String = ""
    Private _fin As Date

    ''' <summary>Montre <paramref name="texte"/> en tres gros (et <paramref name="detail"/> dessous,
    ''' plus petit). Peut etre appelee de n'importe quel fil.</summary>
    Friend Sub Montrer(texte As String, Optional detail As String = "")
        If ConfierAuFilPrincipal(Sub() Montrer(texte, detail), "Annonce") Then Exit Sub
        Try
            _texte = If(texte, "").Trim()
            _detail = If(detail, "").Trim()
            If _texte = "" Then Exit Sub
            _fin = Now.AddMilliseconds(DureeMs)
            Dessiner()
            _minuteur.Start()
        Catch ex As Exception
            DiagLog("Annonce : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Ajoute ou change la seconde ligne de l'annonce EN COURS (resultat d'une commande) ;
    ''' sans effet s'il n'y a pas a l'ecran d'annonce qui commence par <paramref name="debut"/>.</summary>
    Friend Sub Preciser(debut As String, detail As String)
        If ConfierAuFilPrincipal(Sub() Preciser(debut, detail), "Annonce") Then Exit Sub
        Try
            If _calque Is Nothing OrElse _calque.IsDisposed OrElse Not _calque.Visible Then Exit Sub
            If Not _texte.StartsWith(If(debut, ""), StringComparison.OrdinalIgnoreCase) Then Exit Sub
            _detail = If(detail, "").Trim()
            _fin = Now.AddMilliseconds(DureeMs)
            Dessiner()
        Catch ex As Exception
            DiagLog("Annonce : " & ex.Message)
        End Try
    End Sub

    Private Sub Dessiner()
        Dim principale As Form = FenetrePrincipale()
        If principale Is Nothing OrElse principale.IsDisposed OrElse Not principale.Visible OrElse
           principale.WindowState = FormWindowState.Minimized Then
            Masquer()
            Exit Sub
        End If
        Dim zone As Rectangle = principale.RectangleToScreen(principale.ClientRectangle)
        If zone.Width < 100 OrElse zone.Height < 100 Then Exit Sub
        If _calque Is Nothing OrElse _calque.IsDisposed Then _calque = New FormCalqueImage With {.ClicsTraversants = True}
        Using image As Bitmap = Rendre(_texte, _detail, zone.Size)
            _calque.Montrer(image, New Point(zone.Left + (zone.Width - image.Width) \ 2, zone.Top + (zone.Height - image.Height) \ 2),
                            principale, FullScreen)
        End Using
    End Sub

    Private Sub Masquer()
        _minuteur.Stop()
        If _calque IsNot Nothing AndAlso Not _calque.IsDisposed Then _calque.Masquer()
    End Sub

    Private Sub Minuteur_Tick(sender As Object, e As EventArgs) Handles _minuteur.Tick
        If Now >= _fin Then Masquer()
    End Sub

    ''' <summary>Le texte en tres gros dans un panneau sombre arrondi : hauteur des lettres = un
    ''' septieme de la hauteur de l'image de Slide, reduite si le texte ne tient pas dans 90 % de
    ''' sa largeur. Toutes les tailles sont en pixels (aucune dependance a la resolution).</summary>
    Friend Function Rendre(texte As String, detail As String, zone As Size) As Bitmap
        Dim taille As Single = zone.Height / 7.0F
        Dim largeurMax As Single = zone.Width * 0.9F
        Using famille As FontFamily = FormHorloge.PoliceChoisieOu("Segoe UI"), format As StringFormat = CType(StringFormat.GenericTypographic.Clone(), StringFormat)
            Dim grand As New GraphicsPath(), petit As New GraphicsPath()
            Try
                Dim bornes As RectangleF
                For essai As Integer = 1 To 6
                    grand.Reset()
                    grand.AddString(texte, famille, CInt(FontStyle.Bold), taille, PointF.Empty, format)
                    bornes = grand.GetBounds()
                    If bornes.Width <= largeurMax - 2 * taille * 0.45F OrElse taille < 24 Then Exit For
                    taille *= (largeurMax - 2 * taille * 0.45F) / bornes.Width * 0.98F
                Next
                Dim marge As Single = taille * 0.45F
                Dim tailleDetail As Single = taille * 0.4F
                Dim bornesDetail As RectangleF = RectangleF.Empty
                If detail <> "" Then
                    For essai As Integer = 1 To 6
                        petit.Reset()
                        petit.AddString(detail, famille, CInt(FontStyle.Regular), tailleDetail, PointF.Empty, format)
                        bornesDetail = petit.GetBounds()
                        If bornesDetail.Width <= largeurMax - 2 * marge OrElse tailleDetail < 14 Then Exit For
                        tailleDetail *= (largeurMax - 2 * marge) / bornesDetail.Width * 0.98F
                    Next
                End If
                Dim interligne As Single = If(detail = "", 0, taille * 0.3F)
                Dim largeur As Integer = CInt(Math.Ceiling(Math.Max(bornes.Width, bornesDetail.Width) + 2 * marge))
                Dim hauteur As Integer = CInt(Math.Ceiling(bornes.Height + interligne + bornesDetail.Height + 2 * marge))
                Dim image As New Bitmap(Math.Max(1, largeur), Math.Max(1, hauteur), PixelFormat.Format32bppArgb)
                Using g As Graphics = Graphics.FromImage(image)
                    g.Clear(Color.FromArgb(0, 0, 0, 0))
                    g.SmoothingMode = SmoothingMode.AntiAlias
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality
                    Using panneau As GraphicsPath = Arrondi(New RectangleF(0, 0, largeur, hauteur), marge * 0.7F),
                          fond As New SolidBrush(Color.FromArgb(175, 12, 12, 14))
                        g.FillPath(fond, panneau)
                    End Using
                    Using m As New Matrix()
                        m.Translate((largeur - bornes.Width) / 2 - bornes.Left, marge - bornes.Top)
                        grand.Transform(m)
                    End Using
                    g.FillPath(Brushes.White, grand)
                    If detail <> "" Then
                        Using m As New Matrix()
                            m.Translate((largeur - bornesDetail.Width) / 2 - bornesDetail.Left, marge + bornes.Height + interligne - bornesDetail.Top)
                            petit.Transform(m)
                        End Using
                        Using gris As New SolidBrush(Color.FromArgb(255, 205, 205, 210))
                            g.FillPath(gris, petit)
                        End Using
                    End If
                End Using
                Return image
            Finally
                grand.Dispose()
                petit.Dispose()
            End Try
        End Using
    End Function

    ''' <summary>Rectangle aux coins arrondis.</summary>
    Friend Function Arrondi(r As RectangleF, rayon As Single) As GraphicsPath
        Dim d As Single = Math.Max(1.0F, Math.Min(rayon * 2, Math.Min(r.Width, r.Height)))
        Dim chemin As New GraphicsPath()
        chemin.AddArc(r.Left, r.Top, d, d, 180, 90)
        chemin.AddArc(r.Right - d, r.Top, d, d, 270, 90)
        chemin.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        chemin.AddArc(r.Left, r.Bottom - d, d, d, 90, 90)
        chemin.CloseFigure()
        Return chemin
    End Function

End Module
