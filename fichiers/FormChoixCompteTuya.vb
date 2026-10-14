''' <summary>
''' Plusieurs comptes Tuya : quand on choisit le type "Tuya" pour une ligne de la minuterie (ou qu'on modifie ses options), cette fenetre demande de quel
''' compte sont les appareils de la ligne ; le choix est garde dans l'argument de la ligne ("!/Compte:Nom"). La fenetre de choix qui suit ne propose que les appareils de ce compte (et les groupes).
''' </summary>
Public Class FormChoixCompteTuya

    ''' <summary>Rend Vrai si on valide ; <paramref name="choix"/> : le nom du compte choisi (le compte <paramref name="deja"/> est choisi d'avance).</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, comptes As IEnumerable(Of String), deja As String, ByRef choix As String) As Boolean
        Using f As New FormChoixCompteTuya()
            For Each nom As String In comptes
                f.ListBoxComptes.Items.Add(nom)
            Next
            Dim i As Integer = f.ListBoxComptes.Items.IndexOf(If(deja, ""))
            f.ListBoxComptes.SelectedIndex = Math.Max(0, i)
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            choix = CStr(f.ListBoxComptes.SelectedItem)
            Return True
        End Using
    End Function

    Private Sub FormChoixCompteTuya_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

    Private Sub ListBoxComptes_DoubleClick(sender As Object, e As EventArgs) Handles ListBoxComptes.DoubleClick
        If ListBoxComptes.SelectedIndex >= 0 Then DialogResult = DialogResult.OK
    End Sub

End Class
