Imports Newtonsoft.Json.Linq

''' <summary>
''' Choix des lignes d'une minuterie (28/10/2026, « pouvoir aussi choisir les lignes ») : une liste de lignes a cocher, utilisee pour enregistrer une partie de la minuterie actuelle et pour
''' charger une partie d'une minuterie enregistree (avec le mode : remplacer la minuterie actuelle, ou y ajouter). Les controles sont dans le Concepteur.
''' </summary>
Public Class FormChoixLignes

    Private _lignes As JArray
    Private _avecMode As Boolean
    Private _chargement As Boolean

    ''' <summary>Les lignes cochees.</summary>
    Friend Choisies As New JArray()
    Friend Remplacer As Boolean = True

    Public Sub New()
        InitializeComponent()          ' (constructeur sans parametre : requis par le Concepteur)
    End Sub

    ''' <summary>Montre la liste (toutes les lignes cochees au depart). Vrai si la personne valide au moins une ligne. <paramref name="avecMode"/> : propose « Remplacer » / « Ajouter ».</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, titre As String, sousTitre As String, texteOk As String, lignes As JArray, avecMode As Boolean,
                                    ByRef choisies As JArray, ByRef remplacer As Boolean) As Boolean
        Using f As New FormChoixLignes
            f._lignes = lignes
            f._avecMode = avecMode
            f.Text = titre
            f.LabelTitre.Text = titre
            f.LabelSousTitre.Text = sousTitre
            f.ButtonOk.Text = texteOk
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            choisies = f.Choisies
            remplacer = f.Remplacer
            Return True
        End Using
    End Function

    Private Sub FormChoixLignes_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        PanelMode.BackColor = Color.FromArgb(26, 44, 70)
        ListViewLignes.BackColor = StylePro.Surface
        ListViewLignes.ForeColor = StylePro.Texte
        _chargement = True
        For Each l As JToken In _lignes
            Dim o As JObject = TryCast(l, JObject)
            If o Is Nothing Then Continue For
            Dim t As Func(Of String, String) = Function(c As String) If(o(c) Is Nothing, "", CStr(o(c)))
            Dim actif As Boolean = (o("Actif") Is Nothing OrElse CBool(o("Actif")))
            Dim item As New ListViewItem(t("Jour") & If(actif, "", " (inactive)")) With {.Checked = True, .Tag = l}
            item.SubItems.Add(t("Heure"))
            item.SubItems.Add(t("Type"))
            item.SubItems.Add(t("Action"))
            If Not actif Then item.ForeColor = StylePro.Secondaire
            ListViewLignes.Items.Add(item)
        Next
        _chargement = False
        ' Sans mode (enregistrement) : le cadre du mode disparait et la fenetre se raccourcit.
        If Not _avecMode Then
            Dim place As Integer = PanelMode.Height + (ButtonOk.Top - PanelMode.Bottom)
            PanelMode.Visible = False
            ButtonOk.Top -= place
            ButtonAnnuler.Top -= place
            ClientSize = New Size(ClientSize.Width, ClientSize.Height - place)
        End If
        MettreAJourLeNombre()
    End Sub

    Private Sub MettreAJourLeNombre()
        Dim n As Integer = ListViewLignes.CheckedItems.Count
        LabelNombre.Text = n & " ligne(s) cochée(s) sur " & ListViewLignes.Items.Count
        ButtonOk.Enabled = n > 0
    End Sub

    Private Sub ListViewLignes_ItemChecked(sender As Object, e As ItemCheckedEventArgs) Handles ListViewLignes.ItemChecked
        If Not _chargement Then MettreAJourLeNombre()
    End Sub

    Private Sub ButtonTout_Click(sender As Object, e As EventArgs) Handles ButtonTout.Click
        CocherTout(True)
    End Sub

    Private Sub ButtonAucune_Click(sender As Object, e As EventArgs) Handles ButtonAucune.Click
        CocherTout(False)
    End Sub

    Private Sub CocherTout(coche As Boolean)
        _chargement = True
        For Each item As ListViewItem In ListViewLignes.Items
            item.Checked = coche
        Next
        _chargement = False
        MettreAJourLeNombre()
    End Sub

    Private Sub ButtonOk_Click(sender As Object, e As EventArgs) Handles ButtonOk.Click
        Choisies = New JArray()
        For Each item As ListViewItem In ListViewLignes.CheckedItems
            Choisies.Add(CType(item.Tag, JToken))
        Next
        Remplacer = RadioRemplacer.Checked
        DialogResult = DialogResult.OK
        Close()
    End Sub

End Class
