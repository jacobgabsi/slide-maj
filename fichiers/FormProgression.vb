Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

''' <summary>
''' Fenetre de progression des longs traitements (normalisation du volume des musiques), dans le
''' meme style que la fenetre de fermeture : carte sombre aux coins arrondis avec ombre, icone de
''' Slide, titre, etape, element en cours, temps restant, fine barre de progression et bouton
''' Annuler. Les methodes s'appellent sur le thread de l'interface (le traitement, en tache de
''' fond, passe par BeginInvoke) ; le traitement consulte Annule entre deux morceaux.
'''
''' Bouton "Réduire" : la fenetre devient une petite pastille en haut a gauche de Slide qui ne
''' montre que le pourcentage ; un clic sur la pastille rouvre la fenetre complete.
''' </summary>
Public Class FormProgression
    Inherits Form

    Private ReadOnly _fond As Color = Color.FromArgb(28, 28, 30)
    Private ReadOnly _bord As Color = Color.FromArgb(58, 58, 60)
    Private ReadOnly _texte As Color = Color.FromArgb(245, 245, 247)
    Private ReadOnly _texteDiscret As Color = Color.FromArgb(152, 152, 157)
    Private ReadOnly _piste As Color = Color.FromArgb(52, 52, 56)
    Private ReadOnly _accent As Color = Color.FromArgb(10, 132, 255)
    Private ReadOnly _bouton As Color = Color.FromArgb(58, 58, 62)
    Private ReadOnly _boutonSurvol As Color = Color.FromArgb(74, 74, 80)

    Private ReadOnly _titre As String
    Private _etape As String = "Préparation…"
    Private _detail As String = ""
    Private _fait As Integer
    Private _total As Integer = 1
    Private ReadOnly _chrono As Stopwatch = Stopwatch.StartNew()
    Private _icone As Icon
    Private ReadOnly _echelle As Single
    Private _survolAnnuler As Boolean
    Private _bilan As String = ""
    Private _survolReduire As Boolean
    Private _compact As Boolean
    Private _termine As Boolean
    Private _fermetureAutorisee As Boolean
    Private _tailleComplete As Size
    Private _positionComplete As Point
    Private _devant As Form
    Private WithEvents _minuteur As New Timer With {.Interval = 1500}

    ''' <summary>Une ligne du journal : texte a gauche, etiquette coloree a droite.</summary>
    Private Structure LigneJournal
        Public Texte As String
        Public Etiquette As String
        Public Couleur As Color
    End Structure
    Private ReadOnly _journal As New List(Of LigneJournal)
    Private Const LignesGardees As Integer = 12

    ''' <summary>Vrai des que l'utilisateur a clique sur Annuler (ou appuye sur Echap).</summary>
    Public ReadOnly Property Annule As Boolean

    ''' <summary>Affiche la fenetre centree sur la fenetre donnee, ou directement en pastille
    ''' reduite (lancement automatique au demarrage : sans prendre le clavier).</summary>
    Public Shared Function Afficher(titre As String, devant As Form, Optional reduite As Boolean = False) As FormProgression
        Dim f As New FormProgression(titre)
        Dim centre As Rectangle = Screen.PrimaryScreen.WorkingArea
        Try
            If devant IsNot Nothing AndAlso Not devant.IsDisposed AndAlso devant.Visible AndAlso
               devant.WindowState <> FormWindowState.Minimized Then
                centre = devant.Bounds
            End If
        Catch ex As Exception
        End Try
        f.Location = New Point(centre.Left + (centre.Width - f.Width) \ 2, centre.Top + (centre.Height - f.Height) \ 2)
        f._devant = devant
        If reduite Then f.Reduire(False)
        ' Sans proprietaire : la fenetre doit survivre a la fermeture des reglages.
        f.Show()
        f.Update()
        Return f
    End Function

    Public Sub New(titre As String)
        Using g As Graphics = Graphics.FromHwnd(IntPtr.Zero)
            _echelle = g.DpiX / 96.0F
        End Using
        _titre = titre
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.Manual
        Me.ShowInTaskbar = False
        Me.TopMost = True
        Me.KeyPreview = True
        Me.BackColor = _fond
        Me.Text = titre
        Me.ClientSize = New Size(Px(560), Px(400))
        Me.DoubleBuffered = True
        Try
            _icone = Icon.ExtractAssociatedIcon(Application.ExecutablePath)
        Catch ex As Exception
            _icone = Nothing
        End Try
    End Sub

    ''' <summary>Met a jour l'avancement et redessine tout de suite.</summary>
    Public Sub Avancer(etape As String, fait As Integer, total As Integer, Optional detail As String = "")
        _etape = etape
        _detail = detail
        _total = Math.Max(1, total)
        _fait = Math.Max(0, Math.Min(_total, fait))
        Me.Invalidate()
        Me.Update()
    End Sub

    ''' <summary>Vrai quand la fenetre est reduite a la pastille du pourcentage.</summary>
    Public ReadOnly Property Reduite As Boolean
        Get
            Return _compact
        End Get
    End Property

    ''' <summary>Reduit la fenetre a une pastille "37 %" en haut a gauche de Slide.</summary>
    Public Sub Reduire(Optional rendreLeClavier As Boolean = True)
        If _compact Then Exit Sub
        _compact = True
        _tailleComplete = Me.ClientSize
        _positionComplete = Me.Location
        _survolAnnuler = False : _survolReduire = False
        Me.ClientSize = New Size(Px(70), Px(30))
        PlacerLaPastille()
        Me.Cursor = Cursors.Hand
        Me.Invalidate()
        _minuteur.Interval = 500                            ' suit vite l'ouverture/fermeture des reglages
        _minuteur.Start()
        ' Le clavier revient a la fenetre d'ou la normalisation a ete lancee (ou a Slide).
        If rendreLeClavier Then
        Try
            Dim retour As Form = If(_devant IsNot Nothing AndAlso Not _devant.IsDisposed AndAlso _devant.Visible, _devant, FenetrePrincipale())
            If retour IsNot Nothing Then retour.Activate()
        Catch ex As Exception
        End Try
        End If
        ' Premier plan gere a la main (RangerLaPastille) : sous les reglages s'ils sont ouverts.
        Me.TopMost = False
        RangerLaPastille()
    End Sub

    ''' <summary>Rouvre la fenetre complete.</summary>
    Public Sub Agrandir()
        If Not _compact Then
            Me.Activate()
            Exit Sub
        End If
        _compact = False
        _minuteur.Stop()
        Me.TopMost = True                                   ' fenetre complete : devant tout
        CalqueWin32.ProtegerLaFenetre(Me.Handle, IntPtr.Zero)  ' et devant les PIP
        Me.ClientSize = _tailleComplete
        Me.Location = _positionComplete
        Me.Cursor = Cursors.Default
        Me.Invalidate()
        Me.Activate()
    End Sub

    ''' <summary>Fin du traitement. Pastille : "✓" quelques secondes, puis fermeture. Fenetre
    ''' complete : fermeture immediate (l'appelant affiche le bilan).</summary>
    Public Sub Terminer()
        _termine = True
        _fermetureAutorisee = True
        If _compact Then
            _fait = _total
            Me.Invalidate()
            _minuteur.Stop()
            Dim fin As New Timer With {.Interval = 4000}
            AddHandler fin.Tick, Sub()
                                     fin.Stop() : fin.Dispose()
                                     Me.Close()
                                 End Sub
            fin.Start()
        Else
            Me.Close()
        End If
    End Sub

    ''' <summary>Pastille en haut a gauche de l'image de Slide (ou de l'ecran si Slide est cache).</summary>
    Private Sub PlacerLaPastille()
        Dim zone As Rectangle = Screen.PrimaryScreen.WorkingArea
        Try
            Dim principale As Form = FenetrePrincipale()
            If principale IsNot Nothing AndAlso principale.Visible AndAlso principale.WindowState <> FormWindowState.Minimized Then
                zone = principale.RectangleToScreen(principale.ClientRectangle)
            End If
        Catch ex As Exception
        End Try
        Dim p As New Point(zone.Left + Px(12), zone.Top + Px(12))
        If Me.Location <> p Then Me.Location = p
    End Sub

    ''' <summary>Recale la pastille tout de suite, sans attendre le minuteur.</summary>
    Public Sub RecalerLaPastille()
        If _compact Then _minuteur_Tick(Me, EventArgs.Empty)
    End Sub

    Private Sub _minuteur_Tick(sender As Object, e As EventArgs) Handles _minuteur.Tick
        ' Slide remet regulierement ses PIP et sa fenetre devant : la pastille reste au-dessus et
        ' suit la fenetre (plein ecran, deplacement).
        If Not _compact OrElse Me.IsDisposed Then Exit Sub
        ' Comme les PIP : la pastille n'est visible que si la fenetre de Slide l'est.
        Dim principale As Form = FenetrePrincipale()
        Dim slideVisible As Boolean = principale IsNot Nothing AndAlso principale.Visible AndAlso
                                      principale.WindowState <> FormWindowState.Minimized
        If Not slideVisible Then
            If Me.Visible Then Me.Hide()
            Exit Sub
        End If
        If Not Me.Visible Then Me.Show()                    ' sans prendre le clavier (ShowWithoutActivation)
        PlacerLaPastille()
        RangerLaPastille()
    End Sub

    ''' <summary>La pastille passe SOUS la fenetre des reglages (Form4) quand elle est ouverte, et
    ''' reste au-dessus de tout le reste (diaporama, plein ecran, PIP).</summary>
    Private Sub RangerLaPastille()
        Try
            Dim reglages As Form = Nothing
            For Each f As Form In Application.OpenForms
                ' (des reglages restes ouverts DERRIERE l'image de Slide ne comptent pas : la pastille y serait cachee)
                If TypeOf f Is Form4 AndAlso f.Visible AndAlso f.WindowState <> FormWindowState.Minimized AndAlso CalqueWin32.EstDevantSlide(f.Handle) Then reglages = f : Exit For
            Next
            If reglages IsNot Nothing Then
                CalqueWin32.ProtegerLaFenetre(Me.Handle, reglages.Handle)
                CalqueWin32.PlacerDerriere(Me, reglages.Handle)
            ElseIf Not CalqueWin32.SlideAuPremierPlan() Then
                ' Un autre programme est devant Slide : la pastille passe derriere lui, avec Slide.
                CalqueWin32.ProtegerLaFenetre(Me.Handle, IntPtr.Zero)
                CalqueWin32.PlacerAuDessusDeSlide(Me)
            Else
                CalqueWin32.ProtegerLaFenetre(Me.Handle, IntPtr.Zero)
                CalqueWin32.DefinirPremierPlanSeul(Me, True)
            End If
        Catch ex As Exception
        End Try
    End Sub

    Protected Overrides Sub OnFormClosing(e As FormClosingEventArgs)
        ' Alt+F4 pendant le traitement : on reduit au lieu de fermer (le travail continue).
        If Not _fermetureAutorisee AndAlso e.CloseReason = CloseReason.UserClosing Then
            e.Cancel = True
            Reduire()
            Exit Sub
        End If
        _minuteur.Stop()
        CalqueWin32.ProtegerLaFenetre(IntPtr.Zero, IntPtr.Zero)
        MyBase.OnFormClosing(e)
    End Sub

    ''' <summary>Ajoute une ligne au journal (la plus recente en bas). Redessinee au prochain Avancer().</summary>
    Public Sub Journal(texte As String, etiquette As String, couleur As Color)
        _journal.Add(New LigneJournal With {.Texte = texte, .Etiquette = etiquette, .Couleur = couleur})
        If _journal.Count > LignesGardees Then _journal.RemoveAt(0)
    End Sub

    ''' <summary>Ligne de bilan au-dessus de la barre (compteurs).</summary>
    Public Sub Bilan(texte As String)
        _bilan = texte
    End Sub

    Private Function Px(valeur As Single) As Integer
        Return CInt(Math.Round(valeur * _echelle))
    End Function

    Private Function TempsRestant() As String
        If _fait <= 0 OrElse _fait >= _total OrElse _chrono.Elapsed.TotalSeconds < 3 Then Return ""
        Dim reste As Double = _chrono.Elapsed.TotalSeconds * (_total - _fait) / _fait
        If reste < 60 Then Return "environ " & Math.Max(5, CInt(Math.Ceiling(reste / 5) * 5)) & " s restantes"
        Dim minutes As Integer = CInt(Math.Ceiling(reste / 60))
        Return "environ " & minutes & " min restante" & If(minutes > 1, "s", "")
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Boutons Réduire et Annuler (dessines)
    ' ------------------------------------------------------------------------------------------

    Private Function ZoneAnnuler() As Rectangle
        Dim l As Integer = Px(96), h As Integer = Px(30), marge As Integer = Px(24)
        Return New Rectangle(Me.ClientSize.Width - marge - l, Me.ClientSize.Height - marge - h, l, h)
    End Function

    Private Function ZoneReduire() As Rectangle
        Dim a As Rectangle = ZoneAnnuler()
        Return New Rectangle(a.Left - Px(8) - a.Width, a.Top, a.Width, a.Height)
    End Function

    Private Sub DemanderAnnulation()
        If _Annule OrElse _compact Then Exit Sub
        _Annule = True
        _etape = "Annulation…"
        Me.Invalidate()
        Me.Update()
    End Sub

    Protected Overrides Sub OnMouseMove(e As MouseEventArgs)
        MyBase.OnMouseMove(e)
        If _compact Then Exit Sub
        Dim surAnnuler As Boolean = Not _Annule AndAlso ZoneAnnuler().Contains(e.Location)
        Dim surReduire As Boolean = ZoneReduire().Contains(e.Location)
        If surAnnuler <> _survolAnnuler OrElse surReduire <> _survolReduire Then
            _survolAnnuler = surAnnuler
            _survolReduire = surReduire
            Me.Cursor = If(surAnnuler OrElse surReduire, Cursors.Hand, Cursors.Default)
            Me.Invalidate(Rectangle.Union(ZoneAnnuler(), ZoneReduire()))
        End If
    End Sub

    Protected Overrides Sub OnMouseLeave(e As EventArgs)
        MyBase.OnMouseLeave(e)
        If _survolAnnuler OrElse _survolReduire Then
            _survolAnnuler = False : _survolReduire = False
            Me.Invalidate()
        End If
    End Sub

    Protected Overrides Sub OnMouseClick(e As MouseEventArgs)
        MyBase.OnMouseClick(e)
        If e.Button <> MouseButtons.Left Then Exit Sub
        If _compact Then
            If Not _termine Then Agrandir()
        ElseIf ZoneAnnuler().Contains(e.Location) Then
            DemanderAnnulation()
        ElseIf ZoneReduire().Contains(e.Location) Then
            Reduire()
        End If
    End Sub

    Protected Overrides Sub OnKeyDown(e As KeyEventArgs)
        MyBase.OnKeyDown(e)
        If e.KeyCode = Keys.Escape Then DemanderAnnulation()
    End Sub

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

    ''' <summary>Pastille : affichee sans prendre le clavier (lancement automatique).</summary>
    Protected Overrides ReadOnly Property ShowWithoutActivation As Boolean
        Get
            Return _compact
        End Get
    End Property

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        ' Les PIP ne doivent jamais passer devant la normalisation (voir CalqueWin32).
        If Not _compact Then CalqueWin32.ProtegerLaFenetre(Me.Handle, IntPtr.Zero)
        Try
            Dim preference As Integer = DWMWCP_ROUND
            DwmSetWindowAttribute(Me.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, preference, 4)
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
        If _compact Then
            PeindreLaPastille(g)
            Exit Sub
        End If

        Using stylo As New Pen(_bord, 1)
            g.DrawRectangle(stylo, 0, 0, Me.ClientSize.Width - 1, Me.ClientSize.Height - 1)
        End Using

        Dim marge As Integer = Px(24)
        Dim tailleIcone As Integer = Px(40)
        Dim xTexte As Integer = marge
        If _icone IsNot Nothing Then
            Using grande As New Icon(_icone, tailleIcone, tailleIcone)
                g.DrawIcon(grande, New Rectangle(marge, marge, tailleIcone, tailleIcone))
            End Using
            xTexte = marge + tailleIcone + Px(14)
        End If

        Dim pourcent As Integer = CInt(Math.Floor(_fait * 100.0 / _total))
        Dim zoneBouton As Rectangle = ZoneAnnuler()
        Dim hauteurBarre As Integer = Px(4)
        Dim piste As New Rectangle(marge, zoneBouton.Top - Px(18) - hauteurBarre, Me.ClientSize.Width - 2 * marge, hauteurBarre)

        Using titre As New Font("Segoe UI Semibold", 13.0F, FontStyle.Regular, GraphicsUnit.Point),
              texte As New Font("Segoe UI", 10.0F, FontStyle.Regular, GraphicsUnit.Point),
              petit As New Font("Segoe UI", 9.0F, FontStyle.Regular, GraphicsUnit.Point),
              pinceauTexte As New SolidBrush(_texte),
              pinceauDiscret As New SolidBrush(_texteDiscret)

            Dim format As New StringFormat(StringFormatFlags.NoWrap) With {.Trimming = StringTrimming.EllipsisPath}
            Dim aDroite As New StringFormat(StringFormatFlags.NoWrap) With {.Alignment = StringAlignment.Far}
            Dim largeurTexte As Integer = Me.ClientSize.Width - xTexte - marge

            ' Titre et pourcentage
            Dim yTitre As Integer = marge - Px(3)
            g.DrawString(_titre, titre, pinceauTexte, New RectangleF(xTexte, yTitre, largeurTexte - Px(60), titre.Height), format)
            g.DrawString(pourcent & " %", titre, pinceauTexte, New RectangleF(xTexte, yTitre, largeurTexte, titre.Height), aDroite)

            ' Etape, puis element en cours
            Dim yEtape As Integer = yTitre + titre.Height + Px(1)
            g.DrawString(_etape, texte, pinceauDiscret, New RectangleF(xTexte, yEtape, largeurTexte, texte.Height + Px(2)), format)
            If _detail <> "" Then
                g.DrawString(_detail, petit, pinceauDiscret, New RectangleF(xTexte, yEtape + texte.Height + Px(2), largeurTexte, petit.Height + Px(2)), format)
            End If

            ' Journal : les derniers morceaux traites, dans un cadre plus sombre
            Dim haut As Integer = yEtape + texte.Height + petit.Height + Px(14)
            Dim cadre As New Rectangle(marge, haut, Me.ClientSize.Width - 2 * marge, piste.Top - Px(32) - haut)
            Using fondJournal As New SolidBrush(Color.FromArgb(20, 20, 22))
                RemplirRectangleArrondi(g, fondJournal, cadre, Px(8))
            End Using
            Dim hLigne As Integer = petit.Height + Px(5)
            Dim y As Integer = cadre.Top + Px(8)
            Dim xG As Integer = cadre.Left + Px(12), largeurEtiquette As Integer = Px(150)
            Dim visibles As Integer = Math.Max(1, (cadre.Height - Px(16)) \ hLigne)
            Dim premiere As Integer = Math.Max(0, _journal.Count - visibles)
            Dim formatEtiquette As New StringFormat(StringFormatFlags.NoWrap) With {.Alignment = StringAlignment.Far}
            Dim formatNom As New StringFormat(StringFormatFlags.NoWrap) With {.Trimming = StringTrimming.EllipsisCharacter}
            For i As Integer = premiere To _journal.Count - 1
                Dim l As LigneJournal = _journal(i)
                ' Les lignes anciennes s'estompent un peu.
                Dim alpha As Integer = Math.Max(100, 255 - (_journal.Count - 1 - i) * 22)
                Using pt As New SolidBrush(Color.FromArgb(alpha, _texte)), pe As New SolidBrush(Color.FromArgb(alpha, l.Couleur))
                    g.DrawString(l.Texte, petit, pt, New RectangleF(xG, y, cadre.Width - largeurEtiquette - Px(32), hLigne), formatNom)
                    g.DrawString(l.Etiquette, petit, pe, New RectangleF(cadre.Right - Px(12) - largeurEtiquette, y, largeurEtiquette, hLigne), formatEtiquette)
                End Using
                y += hLigne
            Next
            If _journal.Count = 0 Then g.DrawString("En attente du premier morceau…", petit, pinceauDiscret, xG, y)

            ' Bilan
            If _bilan <> "" Then
                g.DrawString(_bilan, petit, pinceauDiscret, New RectangleF(marge, piste.Top - Px(24), piste.Width, petit.Height + Px(2)), formatNom)
            End If

            ' Barre
            Using pinceauPiste As New SolidBrush(_piste)
                RemplirArrondi(g, pinceauPiste, piste)
            End Using
            Dim largeurRemplie As Integer = CInt(piste.Width * _fait / _total)
            If largeurRemplie >= hauteurBarre Then
                Using pinceauAccent As New SolidBrush(_accent)
                    RemplirArrondi(g, pinceauAccent, New Rectangle(piste.Left, piste.Top, largeurRemplie, piste.Height))
                End Using
            End If

            ' Temps restant, a gauche du bouton
            Dim centreVertical As New StringFormat(StringFormatFlags.NoWrap) With {.LineAlignment = StringAlignment.Center}
            g.DrawString(TempsRestant(), petit, pinceauDiscret,
                         New RectangleF(marge, zoneBouton.Top, ZoneReduire().Left - marge - Px(8), zoneBouton.Height), centreVertical)

            ' Bouton Annuler
            Using fondBouton As New SolidBrush(If(_survolAnnuler, _boutonSurvol, _bouton)),
                  pinceauBouton As New SolidBrush(If(_Annule, _texteDiscret, _texte))
                RemplirRectangleArrondi(g, fondBouton, zoneBouton, Px(6))
                Dim centre As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                g.DrawString("Annuler", texte, pinceauBouton, zoneBouton, centre)
            End Using

            ' Bouton Réduire : la normalisation continue, seule une pastille "%" reste visible
            Dim zoneR As Rectangle = ZoneReduire()
            Using fondBouton As New SolidBrush(If(_survolReduire, _boutonSurvol, _bouton)),
                  pinceauBouton As New SolidBrush(_texte)
                RemplirRectangleArrondi(g, fondBouton, zoneR, Px(6))
                Dim centre As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
                g.DrawString("Réduire", texte, pinceauBouton, zoneR, centre)
            End Using
        End Using
    End Sub

    ''' <summary>Pastille reduite : le pourcentage seul, souligne d'une fine barre.</summary>
    Private Sub PeindreLaPastille(g As Graphics)
        Dim pourcent As Integer = CInt(Math.Floor(_fait * 100.0 / _total))
        Using police As New Font("Segoe UI Semibold", 10.5F, FontStyle.Regular, GraphicsUnit.Point),
              pinceau As New SolidBrush(If(_termine, Color.FromArgb(48, 209, 88), _texte))
            Dim centre As New StringFormat With {.Alignment = StringAlignment.Center, .LineAlignment = StringAlignment.Center}
            Dim zoneTexte As New Rectangle(0, 0, Me.ClientSize.Width, Me.ClientSize.Height - Px(3))
            g.DrawString(If(_termine, "✓ 100 %", pourcent & " %"), police, pinceau, zoneTexte, centre)
        End Using
        Dim piste As New Rectangle(Px(8), Me.ClientSize.Height - Px(6), Me.ClientSize.Width - Px(16), Px(3))
        Using pinceauPiste As New SolidBrush(_piste)
            RemplirArrondi(g, pinceauPiste, piste)
        End Using
        Dim largeur As Integer = CInt(piste.Width * _fait / _total)
        If largeur >= piste.Height Then
            Using pinceauAccent As New SolidBrush(_accent)
                RemplirArrondi(g, pinceauAccent, New Rectangle(piste.Left, piste.Top, largeur, piste.Height))
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

    Private Shared Sub RemplirRectangleArrondi(g As Graphics, pinceau As Brush, zone As Rectangle, rayon As Integer)
        Dim d As Integer = rayon * 2
        Using chemin As New GraphicsPath()
            chemin.AddArc(zone.Left, zone.Top, d, d, 180, 90)
            chemin.AddArc(zone.Right - d, zone.Top, d, d, 270, 90)
            chemin.AddArc(zone.Right - d, zone.Bottom - d, d, d, 0, 90)
            chemin.AddArc(zone.Left, zone.Bottom - d, d, d, 90, 90)
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
