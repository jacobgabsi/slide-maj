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
Friend Module RechercheNflix

    ''' <summary>Au plus ce nombre de resultats dans le tableau (au dela, la recherche est a preciser).</summary>
    Friend Const MaximumDeResultats As Integer = 3000

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
        Return Rechercher(fichiers, terme, maximum, True, tronque, groupesTrouves, dansLeGroupe)
    End Function

    Private Function Rechercher(fichiers As IList(Of String), terme As String, maximum As Integer, avecLesGroupes As Boolean,
                                ByRef tronque As Boolean, ByRef groupesTrouves As List(Of String),
                                Optional dansLeGroupe As String = Nothing) As List(Of Entree)
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
                    If i = 0 AndAlso contientTout(groupesSimples(g)) Then groupesTrouves.Add(g)    ' (la liste affichee)
                Next
            Next
        End If

        Dim trouves As New List(Of KeyValuePair(Of Integer, Entree))
        Dim toutes As IEnumerable(Of Entree) = memoires.SelectMany(Function(mm) mm.Entrees)
        For Each e As Entree In toutes
            ' "Dans ce groupe" (15/10/2026) : seulement les titres du groupe choisi.
            If dansLeGroupe IsNot Nothing AndAlso Not String.Equals(e.Groupe, dansLeGroupe, StringComparison.Ordinal) Then Continue For
            If e.NomSimple Is Nothing Then e.NomSimple = Simplifier(e.Nom)
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

    ''' <summary>Remplit le tableau d'un coup (titre, adresse, logo : les colonnes lues par Nflix ; puis le genre : chaine, film ou serie).</summary>
    Friend Sub RemplirLaGrille(grille As DataGridView, entrees As List(Of Entree))
        grille.SuspendLayout()
        Try
            grille.Rows.Clear()
            Dim lignes(entrees.Count - 1) As DataGridViewRow
            For i As Integer = 0 To entrees.Count - 1
                Dim r As New DataGridViewRow()
                r.CreateCells(grille, entrees(i).Nom, entrees(i).Adresse, entrees(i).Logo, entrees(i).Contenu())
                lignes(i) = r
            Next
            If lignes.Length > 0 Then grille.Rows.AddRange(lignes)
        Finally
            grille.ResumeLayout()
        End Try
    End Sub

End Module
