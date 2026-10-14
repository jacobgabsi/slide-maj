''' <summary>
''' Fenetre « Jours de la tâche » (liste Jour de la minuterie > « Personnalisé… », 28/10/2026) : on coche les jours de la semaine ; le resultat s'affiche tout de suite
''' (« Lun, Mer, Ven » ; un seul jour, Lun-Ven, Sam-Dim et TLJ gardent leur nom habituel). Voir JoursPerso. Les controles sont dans le Concepteur.
''' </summary>
Public Class FormJoursPerso

    Private _cases As CheckBox()
    Private _chargement As Boolean

    ''' <summary>Masque des jours choisis (bit 0 = lundi).</summary>
    Friend MasqueChoisi As Integer

    Private _masqueDepart As Integer

    Public Sub New()
        InitializeComponent()          ' (constructeur sans parametre : requis par le Concepteur)
    End Sub

    ' Cadre bien visible sur le fond sombre, comme la fenetre « Heure de la tâche » : bordure bleue de la fenetre (Windows 11) et filet bleu peint autour.
    <Runtime.InteropServices.DllImport("dwmapi.dll")>
    Private Shared Function DwmSetWindowAttribute(hwnd As IntPtr, attribut As Integer, ByRef valeur As Integer, taille As Integer) As Integer
    End Function

    Protected Overrides Sub OnHandleCreated(e As EventArgs)
        MyBase.OnHandleCreated(e)
        Try
            Dim bleu As Integer = (255 << 16) Or (132 << 8) Or 10                ' COLORREF 0x00BBGGRR de (10, 132, 255)
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

    ''' <summary>Montre la fenetre, les jours de <paramref name="masqueDepart"/> deja coches. Vrai si la personne valide ; <paramref name="masque"/> : les jours choisis.</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, masqueDepart As Integer, ByRef masque As Integer) As Boolean
        Using f As New FormJoursPerso
            f._masqueDepart = masqueDepart
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            masque = f.MasqueChoisi
            Return True
        End Using
    End Function

    Private Sub FormJoursPerso_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        _cases = {CaseLundi, CaseMardi, CaseMercredi, CaseJeudi, CaseVendredi, CaseSamedi, CaseDimanche}
        ' Le cadre bleu des jours et le cadre vert du resultat gardent leurs teintes apres le style commun.
        PanelJours.BackColor = Color.FromArgb(26, 44, 70)
        PanelResultat.BackColor = Color.FromArgb(26, 54, 38)
        _chargement = True
        Poser(_masqueDepart)
        _chargement = False
        Actualiser()
    End Sub

    Private Sub Poser(masque As Integer)
        For i As Integer = 0 To 6
            _cases(i).Checked = (masque And (1 << i)) <> 0
        Next
    End Sub

    Private Function Choisis() As Integer
        Dim m As Integer = 0
        For i As Integer = 0 To 6
            If _cases(i).Checked Then m = m Or (1 << i)
        Next
        Return m
    End Function

    Private Sub Case_CheckedChanged(sender As Object, e As EventArgs) Handles CaseLundi.CheckedChanged, CaseMardi.CheckedChanged, CaseMercredi.CheckedChanged, CaseJeudi.CheckedChanged,
                                                                              CaseVendredi.CheckedChanged, CaseSamedi.CheckedChanged, CaseDimanche.CheckedChanged
        If Not _chargement Then Actualiser()
    End Sub

    Private Sub Actualiser()
        Dim m As Integer = Choisis()
        If m = 0 Then
            LabelResultat.Text = "Aucun jour choisi"
            LabelResultatTitre.Text = "COCHEZ AU MOINS UN JOUR"
        Else
            LabelResultat.Text = JoursPerso.ValeurDuJour(m)
            LabelResultatTitre.Text = "LA TÂCHE DÉMARRERA"
        End If
    End Sub

    Private Sub ButtonSemaine_Click(sender As Object, e As EventArgs) Handles ButtonSemaine.Click
        Poser(&H1F)
    End Sub

    Private Sub ButtonWeekEnd_Click(sender As Object, e As EventArgs) Handles ButtonWeekEnd.Click
        Poser(&H60)
    End Sub

    Private Sub ButtonTous_Click(sender As Object, e As EventArgs) Handles ButtonTous.Click
        Poser(&H7F)
    End Sub

    Private Sub ButtonAucun_Click(sender As Object, e As EventArgs) Handles ButtonAucun.Click
        Poser(0)
    End Sub

    Private Sub ButtonOk_Click(sender As Object, e As EventArgs) Handles ButtonOk.Click
        Dim m As Integer = Choisis()
        If m = 0 Then
            MessageBox.Show(Me, "Cochez au moins un jour.", "Jours de la tâche", MessageBoxButtons.OK, MessageBoxIcon.Exclamation)
            Exit Sub
        End If
        MasqueChoisi = m
        DialogResult = DialogResult.OK
        Close()
    End Sub

End Class
