''' <summary>
''' Type "Tuya" dans la minuterie (ou « Modifier les options » d'une ligne Tuya) : cette fenetre demande de quelle MAISON sont les appareils de la ligne (28/10/2026 : « il faut lister les maisons,
''' pas les comptes »). Chaque maison appartient a un compte ; le choix est garde dans l'argument de la ligne ("!/Compte:Nom!/Maison:Nom"). La fenetre de choix qui suit ne propose que les
''' commandes de cette maison (et les groupes de la maison).
''' </summary>
Public Class FormChoixCompteTuya

    ''' <summary>Une maison de la liste : son compte, son nom ("" : compte dont les maisons ne sont pas connues).</summary>
    Private NotInheritable Class Entree
        Friend ReadOnly Compte As String
        Friend ReadOnly Maison As String
        Private ReadOnly _libelle As String
        Friend Sub New(compte As String, maison As String, avecLeCompte As Boolean)
            Me.Compte = compte : Me.Maison = maison
            ' « Compte  ›  Maison » : le compte et la maison sont toujours presentes ensemble (28/10/2026).
            _libelle = compte & If(maison <> "", "   ›   " & maison, "   ›   (maison inconnue : rechargez les appareils)")
        End Sub
        Public Overrides Function ToString() As String
            Return _libelle
        End Function
    End Class

    ''' <summary>Rend Vrai si on valide. <paramref name="maisons"/> : les couples (compte, maison) a proposer ; <paramref name="dejaCompte"/> / <paramref name="dejaMaison"/> : la maison deja choisie d'avance ;
    ''' <paramref name="compte"/> et <paramref name="maison"/> : ce qui est choisi.</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, maisons As IEnumerable(Of KeyValuePair(Of String, String)), dejaCompte As String, dejaMaison As String,
                                    ByRef compte As String, ByRef maison As String) As Boolean
        Using f As New FormChoixCompteTuya()
            Dim liste As List(Of KeyValuePair(Of String, String)) = maisons.ToList()
            Dim avecLeCompte As Boolean = liste.Select(Function(p) p.Key).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1
            Dim choisie As Integer = 0
            For i As Integer = 0 To liste.Count - 1
                f.ListBoxComptes.Items.Add(New Entree(liste(i).Key, liste(i).Value, avecLeCompte))
                If String.Equals(liste(i).Key, If(dejaCompte, ""), StringComparison.OrdinalIgnoreCase) AndAlso String.Equals(liste(i).Value, If(dejaMaison, ""), StringComparison.CurrentCultureIgnoreCase) Then choisie = i
            Next
            f.ListBoxComptes.SelectedIndex = If(liste.Count > 0, choisie, -1)
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            Dim e As Entree = TryCast(f.ListBoxComptes.SelectedItem, Entree)
            If e Is Nothing Then Return False
            compte = e.Compte
            maison = e.Maison
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
