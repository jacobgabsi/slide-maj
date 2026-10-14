Option Strict On
Option Explicit On

Imports System.IO
Imports System.Net.Http
Imports System.Text.Json
Imports System.Text.RegularExpressions

''' <summary>
''' Les notes de la concurrence d'un film (IMDb, Rotten Tomatoes, Metacritic) par OMDb (omdbapi.com), pour les comparer a celle du
''' serveur (fenetre "Plus d'infos" de Nflix, 19/10/2026). Allocine n'a pas d'API publique : il n'est pas interroge.
'''
''' La cle d'acces (gratuite, 1 000 requetes par jour) est lue dans data\settings\omdb.txt, une ligne : elle n'est JAMAIS dans le
''' programme ni dans les fichiers publies (data\settings n'est pas publie), et n'est jamais ecrite dans le journal.
''' Le film est cherche par son titre original (ou son nom nettoye des mentions "4K-FR-HDR", "(2026)", "MULTI"...) et son annee.
''' </summary>
Friend Module NotesOmdb

    Private ReadOnly _client As New HttpClient With {.Timeout = TimeSpan.FromSeconds(15)}
    Private ReadOnly _gardees As New Dictionary(Of String, String)(StringComparer.OrdinalIgnoreCase)

    ''' <summary>Fichier de la cle : data\settings\omdb.txt.</summary>
    Friend Function FichierDeLaCle() As String
        Return Path.Combine(DossierReglages, "omdb.txt")
    End Function

    ''' <summary>La cle OMDb ("" si le fichier est absent ou vide).</summary>
    Friend Function CleDAcces() As String
        Try
            If File.Exists(FichierDeLaCle()) Then
                For Each ligne As String In File.ReadLines(FichierDeLaCle())
                    Dim t As String = ligne.Trim()
                    If t <> "" AndAlso Not t.StartsWith("#", StringComparison.Ordinal) Then Return t
                Next
            End If
        Catch ex As Exception
        End Try
        Return ""
    End Function

    ''' <summary>Le nom d'un titre du catalogue sans prefixe de langue ("4K-FR-HDR - "), annee, crochets ni mentions techniques.</summary>
    Friend Function TitreNettoye(nom As String) As String
        Dim t As String = If(nom, "")
        t = Regex.Replace(t, "^\s*[A-Za-z0-9\-\|:\. ]{2,14}\s[-|:]\s+", "")                ' "4K-FR-HDR - ", "FR - "
        t = Regex.Replace(t, "\[[^\]]*\]", " ")
        t = Regex.Replace(t, "\((?:19|20)\d\d\)", " ")
        t = Regex.Replace(t, "\b(?:UHD|FHD|HD|SD|4K|HDR|MULTI|VFF|VFQ|VF|VOSTFR|TRUEFRENCH|FRENCH|BLURAY|WEB|DOLBY)\b", " ", RegexOptions.IgnoreCase)
        Return Regex.Replace(t, "\s{2,}", " ").Trim(" "c, "-"c, "."c)
    End Function

    ''' <summary>L'annee de sortie lue dans un nom ("(2024)"), "" sinon.</summary>
    Friend Function AnneeDuNom(nom As String) As String
        Dim m As Match = Regex.Match(If(nom, ""), "\(((?:19|20)\d\d)\)")
        Return If(m.Success, m.Groups(1).Value, "")
    End Function

    ''' <summary>Les notes de la concurrence, une par ligne ("IMDb : 7.8 / 10 (123 456 votes)"). Dit pourquoi si elles manquent.
    ''' A appeler en tache de fond (requetes reseau).</summary>
    Friend Function Chercher(cle As String, titreOriginal As String, nomAffiche As String, annee As String) As String
        If cle = "" Then Return "Notes IMDb, Rotten Tomatoes, Metacritic : clé OMDb absente." & vbCrLf &
                                "  1. Créez une clé gratuite sur https://www.omdbapi.com/apikey.aspx" & vbCrLf &
                                "  2. Écrivez-la seule sur une ligne dans : " & FichierDeLaCle()
        Dim an As String = If(annee <> "", annee, AnneeDuNom(nomAffiche))
        Dim titres As New List(Of String)
        If Not String.IsNullOrWhiteSpace(titreOriginal) Then titres.Add(titreOriginal.Trim())
        Dim propre As String = TitreNettoye(nomAffiche)
        If propre <> "" AndAlso Not titres.Contains(propre, StringComparer.OrdinalIgnoreCase) Then titres.Add(propre)
        If titres.Count = 0 Then Return "Notes IMDb, Rotten Tomatoes, Metacritic : titre inconnu."

        Dim cleDeCache As String = String.Join("|", titres) & "|" & an
        SyncLock _gardees
            Dim deja As String = Nothing
            If _gardees.TryGetValue(cleDeCache, deja) Then Return deja
        End SyncLock

        Dim erreur As String = ""
        For Each titre As String In titres
            For Each avecAnnee As Boolean In {True, False}
                If avecAnnee AndAlso an = "" Then Continue For
                Dim texte As String = Interroger(cle, titre, If(avecAnnee, an, ""), erreur)
                If texte <> "" Then
                    SyncLock _gardees
                        _gardees(cleDeCache) = texte
                    End SyncLock
                    Return texte
                End If
                If erreur.StartsWith("limite", StringComparison.Ordinal) OrElse erreur.StartsWith("cle", StringComparison.Ordinal) OrElse erreur.StartsWith("reseau", StringComparison.Ordinal) Then
                    Return "Notes IMDb, Rotten Tomatoes, Metacritic : " & DecrireErreur(erreur)
                End If
            Next
        Next
        Return "Notes IMDb, Rotten Tomatoes, Metacritic : film introuvable sur OMDb (« " & titres(0) & If(an <> "", " " & an, "") & " »)."
    End Function

    ' ------------------------------------------------------------------------------------------
    ' Note IMDb seule (22/10/2026 : "la note du serveur differe beaucoup de celle d'IMDb ; en tache de fond, change la couleur des lignes qui ne correspondent pas").
    ' Gardee dans data\settings\imdb_notes.txt (une ligne "titre|annee<TAB>note") : un film deja interroge ne coute plus de requete (limite de 1 000 par jour).
    ' ------------------------------------------------------------------------------------------

    Private ReadOnly _imdb As New Dictionary(Of String, Double)(StringComparer.OrdinalIgnoreCase)
    Private _imdbLu As Boolean

    Private Function FichierDesNotes() As String
        Return Path.Combine(DossierReglages, "imdb_notes.txt")
    End Function

    Private Sub LireLesNotesGardees()
        SyncLock _imdb
            If _imdbLu Then Exit Sub
            _imdbLu = True
            Try
                If File.Exists(FichierDesNotes()) Then
                    For Each ligne As String In File.ReadLines(FichierDesNotes())
                        Dim p As Integer = ligne.LastIndexOf(ChrW(9))
                        Dim x As Double
                        If p > 0 AndAlso Double.TryParse(ligne.Substring(p + 1), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, x) Then _imdb(ligne.Substring(0, p)) = x
                    Next
                End If
            Catch ex As Exception
            End Try
        End SyncLock
    End Sub

    ''' <summary>La note IMDb (sur 10) d'un film du catalogue, 0 si OMDb ne la connait pas, -1 si OMDb ne repond pas (limite du jour, cle refusee, reseau : inutile d'insister).
    ''' A appeler en tache de fond.</summary>
    Friend Function NoteImdb(cle As String, nomAffiche As String) As Double
        Dim propre As String = TitreNettoye(nomAffiche)
        If propre = "" Then Return 0
        Dim id As String = propre.ToLowerInvariant() & "|" & AnneeDuNom(nomAffiche)
        LireLesNotesGardees()
        SyncLock _imdb
            Dim deja As Double
            If _imdb.TryGetValue(id, deja) Then Return deja
        End SyncLock
        Dim texte As String = Chercher(cle, "", nomAffiche, "")
        Dim m As Match = Regex.Match(If(texte, ""), "IMDb : (\d+(?:[.,]\d+)?) / 10")
        Dim note As Double = 0
        If m.Success Then
            Double.TryParse(m.Groups(1).Value.Replace(",", "."), Globalization.NumberStyles.Float, Globalization.CultureInfo.InvariantCulture, note)
        ElseIf texte.Contains("limite", StringComparison.Ordinal) OrElse texte.Contains("refusée", StringComparison.Ordinal) OrElse texte.Contains("ne répond pas", StringComparison.Ordinal) Then
            Return -1
        End If
        SyncLock _imdb
            _imdb(id) = note
            Try
                File.AppendAllText(FichierDesNotes(), id & ChrW(9) & note.ToString(Globalization.CultureInfo.InvariantCulture) & vbCrLf)
            Catch ex As Exception
            End Try
        End SyncLock
        Return note
    End Function

    Private Function DecrireErreur(erreur As String) As String
        If erreur.StartsWith("limite", StringComparison.Ordinal) Then Return "limite de 1 000 requêtes par jour atteinte."
        If erreur.StartsWith("cle", StringComparison.Ordinal) Then Return "clé OMDb refusée (vérifiez " & FichierDeLaCle() & ")."
        Return "OMDb ne répond pas (" & erreur.Replace("reseau : ", "") & ")."
    End Function

    ''' <summary>Une requete OMDb. Rend le texte des notes, ou "" avec <paramref name="erreur"/> ("introuvable", "limite...", "cle...", "reseau : ...").</summary>
    Private Function Interroger(cle As String, titre As String, annee As String, ByRef erreur As String) As String
        erreur = ""
        Try
            Dim adresse As String = "https://www.omdbapi.com/?apikey=" & Uri.EscapeDataString(cle) & "&type=movie&t=" & Uri.EscapeDataString(titre) &
                                    If(annee <> "", "&y=" & Uri.EscapeDataString(annee), "")
            Dim json As String = _client.GetStringAsync(adresse).GetAwaiter().GetResult()
            Using doc As JsonDocument = JsonDocument.Parse(json)
                Dim racine As JsonElement = doc.RootElement
                If Texte(racine, "Response") <> "True" Then
                    Dim message As String = Texte(racine, "Error")
                    If message.IndexOf("limit", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        erreur = "limite"
                    ElseIf message.IndexOf("API key", StringComparison.OrdinalIgnoreCase) >= 0 Then
                        erreur = "cle"
                    Else
                        erreur = "introuvable"
                    End If
                    Return ""
                End If
                Dim lignes As New List(Of String)
                lignes.Add("Notes comparées (OMDb) : " & Texte(racine, "Title") & " (" & Texte(racine, "Year") & ")")
                Dim imdb As String = Texte(racine, "imdbRating")
                If imdb <> "" AndAlso imdb <> "N/A" Then
                    Dim votes As String = Texte(racine, "imdbVotes")
                    lignes.Add("  IMDb : " & imdb & " / 10" & If(votes <> "" AndAlso votes <> "N/A", " (" & votes.Replace(",", " ") & " votes)", ""))
                End If
                Dim notes As JsonElement = Nothing
                If racine.TryGetProperty("Ratings", notes) AndAlso notes.ValueKind = JsonValueKind.Array Then
                    For Each n As JsonElement In notes.EnumerateArray()
                        Dim source As String = Texte(n, "Source")
                        Dim valeur As String = Texte(n, "Value")
                        If source = "" OrElse valeur = "" OrElse source.StartsWith("Internet Movie", StringComparison.OrdinalIgnoreCase) Then Continue For
                        lignes.Add("  " & source & " : " & valeur)
                    Next
                End If
                If lignes.Count = 1 Then lignes.Add("  (aucune note publiée pour ce film)")
                Return String.Join(vbCrLf, lignes)
            End Using
        Catch ex As Exception
            erreur = "reseau : " & ex.Message
            Return ""
        End Try
    End Function

    Private Function Texte(e As JsonElement, nom As String) As String
        Dim v As JsonElement = Nothing
        If e.ValueKind <> JsonValueKind.Object OrElse Not e.TryGetProperty(nom, v) Then Return ""
        Return If(v.ValueKind = JsonValueKind.String, If(v.GetString(), ""), "")
    End Function

End Module
