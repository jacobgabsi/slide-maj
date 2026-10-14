Option Strict Off
Option Explicit On

Imports Zmanim
Imports Zmanim.TimeZone
Imports Zmanim.TzDatebase
Imports Zmanim.Utilities

''' <summary>
''' Fetes juives (Yom Tov : Rosh Hashana, Yom Kippour, Souccot, Shemini Atseret / Sim'hat Torah, Pessah, Chavouot) annoncees comme Chabbat Chalom,
''' mais PROGRAMMEES DANS LA MINUTERIE (20/10/2026 : "cree la fonction yomtov dans la minuterie et laisse-moi choisir quelles fetes inserer, avec
''' possibilite de date manuelle").
'''
'''   - Type de tache "Yom Tov" : une ligne = une annonce, "Entree : fete" ou "Sortie : fete", avec la date dans le reglage de la ligne
'''     (!/Date:aaaa-mm-jj). La ligne se declenche le jour de la semaine et a l'heure de la ligne, mais n'agit que si la date est celle d'aujourd'hui.
'''   - L'heure de la ligne est calculee a l'insertion avec les zmanim DE LA DATE de la ligne : entree = 3 minutes avant l'allumage des bougies, sortie =
'''     1 minute apres la sortie (tzeit), comme Chabbat. Entree : video du dossier data\HagSameah ; sortie : data\ChavouaTov (AnnonceShabbat, x = 2 puis 3).
'''   - La fenetre FormYomTov propose les fetes du calendrier hebraique (bibliotheque Zmanim) et une date manuelle ; Slide insere les lignes.
'''   - Une entree un vendredi (et une sortie un vendredi ou un samedi) n'est pas inseree si Chabbat Chalom / Chavoua Tov l'annoncent deja.
''' </summary>
Friend Module YomTov

    Friend Const TypeMinuterie As String = "Yom Tov"

    Friend Class Fete
        Friend Nom As String = ""
        Friend Entree As Date
        Friend Sortie As Date

        Friend ReadOnly Property Cle As String
            Get
                Return Entree.ToString("yyyy-MM-dd") & "|" & Sortie.ToString("yyyy-MM-dd")
            End Get
        End Property

        ''' <summary>Faux si Chabbat Chalom annonce deja ce soir-la (entree un vendredi).</summary>
        Friend ReadOnly Property EntreeAnnoncee As Boolean
            Get
                Return Not (Entree.DayOfWeek = DayOfWeek.Friday AndAlso AfficheVideoChabbatShalom)
            End Get
        End Property

        ''' <summary>Faux si Chavoua Tov annonce deja (sortie un samedi) ou si Chabbat suit (sortie un vendredi).</summary>
        Friend ReadOnly Property SortieAnnoncee As Boolean
            Get
                Return Sortie.DayOfWeek <> DayOfWeek.Friday AndAlso Not (Sortie.DayOfWeek = DayOfWeek.Saturday AndAlso AfficheVideoChavouaTov)
            End Get
        End Property
    End Class

    ' ------------------------------------------------------------------------------------------
    ' Localite : les horaires (et un jour ou deux de fete) dependent de la ville.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les localites connues (celles de la liste "Ville" du cadre Sabbat).</summary>
    Friend Function Villes() As List(Of String)
        Dim liste As New List(Of String)
        For u As Integer = 1 To 50
            Dim nom As String = Convert.ToString(City(u, 0))
            If nom <> "" AndAlso Not liste.Contains(nom) Then liste.Add(nom)
        Next
        Return liste
    End Function

    ''' <summary>La localite des reglages : celle affichee dans la liste "Ville" de Form4 (meme non encore validee), sinon celle enregistree.</summary>
    Friend Function VilleDesReglages() As String
        Try
            Dim affichee As String = Convert.ToString(Form4.ComboBox1.Text)
            If affichee <> "" AndAlso Villes().Contains(affichee) Then Return affichee
        Catch ex As Exception
        End Try
        Return Convert.ToString(Shabbat.Ville)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Calendrier : les fetes entre deux dates.
    ' ------------------------------------------------------------------------------------------

    Private Function VilleEnIsrael(ville As String) As Boolean
        For u As Integer = 1 To 50
            If City(u, 0) = ville Then Return String.Equals(City(u, 3), "Asia/Jerusalem", StringComparison.OrdinalIgnoreCase)
        Next
        Return False
    End Function

    ''' <summary>Les fetes dont le jour d'entree ou le jour de sortie tombe entre <paramref name="depuis"/> et <paramref name="jusqua"/>.
    ''' Hors d'Israel les fetes durent deux jours (Pessah, Chavouot, Souccot, Rosh Hashana, Sim'hat Torah) : l'entree est la veille du premier jour,
    ''' la sortie le dernier jour.</summary>
    Friend Function Calculer(depuis As Date, jusqua As Date, ville As String) As List(Of Fete)
        Dim fetes As New List(Of Fete)
        Dim enIsrael As Boolean = VilleEnIsrael(ville)
        Dim jc As New Zmanim.JewishCalendar.JewishCalendar()
        Dim jour As Date = depuis.Date.AddDays(-2)
        Dim fin As Date = jusqua.Date.AddDays(2)
        While jour <= fin
            If jc.IsYomTovAssurBemelacha(jour, enIsrael) Then
                Dim debut As Date = jour
                Dim dernier As Date = jour
                While jc.IsYomTovAssurBemelacha(dernier.AddDays(1), enIsrael)
                    dernier = dernier.AddDays(1)
                End While
                Dim nom As String = NomDeLaFete(jc.GetJewishHoliday(debut, enIsrael).ToString())
                Dim fete As New Fete With {.Nom = nom, .Entree = debut.AddDays(-1), .Sortie = dernier}
                If fete.Sortie >= depuis.Date AndAlso fete.Entree <= jusqua.Date Then fetes.Add(fete)
                jour = dernier.AddDays(1)
            Else
                jour = jour.AddDays(1)
            End If
        End While
        ' Pessah revient deux fois (premiers et derniers jours) : on les distingue.
        For i As Integer = 0 To fetes.Count - 1
            If fetes(i).Nom = "Pessah" Then
                Dim autre As Integer = fetes.FindIndex(Function(x) x IsNot fetes(i) AndAlso x.Nom = "Pessah" AndAlso Math.Abs((x.Entree - fetes(i).Entree).TotalDays) < 20)
                If autre >= 0 Then
                    Dim premiere As Boolean = fetes(i).Entree < fetes(autre).Entree
                    fetes(i).Nom = If(premiere, "Pessah (premiers jours)", "Pessah (derniers jours)")
                End If
            End If
        Next
        Return fetes
    End Function

    Private Function NomDeLaFete(codeBiblio As String) As String
        Select Case codeBiblio
            Case "ROSH_HASHANA" : Return "Roch Hachana"
            Case "YOM_KIPPUR" : Return "Yom Kippour"
            Case "SUCCOS" : Return "Souccot"
            Case "SHEMINI_ATZERES", "SIMCHAS_TORAH" : Return "Chemini Atseret / Sim'hat Torah"
            Case "PESACH" : Return "Pessah"
            Case "SHAVUOS" : Return "Chavouot"
        End Select
        Return "Fête"
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Horaires du jour (zmanim de la date, comme Shabbat : allumage -3 min, sortie +1 min, +60 pour la ville numero 4).
    ' ------------------------------------------------------------------------------------------

    Private Function Calendrier(jour As Date, ville As String) As ComplexZmanimCalendar
        Dim trouve As Integer = 0
        For u As Integer = 1 To 50
            If City(u, 0) = ville Then trouve = u : Exit For
        Next
        If trouve = 0 Then Return Nothing
        Dim fuseau As ITimeZone = New OlsonTimeZone(City(trouve, 3))
        Dim lieu As New GeoLocation(City(trouve, 0), City(trouve, 1), City(trouve, 2), 0, fuseau)
        Return New ComplexZmanimCalendar(jour, lieu)
    End Function

    Private Function Ajoute(ville As String) As Integer
        Return If(City(4, 0) = ville, 60, 0)
    End Function

    ''' <summary>"HH:mm" de l'annonce d'entree (allumage -3 min ; un samedi : la sortie du Shabbat +1 min), "" si inconnue.</summary>
    Friend Function HeureEntree(f As Fete, ville As String) As String
        Try
            Dim zc As ComplexZmanimCalendar = Calendrier(f.Entree, ville)
            If zc Is Nothing Then Return ""
            If f.Entree.DayOfWeek = DayOfWeek.Saturday Then
                Dim t As Date = zc.GetTzais
                Return Format(t.AddMinutes(1 + Ajoute(ville)), "HH:mm")
            End If
            Dim b As Date = zc.GetCandleLighting
            Return Format(b.AddMinutes(-3 + Ajoute(ville)), "HH:mm")
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ''' <summary>"HH:mm" de l'annonce de sortie (sortie +1 min), "" si inconnue.</summary>
    Friend Function HeureSortie(f As Fete, ville As String) As String
        Try
            Dim zc As ComplexZmanimCalendar = Calendrier(f.Sortie, ville)
            If zc Is Nothing Then Return ""
            Dim t As Date = zc.GetTzais
            Return Format(t.AddMinutes(1 + Ajoute(ville)), "HH:mm")
        Catch ex As Exception
            Return ""
        End Try
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Colonne Play : la video de l'annonce (21/10/2026 : "colonne Play, montre la video"). Le clic sur la case la montre.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les fichiers dont une video est tiree au hasard pour l'annonce : entree = data\HagSameah (sinon ShabbatChalom), sortie = data\ChavouaTov.</summary>
    Friend Function FichiersDeLaVideo(entree As Boolean) As String()
        Try
            Dim dossier As String = AppPath & If(entree, "\data\HagSameah\", "\data\chavouatov\")
            If entree AndAlso (Not IO.Directory.Exists(dossier) OrElse IO.Directory.GetFiles(dossier).Length = 0) Then dossier = AppPath & "\data\ShabbatChalom\"
            If Not IO.Directory.Exists(dossier) Then Return New String() {}
            Dim fichiers As String() = IO.Directory.GetFiles(dossier)
            Array.Sort(fichiers, StringComparer.OrdinalIgnoreCase)
            Return fichiers
        Catch ex As Exception
            Return New String() {}
        End Try
    End Function

    ''' <summary>Ce que la case Play montre : le nom de la video (et combien d'autres peuvent passer a sa place).</summary>
    Friend Function TexteDeLaVideo(entree As Boolean) As String
        Dim fichiers As String() = FichiersDeLaVideo(entree)
        If fichiers.Length = 0 Then Return "(aucune vidéo)"
        Return IO.Path.GetFileName(fichiers(0)) & If(fichiers.Length > 1, " (+" & (fichiers.Length - 1) & ")", "")
    End Function

    ''' <summary>Montre la video de l'annonce (clic sur la case Play) avec le lecteur de videos de Windows.</summary>
    Friend Sub MontrerLaVideo(entree As Boolean)
        Try
            Dim fichiers As String() = FichiersDeLaVideo(entree)
            If fichiers.Length = 0 Then
                MessageBox.Show("Aucune vidéo pour cette annonce dans " & If(entree, "data\HagSameah", "data\ChavouaTov") & ".", "Fêtes juives", MessageBoxButtons.OK, MessageBoxIcon.Information)
                Exit Sub
            End If
            Process.Start(New ProcessStartInfo(fichiers(0)) With {.UseShellExecute = True})
        Catch ex As Exception
            DiagLog("Yom Tov : video non montree : " & ex.Message)
        End Try
    End Sub

    ''' <summary>Remplit la case Play des lignes Yom Tov qui n'en ont pas (lignes inserees avant cette colonne).</summary>
    Friend Sub CompleterLesCasesPlay()
        Try
            For Each ligne As DataGridViewRow In Form4.DataGridView1.Rows
                If ligne.IsNewRow Then Continue For
                If UCase(Convert.ToString(ligne.Cells("Type").Value)) <> UCase(TypeMinuterie) Then Continue For
                If Convert.ToString(ligne.Cells("Play").Value).Trim() <> "" Then Continue For
                ligne.Cells("Play").Value = TexteDeLaVideo(UCase(Convert.ToString(ligne.Cells("Action").Value)).StartsWith("ENTR"))
            Next
        Catch ex As Exception
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' Insertion dans la minuterie.
    ' ------------------------------------------------------------------------------------------

    Private Function ArgumentDeLaLigne(date_ As Date, nom As String) As String
        Return "!/Date:" & date_.ToString("yyyy-MM-dd") & "!/Fete:" & nom.Replace("!/", " ")
    End Function

    ''' <summary>Cette annonce (date + entree/sortie) est-elle deja dans la minuterie ?</summary>
    Private Function DejaDansLaMinuterie(date_ As Date, entree As Boolean) As Boolean
        For Each ligne As DataGridViewRow In Form4.DataGridView1.Rows
            If ligne.IsNewRow Then Continue For
            If UCase(Convert.ToString(ligne.Cells("Type").Value)).Trim() <> UCase(TypeMinuterie) Then Continue For
            If CStr(Arg("Date", Convert.ToString(ligne.Cells("Argument").Value))) <> date_.ToString("yyyy-MM-dd") Then Continue For
            Dim estEntree As Boolean = UCase(Convert.ToString(ligne.Cells("Action").Value)).StartsWith("ENTR")
            If estEntree = entree Then Return True
        Next
        Return False
    End Function

    ''' <summary>Insere dans la minuterie l'entree et/ou la sortie des fetes choisies, avec les horaires (zmanim) de leur jour. Si
    ''' <paramref name="ligneLibre"/> est une ligne (-1 : aucune), la premiere annonce la remplit. Rend un compte rendu a montrer.</summary>
    Friend Function Inserer(fetes As IEnumerable(Of Fete), avecEntree As Boolean, avecSortie As Boolean, ligneLibre As Integer, ville As String) As String
        Dim ajoutees As Integer = 0
        Dim ignorees As New List(Of String)
        Dim derniere As Integer = -1
        Dim grille As DataGridView = Form4.DataGridView1
        For Each f As Fete In fetes
            For Each estEntree As Boolean In {True, False}
                If estEntree AndAlso Not avecEntree Then Continue For
                If Not estEntree AndAlso Not avecSortie Then Continue For
                Dim jour As Date = If(estEntree, f.Entree, f.Sortie)
                Dim texte As String = If(estEntree, "Entrée : ", "Sortie : ") & f.Nom
                If Not If(estEntree, f.EntreeAnnoncee, f.SortieAnnoncee) Then
                    ignorees.Add(texte & " (déjà annoncée par Chabbat)")
                    Continue For
                End If
                If DejaDansLaMinuterie(jour, estEntree) Then
                    ignorees.Add(texte & " (déjà dans la minuterie)")
                    Continue For
                End If
                Dim heure As String = If(estEntree, HeureEntree(f, ville), HeureSortie(f, ville))
                If heure = "" Then heure = "18:00"
                Dim nomJour As String = jourDeLaSemaine((CInt(jour.DayOfWeek) + 6) Mod 7)
                Dim argument As String = ArgumentDeLaLigne(jour, f.Nom)
                Dim numero As Integer
                If ligneLibre >= 0 AndAlso ligneLibre < grille.Rows.Count AndAlso Not grille.Rows(ligneLibre).IsNewRow Then
                    numero = ligneLibre
                    ligneLibre = -1
                    With grille.Rows(numero)
                        .Cells("Actif").Value = True
                        .Cells("Jour").Value = nomJour
                        .Cells("Heure").Value = heure
                        .Cells("Volume").Value = ""
                        .Cells("Play").Value = TexteDeLaVideo(estEntree)
                        .Cells("Argument").Value = argument
                    End With
                Else
                    numero = grille.Rows.Add({True, nomJour, heure, TypeMinuterie, "", TexteDeLaVideo(estEntree), "", "", "999999"})
                    grille.Rows(numero).Cells("Argument").Value = argument
                End If
                ListeDesActionsDuType(numero, TypeMinuterie, texte)
                derniere = numero
                ajoutees += 1
            Next
        Next
        If derniere >= 0 Then AjouterUneLigne(2, derniere)           ' (recalcule l'ordre et trie le tableau)
        ' Les lignes inserees sont montrees : la premiere est selectionnee et visible dans le tableau.
        Try
            For Each ligne As DataGridViewRow In grille.Rows
                If Not ligne.IsNewRow AndAlso UCase(Convert.ToString(ligne.Cells("Type").Value)) = UCase(TypeMinuterie) AndAlso
                   Convert.ToString(ligne.Cells("Argument").Value).StartsWith("!/Date:") Then
                    grille.ClearSelection()
                    grille.CurrentCell = ligne.Cells("Action")
                    ligne.Selected = True
                    If ligne.Index < grille.FirstDisplayedScrollingRowIndex OrElse ligne.Index > grille.FirstDisplayedScrollingRowIndex + 8 Then grille.FirstDisplayedScrollingRowIndex = ligne.Index
                    Exit For
                End If
            Next
        Catch ex As Exception
        End Try
        DiagLog("Yom Tov : " & ajoutees & " annonce(s) inseree(s) dans la minuterie" & If(ignorees.Count > 0, ", " & ignorees.Count & " ignoree(s)", ""))
        Dim bilan As String = ajoutees & " annonce(s) insérée(s) dans la minuterie."
        If ignorees.Count > 0 Then bilan &= vbCrLf & vbCrLf & "Non insérées :" & vbCrLf & " - " & String.Join(vbCrLf & " - ", ignorees)
        Return bilan
    End Function

    ''' <summary>Insere deux lignes Yom Tov (entree, sortie) SANS fete ni date, a regler a la main (jour, heure) : leur annonce passe chaque semaine au jour
    ''' et a l'heure de la ligne (sans date, aucune verification de jour : voir Annoncer). Jour du jour, 18:00 pour l'entree et 20:00 pour la sortie
    ''' au depart. Si <paramref name="ligneLibre"/> est une ligne (-1 : aucune), la premiere annonce la remplit.</summary>
    Friend Function InsererLignesAReglerALaMain(avecEntree As Boolean, avecSortie As Boolean, ligneLibre As Integer) As String
        Dim grille As DataGridView = Form4.DataGridView1
        Dim nomJour As String = jourDeLaSemaine((CInt(Date.Today.DayOfWeek) + 6) Mod 7)
        Dim ajoutees As Integer = 0
        Dim derniere As Integer = -1
        For Each estEntree As Boolean In {True, False}
            If estEntree AndAlso Not avecEntree Then Continue For
            If Not estEntree AndAlso Not avecSortie Then Continue For
            Dim texte As String = If(estEntree, "Entrée de fête", "Sortie de fête")
            Dim heure As String = If(estEntree, "18:00", "20:00")
            Dim numero As Integer
            If ligneLibre >= 0 AndAlso ligneLibre < grille.Rows.Count AndAlso Not grille.Rows(ligneLibre).IsNewRow Then
                numero = ligneLibre
                ligneLibre = -1
                With grille.Rows(numero)
                    .Cells("Actif").Value = True
                    .Cells("Jour").Value = nomJour
                    .Cells("Heure").Value = heure
                    .Cells("Volume").Value = ""
                    .Cells("Play").Value = TexteDeLaVideo(estEntree)
                    .Cells("Argument").Value = ""
                End With
            Else
                numero = grille.Rows.Add({True, nomJour, heure, TypeMinuterie, "", TexteDeLaVideo(estEntree), "", "", "999999"})
            End If
            ListeDesActionsDuType(numero, TypeMinuterie, texte)
            derniere = numero
            ajoutees += 1
        Next
        If derniere >= 0 Then AjouterUneLigne(2, derniere)
        Try
            ' La premiere ligne inseree est selectionnee, pour la regler tout de suite.
            For Each ligne As DataGridViewRow In grille.Rows
                If Not ligne.IsNewRow AndAlso UCase(Convert.ToString(ligne.Cells("Type").Value)) = UCase(TypeMinuterie) AndAlso Convert.ToString(ligne.Cells("Argument").Value) = "" AndAlso
                   UCase(Convert.ToString(ligne.Cells("Action").Value)).EndsWith("DE FÊTE") Then
                    grille.ClearSelection()
                    grille.CurrentCell = ligne.Cells("Heure")
                    ligne.Selected = True
                    Exit For
                End If
            Next
        Catch ex As Exception
        End Try
        DiagLog("Yom Tov : " & ajoutees & " ligne(s) a regler a la main inseree(s) dans la minuterie")
        Return ajoutees & " ligne(s) insérée(s) dans la minuterie, à régler à la main : le jour et l'heure (le jour est celui d'aujourd'hui, " & nomJour.ToLower() & ", à 18:00 pour l'entrée et 20:00 pour la sortie)." & vbCrLf & vbCrLf &
            "Sans date, la ligne passe chaque semaine à ce jour et cette heure ; décochez-la ou supprimez-la après la fête."
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Execution (Module1.Minuterie) : la ligne ne joue que le jour de sa date.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Execute une ligne "Yom Tov" a l'heure de la minuterie : l'annonce n'est jouee que si la date de la ligne est celle d'aujourd'hui.</summary>
    Friend Sub Annoncer(action As String, argument As String)
        Try
            Dim date_ As String = CStr(Arg("Date", argument))
            ' Avec une date (fete inseree par la fenetre) : seulement ce jour-la. Sans date (ligne reglee a la main) : au jour et a l'heure de la ligne.
            If date_ <> "" AndAlso date_ <> Date.Today.ToString("yyyy-MM-dd") Then
                DiagLog("Yom Tov : ligne '" & action & "' (" & date_ & ") ignoree, ce n'est pas le jour")
                Exit Sub
            End If
            AnnonceShabbat(If(UCase(If(action, "")).StartsWith("ENTR"), 2, 3))
        Catch ex As Exception
            DiagLog("Yom Tov : annonce impossible : " & ex.Message)
        End Try
    End Sub

    ' ------------------------------------------------------------------------------------------
    ' A l'ouverture des reglages : proposer de choisir la fete qui arrive.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Les fetes des 7 prochains jours (ou en cours) qui ne sont ni dans la minuterie ni refusees.</summary>
    Friend Function Propositions() As List(Of Fete)
        Dim refusees As HashSet(Of String) = If(My.Settings.YomTov_Refusees, "").Split(";"c).Where(Function(s) s <> "").ToHashSet()
        Return Calculer(Date.Today, Date.Today.AddDays(7), VilleDesReglages()).
            Where(Function(f) f.Sortie >= Date.Today AndAlso Not refusees.Contains(f.Cle) AndAlso
                              Not (DejaDansLaMinuterie(f.Entree, True) OrElse Not f.EntreeAnnoncee) AndAlso
                              Not (DejaDansLaMinuterie(f.Sortie, False) OrElse Not f.SortieAnnoncee)).ToList()
    End Function

    ''' <summary>Si une fete arrive, propose de choisir les fetes a inserer dans la minuterie (celle qui arrive est deja cochee). Non : la fete
    ''' n'est plus proposee. Appele a l'ouverture des reglages (Form4).</summary>
    Friend Sub ProposerAuxReglages(proprietaire As IWin32Window)
        Try
            Dim aProposer As List(Of Fete) = Propositions()
            If aProposer.Count = 0 Then Exit Sub
            Dim f As Fete = aProposer(0)
            Dim texte As String = f.Nom & " : du " & f.Entree.AddDays(1).ToString("dddd d MMMM") & " au " & f.Sortie.ToString("dddd d MMMM") & "." & vbCrLf & vbCrLf &
                "Voulez-vous choisir les fêtes dont l'entrée et la sortie seront ajoutées à la minuterie ?"
            If MessageBox.Show(proprietaire, texte, "Fête juive : " & f.Nom, MessageBoxButtons.YesNo, MessageBoxIcon.Question) = DialogResult.Yes Then
                FormYomTov.Choisir(proprietaire, aProposer.Select(Function(x) x.Cle).ToList(), -1)
            Else
                For Each p As Fete In aProposer
                    My.Settings.YomTov_Refusees = If(My.Settings.YomTov_Refusees, "") & If(If(My.Settings.YomTov_Refusees, "") = "", "", ";") & p.Cle
                Next
            End If
        Catch ex As Exception
            DiagLog("Yom Tov : proposition impossible : " & ex.Message)
        End Try
    End Sub

End Module
