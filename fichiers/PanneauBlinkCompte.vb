Option Strict On
Option Explicit On

''' <summary>
''' Caméras Blink, étape 1 (26/10/2026) : connexion au compte (adresse, mot de passe, puis code de contrôle reçu par Blink) et liste des caméras. Ouverte par le menu « Caméras Blink... » du clic droit.
''' Le mot de passe n'est jamais enregistré ni écrit au journal (voir BlinkApi). Les contrôles sont dans le Concepteur.
''' </summary>
Public Class PanneauBlinkCompte

    Private _occupe As Boolean
    Private _cameras As New List(Of BlinkApi.CameraBlink)

    Private Sub PanneauBlinkCompte_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        If DesignMode Then Exit Sub
        TextBoxEmail.Text = BlinkApi.Adresse
        TextBoxMotDePasse.Text = BlinkApi.MotDePasse                       ' (enregistre, chiffre : points dans la zone de saisie)
        If BlinkApi.EstConnecte Then
            Etat("Connecté : " & BlinkApi.Adresse & ".")
            ListerLesCameras()
        Else
            Etat("Pas encore connecté.")
        End If
        MettreAJour()
    End Sub

    Private Sub Etat(texte As String)
        LabelEtat.Text = texte
    End Sub

    Private Sub MettreAJour()
        Dim connecte As Boolean = BlinkApi.EstConnecte
        ButtonActualiser.Enabled = connecte AndAlso Not _occupe
        ButtonDeconnecter.Enabled = connecte AndAlso Not _occupe
        ButtonConnexion.Enabled = Not _occupe
        ButtonCode.Enabled = Not _occupe
    End Sub

    Private Async Sub ButtonConnexion_Click(sender As Object, e As EventArgs) Handles ButtonConnexion.Click
        If TextBoxEmail.Text.Trim() = "" OrElse TextBoxMotDePasse.Text = "" Then
            Etat("Saisissez l'adresse et le mot de passe de votre compte Blink.")
            Exit Sub
        End If
        _occupe = True : MettreAJour()
        Etat("Connexion à Blink...")
        Try
            Dim r As String = Await BlinkApi.ConnecterAsync(TextBoxEmail.Text, TextBoxMotDePasse.Text)
            If r = "2FA" Then
                LabelCode.Visible = True : TextBoxCode.Visible = True : ButtonCode.Visible = True
                TextBoxCode.Text = ""
                TextBoxCode.Focus()
                Etat("Blink envoie un code de contrôle (e-mail ou SMS) : saisissez-le puis « Valider le code ».")
            ElseIf r = "OK" Then
                Etat("Connecté.")
                ListerLesCameras()
            Else
                Etat(r)
            End If
        Finally
            _occupe = False : MettreAJour()
        End Try
    End Sub

    Private Async Sub ButtonCode_Click(sender As Object, e As EventArgs) Handles ButtonCode.Click
        If TextBoxCode.Text.Trim() = "" Then Exit Sub
        _occupe = True : MettreAJour()
        Etat("Vérification du code...")
        Try
            Dim r As String = Await BlinkApi.VerifierLeCodeAsync(TextBoxCode.Text)
            If r = "OK" Then
                LabelCode.Visible = False : TextBoxCode.Visible = False : ButtonCode.Visible = False
                Etat("Connecté.")
                ListerLesCameras()
            Else
                Etat(r)
            End If
        Finally
            _occupe = False : MettreAJour()
        End Try
    End Sub

    Private Async Sub ListerLesCameras()
        _occupe = True : MettreAJour()
        Try
            Dim liste As List(Of BlinkApi.CameraBlink) = Await BlinkApi.ListerLesCamerasAsync()
            _cameras = liste
            ListBoxCameras.Items.Clear()
            For Each c As BlinkApi.CameraBlink In liste
                ListBoxCameras.Items.Add(c.Nom & "   (" & TypeEnClair(c.Type) & ")")
            Next
            Etat(If(liste.Count = 0, "Aucune caméra trouvée sur ce compte.", liste.Count & " caméra(s) trouvée(s)."))
        Catch ex As Exception
            Etat("Liste des caméras impossible : " & ex.Message)
            DiagLog("Blink : liste des cameras impossible : " & ex.Message)
        Finally
            _occupe = False : MettreAJour()
        End Try
    End Sub

    Private Shared Function TypeEnClair(type As String) As String
        Select Case type
            Case "owl" : Return "Blink Mini"
            Case "doorbell" : Return "sonnette vidéo"
            Case Else : Return "caméra"
        End Select
    End Function

    Private Sub ButtonActualiser_Click(sender As Object, e As EventArgs) Handles ButtonActualiser.Click
        ListerLesCameras()
    End Sub

    ''' <summary>Joue tout de suite le dernier clip, avec les reglages affiches dans le panneau de l'affichage automatique.</summary>
    Private Async Sub ButtonEssai_Click(sender As Object, e As EventArgs) Handles ButtonEssai.Click
        Dim auto As PanneauBlinkAuto = Nothing
        If Parent IsNot Nothing Then auto = Parent.Controls.OfType(Of PanneauBlinkAuto)().FirstOrDefault()
        ButtonEssai.Enabled = False
        Etat("Téléchargement du dernier clip...")
        Try
            Etat(Await BlinkClips.EssayerLeDernier(If(auto IsNot Nothing, auto.ReglagesAffiches(), BlinkClips.Reglages())))
        Catch ex As Exception
            Etat("Essai impossible : " & ex.Message)
        Finally
            ButtonEssai.Enabled = True
        End Try
    End Sub

    Private Sub ButtonDeconnecter_Click(sender As Object, e As EventArgs) Handles ButtonDeconnecter.Click
        BlinkApi.Deconnecter()
        ListBoxCameras.Items.Clear()
        Etat("Déconnecté : la connexion enregistrée est effacée.")
        MettreAJour()
    End Sub

End Class
