Option Strict On
Option Explicit On

Imports System.Drawing.Drawing2D
Imports System.Runtime.InteropServices

''' <summary>
''' Fenetre qui presente les incoherences de la minuterie quand on valide les reglages par OK
''' (voir CoherenceMinuterie). Elle remplace la boite de message de Windows, jugee "pas
''' professionnelle" (Jacob, 06/10) : meme style que la fenetre de fermeture et celle de la
''' normalisation (carte sombre aux coins arrondis), une carte par probleme avec les numeros de
''' lignes dans une pastille, et deux boutons :
'''   - "Corriger" (bouton par defaut, Entree et Echap) : retour au tableau, sur les lignes du
'''     probleme selectionne ; un double-clic sur une carte fait de meme ;
'''   - "Enregistrer quand meme" : l'enregistrement continue.
''' Les controles sont dans le Concepteur (FormCoherence.Designer.vb) ; seul le dessin des
''' cartes, de l'icone et du bord est fait ici.
''' </summary>
Public Class FormCoherence

    Private ReadOnly _fond As Color = Color.FromArgb(28, 28, 30)
    Private ReadOnly _bord As Color = Color.FromArgb(58, 58, 60)
    Private ReadOnly _carte As Color = Color.FromArgb(44, 44, 46)
    Private ReadOnly _carteChoisie As Color = Color.FromArgb(54, 54, 58)
    Private ReadOnly _texte As Color = Color.FromArgb(245, 245, 247)
    Private ReadOnly _texteDiscret As Color = Color.FromArgb(174, 174, 178)
    Private ReadOnly _accent As Color = Color.FromArgb(10, 132, 255)
    Private ReadOnly _alerte As Color = Color.FromArgb(255, 159, 10)
    Private ReadOnly _erreur As Color = Color.FromArgb(255, 69, 58)
    Private _alerteEnCours As Color = Color.FromArgb(255, 159, 10)     ' couleur de l'icone : rouge s'il y a une erreur

    Private _problemes As New List(Of CoherenceMinuterie.Incoherence)
    Private _policeTitre As Font
    Private _policePastille As Font

    ''' <summary>Vrai si l'on a choisi "Enregistrer quand meme".</summary>
    Public Property Enregistrer As Boolean

    ''' <summary>Probleme a corriger (celui qui etait selectionne), Nothing si aucun.</summary>
    Public Property ProblemeChoisi As CoherenceMinuterie.Incoherence

    ''' <summary>Montre les problemes et attend le choix. Renvoie Vrai pour "Enregistrer quand
    ''' meme" ; sinon <paramref name="aCorriger"/> donne le probleme sur lequel revenir.</summary>
    Public Shared Function Demander(proprietaire As Form, problemes As List(Of CoherenceMinuterie.Incoherence),
                                    ByRef aCorriger As CoherenceMinuterie.Incoherence) As Boolean
        Using f As New FormCoherence()
            f.Preparer(problemes)
            If proprietaire IsNot Nothing AndAlso proprietaire.TopMost Then f.TopMost = True
            f.ShowDialog(proprietaire)
            aCorriger = f.ProblemeChoisi
            Return f.Enregistrer
        End Using
    End Function

    ''' <summary>Remplit la fenetre et ajuste sa hauteur au nombre de problemes.</summary>
    Public Sub Preparer(problemes As List(Of CoherenceMinuterie.Incoherence))
        ' Ce qu'il FAUT corriger d'abord, puis les avertissements (chaque groupe garde son ordre).
        _problemes = If(problemes, New List(Of CoherenceMinuterie.Incoherence)).OrderByDescending(Function(p) p.Bloquant).ToList()
        Dim erreurs As Integer = _problemes.Where(Function(p) p.Bloquant).Count()
        Dim avertissements As Integer = _problemes.Count - erreurs
        _alerteEnCours = If(erreurs > 0, _erreur, _alerte)
        LabelTitre.Text = "Réglages : " & If(_problemes.Count = 1, "1 point à vérifier", _problemes.Count & " points à vérifier")
        If erreurs > 0 Then
            LabelSousTitre.Text = erreurs & If(erreurs > 1, " erreurs à corriger", " erreur à corriger") & " avant de pouvoir enregistrer" &
                                  If(avertissements > 0, ", et " & avertissements & If(avertissements > 1, " avertissements.", " avertissement."), ".")
            LabelAide.Text = "En rouge : à corriger avant d'enregistrer"
        Else
            LabelSousTitre.Text = If(avertissements > 1, "Avertissements", "Avertissement") & " : tu peux corriger, ou enregistrer quand même."
            LabelAide.Text = "Double-clic : aller à l'endroit à corriger"
        End If
        ' "Enregistrer quand meme" n'est propose que s'il n'y a que des avertissements.
        ButtonEnregistrer.Enabled = (erreurs = 0)
        ButtonEnregistrer.ForeColor = If(erreurs = 0, _texte, Color.FromArgb(110, 110, 115))
        PictureBoxIcone.Invalidate()
        If _policeTitre Is Nothing Then
            _policeTitre = New Font("Segoe UI Semibold", ListBoxProblemes.Font.Size, FontStyle.Bold, GraphicsUnit.Point)
            _policePastille = New Font("Segoe UI Semibold", ListBoxProblemes.Font.Size * 0.88F, FontStyle.Bold, GraphicsUnit.Point)
        End If

        ' La hauteur de chaque carte depend de la largeur de la liste : calculee une premiere fois
        ' sans barre de defilement, puis avec si tout ne tient pas.
        Dim bas As Integer = Me.ClientSize.Height - ListBoxProblemes.Bottom          ' zone des boutons
        Dim maxi As Integer = CInt(Screen.FromControl(Me).WorkingArea.Height * 0.55)
        Remplir()
        Dim total As Integer = HauteurDesCartes()
        Dim hauteurListe As Integer = Math.Max(Echelle(70), Math.Min(total, maxi))
        Me.ClientSize = New Size(Me.ClientSize.Width, ListBoxProblemes.Top + hauteurListe + bas)
        If total > hauteurListe Then Remplir()                                        ' avec la barre de defilement
        If ListBoxProblemes.Items.Count > 0 Then ListBoxProblemes.SelectedIndex = 0
    End Sub

    Private Sub Remplir()
        ListBoxProblemes.BeginUpdate()
        ListBoxProblemes.Items.Clear()
        For Each p As CoherenceMinuterie.Incoherence In _problemes
            ListBoxProblemes.Items.Add(p)
        Next
        ListBoxProblemes.EndUpdate()
    End Sub

    Private Function HauteurDesCartes() As Integer
        Dim total As Integer = 0
        For i As Integer = 0 To ListBoxProblemes.Items.Count - 1
            total += ListBoxProblemes.GetItemHeight(i)
        Next
        Return total
    End Function

    Private Function Echelle(valeur As Integer) As Integer
        Return CInt(Math.Round(valeur * Me.DeviceDpi / 96.0))
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Les cartes
    ' ------------------------------------------------------------------------------------------

    Private Const DrapeauxTexte As TextFormatFlags = TextFormatFlags.NoPadding Or TextFormatFlags.NoPrefix

    ''' <summary>Largeur disponible pour le texte d'une carte.</summary>
    Private Function LargeurDuTexte() As Integer
        Return Math.Max(Echelle(120), ListBoxProblemes.ClientSize.Width - 2 * Echelle(14) - Echelle(4))
    End Function

    Private Sub ListBoxProblemes_MeasureItem(sender As Object, e As MeasureItemEventArgs) Handles ListBoxProblemes.MeasureItem
        If e.Index < 0 OrElse e.Index >= ListBoxProblemes.Items.Count OrElse _policeTitre Is Nothing Then Exit Sub
        Dim p As CoherenceMinuterie.Incoherence = CType(ListBoxProblemes.Items(e.Index), CoherenceMinuterie.Incoherence)
        Dim hauteurEntete As Integer = Math.Max(_policeTitre.Height, _policePastille.Height + Echelle(4))
        Dim detail As Size = TextRenderer.MeasureText(e.Graphics, p.Detail, ListBoxProblemes.Font, New Size(LargeurDuTexte(), Integer.MaxValue),
                                                      DrapeauxTexte Or TextFormatFlags.WordBreak)
        ' Marge interieure, en-tete (pastille + titre), detail, marge, puis l'ecart entre deux cartes.
        Dim h As Integer = Echelle(12) + hauteurEntete + Echelle(5) + detail.Height + Echelle(12) + Echelle(8)
        e.ItemHeight = Math.Min(255, h)                    ' limite de Windows pour une ligne de liste
    End Sub

    Private Sub ListBoxProblemes_DrawItem(sender As Object, e As DrawItemEventArgs) Handles ListBoxProblemes.DrawItem
        If e.Index < 0 OrElse e.Index >= ListBoxProblemes.Items.Count OrElse _policeTitre Is Nothing Then Exit Sub
        Dim p As CoherenceMinuterie.Incoherence = CType(ListBoxProblemes.Items(e.Index), CoherenceMinuterie.Incoherence)
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Using fond As New SolidBrush(_fond)
            g.FillRectangle(fond, e.Bounds)
        End Using

        Dim choisie As Boolean = (e.State And DrawItemState.Selected) = DrawItemState.Selected
        Dim carte As New Rectangle(e.Bounds.Left + 1, e.Bounds.Top + 1, e.Bounds.Width - Echelle(4) - 2, e.Bounds.Height - Echelle(8) - 2)
        Using chemin As GraphicsPath = Arrondi(carte, Echelle(8)), pinceau As New SolidBrush(If(choisie, _carteChoisie, _carte))
            g.FillPath(pinceau, chemin)
            If choisie Then
                Using trait As New Pen(_accent, Math.Max(1.5F, Echelle(1) * 1.5F))
                    g.DrawPath(trait, chemin)
                End Using
            End If
        End Using

        ' En-tete : la pastille "Lignes 1 et 2", puis le titre du probleme.
        Dim x As Integer = carte.Left + Echelle(14)
        Dim y As Integer = carte.Top + Echelle(12)
        Dim hauteurEntete As Integer = Math.Max(_policeTitre.Height, _policePastille.Height + Echelle(4))
        Dim tailleEtiquette As Size = TextRenderer.MeasureText(g, p.Etiquette, _policePastille, New Size(Integer.MaxValue, Integer.MaxValue), DrapeauxTexte)
        Dim pastille As New Rectangle(x, y + (hauteurEntete - (_policePastille.Height + Echelle(4))) \ 2,
                                      tailleEtiquette.Width + 2 * Echelle(9), _policePastille.Height + Echelle(4))
        ' Pastille rouge pour ce qu'il faut corriger, bleue pour un avertissement.
        Using chemin As GraphicsPath = Arrondi(pastille, pastille.Height \ 2), pinceau As New SolidBrush(If(p.Bloquant, _erreur, _accent))
            g.FillPath(pinceau, chemin)
        End Using
        TextRenderer.DrawText(g, p.Etiquette, _policePastille, pastille, Color.White,
                              DrapeauxTexte Or TextFormatFlags.HorizontalCenter Or TextFormatFlags.VerticalCenter)
        ' "À corriger", a droite de l'en-tete.
        Dim droite As Integer = carte.Right - Echelle(14)
        If p.Bloquant Then
            Dim mention As String = "À corriger"
            Dim tailleMention As Size = TextRenderer.MeasureText(g, mention, _policePastille, New Size(Integer.MaxValue, Integer.MaxValue), DrapeauxTexte)
            TextRenderer.DrawText(g, mention, _policePastille, New Rectangle(droite - tailleMention.Width, y, tailleMention.Width + 1, hauteurEntete), _erreur,
                                  DrapeauxTexte Or TextFormatFlags.VerticalCenter)
            droite -= tailleMention.Width + Echelle(10)
        End If
        Dim zoneTitre As New Rectangle(pastille.Right + Echelle(10), y, droite - pastille.Right - Echelle(10), hauteurEntete)
        TextRenderer.DrawText(g, p.Titre, _policeTitre, zoneTitre, _texte,
                              DrapeauxTexte Or TextFormatFlags.VerticalCenter Or TextFormatFlags.EndEllipsis)

        ' Detail, sur plusieurs lignes au besoin.
        Dim zoneDetail As New Rectangle(x, y + hauteurEntete + Echelle(5), LargeurDuTexte(), carte.Bottom - Echelle(8) - (y + hauteurEntete + Echelle(5)))
        TextRenderer.DrawText(g, p.Detail, ListBoxProblemes.Font, zoneDetail, _texteDiscret, DrapeauxTexte Or TextFormatFlags.WordBreak)
    End Sub

    Private Shared Function Arrondi(r As Rectangle, rayon As Integer) As GraphicsPath
        Dim chemin As New GraphicsPath()
        Dim d As Integer = Math.Max(2, Math.Min(Math.Min(r.Width, r.Height), rayon * 2))
        chemin.AddArc(r.Left, r.Top, d, d, 180, 90)
        chemin.AddArc(r.Right - d, r.Top, d, d, 270, 90)
        chemin.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90)
        chemin.AddArc(r.Left, r.Bottom - d, d, d, 90, 90)
        chemin.CloseFigure()
        Return chemin
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Icone d'avertissement et bord
    ' ------------------------------------------------------------------------------------------

    Private Sub PictureBoxIcone_Paint(sender As Object, e As PaintEventArgs) Handles PictureBoxIcone.Paint
        Dim g As Graphics = e.Graphics
        g.SmoothingMode = SmoothingMode.AntiAlias
        Dim zone As New Rectangle(1, 1, PictureBoxIcone.Width - 3, PictureBoxIcone.Height - 3)
        Using pinceau As New SolidBrush(_alerteEnCours)
            g.FillEllipse(pinceau, zone)
        End Using
        ' Point d'exclamation : une barre arrondie et un point.
        Dim l As Single = zone.Width * 0.13F
        Dim cx As Single = zone.Left + zone.Width / 2.0F
        Using blanc As New Pen(Color.White, l) With {.StartCap = LineCap.Round, .EndCap = LineCap.Round}
            g.DrawLine(blanc, cx, zone.Top + zone.Height * 0.25F, cx, zone.Top + zone.Height * 0.56F)
        End Using
        g.FillEllipse(Brushes.White, cx - l * 0.62F, zone.Top + zone.Height * 0.69F, l * 1.24F, l * 1.24F)
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Using stylo As New Pen(_bord, 1)
            e.Graphics.DrawRectangle(stylo, 0, 0, Me.ClientSize.Width - 1, Me.ClientSize.Height - 1)
        End Using
    End Sub

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            If Not Me.DesignMode Then cp.ClassStyle = cp.ClassStyle Or CS_DROPSHADOW
            Return cp
        End Get
    End Property

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        If Me.DesignMode Then Exit Sub
        Try
            ' Windows 11 : vrais coins arrondis et lisere dans la couleur du bord (sans effet avant).
            Dim preference As Integer = DWMWCP_ROUND
            DwmSetWindowAttribute(Me.Handle, DWMWA_WINDOW_CORNER_PREFERENCE, preference, 4)
            Dim couleurBord As Integer = _bord.R Or (CInt(_bord.G) << 8) Or (CInt(_bord.B) << 16)
            DwmSetWindowAttribute(Me.Handle, DWMWA_BORDER_COLOR, couleurBord, 4)
            ' Barre de defilement sombre, comme le reste.
            SetWindowTheme(ListBoxProblemes.Handle, "DarkMode_Explorer", Nothing)
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Choix
    ' ------------------------------------------------------------------------------------------

    Private Sub Corriger()
        Enregistrer = False
        Dim i As Integer = ListBoxProblemes.SelectedIndex
        ProblemeChoisi = If(i >= 0 AndAlso i < _problemes.Count, _problemes(i), _problemes.FirstOrDefault())
        Me.DialogResult = DialogResult.Cancel
        Me.Close()
    End Sub

    Private Sub ButtonCorriger_Click(sender As Object, e As EventArgs) Handles ButtonCorriger.Click
        Corriger()
    End Sub

    Private Sub ListBoxProblemes_DoubleClick(sender As Object, e As EventArgs) Handles ListBoxProblemes.DoubleClick
        If ListBoxProblemes.SelectedIndex >= 0 Then Corriger()
    End Sub

    Private Sub ButtonEnregistrer_Click(sender As Object, e As EventArgs) Handles ButtonEnregistrer.Click
        If _problemes.Any(Function(p) p.Bloquant) Then Exit Sub         ' (bouton grise dans ce cas)
        Enregistrer = True
        ProblemeChoisi = Nothing
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    ''' <summary>Fermee autrement (Alt+F4) : comme "Corriger", rien n'est enregistre sans l'avoir dit.</summary>
    Private Sub FormCoherence_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If Not Enregistrer AndAlso ProblemeChoisi Is Nothing Then ProblemeChoisi = _problemes.FirstOrDefault()
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Fenetre sans barre de titre : on la deplace en la tirant par son en-tete.
    ' ------------------------------------------------------------------------------------------

    Private Sub Deplacer(sender As Object, e As MouseEventArgs) Handles Me.MouseDown, LabelTitre.MouseDown, LabelSousTitre.MouseDown, PictureBoxIcone.MouseDown
        If e.Button <> MouseButtons.Left Then Exit Sub
        ReleaseCapture()
        SendMessage(Me.Handle, WM_NCLBUTTONDOWN, New IntPtr(HTCAPTION), IntPtr.Zero)
    End Sub

    Private Sub FormCoherence_FormClosed(sender As Object, e As FormClosedEventArgs) Handles Me.FormClosed
        If _policeTitre IsNot Nothing Then _policeTitre.Dispose() : _policeTitre = Nothing
        If _policePastille IsNot Nothing Then _policePastille.Dispose() : _policePastille = Nothing
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Win32
    ' ------------------------------------------------------------------------------------------

    Private Const CS_DROPSHADOW As Integer = &H20000
    Private Const DWMWA_WINDOW_CORNER_PREFERENCE As Integer = 33
    Private Const DWMWCP_ROUND As Integer = 2
    Private Const DWMWA_BORDER_COLOR As Integer = 34
    Private Const WM_NCLBUTTONDOWN As Integer = &HA1
    Private Const HTCAPTION As Integer = 2

    <DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attribut As Integer, ByRef valeur As Integer, taille As Integer) As Integer
    End Function
    <DllImport("uxtheme.dll", CharSet:=CharSet.Unicode)>
    Private Shared Function SetWindowTheme(hwnd As IntPtr, application As String, liste As String) As Integer
    End Function
    <DllImport("user32.dll")>
    Private Shared Function ReleaseCapture() As Boolean
    End Function
    <DllImport("user32.dll")>
    Private Shared Function SendMessage(hWnd As IntPtr, msg As Integer, wParam As IntPtr, lParam As IntPtr) As IntPtr
    End Function

End Class
