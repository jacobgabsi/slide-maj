Option Strict On
Option Explicit On

Imports System.IO
Imports System.Net.Http
Imports System.Text
Imports System.Text.Json
Imports System.Threading

''' <summary>
''' Annuaire de radios sur Internet (radio-browser.info, annuaire communautaire gratuit, sans cle) : propose des radios a ajouter a la liste
''' (20/10/2026, bouton "+" du cadre Radio). Les radios sont triees par popularite ; celles que l'annuaire sait hors service sont ecartees.
''' L'annuaire a plusieurs serveurs : le premier qui repond est utilise.
''' </summary>
Friend Module AnnuaireRadios

    Friend Class RadioProposee
        Friend Nom As String = ""
        Friend Adresse As String = ""
        Friend Format As String = ""
        Friend Pays As String = ""
        Friend Ecoutes As Integer
    End Class

    Private ReadOnly Serveurs As String() = {"de1", "nl1", "at1"}
    Private ReadOnly _http As New HttpClient With {.Timeout = TimeSpan.FromSeconds(20)}

    ''' <summary>Pays proposes : libelle -> code ISO ("" : tous).</summary>
    Friend ReadOnly Pays As New List(Of KeyValuePair(Of String, String)) From {
        New KeyValuePair(Of String, String)("France", "FR"),
        New KeyValuePair(Of String, String)("Belgique", "BE"),
        New KeyValuePair(Of String, String)("Suisse", "CH"),
        New KeyValuePair(Of String, String)("Canada", "CA"),
        New KeyValuePair(Of String, String)("Israël", "IL"),
        New KeyValuePair(Of String, String)("Royaume-Uni", "GB"),
        New KeyValuePair(Of String, String)("États-Unis", "US"),
        New KeyValuePair(Of String, String)("Espagne", "ES"),
        New KeyValuePair(Of String, String)("Italie", "IT"),
        New KeyValuePair(Of String, String)("Allemagne", "DE"),
        New KeyValuePair(Of String, String)("Tous les pays", "")}

    ''' <summary>Styles proposes : libelle -> mot-cle de l'annuaire ("" : tous).</summary>
    Friend ReadOnly Styles As New List(Of KeyValuePair(Of String, String)) From {
        New KeyValuePair(Of String, String)("Tous les styles", ""),
        New KeyValuePair(Of String, String)("Info et actualité", "news"),
        New KeyValuePair(Of String, String)("Pop", "pop"),
        New KeyValuePair(Of String, String)("Rock", "rock"),
        New KeyValuePair(Of String, String)("Variété française", "variété"),
        New KeyValuePair(Of String, String)("Hits du moment", "hits"),
        New KeyValuePair(Of String, String)("Années 80", "80s"),
        New KeyValuePair(Of String, String)("Années 90", "90s"),
        New KeyValuePair(Of String, String)("Jazz", "jazz"),
        New KeyValuePair(Of String, String)("Classique", "classical"),
        New KeyValuePair(Of String, String)("Électro et dance", "dance"),
        New KeyValuePair(Of String, String)("Rap et hip-hop", "hip-hop"),
        New KeyValuePair(Of String, String)("Chill et détente", "chillout"),
        New KeyValuePair(Of String, String)("Musique du monde", "world"),
        New KeyValuePair(Of String, String)("Latino", "latin"),
        New KeyValuePair(Of String, String)("Talk et culture", "talk")}

    ''' <summary>Cherche des radios. <paramref name="pays"/> : code ISO ou "" ; <paramref name="nom"/> et <paramref name="style"/> : filtres
    ''' ("" : aucun). Au plus <paramref name="limite"/> radios, les plus ecoutees d'abord. Leve une erreur si aucun serveur ne repond.</summary>
    Friend Async Function Chercher(pays As String, nom As String, style As String, limite As Integer, annulation As CancellationToken) As Task(Of List(Of RadioProposee))
        Dim requete As New StringBuilder("/json/stations/search?order=clickcount&reverse=true&hidebroken=true&limit=" & (limite * 2))
        If pays <> "" Then requete.Append("&countrycode=").Append(Uri.EscapeDataString(pays))
        If nom.Trim() <> "" Then requete.Append("&name=").Append(Uri.EscapeDataString(nom.Trim()))
        If style <> "" Then requete.Append("&tag=").Append(Uri.EscapeDataString(style))

        Dim derniere As Exception = Nothing
        For Each serveur As String In Serveurs
            Try
                Using demande As New HttpRequestMessage(HttpMethod.Get, "https://" & serveur & ".api.radio-browser.info" & requete.ToString())
                    demande.Headers.TryAddWithoutValidation("User-Agent", "Slide/8 (diaporama)")
                    Using reponse As HttpResponseMessage = Await _http.SendAsync(demande, annulation)
                        reponse.EnsureSuccessStatusCode()
                        Dim json As String = Await reponse.Content.ReadAsStringAsync(annulation)
                        Return Lire(json, limite)
                    End Using
                End Using
            Catch ex As OperationCanceledException
                Throw
            Catch ex As Exception
                derniere = ex
            End Try
        Next
        Throw New IOException("L'annuaire des radios ne répond pas" & If(derniere Is Nothing, "", " (" & derniere.Message & ")"))
    End Function

    Private Function Lire(json As String, limite As Integer) As List(Of RadioProposee)
        Dim liste As New List(Of RadioProposee)
        Dim vus As New HashSet(Of String)(StringComparer.OrdinalIgnoreCase)
        Using doc As JsonDocument = JsonDocument.Parse(json)
            If doc.RootElement.ValueKind <> JsonValueKind.Array Then Return liste
            For Each e As JsonElement In doc.RootElement.EnumerateArray()
                Dim nom As String = Texte(e, "name").Trim()
                Dim adresse As String = Texte(e, "url_resolved").Trim()
                If adresse = "" Then adresse = Texte(e, "url").Trim()
                If nom = "" OrElse adresse = "" Then Continue For
                If Not (adresse.StartsWith("http://", StringComparison.OrdinalIgnoreCase) OrElse adresse.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) Then Continue For
                If adresse.IndexOf(" "c) >= 0 Then Continue For
                ' Nom propre (48 caracteres au plus : la liste le range sur 49) et une seule radio par nom.
                nom = nom.Replace(vbTab, " ")
                If nom.Length > 48 Then nom = nom.Substring(0, 48).TrimEnd()
                If Not vus.Add(nom) Then Continue For
                Dim codec As String = Texte(e, "codec")
                Dim debit As String = Texte(e, "bitrate")
                Dim format As String = (codec & If(debit <> "" AndAlso debit <> "0", " " & debit & " kb/s", "")).Trim()
                Dim ecoutes As Integer = 0
                Integer.TryParse(Texte(e, "clickcount"), ecoutes)
                liste.Add(New RadioProposee With {.Nom = nom, .Adresse = adresse, .Format = If(format = "", "—", format), .Pays = Texte(e, "countrycode"), .Ecoutes = ecoutes})
                If liste.Count >= limite Then Exit For
            Next
        End Using
        Return liste
    End Function

    Private Function Texte(e As JsonElement, nom As String) As String
        Dim v As JsonElement = Nothing
        If e.ValueKind <> JsonValueKind.Object OrElse Not e.TryGetProperty(nom, v) Then Return ""
        Select Case v.ValueKind
            Case JsonValueKind.String : Return If(v.GetString(), "")
            Case JsonValueKind.Number : Return v.GetRawText()
            Case Else : Return ""
        End Select
    End Function

End Module
