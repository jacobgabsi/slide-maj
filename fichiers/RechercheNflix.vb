Option Strict On
Option Explicit On

Imports System.IO
Imports System.Text
Imports System.Text.RegularExpressions

''' <summary>
''' Lecture et recherche dans les listes de la fenetre Nflix (Form14) : chaines en direct, films, series.
'''
''' Demande de Jacob, 13/10/2026 : "optimise la recherche dans nflix", puis "tri les resultats".
'''
''' Avant, chaque recherche relisait tout le fichier (28 Mo pour 100 000 films), cherchait le texte dans
''' TOUTES les lignes (adresses des flux comprises, donc les identifiants du compte), et ajoutait les
''' resultats un a un dans le tableau ; un clic sur un groupe relisait aussi tout le fichier.
'''
''' Maintenant la liste est lue UNE fois (gardee en memoire tant que le fichier ne change pas et que la
''' fenetre est ouverte) :
'''  - les groupes, le contenu d'un groupe et la recherche viennent de cette lecture ;
'''  - la recherche porte sur le NOM seulement, sans tenir compte des majuscules ni des accents ;
'''    plusieurs mots : tous doivent y etre, dans n'importe quel ordre ;
'''  - les resultats sont TRIES : les noms qui commencent par le texte cherche, puis ceux qui le
'''    contiennent tel quel, puis les autres ; a egalite, ordre alphabetique (S01E02 avant S01E10) ;
'''  - le tableau est rempli d'un coup, avec au plus <see cref="MaximumDeResultats"/> lignes.
''' </summary>
''' <summary>Une fenetre qui sait colorer son tableau selon les notes (Form14 de Slide, FormIptv d'IPTV) : RechercheNflix.RemplirLaGrille la previent sans connaitre son type.</summary>
Friend Interface IFenetreNotes
    Sub ColorerSelonLesNotes(entrees As List(Of RechercheNflix.Entree))
End Interface

Friend Module RechercheNflix

    ''' <summary>Au plus ce nombre de resultats dans le tableau (au dela, la recherche est a preciser).</summary>
    Friend Const MaximumDeResultats As Integer = 3000

    ''' <summary>Note (sur 10) a partir de laquelle un film est "tres bien note" : sa ligne est verte dans le tableau.</summary>
    Friend Const NoteTresBonne As Double = 8.0

    ''' <summary>Le dernier texte cherche (propose de nouveau a la recherche suivante).</summary>
    Friend DerniereRecherche As String = ""

    ''' <summary>La case "Dans ce groupe" de la recherche etait cochee la derniere fois.</summary>
    Friend DerniereRechercheDansLeGroupe As Boolean = False

    ''' <summary>Une chaine, un film ou une serie de la liste.</summary>
    Friend Class Entree
        Friend Nom As String = ""
        Friend Adresse As String = ""
        Friend Logo As String = ""
        Friend Groupe As String = ""
        ''' <summary>Le nom sans accents ni majuscules (calcule a la premiere recherche).</summary>
        Friend NomSimple As String
        ''' <summary>La note sur 10 donnee par le serveur (tvg-rating), 0 si elle n'est pas dans la liste.</summary>
        Friend Note As Double
        ''' <summary>Date d'ajout au catalogue du serveur (secondes depuis 1970, tvg-added), 0 si elle n'est pas dans la liste.</summary>
        Friend Ajoute As Long

        ''' <summary>"Chaîne", "Film" ou "Série" : lu dans l'adresse (une série porte "slide-serie:", un film "/movie/").</summary>
        Friend Function Contenu() As String
            If Adresse.StartsWith(IptvSources.PrefixeSerie, StringComparison.Ordinal) Then Return "Série"
            If Adresse.Contains("/movie/", StringComparison.OrdinalIgnoreCase) OrElse Adresse.Contains("/series/", StringComparison.OrdinalIgnoreCase) Then Return "Film"
            Return "Chaîne"
        End Function
    End Class

    ''' <summary>Une liste lue : ses entrees, ses groupes dans l'ordre du fichier, et ce qui permet de savoir si le fichier a change.</summary>
    Private Class Memoire
        Friend Entrees As New List(Of Entree)
        Friend Groupes As New List(Of String)
        Friend ParGroupe As New Dictionary(Of String, List(Of Entree))
        Friend Ecriture As DateTime
        Friend Taille As Long
    End Class

    Private ReadOnly _memoires As New Dictionary(Of String, Memoire)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>Libere la memoire (fenetre fermee ou cachee).</summary>
    Friend Sub Oublier()
        _memoires.Clear()
    End Sub

    ''' <summary>La valeur d'un attribut d'une ligne #EXTINF (tvg-logo="..." ou tvg-logo=...), "" s'il n'y est pas.</summary>
    Private Function Attribut(ligne As String, nom As String) As String
        Dim p As Integer = ligne.IndexOf(nom & "=", StringComparison.Ordinal)
        If p < 0 Then Return ""
        p += nom.Length + 1
        If p >= ligne.Length Then Return ""
        If ligne(p) = """"c Then
            Dim fin As Integer = ligne.IndexOf(""""c, p + 1)
            If fin < 0 Then Return ligne.Substring(p + 1)
            Return ligne.Substring(p + 1, fin - p - 1)
        End If
        Dim f As Integer = ligne.IndexOfAny({" "c, ","c}, p)
        Return If(f < 0, ligne.Substring(p), ligne.Substring(p, f - p))
    End Function

    ''' <summary>Lit la liste (ou ne fait rien si c'est deja la bonne, inchangee). Le groupe d'une chaine est lu
    ''' comme avant : le texte apres group-title= jusqu'a la premiere virgule, guillemets retires ; une ligne
    ''' sans group-title garde le groupe de la precedente.</summary>
    Private Function Charge(fichier As String) As Memoire
        Dim info As New FileInfo(fichier)
        Dim connue As Memoire = Nothing
        If _memoires.TryGetValue(fichier, connue) AndAlso connue.Ecriture = info.LastWriteTimeUtc AndAlso connue.Taille = info.Length Then Return connue

        Dim entrees As New List(Of Entree)
        Dim groupes As New List(Of String)
        Dim parGroupe As New Dictionary(Of String, List(Of Entree))
        Dim groupe As String = ""
        Dim attente As Entree = Nothing
        Using lecture As New StreamReader(fichier, Encoding.UTF8, True)
            Dim ligne As String = lecture.ReadLine()
            Do While ligne IsNot Nothing
                If ligne.StartsWith("#EXTINF", StringComparison.Ordinal) Then
                    Dim g As Integer = ligne.IndexOf("group-title=", StringComparison.Ordinal)
                    If g >= 0 Then
                        Dim debut As Integer = g + 12
                        Dim virgule As Integer = ligne.IndexOf(","c, debut)
                        groupe = If(virgule >= 0, ligne.Substring(debut, virgule - debut), ligne.Substring(debut)).Replace("""", "")
                    End If
                    Dim e As New Entree With {.Groupe = groupe}
                    Dim v As Integer = ligne.LastIndexOf(","c)
                    If v >= 0 Then e.Nom = ligne.Substring(v + 1).Trim()
                    If e.Nom = "" Then e.Nom = Attribut(ligne, "tvg-name").Trim()
                    e.Logo = Attribut(ligne, "tvg-logo").Trim()
                    Dim note As String = Attribut(ligne, "tvg-rating").Trim()
                    If note <> "" Then Double.TryParse(note, Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, e.Note)
                    Dim ajoute As String = Attribut(ligne, "tvg-added").Trim()
                    If ajoute <> "" Then Long.TryParse(ajoute, e.Ajoute)
                    attente = e
                ElseIf ligne.Length > 0 AndAlso ligne(0) <> "#"c Then
                    If attente IsNot Nothing Then
                        attente.Adresse = ligne.Trim()
                        entrees.Add(attente)
                        Dim liste As List(Of Entree) = Nothing
                        If Not parGroupe.TryGetValue(attente.Groupe, liste) Then
                            liste = New List(Of Entree)
                            parGroupe(attente.Groupe) = liste
                            groupes.Add(attente.Groupe)
                        End If
                        liste.Add(attente)
                        attente = Nothing
                    End If
                End If
                ligne = lecture.ReadLine()
            Loop
        End Using
        Dim m As New Memoire With {.Entrees = entrees, .Groupes = groupes, .ParGroupe = parGroupe, .Ecriture = info.LastWriteTimeUtc, .Taille = info.Length}
        _memoires(fichier) = m
        Return m
    End Function

    ''' <summary>Les groupes de la liste, dans l'ordre du fichier.</summary>
    Friend Function Groupes(fichier As String) As List(Of String)
        Return Charge(fichier).Groupes
    End Function

    ''' <summary>Le contenu d'un groupe, dans l'ordre du fichier.</summary>
    Friend Function DuGroupe(fichier As String, groupe As String) As List(Of Entree)
        Dim liste As List(Of Entree) = Nothing
        If Charge(fichier).ParGroupe.TryGetValue(If(groupe, ""), liste) Then Return liste
        Return New List(Of Entree)
    End Function

    ''' <summary>Sans accents ni majuscules ("Épisode" -> "episode").</summary>
    Friend Function Simplifier(texte As String) As String
        If String.IsNullOrEmpty(texte) Then Return ""
        Dim ascii As Boolean = True
        For Each c As Char In texte
            If AscW(c) > 127 Then
                ascii = False
                Exit For
            End If
        Next
        If ascii Then Return texte.ToLowerInvariant()
        Dim d As String = texte.Normalize(NormalizationForm.FormD)
        Dim sb As New StringBuilder(d.Length)
        For Each c As Char In d
            If Globalization.CharUnicodeInfo.GetUnicodeCategory(c) <> Globalization.UnicodeCategory.NonSpacingMark Then sb.Append(Char.ToLowerInvariant(c))
        Next
        Return sb.ToString()
    End Function

    ''' <summary>Ordre des groupes dans la liste de gauche de Nflix (clic sur son titre) : 0 = celui du fournisseur,
    ''' 1 = A a Z, 2 = Z a A (demande de Jacob, 13/10/2026 : "rajoute un titre au tableau de gauche pour pouvoir trier").</summary>
    Friend OrdreDesGroupes As Integer = 0

    ''' <summary>Les groupes dans l'ordre choisi (accents et majuscules ignores, nombres dans l'ordre : "2020" avant "10000").</summary>
    Friend Function GroupesTries(groupes As IEnumerable(Of String)) As List(Of String)
        If FiltreDesGroupes IsNot Nothing Then
            Dim gardes As New HashSet(Of String)(FiltreDesGroupes)
            groupes = groupes.Where(Function(g) gardes.Contains(g)).ToList()
        End If
        Select Case OrdreDesGroupes
            Case 1
                Return groupes.OrderBy(Function(g) CleDeTri(Simplifier(g)), StringComparer.Ordinal).ToList()
            Case 2
                Return groupes.OrderByDescending(Function(g) CleDeTri(Simplifier(g)), StringComparer.Ordinal).ToList()
        End Select
        Return groupes.ToList()
    End Function

    ''' <summary>Le rang du groupe "Le dernier ajoute" parmi <paramref name="groupes"/> (-1 s'il n'y en a pas) :
    ''' un nom qui contient "dernier" et "ajout", sans tenir compte des accents ni des majuscules, et meme
    ''' ecrit "drenier" (faute de frappe courante dans les listes). Le premier trouve.</summary>
    Friend Function RangDuDernierAjout(groupes As IList(Of String)) As Integer
        If groupes Is Nothing Then Return -1
        For i As Integer = 0 To groupes.Count - 1
            Dim n As String = Simplifier(groupes(i))
            If (n.Contains("dernier", StringComparison.Ordinal) OrElse n.Contains("drenier", StringComparison.Ordinal)) AndAlso
               n.Contains("ajout", StringComparison.Ordinal) Then Return i
        Next
        Return -1
    End Function

    ''' <summary>Cle de tri : les nombres sont completes a 8 chiffres (S01E02 avant S01E10, 2 avant 10).</summary>
    Private Function CleDeTri(nomSimple As String) As String
        Return Regex.Replace(nomSimple, "\d+", Function(m As Match) m.Value.PadLeft(8, "0"c))
    End Function

    ''' <summary>Quand une recherche a trouve des groupes, la liste de gauche n'affiche qu'eux (Nothing : tous).</summary>
    Friend FiltreDesGroupes As List(Of String) = Nothing

    ''' <summary>Les noms qui contiennent tous les mots de <paramref name="terme"/>, tries (voir plus haut), au
    ''' plus <paramref name="maximum"/>. <paramref name="tronque"/> : il y en a davantage.</summary>
    Friend Function Chercher(fichier As String, terme As String, maximum As Integer, ByRef tronque As Boolean) As List(Of Entree)
        Dim groupes As List(Of String) = Nothing
        Return Rechercher({fichier}, terme, maximum, False, tronque, groupes)
    End Function

    ''' <summary>
    ''' La recherche du bouton "Rechercher" de Nflix (demande de Jacob, 13/10/2026 : "la recherche doit porter
    ''' aussi sur les groupes"). Rend les titres dont le NOM contient tous les mots, puis ceux dont le nom
    ''' et le GROUPE les contiennent ensemble (le mot est dans le groupe : "marvel" trouve les films du
    ''' groupe "Collection Marvel"). <paramref name="groupesTrouves"/> : les groupes dont le nom contient
    ''' tous les mots.
    ''' </summary>
    Friend Function ChercherPartout(fichier As String, terme As String, maximum As Integer, ByRef tronque As Boolean,
                                    ByRef groupesTrouves As List(Of String)) As List(Of Entree)
        Return Rechercher({fichier}, terme, maximum, True, tronque, groupesTrouves)
    End Function

    ''' <summary>
    ''' La recherche de Nflix sur TOUS les contenus (demande de Jacob, 13/10/2026 : "la recherche doit porter sur
    ''' tout les contenus") : les chaines, les films et les series. La premiere liste est celle qui est affichee :
    ''' <paramref name="groupesTrouves"/> sont ses groupes qui contiennent les mots.
    ''' </summary>
    Friend Function ChercherDansTout(fichiers As IList(Of String), terme As String, maximum As Integer, ByRef tronque As Boolean,
                                     ByRef groupesTrouves As List(Of String), Optional dansLeGroupe As String = Nothing) As List(Of Entree)
        Return Rechercher(fichiers, terme, maximum, True, tronque, groupesTrouves, dansLeGroupe, QueLesGroupes)
    End Function

    Private Function Rechercher(fichiers As IList(Of String), terme As String, maximum As Integer, avecLesGroupes As Boolean,
                                ByRef tronque As Boolean, ByRef groupesTrouves As List(Of String),
                                Optional dansLeGroupe As String = Nothing, Optional queLesGroupes As Boolean = False) As List(Of Entree)
        tronque = False
        groupesTrouves = New List(Of String)
        Dim memoires As New List(Of Memoire)
        For Each fichier As String In fichiers
            If File.Exists(fichier) Then memoires.Add(Charge(fichier))
        Next
        Dim phrase As String = String.Join(" ", Simplifier(terme).Split({" "c}, StringSplitOptions.RemoveEmptyEntries))
        Dim resultat As New List(Of Entree)
        If phrase = "" Then Return resultat
        Dim mots As String() = phrase.Split(" "c)
        Dim contientTout As Func(Of String, Boolean) =
            Function(texte As String) As Boolean
                For Each mot As String In mots
                    If texte.IndexOf(mot, StringComparison.Ordinal) < 0 Then Return False
                Next
                Return True
            End Function

        Dim groupesSimples As New Dictionary(Of String, String)
        If avecLesGroupes Then
            For i As Integer = 0 To memoires.Count - 1
                For Each g As String In memoires(i).Groupes
                    If Not groupesSimples.ContainsKey(g) Then groupesSimples(g) = Simplifier(g)
                    If i = 0 AndAlso contientTout(groupesSimples(g)) AndAlso (Not QueFrancais OrElse EstGroupeFrancais(g)) Then groupesTrouves.Add(g)    ' (la liste affichee)
                Next
            Next
        End If

        Dim trouves As New List(Of KeyValuePair(Of Integer, Entree))
        Dim toutes As IEnumerable(Of Entree) = memoires.SelectMany(Function(mm) mm.Entrees)
        For Each e As Entree In If(queLesGroupes, Enumerable.Empty(Of Entree)(), toutes)           ' (« uniquement un groupe » : aucun titre)
            ' "Dans ce groupe" (15/10/2026) : seulement les titres du groupe choisi.
            If dansLeGroupe IsNot Nothing AndAlso Not String.Equals(e.Groupe, dansLeGroupe, StringComparison.Ordinal) Then Continue For
            If e.NomSimple Is Nothing Then e.NomSimple = Simplifier(e.Nom)
            If QueFrancais AndAlso Not EstFrancais(e) Then Continue For         ' (case "Que les films en Francais" : que des films)
            Dim rang As Integer = -1
            If contientTout(e.NomSimple) Then
                rang = 2
                If e.NomSimple.StartsWith(phrase, StringComparison.Ordinal) Then
                    rang = 0
                ElseIf e.NomSimple.Contains(phrase, StringComparison.Ordinal) Then
                    rang = 1
                End If
            ElseIf avecLesGroupes Then
                Dim gs As String = Nothing
                If groupesSimples.TryGetValue(e.Groupe, gs) AndAlso contientTout(e.NomSimple & " " & gs) Then rang = 3
            End If
            If rang >= 0 Then trouves.Add(New KeyValuePair(Of Integer, Entree)(rang, e))
        Next
        tronque = trouves.Count > maximum
        Dim tries = trouves.Select(Function(t) New With {.Rang = t.Key, .Cle = CleDeTri(t.Value.NomSimple), .Entree = t.Value}).
                            OrderBy(Function(t) t.Rang).ThenBy(Function(t) t.Cle, StringComparer.Ordinal)
        For Each t In tries.Take(maximum)
            resultat.Add(t.Entree)
        Next
        Return resultat
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Nouveautes des films (menu Fichier > "Nouveautes Film rajoutee") : voir plus bas.
    '
    ' Le catalogue des films est refait au premier besoin de chaque lancement, puis compare a la liste des films
    ' deja vus (data\settings\iptv_N_films_connus.txt : un numero de film par ligne). Les films absents de cette
    ' liste sont les nouveautes ; la liste est ensuite completee. Elle ne perd jamais un numero : un film qui
    ' disparait puis revient n'est pas annonce deux fois. La comparaison est faite UNE fois par lancement (les
    ' ouvertures suivantes montrent la meme liste) ; au tout premier usage la liste est seulement memorisee.
    ' ------------------------------------------------------------------------------------------

    ' ------------------------------------------------------------------------------------------
    ' Films tres bien notes (19/10/2026 : bouton de la fenetre Rechercher). Les notes viennent de la liste des films
    ' (tvg-rating, ecrit au telechargement du catalogue) ; une liste d'avant les notes est retelechargee d'abord.
    ' ------------------------------------------------------------------------------------------

    ' ------------------------------------------------------------------------------------------
    ' "Que les films en Francais" (19/10/2026, case de la fenetre Rechercher). Un film est francais si son GROUPE l'est
    ' ("FR - COMEDIE", "FR: FILMS - SD" : le groupe commence par FR) ou si son nom porte une marque de version francaise
    ' (VF, VFF, VFQ, TRUEFRENCH, FRENCH, FRANCAIS). MULTI et VOSTFR ne comptent pas : la langue n'est pas garantie.
    ' ------------------------------------------------------------------------------------------

    ''' <summary>La case "Que les films en Francais" est cochee (gardee d'une recherche a l'autre).</summary>
    Friend QueFrancais As Boolean = False

    ''' <summary>La case « Rechercher uniquement un groupe » du bouton Rechercher est cochee (decochee au depart, gardee d'une recherche a l'autre, 26/10/2026) : le texte est cherche seulement dans les NOMS
    ''' des groupes - la liste de gauche ne garde que les groupes trouves, la liste des resultats reste vide. Decochee : titres et groupes, comme toujours.</summary>
    Friend QueLesGroupes As Boolean = False

    Private ReadOnly _groupeFr As New Regex("^(fr(?![a-z])|france|french|francais)", RegexOptions.Compiled)
    Private ReadOnly _marqueFr As New Regex("\b(vf|vff|vfq|truefrench|french|francais)\b", RegexOptions.Compiled)

    ''' <summary>Le groupe est un groupe francais ("FR - ...").</summary>
    Friend Function EstGroupeFrancais(groupe As String) As Boolean
        Return _groupeFr.IsMatch(Simplifier(groupe).TrimStart())
    End Function

    ''' <summary>Le film est francais : groupe francais, ou marque de version francaise dans le nom.</summary>
    Friend Function EstFrancais(e As Entree) As Boolean
        If e.Contenu() <> "Film" Then Return False
        If EstGroupeFrancais(e.Groupe) Then Return True
        If e.NomSimple Is Nothing Then e.NomSimple = Simplifier(e.Nom)
        Return _marqueFr.IsMatch(e.NomSimple)
    End Function

    ''' <summary>Le bouton "Films tres bien notes" de la fenetre Rechercher a ete choisi (au lieu de "Rechercher").</summary>
    Friend DemandeTresBienNotes As Boolean = False

    ''' <summary>Avec <see cref="DemandeTresBienNotes"/> : le bouton "Films > 9" (seulement les films de plus de <see cref="NoteExcellente"/>).</summary>
    Friend DemandeExcellents As Boolean = False

    ''' <summary>Avec <see cref="DemandeTresBienNotes"/> : le bouton "Films > 7" (les films de <see cref="NoteBonne"/> ou plus : fond violet leger).</summary>
    Friend DemandeBons As Boolean = False

    ''' <summary>A partir de cette note (sur 10) un film est "bon" : fond violet leger (22/10/2026). Sous la note IMDb, 1 point de moins (voir EcartImdb).</summary>
    Friend Const NoteBonne As Double = 7.0

    ''' <summary>La liste des films a ses notes ; sinon elle est retelechargee. Rend Faux si aucune liste n'est utilisable
    ''' (<paramref name="message"/> dit pourquoi) ; <paramref name="message"/> porte aussi une remarque si le serveur ne repond pas.
    ''' Sans interface : a appeler en tache de fond.</summary>
    Friend Function AssurerLesNotes(source As Integer, ByRef message As String) As Boolean
        message = ""
        Dim fichier As String = IptvSources.FichierDuGenre(source, IptvSources.GenreIptv.Films)
        Try
            If File.Exists(fichier) Then
                For Each ligne As String In File.ReadLines(fichier, Encoding.UTF8)
                    If ligne.StartsWith("#EXTINF", StringComparison.Ordinal) Then
                        If ligne.Contains("tvg-rating=", StringComparison.Ordinal) Then Return True
                        Exit For
                    End If
                Next
            End If
        Catch ex As Exception
        End Try
        Dim erreur As String = ""
        If IptvSources.TelechargerCatalogue(source, IptvSources.GenreIptv.Films, erreur) Then Return True
        If File.Exists(fichier) Then
            message = "Le serveur ne répond pas (" & erreur & ") : les notes ne sont pas à jour."
            Return True
        End If
        message = "Le catalogue des films n'a pas pu être chargé :" & vbCrLf & erreur
        Return False
    End Function

    ''' <summary>Les films notes <see cref="NoteTresBonne"/> ou plus : les mieux notes d'abord, puis par ordre alphabetique.
    ''' <paramref name="texte"/> (mots, tous presents dans le nom) et <paramref name="groupe"/> restreignent la liste quand ils sont remplis.</summary>
    Friend Function FilmsTresBienNotes(source As Integer, texte As String, groupe As String, maximum As Integer, ByRef tronque As Boolean,
                                       Optional excellents As Boolean = False, Optional bons As Boolean = False) As List(Of Entree)
        tronque = False
        Dim fichier As String = IptvSources.FichierDuGenre(source, IptvSources.GenreIptv.Films)
        Dim mots As String() = Simplifier(texte).Split({" "c}, StringSplitOptions.RemoveEmptyEntries)
        Dim trouves As New List(Of Entree)
        For Each e As Entree In Charge(fichier).Entrees
            If e.Contenu() <> "Film" Then Continue For
            If bons Then
                If e.Note < NoteBonne Then Continue For
            ElseIf excellents Then
                If Not (e.Note > NoteExcellente) Then Continue For
            ElseIf e.Note < NoteTresBonne Then
                Continue For
            End If
            If groupe <> "" AndAlso e.Groupe <> groupe Then Continue For
            If QueFrancais AndAlso Not EstFrancais(e) Then Continue For
            If mots.Length > 0 Then
                Dim nom As String = Simplifier(e.Nom)
                Dim tous As Boolean = True
                For Each mot As String In mots
                    If Not nom.Contains(mot, StringComparison.Ordinal) Then tous = False : Exit For
                Next
                If Not tous Then Continue For
            End If
            trouves.Add(e)
        Next
        Dim tries As List(Of Entree) = trouves.OrderByDescending(Function(x) x.Note).ThenBy(Function(x) CleDeTri(Simplifier(x.Nom)), StringComparer.Ordinal).ToList()
        If tries.Count > maximum Then
            tronque = True
            tries = tries.Take(maximum).ToList()
        End If
        Return tries
    End Function

    ''' <summary>Les periodes proposees par le menu Fichier > Nouveautes Film rajoutee (20/10/2026).</summary>
    Friend Enum PeriodeNouveautes
        CetteSemaine = 0
        SemaineDerniere = 1
        CeMois = 2
        DeuxDerniersMois = 3
        TroisDerniersMois = 4
        CetteAnnee = 5
    End Enum

    ''' <summary>Les bornes d'une periode : <paramref name="debut"/> compris, <paramref name="fin"/> exclu. Semaines du lundi au dimanche ;
    ''' "cette semaine" = depuis lundi ; "ce mois" = depuis le 1er ; "2 / 3 derniers mois" = depuis la meme date il y a 2 / 3 mois ;
    ''' "cette annee" = depuis le 1er janvier. Rend le libelle ("cette semaine", "la semaine dernière"...).</summary>
    Friend Function BornesDeLaPeriode(periode As PeriodeNouveautes, ByRef debut As Date, ByRef fin As Date) As String
        Dim aujourdhui As Date = Date.Today
        Dim lundi As Date = aujourdhui.AddDays(-((CInt(aujourdhui.DayOfWeek) + 6) Mod 7))
        fin = Date.Now.AddDays(1)
        Select Case periode
            Case PeriodeNouveautes.CetteSemaine
                debut = lundi : Return "cette semaine"
            Case PeriodeNouveautes.SemaineDerniere
                debut = lundi.AddDays(-7) : fin = lundi : Return "la semaine dernière"
            Case PeriodeNouveautes.CeMois
                debut = New Date(aujourdhui.Year, aujourdhui.Month, 1) : Return "ce mois"
            Case PeriodeNouveautes.DeuxDerniersMois
                debut = aujourdhui.AddMonths(-2) : Return "les 2 derniers mois"
            Case PeriodeNouveautes.TroisDerniersMois
                debut = aujourdhui.AddMonths(-3) : Return "les 3 derniers mois"
            Case Else
                debut = New Date(aujourdhui.Year, 1, 1) : Return "cette année"
        End Select
    End Function

    ''' <summary>La liste des films a la date d'ajout de chaque film (tvg-added) et date de moins de <paramref name="heuresMax"/> heures ;
    ''' sinon elle est retelechargee. Rend Faux si aucune liste n'est utilisable (<paramref name="message"/> dit pourquoi) ;
    ''' <paramref name="message"/> porte aussi une remarque si le serveur ne repond pas. A appeler en tache de fond.</summary>
    Friend Function AssurerLesDates(source As Integer, heuresMax As Double, ByRef message As String) As Boolean
        message = ""
        Dim fichier As String = IptvSources.FichierDuGenre(source, IptvSources.GenreIptv.Films)
        Try
            If File.Exists(fichier) AndAlso (Date.Now - File.GetLastWriteTime(fichier)).TotalHours < heuresMax Then
                For Each ligne As String In File.ReadLines(fichier, Encoding.UTF8)
                    If ligne.StartsWith("#EXTINF", StringComparison.Ordinal) Then
                        If ligne.Contains("tvg-added=", StringComparison.Ordinal) Then Return True
                        Exit For
                    End If
                Next
            End If
        Catch ex As Exception
        End Try
        Dim erreur As String = ""
        If IptvSources.TelechargerCatalogue(source, IptvSources.GenreIptv.Films, erreur) Then Return True
        If File.Exists(fichier) Then
            message = "Le serveur ne répond pas (" & erreur & ") : les dates viennent du dernier catalogue gardé."
            Return True
        End If
        message = "Le catalogue des films n'a pas pu être chargé :" & vbCrLf & erreur
        Return False
    End Function

    ''' <summary>Les films ajoutes au catalogue du serveur pendant la periode, les derniers ajoutes d'abord (date d'ajout lue dans la
    ''' liste : tvg-added). Un film sans date n'est pas une nouveaute. Le catalogue doit etre a jour : voir <see cref="AssurerLesDates"/>
    ''' (a appeler d'abord, en tache de fond). <paramref name="libelle"/> : la periode en toutes lettres.</summary>
    Friend Function NouveauxFilms(source As Integer, periode As PeriodeNouveautes, ByRef libelle As String) As List(Of Entree)
        Dim fichier As String = IptvSources.FichierDuGenre(source, IptvSources.GenreIptv.Films)
        Dim debut As Date, fin As Date
        libelle = BornesDeLaPeriode(periode, debut, fin)
        Dim depuis As Long = New DateTimeOffset(debut).ToUnixTimeSeconds()
        Dim jusqua As Long = New DateTimeOffset(fin).ToUnixTimeSeconds()
        Dim films As List(Of Entree) = Charge(fichier).Entrees.Where(Function(x) x.Contenu() = "Film" AndAlso x.Ajoute >= depuis AndAlso x.Ajoute < jusqua AndAlso (Not QueFrancais OrElse EstFrancais(x))).
                                       OrderByDescending(Function(x) x.Ajoute).ThenBy(Function(x) CleDeTri(Simplifier(x.Nom)), StringComparer.Ordinal).ToList()
        If QueFrancais Then libelle &= " (films en français)"
        DiagLog("Nouveautes films : source " & (source + 1) & ", " & films.Count & " film(s) ajoute(s) " & libelle)
        Return films
    End Function

    ''' <summary>Remplit le tableau d'un coup (titre, adresse, logo : les colonnes lues par Nflix ; puis le genre : chaine, film ou serie).</summary>
    Private ReadOnly _groupeParAdresse As New Dictionary(Of String, String)(StringComparer.Ordinal)

    ''' <summary>Le groupe d'ou vient un film de la liste affichee (22/10/2026 : "quand je clique sur un film, selectionne le groupe pour savoir d'ou il vient"), "" s'il n'est pas connu.</summary>
    Friend Function GroupeDeLAdresse(adresse As String) As String
        Dim g As String = ""
        Return If(adresse IsNot Nothing AndAlso _groupeParAdresse.TryGetValue(adresse, g), g, "")
    End Function

    Friend Sub RemplirLaGrille(grille As DataGridView, entrees As List(Of Entree))
        _groupeParAdresse.Clear()
        For Each e As Entree In entrees
            _groupeParAdresse(e.Adresse) = e.Groupe
        Next
        grille.SuspendLayout()
        Try
            grille.Rows.Clear()
            Dim lignes(entrees.Count - 1) As DataGridViewRow
            For i As Integer = 0 To entrees.Count - 1
                Dim r As New DataGridViewRow()
                r.CreateCells(grille, entrees(i).Nom, entrees(i).Adresse, entrees(i).Logo, entrees(i).Contenu())
                ' Film tres bien note (note de la liste, si elle en a) : fond vert (19/10/2026).
                If entrees(i).Contenu() = "Film" Then ColorerSelonLaNote(r, entrees(i).Note)
                lignes(i) = r
            Next
            If lignes.Length > 0 Then grille.Rows.AddRange(lignes)
        Finally
            grille.ResumeLayout()
        End Try
        ' Les notes en direct du serveur, pour les listes qui n'ont pas encore les notes (voir IptvSources.NotesDuGroupe).
        Dim fenetre As IFenetreNotes = TryCast(grille.FindForm(), IFenetreNotes)
        If fenetre IsNot Nothing AndAlso entrees.Count > 0 Then fenetre.ColorerSelonLesNotes(entrees)
    End Sub

    ''' <summary>Au-dessus de cette note (sur 10), un film est excellent : fond rose (demande de Jacob, 19/10/2026 ; bleu avant le 22/10).</summary>
    Friend Const NoteExcellente As Double = 9.0

    ''' <summary>Fond rose (note au-dessus de <see cref="NoteExcellente"/>) ou vert (<see cref="NoteTresBonne"/> et plus), texte
    ''' blanc ; une note plus basse ne change rien.</summary>
    Friend Sub ColorerSelonLaNote(ligne As DataGridViewRow, note As Double)
        If note > NoteExcellente Then
            ligne.DefaultCellStyle.BackColor = Color.FromArgb(200, 40, 120)         ' rose (22/10/2026 : etait bleu)
            ligne.DefaultCellStyle.ForeColor = Color.White
        ElseIf note >= NoteTresBonne Then
            ligne.DefaultCellStyle.BackColor = Color.FromArgb(0, 110, 50)
            ligne.DefaultCellStyle.ForeColor = Color.White
        ElseIf note >= NoteBonne Then
            ligne.DefaultCellStyle.BackColor = Color.FromArgb(221, 208, 247)         ' violet leger (22/10/2026)
            ligne.DefaultCellStyle.ForeColor = Color.FromArgb(50, 20, 100)
        End If
    End Sub
    ''' <summary>Couleur d'une ligne d'apres la note IMDb (22/10/2026) : elle remplace celle de la note du serveur, moins fiable. Rose au-dessus de <see cref="NoteExcellente"/>,
    ''' vert a partir de <see cref="NoteTresBonne"/>, aucune couleur en dessous.</summary>
    ''' <summary>IMDb note plus severement que le serveur : les seuils de couleur sont abaisses de 1 point (22/10/2026 : "enleve 1 pour les couleurs IMDb") : rose au-dessus de 8, vert a partir de 7.</summary>
    Friend Const EcartImdb As Double = 1.0

    Friend Sub ColorerSelonLaNoteImdb(ligne As DataGridViewRow, note As Double)
        ligne.DefaultCellStyle.BackColor = Color.Empty
        ligne.DefaultCellStyle.ForeColor = Color.Empty
        ColorerSelonLaNote(ligne, note + EcartImdb)
    End Sub


End Module
