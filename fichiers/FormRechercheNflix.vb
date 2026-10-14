Option Strict On
Option Explicit On

''' <summary>
''' Saisie de la recherche de la fenetre Nflix : le texte, et la case « Dans ce groupe » (la recherche ne porte alors que sur
''' le groupe choisi dans la liste de gauche). Remplace l'ancienne boite de saisie simple.
''' </summary>
Public Class FormRechercheNflix

    ''' <summary>Montre la fenetre. Rend le texte saisi, ou Nothing si elle est annulee. <paramref name="dansLeGroupe"/>
    ''' recoit l'etat de la case. Sans groupe choisi, la case est grisee (et decochee).</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, texteInitial As String, groupe As String,
                                    ByRef dansLeGroupe As Boolean) As String
        Using f As New FormRechercheNflix()
            f.TextBoxTexte.Text = If(texteInitial, "")
            f.TextBoxTexte.SelectAll()
            If groupe = "" Then
                f.CheckBoxGroupe.Text = "Dans ce groupe (aucun groupe choisi dans la liste)"
                f.CheckBoxGroupe.Checked = False
                f.CheckBoxGroupe.Enabled = False
            Else
                f.CheckBoxGroupe.Text = "Dans ce groupe seulement : " & groupe
                f.CheckBoxGroupe.Checked = dansLeGroupe
            End If
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return Nothing
            dansLeGroupe = f.CheckBoxGroupe.Checked
            Return f.TextBoxTexte.Text
        End Using
    End Function

    Private Sub FormRechercheNflix_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
