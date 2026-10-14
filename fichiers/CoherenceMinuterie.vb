Option Strict On
Option Explicit On

''' <summary>
''' Verification de la coherence des taches de la minuterie, faite quand on valide les reglages
''' par OK (demande de Jacob, 06/10 : "si j'ai 2 PIP au meme endroit, dis-le moi pour que je
''' puisse modifier"). Le 06/10, deux films en PIP au BOOT dans le meme coin : le second
''' remplacait le premier a chaque lancement, qui en etait rallonge de plusieurs secondes.
'''
''' Ce qui est signale (taches ACTIVES seulement) :
'''   - deux PIP dans le MEME coin au meme moment : le second remplace le premier ;
'''   - deux programmes en plein ecran au meme moment : un seul sera joue ;
'''   - deux chaines TV differentes au meme moment : la cle TNT ne capte qu'une chaine a la fois ;
'''   - deux lignes identiques (la meme tache programmee deux fois) ;
'''   - un film ou une chaine sans rien de choisi (colonne Action vide) ;
'''   - un programme en plein ecran avec un volume a 0 (normal pour un PIP, pas en plein ecran).
'''   - un film dont le fichier n'existe pas (voir FilmsIntrouvables, ajoute le 08/10/2026) ;
''' "Au meme moment" : au lancement (BOOT) toutes les deux, ou a la meme heure un jour commun
''' (Lun-Ven, Sam-Dim et TLJ comptent pour chacun de leurs jours).
'''   - a des heures differentes : un programme qui demarre avant que le precedent soit fini,
'''     d'apres sa colonne Durée (voir Chevauchement), passage de minuit compris.
'''
''' Rien n'est modifie ici : la liste des problemes est rendue a Form4, qui la montre et laisse
''' le choix de corriger ou d'enregistrer quand meme.
''' </summary>
Public Module CoherenceMinuterie

    ''' <summary>Une tache du tableau, telle qu'elle est affichee.</summary>
    Public Class LigneTache
        ''' <summary>Numero de la ligne dans le tableau (1 = la premiere).</summary>
        Public Numero As Integer
        Public Jour As String = ""
        Public Heure As String = ""
        Public Type As String = ""
        Public Action As String = ""
        Public Play As String = ""
        ''' <summary>Colonne Durée : "01:52:19", "01:30" ou "0J2h44m" ; vide = duree inconnue.</summary>
        Public Duree As String = ""
        Public Volume As String = ""
        ''' <summary>Colonne Argument (cachee) : "!/Pip:True!/PipSize:0.28!/PipPosition:Bdroite...".</summary>
        Public Reglages As String = ""
    End Class

    Public Class Incoherence
        ''' <summary>Numeros des lignes concernees (vide pour un reglage hors du tableau).</summary>
        Public Lignes As Integer() = {}
        ''' <summary>Pour un reglage hors du tableau : son nom dans la pastille ("Sources", "Mode RDV").</summary>
        Public Rubrique As String = ""
        ''' <summary>Le probleme en quelques mots ("Deux PIP au même endroit").</summary>
        Public Titre As String = ""
        ''' <summary>Quand, et ce qui va se passer.</summary>
        Public Detail As String = ""
        ''' <summary>Vrai : il faut corriger, on ne peut pas enregistrer ainsi. Faux : simple
        ''' avertissement, "Enregistrer quand meme" reste possible.</summary>
        Public Bloquant As Boolean
        ''' <summary>Pour un reglage hors du tableau : le controle de Form4 ou aller le corriger.</summary>
        Public Cible As Object

        ''' <summary>"Ligne 3", "Lignes 1 et 2", ou le nom du reglage.</summary>
        Public ReadOnly Property Etiquette As String
            Get
                If Lignes Is Nothing OrElse Lignes.Length = 0 Then Return If(Rubrique, "")
                If Lignes.Length = 1 Then Return "Ligne " & Lignes(0)
                Return "Lignes " & String.Join(" et ", Lignes)
            End Get
        End Property

        ''' <summary>Tout en une phrase (journal, essais).</summary>
        Public ReadOnly Property Texte As String
            Get
                Return Etiquette & " : " & Titre & ". " & Detail
            End Get
        End Property
    End Class

    Private Function Probleme(titre As String, detail As String, ParamArray lignes As Integer()) As Incoherence
        Return New Incoherence With {.Lignes = lignes, .Titre = titre, .Detail = detail}
    End Function

    ''' <summary>"Vendredi 16:00" avec une majuscule, pour commencer une phrase.</summary>
    Private Function Majuscule(s As String) As String
        Return If(String.IsNullOrEmpty(s), "", Char.ToUpper(s(0)) & s.Substring(1))
    End Function

    Private ReadOnly Jours As String() = {"Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche"}
    Private ReadOnly TypesAffiches As String() = {"FILM", "TV", "SAT", "IPTV"}

    ''' <summary>Limites venues des reglages (onglet Audio) ; 0 ou "" = pas de controle.</summary>
    Public Class Seuils
        ''' <summary>"Volume TV par defaut" : une chaine TV plus forte est signalee.</summary>
        Public VolumeTv As Integer
        ''' <summary>"Volume Dreambox par defaut" : un film du recepteur satellite plus fort est signale.</summary>
        Public VolumeRecepteur As Integer
        ''' <summary>Debut du chemin des films du recepteur satellite ("\\VUDUO2\").</summary>
        Public Recepteur As String = ""
    End Class

    ''' <summary>Taille des colonnes Action et Argument dans la base : au-dela, l'enregistrement echoue.</summary>
    Public Const LongueurMaxi As Integer = 255

    Public Function Verifier(lignes As List(Of LigneTache), Optional seuils As Seuils = Nothing) As List(Of Incoherence)
        Dim problemes As New List(Of Incoherence)
        If lignes Is Nothing Then Return problemes
        If seuils Is Nothing Then seuils = New Seuils()

        ' Une tache a elle seule
        For Each l As LigneTache In lignes
            Dim film As Boolean = UCase(Propre(l.Type)) = "FILM"
            Dim quandQuoi As String = Majuscule(Moment(l, l)) & ", " & Propre(l.Type) & ". "

            ' Controles qui etaient faits un par un par le bouton OK, chacun dans sa boite de
            ' message (demande de Jacob, 06/10 : tout dans la meme fenetre).
            If Propre(l.Action).Length > LongueurMaxi Then
                Dim p As Incoherence = Probleme("Texte de la colonne Action trop long",
                                                quandQuoi & Propre(l.Action).Length & " caractères pour " & LongueurMaxi & " au maximum : la tâche ne peut pas être enregistrée.", l.Numero)
                p.Bloquant = True
                problemes.Add(p)
            End If
            Dim maxiReglages As Integer = If(BaseMinuterie.ReglagesLongsPossibles, BaseMinuterie.LongueurMaxiLongue, LongueurMaxi)
            If Propre(l.Reglages).Length > maxiReglages Then
                Dim p As Incoherence = Probleme("Réglages de la tâche trop longs",
                                                quandQuoi & Propre(l.Reglages).Length & " caractères pour " & maxiReglages & " au maximum (titre trop long ?) : la tâche ne peut pas être enregistrée.", l.Numero)
                p.Bloquant = True
                problemes.Add(p)
            End If
            Dim volume As Integer
            If Integer.TryParse(Propre(l.Volume), volume) Then
                If UCase(Propre(l.Type)) = "TV" AndAlso seuils.VolumeTv > 0 AndAlso volume > seuils.VolumeTv Then
                    problemes.Add(Probleme("Volume TV supérieur au volume par défaut",
                                           quandQuoi & "Volume " & volume & ", pour un volume TV par défaut de " & seuils.VolumeTv & ".", l.Numero))
                ElseIf film AndAlso seuils.Recepteur <> "" AndAlso seuils.VolumeRecepteur > 0 AndAlso volume > seuils.VolumeRecepteur AndAlso
                       Propre(l.Action).IndexOf(seuils.Recepteur, StringComparison.OrdinalIgnoreCase) >= 0 Then
                    problemes.Add(Probleme("Volume supérieur au volume par défaut du récepteur satellite",
                                           quandQuoi & "Volume " & volume & ", pour un volume par défaut de " & seuils.VolumeRecepteur & " (" & seuils.Recepteur & ").", l.Numero))
                End If
            End If

            ' Colonne Action vide, quel que soit le type : le bouton OK supprime la ligne.
            If Propre(l.Action) = "" Then
                problemes.Add(Probleme(If(film, "Aucun film choisi", If(EstAffichage(l), "Aucune chaîne choisie", "Ligne incomplète")),
                                       quandQuoi & "La colonne Action est vide : la ligne sera supprimée à l'enregistrement.",
                                       l.Numero))
                Continue For
            End If
            If Not EstAffichage(l) Then Continue For
            If Not EstPip(l) AndAlso VolumeNul(l) Then
                ' Plein ecran sans le son (demande de Jacob, 06/10). Un volume a 0 est normal
                ' pour un PIP (image seule, la musique continue) : il n'est pas signale.
                problemes.Add(Probleme("Volume à 0 en plein écran",
                                       Majuscule(Moment(l, l)) & ", " & Propre(l.Type) & ". " &
                                       If(film, "Ce film", "Cette chaîne") & " sera sans le son.",
                                       l.Numero))
            End If
        Next

        ' Deux taches entre elles
        For i As Integer = 0 To lignes.Count - 2
            For j As Integer = i + 1 To lignes.Count - 1
                Dim a As LigneTache = lignes(i), b As LigneTache = lignes(j)
                If Not MemeMoment(a, b) Then
                    ' Heures differentes : l'un demarre-t-il avant que l'autre soit fini ?
                    Dim recouvrement As String = Chevauchement(a, b)
                    If recouvrement = "" Then recouvrement = Chevauchement(b, a)
                    If recouvrement <> "" Then problemes.Add(Probleme("Horaires qui se chevauchent", recouvrement, a.Numero, b.Numero))
                    Continue For
                End If
                Dim quand As String = Majuscule(Moment(a, b))
                If Identiques(a, b) Then
                    problemes.Add(Probleme("Lignes identiques", quand & ". La même tâche est programmée deux fois.", a.Numero, b.Numero))
                ElseIf EstAffichage(a) AndAlso EstAffichage(b) AndAlso Propre(a.Action) <> "" AndAlso Propre(b.Action) <> "" Then
                    Dim pipA As Boolean = EstPip(a), pipB As Boolean = EstPip(b)
                    If pipA AndAlso pipB AndAlso String.Equals(Coin(a), Coin(b), StringComparison.OrdinalIgnoreCase) Then
                        problemes.Add(Probleme("Deux PIP au même endroit",
                                               quand & ", " & NomDuCoin(Coin(a)) & ". Le second remplacera le premier.", a.Numero, b.Numero))
                    ElseIf Not pipA AndAlso Not pipB Then
                        problemes.Add(Probleme("Deux programmes en plein écran au même moment",
                                               quand & ". Un seul sera joué.", a.Numero, b.Numero))
                    ElseIf UCase(Propre(a.Type)) = "TV" AndAlso UCase(Propre(b.Type)) = "TV" AndAlso
                           Not String.Equals(Propre(a.Action), Propre(b.Action), StringComparison.OrdinalIgnoreCase) Then
                        problemes.Add(Probleme("Deux chaînes TV différentes au même moment",
                                               quand & ". La clé TNT ne capte qu'une chaîne à la fois.", a.Numero, b.Numero))
                    End If
                End If
            Next
        Next
        Return Trier(problemes)
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Films introuvables (demande de Jacob, 08/10/2026 : "il faut gerer si le film est introuvable.
    ' Je lui avais mis un film qui n'existait pas" : Slide n'affichait "Film introuvable" qu'a
    ' l'heure de la tache). Verifie a part de Verifier, car il faut regarder le disque.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>Le fichier a chercher pour un film de la colonne Action, ou "" quand il n'y a pas de
    ''' fichier a verifier : adresse web, Netflix ou Prime Video, recherche du dernier
    ''' enregistrement ("dossier\#mot*.ext"), commande d'arret.</summary>
    Public Function FichierDuFilm(action As String) As String
        Dim a As String = Propre(action)
        If a = "" Then Return ""
        Dim m As String = a.ToUpperInvariant()
        If m.StartsWith("HTTP") OrElse m.Contains("WWW.NETFLIX.COM") OrElse m.Contains("WWW.PRIMEVIDEO.COM") OrElse
           m.StartsWith("STOPNETFLIXPRIMEVIDEO:") OrElse a.Contains("#") OrElse m.EndsWith(" OFF") Then Return ""
        Return a
    End Function

    ''' <summary>
    ''' Les films de la minuterie dont le fichier n'existe pas : simple avertissement (le fichier
    ''' peut arriver plus tard). Les fichiers sont cherches tous en meme temps, hors du fil de
    ''' l'interface, pendant <paramref name="delaiMs"/> au plus : un lecteur reseau eteint (le
    ''' recepteur satellite...) ferait sinon attendre de longues secondes par film. Un fichier
    ''' dont on n'a pas eu la reponse a temps n'est PAS signale (on ne sait pas).
    ''' </summary>
    Public Function FilmsIntrouvables(lignes As List(Of LigneTache), Optional delaiMs As Integer = 2000) As List(Of Incoherence)
        Dim problemes As New List(Of Incoherence)
        If lignes Is Nothing Then Return problemes
        Try
            Dim recherches As New Dictionary(Of String, Threading.Tasks.Task(Of Boolean))(StringComparer.OrdinalIgnoreCase)
            For Each l As LigneTache In lignes
                If UCase(Propre(l.Type)) <> "FILM" Then Continue For
                Dim fichier As String = If(Convert.ToString(Arg("Url", l.Reglages)) <> "", "", FichierDuFilm(l.Action))     ' film IPTV : adresse web
                If fichier = "" OrElse recherches.ContainsKey(fichier) Then Continue For
                Dim chemin As String = fichier
                recherches(fichier) = Threading.Tasks.Task.Run(Function() IO.File.Exists(chemin))
            Next
            If recherches.Count = 0 Then Return problemes
            Try
                Threading.Tasks.Task.WaitAll(recherches.Values.ToArray(), Math.Max(100, delaiMs))
            Catch ex As Exception
                ' une recherche en erreur : son film ne sera pas signale
            End Try
            For Each l As LigneTache In lignes
                If UCase(Propre(l.Type)) <> "FILM" Then Continue For
                Dim fichier As String = If(Convert.ToString(Arg("Url", l.Reglages)) <> "", "", FichierDuFilm(l.Action))     ' film IPTV : adresse web
                If fichier = "" Then Continue For
                Dim recherche As Threading.Tasks.Task(Of Boolean) = recherches(fichier)
                If recherche.Status <> Threading.Tasks.TaskStatus.RanToCompletion OrElse recherche.Result Then Continue For
                problemes.Add(Probleme("Film introuvable",
                                       Majuscule(Moment(l, l)) & ", " & Propre(l.Type) & ". Le fichier « " & fichier & " » n'existe pas : à l'heure dite, Slide affichera « Film introuvable » et le film ne sera pas joué.",
                                       l.Numero))
            Next
        Catch ex As Exception
            ' une verification ne doit jamais empecher d'enregistrer
        End Try
        Return problemes
    End Function

    ''' <summary>Les reglages hors du tableau d'abord, puis dans l'ordre du tableau (les problemes
    ''' des premieres lignes en premier).</summary>
    Public Function Trier(problemes As List(Of Incoherence)) As List(Of Incoherence)
        Dim premiere As Func(Of Incoherence, Integer) = Function(p) If(p.Lignes Is Nothing OrElse p.Lignes.Length = 0, 0, p.Lignes.Min())
        Dim derniere As Func(Of Incoherence, Integer) = Function(p) If(p.Lignes Is Nothing OrElse p.Lignes.Length = 0, 0, p.Lignes.Max())
        Return problemes.OrderBy(premiere).ThenBy(Function(p) If(p.Lignes Is Nothing, 0, p.Lignes.Length)).ThenBy(derniere).ToList()
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Chevauchement des horaires (demande de Jacob, 06/10 : "qu'un programme ne demarre pas avant
    ' que l'ancien finisse"). D'apres l'heure de debut et la colonne Durée de la premiere tache.
    ' Pas de signalement quand on ne peut pas savoir, ou quand c'est voulu :
    '   - tache au BOOT (son heure reelle depend du lancement), heure ou duree absente ;
    '   - film "Lire en boucle" : il n'a pas de fin, c'est la tache suivante qui l'arrete ;
    '   - deux taches qui peuvent s'afficher ensemble (plein ecran + PIP, PIP de coins differents).
    ' ------------------------------------------------------------------------------------------

    Private Const MinutesParJour As Integer = 1440
    Private Const MinutesParSemaine As Integer = 7 * MinutesParJour

    ''' <summary>Texte du probleme si <paramref name="suivante"/> demarre pendant
    ''' <paramref name="premiere"/> et que les deux ne peuvent pas cohabiter ; "" sinon.</summary>
    Private Function Chevauchement(premiere As LigneTache, suivante As LigneTache) As String
        If Not EstAffichage(premiere) OrElse Not EstAffichage(suivante) Then Return ""
        If Propre(premiere.Action) = "" OrElse Propre(suivante.Action) = "" Then Return ""
        If EstBoot(premiere) OrElse EstBoot(suivante) Then Return ""
        If UCase(CStr(Arg("Boucle", If(premiere.Reglages, "")))) = "TRUE" Then Return ""
        Dim duree As Integer = DureeEnMinutes(premiere.Duree)
        Dim debut1 As Integer = MinutesDepuisMinuit(premiere), debut2 As Integer = MinutesDepuisMinuit(suivante)
        If duree <= 0 OrElse duree >= MinutesParSemaine OrElse debut1 < 0 OrElse debut2 < 0 Then Return ""

        ' Les deux peuvent-elles s'afficher ensemble ?
        Dim pip1 As Boolean = EstPip(premiere), pip2 As Boolean = EstPip(suivante)
        Dim conflit As String
        If pip1 AndAlso pip2 AndAlso String.Equals(Coin(premiere), Coin(suivante), StringComparison.OrdinalIgnoreCase) Then
            conflit = "Deux PIP au même endroit (" & NomDuCoin(Coin(premiere)) & ") se chevauchent : le second remplacera le premier."
        ElseIf Not pip1 AndAlso Not pip2 Then
            conflit = "Deux programmes en plein écran se chevauchent : le premier sera coupé."
        ElseIf UCase(Propre(premiere.Type)) = "TV" AndAlso UCase(Propre(suivante.Type)) = "TV" AndAlso
               Not String.Equals(Propre(premiere.Action), Propre(suivante.Action), StringComparison.OrdinalIgnoreCase) Then
            conflit = "Deux chaînes TV différentes se chevauchent : la clé TNT ne capte qu'une chaîne à la fois."
        Else
            Return ""
        End If

        ' Un jour de la premiere et un jour de la suivante tels que la suivante demarre avant la fin.
        For Each j1 As Integer In JoursCouverts(premiere).OrderBy(Function(x) x)
            For Each j2 As Integer In JoursCouverts(suivante).OrderBy(Function(x) x)
                Dim depart As Integer = j1 * MinutesParJour + debut1
                Dim ecart As Integer = ((j2 * MinutesParJour + debut2 - depart) Mod MinutesParSemaine + MinutesParSemaine) Mod MinutesParSemaine
                If ecart > 0 AndAlso ecart < duree Then
                    Dim fin As Integer = (depart + duree) Mod MinutesParSemaine
                    Return "La ligne " & suivante.Numero & " démarre " & Jours(j2).ToLower() & " à " & Heure5(suivante) &
                           ", alors que la ligne " & premiere.Numero & " (" & Jours(j1).ToLower() & " " & Heure5(premiere) &
                           ", durée " & DureeLisible(duree) & ") ne finit qu" &
                           If(fin \ MinutesParJour = j2, "'à ", "e " & Jours(fin \ MinutesParJour).ToLower() & " à ") &
                           HeureLisible(fin Mod MinutesParJour) & ". " & conflit
                End If
            Next
        Next
        Return ""
    End Function

    ''' <summary>Heure de debut en minutes depuis minuit, -1 si elle est illisible.</summary>
    Private Function MinutesDepuisMinuit(l As LigneTache) As Integer
        Dim p As String() = Heure5(l).Split(":"c)
        Dim h As Integer, m As Integer
        If p.Length <> 2 OrElse Not Integer.TryParse(p(0), h) OrElse Not Integer.TryParse(p(1), m) Then Return -1
        If h < 0 OrElse h > 23 OrElse m < 0 OrElse m > 59 Then Return -1
        Return h * 60 + m
    End Function

    ''' <summary>Duree de la colonne Durée en minutes : "01:30:17" (les secondes comptent pour une
    ''' minute de plus), "01:30" ou "0J2h44m" ; 0 si elle est vide ou illisible. Meme lecture que
    ''' celle de Form4 ("Horaire a la suite").</summary>
    Private Function DureeEnMinutes(texte As String) As Integer
        Dim t As String = Propre(texte)
        If t = "" Then Return 0
        Dim m As System.Text.RegularExpressions.Match =
            System.Text.RegularExpressions.Regex.Match(t, "^\s*(?:(\d+)\s*[jJ])?\s*(?:(\d+)\s*[hH])?\s*(?:(\d+)\s*[mM])?\s*$")
        If m.Success AndAlso (m.Groups(1).Success OrElse m.Groups(2).Success OrElse m.Groups(3).Success) Then
            Return If(m.Groups(1).Success, CInt(m.Groups(1).Value) * MinutesParJour, 0) +
                   If(m.Groups(2).Success, CInt(m.Groups(2).Value) * 60, 0) +
                   If(m.Groups(3).Success, CInt(m.Groups(3).Value), 0)
        End If
        Dim p As String() = t.Split(":"c)
        Dim a As Integer, b As Integer, c As Integer
        If p.Length = 3 AndAlso Integer.TryParse(p(0), a) AndAlso Integer.TryParse(p(1), b) AndAlso Integer.TryParse(p(2), c) Then
            Return a * 60 + b + If(c > 0, 1, 0)
        End If
        If p.Length = 2 AndAlso Integer.TryParse(p(0), a) AndAlso Integer.TryParse(p(1), b) Then Return a * 60 + b
        Return 0
    End Function

    Private Function HeureLisible(minutes As Integer) As String
        Return (minutes \ 60).ToString("00") & ":" & (minutes Mod 60).ToString("00")
    End Function

    Private Function DureeLisible(minutes As Integer) As String
        If minutes >= MinutesParJour Then Return (minutes \ MinutesParJour) & " j " & HeureLisible(minutes Mod MinutesParJour)
        Return HeureLisible(minutes)
    End Function

    ' ------------------------------------------------------------------------------------------

    Private Function Propre(s As String) As String
        Return If(s, "").Trim()
    End Function

    ''' <summary>Tache qui affiche quelque chose (film, chaine) ; pas un arret ("Chaine OFF"...).</summary>
    Private Function EstAffichage(l As LigneTache) As Boolean
        If Not TypesAffiches.Contains(UCase(Propre(l.Type))) Then Return False
        Return Not Propre(l.Action).EndsWith(" OFF", StringComparison.OrdinalIgnoreCase)
    End Function

    ''' <summary>Volume a 0, ou case vide (lue comme 0 a la lecture).</summary>
    Private Function VolumeNul(l As LigneTache) As Boolean
        Dim v As Integer
        If Propre(l.Volume) = "" Then Return True
        Return Integer.TryParse(Propre(l.Volume), v) AndAlso v <= 0
    End Function

    Private Function EstPip(l As LigneTache) As Boolean
        Return UCase(CStr(Arg("Pip", If(l.Reglages, "")))) = "TRUE"
    End Function

    Private Function Coin(l As LigneTache) As String
        Return CoinDuPip(If(l.Reglages, ""))
    End Function

    Private Function NomDuCoin(coin As String) As String
        Select Case UCase(If(coin, ""))
            Case "BGAUCHE" : Return "en bas à gauche"
            Case "HDROITE" : Return "en haut à droite"
            Case "HGAUCHE" : Return "en haut à gauche"
            Case Else : Return "en bas à droite"
        End Select
    End Function

    Private Function Heure5(l As LigneTache) As String
        Dim h As String = Propre(l.Heure)
        Return If(h.Length > 5, h.Substring(0, 5), h)
    End Function

    Private Function EstBoot(l As LigneTache) As Boolean
        Return UCase(Propre(l.Jour)) = "BOOT"
    End Function

    ''' <summary>Jours de la semaine couverts (0 = lundi) ; vide pour BOOT ou un jour inconnu.</summary>
    Private Function JoursCouverts(l As LigneTache) As HashSet(Of Integer)
        Dim j As String = UCase(Propre(l.Jour))
        Select Case j
            Case "LUN-VEN" : Return New HashSet(Of Integer)({0, 1, 2, 3, 4})
            Case "SAM-DIM" : Return New HashSet(Of Integer)({5, 6})
            Case "TLJ" : Return New HashSet(Of Integer)({0, 1, 2, 3, 4, 5, 6})
        End Select
        Dim i As Integer = Array.FindIndex(Jours, Function(n) UCase(n) = j)
        Return If(i >= 0, New HashSet(Of Integer)({i}), New HashSet(Of Integer)())
    End Function

    Private Function MemeMoment(a As LigneTache, b As LigneTache) As Boolean
        If EstBoot(a) OrElse EstBoot(b) Then Return EstBoot(a) AndAlso EstBoot(b)
        If Heure5(a) = "" OrElse Heure5(a) <> Heure5(b) Then Return False
        Return JoursCouverts(a).Overlaps(JoursCouverts(b))
    End Function

    Private Function Moment(a As LigneTache, b As LigneTache) As String
        If EstBoot(a) Then Return "au lancement (BOOT)"
        Dim jour As String = If(String.Equals(Propre(a.Jour), Propre(b.Jour), StringComparison.OrdinalIgnoreCase),
                                Propre(a.Jour), Propre(a.Jour) & " / " & Propre(b.Jour))
        Return jour & " " & Heure5(a)
    End Function

    Private Function Identiques(a As LigneTache, b As LigneTache) As Boolean
        Return String.Equals(Propre(a.Jour), Propre(b.Jour), StringComparison.OrdinalIgnoreCase) AndAlso
               Heure5(a) = Heure5(b) AndAlso
               String.Equals(Propre(a.Type), Propre(b.Type), StringComparison.OrdinalIgnoreCase) AndAlso
               String.Equals(Propre(a.Action), Propre(b.Action), StringComparison.OrdinalIgnoreCase) AndAlso
               Propre(a.Play) = Propre(b.Play) AndAlso Propre(a.Volume) = Propre(b.Volume) AndAlso
               Propre(a.Reglages) = Propre(b.Reglages) AndAlso Propre(a.Action) <> ""
    End Function

End Module
