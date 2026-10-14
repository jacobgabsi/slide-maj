''' <summary>
''' Annonce des taches de la minuterie (22/10/2026 : "peux-tu afficher ce message 30 s avant pour les taches de la minuterie ?") : 30 secondes avant l'heure d'une tache qui change ce
''' que l'on voit (film, chaine TV, IPTV ou satellite en plein ecran, ecran noir, fete juive, programme lance), la carte "A suivre dans 27 s" de FormAnnonceSuite apparait avec le nom de la tache
''' et sa barre qui se remplit. Case a cocher : Form4, onglet Divers, groupe Systeme. Un minuteur d'une seconde regarde la minuterie (les taches partent a la minute pile).
''' </summary>
Friend Module AnnonceMinuterie

    Private WithEvents _minuteur As New System.Windows.Forms.Timer With {.Interval = 1000}

    ''' <summary>Lance la surveillance (une fois, au demarrage).</summary>
    Friend Sub Demarrer()
        _minuteur.Start()
    End Sub

    ''' <summary>Les taches dont l'heure est dans les 30 prochaines secondes : celle qui vient en premier, avec son compte a rebours.</summary>
    Private Function ProchaineTache(ByRef restant As Double, ByRef titre As String) As Boolean
        Dim maintenant As Date = Now
        Dim jdls As Integer = CInt(maintenant.DayOfWeek) - 1
        If jdls = -1 Then jdls = 6
        Dim aujourdhui As String = Format(jdls, "00")
        Dim meilleur As Double = Double.MaxValue
        For Each objet As Object In LigneMinuterie
            Dim l As Minuterie = CType(objet, Minuterie)
            If Not l.Actif OrElse TableauTuya.Ignoree(l) Then Continue For          ' (minuterie optionnelle sur OFF : pas d'annonce)
            If Not Visible_(l) Then Continue For
            If LigneDejaEnchainee(l) Then Continue For                        ' (deja lancee a la suite du film d'avant : elle joue deja)
            If UCase(CStr(l.Jour)) = "BOOT" OrElse UCase(CStr(l.Jour)) = "SHUTDOWN" Then Continue For                  ' (taches du demarrage : pas d'heure)
            Dim code As String = CStr(RTConvert(l.Tri, jdls))
            If code.Length < 6 OrElse Left(code, 2) <> aujourdhui Then Continue For
            Dim heure As Integer, minute As Integer
            If Not Integer.TryParse(Mid(code, 3, 2), heure) OrElse Not Integer.TryParse(Mid(code, 5, 2), minute) Then Continue For
            Dim cible As Date = maintenant.Date.AddHours(heure).AddMinutes(minute)
            Dim reste As Double = (cible - maintenant).TotalSeconds
            If reste >= 0 AndAlso reste <= FormAnnonceSuite.Secondes AndAlso reste < meilleur Then
                meilleur = reste
                titre = Libelle(l)
            End If
        Next
        restant = meilleur
        Return meilleur < Double.MaxValue
    End Function

    ''' <summary>Une tache qui change l'ecran : film, TV, IPTV ou SAT en plein ecran (pas en PIP, pas un arret "OFF"), ecran noir, fete juive, programme lance.</summary>
    Private Function Visible_(l As Minuterie) As Boolean
        Dim t As String = UCase(CStr(l.Type))
        Dim action As String = UCase(CStr(l.Action))
        Dim pip As Boolean = UCase(CStr(Arg("Pip", If(l.Argument, "")))) = "TRUE"
        Select Case True
            Case t = "FILM" OrElse t = "TV" OrElse t = "IPTV" OrElse t = "CAMÉRA BLINK" OrElse t = "CAMÉRA IP" OrElse t.StartsWith("SAT")
                Return Not pip AndAlso Not action.Contains("OFF")
            Case t = "ECRAN NOIR" OrElse t = "YOM TOV" OrElse t = "LANCER"
                Return True
        End Select
        Return False
    End Function

    Private Function Libelle(l As Minuterie) As String
        Dim t As String = UCase(CStr(l.Type))
        Select Case True
            Case t = "FILM"
                Return TitreDeLaLigne(l)
            Case t = "ECRAN NOIR"
                Return "Écran noir"
            Case t = "LANCER"
                Return "Programme : " & CStr(Arg("Prg", If(l.Argument, "")))
            Case Else
                Return CStr(l.Type) & " : " & CStr(l.Action)
        End Select
    End Function

    Private Sub _minuteur_Tick(sender As Object, e As EventArgs) Handles _minuteur.Tick
        Try
            ' Pas pendant l'annonce d'un enchainement de films (meme carte), ni si l'option est decochee.
            If Not My.Settings.AnnonceMinuterie OrElse SuiteTitre <> "" Then Exit Sub
            Dim restant As Double, titre As String = ""
            If ProchaineTache(restant, titre) Then
                FormAnnonceSuite.Mettre(restant, titre, FenetrePrincipale())
            ElseIf FormAnnonceSuite.EstAffichee Then
                FormAnnonceSuite.Cacher()
            End If
        Catch ex As Exception
        End Try
    End Sub

End Module
