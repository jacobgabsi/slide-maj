''' <summary>
''' Ajouter une radio a la liste des reglages (cadre Radio de l'onglet Audio ; 20/10/2026 : le bouton "+" ne faisait rien).
''' Un nom (48 caracteres au plus : la liste data\radio.txt le range sur 49) et l'adresse du flux ; "Ecouter" l'essaie avant de l'ajouter.
''' Controles : nom et adresse remplis, adresse en http(s) ou ftp, ni le nom ni l'adresse deja dans la liste.
''' </summary>
Public Class FormAjoutRadio

    Private _noms As New List(Of String)
    Private _adresses As New List(Of String)
    Private _essaiEnCours As Boolean

    ''' <summary>Nom saisi (apres OK).</summary>
    Friend Property Nom As String = ""
    ''' <summary>Adresse saisie (apres OK).</summary>
    Friend Property Adresse As String = ""

    ''' <summary>Radios cochees dans la fenetre "Radios sur Internet" (bouton "Choisir parmi des radios sur Internet...") : si elle n'est pas vide,
    ''' c'est elle qui est a ajouter, plutot que le nom et l'adresse saisis.</summary>
    Friend ReadOnly Proposees As New List(Of AnnuaireRadios.RadioProposee)

    ''' <summary>Montre la fenetre. <paramref name="noms"/> et <paramref name="adresses"/> : ce que la liste contient deja (doublons refuses).
    ''' Rend Vrai si des radios sont a ajouter : soit celle saisie (<paramref name="nomChoisi"/>, <paramref name="adresseChoisie"/>), soit celles
    ''' choisies parmi les suggestions (<paramref name="proposees"/> n'est alors pas vide).</summary>
    Friend Shared Function Demander(proprietaire As IWin32Window, noms As IEnumerable(Of String), adresses As IEnumerable(Of String),
                                    ByRef nomChoisi As String, ByRef adresseChoisie As String, proposees As List(Of AnnuaireRadios.RadioProposee)) As Boolean
        Using f As New FormAjoutRadio()
            f._noms.AddRange(noms.Select(Function(n) If(n, "").Trim()))
            f._adresses.AddRange(adresses.Select(Function(a) If(a, "").Trim()))
            ' Une adresse deja copiee (navigateur, site de la radio) est proposee.
            Try
                If Clipboard.ContainsText() Then
                    Dim presse As String = Clipboard.GetText().Trim()
                    If EstUneAdresse(presse) AndAlso presse.IndexOfAny({ChrW(10), ChrW(13), " "c}) < 0 Then f.TextBoxAdresse.Text = presse
                End If
            Catch ex As Exception
            End Try
            If f.ShowDialog(proprietaire) <> DialogResult.OK Then Return False
            nomChoisi = f.Nom
            adresseChoisie = f.Adresse
            proposees.AddRange(f.Proposees)
            Return True
        End Using
    End Function

    Private Shared Function EstUneAdresse(texte As String) As Boolean
        Dim t As String = If(texte, "").Trim().ToLowerInvariant()
        Return t.StartsWith("http://", StringComparison.Ordinal) OrElse t.StartsWith("https://", StringComparison.Ordinal) OrElse t.StartsWith("ftp://", StringComparison.Ordinal)
    End Function

    ''' <summary>Le probleme de la saisie, "" si elle est bonne.</summary>
    Private Function Probleme() As String
        Dim n As String = TextBoxNom.Text.Trim()
        Dim a As String = TextBoxAdresse.Text.Trim()
        If n = "" Then Return "Donnez un nom à la radio."
        If n.StartsWith("# DELETED #", StringComparison.OrdinalIgnoreCase) Then Return "Ce nom est réservé : choisissez-en un autre."
        If a = "" Then Return "Indiquez l'adresse du flux."
        If Not EstUneAdresse(a) Then Return "L'adresse doit commencer par http:// ou https://."
        If a.IndexOfAny({" "c, ChrW(9)}) >= 0 Then Return "L'adresse ne doit pas contenir d'espace."
        If _noms.Any(Function(x) String.Equals(x, n, StringComparison.OrdinalIgnoreCase)) Then Return "Une radio porte déjà ce nom."
        If _adresses.Any(Function(x) String.Equals(x, a, StringComparison.OrdinalIgnoreCase)) Then Return "Cette adresse est déjà dans la liste."
        Return ""
    End Function

    Private Sub Valider(sender As Object, e As EventArgs) Handles TextBoxNom.TextChanged, TextBoxAdresse.TextChanged
        ' Le message n'apparait qu'une fois les deux champs touches : pas d'erreur sur un formulaire encore vide.
        Dim p As String = Probleme()
        LabelMessage.Text = If(TextBoxNom.Text = "" OrElse TextBoxAdresse.Text = "", "", p)
        ButtonAjouter.Enabled = (p = "")
        ButtonEcouter.Enabled = EstUneAdresse(TextBoxAdresse.Text) AndAlso TextBoxAdresse.Text.IndexOf(" "c) < 0
    End Sub

    Private Sub FormAjoutRadio_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        StylePro.Appliquer(Me)
        Valider(Nothing, EventArgs.Empty)
        TextBoxNom.Select()
    End Sub

    ''' <summary>"Ecouter" : joue l'adresse (la radio en cours est remplacee) ; un second clic arrete l'essai.</summary>
    Private Sub ButtonEcouter_Click(sender As Object, e As EventArgs) Handles ButtonEcouter.Click
        If _essaiEnCours Then
            ArreterLEssai()
            Exit Sub
        End If
        Try
            MusiqueVLC.MusiqueJouerRadio(TextBoxAdresse.Text.Trim())
            _essaiEnCours = True
            ButtonEcouter.Text = "Arrêter l'essai"
            DiagLog("Radio (ajout) : essai de " & TextBoxAdresse.Text.Trim())
        Catch ex As Exception
            LabelMessage.Text = "Cette adresse n'a pas pu être lue : " & ex.Message
        End Try
    End Sub

    Private Sub ArreterLEssai()
        If Not _essaiEnCours Then Exit Sub
        _essaiEnCours = False
        ButtonEcouter.Text = "Écouter"
        Try
            MusiqueVLC.MusiqueArreter()
        Catch ex As Exception
        End Try
    End Sub

    ''' <summary>"Choisir parmi des radios sur Internet..." : liste de radios proposees par un annuaire ; celles qui sont cochees sont ajoutees d'un coup.</summary>
    Private Sub ButtonSuggestions_Click(sender As Object, e As EventArgs) Handles ButtonSuggestions.Click
        ArreterLEssai()
        Dim choisies As New List(Of AnnuaireRadios.RadioProposee)
        If Not FormSuggestionsRadio.Demander(Me, _noms, _adresses, choisies) Then Exit Sub
        Proposees.Clear()
        Proposees.AddRange(choisies)
        DialogResult = DialogResult.OK
    End Sub

    ' Modifier l'adresse pendant un essai : l'essai est arrete (il ne correspondrait plus a ce qui sera ajoute).
    Private Sub AdresseModifiee(sender As Object, e As EventArgs) Handles TextBoxAdresse.TextChanged
        If _essaiEnCours Then ArreterLEssai()
    End Sub

    Private Sub ButtonAjouter_Click(sender As Object, e As EventArgs) Handles ButtonAjouter.Click
        Dim p As String = Probleme()
        If p <> "" Then
            LabelMessage.Text = p
            Exit Sub
        End If
        ArreterLEssai()
        Nom = TextBoxNom.Text.Trim()
        Adresse = TextBoxAdresse.Text.Trim()
        DialogResult = DialogResult.OK
    End Sub

    Private Sub FormAjoutRadio_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        ArreterLEssai()                      ' (Annuler, croix : l'essai ne continue pas a jouer)
    End Sub

End Class
