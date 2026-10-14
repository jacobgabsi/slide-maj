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
            RechercheNflix.DemandeTresBienNotes = False
            f.CheckBoxFrancais.Checked = RechercheNflix.QueFrancais
            f.CheckBoxChercherGroupes.Checked = RechercheNflix.QueLesGroupes
            f.TextBoxAnnee.Text = RechercheNflix.DerniereAnnee
            f.ButtonBienNotes.Text = "Films > " & RechercheNflix.NoteTresBonne.ToString("0.#", Globalization.CultureInfo.CurrentCulture) & " (fond vert)"
            f.ButtonExcellents.Text = "Films > " & RechercheNflix.NoteExcellente.ToString("0.#", Globalization.CultureInfo.CurrentCulture) & " (fond rose)"
            RechercheNflix.DemandeExcellents = False
            RechercheNflix.DemandeBons = False
            f.ButtonBons.Text = "Films > " & RechercheNflix.NoteBonne.ToString("0.#", Globalization.CultureInfo.CurrentCulture) & " (fond violet)"
            Dim reponse As DialogResult = f.ShowDialog(proprietaire)
            If reponse <> DialogResult.OK AndAlso reponse <> DialogResult.Yes AndAlso reponse <> DialogResult.Retry AndAlso reponse <> DialogResult.Ignore Then Return Nothing
            RechercheNflix.DemandeTresBienNotes = (reponse = DialogResult.Yes OrElse reponse = DialogResult.Retry OrElse reponse = DialogResult.Ignore)      ' (le texte et le groupe, s'ils sont remplis, restreignent la liste)
            RechercheNflix.DemandeExcellents = (reponse = DialogResult.Retry)
            RechercheNflix.DemandeBons = (reponse = DialogResult.Ignore)
            dansLeGroupe = f.CheckBoxGroupe.Checked
            RechercheNflix.QueFrancais = f.CheckBoxFrancais.Checked
            RechercheNflix.QueLesGroupes = f.CheckBoxChercherGroupes.Checked
            RechercheNflix.DerniereAnnee = f.TextBoxAnnee.Text.Trim()
            RechercheNflix.LireLAnnee(RechercheNflix.DerniereAnnee, RechercheNflix.AnneeDebut, RechercheNflix.AnneeFin)      ' (deja verifiee a la fermeture)
            Return f.TextBoxTexte.Text
        End Using
    End Function

    ''' <summary>Une annee incomprehensible (« 20x6 ») : la fenetre reste ouverte et le dit.</summary>
    Private Sub FormRechercheNflix_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        If Me.DialogResult = DialogResult.Cancel OrElse Me.DialogResult = DialogResult.None Then Exit Sub
        Dim d As Integer, f As Integer
        If Not RechercheNflix.LireLAnnee(TextBoxAnnee.Text, d, f) Then
            e.Cancel = True
            Me.DialogResult = DialogResult.None
            MessageBox.Show(Me, "Année : tapez 2026, ou 2024-2026 pour plusieurs années (ou laissez vide).", "Rechercher", MessageBoxButtons.OK, MessageBoxIcon.Information)
            TextBoxAnnee.Focus()
            TextBoxAnnee.SelectAll()
        End If
    End Sub

    Private Sub FormRechercheNflix_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
    End Sub

End Class
