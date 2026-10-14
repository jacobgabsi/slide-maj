Imports System.Threading

''' <summary>
''' "Verifier les radios" (cadre Radio, 20/10/2026) : chaque radio de la liste est controlee (voir VerificationRadios), six a la fois, avec
''' une barre d'avancement. Celles qui ne repondent pas arrivent dans une liste a cocher (toutes cochees) ; "Supprimer les cochees" rend leurs
''' noms a l'appelant. Les radios qui repondent ne sont pas listees. "Arreter" interrompt la verification.
''' </summary>
Public Class FormVerifierRadios

    Private Const EnParallele As Integer = 6

    ''' <summary>Une radio a controler : son rang dans la liste des reglages (deux radios peuvent porter le meme nom), son nom, son adresse.</summary>
    Friend Class RadioAVerifier
        Friend Rang As Integer
        Friend Nom As String
        Friend Adresse As String
    End Class

    Private Class Panne
        Friend Rang As Integer
        Friend Nom As String
        Friend Detail As String
        Public Overrides Function ToString() As String
            Return Nom & "   —   " & Detail
        End Function
    End Class

    Private _radios As List(Of RadioAVerifier)
    Private ReadOnly _annulation As New CancellationTokenSource()
    Private _faites As Integer
    Private _enCours As Boolean

    ''' <summary>Rangs des radios a supprimer (cochees au moment du clic sur "Supprimer les cochees").</summary>
    Friend ReadOnly ASupprimer As New List(Of Integer)

    ''' <summary>Montre la fenetre et lance la verification. Rend Vrai si des radios sont a supprimer (leurs rangs dans
    ''' <paramref name="aSupprimer"/>).</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, radios As List(Of RadioAVerifier), aSupprimer As List(Of Integer)) As Boolean
        Using f As New FormVerifierRadios()
            f._radios = radios
            f.ShowDialog(proprietaire)
            aSupprimer.AddRange(f.ASupprimer)
            Return f.ASupprimer.Count > 0
        End Using
    End Function

    Private Sub FormVerifierRadios_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

    Private Sub FormVerifierRadios_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        Lancer()
    End Sub

    Private Async Sub Lancer()
        If _radios Is Nothing OrElse _radios.Count = 0 Then
            LabelEtat.Text = "La liste ne contient aucune radio."
            ButtonFermer.Text = "Fermer"
            Exit Sub
        End If
        _enCours = True
        ProgressBarVerif.Maximum = _radios.Count
        ProgressBarVerif.Value = 0
        MontrerLAvancement()
        Dim limite As New SemaphoreSlim(EnParallele)
        Dim jeton As CancellationToken = _annulation.Token
        Dim taches As New List(Of Task)
        For Each radio As RadioAVerifier In _radios
            Dim cetteRadio As RadioAVerifier = radio
            taches.Add(Task.Run(Async Function() As Task
                                    Try
                                        Await limite.WaitAsync(jeton)
                                    Catch ex As OperationCanceledException
                                        Return
                                    End Try
                                    Try
                                        Dim res As VerificationRadios.Resultat = Await VerificationRadios.Verifier(cetteRadio.Adresse, jeton)
                                        If Not Me.IsDisposed AndAlso Me.IsHandleCreated Then
                                            Me.BeginInvoke(New Action(Sub() Rapporter(cetteRadio, res)))
                                        End If
                                    Finally
                                        limite.Release()
                                    End Try
                                End Function))
        Next
        Try
            Await Task.WhenAll(taches)
        Catch ex As Exception
        End Try
        If Me.IsDisposed Then Exit Sub
        _enCours = False
        ButtonFermer.Text = "Fermer"
        Dim pannes As Integer = CheckedListBoxPannes.Items.Count
        If jeton.IsCancellationRequested Then
            LabelEtat.Text = "Vérification arrêtée : " & _faites & " radio(s) sur " & _radios.Count & " contrôlée(s), " & pannes & " ne répond(ent) pas."
        ElseIf pannes = 0 Then
            LabelEtat.Text = "Les " & _radios.Count & " radios répondent."
        Else
            LabelEtat.Text = _radios.Count & " radios contrôlées : " & pannes & " ne répond" & If(pannes > 1, "ent", "") & " pas."
        End If
        ButtonSupprimer.Enabled = CheckedListBoxPannes.CheckedItems.Count > 0
    End Sub

    Private Sub MontrerLAvancement()
        LabelEtat.Text = "Vérification des radios : " & _faites & " / " & _radios.Count & "  (" & CheckedListBoxPannes.Items.Count & " ne répondent pas)"
    End Sub

    ''' <summary>Une radio vient d'etre controlee (fil de l'interface).</summary>
    Private Sub Rapporter(radio As RadioAVerifier, res As VerificationRadios.Resultat)
        _faites += 1
        If _faites <= ProgressBarVerif.Maximum Then ProgressBarVerif.Value = _faites
        If Not res.Ok Then
            CheckedListBoxPannes.Items.Add(New Panne With {.Rang = radio.Rang, .Nom = radio.Nom, .Detail = res.Detail}, True)
            ButtonSupprimer.Enabled = True
        End If
        If _enCours Then MontrerLAvancement()
    End Sub

    Private Sub CheckedListBoxPannes_ItemCheck(sender As Object, e As ItemCheckEventArgs) Handles CheckedListBoxPannes.ItemCheck
        ' (la case change apres cet evenement : on compte avec le nouvel etat)
        Dim cochees As Integer = CheckedListBoxPannes.CheckedItems.Count + If(e.NewValue = CheckState.Checked, 1, 0) - If(e.CurrentValue = CheckState.Checked, 1, 0)
        ButtonSupprimer.Enabled = cochees > 0
    End Sub

    Private Sub ButtonToutCocher_Click(sender As Object, e As EventArgs) Handles ButtonToutCocher.Click
        For i As Integer = 0 To CheckedListBoxPannes.Items.Count - 1
            CheckedListBoxPannes.SetItemChecked(i, True)
        Next
    End Sub

    Private Sub ButtonToutDecocher_Click(sender As Object, e As EventArgs) Handles ButtonToutDecocher.Click
        For i As Integer = 0 To CheckedListBoxPannes.Items.Count - 1
            CheckedListBoxPannes.SetItemChecked(i, False)
        Next
    End Sub

    Private Sub ButtonSupprimer_Click(sender As Object, e As EventArgs) Handles ButtonSupprimer.Click
        Dim choisies As New List(Of Integer)
        For Each element As Object In CheckedListBoxPannes.CheckedItems
            choisies.Add(DirectCast(element, Panne).Rang)
        Next
        If choisies.Count = 0 Then Exit Sub
        Dim reponse As DialogResult = MessageBox.Show(Me, "Supprimer " & choisies.Count & " radio" & If(choisies.Count > 1, "s", "") & " de la liste ?" & vbCrLf & vbCrLf &
                                                      "Elles seront retirées définitivement.", "Supprimer les radios",
                                                      MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2)
        If reponse <> DialogResult.Yes Then Exit Sub
        ASupprimer.AddRange(choisies)
        _annulation.Cancel()
        DialogResult = DialogResult.OK
    End Sub

    ''' <summary>"Arreter" / "Fermer" / croix : la verification en cours est interrompue.</summary>
    Private Sub FormVerifierRadios_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        _annulation.Cancel()
    End Sub

    Private Sub ButtonFermer_Click(sender As Object, e As EventArgs) Handles ButtonFermer.Click
        If _enCours Then
            ' "Arreter" : la verification s'arrete, la fenetre reste pour traiter ce qui a ete trouve.
            _annulation.Cancel()
            ButtonFermer.Text = "Fermer"
            Exit Sub
        End If
        Close()
    End Sub

End Class
