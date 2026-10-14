Option Strict On
Option Explicit On

''' <summary>
''' Reglages de l'affichage automatique du dernier enregistrement Blink (27/10/2026) : voir BlinkClips. Les controles sont dans le Concepteur.
''' </summary>
Public Class PanneauBlinkAuto

    Private Shared ReadOnly Coins As String() = {"Hgauche", "Hdroite", "Mgauche", "Mdroite", "Bgauche", "Bdroite"}
    Private Shared ReadOnly NomsDesCoins As String() = {"En haut à gauche", "En haut à droite", "Au milieu à gauche", "Au milieu à droite", "En bas à gauche", "En bas à droite"}

    Private Sub PanneauBlinkAuto_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If DesignMode Then Exit Sub
        ComboBoxCoin.Items.AddRange(NomsDesCoins)
        Recharger()
    End Sub

    ''' <summary>Affiche les reglages enregistres (taille changee a la souris sur un PIP, minuterie...).</summary>
    Private _affiche As Boolean          ' (les reglages ont ete lus : tant que l'onglet Blink n'a jamais ete ouvert, les controles sont vides et ne doivent rien ecraser)

    Private Sub Recharger()
        Dim r As BlinkClips.ReglagesClips = BlinkClips.Reglages()
        _affiche = True
        CheckedListBoxCameras.Items.Clear()
        CheckBoxActif.Checked = r.Actif
        NumericUpDownIntervalle.Value = Math.Max(NumericUpDownIntervalle.Minimum, Math.Min(NumericUpDownIntervalle.Maximum, r.Intervalle))
        RadioButtonPip.Checked = Not r.PleinEcran
        RadioButtonPlein.Checked = r.PleinEcran
        Dim i As Integer = Array.IndexOf(Coins, r.Coin)
        ComboBoxCoin.SelectedIndex = If(i >= 0, i, 5)
        NumericUpDownTaille.Value = Math.Max(NumericUpDownTaille.Minimum, Math.Min(NumericUpDownTaille.Maximum, r.Taille))
        NumericUpDownVolume.Value = Math.Max(0, Math.Min(100, r.Volume))
        For Each nom As String In r.Cameras
            CheckedListBoxCameras.Items.Add(nom, True)
        Next
        MettreAJour()
    End Sub

    ''' <summary>L'onglet s'ouvre : la liste des cameras est relue (la connexion a pu changer dans l'autre panneau).</summary>
    Private Sub PanneauBlinkAuto_VisibleChanged(sender As Object, e As EventArgs) Handles Me.VisibleChanged
        If Visible AndAlso Not DesignMode Then 
            Recharger()
            RemplirLesCameras()
        End If
    End Sub

    Private Async Sub RemplirLesCameras()
        If Not BlinkApi.EstConnecte Then
            LabelEtat.Text = "Pas encore connecté : « Compte et caméras » d'abord."
            Exit Sub
        End If
        Try
            Dim cameras As List(Of BlinkApi.CameraBlink) = Await BlinkApi.ListerLesCamerasAsync()
            Dim toutCocher As Boolean = Not BlinkClips.Reglages().CamerasChoisies          ' (jamais choisi : toutes cochees de base ; sinon les nouvelles restent decochees)
            For Each c As BlinkApi.CameraBlink In cameras
                Dim deja As Boolean = False
                For Each o As Object In CheckedListBoxCameras.Items
                    If String.Equals(CStr(o), c.Nom, StringComparison.OrdinalIgnoreCase) Then deja = True : Exit For
                Next
                If Not deja Then CheckedListBoxCameras.Items.Add(c.Nom, toutCocher)
            Next
        Catch ex As Exception
            LabelEtat.Text = "Liste des caméras impossible : " & ex.Message
        End Try
    End Sub

    Private Sub ButtonToutCocher_Click(sender As Object, e As EventArgs) Handles ButtonToutCocher.Click
        For i As Integer = 0 To CheckedListBoxCameras.Items.Count - 1
            CheckedListBoxCameras.SetItemChecked(i, True)
        Next
    End Sub

    Private Sub ButtonToutDecocher_Click(sender As Object, e As EventArgs) Handles ButtonToutDecocher.Click
        For i As Integer = 0 To CheckedListBoxCameras.Items.Count - 1
            CheckedListBoxCameras.SetItemChecked(i, False)
        Next
    End Sub

    Private Sub MettreAJour()
        Dim pip As Boolean = RadioButtonPip.Checked
        ComboBoxCoin.Enabled = pip
        NumericUpDownTaille.Enabled = pip
    End Sub

    Private Sub RadioButtonPip_CheckedChanged(sender As Object, e As EventArgs) Handles RadioButtonPip.CheckedChanged, RadioButtonPlein.CheckedChanged
        MettreAJour()
    End Sub

    Private Function LesReglages() As BlinkClips.ReglagesClips
        Dim r As New BlinkClips.ReglagesClips With {
            .Actif = CheckBoxActif.Checked,
            .Intervalle = CInt(NumericUpDownIntervalle.Value),
            .PleinEcran = RadioButtonPlein.Checked,
            .Coin = Coins(Math.Max(0, ComboBoxCoin.SelectedIndex)),
            .Taille = CInt(NumericUpDownTaille.Value),
            .Volume = CInt(NumericUpDownVolume.Value),
            .CamerasChoisies = True,
            .PosX = BlinkClips.Reglages().PosX,
            .PosY = BlinkClips.Reglages().PosY}
        For Each o As Object In CheckedListBoxCameras.CheckedItems
            r.Cameras.Add(CStr(o))
        Next
        Return r
    End Function

    ''' <summary>Reglages tels qu'affiches (meme sans OK) : l'essai du panneau du compte les utilise.</summary>
    Friend Function ReglagesAffiches() As BlinkClips.ReglagesClips
        If Not _affiche Then Return BlinkClips.Reglages()
        Return LesReglages()
    End Function

    ''' <summary>Appele par le OK de la fenetre Option.</summary>
    Friend Sub Enregistrer()
        If Not _affiche Then Exit Sub          ' (onglet Blink jamais ouvert : le OK d'Option ne touche pas aux reglages enregistres ; 27/10/2026, l'affichage automatique avait ete remis a zero)
        BlinkClips.Enregistrer(LesReglages())
    End Sub

End Class
