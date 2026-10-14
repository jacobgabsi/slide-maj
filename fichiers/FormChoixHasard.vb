Option Strict On
Option Explicit On

''' <summary>
''' Choix des animations tirees au sort (demande de Jacob, 07/10 : "dans hasard, laisse-moi
''' selectionner les animations, et rajoute l'option aucune animation dans hasard").
'''
''' Ouverte par le bouton "Choix du hasard..." des onglets Photos et Videos des reglages
''' (Form4). Deux listes a cocher : les arrivees (ou transitions) et les mouvements. Ce qui est
''' coche est tire au sort quand la liste correspondante des reglages est sur "Au hasard" ; voir
''' TirageAuHasard pour la facon dont le choix est retenu (un bit par animation).
''' Les controles sont dans le Concepteur.
''' </summary>
Public Class FormChoixHasard

    ''' <summary>Montre la fenetre et renvoie True si elle est validee : <paramref name="choix1"/> et
    ''' <paramref name="choix2"/> contiennent alors ce qui est coche.</summary>
    ''' <param name="noms1">Ce qui est ecrit dans la premiere liste.</param>
    ''' <param name="numeros1">Pour chaque ligne, le numero de l'animation (son bit dans le choix).</param>
    Public Shared Function Choisir(proprietaire As IWin32Window, titre As String,
                                   titre1 As String, noms1 As String(), numeros1 As Integer(), ByRef choix1 As Integer,
                                   titre2 As String, noms2 As String(), numeros2 As Integer(), ByRef choix2 As Integer) As Boolean
        Using f As New FormChoixHasard()
            f.Text = titre
            f.LabelListe1.Text = titre1
            f.LabelListe2.Text = titre2
            Remplir(f.CheckedListBox1, noms1, numeros1, choix1)
            Remplir(f.CheckedListBox2, noms2, numeros2, choix2)
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            choix1 = Lire(f.CheckedListBox1, numeros1)
            choix2 = Lire(f.CheckedListBox2, numeros2)
            Return True
        End Using
    End Function

    Friend Shared Sub Remplir(liste As CheckedListBox, noms As String(), numeros As Integer(), choix As Integer)
        liste.Items.Clear()
        For i As Integer = 0 To noms.Length - 1
            liste.Items.Add(noms(i), (choix And (1 << numeros(i))) <> 0)
        Next
    End Sub

    Friend Shared Function Lire(liste As CheckedListBox, numeros As Integer()) As Integer
        Dim choix As Integer = 0
        For i As Integer = 0 To Math.Min(liste.Items.Count, numeros.Length) - 1
            If liste.GetItemChecked(i) Then choix = choix Or (1 << numeros(i))
        Next
        Return choix
    End Function

    Private Shared Sub ToutMettre(liste As CheckedListBox, coche As Boolean)
        For i As Integer = 0 To liste.Items.Count - 1
            liste.SetItemChecked(i, coche)
        Next
    End Sub

    Private Sub ButtonTout1_Click(sender As Object, e As EventArgs) Handles ButtonTout1.Click
        ToutMettre(CheckedListBox1, True)
    End Sub

    Private Sub ButtonRien1_Click(sender As Object, e As EventArgs) Handles ButtonRien1.Click
        ToutMettre(CheckedListBox1, False)
    End Sub

    Private Sub ButtonTout2_Click(sender As Object, e As EventArgs) Handles ButtonTout2.Click
        ToutMettre(CheckedListBox2, True)
    End Sub

    Private Sub ButtonRien2_Click(sender As Object, e As EventArgs) Handles ButtonRien2.Click
        ToutMettre(CheckedListBox2, False)
    End Sub

    ''' <summary>Il faut au moins une ligne cochee dans chaque liste : sans cela, le tirage n'aurait
    ''' rien a tirer (pour ne rien animer, il y a la ligne "Aucune").</summary>
    Private Sub ButtonOK_Click(sender As Object, e As EventArgs) Handles ButtonOK.Click
        If CheckedListBox1.CheckedItems.Count = 0 OrElse CheckedListBox2.CheckedItems.Count = 0 Then
            MessageBox.Show(Me, "Cochez au moins une ligne dans chaque liste." & vbCrLf &
                            "Pour ne pas animer, cochez seulement la ligne « Aucune » ou « Aucun ».",
                            Me.Text, MessageBoxButtons.OK, MessageBoxIcon.Information)
            Exit Sub
        End If
        Me.DialogResult = DialogResult.OK
        Me.Close()
    End Sub

    ''' <summary>L'allure commune des fenetres de Slide (voir StylePro).</summary>
    Private Sub FormChoixHasard_StylePro(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
