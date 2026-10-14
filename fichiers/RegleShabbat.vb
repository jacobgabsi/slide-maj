Option Strict On
Option Explicit On

Imports System.Text.RegularExpressions

''' <summary>
''' Heures de la minuterie liees a Chabbat (demande de Jacob, 28/10/2026) : dans la case Heure, au lieu d'une heure fixe, on ecrit
''' ShabbatPreDebut, ShabbatDebut ou ShabbatSortie, suivi si besoin de +n ou -n minutes ("ShabbatDebut-10", "ShabbatSortie+5").
'''
''' La case garde une VRAIE heure ("18:12") : tout le reste du programme (tri, impression, verification, declenchement) la lit comme une autre.
''' La regle est rangee dans les reglages de la tache ("!/HeureRegle:ShabbatDebut-10", colonne cachee Argument) et l'heure et le jour de la ligne sont
''' recalcules d'apres les horaires de la ville de l'onglet Shabbat : au chargement de la minuterie, chaque jour (Form3), au changement de ville.
''' </summary>
Friend Module RegleShabbat

    Friend Const Cle As String = "HeureRegle"

    ''' <summary>Les trois repères d'horaire, tels que Slide les connait (HH:mm ; vide si l'horaire est inconnu).</summary>
    Friend ReadOnly Property ShabbatPreDebut As String
        Get
            Return Convert.ToString(Shabbat.PreDebut)
        End Get
    End Property

    Friend ReadOnly Property ShabbatDebut As String
        Get
            Return Convert.ToString(Shabbat.Debut)
        End Get
    End Property

    Friend ReadOnly Property ShabbatSortie As String
        Get
            Return Convert.ToString(Shabbat.Fin)
        End Get
    End Property

    ' Shabbat / Sabbat / Chabbat + PreDebut / Debut / Sortie, ou Soleil + Lever / Coucher, puis +n ou -n minutes ; majuscules, espaces et accents sans importance.
    Private ReadOnly _motif As New Regex("^\s*(?:(?:sh?|ch)abbat\s*(?<nom>pr[eé]d[eé]but|d[eé]but|sortie)|soleil\s*(?<nom>lever|coucher)|ligne\s*(?<nom>avant|pr[eé]c[eé]dente|suivante|apr[eè]s|fin))\s*(?:(?<signe>[+\-])\s*(?<n>\d{1,4}))?(?:\s*(?<b1>max|min)\s*(?<h1>\d{1,2})\s*[:hH]\s*(?<m1>\d{2})?)?(?:\s*(?<b2>max|min)\s*(?<h2>\d{1,2})\s*[:hH]\s*(?<m2>\d{2})?)?(?:\s*#(?<id>[0-9a-f]{8}))?\s*$",
                                         RegexOptions.IgnoreCase Or RegexOptions.CultureInvariant)

    ''' <summary>Regle saisie dans la case Heure en attendant la fin de l'edition (Nothing : rien de saisi ; "" : heure normale, la regle est retiree).</summary>
    Friend ReglePendante As String = Nothing

    Private _dernierJour As Date = Date.MinValue

    ''' <summary>Le texte est-il une regle ? Rend alors sa forme normalisee ("ShabbatDebut-10").</summary>
    Friend Function Analyser(texte As String, ByRef regle As String) As Boolean
        regle = ""
        Dim nom As String = "", decalage As Integer = 0
        If Not Decomposer(texte, nom, decalage) Then Return False
        Dim plusTard As Integer = -1, plusTot As Integer = -1
        Bornes(texte, plusTard, plusTot)
        regle = Construire(nom, decalage, plusTard, plusTot)
        Return True
    End Function

    ''' <summary>La regle normalisee d'un repere (ShabbatPreDebut, ShabbatDebut, ShabbatSortie, SoleilLever, SoleilCoucher) et d'un decalage en minutes.</summary>
    Friend Function Construire(nom As String, decalage As Integer, Optional plusTard As Integer = -1, Optional plusTot As Integer = -1, Optional idVoisine As String = "") As String
        Dim texte As String = nom & If(decalage = 0, "", If(decalage > 0, "+", "-") & Math.Abs(decalage).ToString())
        If plusTard >= 0 Then texte &= " max " & String.Format("{0:00}:{1:00}", plusTard \ 60, plusTard Mod 60)
        If plusTot >= 0 Then texte &= " min " & String.Format("{0:00}:{1:00}", plusTot \ 60, plusTot Mod 60)
        If idVoisine <> "" Then texte &= "#" & idVoisine
        Return texte
    End Function

    ''' <summary>L'identifiant de la tache voisine retenue par une regle « ligne d'avant / suivante » ("" si la regle n'en a pas).</summary>
    Friend Function IdVoisine(regle As String) As String
        Dim m As Match = _motif.Match(If(regle, ""))
        Return If(m.Success AndAlso m.Groups("id").Success, m.Groups("id").Value.ToLowerInvariant(), "")
    End Function

    Private Function IdDeLaLigne(argument As Object) As String
        Dim m As Match = Regex.Match(Convert.ToString(argument), "!/Id:([0-9a-f]{8})", RegexOptions.IgnoreCase)
        Return If(m.Success, m.Groups(1).Value.ToLowerInvariant(), "")
    End Function

    ''' <summary>Retient la tache voisine (celle qui est AU-DESSUS ou EN DESSOUS de la ligne en ce moment) : un identifiant lui est donne, et la regle le garde.
    ''' La ligne suit ainsi CETTE tache, meme si elle change de place apres le tri (« 5 minutes avant la tache d'apres » la fait passer avant, et ce n'est plus la voisine).</summary>
    Private Function LierALaVoisine(grille As DataGridView, r As Integer, regle As String) As String
        Dim nom As String = "", decalage As Integer = 0
        If Not Decomposer(regle, nom, decalage) OrElse Not EstLigne(nom) OrElse IdVoisine(regle) <> "" Then Return regle
        Dim s As String = If(grille.Columns.Contains("Argument2"), "2", "")
        Dim i As Integer = r + If(nom = "LigneAvant", -1, 1)
        If i < 0 OrElse i >= grille.Rows.Count OrElse grille.Rows(i).IsNewRow Then Return regle
        Dim cellule As DataGridViewCell = grille.Rows(i).Cells("Argument" & s)
        Dim id As String = IdDeLaLigne(cellule.Value)
        If id = "" Then
            id = Guid.NewGuid().ToString("N").Substring(0, 8)
            cellule.Value = Convert.ToString(cellule.Value) & "!/Id:" & id
        End If
        Dim plusTard As Integer = -1, plusTot As Integer = -1
        Bornes(regle, plusTard, plusTot)
        Return Construire(nom, decalage, plusTard, plusTot, id)
    End Function

    ''' <summary>Les bornes d'une regle, en minutes depuis minuit : <paramref name="plusTard"/> (« max 18:00 » : l'heure calculee ne depasse pas) et <paramref name="plusTot"/> (« min 17:00 » : pas avant). -1 : pas de borne.</summary>
    Friend Function Bornes(regle As String, ByRef plusTard As Integer, ByRef plusTot As Integer) As Boolean
        plusTard = -1 : plusTot = -1
        Dim m As Match = _motif.Match(If(regle, ""))
        If Not m.Success Then Return False
        For Each i As String In {"1", "2"}
            If Not m.Groups("b" & i).Success Then Continue For
            Dim h As Integer = Integer.Parse(m.Groups("h" & i).Value)
            Dim mn As Integer = If(m.Groups("m" & i).Success, Integer.Parse(m.Groups("m" & i).Value), 0)
            If h > 23 OrElse mn > 59 Then Continue For
            If String.Equals(m.Groups("b" & i).Value, "max", StringComparison.OrdinalIgnoreCase) Then plusTard = h * 60 + mn Else plusTot = h * 60 + mn
        Next
        Return True
    End Function

    ''' <summary>Le repere (nom normalise) et le decalage d'une regle.</summary>
    Friend Function Decomposer(regle As String, ByRef nom As String, ByRef decalage As Integer) As Boolean
        Dim m As Match = _motif.Match(If(regle, ""))
        If Not m.Success Then Return False
        Dim brut As String = m.Groups("nom").Value
        nom = NomNormalise(brut, m.Value.TrimStart().StartsWith("so", StringComparison.OrdinalIgnoreCase))
        decalage = 0
        If m.Groups("n").Success Then decalage = Integer.Parse(m.Groups("n").Value) * If(m.Groups("signe").Value = "-", -1, 1)
        Return True
    End Function

    ''' <summary>Vrai pour un repere du soleil (lever, coucher), valable tous les jours ; faux pour un repere de Chabbat (vendredi, samedi).</summary>
    Friend Function EstSoleil(nom As String) As Boolean
        Return nom.StartsWith("Soleil", StringComparison.Ordinal)
    End Function

    ''' <summary>Vrai pour un repere « ligne voisine » (LigneAvant, LigneApres) : l'heure de la tache d'avant / d'apres dans la minuterie (18/11/2026), plus ou moins n minutes.</summary>
    Friend Function EstLigne(nom As String) As Boolean
        Return nom.StartsWith("Ligne", StringComparison.Ordinal)
    End Function

    ' Ligne dont on calcule l'heure (grille + index) : necessaire aux reperes « ligne voisine ». Posee par ceux qui calculent sur une ligne de la minuterie.
    Friend ContexteGrille As DataGridView = Nothing
    Friend ContexteLigne As Integer = -1

    ''' <summary>Heure (minutes depuis minuit) de la tache voisine (<paramref name="sens"/> -1 : la ligne d'avant, +1 : la suivante) dans la grille de contexte ; Nothing si elle n'existe pas ou n'a pas d'heure.</summary>
    Private Function HeureVoisine(nom As String, id As String) As Integer?
        Dim g As DataGridView = ContexteGrille
        If g Is Nothing OrElse ContexteLigne < 0 Then Return Nothing
        Dim s As String = If(g.Columns.Contains("Argument2"), "2", "")
        Dim ligne As DataGridViewRow = Nothing
        ' Toujours la voisine DU MOMENT (au-dessus / en dessous dans la minuterie) : la ligne suit ses voisines, sans aucun repere pose sur elles.
        Dim i As Integer = ContexteLigne + If(nom = "LigneApres", 1, -1)
        If i >= 0 AndAlso i < g.Rows.Count AndAlso Not g.Rows(i).IsNewRow Then ligne = g.Rows(i)
        If ligne Is Nothing Then Return Nothing
        Dim texte As String = Left(Convert.ToString(ligne.Cells("Heure" & s).FormattedValue), 5)
        If Not IsDate(texte) Then Return Nothing
        Dim d As Date = CDate(texte)
        Return d.Hour * 60 + d.Minute
    End Function

    ''' <summary>Heure d'une regle « ligne voisine » : l'heure de la tache d'avant / d'apres + decalage, sans jamais depasser l'autre voisine (le rang de la ligne dans la minuterie ne change donc pas).</summary>
    Private Function CalculerVoisine(nom As String, decalage As Integer, id As String, ByRef minutes As Integer, ByRef jourBase As Integer) As Boolean
        Dim base As Integer? = HeureVoisine(nom, id)
        If Not base.HasValue Then Return False
        If nom = "LigneFin" Then
            ' Juste apres la ligne du dessus : son heure + sa duree (1 minute si inconnue, comme le menu « Heure suivante »), + le decalage ; le jour suit apres minuit.
            Dim g As DataGridView = ContexteGrille
            Dim dessus As DataGridViewRow = g.Rows(ContexteLigne - 1)
            Dim connue As Boolean = False
            Dim duree As Integer = Form4.DureeDeLaLigne(dessus, connue)
            If Not connue OrElse duree <= 0 Then duree = 1
            minutes = base.Value + duree + decalage
            Dim s As String = If(g.Columns.Contains("Argument2"), "2", "")
            Dim jourDessus As String = Convert.ToString(dessus.Cells("Jour" & s).Value)
            Dim indice As Integer = Array.FindIndex(jourDeLaSemaine, Function(x) String.Equals(x, jourDessus, StringComparison.OrdinalIgnoreCase))
            jourBase = If(indice >= 0 AndAlso indice <= 6, indice, -1)
            Return True
        End If
        minutes = Math.Max(0, Math.Min(1439, base.Value + decalage))
        Return True
    End Function

    ''' <summary>Cle de tri de la minuterie : numero du jour (0 lundi... 6 dimanche, 7 Lun-Ven, 8 Sam-Dim, 9 TLJ, 99 SHUTDOWN) puis HHmm.</summary>
    Friend Function TriDe(jour As String, heure As String) As String
        Return JoursPerso.CodeTri(jour, heure)
    End Function

    ''' <summary>La ligne redevient une heure fixe (la regle eventuelle est retiree) ; le jour ne change pas, la cle de tri suit.</summary>
    Friend Sub PoserHeureFixe(grille As DataGridView, r As Integer, heure As Date)
        Dim s As String = If(grille.Columns.Contains("Argument2"), "2", "")
        PoserSurLigne(grille, r, "")
        Dim ligne As DataGridViewRow = grille.Rows(r)
        Dim h As String = heure.ToString("HH:mm")
        ligne.Cells("Heure" & s).Value = h
        ligne.Cells("tri" & s).Value = TriDe(Convert.ToString(ligne.Cells("Jour" & s).Value), h)
    End Sub

    Private Function NomNormalise(nom As String, soleil As Boolean) As String
        Dim n As String = nom.ToLowerInvariant().Replace("é", "e").Replace("è", "e")
        If n = "fin" Then Return "LigneFin"          ' « juste apres la ligne du dessus » : a la fin de celle-ci (son heure + sa duree)
        If n.StartsWith("avant") OrElse n.StartsWith("prece") Then Return "LigneAvant"
        If n.StartsWith("suivante") OrElse n.StartsWith("apres") Then Return "LigneApres"
        If soleil Then Return If(n.StartsWith("lever"), "SoleilLever", "SoleilCoucher")
        If n.StartsWith("pre") Then Return "ShabbatPreDebut"
        If n.StartsWith("sortie") Then Return "ShabbatSortie"
        Return "ShabbatDebut"
    End Function

    Private Function VilleChoisie() As String
        Dim ville As String = Convert.ToString(Shabbat.Ville)
        If ville = "" Then ville = Convert.ToString(My.Settings.Shabbat_Ville)
        ' Reglages ouverts : la ville choisie dans l'onglet Shabbat compte tout de suite, sans attendre le OK (04/11/2026 : Paris -> Cannes, l'heure du coucher restait celle de Paris).
        Try
            If Form4.Visible AndAlso Form4.ComboBox1.Text <> "" Then ville = Form4.ComboBox1.Text
        Catch ex As Exception
        End Try
        Return ville
    End Function

    ''' <summary>Minutes depuis minuit (hors bornes admis : le decalage peut passer minuit) et jour de depart (index dans jourDeLaSemaine : 4 vendredi, 5 samedi pour Chabbat ;
    ''' pour le soleil, le jour de la ligne <paramref name="jourLigne"/> si c'est un jour de la semaine, sinon -1 : horaire d'aujourd'hui).
    ''' Faux si l'horaire est inconnu.</summary>
    Friend Function Calculer(regle As String, ByRef minutes As Integer, ByRef jourBase As Integer, Optional jourLigne As String = "") As Boolean
        Dim nom As String = "", decalage As Integer = 0
        If Not Decomposer(regle, nom, decalage) Then Return False
        If EstLigne(nom) Then
            jourBase = -1                                  ' (le jour de la ligne ne change pas)
            If Not CalculerVoisine(nom, decalage, IdVoisine(regle), minutes, jourBase) Then Return False
            Dim tard As Integer = -1, tot As Integer = -1
            Bornes(regle, tard, tot)
            If tard >= 0 AndAlso minutes > tard Then minutes = tard
            If tot >= 0 AndAlso minutes < tot Then minutes = tot
            Return True
        End If
        Dim horaire As String
        If EstSoleil(nom) Then
            ' Lever / coucher du soleil : le prochain jour de ce nom (aujourd'hui compris), ou aujourd'hui pour une ligne de tous les jours (TLJ, Lun-Ven...).
            Dim indice As Integer = Array.FindIndex(jourDeLaSemaine, Function(x) String.Equals(x, jourLigne, StringComparison.OrdinalIgnoreCase))
            Dim dateDuJour As Date = Date.Today
            If indice >= 0 AndAlso indice <= 6 Then
                Dim aujourdhui As Integer = (CInt(Date.Today.DayOfWeek) + 6) Mod 7          ' (0 = lundi)
                dateDuJour = Date.Today.AddDays((indice - aujourdhui + 7) Mod 7)
                jourBase = indice
            Else
                jourBase = -1
            End If
            horaire = HeureDuSoleil(VilleChoisie(), nom = "SoleilLever", dateDuJour)
        Else
            If Convert.ToString(Shabbat.Debut) = "" Then AssurerLesHoraires()
            Select Case nom
                Case "ShabbatPreDebut" : horaire = ShabbatPreDebut : jourBase = 4
                Case "ShabbatSortie" : horaire = ShabbatSortie : jourBase = 5
                Case Else : horaire = ShabbatDebut : jourBase = 4
            End Select
        End If
        If Not IsDate(horaire) Then Return False
        Dim d As Date = CDate(horaire)
        minutes = d.Hour * 60 + d.Minute + decalage
        ' Bornes (« SoleilCoucher max 18:00 ») : le coucher du soleil, mais pas apres 18:00 ; « min » : pas avant.
        Dim plusTard As Integer = -1, plusTot As Integer = -1
        Bornes(regle, plusTard, plusTot)
        If plusTard >= 0 AndAlso minutes > plusTard Then minutes = plusTard
        If plusTot >= 0 AndAlso minutes < plusTot Then minutes = plusTot
        Return True
    End Function

    ''' <summary>L'heure du jour ("HH:mm", sans decalage) d'un repere, pour la liste de la fenetre « Heure de la tâche » ; "" si inconnue.</summary>
    Friend Function HoraireDuJour(nom As String, jourLigne As String) As String
        Dim minutes As Integer, jourBase As Integer
        If Not Calculer(nom, minutes, jourBase, jourLigne) Then Return ""
        minutes = ((minutes Mod 1440) + 1440) Mod 1440
        Return String.Format("{0:00}:{1:00}", minutes \ 60, minutes Mod 60)
    End Function

    ''' <summary>Jour, heure ("HH:mm") et cle de tri de la regle (passage de minuit : le jour change). <paramref name="jour"/> : le jour de la ligne en entree
    ''' (utile au soleil, qui garde le jour choisi) ; pour Chabbat c'est vendredi ou samedi qui est rendu.</summary>
    Friend Function Resoudre(regle As String, ByRef jour As String, ByRef heure As String, ByRef tri As String) As Boolean
        Dim minutes As Integer, jourBase As Integer
        If Not Calculer(regle, minutes, jourBase, jour) Then Return False
        Dim jours As Integer = CInt(Math.Floor(minutes / 1440.0))
        Dim m As Integer = minutes - jours * 1440
        If jourBase >= 0 Then jour = jourDeLaSemaine(((jourBase + jours) Mod 7 + 7) Mod 7)
        heure = String.Format("{0:00}:{1:00}", m \ 60, m Mod 60)
        tri = TriDe(jour, heure)
        Return True
    End Function

    ''' <summary>Horaires de la ville de l'onglet Shabbat (calcul local, gardes pour la journee).</summary>
    Friend Sub AssurerLesHoraires()
        Try
            Dim ville As String = Convert.ToString(Shabbat.Ville)
            If ville = "" Then ville = Convert.ToString(My.Settings.Shabbat_Ville)
            If ville <> "" Then HeureDeShabbat(ville)
        Catch ex As Exception
            DiagLog("Regle Chabbat : horaires impossibles (" & ex.Message & ")")
        End Try
    End Sub

    ''' <summary>La regle rangee dans les reglages d'une tache ("" : aucune).</summary>
    Friend Function LireRegle(argument As Object) As String
        Dim a As String = TryCast(argument, String)
        If a Is Nothing OrElse a.IndexOf("!/" & Cle & ":", StringComparison.OrdinalIgnoreCase) < 0 Then Return ""
        Return Regex.Replace(Convert.ToString(Arg(Cle, a)).Trim(), "#[0-9a-f]{8}$", "", RegexOptions.IgnoreCase)          ' (« #identifiant » : ancien repere de la 9.822, ignore)
    End Function

    ''' <summary>Les reglages d'une tache avec cette regle (vide : la regle est retiree).</summary>
    Friend Function ArgumentAvecRegle(argument As String, regle As String) As String
        Dim a As String = Regex.Replace(If(argument, ""), "!/" & Cle & ":[^!]*", "", RegexOptions.IgnoreCase)
        If regle <> "" Then a &= "!/" & Cle & ":" & regle
        Return a
    End Function

    ''' <summary>Heure tapee a la main : 18:12, 8:05, 18h12, 1812, 18.</summary>
    Friend Function LireHeureSaisie(texte As String) As Date?
        Dim m As Match = Regex.Match(If(texte, "").Trim(), "^(\d{1,2})\s*(?:[:hH.]\s*(\d{2})?)?$")
        If Not m.Success Then m = Regex.Match(If(texte, "").Trim(), "^(\d{2})(\d{2})$")
        If Not m.Success Then Return Nothing
        Dim h As Integer = Integer.Parse(m.Groups(1).Value)
        Dim mn As Integer = If(m.Groups(2).Success, Integer.Parse(m.Groups(2).Value), 0)
        If h > 23 OrElse mn > 59 Then Return Nothing
        Return Date.Today.AddHours(h).AddMinutes(mn)
    End Function

    ''' <summary>Pose la regle sur une ligne de la minuterie (grille des taches actives ou inactives) : reglages, jour, heure et tri. Faux si l'horaire est inconnu.</summary>
    Friend Function PoserSurLigne(grille As DataGridView, r As Integer, regle As String) As Boolean
        Dim s As String = If(grille.Columns.Contains("Argument2"), "2", "")
        Dim ligne As DataGridViewRow = grille.Rows(r)
        ' La regle retenue sur la case Heure (Tag) est retiree AVANT les reglages : sinon la surveillance de Form4 la remettrait (voir SurveillerLesReglesChabbat).
        Dim ancienneRegle As String = LireRegle(ligne.Cells("Argument" & s).Value)
        If regle = "" Then Marquer(ligne.Cells("Heure" & s), "")
        Dim ancien As String = Convert.ToString(ligne.Cells("Argument" & s).Value)
        Dim nouveau As String = ArgumentAvecRegle(ancien, regle)
        If nouveau <> ancien Then ligne.Cells("Argument" & s).Value = nouveau
        If regle = "" Then Return True          ' (l'avertissement « elle deviendra fixe » est donne AVANT, des le debut de la modification : voir Form4.EditeurHeureTexte)
        Dim jour As String = Convert.ToString(ligne.Cells("Jour" & s).Value), heure As String = "", tri As String = ""
        ContexteGrille = grille : ContexteLigne = r
        Dim resolue As Boolean
        Try
            resolue = Resoudre(regle, jour, heure, tri)
        Finally
            ContexteGrille = Nothing : ContexteLigne = -1
        End Try
        If Not resolue Then Return False
        ligne.Cells("Jour" & s).Value = jour
        ligne.Cells("Heure" & s).Value = heure
        ligne.Cells("tri" & s).Value = tri
        Marquer(ligne.Cells("Heure" & s), regle)
        Return True
    End Function

    Private _policeRegle As Font

    ''' <summary>Montre qu'une heure est liee a Chabbat : fond bleu-vert, texte blanc en gras, info-bulle ; la regle est retenue dans le Tag de la case (voir la surveillance de Form4).
    ''' Le style est pose sur la case elle-meme : la colonne Heure ne declenche jamais CellFormatting (CalendarCell.GetFormattedValue rend l'heure sans passer par la base).
    ''' Regle vide : la case reprend l'allure des autres.</summary>
    Friend Sub Marquer(cellule As DataGridViewCell, regle As String)
        If regle = "" Then
            cellule.Style.BackColor = Color.Empty
            cellule.Style.ForeColor = Color.Empty
            cellule.Style.SelectionBackColor = Color.Empty
            cellule.Style.SelectionForeColor = Color.Empty
            cellule.Style.Font = Nothing
            cellule.ToolTipText = ""
            cellule.Tag = Nothing
            Exit Sub
        End If
        If _policeRegle Is Nothing AndAlso cellule.DataGridView IsNot Nothing Then _policeRegle = New Font(cellule.DataGridView.Font, FontStyle.Bold)
        cellule.Style.BackColor = Color.FromArgb(0, 105, 120)
        cellule.Style.ForeColor = Color.White
        cellule.Style.SelectionBackColor = Color.FromArgb(0, 150, 170)
        cellule.Style.SelectionForeColor = Color.White
        If _policeRegle IsNot Nothing Then cellule.Style.Font = _policeRegle
        cellule.ToolTipText = InfoBulle(regle)
        cellule.Tag = regle
    End Sub

    Private Function InfoBulle(regle As String) As String
        Return "Horaire calculé : " & regle & " (recalculé automatiquement)."
    End Function

    ''' <summary>Recalcule l'heure et le jour de toutes les lignes qui ont une regle (taches actives et inactives). Vrai si une ligne a change.</summary>
    Friend Function ActualiserLaGrille() As Boolean
        Dim change As Boolean = False
        AssurerLesHoraires()
        For Each grille As DataGridView In {Form4.DataGridView1, Form4.DataGridViewTuya, Form4.DataGridView2}
            Dim s As String = If(grille.Columns.Contains("Argument2"), "2", "")
            ' Trois passes : une ligne « d'apres la ligne voisine » peut dependre d'une ligne elle-meme calculee (chaine) ; une boucle s'arrete d'elle-meme (aucun changement).
            For passe As Integer = 1 To 3
                Dim changePasse As Boolean = False
                For Each ligne As DataGridViewRow In grille.Rows
                    If ligne.IsNewRow Then Continue For
                    ' Nettoyage : les reperes « !/Id: » poses sur les taches voisines par la 9.822 sont retires (la ligne suit maintenant ses voisines du moment, sans repere).
                    Dim argBrut As String = Convert.ToString(ligne.Cells("Argument" & s).Value)
                    If argBrut.IndexOf("!/Id:", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        ligne.Cells("Argument" & s).Value = Regex.Replace(argBrut, "!/Id:[0-9a-f]{8}", "", RegexOptions.IgnoreCase)
                    End If
                    Dim regle As String = LireRegle(ligne.Cells("Argument" & s).Value)
                    If regle = "" Then Continue For
                    Dim jour As String = Convert.ToString(ligne.Cells("Jour" & s).Value), heure As String = "", tri As String = ""
                    ContexteGrille = grille : ContexteLigne = ligne.Index
                    Dim resolue As Boolean
                    Try
                        resolue = Resoudre(regle, jour, heure, tri)
                    Finally
                        ContexteGrille = Nothing : ContexteLigne = -1
                    End Try
                    If Not resolue Then Continue For
                    Marquer(ligne.Cells("Heure" & s), regle)
                    Dim jourActuel As String = Convert.ToString(ligne.Cells("Jour" & s).Value)
                    Dim heureActuelle As String = Left(Convert.ToString(ligne.Cells("Heure" & s).FormattedValue), 5)
                    If jourActuel <> jour OrElse heureActuelle <> heure Then
                        DiagLog("Regle Chabbat : " & regle & " -> " & jour & " " & heure & " (avant : " & jourActuel & " " & heureActuelle & ")")
                        ligne.Cells("Jour" & s).Value = jour
                        ligne.Cells("Heure" & s).Value = heure
                        ligne.Cells("tri" & s).Value = tri
                        change = True : changePasse = True
                    End If
                Next
                If Not changePasse Then Exit For
            Next
        Next
        Return change
    End Function

    ''' <summary>Apres le chargement de la minuterie : les heures sont recalculees ; si l'une a change, la minuterie en cours est reconstruite avec les nouvelles.</summary>
    Friend Sub ActualiserEtEnregistrer()
        If Not ActualiserLaGrille() Then Exit Sub
        Form4.DataGridView1.Sort(Form4.DataGridView1.Columns("tri"), System.ComponentModel.ListSortDirection.Ascending)
        Form4.DataGridViewTuya.Sort(Form4.DataGridViewTuya.Columns("tri"), System.ComponentModel.ListSortDirection.Ascending)
        Form4.DataGridView2.Sort(Form4.DataGridView2.Columns("tri2"), System.ComponentModel.ListSortDirection.Ascending)
        UpdateData()
    End Sub

    ''' <summary>Appelee par la veille de Form3 : une seule fois par jour (Slide peut rester ouvert des semaines). Reportee si les reglages sont ouverts.</summary>
    Friend Sub ActualiserSiNouveauJour()
        If _dernierJour = Date.Today Then Exit Sub
        Try
            If Form4.Visible Then Exit Sub
            _dernierJour = Date.Today
            ActualiserEtEnregistrer()
        Catch ex As Exception
            DiagLog("Regle Chabbat : mise a jour quotidienne impossible (" & ex.Message & ")")
        End Try
    End Sub

End Module
