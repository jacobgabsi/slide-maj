''' <summary>
''' Import des parametres : la liste des familles de reglages que contient le fichier (Photos, Vidéos, Audio, Deezer, IPTV, Tuya, Affichage...), a cocher.
''' Seules les familles cochees sont importees ; les autres gardent les valeurs de ce PC (voir TransfertParametres.Importer).
''' </summary>
Public Class FormChoixImport

    Private _cles As New List(Of String)

    ''' <summary>
    ''' Montre la fenetre. <paramref name="familles"/> : (cle, libelle avec le nombre de reglages) de ce que contient le fichier. Rend Vrai si on valide, avec les cles cochees
    ''' dans <paramref name="choix"/>.
    ''' </summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, texte As String, familles As IEnumerable(Of KeyValuePair(Of String, String)), ByRef choix As HashSet(Of String)) As Boolean
        Using f As New FormChoixImport()
            f.LabelTexte.Text = texte
            f.TopMost = True                       ' devant tout : sous Slide en plein ecran ou ses PIP, la fenetre pouvait rester cachee (22/10/2026 : "apres le fichier, plus rien")
            f.ShowInTaskbar = True
            f.StartPosition = FormStartPosition.CenterScreen
            For Each famille As KeyValuePair(Of String, String) In familles
                f._cles.Add(famille.Key)
                f.CheckedListBoxChoix.Items.Add(famille.Value, True)
            Next
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            choix = New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
            For i As Integer = 0 To f._cles.Count - 1
                If f.CheckedListBoxChoix.GetItemChecked(i) Then choix.Add(f._cles(i))
            Next
            Return True
        End Using
    End Function

    Private Sub FormChoixImport_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        LabelAvertissement.Text = "Les familles décochées gardent les valeurs actuelles de ce PC. Vos réglages actuels sont sauvegardés avant l'import (dossier data\settings), " &
                                  "et Slide devra être relancé ensuite. Les chemins (dossiers de photos et de films) sont repris tels quels."
    End Sub

    Private Sub ButtonToutCocher_Click(sender As Object, e As EventArgs) Handles ButtonToutCocher.Click
        For i As Integer = 0 To CheckedListBoxChoix.Items.Count - 1
            CheckedListBoxChoix.SetItemChecked(i, True)
        Next
    End Sub

    Private Sub ButtonAucun_Click(sender As Object, e As EventArgs) Handles ButtonAucun.Click
        For i As Integer = 0 To CheckedListBoxChoix.Items.Count - 1
            CheckedListBoxChoix.SetItemChecked(i, False)
        Next
    End Sub

    Private Sub FormChoixImport_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        If DialogResult <> DialogResult.OK Then Exit Sub
        If CheckedListBoxChoix.CheckedItems.Count = 0 Then
            MessageBox.Show(Me, "Rien n'est coché : cochez au moins une famille à importer.", "Importer les paramètres", MessageBoxButtons.OK, MessageBoxIcon.Information)
            e.Cancel = True
        End If
    End Sub

End Class
