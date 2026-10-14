''' <summary>
''' Reglages et commandes des mises a jour de Slide (voir MiseAJour) : ou sont publiees les versions, ce qu'on
''' fait d'une version plus recente, verification a la demande, installation immediate, et - sur le poste de
''' developpement - publication de la version en cours. Demande de Jacob, 14/10/2026.
''' </summary>
Public Class FormMiseAJour

    Private Const MotifAuto As String = "La préparer toute seule en arrière-plan (installée au prochain lancement ou la nuit)"
    Private Const MotifDemander As String = "Me la proposer au lancement (installée d'elle-même après 30 s sans réponse)"
    Private Const MotifNon As String = "Ne rien faire"

    Private Sub FormMiseAJour_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        MoletteSousLaSouris.Suivre(Me)
        Dim source As Boolean = MiseAJour.EstLaSource()
        LabelVersion.Text = "Slide " & SlideVersion & If(source, "  -  programme source (dossier du projet) : il se publie, il ne se met pas à jour", "")
        TextBoxDossier.Text = My.Settings.Maj_Dossier
        TextBoxAdresse.Text = My.Settings.Maj_AdresseVersion
        ComboBoxMode.Items.AddRange({MotifAuto, MotifDemander, MotifNon})
        Select Case If(My.Settings.Maj_Mode, "demander")
            Case "demander" : ComboBoxMode.SelectedItem = MotifDemander
            Case "non" : ComboBoxMode.SelectedItem = MotifNon
            Case "auto" : ComboBoxMode.SelectedItem = MotifAuto
            Case Else : ComboBoxMode.SelectedItem = MotifDemander
        End Select
        ButtonVerifier.Enabled = Not source                       ' (le poste qui publie ne se met pas a jour)
        LabelPaquet.Visible = False                              ' (plus de paquet zip : les fichiers sont pris un par un)
        TextBoxPaquet.Visible = False
        ButtonInstaller.Visible = MiseAJour.PaquetPret()
        LabelEtat.Text = If(ButtonInstaller.Visible, "La version " & MiseAJour.VersionDisponible & " est prête.", "")
        If MiseAJour.PaquetPret() Then LabelEtat.Text = "Une version plus récente est prête : elle sera installée au prochain lancement, ou tout de suite par « Installer maintenant »."
    End Sub

    ''' <summary>Les champs deviennent les reglages (sans quitter la fenetre).</summary>
    Private Sub Retenir()
        My.Settings.Maj_Dossier = TextBoxDossier.Text.Trim()
        My.Settings.Maj_AdresseVersion = TextBoxAdresse.Text.Trim()
        Select Case CStr(ComboBoxMode.SelectedItem)
            Case MotifAuto : My.Settings.Maj_Mode = "auto"
            Case MotifNon : My.Settings.Maj_Mode = "non"
            Case Else : My.Settings.Maj_Mode = "demander"
        End Select
        Try
            My.Settings.Save()
        Catch ex As Exception
        End Try
    End Sub

    Private Sub ButtonParcourir_Click(sender As Object, e As EventArgs) Handles ButtonParcourir.Click
        Using choix As New FolderBrowserDialog
            choix.Description = "Dossier des versions publiées (" & MiseAJour.NomDuManifeste & " et son dossier fichiers)"
            choix.UseDescriptionForTitle = True
            If TextBoxDossier.Text.Trim() <> "" AndAlso IO.Directory.Exists(TextBoxDossier.Text.Trim()) Then choix.SelectedPath = TextBoxDossier.Text.Trim()
            If choix.ShowDialog(Me) = DialogResult.OK Then TextBoxDossier.Text = choix.SelectedPath
        End Using
    End Sub

    Private Async Sub ButtonVerifier_Click(sender As Object, e As EventArgs) Handles ButtonVerifier.Click
        Retenir()
        ButtonVerifier.Enabled = False
        ProgressBarMaj.Style = ProgressBarStyle.Marquee
        ProgressBarMaj.Visible = True
        LabelEtat.Text = "Vérification..."
        Dim progres As New Progress(Of KeyValuePair(Of Long, Long))(
            Sub(kv)
                If kv.Value > 0 Then
                    ProgressBarMaj.Style = ProgressBarStyle.Continuous
                    ProgressBarMaj.Value = CInt(Math.Min(100L, kv.Key * 100L \ kv.Value))
                    LabelEtat.Text = "Téléchargement : " & CInt(Math.Min(100L, kv.Key * 100L \ kv.Value)) & " %  (" & Telechargements.TailleLisible(kv.Key) & " / " & Telechargements.TailleLisible(kv.Value) & ")"
                End If
            End Sub)
        Dim bilan As BilanMaj = Await MiseAJour.Verifier(True, progres)
        ProgressBarMaj.Visible = False
        ButtonVerifier.Enabled = Not MiseAJour.EstLaSource()
        LabelEtat.Text = bilan.Message & If(bilan.Info IsNot Nothing AndAlso bilan.Info.Notes <> "", vbCrLf & "Notes : " & bilan.Info.Notes, "")
        ButtonInstaller.Visible = MiseAJour.PaquetPret()
    End Sub

    Private Sub ButtonInstaller_Click(sender As Object, e As EventArgs) Handles ButtonInstaller.Click
        If MessageBox.Show(Me, "Slide va se fermer puis redémarrer avec la nouvelle version (le diaporama reprend là où il en était)." & vbCrLf & vbCrLf & "Continuer ?",
                           "Mises à jour", MessageBoxButtons.OKCancel, MessageBoxIcon.Question) <> DialogResult.OK Then Exit Sub
        Retenir()
        RelanceDeNuit.RelancerMaintenant()
    End Sub

    Private Sub ButtonEnregistrer_Click(sender As Object, e As EventArgs) Handles ButtonEnregistrer.Click
        Retenir()
        Close()
    End Sub

End Class
