''' <summary>
''' Fenetre « Heure de la tâche » (double-clic sur la case Heure de la minuterie, 28/10/2026) : une heure fixe, ou une heure calculee chaque jour / semaine
''' (repere de Chabbat ou du soleil + decalage en minutes) avec l'heure qui en resulte montree tout de suite. Pas de syntaxe a retenir (voir RegleShabbat).
''' Les controles sont dans le Concepteur.
''' </summary>
Public Class FormHeureTache

    Private ReadOnly _noms As String() = {"ShabbatPreDebut", "ShabbatDebut", "ShabbatSortie", "SoleilLever", "SoleilCoucher"}
    Private _chargement As Boolean

    ''' <summary>Rendu : "" pour une heure fixe, sinon la regle ("ShabbatDebut-10").</summary>
    Friend Regle As String = ""
    Friend HeureFixe As Date = Date.Today

    Private _regleDepart As String = ""
    Private _heureDepart As Date?
    Private _jourLigne As String = ""

    Public Sub New()
        InitializeComponent()          ' (constructeur sans parametre : requis par le Concepteur)
    End Sub

    ' Cadre bien visible (28/10/2026 : « on a du mal a voir son contour » sur le fond sombre) : bordure bleue de la fenetre elle-meme (Windows 11, DWM)
    ' et filet bleu peint tout autour de la zone interieure (autres Windows).
    <Runtime.InteropServices.DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attribut As Integer, ByRef valeur As Integer, taille As Integer) As Integer
    End Function

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        Try
            Dim bleu As Integer = &H10 * &H10000 + &H84 * &H100 + &HFF         ' COLORREF 0x00BBGGRR de (10, 132, 255) : B=255 G=132 R=10
            bleu = (255 << 16) Or (132 << 8) Or 10
            DwmSetWindowAttribute(Handle, 34, bleu, 4)                           ' DWMWA_BORDER_COLOR
        Catch ex As Exception
        End Try
    End Sub

    Protected Overrides Sub OnPaint(e As PaintEventArgs)
        MyBase.OnPaint(e)
        Dim epaisseur As Integer = Math.Max(2, CInt(Math.Round(3 * DeviceDpi / 96.0)))
        Using stylo As New Pen(StylePro.Accent, epaisseur)
            e.Graphics.DrawRectangle(stylo, epaisseur \ 2, epaisseur \ 2, ClientSize.Width - epaisseur, ClientSize.Height - epaisseur)
        End Using
    End Sub

    ''' <summary>Montre la fenetre pour une ligne (<paramref name="regleActuelle"/> : sa regle, "" si l'heure est fixe ; <paramref name="jourLigne"/> : son jour, utile au soleil).
    ''' Vrai si la personne valide.</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, regleActuelle As String, heureActuelle As Date?, jourLigne As String, ByRef regle As String, ByRef heure As Date) As Boolean
        Using f As New FormHeureTache
            f._regleDepart = If(regleActuelle, "")
            f._heureDepart = heureActuelle
            f._jourLigne = If(jourLigne, "")
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            regle = f.Regle
            heure = f.HeureFixe
            Return True
        End Using
    End Function

    Private Sub FormHeureTache_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        _chargement = True
        RegleShabbat.AssurerLesHoraires()
        ' Les cinq reperes toujours sous les yeux, chacun avec son heure du jour (le choix le plus important de la fenetre).
        ListeRepere.ItemHeight = CInt(Math.Round(34 * DeviceDpi / 96.0))
        ListeRepere.Height = ListeRepere.ItemHeight * 5 + 2
        Dim ecart As Integer = CInt(Math.Round(14 * DeviceDpi / 96.0))
        For Each c As Control In {NumericDecalage, LabelDecalage, LabelUnite}
            c.Top = ListeRepere.Bottom + ecart + If(c Is NumericDecalage, 0, CInt(Math.Round(4 * DeviceDpi / 96.0)))
        Next
        ' Le cadre et le reste de la fenetre suivent la vraie hauteur de la liste (la ligne « Décalage » en bas du cadre etait coupee sur un ecran tres agrandi, 28/10/2026).
        ' Bornes : « pas plus tard que » / « pas plus tot que », sous le decalage.
        CheckPlusTard.Top = NumericDecalage.Bottom + ecart
        TimePlusTard.Top = CheckPlusTard.Top - CInt(Math.Round(4 * DeviceDpi / 96.0))
        CheckPlusTot.Top = Math.Max(CheckPlusTard.Bottom, TimePlusTard.Bottom) + CInt(Math.Round(10 * DeviceDpi / 96.0))
        TimePlusTot.Top = CheckPlusTot.Top - CInt(Math.Round(4 * DeviceDpi / 96.0))
        Dim decalageY As Integer = Math.Max(CheckPlusTot.Bottom, TimePlusTot.Bottom) + CInt(Math.Round(20 * DeviceDpi / 96.0)) - PanelCalcule.Height
        If decalageY <> 0 Then
            PanelCalcule.Height += decalageY
            PanelApercu.Top += decalageY
            ButtonOk.Top += decalageY
            ButtonAnnuler.Top += decalageY
            ClientSize = New Size(ClientSize.Width, ClientSize.Height + decalageY)
        End If
        ListeRepere.Items.Add(New Repere("Avant l'entrée de Chabbat", "ShabbatPreDebut", Horaire(RegleShabbat.ShabbatPreDebut)))
        ListeRepere.Items.Add(New Repere("Entrée de Chabbat", "ShabbatDebut", Horaire(RegleShabbat.ShabbatDebut)))
        ListeRepere.Items.Add(New Repere("Sortie de Chabbat", "ShabbatSortie", Horaire(RegleShabbat.ShabbatSortie)))
        ListeRepere.Items.Add(New Repere("Lever du soleil", "SoleilLever", Horaire(RegleShabbat.HoraireDuJour("SoleilLever", _jourLigne))))
        ListeRepere.Items.Add(New Repere("Coucher du soleil", "SoleilCoucher", Horaire(RegleShabbat.HoraireDuJour("SoleilCoucher", _jourLigne))))
        ListeRepere.SelectedIndex = 1
        TimeFixe.Value = Date.Today.Add(If(_heureDepart.HasValue, _heureDepart.Value.TimeOfDay, New TimeSpan(18, 0, 0)))
        Dim nom As String = "", decalage As Integer = 0
        Dim calculee As Boolean = (_regleDepart <> "" AndAlso RegleShabbat.Decomposer(_regleDepart, nom, decalage))
        If calculee Then
            ListeRepere.SelectedIndex = Math.Max(0, Array.IndexOf(_noms, nom))
            NumericDecalage.Value = Math.Max(NumericDecalage.Minimum, Math.Min(NumericDecalage.Maximum, decalage))
        End If
        ' Bornes de la regle de depart (heure par defaut quand le repere tombe trop tard / trop tot).
        TimePlusTard.Value = Date.Today.AddHours(18)
        TimePlusTot.Value = Date.Today.AddHours(17)
        Dim plusTard As Integer = -1, plusTot As Integer = -1
        If calculee Then RegleShabbat.Bornes(_regleDepart, plusTard, plusTot)
        CheckPlusTard.Checked = plusTard >= 0
        CheckPlusTot.Checked = plusTot >= 0
        If plusTard >= 0 Then TimePlusTard.Value = Date.Today.AddMinutes(plusTard)
        If plusTot >= 0 Then TimePlusTot.Value = Date.Today.AddMinutes(plusTot)
        LabelSousTitre.Text = If(_jourLigne <> "", "Tâche du " & _jourLigne.ToLowerInvariant() & " : choisissez quand elle doit démarrer.", LabelSousTitre.Text)
        _chargement = False
        Choisir(Not calculee)
        DiagLog("Regle Chabbat : fenetre ouverte sur " & If(calculee, "« heure calculee » (" & _regleDepart & ")", "« heure fixe » (regle lue : '" & _regleDepart & "')"))
    End Sub

    ''' <summary>Un seul mode a la fois : heure fixe, ou heure calculee. Toucher a un de ses reglages (ou cliquer dans son cadre) le choisit.</summary>
    Private Sub Choisir(fixe As Boolean)
        _chargement = True
        RadioFixe.Checked = fixe
        RadioShabbat.Checked = Not fixe
        _chargement = False
        Actualiser()
    End Sub

    Private Sub ChoixFixe(sender As Object, e As EventArgs) Handles RadioFixe.Click, PanelFixe.Click
        Choisir(True)
    End Sub

    Private Sub ChoixCalcule(sender As Object, e As EventArgs) Handles RadioShabbat.Click, PanelCalcule.Click, LabelAide.Click, LabelDecalage.Click, LabelUnite.Click
        Choisir(False)
    End Sub

    Private Sub ReglageFixeChange(sender As Object, e As EventArgs) Handles TimeFixe.ValueChanged
        If Not _chargement Then Choisir(True)
    End Sub

    Private Sub ReglageCalculeChange(sender As Object, e As EventArgs) Handles ListeRepere.SelectedIndexChanged, NumericDecalage.ValueChanged, CheckPlusTard.CheckedChanged, TimePlusTard.ValueChanged, CheckPlusTot.CheckedChanged, TimePlusTot.ValueChanged
        If Not _chargement Then Choisir(False)
    End Sub

    Private Shared Function Horaire(h As String) As String
        Return If(IsDate(h), CDate(h).ToString("HH:mm"), "--:--")
    End Function

    ''' <summary>Une ligne de la liste des reperes : libelle, nom de la regle et heure du jour.</summary>
    Private NotInheritable Class Repere
        Friend ReadOnly Libelle As String
        Friend ReadOnly Nom As String
        Friend ReadOnly Heure As String
        Friend Sub New(libelle As String, nom As String, heure As String)
            Me.Libelle = libelle : Me.Nom = nom : Me.Heure = heure
        End Sub
        Public Overrides Function ToString() As String
            Return Libelle
        End Function
    End Class

    ''' <summary>Dessin d'une ligne : libelle a gauche, heure du jour a droite en gras ; la ligne choisie est en bleu.</summary>
    Private Sub ListeRepere_DrawItem(sender As Object, e As DrawItemEventArgs) Handles ListeRepere.DrawItem
        If e.Index < 0 Then Exit Sub
        Dim r As Repere = TryCast(ListeRepere.Items(e.Index), Repere)
        If r Is Nothing Then Exit Sub
        Dim choisie As Boolean = (e.State And DrawItemState.Selected) <> 0
        Dim marge As Integer = CInt(Math.Round(12 * DeviceDpi / 96.0))
        Dim zone As Rectangle = e.Bounds
        Using fond As New SolidBrush(If(choisie, StylePro.Accent, ListeRepere.BackColor))
            e.Graphics.FillRectangle(fond, zone)
        End Using
        If Not choisie AndAlso e.Index < ListeRepere.Items.Count - 1 Then
            Using filet As New Pen(Color.FromArgb(52, 52, 56))
                e.Graphics.DrawLine(filet, zone.Left + marge, zone.Bottom - 1, zone.Right - marge, zone.Bottom - 1)
            End Using
        End If
        Dim encre As Color = If(choisie, Color.White, StylePro.Texte)
        Dim format As TextFormatFlags = TextFormatFlags.VerticalCenter Or TextFormatFlags.NoPrefix Or TextFormatFlags.SingleLine
        TextRenderer.DrawText(e.Graphics, r.Libelle, ListeRepere.Font, New Rectangle(zone.Left + marge, zone.Top, zone.Width - 6 * marge, zone.Height), encre, format Or TextFormatFlags.Left)
        Using gras As New Font(ListeRepere.Font, FontStyle.Bold)
            TextRenderer.DrawText(e.Graphics, r.Heure, gras, New Rectangle(zone.Left, zone.Top, zone.Width - marge, zone.Height), If(choisie, Color.White, StylePro.Secondaire), format Or TextFormatFlags.Right)
        End Using
    End Sub

    Private Function RegleChoisie() As String
        Dim plusTard As Integer = If(CheckPlusTard.Checked, TimePlusTard.Value.Hour * 60 + TimePlusTard.Value.Minute, -1)
        Dim plusTot As Integer = If(CheckPlusTot.Checked, TimePlusTot.Value.Hour * 60 + TimePlusTot.Value.Minute, -1)
        Return RegleShabbat.Construire(_noms(Math.Max(0, ListeRepere.SelectedIndex)), CInt(NumericDecalage.Value), plusTard, plusTot)
    End Function

    ''' <summary>Active ce qui sert, et montre l'heure qui en resulte.</summary>
    Private Sub Actualiser()
        If _chargement Then Exit Sub
        ' Le cadre du mode choisi est marque (barre bleue + fond plus clair) ; l'autre est en retrait.
        ' Une couleur par section : orange pour l'heure fixe, bleu pour l'heure calculee, vert pour le resultat. Le mode choisi est teinte ; l'autre reste sombre.
        PanelFixe.BackColor = If(RadioFixe.Checked, Color.FromArgb(62, 48, 26), StylePro.Fond)
        PanelCalcule.BackColor = If(RadioShabbat.Checked, Color.FromArgb(26, 44, 70), StylePro.Fond)
        PanelMarqueFixe.Visible = RadioFixe.Checked
        PanelMarqueCalcule.Visible = RadioShabbat.Checked
        RadioFixe.ForeColor = If(RadioFixe.Checked, Color.FromArgb(255, 190, 80), StylePro.Secondaire)
        RadioShabbat.ForeColor = If(RadioShabbat.Checked, Color.FromArgb(110, 180, 255), StylePro.Secondaire)
        LabelAide.ForeColor = If(RadioShabbat.Checked, Color.FromArgb(150, 175, 210), Color.FromArgb(110, 110, 114))
        ListeRepere.BackColor = If(RadioShabbat.Checked, Color.FromArgb(18, 32, 54), Color.FromArgb(36, 36, 38))
        PanelApercu.BackColor = Color.FromArgb(26, 54, 38)
        If RadioFixe.Checked Then
            LabelApercu.Text = "À " & TimeFixe.Value.ToString("HH:mm")
            LabelApercuDetail.Text = If(_regleDepart <> "", "Attention : l'heure paramétrée (" & _regleDepart & ") sera remplacée par cette heure fixe.", "Heure fixe : elle ne change pas.")
            Exit Sub
        End If
        Dim jour As String = _jourLigne, heure As String = "", tri As String = ""
        If RegleShabbat.Resoudre(RegleChoisie(), jour, heure, tri) Then
            Dim chabbat As Boolean = Not RegleShabbat.EstSoleil(_noms(Math.Max(0, ListeRepere.SelectedIndex)))
            LabelApercu.Text = If(jour <> "", jour & " à ", "À ") & heure
            LabelApercuDetail.Text = If(chabbat, "Recalculé chaque semaine", "Recalculé chaque jour") & "  ·  " & RegleChoisie()
        Else
            LabelApercu.Text = "Horaire inconnu"
            LabelApercuDetail.Text = "Choisissez une ville dans l'onglet Shabbat."
        End If
    End Sub

    Private Sub ButtonOk_Click(sender As Object, e As EventArgs) Handles ButtonOk.Click
        If RadioShabbat.Checked Then
            Dim jour As String = _jourLigne, heure As String = "", tri As String = ""
            If Not RegleShabbat.Resoudre(RegleChoisie(), jour, heure, tri) Then
                MessageBox.Show(Me, "Horaire inconnu : choisissez une ville dans l'onglet Shabbat.", "Heure de la tâche", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
                Exit Sub
            End If
            Regle = RegleChoisie()
        Else
            Regle = ""
            HeureFixe = TimeFixe.Value
        End If
        DialogResult = DialogResult.OK
        Close()
    End Sub

End Class
