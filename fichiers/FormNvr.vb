Option Strict On
Option Explicit On

''' <summary>
''' Compte de l'enregistreur de cameras IP (NVR) : adresse, port RTSP, identifiant et mot de passe (NvrIp). Le mot de passe n'est jamais prerempli a l'ecran : s'il est laisse vide, l'ancien est garde.
''' Les controles sont dans le Concepteur.
''' </summary>
Public Class FormNvr

    Private Sub FormNvr_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        TextBoxHote.Text = NvrIp.Hote
        NumericPort.Value = Math.Max(NumericPort.Minimum, Math.Min(NumericPort.Maximum, NvrIp.Port))
        TextBoxIdentifiant.Text = NvrIp.Identifiant
        TextBoxMotDePasse.Text = NvrIp.MotDePasse                       ' (enregistre, chiffre : points dans la zone de saisie)
    End Sub

    Private Sub FormNvr_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult <> DialogResult.OK Then Exit Sub
        If TextBoxHote.Text.Trim() = "" OrElse TextBoxIdentifiant.Text.Trim() = "" Then
            MessageBox.Show(Me, "L'adresse et l'identifiant sont obligatoires.", "Caméras IP", MessageBoxButtons.OK, MessageBoxIcon.Information)
            e.Cancel = True
            Exit Sub
        End If
        Dim motDePasse As String = If(TextBoxMotDePasse.Text <> "", TextBoxMotDePasse.Text, NvrIp.MotDePasse)
        NvrIp.Enregistrer(TextBoxHote.Text, CInt(NumericPort.Value), TextBoxIdentifiant.Text, motDePasse)
    End Sub

End Class
