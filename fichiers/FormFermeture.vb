Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

''' <summary>
''' Fenetre affichee pendant la fermeture de Slide (menu "End", touche Echap, bouton "Quitter le
''' programme" des reglages). Remplace Form15 pour la fermeture : Form15 reste la barre de
''' progression de la normalisation du volume et de la synchro USB.
'''
''' Carte sombre aux coins arrondis (Windows 11) avec ombre : icone et version de Slide, etape en
''' cours, fine barre de progression. La fermeture tourne sur le thread de l'interface : chaque
''' Etape() redessine la fenetre tout de suite (pas de minuteur, il ne tournerait pas).
''' </summary>
Public Class FormFermeture
    Inherits Form

    Private ReadOnly _fond As Color = Color.FromArgb(28, 28, 30)
    Private ReadOnly _bord As Color = Color.FromArgb(58, 58, 60)
    Private ReadOnly _texte As Color = Color.FromArgb(245, 245, 247)
    Private ReadOnly _texteDiscret As Color = Color.FromArgb(152, 152, 157)
    Private ReadOnly _piste As Color = Color.FromArgb(52, 52, 56)
    Private ReadOnly _accent As Color = Color.FromArgb(10, 132, 255)

    Private _etape As String = "Fermeture en cours…"
    Private _pourcent As Integer
    Private _icone As Icon
    Private ReadOnly _echelle As Single

    ''' <summary>Affiche la fenetre, centree sur la fenetre de Slide (ou l'ecran principal).</summary>
    Public Shared Function Afficher(Optional devant As Form = Nothing) As FormFermeture
        Dim chrono As Stopwatch = Stopwatch.StartNew()
        Dim f As New FormFermeture()
        Dim ecran As Rectangle = If(devant IsNot Nothing AndAlso Not devant.IsDisposed,
                                    Screen.FromControl(devant).WorkingArea,
                                    Screen.PrimaryScreen.WorkingArea)
        Dim centre As Rectangle = ecran
        Try
            ' Centree sur l'image de Slide si elle est a l'ecran, sinon sur l'ecran.
            If devant IsNot Nothing AndAlso devant.Visible AndAlso devant.WindowState <> FormWindowState.Minimized Then
                centre = devant.RectangleToScreen(devant.ClientRectangle)
            End If
        Catch ex As Exception
        End Try
        f.Location = New Point(centre.Left + (centre.Width - f.Width) \ 2, centre.Top + (centre.Height - f.Height) \ 2)
        Dim msCreation As Long = chrono.ElapsedMilliseconds
        f.Show()
        Dim msAffichage As Long = chrono.ElapsedMilliseconds - msCreation
        f.Update()
        DetailDeLOuverture = "creation " & msCreation & ", affichage " & msAffichage & ", dessin " & (chrono.ElapsedMilliseconds - msCreation - msAffichage)
        Return f
    End Function

    ''' <summary>Temps de la derniere ouverture de la fenetre, en millisecondes, pour le journal
    ''' (ligne "Fermeture : ..." de Form3).</summary>
    Public Shared DetailDeLOuverture As String = ""

    Public Sub New()
        ' Echelle de l'ecran (sans creer la fenetre : ses proprietes ne sont pas encore posees).
        Using g As Graphics = Graphics.FromHwnd(IntPtr.Zero)
            _echelle = g.DpiX / 96.0F
        End Using
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.Manual
        Me.ShowInTaskbar = False
        Me.TopMost = True
        Me.BackColor = _fond
        Me.Text = "Fermeture de Slide"
        Me.ClientSize = New Size(Px(440), Px(150))
        Me.DoubleBuffered = True
        Me.Cursor = Cursors.WaitCursor
        _icone = IconeDuProgramme(Px(48))
    End Sub

    ''' <summary>
    ''' L'icone de Slide, prise dans le programme DEJA CHARGE en memoire. Avant (13/10/2026) elle etait
    ''' demandee a Windows d'apres le chemin de l'exe : Slide tourne depuis un lecteur reseau, et cette
    ''' demande faisait charger a Windows ses modules de reseau au moment meme de la fermeture - la
    ''' fenetre mettait 0,8 s a apparaitre (journal : "fenetre 771 ms"). Si l'exe n'a pas d'icone a cet
    ''' endroit, on revient a l'ancienne methode.
    ''' </summary>
    Private Shared Function IconeDuProgramme(cote As Integer) As Icon
        Try
            ' 32512 : numero de l'icone principale d'un programme .NET ; 1 = IMAGE_ICON.
            Dim h As IntPtr = LoadImage(GetModuleHandle(Nothing), New IntPtr(32512), 1, cote, cote, 0)
            If h <> IntPtr.Zero Then
                Try
                    Using provisoire As Icon = Icon.FromHandle(h)
                        Return DirectCast(provisoire.Clone(), Icon)
                    End Using
                Finally
                    DestroyIcon(h)
                End Try
            End If
        Catch ex As Exception
        End Try
        Try
            Return Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch ex As Exception
            Return Nothing
        End Try
    End Function

    <DllImport("kernel32.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function GetModuleHandle(nom As String) As IntPtr
    End Function
    <DllImport("user32.dll", CharSet:=CharSet.Unicode, EntryPoint:="LoadImageW")>
    Private Shared Function LoadImage(module_ As IntPtr, nom As IntPtr, genre As UInteger, largeur As Integer, hauteur As Integer, options As UInteger) As IntPtr
    End Function
    <DllImport("user32.dll")>
    Private Shared Function DestroyIcon(icone As IntPtr) As Boolean
    End Function

    ''' <summary>Passe a l'etape suivante et redessine aussitot.</summary>
    Public Sub Etape(texte As String, pourcent As Integer)
        _etape = texte
        _pourcent = Math.Max(0, Math.Min(100, pourcent))
        Me.Invalidate()
        Me.Update()
    End Sub

    Private Function Px(valeur As Single) As Integer
        Return CInt(Math.Round(valeur * _echelle))
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Ombre et coins arrondis
    ' ------------------------------------------------------------------------------------------

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ClassStyle = cp.ClassStyle Or CS_DROPSHADOW
            cp.ExStyle = cp.ExStyle Or CalqueWin32.WS_EX_TOOLWINDOW
            Return cp
        End Get
    End Property

    ''' <summary>Ne prend pas le focus : le clavier reste a Slide pendant la fermeture.</summary>
    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return True
        End Get
    End Property

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        Try
            ' Windows 11 : vrais coins arrondis, avec l'ombre du systeme. Sans effet sur Windows 10.
            Dim preference As Integer = DWMWCP_ROUND
            DwmSetWindowAttribute(Me.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, preference, 4)
            ' Liseré du systeme dans la couleur du bord (COLORREF = 0x00BBGGRR).
            Dim couleurBord As Integer = _bord.R Or (CInt(_bord.G) << 8) Or (CInt(_bord.B) << 16)
            DwmSetWindowAttribute(Me.Handle, DWMWA_BORDER_COLOR, couleurBord, 4)
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Dessin
    ' ------------------------------------------------------------------------------------------

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        g.TextRenderingHint = Drawing.Text.TextRenderingHint.ClearTypeGridFit
        g.Clear(_fond)

        Using stylo As New Pen(_bord, 1)
            g.DrawRectangle(stylo, 0, 0, Me.ClientSize.Width - 1, Me.ClientSize.Height - 1)
        End Using

        Dim marge As Integer = Px(24)
        Dim tailleIcone As Integer = Px(48)
        Dim xTexte As Integer = marge

        If _icone IsNot Nothing Then
            Using grande As New Icon(_icone, tailleIcone, tailleIcone)
                g.DrawIcon(grande, New Rectangle(marge, marge, tailleIcone, tailleIcone))
            End Using
            xTexte = marge + tailleIcone + Px(16)
        End If

        Using titre As New Font("Segoe UI Semibold", 15.0F, FontStyle.Regular, GraphicsUnit.Point),
              version As New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point),
              etape As New Font("Segoe UI", 10.0F, FontStyle.Regular, GraphicsUnit.Point),
              pinceauTexte As New SolidBrush(_texte),
              pinceauDiscret As New SolidBrush(_texteDiscret)

            Dim format As New StringFormat(StringFormatFlags.NoWrap) With {.Trimming = StringTrimming.EllipsisCharacter}
            Dim largeurTexte As Integer = Me.ClientSize.Width - xTexte - marge

            Dim yTitre As Integer = marge - Px(4)
            g.DrawString("Slide", titre, pinceauTexte, xTexte, yTitre)
            Dim largeurTitre As Single = g.MeasureString("Slide", titre).Width
            g.DrawString("v" & SlideVersionAuto, version, pinceauDiscret,
                         xTexte + largeurTitre, yTitre + (titre.Height - version.Height) - Px(3))

            g.DrawString(_etape, etape, pinceauDiscret,
                         New RectangleF(xTexte, yTitre + titre.Height + Px(2), largeurTexte, etape.Height + Px(4)), format)
        End Using

        ' Barre de progression : fine, arrondie, sous tout le contenu.
        Dim hauteurBarre As Integer = Px(4)
        Dim piste As New Rectangle(marge, Me.ClientSize.Height - marge - hauteurBarre,
                                   Me.ClientSize.Width - 2 * marge, hauteurBarre)
        Using pinceauPiste As New SolidBrush(_piste)
            RemplirArrondi(g, pinceauPiste, piste)
        End Using
        Dim largeurRemplie As Integer = CInt(piste.Width * _pourcent / 100.0)
        If largeurRemplie >= hauteurBarre Then
            Using pinceauAccent As New SolidBrush(_accent)
                RemplirArrondi(g, pinceauAccent, New Rectangle(piste.Left, piste.Top, largeurRemplie, piste.Height))
            End Using
        End If
    End Sub

    Private Shared Sub RemplirArrondi(g As Graphics, pinceau As Brush, zone As Rectangle)
        Dim d As Integer = zone.Height
        Using chemin As New GraphicsPath()
            chemin.AddArc(zone.Left, zone.Top, d, d, 90, 180)
            chemin.AddArc(zone.Right - d, zone.Top, d, d, 270, 180)
            chemin.CloseFigure()
            g.FillPath(pinceau, chemin)
        End Using
    End Sub

    Protected Overrides Sub Dispose(disposing As Boolean)
        If disposing AndAlso _icone IsNot Nothing Then _icone.Dispose() : _icone = Nothing
        MyBase.Dispose(disposing)
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Win32
    ' ------------------------------------------------------------------------------------------

    Private Const CS_DROPSHADOW As Integer = &H20000
    Private Const DWMWA_WINDOW_CORNER_PREFERENCE As Integer = 33
    Private Const DWMWCP_ROUND As Integer = 2
    Private Const DWMWA_BORDER_COLOR As Integer = 34

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attribut As Integer, ByRef valeur As Integer, taille As Integer) As Integer
    End Function

End Class
