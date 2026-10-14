Option Strict On
Option Explicit On

''' <summary>
''' Cadre « Caméra IP » de l'onglet Camera des options (Jacob, 29/10/2026) : le compte de l'enregistreur de caméras IP (NVR, ex. Herospeed), le meme que la fenetre FormNvr de la minuterie.
''' Le mot de passe n'est jamais prerempli : laisse vide, l'ancien est garde. Les controles sont dans le Concepteur.
''' </summary>
Public Class PanneauCameraIp

    Private ReadOnly _infobulle As New ToolTip()

    Private Sub PanneauCameraIp_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If DesignMode Then Exit Sub
        TextBoxHote.Text = NvrIp.Hote
        NumericPort.Value = Math.Max(NumericPort.Minimum, Math.Min(NumericPort.Maximum, NvrIp.Port))
        TextBoxIdentifiant.Text = NvrIp.Identifiant
        TextBoxMotDePasse.Text = NvrIp.MotDePasse                       ' (enregistre, chiffre : points dans la zone de saisie, comme pour Blink)
        TextBoxHoteDistant.Text = NvrIp.HoteDistant
        _infobulle.SetToolTip(TextBoxHoteDistant, "Adresse de l'enregistreur quand vous n'êtes pas sur son réseau : celle du PC du bureau dans Tailscale (100.x.x.x). Slide essaie d'abord l'adresse locale, puis celle-ci.")
        LabelEtat.Text = If(NvrIp.EstConfigure, "Enregistreur réglé : " & NvrIp.Hote & " (mot de passe chiffré pour ce poste).", "Enregistreur pas encore réglé. Ligne de minuterie : type « Caméra IP ».")
    End Sub

    Private Function MotDePasseSaisi() As String
        Return If(TextBoxMotDePasse.Text <> "", TextBoxMotDePasse.Text, NvrIp.MotDePasse)
    End Function

    Private Sub ButtonEnregistrer_Click(sender As Object, e As EventArgs) Handles ButtonEnregistrer.Click
        If TextBoxHote.Text.Trim() = "" OrElse TextBoxIdentifiant.Text.Trim() = "" Then
            LabelEtat.Text = "L'adresse et l'identifiant sont obligatoires."
            Exit Sub
        End If
        NvrIp.Enregistrer(TextBoxHote.Text, CInt(NumericPort.Value), TextBoxIdentifiant.Text, MotDePasseSaisi(), TextBoxHoteDistant.Text)
        LabelEtat.Text = "Enregistré : " & NvrIp.Hote & If(NvrIp.HoteDistant <> "", " (à distance : " & NvrIp.HoteDistant & ")", "") & If(NvrIp.MotDePasse <> "", " ; mot de passe enregistré.", " ; ATTENTION : pas de mot de passe.")
    End Sub

    Private Async Sub ButtonEssai_Click(sender As Object, e As EventArgs) Handles ButtonEssai.Click
        Dim hote As String = TextBoxHote.Text, port As Integer = CInt(NumericPort.Value), id As String = TextBoxIdentifiant.Text, mdp As String = MotDePasseSaisi()
        Dim distant As String = TextBoxHoteDistant.Text.Trim()
        ButtonEssai.Enabled = False
        LabelEtat.Text = "Essai en cours..."
        Try
            Dim local As String = Await Task.Run(Function() NvrIp.Tester(hote, port, id, mdp))
            If distant = "" Then
                LabelEtat.Text = local
            Else
                LabelEtat.Text = "Local : " & local & "   |   À distance : " & Await Task.Run(Function() NvrIp.Tester(distant, port, id, mdp))
            End If
        Finally
            ButtonEssai.Enabled = True
        End Try
    End Sub

End Class
