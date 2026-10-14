Option Strict On
Option Explicit On

Imports System.Net.Http
Imports System.Net.Sockets
Imports System.Text
Imports System.Text.RegularExpressions
Imports System.Threading

''' <summary>
''' Verifie qu'une radio de la liste repond (20/10/2026 : "verifier que toutes les radios marchent") SANS la jouer : on se connecte a
''' l'adresse, on lit la reponse et quelques octets. Une radio marche si le serveur repond "200" (ou "ICY 200", la reponse des serveurs
''' Shoutcast, que HttpClient refuse) et envoie des donnees. Les listes .m3u / .pls sont des petits fichiers texte : la reponse suffit.
''' Une adresse en https est controlee par HttpClient ; en http, par une connexion directe (qui comprend aussi "ICY 200").
''' </summary>
Friend Module VerificationRadios

    Friend Class Resultat
        Friend Ok As Boolean
        Friend Detail As String = ""
    End Class

    Private ReadOnly _client As New HttpClient(New HttpClientHandler With {.AllowAutoRedirect = True, .MaxAutomaticRedirections = 5}) With {.Timeout = Timeout.InfiniteTimeSpan}

    ''' <summary>Delai accorde a chaque radio (connexion, reponse et premieres donnees).</summary>
    Friend Const DelaiParRadioMs As Integer = 12000

    ''' <summary>Controle une adresse. Ne leve jamais d'erreur : le probleme est dans <see cref="Resultat.Detail"/>.</summary>
    Friend Async Function Verifier(adresse As String, annulation As CancellationToken) As Task(Of Resultat)
        Dim res As New Resultat
        Try
            Dim uri As Uri = Nothing
            If Not Uri.TryCreate(If(adresse, "").Trim(), UriKind.Absolute, uri) OrElse (uri.Scheme <> "http" AndAlso uri.Scheme <> "https") Then
                res.Detail = "adresse invalide"
                Return res
            End If
            Using delai As New CancellationTokenSource(DelaiParRadioMs)
                Using lien As CancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(delai.Token, annulation)
                    Try
                        If uri.Scheme = "https" Then
                            Await VerifierHttpClient(uri, res, lien.Token)
                        Else
                            Await VerifierConnexion(uri, res, lien.Token)
                        End If
                    Catch ex As OperationCanceledException
                        res.Ok = False
                        res.Detail = If(annulation.IsCancellationRequested, "verification arrêtée", "pas de réponse en " & (DelaiParRadioMs \ 1000) & " s")
                    End Try
                End Using
            End Using
        Catch ex As Exception
            res.Ok = False
            res.Detail = Court(ex)
        End Try
        Return res
    End Function

    Private Function Court(ex As Exception) As String
        Dim e As Exception = ex
        Do While e.InnerException IsNot Nothing
            e = e.InnerException
        Loop
        Dim t As String = e.Message.Replace(vbCr, " ").Replace(vbLf, " ").Trim()
        Return If(t.Length > 80, t.Substring(0, 80) & "…", t)
    End Function

    ''' <summary>https : HttpClient, entetes puis premiers octets.</summary>
    Private Async Function VerifierHttpClient(uri As Uri, res As Resultat, ct As CancellationToken) As Task
        Using req As New HttpRequestMessage(HttpMethod.Get, uri)
            req.Headers.TryAddWithoutValidation("User-Agent", "VLC/3.0.20 LibVLC/3.0.20")
            req.Headers.TryAddWithoutValidation("Icy-MetaData", "0")
            Using rep As HttpResponseMessage = Await _client.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct)
                If Not rep.IsSuccessStatusCode Then
                    res.Detail = "le serveur répond " & CInt(rep.StatusCode) & " " & rep.ReasonPhrase
                    Return
                End If
                Using flux As IO.Stream = Await rep.Content.ReadAsStreamAsync(ct)
                    Dim tampon(1023) As Byte
                    Dim lus As Integer = Await flux.ReadAsync(tampon.AsMemory(0, tampon.Length), ct)
                    If lus <= 0 Then
                        res.Detail = "le serveur n'envoie aucune donnée"
                        Return
                    End If
                    Dim typeContenu As String = If(rep.Content.Headers.ContentType?.MediaType, "")
                    If typeContenu.StartsWith("text/html", StringComparison.OrdinalIgnoreCase) Then
                        res.Detail = "le serveur envoie une page web, pas un flux"
                        Return
                    End If
                    res.Ok = True
                    res.Detail = "répond (" & If(typeContenu = "", "flux", typeContenu) & ")"
                End Using
            End Using
        End Using
    End Function

    ''' <summary>http : connexion directe ; la 1re ligne de la reponse dit "HTTP/1.x 200" ou "ICY 200 OK" (Shoutcast).</summary>
    Private Async Function VerifierConnexion(uri As Uri, res As Resultat, ct As CancellationToken) As Task
        Using client As New TcpClient()
            Await client.ConnectAsync(uri.Host, uri.Port, ct)
            Using flux As NetworkStream = client.GetStream()
                Dim demande As String = "GET " & uri.PathAndQuery & " HTTP/1.0" & vbCrLf & "Host: " & uri.Authority & vbCrLf &
                                        "User-Agent: VLC/3.0.20 LibVLC/3.0.20" & vbCrLf & "Icy-MetaData: 0" & vbCrLf & "Connection: close" & vbCrLf & vbCrLf
                Dim octetsDemande As Byte() = Encoding.ASCII.GetBytes(demande)
                Await flux.WriteAsync(octetsDemande.AsMemory(0, octetsDemande.Length), ct)

                ' La reponse : entetes puis premiers octets (jusqu'a 4 Ko).
                Dim tampon(4095) As Byte
                Dim total As Integer = 0
                Do While total < tampon.Length
                    Dim lus As Integer = Await flux.ReadAsync(tampon.AsMemory(total, tampon.Length - total), ct)
                    If lus <= 0 Then Exit Do
                    total += lus
                    If total >= 1024 OrElse Encoding.ASCII.GetString(tampon, 0, total).Contains(vbCrLf & vbCrLf) Then Exit Do
                Loop
                If total = 0 Then
                    res.Detail = "le serveur ferme la connexion sans répondre"
                    Return
                End If
                Dim texte As String = Encoding.ASCII.GetString(tampon, 0, total)
                Dim premiere As String = texte.Split({ControlChars.Cr, ControlChars.Lf}, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault()
                premiere = If(premiere, "").Trim()

                Dim m As Match = Regex.Match(premiere, "^(?:ICY|HTTP/\d\.\d)\s+(\d{3})", RegexOptions.IgnoreCase)
                If Not m.Success Then
                    res.Detail = "réponse inconnue"
                    Return
                End If
                Dim code As Integer = CInt(m.Groups(1).Value)
                If code >= 300 AndAlso code < 400 Then
                    ' Redirection : suivie par HttpClient (adresse https ou autre serveur).
                    Dim lieu As Match = Regex.Match(texte, "(?im)^Location:\s*(\S+)")
                    Dim cible As Uri = Nothing
                    If lieu.Success AndAlso Uri.TryCreate(uri, lieu.Groups(1).Value, cible) AndAlso cible.AbsoluteUri <> uri.AbsoluteUri Then
                        Await VerifierHttpClient(cible, res, ct)
                        Return
                    End If
                End If
                If code < 200 OrElse code >= 300 Then
                    res.Detail = "le serveur répond " & code
                    Return
                End If
                If Regex.IsMatch(texte, "(?im)^Content-Type:\s*text/html") Then
                    res.Detail = "le serveur envoie une page web, pas un flux"
                    Return
                End If
                res.Ok = True
                res.Detail = "répond (" & premiere & ")"
            End Using
        End Using
    End Function

End Module
